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
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    
    private StreamWriter writer;
    private string filePath;
    // Start is called before the first frame update
    void Start()
    {
        if (imitoneVoiceIntepreter == null)
        {
            Debug.LogError("Exception: ImitoneVoiceIntepreter not found");
        }

        //Write Session Data
        filePath = Path.Combine(Application.streamingAssetsPath, "SessionData.csv");
        writer = new StreamWriter(filePath, false);
        writer.WriteLine("Clock, " + 
            "Run Time, " +
            "Respiration: Rate, " +
            "Respiration: Mean Tone Length, " +
            "Respiration: Mean Rest Length, " +
            "Respiration Detail: Rate 1min measurement window, " + 
            "Respiration Detail: Rate 2min measurement window, " +
            "Respiration Detail: Rate Raw 1m, " +
            "Respiration Detail: Rate Raw 2m" +
            "Respiration Detail: Mean Tone Length 1m, " +
            "Respiration Detail: Mean Tone Length 2m, " +
            "Respiration Detail: Mean Rest Length 1m, " +
            "Respiration Detail: Mean Rest Length 2m, ");

        // Invoke the WriteSessionData method every second
        InvokeRepeating("WriteSessionData", 0f, 1f);
    }

    void WriteSessionData()
    {
        // Write Session Data
        // get the OS's current time
        DateTime timeNow = DateTime.Now;
        float timeSinceLaunch = Time.time;

        writer.WriteLine($"{DateTime.Now}, " +
                         $"{Time.time}, " +
                         $"{respirationTracker._respirationRate}, " +
                         $"{respirationTracker._meanToneLength}, " +
                         $"{respirationTracker._meanRestLength}, " +
                         $"{respirationTracker._respirationRate1min}, " +
                         $"{respirationTracker._respirationRate2min}, " +
                         $"{respirationTracker._respirationRateRaw1min}, " +
                         $"{respirationTracker._respirationRateRaw2min}" +
                         $"{respirationTracker._meanToneLength1min}, " +
                         $"{respirationTracker._meanToneLength2min}, " +
                         $"{respirationTracker._meanRestLength1min}, " +
                         $"{respirationTracker._meanRestLength2min}, ");
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

    void OnDisable()
    {
        if (writer != null)
        {
            writer.Close();
        }
    }

}
