using UnityEngine;
using UnityEngine.Events;

/// <summary>Shared calibration section UI (headphone, mic, etc.). UnityEvents must use this type name — scenes were migrated off the legacy <c>MicrophoneCalibrationScreen</c> string.</summary>
public class CalibrationScreen : MonoBehaviour
{
    public GameObject troubleshootButton;
    public GameObject middleScreen;
    public GameObject lastScreen;

    public UnityEvent OnTroubleshootButtonPress;
    public UnityEvent OnNextStepButtonPress;
    public UnityEvent OnBackScreenButtonPress;

    public void TroubleShootButtonPress()
    {
        OnTroubleshootButtonPress?.Invoke();
        // Implement troubleshooting logic here
        middleScreen.SetActive(true);
        lastScreen.SetActive(false);
        troubleshootButton.SetActive(false);

    }

    public void NextStepButtonPress()
    {
        OnNextStepButtonPress?.Invoke();
        // Implement logic to transition to the next screen here
        middleScreen.SetActive(false);
        lastScreen.SetActive(true);

    }

    public void BackScreenButtonPress()
    {
        OnBackScreenButtonPress?.Invoke();
        middleScreen.SetActive(true);
        lastScreen.SetActive(false);
        //troubleshootButton.SetActive(true);
    }
}
