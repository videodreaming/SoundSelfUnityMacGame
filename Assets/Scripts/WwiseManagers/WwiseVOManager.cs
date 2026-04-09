using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;
using SoundSelf.Sequence;

//REFACTORING THOUGHTS FROM ROBIN
//WE SHOULD ENSURE THAT THE USERAUDIOSOURCE IS ONLY REFERENCED AND CONTROLLED FROM ONE SCRIPT

public class WwiseVOManager : MonoBehaviour
{
    public CSVLoader csvLoader;
    public Sequencer sequencer;
    public Director director;
    //public CSVWriter CSVWriter;
    public LightControl lightControl;
    public MusicSystem1 musicSystem1;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public Tutorial tutorial;
    public WorldShuffler worldShuffler;
    private bool debugAllowMusicLogs = true;
    private bool pause = true;
    public bool layingDown = true;
    
    //public CSVWriter csvWriter;
    private Coroutine countdownCoroutine; // Reference to the coroutines
    private bool debugAllowLogs;
    private bool developmentModeWarningFlag = false;
    private int tutorialGuidanceCount = 0;

    //public GameObject micPlayback;
    //public UnityPlayBack unityPlaybackScript;
    //private bool silentPlaying = false;

    void Start()
    {
        //NOTE ABOUT WWISE:
        //THE GAMEOBJECT POINTS TO *THIS* GAMEOBJECT. SO WE CAN'T START
        //IT FROM ONE GAMEOBJECT AND THEN STOP IT FROM ANOTHER. IT HAS TO BE THE SAME GAMEOBJECT


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


    

    public void VOCallbackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
            // NOT-YET INTEGRATED ONES
            // BreatheOut_Start
            // Cue_ThematicOpening_End

        if (sequencer == null)
        {
            Debug.LogError("WwiseVOManager: 'sequencer' reference is missing!");
        }
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
            else if (musicSyncInfo.userCueName == "Cue_Start_Tutorial" || musicSyncInfo.userCueName == "Cue_Tutorial_Start" || musicSyncInfo.userCueName == "Cue_StartTutorial") //TODO: remove "GuidedVocalization_Start" as it is deprecated, once Lorna commits change.
            {
                Debug.Log($"WWise_VO_CUE: {musicSyncInfo.userCueName} (expected is Cue_Tutorial_Start, variations allowed for backward compatibilty)");
                sequencer.HandleSequenceCommand(SequenceCommand.StartTutorial);  
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
            {
                UI_CurrentSession.Instance.currentSession = "Opening Teaching";
                Debug.Log("WWise_VO_CUE: Cue_VO_GuidedVocalization_Start"); //This is when Jaya begins speaking, in the test tones. I don't think it is called from this script, but instead from Tutorial.cs
                imitoneVoiceIntepreter.gameOn = false;
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
            {
                Debug.Log("WWise_VO_CUE: Cue_VO_GuidedVocalization_End"); //This is when Jaya begins speaking, in the test tones. I don't think it is called from this script, but instead from Tutorial.cs
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
            else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start" || musicSyncInfo.userCueName == "Cue_BreatIn_Start")
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
            else if (musicSyncInfo.userCueName == "Cue_LinearHum_Start" || musicSyncInfo.userCueName == "Cue_LInearHum_Start")
            {
                Debug.Log($"WWise_VO_CUE: {musicSyncInfo.userCueName} (expected is Cue_LinearHum_Start, variations allowed for backward compatibilty)");
                sequencer.HandleSequenceCommand(SequenceCommand.FirstVocalizationStart);
            } else if(musicSyncInfo.userCueName == "Cue_LinearHum")
            {
                
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
                musicSystem1.SetFundamentalModeLock(false);
            }
            else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromAhhToOhh")
            {
                Debug.Log("WWise_VO_CUE: Cue Change to Ohh");
                tutorial.SetTestVocalizationType("Ohh");
                musicSystem1.SetFundamentalModeLock(false);
            }
            else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromOhhToAdvanced")
            {
                Debug.Log("WWise_VO_CUE: Cue Change to Advanced");
                tutorial.SetTestVocalizationType("Advanced");
                musicSystem1.SetFundamentalModeLock(false);
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
                director.Enable();
            }
            else if (musicSyncInfo.userCueName == "Cue_Break_Tests") //End of "Keep going" (the last instruction)
            {
                Debug.Log("WWise_VO_CUE: Wwise_Tutorial_Break_All_Tests");
                if (sequencer == null || !sequencer.HandleSequenceCommand(SequenceCommand.Break_Tests))
                    //tutorial.EndTutorialNaturally();
                    Debug.LogWarning("WWise_VO_CUE: This should end the tutorial naturally, but I commented it out.");
            }
            else if (musicSyncInfo.userCueName == "Cue_StartInteractive")
            {
                Debug.Log("WWise_VO_CUE: Cue_StartInteractive");
                if (sequencer != null)
                    sequencer.HandleSequenceCommand(SequenceCommand.StartInteractive);
                else
                    Debug.LogError("WwiseVOManager: sequencer is null. Cannot handle Cue_StartInteractive.");
            }
            else if (musicSyncInfo.userCueName == "Cue_WaitForButton")
            {
                Debug.Log("WWise_VO_CUE: Cue_WaitForButton");
                if (sequencer != null)
                    sequencer.HandleSequenceCommand(SequenceCommand.WaitForButton);
                //TODO: Legacy — implement the button to show up, and the logic to wait for it to be pressed.
            }
            else if (musicSyncInfo.userCueName == "Cue_Start_Breathworkcycle")
            {
                Debug.Log("WWise_VO_CUE: Cue_Start_Breathworkcycle");
                musicSystem1.SetBreathworkCycle(true);
            }
            else if (musicSyncInfo.userCueName == "Cue_Music_Ending")
            {
                Debug.Log("WWise_VO_CUE: Cue_Music_Ending");
                if (sequencer != null)
                    sequencer.HandleSequenceCommand(SequenceCommand.MusicTrackEnding);
            }
            else
            {
                Debug.Log("WWise_VO_CUE: Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
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
                if (sequencer != null)
                    sequencer.HandleSequenceCommand(SequenceCommand.ThematicSavasana_End);
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
            } else if (musicSyncInfo.userCueName == "Cue_Stop_Interactive")
            {
                Debug.Log("WWise_VO: Cue_Stop_Interactive");
                MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.FrozenFreeplay);
            }
            else
            {
                Debug.Log("WWise_VO: Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
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

    public void SetToEsketamineAscending()
    {
        if (musicSystem1 != null)
            musicSystem1.SetProtocolStacksAscendingDefaults();
        else
            Debug.LogWarning("WwiseVOManager: musicSystem1 is null, cannot set Protocol Stacks Ascending defaults.");
    }

    public void SetToEsketamineDescending()
    {
        //TODO: Implement this
        Debug.LogWarning("WWise_VO: SetToEsketamineDescending is not yet implemented.");
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
        //AkSoundEngine.PostEvent("Play_Inhale_Long", gameObject);
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
        Debug.Log("WWise_VO: Play Opening Sequence: " + openingSequenceType);
        switch (openingSequenceType)
        {
            case "Preparation_Long":
            AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
            Debug.Log("WWise_VO: Play Preparation Long Opening Sequence");
            break;
            case "Preparation_Short":
            AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_SHORT", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
            Debug.Log("WWise_VO: Play Preparation Short Opening Sequence");
            break;
            case "Integration_Short":
            AkSoundEngine.PostEvent("Play_INTEGRATION_OPENING_SEQUENCE_SHORT", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
            Debug.Log("WWise_VO: Play Integration Short Opening Sequence");
            break;
            case "PS_Ascending":
            AkSoundEngine.PostEvent("Play_ASCENDING_OPENING", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
            Debug.Log("WWise_VO: Play Ascending Opening Sequence");
            break;
            default:
            Debug.LogError("WWise_VO: Invalid openingSequenceType: " + openingSequenceType);
            break;
        }
    }      

    public void StopOpeningSequence()
    {
        Debug.Log("WWise_VO: Stop Opening Sequence");
        AkSoundEngine.PostEvent("Stop_PREPARATION_OPENING_SEQUENCE_LONG", gameObject);
        AkSoundEngine.PostEvent("Stop_PREPARATION_OPENING_SEQUENCE_SHORT", gameObject);
        AkSoundEngine.PostEvent("Stop_INTEGRATION_OPENING_SEQUENCE_SHORT", gameObject);
        AkSoundEngine.PostEvent("Stop_ASCENDING_OPENING", gameObject);
    }

    //TUTORIAL VO CALLS
    public int PlayTutorialGuidance(string guidanceType)
    {
        
        if(debugAllowLogs)
        {
            Debug.Log("Wwise_VO: Play " + guidanceType + " Guidance");
        }
        
        switch(guidanceType)
        {
            case "Hum":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationHum", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                tutorialGuidanceCount++;
                break;
            case "Ahh":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAhh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                tutorialGuidanceCount++;
                break;
            case "Ohh":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationOhh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                tutorialGuidanceCount++;
                break;
            case "Advanced":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAdvanced", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                tutorialGuidanceCount++;
                break;
            case "Lite":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationLite", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                tutorialGuidanceCount++;
                break;
            default:
                Debug.LogError("Invalid testVocalizationType: " + guidanceType);
                break;
        }
        return tutorialGuidanceCount;
    }

    public void ResetTutorialGuidanceCount()
    {
        tutorialGuidanceCount = 0;
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

    public void PlayAscendingClosing()
    {
        Debug.Log("WWise_VO: Play Ascending Closing");
        AkSoundEngine.PostEvent("Play_ASCENDING_CLOSING", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, ClosingCallBackFunction, null);
    }

    public void SetTestRepairSwitch(string AorC)
    {
        if(AorC == "A")
        {
            AkSoundEngine.SetSwitch("VO_testRepair", "A", gameObject);
        }
        else if(AorC == "C")
        {
            AkSoundEngine.SetSwitch("VO_testRepair", "C", gameObject);
        }
        else
        {
            Debug.LogError("WWise_VO: Invalid testRepairSwitch: " + AorC);
        }
    }

}

