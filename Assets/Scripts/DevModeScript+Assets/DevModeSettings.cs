using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//WE DO NOT USE THIS ANYMORE, WE USE DEVELOPMENTMODE.CS
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
    }

}

