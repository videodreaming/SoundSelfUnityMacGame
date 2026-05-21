using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SoundSelf.Sequence
{
    public class SequenceRunner : MonoBehaviour
    {
        
        [Header("Sequence Definitions (Inspector)")]
#if UNITY_EDITOR
        [Header("Development — Editor only (field and Start() path stripped from non-editor builds)")]
        /// <summary>
        /// If set in the Editor, <see cref="Start"/> runs this sequence instead of CSV / pack resolution.
        /// This field is not compiled into release players.
        /// </summary>
        [FormerlySerializedAs("startDefinition")]
        [SerializeField] private SequenceDefinition definitionOverride;
#endif
        
        [Header("API-callable sequences")]
        [SerializeField] private SequenceDefinition protocolStacksInteractiveDefinition;
        [FormerlySerializedAs("protocolStacksMusicPlaylistDefinition")]
        [SerializeField] private SequenceDefinition protocolStacksMusicPlaylist60mDefinition;
        [SerializeField] private SequenceDefinition protocolStacksMusicPlaylist40mDefinition;

        // Current active definition (can change at runtime via StartSequence/SetDefinition).
        private SequenceDefinition definition;

        public int CurrentStageIndex { get; private set; } = -1;
        public StageType? CurrentStage => CurrentStageIndex >= 0 && definition != null && CurrentStageIndex < definition.StagesOrEmpty.Length
            ? definition.StagesOrEmpty[CurrentStageIndex].type
            : null;

        public event Action<int, StageType> OnStageChanged;
        /// <summary>Fired when the sequence completes (all stages finished).</summary>
        public event Action OnSequenceComplete;

        /// <summary>True when the sequence has finished (no current stage).</summary>
        public bool IsSequenceComplete => _sequenceComplete;

        /// <summary>Number of stages in the current definition, or 0 if none.</summary>
        public int StageCount => definition?.StagesOrEmpty.Length ?? 0;

        private IStageHandler[] _handlers;
        private bool _sequenceComplete;
        private int _transitioningOutStageIndex = -1;

        /// <summary>
        /// Assigns the in-memory sequence definition only. Does <b>not</b> exit active stage handlers, stop Wwise/calibration audio, or advance —
        /// the runner keeps whatever stage index and handlers it already had, now pointed at a new asset (usually wrong at runtime).
        /// To switch sequences from UI (e.g. after calibration), use <see cref="StartSequence"/> with the target definition, or
        /// <see cref="StartProtocolStacksMusicPlaylist40mSequence"/> / <see cref="StartProtocolStacksMusicPlaylist60mSequence"/> / <see cref="StartProtocolStacksInteractiveSequence"/>.
        /// </summary>
        public void SetDefinition(SequenceDefinition def) => definition = def;
        public SequenceDefinition Definition => definition;
        public void SetHandlers(IStageHandler[] handlers) => _handlers = handlers;

        /// <summary>Notifies the current handler of a sequence command. Returns true if a handler was watching and handled it.</summary>
        public bool TryExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            bool logEndStage = sequenceCommand == SequenceCommand.EndThisSequenceStage;
            var current = GetCurrentHandler();
            var transitioning = GetTransitioningOutHandler();
            if (logEndStage)
            {
                bool curWatch = current != null && current.WatchesSequenceCommand(sequenceCommand);
                bool transWatch = transitioning != null && !ReferenceEquals(transitioning, current) && transitioning.WatchesSequenceCommand(sequenceCommand);
                Debug.Log(
                    "SequenceRunner.TryExecute EndThisSequenceStage: " +
                    $"currentIdx={CurrentStageIndex} currentStage={CurrentStage} currentHandler={(current == null ? "null" : current.GetType().Name)} currentWatches={curWatch}; " +
                    $"transitioningIdx={_transitioningOutStageIndex} transitioningHandler={(transitioning == null ? "null" : transitioning.GetType().Name)} transitioningWatches={transWatch}.");
            }

            bool handled = false;
            if (current != null && current.WatchesSequenceCommand(sequenceCommand))
            {
                current.ExecuteSequenceCommand(sequenceCommand);
                handled = true;
            }

            if (transitioning != null && !ReferenceEquals(transitioning, current) && transitioning.WatchesSequenceCommand(sequenceCommand))
            {
                transitioning.ExecuteSequenceCommand(sequenceCommand);
                handled = true;
            }

            if (logEndStage)
                Debug.Log("SequenceRunner.TryExecute EndThisSequenceStage: handled=" + handled + ".");

            return handled;
        }

        /// <summary>
        /// Same completion poll as <see cref="Update"/> so UI-driven <see cref="SequenceCommand.EndThisSequenceStage"/>
        /// can advance the same frame (otherwise <see cref="MonoBehaviour"/> script order can leave the stage stuck until the next frame).
        /// </summary>
        public void TryAdvanceIfCurrentStageComplete()
        {
            if (_sequenceComplete || definition == null || CurrentStageIndex < 0 || _handlers == null)
                return;
            var stages = definition.StagesOrEmpty;
            if (CurrentStageIndex >= stages.Length)
            {
                MarkSequenceComplete();
                return;
            }
            var handler = GetHandlerFor(stages[CurrentStageIndex].type);
            if (handler != null && handler.IsComplete)
            {
                Debug.Log($"SequenceRunner.TryAdvanceIfCurrentStageComplete: advancing from stage index {CurrentStageIndex} ({stages[CurrentStageIndex].type}).");
                TransitionToNextStage();
            }
        }

        public void StartSequence(SequenceDefinition def = null)
        {
            var nextDefinition = def ?? definition;

            if (nextDefinition == null || nextDefinition.StagesOrEmpty.Length == 0)
            {
                Debug.LogWarning("SequenceRunner: Cannot start — no definition or empty stages.");
                MarkSequenceComplete();
                return;
            }
            var sequencer = GetComponent<Sequencer>();

            if (sequencer == null)
            {
                Debug.LogError("SequenceRunner: Sequencer is null. Cannot start sequence lifecycle.");
                return;
            }
            // Hard reset before starting.
            // TimeSincePlaygroundStart must not carry over between sequence runs (Playground.Exit resets it when the stage retires; this covers skips/restarts where Exit may not run).
            var tracker = TimeTrackerScript.instance;
            if (tracker != null)
                tracker.ResetTimeSincePlaygroundStart();
            else
                Debug.LogError("SequenceRunner: TimeTrackerScript.instance is null. Cannot reset TimeSincePlaygroundStart before sequence start.");

            // Exit every registered handler before swapping definitions. Indices only cover current + transitioning
            // stages on the *old* definition — other StageType handlers (same singleton instance) may never have
            // received Exit() during linear play, so they can retain stale state across StartSequence (nested playlists, etc.).
            ForceExitAllHandlers();
            definition = nextDefinition;
            _sequenceComplete = false;
            _transitioningOutStageIndex = -1;
            // Critical: the previous sequence's index must not be used against the new definition's stages[].
            // Otherwise BeginTransitionOut may read the wrong stage or go out of range; Update() can also mark
            // the new sequence complete immediately (index >= new length) without ever entering stage 0 — e.g. 0 countdown for MusicPlaylist.
            CurrentStageIndex = -1;

            var stages0 = definition.StagesOrEmpty;
            if (stages0.Length > 0)
            {
                Debug.Log(
                    $"SequenceRunner.StartSequence: \"{definition.displayName}\" ({stages0.Length} stages) — first stage index 0: {stages0[0].type} / {stages0[0].variant}.");
            }

            AdvanceToStage(0);
        }

        /// <param name="expectNext">If non-null, logs a warning when the next stage does not match. Caller provides expected stage for validation.</param>
        public void AdvanceToNextStage(StageType? expectNext = null)
        {
            TransitionToNextStage(expectNext);
        }

        /// <summary>
        /// Starts the next stage immediately and puts current stage into transition-out.
        /// Older transitioning-out stages are force-completed/exited.
        /// </summary>
        public void TransitionToNextStage(StageType? expectNext = null)
        {
            if (definition == null) return;
            var stages = definition.StagesOrEmpty;
            int next = CurrentStageIndex + 1;
            if (next >= stages.Length)
            {
                if (expectNext != null)
                    Debug.LogWarning("AdvanceToNextStage: No next stage (sequence would complete). Expected " + expectNext + ".");
                ForceExitCurrentAndTransitioningHandlers();
                MarkSequenceComplete();
                return;
            }
            if (expectNext != null)
            {
                var nextStage = stages[next].type;
                if (nextStage != expectNext.Value)
                    Debug.LogWarning("TransitionToNextStage: Expected next stage " + expectNext + ", got " + nextStage);
            }
            AdvanceToStage(next);
        }

        public void AdvanceToStage(int index)
        {
            if (definition == null || index < 0) return;

            var stages = definition.StagesOrEmpty;
            if (index >= stages.Length)
            {
                MarkSequenceComplete();
                return;
            }

            _sequenceComplete = false;  // We're advancing to a valid stage; ensure Update will poll

            // 1) Current stage enters tail logic first.
            if (CurrentStageIndex >= 0 && CurrentStageIndex != index)
            {
                var current = GetHandlerFor(stages[CurrentStageIndex].type);
                current?.BeginTransitionOut();
                _transitioningOutStageIndex = CurrentStageIndex;
            }

            // 2) Any transitioning stage older than immediate previous is exited.
            ForceExitTooOldTransitioningStage(index);

            // 3. Set index
            CurrentStageIndex = index;
            var stage = stages[index];

            // 4. Get handler and enter
            var handler = GetHandlerFor(stage.type);
            if (handler == null)
            {
                Debug.LogWarning($"SequenceRunner: No handler for stage type {stage.type}. Skipping.");
                AdvanceToNextStage();
                return;
            }

            handler.Enter(stage.variant);

            // Re-entrancy guard: a handler's Enter() may call AdvanceToStage itself
            // (e.g. CodeStageHandler routes to SetMenu / End for dual-stage section ends).
            // In that case the inner call has already applied UI state, fired OnStageChanged,
            // and possibly advanced past this stage. Skip the rest so we do not overwrite
            // the inner stage's UI with this (now-stale) stage's UI.
            if (CurrentStageIndex != index)
            {
                Debug.Log($"SequenceRunner.AdvanceToStage: handler for stage {index} ({stage.type}) re-entered runner; current index is now {CurrentStageIndex}. Skipping post-Enter UI/event for the outer stage.");
                return;
            }

            ApplyMenuScreenForSequenceStage(stage.type);
            ApplyDuskBackgroundForSequenceStage(stage.type);

            // 5. Fire event
            OnStageChanged?.Invoke(index, stage.type);
            Debug.Log($"Sequence: Entered stage {index} ({stage.type})");

            // 6. If handler is already complete (e.g. sync), advance immediately
            if (handler.IsComplete)
                TransitionToNextStage();
        }

        private void Start()
        {
            // Start() (not Awake) avoids ordering issues with handler registration in Sequencer.Awake().
#if UNITY_EDITOR
            if (definitionOverride != null)
            {
                Debug.LogWarning(
                    "SequenceRunner: Definition Override is assigned in the Editor — starting from it instead of CSV / pack resolution. This path is not included in release player builds.");
                LogDevelopmentStartBanner("Starting sequence from Definition Override (inspector).");
                StartSequence(definitionOverride);
                return;
            }
#endif
            StartFromCurrentCsvSession();
        }

#if UNITY_EDITOR
        private static void LogDevelopmentStartBanner(string detailLine)
        {
            Debug.Log("SequenceRunner: =========================================================");
            Debug.Log("SequenceRunner: Development sequence start (Editor only, not in player builds):");
            Debug.Log("SequenceRunner: " + detailLine);
            Debug.Log("SequenceRunner: =========================================================");
        }
#endif

        /// <summary>
        /// Starts the sequence from <see cref="HummingbirdContentPackDefinition.SequenceDefinition"/> on <see cref="CSVLoader.ResolvedSessionPack"/>.
        /// </summary>
        public void StartFromCurrentCsvSession()
        {
            var def = GetSequenceDefinitionForCurrentCsvSession();
            if (def != null)
            {
                StartSequence(def);
                return;
            }

            Debug.LogError("SequenceRunner: No SequenceDefinition resolved for current CSV session.");
        }

        /// <summary>Starts the interactive sequence (exposed for adjunctive branch transitions).</summary>
        public void StartProtocolStacksInteractiveSequence() => StartNamedSequence(protocolStacksInteractiveDefinition, nameof(protocolStacksInteractiveDefinition));

        /// <summary>Starts the 60-minute music playlist sequence.</summary>
        public void StartProtocolStacksMusicPlaylist60mSequence() => StartNamedSequence(protocolStacksMusicPlaylist60mDefinition, nameof(protocolStacksMusicPlaylist60mDefinition));

        /// <summary>Starts the 40-minute music playlist sequence.</summary>
        public void StartProtocolStacksMusicPlaylist40mSequence() => StartNamedSequence(protocolStacksMusicPlaylist40mDefinition, nameof(protocolStacksMusicPlaylist40mDefinition));

        /// <summary>
        /// Session startup uses only <see cref="HummingbirdContentPackDefinition.SequenceDefinition"/> on <see cref="CSVLoader.ResolvedSessionPack"/> — no mode-level fallbacks.
        /// </summary>
        private SequenceDefinition GetSequenceDefinitionForCurrentCsvSession()
        {
            var loader = CSVLoader.instance;
            if (loader == null)
            {
                Debug.LogError("SequenceRunner: CSVLoader.instance is null. Cannot resolve session sequence.");
                return null;
            }

            var pack = loader.ResolvedSessionPack;
            if (pack == null)
            {
                Debug.LogError(
                    "SequenceRunner: No resolved session pack — assign HummingbirdContentPackRegistry on CSVLoader and ensure session_params match a registry row.");
                return null;
            }

            if (pack.SequenceDefinition != null)
                return pack.SequenceDefinition;

            Debug.LogError(
                "SequenceRunner: Pack \"" + pack.name + "\" has no sequenceDefinition. Assign it on the HummingbirdContentPackDefinition asset.");
            return null;
        }

        private void StartNamedSequence(SequenceDefinition definitionToStart, string definitionLabel)
        {
            if (definitionToStart == null)
            {
                Debug.LogError("SequenceRunner: Cannot start sequence; missing definition reference: " + definitionLabel + ".");
                return;
            }
            StartSequence(definitionToStart);
        }

        private void Update()
        {
            if (_sequenceComplete || definition == null || CurrentStageIndex < 0 || _handlers == null) return;

            var stages = definition.StagesOrEmpty;
            if (CurrentStageIndex >= stages.Length)
            {
                MarkSequenceComplete();
                return;
            }

            var handler = GetHandlerFor(stages[CurrentStageIndex].type);
            if (handler != null && handler.IsComplete)
                TransitionToNextStage();
        }

        private IStageHandler GetHandlerFor(StageType type)
        {
            if (_handlers == null) return null;
            foreach (var h in _handlers)
            {
                if (h != null && h.StageType == type)
                    return h;
            }
            return null;
        }

        /// <summary>
        /// Maps sequence <see cref="StageType"/> to menu roots: session stages use Meditation Session (Start);
        /// <see cref="StageType.End"/> uses Meditation Session (End). Other stages leave the menu to their handlers.
        /// Uses <see cref="UIManager"/> idempotent setters so repeated enters do not re-trigger fades.
        /// </summary>
        private static void ApplyMenuScreenForSequenceStage(StageType stageType)
        {
            if (UIManager.Instance == null)
                return;
            switch (stageType)
            {
                case StageType.Opening:
                case StageType.Tutorial:
                case StageType.Playground:
                case StageType.Savasana:
                case StageType.MusicPlaylist:
                case StageType.Inquiry:
                case StageType.LinearAudio:
                    UIManager.Instance.SetMeditationScreen();
                    break;
                case StageType.End:
                    UIManager.Instance.SetEndMeditationScreen();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Shows the dusk background on pre-session / menu / end stages; hides it during in-session meditation stages.
        /// Uses <see cref="UIManager.ShowDuskBackground"/> (idempotent) so repeated enters do not re-trigger fades.
        /// </summary>
        private static void ApplyDuskBackgroundForSequenceStage(StageType stageType)
        {
            if (UIManager.Instance == null)
                return;
            switch (stageType)
            {
                case StageType.Calibration:
                case StageType.SetMenu:
                case StageType.End:
                    UIManager.Instance.ShowDuskBackground(true);
                    break;
                case StageType.LinearAudio:
                case StageType.MusicPlaylist:
                case StageType.Inquiry:
                case StageType.Opening:
                case StageType.Tutorial:
                case StageType.Playground:
                case StageType.Savasana:
                case StageType.StartCountdown:
                    UIManager.Instance.ShowDuskBackground(false);
                    break;
                case StageType.Code:
                    break;
            }
        }

        private IStageHandler GetCurrentHandler()
        {
            var stage = CurrentStage;
            return stage.HasValue ? GetHandlerFor(stage.Value) : null;
        }

        /// <summary>Invokes <see cref="IStageHandler.OnSessionSkipFromUi"/> on the current stage handler (meditation skip — cleanup before <see cref="SequenceCommand.EndThisSequenceStage"/>).</summary>
        public void NotifyCurrentStageSessionSkipFromUi()
        {
            if (_sequenceComplete || definition == null || CurrentStageIndex < 0 || _handlers == null)
                return;
            var stages = definition.StagesOrEmpty;
            if (CurrentStageIndex >= stages.Length)
                return;
            var handler = GetHandlerFor(stages[CurrentStageIndex].type);
            handler?.OnSessionSkipFromUi();
        }

        private IStageHandler GetTransitioningOutHandler()
        {
            if (_transitioningOutStageIndex < 0 || definition == null) return null;
            var stages = definition.StagesOrEmpty;
            if (_transitioningOutStageIndex >= stages.Length) return null;
            return GetHandlerFor(stages[_transitioningOutStageIndex].type);
        }

        private void ForceExitTooOldTransitioningStage(int newCurrentIndex)
        {
            if (_transitioningOutStageIndex < 0) return;
            if (_transitioningOutStageIndex >= newCurrentIndex - 1) return;

            var transitioning = GetTransitioningOutHandler();
            if (transitioning != null)
            {
                transitioning.Exit();
            }
            _transitioningOutStageIndex = -1;
        }

        /// <summary>
        /// Calls <see cref="IStageHandler.Exit"/> on every handler registered with <see cref="SetHandlers"/>.
        /// Use when abandoning the current definition (e.g. <see cref="StartSequence"/>) so per-<see cref="StageType"/> singletons do not keep state from a stage that never received a runner-driven Exit.
        /// </summary>
        private void ForceExitAllHandlers()
        {
            if (_handlers == null)
                return;
            foreach (var handler in _handlers)
                handler?.Exit();
        }

        private void ForceExitCurrentAndTransitioningHandlers()
        {
            if (definition == null) return;
            var stages = definition.StagesOrEmpty;

            if (_transitioningOutStageIndex >= 0 && _transitioningOutStageIndex < stages.Length)
            {
                var prev = GetHandlerFor(stages[_transitioningOutStageIndex].type);
                prev?.Exit();
            }

            if (CurrentStageIndex >= 0 && CurrentStageIndex < stages.Length)
            {
                var current = GetHandlerFor(stages[CurrentStageIndex].type);
                current?.Exit();
            }
        }

        private void MarkSequenceComplete()
        {
            if (_sequenceComplete) return;
            _sequenceComplete = true;
            CurrentStageIndex = -1;
            _transitioningOutStageIndex = -1;
            Debug.Log("SequenceRunner: Sequence complete.");
            OnSequenceComplete?.Invoke();
        }
    }
}
