using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub handler for the End stage. End stage that happens at the end of a sequence. Skips immediately until implementation is added.</summary>
    public class EndStageHandler : IStageHandler
    {
        public StageType StageType => StageType.End;

        public bool IsComplete => true;

        public void Enter(string variant)
        {
            Debug.Log("EndStageHandler: Enter (stub - skipping until implementation added)");
        }

        public void Exit()
        {
            // Stub - no cleanup
        }
    }
}
