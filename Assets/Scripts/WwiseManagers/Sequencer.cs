using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class Sequencer : MonoBehaviour
{
    public DevelopmentMode developmentMode;
    public CSVLoader csvLoader;
    public TimeLeftScript timeLeftScript;
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
    public WorldShuffler worldShuffler;
    public CSVWriter csvWriter;
    private bool lightsInitialized = false;
    

    //public uint playingId;
    //[SerializeField]
    //private int currentStage = 0; // Tracks the current stage of the sound world
    // AVS Controls
    private float _absorptionThreshold;
    private float d = 1f; //debug timer mult, higher makes it go faster for testing

    //THINGS THAT PERTAIN TO STORY PROGRESSION    

    //private float interactiveMusicExperienceTotalTime;
    public float _countdownToSavasana;
    public float _timeSinceTutorial;
    private bool savasanaTriggered = false; // Flag to control the event triggering
    private bool wakeUpTriggered = false;
    public float _countdownToWakeUpEnd = 120f; 
    //private float soundWorldChangeTime;
    //private float finalStagePreLogicTime;
    //private bool finalStagePreLogicExecuted = false; 
    private bool flagTriggerStart1 = false;
    private bool flagTriggerStart2 = false;
    private bool flagTriggerEnd1 = false;
    private bool flagTriggerEnd2 = false;
    private bool flagTriggerEnd3 = false;
    private bool flagThetaCoroutine = false;
    public bool lastMinuteTriggered {get; private set;} = false; 
    private List<int> coroutineCleanupList = new List<int>();
    private Coroutine CoroutineDynamicDropStart;
    private Coroutine CoroutineDynamicDropTheta;
    private Coroutine CoroutineDynamicDropEnd;

    private Coroutine countdownCoroutine; // Reference to the coroutines
    private int currentStage = 0; //As Sonoflore
    public float timeInUnguidedVocalization;
    //public float totalTimeOfExperience;

    void Awake()
    {
        musicSystem1.SetSoundWorld("SonoFlore");

        if(!developmentMode.developmentMode)
        {
            d = 1f;
        }
    }

    void Start()
    {
        //These initializations should all be in CSVLoader.cs. Suggest not making _countdownToSavasana public, but initialize it with a public Method.
        if (developmentMode.startRightBeforeSavasana)
        {
            tutorial.tutorialComplete = true;
            worldShuffler.BeginShuffle(false);
            _countdownToSavasana = 190f;
            Debug.Log("Sequencer: ThematicSavasanaCountdown Counter set to " + _countdownToSavasana + " for debug.");
            flagTriggerStart1 = true;
            flagTriggerStart2 = true;
        }
        else if (developmentMode.startInSavasana)
        {
            tutorial.tutorialComplete = true;
            _countdownToSavasana = 1f;
            Debug.Log("Sequencer: ThematicSavasanaCountdown Counter set to " + _countdownToSavasana + " for debug.");
            FadeOut();
            
            flagTriggerStart1 = true;
            flagTriggerStart2 = true;
        }
        else if (developmentMode.startInTutorial)
        {
            tutorial.StartTutorial();
            InitializeLights();
            worldShuffler.ExcludeColorWorld("Blue");
            worldShuffler.ExcludeMusicWorld("Shadow");
        }
        else if (developmentMode.startInPlayground)
        {
            tutorial.tutorialComplete = true;
            worldShuffler.BeginShuffle(false);
            InitializeLights();
        }
        else
        {
            musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.Silent);          
            director.disable = true;
            worldShuffler.ExcludeColorWorld("Blue");
            worldShuffler.ExcludeMusicWorld("Shadow");
        }
        
        Debug.Log("Sequencer: ThematicSavasanaCountdown Counter starts at " + _countdownToSavasana);
        
        _absorptionThreshold = UnityEngine.Random.Range(0.08f, 0.35f);
        
        if(!developmentMode.configureMode)
        {
            Debug.Log("AVS_Program_DynamicDrop_Start is starting");
            CoroutineDynamicDropStart = StartCoroutine(AVS_Program_DynamicDrop_Start());
        }

        //PLAY OPENING SEQUENCE
        if(!developmentMode.developmentMode || developmentMode.startAtStart)
        {
            if(CSVLoader.gameMode == "Preparation" || CSVLoader.gameMode == "Skills Training")
            {
                if(csvLoader.firstTimeUser)
                {
                    wwiseVOManager.PlayOpeningSequence("Preparation_Long");
                }
                else
                {
                    wwiseVOManager.PlayOpeningSequence("Preparation_Short");
                }
            }
            else if (CSVLoader.gameMode == "Integration")
            {
                wwiseVOManager.PlayOpeningSequence("Integration_Short");
            }
            else
            {
                Debug.LogWarning("Sequencer: No Opening Sequence for this game mode.");
            }
        }
        else
        {
            Debug.Log("Sequencer: (DEVELOPMENT) skipping opening sequence");
        }
        

    }
   
    // Update is called once per frame
    void Update()
    {
        if(developmentMode.developmentMode)
        {
            if(Input.GetKeyDown(KeyCode.L))
            {
                _countdownToSavasana = 190f;
                Debug.Log("Sequencer ThematicSavasanaCountdown Counter set to " + _countdownToSavasana);
            }
        }

        //add time to the _timeSinceTutorial counter, once the tutorial has been completed
        if(tutorial.tutorialComplete)
        {
            _timeSinceTutorial += Time.deltaTime;
        }
       
        //Early Behaviors
        if(_timeSinceTutorial <= 60 && !flagTriggerStart1)
        {
            Debug.Log("Sequencer: Triggering Start1 Behaviors: Reset Music Worlds for Shuffle");
            worldShuffler.ResetMusicWorlds();
            flagTriggerStart1 = true;
        }
        if(_timeSinceTutorial <= 300 && !flagTriggerStart2)
        {
            Debug.Log("Sequencer: Triggering Start2 Behaviors: Reset Color Worlds for Shuffle");
            worldShuffler.ResetColorWorlds();
            flagTriggerStart2 = true;
        }

        //End Behaviors
        if(!developmentMode.startInSavasana)
        {
            if(_countdownToSavasana <= 300 && !flagTriggerEnd1)
            {
                Debug.Log("Sequencer: Triggering End1 Behaviors: No Shadow or Shruti Allowed");
                worldShuffler.ResetMusicWorlds();
                worldShuffler.ExcludeMusicWorld("Shadow");
                worldShuffler.ExcludeMusicWorld("Shruti"); //removing shruti, as we want it to go last
                flagTriggerEnd1 = true;
                flagTriggerStart1 = true;
                flagTriggerStart2 = true;
            }
            if(_countdownToSavasana <= 180f && !flagTriggerEnd2)
            {
                Debug.Log("Sequencer: Triggering End2 Behaviors: Queue Shruti, Close Music Queue, Start AVS End Sequence");
                //finally, queue shruti and prevent further queueing of shuffled sound worlds.
                director.AddActionToQueue(musicSystem1.Action_SetSoundWorld("Shruti"), "SoundWorld", true, false, 180.0f, true, 2);
                director.AddActionToQueue(director.Action_PlayTransitionSound(), "TransitionSound", true, false, 180.0f, true, 2);
                worldShuffler.CloseMusicQueue();
                CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End());
                flagTriggerEnd2 = true;
            }

            if(_countdownToSavasana <= 60f && !flagTriggerEnd3)
            {
                Debug.Log("Sequencer: Triggering End3 Behaviors: Start Last Minute Behaviors");
                StartCoroutine(LastMinute());
                flagTriggerEnd3 = true;
            }
        }
        
        //THEMATIC SAVASANA TIMER AND TRIGGER
        
        if(_countdownToSavasana > 0f)
        {
            _countdownToSavasana -= Time.deltaTime;
        }
        else if(_countdownToSavasana <= 0.0f && !savasanaTriggered)
        {
            savasana.PlayThematicSavasana();
            _countdownToSavasana = -1.0f;
            savasanaTriggered = true;
        }

        //WAKE UP FROM SILENT MEDITATION TIMER AND TRIGGER
        if((twoMinMeditationTimer == true) && (_countdownToWakeUpEnd > 0))
        {
            _countdownToWakeUpEnd -= Time.deltaTime;
        }
        else if(_countdownToWakeUpEnd <= 0.0 && !wakeUpTriggered)
        {
            wwiseVOManager.PlayWakeUpSoonVO();   
            _countdownToWakeUpEnd = -1.0f;     
            wakeUpTriggered = true;
        }
    }

    //====================================================================================================
    //TIMED BEHAVIORS
    //====================================================================================================
    IEnumerator LastMinute()
    {
        Debug.Log("Sequencer Last Minute: Starting Last Minute Behaviors.");
        lastMinuteTriggered = true;
        worldShuffler.StopShuffle(); //we should be in Shruti now.
        //recordedAudioPlaybackTest.SetRecordMode(false);
        //recordedAudioPlaybackTest.SetPlaybackMode(false);
        //PLAY SOUND FOR TRANSITIONING TO SAVASANA
    
        AkSoundEngine.PostEvent("Play_sfx_EndInteractive", gameObject);
        // Wait until toneActiveConfident becomes false
        yield return new WaitUntil(() => !imitoneVoiceInterpreter.toneActiveConfident || _countdownToSavasana <= 30f);
        Debug.Log("Sequencer Last Minute: Test 1 (Rest or Time) passed");
        
        // Wait until toneActiveConfident becomes true
        yield return new WaitUntil(() => imitoneVoiceInterpreter.toneActiveConfident || _countdownToSavasana <= 30f);
        Debug.Log("Sequencer Last Minute: Test 2 (Tone or Time) passed. Starting Final Behaviors. Wake Up Counter" + _countdownToSavasana);
        director.ActivateQueue(15f);
        director.disable = true;
        musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.FrozenFreeplay);

        //musicSystem1.PlaygroundMode(false);

        Debug.Log("Sequencer Last Minute: Starting Thematic Savasana.");
        yield return null;
        Debug.Log("wake Up Counter:" + _countdownToSavasana);
        yield return new WaitUntil(() => _countdownToSavasana <= 15f);
        
        Debug.Log("Sequencer Last Minute: Starting Light Fade-Out. Waiting for _countdownToSavasana to reach 0.");
        FadeOut();

        while(_countdownToSavasana > 0f)
        {
            yield return null;
        }
        Debug.Log("Sequencer Last Minute: Starting Thematic Savasana, and ending coroutine");
        timeLeftScript.SetTimeLeftSeconds(csvLoader.totalTimeOfPostUnguidedVocalizationContent);
    }

    private void FadeOut()
    {
        musicSystem1.SetMusicModeTo(MusicSystem1.MusicMode.Environment);
        lightControl.SetPreferredColor("Dark", 18f);
    }
    
    

    //====================================================================================================
    //LIGHT CONTROL
    //====================================================================================================

    
    public void InitializeLights()
    {
        if(!lightsInitialized)
        {
            Debug.Log("Sequencer: InitializeLights");
            lightControl.SetPreferredColor("Red", 5.0f);
            lightsInitialized = true;
        }
    }

    IEnumerator AVS_Program_DynamicDrop_Start()
    {
        bool stopProgression = false;
        Cleanup(coroutineCleanupList); //not necessary for the first one, but placing it here for convention.
        yield return null;
       
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDropStart. Waiting for lights. Currently:" + lightControl.currentColorType);

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
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDropStart. Lights detected, set strobe to 45hz.");
        lightControl.SetStrobeRate(45.0f, 0.0f);
        float _timer = 10f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //AFTER 10 SECOND HOLD IS FINISHED, DROP TO 11HZ OVER 30 SECONDS
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDropStart. Initializeing drop from gamma to high alpha.");
        _timer = 30f / d;
        lightControl.SetStrobeRate(11.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //NOW TAKE 150 SECONDS TO DROP TO 8.5HZ
        //FOLLOWING THIS POINT, IF THE ABSORPTION THRESHOLD IS MET, WE WILL SKIP TO THE NEXT PROGRAM
        _timer = 150f / d;
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDropStart. Begining drop from high alpha to 10hz.");
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
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDropStart. Starting Saw Strobe Coroutine.");
        float _wavelength = 360f / d;
        float _halfWavelength = _wavelength / 2;
        lightControl.SetSawStrobe(8.5f, 11.5f, _wavelength);
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDropStart. Waiting for absorption threshold to be met.");

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
            Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: Stopping Coroutine from THETA Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
          
        yield return null;
        Cleanup(coroutineCleanupList);
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Starting Theta program.");

        //SET CORRECT MONO/STEREO
        director.ClearQueueOfType("monostereo");
        //add mono to queue, if we're in bilateral
        if(lightControl.bilateral)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, true, 2));
            Debug.Log(_countdownToSavasana + " Sequencer Director Queue: (AVS Program) DynamicDrop_Theta. Since starting in bilateral, adding " + (director.queueIndex - 1) + " monostereo=mono to director queue, and waiting.");
            director.LogQueue();
        }
        //and wait to enter mono...
        while(lightControl.bilateral)
        {
            yield return null;
        }
        //DROP TO 7HZ
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 7hz.");
        float _timer = 120f / d;
        lightControl.SetStrobeRate(7.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //CYCLE THROUGH BILATERAL ONCE
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Queueing Bilateral Strobe.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60f/d, true, 2));
        while(!lightControl.bilateral)
        {
            yield return null;
        }
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, true, 2));
        while(lightControl.bilateral)
        {
            yield return null;
        }
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Queueing Mono Strobe.");
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, true, 2));
        while(lightControl.bilateral)
        {
            yield return null;
        }
        //DROP TO 6HZ
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 6hz.");
        _timer = 60f / d;
        lightControl.SetStrobeRate(6.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //GAMMA BURSTS, THEN HANG HERE. 
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Queuing Gamma Burst.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f/d, true, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Queuing Gamma Burst Stop.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 180f/d, true, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //DROP TO 5HZ
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 5hz.");
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
                    Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst.");
                }
            }
            else if(_timer <= _halfWave)
            {
                flag1 = false;
                if(!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 60f/d, true, 2));
                    flag2 = true;
                    Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst Stop.");
                }
            }
            yield return null;
        }
    }

    IEnumerator AVS_Program_DynamicDrop_End()
    {
        if(CoroutineDynamicDropStart != null)
        {
            Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
        if(CoroutineDynamicDropTheta != null)
        {
            Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropTheta);
        }

        yield return null;
        Cleanup(coroutineCleanupList);
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_End. Starting End Program.");
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

        while(_countdownToSavasana > 110f)
        {
            yield return null;
        }
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_End. Stabilizing before dramatic rise.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 10.0f, true, 2));

        while(_countdownToSavasana > 90f)
        {
            yield return null;
        }
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_End. Starting dramatic rise to 40hz.");
        lightControl.SetStrobeRate(40.0f, 90f);

        while(_countdownToSavasana > 0f)
        {
            yield return null;
        }
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDrop_End. End of AVS Program. Goodnight!");
    }

    private void Cleanup(List<int> coroutineCleanupList)
    {
        Debug.Log("Performing cleanup.");
        foreach (int index in coroutineCleanupList)
        {
            Debug.Log(_countdownToSavasana + "Sequencer  Director Queue (AVS Program): DynamicDrop (Transitioning). Removing " + index + " " + director.queue[index].Item2);
            director.queue.Remove(index);
        }
        director.LogQueue();
    }
    
    //====================================================================================================
    //DIRECTOR QUEUE
    //====================================================================================================
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
}
