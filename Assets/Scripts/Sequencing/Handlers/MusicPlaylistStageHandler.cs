using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub handler for the MusicPlaylist stage. Skips immediately until implementation is added.</summary>
    public class MusicPlaylistStageHandler : IStageHandler
    {
        public StageType StageType => StageType.MusicPlaylist;

        public bool IsComplete => true;

        public void Enter(string variant)
        {
            Debug.Log("MusicPlaylistStageHandler: Enter (stub - skipping until implementation added)");
        }

        public void Exit()
        {
            // Stub - no cleanup
        }
    }
}
