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
        
        [Header("Adjunctive")]
        [SerializeField] private SequenceDefinition protocolStacksCalibrationDefinition;
        [SerializeField] private SequenceDefinition protocolStacksInteractiveDefinition;
        [FormerlySerializedAs("protocolStacksMusicPlaylistDefinition")]
        [SerializeField] private SequenceDefinition protocolStacksMusicPlaylist60mDefinition;
        [SerializeField] private SequenceDefinition protocolStacksMusicPlaylist40mDefinition;

        [Header("Standard Modes")]
        [FormerlySerializedAs("integrationDefinition")]
        [SerializeField] private SequenceDefinition activationDefinition;
        [FormerlySerializedAs("skillsTrainingDefinition")]
        [SerializeField] private SequenceDefinition sonofloreDefinition;


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

        public void SetDefinition(SequenceDefinition def) => definition = def;
        public SequenceDefinition Definition => definition;
        public void SetHandlers(IStageHandler[] handlers) => _handlers = handlers;

        /// <summary>Notifies the current handler of a sequence command. Returns true if a handler was watching and handled it.</summary>
        public bool TryExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            bool handled = false;
            var current = GetCurrentHandler();
            if (current != null && current.WatchesSequenceCommand(sequenceCommand))
            {
                current.ExecuteSequenceCommand(sequenceCommand);
                handled = true;
            }

            var transitioning = GetTransitioningOutHandler();
            if (transitioning != null && !ReferenceEquals(transitioning, current) && transitioning.WatchesSequenceCommand(sequenceCommand))
            {
                transitioning.ExecuteSequenceCommand(sequenceCommand);
                handled = true;
            }

            return handled;
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
        /// Production startup path: resolves mode/content-pack from CSV and starts that sequence.
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

        /// <summary>Adjunctive branch entry: starts the calibration sequence definition.</summary>
        public void StartProtocolStacksCalibrationSequence() => StartNamedSequence(protocolStacksCalibrationDefinition, nameof(protocolStacksCalibrationDefinition));

        /// <summary>Adjunctive branch entry: starts the interactive sequence definition.</summary>
        public void StartProtocolStacksInteractiveSequence() => StartNamedSequence(protocolStacksInteractiveDefinition, nameof(protocolStacksInteractiveDefinition));

        /// <summary>Adjunctive branch entry: starts the 60-minute music playlist sequence definition.</summary>
        public void StartProtocolStacksMusicPlaylist60mSequence() => StartNamedSequence(protocolStacksMusicPlaylist60mDefinition, nameof(protocolStacksMusicPlaylist60mDefinition));

        /// <summary>Adjunctive branch entry: starts the 40-minute music playlist sequence definition.</summary>
        public void StartProtocolStacksMusicPlaylist40mSequence() => StartNamedSequence(protocolStacksMusicPlaylist40mDefinition, nameof(protocolStacksMusicPlaylist40mDefinition));

        /// <summary>
        /// Prefers <see cref="HummingbirdContentPackDefinition.SequenceDefinition"/> on <see cref="CSVLoader.ResolvedSessionPack"/> when set; otherwise legacy mode branches.
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
            if (pack != null && pack.SequenceDefinition != null)
                return pack.SequenceDefinition;

            if (loader.gameMode == CSVLoader.GameModeAdjunctive)
            {
                if (loader.contentPack == CSVLoader.ContentPackSingleStage)
                    Debug.LogWarning("SequenceRunner: Adjunctive + Single Stage has no dedicated SequenceDefinition entry yet (stub); starting calibration like other Adjunctive packs.");
                // TODO: When Single Stage has its own SequenceDefinition entry flow, branch on loader.contentPack == CSVLoader.ContentPackSingleStage (currently all Adjunctive sessions start calibration like Dual Stage).
                // Adjunctive starts from calibration; practitioner choice later selects interactive vs playlist branch.
                return protocolStacksCalibrationDefinition;
            }

            if (loader.gameMode == CSVLoader.GameModeAlbums)
            {
                Debug.LogWarning("SequenceRunner: Albums mode has no SequenceDefinition resolver yet (stub).");
                return null;
            }

            if (loader.gameMode == CSVLoader.GameModeSonoflore)
                return sonofloreDefinition;

            if (loader.gameMode == CSVLoader.GameModeActivation)
                return activationDefinition;

            Debug.LogWarning("SequenceRunner: No sequence definition resolver for gameMode '" + loader.gameMode + "'.");
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
