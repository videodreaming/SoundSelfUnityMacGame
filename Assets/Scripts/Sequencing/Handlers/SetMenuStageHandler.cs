using System.Collections;
using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handler for the SetMenu stage. Watches for <see cref="SequenceCommand.WaitForButton"/> (e.g. Cue_WaitForButton); stub completes immediately until implementation.</summary>
    public class SetMenuStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;

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
                Debug.Log("SetMenuStageHandler: Enter Menu_Ps_InteractiveOrMusic (stub) - auto-branching to Adjunctive interactive sequence.");
                MarkComplete(); // Temporary behavior until menu UI branch choice is implemented.
                if (_sequencer != null)
                    _sequencer.StartCoroutine(StartProtocolStacksInteractiveNextFrame());
                else
                    Debug.LogError("SetMenuStageHandler: Sequencer is null. Cannot auto-branch to Adjunctive interactive sequence.");
                return;
            }

            Debug.Log("SetMenuStageHandler: Enter (stub - skipping until implementation added)");
            MarkComplete(); // Stub: complete immediately; cue-watching in place for when implementation is added
        }

        private IEnumerator StartProtocolStacksInteractiveNextFrame()
        {
            // Avoid re-entering SequenceRunner.StartSequence while it is still inside AdvanceToStage/TransitionToNextStage.
            yield return null;
            if (_sequencer != null)
                _sequencer.StartProtocolStacksInteractiveSequence();
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
