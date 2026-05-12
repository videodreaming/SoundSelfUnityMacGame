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
        /// <summary>True once <see cref="Enter"/> has cached the prior normalization rise-rate so <see cref="LocalCleanup"/> can restore it exactly once.</summary>
        private bool _hasCapturedNormalizationRiseRate;
        /// <summary>Cached rise-rate (dB/sec) from <see cref="ImitoneVoiceIntepreter"/> at <see cref="Enter"/> time; restored on cleanup. We multiply by 6× while calibration is active.</summary>
        private float _capturedNormalizationRiseRateDbPerSecond;
        private const float CalibrationNormalizationRiseRateMultiplier = 6f;
        /// <summary>True while calibration has claimed the <see cref="MusicSystem1.SetCalibrationMonitoringOverride"/> flag, so cleanup releases it idempotently.</summary>
        private bool _hasClaimedMonitoringOverride;

        public StageType StageType => StageType.Calibration;

        public bool IsComplete { get; private set; }

        public CalibrationStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        /// <summary>Watches the 4 calibration music-sync cue commands routed from <c>CalibrationMenu</c>. Completion is driven from UI + Wwise in Stage E — <see cref="SequenceCommand.EndThisSequenceStage"/> is intentionally not watched here.</summary>
        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.CalibrationMicrophoneOn
            || sequenceCommand == SequenceCommand.CalibrationMicrophoneOff
            || sequenceCommand == SequenceCommand.CalibrationAvsStart
            || sequenceCommand == SequenceCommand.CalibrationAvsEnd;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            switch (sequenceCommand)
            {
                case SequenceCommand.CalibrationMicrophoneOn:
                    if (_sequencer != null && _sequencer.imitoneVoiceInterpreter != null)
                        _sequencer.imitoneVoiceInterpreter.SetGameOn(true);
                    else
                        Debug.LogWarning("CalibrationStageHandler: CalibrationMicrophoneOn — imitoneVoiceInterpreter is null.");
                    break;
                case SequenceCommand.CalibrationMicrophoneOff:
                    if (_sequencer != null && _sequencer.imitoneVoiceInterpreter != null)
                        _sequencer.imitoneVoiceInterpreter.SetGameOn(false);
                    else
                        Debug.LogWarning("CalibrationStageHandler: CalibrationMicrophoneOff — imitoneVoiceInterpreter is null.");
                    break;
                case SequenceCommand.CalibrationAvsStart:
                    if (LightControl.instance != null && LightControl.instance.gameObject.activeInHierarchy)
                    {
                        LightControl.instance.SetPreferredColor("White", 5.0f);
                        LightControl.instance.SetStrobeRate(10f, 0.0f);
                    }
                    else
                        Debug.LogError("CalibrationStageHandler: CalibrationAvsStart — LightControl.instance is null or inactive; cannot drive lights.");
                    break;
                case SequenceCommand.CalibrationAvsEnd:
                    if (LightControl.instance != null && LightControl.instance.gameObject.activeInHierarchy)
                        LightControl.instance.LightSettingsInitialization(5.0f);
                    else
                        Debug.LogError("CalibrationStageHandler: CalibrationAvsEnd — LightControl.instance is null or inactive; cannot restore lights.");
                    break;
            }
        }

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
            ApplyCalibrationMonitoringBoost();
            if (_sequencer != null && _sequencer.calibrationMenu != null)
            {
                _sequencer.calibrationMenu.SetCalibrationPortionSwitch(MapStepToPortion(_steps[_stepIndex]));
                _sequencer.calibrationMenu.StartCalibrationSequence(_sequencer);
                UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
                SubscribeCalibrationUiListeners();
                EnforceGameOnForStep(_steps[_stepIndex]);
            }
            else
                Debug.LogError("CalibrationStageHandler: Sequencer or calibrationMenu is null. Cannot start calibration UI.");
        }

        /// <summary>
        /// Defensive bookend for <c>imitoneVoiceInterpreter.gameOn</c>: forces <c>false</c> on every step except <see cref="CalibrationUI.Microphone"/>,
        /// where <c>Cue_Microphone_ON</c> / <c>Cue_Microphone_OFF</c> from <see cref="CalibrationMenu"/>'s music-sync callback drive it instead.
        /// Single source of truth so step-list reordering can't accidentally leave the mic open in non-mic steps.
        /// </summary>
        private void EnforceGameOnForStep(CalibrationUI step)
        {
            if (_sequencer == null || _sequencer.imitoneVoiceInterpreter == null)
                return;
            if (step == CalibrationUI.Microphone)
                return;
            _sequencer.imitoneVoiceInterpreter.SetGameOn(false);
        }

        /// <summary>Calibration mirrors tutorial: monitor unattenuated (louder), bypass chant-driven ducking (chantPresence + chargeDuck → 1f, mic-gate still live), and boost the normalization rise-rate inside the confident-tone window by <see cref="CalibrationNormalizationRiseRateMultiplier"/>×. All released in <see cref="LocalCleanup"/>.</summary>
        private void ApplyCalibrationMonitoringBoost()
        {
            // Direct-voice monitoring: force unattenuated (louder), same shape tutorial uses; also bypass chant-driven ducking.
            var directVoiceMonitoring = Object.FindObjectOfType<DirectVoiceMonitoring>();
            if (directVoiceMonitoring != null)
            {
                directVoiceMonitoring.AttenuateMonitoring(false);
                directVoiceMonitoring.SetChantBasedAttenuationOverride(true);
            }
            if (MusicSystem1.instance != null)
            {
                MusicSystem1.instance.NotifyMonitoringAttenuationChangedExternally(false);
                if (!_hasClaimedMonitoringOverride)
                {
                    MusicSystem1.instance.SetCalibrationMonitoringOverride(true);
                    _hasClaimedMonitoringOverride = true;
                }
            }

            // Normalization gain-riding rise-rate: 6× while calibration is active. Cache once so re-entry doesn't stomp our own boosted value.
            if (!_hasCapturedNormalizationRiseRate && _sequencer != null && _sequencer.imitoneVoiceInterpreter != null)
            {
                _capturedNormalizationRiseRateDbPerSecond = _sequencer.imitoneVoiceInterpreter.GetNormalizationGainRidingRaiseRateDbPerSecond();
                _sequencer.imitoneVoiceInterpreter.SetNormalizationGainRidingRaiseRateDbPerSecond(_capturedNormalizationRiseRateDbPerSecond * CalibrationNormalizationRiseRateMultiplier);
                _hasCapturedNormalizationRiseRate = true;
            }
        }

        private void ReleaseCalibrationMonitoringBoost()
        {
            if (_hasCapturedNormalizationRiseRate && _sequencer != null && _sequencer.imitoneVoiceInterpreter != null)
            {
                _sequencer.imitoneVoiceInterpreter.SetNormalizationGainRidingRaiseRateDbPerSecond(_capturedNormalizationRiseRateDbPerSecond);
            }
            _hasCapturedNormalizationRiseRate = false;

            var directVoiceMonitoring = Object.FindObjectOfType<DirectVoiceMonitoring>();
            if (directVoiceMonitoring != null)
            {
                directVoiceMonitoring.SetChantBasedAttenuationOverride(false);
            }

            if (_hasClaimedMonitoringOverride && MusicSystem1.instance != null)
            {
                MusicSystem1.instance.SetCalibrationMonitoringOverride(false);
            }
            _hasClaimedMonitoringOverride = false;
        }

        /// <summary>UI step → Wwise <c>Calibration_Sequence</c> switch state. Reuses legacy portion names so existing Wwise containers continue to match.</summary>
        private static string MapStepToPortion(CalibrationUI step)
        {
            switch (step)
            {
                case CalibrationUI.Start:         return "Intro";
                case CalibrationUI.Headphone:     return "Volume";
                case CalibrationUI.Microphone:    return "Mic";
                case CalibrationUI.VibroAcoustic: return "Vibration";
                case CalibrationUI.LightGlasses:  return "Lights";
                case CalibrationUI.Conclusion:    return "End";
                default:
                    Debug.LogWarning("CalibrationStageHandler: MapStepToPortion — unknown step " + step + "; defaulting to Intro.");
                    return "Intro";
            }
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
            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.SetCalibrationPortionSwitch(MapStepToPortion(_steps[_stepIndex]));
            EnforceGameOnForStep(_steps[_stepIndex]);
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
            // Stop + SetSwitch + Play (same frame, same GameObject) — Back does not wait for any cue.
            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.RestartFromPortion(_sequencer, MapStepToPortion(_steps[_stepIndex]));
            EnforceGameOnForStep(_steps[_stepIndex]);
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

            ReleaseCalibrationMonitoringBoost();

            // Defensive: leave gameOn off for the next stage (Opening etc.). Next stage can override on its Enter.
            if (_sequencer != null && _sequencer.imitoneVoiceInterpreter != null)
                _sequencer.imitoneVoiceInterpreter.SetGameOn(false);
        }

        public void Exit()
        {
            LocalCleanup();
        }
    }
}
