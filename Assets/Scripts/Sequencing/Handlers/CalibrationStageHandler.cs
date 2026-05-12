using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub Calibration stage handler. Pre-sequence UI/audio is still driven mainly by CalibrationMenu; completes immediately until wired to that flow.</summary>
    public class CalibrationStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _calibrationUiListenersRegistered;
        /// <summary>Variant passed to <see cref="Enter"/> for this calibration run. Stubs: <see cref="StageVariant.Calibration_Album"/>, <see cref="StageVariant.Calibration_NoVibro"/>.</summary>
        private StageVariant _activeCalibrationVariant = StageVariant.Calibration_Default;

        public StageType StageType => StageType.Calibration;

        public bool IsComplete { get; private set; }

        public CalibrationStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

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
            if (variant == StageVariant.Calibration_Album || variant == StageVariant.Calibration_NoVibro || variant == StageVariant.Calibration_Default)
                _activeCalibrationVariant = variant;
            else
                _activeCalibrationVariant = StageVariant.Calibration_Default;
            LogCalibrationVariantStub(_activeCalibrationVariant);

            Debug.Log("CalibrationStageHandler: Enter (stub - skipping until implementation added)");
            if (_sequencer != null && _sequencer.calibrationMenu != null)
            {
                _sequencer.calibrationMenu.StartCalibrationSequence();
                // Open the calibration UI:
                UIManager.Instance.SetCalibrationScreen(CalibrationUI.Headphone);

                SubscribeCalibrationUiListeners();
            }
            else
                Debug.LogError("CalibrationStageHandler: Sequencer or calibrationMenu is null. Cannot start calibration UI.");

            // Stub behavior: auto-advance until calibration flow owns completion signaling.
            // MarkComplete();
        }

        private void SubscribeCalibrationUiListeners()
        {
            var ui = UIManager.Instance;
            if (ui == null)
            {
                Debug.LogWarning("CalibrationStageHandler: UIManager.Instance is null; cannot subscribe to calibration UI events.");
                return;
            }

            if (_calibrationUiListenersRegistered)
                return;

            ui.OnMicrophoneNextStepPress += HandleMicrophoneNextStepPress;
            ui.OnHeadphoneNextStepPress += HandleHeadphoneNextStepPress;
            ui.OnVibroacousticNextStepPress += HandleVibroacousticNextStepPress;
            ui.OnLightGlassesNextStepPress += HandleLightGlassesNextStepPress;
            ui.OnHeadphoneTroubleshootingPress += HandleHeadphoneTroubleshootingPress;
            _calibrationUiListenersRegistered = true;
        }

        private void UnsubscribeCalibrationUiListeners()
        {
            if (!_calibrationUiListenersRegistered)
                return;

            var ui = UIManager.Instance;
            if (ui != null)
            {
                ui.OnMicrophoneNextStepPress -= HandleMicrophoneNextStepPress;
                ui.OnHeadphoneNextStepPress -= HandleHeadphoneNextStepPress;
                ui.OnVibroacousticNextStepPress -= HandleVibroacousticNextStepPress;
                ui.OnLightGlassesNextStepPress -= HandleLightGlassesNextStepPress;
                ui.OnHeadphoneTroubleshootingPress -= HandleHeadphoneTroubleshootingPress;
            }

            _calibrationUiListenersRegistered = false;
        }

        private void HandleHeadphoneNextStepPress()
        {
            Debug.Log("CalibrationStageHandler: OnHeadphoneNextStepPress. Transitioning to Microphone screen.");
            UIManager.Instance.SetCalibrationScreen(CalibrationUI.Microphone);
        }

        private void HandleMicrophoneNextStepPress()
        {
            Debug.Log("CalibrationStageHandler: OnMicrophoneNextStepPress (stub).");
            UIManager.Instance.SetCalibrationScreen(CalibrationUI.VibroAcoustic);
        }

        private void HandleVibroacousticNextStepPress()
        {
            Debug.Log("CalibrationStageHandler: OnVibroacousticNextStepPress (stub).");
            UIManager.Instance.SetCalibrationScreen(CalibrationUI.LightGlasses);
        }

        private void HandleLightGlassesNextStepPress()
        {
            Debug.Log("CalibrationStageHandler: OnLightGlassesNextStepPress (stub).");
            UIManager.Instance.SetChoiceSSOrMusicScreen();
        }

        private void HandleHeadphoneTroubleshootingPress()
        {
            Debug.Log("CalibrationStageHandler: OnHeadphoneTroubleshootingPress (stub).");
        }

        /// <summary>Stub until step lists read variant: NoVibro skips vibro; Album reserved for alternate copy/order.</summary>
        private static void LogCalibrationVariantStub(StageVariant variant)
        {
            switch (variant)
            {
                case StageVariant.Calibration_Album:
                    Debug.Log("CalibrationStageHandler: variant Calibration_Album (stub — same stage type, alternate flow TBD).");
                    break;
                case StageVariant.Calibration_NoVibro:
                    Debug.Log("CalibrationStageHandler: variant Calibration_NoVibro (stub — vibro step will be omitted when wired).");
                    break;
                default:
                    Debug.Log("CalibrationStageHandler: variant " + variant + " (default full calibration path when wired).");
                    break;
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
            Debug.Log("CalibrationStageHandler: Marking stage complete.");
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
            // Stub — IStageHandler default is no-op; explicit method documents intent.
        }

        /// <summary>Shared teardown; intended to be called from Exit() or from both Exit() and BeginTransitionOut() (then must keep idempotent).</summary>
        private void LocalCleanup()
        {
            UnsubscribeCalibrationUiListeners();

            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.StopCalibrationSequence();
        }

        /// <summary>Runner-only: final retirement; safe if called more than once.</summary>
        public void Exit()
        {
            LocalCleanup();
        }
    }
}
