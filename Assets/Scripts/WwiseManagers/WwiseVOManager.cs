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
    public CSVWriter CSVWriter;

    public User userObject;
    public AudioSource userAudioSource;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public MusicSystem1 musicSystem1;
    
    public float fadeDuration = 54.0f;
    public float targetValue = 80.0f;
    public RTPC silentrtpcvolume;
    public RTPC toningrtpcvolume;
    public RTPC silentFundamentalrtpcvolume;
    public RTPC toningFundamentalrtpcvolume;
    public RTPC silentHarmonyrtpcvolume;
    public RTPC toningHarmonyrtpcvolume;
    public bool firstTimeUser = true;
    public bool layingDown = true;
    
    [SerializeField]
    public bool interactive = false;

    public CSVWriter csvWriter;

    private bool silentPlaying = false;
    private bool previousToneActiveConfident = false;


    void Start()
    {
        if(userObject != null)
        {
            userAudioSource = userObject.GetComponent<AudioSource>();
            userAudioSource.volume = 0.0f;
        }
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Peace", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicContent","Peace", gameObject);
        assignVOs();
        if(firstTimeUser)
        {
            //AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject,(uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);
            AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);  
            AkSoundEngine.SetSwitch("VO_Somatic","Long",gameObject);
        } else {
            //AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_SHORT", gameObject);
        }

    }
    

    void OpeningCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
        // Posture_Start
        // orientation_Start
        // thematicOpening_Start
        // Cue_ThematicOpening_End
        // Cue_VoiceElicitation1_Start
        // Cue_Microphone_ON
        // Cue_BreathIn_Start
        // Cue_Sigh_Start
        // Cue_Microphone_OFF
        // Cue_VoiceElicitation1_End
        // Cue_Somatic_Start
        // Cue_Microphone_ON
        //Cue_BreathIN_start
        // BreatheOut_Start
        // Linear 1 2 and 3
        // Cue_InteractiveMusicSystem_Start
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
                }
                else if(musicSyncInfo.userCueName == "Cue_Microphone_ON")
                {
                    csvWriter.microphoneMonitoring = true;
                    userAudioSource.volume = 1.0f;
                    Debug.Log("Cue Mic On");
                } else if(musicSyncInfo.userCueName == "Cue_Somatic_Start")
                {
                    Debug.Log("Somatic Start");
                }
                 else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
                {
                    //Robin to do AVS Stuff here
                    Debug.Log("Cue BreathIn Start");
                } else if(musicSyncInfo.userCueName == "Cue_Orientation_Start")
                {
                    Debug.Log("Cue Orientation Start");
                } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
                {
                    //Robin to do AVS Stuff here
                    Debug.Log("Cue Sigh Start");
                } else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
                {
                    //Robin to do AVS Stuff heres
                    Debug.Log("Cue BreatheInStart");
                } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
                {
                    //Robin to do AVS Stuff here
                    Debug.Log("Cue Sigh Start");
                } else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
                {
                    //Robin to do AVS Stuff here
                    csvWriter.microphoneMonitoring = false;
                    userAudioSource.volume = 0.0f;
                     Debug.Log("Cue Mic OFF");
                } else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_End")
                {
                    Debug.Log("PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
                } else if (musicSyncInfo.userCueName == "Cue_LinearHum1_Start")
                {

                } else if (musicSyncInfo.userCueName == "Cue_LinearHum2_Start")
                {

                } else if (musicSyncInfo.userCueName == "Cue_LinearHum3_Start")
                {

                } else if (musicSyncInfo.userCueName == "Cue_InteractiveMusicSystem_Start")
                {
                    StartCoroutine (InteractiveMusicSystemFade());
                    interactive = true;
                    AkSoundEngine.PostEvent("Play_SilentLoops_v3_FundamentalOnly",gameObject);
                    AkSoundEngine.PostEvent("Play_SilentLoops_v3_HarmonyOnly",gameObject);
                } 
            }   
    }
    
    private IEnumerator InteractiveMusicSystemFade()
    {
        float initialValue = 0.0f;
        float startTime = Time.time;

        while(Time.time - startTime <fadeDuration)
        {
            float elapsed = (Time.time - startTime)/fadeDuration;
            float currentValue = Mathf.Lerp(initialValue, targetValue, elapsed);
            silentrtpcvolume.SetGlobalValue(currentValue);
            toningrtpcvolume.SetGlobalValue(currentValue);
            yield return null;
        }
        silentrtpcvolume.SetGlobalValue(targetValue);
        toningrtpcvolume.SetGlobalValue(targetValue);
        yield break;
    }
    
    private float GetRTPCValue(RTPC rtpc)
    {
        uint rtpcID = AkSoundEngine.GetIDFromString(rtpc.Name);
        int valueType = 1; // AkRTPCValue_type type, 0 for game object, 1 for global RTPC
        float value;
        AkSoundEngine.GetRTPCValue(rtpcID, gameObject, 0, out value, ref valueType);
        return value;
    }
    
    public void fundamentalChanged(string FundamentalnoteReceived, string HarmonynoteRecieved)
    {
        Debug.Log("Fundamental Note = " + FundamentalnoteReceived);
        Debug.Log("Harmony Note = " + HarmonynoteRecieved);
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", FundamentalnoteReceived, gameObject);
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", HarmonynoteRecieved, gameObject);
    }
    void Update()
    {
        checkInteractive();
       
    }
    void checkInteractive()
    {
         if(interactive == true)
        {
            if(silentPlaying == false)
            {
                // silentrtpcvolume.SetGlobalValue(targetValue);
                // toningrtpcvolume.SetGlobalValue(targetValue);
                // AkSoundEngine.PostEvent("Play_SilentLoops_v3_FundamentalOnly",gameObject);
                // AkSoundEngine.PostEvent("Play_SilentLoops_v3_HarmonyOnly",gameObject);
                // silentPlaying = true;
            }
            bool currentToneActiveConfident = imitoneVoiceIntepreter.toneActiveConfident;
            if(currentToneActiveConfident && !previousToneActiveConfident)
            {
                Debug.Log("Playing Toning");
                AkSoundEngine.PostEvent("Play_Toning_v3_FundamentalOnly",gameObject);
                AkSoundEngine.PostEvent("Play_Toning_v3_HarmonyOnly",gameObject);
            } else if (!currentToneActiveConfident && previousToneActiveConfident)
            {
                AkSoundEngine.PostEvent("Stop_Toning",gameObject);
            }
            previousToneActiveConfident = currentToneActiveConfident;
        }
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
    public void PassBackToVOManager()
    {
        Debug.Log("RanFinalStageLogic");
       
        AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject);
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

