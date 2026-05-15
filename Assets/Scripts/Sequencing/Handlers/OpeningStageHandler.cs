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
        private bool _variantTransitionsToMusicLoop = false;

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
            _sequencer.worldShuffler.ExcludeColorWorld("Blue");
            _sequencer.worldShuffler.ExcludeSoundscape("Shadow");

            if (variant == StageVariant.Opening_PS_Ascending)
            {
                Debug.Log("OpeningStageHandler: Adjunctive mode detected. Initializing Adjunctive session.");
                MusicSystem1.instance.SetSoundWorld("Shadow");
                MusicSystem1.instance.SetSoundscape("ShiftingEarth");
                _variantTransitionsToMusicLoop = true;
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
            }
            else if (variant == StageVariant.Opening_Activation)
            {
                _sequencer.wwiseVOManager.PlayOpeningSequence("Integration_Short");
                Debug.Log("OpeningStageHandler: Playing Activation opening sequence (Wwise: Integration_Short).");
                _variantForcesTone = true;
                _sequencer.wwiseVOManager.SetTestRepairSwitch("A");
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
                UIManager.Instance.EnableSkipButton(true, "Skip Opening Meditation");
            }
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
                MarkComplete();
                return;
            }

            if (sequenceCommand == SequenceCommand.StartInteractive || sequenceCommand == SequenceCommand.StartTutorial)
                MarkComplete();

            if (sequenceCommand == SequenceCommand.FirstVocalizationStart)
            {
                LightControl.instance.StartLightsWithDelay();
                
                if(_variantForcesTone)
                {
                    Debug.Log("OpeningStageHandler: Making Wwise Tone");
                    _sequencer.MakeWwiseTone();
                }
            }
            if(sequenceCommand == SequenceCommand.MusicTrackEnding && _variantTransitionsToMusicLoop)
            {
                Debug.Log("OpeningStageHandler: Transitioning to Music Loop");
                TransitionToAlternativeMusic("MusicLoop");
            }
        }

        public void TransitionToAlternativeMusic(string alternativeMusicVariant)
        {
            if(alternativeMusicVariant == "MusicLoop")
            {
                _sequencer.StartPlayground(false, false, true, 30.0f, false, false);
                MusicSystem1.instance.SetSoundscape("ShiftingEarth");

            }
            else
            {
                Debug.LogError("OpeningStageHandler: Invalid alternative music variant: " + alternativeMusicVariant);
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
            _sequencer.wwiseVOManager.StopOpeningSequence();
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
