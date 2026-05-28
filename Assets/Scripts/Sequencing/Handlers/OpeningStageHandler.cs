using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Opening stage: light init, Wwise opening sequence, and AVS program. Watches for Cue_StartInteractive; when it fires, marks complete and SequenceRunner advances on next poll.</summary>
    public class OpeningStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private readonly AVSSequence _avsSequence;
        private bool _hasEntered;
        private bool _variantForcesTone = false;
        /// <summary>
        /// When true, <see cref="SequenceCommand.StartInteractive"/> / <see cref="SequenceCommand.StartTutorial"/> also flip
        /// <see cref="Sequencer.IsOpeningMusicPlaying"/> to false (standard openings: bed audio is treated as done at handoff).
        /// When false, only <see cref="SequenceCommand.MusicTrackEnding"/> or a force-exit clears the flag (PS_Ascending: musical tail outlives the opening stage).
        /// Set in <see cref="Enter"/>; reset on <see cref="LocalCleanup"/>.
        /// </summary>
        private bool _variantClearsOpeningMusicFlagOnHandoffCue = false;

        public StageType StageType => StageType.Opening;

        public bool IsComplete { get; private set; }

        public OpeningStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
            _avsSequence = sequencer != null ? sequencer.GetComponent<AVSSequence>() : null;
        }

        public void Enter(StageVariant variant)
        {
            if (_hasEntered)
            {
                Debug.LogWarning("OpeningStageHandler: Enter() called again before Exit(). Skipping to prevent double-play.");
                return;
            }
            _hasEntered = true;
            IsComplete = false;

            //UI CALLS
            //TODO: ADD UI CALL HERE
            //TO SET UI TO INTERACTIVE MAIN SEQUENCE (IF IT'S NOT ALREADY SET)

            bool isFirstTimeUser = _sequencer.csvLoader.IsFirstTimeUser;

            //DO NULL CHECKS
            
            if (variant == StageVariant.None)
            {
                Debug.LogError("OpeningStageHandler: Variant is None. Skipping to prevent crash.");
                _hasEntered = false;
                MarkComplete();
                return;
            }

            if (_sequencer == null)
            {
                Debug.LogError("OpeningStageHandler: Sequencer is null.");
                _hasEntered = false;
                MarkComplete();
                return;
            }

            _sequencer.StopCalibrationInteractiveMusicFromStageEnter();
            if (LightControl.instance == null)
            {
                Debug.LogError("OpeningStageHandler: LightControl.instance is null. Cannot initialize lights.");
                _hasEntered = false;
                MarkComplete();
                return;
            }

            if (_sequencer.wwiseVOManager == null)
            {
                Debug.LogError("OpeningStageHandler: wwiseVOManager is null. Cannot play opening sequence.");
                _hasEntered = false;
                MarkComplete();
                return;
            }

            if (_sequencer.worldShuffler == null)
            {
                Debug.LogError("OpeningStageHandler: worldShuffler is null. Cannot initialize soundscape/color exclusions.");
                _hasEntered = false;
                MarkComplete();
                return;
            }
            
            //INITIALIZE LIGHTS
            try
            {
                LightControl.instance.LightSettingsInitialization(5.0f);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("OpeningStageHandler: LightSettingsInitialization failed: " + ex.Message);
                Debug.LogError("Stack trace: " + ex.StackTrace);
                _hasEntered = false;
                MarkComplete();
                return;
            }

            //INITIALIZE MUSIC AND DIRECTOR
            
            MusicSystem1.instance.SetMusicSilentLayerVolume(MusicSystem1.instance._silentVolumeLow, 0.0f);
            _sequencer.director.Disable();
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Silent);
            _sequencer.worldShuffler.ExcludeColorWorld(PreferredColorWorld.Blue);
            _sequencer.worldShuffler.ExcludeSoundscape("Shadow");

            if (variant == StageVariant.Opening_PS_Ascending)
            {
                Debug.Log("OpeningStageHandler: Adjunctive mode detected. Initializing Adjunctive session.");
                MusicSystem1.instance.SetSoundWorld("Shadow");
                MusicSystem1.instance.SetSoundscape("ShiftingEarth");
            }
            else
            {
                Debug.Log("OpeningStageHandler: Standard mode detected. Initializing Standard.");
                MusicSystem1.instance.SetSoundscape("SonoFlore");
            }


            //PLAY OPENING MUSIC AND VO
            EnsureThematicContentFallbackForStandardModes(variant);
            if (variant == StageVariant.Opening_PS_Ascending)
            {
                _sequencer.wwiseVOManager.PlayOpeningSequence("PS_Ascending");
                Debug.Log("OpeningStageHandler: Playing opening sequence: PS_Ascending");
                
                _sequencer.wwiseVOManager.SetTestRepairSwitch("C");
                // PS_Ascending's musical bed outlives the opening stage: Tutorial_Short must wait for the natural
                // Cue_Music_Ending (SequenceCommand.MusicTrackEnding) before starting its replacement music.
                _variantClearsOpeningMusicFlagOnHandoffCue = false;
            }
            else if (variant == StageVariant.Opening_Preparation || variant == StageVariant.Opening_Sonoflore)
            {
                Debug.Log("OpeningStageHandler: Playing Sonoflore opening sequence.");
                if(isFirstTimeUser)
                {
                    _sequencer.wwiseVOManager.PlayOpeningSequence("Preparation_Long");
                    Debug.Log("OpeningStageHandler: Playing Preparation Long Opening Sequence.");
                }
                else
                {
                    _sequencer.wwiseVOManager.PlayOpeningSequence("Preparation_Short");
                    Debug.Log("OpeningStageHandler: Playing Preparation Short Opening Sequence.");
                }
                _variantForcesTone = true;
                _sequencer.wwiseVOManager.SetTestRepairSwitch("A");
                _variantClearsOpeningMusicFlagOnHandoffCue = true;
            }
            else if (variant == StageVariant.Opening_Activation)
            {
                _sequencer.wwiseVOManager.PlayOpeningSequence("Integration_Short");
                Debug.Log("OpeningStageHandler: Playing Activation opening sequence (Wwise: Integration_Short).");
                _variantForcesTone = true;
                _sequencer.wwiseVOManager.SetTestRepairSwitch("A");
                _variantClearsOpeningMusicFlagOnHandoffCue = true;
            }
            else
            {
                Debug.LogError("OpeningStageHandler: Invalid opening variant: " + variant + ". Skipping to prevent crash.");
                _hasEntered = false;
                MarkComplete();
                return;
            }

            //INITIALIZE AVS PROGRAM
            _avsSequence.StartOpeningAVSProgram();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetMeditationScreen();
                UIManager.Instance.SetSessionSectionHeader(SessionSectionHeaderKind.OpeningMeditation);
                UIManager.Instance.RefreshSessionDualStageBannerFromSequencer(_sequencer);
                UIManager.Instance.RefreshSkipStageButtonFromSequencer(_sequencer);
                UIManager.Instance.EnableSkipButton(false);
            }

            // Opening sequence audio is now playing. Tutorial reads this on Enter to decide whether to start its
            // music immediately or wait for OnOpeningMusicEnded. See Docs/TUTORIAL_OPENING_MUSIC_HANDOFF_PLAN.md.
            _sequencer.NotifyOpeningMusicStarted();
        }

        private void EnsureThematicContentFallbackForStandardModes(StageVariant variant) //for debugging, if we are using a development sequence definition that doesn't match the csv...
        {
            if (_sequencer == null || _sequencer.wwiseVOManager == null)
                return;

            var loader = _sequencer.csvLoader;
            if (loader == null)
            {
                // If CSV state is unavailable, only apply fallback for explicit stage variants.
                if (variant == StageVariant.Opening_Sonoflore || variant == StageVariant.Opening_Preparation)
                {
                    Debug.LogWarning("OpeningStageHandler: CSVLoader unavailable during Sonoflore opening. Applying fallback thematic content: Narrative.");
                    _sequencer.wwiseVOManager.SetToNarrative();
                }
                else if (variant == StageVariant.Opening_Activation)
                {
                    Debug.LogWarning("OpeningStageHandler: CSVLoader unavailable during Activation opening. Applying fallback thematic content: Fireflies.");
                    _sequencer.wwiseVOManager.SetToFireflies();
                }
                return;
            }

            bool sonofloreMode = loader.gameMode == CSVLoader.GameModeSonoflore;
            bool activationMode = loader.gameMode == CSVLoader.GameModeActivation;

            if (sonofloreMode)
            {
                bool recognizedSonoflorePack =
                    loader.contentPack == CSVLoader.ContentPackMindfulnessAndJoy
                    || loader.contentPack == CSVLoader.ContentPackPsychologicalFlexibility
                    || loader.contentPack == CSVLoader.ContentPackSurrenderResponse;
                if (!recognizedSonoflorePack)
                {
                    Debug.LogWarning("OpeningStageHandler: CSV thematic content not recognized for Sonoflore (contentPack='" + loader.contentPack + "'). Applying fallback thematic content: Narrative.");
                    _sequencer.wwiseVOManager.SetToNarrative();
                }
                return;
            }

            if (activationMode)
            {
                bool recognizedActivationPack =
                    loader.contentPack == CSVLoader.ContentPackSelfCompassion
                    || loader.contentPack == CSVLoader.ContentPackLovingKindness
                    || loader.contentPack == CSVLoader.ContentPackTransitionsGriefAndAppreciation;
                if (!recognizedActivationPack)
                {
                    Debug.LogWarning("OpeningStageHandler: CSV thematic content not recognized for Activation (contentPack='" + loader.contentPack + "'). Applying fallback thematic content: Fireflies.");
                    _sequencer.wwiseVOManager.SetToFireflies();
                }
            }
        }


        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.StartInteractive
            || sequenceCommand == SequenceCommand.StartTutorial
            || sequenceCommand == SequenceCommand.FirstVocalizationStart
            || sequenceCommand == SequenceCommand.MusicTrackEnding
            || sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            if (sequenceCommand == SequenceCommand.EndThisSequenceStage)
            {
                // Force-exit path: any tutorial currently subscribed to OnOpeningMusicEnded should still receive
                // the callback (no leaked subscription), and any tutorial about to enter should see the flag false.
                _sequencer.NotifyOpeningMusicEnded();
                MarkComplete();
                return;
            }

            if (sequenceCommand == SequenceCommand.StartInteractive || sequenceCommand == SequenceCommand.StartTutorial)
            {
                // Standard openings (Preparation / Sonoflore / Activation): bed audio is effectively done at handoff,
                // so flip the flag here so Tutorial_Long (or any tutorial after them) sees IsOpeningMusicPlaying = false
                // when it Enters. PS_Ascending intentionally does NOT clear here — its musical bed keeps playing past
                // the handoff cue and is cleared later by MusicTrackEnding (see _variantClearsOpeningMusicFlagOnHandoffCue).
                if (_variantClearsOpeningMusicFlagOnHandoffCue)
                    _sequencer.NotifyOpeningMusicEnded();
                MarkComplete();
            }

            if (sequenceCommand == SequenceCommand.FirstVocalizationStart)
            {
                LightControl.instance.StartLightsWithDelay();
                
                if(_variantForcesTone)
                {
                    Debug.Log("OpeningStageHandler: Making Wwise Tone");
                    _sequencer.MakeWwiseTone();
                }
            }
            if (sequenceCommand == SequenceCommand.MusicTrackEnding)
            {
                // PS_Ascending's long musical tail has reached its natural end. Flip the flag so any Tutorial
                // currently subscribed to OnOpeningMusicEnded starts its own music. The Tutorial owns the "what
                // music plays next" decision now — Opening no longer calls _sequencer.StartPlayground from here.
                _sequencer.NotifyOpeningMusicEnded();
            }
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
            Debug.Log("OpeningStageHandler: Marking stage complete. Note that audio may still be playing from this stage.");
            // Next: On the next SequenceRunner.Update(), the runner sees IsComplete and calls TransitionToNextStage().
            // That calls AdvanceToStage(next), which invokes BeginTransitionOut() on this handler (tail / fade start),
            // then enters the next stage. Cleanup when this stage is fully retired belongs in Exit() (via LocalCleanup).
            // Exit() is invoked by the runner when this stage leaves the tracked window, e.g. a jump skips past it
            // (older than immediate previous), StartSequence resets, or similar — not necessarily on every linear step.
        }


        /// <summary>Runner-only: start transition-out (tail) while the next stage is already entering.</summary>
        public void BeginTransitionOut()
        {
            Debug.Log("OpeningStageHandler: BeginTransitionOut.");
            // Tail-only: fades, VO tails, etc. Final teardown stays in Exit() -> LocalCleanup() so it runs once when retired.
            // No tail yet for Opening; IStageHandler default is also no-op — explicit method documents intent.
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
            _hasEntered = false;  // Allow re-enter on sequence restart
            // Reset per-variant flags so a subsequent Enter for a different variant starts from a clean slate.
            // (Pre-refactor, _variantForcesTone could leak true across sequence runs — e.g. Activation/Sonoflore → new
            // sequence with PS_Ascending — and erroneously call MakeWwiseTone on the new opening's FirstVocalizationStart.)
            _variantForcesTone = false;
            _variantClearsOpeningMusicFlagOnHandoffCue = false;
            _sequencer.wwiseVOManager.StopOpeningSequence();
            // Force-stop path: if MusicTrackEnding never arrived (e.g. PS_Ascending skipped before its tail finished),
            // the Tutorial may still be subscribed to OnOpeningMusicEnded — flip the flag so its callback runs.
            _sequencer.NotifyOpeningMusicEnded();
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
