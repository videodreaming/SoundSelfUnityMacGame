using System.Collections;
using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Playground stage: countdown-based timed steps. Waits on <see cref="TimeTrackerScript.CountdownThisSection"/> until main segment reaches 0.</summary>
    public class PlaygroundStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private readonly AVSSequence _avsSequence;
        private Coroutine _playgroundCoroutine;
        private StageVariant _variant = StageVariant.None;

        public StageType StageType => StageType.Playground;

        public bool IsComplete { get; private set; }

        public PlaygroundStageHandler(Sequencer sequencer, AVSSequence avsSequence)
        {
            _sequencer = sequencer;
            _avsSequence = avsSequence;
        }

        public void Enter(StageVariant variant)
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

            var timeTracker = TimeTrackerScript.instance;
            if (timeTracker != null)
                timeTracker.OnPlaygroundStageEntered();
            else
                Debug.LogWarning("PlaygroundStageHandler: TimeTrackerScript.instance is null; skipping OnPlaygroundStageEntered (TimeSincePlaygroundStart will not reset/arm).");

            const float defaultCountdownPlaceholderThreshold = 999_999f;
            if (timeTracker != null)
            {
                if (timeTracker.CountdownThisSection >= defaultCountdownPlaceholderThreshold)
                {
                    Debug.LogWarning("PlaygroundStageHandler: [CountdownThisSection] still at default ~1e6 (CSV does not start countdown). Real values apply when the sequence hits StartCountdown → BeginCountdownPair.") ;
                }
                if (timeTracker.CountdownFull >= defaultCountdownPlaceholderThreshold)
                {
                    Debug.LogWarning("PlaygroundStageHandler: [CountdownFull] still at default ~1e6 (CSV does not start countdown). Real values apply when the sequence hits StartCountdown → BeginCountdownPair.");
                }
            }

            _variant = variant;

            if (_sequencer.director == null || _sequencer.worldShuffler == null || MusicSystem1.instance == null)
            {
                Debug.LogError("PlaygroundStageHandler: Missing director/worldShuffler/MusicSystem1 instance. Cannot run playground stage.");
                MarkComplete();
                return;
            }

            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.Enable();
            MusicSystem1.instance.SetAllowTransitionFromEnvironmentToFreeplay(true);
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
            MusicSystem1.instance.SetBreathworkCycle(false);
            MusicSystem1.instance.SetAllowThumpAlways(false);
            MusicSystem1.instance.SetAllowThumpWhenModeIsPlayful(true);
            if (IsStandardVariant())
            {
                Debug.Log("PlaygroundStageHandler: Standard variant: Starting Standard Playground coroutine.");
                if(!_sequencer.worldShuffler.shuffling)
                {
                    _sequencer.worldShuffler.BeginShuffle();
                }
                _playgroundCoroutine = _sequencer.StartCoroutine(StandardSequencePlaygroundCoroutine(variant == StageVariant.Playground_SkipStandard));
            }
            else if (variant == StageVariant.Playground_Ascending || variant == StageVariant.Playground_SkipAscending)
            {
                Debug.Log("PlaygroundStageHandler: Ascending variant: Starting Protocol Stacks Playground coroutine.");
                _playgroundCoroutine = _sequencer.StartCoroutine(ProtocolStacksPlaygroundCoroutine(variant == StageVariant.Playground_SkipAscending));
            }
            else
            {
                Debug.LogWarning("PlaygroundStageHandler: Unsupported variant '" + variant + "'. Completing stage.");
                MarkComplete();
            }
        }

        private bool IsStandardVariant() =>
            _variant == StageVariant.Playground_Standard || _variant == StageVariant.Playground_SkipStandard;

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.CueStopInteractive && IsStandardVariant();

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            if (sequenceCommand != SequenceCommand.CueStopInteractive || !IsStandardVariant())
                return;
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.FrozenFreeplay);
            Debug.Log("PlaygroundStageHandler: CueStopInteractive — SetMusicModeTo FrozenFreeplay (Standard).");
        }

        /// <summary><see cref="TimeTrackerScript.CountdownThisSection"/> for protocol step thresholds (main segment, not full session).</summary>
        private float SessionCountdownThisSection()
        {
            var tt = TimeTrackerScript.instance;
            if (tt != null)
                return tt.CountdownThisSection;
            return _sequencer != null ? _sequencer.CountdownThisSection : 0f;
        }

        /// <summary>
        /// If the session clock is not ticking, starts a <b>development</b> duration of <paramref name="initialCd"/> seconds
        /// (intended as step-1 threshold + a few seconds so this coroutine reaches Step 1 quickly). Production sessions should use <see cref="StartCountdownStageHandler"/> first.
        /// </summary>
        private void CheckCountdownRunning(float initialCd)
        {
            var tt = TimeTrackerScript.instance;
            if (tt == null)
            {
                Debug.LogWarning("PlaygroundStageHandler: TimeTrackerScript.instance is null — cannot start a fallback countdown. Add a TimeTrackerScript to the scene.");
                return;
            }
            if (!tt.IsCountdownRunning)
            {
                float closing = CSVLoader.instance != null ? Mathf.Max(0f, CSVLoader.instance.totalTimeOfPostUnguidedVocalizationContent) : 0f;
                float full = closing > 0f ? initialCd + closing : initialCd;
                Debug.LogWarning(
                    "PlaygroundStageHandler: Session countdown is not running. [Development fallback] BeginCountdownPair [CountdownThisSection]=" + initialCd + " [CountdownFull]=" + full +
                    " (~" + (initialCd / 60f) + " min main \"time remaining\"). For production, run StartCountdown before Playground.");
                tt.ConfigureCountdownPair(initialCd, full);
                tt.BeginCountdownPair();
            }
        }

        private void EnsureCountdownRunningForFallback(string coroutineLabel, float fallbackSeconds)
        {
            var tt = TimeTrackerScript.instance;
            if (tt == null)
            {
                Debug.LogError("PlaygroundStageHandler: " + coroutineLabel + " — TimeTrackerScript.instance is null; CountdownThisSection unavailable.");
                return;
            }
            if (tt.IsCountdownRunning)
                return;

            float closing = CSVLoader.instance != null ? Mathf.Max(0f, CSVLoader.instance.totalTimeOfPostUnguidedVocalizationContent) : 0f;
            float full = closing > 0f ? fallbackSeconds + closing : fallbackSeconds;
            Debug.LogWarning("PlaygroundStageHandler: " + coroutineLabel + " — session countdown is not running. [Fallback] BeginCountdownPair [CountdownThisSection]=" + fallbackSeconds + " [CountdownFull]=" + full + ".");
            tt.BeginCountdownPair(fallbackSeconds, full);
        }

        private IEnumerator ProtocolStacksPlaygroundCoroutine(bool skipToEnd = false)
        {
            var tt = TimeTrackerScript.instance;
            float initialCd = SessionCountdownThisSection();


            Debug.Log("PlaygroundStageHandler: STARTED - [CountdownThisSection]=" + initialCd + " s [CountdownFull]=" + (tt != null ? tt.CountdownFull.ToString() : "?") + " (" + (initialCd / 60f) + " min main)");

            if (tt == null)
                Debug.LogWarning("PlaygroundStageHandler: TimeTrackerScript.instance is null — [CountdownThisSection] reads 0; add a tracker to the scene. StartCountdown also requires it.");

            if (_sequencer.worldShuffler == null || _sequencer.director == null || LightControl.instance == null)
            {
                Debug.LogError("PlaygroundStageHandler: worldShuffler, director, or LightControl.instance is null. Cannot run Playground stage.");
                _playgroundCoroutine = null;
                MarkComplete();
                yield break;
            }

            float step1Threshold = 20f * 60f; // 1200 seconds = 20 minutes
            
            CheckCountdownRunning(step1Threshold + 5.0f);

            Debug.Log("PlaygroundStageHandler: Waiting for countdown to reach " + step1Threshold + " seconds (20 minutes). Current: " + SessionCountdownThisSection() + " (or call ForceSequenceAdvance() to skip)");
            int frameCount = 0;
            while (SessionCountdownThisSection() > step1Threshold && !ShouldSkip(skipToEnd))
            {
                frameCount++;
                if (frameCount % 600 == 0)
                {
                    float cd = SessionCountdownThisSection();
                    Debug.Log("PlaygroundStageHandler: Still waiting. Countdown: " + cd + " seconds (" + (cd / 60f) + " minutes). Threshold: " + step1Threshold);
                }
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: Threshold reached! Countdown: " + SessionCountdownThisSection() + " seconds. Proceeding to Step 1.");
            Debug.Log("PlaygroundStageHandler: Step 1 - Starting interactive music (20 minutes or less remaining)");
            MusicSystem1.instance.SetBreathworkCycle(false);
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
            MusicSystem1.instance.SetSoundscape("ShiftingEarth");
            _sequencer.StartPlayground(false, false, true);

            _sequencer.worldShuffler.ExcludeSoundscape("Shadow");

            while (SessionCountdownThisSection() > (19f * 60f - 30f) && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: Step 2");
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("SitarAmbience"), "Soundscape", true, false, 180.0f, DirectorActivationBehavior.ActivateEntireQueueOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType);

            while (SessionCountdownThisSection() > (16f * 60f) && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("Shadow"), "Soundscape", true, false, 180.0f, DirectorActivationBehavior.ActivateEntireQueueOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType);
            _sequencer.director.AddActionToQueue(LightControl.instance.Action_SetPreferredColorWorld("Blue", 8.0f), "ColorWorld", false, true, 180.0f, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType);
            Debug.Log("PlaygroundStageHandler: Step 4");

            while (SessionCountdownThisSection() > (13f * 60f) && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("PinkNoiseAtmosphere"), "Soundscape", true, false, 180.0f, DirectorActivationBehavior.ActivateEntireQueueOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType);
            Debug.Log("PlaygroundStageHandler: Step 5");

            while (SessionCountdownThisSection() > (12f * 60f) && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            _sequencer.worldShuffler.BeginShuffle(false);

            while (SessionCountdownThisSection() > (10f * 60f) && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            Debug.Log("PlaygroundStageHandler: Step 6");
            _sequencer.worldShuffler.ExcludeSoundscape("SonoFlore");

            while (SessionCountdownThisSection() > (4f * 60f) && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            Debug.Log("PlaygroundStageHandler: Step 8");
            _sequencer.worldShuffler.StopShuffle();
            _sequencer.worldShuffler.CloseSoundscapeQueue();
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("SonoFlore"), "Soundscape", true, false, 180.0f, DirectorActivationBehavior.ActivateEntireQueueOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType);

            while (SessionCountdownThisSection() > 60f && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            while (SessionCountdownThisSection() > 0f && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: [CountdownThisSection] reached 0. Playground complete.");
            _playgroundCoroutine = null;
            MarkComplete();
        }

        private IEnumerator StandardSequencePlaygroundCoroutine(bool skipToEnd = false)
        {
            if (_sequencer == null || _sequencer.worldShuffler == null || _sequencer.director == null)
            {
                Debug.LogError("PlaygroundStageHandler: Missing Sequencer/worldShuffler/director. Cannot run standard playground.");
                _playgroundCoroutine = null;
                MarkComplete();
                yield break;
            }

            // Keep legacy behavior parity while running inside stage handlers.
            EnsureCountdownRunningForFallback("StandardSequencePlaygroundCoroutine", 305f);

            while ((TimeTrackerScript.instance != null ? TimeTrackerScript.instance.TimeSincePlaygroundStart : 0f) < 60f
                   && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.worldShuffler.ResetSoundscapeExclusions();
            Debug.Log("PlaygroundStageHandler(Standard): Start1 at 60s — reset soundscape exclusions.");

            while ((TimeTrackerScript.instance != null ? TimeTrackerScript.instance.TimeSincePlaygroundStart : 0f) < 300f
                   && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.worldShuffler.ResetColorWorlds();
            Debug.Log("PlaygroundStageHandler(Standard): Start2 at 300s — reset color worlds.");

            while (SessionCountdownThisSection() > 300f && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.worldShuffler.ResetSoundscapeExclusions();
            _sequencer.worldShuffler.ExcludeSoundscape("Shadow");
            _sequencer.worldShuffler.ExcludeSoundscape("Shruti");
            Debug.Log("PlaygroundStageHandler(Standard): End1 at <=300s — exclude Shadow/Shruti.");

            while (SessionCountdownThisSection() > 180f && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("Shruti"), "Soundscape", true, false, 180.0f, DirectorActivationBehavior.ActivateEntireQueueOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType);
            _sequencer.director.AddActionToQueue(_sequencer.director.Action_PlayTransitionSound(), "TransitionSound", true, false, 180.0f, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType);
            _sequencer.worldShuffler.CloseSoundscapeQueue();
            if (_avsSequence != null)
                _avsSequence.StartDynamicDropEnd(180f);
            else
                Debug.LogError("PlaygroundStageHandler(Standard): AVSSequence is null. Cannot start dynamic drop end.");
            Debug.Log("PlaygroundStageHandler(Standard): End2 at <=180s — queue Shruti + transition + dynamic drop end.");

            while (SessionCountdownThisSection() > 60f && !ShouldSkip(skipToEnd))
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            Debug.Log("PlaygroundStageHandler(Standard): End3 at <=60s — starting LastMinute behavior.");

            EnsureCountdownRunningForFallback("StandardLastMinute", 90f);
            MusicSystem1.instance.SetAllowTransitionFromEnvironmentToFreeplay(false);
            _sequencer.worldShuffler.StopShuffle();
            AkSoundEngine.PostEvent("Play_sfx_EndInteractive", _sequencer.gameObject);

            while (ToneActiveConfident() && SessionCountdownThisSection() > 30f && !ShouldSkip(skipToEnd))
                yield return null;
            _sequencer.ForceSequenceAdvanceRequested = false;

            while (!ToneActiveConfident() && SessionCountdownThisSection() > 30f && !ShouldSkip(skipToEnd))
                yield return null;
            _sequencer.ForceSequenceAdvanceRequested = false;

            _sequencer.director.ActivateQueue(15f);
            _sequencer.director.Disable();
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.FrozenFreeplay);

            while (SessionCountdownThisSection() > 15f && !ShouldSkip(skipToEnd))
                yield return null;
            _sequencer.ForceSequenceAdvanceRequested = false;

            _sequencer.FadeOut();
            if (_sequencer.tutorial != null)
                _sequencer.tutorial.StopTutorial();

            while (SessionCountdownThisSection() > 0f && !ShouldSkip(skipToEnd))
                yield return null;
            _sequencer.ForceSequenceAdvanceRequested = false;

            _playgroundCoroutine = null;
            MarkComplete();
        }

        private bool ToneActiveConfident()
        {
            if (_sequencer == null || _sequencer.imitoneVoiceInterpreter == null)
                return false;
            return _sequencer.imitoneVoiceInterpreter.toneActiveConfident;
        }

        private bool ShouldSkip(bool skipToEnd)
        {
            return skipToEnd || (_sequencer != null && _sequencer.ForceSequenceAdvanceRequested);
        }

        //--------------------------------
        // Lifecycle after main work: complete -> (optional) transition-out tail -> Exit
        // SequenceRunner owns BeginTransitionOut / Exit timing; handlers should not call those locally.
        //--------------------------------

        // Completion: ProtocolStacksPlaygroundCoroutine/StandardSequencePlaygroundCoroutine call MarkComplete() when their timelines finish; runner then calls TransitionToNextStage().
        /// <summary>Main phase done. Does not start transition-out; that begins when the runner advances.</summary>
        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
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
