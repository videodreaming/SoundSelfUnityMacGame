using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Stub Calibration stage handler. Pre-sequence UI/audio is still driven mainly by CalibrationMenu; completes immediately until wired to that flow.</summary>
    public class CalibrationStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _calibrationUiListenersRegistered;

        public StageType StageType => StageType.Calibration;

        public bool IsComplete { get; private set; }

        public CalibrationStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public void Enter(StageVariant variant)
        {
            IsComplete = false;
            Debug.Log("CalibrationStageHandler: Enter (stub - skipping until implementation added)");
            if (_sequencer != null && _sequencer.calibrationMenu != null)
            {
                _sequencer.calibrationMenu.StartCalibrationSequence();
                //Open the Calibraiton Menu:
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

            ui.OnMicrophoneScreenNextPress += HandleMicrophoneScreenNextPress;
            ui.OnHeadphoneScreenNextPress += HandleHeadphoneScreenNextPress;
            ui.OnVibroacousticScreenNextPress += HandleVibroacousticScreenNextPress;
            ui.OnLightglassScreenNextPress += HandleLightglassScreenNextPress;
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
                ui.OnMicrophoneScreenNextPress -= HandleMicrophoneScreenNextPress;
                ui.OnHeadphoneScreenNextPress -= HandleHeadphoneScreenNextPress;
                ui.OnVibroacousticScreenNextPress -= HandleVibroacousticScreenNextPress;
                ui.OnLightglassScreenNextPress -= HandleLightglassScreenNextPress;
                ui.OnHeadphoneTroubleshootingPress -= HandleHeadphoneTroubleshootingPress;
            }

            _calibrationUiListenersRegistered = false;
        }

        private void HandleHeadphoneScreenNextPress()
        {
            Debug.Log("CalibrationStageHandler: OnHeadphoneScreenNextPress. Transitioning to Microphone Screen.");
            UIManager.Instance.SetCalibrationScreen(CalibrationUI.Microphone);
        }

        private void HandleMicrophoneScreenNextPress()
        {
            Debug.Log("CalibrationStageHandler: OnMicrophoneScreenNextPress (stub).");
            UIManager.Instance.SetCalibrationScreen(CalibrationUI.VibroAcoustic);
        }

        private void HandleVibroacousticScreenNextPress()
        {
            Debug.Log("CalibrationStageHandler: OnVibroacousticScreenNextPress (stub).");
            UIManager.Instance.SetCalibrationScreen(CalibrationUI.LightGlass);
        }

        private void HandleLightglassScreenNextPress()
        {
            Debug.Log("CalibrationStageHandler: OnLightglassScreenNextPress (stub).");
            UIManager.Instance.SetChoiceScreen();
        }

        private void HandleHeadphoneTroubleshootingPress()
        {
            Debug.Log("CalibrationStageHandler: OnHeadphoneTroubleshootingPress (stub).");
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
