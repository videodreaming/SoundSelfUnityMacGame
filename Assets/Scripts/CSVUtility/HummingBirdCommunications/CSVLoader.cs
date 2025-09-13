using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // For scene loading
using System.IO;
using System;

public class CSVLoader : MonoBehaviour
{
    public Sequencer sequencer;
    
    public WwiseVOManager wwiseVOManager;
    public DevelopmentMode developmentMode;
    public TimeLeftScript timeLeftScript;
    public static string gameMode {get; private set;}
    public static string subGameMode {get; private set;}
    public float timeToPlayClosingGoodbye;
    public float totalTimeOfPostUnguidedVocalizationContent;

    public bool firstTimeUser {get; private set;} = true;
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



    void Awake()
    {
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
            timeLeftScript.SetTimeLeftSeconds(2400.0f);
            Debug.Log("CSVLoader: Setting up for Preperation or Skills Training");
            if (subGameMode == "Peace" || subGameMode == "Mindfulness and Joy")
            {
                Debug.Log("CSVLoader: Mindfulness and Joy");
                totalTimeOfPostUnguidedVocalizationContent = (11.0f * 60.0f) + 11.0f; 
                wwiseVOManager.SetToPeace();
            }
            else if (subGameMode == "Narrative" || subGameMode == "Psychological Flexibility")
            {
                Debug.Log("CSVLoader: Psychological Flexibility");
                totalTimeOfPostUnguidedVocalizationContent = (8.0f * 60.0f) + 44.0f;
                wwiseVOManager.SetToNarrative();
            }
            else if (subGameMode == "Surrender" || subGameMode == "Psychedelic Preparation")
            {
                Debug.Log("CSVLoader: Surrender Training");
                totalTimeOfPostUnguidedVocalizationContent = (9.0f * 60.0f) + 17.0f;
                wwiseVOManager.SetToSurrender();
            }

            if (firstTimeUser)
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
            timeLeftScript.SetTimeLeftSeconds(1500.0f);
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
        }
        sequencer.SetCountdownToSavasana(timeLeftScript._timeLeft - totalTimeOfPostUnguidedVocalizationContent);
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
                if(encryptedGameMode == "Set Levels")
                {
                    Debug.Log("Encrypted Game Mode: " + encryptedGameMode);
                    if(encryptedSubGameMode == "Set Levels")
                    {
                        Debug.Log("Encrypted Sub Game Mode: " + encryptedSubGameMode);
                        developmentMode.configureMode = true;
                        developmentMode.startInTutorial = true;
                        developmentMode.startAtStart = false;
                    }
                } else
                {
                    Debug.Log("Encrypted Game Mode: " + encryptedGameMode);
                    Debug.Log("Encrypted Sub Game Mode: " + encryptedSubGameMode);
                    decryptedGameMode = EncryptionHelper.Decrypt(encryptedGameMode);
                    decryptedSubGameMode = EncryptionHelper.Decrypt(encryptedSubGameMode);
                    gameMode = decryptedGameMode;
                    subGameMode = decryptedSubGameMode;
                }
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
}
