using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SoundSelf.Sequence
{
    public class SequenceRunner : MonoBehaviour
    {
        
        [Header("Sequence Definitions (Inspector)")]
        [Header("Development (Starts on Awake) — Editor only; clear before release.")]
        /// <summary>
        /// Editor-only: if set, this sequence starts instead of resolving from the CSV session / pack SO.
        /// Pack impersonation lives on <see cref="CSVLoader"/> (<c>hummingbirdContentPackOverride</c>); this field only overrides which <see cref="SequenceDefinition"/> runs first.
        /// </summary>
        [FormerlySerializedAs("startDefinition")]
        [SerializeField] private SequenceDefinition definitionOverride;
        
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

            // Exit currently active handlers before swapping definitions; otherwise indices can resolve against the wrong stage list.
            ForceExitCurrentAndTransitioningHandlers();
            definition = nextDefinition;
            _sequenceComplete = false;
            _transitioningOutStageIndex = -1;
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

            ApplyMenuScreenForSequenceStage(stage.type);

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
                Debug.LogError(
                    "SequenceRunner: Definition Override is active (Editor only). Clear the field before shipping; it forces this SequenceDefinition instead of CSV / pack SO resolution.");
                LogDevelopmentStartBanner("Starting sequence from Definition Override (inspector).");
                StartSequence(definitionOverride);
                return;
            }
#endif
            StartFromCurrentCsvSession();
        }

        private static void LogDevelopmentStartBanner(string detailLine)
        {
            Debug.LogWarning("SequenceRunner: =========================================================");
            Debug.LogWarning("SequenceRunner: DEVELOPMENT BEHAVIOR, REMOVE THIS BEFORE RELEASE:");
            Debug.LogWarning("SequenceRunner: " + detailLine);
            Debug.LogWarning("SequenceRunner: =========================================================");
        }

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

        private IStageHandler GetCurrentHandler()
        {
            var stage = CurrentStage;
            return stage.HasValue ? GetHandlerFor(stage.Value) : null;
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
