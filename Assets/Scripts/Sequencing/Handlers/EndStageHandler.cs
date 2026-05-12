using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub handler for the End stage. End stage that happens at the end of a sequence. Skips immediately until implementation is added.</summary>
    public class EndStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;

        public EndStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public StageType StageType => StageType.End;

        public bool IsComplete { get; private set; }

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            if (sequenceCommand == SequenceCommand.EndThisSequenceStage)
                MarkComplete();
        }

        public void Enter(StageVariant variant)
        {
            IsComplete = false;
            Debug.Log("EndStageHandler: Enter (stub - skipping until implementation added)");
            // Stage D: ensure the linear ambient bed is torn down at session end. Idempotent.
            if (MusicSystemLinear.instance != null)
                MusicSystemLinear.instance.Stop();
            MarkComplete(); // Stub: complete immediately until implementation is added
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
            Debug.Log("EndStageHandler: Marking stage complete.");
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
            // Stub — IStageHandler default is no-op; explicit method documents intent.
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            LocalCleanup();
        }
    }
}
