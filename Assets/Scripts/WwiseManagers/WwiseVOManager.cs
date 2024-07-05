using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;



public class WwiseVOManager : MonoBehaviour
{
    public AudioManager audioManager;

    private bool pause = true;

    public User userObject;
    public AudioSource userAudioSource;
    

    public string ThematicContent = null;
    public string ThematicSavasana = null;
    public string VO_ClosingGoodbye = null;
    public bool firstTimeUser = true;
    public bool layingDown = true;


    public CSVWriter csvWriter;


    void Awake()
    {

    }


    void Start()
    {
        if(userObject != null)
        {
            userAudioSource = userObject.GetComponent<AudioSource>();
            userAudioSource.volume = 0.0f;
        }

        AkSoundEngine.SetSwitch("VO_ThematicContent","Peace", gameObject);
        assignVOs();
        if(firstTimeUser)
        {
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);  
        } else {
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_SHORT", gameObject);
        }

    }
    

    void OpeningCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
         if (in_type == AkCallbackType.AK_MusicSyncUserCue)
            {
                AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
                if (musicSyncInfo.userCueName == "Cue_Posture_Start")
                {
                    Debug.Log("Cue_Posture_Start");
                } else if (musicSyncInfo.userCueName == "Cue_ThematicOpening_Start")
                {
                    Debug.Log("Cue_ThematicOpening_Start");
                } else if(musicSyncInfo.userCueName == "Cue_VoiceElicitation1_Start")
                 {
                    Debug.Log("Stopping Openign Seq, play sigh Query Seq");
                    AkSoundEngine.PostEvent("Stop_OPENING_SEQUENCE",gameObject);
                }
                else if(musicSyncInfo.userCueName == "Cue_Microphone_ON")
                {
                    userAudioSource.volume = 1.0f;
                    Debug.Log("Cue Mic On");
                } else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
                {
                    Debug.Log("Cue BreathIn Start");
                } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
                {
                    Debug.Log("Cue Sigh Start");
                } else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
                {
                    Debug.Log("Cue BreatheInStart");
                } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
                {
                    Debug.Log("Cue Sigh Start");
                } else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
                {
                    userAudioSource.volume = 0.0f;
                     Debug.Log("Cue Mic OFF");
                } else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_End")
                {
                    Debug.Log("PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
                }
            }   
    }
    

    void ThematicCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if(in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
            // if(musicSyncInfo.userCueName == "Cue_VoiceElicitation1_Start")
            // {
            //     Debug.Log("Stopping Openign Seq, play sigh Query Seq");
            //     AkSoundEngine.PostEvent("Stop_OPENING_SEQUENCE",gameObject);
            //     AkSoundEngine.PostEvent("Play_SIGH_QUERY_SEQUENCE_1",gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, SighQueryFunction ,null);
            // }
        }
    }
    
    void SighQueryFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
         if(in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
            // if(musicSyncInfo.userCueName == "Cue_Microphone_ON")
            // {
            //     Debug.Log("Cue Mic On");
            // } else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
            // {
            //     Debug.Log("Cue BreathIn Start");
            // } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
            // {
            //     Debug.Log("Cue Sigh Start");
            // } else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
            // {
            //     Debug.Log("Cue BreatheInStart");
            // } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
            // {
            //     Debug.Log("Cue Sigh Start");
            // } else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
            // {
            //     Debug.Log("Cue Mic OFF");
            // } else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_End")
            // {
            //     Debug.Log("PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
            //     AkSoundEngine.PostEvent("Play_sfx_SoundSeed_BreathCycle",gameObject);
            //     AkSoundEngine.PostEvent("Play_SOMATIC_SEQUENCE",gameObject);
            // }
        }
    }
    void Update()
    {
      
    }

    
    void assignVOs()
    {
        //Set VO_Posture
        
        if(layingDown)
        {
            AkSoundEngine.SetSwitch("VO_Posture","LieDown",gameObject);
        } else 
        {
            AkSoundEngine.SetSwitch("VO_Posture","Relax",gameObject);
        }


        if(firstTimeUser == true)
        {
           // AkSoundEngine.SetSwitch("VO_Opening", "openingLong", gameObject);
            //AkSoundEngine.SetSwitch("VO_Somatic", "long", gameObject);
        } else if (firstTimeUser == false)
        {
           // AkSoundEngine.SetSwitch("VO_Opening", "openingShort", gameObject);
           // AkSoundEngine.SetSwitch("VO_Somatic", "short", gameObject);
        } else 
        {
            //AkSoundEngine.SetSwitch("VO_Opening", "openingPassive", gameObject);
            //AkSoundEngine.SetSwitch("VO_Somatic", "short", gameObject);
        }

        if(csvWriter.SubGameMode == "DieWell")
        {
            AkSoundEngine.SetSwitch("VO_ThematicContent", "DieWell", gameObject);
        } else if (csvWriter.SubGameMode == "Narrative")
        {
           AkSoundEngine.SetSwitch("VO_ThematicContent", "Narrative", gameObject);
        } else if (csvWriter.SubGameMode == "Peace")
        {
            AkSoundEngine.SetSwitch("VO_ThematicContent", "Peace", gameObject);
        } else if (csvWriter.SubGameMode == "Surrender")
        {
            AkSoundEngine.SetSwitch("VO_ThematicContent", "Surrender", gameObject);
        }
    
        if(csvWriter.GameMode == "Preperation")
        {

        } 
        if(csvWriter.GameMode == "Integration")
        {

        }
        if(csvWriter.GameMode == "Adjunctive")
        {

        }
    }



    void PlayNext()
    {


    }

        void MyCallbackFunction(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (in_type == AkCallbackType.AK_MusicSyncUserCue)
            {
                Debug.Log("cue hit");
                PlayNext();
            }   
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                Debug.Log("callback ran");
                audioManager.OnAudioFinished();
            }  
        }

        
            IEnumerator StartSighElicitationTimer()
            {
                yield return new WaitForSeconds(6.0f); // Wait for the audio event to finish playing
                audioManager.OnAudioFinished();
                pause = false;
            }
            IEnumerator StartQueryElicitationTimer()
            {
                pause = true;
                audioManager.Query1CheckStarted = true;
                yield return new WaitForSeconds(30.0f); // Wait for the audio event to finish playing
                audioManager.OnAudioFinished();
                pause = false;
            }
                    
        
}

