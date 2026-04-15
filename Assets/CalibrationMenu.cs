using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalibrationMenu : MonoBehaviour
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
    public LightControl lightControl;

    public string portionName;

    public VerticalLayoutGroupController verticalLayoutGroupController;
    public List<GameObject> calibrationTexts;
    public List<GameObject> calibrationMarks;
    public List<GameObject> calibrationDiagrams;
    private int calibrationTextIndex = 0;
    private bool isCalibrationStarted = false;
    public bool startedExperience = false;

    public Sprite onMarkImage;
    public Sprite offMarkImage;

    public GameObject mainText;


    // Start is called before the first frame update
    public void StartCalibrationSequence()
    {
        //THESE PARTS ARE LEFT OVER FROM WHEN THE CALIBRATION STARTED ON LAUNCH. WE SHOULD REMOVE THEM SOON.
        startButton.onClick.AddListener(OnStartButtonClicked);
        startConfigButton.onClick.AddListener(OnStartConfigButtonClicked);
        endTutorialButton.onClick.AddListener(OnEndTutorialButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
        endTutorialButton.gameObject.SetActive(false); // Hide the end tutorial button initially
        nextButton.gameObject.SetActive(false); // Hide the next button initially
        
        //THIS ONE WE SHOULD KEEP.
        AkSoundEngine.PostEvent("Play_Calibration_Sequence", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, CalibrationCallBackFunction, null);
    }
    public void OnStartButtonClicked()
    {
        if(!startedExperience)
        {
            startedExperience = true; // Set the flag to true to prevent multiple clicks
            
            // Null checks to prevent crashes
            if (mainText != null)
            {
                mainText.SetActive(false); // Hide the main text when the calibration starts
            }
            else
            {
                Debug.LogWarning("CalibrationMenu: mainText is null!");
            }
            
            isCalibrationStarted = true; // Set the flag to true to prevent multiple clicks
            endTutorialButton.gameObject.SetActive(false); // Hide the end tutorial button when the game starts
            startConfigButton.gameObject.SetActive(false); // Show the start config button when the game starts
            AkSoundEngine.PostEvent("Stop_Calibration_Sequence", gameObject);

            if (sequencer != null)
            {
                sequencer.StartTrueStart(); // Start the first sequence in the sequencer
            }
            else
            {
                Debug.LogError("CalibrationMenu: sequencer is null! Cannot start sequence.");
                return; // Exit early if sequencer is null
            }
            TimeLeftScript timeLeftScript = FindObjectOfType<TimeLeftScript>();
            if (timeLeftScript != null && experienceDurationDatabase != null && CSVLoader.instance != null)
            {
                if (CSVLoader.instance.gameMode == CSVLoader.GameModeSkillsTraining)
                {
                    Debug.LogWarning("CalibrationMenu: Setting up for Skills Training (WARNING, THIS DOESN'T CURRENTLY DO ANYTHING)");
                    //timeLeftScript.SetTimeLeftSeconds(2400.0f); // 40 minutes
                }
                else if (CSVLoader.instance.gameMode == "Integration")
                {
                    Debug.LogWarning("CalibrationMenu: Setting up for Integration (WARNING, THIS DOESN'T CURRENTLY DO ANYTHING)");
                    //timeLeftScript.SetTimeLeftSeconds(1200.0f); // 20 minutes
                }
            }
            else if (CSVLoader.instance == null)
            {
                Debug.LogError("CalibrationMenu: CSVLoader.instance is null! Cannot determine game mode.");
            }

            currentTutorialPortionIndex = 0; // Reset tutorial portion
            SetTutorialSwitch();
            startButton.gameObject.SetActive(false); // Hide the start button when the game starts
            
            if (canvasManager != null)
            {
                canvasManager.SwitchToMainCanvas(); // Switch to canvas4 (new main canvas)
            }
            else
            {
                Debug.LogError("CalibrationMenu: canvasManager is null! Cannot switch canvas.");
            }
        }

    }
    public void OnStartConfigButtonClicked()
    {
        if (!isCalibrationStarted)
        {
            calibrationTextIndex = 0;
            currentTutorialPortionIndex = 0; // Reset tutorial portion index
            canvasManager.SwitchToCalibrationCanvas();
            mainText.SetActive(false); // Hide the main text when the calibration starts
            verticalLayoutGroupController.highLightText(calibrationTexts[calibrationTextIndex]); // Highlight the first calibration text
            StartCoroutine(verticalLayoutGroupController.scaleText(calibrationTexts[calibrationTextIndex], 1.1f));
            highlightMark(calibrationMarks[calibrationTextIndex]); // Highlight the first calibration mark
            AkSoundEngine.SetSwitch("Calibration_Sequence", "Intro", gameObject);
            
            endTutorialButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(true); // Show the next button when the start config button is clicked
            startButton.gameObject.SetActive(false); // Hide the start button when the start config button is clicked
            startConfigButton.gameObject.SetActive(false); // Hide the start config button when the start config button is clicked
            imitoneVoiceIntepreter.SetGameOn(false);
        }
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
        mainText.SetActive(true); // Show the main text when the tutorial ends
        StartCoroutine(verticalLayoutGroupController.unScaleText(calibrationTexts[calibrationTextIndex], 1.1f));
        verticalLayoutGroupController.unhighLightText(calibrationTexts[calibrationTextIndex]); //
        AkSoundEngine.PostEvent("Stop_Calibration_Sequence", gameObject);
        startConfigButton.gameObject.SetActive(true); // Show the start config button when the tutorial ends
        nextButton.gameObject.SetActive(false); // Hide the next button when the tutorial ends
        startButton.gameObject.SetActive(true); // Show the start button when the tutorial ends
        endTutorialButton.gameObject.SetActive(false); // Hide the end tutorial button when the tutorial ends
        canvasManager.calibrationCanvas.enabled= false; // Switch back to the main canvas
        isCalibrationStarted = false; // Reset the flag to allow starting the calibration again
        lightControl.LightSettingsInitialization(5.0f);
    }

    public void OnNextButtonClicked()
    {
        calibrationTextIndex++;
        //lightControl.LightSettingsInitialization(5.0f);
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
        AkSoundEngine.SetSwitch("Calibration_Sequence", portionName, gameObject);

        // If we're at the 'End' portion, hide the Next button
        if (portion == TutorialPortions.End)
        {
            nextButton.gameObject.SetActive(false);
        }
    }

    public void StopCalibrationSequence()
    {
        AkSoundEngine.PostEvent("Stop_Calibration_Sequence", gameObject);
    }


    public void CalibrationCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
            if (musicSyncInfo.userCueName == "Cue_Microphone_ON")
            {
                Debug.Log("Calibration:  Turn ON GameOn for Mic");
                imitoneVoiceIntepreter.SetGameOn(true);
            }
            else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
            {                
                Debug.Log("Calibration:  Turn off GameOn for Mic");                

                imitoneVoiceIntepreter.SetGameOn(false);
            }
            else if (musicSyncInfo.userCueName == "Cue_AVS_Calibration_Start")
            {
                Debug.Log("Calibration:  Cue_AVS_Calibration_Start");
                if (lightControl != null && lightControl.gameObject.activeInHierarchy)
                {
                    lightControl.SetPreferredColor("White", 5.0f);
                    lightControl.SetStrobeRate(10f, 0.0f);
                }
                else
                {
                    Debug.LogError($"Calibration: lightControl is null or inactive! lightControl={lightControl}, activeInHierarchy={lightControl?.gameObject.activeInHierarchy}. Cannot turn on lights.");
                }
            }
            else if (musicSyncInfo.userCueName == "Cue_AVS_Calibration_End")
            {
                Debug.Log("Calibration:  Cue_AVS_Calibration_End");
                if (lightControl != null && lightControl.gameObject.activeInHierarchy)
                {
                    lightControl.LightSettingsInitialization(5.0f);
                }
                else
                {
                    Debug.LogError($"Calibration: lightControl is null or inactive! lightControl={lightControl}, activeInHierarchy={lightControl?.gameObject.activeInHierarchy}. Cannot turn off lights.");
                }
            }
            else
            {
                Debug.LogWarning("Calibration:  Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
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
