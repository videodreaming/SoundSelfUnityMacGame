using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handler for the SetMenu stage. Watches for <see cref="SequenceCommand.WaitForButton"/> (e.g. Cue_WaitForButton); stub completes immediately until implementation.</summary>
    public class SetMenuStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        /// <summary>True after <see cref="Enter"/> started the menu linear bed (Welcome or Album Choice); cleared when we <see cref="StopMenuLinearBedIfWeStartedIt"/>.</summary>
        private bool _startedMenuLinearBed;

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
            _sequencer?.FadePreferredColorDarkAndStopAvs();
            _sequencer?.StopOpeningAudioFromStageEnter();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.EnableSkipButton(false);
                UIManager.Instance.EnableSkipStageButton(false);
            }

            if (variant == StageVariant.Menu_Ps_InteractiveOrMusic)
            {
                Debug.Log("SetMenuStageHandler: Enter Menu_Ps_InteractiveOrMusic — Choice SS or Music.");
                if (UIManager.Instance != null)
                {
                    bool secondStageVariant = _sequencer != null && _sequencer.dualstageStage >= 1;
                    UIManager.Instance.SetChoiceSSOrMusicScreen(secondStageVariant);
                }
                else
                    Debug.LogError("SetMenuStageHandler: UIManager.Instance is null; cannot show Choice SS or Music screen.");
                return;
            }
            else if (variant == StageVariant.Menu_AlbumChoice)
            {
                Debug.Log("SetMenuStageHandler: Enter Menu_AlbumChoice — Choice Album.");
                if (UIManager.Instance != null)
                    UIManager.Instance.SetChoiceScreen(ChoiceScreen.Album, clearNavigationStack: true);
                else
                    Debug.LogError("SetMenuStageHandler: UIManager.Instance is null; cannot show Choice Album screen.");
                TryStartMenuLinearAmbientBedIfPolicySaysSo(variant, "Album Choice");
                return;
            }
            else if (variant == StageVariant.Menu_Welcome_PreCalibration)
            {
                Debug.Log("SetMenuStageHandler: Enter Menu_Welcome_PreCalibration.");
                if (UIManager.Instance != null)
                    UIManager.Instance.SetWelcomeScreen();
                else
                    Debug.LogError("SetMenuStageHandler: UIManager.Instance is null; cannot show Welcome screen.");
                TryStartMenuLinearAmbientBedIfPolicySaysSo(variant, "Welcome");
                return;
            }
            else
            {
                Debug.Log("SetMenuStageHandler: UNDEFINED VARIANT (stub - skipping until implementation added)");
                MarkComplete();
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
            StopMenuLinearBedIfWeStartedIt();
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
            // Safety net: force-exit / sequence reset may call Exit without a prior BeginTransitionOut for this visit.
            StopMenuLinearBedIfWeStartedIt();
        }

        /// <summary>Idempotent ambient bed start when <see cref="SetMenuStagePolicy"/> expects it for this variant.</summary>
        private void TryStartMenuLinearAmbientBedIfPolicySaysSo(StageVariant variant, string menuLabel)
        {
            if (!SetMenuStagePolicy.TryGetEnterExpectation(variant, out var exp) || !exp.StartsLinearAmbientBed)
                return;

            // Linear ambient bed: this stage starts it; we stop it on BeginTransitionOut / Exit (same owner).
            // MusicPlaylistStageHandler.Enter always Stop()s as well before playlist VO/music.
            if (MusicSystemLinear.instance != null)
            {
                MusicSystemLinear.instance.Play();
                _startedMenuLinearBed = true;
            }
            else
            {
                Debug.LogWarning("SetMenuStageHandler: MusicSystemLinear.instance is null — ambient bed will not start at " + menuLabel + ".");
                _startedMenuLinearBed = false;
            }
        }

        private void StopMenuLinearBedIfWeStartedIt()
        {
            if (!_startedMenuLinearBed)
                return;
            _startedMenuLinearBed = false;
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
