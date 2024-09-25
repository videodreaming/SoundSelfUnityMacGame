using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevelopmentMode : MonoBehaviour
{
    public bool developmentMode = false;
    public bool configureMode = false; //not actually a development mode, used for configuring light and sound.
    [Header("(Optional) Choose One to Modify Start Positiion:")]
    public bool startAtStart = true;
    public bool startInPlayground = false;
    public bool startInTutorial = false;
    public bool startRightBeforeSavasana = false;
    public bool startInSavasana = false;

    void Awake()
    {
        if(developmentMode)
        {
            Debug.Log("AWAKE IN DEVELOPMENT MODE");
            if(startInPlayground)
            {
                Debug.Log("AWAKE STARTING AT PLAYGROUND");
                startInTutorial = false;
                startRightBeforeSavasana = false;
                startInSavasana = false;
                startAtStart = false;
            }
            else if(startInTutorial)
            {
                Debug.Log("AWAKE STARTING IN TUTORIAL");
                startRightBeforeSavasana = false;
                startInSavasana = false;
                startAtStart = false;
            }
            else if(startRightBeforeSavasana)
            {
                Debug.Log("AWAKE STARTING RIGHT BEFORE SAVASANA");
                startInSavasana = false;
                startAtStart = false;
            }
            else if(startInSavasana)
            {
                Debug.Log("AWAKE STARTING AT SAVASANA");
                startAtStart = false;
            }
            else
            {
                Debug.Log("AWAKE STARTING AT THE BEGINNING");
                startAtStart = true;
            }

        }
        else
        {
            Debug.Log("AWAKE IN PRODUCTION MODE");
            startInPlayground = false;
            startInTutorial = false;
            startInSavasana = false;
            startRightBeforeSavasana = false;
            startAtStart = true;
        }
    }

    public void LogChangeBool(string text, bool input){
        bool oldBoolInput = false;
        if(input != oldBoolInput){
            //Debug.Log(text + input);
            oldBoolInput = input;
        }
    }

    public void LogChangeFloat(string text, float input){
        float oldFloatInput = 0.0f;
        if(input != oldFloatInput){
            //Debug.Log(text + input);
            oldFloatInput = input;
        }
    }
}
