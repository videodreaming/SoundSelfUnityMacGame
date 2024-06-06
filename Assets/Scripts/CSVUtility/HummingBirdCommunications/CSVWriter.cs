using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

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
    private string encryptedSessionNumber;
    private List<List<string>> frameDataList = new List<List<string>>();
    public RespirationTracker respirationTracker;
    public GameManagement gameManagement;
    public string encryptedReadyCheck;
    public string decryptedReadyCheck;
    public int sessionNumer;
    public string GameMode;
    public string SubGameMode;

    void Start()
    {

        #if UNITY_STANDALONE_OSX
                string userFolder = "/Users/harithliew/AppData/Roaming/Hummingbird";
                baseSessionsFolderPath = userFolder;
        #elif UNITY_STANDALONE_WIN
                baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
        #else
                Debug.LogError("Unsupported platform");
                return;
        #endif

        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists
        // Path to the sessions.csv file
        string sessionsCsvPath = Path.Combine(baseSessionsFolderPath, "sessions.csv");
        Debug.Log(sessionsCsvPath);
        // Check if sessions.csv exists
        if (File.Exists(sessionsCsvPath))
        {
            using (StreamReader reader = new StreamReader(sessionsCsvPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');
                    if (columns.Length >= 2)
                    {
                        encryptedReadyCheck = columns[1].Trim();
                        decryptedReadyCheck = EncryptionHelper.Decrypt(encryptedReadyCheck);
                        if(decryptedReadyCheck == "ready")
                        {
                            encryptedSessionNumber = columns[0].Trim();
                            if (int.TryParse(EncryptionHelper.Decrypt(encryptedSessionNumber), out int sessionNumber)) //if decrypted session number is an integer
                            {
                                currentSessionNumber = sessionNumber;
                                sessionStatusPath = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}", "session_status.csv");
                                break;
                            }
                        }
                    }
                }
            }
        }
        else
        {
           // Debug.LogError("sessions.csv not found.");
        }
        ReadCSV();
    }

    void GetData()
    {
        var dataList = new List<string>
        {
            EncryptionHelper.Encrypt($"{Time.time}"),
            EncryptionHelper.Encrypt($"{respirationTracker._respirationRate}"),
            EncryptionHelper.Encrypt($"{respirationTracker._meanToneLength}"),
            EncryptionHelper.Encrypt($"{respirationTracker._meanRestLength}"),
            EncryptionHelper.Encrypt($"{respirationTracker._respirationRate1min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._respirationRate2min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._respirationRateRaw1min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._respirationRateRaw2min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._meanToneLength1min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._meanToneLength2min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._meanRestLength1min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._meanRestLength2min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._absorption}"),
            EncryptionHelper.Encrypt($"{respirationTracker._absorptionRaw}"),
            EncryptionHelper.Encrypt($"{respirationTracker._standardDeviationTone1min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._standardDeviationTone2min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._standardDeviationRest1min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._standardDeviationRest2min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._absorptionRespirationRateMultiplier1min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._absorptionRespirationRateMultiplier2min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._absorptionToneLengthMultiplier1min}"),
            EncryptionHelper.Encrypt($"{respirationTracker._absorptionToneLengthMultiplier2min}")
        };

        frameDataList.Add(dataList);
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
            Debug.Log(sessionStatusPath);
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
                        else if(controlStatus == "terminated")
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
        var dataList = new List<string> { EncryptionHelper.Encrypt($"{Time.time}"), EncryptionHelper.Encrypt(message) };
        frameDataList.Add(dataList);
    }

    public void writeCSV()
    {
        string sessionsFolder = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}");
        Directory.CreateDirectory(sessionsFolder); // Ensure session folder exists
        session_resultsPath = Path.Combine(sessionsFolder, "session_results.csv");
        Debug.Log(baseSessionsFolderPath + session_resultsPath);
        if (!File.Exists(session_resultsPath))
        {
            Debug.Log("created new file at: " + session_resultsPath);
            using (TextWriter tw = new StreamWriter(session_resultsPath, false))
            {
                tw.Close();
            }
        }
        else
        {
            Debug.Log("file already exists at: " + session_resultsPath);
        }
        using (TextWriter tw = new StreamWriter(session_resultsPath, true))
        {
            foreach (var dataList in frameDataList)
            {
                tw.WriteLine(string.Join(",", dataList));
            }
        }
        frameDataList.Clear();
    }
}
