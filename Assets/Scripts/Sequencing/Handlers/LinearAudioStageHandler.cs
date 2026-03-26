using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub handler for the LinearAudio stage. Multi-purpose linear audio stage. Skips immediately until implementation is added.</summary>
    public class LinearAudioStageHandler : IStageHandler
    {
        public StageType StageType => StageType.LinearAudio;

        public bool IsComplete => true;

        public void Enter(string variant)
        {
            Debug.Log("LinearAudioStageHandler: Enter (stub - skipping until implementation added)");
        }

        public void Exit()
        {
            // Stub - no cleanup
        }
    }
}
