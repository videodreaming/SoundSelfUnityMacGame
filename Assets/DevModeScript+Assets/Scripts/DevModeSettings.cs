using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevModeSettings : MonoBehaviour
{
    private bool startInDevMode = true;
    private bool devMode;
    
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

        if(devMode == true){
            //T = FORCE TONEACTIVE 
            if(Input.GetKeyDown(KeyCode.T)){
                Debug.Log("Force Tone");
                forceToneActive = true;
                forceNoTone = false;
            }
            else if (Input.GetKeyUp(KeyCode.T) && forceToneActive == true){
                Debug.Log("Force No-Tone");
                forceToneActive = false;
                forceNoTone = true;
            }
        }
    }

    public void LogChangeBool(string text, bool input){
        bool oldInput = false;
        if(input != oldInput){
            Debug.Log(text + input);
            oldInput = input;
        }
    }
    public void LogChangeFloat(string text, float input){
        float oldInput = 0.0f;
        if(input != oldInput){
            Debug.Log(text + input);
            oldInput = input;
        }
    }
}

