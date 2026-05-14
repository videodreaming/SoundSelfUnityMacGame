using System.Collections;
using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handler for MusicPlaylist stage. Sets playlist switch/event, then waits for CountdownThisSection to reach 0.</summary>
    public class MusicPlaylistStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private Coroutine _completionCoroutine;
        private bool _playlistStopped;

        public MusicPlaylistStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public StageType StageType => StageType.MusicPlaylist;

        public bool IsComplete { get; private set; }

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            if (sequenceCommand == SequenceCommand.EndThisSequenceStage)
            {
                MarkComplete();
            }
        }

        public void Enter(StageVariant variant)
        {
            IsComplete = false;
            _playlistStopped = false;

            if (_sequencer == null || _sequencer.wwiseVOManager == null)
            {
                Debug.LogError("MusicPlaylistStageHandler: Sequencer or WwiseVOManager is null. Cannot start music playlist.");
                MarkComplete();
                return;
            }

            _sequencer.StopCalibrationInteractiveMusicFromStageEnter();
            if (!TryResolvePlaylistVariant(variant, out string playlistSwitch))
            {
                Debug.LogError("MusicPlaylistStageHandler: Unsupported playlist variant '" + variant + "'.");
                MarkComplete();
                return;
            }

            _sequencer.wwiseVOManager.PlayMusicPlaylist(playlistSwitch);
            _completionCoroutine = _sequencer.StartCoroutine(WaitForSectionCountdownToReachZero());
            Debug.Log("MusicPlaylistStageHandler: Started playlist " + playlistSwitch + ". Waiting for CountdownThisSection to reach 0.");
        }

        private static bool TryResolvePlaylistVariant(StageVariant variant, out string playlistSwitch)
        {
            playlistSwitch = string.Empty;

            switch (variant)
            {
                case StageVariant.Playlist_40m:
                    playlistSwitch = "_40m";
                    return true;
                case StageVariant.Playlist_60m:
                    playlistSwitch = "_60m";
                    return true;
                case StageVariant.Playlist_Default:
                    // Backward-compatible default until all assets are migrated.
                    playlistSwitch = "_60m";
                    Debug.LogWarning("MusicPlaylistStageHandler: Playlist_Default used; defaulting to _60m. Prefer explicit Playlist_40m or Playlist_60m.");
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Reads TimeTracker first; falls back to Sequencer mirror for legacy compatibility.</summary>
        private float SessionCountdownThisSection()
        {
            var tt = TimeTrackerScript.instance;
            if (tt != null)
                return tt.CountdownThisSection;
            return _sequencer != null ? _sequencer.CountdownThisSection : 0f;
        }

        private IEnumerator WaitForSectionCountdownToReachZero()
        {
            var timeTracker = TimeTrackerScript.instance;
            const float defaultCountdownPlaceholderThreshold = 999_999f;

            if (timeTracker == null)
                Debug.LogWarning("MusicPlaylistStageHandler: TimeTrackerScript.instance is null; using Sequencer.CountdownThisSection fallback.");
            else if (timeTracker.CountdownThisSection >= defaultCountdownPlaceholderThreshold)
                Debug.LogWarning("MusicPlaylistStageHandler: [CountdownThisSection] still near default ~1e6. Ensure a StartCountdown stage runs before MusicPlaylist.");

            while (SessionCountdownThisSection() > 0f)
                yield return null;

            Debug.Log("MusicPlaylistStageHandler: [CountdownThisSection] reached 0. MusicPlaylist complete.");
            MarkComplete();
        }

        //--------------------------------
        // Lifecycle after main work: complete -> (optional) transition-out tail -> Exit
        // SequenceRunner owns BeginTransitionOut / Exit timing; handlers should not call those locally.
        //--------------------------------

        /// <summary>Main phase done. Does not start transition-out; that begins when the runner advances.</summary>
        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
            Debug.Log("MusicPlaylistStageHandler: Marking stage complete.");
            // Next: On the next SequenceRunner.Update(), the runner sees IsComplete and calls TransitionToNextStage().
            // That calls AdvanceToStage(next), which invokes BeginTransitionOut() on this handler (tail / fade start),
            // then enters the next stage. Cleanup when this stage is fully retired belongs in Exit() (via LocalCleanup).
            // Exit() is invoked by the runner when this stage leaves the tracked window, e.g. a jump skips past it
            // (older than immediate previous), StartSequence resets, or similar — not necessarily on every linear step.
        }

        /// <summary>Runner-only: start transition-out (tail) while the next stage is already entering.</summary>
        public void BeginTransitionOut()
        {
            LocalCleanup();
            // Tail-only: fades, VO tails, etc. Final teardown stays in Exit() -> LocalCleanup() so it runs once when retired.
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
            if (_completionCoroutine != null && _sequencer != null)
            {
                _sequencer.StopCoroutine(_completionCoroutine);
                _completionCoroutine = null;
            }

            if (_playlistStopped)
                return;

            if (_sequencer != null && _sequencer.wwiseVOManager != null)
                _sequencer.wwiseVOManager.StopMusicPlaylists();
            _playlistStopped = true;
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            LocalCleanup();
        }
    }
}
