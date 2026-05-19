using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>
    /// Scripted sequence branches for <see cref="StageType.Code"/>.
    /// Dual-stage section end routes to <see cref="StageType.SetMenu"/> or <see cref="StageType.End"/> in the
    /// active definition (expected layout: Code → SetMenu → End).
    /// </summary>
    public class CodeStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;

        public CodeStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public StageType StageType => StageType.Code;

        public bool IsComplete { get; private set; }

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            if (sequenceCommand == SequenceCommand.EndThisSequenceStage)
                MarkComplete();
        }

        public void Enter(StageVariant variant)
        {
            IsComplete = false;
            if (UIManager.Instance != null)
                UIManager.Instance.EnableSkipButton(false, null);

            if (variant == StageVariant.Code_Dualstage_SectionEnd)
                EnterDualstageSectionEnd();
            else
            {
                Debug.LogWarning($"CodeStageHandler: Unimplemented variant {variant}; completing immediately.");
                MarkComplete();
            }
        }

        /// <summary>
        /// Router after a dual-stage segment. Expects the current definition to list
        /// <c>Code → SetMenu (<see cref="StageVariant.Menu_Ps_InteractiveOrMusic"/>) → End</c> at the tail.
        /// <list type="bullet">
        /// <item><c>dualstageStage == 1</c> — second-visit choice (<see cref="StageType.SetMenu"/>).</item>
        /// <item><c>dualstageStage == 2</c> — session end (<see cref="StageType.End"/>), skipping SetMenu.</item>
        /// </list>
        /// </summary>
        private void EnterDualstageSectionEnd()
        {
            if (_sequencer == null)
            {
                Debug.LogError("CodeStageHandler.EnterDualstageSectionEnd: Sequencer is null.");
                MarkComplete();
                return;
            }

            _sequencer.FadePreferredColorDarkAndStopAvs();

            var runner = _sequencer.SequenceRunner;
            if (runner == null)
            {
                Debug.LogError("CodeStageHandler.EnterDualstageSectionEnd: SequenceRunner is null.");
                MarkComplete();
                return;
            }

            int codeIndex = runner.CurrentStageIndex;
            var stages = runner.Definition?.StagesOrEmpty;
            if (stages == null || codeIndex < 0 || codeIndex >= stages.Length || stages[codeIndex].type != StageType.Code)
            {
                Debug.LogError(
                    $"CodeStageHandler.EnterDualstageSectionEnd: expected Code at index {codeIndex}, " +
                    $"got {(stages == null || codeIndex < 0 || codeIndex >= stages.Length ? "invalid index" : stages[codeIndex].type.ToString())}.");
                MarkComplete();
                return;
            }

            int ds = _sequencer.dualstageStage;
            if (ds == 1)
            {
                int setMenuIndex = codeIndex + 1;
                if (!ValidateTailStage(stages, setMenuIndex, StageType.SetMenu, StageVariant.Menu_Ps_InteractiveOrMusic,
                        "dualstageStage==1 → SetMenu"))
                {
                    MarkComplete();
                    return;
                }

                Debug.Log("CodeStageHandler: Code_Dualstage_SectionEnd — routing to SetMenu (second visit).");
                runner.AdvanceToStage(setMenuIndex);
                return;
            }

            if (ds == 2)
            {
                int endIndex = codeIndex + 2;
                if (!ValidateTailStage(stages, endIndex, StageType.End, StageVariant.End_Default,
                        "dualstageStage==2 → End"))
                {
                    MarkComplete();
                    return;
                }

                Debug.Log("CodeStageHandler: Code_Dualstage_SectionEnd — routing to End (skipping SetMenu).");
                runner.AdvanceToStage(endIndex);
                return;
            }

            Debug.LogWarning($"CodeStageHandler: Code_Dualstage_SectionEnd with unexpected dualstageStage={ds}; completing.");
            MarkComplete();
        }

        private static bool ValidateTailStage(
            SequenceStage[] stages,
            int index,
            StageType expectedType,
            StageVariant expectedVariant,
            string routeLabel)
        {
            if (index < 0 || index >= stages.Length)
            {
                Debug.LogWarning(
                    $"CodeStageHandler: Cannot {routeLabel} — index {index} is out of range (stage count {stages.Length}). " +
                    "Expected Code → SetMenu → End at the sequence tail.");
                return false;
            }

            var stage = stages[index];
            if (stage.type != expectedType || stage.variant != expectedVariant)
            {
                Debug.LogWarning(
                    $"CodeStageHandler: Cannot {routeLabel} — stage at index {index} is {stage.type}/{stage.variant}, " +
                    $"expected {expectedType}/{expectedVariant}.");
                return false;
            }

            return true;
        }

        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
            Debug.Log("CodeStageHandler: Marking stage complete.");
        }

        public void BeginTransitionOut()
        {
        }

        public void Exit()
        {
        }
    }
}
