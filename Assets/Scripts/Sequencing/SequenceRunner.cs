using System;
using UnityEngine;

namespace SoundSelf.Sequence
{
    public class SequenceRunner : MonoBehaviour
    {
        // If assigned in the inspector, the sequence will begin automatically when this component wakes.
        // Formerly serialized as `definition` to keep existing inspector assignments.
        [SerializeField]
        private SequenceDefinition startDefinition;

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
            if (def != null)
                definition = def;

            if (definition == null || definition.StagesOrEmpty.Length == 0)
            {
                Debug.LogWarning("SequenceRunner: Cannot start — no definition or empty stages.");
                MarkSequenceComplete();
                return;
            }

            // Hard reset before starting.
            ForceExitCurrentAndTransitioningHandlers();
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
            // If an inspector start definition exists, trigger startup during Start().
            // This avoids Unity Awake-order issues with handler registration in Sequencer.Awake().
            if (startDefinition != null)
                StartSequence(startDefinition);
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
