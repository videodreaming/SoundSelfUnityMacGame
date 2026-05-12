using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Calibration UI flow: variant-driven step list; generic Next/Back; Conclusion confirm (Stage E adds VO-done + <c>MarkComplete</c>).</summary>
    public class CalibrationStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _calibrationUiListenersRegistered;
        /// <summary>Variant passed to <see cref="Enter"/> for this calibration run.</summary>
        private StageVariant _activeCalibrationVariant = StageVariant.Calibration_Default;
        private CalibrationUI[] _steps = System.Array.Empty<CalibrationUI>();
        private int _stepIndex;
        /// <summary>Set when user presses conclusion confirm; Stage E pairs with VO-done before <see cref="MarkComplete"/>.</summary>
        private bool _conclusionButtonPressed;

        public StageType StageType => StageType.Calibration;

        public bool IsComplete { get; private set; }

        public CalibrationStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        /// <summary>Completion is driven from UI + Wwise in Stage E — not <see cref="SequenceCommand.EndThisSequenceStage"/> on this handler.</summary>
        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) => false;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand) { }

        public void Enter(StageVariant variant)
        {
            IsComplete = false;
            _conclusionButtonPressed = false;
            if (variant == StageVariant.Calibration_Album || variant == StageVariant.Calibration_NoVibro || variant == StageVariant.Calibration_Default)
                _activeCalibrationVariant = variant;
            else
                _activeCalibrationVariant = StageVariant.Calibration_Default;
            LogCalibrationVariantStub(_activeCalibrationVariant);

            _steps = BuildStepList(_activeCalibrationVariant);
            _stepIndex = 0;

            Debug.Log("CalibrationStageHandler: Enter — step count " + _steps.Length + ", first screen " + _steps[0]);
            if (_sequencer != null && _sequencer.calibrationMenu != null)
            {
                _sequencer.calibrationMenu.StartCalibrationSequence();
                UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
                SubscribeCalibrationUiListeners();
            }
            else
                Debug.LogError("CalibrationStageHandler: Sequencer or calibrationMenu is null. Cannot start calibration UI.");
        }

        private static CalibrationUI[] BuildStepList(StageVariant variant)
        {
            switch (variant)
            {
                case StageVariant.Calibration_NoVibro:
                    return new[]
                    {
                        CalibrationUI.Start,
                        CalibrationUI.Headphone,
                        CalibrationUI.Microphone,
                        CalibrationUI.LightGlasses,
                        CalibrationUI.Conclusion
                    };
                case StageVariant.Calibration_Album:
                case StageVariant.Calibration_Default:
                default:
                    return new[]
                    {
                        CalibrationUI.Start,
                        CalibrationUI.Headphone,
                        CalibrationUI.Microphone,
                        CalibrationUI.VibroAcoustic,
                        CalibrationUI.LightGlasses,
                        CalibrationUI.Conclusion
                    };
            }
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

            ui.OnCalibrationNextStepPress += HandleCalibrationNextStepPress;
            ui.OnCalibrationBackPress += HandleCalibrationBackPress;
            ui.OnCalibrationConclusionConfirmPress += HandleCalibrationConclusionConfirmPress;
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
                ui.OnCalibrationNextStepPress -= HandleCalibrationNextStepPress;
                ui.OnCalibrationBackPress -= HandleCalibrationBackPress;
                ui.OnCalibrationConclusionConfirmPress -= HandleCalibrationConclusionConfirmPress;
                ui.OnHeadphoneTroubleshootingPress -= HandleHeadphoneTroubleshootingPress;
            }

            _calibrationUiListenersRegistered = false;
        }

        private void HandleCalibrationNextStepPress()
        {
            if (_steps == null || _steps.Length == 0)
                return;
            if (_stepIndex < 0 || _stepIndex >= _steps.Length)
                return;

            if (_steps[_stepIndex] == CalibrationUI.Conclusion)
            {
                Debug.Log("CalibrationStageHandler: Next step ignored on Conclusion — use CalibrationConclusionConfirmButtonPress.");
                return;
            }

            if (_stepIndex >= _steps.Length - 1)
            {
                Debug.Log("CalibrationStageHandler: Next step ignored — already at last step.");
                return;
            }

            _stepIndex++;
            Debug.Log("CalibrationStageHandler: Next step → index " + _stepIndex + " screen " + _steps[_stepIndex]);
            UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
        }

        private void HandleCalibrationBackPress()
        {
            if (_steps == null || _steps.Length == 0)
                return;
            if (_stepIndex <= 0)
            {
                Debug.Log("CalibrationStageHandler: Back ignored on Start.");
                return;
            }

            _stepIndex--;
            Debug.Log("CalibrationStageHandler: Back → index " + _stepIndex + " screen " + _steps[_stepIndex]);
            UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
        }

        private void HandleCalibrationConclusionConfirmPress()
        {
            if (_steps == null || _steps.Length == 0)
                return;
            if (_stepIndex < 0 || _stepIndex >= _steps.Length || _steps[_stepIndex] != CalibrationUI.Conclusion)
            {
                Debug.Log("CalibrationStageHandler: Conclusion confirm ignored — not on Conclusion screen.");
                return;
            }

            _conclusionButtonPressed = true;
            Debug.Log("CalibrationStageHandler: Conclusion confirm recorded (stub — Stage E: pair with VO-done then MarkComplete).");
        }

        private void HandleHeadphoneTroubleshootingPress()
        {
            Debug.Log("CalibrationStageHandler: OnHeadphoneTroubleshootingPress (stub).");
        }

        private static void LogCalibrationVariantStub(StageVariant variant)
        {
            switch (variant)
            {
                case StageVariant.Calibration_Album:
                    Debug.Log("CalibrationStageHandler: variant Calibration_Album (stub — same step count as default until copy/order differs).");
                    break;
                case StageVariant.Calibration_NoVibro:
                    Debug.Log("CalibrationStageHandler: variant Calibration_NoVibro — vibro step omitted from step list.");
                    break;
                default:
                    Debug.Log("CalibrationStageHandler: variant " + variant + " (default full calibration path).");
                    break;
            }
        }

        public void BeginTransitionOut()
        {
            LocalCleanup();
        }

        private void LocalCleanup()
        {
            UnsubscribeCalibrationUiListeners();

            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.StopCalibrationSequence();
        }

        public void Exit()
        {
            LocalCleanup();
        }
    }
}
