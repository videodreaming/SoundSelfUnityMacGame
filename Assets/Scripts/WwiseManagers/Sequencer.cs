using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class Sequencer : MonoBehaviour
{
    public DevelopmentMode developmentMode;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    //public RecordedAudioPlaybackTest recordedAudioPlaybackTest;
    public MusicSystem1 musicSystem1;
    public LightControl lightControl;
    public RespirationTracker respirationTracker;
    public WwiseVOManager wwiseVOManager;
    public Director director;
    public Tutorial tutorial;
    //private int fundamentalCount = -1;
    //private int harmonyCount = -1;
    public SavasanaPlayer savasana;

    //public uint playingId;
    //[SerializeField]
    //private int currentStage = 0; // Tracks the current stage of the sound world
    public CSVWriter csvWriter;
    // AVS Controls
    private float _absorptionThreshold;
    private float d = 1f; //debug timer mult, higher makes it go faster for testing
    private int debugWorldCount = 0;

    //THINGS THAT PERTAIN TO STORY PROGRESSION    

    //private float interactiveMusicExperienceTotalTime;
    private float WakeUpCounter;
    private bool wakeUpEndSoonTriggered = false; // Flag to control the event triggering
    //private float soundWorldChangeTime;
    //private float finalStagePreLogicTime;
    //private bool finalStagePreLogicExecuted = false; 
    private bool flagTriggerEnd1 = false;
    private bool flagTriggerEnd2 = false;
    private bool flagThetaCoroutine = false;
    private List<int> coroutineCleanupList = new List<int>();
    private Coroutine CoroutineDynamicDropStart;
    private Coroutine CoroutineDynamicDropTheta;
    private Coroutine CoroutineDynamicDropEnd;

    private Coroutine countdownCoroutine; // Reference to the coroutines
    private int currentStage = 0; //As Sonoflore
    public float timeInUnguidedVocalization;
    public float totalTimeOfPostUnguidedVocalizationContant;



    void Awake()
    {
        AkSoundEngine.SetState("SoundWorldMode","SonoFlore");

        if(!developmentMode.developmentMode)
        {
            d = 1f;
        }
    }

    void Start()
    {
        if (developmentMode.startRightBeforeSavasana)
        {
            WakeUpCounter = 190f;
        }
        else if (developmentMode.startInSavasana)
        {
            WakeUpCounter = 1f;
            FadeOut();
        }
        else
        {
            WakeUpCounter = 2280.0f;
        }
        
        _absorptionThreshold = UnityEngine.Random.Range(0.08f, 0.35f);
        
        if(!developmentMode.configureMode)
        {
            CoroutineDynamicDropStart = StartCoroutine(AVS_Program_DynamicDrop_Start());
        }
        musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);

    }
   
    // Update is called once per frame
    void Update()
    {

        if(developmentMode.developmentMode)
        {
            if(Input.GetKeyDown(KeyCode.L))
            {
                WakeUpCounter = 190f;
                Debug.Log("Sequencer Wakeup Counter set to " + WakeUpCounter);
            }
        }

        if(WakeUpCounter > -1.0f)
        {
            WakeUpCounter -= Time.deltaTime;
        }
        else
        {
            WakeUpCounter = -1.0f;
        }
       
        //in playground mode, when I press the M button, cycle to the next music world (Gentle, Shadow, Shruti, Sonoflore)
        if(developmentMode.startInPlayground)
        {
            if(Input.GetKeyDown(KeyCode.M))
            {
                debugWorldCount++;
                if(debugWorldCount > 3)
                {
                    debugWorldCount = 0;
                }
                switch(debugWorldCount)
                {
                    case 0:
                        QueueNewWorld("Gentle", "Red", 1f);
                        break;
                    case 1:
                        QueueNewWorld("Shadow", "Blue", 1f);
                        break;
                    case 2:
                        QueueNewWorld("Shruti", "White", 1f);
                        break;
                    case 3:
                        QueueNewWorld("SonoFlore", "Red", 1f);
                        break;
                }
            }
        }
        //REEF - I think the behaviors you are working on in WwiseVOManager.cs bel dong in here, because this is where we deal with other elements of the sequence.
        //... Some of what you areoing could be done with something like what LastMinute() is doing, which triggers in the last minute of the wake up counter. 

        //End Behaviors
        if(!developmentMode.startInSavasana)
        {
            if(WakeUpCounter <= 180f && !flagTriggerEnd1)
            {
                CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End());
                flagTriggerEnd1 = true;
            }

            if(WakeUpCounter <= 60f && !flagTriggerEnd2)
            {
                StartCoroutine(LastMinute());
                flagTriggerEnd2 = true;
            }
        }
        
        if( WakeUpCounter <= 0.0f && !wakeUpEndSoonTriggered)
        {
            AkSoundEngine.PostEvent("Play_WakeUpEndSoon_SEQUENCE", gameObject);
            WakeUpCounter = -1.0f;
            wakeUpEndSoonTriggered = true;
        }
    }

    //====================================================================================================
    //TIMED BEHAVIORS
    //====================================================================================================
    IEnumerator LastMinute()
    {
        Debug.Log("Sequencer Last Minute: Starting Last Minute Behaviors.");
        //recordedAudioPlaybackTest.SetRecordMode(false);
        //recordedAudioPlaybackTest.SetPlaybackMode(false);

        // Wait until toneActiveConfident becomes false
        yield return new WaitUntil(() => !imitoneVoiceInterpreter.toneActiveConfident || WakeUpCounter <= 30f);
        Debug.Log("Sequencer Last Minute: Test 1 (Rest or Time) passed");
        
        // Wait until toneActiveConfident becomes true
        yield return new WaitUntil(() => imitoneVoiceInterpreter.toneActiveConfident || WakeUpCounter <= 30f);
        Debug.Log("Sequencer Last Minute: Test 2 (Tone or Time) passed. Starting Final Behaviors. Wake Up Counter" + WakeUpCounter);
        director.ActivateQueue(15f);
        musicSystem1.PlaygroundMode(false);

        Debug.Log("Sequencer Last Minute: Starting Thematic Savasana.");
        yield return null;
        Debug.Log("wake Up Counter:" + WakeUpCounter);
        yield return new WaitUntil(() => WakeUpCounter <= 15f);
        
        Debug.Log("Sequencer Last Minute: Starting Light Fade-Out.");
        FadeOut();

        while(WakeUpCounter > 0f)
        {
            yield return null;
            Debug.Log("Sequencer Last Minute: Waiting for WakeUpCounter to reach 0.");
        }
    }

    private void FadeOut()
    {
        musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.Environment);
        lightControl.SetPreferredColor("Dark");
        lightControl.NextPreferredColorWorld(18f);
    }
    
    //====================================================================================================
    //MUSIC PROGRESSION
    //====================================================================================================

    //Also noting that the design of this doesn't lend itself easily or naturally to different sequences, using different instrument sets, or a different order, or not all of them, which will become more relevant in the not too distant future, but is relevant even now given the possibility that someone will just "stay" in the tutorial.
    //... The ideal system would have a list of sound worlds to play, and would move through the list. That list could be modified based on (a) the launch initializations and (b) the amount of time left when the sequence initiates
    //This will work for now, but I think the system could be cleaner.
    public void BeginMusicSequence(float currentTime)
    {
        timeInUnguidedVocalization = wwiseVOManager.totalTimeOfExperience - wwiseVOManager.totalTimeOfPostUnguidedVocalizationContant - currentTime;   
        float timeInEachSegment = timeInUnguidedVocalization / 4;
        StartCountdownToNextSegment(timeInEachSegment);
        Debug.Log("WWise_VO: Time in each segment: " + timeInEachSegment);
        Debug.Log("WWise_VO: Time in Unguided Vocalization: " + timeInUnguidedVocalization);
    }
    public void StartCountdownToNextSegment(float timeInEachSegment) //REEF - renamed this for clarity
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownToNextSegmentCoroutine(timeInEachSegment));
    }

    IEnumerator CountdownToNextSegmentCoroutine(float timeInEachSegment)
    {
        //REEF - **Important**, I've changed this to use the director system instead of causing a change right away on the clock. This makes the system more responsive. This way, instead of happening on a precise schedule, the desired change is "queued" and then triggers when an player-driven behavior change happens in the player, so it feels like it is responding to them. Right now, it is set to change  after 60 seconds (that's the 60f) even if there is not behavior change in the player, but I'd recommend this being 120 seconds instead, because it's such a big and important change, and we really want the player to feel it as a response from them. 
        //The system you've designed lends itself to precise, clockwork timing. It should bere-evaluated to work well with the director system, which includes a variable delay. The reconfigured system should calculate the time until the next world-change is added to the queue when the change actually is dynamically triggered. The behavior of setting a new countdown would then have to be triggered by the director system activation event. So you'd put the behavior that starts a new countdown in sequencer.SetSoundWorld. I've put a comment there for you to look at.
        Debug.Log("WWise_VO: Starting Countdown to Next Segment with :" + timeInEachSegment + " | Current Stage: " + currentStage);
        if (currentStage == 0)
        {
            QueueNewWorld("SonoFlore", "Red", 120f);
            currentStage = 1;
            Debug.Log("WWise_VO: Current Stage: " + currentStage + " | Time in each segment: " + timeInEachSegment + " | SoundWorldMode: SonoFlore");
        }
        else if (currentStage == 1)
        {
            QueueNewWorld("Gentle", "Red", 120f);
            currentStage = 2;
            Debug.Log("WWise_VO: Current Stage: " + currentStage + " | Time in each segment: " + timeInEachSegment + " | SoundWorldMode: Gentle");
        }
        else if (currentStage == 2)
        {
            QueueNewWorld("Shadow", "Blue", 120f);
            currentStage = 3;
            Debug.Log("WWise_VO: Current Stage: " + currentStage + " | Time in each segment: " + timeInEachSegment + " | SoundWorldMode: Shadow");
            
        } else if (currentStage == 3)
        {
            QueueNewWorld("Shruti", "White", 120f);
            currentStage = 4;
            Debug.Log("WWise_VO: Current Stage: " + currentStage + " | Time in each segment: " + timeInEachSegment + " | SoundWorldMode: Shruti");
        } else if (currentStage == 4)
        {
            Debug.Log("PlayingThematicSavasana");
            savasana.PlayThematicSavasana();
            yield break; // End the coroutine here to avoid further countdown logic.
        }

        if (currentStage < 4)
        {
            yield return new WaitForSeconds(timeInEachSegment);
            StartCountdownToNextSegment(timeInEachSegment);
            Debug.Log("Starting Countdown to Next Segment with :" + timeInEachSegment);
        }
        else if (currentStage == 4) // Ensure this logic does not conflict with LastMinute
        {
            Debug.Log("Starting Countdown to Next Segment with -60f :" + timeInEachSegment);
            yield return new WaitForSeconds(timeInEachSegment - 60f);
            StartCountdownToNextSegment(timeInEachSegment-60f);
        }
    }

    //====================================================================================================
    //LIGHT CONTROL
    //====================================================================================================
    IEnumerator AVS_Program_DynamicDrop_Start()
    {
        bool stopProgression = false;
        Cleanup(coroutineCleanupList); //not necessary for the first one, but placing it here for convention.
        yield return null;
        //define a list of integers to hold the director queue index items that are created in this coroutine
       
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDropStart. Waiting for lights. Currently:" + lightControl.currentColorType);

        if(developmentMode.startInPlayground || developmentMode.startRightBeforeSavasana)
        {
            lightControl.SetColorWorldByType("Red", 0.0f);
        }

        //WAIT UNTIL WE CHANGE TO A REAL COLOR TYPE, WHICH USUALLY HAPPENS ON THE FIRST HUM, IN WWISEVOMANAGER.
        while((lightControl.currentColorType == "Dark") || (lightControl.currentColorType == "BreathOnly"))
        {
            yield return null;
        }
        //ONCE THE LIGHTS TURN ON, START AT 45HZ
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDropStart. Lights detected, set strobe to 45hz.");
        lightControl.SetStrobeRate(45.0f, 0.0f);
        float _timer = 10f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //AFTER 10 SECOND HOLD IS FINISHED, DROP TO 11HZ OVER 30 SECONDS
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDropStart. Initializeing drop from gamma to high alpha.");
        _timer = 30f / d;
        lightControl.SetStrobeRate(11.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //NOW TAKE 120 SECONDS TO DROP TO 8.5HZ
        //FOLLOWING THIS POINT, IF THE ABSORPTION THRESHOLD IS MET, WE WILL SKIP TO THE NEXT PROGRAM
        _timer = 150f / d;
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDropStart. Begining drop from high alpha to 10hz.");
        lightControl.SetStrobeRate(8.5f, _timer);
        while(_timer > 0 || stopProgression)
        {
            if(AVS_Program_ManageThetaTransition(coroutineCleanupList))
            {
                stopProgression = true;
                break;
            }
            _timer -= Time.deltaTime;
            yield return null;
        }
        yield return null;
        //NOW START A SAW STROBE COROUTINE AROUND ALPHA
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDropStart. Starting Saw Strobe Coroutine.");
        float _wavelength = 360f / d;
        float _halfWavelength = _wavelength / 2;
        lightControl.SetSawStrobe(8.5f, 11.5f, _wavelength);
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDropStart. Waiting for absorption threshold to be met.");

        _timer = _halfWavelength;
        bool flag1 = false;
        bool flag2 = false;
        while(stopProgression)
        {
            if(AVS_Program_ManageThetaTransition(coroutineCleanupList))
            {
                stopProgression = true;
                break;
            }

            //ADD SOME MONO/STEREO BEHAVIOR. RHYTHMICALLY ADD MONO/STEREO COMMANDS TO DIRECTOR QUEUE
            //MONO ACTIVATES AT END, STEREO DOES NOT. COMMANDS ARE EXCLUSIVE.
            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
            }
            else
            {
                _timer = _halfWavelength;
            }
            if(_timer > _halfWavelength*3/4)
            {
                flag2 = false;
                if(!flag1)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60.0f, false, 2));
                    flag1 = true;
                }
            }
            else if(_timer <= _halfWavelength*3/4)
            {
                flag1 = false;
                if(!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, true, 2));
                    flag2 = true;
                }
            }
            yield return null;
        }
        
    }

    private bool AVS_Program_ManageThetaTransition(List<int> coroutineCleanupList)
    {
        bool forceIt = developmentMode.developmentMode && Input.GetKeyDown(KeyCode.K);
        if((respirationTracker._absorption > _absorptionThreshold || forceIt) && !flagThetaCoroutine)
        {
            flagThetaCoroutine = true;
            CoroutineDynamicDropTheta = StartCoroutine(AVS_Program_DynamicDrop_Theta());
            return true;
        }
        return false;
    }

    IEnumerator AVS_Program_DynamicDrop_Theta()
    {
        if(CoroutineDynamicDropStart != null)
        {
            Debug.Log(WakeUpCounter + "Sequencer | AVS Program: Stopping Coroutine from THETA Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
          
        yield return null;
        Cleanup(coroutineCleanupList);
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Starting Theta program.");

        //SET CORRECT MONO/STEREO
        director.ClearQueueOfType("monostereo");
        //add mono to queue, if we're in bilateral
        if(lightControl.bilateral)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, true, 2));
            Debug.Log(WakeUpCounter + " Sequencer Director Queue: (AVS Program) DynamicDrop_Theta. Since starting in bilateral, adding " + (director.queueIndex - 1) + " monostereo=mono to director queue, and waiting.");
            director.LogQueue();
        }
        //and wait to enter mono...
        while(lightControl.bilateral)
        {
            yield return null;
        }
        //DROP TO 7HZ
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 7hz.");
        float _timer = 120f / d;
        lightControl.SetStrobeRate(7.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //CYCLE THROUGH BILATERAL ONCE
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Queueing Bilateral Strobe.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60f/d, true, 2));
        while(!lightControl.bilateral)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, true, 2));
        while(lightControl.bilateral)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Queueing Mono Strobe.");
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, true, 2));
        while(lightControl.bilateral)
        {
            yield return null;
        }
        //DROP TO 6HZ
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 6hz.");
        _timer = 60f / d;
        lightControl.SetStrobeRate(6.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //GAMMA BURSTS, THEN HANG HERE. 
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Queuing Gamma Burst.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f/d, true, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Queuing Gamma Burst Stop.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 180f/d, true, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //DROP TO 5HZ
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 5hz.");
        _timer = 60f / d;
        lightControl.SetStrobeRate(5.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //NOW CYCLE THROUGH GAMMA BETWEEN GAMMA AND NOT-GAMMA, AS WE WERE DOING WITH BILATERAL IN THE LAST PROGRAM
        _timer = 300f / d;
        float _halfWave = _timer / 2;
        bool flag1 = false;
        bool flag2 = false;
        while(true)
        {
            if(_timer > 0)
            {
                _timer -= Time.deltaTime;
            }
            else
            {
                _timer = 300f / d;
            }
            if(_timer > _halfWave)
            {
                flag2 = false;
                if(!flag1)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f/d, true, 2));
                    flag1 = true;
                    Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst.");
                }
            }
            else if(_timer <= _halfWave)
            {
                flag1 = false;
                if(!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 60f/d, true, 2));
                    flag2 = true;
                    Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst Stop.");
                }
            }
            yield return null;
        }
    }

    IEnumerator AVS_Program_DynamicDrop_End()
    {
        if(CoroutineDynamicDropStart != null)
        {
            Debug.Log(WakeUpCounter + "Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
        if(CoroutineDynamicDropTheta != null)
        {
            Debug.Log(WakeUpCounter + "Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropTheta);
        }

        yield return null;
        Cleanup(coroutineCleanupList);
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_End. Starting End Program.");
        director.ClearQueueOfType("gamma");
        director.ClearQueueOfType("monostereo");
        if(!lightControl.bilateral)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 30.0f, true, 2));
        }
        if(lightControl._gammaBurstMode != 0.0f)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 30.0f, true, 2));
        }

        while(WakeUpCounter > 110f)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_End. Stabilizing before dramatic rise.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 10.0f, true, 2));

        while(WakeUpCounter > 90f)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_End. Starting dramatic rise to 40hz.");
        lightControl.SetStrobeRate(40.0f, 90f);

        while(WakeUpCounter > 0f)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "Sequencer | AVS Program: DynamicDrop_End. End of AVS Program. Goodnight!");
    }

    private void Cleanup(List<int> coroutineCleanupList)
    {
        Debug.Log("Performing cleanup.");
        foreach (int index in coroutineCleanupList)
        {
            Debug.Log(WakeUpCounter + "Sequencer  Director Queue (AVS Program): DynamicDrop (Transitioning). Removing " + index + " " + director.queue[index].Item2);
            director.queue.Remove(index);
        }
        director.LogQueue();
    }
    
    //====================================================================================================
    //DIRECTOR QUEUE
    //====================================================================================================
    public void QueueNewWorld(string world, string color, float _seconds = 120.0f)
    {
        Debug.Log("Sequencer QueueNewWorld: Queuing New World: " + world + " with color: " + color);
        director.AddActionToQueue(Action_SetSoundWorld(world), "SoundWorld", true, false, _seconds, true, 2);
        director.AddActionToQueue(Action_SetPreferredColor(color), "ColorPreference", false, true, _seconds, true, 2);
        director.AddActionToQueue(Action_NextColorWorld(120.0f), "ColorCycle", false, true, _seconds, true, 2);
        director.AddActionToQueue(Action_PlayTransitionSound(), "TransitionSound", true, false, _seconds, true, 2);
    }

    private Action Action_SetSoundWorld(string soundWorld)
    {
        return () => SetSoundWorld(soundWorld);
    }

    private void SetSoundWorld(string soundWorld)
    {
        AkSoundEngine.SetState("SoundWorldMode", soundWorld);
        Debug.Log("Sequencer Sound World Set To: " + soundWorld);
        //REEF, if you want something to happen when the sound world actually changes, it should be here.
    }

    private Action Action_SetPreferredColor(string color)
    {
        return () => lightControl.SetPreferredColor(color);
    }

    private Action Action_NextColorWorld(float _seconds)
    {
        return () => lightControl.NextPreferredColorWorld(_seconds);
    }
    private Action Action_Gamma(bool gammaOn)
    {
        return () => lightControl.Gamma(gammaOn);
    }
    private Action Action_Strobe_MonoStereo(bool bilateral = false)
    {
        return () => lightControl.Strobe_MonoStereo(bilateral);
    }
    private Action Action_Strobe_Frequency(float frequency, float seconds)
    {
        return () => lightControl.SetStrobeRate(frequency, seconds);
    }
    private Action Action_PlayTransitionSound()
    {
        return () => director.PlayTransitionSound();
    }


}
