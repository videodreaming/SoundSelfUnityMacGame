using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Scripted sequence branches for <see cref="StageType.Code"/>.</summary>
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
        /// After segment 1, <see cref="Sequencer.dualstageStage"/> is still 1 (increment ran on first choice) — show choice UI in second-visit mode.
        /// After segment 2, <c>dualstageStage</c> is 2 — show Meditation Session (End).
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

            int ds = _sequencer.dualstageStage;
            if (ds == 1)
            {
                if (UIManager.Instance == null)
                {
                    Debug.LogError("CodeStageHandler: UIManager.Instance is null; cannot show Choice SS or Music.");
                    MarkComplete();
                    return;
                }

                Debug.Log("CodeStageHandler: Code_Dualstage_SectionEnd — dualstageStage==1 → Choice SS/Music (second visit). Waits for EndThisSequenceStage.");
                UIManager.Instance.SetChoiceSSOrMusicScreen(true);
                return;
            }

            if (ds == 2)
            {
                if (UIManager.Instance == null)
                    Debug.LogWarning("CodeStageHandler: UIManager.Instance is null; cannot show End meditation screen.");
                else
                {
                    Debug.Log("CodeStageHandler: Code_Dualstage_SectionEnd — dualstageStage==2 → Meditation Session (End).");
                    UIManager.Instance.SetEndMeditationScreen();
                }

                MarkComplete();
                return;
            }

            Debug.LogWarning($"CodeStageHandler: Code_Dualstage_SectionEnd with unexpected dualstageStage={ds}; completing without UI.");
            MarkComplete();
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
