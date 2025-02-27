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
            Debug.Log("Base sessions folder path for Loader: " + baseSessionsFolderPath);
        #elif UNITY_STANDALONE_WIN
            baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
        #else
            Debug.LogError("Unsupported platform");
            return;
        #endif

        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists
        string sessionsCsvPath = Path.Combine(baseSessionsFolderPath, "sessions.csv");
        Debug.Log("CSVSessionsPath : " +sessionsCsvPath);

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

        //=======================================================================================================
        // VO INITIALIZATION        
        //=======================================================================================================
        //GAME MODES
        if(gameMode == "Preperation" || gameMode == "Skills Training")
        {
            Debug.Log("CSVLoader: Setting up for Preperation or Skills Training");
            timeLeftScript.SetTimeLeftSeconds(2700.0f);
            if (subGameMode == "Peace" || subGameMode == "Mindfulness and Joy")
            {
                totalTimeOfPostUnguidedVocalizationContent = 889.0f;
                wwiseVOManager.SetToPeace();
            } 
            else if (subGameMode == "Narrative" || subGameMode == "Psychological Flexibility")
            {
                Debug.Log("CSVLoader: Psychological Flexibility or Narrative");
                totalTimeOfPostUnguidedVocalizationContent = 742.0f;
                wwiseVOManager.SetToNarrative();
            } 
            else if (subGameMode == "Surrender" || subGameMode == "Psychedelic Prepeation")
            {
                totalTimeOfPostUnguidedVocalizationContent = 775.0f;
                wwiseVOManager.SetToSurrender();
            } 

            if(firstTimeUser)
            {
                wwiseVOManager.firstTimeUser();
                Debug.Log("CSVLoader: First Time User");
            } else {
                wwiseVOManager.notFirstTimeUser();
                Debug.Log("CSVLoader: Not First Time User");
            }
        } else if (gameMode == "Integration")
        {
            //sequencer.totalTimeOfExperience = 1500.0f;
            timeLeftScript.SetTimeLeftSeconds(1500.0f);
            if(subGameMode == "Fireflies")
            {
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Fireflies", gameObject);
                totalTimeOfPostUnguidedVocalizationContent = 415.0f;
            } else if (subGameMode == "Kindness")
            {
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Kindness", gameObject);
                totalTimeOfPostUnguidedVocalizationContent = 349.0f;
            } else if (subGameMode == "Metta")
            {
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Metta", gameObject);
                totalTimeOfPostUnguidedVocalizationContent = 597.0f;
            }
        }
        
        //SET CORRECT WAKEUP TIMER
        sequencer._wakeUpCounter = timeLeftScript._timeLeft - totalTimeOfPostUnguidedVocalizationContent;
        Debug.Log("CSVLoader: Wakeup Counter set to " + sequencer._wakeUpCounter + " seconds");
        
        //OTHER VO INITIALIZATIONS
        if(layingDown)
        {
            AkSoundEngine.SetSwitch("VO_Posture","LieDown",gameObject);
        } else 
        {
            AkSoundEngine.SetSwitch("VO_Posture","Relax",gameObject);
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
                encryptedGameMode = data[0].Trim();
                encryptedSubGameMode = data[1].Trim();
                decryptedGameMode = EncryptionHelper.Decrypt(encryptedGameMode);
                decryptedSubGameMode = EncryptionHelper.Decrypt(encryptedSubGameMode);
                gameMode = decryptedGameMode;
                subGameMode = decryptedSubGameMode;
            }
            else 
            {
                Debug.LogError("CSV file not found at: " + sessionsParams);
            }
        }
    }
}
