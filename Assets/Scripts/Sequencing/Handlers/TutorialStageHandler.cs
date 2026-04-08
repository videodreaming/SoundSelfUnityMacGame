using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Completes on Wwise cues / <see cref="SequenceCommand.TutorialPassed"/>, or when <see cref="Tutorial"/> detects <see cref="TimeTrackerScript.CountdownThisSection"/> reached 0 (main segment time exhausted).</summary>
    public class TutorialStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;

        private bool variantWatchesWwiseVOCuesForCompletion = true;
        private bool _hasEntered = false;

        public TutorialStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public StageType StageType => StageType.Tutorial;

        public bool IsComplete { get; private set; }
        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.StartInteractive
            || sequenceCommand == SequenceCommand.Break_Tests
            || sequenceCommand == SequenceCommand.TutorialPassed;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            // Long: Wwise cues (StartInteractive, Break_Tests) or explicit TutorialPassed / StopTutorial.
            // Short: only TutorialPassed (e.g. guidance-count path or StopTutorial); no StartInteractive/Break_Tests from Wwise for that flow.
        
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

        public void Enter(string variant)
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
            Debug.Log("TutorialStageHandler: Enter");

            if(variant == "Long")
            {
                _sequencer.tutorial.SetTestVocalizationType("Hum");
                MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.InteractiveTutorial);
                _sequencer.tutorial.StartTutorial("Long");
                variantWatchesWwiseVOCuesForCompletion = true;
            }
            else if(variant == "Short")
            {
                _sequencer.tutorial.SetTestVocalizationType("Ahh");
                MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Silent);
                _sequencer.tutorial.StartTutorial("Short");
                variantWatchesWwiseVOCuesForCompletion = false;
            }
            else
            {
                Debug.LogError("TutorialStageHandler: Invalid variant: " + variant);
                _hasEntered = false;
                return;
            }

            _sequencer.wwiseVOManager.ResetTutorialGuidanceCount();
            MusicSystem1.instance.SetAllowTransitionFromEnvironmentToFreeplay(false);
            _sequencer.StartLights();
            _sequencer.imitoneVoiceInterpreter.gameOn = true;

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
            if (_sequencer != null && _sequencer.tutorial != null)
                _sequencer.tutorial.StopTutorial();
            _hasEntered = false;
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            LocalCleanup();
        }
    }
}
