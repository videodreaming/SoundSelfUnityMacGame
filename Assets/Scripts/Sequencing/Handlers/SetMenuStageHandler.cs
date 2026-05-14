using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handler for the SetMenu stage. Watches for <see cref="SequenceCommand.WaitForButton"/> (e.g. Cue_WaitForButton); stub completes immediately until implementation.</summary>
    public class SetMenuStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        /// <summary>True after <see cref="Enter"/> successfully started the welcome linear bed; cleared when we <see cref="StopWelcomeLinearBedIfWeStartedIt"/>.</summary>
        private bool _startedWelcomeLinearBed;

        public SetMenuStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public StageType StageType => StageType.SetMenu;

        public bool IsComplete { get; private set; }

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            //sequenceCommand == SequenceCommand.WaitForButton ||
            sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            //if (sequenceCommand == SequenceCommand.WaitForButton || sequenceCommand == SequenceCommand.EndThisSequenceStage)
            if (sequenceCommand == SequenceCommand.EndThisSequenceStage)
                MarkComplete();
        }

        public void Enter(StageVariant variant)
        {
            IsComplete = false;
            if (variant == StageVariant.Menu_Ps_InteractiveOrMusic)
            {
                Debug.Log("SetMenuStageHandler: Enter Menu_Ps_InteractiveOrMusic — Choice SS or Music.");
                if (UIManager.Instance != null)
                    UIManager.Instance.SetChoiceSSOrMusicScreen();
                else
                    Debug.LogError("SetMenuStageHandler: UIManager.Instance is null; cannot show Choice SS or Music screen.");
                return;
            }
            else if (variant == StageVariant.Menu_Welcome_PreCalibration)
            {
                Debug.Log("SetMenuStageHandler: Enter Menu_Welcome_PreCalibration.");
                UIManager.Instance.SetWelcomeScreen();
                // Linear ambient bed: this stage starts it; we stop it on BeginTransitionOut / Exit (same owner — no blanket Stop in other handlers).
                if (MusicSystemLinear.instance != null)
                {
                    MusicSystemLinear.instance.Play();
                    _startedWelcomeLinearBed = true;
                }
                else
                {
                    Debug.LogWarning("SetMenuStageHandler: MusicSystemLinear.instance is null — ambient bed will not start at Welcome.");
                    _startedWelcomeLinearBed = false;
                }
                return;
            }
            else
            {
                Debug.Log("SetMenuStageHandler: UNDEFINED VARIANT (stub - skipping until implementation added)");
                MarkComplete(); // Stub: complete immediately; cue-watching in place for when implementation is added
                return;
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
            Debug.Log("SetMenuStageHandler: Marking stage complete.");
            // Next: On the next SequenceRunner.Update(), the runner sees IsComplete and calls TransitionToNextStage().
            // That calls AdvanceToStage(next), which invokes BeginTransitionOut() on this handler (tail / fade start),
            // then enters the next stage. Cleanup when this stage is fully retired belongs in Exit() (via LocalCleanup).
            // Exit() is invoked by the runner when this stage leaves the tracked window, e.g. a jump skips past it
            // (older than immediate previous), StartSequence resets, or similar — not necessarily on every linear step.
        }

        /// <summary>Runner-only: start transition-out (tail) while the next stage is already entering.</summary>
        public void BeginTransitionOut()
        {
            // Runner calls this on the outgoing stage before the next stage's Enter — right place to tear down welcome-only audio.
            StopWelcomeLinearBedIfWeStartedIt();
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
            // Safety net: force-exit / sequence reset may call Exit without a prior BeginTransitionOut for this visit.
            StopWelcomeLinearBedIfWeStartedIt();
        }

        private void StopWelcomeLinearBedIfWeStartedIt()
        {
            if (!_startedWelcomeLinearBed)
                return;
            _startedWelcomeLinearBed = false;
            if (MusicSystemLinear.instance != null)
                MusicSystemLinear.instance.Stop();
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            LocalCleanup();
        }
    }
}
