using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // For scene loading
using System.IO;
using System;

public class CSVLoader : MonoBehaviour
{
    public static CSVLoader instance {get; private set;}
    public Sequencer sequencer;
    
    public WwiseVOManager wwiseVOManager;
    public string gameMode {get; private set;}
    public string subGameMode {get; private set;}
    public string firstTimeUserString {get; private set;}
    public float timeToPlayClosingGoodbye;
    public float totalTimeOfPostUnguidedVocalizationContent;

    private bool layingDown = true;
    public static int currentSessionNumber = 0;
    private string baseSessionsFolderPath = "";
    public string encryptedReadyCheck;
    public string decryptedReadyCheck;
    private string encryptedSessionNumber;
    [SerializeField] private string encryptedGameMode;
    [SerializeField] private string encryptedSubGameMode;
    [SerializeField] private string decryptedGameMode;
    [SerializeField] private string decryptedSubGameMode;
    [SerializeField] private string encryptedFirstTimeUser;
    [SerializeField] private string decryptedFirstTimeUser;




    void Awake()
    {
        // --- Singleton guard ---
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Optional: persist across scenes (remove if you want per-scene behavior)
        DontDestroyOnLoad(gameObject);

        //=======================================================================================================
        // READ SESSIONS.CSV TO GET CURRENT SESSION NUMBER
        //=======================================================================================================
    #if UNITY_STANDALONE_OSX
                string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                baseSessionsFolderPath = System.IO.Path.Combine(userFolder, "Appdata", "Roaming", "Hummingbird");
    #elif UNITY_STANDALONE_WIN
            baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
            Debug.Log("Base path: " + baseSessionsFolderPath);
    #else
                Debug.LogError("Unsupported platform");
                return;
    #endif

        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists
        string sessionsCsvPath = Path.Combine(baseSessionsFolderPath, "sessions.csv");

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
                        if (decryptedReadyCheck == "ready")
                        {
                            encryptedSessionNumber = columns[0].Trim();
                            if (int.TryParse(EncryptionHelper.Decrypt(encryptedSessionNumber), out int sessionNumber))
                            {
                                currentSessionNumber = sessionNumber;
                                break;
                            }
                        }
                    }
                }
            }
        }
        //OTHER VO INITIALIZATIONS

        ReadSessionParams();
        //=======================================================================================================
        // VO INITIALIZATION        
        //=======================================================================================================
        //GAME MODES
        if (gameMode == "Preperation" || gameMode == "Skills Training")
        {
            if(TimeLeftScript.instance != null)
            {
                TimeLeftScript.instance.SetTimeLeftSeconds(2400.0f);
            }
            Debug.Log("CSVLoader: Setting up for Preperation or Skills Training");
            if (subGameMode == "Peace" || subGameMode == "Mindfulness and Joy")
            {
                totalTimeOfPostUnguidedVocalizationContent = (14.0f * 60.0f) + 0.0f; //9 min 49 seconds //July 7 2025, added 11 seconds
                //Add 4mins to cut unguided 
                wwiseVOManager.SetToPeace();
            }
            else if (subGameMode == "Narrative" || subGameMode == "Psychological Flexibility")
            {
                Debug.Log("CSVLoader: Psychological Flexibility or Narrative");
                totalTimeOfPostUnguidedVocalizationContent = (11.0f * 60.0f) + 33.0f; //7 min 33 seconds //July 7 2025, added 11 seconds
                // // Added 4 mins to cut unguided 
                wwiseVOManager.SetToNarrative();
            }
            else if (subGameMode == "Surrender" || subGameMode == "Psychedelic Preparation")
            {
                totalTimeOfPostUnguidedVocalizationContent = (12.0f * 60.0f) + 06.0f; //8 min 6 seconds //July 7 2025, added 11 seconds
                // Added 4 mins for to cut unguided 
                wwiseVOManager.SetToSurrender();
            }

            if (firstTimeUserString == "First Time User")
            {
                wwiseVOManager.firstTimeUser();
                Debug.Log("CSVLoader: First Time User");
            }
            else
            {
                wwiseVOManager.notFirstTimeUser();
                Debug.Log("CSVLoader: Not First Time User");
            }
        }
        else if (gameMode == "Integration")
        {
            wwiseVOManager.notFirstTimeUser();
            Debug.Log("CSVLoader: Not First Time User");
            
            //sequencer.totalTimeOfExperience = 1500.0f;
            if(TimeLeftScript.instance != null)
            {
                TimeLeftScript.instance.SetTimeLeftSeconds(1500.0f);
            }
            if (subGameMode == "Fireflies" || subGameMode == "Self Compassion")
            {
                wwiseVOManager.SetToFireflies();
                totalTimeOfPostUnguidedVocalizationContent = 415.0f;
            }
            else if (subGameMode == "Kindness" || subGameMode == "Loving Kindness")
            {
                wwiseVOManager.SetToKindness();
                totalTimeOfPostUnguidedVocalizationContent = 349.0f;
            }
            else if (subGameMode == "Metta" || subGameMode == "Transitions")
            {
                wwiseVOManager.SetToMetta();
                totalTimeOfPostUnguidedVocalizationContent = 597.0f;
            }
        } else if (gameMode == "Protocol Stacks")
        {
            wwiseVOManager.notFirstTimeUser();
            Debug.Log(GetCurrentMode());
            if(TimeLeftScript.instance != null)
            {
                //TimeLeftScript.instance.SetTimeLeftSeconds(900.0f);
            }

            if(subGameMode == "Ascending")
            {
                wwiseVOManager.SetToEsketamineAscending();
            }
            else if(subGameMode == "Descending")
            {
                wwiseVOManager.SetToEsketamineDescending();
            }
        } else if (gameMode == "Quick Dive")
        {
            wwiseVOManager.notFirstTimeUser();
            Debug.Log(GetCurrentMode());
            if (TimeLeftScript.instance != null)
            {
                //TimeLeftScript.instance.SetTimeLeftSeconds(900.0f);
            }
        }
        
        if(TimeLeftScript.instance != null)
        {
            sequencer.SetCountdownToSavasana(TimeLeftScript.instance._timeLeft - totalTimeOfPostUnguidedVocalizationContent);
            sequencer.SetIntegrationEndTimer(TimeLeftScript.instance._timeLeft - 247.0f);
            
            Debug.Log("countdownToSavasana: " + sequencer._countdownToSavasana);
        }
        else
        {
            Debug.LogWarning("TimeLeftScript instance is null, timing behaviors will not work properly, and _countdownToSavasana and _integrationEnd will not be set.");
        }

    }

    void ReadSessionParams()
    {
        if(currentSessionNumber != 0)
        {
            string sessionsParams = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}", "session_params.csv");
            if(File.Exists(sessionsParams))
            {
                Debug.Log("CSV file found at: " + sessionsParams);
                string[] data = File.ReadAllText(sessionsParams).Split(new string[] {",","\n"}, StringSplitOptions.None);
                encryptedGameMode = data[0].Trim();
                encryptedSubGameMode = data[1].Trim();
                encryptedFirstTimeUser = data[5].Trim();
                
                Debug.Log("Encrypted Game Mode: " + encryptedGameMode);
                Debug.Log("Encrypted Sub Game Mode: " + encryptedSubGameMode);
                decryptedFirstTimeUser = EncryptionHelper.Decrypt(encryptedFirstTimeUser);
                decryptedGameMode = EncryptionHelper.Decrypt(encryptedGameMode);
                decryptedSubGameMode = EncryptionHelper.Decrypt(encryptedSubGameMode);
                gameMode = decryptedGameMode;
                subGameMode = decryptedSubGameMode;
                firstTimeUserString = decryptedFirstTimeUser;
            
            }
            else 
            {
                Debug.LogError("CSV file not found at: " + sessionsParams);
            }
        }
    }

    public string GetCurrentMode()
    {
        return gameMode;
    }

    public string GetCurrentSubMode()
    {
        return subGameMode;
    }

    public string GetDecryptedFirstTimeUser()
    {
        return firstTimeUserString;
    }
}
