using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Opening stage: light init, Wwise opening sequence, and AVS program. Watches for Cue_StartInteractive; when it fires, marks complete and SequenceRunner advances on next poll.</summary>
    public class OpeningStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _hasEntered;
        private bool _variantForcesTone = false;

        public StageType StageType => StageType.Opening;

        public bool IsComplete { get; private set; }

        public OpeningStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public void Enter(string variant)
        {
            if (_hasEntered)
            {
                Debug.LogWarning("OpeningStageHandler: Enter() called again before Exit(). Skipping to prevent double-play.");
                return;
            }
            _hasEntered = true;
            IsComplete = false;
            
            bool isFirstTimeUser = _sequencer.csvLoader.IsFirstTimeUser;

            //CLEAN UP PREVIOUS THINGS
            _sequencer.calibrationMenu.StopCalibrationSequence();

            //DO NULL CHECKS
            
            if(string.IsNullOrEmpty(variant))
            {
                Debug.LogError("OpeningStageHandler: Variant is null. Skipping to prevent crash.");
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

            if (_sequencer.lightControl == null)
            {
                Debug.LogError("OpeningStageHandler: lightControl is null. Cannot initialize lights.");
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
                _sequencer.lightControl.LightSettingsInitialization(5.0f);
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

            if (variant == "PS_Ascending")
            {
                Debug.Log("OpeningStageHandler: Protocol Stacks mode detected. Initializing Protocol Stacks.");
                MusicSystem1.instance.SetSoundWorld("Shadow");
                MusicSystem1.instance.SetSoundscape("ShiftingEarth");
            }
            else
            {
                Debug.Log("OpeningStageHandler: Standard mode detected. Initializing Standard.");
                MusicSystem1.instance.SetSoundscape("SonoFlore");
            }


            //PLAY OPENING MUSIC AND VO
            if (variant == "PS_Ascending")
            {
                _sequencer.wwiseVOManager.PlayOpeningSequence("PS_Ascending");
                Debug.Log("OpeningStageHandler: Playing opening sequence: PS_Ascending");

            }
            else if(variant == "Preparation" || variant == "Skills Training")
            {
                Debug.Log("OpeningStageHandler: Playing Skills Training Opening Sequence.");
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
            }
            else if (variant == "Integration")
            {
                _sequencer.wwiseVOManager.PlayOpeningSequence("Integration_Short");
                Debug.Log("OpeningStageHandler: Playing Integration Opening Sequence.");
                _variantForcesTone = true;
            }
            else
            {
                Debug.LogError("OpeningStageHandler: Invalid opening variant: " + variant + ". Skipping to prevent crash.");
                _hasEntered = false;
                MarkComplete();
                return;
            }

            //INITIALIZE AVS PROGRAM
            _sequencer.StartOpeningAVSProgram();

        }


        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) => sequenceCommand == SequenceCommand.StartInteractive || sequenceCommand == SequenceCommand.StartTutorial || sequenceCommand == SequenceCommand.FirstVocalizationStart;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            if (sequenceCommand == SequenceCommand.StartInteractive || sequenceCommand == SequenceCommand.StartTutorial)
                MarkComplete();

            if (sequenceCommand == SequenceCommand.FirstVocalizationStart)
            {
                _sequencer.StartLightsWithDelay();
                
                if(_variantForcesTone)
                {
                    Debug.Log("OpeningStageHandler: Making Wwise Tone");
                    _sequencer.MakeWwiseTone();
                }
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
            Debug.Log("OpeningStageHandler: BeginTransitionOut, no tail behavior required.");
            // Tail-only: fades, VO tails, etc. Final teardown stays in Exit() -> LocalCleanup() so it runs once when retired.
            // No tail yet for Opening; IStageHandler default is also no-op — explicit method documents intent.
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
            _sequencer.wwiseVOManager.StopOpeningSequence();
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            _hasEntered = false;  // Allow re-enter on sequence restart
            LocalCleanup();
        }

    }
}
