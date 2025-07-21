using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartButtonScript : MonoBehaviour
{
    public ExperienceDurationDatabase experienceDurationDatabase;
    public Button startButton;
    public Button startConfigButton;
    public Button endTutorialButton;

    public Button nextButton;
    public Sequencer sequencer;

    private int currentTutorialPortionIndex = 0;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;  
    public canvasSwitcher canvasManager;
    // Start is called before the first frame update
    void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        startConfigButton.onClick.AddListener(OnStartConfigButtonClicked);
        endTutorialButton.onClick.AddListener(OnEndTutorialButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
        endTutorialButton.gameObject.SetActive(false); // Hide the end tutorial button initially
        nextButton.gameObject.SetActive(false); // Hide the next button initially
    }

    public void OnStartButtonClicked()
    {
        endTutorialButton.gameObject.SetActive(false); // Hide the end tutorial button when the game starts
        startConfigButton.gameObject.SetActive(false); // Show the start config button when the game starts

        sequencer.PlayFirstSequence(); // Start the first sequence in the sequencer
        TimeLeftScript timeLeftScript = FindObjectOfType<TimeLeftScript>();
        if (timeLeftScript != null && experienceDurationDatabase != null)
        {
            float duration = experienceDurationDatabase.GetDurationForMode(CSVLoader.gameMode);
            timeLeftScript.SetTimeLeftSeconds(duration);
        }

        currentTutorialPortionIndex = 0; // Reset tutorial portion
        SetTutorialSwitch();

        startButton.gameObject.SetActive(false); // Hide the start button when the game starts
    }
    public void OnStartConfigButtonClicked()
    {
        Debug.Log("Start Config button clicked");
        AkSoundEngine.SetSwitch("Calibration_Sequence", "Intro", gameObject);
        AkSoundEngine.PostEvent("Play_Calibration_Sequence", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, CalibrationCallBackFunction, null);
        endTutorialButton.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(true); // Show the next button when the start config button is clicked
        startButton.gameObject.SetActive(false); // Hide the start button when the start config button is clicked
    }

    public void OnEndTutorialButtonClicked()
    {
        Debug.Log("End Tutorial button clicked");
        AkSoundEngine.PostEvent("Stop_Calibration_Sequence", gameObject);
        canvasManager.SwitchToMainCanvas();
        startConfigButton.gameObject.SetActive(true); // Show the start config button when the tutorial ends
        startButton.gameObject.SetActive(true); // Show the start button when the tutorial ends
        endTutorialButton.gameObject.SetActive(false); // Hide the end tutorial button when the tutorial ends
    }

    public void OnNextButtonClicked()
    {
        currentTutorialPortionIndex++;

        // Prevent going out of bounds
        if (currentTutorialPortionIndex >= System.Enum.GetNames(typeof(TutorialPortions)).Length)
        {
            Debug.Log("Reached the end of tutorial portions.");
            return;
        }

        SetTutorialSwitch();
    }

    public void SetTutorialSwitch()
    {
        TutorialPortions portion = (TutorialPortions)currentTutorialPortionIndex;
        string portionName = portion.ToString();
        Debug.Log($"Setting Wwise switch to: {portionName}");
        AkSoundEngine.SetSwitch("Calibration_Sequence", portionName, gameObject);

        // If we're at the 'End' portion, hide the Next button
        if (portion == TutorialPortions.End)
        {
            nextButton.gameObject.SetActive(false);
        }
    }

    public void CalibrationCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            Debug.Log("WWise_VO_CUE: Callback triggered: " + in_type);
            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
            if (musicSyncInfo.userCueName == "Cue_Microphone_ON")
            {
                Debug.Log("WWise_VO_CUE: Cue Mic On");
                imitoneVoiceIntepreter.SetGameOn(true);
            }
            else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
            {
                Debug.Log("WWise_VO_CUE: Cue Mic OFF");
                imitoneVoiceIntepreter.SetGameOn(false);
            }
        }
    }
}



public enum TutorialPortions
    {
        Intro,
        Volume,
        Mic,
        Vibration,
        Lights,
        End,
        TechnicalIssues
    }
