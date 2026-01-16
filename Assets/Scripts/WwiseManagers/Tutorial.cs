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
    public WorldShuffler worldShuffler;
    public CSVLoader csvLoader;
    public TimeLeftScript timeLeftScript;
    public bool active {get; private set;}  = false; //currently, this is just for external objects to view if the tutorial is doing anything or not, all the actual behaviors are in StartTutorial() and EndTutorial()
    float testThreshold = 1.5f;
    float failThreshold = 8.0f;
    private bool testSuccess = false;
    public string testVocalizationType;
    public string testVocalizationTypeLastFrame;
    private Coroutine testCoroutine;
    private Coroutine correctionCoroutine;
    public bool inTutorial = false;
    public bool tutorialComplete = false;
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
        }        

        if(testVocalizationType != testVocalizationTypeLastFrame)
        {
           if(testVocalizationType == "Advanced")
            {
                musicSystem1.SetMusicSilentLayerVolume(musicSystem1._silentVolumeHigh, 40f);
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
    
    //TODO: Move StartTutorial() to Sequencer.cs, and the call for it, which right now is in WwiseVOManager.cs, should reference something in Sequencer.cs. Basically. Sequencer wants to control the sequence of events.
    //TODO: With that in mind, move inTutorial to Sequencer.cs. Sequencer should be able to tell us where in the sequence we are: Opening, Tutorial, Freeplay, Savasana.
    public void StartTutorial()
    {
        if(!active)
        {
            inTutorial = true;
            Debug.Log("Tutorial: START");
            active = true;
            SetTestVocalizationType("Hum");

            musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.Tutorial);

            sequencer.InitializeLights(); //this is probably already initialized, just making sure.
            
            testCoroutine = StartCoroutine(VoiceTestCoroutine());

        }
    }

    public void TutorialCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
         if (in_type == AkCallbackType.AK_MusicSyncUserCue)
            {
                AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;

                //NOTE: I've moved everything from here into WwiseVOManager.cs, am just keeping this here to catch anything unexpected so we can fix it.
             
                Debug.LogWarning("Tutorial: Unexpected Wwise Cue: " + in_type + " | " + musicSyncInfo.userCueName);
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
                } else {
                    _failTimer = 0.0f;
                }
                yield return null;
            }
            Debug.Log("Tutorial: TEST SUCCESS (wait for breath)");
            while(imitoneVoiceInterpreter.toneActiveBiasTrue)
            {
                yield return null;
            }
            //on success, start the next coroutine
            wwiseVOManager.PlayTutorialGuidance(testVocalizationType);
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
        
        musicSystem1.SetFundamentalModeLock(true, NoteName.C);

        wwiseVOManager.PlayCorrectionGuidance(testVocalizationType); 
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
            } else {
                _failTimer = 0.0f;
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
        if(testVocalizationType != "Hum")
        {
            musicSystem1.SetFundamentalModeLock(false);
        }
        wwiseVOManager.PlayCorrectionConfirmationVO();
        testCoroutine = StartCoroutine(VoiceTestCoroutine());
    }

    public void EndTutorialNaturally()
    {
        StopTutorial();
        musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
        
        if(!worldShuffler.shuffling)
        {
            worldShuffler.BeginShuffle();
        }
        director.disable = false;
        Debug.Log("TUTORIAL: END naturally with " + TimeTrackerScript.TotalElapsedTime);
    }

    public void StopTutorial()
    {
        if (inTutorial)
        {
            Debug.Log("Tutorial: Stopping");
            inTutorial = false;
            tutorialComplete = true;
            active = false;

            if (testCoroutine != null)
            {
                StopCoroutine(testCoroutine);
            }
            if (correctionCoroutine != null)
            {
                StopCoroutine(correctionCoroutine);
            }
        }
        else
        {
            Debug.Log("Tutorial: StopTutorial called, but tutorial is not active.");
        }
    }

    public void SetTestVocalizationType(string vocalizationType)
    {
        //first, check if the string is a supported type
        if (vocalizationType != "Hum" && vocalizationType != "Ahh" && vocalizationType != "Ohh" && vocalizationType != "Advanced")
        {
            Debug.LogError("Tutorial: Invalid vocalization type: " + vocalizationType);
            return;
        }
        else
        {
            testVocalizationType = vocalizationType;
        }
    }
}