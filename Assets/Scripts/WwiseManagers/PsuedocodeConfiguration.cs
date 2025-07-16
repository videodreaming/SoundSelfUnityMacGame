public class Configuration : MonoBehaviour
{
    private bool passMoment = false; //noting that the logic for activating passMoment must happen before the logic for checking it.
    private bool passGate = false;
    private bool pass = false;
    private bool buttonPressed = false; //this turns to true when the button is pressed
    private bool voPlaying = false; //this should turn on when the vo is playing, and off when it stops.

    void Start()
    {
    }

    void Update ()
    {
        //When the button is pressed, ButtonPressedCoroutine();
    }

    void LateUpdate()
    {
        passMoment = false;
    }

    public void VOCallbackFunction ()//INITIATE ME
    {
        if (in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            Debug.Log("Configuration: Callback triggered: " + in_type);

            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;

            if (musicSyncInfo.userCueName == "Cue_Calibration_BreakMoment")
            {
                Debug.Log("Configuration: BreakMoment cue triggered");
                passMoment = true;
            }
            else if (musicSyncInfo.userCueName == "Cue_Calibration_BreakOpen")
            {
                passGate = true;
            }
            else if (musicSyncInfo.userCueName == "Cue_Calibration_BreakClose")
            {
                passGate = false;
            }
            else if (musicSyncInfo.userCueName == "Cue_Calibration_MicOn")
            {
                imitoneVoiceInterpreter.SetGameOn(true);
            }
            else if (musicSyncInfo.userCueName == "Cue_Calibration_End")
            {
                // Handle the specific cue
            }
            else
            {
                Debug.LogWarning("Configuration: Unhandled cue: " + musicSyncInfo.userCueName);
            }
        }
    }

    IEnumerator StartConfigurationSequence(bool useVibroacoustics)
    {
        //NOTE THAT THIS DOES NOT INCLUDE DESIGN FOR LORNA'S "TECHNICAL ISSUES" VO. PROBABLY THAT SHOULD BE HANDLED IN WWISE

        //PSEUDOCODE: PLAY THE VO_CALIBRATION_INTRO
        //PSUEDOCODE: PLAY MUSIC_CALIBRATION
        yield return null;

        //FIRST, WAIT FOR THE END OF THE INTRO, OR FOR THE BUTTON TO BE PRESSED AND THEN 
        while (!pass && voPlaying)
        {
            yield return null;
        }
        ResetTest();
        //PSEUDOCODE: PLAY VO_CALIBRATION_VOLUME

        while(!pass)
        {
            yield return null;
        }
        ResetTest();
        //PSEUDOCODE: PLAY VO_CALIBRATION_MIC

        while(!pass)
        {
            yield return null;
        }
        ResetTest();
        
        if(useVibroacoustics)
        {
            //PSEUDOCODE: PLAY VO_CALIBRATION_VIBRATION
            while(!pass)
            {
                yield return null;
            }
            ResetTest();
        }

        //PSEUDOCODE: PLAY VO_CALIBRATION_LIGHTS
        while(!pass)
        {
            yield return null;
        }
        ResetTest();
        //PSEUDOCODE: PLAY VO_CALIBRATION_END
       
        while(!pass && voPlaying)
        {
            yield return null;
        }
        ResetTest();
        //PSEUDOCODE: FADE OUT MUSIC
        //PSEUDOCODE: START FULL EXPERIENCE
    }

    private void ResetTest()
    {
        passMoment = false; //probably not necessary
        passGate = false; //probably not necessary
        pass = false; //definitely necessary
        buttonPressed = false; //definitely necessary
        imitoneVoiceInterpreter.SetGameOn(false); //only necessary after mic test, but putting it here anyway for cleanliness
    }

    private bool PassTest()
    {
        bool allowPass = passMoment || passGate;
        passGate = false;
        return allowPass;
    }

    IEnumerator buttonPressedCoroutine() //
    {
        //first, if buttonPressed is already true, return with a warning
        if (buttonPressed)
        {
            Debug.LogWarning("Configuration: Button was already pressed");
            yield break;
        }
        buttonPressed = true;

        // Wait for the button press to be released
        while (!PassTest())
        {
            yield return null;
        }
        pass = true;
    }
}