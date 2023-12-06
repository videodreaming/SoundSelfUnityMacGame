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
            //SHIFT+T = FORCE TONEACTIVE 
            if(Input.GetKeyUp(KeyCode.LeftShift)){ //Release Control
                forceToneActive = false;
                forceNoTone = false;
            }
            else if(Input.GetKey(KeyCode.LeftShift)){
                if(Input.GetKey(KeyCode.T)){
                    forceToneActive = true;
                    forceNoTone = false;
                } 
                else
                {
                    forceToneActive = false;
                    forceNoTone = true;
                }
            }
        }
    }
}
