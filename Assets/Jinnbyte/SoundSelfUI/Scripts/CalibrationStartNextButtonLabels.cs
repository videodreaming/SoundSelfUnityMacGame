using UnityEngine;

/// <summary>
/// Section Calibration Start (orientation) — the Next button text always reads “Please Wait”;
/// the step auto-advances on <c>Cue_Calibration_Intro_End</c>, so there is no primary call to
/// action to swap to. Loading-spinner feedback on Next press is driven by
/// <see cref="CalibrationCueWaitBinding"/>; configure its <c>labelWhileWaiting</c> on this screen
/// to be null (or point to the primary-action GameObject we hide here) so the spinner does not
/// hide “Please Wait” on press.
/// </summary>
public class CalibrationStartNextButtonLabels : MonoBehaviour
{
    [SerializeField] private GameObject pleaseWaitText;
    [SerializeField] private GameObject primaryActionText;

    private void OnEnable()
    {
        if (pleaseWaitText != null)
            pleaseWaitText.SetActive(true);
        if (primaryActionText != null)
            primaryActionText.SetActive(false);
    }
}
