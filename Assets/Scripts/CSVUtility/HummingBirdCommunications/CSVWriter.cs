using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.Android;
using UnityEngine.Rendering;

public class CSVWriter : MonoBehaviour
{
    bool wasPaused = false; 
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    string sessionStatusPath = "";
    string session_resultsPath = "";
    string wavFilesPath = "";
    public bool paused = false;
    public int currentSessionNumber = 0;
    string baseSessionsFolderPath = "";
    public UserOutput playerOutput;
    private List<string> frameDataList = new List<string>();
    public RespirationTracker respirationTracker;
    public GameManagement gameManagement;


    public string GameMode;
    public string SubGameMode;




    // Start is called before the first frame update
    void Start()
    {
        baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists

        // Path to the sessions.csv file
        string sessionsCsvPath = Path.Combine(baseSessionsFolderPath, "sessions.csv");

        // Check if sessions.csv exists
        if (File.Exists(sessionsCsvPath))
        {
            using (StreamReader reader = new StreamReader(sessionsCsvPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');
                    if (columns.Length >= 2 && columns[1].Trim().ToLower() == "ready")
                    {
                        if (int.TryParse(columns[0].Trim(), out int sessionNumber))
                        {
                            currentSessionNumber = sessionNumber;
                            sessionStatusPath = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}", "session_status.csv");
                            break; // Stop reading once the ready session is found
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("sessions.csv not found.");
        }
        ReadCSV();
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

    void ReadCSV()
    {
        if(currentSessionNumber != 0)
        {
            string sessionsParams = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}", "session_params.csv");
            if(File.Exists(sessionsParams))
            {
                string[] data = File.ReadAllText(sessionsParams).Split(new string[] {",","\n"}, StringSplitOptions.None);
                GameMode = data[0];
                SubGameMode = data[1];
            }
            else 
            {
                Debug.LogError("CSV file not found at: " + sessionsParams);
            }
        }

    }
    void Update()
    {
        CheckPauseStatus();
        if (!paused)
        {
            if(wasPaused)
            {
                LogMessage("Resumed");
                wasPaused = false;
            }
            GetData();
        }
        else if(paused)
        {
            LogMessage("Paused");
            wasPaused = true;
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
                    if (statusParts.Length > 0) // ensure the line is not empty
                    {
                        string controlStatus = statusParts[0].Trim().ToLower();
                        if(controlStatus == "paused")
                        {
                            paused = true;
                        }
                        else if(controlStatus == "resumed")
                        {
                            paused = false;
                        }
                        else if( controlStatus == "terminated")
                        {
                            writeCSV();
                            gameManagement.EndGame();
                        }
                    }
                }
            }
        }
    }

    void LogMessage(string message)
    {
        string data = $"{Time.time}, {message}";
        frameDataList.Add(data);
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
