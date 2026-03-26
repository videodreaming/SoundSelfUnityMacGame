using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub handler for the Inquiry stage. Asks player how they are feeling and records the answer. Skips immediately until implementation is added.</summary>
    public class InquiryStageHandler : IStageHandler
    {
        public StageType StageType => StageType.Inquiry;

        public bool IsComplete => true;

        public void Enter(string variant)
        {
            Debug.Log("InquiryStageHandler: Enter (stub - skipping until implementation added)");
        }

        public void Exit()
        {
            // Stub - no cleanup
        }
    }
}
