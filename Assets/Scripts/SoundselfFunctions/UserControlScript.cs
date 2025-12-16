using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserControlScript : MonoBehaviour
{
    public RespirationTracker respirationTracker;
    public WwiseGlobalManager wwiseGlobalManager;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    private StreamWriter writer;
    private bool cacheGameOn;
    private bool developmentModeWarningFlag = false;
    private string filePath;
    // Start is called before the first frame update
    void Start()
    {
        if (imitoneVoiceInterpreter == null)
        {
            Debug.LogError("UserControlScript: Exception: ImitoneVoiceIntepreter not found");
        }
    }

    void Update()
    {
        if((DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode))
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Debug.Log("UserControlScript: Pause Placeholder Activated");
                cacheGameOn = imitoneVoiceInterpreter.gameOn;
                imitoneVoiceInterpreter.gameOn = false;
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                Debug.Log("UserControlScript: Pause Placeholder Deactivated");
                imitoneVoiceInterpreter.gameOn = cacheGameOn;
            }
        }
        else if (DevelopmentMode.instance == null && !developmentModeWarningFlag)
        {
            developmentModeWarningFlag = true;
            Debug.LogWarning("UserControlScript: DevelopmentMode instance not found, UserControlScript pause functionality disabled");
        }
    }
}
