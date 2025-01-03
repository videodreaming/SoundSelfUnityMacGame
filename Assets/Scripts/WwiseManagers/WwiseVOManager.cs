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
    public CSVLoader csvLoader;
    public Sequencer sequencer;
    public DevelopmentMode  developmentMode;
    public Director director;
    public CSVWriter CSVWriter;
    public LightControl lightControl;
    public MusicSystem1 musicSystem1;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public Tutorial tutorial;
    //public MusicSystem1 musicSystem1;
    //public RTPC silentFundamentalrtpcvolume;
    //public RTPC toningFundamentalrtpcvolume;s
    //public RTPC silentHarmonyrtpcvolume;
    //public RTPC toningHarmonyrtpcvolume;
    //public float fadeDuration = 54.0f;
    //public float targetValue = 80.0f;
    private bool debugAllowMusicLogs = true;
    private bool pause = true;
    public bool firstTimeUser = true;
    public bool layingDown = true;
    private bool lightsInitialized = false;
    public CSVWriter csvWriter;
    public float totalTimeOfPostUnguidedVocalizationContant;
    public float timeInUnguidedVocalization;
    private int currentSegment = 0; //As Sonoflore

    //private bool silentPlaying = false;

    void Awake()
    {
        if(CSVLoader.gameMode == "Preperation" || CSVLoader.gameMode == "Skills Training")
        {
            if (CSVLoader.subGameMode == "Peace" || CSVLoader.subGameMode == "Mindfulness and Joy")
            {
                totalTimeOfPostUnguidedVocalizationContant = 889.0f;
                AkSoundEngine.SetSwitch("VO_ThematicContent", "Peace", gameObject);
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Peace", gameObject);
            } 
            else if (CSVLoader.subGameMode == "Narrative" || CSVLoader.subGameMode == "Psychological Flexibility")
            {
                totalTimeOfPostUnguidedVocalizationContant = 742.0f;
                AkSoundEngine.SetSwitch("VO_ThematicContent", "Narrative", gameObject);
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Narrative", gameObject);
            } 
            else if (CSVLoader.subGameMode == "Surrender" || CSVLoader.subGameMode == "Psychedelic Prepeation")
            {
                totalTimeOfPostUnguidedVocalizationContant = 775.0f;
                AkSoundEngine.SetSwitch("VO_ThematicContent", "Surrender", gameObject);
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Surrender", gameObject);
            } 

            if(firstTimeUser)
            {
                //AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject,(uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);
                AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);  
                AkSoundEngine.SetSwitch("VO_Somatic","Long",gameObject);
            } else {
                AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_SHORT", gameObject);
                AkSoundEngine.SetSwitch("VO_Somatic","Long",gameObject);
            }
        } else if (CSVLoader.gameMode == "Integration")
        {
            AkSoundEngine.PostEvent("Play_INTEGRATION_OPENING_SEQUENCE_SHORT", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);
        }
        //SOME IMPORTANT STARTUP BEHAVIORS ARE IN SEQUENCER.CS AND MUSICSYSTEM1.CS
        
        assignVOs();
        
        if(developmentMode.startAtStart) //NORMAL START
        {

        }
        else if (developmentMode.startInTutorial)
        {
            tutorial.StartTutorial();
            InitializeLights();
        }
        else if(developmentMode.startInPlayground)
        {
            InitializeLights();
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
                } else if (musicSyncInfo.userCueName == "Cue_StartTutorial")
                {
                    tutorial.StartTutorial();
                } else if (musicSyncInfo.userCueName == "Cue_InteractiveMusicSystem_Start")
                {
                    Debug.Log("WWise_VO: Cue_InteractiveMusicSystem_Start");
                    musicSystem1.InteractiveMusicInitializations();
                } else if (musicSyncInfo.userCueName == "Cue_Opening_Start")
                {
                    Debug.Log("WWise_VO: Cue_Opening_Start");
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
            if(musicSystem1.localToneOn)
            {   //if an actual music system tone comes on, break the loop, so we don't de-activate it here.
                yield break;
            }
            _t -= Time.deltaTime;
            yield return null;
        }
        
        if(musicSystem1.localToneOn)
        { //just in case this would end on the exact frame that the tone starts, check again...
            yield break;
        }

        Debug.Log("WWise_VO: Stopping a False Tone in WWise");
        musicSystem1.StopWwiseToning();
    }

    public void InitializeLights()
    {
        if(!lightsInitialized)
        {
            Debug.Log("WWise_VO: InitializeLights");
            lightControl.SetPreferredColor("Red");
            lightControl.NextPreferredColorWorld(5.0f);
            lightsInitialized = true;
        }
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
    }
    public void PassBackToVOManager()
    {
        Debug.Log("WWise_VO: RanFinalStageLogic");
       
        AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject);
    }
    public void calculateRemainingTime(float currentTime)
    {
        timeInUnguidedVocalization = 2700.0f - totalTimeOfPostUnguidedVocalizationContant - currentTime;   
        float timeInEachSegment = timeInUnguidedVocalization / 4;
        StartCoroutine(CountdownToNextSegment(timeInEachSegment));
    }

    IEnumerator CountdownToNextSegment(float timeInEachSegment)
    {
        yield return new WaitForSeconds(timeInEachSegment);

        if (currentStage == 0)
        {
            AkSoundEngine.SetState("SoundWorldMode", "Gentle");
            currentStage = 1;
        }
        else if (currentStage == 1)
        {
            AkSoundEngine.SetState("SoundWorldMode", "Shadow");
            currentStage = 2;
        }
        else if (currentStage == 2)
        {
            AkSoundEngine.SetState("SoundWorldMode", "Shruti");
            currentStage = 3;
        }
    }

    // Restart manually
    public void RestartCountdown(float timeInEachSegment)
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownToNextSegment(timeInEachSegment));
    }


    public void breathInBehaviour()
    {
        lightControl.FXWave(0.6f, 5f, 0.25f, true, true);
    }
        
    IEnumerator StartSighElicitationTimer()
        {
            yield return new WaitForSeconds(6.0f); // Wait for the audio event to finish playing
            pause = false;
        }
    IEnumerator StartQueryElicitationTimer()
        {
            pause = true;
            yield return new WaitForSeconds(30.0f); // Wait for the audio event to finish playing
            pause = false;
        }
                    
    
}

