using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // For scene loading
using System.IO;
using System;

public class InitializationManager : MonoBehaviour
{
    public static string GameMode;
    public static string SubGameMode;
    public static int currentSessionNumber = 0;
    private string baseSessionsFolderPath = "";
    public string encryptedReadyCheck;
    public string decryptedReadyCheck;
    private string encryptedSessionNumber;
    [SerializeField] private string encryptedGameMode;
    [SerializeField] private string encryptedSubGameMode;
    [SerializeField] private string decryptedGameMode;
    [SerializeField] private string decryptedSubGameMode;
    
    
    void Start()
    {
        #if UNITY_STANDALONE_OSX
            string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            baseSessionsFolderPath = System.IO.Path.Combine(userFolder, "Hummingbird");
            Debug.Log("Base sessions folder path: " + baseSessionsFolderPath);
        #elif UNITY_STANDALONE_WIN
            baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
        #else
            Debug.LogError("Unsupported platform");
            return;
        #endif

        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists
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
                        encryptedReadyCheck = columns[1].Trim();
                        decryptedReadyCheck = EncryptionHelper.Decrypt(encryptedReadyCheck);
                        if(decryptedReadyCheck == "ready")
                        {
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

        ReadSessionParams();

        // Load Preparation Scene if game mode is set to "preparation"
        if (GameMode == "Preparation")
        {
            Debug.Log("Attempting to load scene: " + GameMode);
            SceneManager.LoadScene("PreperationSession");
        }
        else if (GameMode == "Integration")
        {
            Debug.Log("Attempting to load scene: " + GameMode);
            SceneManager.LoadScene("IntegrationSession");
        } else if (GameMode == "Passive")
        {
            Debug.Log("Attempting to load scene: " + GameMode);
            SceneManager.LoadScene("PassiveSession");
        } else if (GameMode == "Wisdom")
        {
            Debug.Log("Attempting to load scene: " + GameMode);
            SceneManager.LoadScene("WisdomSession");
        } else if (GameMode == "Adjunctive")
        {
            Debug.Log("Attempting to load scene: " + GameMode);
            SceneManager.LoadScene("AdjunctiveSession");
        }
    }

    void ReadSessionParams()
    {
        if(currentSessionNumber != 0)
        {
            string sessionsParams = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}", "session_params.csv");
            if(File.Exists(sessionsParams))
            {
                string[] data = File.ReadAllText(sessionsParams).Split(new string[] {",","\n"}, StringSplitOptions.None);
                encryptedGameMode = data[0];
                encryptedSubGameMode = data[1];
                decryptedGameMode = EncryptionHelper.Decrypt(encryptedGameMode);
                decryptedSubGameMode = EncryptionHelper.Decrypt(encryptedSubGameMode);
                GameMode = decryptedGameMode;
                SubGameMode = decryptedSubGameMode;
            }
            else 
            {
                Debug.LogError("CSV file not found at: " + sessionsParams);
            }
        }
    }
}
