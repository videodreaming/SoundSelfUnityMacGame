using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;


public class Tutorial : MonoBehaviour
{
    private bool debugAllowLogs = true;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    //public RecordedAudioPlaybackTest recordedAudioPlaybackTest;
    public WwiseVOManager wwiseVOManager;
    public Sequencer sequencer;
    public MusicSystem1 musicSystem1;
    public Director director;
    public bool active {get; private set;}  = false; //currently, this is just for external objects to view if the tutorial is doing anything or not, all the actual behaviors are in StartTutorial() and EndTutorial()
    float testThreshold = 1.5f;
    float failThreshold = 8.0f;
    private bool testSuccess = false;
    string testVocalizationType;
    string testVocalizationTypeLastFrame;
    private Coroutine testCoroutine;
    private Coroutine correctionCoroutine;
    public bool inTutorial = false;
    public float totalTimeOfPostUnguidedVocalizationContant;
    public TimeTrackerScript TimeTrackerScript;
    
    // Start is called before the first frame update
    void Start()
    {
        testVocalizationType = "Hum";
    }

    // Update is called once per frame
    void Update()
    {
        if(imitoneVoiceInterpreter._tThisToneBiasTrue >= testThreshold)
        {
            testSuccess = true;
            Debug.Log("Tutorial: Test Success");
        }        

        if(testVocalizationType != testVocalizationTypeLastFrame)
        {
           if(testVocalizationType == "Advanced")
            {
                musicSystem1.SetSilentVolume(80f, 40f);
            }
            testVocalizationTypeLastFrame = testVocalizationType;
        }
            //REEF, WOULD YOU TEST THAT THESE THINGS ARE IMPLEMENTED? I *THINK* THEY ARE.
            //NOTES FROM MEETING ON 9/9/2024
            //I THINK THESE ONES ARE DONE BUT NEED TO CONFIRM 
            //Use a cue from WWise to change testVocalizationType from "hum" to "ahh" to "ohh" to "advanced", at the very beginning of the line being spoken.
            //- Whenever he is talking, the "mic off" cue should happen right at the start of his vo DONE
            //- We should then trigger the "mic on" cue near the end (but not AT) the end, when he says "breathe in" or whatever. DONE
            //      - (The other cue pair that has the same Unity behavior will work as well, AS LONG AS WE ARE TRIGGERING GAMEON CORRECTLY)
            //- We need a cue at the beginning of each tutorial VO that tells us if it is Hum/Ahh/Ohh etc.

            //MORE WWISE THINGS TO CHANGES
            //- Set up a cue at the beginning of the last VO that triggers breaking all tests, cos we're done! 
            //- We need a WWise Event for the correction success (vo_testRepair_succeed) ("Now you keep going on your own")
            //- What is cueing FreePlay right now? That *should* be the end of the tutorial.
            //- Need to check on this cue: "WWise_VO: Cue_InteractiveMusicSystem_Start" (whis will currently trigger the start of the tutorial, if I understand it correctly, it should happen at the end of the somatic meditaiton, so that's where I've put the call to StartTutorial())
            //- Let's check each of the test vos for a good place to put the breath in cue, even if he doesn't say "breathe in"
    }
    
    public void StartTutorial()
    {
        if(!active)
        {
            inTutorial = true;
            Debug.Log("Tutorial: START");
            active = true;
            testVocalizationType = "Hum";

            musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.Tutorial);

            wwiseVOManager.InitializeLights(); //this is probably already initialized, just making sure.
            
            testCoroutine = StartCoroutine(VoiceTestCoroutine());

        }
    }

    public void TutorialCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
         if (in_type == AkCallbackType.AK_MusicSyncUserCue)
            {
                AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
                if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
                { 
                    Debug.Log("WWise_VO Tutorial: Cue_VO_GuidedVocalization_Start"); //These are used during the test tones, for when Jaya speaks or not.
                    imitoneVoiceInterpreter.gameOn = false; //I think one of these is not correct. (also see Sequencer.cs and MusicSystem1.cs).
                } else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
                {
                    Debug.Log("WWise_VO Tutorial: Cue_VO_GuidedVocalization_End");
                    imitoneVoiceInterpreter.gameOn = true;//I think one of these is not correct. (also see Sequencer.cs and MusicSystem1.cs)
                } else if (musicSyncInfo.userCueName == "Cue_BreathIn")
                {
                    Debug.Log("WWise_VO Tutorial: Cue_BreathIn");
                    wwiseVOManager.breathInBehaviour();
                } else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromHmmToAhh")
                {
                    Debug.Log("WWise_VO Tutorial: Cue Change to Ahh");
                    testVocalizationType = "Ahh";
                } else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromAhhToOhh")
                {
                    Debug.Log("WWise_VO Tutorial: Cue Change to Ohh");
                    testVocalizationType = "Ohh";
                } else if (musicSyncInfo.userCueName == "Cue_ChangeVocalizationTypeFromOhhToAdvanced")
                {
                    Debug.Log("WWise_VO Tutorial: Cue Change to Advanced");
                    testVocalizationType = "Advanced";
                    musicSystem1.LockToC(false);
                } else if (musicSyncInfo.userCueName == "Cue_FreePlay") //"Your task is to continue toning like this..." (about halfway through)
                {
                    Debug.Log("WWise_VO Tutorial: Cue_FreePlay");
                    
                    musicSystem1.SetSilentVolume(80f, 40f);            
                    director.disable = false;
                } else if (musicSyncInfo.userCueName == "Cue_Break_Tests") //End of "Keep going" (the last instruction)
                {
                    Debug.Log("Wwise_Tutorial_Break_All_Tests");
                    EndTutorial();
                }
                else
                {
                    Debug.LogWarning("WWise_VO: Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
                }
            } 
    }

    private IEnumerator VoiceTestCoroutine()
    {
        if(inTutorial)
        {
             //I think that the logic of setting the testSuccess to false at the beginning of this coroutine is correct.
            testSuccess = false;
            Debug.Log("Tutorial: Voice Test Coroutine");
            //First, wait one second, to give room for the cue to be triggered.
            float _tWait = 0.0f;

            if(debugAllowLogs)
            {
                Debug.Log("Tutorial: About to test...");
            }

            while(_tWait < 3.0f)
            {
                _tWait += Time.deltaTime;
                yield return null;
            }

            while(!imitoneVoiceInterpreter.gameOn)
            {
                //wait for the previous guidance to end
                yield return null;
            }

            if(debugAllowLogs)
            {
                Debug.Log("Tutorial: Testing...");
            }

            float _failTimer = 0.0f;
            while(!testSuccess)
            {
                //waiting for success...
                if(!imitoneVoiceInterpreter.toneActiveBiasTrue)
                {
                    //...while testing for failure
                    _failTimer += Time.deltaTime;
                    if(_failTimer > failThreshold)
                    {
                        Debug.Log("Tutorial: TEST FAIL");
                        correctionCoroutine = StartCoroutine(ProvideCorrection());                   
                        yield break;
                    }
                }
                yield return null;
            }
            Debug.Log("Tutorial: TEST SUCCESS (wait for breath)");
            while(imitoneVoiceInterpreter.toneActiveBiasTrue)
            {
                yield return null;
            }
            //on success, start the next coroutine
            PlayTutorialGuidance();
            testCoroutine = StartCoroutine(VoiceTestCoroutine());
        } else {
            Debug.Log("Tutorial: Voice Test Coroutine: Tutorial is over");
        }
    }

    private IEnumerator ProvideCorrection()
    {
        testSuccess = false;
        if(debugAllowLogs)
        {
            Debug.Log("Tutorial: Provide Correction, playing guidance...");
        }
        
        musicSystem1.LockToC(true);

        PlayCorrectionGuidance(); 
        //Wait one second, to give room for the cue to be triggered.
        float _tWait = 0.0f;
        while(_tWait < 1.0f)
        {
            _tWait += Time.deltaTime;
            yield return null;
        }
     
        while(!imitoneVoiceInterpreter.gameOn)
        {
            //wait for the correction guidance to end
            yield return null;
        }

        if(debugAllowLogs)
        {
            Debug.Log("Tutorial: Testing correction...");
        }
        float _failTimer = 0.0f;
        while(!testSuccess)
        {
            //waiting for success...
            if(!imitoneVoiceInterpreter.toneActiveBiasTrue)
            {
                //...while testing for failure
                _failTimer += Time.deltaTime;
                if(_failTimer > failThreshold)
                {
                    Debug.Log("Tutorial: CORRECTION TEST FAIL");
                    correctionCoroutine = StartCoroutine(ProvideCorrection());                   
                    yield break;
                }
            }
            yield return null;
        }
        Debug.Log("Tutorial: CORRECTION TEST SUCCESS (wait for breath...)");
        while(imitoneVoiceInterpreter.toneActiveBiasTrue)
        {
            yield return null;
        }
        if(debugAllowLogs)
        {
            Debug.Log("Tutorial: Play correction confirmation vo");
        }
        if(testVocalizationType == "Advanced")
        {
            musicSystem1.LockToC(false);
        }
        AkSoundEngine.PostEvent("Play_VO_testRepair_succeed", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
        testCoroutine = StartCoroutine(VoiceTestCoroutine());
    }

    private void PlayTutorialGuidance()
    {
        
        if(debugAllowLogs)
        {
            Debug.Log("Tutorial: Play " + testVocalizationType + " Guidance");
        }
        
        switch(testVocalizationType)
        {
            case "Hum":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationHum", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
                break;
            case "Ahh":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAhh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
                break;
            case "Ohh":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationOhh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
                break;
            case "Advanced":
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAdvanced", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
                break;
            default:
                Debug.LogError("Invalid testVocalizationType: " + testVocalizationType);
                break;
        }
    }

    private void PlayCorrectionGuidance()
    {
        
        if(debugAllowLogs)
        {
            Debug.Log("Tutorial: Play " + testVocalizationType + " Correction Guidance");
        }

        switch(testVocalizationType)
        {
            case "Hum":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Hum", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
                break;
            case "Ahh":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Ahh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
                break;
            case "Ohh":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Ohh", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
                break;
            case "Advanced":
                AkSoundEngine.PostEvent("Play_VO_testRepair_Extended", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
                break;
            default:
                Debug.LogError("Invalid testVocalizationType: " + testVocalizationType);
                break;
        }
    }

    private void EndTutorial()
    {
        Debug.Log("TutorialEnded");
        // Run this when the cue for the end of the tutorial hits.
        inTutorial = false;

        if (testCoroutine != null)
        {
            StopCoroutine(testCoroutine);
        }
        if (correctionCoroutine != null)
        {
            StopCoroutine(correctionCoroutine);
        }
        sequencer.BeginMusicSequence(TimeTrackerScript.TotalElapsedTime);
        musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
        director.disable = false;
        active = false;
        Debug.Log("TUTORIAL: END with" + TimeTrackerScript.TotalElapsedTime);
    }
}