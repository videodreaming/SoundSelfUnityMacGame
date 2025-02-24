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
    public DevelopmentMode developmentMode;
    public Director director;
    public CSVWriter CSVWriter;
    public LightControl lightControl;
    public MusicSystem1 musicSystem1;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public Tutorial tutorial;
    //public MusicSystem1 musicSystem1;
    //public RTPC silentFundamentalrtpcvolume;
    //public RTPC toningFundamentalrtpcvolume;
    //public RTPC silentHarmonyrtpcvolume;
    //public RTPC toningHarmonyrtpcvolume;
    //public float fadeDuration = 54.0f;
    //public float targetValue = 80.0f;
    private bool debugAllowMusicLogs = true;
    private bool pause = true;
    public bool layingDown = true;
    
    public CSVWriter csvWriter;
    private Coroutine countdownCoroutine; // Reference to the coroutines
    private bool debugAllowLogs;
    
    //private bool silentPlaying = false;

    void Start()
    {
        
        //SOME IMPORTANT STARTUP BEHAVIORS ARE IN SEQUENCER.CS AND MUSICSYSTEM1.CS
       
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
            Debug.Log("WWise_VO_CUE: Callback triggered: " + in_type);
            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
            if (musicSyncInfo.userCueName == "Cue_Posture_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue_Posture_Start");
            } else if (musicSyncInfo.userCueName == "Cue_ThematicOpening_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue_ThematicOpening_Start");
            } else if(musicSyncInfo.userCueName == "Cue_VoiceElicitation1_Start")
            {
                Debug.Log("WWise_VO_CUE: Stopping Openign Seq, play sigh Query Seq");
            }
            else if(musicSyncInfo.userCueName == "Cue_Microphone_ON")
            {
                Debug.Log("WWise_VO_CUE: Cue Mic On"); //Mic On and Mic Off are used in the "voice elicitation" sequences
                imitoneVoiceIntepreter.gameOn = true;
            }
            else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
            {
                Debug.Log("WWise_VO_CUE: Cue Mic OFF");
                imitoneVoiceIntepreter.gameOn = false;//I think one of these is not correct.  (also see tutorial.cs and MusicSystem1.cs).
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue_VO_GuidedVocalization_Start (Robin expects we won't see this, as it's called from tutorial)"); //This is when Jaya begins speaking, in the test tones. I don't think it is called from this script, but instead from Tutorial.cs
                imitoneVoiceIntepreter.gameOn = false;
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
            {
                Debug.Log("WWise_VO_CUE: Cue_VO_GuidedVocalization_End (Robin expects we won't see this, as it's called from tutorial)"); //This is when Jaya begins speaking, in the test tones. I don't think it is called from this script, but instead from Tutorial.cs
                imitoneVoiceIntepreter.gameOn = true;
            }
            else if(musicSyncInfo.userCueName == "Cue_Somatic_Start")
            {
                Debug.Log("WWise_VO_CUE: Somatic Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_BreathIn")
            {
                Debug.Log("WWise_VO_CUE: Cue_BreathIn");
                breathInBehaviour();
            } 
            else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue BreathIn Start");
                breathInBehaviour();
            } else if(musicSyncInfo.userCueName == "Cue_Orientation_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue Orientation Start");
            } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue Sigh Start");
            }  else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_End")
            {
                Debug.Log("WWise_VO_CUE: PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
            } else if (musicSyncInfo.userCueName == "Cue_LinearHum_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue_LinearHum_Start");
                sequencer.InitializeLights();
                StartCoroutine(MakeWWiseTone());
            } else if (musicSyncInfo.userCueName == "Cue_StartTutorial") //This is called from the end of the Somatic Sequence, near the end. He says "Humming and toning should first come from a relaxed place. Breathe in, and hum"
            {
                tutorial.StartTutorial();
            } else if (musicSyncInfo.userCueName == "Cue_InteractiveMusicSystem_Start")
            {
                Debug.LogWarning("WWise_VO_CUE: WARNING, THIS CUE IS NOT EXPECTED, IT IS A DUPLICATE OF CUE_FREEPLAY: Cue_InteractiveMusicSystem_Start");
            } else if (musicSyncInfo.userCueName == "Cue_Opening_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue_Opening_Start");
            } 
            else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromHmmToAhh")
            {
                Debug.Log("WWise_VO_CUE: Cue Change to Ahh");
                tutorial.SetTestVocalizationType("Ahh");
            } else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromAhhToOhh")
            {
                Debug.Log("WWise_VO_CUE: Cue Change to Ohh");
                tutorial.SetTestVocalizationType("Ohh");
            } else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromOhhToAdvanced")
            {
                Debug.Log("WWise_VO_CUE: Cue Change to Advanced");
                tutorial.SetTestVocalizationType("Advanced");
                musicSystem1.LockToC(false);
            } else if (musicSyncInfo.userCueName == "Cue_FreePlay") //"Your task is to continue toning like this..." (about halfway through)
            {
                Debug.Log("WWise_VO_CUE: Cue_FreePlay");
                
                musicSystem1.SetSilentVolume(80f, 40f);            
                director.disable = false;
            } else if (musicSyncInfo.userCueName == "Cue_Break_Tests") //End of "Keep going" (the last instruction)
            {
                Debug.Log("WWise_VO_CUE: Wwise_Tutorial_Break_All_Tests");
                tutorial.EndTutorial();
            }
            else
            {
                Debug.LogWarning("WWise_VO_CUE: Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
            }
        }   
    }
    public void SetToPeace()
    {
        AkSoundEngine.SetSwitch("VO_ThematicContent", "Peace", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Peace", gameObject);
    }
    public void SetToNarrative()
    {
        AkSoundEngine.SetSwitch("VO_ThematicContent", "Narrative", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Narrative", gameObject);
    }
    public void SetToSurrender()
    {
        AkSoundEngine.SetSwitch("VO_ThematicContent", "Surrender", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Surrender", gameObject);
    }

    public void firstTimeUser()
    {
        AkSoundEngine.SetSwitch("VO_Somatic","Long",gameObject);
        AkSoundEngine.SetSwitch("VO_ClosingGoodbye","Long",gameObject);
    }

    public void notFirstTimeUser()
    {
        AkSoundEngine.SetSwitch("VO_Somatic","Short",gameObject);
        AkSoundEngine.SetSwitch("VO_ClosingGoodbye","Short",gameObject);
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

    
    private float GetRTPCValue(RTPC rtpc)
    {
        uint rtpcID = AkSoundEngine.GetIDFromString(rtpc.Name);
        int valueType = 1; // AkRTPCValue_type type, 0 for game object, 1 for global RTPC
        float value;
        AkSoundEngine.GetRTPCValue(rtpcID, gameObject, 0, out value, ref valueType);
        return value;
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

    //INTRO VO CALLS
    public void PlayOpeningSequence(string openingSequenceType)
    {
        switch (openingSequenceType)
        {
            case "Preparation_Long":
            AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);
            break;
            case "Preparation_Short":
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_SHORT", gameObject);
            break;
            case "Integration_Short":
            AkSoundEngine.PostEvent("Play_INTEGRATION_OPENING_SEQUENCE_SHORT", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);
            break;
            default:
            Debug.LogError("WWise_VO: Invalid openingSequenceType: " + openingSequenceType);
            break;
        }
    }         
    //TUTORIAL VO CALLS
    public void PlayTutorialGuidance(string guidanceType)
    {
        
        if(debugAllowLogs)
        {
            Debug.Log("Wwise_VO: Play " + guidanceType + " Guidance");
        }
        
        switch(guidanceType)
        {
            case "Hum":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationHum", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
                break;
            case "Ahh":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAhh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
                break;
            case "Ohh":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationOhh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
                break;
            case "Advanced":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAdvanced", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
                break;
            default:
                Debug.LogError("Invalid testVocalizationType: " + guidanceType);
                break;
        }
    }
    
    public void PlayCorrectionGuidance(string guidanceType)
    {
        
        if(debugAllowLogs)
        {
            Debug.Log("WWise_VO: Play " + guidanceType + " Correction Guidance");
        }

        switch(guidanceType)
        {
            case "Hum":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Hum", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
                break;
            case "Ahh":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Ahh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
                break;
            case "Ohh":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Ohh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
                break;
            case "Advanced":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Extended", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
                break;
            default:
                Debug.LogError("WWise_VO: Invalid testVocalizationType: " + guidanceType);
                break;
        }
    }

    public void PlayCorrectionConfirmationVO()
    {
        AkSoundEngine.PostEvent("Play_VO_testRepair_succeed", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, tutorial.TutorialCallBackFunction, null);
        Debug.Log("WWise_VO: Play Repair Success");
    }

    //WAS IN SEQUENCER.CS BEFORE I MOVED IT
    public void PlayWakeUpSoonVO()
    {
        AkSoundEngine.PostEvent("Play_WakeUpEndSoon_SEQUENCE", gameObject);
        Debug.Log("WWise_VO: Play Wake Up Soon Sequence");
    }
}

