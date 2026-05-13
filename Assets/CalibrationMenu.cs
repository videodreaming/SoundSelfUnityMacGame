using UnityEngine;
using SoundSelf.Sequence;

/// <summary>
/// Wwise relay for the calibration music sequence.
/// Single Ak GameObject owner of <c>Play_Calibration_Sequence</c> / <c>Stop_Calibration_Sequence</c> / <c>SetSwitch("Calibration_Sequence", ...)</c>.
/// The Wwise API requires the same GameObject for Play / Stop / SetSwitch on a given sequence (see WwiseVOManager note), so all three live on this component.
/// Translates <see cref="AkCallbackType.AK_MusicSyncUserCue"/> user-cue names into <see cref="SequenceCommand"/> and forwards them through <see cref="Sequencer.HandleSequenceCommand"/>.
/// Gameplay side-effects (Imitone, lights, polite Next) live in <c>CalibrationStageHandler.ExecuteSequenceCommand</c>; this class is wire only.
///
/// <para><b>Polite Next (dual mirror):</b> Wwise interactive music already advances audio at safe points (<c>Cue_Calibration_Next</c>, instruction ON/OFF).
/// Unity mirrors that on buttons/screens (loading until unlock) — redundant state by design; a bit inelegant but intentional. See <c>Docs/CALIBRATION_UI_SEQUENCING_PLAN.md</c>.</para>
/// </summary>
public class CalibrationMenu : MonoBehaviour
{
    private Sequencer _sequencerForCallback;
    private bool _isPlaying;

    /// <summary>Posts <c>Play_Calibration_Sequence</c> on this GameObject with an <c>AK_MusicSyncUserCue</c> callback. Idempotent: re-entry is a no-op until <see cref="StopCalibrationSequence"/>. Pass the active sequencer so the music-sync callback can route cues through <see cref="Sequencer.HandleSequenceCommand"/>.</summary>
    public void StartCalibrationSequence(Sequencer sequencer)
    {
        _sequencerForCallback = sequencer;
        if (_isPlaying)
        {
            Debug.Log("CalibrationMenu: StartCalibrationSequence — already playing; skipping re-post (idempotent).");
            return;
        }
        _isPlaying = true;
        AkSoundEngine.PostEvent("Play_Calibration_Sequence", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, CalibrationMusicSyncCallback, null);
    }

    /// <summary>Posts <c>Stop_Calibration_Sequence</c> on this GameObject. Safe to call when not playing (Wwise no-op).</summary>
    public void StopCalibrationSequence()
    {
        AkSoundEngine.PostEvent("Stop_Calibration_Sequence", gameObject);
        _isPlaying = false;
    }

    /// <summary>Sets the Wwise switch <c>Calibration_Sequence</c> to the named portion (e.g. <c>Intro</c>, <c>Volume</c>, <c>Mic</c>, <c>Vibration</c>, <c>Lights</c>, <c>End</c>). Switch state must be set on the same GameObject that owns Play/Stop.</summary>
    public void SetCalibrationPortionSwitch(string portion)
    {
        if (string.IsNullOrEmpty(portion))
        {
            Debug.LogWarning("CalibrationMenu: SetCalibrationPortionSwitch called with empty portion; ignoring.");
            return;
        }
        AkSoundEngine.SetSwitch("Calibration_Sequence", portion, gameObject);
    }

    /// <summary>Back-step rewind: Stop the current sequence, set the switch to <paramref name="portion"/>, then Play again — all on this GameObject in the same frame. Does not wait for any cue; the UI step has already moved.</summary>
    public void RestartFromPortion(Sequencer sequencer, string portion)
    {
        AkSoundEngine.PostEvent("Stop_Calibration_Sequence", gameObject);
        _isPlaying = false;
        SetCalibrationPortionSwitch(portion);
        StartCalibrationSequence(sequencer);
    }

    private void CalibrationMusicSyncCallback(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type != AkCallbackType.AK_MusicSyncUserCue)
            return;

        var info = in_info as AkMusicSyncCallbackInfo;
        if (info == null)
        {
            Debug.Log("CalibrationMenu: MusicSync callback — in_info is not AkMusicSyncCallbackInfo (ignored).");
            return;
        }

        string cue = info.userCueName;
        if (string.IsNullOrEmpty(cue))
        {
            Debug.Log("CalibrationMenu: MusicSyncUserCue callback — userCueName is null or empty (ignored).");
            return;
        }

        Debug.Log("CalibrationMenu: MusicSyncUserCue received: userCueName='" + cue + "'");

        if (_sequencerForCallback == null)
        {
            Debug.LogWarning("CalibrationMenu: Music-sync cue '" + cue + "' fired but sequencer reference is null — cannot route to HandleSequenceCommand.");
            return;
        }

        if (!TryMapCalibrationMusicUserCue(cue, out SequenceCommand command))
        {
            Debug.Log("CalibrationMenu: MusicSyncUserCue '" + cue + "' — unmapped, not forwarded to Sequencer.");
            return;
        }

        bool handled = _sequencerForCallback.HandleSequenceCommand(command);
        Debug.Log("CalibrationMenu: MusicSyncUserCue '" + cue + "' → " + command + "; HandleSequenceCommand handled=" + handled);
    }

    /// <summary>Maps Wwise <c>AK_MusicSyncUserCue</c> <c>userCueName</c> from <c>Play_Calibration_Sequence</c> to <see cref="SequenceCommand"/>.</summary>
    private static bool TryMapCalibrationMusicUserCue(string userCueName, out SequenceCommand command)
    {
        switch (userCueName)
        {
            case "Cue_Microphone_ON":
                command = SequenceCommand.CalibrationMicrophoneOn;
                return true;
            case "Cue_Microphone_OFF":
                command = SequenceCommand.CalibrationMicrophoneOff;
                return true;
            case "Cue_AVS_Calibration_Start":
                command = SequenceCommand.CalibrationAvsStart;
                return true;
            case "Cue_AVS_Calibration_End":
                command = SequenceCommand.CalibrationAvsEnd;
                return true;
            case "Cue_Calibration_Instruction_ON":
                command = SequenceCommand.CalibrationInstructionVoStarted;
                return true;
            case "Cue_Calibration_Instruction_OFF":
            case "Cue_Calibration_Instruction_End":
                // _End: same intent as _OFF per sound design; not present in checked-in Wwise — alias if authoring adds it.
                command = SequenceCommand.CalibrationInstructionVoEnded;
                return true;
            // Still mapped so logs show receipt; CalibrationStageHandler does NOT unlock pending Next from this cue (Wwise often never posts it to Unity — see handler CalibrationPoliteNext case).
            case "Cue_Calibration_Next":
                command = SequenceCommand.CalibrationPoliteNext;
                return true;
            default:
                command = default;
                return false;
        }
    }
}
/*
public class CalibrationMenu : MonoBehaviour
{
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
    private bool _calibrationListenersRegistered;


    // Start is called before the first frame update
    public void StartCalibrationSequence()
    {
        //THESE PARTS ARE LEFT OVER FROM WHEN THE CALIBRATION STARTED ON LAUNCH. WE SHOULD REMOVE THEM SOON.
        RegisterCalibrationUiListenersIfNeeded();
        endTutorialButton.gameObject.SetActive(false); // Hide the end tutorial button initially
        nextButton.gameObject.SetActive(false); // Hide the next button initially
        
        //THIS ONE WE SHOULD KEEP.
        AkSoundEngine.PostEvent("Play_Calibration_Sequence", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, CalibrationCallBackFunction, null);
    }

    private void RegisterCalibrationUiListenersIfNeeded()
    {
        if (_calibrationListenersRegistered)
            return;

        startButton.onClick.AddListener(OnStartButtonClicked);
        startConfigButton.onClick.AddListener(OnStartConfigButtonClicked);
        endTutorialButton.onClick.AddListener(OnEndTutorialButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
        _calibrationListenersRegistered = true;
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
                // sequencer.StartTrueStart(); // Temporarily disabled while calibration menu flow is redesigned.
            }
            else
            {
                Debug.LogError("CalibrationMenu: sequencer is null! Cannot start sequence.");
                return; // Exit early if sequencer is null
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
*/


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
