using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // For scene loading
using System.IO;
using System;
using SoundSelf.Sequence;

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

    [SerializeField] private SequenceDefinition protocolStacksAscendingDefinition;
    [SerializeField] private SequenceDefinition protocolStacksDescendingDefinition;

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
        VOInitializations();
        TimeLeftInitializations();
    }

    /// <summary>Returns the SequenceDefinition for Protocol Stacks based on subGameMode. Single source of truth — assign definitions here only.</summary>
    public SequenceDefinition GetSequenceDefinitionForProtocolStacks()
    {
        if (gameMode != "Protocol Stacks") return null;
        return subGameMode == "Descending" ? protocolStacksDescendingDefinition : protocolStacksAscendingDefinition;
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
                //gameMode = decryptedGameMode;
                //subGameMode = decryptedSubGameMode;
                gameMode = "Protocol Stacks"; //TODO: REMOVE THIS AFTER TESTING PROTOCOL STACKS
                subGameMode = "Ascending";
                
                firstTimeUserString = decryptedFirstTimeUser;
            
            }
            else 
            {
                Debug.LogError("CSV file not found at: " + sessionsParams);
            }
        }
        else
        {
            Debug.LogError("CSVLoader: No session number found");
        }
        Debug.Log("CSVLoader: modes set to: Game Mode(" + GetCurrentMode() + ") Sub Mode(" + GetCurrentSubMode() + ")");

    }

    private void VOInitializations()
    {
        if (wwiseVOManager == null)
        {
            Debug.LogError("CSVLoader: VOInitializations() - wwiseVOManager is null! Cannot initialize VO.");
            return;
        }

        //GAME MODES
        if (gameMode == "Preperation" || gameMode == "Skills Training")
        {
            if (subGameMode == "Peace" || subGameMode == "Mindfulness and Joy")
            {
                wwiseVOManager.SetToPeace();
            }
            else if (subGameMode == "Narrative" || subGameMode == "Psychological Flexibility")
            {
                wwiseVOManager.SetToNarrative();
            }
            else if (subGameMode == "Surrender" || subGameMode == "Psychedelic Preparation")
            {
                wwiseVOManager.SetToSurrender();
            }
            else
            {
                Debug.LogWarning("CSVLoader: VOInitializations() - Unknown subGameMode '" + subGameMode + "' for gameMode '" + gameMode + "'. VO content not set.");
            }

            if (firstTimeUserString == "First Time User")
            {
                wwiseVOManager.firstTimeUser();
            }
            else
            {
                wwiseVOManager.notFirstTimeUser();
            }
        }
        else if (gameMode == "Integration")
        {
            wwiseVOManager.notFirstTimeUser();
            
            if (subGameMode == "Fireflies" || subGameMode == "Self Compassion")
            {
                wwiseVOManager.SetToFireflies();
            }
            else if (subGameMode == "Kindness" || subGameMode == "Loving Kindness")
            {
                wwiseVOManager.SetToKindness();
            }
            else if (subGameMode == "Metta" || subGameMode == "Transitions")
            {
                wwiseVOManager.SetToMetta();
            }
            else
            {
                Debug.LogWarning("CSVLoader: VOInitializations() - Unknown subGameMode '" + subGameMode + "' for gameMode '" + gameMode + "'. VO content not set.");
            }
        } 
        else if (gameMode == "Protocol Stacks")
        {
            wwiseVOManager.notFirstTimeUser();

            if(subGameMode == "Ascending")
            {
                wwiseVOManager.SetToEsketamineAscending();
            }
            else if(subGameMode == "Descending")
            {
                wwiseVOManager.SetToEsketamineDescending();
            }
            else
            {
                Debug.LogWarning("CSVLoader: VOInitializations() - Unknown subGameMode '" + subGameMode + "' for gameMode '" + gameMode + "'. VO content not set.");
            }
        } 
        else if (gameMode == "Quick Dive")
        {
            wwiseVOManager.notFirstTimeUser();
        }
        else
        {
            Debug.LogWarning("CSVLoader: VOInitializations() - Unknown gameMode '" + gameMode + "'. No VO initialization performed.");
        }
    }

    private void TimeLeftInitializations()
    {
        if (sequencer == null)
        {
            Debug.LogError("CSVLoader: TimeLeftInitializations() - sequencer is null! Cannot set countdown.");
            return;
        }
        
        if(TimeLeftScript.instance == null)
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - TimeLeftScript.instance is null. Timing behaviors will not work properly.");
            return;
        }
        
        // Set TimeLeft and totalTimeOfPostUnguidedVocalizationContent based on game mode
        if (gameMode == "Preperation" || gameMode == "Skills Training")
        {
            TimeLeftScript.instance.SetTimeLeftSeconds(2400.0f); // 40 minutes
            
            if (subGameMode == "Peace" || subGameMode == "Mindfulness and Joy")
            {
                totalTimeOfPostUnguidedVocalizationContent = (14.0f * 60.0f) + 0.0f; //9 min 49 seconds //July 7 2025, added 11 seconds
                //Add 4mins to cut unguided 
            }
            else if (subGameMode == "Narrative" || subGameMode == "Psychological Flexibility")
            {
                totalTimeOfPostUnguidedVocalizationContent = 900.0f; //15 minutes
                //totalTimeOfPostUnguidedVocalizationContent = (11.0f * 60.0f) + 33.0f; //7 min 33 seconds //July 7 2025, added 11 seconds
                // // Added 4 mins to cut unguided 
            }
            else if (subGameMode == "Surrender" || subGameMode == "Psychedelic Preparation")
            {
                totalTimeOfPostUnguidedVocalizationContent = (12.0f * 60.0f) + 06.0f; // 12 minutes 6 seconds
            }
            else
            {
                Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Unknown subGameMode '" + subGameMode + "' for gameMode '" + gameMode + "'. totalTimeOfPostUnguidedVocalizationContent not set.");
            }
        }
        else if (gameMode == "Integration")
        {
            TimeLeftScript.instance.SetTimeLeftSeconds(1500.0f); // 25 minutes
            
            if (subGameMode == "Fireflies" || subGameMode == "Self Compassion")
            {
                totalTimeOfPostUnguidedVocalizationContent = 415.0f;
            }
            else if (subGameMode == "Kindness" || subGameMode == "Loving Kindness")
            {
                totalTimeOfPostUnguidedVocalizationContent = 349.0f;
            }
            else if (subGameMode == "Metta" || subGameMode == "Transitions")
            {
                totalTimeOfPostUnguidedVocalizationContent = 597.0f;
            }
            else
            {
                Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Unknown subGameMode '" + subGameMode + "' for gameMode '" + gameMode + "'. totalTimeOfPostUnguidedVocalizationContent not set.");
            }
        } 
        else if (gameMode == "Protocol Stacks")
        {
            TimeLeftScript.instance.SetTimeLeftSeconds(2400.0f); // 40 minutes

            if(subGameMode == "Ascending" || subGameMode == "Descending")
            {
                totalTimeOfPostUnguidedVocalizationContent = 900.0f; // 15 minutes
            }
            else
            {
                Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Unknown subGameMode '" + subGameMode + "' for gameMode '" + gameMode + "'. totalTimeOfPostUnguidedVocalizationContent not set.");
            }
        } 
        else if (gameMode == "Quick Dive")
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Quick Dive mode does not set TimeLeft or totalTimeOfPostUnguidedVocalizationContent.");
        }
        else
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Unknown gameMode '" + gameMode + "'. No TimeLeft initialization performed.");
        }

        // Set countdown after TimeLeft and totalTimeOfPostUnguidedVocalizationContent are set
        float timeLeft = TimeLeftScript.instance._timeLeft;
        float calculatedCountdown = timeLeft - totalTimeOfPostUnguidedVocalizationContent;
        
        if (timeLeft <= 0f)
        {
            Debug.LogError("CSVLoader: TimeLeftInitializations() - timeLeft is " + timeLeft + " (should be > 0). Countdown will not be set.");
            return;
        }
        
        if (totalTimeOfPostUnguidedVocalizationContent <= 0f)
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - totalTimeOfPostUnguidedVocalizationContent is " + totalTimeOfPostUnguidedVocalizationContent + " (should be > 0). Countdown calculation may be incorrect.");
        }
        
        if (calculatedCountdown <= 0f)
        {
            Debug.LogError("CSVLoader: TimeLeftInitializations() - Calculated countdown is " + calculatedCountdown + " (should be > 0). This will cause ProtocolStacksCoroutine to hang!");
        }
        
        sequencer.SetCountdownToSavasana(calculatedCountdown);
        
        Debug.Log("CSVLoader: TimeLeftInitializations() - countdownToSavasana set to " + sequencer._countdownToSavasana + " seconds (" + (sequencer._countdownToSavasana / 60f) + " minutes). timeLeft=" + timeLeft + ", totalTimeOfPostUnguidedVocalizationContent=" + totalTimeOfPostUnguidedVocalizationContent);
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
