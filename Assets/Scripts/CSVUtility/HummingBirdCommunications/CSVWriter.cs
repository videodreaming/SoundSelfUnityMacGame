using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.Android;

public class CSVWriter : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    string sessionStatusPath = "";
    string session_resultsPath = "";
    string wavFilesPath = "";
    int currentSessionNumber = 0;
    string baseSessionsFolderPath = "";
    bool paused = false;
    public UserOutput playerOutput;
    private List<string> frameDataList = new List<string>();
    public RespirationTracker respirationTracker;


    // Start is called before the first frame update
    void Start()
    {
        baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists

        // Retrieve all directories in the base session folder that start with "session_"
        string[] sessionDirectories = Directory.GetDirectories(baseSessionsFolderPath)
            .Where(dir => Path.GetFileName(dir).StartsWith("session_"))
            .ToArray();

        foreach (string sessionDir in sessionDirectories)
        {
            sessionStatusPath = Path.Combine(sessionDir, "session_status.csv");
            if (File.Exists(sessionStatusPath))
            {
                // Read the first line of the file and check if the status is "ready"
                using (StreamReader sr = new StreamReader(sessionStatusPath))
                {
                    string firstLine = sr.ReadLine();
                    if (firstLine != null && firstLine.Split(',').First().Trim().ToLower() == "ready")
                    {
                        string sessionNumberPart = Path.GetFileName(sessionDir).Replace("session_", "");
                        if (int.TryParse(sessionNumberPart, out int sessionNumber))
                        {
                            currentSessionNumber = sessionNumber;
                        }
                    }
                }
            }
        }
    }

    void GetData()
    {
                string data = $"{Time.time}," +
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
                      $"{respirationTracker._absorptionToneLengthMultiplier2min}";
        // Log data by adding to the list
        frameDataList.Add(data);
    }

    void Update()
    {
        CheckPauseStatus();
        if (!paused)
        {
            GetData();
        }
    }

     void CheckPauseStatus()
    {
        if (File.Exists(sessionStatusPath))
        {
            using (StreamReader sr = new StreamReader(sessionStatusPath))
            {
                string statusLine = sr.ReadLine();
                if (!string.IsNullOrEmpty(statusLine))
                {
                    string[] statusParts = statusLine.Split(',');
                    if (statusParts.Length > 1) // Check if the second part exists
                    {
                        string controlStatus = statusParts[1].Trim().ToLower();
                        paused = controlStatus != "resume";
                    }
                }
            }
        }
    }

    float GetData1()
    {
        return imitoneVoiceIntepreter.pitch_hz;
    }
    float GetData2()
    {
        return imitoneVoiceIntepreter._dbValue;
    }
    float GetData3()
    {
        return imitoneVoiceIntepreter._harmonicity;
    }

    public void writeCSV()
    {

        string sessionsFolder = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}");
        Directory.CreateDirectory(sessionsFolder);
        session_resultsPath = Path.Combine(sessionsFolder, "session_results.csv");
        Debug.Log(baseSessionsFolderPath + session_resultsPath);
    // Ensure the session results file is created if it does not exist
        if (!File.Exists(session_resultsPath))
        {
            Debug.Log("created new file at: " + session_resultsPath);
            using (TextWriter tw = new StreamWriter(session_resultsPath, false))
            {
                tw.Close();
            }
        } else {
            Debug.Log("file already exists at: " + session_resultsPath);
        }

        // Append new data to the session results file
        using (TextWriter tw = new StreamWriter(session_resultsPath, true))
        {
            foreach (var frameData in frameDataList)
            {
                // Writing each tuple's data as a new line in the CSV
                tw.WriteLine(frameData);
            }
        }

        // Optionally, clear the list after writing to prevent duplicate entries in subsequent writes
        frameDataList.Clear();
    }
}
