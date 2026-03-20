using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub handler for the Tutorial stage. Completes immediately; WatchesCue/NotifyCue in place for when implementation is added.</summary>
    public class TutorialStageHandler : IStageHandler
    {
        public StageType StageType => StageType.Tutorial;

        public bool IsComplete { get; private set; }

        public bool WatchesCue(CueType cue) => cue == CueType.StartInteractive || cue == CueType.Break_Tests;

        public void NotifyCue(CueType cue)
        {
            if (cue == CueType.StartInteractive || cue == CueType.Break_Tests)
                MarkComplete();
        }

        public void Enter(string variant)
        {
            IsComplete = true;  // Stub: complete immediately; cue-watching in place for when implementation is added
            Debug.Log("TutorialStageHandler: Enter (stub - skipping until implementation added)");
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
