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
    private string combinedData = ""; // Store all data in a single string
    public RespirationTracker respirationTracker;
    public GameManagement gameManagement;
    public string encryptedReadyCheck;
    public string decryptedReadyCheck;
    public int sessionNumer;
    public string GameMode;
    public string SubGameMode;
    public bool microphoneMonitoring = false;

    void Start()
    {
        #if UNITY_STANDALONE_OSX
            string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string baseSessionsFolderPath = System.IO.Path.Combine(userFolder, "Hummingbird");
            Debug.Log("Base sessions folder path: " + baseSessionsFolderPath);
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
                                Debug.Log(currentSessionNumber);
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
             string data = string.Join(";",
            $"{Time.time}",
            $"{respirationTracker._respirationRate}",
            $"{respirationTracker._meanToneLength}",
            $"{respirationTracker._meanRestLength}",
            $"{respirationTracker._respirationRate1min}",
            $"{respirationTracker._respirationRate2min}",
            $"{respirationTracker._respirationRateRaw1min}",
            $"{respirationTracker._respirationRateRaw2min}",
            $"{respirationTracker._meanToneLength1min}",
            $"{respirationTracker._meanToneLength2min}",
            $"{respirationTracker._meanRestLength1min}",
            $"{respirationTracker._meanRestLength2min}",
            $"{respirationTracker._absorption}",
            $"{respirationTracker._absorptionRaw}",
            $"{respirationTracker._standardDeviationTone1min}",
            $"{respirationTracker._standardDeviationTone2min}",
            $"{respirationTracker._standardDeviationRest1min}",
            $"{respirationTracker._standardDeviationRest2min}",
            $"{respirationTracker._absorptionRespirationRateMultiplier1min}",
            $"{respirationTracker._absorptionRespirationRateMultiplier2min}",
            $"{respirationTracker._absorptionToneLengthMultiplier1min}",
            $"{respirationTracker._absorptionToneLengthMultiplier2min}"
        );
        string encryptedData = EncryptionHelper.Encrypt(data);
        combinedData += encryptedData + " "; // Append encrypted data with a space as a separator
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
        if(Input.GetKeyDown(KeyCode.Space))
        {
            writeCSV();
        }
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
                        string encryptedcontrolStatus = statusParts[0].Trim();
                        string controlStatus = EncryptionHelper.Decrypt(encryptedcontrolStatus);
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
                            //writeCSV();
                            gameManagement.EndGame();
                        }
                    }
                }
            }
        }
    }

    void LogMessage(string message)
    {
        string data = string.Join(";",
            $"{Time.time}",
            message
        );

        string encryptedData = EncryptionHelper.Encrypt(data);
        combinedData += encryptedData + " "; // Append encrypted data with a space as a separator
    }

    public void writeCSV()
    {
        string sessionsFolder = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}");
        Directory.CreateDirectory(sessionsFolder); // Ensure session folder exists
        session_resultsPath = Path.Combine(sessionsFolder, "session_results.csv");
        Debug.Log(baseSessionsFolderPath + session_resultsPath);

        Debug.Log("Encrypted Data: " + combinedData);

        string[] encryptedEntries = combinedData.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Debug.Log("Decrypted Data: ");
        foreach (string encryptedEntry in encryptedEntries)
        {
            string decryptedEntry = EncryptionHelper.Decrypt(encryptedEntry);
            Debug.Log(decryptedEntry);
        }

        using (TextWriter tw = new StreamWriter(session_resultsPath, true))
        {   
            // Write the encrypted data on the first line
            tw.WriteLine(combinedData);
        }

        combinedData = ""; // Clear the combined data after writing to CSV
        
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
            tw.WriteLine(combinedData); // Write all combined data in one cell
        }
        combinedData = ""; // Clear the combined data after writing to CSV
    }
}
