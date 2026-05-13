using UnityEngine;

/// <summary>
/// Section Calibration Start — Next button: show <c>pleaseWaitText</c> until the first Wwise <c>Cue_Calibration_Instruction_OFF</c> on this step
/// (via <see cref="UIManager.OnCalibrationStartInstructionVoLineEnded"/>), then show <c>primaryActionText</c>. Does not block the button. Resets when re-enabled.
/// </summary>
public class CalibrationStartNextButtonLabels : MonoBehaviour
{
    [SerializeField] private GameObject pleaseWaitText;
    [SerializeField] private GameObject primaryActionText;

    private bool _subscribed;
    private bool _switchedAfterFirstInstructionOff;

    private void OnEnable()
    {
        _switchedAfterFirstInstructionOff = false;
        ApplyInitialLabelState();
        SubscribeIfNeeded();
    }

    private void Start()
    {
        SubscribeIfNeeded();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void SubscribeIfNeeded()
    {
        if (_subscribed)
            return;
        if (UIManager.Instance == null)
            return;
        UIManager.Instance.OnCalibrationStartInstructionVoLineEnded += OnCalibrationStartInstructionVoLineEnded;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;
        if (UIManager.Instance != null)
            UIManager.Instance.OnCalibrationStartInstructionVoLineEnded -= OnCalibrationStartInstructionVoLineEnded;
        _subscribed = false;
    }

    private void ApplyInitialLabelState()
    {
        if (pleaseWaitText != null)
            pleaseWaitText.SetActive(true);
        if (primaryActionText != null)
            primaryActionText.SetActive(false);
    }

    private void OnCalibrationStartInstructionVoLineEnded()
    {
        if (_switchedAfterFirstInstructionOff)
            return;
        _switchedAfterFirstInstructionOff = true;
        if (pleaseWaitText != null)
            pleaseWaitText.SetActive(false);
        if (primaryActionText != null)
            primaryActionText.SetActive(true);
    }
}
