using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using TMPro;
using System.Security.Cryptography.X509Certificates;
using ConversionUtilities;

public class Sequencer : MonoBehaviour
{
    public CSVLoader csvLoader;
    public StartButtonScript startButtonScript;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;

    public LightControl lightControl;
    public RespirationTracker respirationTracker;
    public WwiseVOManager wwiseVOManager;
    public Director director;
    public Tutorial tutorial;
    public SavasanaPlayer savasana;
    public WorldShuffler worldShuffler;
    public CSVWriter csvWriter;
    private bool lightsInitialized = false;

    public TMP_Dropdown startModeDropdown;


    // AVS Controls
    private float _absorptionThreshold;
    private float d = 1f; //debug timer mult, higher makes it go faster for testing

    //THINGS THAT PERTAIN TO STORY PROGRESSION    

    //private float interactiveMusicExperienceTotalTime;
    public float _countdownToSavasana = 1000000.0f; //initialize at a basically infitite value.
    private float _timeSinceTutorial;
    private bool savasanaCountdownCompleteFlag = false; // Flag to control the event triggering
    [SerializeField] public bool endSoonFlag = false;
    private bool startButtonFlag = false;
    private bool flagTriggerStart1 = false;
    private bool flagTriggerStart2 = false;
    private bool flagTriggerEnd1 = false;
    private bool flagTriggerEnd2 = false;
    private bool flagTriggerEnd3 = false;
    private bool flagTriggerEnd4 = false;
    private bool flagThetaCoroutine = false;
    public bool lastMinuteTriggered {get; private set;} = false; 
    private List<int> coroutineCleanupList = new List<int>();
    private Coroutine CoroutineDynamicDropStart;
    private Coroutine CoroutineDynamicDropTheta;
    private Coroutine CoroutineDynamicDropEnd;
    private bool developmentModeWarningFlag = false;
    private Coroutine countdownCoroutine; // Reference to the coroutines
    private int currentStage = 0; //As SonoFlore
    private bool openingSequenceFlag = false;
    public float timeInUnguidedVocalization;
    
    // Debug log category flags
    private bool debugAllowTimingLogs = true;
    
    // Time tracking for debug logs
    private float _timeSinceStart = 0f;
    private float _lastTimingLogTime = 0f;


    void Awake()
    {
        if(MusicSystem1.instance != null)
        {
            MusicSystem1.instance.SetSoundscape("SonoFlore");  
        }


        if(!(DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode))
        {
            d = 1f;
        }
        else if(DevelopmentMode.instance == null)
        {
            Debug.LogWarning("Sequencer: DevelopmentMode instance is null in Awake().");
        }
        if (startModeDropdown != null)
            startModeDropdown.onValueChanged.AddListener(OnStartModeDropdownChanged);
        
    }

    private void OnDestroy()
    {
        if (startModeDropdown != null)
            startModeDropdown.onValueChanged.RemoveListener(OnStartModeDropdownChanged);
    }

    void Start()
    {
        
        _absorptionThreshold = UnityEngine.Random.Range(0.08f, 0.35f);
        if (_countdownToSavasana >= 999999.0f)
        {
            Debug.LogWarning("Sequencer: _countdownToSavasana was not initialized by CSVLoader or anything else. It's current value is " + _countdownToSavasana + ", which is stupid. Please ensure it is set properly.");
        }
        else
        {
            Debug.Log("Sequencer: ThematicSavasanaCountdown Counter starts at " + _countdownToSavasana);
        }

        if (startModeDropdown != null)
                OnStartModeDropdownChanged(startModeDropdown.value);
    }
// if(DevelopmentMode.Instance != null && DevelopmentMode.Instance.developmentMode)
 // {   //do something  }
    
   
    // Update is called once per frame
    void Update()
    {
        // Track time since start for debug logging
        _timeSinceStart += Time.deltaTime;
        
        // Log timestamp every 1 second as minutes:seconds
        if(debugAllowTimingLogs && _timeSinceStart - _lastTimingLogTime >= 1.0f)
        {
            int totalSeconds = Mathf.RoundToInt(_timeSinceStart);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            Debug.Log($"Sequencer: [tick] {minutes}:{seconds:D2}");
            _lastTimingLogTime = _timeSinceStart;
        }
        
        //add time to the _timeSinceTutorial counter, once the tutorial has been completed
        if(tutorial.tutorialComplete)
        {
            _timeSinceTutorial += Time.deltaTime;
        }

        if(CSVLoader.instance != null)
        {
            if(CSVLoader.instance.gameMode == "Integration" || CSVLoader.instance.gameMode == "Preparation" || CSVLoader.instance.gameMode == "Skills Training")
            {
                StandardSequenceUpdate();
            }
            else if(CSVLoader.instance.gameMode == "Protocol Stacks")
            {
                //ProtocolStacksSequenceUpdate();
            }

            //controlTiming        
            if(_countdownToSavasana > 0f)
            {
                if(startButtonScript != null)
                {
                    if (startButtonScript.startedExperience)
                    {
                        _countdownToSavasana -= Time.deltaTime;
                        if(startButtonFlag == false)
                        {
                            if(DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode)
                            {
                                Debug.Log("Sequencer: Development Mode is ON. Game will not start until commandeded.");
                            }
                            else
                            {
                                StartTrueStart();
                            }        
                            startButtonFlag = true;
                        }
                    }
                }
            }
            else if(_countdownToSavasana <= 0.0f && !savasanaCountdownCompleteFlag)
            {
                _countdownToSavasana = -1.0f;
                savasanaCountdownCompleteFlag = true;
            }
        }
        else
        {
            Debug.LogWarning("CSVLoader instance is null, cannot determine game mode for update sequences.");
        }
    }

    //====================================================================================================
    //START THE SEQUENCE
    //====================================================================================================
    public void PlayFirstSequence()
    {
        Debug.Log("Sequencer: PlayFirstSequence() called.");
        if(!openingSequenceFlag)
        {
            openingSequenceFlag = true;
            //PLAY OPENING SEQUENCE
            if(CSVLoader.instance != null)
            {
                if(CSVLoader.instance.gameMode == "Preparation" || CSVLoader.instance.gameMode == "Skills Training")
                {
                    Debug.Log("Sequencer: Playing Skills Training Opening Sequence.");
                    if(CSVLoader.instance.GetDecryptedFirstTimeUser() == "First Time User")
                    {
                        wwiseVOManager.PlayOpeningSequence("Preparation_Long");
                    }
                    else
                    {
                        wwiseVOManager.PlayOpeningSequence("Preparation_Short");
                    }
                }
                else if (CSVLoader.instance.gameMode == "Integration")
                {
                    Debug.Log("Sequencer: Playing Integration Opening Sequence.");
                    wwiseVOManager.PlayOpeningSequence("Integration_Short");
                } else if (CSVLoader.instance.gameMode == "Protocol Stacks")
                {
                    if(CSVLoader.instance.subGameMode == "Ascending")
                    {
                        wwiseVOManager.PlayOpeningSequence("Ascending");
                        //wwiseVOManager.PlayOpeningSequence("Esketamine_Ascending");
                        //Debug.Log("Sequencer: Playing Esketamine Ascending Opening Sequence.");
                    }
                    else if(CSVLoader.instance.subGameMode == "Descending")
                    {
                        wwiseVOManager.PlayOpeningSequence("Descending");
                        //wwiseVOManager.PlayOpeningSequence("Esketamine_Descending");
                        //Debug.Log("Sequencer: Playing Esketamine Descending Opening Sequence.");
                    }
                }
                else
                {
                    Debug.LogWarning("Sequencer: No Opening Sequence for this game mode.");
                }
            } else
            {
                Debug.LogWarning("Sequencer: CSVLoader instance is null, cannot determine game mode for opening sequence.");
            }
                
            Debug.Log("AVS_Program_DynamicDrop_Start is starting");
            CoroutineDynamicDropStart = StartCoroutine(AVS_Program_DynamicDrop_Start());
        }
        else
        {
            Debug.LogWarning("Sequencer: PlayFirstSequence() called, but opening sequence has already been played.");
        }
    }
    //====================================================================================================
    //Protocol Stacks Sequence
    //====================================================================================================
    //TODO:
    // [ ] AkSoundEngine.PostEvent("Play_sfx_EndInteractive", gameObject); for when the mic goes off.
    // [ ] Missing: `FadeOut()` (Environment mode + Dark color)
    // [ ] CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End(180f));
    // [ ] tutorial.StopTutorial(), once we add the dynamic tutorial.






    public void ProtocolStacksPlaygroundStart()
    {
        Debug.Log("Sequencer: ProtocolStacksPlaygroundStart - Called when opening sequence ends");
        Debug.Log("Sequencer: ProtocolStacksPlaygroundStart - Current countdown: " + _countdownToSavasana + " seconds (" + (_countdownToSavasana / 60f) + " minutes)");
        Debug.Log("Sequencer: ProtocolStacksPlaygroundStart - Current music mode: " + MusicSystem1.instance.currentMusicMode);
        Debug.Log("Sequencer: ProtocolStacksPlaygroundStart - startedExperience: " + (startButtonScript != null ? startButtonScript.startedExperience.ToString() : "startButtonScript is null"));
        // Called when opening sequence ends (via Cue_StartInteractive cue from Wwise)
        // Starts the ProtocolStacksCoroutine which manages timed behaviors based on _countdownToSavasana
        // IMPORTANT: The coroutine will wait until _countdownToSavasana <= 20 minutes before executing Step 1
        // This means Step 1 does NOT happen immediately - it waits for the countdown to reach the threshold
        //Expected behviors:
        // - play Shifting Earth.
        // - play Music Loop.
        // - play Silent Loop.
        StartCoroutine(ProtocolStacksCoroutine());

    }

    //A coroutine that moves through several steps, depending on _timeSinceTutorial and _countdownToSavasana.
    private IEnumerator ProtocolStacksCoroutine()
    {
        Debug.Log("Sequencer: ProtocolStacksCoroutine STARTED - Current countdown: " + _countdownToSavasana + " seconds (" + (_countdownToSavasana / 60f) + " minutes)");
        
        // STEP 1: Wait until we have 20 minutes or less remaining in the countdown
        // This ensures Step 1 happens at the right time based on countdown, not immediately when coroutine starts
        // When this threshold is reached, we start the interactive music system:
        //   - Stop breathwork cycle
        //   - Set music mode to Freeplay (exits Silent mode)
        //   - Set soundscape to ShiftingEarth (MusicLoop)
        //   - Start playground (enables director, begins shuffle, etc.)
        float step1Threshold = 20f * 60f; // 1200 seconds = 20 minutes
        Debug.Log("Sequencer: ProtocolStacksCoroutine - Waiting for countdown to reach " + step1Threshold + " seconds (20 minutes). Current: " + _countdownToSavasana);

        int frameCount = 0;
        while (_countdownToSavasana > step1Threshold)
        {
            frameCount++;
            // Log every 10 seconds to help diagnose if countdown is decrementing
            if (frameCount % 600 == 0) // ~10 seconds at 60fps
            {
                Debug.Log("Sequencer: ProtocolStacksCoroutine - Still waiting. Countdown: " + _countdownToSavasana + " seconds (" + (_countdownToSavasana / 60f) + " minutes). Threshold: " + step1Threshold);
            }
            yield return null;
        }
        
        Debug.Log("Sequencer: ProtocolStacksCoroutine - Threshold reached! Countdown: " + _countdownToSavasana + " seconds. Proceeding to Step 1.");
        Debug.Log("Sequencer: ProtocolStack Step 1 - Starting interactive music (20 minutes or less remaining)");
        MusicSystem1.instance.StopBreathworkCycle();
        //tutorial.tutorialComplete = true; // set in StartPlayground()
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
        MusicSystem1.instance.SetSoundscape("ShiftingEarth");
        StartPlayground(false);
        
        worldShuffler.ExcludeSoundscape("Shadow");
        // musicSystem.SetMusicModeTo(MusicMode.Freeplay);

        // STEP 2: Wait until we have 19 minutes - 30 seconds (18.5 minutes) remaining
        while (_countdownToSavasana > (19f * 60f - 30f))
        {
            yield return null;
        }
        
        Debug.Log("Sequencer: ProtocolStack Step 2");
        
        director.ReplaceActionInQueue(MusicSystem1.instance.Action_SetSoundscape("SitarAmbience"), "Soundscape", "SoundscapeShuffle", true, false, 180.0f, true);
        
        // worldShuffler.QueueWorldShuffle();

        while (_countdownToSavasana > (16f * 60f))
        {
            yield return null;
        }
        director.ReplaceActionInQueue(MusicSystem1.instance.Action_SetSoundscape("Shadow"), "Soundscape", "SoundscapeShuffle", true, false, 180.0f, true);
        director.AddActionToQueue(lightControl.Action_SetPreferredColorWorld("Blue", 8.0f), "ColorWorld", false, true, 180.0f, true, 2);
        Debug.Log("Sequencer: ProtocolStack Step 4");
        // director.AddActionToQueue(...);

        // Step 5 at 280 seconds
        while (_countdownToSavasana > (13f * 60f))
        {
            yield return null;
        }
        director.ReplaceActionInQueue(MusicSystem1.instance.Action_SetSoundscape("PinkNoiseAtmosphere"), "Soundscape", "SoundscapeShuffle", true, false, 180.0f, true);
        Debug.Log("Sequencer: ProtocolStack Step 5");
        // StartCoroutine(SpecialProtocolEndingRoutine());

        while (_countdownToSavasana > (10f * 60f))
        {
            yield return null;
        }
        //director.ReplaceActionInQueue(MusicSystem1.instance.Action_SetSoundscape("Shruti"), "Soundscape", "SoundscapeShuffle", true, false, 180.0f, true);
        Debug.Log("Sequencer: ProtocolStack Step 6");
        worldShuffler.ExcludeSoundscape("SonoFlore");

        while (_countdownToSavasana > (4f * 60f))
        {
            yield return null;
        }
        Debug.Log("Sequencer: ProtocolStack Step 8");
        worldShuffler.StopShuffle();
        worldShuffler.CloseSoundscapeQueue();
        director.ReplaceActionInQueue(MusicSystem1.instance.Action_SetSoundscape("SonoFlore"), "Soundscape", "SoundscapeShuffle", true, false, 180.0f, true);

        while (_countdownToSavasana > 60f)
        {
            yield return null;
        }


        while(_countdownToSavasana > 0f)
        {
            yield return null;
        }

        //Turn off Director 
        //Turn off World Shuffler
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);

        //TODO: move these to about 60 seconds before "it's time now to internalize your sound..."
        director.ActivateQueue(15f);
        director.Disable();
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.MusicLoopSilent);
        wwiseVOManager.PlayAscendingClosing(); //this is basically the ProtocolStacks version of savasana.
        _countdownToSavasana = -1.0f;
        
    }

    //====================================================================================================
    //STANDARD SEQUENCE
    //====================================================================================================
    private void StandardSequenceUpdate()
    {
        // Guard: Only run for standard sequences, not Protocol Stacks
        if(CSVLoader.instance != null && CSVLoader.instance.gameMode == "Protocol Stacks")
        {
            return; // Protocol Stacks uses ProtocolStacksCoroutine() instead
        }
        
        //Early Behaviors
        if(_timeSinceTutorial >= 60 && !flagTriggerStart1)
        {
            Debug.Log("Sequencer: StandardSequence Triggering Start1 Behaviors: Reset Soundscape Exclusions for Shuffle");
            worldShuffler.ResetSoundscapeExclusions();
            flagTriggerStart1 = true;
        }
        if(_timeSinceTutorial >= 300 && !flagTriggerStart2)
        {
            Debug.Log("Sequencer: StandardSequence Triggering Start2 Behaviors: Reset Color Exclusions for Shuffle");
            worldShuffler.ResetColorWorlds();
            flagTriggerStart2 = true;
        }

        //End Behaviors
        if(_countdownToSavasana <= 300 && !flagTriggerEnd1)
        {
            Debug.Log("Sequencer: StandardSequence Triggering End1 Behaviors: No Shadow or Shruti Allowed");
            worldShuffler.ResetSoundscapeExclusions();
            worldShuffler.ExcludeSoundscape("Shadow");
            worldShuffler.ExcludeSoundscape("Shruti"); //removing shruti, as we want it to go last
            flagTriggerEnd1 = true;
            flagTriggerStart1 = true;
            flagTriggerStart2 = true;
        }
        if(_countdownToSavasana <= 180f && !flagTriggerEnd2)
        {
            Debug.Log("Sequencer: StandardSequence Triggering End2 Behaviors: Queue Shruti, Close Music Queue, Start AVS End Sequence");
            //finally, queue shruti and prevent further queueing of shuffled soundscapes.
            director.ReplaceActionInQueue(MusicSystem1.instance.Action_SetSoundscape("Shruti"), "Soundscape", "SoundscapeShuffle", true, false, 180.0f, true);
            director.AddActionToQueue(director.Action_PlayTransitionSound(), "TransitionSound", true, false, 180.0f, true, 2);
            worldShuffler.CloseSoundscapeQueue();
            CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End(180f));
            flagTriggerEnd2 = true;
        }

        if(_countdownToSavasana <= 60f && !flagTriggerEnd3)
        {
            Debug.Log("Sequencer: StandardSequence Triggering End3 Behaviors: Start Last Minute Behaviors");
            StartCoroutine(LastMinute());
            flagTriggerEnd3 = true;
        }

        if(_countdownToSavasana <= 0f && !flagTriggerEnd4)
        {
            // Only trigger savasana for standard sequences, not Protocol Stacks
            if(CSVLoader.instance != null && CSVLoader.instance.gameMode == "Protocol Stacks")
            {
                Debug.LogWarning("Sequencer: StandardSequence Attempted to trigger Thematic Savasana in Protocol Stacks mode - this should not happen!");
                return;
            }
           
            Debug.Log("Sequencer: StandardSequence Triggering Thematic Savasana."); 
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Silent); 

            wwiseVOManager.PlayThematicSavasana();
            flagTriggerEnd4 = true;
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
        director.Disable();
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.FrozenFreeplay);


        Debug.Log("Sequencer Last Minute: Starting Thematic Savasana.");
        yield return null;
        Debug.Log("wake Up Counter:" + _countdownToSavasana);
        yield return new WaitUntil(() => _countdownToSavasana <= 15f);
        
        Debug.Log("Sequencer Last Minute: Starting Light Fade-Out. Waiting for _countdownToSavasana to reach 0.");
        FadeOut();
        tutorial.StopTutorial();

        while (_countdownToSavasana > 0f)
        {
            yield return null;
        }
        Debug.Log("Sequencer Last Minute: Starting Thematic Savasana, and ending coroutine");
        if(CSVLoader.instance != null && TimeLeftScript.instance != null)
        {
            TimeLeftScript.instance.SetTimeLeftSeconds(CSVLoader.instance.totalTimeOfPostUnguidedVocalizationContent);
        }


    }

    private void FadeOut()
    {
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Environment);
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
        Cleanup(coroutineCleanupList); //not necessary for the first one, but placing it here for convention.
        yield return null;
       
        Debug.Log(_countdownToSavasana + "Sequencer | AVS Program: DynamicDropStart. Waiting for lights. Currently:" + lightControl.currentColorType);

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
        while(_timer > 0)
        {
            if(AVS_Program_ManageThetaTransition())
            {
                yield break;
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
        while(true)
        {
            if(AVS_Program_ManageThetaTransition())
            {
                yield break;
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

    private bool AVS_Program_ManageThetaTransition()
    {
        
        if(((respirationTracker._absorption > _absorptionThreshold)) && !flagThetaCoroutine)
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


    IEnumerator AVS_Program_DynamicDrop_End(float transitionTime = 180f)
    {
        if(CoroutineDynamicDropStart != null)
        {
            Debug.Log("Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
        if(CoroutineDynamicDropTheta != null)
        {
            Debug.Log("Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropTheta);
        }

        yield return null;
        Cleanup(coroutineCleanupList);
        Debug.Log("Sequencer | AVS Program: DynamicDrop_End. Starting End Program with transitionTime=" + transitionTime + "s.");
        
        // Calculate proportional timing (based on original 180s total)
        float timeScale = transitionTime / 180f;
        float phase1Duration = 70f * timeScale;  // Original: 180 to 110 (70 seconds)
        float phase2Duration = 20f * timeScale;  // Original: 110 to 90 (20 seconds)
        float phase3Duration = 90f * timeScale;  // Original: 90 to 0 (90 seconds) - strobe transition
        
        director.ClearQueueOfType("gamma");
        director.ClearQueueOfType("monostereo");
        if(!lightControl.bilateral)
        {
            float queueTime1 = 30.0f * timeScale;
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, queueTime1, true, 2));
        }
        if(lightControl._gammaBurstMode != 0.0f)
        {
            float queueTime2 = 30.0f * timeScale;
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, queueTime2, true, 2));
        }

        // Phase 1: Initial wait (proportional to original 70 seconds)
        float elapsedTime = 0f;
        while(elapsedTime < phase1Duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Debug.Log("Sequencer | AVS Program: DynamicDrop_End. Stabilizing before dramatic rise. Elapsed: " + elapsedTime + "s / " + transitionTime + "s");
        float queueTime3 = 10.0f * timeScale;
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, queueTime3, true, 2));

        // Phase 2: Wait before strobe transition (proportional to original 20 seconds)
        while(elapsedTime < phase1Duration + phase2Duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Debug.Log("Sequencer | AVS Program: DynamicDrop_End. Starting dramatic rise to 40hz. Elapsed: " + elapsedTime + "s / " + transitionTime + "s");
        lightControl.SetStrobeRate(40.0f, phase3Duration);

        // Phase 3: Wait during strobe transition (proportional to original 90 seconds)
        while(elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Debug.Log("Sequencer | AVS Program: DynamicDrop_End. End of AVS Program. Elapsed: " + elapsedTime + "s / " + transitionTime + "s. Goodnight!");
    }

    private void Cleanup(List<int> coroutineCleanupList)
    {
        Debug.Log("Performing cleanup.");
        foreach (int index in coroutineCleanupList)
        {
            if (director.queue.ContainsKey(index))
            {
                Debug.Log(_countdownToSavasana + " Sequencer  Director Queue (AVS Program): DynamicDrop (Transitioning). Removing " + index + " " + director.queue[index].Item2);
                director.queue.Remove(index);
            }
            else
            {
                Debug.LogWarning("Cleanup: Key " + index + " not found in director.queue, skipping.");
            }
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

    //====================================================================================================
    //PUBLIC METHODS
    //====================================================================================================

    public void SetCountdownToSavasana(float timeInSeconds)
    {
        _countdownToSavasana = timeInSeconds;
        Debug.Log("Sequencer: ThematicSavasanaCountdown Counter set to " + _countdownToSavasana + " via SetCountdownToSavasana().");
    }

    public void StartTrueStart() //THIS ONE IS OK TO CALL IN NORMAL TIME (NON DEVELOPMENT MODE)
    {
        Debug.Log("Sequencer: Starting True Start Sequence.");
        MusicSystem1.instance.SetMusicSilentLayerVolume(MusicSystem1.instance._silentVolumeLow, 0.0f);
        director.Disable();
        if(csvLoader != null)
        {
            if(csvLoader.gameMode == "Protocol Stacks")
            {
                ProtocolStacksInitialization();
            }
            else if(csvLoader.gameMode == "Integration" || csvLoader.gameMode == "Preparation" || csvLoader.gameMode == "Skills Training")
            {
                SkillsTrainingOrIntegrationInitialization();
            }
        }
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Silent);        
        PlayFirstSequence();
        //lightControl.SetColorWorldByType("Dark", 0.0f);
    }

    public void ProtocolStacksInitialization()
    {
        Debug.Log("Sequencer: Protocol Stacks mode detected. Initializing Protocol Stacks.");
        MusicSystem1.instance.SetSoundWorld("Shadow");
        MusicSystem1.instance.SetSoundscape("ShiftingEarth");
        worldShuffler.ExcludeColorWorld("Blue");
        worldShuffler.ExcludeSoundscape("Shadow");
    }

    public void SkillsTrainingOrIntegrationInitialization()
    {
        Debug.Log("Sequencer: Standard mode detected. Initializing Standard.");
        MusicSystem1.instance.SetSoundscape("SonoFlore");
        worldShuffler.ExcludeColorWorld("Blue");
        worldShuffler.ExcludeSoundscape("Shadow");
    }

    //WOE TO YOU WHO USESE THESE START FUNCTIONS EXCEPT IN DEVELOPMENT MODE
    //THEY ARE NOT MADE OR TESTED FOR THAT (YET), THOUGH PERHAPS THEY SHOULD BE.
    //When these were first made, they were envisioned as a way to cheat the system into getting into the zone it should be at that moment.
    //it is NOT running the actual logic of the experience, so using these outside of development mode may have unintended consequences.
    //if you would like to use them that way, which would be more elegant, further development will be required.
    public void StartTutorialSequence()
    {
        Debug.Log("Sequencer: Starting Tutorial Sequence.");
        tutorial.StartTutorial();
        InitializeLights();
        worldShuffler.ExcludeColorWorld("Blue");
        worldShuffler.ExcludeSoundscape("Shadow");
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Tutorial);
        MusicSystem1.instance.SetMusicSilentLayerVolume(MusicSystem1.instance._silentVolumeHigh, 0.0f);
        director.Disable();
    }
    public void StartPlayground(bool setTimeSinceTutorial = false, float transitionTime = 20f)
    {
        if(setTimeSinceTutorial && _timeSinceTutorial < 300f)
        {
            _timeSinceTutorial = 300f;
        }
        if(DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode)
        {
            lightControl.SetColorWorldByType("Red", 0.0f);
        }
        Debug.Log("Sequencer: Starting Playground Sequence.");
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);          
        director.Enable();
        MusicSystem1.instance.SetMusicSilentLayerVolume(MusicSystem1.instance._silentVolumeHigh, transitionTime);
        tutorial.tutorialComplete = true;
        worldShuffler.BeginShuffle(false);
        InitializeLights(); 
    }
    public void StartRightBeforeSavasana()
    {
        if(_timeSinceTutorial < 300f)
        {
            _timeSinceTutorial = 300f;
        }
        Debug.Log("Sequencer: Starting 181s Before Savasana Sequence.");
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);          
        director.Enable();
        MusicSystem1.instance.SetMusicSilentLayerVolume(MusicSystem1.instance._silentVolumeHigh, 0f);
        tutorial.tutorialComplete = true;
        worldShuffler.BeginShuffle(false);
        _countdownToSavasana = 181f;
        Debug.Log("Sequencer: ThematicSavasanaCountdown Counter set to " + _countdownToSavasana + " for debug.");
        //flagTriggerStart1 = true;
        //flagTriggerStart2 = true;
        //flagTriggerEnd1 = true;
        //flagTriggerEnd2 = false;
        //flagTriggerEnd3 = false;

        if(DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode)
        {
            lightControl.SetColorWorldByType("Red", 0.0f);
        }
    }
    
    public void StartSavasana()
    {
        
        if(_timeSinceTutorial < 300f)
        {
            _timeSinceTutorial = 300f;
        }
        Debug.Log("Sequencer: Starting Savasana Sequence in 1 Second.");
        tutorial.tutorialComplete = true;
        _countdownToSavasana = 1f;
        Debug.Log("Sequencer: ThematicSavasanaCountdown Counter set to " + _countdownToSavasana + " for debug.");
        FadeOut();

        //flagTriggerStart1 = true;
        //flagTriggerStart2 = true;
        //flagTriggerEnd1 = true;
        //flagTriggerEnd2 = false;
        //flagTriggerEnd3 = false;
    }
    
    public void Initialize()
    {
        if(DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode)
        {
            Debug.Log("Sequencer: Initialize() called in Development Mode. No action taken.");
            return;
        }
        else if (DevelopmentMode.instance == null)
        {
            Debug.LogWarning("Sequencer: Initialize() called. This should only happen in developmentMode.");
        }
    }

        private void OnStartModeDropdownChanged(int index)
        {
            if(startModeDropdown != null)
            {
                switch (index)
                {
                    case 0: Initialize(); Debug.Log("Initialize called.");  break;
                    case 1: StartTrueStart(); Debug.Log("StartTrueStart called."); break;
                    case 2: StartTutorialSequence(); Debug.Log("StartTutorialSequence called.");  break;
                    case 3: StartPlayground(true, 0.5f);Debug.Log("StartPlayground called.");  break;
                    case 4: StartRightBeforeSavasana(); Debug.Log("StartRightBeforeSavasana called."); break;
                    case 5: StartSavasana(); Debug.Log("StartSavasana called."); break;
                    default: Initialize();  break;
                }
            }
            
        }
}
