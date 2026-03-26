using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Opening stage: light init, Wwise opening sequence, and AVS program. Watches for Cue_StartInteractive; when it fires, marks complete and SequenceRunner advances on next poll.</summary>
    public class OpeningStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _hasEntered;

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

            //DO NULL CHECKS
            
            if(string.IsNullOrEmpty(variant))
            {
                Debug.LogError("OpeningStageHandler: Variant is null. Skipping to prevent crash.");
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            if (_sequencer == null)
            {
                Debug.LogError("OpeningStageHandler: Sequencer is null.");
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            if (_sequencer.lightControl == null)
            {
                Debug.LogError("OpeningStageHandler: lightControl is null. Cannot initialize lights.");
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            if (_sequencer.wwiseVOManager == null)
            {
                Debug.LogError("OpeningStageHandler: wwiseVOManager is null. Cannot play opening sequence.");
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            if (_sequencer.worldShuffler == null)
            {
                Debug.LogError("OpeningStageHandler: worldShuffler is null. Cannot initialize soundscape/color exclusions.");
                _hasEntered = false;
                IsComplete = true;
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
                IsComplete = true;
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
            }
            else if (variant == "Integration")
            {
                _sequencer.wwiseVOManager.PlayOpeningSequence("Integration_Short");
                Debug.Log("OpeningStageHandler: Playing Integration Opening Sequence.");
            }
            else
            {
                Debug.LogError("OpeningStageHandler: Invalid opening variant: " + variant + ". Skipping to prevent crash.");
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            //INITIALIZE AVS PROGRAM
            _sequencer.StartOpeningAVSProgram();

        }

        public void Exit()
        {
            _hasEntered = false;  // Allow re-enter on sequence restart
            // AVS coroutine continues running in parallel; no cleanup needed when advancing to Playground
        }


        public bool WatchesCue(CueType cue) => cue == CueType.StartInteractive || cue == CueType.StartTutorial;

        public void NotifyCue(CueType cue)
        {
            if (cue == CueType.StartInteractive || cue == CueType.StartTutorial)
                MarkComplete();
        }

        /// <summary>Marks the stage complete; SequenceRunner advances on next poll. Idempotent.</summary>
        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
            Debug.Log("OpeningStageHandler: Marking stage complete. Note that audio may still be playing from this stage.");
        }
    }
}
