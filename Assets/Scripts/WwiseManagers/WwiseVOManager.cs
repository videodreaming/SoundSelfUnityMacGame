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
    //public CSVWriter CSVWriter;
    public LightControl lightControl;
    public MusicSystem1 musicSystem1;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public Tutorial tutorial;
    public WorldShuffler worldShuffler;
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
    
    //public CSVWriter csvWriter;
    private Coroutine countdownCoroutine; // Reference to the coroutines
    private bool debugAllowLogs;

    public GameObject micPlayback;
    public AudioSource audioSource;
    //public UnityPlayBack unityPlaybackScript;
    //private bool silentPlaying = false;

    void Start()
    {
        //NOTE ABOUT WWISE:
        //THE GAMEOBJECT POINTS TO *THIS* GAMEOBJECT. SO WE CAN'T START
        //IT FROM ONE GAMEOBJECT AND THEN STOP IT FROM ANOTHER. IT HAS TO BE THE SAME GAMEOBJECT
        audioSource = micPlayback.GetComponent<AudioSource>();

        if (layingDown)
        {
            AkSoundEngine.SetSwitch("VO_Posture", "LieDown", gameObject);
            Debug.Log("CSVLoader: Setting VO Posture to LieDown");
        }
        else
        {
            AkSoundEngine.SetSwitch("VO_Posture", "Relax", gameObject);
            Debug.Log("CSVLoader: Setting VO Posture to Relax");
        }
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
            if (Input.GetKeyDown(KeyCode.N))
            {
                StartCoroutine(MakeWWiseTone());
            }
        }
    }
    

    public void VOCallbackFunction(object in_cookie, AkCallbackType in_type, object in_info)
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
            }
            else if (musicSyncInfo.userCueName == "Cue_ThematicOpening_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue_ThematicOpening_Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_Start")
            {
                if (UI_CurrentSession.Instance != null)
                {
                    Debug.Log("Not Null");
                    UI_CurrentSession.Instance.currentSession = "Opening Inquiry";
                }
                Debug.Log("WWise_VO_CUE: Stopping Openign Seq, play sigh Query Seq");
            }
            else if (musicSyncInfo.userCueName == "Cue_Microphone_ON")
            {
                Debug.Log("WWise_VO_CUE: Cue Mic On"); //Mic On and Mic Off are used in the "voice elicitation" sequences
                imitoneVoiceIntepreter.SetGameOn(true);
            }
            else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
            {
                Debug.Log("WWise_VO_CUE: Cue Mic OFF");
                imitoneVoiceIntepreter.SetGameOn(false);
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
            {
                UI_CurrentSession.Instance.currentSession = "Opening Teaching";
                Debug.Log("WWise_VO_CUE: Cue_VO_GuidedVocalization_Start (Robin expects we won't see this, as it's called from tutorial)"); //This is when Jaya begins speaking, in the test tones. I don't think it is called from this script, but instead from Tutorial.cs
                imitoneVoiceIntepreter.gameOn = false;
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
            {
                Debug.Log("WWise_VO_CUE: Cue_VO_GuidedVocalization_End (Robin expects we won't see this, as it's called from tutorial)"); //This is when Jaya begins speaking, in the test tones. I don't think it is called from this script, but instead from Tutorial.cs
                imitoneVoiceIntepreter.gameOn = true;
            }
            else if (musicSyncInfo.userCueName == "Cue_Somatic_Start")
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
            }
            else if (musicSyncInfo.userCueName == "Cue_Orientation_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue Orientation Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue Sigh Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_End")
            {
                Debug.Log("WWise_VO_CUE: PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
            }
            else if (musicSyncInfo.userCueName == "Cue_LinearHum_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue_LinearHum_Start");
                InitializeLightsWithDelay();
                //sequencer.InitializeLights();
                StartCoroutine(MakeWWiseTone());
            }
            else if (musicSyncInfo.userCueName == "Cue_StartTutorial") //This is called from the end of the Somatic Sequence, near the end. He says "Humming and toning should first come from a relaxed place. Breathe in, and hum"
            {
                tutorial.StartTutorial();
            }
            else if (musicSyncInfo.userCueName == "Cue_InteractiveMusicSystem_Start")
            {
                Debug.LogWarning("WWise_VO_CUE: Cue_InteractiveMusicSystem_Start");
                musicSystem1.SetMusicSilentLayerVolume(musicSystem1._silentVolumeHigh, 54f);
            }
            else if (musicSyncInfo.userCueName == "Cue_Opening_Start")
            {
                Debug.Log("WWise_VO_CUE: Cue_Opening_Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromHmmToAhh")
            {
                Debug.Log("WWise_VO_CUE: Cue Change to Ahh");
                tutorial.SetTestVocalizationType("Ahh");
                musicSystem1.LockToC(false);
            }
            else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromAhhToOhh")
            {
                Debug.Log("WWise_VO_CUE: Cue Change to Ohh");
                tutorial.SetTestVocalizationType("Ohh");
                musicSystem1.LockToC(false);
            }
            else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromOhhToAdvanced")
            {
                Debug.Log("WWise_VO_CUE: Cue Change to Advanced");
                tutorial.SetTestVocalizationType("Advanced");
                musicSystem1.LockToC(false);
                if (!worldShuffler.shuffling)
                {
                    worldShuffler.BeginShuffle();
                }
            }
            else if (musicSyncInfo.userCueName == "Cue_FreePlay") //"Your task is to continue toning like this..." (about halfway through)
            {
                Debug.Log("WWise_VO_CUE: Cue_FreePlay");
                UI_CurrentSession.Instance.currentSession = "Free Interaction";
                musicSystem1.SetMusicSilentLayerVolume(musicSystem1._silentVolumeHigh, 40f);
                director.disable = false;
            }
            else if (musicSyncInfo.userCueName == "Cue_Break_Tests") //End of "Keep going" (the last instruction)
            {
                Debug.Log("WWise_VO_CUE: Wwise_Tutorial_Break_All_Tests");
                tutorial.EndTutorialNaturally();
            }
            else
            {
                Debug.LogWarning("WWise_VO_CUE: Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
            }
        }   
    }

    public void ClosingCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            Debug.Log("WWise_VO: Callback triggered: " + in_type);
            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
            if (musicSyncInfo.userCueName == "Cue_ThematicSavasana_Start")
            {
                UI_CurrentSession.Instance.currentSession = "Thematic Savasana";
                Debug.Log("WWise_VO: Cue_ThematicSavasana_Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_ThematicSavasana_End")
            {
                UI_CurrentSession.Instance.currentSession = "Closing Teaching";
                Debug.Log("Wwise_VO: Cue_ThematicSavasana_End");
                sequencer.StartSilentMeditation();

            }
            else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation2_Start")
            {
                UI_CurrentSession.Instance.currentSession = "Closing Inquiry";
                Debug.Log("Wwise_VO: Cue_VoiceElicitation2_Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_Wakeup_Start")
            {
                UI_CurrentSession.Instance.currentSession = "Wake Up";
                Debug.Log("Wwise_VO: Cue_VO_Wakeup_Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_Goodbye_Start")
            {
                UI_CurrentSession.Instance.currentSession = "Closing Words";
            }
            else if(musicSyncInfo.userCueName == "Cue_Microphone_ON")
            {
                Debug.Log("WWise_VO_CUE: Cue Mic On");
                imitoneVoiceIntepreter.SetGameOn(true);
            }
            else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
            {
                Debug.Log("WWise_VO_CUE: Cue Mic OFF");
                imitoneVoiceIntepreter.SetGameOn(false);
            }
        }
    }

    public void SetToFireflies()
    {
        Debug.Log("WWise_VO: Set to Fireflies");
        AkSoundEngine.SetSwitch("VO_ThematicContent", "Fireflies", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Fireflies", gameObject);
        AkSoundEngine.SetSwitch("VO_THEMATICSAVASANA_SWITCH", "Fireflies", gameObject);
    }
    public void SetToKindness()
    {
        Debug.Log("WWise_VO: Set to Kindness");
        AkSoundEngine.SetSwitch("VO_ThematicContent", "Kindness", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Kindness", gameObject);
        AkSoundEngine.SetSwitch("VO_THEMATICSAVASANA_SWITCH", "Kindness", gameObject);
    }
    public void SetToMetta()
    {
        Debug.Log("WWise_VO: Set to Metta");
        AkSoundEngine.SetSwitch("VO_ThematicContent", "Metta", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Metta", gameObject);
        AkSoundEngine.SetSwitch("VO_THEMATICSAVASANA_SWITCH", "Metta", gameObject);
    }

    public void SetToPeace()
    {
        Debug.Log("WWise_VO: Set to Peace");
        AkSoundEngine.SetSwitch("VO_ThematicContent", "Peace", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Peace", gameObject);
        AkSoundEngine.SetSwitch("VO_THEMATICSAVASANA_SWITCH", "Peace", gameObject);
    }
    public void SetToNarrative()
    {

        Debug.Log("WWise_VO: Set to Narrative");
        AkSoundEngine.SetSwitch("VO_ThematicContent", "Narrative", gameObject);
        AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Narrative", gameObject);
        AkSoundEngine.SetSwitch("VO_THEMATICSAVASANA_SWITCH", "Narrative", gameObject);
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

    private void breathInBehaviour()
    {
        lightControl.FXWave(0.6f, 5f, 0.25f, true, false);
        AkSoundEngine.PostEvent("Play_Inhale_Long", gameObject);
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
            AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
    
            break;
            case "Preparation_Short":
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_SHORT", gameObject);
            break;
            case "Integration_Short":
            AkSoundEngine.PostEvent("Play_INTEGRATION_OPENING_SEQUENCE_SHORT", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
            break;
            default:
            Debug.LogError("WWise_VO: Invalid openingSequenceType: " + openingSequenceType);
            break;
        }
    }         

    public void Stop_InteractiveMusicSystem()
    {
        AkSoundEngine.PostEvent("Stop_InteractiveMusicSystem", gameObject);
        Debug.Log("WWise_VO: Stop Interactive Music System");
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
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationHum", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            case "Ahh":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAhh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            case "Ohh":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationOhh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            case "Advanced":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAdvanced", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
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
                AkSoundEngine.PostEvent("Play_VO_testRepair_Hum", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            case "Ahh":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Ahh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            case "Ohh":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Ohh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            case "Advanced":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Extended", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            default:
                Debug.LogError("WWise_VO: Invalid testVocalizationType: " + guidanceType);
                break;
        }
    }

    public void PlayCorrectionConfirmationVO()
    {
        AkSoundEngine.PostEvent("Play_VO_testRepair_succeed", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
        Debug.Log("WWise_VO: Play Repair Success");
    }

    public void PlayThematicSavasana()
    {
        AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, ClosingCallBackFunction, null);
    }

    private void InitializeLightsWithDelay()
    {
        StartCoroutine(InitializeLightsCoroutine());
    }

    private IEnumerator InitializeLightsCoroutine()
    {
        yield return new WaitForSeconds(1f);
        sequencer.InitializeLights();
    }
}

