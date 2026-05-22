using System.Collections;
using UnityEngine;
using ConversionUtilities;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Savasana stage: shared transition setup + variant-based VO routing (thematic vs ascending), then completes on Wwise <c>Cue_ClosingGoodbye_End</c>.</summary>
    public class SavasanaStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _hasEntered;
        private StageVariant _variant = StageVariant.None;

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

        /// <param name="variant">Selects savasana behavior for sequence commands (e.g. Standard vs PS Ascending).</param>
        public void Enter(StageVariant variant)
        {
            if (_hasEntered)
            {
                Debug.LogError("SavasanaStageHandler: ENTER() CALLED AGAIN BEFORE EXIT(). THE SEQUENCE IS LIKELY BROKEN. STOP THE SEQUENCE BEFORE STARTING IT AGAIN.");
                return;
            }
            _hasEntered = true;
            IsComplete = false;
            _variant = variant;

            if (_sequencer == null)
            {
                Debug.LogError("SavasanaStageHandler: Sequencer is null (countdown not updated).");
                _hasEntered = false;
                MarkComplete();
                return;
            }
            _sequencer.StopCalibrationInteractiveMusicFromStageEnter();
            _sequencer.StopOpeningAudioFromStageEnter();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetMeditationScreen();
                UIManager.Instance.RefreshSessionDualStageBannerFromSequencer(_sequencer);
                UIManager.Instance.RefreshSkipStageButtonFromSequencer(_sequencer);
                UIManager.Instance.EnableSkipButton(false, null);
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

            Debug.Log("SavasanaStageHandler: Enter - running Savasana for variant '" + _variant + "'.");

            MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);
            _sequencer.director.ActivateQueue(15f);
            _sequencer.director.Disable();
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.MusicLoopSilent);
            MusicSystem1.instance.SetBreathworkCycle(false);
            MusicSystem1.instance.SetAllowThumpAlways(false);
            MusicSystem1.instance.SetAllowThumpWhenModeIsPlayful(false);
            PlaySavasanaVoForVariant(_variant);

            if (_sequencer.savasana != null && UIManager.Instance != null && _sequencer.imitoneVoiceInterpreter != null)
                _sequencer.savasana.BeginShowSavasanaSectionHeaderWhenGameOff(_sequencer.imitoneVoiceInterpreter, UIManager.Instance, _sequencer);

            if(IsAscendingVariant())
            {
                _sequencer.StopAllAvsPrograms();
                if (AVSSequence.instance != null)
                    AVSSequence.instance.StartDropToDelta();
                else
                    Debug.LogError("SavasanaStageHandler: AVSSequence.instance is null. Cannot start DropToDelta.");
            }
            if (IsStandardVariant())
            {
                _sequencer.FadePreferredColorDarkAndStopAvs();
            }
        }

        private bool IsStandardVariant() => _variant == StageVariant.Savasana_Standard;
        private bool IsAscendingVariant() => _variant == StageVariant.Savasana_PsAscending;

        private void PlaySavasanaVoForVariant(StageVariant variant)
        {
            if (IsAscendingVariant())
            {
                Debug.Log("SavasanaStageHandler: Playing Ascending closing VO.");
                _sequencer.wwiseVOManager.PlayAscendingClosing();
                return;
            }

            if (IsStandardVariant())
            {
                Debug.Log("SavasanaStageHandler: Playing Thematic savasana VO.");
                _sequencer.wwiseVOManager.PlayThematicSavasana();
                return;
            }

            Debug.LogWarning("SavasanaStageHandler: Unrecognized variant '" + variant + "'. Defaulting to Thematic savasana VO.");
            _sequencer.wwiseVOManager.PlayThematicSavasana();
        }

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand)
        {
            return sequenceCommand == SequenceCommand.EndThisSequenceStage
                || sequenceCommand == SequenceCommand.CueClosingGoodbyeEnd
                || sequenceCommand == SequenceCommand.CueStopInteractive
                || sequenceCommand == SequenceCommand.CueStopInteractive3m
                || sequenceCommand == SequenceCommand.CueSilentMeditationStart;
        }

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            if (sequenceCommand == SequenceCommand.EndThisSequenceStage)
            {
                MarkComplete();
                return;
            }

            if (sequenceCommand == SequenceCommand.CueClosingGoodbyeEnd)
            {
                Debug.Log("SavasanaStageHandler: Cue_ClosingGoodbye_End — completing Savasana. " + SessionCountdownPairForLog());
                MarkComplete();
                return;
            }

            switch (sequenceCommand)
            {
                case SequenceCommand.CueStopInteractive:
                    if (IsStandardVariant())
                    {
                        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.FrozenFreeplay);
                        Debug.Log("SavasanaStageHandler: CueStopInteractive — SetMusicModeTo FrozenFreeplay (Standard).");
                    }
                    else
                    {
                        Debug.Log($"SavasanaStageHandler: {sequenceCommand} — not handled for variant: {_variant}");
                    }
                    break;
                case SequenceCommand.CueStopInteractive3m:
                    _sequencer.StartCoroutine(DelayedMicOff(120f));
                    break;
                case SequenceCommand.CueSilentMeditationStart:
                    Debug.Log("SavasanaStageHandler: CueSilentMeditationStart — fading to dark and stopping AVS programs.");
                    _sequencer.FadePreferredColorDarkAndStopAvs();
                    break;
            }
        }

        private IEnumerator DelayedMicOff(float delay)
        {
            yield return new WaitForSeconds(delay);
            if(_hasEntered && !IsComplete)
            _sequencer.imitoneVoiceInterpreter.SetGameOn(false);
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
            if (_sequencer != null && _sequencer.savasana != null)
                _sequencer.savasana.CancelSavasanaSectionHeaderWaitIfRunning();
            if (MusicSystem1.instance != null)
                MusicSystem1.instance.SetBreathworkCycle(false);
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            _hasEntered = false;  // Allow re-enter on sequence restart
            _variant = StageVariant.None;
            LocalCleanup();
        }

        /// <inheritdoc />
        public void OnSessionSkipFromUi() => LocalCleanup();
    }
}
