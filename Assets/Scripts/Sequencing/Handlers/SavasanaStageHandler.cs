using System.Collections;
using UnityEngine;
using ConversionUtilities;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Savasana stage: locks fundamental, activates director queue, plays Ascending Closing VO. Completes immediately (savasana plays to end).</summary>
    public class SavasanaStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _hasEntered;

        public StageType StageType => StageType.Savasana;

        public bool IsComplete { get; private set; }

        public SavasanaStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        /// <summary>Session countdown lives on <see cref="TimeTrackerScript"/>; Sequencer exposes the same value for convenience.</summary>
        private string SessionCountdownPairForLog()
        {
            var tt = TimeTrackerScript.instance;
            if (tt != null)
                return "[CountdownThisSection]=" + tt.CountdownThisSection + " [CountdownFull]=" + tt.CountdownFull;
            return _sequencer != null ? "[CountdownThisSection]=" + _sequencer.CountdownThisSection : "tracker null";
        }

        /// <param name="variant">Reserved for future use. Will select savasana type when we add back Preparation, Integration, and PS_Descending modes.</param>
        public void Enter(string variant)
        {
            if (_hasEntered)
            {
                Debug.LogError("SavasanaStageHandler: ENTER() CALLED AGAIN BEFORE EXIT(). THE SEQUENCE IS LIKELY BROKEN. STOP THE SEQUENCE BEFORE STARTING IT AGAIN.");
                return;
            }
            _hasEntered = true;
            IsComplete = false;

            if (_sequencer == null)
            {
                Debug.LogError("SavasanaStageHandler: Sequencer is null (countdown not updated).");
                _hasEntered = false;
                MarkComplete();
                return;
            }
            if (_sequencer.director == null)
            {
                Debug.LogError("SavasanaStageHandler: director is null. " + SessionCountdownPairForLog());
                _hasEntered = false;
                MarkComplete();
                return;
            }
            if (_sequencer.wwiseVOManager == null)
            {
                Debug.LogError("SavasanaStageHandler: wwiseVOManager is null. " + SessionCountdownPairForLog());
                _hasEntered = false;
                MarkComplete();
                return;
            }
            if (MusicSystem1.instance == null)
            {
                Debug.LogError("SavasanaStageHandler: MusicSystem1.instance is null. " + SessionCountdownPairForLog());
                _hasEntered = false;
                MarkComplete();
                return;
            }

            Debug.Log("SavasanaStageHandler: Enter - running Savasana (Ascending Closing)");

            MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);
            _sequencer.director.ActivateQueue(15f);
            _sequencer.director.Disable();
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.MusicLoopSilent);
            MusicSystem1.instance.SetAllowTransitionFromEnvironmentToFreeplay(false);
            MusicSystem1.instance.SetBreathworkCycle(false);
            MusicSystem1.instance.SetAllowThumpAlways(false);
            MusicSystem1.instance.SetAllowThumpWhenModeIsPlayful(false);
            _sequencer.wwiseVOManager.PlayAscendingClosing();

            _sequencer.StartCoroutine(WaitForTimerToEnd());
        }

        private IEnumerator WaitForTimerToEnd()
        {
            //waits for the "this section" countdown to come to an end
            while(TimeTrackerScript.instance.CountdownThisSection > 0)
            {
                yield return null;
            }
            MarkComplete();
        }

        //--------------------------------
        // Lifecycle after main work: complete -> (optional) transition-out tail -> Exit
        // SequenceRunner owns BeginTransitionOut / Exit timing; handlers should not call those locally.
        //--------------------------------

        /// <summary>Main phase done (or unrecoverable error path). Does not start transition-out; that begins when the runner advances.</summary>
        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
            Debug.Log("SavasanaStageHandler: Marking stage complete.");
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
            // No tail yet for Savasana; IStageHandler default is also no-op — explicit method documents intent.
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            _hasEntered = false;  // Allow re-enter on sequence restart
            LocalCleanup();
        }
    }
}
