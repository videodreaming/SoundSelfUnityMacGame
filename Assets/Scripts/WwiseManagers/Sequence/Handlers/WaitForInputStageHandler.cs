using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub handler for the WaitForInput stage. Watches for Cue_WaitForButton; completes immediately until implementation.</summary>
    public class WaitForInputStageHandler : IStageHandler
    {
        public StageType StageType => StageType.WaitForInput;

        public bool IsComplete { get; private set; }

        public bool WatchesCue(CueType cue) => cue == CueType.WaitForButton;

        public void NotifyCue(CueType cue)
        {
            if (cue == CueType.WaitForButton)
                MarkComplete();
        }

        public void Enter(string variant)
        {
            IsComplete = true;  // Stub: complete immediately; cue-watching in place for when implementation is added
            Debug.Log("WaitForInputStageHandler: Enter (stub - skipping until implementation added)");
        }

        public void Exit()
        {
            // Stub - no cleanup
        }

        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
        }
    }
}
