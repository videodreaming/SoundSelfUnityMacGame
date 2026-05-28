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
     *
     * Start (orientation) step is special: it always auto-advances when Wwise fires <c>Cue_Calibration_Intro_End</c>
     * (regardless of whether the user pressed Next). A Next press on Start hides the session skip control and
     * shows the loading spinner; the actual advance (and the audio switch to the next portion) happens on the cue. See
     * <c>CalibrationIntroEnded</c> case below and <c>HandleCalibrationNextStepPress</c>.
     */
    /// <summary>
    /// Calibration UI: variant step list; Next may wait for Wwise polite cues; Conclusion waits for instruction VO off after confirm unless already between lines / dev skip.
    /// <para><b>Back:</b> calibration section Back buttons call <see cref="UIManager.BackStepButtonPress"/> → <see cref="OnCalibrationBackPress"/> (this handler). That path is separate from choice-menu navigation (<see cref="UIManager.ChoiceMenuBackButtonPress"/>, <see cref="ChoiceMenuScreen"/> stack on <see cref="UIManager"/>).</para>
    /// </summary>
    public class CalibrationStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _calibrationUiListenersRegistered;
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
        private const string CalibrationMicMixerVolumeContributionName = "Calibration";
        private const float CalibrationMicMixerVolumeContributionDb = -6f;
        private bool _hasClaimedMonitoringOverride;

        private const float CalibrationCueWaitTimeoutSeconds = 120f;

        public StageType StageType => StageType.Calibration;

        public bool IsComplete { get; private set; }

        public CalibrationStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.CalibrationAvsStart
            //|| sequenceCommand == SequenceCommand.CalibrationMicrophoneOn
            //|| sequenceCommand == SequenceCommand.CalibrationMicrophoneOff
            || sequenceCommand == SequenceCommand.CalibrationAvsEnd
            || sequenceCommand == SequenceCommand.CalibrationInstructionVoStarted
            || sequenceCommand == SequenceCommand.CalibrationInstructionVoEnded
            || sequenceCommand == SequenceCommand.CalibrationPoliteNext
            || sequenceCommand == SequenceCommand.CalibrationIntroEnded
            || sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            switch (sequenceCommand)
            {
                case SequenceCommand.EndThisSequenceStage:
                    SkipCalibrationFromSequenceCommand();
                    break;
                case SequenceCommand.CalibrationInstructionVoStarted:
                    if (_pendingNextAfterAdvanceCue)
                        CompletePendingNextStepFromWwiseMirror();
                    _instructionVoActive = true;
                    break;
                case SequenceCommand.CalibrationInstructionVoEnded:
                    _instructionVoActive = false;
                    if (TryCompleteConclusionAfterInstructionOff())
                        break;
                    if (_pendingNextAfterAdvanceCue)
                        CompletePendingNextStepFromWwiseMirror();
                    break;
                case SequenceCommand.CalibrationIntroEnded:
                    // Start (orientation) always auto-advances on this cue, whether or not the user pressed Next.
                    // The Next button on Start only shows the loading spinner — actual advance happens here.
                    if (IsComplete)
                        break;
                    if (_steps != null && _stepIndex >= 0 && _stepIndex < _steps.Length && _steps[_stepIndex] == CalibrationUI.Start)
                    {
                        Debug.Log("CalibrationStageHandler: Cue_Calibration_Intro_End → auto-advance from Start.");
                        AdvanceCalibrationStepImmediate();
                    }
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
                /* We are handling this by stages instead.
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
                */
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
            var calibrationVariant = variant == StageVariant.Calibration_Album
                || variant == StageVariant.Calibration_NoVibro
                || variant == StageVariant.Calibration_Default
                ? variant
                : StageVariant.Calibration_Default;
            LogCalibrationVariantStub(calibrationVariant);

            _steps = BuildStepList(calibrationVariant);
            _stepIndex = 0;

            Debug.Log("CalibrationStageHandler: Enter — step count " + _steps.Length + ", first screen " + _steps[0]);
            UIManager.Instance?.EnableSkipStageButton(false);
            ApplyCalibrationMonitoringBoost();
            if (_sequencer != null && _sequencer.calibrationMenu != null)
            {
                _sequencer.calibrationMenu.SetCalibrationPortionSwitch(MapStepToPortion(_steps[_stepIndex]));
                _sequencer.calibrationMenu.StartCalibrationSequence(_sequencer);
                UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
                RefreshCalibrationProgressUi();
                SubscribeCalibrationUiListeners();
                EnforceGameOnForStep(_steps[_stepIndex]);
            }
            else
                Debug.LogError("CalibrationStageHandler: Sequencer or calibrationMenu is null. Cannot start calibration UI.");
        }

        private void RefreshCalibrationProgressUi()
        {
            if (UIManager.Instance == null || _steps == null || _steps.Length == 0)
                return;
            if (_stepIndex < 0 || _stepIndex >= _steps.Length)
                return;
            UIManager.Instance.SetCalibrationProgress(_steps.Length, _stepIndex);
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

        private static bool CalibrationStepUsesGameOn(CalibrationUI step) =>
            step == CalibrationUI.Microphone || step == CalibrationUI.VibroAcoustic;

        /// <summary><c>gameOn</c> only on Microphone and Vibroacoustic steps; driven by step transitions, not Wwise mic cues.</summary>
        private void EnforceGameOnForStep(CalibrationUI step)
        {
            if (_sequencer == null || _sequencer.imitoneVoiceInterpreter == null)
                return;

            _sequencer.imitoneVoiceInterpreter.SetGameOn(CalibrationStepUsesGameOn(step));
        }

        private void ApplyCalibrationMonitoringBoost()
        {
            var directVoiceMonitoring = Object.FindObjectOfType<DirectVoiceMonitoring>();
            if (directVoiceMonitoring != null)
            {
                directVoiceMonitoring.AttenuateMonitoring(false);
                directVoiceMonitoring.SetChantBasedAttenuationOverride(true);
                directVoiceMonitoring.SetMicMixerVolumeContributionDb(
                    CalibrationMicMixerVolumeContributionName,
                    CalibrationMicMixerVolumeContributionDb);
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
                directVoiceMonitoring.RemoveMicMixerVolumeContribution(CalibrationMicMixerVolumeContributionName);
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

        /// <summary>Polite-next gating for non-Start steps. Start uses its own cue-driven auto-advance (see <see cref="HandleCalibrationNextStepPress"/>).</summary>
        private bool ShouldWaitForPoliteNextBeforeAdvancing()
        {
            if (_steps == null || _stepIndex < 0 || _stepIndex >= _steps.Length)
                return false;
            return _instructionVoActive;
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

            if (SkipCalibrationCueGating())
            {
                AdvanceCalibrationStepImmediate();
                return;
            }

            // Start: visual-only press. Audio + UI advance happen on Cue_Calibration_Intro_End (CalibrationIntroEnded).
            if (_steps[_stepIndex] == CalibrationUI.Start)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.EnableSkipButton(false);
                    UIManager.Instance.SetCalibrationStepNextCuePendingVisual(CalibrationUI.Start, true);
                }
                Debug.Log("CalibrationStageHandler: Start Next pressed; skip hidden and loading spinner shown. Auto-advance happens on Cue_Calibration_Intro_End.");
                return;
            }

            if (_pendingNextAfterAdvanceCue)
                return;

            if (!ShouldWaitForPoliteNextBeforeAdvancing())
            {
                AdvanceCalibrationStepImmediate();
                return;
            }

            _pendingNextAfterAdvanceCue = true;
            _pendingNextFromStep = _steps[_stepIndex];
            _pendingNextTargetStepIndex = _stepIndex + 1;
            var pendingTarget = _steps[_pendingNextTargetStepIndex];
            EnforceGameOnForStep(pendingTarget);
            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.SetCalibrationPortionSwitch(MapStepToPortion(pendingTarget));
            if (UIManager.Instance != null)
                UIManager.Instance.SetCalibrationStepNextCuePendingVisual(_pendingNextFromStep, true);
            if (_sequencer != null)
                _nextAdvanceCueTimeoutCoroutine = _sequencer.StartCoroutine(NextAdvanceCueTimeoutRoutine());
        }

        private void AdvanceCalibrationStepImmediate()
        {
            if (IsComplete)
                return;
            if (_steps == null || _steps.Length == 0)
                return;
            if (_stepIndex < 0 || _stepIndex >= _steps.Length - 1)
                return;

            _stepIndex++;
            Debug.Log("CalibrationStageHandler: Next step → index " + _stepIndex + " screen " + _steps[_stepIndex]);
            UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
            RefreshCalibrationProgressUi();
            EnforceGameOnForStep(_steps[_stepIndex]);
            if (_sequencer != null && _sequencer.calibrationMenu != null)
                _sequencer.calibrationMenu.SetCalibrationPortionSwitch(MapStepToPortion(_steps[_stepIndex]));
        }

        private void CompletePendingNextStepFromWwiseMirror()
        {
            if (!_pendingNextAfterAdvanceCue)
                return;
            CancelNextAdvanceCueTimeoutCoroutine();
            _pendingNextAfterAdvanceCue = false;
            // Do not SetCalibrationStepNextCuePendingVisual(false) on the outgoing step: keeps loading through fade-out;
            // the incoming calibration root's OnEnable (CalibrationCueWaitBinding) resets to label.

            _stepIndex = _pendingNextTargetStepIndex;
            Debug.Log("CalibrationStageHandler: Pending Next unlocked (Wwise mirror) → index " + _stepIndex + " screen " + _steps[_stepIndex]);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex]);
                RefreshCalibrationProgressUi();
            }
            EnforceGameOnForStep(_steps[_stepIndex]);
        }

        private IEnumerator NextAdvanceCueTimeoutRoutine()
        {
            yield return new WaitForSeconds(CalibrationCueWaitTimeoutSeconds);
            _nextAdvanceCueTimeoutCoroutine = null;
            if (!_pendingNextAfterAdvanceCue)
                yield break;
            Debug.LogError("CalibrationStageHandler: Timeout waiting for polite Next cue (Instruction_OFF or next portion Instruction_ON). Resyncing Wwise to current UI step.");
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
            {
                UIManager.Instance.SetCalibrationScreen(_steps[_stepIndex], reverse: true);
                RefreshCalibrationProgressUi();
            }
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
            if (UIManager.Instance != null)
                UIManager.Instance.ClearCalibrationProgress();
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
            {
                UIManager.Instance.ClearAllCalibrationCueWaitVisuals();
                UIManager.Instance.ClearCalibrationProgress();
            }

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
