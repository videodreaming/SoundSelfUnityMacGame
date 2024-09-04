using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserControlScript : MonoBehaviour
{
    public AudioManager audioManager;
    public RespirationTracker respirationTracker;
    public WwiseGlobalManager wwiseGlobalManager;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    
    private StreamWriter writer;
    private bool cacheGameOn;
    private string filePath;
    // Start is called before the first frame update
    void Start()
    {
        if (imitoneVoiceInterpreter == null)
        {
            Debug.LogError("Exception: ImitoneVoiceIntepreter not found");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("Pause Placeholder Activated");
            cacheGameOn = imitoneVoiceInterpreter.gameOn;
            imitoneVoiceInterpreter.gameOn = false;
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            Debug.Log("Pause Placeholder Deactivated");
            imitoneVoiceInterpreter.gameOn = cacheGameOn;
        }
    }
}
