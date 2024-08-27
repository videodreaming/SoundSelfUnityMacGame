using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserControlScriptForPlayground : MonoBehaviour
{
    public AudioManager audioManager;
    public RespirationTrackerForPlayground respirationTracker;
    public WwiseGlobalManager wwiseGlobalManager;
    public ImitoneVoiceIntepreterForPlayground imitoneVoiceIntepreter;
    
    private StreamWriter writer;
    private string filePath;
    // Start is called before the first frame update
    void Start()
    {
        if (imitoneVoiceIntepreter == null)
        {
            Debug.LogError("Exception: ImitoneVoiceIntepreter not found");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("Pause Placeholder Activated");
            imitoneVoiceIntepreter.SetMute(true);
            respirationTracker.pauseGuardTone = true;
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            Debug.Log("Pause Placeholder Deactivated");
            imitoneVoiceIntepreter.SetMute(false);
            respirationTracker.pauseGuardTone = false;
        }
    }
}