using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using TMPro;
using System.Security.Cryptography.X509Certificates;
using ConversionUtilities;
using SoundSelf.Sequence;

/// <summary>Wwise sound banks that can be unloaded via UnloadBank.</summary>
public enum SequencerBank
{
    CALIBRATION,
    OPENING,
    INTERACTIVE,
    CLOSING
}

public class Sequencer : MonoBehaviour
{
    public CSVLoader csvLoader;
    public CalibrationMenu calibrationMenu;
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

    [Header("Debug logs")]
    [SerializeField] private bool debugAllowLogsSequencer = true;
    [SerializeField] private bool debugAllowLogsAVSProgram = false;
    [Tooltip("When true, warnings still log even if Sequencer / AVS info logs are off.")]
    [SerializeField] private bool debugAllowLogsWarnings = true;

    public TMP_Dropdown startModeDropdown;

    /// <summary>For <see cref="IStageHandler"/> and other non-<see cref="Sequencer"/> code: warning path only, respects <see cref="debugAllowLogsWarnings"/>.</summary>
    public void LogSequencerWarning(string message) => DbgLogSequencer(message, true);

    /// <param name="isWarning">If true, <c>LogWarning</c> when <see cref="debugAllowLogsWarnings"/>; else <c>Log</c> when <see cref="debugAllowLogsSequencer"/>.</param>
    private void DbgLogSequencer(string message, bool isWarning = false)
    {
        if (isWarning)
        {
            if (debugAllowLogsWarnings)
                Debug.LogWarning(message);
        }
        else if (debugAllowLogsSequencer)
        {
            Debug.Log(message);
        }
    }

    /// <param name="isWarning">If true, <c>LogWarning</c> when <see cref="debugAllowLogsWarnings"/>; else <c>Log</c> when <see cref="debugAllowLogsAVSProgram"/>.</param>
    private void DbgLogAvs(string message, bool isWarning = false)
    {
        if (isWarning)
        {
            if (debugAllowLogsWarnings)
                Debug.LogWarning(message);
        }
        else if (debugAllowLogsAVSProgram)
        {
            Debug.Log(message);
        }
    }


    // AVS Controls
    private float _absorptionThreshold;
    private float d = 1f; //debug timer mult, higher makes it go faster for testing

    //THINGS THAT PERTAIN TO STORY PROGRESSION    

    private static bool _warnedMissingTimeTrackerForCountdown;

    /// <summary><see cref="TimeTrackerScript.CountdownThisSection"/> — main-segment countdown for protocol steps (not full session).</summary>
    public float CountdownThisSection
    {
        get
        {
            var tt = TimeTrackerScript.instance;
            if (tt == null)
            {
                if (!_warnedMissingTimeTrackerForCountdown)
                {
                    _warnedMissingTimeTrackerForCountdown = true;
                    DbgLogSequencer("Sequencer: TimeTrackerScript.instance is null; [CountdownThisSection] reads as 0 until the tracker exists.", true);
                }
                return 0f;
            }
            return tt.CountdownThisSection;
        }
    }

    [SerializeField] public bool endSoonFlag = false;
    private bool startButtonFlag = false;
    private bool standardSequenceMilestoneStart1 = false;
    private bool standardSequenceMilestoneStart2 = false;
    private bool standardSequenceMilestoneEnd1 = false;
    private bool standardSequenceMilestoneEnd2 = false;
    private bool standardSequenceMilestoneEnd3 = false;
    private bool standardSequenceMilestoneEnd4 = false;

    /// <summary>Resets Integration / Skills Training standard-sequence one-shots. Invoked from <see cref="SoundSelf.Sequence.SequenceRunner.StartSequence"/>.</summary>
    public void ResetStandardSequenceMilestones()
    {
        standardSequenceMilestoneStart1 = false;
        standardSequenceMilestoneStart2 = false;
        standardSequenceMilestoneEnd1 = false;
        standardSequenceMilestoneEnd2 = false;
        standardSequenceMilestoneEnd3 = false;
        standardSequenceMilestoneEnd4 = false;
    }
    private bool flagThetaCoroutine = false;
    private List<int> coroutineCleanupList = new List<int>();
    private Coroutine CoroutineDynamicDropStart;
    private Coroutine CoroutineDynamicDropTheta;
    private Coroutine CoroutineDynamicDropEnd;
    private bool developmentModeWarningFlag = false;
    private Coroutine countdownCoroutine; // Reference to the coroutines
    //private int currentStage = 0; //As SonoFlore

    [SerializeField] private SequenceRunner sequenceRunner;
    
    [Header("Sequence Definitions (Inspector)")]
     [Header("Protocol Stacks")]
    [SerializeField] private SequenceDefinition protocolStacksAscendingDefinition;
    [SerializeField] private SequenceDefinition protocolStacksDescendingDefinition;
    
    [Header("SkillsTraining")]
    [SerializeField] private SequenceDefinition peaceDefinition;
    [SerializeField] private SequenceDefinition narrativeDefinition;
    [SerializeField] private SequenceDefinition surrenderDefinition;
    [Header("Integration")]
    [SerializeField] private SequenceDefinition firefliesDefinition;
    [SerializeField] private SequenceDefinition kindnessDefinition;
    [SerializeField] private SequenceDefinition mettaDefinition;

    private CalibrationStageHandler _calibrationHandler;
    private OpeningStageHandler _openingHandler;
    private PlaygroundStageHandler _playgroundHandler;
    private SavasanaStageHandler _savasanaHandler;
    private TutorialStageHandler _tutorialHandler;
    private SetMenuStageHandler _setMenuHandler;
    private MusicPlaylistStageHandler _musicPlaylistHandler;
    private InquiryStageHandler _inquiryHandler;
    private EndStageHandler _endHandler;
    private LinearAudioStageHandler _linearAudioHandler;
    private StartCountdownStageHandler _startCountdownHandler;

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
            DbgLogSequencer("Sequencer: DevelopmentMode instance is null in Awake().", true);
        }
        if (startModeDropdown != null)
            startModeDropdown.onValueChanged.AddListener(OnStartModeDropdownChanged);

        if (sequenceRunner == null)
            sequenceRunner = gameObject.GetComponent<SequenceRunner>() ?? gameObject.AddComponent<SequenceRunner>();

        // This is the StageType -> IStageHandler registration map SequenceRunner uses to dispatch Enter/Exit and completion checks.
        // It's primary use is to pass the sequencer instance to the handlers, so they can access CountdownThisSection (main segment) / startPlayground, etc.
        // We do it like this, instead of using singletons, to prevent null references and other issues.
        _calibrationHandler = new CalibrationStageHandler(this);
        _openingHandler = new OpeningStageHandler(this);
        _playgroundHandler = new PlaygroundStageHandler(this);
        _savasanaHandler = new SavasanaStageHandler(this);
        _tutorialHandler = new TutorialStageHandler(this);
        _startCountdownHandler = new StartCountdownStageHandler(this);
        _setMenuHandler = new SetMenuStageHandler();
        _musicPlaylistHandler = new MusicPlaylistStageHandler();
        _inquiryHandler = new InquiryStageHandler();
        _endHandler = new EndStageHandler();
        _linearAudioHandler = new LinearAudioStageHandler();
        sequenceRunner.SetHandlers(new IStageHandler[] { _calibrationHandler, _openingHandler, _startCountdownHandler, _playgroundHandler, _savasanaHandler, _tutorialHandler, _setMenuHandler, _musicPlaylistHandler, _inquiryHandler, _endHandler, _linearAudioHandler });
    }

    private void OnDestroy()
    {
        if (startModeDropdown != null)
            startModeDropdown.onValueChanged.RemoveListener(OnStartModeDropdownChanged);
    }

    void Start()
    {
        
        _absorptionThreshold = UnityEngine.Random.Range(0.08f, 0.35f);
        if (TimeTrackerScript.instance == null)
            DbgLogSequencer("Sequencer: TimeTrackerScript.instance is null in Start(); session countdown is unavailable until the tracker exists. Add a TimeTrackerScript to the scene.", true);

        if (startModeDropdown != null)
                OnStartModeDropdownChanged(startModeDropdown.value);
    }
// if(DevelopmentMode.Instance != null && DevelopmentMode.Instance.developmentMode)
 // {   //do something  }
    
   
    // Update is called once per frame
    void Update()
    {
        if(CSVLoader.instance != null)
        {
            if(CSVLoader.instance.UsesStandardSequenceUpdate)
            {
                StandardSequenceUpdate();
            }
        }
        else
        {
            DbgLogSequencer("CSVLoader instance is null, cannot determine game mode for update sequences.", true);
        }
    }

    /// <summary>Unloads a Wwise sound bank by enum. Use this to free memory when a bank is no longer needed.</summary>
    /// <param name="bank">The bank to unload (CALIBRATION, CLOSING, OPENING, or INTERACTIVE).</param>
    /// //TODO: UNLOAD THE BANKS WHEN CALIBRATION ETC. IS COMPLETE.
    public void UnloadBank(SequencerBank bank)
    {
        string bankName = bank.ToString() + ".bnk";
        AkBankManager.UnloadBank(bankName);
        DbgLogSequencer("Sequencer: Unloaded WWise AKSoundEngine bank: " + bankName);
    }

    //====================================================================================================
    //Protocol Stacks Sequence
    //====================================================================================================
    //TODO:
    // [ ] AkSoundEngine.PostEvent("Play_sfx_EndInteractive", gameObject); for when the mic goes off.
    // [ ] Missing: `FadeOut()` (Environment mode + Dark color)
    // [ ] CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End(180f));
    // [ ] tutorial.StopTutorial(), once we add the dynamic tutorial.






    /// <summary>Dispatches cue to current handler if watching. Returns true if handled. Caller does legacy when false. StartInteractive has built-in legacy (ProtocolStacksPlaygroundStart) when not in sequence.</summary>
    public bool HandleSequenceCommand(SequenceCommand sequenceCommand)
    {
        if (sequenceRunner == null || sequenceRunner.CurrentStageIndex < 0)
            return false;
        if (sequenceRunner.TryExecuteSequenceCommand(sequenceCommand))
        {
            DbgLogSequencer(sequenceCommand + ": Handled by sequence stages (current and/or transitioning-out).");
            return true;
        }
        DbgLogSequencer(sequenceCommand + " fired but it's not being watched for, so nothing is happening.", true);
        return false;
    }

    /// <summary>Starts next stage now and leaves current stage in transition-out tail.</summary>
    public void TransitionToNextStage()
    {
        if (sequenceRunner == null)
        {
            DbgLogSequencer("Sequencer: Cannot transition to next stage because sequenceRunner is null.", true);
            return;
        }
        sequenceRunner.TransitionToNextStage();
    }

    public void ProtocolStacksPlaygroundStart()
    {
        DbgLogSequencer("Sequencer: ProtocolStacksPlaygroundStart - Called when opening sequence ends");
        DbgLogSequencer("Sequencer: ProtocolStacksPlaygroundStart - Current countdown: " + CountdownThisSection + " seconds (" + (CountdownThisSection / 60f) + " minutes)");
        DbgLogSequencer("Sequencer: ProtocolStacksPlaygroundStart - Current music mode: " + MusicSystem1.instance.currentMusicMode);
        // Called when opening sequence ends (via Cue_StartInteractive cue from Wwise)
        // Starts the ProtocolStacksCoroutine which manages timed behaviors based on CountdownThisSection
        // IMPORTANT: The coroutine will wait until CountdownThisSection <= 20 minutes before executing Step 1
        // This means Step 1 does NOT happen immediately - it waits for the countdown to reach the threshold
        //Expected behviors:
        // - play Shifting Earth.
        // - play Music Loop.
        // - play Silent Loop.
        StartCoroutine(ProtocolStacksCoroutine());

    }

    //====================================================================================================
    // YOU GOT HERE - TESTING THIS COROUTINE FOR WHEN THE MUSIC STOPS
    //====================================================================================================

    // Debug helper: set to true to advance ProtocolStacksCoroutine/PlaygroundStageHandler past the current wait (countdown or step)
    private bool _forceSequenceAdvanceRequested = false;

    /// <summary>For PlaygroundStageHandler and debug stepping. Get/set the force-advance flag.</summary>
    public bool ForceSequenceAdvanceRequested { get => _forceSequenceAdvanceRequested; set => _forceSequenceAdvanceRequested = value; }

    /// <summary>
    /// Advances the ProtocolStacksCoroutine past the current wait. Call from InputReferences or elsewhere for debug stepping.
    /// The coroutine waits for either the countdown threshold OR this call—whichever comes first.
    /// </summary>
    public void ForceSequenceAdvance()
    {
        _forceSequenceAdvanceRequested = true;
        DbgLogSequencer("Sequencer: ForceSequenceAdvance() called - advancing to next step.");
    }

    /// <summary>Same dev quick-step idea as <see cref="PlaygroundStageHandler"/> — just above the 20-minute remaining gate.</summary>
    private const float LegacyProtocolStacksCoroutineCountdownFallbackSeconds = 20f * 60f + 5f;

    /// <summary>Enough runway for LastMinute waits if the clock was never started (should not happen if StandardSequence ran).</summary>
    private const float LegacyLastMinuteCountdownFallbackSeconds = 90f;

    /// <summary>Phase 5: Legacy coroutines that read <see cref="CountdownThisSection"/> should not run with a stopped session clock.</summary>
    private void EnsureLegacyCoroutineCountdown(string coroutineLabel, float fallbackSeconds)
    {
        var t = TimeTrackerScript.instance;
        if (t == null)
        {
            Debug.LogError("Sequencer: " + coroutineLabel + " — TimeTrackerScript.instance is null; CountdownThisSection unavailable. Add a tracker to the scene.");
            return;
        }
        if (t.IsCountdownRunning)
            return;
        float closing = CSVLoader.instance != null ? Mathf.Max(0f, CSVLoader.instance.totalTimeOfPostUnguidedVocalizationContent) : 0f;
        float sec = fallbackSeconds;
        float full = closing > 0f ? sec + closing : sec;
        Debug.LogError("Sequencer: " + coroutineLabel + " — session countdown is not running. Use a StartCountdown stage in the sequence. [Legacy fallback] BeginCountdownPair [CountdownThisSection]=" + sec + " [CountdownFull]=" + full + ".");
        t.BeginCountdownPair(sec, full);
    }

    /// <summary>
    /// Legacy Protocol Stacks timeline: same countdown thresholds and director steps as
    /// <see cref="SoundSelf.Sequence.PlaygroundStageHandler"/> (its <c>PlaygroundCoroutine</c>), but started from
    /// <see cref="ProtocolStacksPlaygroundStart"/> when Wwise advances interactive play outside the sequence runner.
    /// Prefer the Playground stage handler for new work; retain this until Protocol Stacks fully uses <c>SequenceDefinition</c> Playground.
    /// TODO: Delete this once the new one is confirmed to work correctly.
    /// </summary>
    private IEnumerator ProtocolStacksCoroutine()
    {
        DbgLogSequencer("Sequencer: ProtocolStacksCoroutine STARTED - Current countdown: " + CountdownThisSection + " seconds (" + (CountdownThisSection / 60f) + " minutes)");

        EnsureLegacyCoroutineCountdown(nameof(ProtocolStacksCoroutine), LegacyProtocolStacksCoroutineCountdownFallbackSeconds);

        // STEP 1: Wait until we have 20 minutes or less remaining in the countdown (or ForceSequenceAdvance() is called)
        // This ensures Step 1 happens at the right time based on countdown, not immediately when coroutine starts
        // When this threshold is reached, we start the interactive music system:
        //   - Stop breathwork cycle
        //   - Set music mode to Freeplay (exits Silent mode)
        //   - Set soundscape to ShiftingEarth (MusicLoop)
        //   - Start playground (enables director, begins shuffle, etc.)
        float step1Threshold = 20f * 60f; // 1200 seconds = 20 minutes
        
        DbgLogSequencer("Sequencer: ProtocolStacksCoroutine - Waiting for countdown to reach " + step1Threshold + " seconds (20 minutes). Current: " + CountdownThisSection + " (or call ForceSequenceAdvance() to skip)");
        int frameCount = 0;
        while (CountdownThisSection > step1Threshold && !_forceSequenceAdvanceRequested)
        {
            frameCount++;
            // Log every 10 seconds to help diagnose if countdown is decrementing
            if (frameCount % 600 == 0) // ~10 seconds at 60fps
            {
                DbgLogSequencer("Sequencer: ProtocolStacksCoroutine - Still waiting. Countdown: " + CountdownThisSection + " seconds (" + (CountdownThisSection / 60f) + " minutes). Threshold: " + step1Threshold);
            }
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;
        
        DbgLogSequencer("Sequencer: ProtocolStacksCoroutine - Threshold reached! Countdown: " + CountdownThisSection + " seconds. Proceeding to Step 1.");
        DbgLogSequencer("Sequencer: ProtocolStack Step 1 - Starting interactive music (20 minutes or less remaining)");
        MusicSystem1.instance.SetBreathworkCycle(false);
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
        MusicSystem1.instance.SetSoundscape("ShiftingEarth");
        StartPlayground(false, false, true);

        worldShuffler.ExcludeSoundscape("Shadow");
        // musicSystem.SetMusicModeTo(MusicMode.Freeplay);

        // STEP 2: Wait until we have 19 minutes - 30 seconds (18.5 minutes) remaining (or ForceSequenceAdvance())
        while (CountdownThisSection > (19f * 60f - 30f) && !_forceSequenceAdvanceRequested)
        {
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;
        
        DbgLogSequencer("Sequencer: ProtocolStack Step 2");
        
        director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("SitarAmbience"), "Soundscape", true, false, 180.0f, 2, 2);
        
        // worldShuffler.QueueWorldShuffle();

        while (CountdownThisSection > (16f * 60f) && !_forceSequenceAdvanceRequested)
        {
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;
        director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("Shadow"), "Soundscape", true, false, 180.0f, 2, 2);
        director.AddActionToQueue(lightControl.Action_SetPreferredColorWorld("Blue", 8.0f), "ColorWorld", false, true, 180.0f, 1, 2);
        DbgLogSequencer("Sequencer: ProtocolStack Step 4");
        // director.AddActionToQueue(...);

        // Step 5 at 280 seconds
        while (CountdownThisSection > (13f * 60f) && !_forceSequenceAdvanceRequested)
        {
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;
        director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("PinkNoiseAtmosphere"), "Soundscape", true, false, 180.0f, 2, 2);
        DbgLogSequencer("Sequencer: ProtocolStack Step 5");
        // StartCoroutine(SpecialProtocolEndingRoutine());

        while (CountdownThisSection > (12f * 60f) && !_forceSequenceAdvanceRequested)
        {
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;

        worldShuffler.BeginShuffle(false);
        
        while (CountdownThisSection > (10f * 60f) && !_forceSequenceAdvanceRequested)
        {
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;
        //director.ReplaceActionInQueue(MusicSystem1.instance.Action_SetSoundscape("Shruti"), "Soundscape", "SoundscapeShuffle", true, false, 180.0f, 1);
        DbgLogSequencer("Sequencer: ProtocolStack Step 6");
        worldShuffler.ExcludeSoundscape("SonoFlore");

        while (CountdownThisSection > (4f * 60f) && !_forceSequenceAdvanceRequested)
        {
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;
        DbgLogSequencer("Sequencer: ProtocolStack Step 8");
        worldShuffler.StopShuffle();
        worldShuffler.CloseSoundscapeQueue();
        director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("SonoFlore"), "Soundscape", true, false, 180.0f, 2, 2);

        while (CountdownThisSection > 60f && !_forceSequenceAdvanceRequested)
        {
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;

        while(CountdownThisSection > 0f && !_forceSequenceAdvanceRequested)
        {
            yield return null;
        }
        _forceSequenceAdvanceRequested = false;

        //Turn off Director 
        //Turn off World Shuffler
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);

        //TODO: move these to about 60 seconds before "it's time now to internalize your sound..."
        director.ActivateQueue(15f);
        director.Disable();
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.MusicLoopSilent);
        wwiseVOManager.PlayAscendingClosing(); //this is basically the ProtocolStacks version of savasana.
    }

    //====================================================================================================
    //STANDARD SEQUENCE
    //====================================================================================================
    private void StandardSequenceUpdate()
    {
        //Early Behaviors
        float timeSincePlaygroundStart = TimeTrackerScript.instance != null ? TimeTrackerScript.instance.TimeSincePlaygroundStart : 0f;
        if(timeSincePlaygroundStart >= 60 && !standardSequenceMilestoneStart1)
        {
            DbgLogSequencer("Sequencer: StandardSequence Triggering Start1 Behaviors: Reset Soundscape Exclusions for Shuffle");
            worldShuffler.ResetSoundscapeExclusions();
            standardSequenceMilestoneStart1 = true;
        }
        if(timeSincePlaygroundStart >= 300 && !standardSequenceMilestoneStart2)
        {
            DbgLogSequencer("Sequencer: StandardSequence Triggering Start2 Behaviors: Reset Color Exclusions for Shuffle");
            worldShuffler.ResetColorWorlds();
            standardSequenceMilestoneStart2 = true;
        }

        //End Behaviors
        if(CountdownThisSection <= 300 && !standardSequenceMilestoneEnd1)
        {
            DbgLogSequencer("Sequencer: StandardSequence Triggering End1 Behaviors: No Shadow or Shruti Allowed");
            worldShuffler.ResetSoundscapeExclusions();
            worldShuffler.ExcludeSoundscape("Shadow");
            worldShuffler.ExcludeSoundscape("Shruti"); //removing shruti, as we want it to go last
            standardSequenceMilestoneEnd1 = true;
            standardSequenceMilestoneStart1 = true;
            standardSequenceMilestoneStart2 = true;
        }
        if(CountdownThisSection <= 180f && !standardSequenceMilestoneEnd2)
        {
            DbgLogSequencer("Sequencer: StandardSequence Triggering End2 Behaviors: Queue Shruti, Close Music Queue, Start AVS End Sequence");
            //finally, queue shruti and prevent further queueing of shuffled soundscapes.
            director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("Shruti"), "Soundscape", true, false, 180.0f, 2, 2);
            director.AddActionToQueue(director.Action_PlayTransitionSound(), "TransitionSound", true, false, 180.0f, 1, 2);
            worldShuffler.CloseSoundscapeQueue();
            CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End(180f));
            standardSequenceMilestoneEnd2 = true;
        }

        if(CountdownThisSection <= 60f && !standardSequenceMilestoneEnd3)
        {
            DbgLogSequencer("Sequencer: StandardSequence Triggering End3 Behaviors: Start Last Minute Behaviors");
            StartCoroutine(LastMinute());
            standardSequenceMilestoneEnd3 = true;
        }

        if(CountdownThisSection <= 0f && !standardSequenceMilestoneEnd4)
        {
            DbgLogSequencer("Sequencer: StandardSequence Triggering Thematic Savasana."); 
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Silent); 

            wwiseVOManager.PlayThematicSavasana();
            standardSequenceMilestoneEnd4 = true;
        }
    
    }

    //====================================================================================================
    //TIMED BEHAVIORS
    //====================================================================================================
    IEnumerator LastMinute()
    {
        DbgLogSequencer("Sequencer Last Minute: Starting Last Minute Behaviors.");

        EnsureLegacyCoroutineCountdown(nameof(LastMinute), LegacyLastMinuteCountdownFallbackSeconds);

        MusicSystem1.instance.SetAllowTransitionFromEnvironmentToFreeplay(false);
        worldShuffler.StopShuffle(); //we should be in Shruti now.
        //recordedAudioPlaybackTest.SetRecordMode(false);
        //recordedAudioPlaybackTest.SetPlaybackMode(false);
        //PLAY SOUND FOR TRANSITIONING TO SAVASANA
    
        AkSoundEngine.PostEvent("Play_sfx_EndInteractive", gameObject);
        // Wait until toneActiveConfident becomes false
        yield return new WaitUntil(() => !imitoneVoiceInterpreter.toneActiveConfident || CountdownThisSection <= 30f);
        DbgLogSequencer("Sequencer Last Minute: Test 1 (Rest or Time) passed");
        
        // Wait until toneActiveConfident becomes true
        yield return new WaitUntil(() => imitoneVoiceInterpreter.toneActiveConfident || CountdownThisSection <= 30f);
        DbgLogSequencer("Sequencer Last Minute: Test 2 (Tone or Time) passed. Starting Final Behaviors. Wake Up Counter" + CountdownThisSection);
        director.ActivateQueue(15f);
        director.Disable();
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.FrozenFreeplay);


        DbgLogSequencer("Sequencer Last Minute: Starting Thematic Savasana.");
        yield return null;
        DbgLogSequencer("wake Up Counter:" + CountdownThisSection);
        yield return new WaitUntil(() => CountdownThisSection <= 15f);
        
        DbgLogSequencer("Sequencer Last Minute: Starting Light Fade-Out. Waiting for [CountdownThisSection] to reach 0.");
        FadeOut();
        tutorial.StopTutorial();

        while (CountdownThisSection > 0f)
        {
            yield return null;
        }
        DbgLogSequencer("Sequencer Last Minute: [CountdownThisSection] reached 0; resyncing [CountdownFull] to closing duration if configured.");
    }

    private void FadeOut()
    {
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Environment);
        lightControl.SetPreferredColor("Dark", 18f);
    }
    
    

    //====================================================================================================
    //LIGHT CONTROL
    //====================================================================================================

    
    public void StartLights()
    {
        if(!lightsInitialized)
        {
            DbgLogSequencer("Sequencer: StartLights");
            lightControl.SetPreferredColor("Red", 5.0f);
            lightsInitialized = true;
        }
        else
        {
            DbgLogSequencer("Sequencer: StartLights already initialized, skipping");
        }
    }

    /// <summary>Starts the AVS opening program coroutine. Called by OpeningStageHandler.</summary>
    /// TODO: Move this into an AVS program handler.
    public void StartOpeningAVSProgram()
    {
        DbgLogAvs("Sequencer: StartOpeningAVSProgram - AVS_Program_DynamicDrop_Start is starting");
        CoroutineDynamicDropStart = StartCoroutine(AVS_Program_DynamicDrop_Start());
    }

    IEnumerator AVS_Program_DynamicDrop_Start()
    {
        Cleanup(coroutineCleanupList); //not necessary for the first one, but placing it here for convention.
        yield return null;
       
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Waiting for lights. Currently:" + lightControl.currentColorType);

        // Ensure lights are Dark at the start (in case calibration left them in a different state)
        // This prevents premature triggering if calibration sequence was interrupted
        if(lightControl.currentColorType != "Dark" && lightControl.currentColorType != "BreathOnly")
        {
            DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Lights not Dark at start, resetting to Dark first.");
            lightControl.SetPreferredColor("Dark", 0.1f);
            lightControl.SetStrobeRate(0f, 0.1f);
            yield return new WaitForSeconds(0.15f); // Brief wait for transition
        }

        //WAIT UNTIL WE CHANGE TO A REAL COLOR TYPE, WHICH USUALLY HAPPENS ON THE FIRST HUM, IN WWISEVOMANAGER.
        while((lightControl.currentColorType == "Dark") || (lightControl.currentColorType == "BreathOnly"))
        {
            yield return null;
        }
        //ONCE THE LIGHTS TURN ON, START AT 45HZ
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Lights detected, set strobe to 45hz.");
        lightControl.SetStrobeRate(45.0f, 0.0f);
        float _timer = 10f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //AFTER 10 SECOND HOLD IS FINISHED, DROP TO 11HZ OVER 30 SECONDS
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Initializeing drop from gamma to high alpha.");
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
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Begining drop from high alpha to 10hz.");
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
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Starting Saw Strobe Coroutine.");
        float _wavelength = 360f / d;
        float _halfWavelength = _wavelength / 2;
        lightControl.SetSawStrobe(8.5f, 11.5f, _wavelength);
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Waiting for absorption threshold to be met.");

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
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60.0f, 0, 2));
                    flag1 = true;
                }
            }
            else if(_timer <= _halfWavelength*3/4)
            {
                flag1 = false;
                if(!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, 1, 2));
                    flag2 = true;
                }
            }
            yield return null;
        }
    }

    private bool AVS_Program_ManageThetaTransition()
    {
        
        if(((RespirationTracker.instance._absorption > _absorptionThreshold)) && !flagThetaCoroutine)
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
            DbgLogAvs("Sequencer | AVS Program: Stopping Coroutine from THETA Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }

        yield return null;
        Cleanup(coroutineCleanupList);
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Starting Theta program.");

        //SET CORRECT MONO/STEREO
        director.ClearQueueOfType("monostereo");
        //add mono to queue, if we're in bilateral
        if(lightControl.bilateral)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, 1, 2));
            DbgLogAvs("Sequencer Director Queue: (AVS Program) DynamicDrop_Theta. Since starting in bilateral, adding " + (director.queueIndex - 1) + " monostereo=mono to director queue, and waiting.");
            director.LogQueue();
        }
        //and wait to enter mono...
        while(lightControl.bilateral)
        {
            yield return null;
        }
        //DROP TO 7HZ
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 7hz.");
        float _timer = 120f / d;
        lightControl.SetStrobeRate(7.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //CYCLE THROUGH BILATERAL ONCE
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queueing Bilateral Strobe.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60f/d, 1, 2));
        while(!lightControl.bilateral)
        {
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, 1, 2));
        while(lightControl.bilateral)
        {
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queueing Mono Strobe.");
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, 1, 2));
        while(lightControl.bilateral)
        {
            yield return null;
        }
        //DROP TO 6HZ
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 6hz.");
        _timer = 60f / d;
        lightControl.SetStrobeRate(6.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //GAMMA BURSTS, THEN HANG HERE. 
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queuing Gamma Burst.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f/d, 1, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queuing Gamma Burst Stop.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 180f/d, 1, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //DROP TO 5HZ
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 5hz.");
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
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f/d, 1, 2));
                    flag1 = true;
                    DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst.");
                }
            }
            else if(_timer <= _halfWave)
            {
                flag1 = false;
                if(!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 60f/d, 1, 2));
                    flag2 = true;
                    DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst Stop.");
                }
            }
            yield return null;
        }
    }


    IEnumerator AVS_Program_DynamicDrop_End(float transitionTime = 180f)
    {
        if(CoroutineDynamicDropStart != null)
        {
            DbgLogAvs("Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
        if(CoroutineDynamicDropTheta != null)
        {
            DbgLogAvs("Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropTheta);
        }

        yield return null;
        Cleanup(coroutineCleanupList);
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_End. Starting End Program with transitionTime=" + transitionTime + "s.");
        
        // Calculate proportional timing (based on original 180s total)
        float localTimescale = transitionTime / 180f;
        float phase1Duration = 70f * localTimescale;  // Original: 180 to 110 (70 seconds)
        float phase2Duration = 20f * localTimescale;  // Original: 110 to 90 (20 seconds)
        float phase3Duration = 90f * localTimescale;  // Original: 90 to 0 (90 seconds) - strobe transition
        
        director.ClearQueueOfType("gamma");
        director.ClearQueueOfType("monostereo");
        if(!lightControl.bilateral)
        {
            float queueTime1 = 30.0f * localTimescale;
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, queueTime1, 1, 2));
        }
        if(lightControl._gammaBurstMode != 0.0f)
        {
            float queueTime2 = 30.0f * localTimescale;
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, queueTime2, 1, 2));
        }

        // Phase 1: Initial wait (proportional to original 70 seconds)
        float elapsedTime = 0f;
        while(elapsedTime < phase1Duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_End. Stabilizing before dramatic rise. Elapsed: " + elapsedTime + "s / " + transitionTime + "s");
        float queueTime3 = 10.0f * localTimescale;
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, queueTime3, 1, 2));

        // Phase 2: Wait before strobe transition (proportional to original 20 seconds)
        while(elapsedTime < phase1Duration + phase2Duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_End. Starting dramatic rise to 40hz. Elapsed: " + elapsedTime + "s / " + transitionTime + "s");
        lightControl.SetStrobeRate(40.0f, phase3Duration);

        // Phase 3: Wait during strobe transition (proportional to original 90 seconds)
        while(elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_End. End of AVS Program. Elapsed: " + elapsedTime + "s / " + transitionTime + "s. Goodnight!");
    }

    private void Cleanup(List<int> coroutineCleanupList)
    {
        DbgLogAvs("Performing cleanup.");
        foreach (int index in coroutineCleanupList)
        {
            if (director.queue.ContainsKey(index))
            {
                DbgLogAvs("Sequencer  Director Queue (AVS Program): DynamicDrop (Transitioning). Removing " + index + " " + director.queue[index].Item2);
                director.queue.Remove(index);
            }
            else
            {
                DbgLogAvs("Cleanup: Key " + index + " not found in director.queue, skipping.", true);
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

    public void StartTrueStart() //THIS ONE IS OK TO CALL IN NORMAL TIME (NON DEVELOPMENT MODE)
    {
        DbgLogSequencer("Sequencer: Starting True Start Sequence.");
        if (sequenceRunner != null)
        {
            var def = GetSequenceDefinitionForProtocolStacks();
            if (def != null)
                sequenceRunner.StartSequence(def);
            else
                Debug.LogError("Sequencer: No SequenceDefinition for StartTrueStart().");
        }
        else
        {
            string reason = sequenceRunner == null ? "sequenceRunner is null" : "unknown";
            DbgLogSequencer("Sequencer: Cannot start opening sequence because " + reason + ".", true);
        }
    }

    /// <summary>
    /// Returns the SequenceDefinition for Protocol Stacks based on CSVLoader gameMode/subGameMode.
    /// Sequencer owns the ScriptableObject references (assigned in inspector).
    /// </summary>
    private SequenceDefinition GetSequenceDefinitionForProtocolStacks()
    {
        // Prefer the explicit reference if present; fall back to singleton if needed.
        var loader = csvLoader != null ? csvLoader : CSVLoader.instance;
        if (loader == null) return null;
        if (loader.gameMode != "Protocol Stacks") return null;

        return loader.subGameMode == "Descending"
            ? protocolStacksDescendingDefinition
            : protocolStacksAscendingDefinition;
    }

    //WOE TO YOU WHO USESE THESE START FUNCTIONS EXCEPT IN DEVELOPMENT MODE
    //THEY ARE NOT MADE OR TESTED FOR THAT (YET), THOUGH PERHAPS THEY SHOULD BE.
    //When these were first made, they were envisioned as a way to cheat the system into getting into the zone it should be at that moment.
    //it is NOT running the actual logic of the experience, so using these outside of development mode may have unintended consequences.
    //if you would like to use them that way, which would be more elegant, further development will be required.
    
    /*
    public void StartTutorialSequence()
    {
        DbgLogSequencer("Sequencer: Starting Tutorial Sequence.");
        tutorial.StartTutorial();
        StartLights();
        worldShuffler.ExcludeColorWorld("Blue");
        worldShuffler.ExcludeSoundscape("Shadow");
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.InteractiveTutorial);
        MusicSystem1.instance.SetMusicSilentLayerVolume(MusicSystem1.instance._silentVolumeHigh, 0.0f);
        director.Disable();
    }
    */
    public void StartPlayground(bool setTimeSincePlaygroundStart = false, bool beginShuffle = true, bool directorEnabled = true, float transitionTime = 20f, bool completeTutorial = true, bool startLights = true)
    {
        
        DbgLogSequencer("Sequencer: Starting Playground Sequence.");
        if(setTimeSincePlaygroundStart && TimeTrackerScript.instance != null && TimeTrackerScript.instance.TimeSincePlaygroundStart < 300f)
        {
            TimeTrackerScript.instance.SetTimeSincePlaygroundStart(300f);
            DbgLogSequencer("Sequencer: SetTimeSincePlaygroundStart to 300f (usually for debug purposes)");
        }
        if(DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode && startLights)
        {
            lightControl.SetColorWorldByType("Red", 0.0f);
        }
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);          
        if(directorEnabled)
        {
            director.Enable();
        }
        if(completeTutorial)
        {
            MusicSystem1.instance.SetMusicSilentLayerVolume(MusicSystem1.instance._silentVolumeHigh, transitionTime);
        }
        if(beginShuffle)
        {
            worldShuffler.BeginShuffle(false);
        }
        if(startLights)
        {
            StartLights();
        }
    }
    public void Initialize()
    {
        if(DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode)
        {
            DbgLogSequencer("Sequencer: Initialize() called in Development Mode. No action taken.");
            return;
        }
        else if (DevelopmentMode.instance == null)
        {
            DbgLogSequencer("Sequencer: Initialize() called. This should only happen in developmentMode.", true);
        }
    }

    /// <summary>Dev UI only: options that jump mid-session (playground / savasana shortcuts) were removed — use <see cref="StartTrueStart"/>.</summary>
    private void OnStartModeDropdownChanged(int index)
    {
        if(startModeDropdown == null)
            return;

        switch (index)
        {
            case 0:
                Initialize();
                DbgLogSequencer("Sequencer: Initialize called.");
                break;
            case 1:
                StartTrueStart();
                DbgLogSequencer("Sequencer: StartTrueStart called.");
                break;
            case 2:
                DbgLogSequencer("Sequencer: Tutorial start shortcut removed — use Start True Start.", true);
                break;
            case 3:
            case 4:
            case 5:
                DbgLogSequencer("Sequencer: Mid-session start shortcuts removed — use Start True Start (index 1) so countdown and stages run from the beginning.", true);
                break;
            default:
                Initialize();
                break;
        }
    }

    public void StartLightsWithDelay()
    {
        DbgLogSequencer("Sequencer: Starting Lights with Delay");
        StartCoroutine(StartLightsCoroutine());
    }

    private IEnumerator StartLightsCoroutine()
    {
        DbgLogSequencer("Sequencer: Waiting for 1 second before starting lights");
        yield return new WaitForSeconds(1f);
        StartLights();
    }

    public void MakeWwiseTone()
    {
        StartCoroutine(MakeWWiseToneCoroutine());
    }
    private IEnumerator MakeWWiseToneCoroutine()
    {
        DbgLogSequencer("Sequencer: Triggering a False Tone in WWise");
        MusicSystem1.instance.PostTheToningEvents();
        float _t = 4f;
        while (_t > 0)
        {
            if(MusicSystem1.instance.localToneOn)
            {   //if an actual music system tone comes on, break the loop, so we don't de-activate it here.
                yield break;
            }
            _t -= Time.deltaTime;
            yield return null;
        }
        
        if(MusicSystem1.instance.localToneOn)
        { //just in case this would end on the exact frame that the tone starts, check again...
            yield break;
        }

        DbgLogSequencer("Sequencer: Stopping a False Tone in WWise");
        MusicSystem1.instance.StopWwiseToning();
    }
}
