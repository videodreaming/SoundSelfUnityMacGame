using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class CSVWriter : MonoBehaviour
{
    public static string gameMode;
    public static string subGameMode;
    public int currentSessionNumber;
    private string baseSessionsFolderPath = "";
    private string combinedData = "";
    private string session_resultsPath = "";
    public RespirationTracker respirationTracker;
    public GameManagement gameManagement;
    public CSVLoader csvLoader;
    public string encryptedstatus = "";
    public string decryptedstatus = "";
    public bool CSVDevMode = false;

    private float dataLogTimer = 0f;
    private float dataLogInterval = 1f; // Log data every 


    void Start()
    {
        #if UNITY_STANDALONE_OSX
            string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            baseSessionsFolderPath = System.IO.Path.Combine(userFolder, "Appdata", "Roaming", "Hummingbird");
        #elif UNITY_STANDALONE_WIN
            baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
        #else
            Debug.LogError("Unsupported platform");
            return;
        #endif

        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists
        currentSessionNumber = CSVLoader.currentSessionNumber; // Get session number from InitializationManager
        Debug.Log("Current session number: " + currentSessionNumber);
        gameMode = CSVLoader.instance.gameMode;
        subGameMode = CSVLoader.instance.subGameMode;
    }
    
    void Update()
    {
        GetStatus();
        if(decryptedstatus == "paused")
        {
            Debug.Log("game paused");
        } else if (decryptedstatus == "terminated")
        {
            Debug.Log("Game Terminated");
            gameManagement.EndGame();
        } else if (decryptedstatus == "resumed"||decryptedstatus == "ready")
        {
            dataLogTimer += Time.deltaTime;
            if(dataLogTimer >= dataLogInterval)
            {
                GetData();
                dataLogTimer = 0f;
            }
        }
    }



    void GetStatus()
    {
        if(CSVDevMode)
        {
            string sessionsCsvPath = Path.Combine(baseSessionsFolderPath, "sessions.csv");
            Debug.Log(sessionsCsvPath);
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
                                string encryptedReadyCheck;
                                string decryptedReadyCheck;
                                encryptedReadyCheck = columns[1].Trim();
                                decryptedReadyCheck = EncryptionHelper.Decrypt(encryptedReadyCheck);
                                if(decryptedReadyCheck == "ready")
                                {
                                    string encryptedSessionNumber;
                                    encryptedSessionNumber = columns[0].Trim();
                                    if (int.TryParse(EncryptionHelper.Decrypt(encryptedSessionNumber), out int sessionNumber)) 
                                    {
                                        currentSessionNumber = sessionNumber;
                                        Debug.Log(currentSessionNumber);
                                        break;
                                    }
                                }
                            }
                        }
                    }
            }
        }
        string sessionsStatusPath = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}", "session_status.csv");
        if(File.Exists(sessionsStatusPath))
        {
            string[] data = File.ReadAllText(sessionsStatusPath).Split(new string[] {",","\n"}, StringSplitOptions.None);
            encryptedstatus = data[0];
            decryptedstatus = EncryptionHelper.Decrypt(encryptedstatus);
        }
    }

    void GetData()
    {
            string data = string.Join(";",
            $"{Time.time}",
            $"{RespirationTracker.instance._respirationRate}",
            $"{RespirationTracker.instance._meanToneLength}",
            $"{RespirationTracker.instance._meanRestLength}",
            $"{RespirationTracker.instance._respirationRate1min}",
            $"{RespirationTracker.instance._respirationRate2min}",
            $"{RespirationTracker.instance._respirationRateRaw1min}",
            $"{RespirationTracker.instance._respirationRateRaw2min}",
            $"{RespirationTracker.instance._meanToneLength1min}",
            $"{RespirationTracker.instance._meanToneLength2min}",
            $"{RespirationTracker.instance._meanRestLength1min}",
            $"{RespirationTracker.instance._meanRestLength2min}",
            $"{RespirationTracker.instance._absorption}",
            $"{RespirationTracker.instance._absorptionRaw}",
            $"{RespirationTracker.instance._standardDeviationTone1min}",
            $"{RespirationTracker.instance._standardDeviationTone2min}",
            $"{RespirationTracker.instance._standardDeviationRest1min}",
            $"{RespirationTracker.instance._standardDeviationRest2min}",
            $"{RespirationTracker.instance._absorptionRespirationRateMultiplier1min}",
            $"{RespirationTracker.instance._absorptionRespirationRateMultiplier2min}",
            $"{RespirationTracker.instance._absorptionToneLengthMultiplier1min}",
            $"{RespirationTracker.instance._absorptionToneLengthMultiplier2min}"
        );
        string encryptedData = EncryptionHelper.Encrypt(data);
        combinedData += encryptedData + " "; // Append encrypted data with a space as a separator
        dataLogTimer = 0f;
    }

    public void writeCSV()
    {
        string sessionsFolder = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}");
        Directory.CreateDirectory(sessionsFolder); // Ensure session folder exists
        session_resultsPath = Path.Combine(sessionsFolder, "session_results.csv");
        string[] encryptedEntries = combinedData.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string encryptedEntry in encryptedEntries)
        {
            string decryptedEntry = EncryptionHelper.Decrypt(encryptedEntry);
        }

        using (TextWriter tw = new StreamWriter(session_resultsPath, true))
        {
            tw.WriteLine(combinedData);
        }
        combinedData = ""; // Clear the combined data after writing to CSV
    }
}
