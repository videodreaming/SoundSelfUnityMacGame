using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataOutput : MonoBehaviour
{
    public RespirationTracker respirationTracker;
    public LightControl lightControl;
    public ImitoneVoiceIntepreter imitoneVoiceInterprter;

    string AVSColorCommand = "";
    string AVSStrobeCommand = "";
    
    private StreamWriter writer;
    private string filePath;
    // Start is called before the first frame update
    void Start()
    {
        //Future questions for our Machine Learning Algorithm:
        // - Of the various abasorption details, which are most important for determining the user's emotional state?
        // - Is the dynamic switching between 1m and 2m valuable, or should we just use 2m?

        //Write Session Data
        filePath = Path.Combine(Application.streamingAssetsPath, "SessionData.csv");
        writer = new StreamWriter(filePath, false) { AutoFlush = false };
        writer.WriteLine("Clock," + 
            "Run Time," +
            "Command: AVS Strobe Rate," +
            "Command: AVS Color," +
            "Microphone Interactivity," +
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
                         $"{imitoneVoiceInterprter.gameOn}," +
                         $"{RespirationTracker.instance._respirationRate}," +
                         $"{RespirationTracker.instance._meanToneLength}," +
                         $"{RespirationTracker.instance._meanRestLength}," +
                         $"{RespirationTracker.instance._respirationRate1min}," +
                         $"{RespirationTracker.instance._respirationRate2min}," +
                         $"{RespirationTracker.instance._respirationRateRaw1min}," +
                         $"{RespirationTracker.instance._respirationRateRaw2min}," +
                         $"{RespirationTracker.instance._meanToneLength1min}," +
                         $"{RespirationTracker.instance._meanToneLength2min}," +
                         $"{RespirationTracker.instance._meanRestLength1min}," +
                         $"{RespirationTracker.instance._meanRestLength2min}," +
                         $"{RespirationTracker.instance._absorption}," +
                         $"{RespirationTracker.instance._absorptionRaw}," +
                         $"{RespirationTracker.instance._standardDeviationTone1min}," +
                         $"{RespirationTracker.instance._standardDeviationTone2min}," +
                         $"{RespirationTracker.instance._standardDeviationRest1min}," +
                         $"{RespirationTracker.instance._standardDeviationRest2min}," +
                         $"{RespirationTracker.instance._absorptionRespirationRateMultiplier1min}," +
                         $"{RespirationTracker.instance._absorptionRespirationRateMultiplier2min}," +
                         $"{RespirationTracker.instance._absorptionToneLengthMultiplier1min}," +
                         $"{RespirationTracker.instance._absorptionToneLengthMultiplier2min},");

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
