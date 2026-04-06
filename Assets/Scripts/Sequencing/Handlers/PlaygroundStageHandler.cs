using System.Collections;
using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Playground stage: countdown-based timed steps. Completes when CountdownSeconds reaches 0.</summary>
    public class PlaygroundStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private Coroutine _playgroundCoroutine;

        public StageType StageType => StageType.Playground;

        public bool IsComplete { get; private set; }

        public PlaygroundStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public void Enter(string variant)
        {
            IsComplete = false;
            if (_sequencer == null)
            {
                Debug.LogError("PlaygroundStageHandler: Sequencer is null.");
                MarkComplete();
                return;
            }
            if (_playgroundCoroutine != null)
            {
                Debug.LogError("PlaygroundStageHandler: ENTER() CALLED WHILE COROUTINE IS ALREADY RUNNING. THE SEQUENCE IS LIKELY BROKEN. STOP THE SEQUENCE BEFORE STARTING IT AGAIN.");
                return;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _playgroundCoroutine = _sequencer.StartCoroutine(PlaygroundCoroutine());
            _sequencer.director.Enable();
            MusicSystem1.instance.SetAllowTransitionFromEnvironmentToFreeplay(true);
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
            MusicSystem1.instance.SetBreathworkCycle(false);
            if(variant == "Standard")
            {
                Debug.Log("PlaygroundStageHandler: Standard variant: Starting Standard Playground");
                if(!_sequencer.worldShuffler.shuffling)
                {
                    _sequencer.worldShuffler.BeginShuffle();
                }
            }
            else if(variant == "Ascending")
            {
                Debug.Log("PlaygroundStageHandler: Ascending variant: Starting Ascending Playground");
            }
        }

        private IEnumerator PlaygroundCoroutine()
        {
            Debug.Log("PlaygroundStageHandler: STARTED - Current countdown: " + _sequencer.CountdownSeconds + " seconds (" + (_sequencer.CountdownSeconds / 60f) + " minutes)");

            if (_sequencer.calibrationMenu == null)
            {
                Debug.LogWarning("PlaygroundStageHandler: calibrationMenu is null - countdown will not decrement.");
            }
            else if (_sequencer.calibrationMenu.startedExperience != true)
            {
                Debug.LogWarning("PlaygroundStageHandler: Experience not started yet (calibrationMenu.startedExperience != true). The sequence will not progress.");
            }

            if (_sequencer.worldShuffler == null || _sequencer.director == null || _sequencer.lightControl == null)
            {
                Debug.LogError("PlaygroundStageHandler: worldShuffler, director, or lightControl is null. Cannot run Playground stage.");
                _playgroundCoroutine = null;
                MarkComplete();
                yield break;
            }

            float step1Threshold = 20f * 60f; // 1200 seconds = 20 minutes

            Debug.Log("PlaygroundStageHandler: Waiting for countdown to reach " + step1Threshold + " seconds (20 minutes). Current: " + _sequencer.CountdownSeconds + " (or call ForceSequenceAdvance() to skip)");
            int frameCount = 0;
            while (_sequencer.CountdownSeconds > step1Threshold && !_sequencer.ForceSequenceAdvanceRequested)
            {
                frameCount++;
                if (frameCount % 600 == 0)
                {
                    Debug.Log("PlaygroundStageHandler: Still waiting. Countdown: " + _sequencer.CountdownSeconds + " seconds (" + (_sequencer.CountdownSeconds / 60f) + " minutes). Threshold: " + step1Threshold);
                }
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: Threshold reached! Countdown: " + _sequencer.CountdownSeconds + " seconds. Proceeding to Step 1.");
            Debug.Log("PlaygroundStageHandler: Step 1 - Starting interactive music (20 minutes or less remaining)");
            MusicSystem1.instance.SetBreathworkCycle(false);
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
            MusicSystem1.instance.SetSoundscape("ShiftingEarth");
            _sequencer.StartPlayground(false, false, true);

            _sequencer.worldShuffler.ExcludeSoundscape("Shadow");

            while (_sequencer.CountdownSeconds > (19f * 60f - 30f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: Step 2");
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("SitarAmbience"), "Soundscape", true, false, 180.0f, 2, 2);

            while (_sequencer.CountdownSeconds > (16f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("Shadow"), "Soundscape", true, false, 180.0f, 2, 2);
            _sequencer.director.AddActionToQueue(_sequencer.lightControl.Action_SetPreferredColorWorld("Blue", 8.0f), "ColorWorld", false, true, 180.0f, 1, 2);
            Debug.Log("PlaygroundStageHandler: Step 4");

            while (_sequencer.CountdownSeconds > (13f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("PinkNoiseAtmosphere"), "Soundscape", true, false, 180.0f, 2, 2);
            Debug.Log("PlaygroundStageHandler: Step 5");

            while (_sequencer.CountdownSeconds > (12f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            _sequencer.worldShuffler.BeginShuffle(false);

            while (_sequencer.CountdownSeconds > (10f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            Debug.Log("PlaygroundStageHandler: Step 6");
            _sequencer.worldShuffler.ExcludeSoundscape("SonoFlore");

            while (_sequencer.CountdownSeconds > (4f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            Debug.Log("PlaygroundStageHandler: Step 8");
            _sequencer.worldShuffler.StopShuffle();
            _sequencer.worldShuffler.CloseSoundscapeQueue();
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("SonoFlore"), "Soundscape", true, false, 180.0f, 2, 2);

            while (_sequencer.CountdownSeconds > 60f && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            while (_sequencer.CountdownSeconds > 0f && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: Countdown reached 0. Playground complete.");
            _playgroundCoroutine = null;
            MarkComplete();
        }

        //--------------------------------
        // Lifecycle after main work: complete -> (optional) transition-out tail -> Exit
        // SequenceRunner owns BeginTransitionOut / Exit timing; handlers should not call those locally.
        //--------------------------------

        // Completion: PlaygroundCoroutine calls MarkComplete() when countdown logic finishes; runner then calls TransitionToNextStage().
        /// <summary>Main phase done. Does not start transition-out; that begins when the runner advances.</summary>
        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
            TimeTrackerScript.instance?.ForceSetCountdownSecondsAndStop(-1f);
            Debug.Log("PlaygroundStageHandler: Marking stage complete.");
            // Next: On the next SequenceRunner.Update(), the runner sees IsComplete and calls TransitionToNextStage().
            // That calls AdvanceToStage(next), which invokes BeginTransitionOut() on this handler (tail / fade start),
            // then enters the next stage. Cleanup when this stage is fully retired belongs in Exit() (via LocalCleanup).
            // Exit() is invoked by the runner when this stage leaves the tracked window, e.g. a jump skips past it
            // (older than immediate previous), StartSequence resets, or similar — not necessarily on every linear step.
        }


        /// <summary>Runner-only: start transition-out (tail) while the next stage is already entering.</summary>
        public void BeginTransitionOut()
        {
            // Tail-only: fades, VO tails, etc. Final teardown stays in Exit() -> LocalCleanup() so it runs once when retired.
            // No tail yet for Playground; IStageHandler default is also no-op — explicit method documents intent.
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
            if (_playgroundCoroutine != null && _sequencer != null)
            {
                _sequencer.StopCoroutine(_playgroundCoroutine);
                _playgroundCoroutine = null;
            }
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            LocalCleanup();
        }

    }
}
