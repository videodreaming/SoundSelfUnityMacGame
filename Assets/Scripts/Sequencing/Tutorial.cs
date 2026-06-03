using System.Collections;

using System.Collections.Generic;

using System.Data.Common;

using UnityEngine;

using AK.Wwise;

using Unity.VisualScripting;

using ConversionUtilities;

using SoundSelf.Sequence;





public class Tutorial : MonoBehaviour

{

    private bool debugAllowLogs = true;

    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;

    //public RecordedAudioPlayback recordedAudioPlayback;

    public WwiseVOManager wwiseVOManager;

    public Sequencer sequencer;

    public MusicSystem1 musicSystem1;

    public Director director;

    public WorldShuffler worldShuffler;

    public CSVLoader csvLoader;

    public TimeLeftScript timeLeftScript;

    //public bool active {get; private set;}  = false; //currently, this is just for external objects to view if the tutorial is doing anything or not, all the actual behaviors are in StartTutorial() and EndTutorial()

    float testThreshold = 1.5f;

    float failThreshold = 8.0f;

    private bool testSuccess = false;

    public string testVocalizationType { get; private set; } = "Hum";

    public string testVocalizationTypeLastFrame;

    private Coroutine testCoroutine;

    private Coroutine correctionCoroutine;

    private string variant;

    public bool inTutorial { get; private set; } = false;

    public int guidanceCount { get; private set; } = 0;

    //public bool tutorialComplete = false;

    public TimeTrackerScript TimeTrackerScript;



    /// <summary>Vocalization for the active test/correction window (Block 9 — not the next queued segment).</summary>

    private string _vocalizationTypeUnderTest;



    /// <summary>Same source as <see cref="SoundSelf.Sequence.PlaygroundStageHandler"/> — main segment clock, not full session.</summary>

    private float SessionCountdownThisSection()

    {

        if (TimeTrackerScript != null)

            return TimeTrackerScript.CountdownThisSection;

        var inst = global::TimeTrackerScript.instance;

        return inst != null ? inst.CountdownThisSection : float.PositiveInfinity;

    }



    // Start is called before the first frame update

    void Awake()

    {

        //SetTestVocalizationType("Hum");

        //testVocalizationType = "Hum";

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



        if (inTutorial && SessionCountdownThisSection() <= 10.0f)

        {

            Debug.Log("Tutorial: Ending tutorial because [CountdownThisSection] has reached 10f — the main session segment timer (from StartCountdown / TimeTrackerScript) has no time left. " +

                      "Stopping VO/tests and sending TutorialPassed so the sequence advances to Playground even if Wwise cues or Short guidance count have not finished.");

            StopTutorial();

        }

    }

    

    //TODO: With that in mind, move inTutorial to Sequencer.cs. Sequencer should be able to tell us where in the sequence we are: Opening, Tutorial, Freeplay, Savasana.

    public void StartTutorial(string startVariant = null)

    {

        if(!inTutorial)

        {

            

            inTutorial = true;            

            if(startVariant == null)

            {

                Debug.LogError("Tutorial: StartTutorial called with null variant");

                inTutorial = false;

                return;

            }

            else if(startVariant == "Long")

            {

                Debug.Log("Tutorial: StartTutorial: Long");

                failThreshold = 8.0f;

                variant = "Long";

            }

            else if(startVariant == "Short")

            {

                Debug.Log("Tutorial: StartTutorial: Short");

                failThreshold = 24.0f;

                variant = "Short";

            }

            else

            {

                Debug.LogError("Tutorial: Invalid variant: " + startVariant);

                inTutorial = false;

                return;

            }

            

            testCoroutine = StartCoroutine(VoiceTestCoroutine());

            Debug.Log("Tutorial: START");

        }

    }



    private IEnumerator VoiceTestCoroutine()

    {

        

        if(inTutorial)

        {

            

            Debug.Log("Tutorial: Voice Test Coroutine");



            testSuccess = false;

            float _sectionTimer = 0.0f;



            //First, wait three seconds, to give room for the cue to be triggered.

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



            CaptureVocalizationTypeUnderTest();



            if(debugAllowLogs)

            {

                Debug.Log("Tutorial: Testing " + _vocalizationTypeUnderTest + "...");

            }



        

            while(!testSuccess)

            {

                //waiting for success...

                if(!imitoneVoiceInterpreter.toneActiveBiasTrue)

                {

                    //...while testing for failure

                    _sectionTimer += Time.deltaTime;

                    if(_sectionTimer > failThreshold)

                    {

                        Debug.Log("Tutorial: TEST FAIL");

                        correctionCoroutine = StartCoroutine(ProvideCorrection());                   

                        yield break;

                    }

                } else if (variant == "Long") {

                    _sectionTimer = 0.0f;  //I don't know if this is needed, but it was here before I ported this for Ascending...

                }

                yield return null;

            }





            // For "Short" variant: continue to wait until _sectionTimer > failThreshold AND tone is not active

            if (variant == "Short")

            {

                float _waitThreshold = failThreshold;

                Debug.Log($"Tutorial: TEST SUCCESS, just waiting for _sectionTimer to exceed _waitThreshold (time left: {_waitThreshold - _sectionTimer:F2}s)");



                while (_sectionTimer < _waitThreshold)

                {

                    _sectionTimer += Time.deltaTime;

                    yield return null;

                }

            }





            Debug.Log("Tutorial: TEST SUCCESS (wait for breath)");

            while(imitoneVoiceInterpreter.toneActiveBiasTrue)

            {

                yield return null;

            }

            //on success, start the next coroutine



            if(variant == "Short")

            {

                guidanceCount = wwiseVOManager.PlayTutorialGuidance("Lite");

                Debug.Log("Tutorial: (Short) Played Lite guidance, guidanceCount: " + guidanceCount);

                if(guidanceCount >= 4)

                {

                    if(sequencer != null)

                    {

                        sequencer.HandleSequenceCommand(SequenceCommand.TutorialPassed);

                    }

                    // Short ends by guidance count; Long ends via Wwise stage cues (e.g. Break_Tests) after all 19 guidance lines.

                }

            }

            else

            {

                if (guidanceCount >= TutorialStagePolicy.LongGuidanceTotal)

                {

                    Debug.Log("Tutorial: (Long) All " + TutorialStagePolicy.LongGuidanceTotal + " guidance lines delivered; waiting for Wwise stage cues (e.g. Cue_Break_Tests) to end the tutorial.");

                    yield break;

                }



                ApplyLongVocalizationTypeFromGuidanceCount(guidanceCount);

                if (variant == "Long" && imitoneVoiceInterpreter != null)

                    imitoneVoiceInterpreter.SetGameOn(false);

                guidanceCount = wwiseVOManager.PlayTutorialGuidance(testVocalizationType);

                Debug.Log("Tutorial: (Long) Played " + testVocalizationType + " guidance, guidanceCount: " + guidanceCount);

            }



            testCoroutine = StartCoroutine(VoiceTestCoroutine());

        } else {

            Debug.Log("Tutorial: Voice Test Coroutine: Tutorial is over");

        }

    }



    private IEnumerator ProvideCorrection()

    {

        testSuccess = false;

        CaptureVocalizationTypeUnderTest();

        if(debugAllowLogs)

        {

            Debug.Log("Tutorial: Provide Correction for " + _vocalizationTypeUnderTest + "...");

        }

        

        musicSystem1.SetFundamentalModeLock(true, NoteName.C);



        if (variant == "Long")

            imitoneVoiceInterpreter.SetGameOn(false);



        wwiseVOManager.PlayCorrectionGuidance(_vocalizationTypeUnderTest);

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



        CaptureVocalizationTypeUnderTest();



        if(debugAllowLogs)

        {

            Debug.Log("Tutorial: Testing correction for " + _vocalizationTypeUnderTest + "...");

        }

        float _failTimer = 0.0f;

        while(!testSuccess)

        {

            //waiting for success...

            if(!imitoneVoiceInterpreter.toneActiveBiasTrue)

            {

                //...while testing for failure

                _failTimer += Time.deltaTime;

                float correctionFailLimit = variant == "Long" ? (failThreshold + 4.0f) : failThreshold;

                if(_failTimer > correctionFailLimit)

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





    public void StopTutorial()

    {

        if (inTutorial)

        {

            Debug.Log("Tutorial: Stopping");

            inTutorial = false;

            //tutorialComplete = true;

            //active = false;



            if (testCoroutine != null)

            {

                StopCoroutine(testCoroutine);

            }

            if (correctionCoroutine != null)

            {

                StopCoroutine(correctionCoroutine);

            }



            if (sequencer != null)

                sequencer.HandleSequenceCommand(SequenceCommand.TutorialPassed);

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

            Debug.Log("Tutorial: SetTestVocalizationType: " + vocalizationType);

        }

    }



    private void CaptureVocalizationTypeUnderTest()

    {

        if (variant == "Long" && wwiseVOManager != null)

        {

            _vocalizationTypeUnderTest = TutorialStagePolicy.GetVocalizationTypeUnderTestForLong(

                wwiseVOManager.TutorialGuidanceCount);

        }

        else

        {

            _vocalizationTypeUnderTest = testVocalizationType;

        }

    }



    /// <summary>Long only: pick Hum/Ahh/Ohh/Advanced from how many guidance lines have already been posted; apply unlock/shuffle side effects that used to live on Wwise change-type cues.</summary>

    private void ApplyLongVocalizationTypeFromGuidanceCount(int guidanceCountSoFar)

    {

        string nextType = TutorialStagePolicy.GetLongVocalizationTypeForGuidanceCount(guidanceCountSoFar);

        if (nextType == testVocalizationType)

            return;



        string previousType = testVocalizationType;

        ApplyLongVocalizationTransitionSideEffects(previousType, nextType);

        testVocalizationType = nextType;

        Debug.Log("Tutorial: (Long) guidanceCount " + guidanceCountSoFar + " → vocalization type " + nextType + " (was " + previousType + ")");

    }



    private void ApplyLongVocalizationTransitionSideEffects(string fromType, string toType)

    {

        if (fromType == toType)

            return;



        if (toType != "Hum" && musicSystem1 != null)

            musicSystem1.SetFundamentalModeLock(false);



        if (fromType == "Ohh" && toType == "Advanced" && worldShuffler != null && !worldShuffler.shuffling)

            worldShuffler.BeginShuffle();

    }

}


