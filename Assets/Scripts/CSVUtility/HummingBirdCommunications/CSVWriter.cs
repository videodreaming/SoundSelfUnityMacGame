using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class CSVWriter : MonoBehaviour
{
    public int currentSessionNumber;
    private string baseSessionsFolderPath = "";
    private string combinedData = "";
    private string session_resultsPath = "";
    public RespirationTracker respirationTracker;
    public GameManagement gameManagement;
    public string GameMode;
    public string SubGameMode;
    public string encryptedstatus = "";
    public string decryptedstatus = "";
    public bool CSVDevMode = false;

    void Start()
    {
        #if UNITY_STANDALONE_OSX
            string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            baseSessionsFolderPath = System.IO.Path.Combine(userFolder, "Hummingbird");
        #elif UNITY_STANDALONE_WIN
            baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
        #else
            Debug.LogError("Unsupported platform");
            return;
        #endif

        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists
        currentSessionNumber = InitializationManager.currentSessionNumber; // Get session number from InitializationManager
        GameMode = InitializationManager.GameMode;
        SubGameMode = InitializationManager.SubGameMode;
    }
    
    void Update()
    {
        GetStatus();
        if(decryptedstatus == "paused")
        {
            
        } else if (decryptedstatus == "terminated")
        {
            writeCSV();
            gameManagement.EndGame();
        } else if (decryptedstatus == "resumed")
        {
            GetData();
        } else if (decryptedstatus == "ready")
        {
            //Debug.Log("Ready");      
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

    public void writeCSV()
    {
        string sessionsFolder = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}");
        Directory.CreateDirectory(sessionsFolder); // Ensure session folder exists
        session_resultsPath = Path.Combine(sessionsFolder, "session_results.csv");

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
            tw.WriteLine(combinedData);
        }
        combinedData = ""; // Clear the combined data after writing to CSV
    }
}
