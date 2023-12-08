using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevModeSettings : MonoBehaviour
{
    private bool startInDevMode = true;
    public bool devMode;
    
    public bool forceToneActive = false;
    public bool forceNoTone = false;

    void Start()
    {
        devMode = startInDevMode;
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha0)){
            devMode = false;
            forceToneActive = false;
            forceNoTone = false;
        }
        if(Input.GetKeyDown(KeyCode.Alpha9)){
            if(startInDevMode){
                devMode = true;
            }
        }
    }


    public void LogChangeBool(string text, bool input){
        bool oldBoolInput = false;
        if(input != oldBoolInput){
            Debug.Log(text + input);
            oldBoolInput = input;
        }
    }

    public void LogChangeFloat(string text, float input){
        float oldFloatInput = 0.0f;
        if(input != oldFloatInput){
            Debug.Log(text + input);
            oldFloatInput = input;
        }
    }
}

