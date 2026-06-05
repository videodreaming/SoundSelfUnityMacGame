using UnityEngine;
using UnityEngine.Events;
/// <summary>Shared calibration section UI (headphone, mic, etc.). UnityEvents must use this type name — scenes were migrated off the legacy <c>MicrophoneCalibrationScreen</c> string.</summary>
public class CalibrationScreen : MonoBehaviour
{
    public GameObject troubleshootButton;
    public GameObject voiceMeterActivationPopup;
    public GameObject noVibrationPopup;
    public GameObject stillNoVibrationPopup;

    [HideInInspector]
    public UnityEvent OnTroubleshootButtonPress;

    void OnEnable()
    {
        ResetCalibrationScreen();
    }

    public void TroubleShootButtonPress()
    {
        OnTroubleshootButtonPress?.Invoke();
        // Implement troubleshooting logic here
        voiceMeterActivationPopup.SetActive(true);
        noVibrationPopup.SetActive(false);
        stillNoVibrationPopup.SetActive(false);
        troubleshootButton.SetActive(false);

    }


    public void ShowVoiceMeterActivationPopup()
    {
        voiceMeterActivationPopup.SetActive(true);
        noVibrationPopup.SetActive(false);
        stillNoVibrationPopup.SetActive(false);

    }
    public void ShowNoVibrationPopup()
    {
        voiceMeterActivationPopup.SetActive(false);
        noVibrationPopup.SetActive(true);
        stillNoVibrationPopup.SetActive(false);
    }
    public void ShowStillNoVibrationPopup()
    {
        voiceMeterActivationPopup.SetActive(false);
        noVibrationPopup.SetActive(false);
        stillNoVibrationPopup.SetActive(true);
    }
    public void ResetCalibrationScreen()
    {
        voiceMeterActivationPopup.SetActive(false);
        noVibrationPopup.SetActive(false);
        stillNoVibrationPopup.SetActive(false);
        troubleshootButton.SetActive(true);
    }
}
