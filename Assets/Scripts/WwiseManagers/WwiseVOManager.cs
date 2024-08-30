using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;

//REFACTORING THOUGHTS FROM ROBIN
//WE SHOULD ENSURE THAT THE USERAUDIOSOURCE IS ONLY REFERENCED AND CONTROLLED FROM ONE SCRIPT
//WHAT IS CURRENTTLY "WWISEVOMANAGER", I.E. THIS SCRIPT, MAKES SENSE TO ME AS SOMETHING THAT CONTROLS
//EVERYTHING THAT HAPPENS ON A LINEAR TIMELINE, AND BE LIKE THE MASTER TIME-CONTROL.
//THERE ARE CERTAIN BEHAVIORS IN WWISEINTERACTIVEMUSICMANAGER THAT SHOULD REFERENCE THIS
//THAT CONTROL AVS BEHAVIORS. WOULD LIKE REEF'S HELP ON REFACTORING AND ORGANIZATION.

public class WwiseVOManager : MonoBehaviour
{
    public AudioManager audioManager;
    public WwiseInteractiveMusicManager wwiseInteractiveMusicManager;
    public DevelopmentMode  developmentMode;
    public CSVWriter CSVWriter;

    public LightControl lightControl;
    public User userObject;
    public AudioSource userAudioSource;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    //public MusicSystem1 musicSystem1;
    public RTPC silentrtpcvolume;
    public RTPC toningrtpcvolume;
    public RTPC silentFundamentalrtpcvolume;
    public RTPC toningFundamentalrtpcvolume;
    public RTPC silentHarmonyrtpcvolume;
    public RTPC toningHarmonyrtpcvolume;
    public float fadeDuration = 54.0f;
    public float targetValue = 80.0f;
    private bool debugAllowMusicLogs = true;
    private bool pause = true;
    public bool firstTimeUser = true;
    public bool layingDown = true;
    public bool muteInteraction = false;
    
    [SerializeField]
    public bool interactive = false;

    public CSVWriter csvWriter;

    //private bool silentPlaying = false;
    private bool previousToneActiveConfident = false;


    void Start()
    {
        
        if(developmentMode.developmentPlayground)
        {
            InteractiveMusicInitializations();
        }
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

        //NOTE FROM ROBIN ABOUT WWISE CUE HANDLING FUNCTIONALITY:
        //THIS IS WEIRD BEHAVIOR BUT IT WORKS AND HONESTLY I CAN'T MAKE SENSE OF IT
        //BUT WE ARE TAKING IT AS A BLESSING.
        //
        //BASICALLY, AS SOON AS WE RUN THE POSTEVENT THAT TAKES OPENINGCALLBACKFUNCTION AS A PARAMETER
        //WWISE SEEMS TO SEND CUES TO UNITY USING THE OPENINGCALLBACKFUNCITON
        //
        //FOR THIS REASON, WE THINK WE NEED TO BE CAUTIOUS ABOUT GAMEOBJECTS REFERENCED IN THE
        //AKSOUNDENGINE FUNCTIONS. WE THINK THEY ALL HAVE TO BE CALLED FROM THE SAME GAMEOBJECT
        //I.E. THE SAME SCRIPT
        //
    }

    void Update()
    {
    }
    
    public void handleTutorialMeditation(string tutorialToPlay)
    {
        AkSoundEngine.PostEvent(tutorialToPlay, gameObject,(uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
    }

    public void TutorialCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
         if (in_type == AkCallbackType.AK_MusicSyncUserCue)
            {
                Debug.Log("WWise_VO: Callback triggered: " + in_type);
                AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
                if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_Start");
                } else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_End");
                } else if (musicSyncInfo.userCueName == "Cue_BreathIn")
                {
                    breathInBehaviour();
                    Debug.Log("WWise_VO: Cue_BreathIn");
                }
            } 

    }
    private void breathInBehaviour()
    {
        //AVS + Audio stuff here
    }
    public void OpeningCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
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
                Debug.Log("WWise_VO: Callback triggered: " + in_type);
                AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
                if (musicSyncInfo.userCueName == "Cue_Posture_Start")
                {
                    Debug.Log("WWise_VO: Cue_Posture_Start");
                } else if (musicSyncInfo.userCueName == "Cue_ThematicOpening_Start")
                {
                    Debug.Log("WWise_VO: Cue_ThematicOpening_Start");
                } else if(musicSyncInfo.userCueName == "Cue_VoiceElicitation1_Start")
                 {
                    Debug.Log("WWise_VO: Stopping Openign Seq, play sigh Query Seq");
                }
                else if(musicSyncInfo.userCueName == "Cue_Microphone_ON")
                {
                    csvWriter.microphoneMonitoring = true;
                    userAudioSource.volume = 1.0f;
                    Debug.Log("WWise_VO: Cue Mic On");
                }
                else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
                {
                    //Robin to do AVS Stuff here
                    csvWriter.microphoneMonitoring = false;
                    userAudioSource.volume = 0.0f;
                     Debug.Log("WWise_VO: Cue Mic OFF");
                }
                else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_Start");
                } 
                else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_End");
                }
                else if(musicSyncInfo.userCueName == "Cue_Somatic_Start")
                {
                    Debug.Log("WWise_VO: Somatic Start");
                }
                 else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
                {
                    breathInBehaviour();
                    Debug.Log("WWise_VO: Cue BreathIn Start");
                } else if(musicSyncInfo.userCueName == "Cue_Orientation_Start")
                {
                    Debug.Log("WWise_VO: Cue Orientation Start");
                } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
                {
                    //Robin to do AVS Stuff here
                    Debug.Log("WWise_VO: Cue Sigh Start");
                }  else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_End")
                {
                    Debug.Log("WWise_VO: PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
                } else if (musicSyncInfo.userCueName == "Cue_LinearHum1_Start")
                {
                    Debug.Log("WWise_VO: Cue_LinearHum1_Start");
                    lightControl.SetPreferredColor("Red");
                    lightControl.NextColorWorld(5.0f);
                } else if (musicSyncInfo.userCueName == "Cue_LinearHum2_Start")
                {
                    Debug.Log("WWise_VO: Cue_LinearHum2_Start");
                } else if (musicSyncInfo.userCueName == "Cue_LinearHum3_Start")
                {
                    Debug.Log("WWise_VO: Cue_LinearHum3_Start");
                } else if (musicSyncInfo.userCueName == "Cue_InteractiveMusicSystem_Start")
                {
                    InteractiveMusicInitializations();
                } else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_Start");
                } else if (musicSyncInfo.userCueName == "Cue_Opening_Start")
                {
                    Debug.Log("WWise_VO: Cue_Opening_Start");
                }
            }   
    }
    
    private void InteractiveMusicInitializations()
    {
        Debug.Log("InteractiveMusicSystemFade is running");
        AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
        if(developmentMode.developmentPlayground)
        {
            silentrtpcvolume.SetGlobalValue(80.0f);
            toningrtpcvolume.SetGlobalValue(80.0f);
        }

        else if(developmentMode.developmentPlayground==false)
        {
            StartCoroutine(InteractiveMusicSystemFade());
        }

        interactive = true;
        AkSoundEngine.PostEvent("Play_SilentLoops_v3_FundamentalOnly",gameObject);
        AkSoundEngine.PostEvent("Play_SilentLoops_v3_HarmonyOnly",gameObject);
        AkSoundEngine.PostEvent("Play_AMBIENT_ENVIRONMENT_LOOP",gameObject);
    }

    private IEnumerator InteractiveMusicSystemFade()
    {

        //NOTE: this can be cleaned up by using the RTPC's transition time in ms, which Robin uses all the time (see lightControl)
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
        Debug.Log("WWise_VO: RanFinalStageLogic");
       
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

