using System.Collections;
using UnityEngine;

namespace SoundSelf.Sequence
{
    /*
     * Polite Next Step — dual mirror (intentionally redundant, a bit inelegant):
     * Wwise interactive music advances audio at safe points; Unity mirrors on buttons (loading until unlock).
     * Unlock for pending Next: primarily <c>Cue_Calibration_Instruction_OFF</c> and the <b>next segment's first</b>
     * <c>Cue_Calibration_Instruction_ON</c> (Wwise often does not deliver <c>Cue_Calibration_Next</c> to Unity — see <c>CalibrationPoliteNext</c> case below).
     * See Docs/CALIBRATION_UI_SEQUENCING_PLAN.md.
     * Back is immediate interrupt on both sides (no polite wait).
     * Conclusion: <c>MarkComplete</c> after confirm + <c>Cue_Calibration_Instruction_OFF</c> (instruction line ended). <c>Cue_Calibration_Next</c> is ignored on the Conclusion step for completion gating.
     */
    /// <summary>
    /// Calibration UI: variant step list; Next may wait for Wwise polite cues; Conclusion waits for instruction VO off after confirm unless already between lines / dev skip.
    /// <para><b>Back:</b> calibration section Back buttons call <see cref="UIManager.BackStepButtonPress"/> → <see cref="OnCalibrationBackPress"/> (this handler). That path is separate from choice-menu navigation (<see cref="UIManager.ChoiceMenuBackButtonPress"/>, <see cref="ChoiceMenuScreen"/> stack on <see cref="UIManager"/>).</para>
    /// </summary>
    public class CalibrationStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _calibrationUiListenersRegistered;
        private StageVariant _activeCalibrationVariant = StageVariant.Calibration_Default;
        private CalibrationUI[] _steps = System.Array.Empty<CalibrationUI>();
        private int _stepIndex;

        /// <summary>True while Wwise reports instruction VO in the “on” region (between Instruction_ON and Instruction_OFF).</summary>
        private bool _instructionVoActive;
        private bool _pendingNextAfterAdvanceCue;
        private CalibrationUI _pendingNextFromStep;
        private int _pendingNextTargetStepIndex;
        private Coroutine _nextAdvanceCueTimeoutCoroutine;

        /// <summary>User pressed conclusion confirm while <see cref="_instructionVoActive"/>; wait for <c>Cue_Calibration_Instruction_OFF</c> before <c>MarkComplete</c>. <c>Cue_Calibration_Next</c> is not used on Conclusion.</summary>
        private bool _pendingConclusionConfirmWaitForInstructionOff;
        private Coroutine _conclusionInstructionOffTimeoutCoroutine;

        private bool _hasCapturedNormalizationRiseRate;
        private float _capturedNormalizationRiseRateDbPerSecond;
        private const float CalibrationNormalizationRiseRateMultiplier = 6f;
        private bool _hasClaimedMonitoringOverride;

        private const float CalibrationCueWaitTimeoutSeconds = 120f;

        public StageType StageType => StageType.Calibration;

        public bool IsComplete { get; private set; }

        public CalibrationStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.CalibrationMicrophoneOn
            || sequenceCommand == SequenceCommand.CalibrationMicrophoneOff
            || sequenceCommand == SequenceCommand.CalibrationAvsStart
            || sequenceCommand == SequenceCommand.CalibrationAvsEnd
            || sequenceCommand == SequenceCommand.CalibrationInstructionVoStarted
            || sequenceCommand == SequenceCommand.CalibrationInstructionVoEnded
            || sequenceCommand == SequenceCommand.CalibrationPoliteNext
            || sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            switch (sequenceCommand)
            {
                case SequenceCommand.EndThisSequenceStage:
                    SkipCalibrationFromSequenceCommand();
                    break;
                case SequenceCommand.CalibrationInstructionVoStarted:
                    // Wwise may not post Cue_Calibration_Next to Unity when the user already pressed Next (handled inside music graph).
                    // The first Instruction_ON of the *destination* portion is a practical unlock signal (hacky but matches shipped behavior).
                    if (_pendingNextAfterAdvanceCue)
                        CompletePendingNextStepFromWwiseMirror();
                    _instructionVoActive = true;
                    break;
                case SequenceCommand.CalibrationInstructionVoEnded:
                    _instructionVoActive = false;
                    if (TryCompleteConclusionAfterInstructionOff())
                        break;
                    var onStartScreen = OnCalibrationStartScreenStep();
                    if (_pendingNextAfterAdvanceCue)
                        CompletePendingNextStepFromWwiseMirror();
                    else if (onStartScreen && UIManager.Instance != null)
                        UIManager.Instance.NotifyCalibrationStartInstructionVoLineEnded();
                    break;
                case SequenceCommand.CalibrationPoliteNext:
                    // Conclusion: never use Next for exit gating.
                    if (_steps != null && _stepIndex >= 0 && _stepIndex < _steps.Length && _steps[_stepIndex] == CalibrationUI.Conclusion)
                        break;
                    /*
                     * Pending Next unlock via Cue_Calibration_Next — DISABLED (May 2026):
                     * In practice Wwise consumes Cue_Calibration_Next inside the interactive music transition; the callback
                     * often never reaches Unity, so the UI stayed stuck in "waiting". We unlock on Instruction_OFF and on the
                     * next portion's first Instruction_ON instead (see CalibrationInstructionVoStarted / VoEnded above).
                     * Keep mapping in CalibrationMenu for logging / future Wwise changes.
                     *
                    if (_pendingNextAfterAdvanceCue)
                        CompletePendingNextStepFromWwiseMirror();
                    */
                    break;
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
            ResetPoliteCueStateForNewRun();
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

        private void ResetPoliteCueStateForNewRun()
        {
            CancelNextAdvanceCueTimeoutCoroutine();
            CancelConclusionInstructionOffTimeoutCoroutine();
            _instructionVoActive = false;
            _pendingNextAfterAdvanceCue = false;
            _pendingConclusionConfirmWaitForInstructionOff = false;
            if (UIManager.Instance != null)
                UIManager.Instance.ClearAllCalibrationCueWaitVisuals();
        }

        private void EnforceGameOnForStep(CalibrationUI step)
        {
            if (_sequencer == null || _sequencer.imitoneVoiceInterpreter == null)
                return;
            if (step == CalibrationUI.Microphone)
                return;
            _sequencer.imitoneVoiceInterpreter.SetGameOn(false);
        }

        private void ApplyCalibrationMonitoringBoost()
        {
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

        private static string MapStepToPortion(CalibrationUI step)
        {
            switch (step)
            {
                case CalibrationUI.Start: return "Intro";
                case CalibrationUI.Headphone: return "Volume";
                case CalibrationUI.Microphone: return "Mic";
                case CalibrationUI.VibroAcoustic: return "Vibration";
                case CalibrationUI.LightGlasses: return "Lights";
                case CalibrationUI.Conclusion: return "End";
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

        private bool SkipCalibrationCueGating() =>
            _sequencer != null && _sequencer.CalibrationSkipCueGatingForDev;

        private bool OnCalibrationStartScreenStep() =>
            _steps != null && _stepIndex >= 0 && _stepIndex < _steps.Length && _steps[_stepIndex] == CalibrationUI.Start;

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

            if (SkipCalibrationCueGating())
            {
                AdvanceCalibrationStepImmediate();
                return;
            }

            if (_pendingNextAfterAdvanceCue)
                return;

            if (!_instructionVoActive)
            {
                AdvanceCalibrationStepImmediate();
                return;
            }

            _pendingNextAfterAdvanceCue = true;
            _pendingNextFromStep = _steps[_stepIndex];
            _pendingNextTargetStepIndex = _stepIndex + 1;
            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.SetCalibrationPortionSwitch(MapStepToPortion(_steps[_pendingNextTargetStepIndex]));
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationStepNextCuePendingVisual(_pendingNextFromStep, true);
            if (_sequencer != null)
                _nextAdvanceCueTimeoutCoroutine = _sequencer.StartCoroutine(NextAdvanceCueTimeoutRoutine());
        }

        private void AdvanceCalibrationStepImmediate()
        {
            if (_steps == null || _steps.Length == 0)
                return;
            if (_stepIndex < 0 || _stepIndex >= _steps.Length - 1)
                return;

            _stepIndex++;
            Debug.Log("CalibrationStageHandler: Next step → index " + _stepIndex + " screen " + _steps[_stepIndex]);
            UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.SetCalibrationPortionSwitch(MapStepToPortion(_steps[_stepIndex]));
            EnforceGameOnForStep(_steps[_stepIndex]);
        }

        private void CompletePendingNextStepFromWwiseMirror()
        {
            if (!_pendingNextAfterAdvanceCue)
                return;
            bool notifyStartPrimaryLabels = _pendingNextFromStep == CalibrationUI.Start;
            CancelNextAdvanceCueTimeoutCoroutine();
            _pendingNextAfterAdvanceCue = false;
            // Do not SetCalibrationStepNextCuePendingVisual(false) on the outgoing step: keeps loading through fade-out;
            // the incoming calibration root's OnEnable (CalibrationCueWaitBinding) resets to label.

            _stepIndex = _pendingNextTargetStepIndex;
            Debug.Log("CalibrationStageHandler: Pending Next unlocked (Wwise mirror) → index " + _stepIndex + " screen " + _steps[_stepIndex]);
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
            EnforceGameOnForStep(_steps[_stepIndex]);
            if (notifyStartPrimaryLabels && UIManager.Instance != null)
                UIManager.Instance.NotifyCalibrationStartInstructionVoLineEnded();
        }

        private IEnumerator NextAdvanceCueTimeoutRoutine()
        {
            yield return new WaitForSeconds(CalibrationCueWaitTimeoutSeconds);
            _nextAdvanceCueTimeoutCoroutine = null;
            if (!_pendingNextAfterAdvanceCue)
                yield break;
            Debug.LogError("CalibrationStageHandler: Timeout waiting for polite Next (Instruction_OFF or next portion Instruction_ON; Cue_Calibration_Next not relied on — see handler). Resyncing Wwise to current UI step.");
            _pendingNextAfterAdvanceCue = false;
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationStepNextCuePendingVisual(_pendingNextFromStep, false);
            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.RestartFromPortion(_sequencer, MapStepToPortion(_steps[_stepIndex]));
        }

        private void CancelNextAdvanceCueTimeoutCoroutine()
        {
            if (_nextAdvanceCueTimeoutCoroutine != null && _sequencer != null)
            {
                _sequencer.StopCoroutine(_nextAdvanceCueTimeoutCoroutine);
                _nextAdvanceCueTimeoutCoroutine = null;
            }
        }

        private void HandleCalibrationBackPress()
        {
            CancelNextAdvanceCueTimeoutCoroutine();
            var hadPendingNext = _pendingNextAfterAdvanceCue;
            if (_pendingNextAfterAdvanceCue)
            {
                _pendingNextAfterAdvanceCue = false;
                if (_sequencer != null && _sequencer.calibrationMenu != null)
                    _sequencer.calibrationMenu.RestartFromPortion(_sequencer, MapStepToPortion(_steps[_stepIndex]));
            }

            CancelConclusionInstructionOffTimeoutCoroutine();
            _pendingConclusionConfirmWaitForInstructionOff = false;
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationConclusionConfirmCuePendingVisual(false);

            if (_steps == null || _steps.Length == 0)
                return;
            if (_stepIndex <= 0)
            {
                if (hadPendingNext && UIManager.Instance != null)
                    UIManager.Instance.SetCalibrationStepNextCuePendingVisual(_steps[_stepIndex], false);
                Debug.Log("CalibrationStageHandler: Back ignored on Start.");
                return;
            }

            _stepIndex--;
            Debug.Log("CalibrationStageHandler: Back → index " + _stepIndex + " screen " + _steps[_stepIndex]);
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
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

            if (IsComplete)
                return;

            if (SkipCalibrationCueGating())
            {
                MarkComplete();
                return;
            }

            // Between instruction lines (or before first ON): no need to wait for OFF.
            if (!_instructionVoActive)
            {
                MarkComplete();
                return;
            }

            if (_pendingConclusionConfirmWaitForInstructionOff)
                return;

            _pendingConclusionConfirmWaitForInstructionOff = true;
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationConclusionConfirmCuePendingVisual(true);
            if (_sequencer != null)
                _conclusionInstructionOffTimeoutCoroutine = _sequencer.StartCoroutine(ConclusionInstructionOffWaitTimeoutRoutine());
        }

        /// <returns>True if conclusion was completed (caller should skip pending-Next unlock on same OFF).</returns>
        private bool TryCompleteConclusionAfterInstructionOff()
        {
            if (IsComplete || !_pendingConclusionConfirmWaitForInstructionOff)
                return false;
            if (_steps == null || _stepIndex < 0 || _stepIndex >= _steps.Length || _steps[_stepIndex] != CalibrationUI.Conclusion)
                return false;

            CancelConclusionInstructionOffTimeoutCoroutine();
            _pendingConclusionConfirmWaitForInstructionOff = false;
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationConclusionConfirmCuePendingVisual(false);
            MarkComplete();
            return true;
        }

        private IEnumerator ConclusionInstructionOffWaitTimeoutRoutine()
        {
            yield return new WaitForSeconds(CalibrationCueWaitTimeoutSeconds);
            _conclusionInstructionOffTimeoutCoroutine = null;
            if (!_pendingConclusionConfirmWaitForInstructionOff)
                yield break;
            Debug.LogError("CalibrationStageHandler: Timeout waiting for Cue_Calibration_Instruction_OFF after conclusion confirm. Clearing wait; user must press confirm again.");
            _pendingConclusionConfirmWaitForInstructionOff = false;
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationConclusionConfirmCuePendingVisual(false);
        }

        private void CancelConclusionInstructionOffTimeoutCoroutine()
        {
            if (_conclusionInstructionOffTimeoutCoroutine != null && _sequencer != null)
            {
                _sequencer.StopCoroutine(_conclusionInstructionOffTimeoutCoroutine);
                _conclusionInstructionOffTimeoutCoroutine = null;
            }
        }

        /// <summary>
        /// UI / debug skip (e.g. invisible button → <see cref="UIManager.EndThisSequenceStageButtonPress"/>).
        /// Finishes the calibration stage immediately; full teardown still runs in <see cref="BeginTransitionOut"/> / <see cref="Exit"/>.
        /// </summary>
        private void SkipCalibrationFromSequenceCommand()
        {
            if (IsComplete)
            {
                Debug.Log("CalibrationStageHandler: SkipCalibration (EndThisSequenceStage) ignored — already IsComplete.");
                return;
            }
            Debug.Log("CalibrationStageHandler: SkipCalibration (EndThisSequenceStage) — clearing waits and MarkComplete.");
            CancelNextAdvanceCueTimeoutCoroutine();
            CancelConclusionInstructionOffTimeoutCoroutine();
            _pendingNextAfterAdvanceCue = false;
            _pendingConclusionConfirmWaitForInstructionOff = false;
            _instructionVoActive = false;
            if (UIManager.Instance != null)
                UIManager.Instance.ClearAllCalibrationCueWaitVisuals();
            MarkComplete();
            Debug.Log("CalibrationStageHandler: SkipCalibration finished; IsComplete=" + IsComplete + " (runner should TryAdvance same frame).");
        }

        private void MarkComplete()
        {
            if (IsComplete)
                return;
            IsComplete = true;
            Debug.Log("CalibrationStageHandler: MarkComplete — calibration stage finished (conclusion confirm; Instruction_OFF when VO was active, or immediate when between lines / dev bypass).");
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
            CancelNextAdvanceCueTimeoutCoroutine();
            CancelConclusionInstructionOffTimeoutCoroutine();
            _pendingNextAfterAdvanceCue = false;
            _pendingConclusionConfirmWaitForInstructionOff = false;
            if (UIManager.Instance != null)
                UIManager.Instance.ClearAllCalibrationCueWaitVisuals();

            UnsubscribeCalibrationUiListeners();

            ReleaseCalibrationMonitoringBoost();

            if (_sequencer != null && _sequencer.imitoneVoiceInterpreter != null)
                _sequencer.imitoneVoiceInterpreter.SetGameOn(false);
        }

        public void Exit()
        {
            LocalCleanup();
            if (_sequencer != null)
                _sequencer.StopCalibrationInteractiveMusicFromStageEnter();
        }

        /// <inheritdoc />
        public void OnSessionSkipFromUi() => LocalCleanup();
    }
}
