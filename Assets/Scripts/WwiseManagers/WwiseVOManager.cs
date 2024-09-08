using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;

//REFACTORING THOUGHTS FROM ROBIN
//WE SHOULD ENSURE THAT THE USERAUDIOSOURCE IS ONLY REFERENCED AND CONTROLLED FROM ONE SCRIPT

public class WwiseVOManager : MonoBehaviour
{
    public AudioManager audioManager;
    public Sequencer sequencer;
    public DevelopmentMode  developmentMode;
    public Director director;
    public CSVWriter CSVWriter;
    public LightControl lightControl;
    public MusicSystem1 musicSystem1;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    //public MusicSystem1 musicSystem1;
    //public RTPC silentFundamentalrtpcvolume;
    //public RTPC toningFundamentalrtpcvolume;
    //public RTPC silentHarmonyrtpcvolume;
    //public RTPC toningHarmonyrtpcvolume;
    //public float fadeDuration = 54.0f;
    //public float targetValue = 80.0f;
    private bool debugAllowMusicLogs = true;
    private bool pause = true;
    public bool firstTimeUser = true;
    public bool layingDown = true;
    public CSVWriter csvWriter;

    //private bool silentPlaying = false;

    void Start()
    {
        //SOME IMPORTANT STARTUP BEHAVIORS ARE IN SEQUENCER.CS
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Peace", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicContent","Peace", gameObject);
        assignVOs();
        
        if(developmentMode.developmentPlayground)
        {
            musicSystem1.InteractiveMusicInitializations();
            musicSystem1.LockToC(false);
            imitoneVoiceIntepreter.gameOn = true;
            director.disable = false;
            InitializeLights();
        }
        if(!developmentMode.developmentPlayground)
        {
            musicSystem1.LockToC(true);
            imitoneVoiceIntepreter.gameOn = false;
            director.disable = true;
            if(firstTimeUser)
            {
                //AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject,(uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);
                AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);  
                AkSoundEngine.SetSwitch("VO_Somatic","Long",gameObject);
            } else {
                //AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_SHORT", gameObject);
            }
        }
        //NOTE ABOUT WWISE:
        //THE GAMEOBJECT POINTS TO *THIS* GAMEOBJECT. SO WE CAN'T START
        //IT FROM ONE GAMEOBJECT AND THEN STOP IT FROM ANOTHER. IT HAS TO BE THE SAME GAMEOBJECT
    }

    void Update()
    {
        if(developmentMode.developmentMode)
        {
            //toggle gameOn with the "G" button
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (imitoneVoiceIntepreter.gameOn)
                {
                    imitoneVoiceIntepreter.gameOn = false;
                }
                else
                {
                    imitoneVoiceIntepreter.gameOn = true;
                }
            }
        }

        //when I press "n", run MakeWWiseTone()
        if (Input.GetKeyDown(KeyCode.N))
        {
            StartCoroutine(MakeWWiseTone());
        }
    }
    
    public void handleTutorialMeditation(string tutorialToPlay)
    {
        AkSoundEngine.PostEvent(tutorialToPlay, gameObject,(uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
    }

    public void TutorialCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
         if (in_type == AkCallbackType.AK_MusicSyncUserCue)
            {
                AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
                if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_Start");
                    imitoneVoiceIntepreter.gameOn = false;
                } else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_End");
                    imitoneVoiceIntepreter.gameOn = true;
                } else if (musicSyncInfo.userCueName == "Cue_BreathIn")
                {
                    Debug.Log("WWise_VO: Cue_BreathIn");
                    breathInBehaviour();
                }
                else
                {
                    Debug.LogWarning("WWise_VO: Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
                }
            } 

    }
    public void OpeningCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
            // NOT-YET INTEGRATED ONES
            // BreatheOut_Start
            // Cue_ThematicOpening_End

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
                    Debug.Log("WWise_VO: Cue Mic On");
                    imitoneVoiceIntepreter.gameOn = true;
                }
                else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
                {
                    Debug.Log("WWise_VO: Cue Mic OFF");
                    imitoneVoiceIntepreter.gameOn = false;
                }
                else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_Start");
                    imitoneVoiceIntepreter.gameOn = false;
                } 
                else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
                {
                    Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_End");
                    imitoneVoiceIntepreter.gameOn = true;
                }
                else if(musicSyncInfo.userCueName == "Cue_Somatic_Start")
                {
                    Debug.Log("WWise_VO: Somatic Start");
                }
                 else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
                {
                    Debug.Log("WWise_VO: Cue BreathIn Start");
                    breathInBehaviour();
                } else if(musicSyncInfo.userCueName == "Cue_Orientation_Start")
                {
                    Debug.Log("WWise_VO: Cue Orientation Start");
                } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
                {
                    Debug.Log("WWise_VO: Cue Sigh Start");
                }  else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_End")
                {
                    Debug.Log("WWise_VO: PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
                } else if (musicSyncInfo.userCueName == "Cue_LinearHum_Start")
                {
                    Debug.Log("WWise_VO: Cue_LinearHum_Start");
                    InitializeLights();
                    StartCoroutine(MakeWWiseTone());
                } else if (musicSyncInfo.userCueName == "Cue_InteractiveMusicSystem_Start")
                {
                    Debug.Log("WWise_VO: Cue_InteractiveMusicSystem_Start");
                    musicSystem1.InteractiveMusicInitializations();
                } else if (musicSyncInfo.userCueName == "Cue_Opening_Start")
                {
                    Debug.Log("WWise_VO: Cue_Opening_Start");
                } else if (musicSyncInfo.userCueName == "Cue_FreePlay")
                {
                    Debug.Log("WWise_VO: Cue_FreePlay");
                    musicSystem1.LockToC(false);
                    director.disable = false;
                }
                else
                {
                    Debug.LogWarning("WWise_VO: Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
                }
            }   
    }

    private IEnumerator MakeWWiseTone()
    {
        Debug.Log("WWise_VO: Triggering a False Tone in WWise");
        musicSystem1.PostTheToningEvents();
        float _t = 4f;
        while (_t > 0)
        {
            _t -= Time.deltaTime;
            yield return null;
        }
        Debug.Log("WWise_VO: Stopping a False Tone in WWise");
        musicSystem1.StopWwiseToning();
    }

    private void InitializeLights()
    {
        Debug.Log("WWise_VO: InitializeLights");
        lightControl.SetPreferredColor("Red");
        lightControl.NextPreferredColorWorld(5.0f);
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

    private void breathInBehaviour()
    {
        lightControl.FXWave(0.6f, 5f, 0.25f, true, true);
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

