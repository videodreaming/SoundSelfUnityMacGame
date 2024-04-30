using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;



public class WwiseVOManager : MonoBehaviour
{
    public AudioManager audioManager;
    private bool voOpeningEventPlayed = false;
    private bool voSighElicitation1Played = false;
    private bool voSighElicitationfail1Played = false;
    private bool QueryElicitation1Played = false;
    private bool QueryElicitationFail1Played = false;
    private bool QueryElicitationPassThankYou1Played = false;
    private bool ThematicContentPlayed = false;
    private bool PosturePlayed = false;
    private bool OrientationPlayed = false;
    private bool SomaticPlayed = false;
    private bool GuidededVocalizationHumPlayed = false;
    private bool GuidededVocalizationAhhPlayed = false;
    private bool GuidededVocalizationOhhPlayed = false;
    private bool GuidededVocalizationAdvancedPlayed = false;
    private bool UnGuidedVocalizationPlayed = false;
    private bool ThematicSavasanaPlayed= false;
    private bool SilentMeditationPlayed = false;
    private bool WakeUpPlayed = false;
    private bool EndingSoonPlayed = false;
    private bool SighElicitation2Played = false;
    private bool SighElicitationFail2Played = false;
    private bool QueryElicitationPass2Played = false;
    private bool ClosingGoodbyePlayed = false;
    private bool pause = true;

    public string ThematicContent = null;
    public string ThematicSavasana = null;
    public string VO_ClosingGoodbye = null;

    public CSVReader csvReader;

    // Start is called before the first frame update
    void Start()
    {
        assignVOs();
    }
    
    void assignVOs()
    {
        if(csvReader.GameSettings.GameMode == "Preperation")
        {
            Debug.Log("Preperation");
            if(csvReader.GameSettings.SubGameMode == "Narrative")
            {
                Debug.Log("Metta");
            } else if (csvReader.GameSettings.SubGameMode == "Peace")
            {
                Debug.Log("Guided");
            } else if (csvReader.GameSettings.SubGameMode == "Surrender")
            {
                Debug.Log("Silent");
            }
        } 
        if(csvReader.GameSettings.GameMode == "Integration")
        {
            Debug.Log("Integration");
            if(csvReader.GameSettings.SubGameMode == "Kindness")
            {
                Debug.Log("Kindness");
            } else if (csvReader.GameSettings.SubGameMode == "Metta")
            {
                Debug.Log("Metta");
            } else if (csvReader.GameSettings.SubGameMode == "Fireflies")
            {
                Debug.Log("Fireflies");
            }
        }
        if(csvReader.GameSettings.GameMode == "Adjunctive")
        {
            Debug.Log("Adjunctive");
            if(csvReader.GameSettings.SubGameMode == "Anchoring")
            {
                Debug.Log("Anchoring");
            } else if (csvReader.GameSettings.SubGameMode == "Meditation")
            {
                Debug.Log("Meditation");
            } else if (csvReader.GameSettings.SubGameMode == "Gratitude")
            {
                Debug.Log("Gratitude");
            }
        }
    }

    public void playOpening(string length)
    {
        if(length=="openingLong")
        {
            AkSoundEngine.SetSwitch("VO_Opening", length, gameObject);
        } else if(length=="openingShort") {
            AkSoundEngine.SetSwitch("VO_Opening", length, gameObject);
        } else if(length=="openingPassive"){
            AkSoundEngine.SetSwitch("VO_Opening", length, gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!pause)
        {
            if(audioManager.currentState == AudioManager.AudioManagerState.Opening && !voOpeningEventPlayed)
                {
                    voOpeningEventPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_Opening", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.SighElicitation1 && !voSighElicitation1Played)
                {
                    voSighElicitation1Played = true;
                    AkSoundEngine.PostEvent("Play_VO_SighElicitation1", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.SighElicitationFail1 && !voSighElicitationfail1Played && audioManager.SighElicitationPass1 == false)
                {
                    voSighElicitationfail1Played = true;
                    AkSoundEngine.PostEvent("Play_VO_SighElicitationFail1", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.QueryElicitation1 && !QueryElicitation1Played && audioManager.SighElicitationPass1 == true)
                {
                    QueryElicitation1Played = true;
                    AkSoundEngine.PostEvent("Play_VO_QueryElicitation1", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.QueryElicitationFail1 && !QueryElicitationFail1Played)
                {
                    QueryElicitationFail1Played = true;
                    AkSoundEngine.PostEvent("Play_VO_QueryElicitationFail1", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.QueryElicitationPassThankYou1 && !QueryElicitationPassThankYou1Played)
                {
                    Debug.LogWarning("QueryElicitationPassThankYou1Played");
                    QueryElicitationPassThankYou1Played = true;
                    AkSoundEngine.PostEvent("Play_VO_QueryElicitationPassThankYou1", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.ThematicContent && !ThematicContentPlayed)
                {
                    ThematicContentPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_ThematicContent", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.Posture && !PosturePlayed)
                {
                    PosturePlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_Posture", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.Orientation && !OrientationPlayed)
                {
                    OrientationPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_Orientation", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.Somatic && !SomaticPlayed)
                {
                    SomaticPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_Somatic", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationHum && !GuidededVocalizationHumPlayed)
                {
                    GuidededVocalizationHumPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationHum", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationAhh && !GuidededVocalizationAhhPlayed)
                {
                    GuidededVocalizationAhhPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAhh", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationOhh && !GuidededVocalizationOhhPlayed)
                {
                    GuidededVocalizationOhhPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationOhh", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationAdvanced && !GuidededVocalizationAdvancedPlayed)
                {
                    GuidededVocalizationAdvancedPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAdvanced", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.UnGuidedVocalization && !UnGuidedVocalizationPlayed)
                {
                    UnGuidedVocalizationPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_UnGuidedVocalization", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                } else if (audioManager.currentState == AudioManager.AudioManagerState.ThematicSavasana && !ThematicSavasanaPlayed)
                {
                    ThematicSavasanaPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_ThematicSavasana", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);  
                } else if (audioManager.currentState == AudioManager.AudioManagerState.SilentMeditation && !SilentMeditationPlayed){
                    SilentMeditationPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_SilentMeditation", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);  
                } else if (audioManager.currentState == AudioManager.AudioManagerState.WakeUp && !WakeUpPlayed){
                    WakeUpPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_WakeUp", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);  
                } else if (audioManager.currentState == AudioManager.AudioManagerState.EndingSoon && !EndingSoonPlayed){
                    EndingSoonPlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_EndingSoon", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);  
                } else if (audioManager.currentState == AudioManager.AudioManagerState.SighElicitation2 && !SighElicitation2Played){
                    SighElicitation2Played = true;
                    AkSoundEngine.PostEvent("Play_VO_SighElicitation2", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);  
                } else if (audioManager.currentState == AudioManager.AudioManagerState.SighElicitationFail2 && !SighElicitationFail2Played){
                    SighElicitationFail2Played = true;
                    AkSoundEngine.PostEvent("Play_VO_SighElicitationFail2", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);  
                } else if (audioManager.currentState == AudioManager.AudioManagerState.QueryElicitationPass2 && !QueryElicitationPass2Played){
                    QueryElicitationPass2Played = true;
                    AkSoundEngine.PostEvent("Play_VO_QueryElicitationPass2", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);  
                } else if (audioManager.currentState == AudioManager.AudioManagerState.ClosingGoodbye && !ClosingGoodbyePlayed){
                    ClosingGoodbyePlayed = true;
                    AkSoundEngine.PostEvent("Play_VO_ClosingGoodbye", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);  
                }
        }
    }

        void MyCallbackFunction(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                // Call OnAudioFinished only if the AudioManager state is not SighElicitation1
                if (audioManager.currentState == AudioManager.AudioManagerState.SighElicitation1 || audioManager.currentState == AudioManager.AudioManagerState.SighElicitationFail1)
                {
                    pause = true;
                    StartCoroutine(StartSighElicitationTimer());
                    resetFlags();
                } else if (audioManager.currentState == AudioManager.AudioManagerState.QueryElicitation1)
                {
                    StartCoroutine(StartQueryElicitationTimer());
                    resetFlags();
                }
                else
                {
                    audioManager.OnAudioFinished();
                    resetFlags();
                }
            }
        }
        void resetFlags()
        {
            voOpeningEventPlayed = false;
            voSighElicitation1Played = false;
            voSighElicitationfail1Played = false;
            QueryElicitation1Played = false;
            QueryElicitationFail1Played = false;
            QueryElicitationPassThankYou1Played = false;
            ThematicContentPlayed = false;
            PosturePlayed = false;
            OrientationPlayed = false;
            SomaticPlayed = false;
            GuidededVocalizationHumPlayed = false;
            GuidededVocalizationAhhPlayed = false;
            GuidededVocalizationOhhPlayed = false;
            GuidededVocalizationAdvancedPlayed = false;
            UnGuidedVocalizationPlayed = false;
            ThematicSavasanaPlayed= false;
            SilentMeditationPlayed = false;
            WakeUpPlayed = false;
            EndingSoonPlayed = false;
            SighElicitation2Played = false;
            SighElicitationFail2Played = false;
            QueryElicitationPass2Played = false;
            ClosingGoodbyePlayed = false;
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

