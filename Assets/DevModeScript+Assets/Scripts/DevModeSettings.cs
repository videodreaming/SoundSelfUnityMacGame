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
        }
        if(Input.GetKeyDown(KeyCode.Alpha9)){
            if(startInDevMode){
                devMode = true;
            }
        }
        if(devMode == true){
            if(Input.GetKey(KeyCode.LeftShift)){
                if(Input.GetKey(KeyCode.T)){
                    forceNoTone = true;
                    forceToneActive = false;
                } 
                else 
                {
                    forceNoTone = false;
                }
            }
            else 
            {
                if(Input.GetKey(KeyCode.T)){
                    forceToneActive = true;
                    forceNoTone = false;
                } else {
                forceToneActive = false;
                }
            }

            
        }
    }
}
