using System;
using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Completes on Wwise cues / <see cref="SequenceCommand.TutorialPassed"/>, or when <see cref="Tutorial"/> ends early because <see cref="TimeTrackerScript.CountdownThisSection"/> on the main session segment is at or below 10 seconds (watchdog in <see cref="Tutorial.Update"/> calls <c>StopTutorial()</c>, which sends <see cref="SequenceCommand.TutorialPassed"/>).</summary>
    public class TutorialStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;

        private bool variantWatchesWwiseVOCuesForCompletion = true;
        private bool _hasEntered = false;

        /// <summary>The active variant captured in <see cref="Enter"/>; used by the <see cref="Sequencer.OnOpeningMusicEnded"/> callback so it can route to the correct music-intent branch.</summary>
        private StageVariant _activeVariant = StageVariant.None;
        /// <summary>Stored handler reference so we can <c>-=</c> the same delegate we subscribed with. Null when not subscribed.</summary>
        private Action _onOpeningMusicEndedHandler;

        public TutorialStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public StageType StageType => StageType.Tutorial;

        public bool IsComplete { get; private set; }
        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.StartInteractive
            || sequenceCommand == SequenceCommand.Break_Tests
            || sequenceCommand == SequenceCommand.TutorialPassed
            || sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            // Long: Wwise cues (StartInteractive, Break_Tests) or explicit TutorialPassed / StopTutorial.
            // Short: only TutorialPassed (e.g. guidance-count path or StopTutorial); no StartInteractive/Break_Tests from Wwise for that flow.

            if (sequenceCommand == SequenceCommand.EndThisSequenceStage)
            {
                MarkComplete();
                return;
            }

            if (sequenceCommand == SequenceCommand.StartInteractive || sequenceCommand == SequenceCommand.Break_Tests)
            {
                if (variantWatchesWwiseVOCuesForCompletion)
                {
                    MarkComplete();
                }
                else
                {
                    Debug.LogWarning("TutorialStageHandler: This variant is not watching this Wwise cue for completion. Skipping completion.");
                }
            }
            
            
            if (sequenceCommand == SequenceCommand.TutorialPassed)
                MarkComplete();
            
            //THERE IS AN INELEGANCE HERE:
            //Short tutorial ends by counting the amount of guidance played.
            //Long tutorial ends by waiting for the cue from Wwise.
            //(Both use HandleSequenceCommand())
        }

        public void Enter(StageVariant variant)
        {
            if(_hasEntered)
            {
                Debug.LogWarning("TutorialStageHandler: Enter() called again before Exit(). Skipping to prevent double-play.");
                return;
            }
            if(_sequencer == null || _sequencer.tutorial == null)
            {
                Debug.LogError("TutorialStageHandler: tutorial is null. Marking stage complete.");
                MarkComplete();
                _hasEntered = false;
                return;
            }
            
            _hasEntered = true;
            IsComplete = false;

            string gameMode = _sequencer.csvLoader != null ? _sequencer.csvLoader.gameMode : null;
            bool isFirstTimeUser = _sequencer.csvLoader != null && _sequencer.csvLoader.IsFirstTimeUser;
            StageVariant assetVariant = variant;
            variant = TutorialStagePolicy.ResolveEffectiveVariant(variant, gameMode, isFirstTimeUser);
            if (variant != assetVariant)
                Debug.Log("TutorialStageHandler: Resolved tutorial variant " + assetVariant + " → " + variant + ".");
            _activeVariant = variant;

            _sequencer.StopCalibrationInteractiveMusicFromStageEnter();
            Debug.Log("TutorialStageHandler: Enter");

            _sequencer.imitoneVoiceInterpreter?.ClearPinnedOrientationNoiseFloorHistory();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetMeditationScreen();
                UIManager.Instance.SetSessionSectionHeader(SessionSectionHeaderKind.Tutorial);
                UIManager.Instance.RefreshSessionDualStageBannerFromSequencer(_sequencer);
                UIManager.Instance.RefreshSkipStageButtonFromSequencer(_sequencer);
                UIManager.Instance.EnableSkipButton(false);
            }

            if (variant == StageVariant.Tutorial_Long)
            {
                _sequencer.tutorial.SetTestVocalizationType("Hum");
                _sequencer.tutorial.StartTutorial("Long");
                _sequencer.wwiseVOManager.SetTestRepairSwitch("A");
                variantWatchesWwiseVOCuesForCompletion = true;
            }
            else if (variant == StageVariant.Tutorial_Short)
            {
                _sequencer.tutorial.SetTestVocalizationType("Ahh");
                _sequencer.tutorial.StartTutorial("Short");
                variantWatchesWwiseVOCuesForCompletion = false;
            }
            else
            {
                Debug.LogError("TutorialStageHandler: Invalid variant: " + variant);
                _hasEntered = false;
                _activeVariant = StageVariant.None;
                return;
            }

            _sequencer.wwiseVOManager.ResetTutorialGuidanceCount();
            MusicSystem1.instance.SetAllowThumpAlways(true);
            MusicSystem1.instance.SetTutorialMonitoringOverride(true);
            LightControl.instance.StartLights();
            _sequencer.imitoneVoiceInterpreter.gameOn = true;
            MicNormalizationStagePolicy.ApplyRaiseFrozenOnStageEnter(
                _sequencer.imitoneVoiceInterpreter, StageType.Tutorial);

            // Music intent for the tutorial variant. If opening music is still playing (PS_Ascending today), wait for
            // Sequencer.OnOpeningMusicEnded so we don't trample the still-playing opening bed. Otherwise start now.
            // See Docs/TUTORIAL_OPENING_MUSIC_HANDOFF_PLAN.md §2.3.
            if (_sequencer.IsOpeningMusicPlaying)
            {
                Debug.Log("TutorialStageHandler: Opening music still playing — subscribing to OnOpeningMusicEnded to start tutorial music when it ends.");
                SubscribeToOpeningMusicEnded();
            }
            else
            {
                StartTutorialMusicForVariant(variant);
            }
        }

        /// <summary>
        /// Per-variant "what music plays during/after the tutorial". Called either immediately from <see cref="Enter"/>
        /// (when no opening music is playing) or from the <see cref="Sequencer.OnOpeningMusicEnded"/> callback when the
        /// opening's musical tail has finished. Adding a new tutorial variant in the future = one new case here.
        /// </summary>
        private void StartTutorialMusicForVariant(StageVariant variant)
        {
            switch (variant)
            {
                case StageVariant.Tutorial_Long:
                    MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.InteractiveTutorial);
                    break;

                case StageVariant.Tutorial_Short:
                    // Equivalent to the legacy OpeningStageHandler.TransitionToAlternativeMusic("MusicLoop") call.
                    // Sequencer.StartPlayground is a debug-style facade; the awkward name from the tutorial side is
                    // tracked as a follow-up cleanup in Docs/TUTORIAL_OPENING_MUSIC_HANDOFF_PLAN.md §6 (Q2).
                    _sequencer.StartPlayground(false, false, true, 30.0f, false, false);
                    MusicSystem1.instance.SetSoundscape("ShiftingEarth");
                    break;

                default:
                    Debug.LogError("TutorialStageHandler.StartTutorialMusicForVariant: No music intent defined for variant " + variant + ".");
                    break;
            }
        }

        private void SubscribeToOpeningMusicEnded()
        {
            if (_onOpeningMusicEndedHandler != null)
                return; // already subscribed
            _onOpeningMusicEndedHandler = HandleOpeningMusicEnded;
            _sequencer.OnOpeningMusicEnded += _onOpeningMusicEndedHandler;
        }

        private void UnsubscribeFromOpeningMusicEnded()
        {
            if (_onOpeningMusicEndedHandler == null)
                return;
            _sequencer.OnOpeningMusicEnded -= _onOpeningMusicEndedHandler;
            _onOpeningMusicEndedHandler = null;
        }

        private void HandleOpeningMusicEnded()
        {
            Debug.Log("TutorialStageHandler: OnOpeningMusicEnded fired — starting tutorial music for variant " + _activeVariant + ".");
            // One-shot: unsubscribe before starting music in case the music start path triggers any reentrant signal.
            UnsubscribeFromOpeningMusicEnded();
            StartTutorialMusicForVariant(_activeVariant);
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
            Debug.Log("TutorialStageHandler: Marking stage complete.");
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
            // Unsubscribe first so a late MusicTrackEnding cue during the playground stage cannot reach us and trample
            // the playground's own MusicSystem1 setup. Idempotent — safe to call from any exit path.
            if (_sequencer != null)
                UnsubscribeFromOpeningMusicEnded();

            if (MusicSystem1.instance != null)
            {
                // Tutorial no longer has attenuation priority; immediately restore interaction-based rule.
                MusicSystem1.instance.SetTutorialMonitoringOverride(false);
            }

            if (_sequencer != null && _sequencer.tutorial != null)
                _sequencer.tutorial.StopTutorial();
            _hasEntered = false;
            _activeVariant = StageVariant.None;
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            LocalCleanup();
        }

        /// <inheritdoc />
        public void OnSessionSkipFromUi() => LocalCleanup();
    }
}
