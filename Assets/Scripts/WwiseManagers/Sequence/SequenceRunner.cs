using System;
using UnityEngine;

namespace SoundSelf.Sequence
{
    public class SequenceRunner : MonoBehaviour
    {
        [SerializeField] private SequenceDefinition definition;
        [SerializeField] private bool autoStartOnAwake;

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

        public void SetDefinition(SequenceDefinition def) => definition = def;
        public void SetHandlers(IStageHandler[] handlers) => _handlers = handlers;

        /// <summary>Notifies the current handler of a cue. Returns true if a handler was watching and handled it.</summary>
        public bool TryNotifyCue(CueType cue)
        {
            var handler = GetCurrentHandler();
            if (handler != null && handler.WatchesCue(cue))
            {
                handler.NotifyCue(cue);
                return true;
            }
            return false;
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

            _sequenceComplete = false;
            AdvanceToStage(0);
        }

        /// <param name="expectNext">If non-null, logs a warning when the next stage does not match. Caller provides expected stage for validation.</param>
        public void AdvanceToNextStage(StageType? expectNext = null)
        {
            if (definition == null) return;
            var stages = definition.StagesOrEmpty;
            int next = CurrentStageIndex + 1;
            if (next >= stages.Length)
            {
                if (expectNext != null)
                    Debug.LogWarning("AdvanceToNextStage: No next stage (sequence would complete). Expected " + expectNext + ".");
                MarkSequenceComplete();
                return;
            }
            if (expectNext != null)
            {
                var nextStage = stages[next].type;
                if (nextStage != expectNext.Value)
                    Debug.LogWarning("AdvanceToNextStage: Expected next stage " + expectNext + ", got " + nextStage);
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

            // 1. Exit current handler
            if (CurrentStageIndex >= 0)
            {
                var current = GetHandlerFor(stages[CurrentStageIndex].type);
                current?.Exit();
            }

            // 2. Set index
            CurrentStageIndex = index;
            var stage = stages[index];

            // 3. Get handler and enter
            var handler = GetHandlerFor(stage.type);
            if (handler == null)
            {
                Debug.LogWarning($"SequenceRunner: No handler for stage type {stage.type}. Skipping.");
                AdvanceToNextStage();
                return;
            }

            handler.Enter(stage.variant);

            // 4. Fire event
            OnStageChanged?.Invoke(index, stage.type);
            Debug.Log($"Sequence: Entered stage {index} ({stage.type})");

            // 5. If handler is already complete (e.g. sync), advance immediately
            if (handler.IsComplete)
                AdvanceToNextStage();
        }

        private void Awake()
        {
            if (autoStartOnAwake && definition != null)
                StartSequence();
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
                AdvanceToNextStage();
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

        private void MarkSequenceComplete()
        {
            if (_sequenceComplete) return;
            _sequenceComplete = true;
            CurrentStageIndex = -1;
            Debug.Log("SequenceRunner: Sequence complete.");
            OnSequenceComplete?.Invoke();
        }
    }
}
