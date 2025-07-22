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

    public string portionName;

    public VerticalLayoutGroupController verticalLayoutGroupController;
    public List<GameObject> calibrationTexts;
    public List<GameObject> calibrationMarks;
    public List<GameObject> calibrationDiagrams;
    private int calibrationTextIndex = 0;
    private bool isCalibrationStarted = false;

    public Sprite onMarkImage;
    public Sprite offMarkImage;

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
        if (!isCalibrationStarted)
        {
            isCalibrationStarted = true; // Set the flag to true to prevent multiple clicks
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
            canvasManager.SwitchToMainCanvas(); // Switch to the calibration canvas
        }

    }
    public void OnStartConfigButtonClicked()
    {
        canvasManager.SwitchToCalibrationCanvas();
        verticalLayoutGroupController.highLightText(calibrationTexts[calibrationTextIndex]); // Highlight the first calibration text
        StartCoroutine(verticalLayoutGroupController.scaleText(calibrationTexts[calibrationTextIndex], 1.1f));
        highlightMark(calibrationMarks[calibrationTextIndex]); // Highlight the first calibration mark
        AkSoundEngine.SetSwitch("Calibration_Sequence", "Intro", gameObject);
        AkSoundEngine.PostEvent("Play_Calibration_Sequence", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, CalibrationCallBackFunction, null);
        endTutorialButton.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(true); // Show the next button when the start config button is clicked
        startButton.gameObject.SetActive(false); // Hide the start button when the start config button is clicked
        startConfigButton.gameObject.SetActive(false); // Hide the start config button when the start config button is clicked
    }

    public void highlightMark(GameObject mark)
    {
        Image markImage = mark.GetComponent<Image>();
        markImage.sprite = onMarkImage; // Change the sprite to the highlighted version
    }

    public void unhighlightMark(GameObject mark)
    {
        Image markImage = mark.GetComponent<Image>();
        markImage.sprite = offMarkImage; // Change the sprite to the unhighlighted version
    }

    public void OnEndTutorialButtonClicked()
    {
        Debug.Log("End Tutorial button clicked");
        AkSoundEngine.PostEvent("Stop_Calibration_Sequence", gameObject);
        startConfigButton.gameObject.SetActive(true); // Show the start config button when the tutorial ends
        startButton.gameObject.SetActive(true); // Show the start button when the tutorial ends
        endTutorialButton.gameObject.SetActive(false); // Hide the end tutorial button when the tutorial ends
        canvasManager.calibrationCanvas.enabled= false; // Switch back to the main canvas
        isCalibrationStarted = false; // Reset the flag to allow starting the calibration again
    }

    public void OnNextButtonClicked()
    {
        calibrationTextIndex++;
        if (calibrationTextIndex < calibrationTexts.Count)
        {
            StartCoroutine(verticalLayoutGroupController.scaleText(calibrationTexts[calibrationTextIndex], 1.1f));
            StartCoroutine(verticalLayoutGroupController.unScaleText(calibrationTexts[calibrationTextIndex - 1], 1.1f));
            highlightMark(calibrationMarks[calibrationTextIndex]); // Highlight the next calibration mark
            unhighlightMark(calibrationMarks[calibrationTextIndex - 1]); // Unhighlight the previous calibration mark
            verticalLayoutGroupController.unhighLightText(calibrationTexts[calibrationTextIndex - 1]); // Unhighlight the previous calibration text
            verticalLayoutGroupController.highLightText(calibrationTexts[calibrationTextIndex]); // Highlight the next calibration text

            if (calibrationTextIndex == 1)
            {
                //Calibration going into Vol
                calibrationDiagrams[1].SetActive(true); // Show the second calibration diagram
            }
            else if (calibrationTextIndex == 2)
            {
                //Calibration going into Mic
                calibrationDiagrams[1].SetActive(false); // Hide the second calibration diagram
                calibrationDiagrams[2].SetActive(true); // Show the third calibration diagram
            }
            else if (calibrationTextIndex == 3)
            {
                //Calibration going into Vibration
                calibrationDiagrams[2].SetActive(false); // Hide the third calibration diagram
                calibrationDiagrams[3].SetActive(true); // Show the fourth calibration diagram
            }
            else if (calibrationTextIndex == 4)
            {
                //Calibration going into Lights
                calibrationDiagrams[3].SetActive(false); // Hide the fourth calibration diagram
                calibrationDiagrams[4].SetActive(true); // Show the fifth calibration diagram
            }
            else if (calibrationTextIndex == 5)
            {
                // Calibration going into End
                calibrationDiagrams[4].SetActive(false); // Hide the fifth calibration diagram
            }
        }
        else
        {
            Debug.Log("No more calibration texts to highlight.");
            return; // Exit if there are no more texts to highlight
        }
        currentTutorialPortionIndex++;
        verticalLayoutGroupController.highLightText(nextButton.gameObject); // Highlight the next button text

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
        portionName = portion.ToString();
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
