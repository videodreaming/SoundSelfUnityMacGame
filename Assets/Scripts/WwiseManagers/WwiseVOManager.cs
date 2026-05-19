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

    /// <summary>Last value passed to <see cref="SetTestRepairSwitch"/> (A or C). Used so Ahh/Advanced repair lines can verify Wwise <c>VO_testRepair</c> is on A.</summary>
    private string _lastTestRepairSwitch;

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


    /// <summary>Wwise music-sync cue handlers often skip work when scene refs are missing — log so it is not silent.</summary>
    private static void WarnCueSkipped(string userCueName, string missingDependency, string skippedBehavior)
    {
        Debug.LogWarning("WwiseVOManager: cue \"" + userCueName + "\" skipped " + skippedBehavior + " (" + missingDependency + " is null).");
    }

    // ---- VO music-sync: null-safe scene hooks (each logs WarnCueSkipped if a dependency is missing) ----

    private void VoTryImitoneSetGameOn(string cueName, bool on)
    {
        if (imitoneVoiceIntepreter != null)
            imitoneVoiceIntepreter.SetGameOn(on);
        else
            WarnCueSkipped(cueName, "imitoneVoiceIntepreter", on ? "SetGameOn(true)" : "SetGameOn(false)");
    }

    private void VoTrySequencerCommand(string cueName, SequenceCommand cmd, string skippedDescription)
    {
        if (sequencer != null)
            sequencer.HandleSequenceCommand(cmd);
        else
            WarnCueSkipped(cueName, "sequencer", skippedDescription);
    }

    private void VoTryMusicSilentLayerHigh(string cueName, float blendSeconds, string skippedDescription)
    {
        if (musicSystem1 != null)
            musicSystem1.SetMusicSilentLayerVolume(musicSystem1._silentVolumeHigh, blendSeconds);
        else
            WarnCueSkipped(cueName, "musicSystem1", skippedDescription);
    }

    private void VoTryFundamentalModeUnlock(string cueName)
    {
        if (musicSystem1 != null)
            musicSystem1.SetFundamentalModeLock(false);
        else
            WarnCueSkipped(cueName, "musicSystem1", "SetFundamentalModeLock(false)");
    }

    private void VoTryTutorialSetVocalization(string cueName, string vocalizationType)
    {
        if (tutorial != null)
            tutorial.SetTestVocalizationType(vocalizationType);
        else
            WarnCueSkipped(cueName, "tutorial", "SetTestVocalizationType(" + vocalizationType + ")");
    }

    private void VoTryDirectorEnable(string cueName)
    {
        if (director != null)
            director.Enable();
        else
            WarnCueSkipped(cueName, "director", "Enable()");
    }

    private void VoTryBeginShuffleIfIdle(string cueName)
    {
        if (worldShuffler != null && !worldShuffler.shuffling)
            worldShuffler.BeginShuffle();
        else if (worldShuffler == null)
            WarnCueSkipped(cueName, "worldShuffler", "BeginShuffle");
    }

    private void VoTrySetBreathworkCycle(string cueName, bool on)
    {
        if (musicSystem1 != null)
            musicSystem1.SetBreathworkCycle(on);
        else
            WarnCueSkipped(cueName, "musicSystem1", "SetBreathworkCycle(" + on + ")");
    }

    public void VOCallbackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
        // NOT-YET INTEGRATED ONES
        // BreatheOut_Start
        // Cue_ThematicOpening_End

        if (sequencer == null)
            Debug.LogError("WwiseVOManager: 'sequencer' reference is missing!");

        if (in_type != AkCallbackType.AK_MusicSyncUserCue)
            return;

        Debug.Log("WWise_VO_CUE: Callback triggered: " + in_type);
        var musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
        string cue = musicSyncInfo.userCueName;

        switch (cue)
        {
            case "Cue_Posture_Start":
                Debug.Log("WWise_VO_CUE: Cue_Posture_Start");
                break;

            case "Cue_ThematicOpening_Start":
                Debug.Log("WWise_VO_CUE: Cue_ThematicOpening_Start");
                break;

            case "Cue_VoiceElicitation1_Start":
                if (UI_CurrentSession.Instance != null)
                {
                    Debug.Log("Not Null");
                    UI_CurrentSession.Instance.currentSession = "Opening Inquiry";
                }
                Debug.Log("WWise_VO_CUE: Stopping Openign Seq, play sigh Query Seq");
                break;

            case "Cue_Microphone_ON":
                Debug.Log("WWise_VO_CUE: Cue Mic On"); // Mic On / Off: voice elicitation sequences
                VoTryImitoneSetGameOn(cue, true);
                break;

            case "Cue_Microphone_OFF":
                Debug.Log("WWise_VO_CUE: Cue Mic OFF");
                VoTryImitoneSetGameOn(cue, false);
                break;

            case "Cue_Start_Tutorial":
            case "Cue_Tutorial_Start":
            case "Cue_StartTutorial": // TODO: remove deprecated aliases once Wwise settled
                Debug.Log($"WWise_VO_CUE: {cue} (expected is Cue_Tutorial_Start, variations allowed for backward compatibilty)");
                VoTrySequencerCommand(cue, SequenceCommand.StartTutorial, "HandleSequenceCommand(StartTutorial)");
                break;

            case "Cue_VO_GuidedVocalization_Start":
                Debug.Log("WWise_VO_CUE: Cue_VO_GuidedVocalization_Start");
                VoTryImitoneSetGameOn(cue, false);
                break;

            case "Cue_VO_GuidedVocalization_End":
                Debug.Log("WWise_VO_CUE: Cue_VO_GuidedVocalization_End");
                VoTryImitoneSetGameOn(cue, true);
                break;

            case "Cue_Somatic_Start":
                Debug.Log("WWise_VO_CUE: Somatic Start");
                break;

            case "Cue_BreathIn":
                Debug.Log("WWise_VO_CUE: Cue_BreathIn");
                breathInBehaviour();
                break;

            case "Cue_BreathIn_Start":
            case "Cue_BreatIn_Start":
                Debug.Log("WWise_VO_CUE: Cue BreathIn Start");
                breathInBehaviour();
                break;

            case "Cue_Orientation_Start":
                Debug.Log("WWise_VO_CUE: Cue Orientation Start");
                break;

            case "Cue_Sigh_Start":
                Debug.Log("WWise_VO_CUE: Cue Sigh Start");
                break;

            case "Cue_VoiceElicitation1_End":
                Debug.Log("WWise_VO_CUE: PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
                break;

            case "Cue_LinearHum_Start":
            case "Cue_LInearHum_Start":
                Debug.Log($"WWise_VO_CUE: {cue} (expected is Cue_LinearHum_Start, variations allowed for backward compatibilty)");
                VoTrySequencerCommand(cue, SequenceCommand.FirstVocalizationStart, "HandleSequenceCommand(FirstVocalizationStart)");
                break;

            case "Cue_LinearHum":
                break;

            case "Cue_InteractiveMusicSystem_Start":
                Debug.LogWarning("WWise_VO_CUE: Cue_InteractiveMusicSystem_Start");
                VoTryMusicSilentLayerHigh(cue, 54f, "SetMusicSilentLayerVolume");
                break;

            case "Cue_Opening_Start":
                Debug.Log("WWise_VO_CUE: Cue_Opening_Start");
                break;

            case "Cue_ChangeVocalizationTypeFromHmmToAhh":
                Debug.Log("WWise_VO_CUE: Cue_ChangeVocalizationTypeFromHmmToAhh — Long vocalization type and side effects are now driven by tutorial guidanceCount; cue kept for Wwise timeline compatibility.");
                break;

            case "Cue_ChangeVocalizationTypeFromAhhToOhh":
                Debug.Log("WWise_VO_CUE: Cue_ChangeVocalizationTypeFromAhhToOhh — Long vocalization type and side effects are now driven by tutorial guidanceCount; cue kept for Wwise timeline compatibility.");
                break;

            case "Cue_ChangeVocalizationTypeFromOhhToAdvanced":
                Debug.Log("WWise_VO_CUE: Cue_ChangeVocalizationTypeFromOhhToAdvanced — Long vocalization type and side effects are now driven by tutorial guidanceCount; cue kept for Wwise timeline compatibility.");
                break;

            case "Cue_FreePlay": // "Your task is to continue toning like this..." (~halfway through)
                Debug.Log("WWise_VO_CUE: Cue_FreePlay");
                VoTryMusicSilentLayerHigh(cue, 40f, "SetMusicSilentLayerVolume");
                VoTryDirectorEnable(cue);
                break;

            case "Cue_Break_Tests": // End of "Keep going" (last instruction)
                Debug.Log("WWise_VO_CUE: Wwise_Tutorial_Break_All_Tests");
                if (sequencer == null || !sequencer.HandleSequenceCommand(SequenceCommand.Break_Tests))
                    Debug.LogWarning("WWise_VO_CUE: This should end the tutorial naturally, but I commented it out.");
                break;

            case "Cue_StartInteractive":
                Debug.Log("WWise_VO_CUE: Cue_StartInteractive");
                if (sequencer != null)
                    sequencer.HandleSequenceCommand(SequenceCommand.StartInteractive);
                else
                    Debug.LogError("WwiseVOManager: sequencer is null. Cannot handle Cue_StartInteractive.");
                break;

            case "Cue_WaitForButton":
                Debug.Log("WWise_VO_CUE: Cue_WaitForButton");
                VoTrySequencerCommand(cue, SequenceCommand.WaitForButton, "HandleSequenceCommand(WaitForButton)");
                // TODO: Legacy — button UI + wait for press
                break;

            case "Cue_Start_Breathworkcycle":
                Debug.Log("WWise_VO_CUE: Cue_Start_Breathworkcycle");
                VoTrySetBreathworkCycle(cue, true);
                break;

            case "Cue_Music_Ending":
                Debug.Log("WWise_VO_CUE: Cue_Music_Ending");
                VoTrySequencerCommand(cue, SequenceCommand.MusicTrackEnding, "HandleSequenceCommand(MusicTrackEnding)");
                break;

            default:
                Debug.Log("WWise_VO_CUE: Unexpected Cue: " + in_type + " | " + cue);
                break;
        }
    }

    public void ClosingCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type != AkCallbackType.AK_MusicSyncUserCue)
            return;

        Debug.Log("WWise_VO: Callback triggered: " + in_type);
        var musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
        string cue = musicSyncInfo.userCueName;

        switch (cue)
        {
            case "Cue_ThematicSavasana_Start":
                Debug.Log("WWise_VO: Cue_ThematicSavasana_Start");
                break;

            case "Cue_ThematicSavasana_End":
                Debug.Log("Wwise_VO: Cue_ThematicSavasana_End");
                VoTrySequencerCommand(cue, SequenceCommand.ThematicSavasana_End, "HandleSequenceCommand(ThematicSavasana_End)");
                break;

            case "Cue_VoiceElicitation2_Start":
                Debug.Log("Wwise_VO: Cue_VoiceElicitation2_Start");
                break;

            case "Cue_VO_Wakeup_Start":
                Debug.Log("Wwise_VO: Cue_VO_Wakeup_Start");
                break;

            case "Cue_Goodbye_Start":
                Debug.Log("Wwise_VO: Cue_Goodbye_Start");
                break;

            case "Cue_Microphone_ON":
                Debug.Log("WWise_VO_CUE: Cue Mic On");
                VoTryImitoneSetGameOn(cue, true);
                break;

            case "Cue_Microphone_OFF":
                Debug.Log("WWise_VO_CUE: Cue Mic OFF");
                VoTryImitoneSetGameOn(cue, false);
                break;

            case "Cue_Stop_Interactive":
                Debug.Log("WWise_VO: Cue_Stop_Interactive");
                bool handled = sequencer != null && sequencer.HandleSequenceCommand(SequenceCommand.CueStopInteractive);
                if (!handled && musicSystem1 != null)
                    musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.FrozenFreeplay);
                break;

            case "Cue_Stop_Interactive_3m":
                Debug.Log("WWise_VO: Cue_Stop_Interactive_3m");
                VoTrySequencerCommand(cue, SequenceCommand.CueStopInteractive3m, "HandleSequenceCommand(CueStopInteractive3m)");
                break;

            case "Cue_SilentMeditation_Start":
                Debug.Log("WWise_VO: Cue_SilentMeditation_Start");
                VoTrySequencerCommand(cue, SequenceCommand.CueSilentMeditationStart, "HandleSequenceCommand(CueSilentMeditationStart)");
                break;

            default:
                Debug.Log("WWise_VO: Unexpected Cue: " + in_type + " | " + cue);
                break;
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
        AkSoundEngine.SetSwitch("VO_THEMATICSAVASANA_SWITCH", "Surrender", gameObject); //4/16/2026 this wasn't here for some reason... test this.
    }

    public void SetToEsketamineAscending()
    {
        if (musicSystem1 != null)
            musicSystem1.SetProtocolStacksAscendingDefaults();
        else
            Debug.LogWarning("WwiseVOManager: musicSystem1 is null, cannot set Adjunctive Dual Stage defaults.");
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
        AkSoundEngine.PostEvent("Stop_OPENING_SEQUENCE", gameObject);
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
                EnsureVoTestRepairSwitchAForAhhOrAdvancedCorrection(guidanceType);
                AkSoundEngine.PostEvent("Play_VO_testRepair_Ahh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            case "Ohh":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Ohh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            case "Advanced":
                EnsureVoTestRepairSwitchAForAhhOrAdvancedCorrection(guidanceType);
                AkSoundEngine.PostEvent("Play_VO_testRepair_Extended", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
                break;
            default:
                Debug.LogError("WWise_VO: Invalid testVocalizationType: " + guidanceType);
                break;
        }
    }

    public void PlayCorrectionConfirmationVO()
    {
        AkSoundEngine.PostEvent("Play_VO_testRepair_Succeed", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, VOCallbackFunction, null);
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

    public void PlayMusicPlaylist(string playlistDurationSwitch)
    {
        if (playlistDurationSwitch != "_40m" && playlistDurationSwitch != "_60m")
        {
            Debug.LogError("WWise_VO: Invalid MusicPlaylist switch '" + playlistDurationSwitch + "'. Expected _40m or _60m.");
            return;
        }

        AkSoundEngine.SetSwitch("MusicPlaylist_Switch", playlistDurationSwitch, gameObject);
        Debug.Log("WWise_VO: Play Music Playlist (" + playlistDurationSwitch + ")");
        AkSoundEngine.PostEvent("Play_MusicPlaylist", gameObject);
    }

    public void StopMusicPlaylists() //TODO: tie this to a ui element.
    {
        Debug.Log("WWise_VO: Stop Music Playlist");
        AkSoundEngine.PostEvent("Stop_MusicPlaylist", gameObject);
    }

    public void SetTestRepairSwitch(string AorC)
    {
        if(AorC == "A")
        {
            if (debugAllowLogs)
                Debug.Log("WWise_VO: Set Test Repair Switch to A");
            AkSoundEngine.SetSwitch("VO_testRepair", "A", gameObject);
            _lastTestRepairSwitch = "A";
        }
        else if(AorC == "C")
        {
            if (debugAllowLogs)
                Debug.Log("WWise_VO: Set Test Repair Switch to C");
            AkSoundEngine.SetSwitch("VO_testRepair", "C", gameObject);
            _lastTestRepairSwitch = "C";
        }
        else
        {
            Debug.LogError("WWise_VO: Invalid testRepairSwitch: " + AorC);
        }
    }

    /// <summary>Wwise repair lines for Ahh/Extended require <c>VO_testRepair</c> switch A (e.g. after Ascending opening sets C).</summary>
    private void EnsureVoTestRepairSwitchAForAhhOrAdvancedCorrection(string guidanceType)
    {
        if (guidanceType != "Ahh" && guidanceType != "Advanced")
            return;
        if (_lastTestRepairSwitch == "A")
            return;
        Debug.LogWarning(
            "WwiseVOManager: Ahh/Advanced correction expects VO_testRepair switch A; last SetTestRepairSwitch was '" +
            (_lastTestRepairSwitch ?? "unset") + "'. Setting to A.");
        SetTestRepairSwitch("A");
    }

}

