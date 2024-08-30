using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataOutput : MonoBehaviour
{
    public AudioManager audioManager;
    public RespirationTracker respirationTracker;
    public WwiseGlobalManager wwiseGlobalManager;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public LightControl lightControl;

    string AVSColorCommand = "";
    string AVSStrobeCommand = "";
    
    private StreamWriter writer;
    private string filePath;
    // Start is called before the first frame update
    void Start()
    {
        if (imitoneVoiceIntepreter == null)
        {
            Debug.LogError("Exception: ImitoneVoiceIntepreter not found");
        }

        //Future questions for our Machine Learning Algorithm:
        // - Of the various abasorption details, which are most important for determining the user's emotional state?
        // - Is the dynamic switching between 1m and 2m valuable, or should we just use 2m?

        //Write Session Data
        filePath = Path.Combine(Application.streamingAssetsPath, "SessionData.csv");
        writer = new StreamWriter(filePath, false);
        writer.WriteLine("Clock," + 
            "Run Time," +
            "Command: AVS Strobe Rate," +
            "Command: AVS Color," +
            "Respiration: Rate," +
            "Respiration: Mean Tone Length," +
            "Respiration: Mean Rest Length," +
            "Respiration Detail: Rate 1min measurement window," + 
            "Respiration Detail: Rate 2min measurement window," +
            "Respiration Detail: Rate Raw 1m," +
            "Respiration Detail: Rate Raw 2m," +
            "Respiration Detail: Mean Tone Length 1m," +
            "Respiration Detail: Mean Tone Length 2m," +
            "Respiration Detail: Mean Rest Length 1m," +
            "Respiration Detail: Mean Rest Length 2m," +
            "Absorption," +
            "Absorption Raw," +
            "Absorption Detail: Standard Deviation Tone 1m," +
            "Absorption Detail: Standard Deviation Tone 2m," +
            "Absorption Detail: Standard Deviation Rest 1m," +
            "Absorption Detail: Standard Deviation Rest 2m," +
            "Absorption Detail: Respiration Rate Multiplier 1m," +
            "Absorption Detail: Respiration Rate Multiplier 2m," +
            "Absorption Detail: Tone Length Multiplier 1m," +
            "Absorption Detail: Tone Length Multiplier 2m,");

        // Invoke the WriteSessionData method every second
        InvokeRepeating("WriteSessionData", 1f, 1f);
    }

    void WriteSessionData()
    {
        // Write Session Data
        // get the OS's current time
        DateTime timeNow = DateTime.Now;
        float timeSinceLaunch = Time.time;

        writer.WriteLine($"{DateTime.Now}," +
                         $"{Time.time}," +
                         $"{AVSStrobeCommand}," +
                         $"{AVSColorCommand}," +
                         $"{respirationTracker._respirationRate}," +
                         $"{respirationTracker._meanToneLength}," +
                         $"{respirationTracker._meanRestLength}," +
                         $"{respirationTracker._respirationRate1min}," +
                         $"{respirationTracker._respirationRate2min}," +
                         $"{respirationTracker._respirationRateRaw1min}," +
                         $"{respirationTracker._respirationRateRaw2min}," +
                         $"{respirationTracker._meanToneLength1min}," +
                         $"{respirationTracker._meanToneLength2min}," +
                         $"{respirationTracker._meanRestLength1min}," +
                         $"{respirationTracker._meanRestLength2min}," +
                         $"{respirationTracker._absorption}," +
                         $"{respirationTracker._absorptionRaw}," +
                         $"{respirationTracker._standardDeviationTone1min}," +
                         $"{respirationTracker._standardDeviationTone2min}," +
                         $"{respirationTracker._standardDeviationRest1min}," +
                         $"{respirationTracker._standardDeviationRest2min}," +
                         $"{respirationTracker._absorptionRespirationRateMultiplier1min}," +
                         $"{respirationTracker._absorptionRespirationRateMultiplier2min}," +
                         $"{respirationTracker._absorptionToneLengthMultiplier1min}," +
                         $"{respirationTracker._absorptionToneLengthMultiplier2min},");

        AVSColorCommand = "";
        AVSStrobeCommand = "";
    }

    void Update()
    {
        // "Commands"
        /*if (lightControl.AVSColorCommand != "")
        {
            AVSColorCommand = lightControl.AVSColorCommand;
        }
        if (lightControl.AVSStrobeCommand != "")
        {
            AVSStrobeCommand = lightControl.AVSStrobeCommand;
        }*/
    }

    void OnDisable()
    {
        if (writer != null)
        {
            writer.Close();
        }
    }

}
