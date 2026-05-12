using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
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
    public RespirationTracker respirationTracker;
    public WwiseVOManager wwiseVOManager;
    public Director director;
    public Tutorial tutorial;
    public SavasanaPlayer savasana;
    public WorldShuffler worldShuffler;
    public CSVWriter csvWriter;

    [Header("Debug logs")]
    [SerializeField] private bool debugAllowLogsSequencer = true;
    [Tooltip("When true, warnings still log even if Sequencer info logs are off.")]
    [SerializeField] private bool debugAllowLogsWarnings = true;

    /// <summary>For <see cref="IStageHandler"/> and other non-<see cref="Sequencer"/> code: warning path only, respects <see cref="debugAllowLogsWarnings"/>.</summary>
    public void LogSequencerWarning(string message) => DbgLogSequencer(message, true);

    /// <param name="isWarning">If true, <c>LogWarning</c> when <see cref="debugAllowLogsWarnings"/>; else <c>Log</c> when <see cref="debugAllowLogsSequencer"/>.</param>
    internal void DbgLogSequencer(string message, bool isWarning = false)
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
    private bool developmentModeWarningFlag = false;
    private Coroutine countdownCoroutine; // Reference to the coroutines
    //private int currentStage = 0; //As SonoFlore

    [SerializeField] private SequenceRunner sequenceRunner;
    private AVSSequence _avsSequence;

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

        if (sequenceRunner == null)
            sequenceRunner = gameObject.GetComponent<SequenceRunner>() ?? gameObject.AddComponent<SequenceRunner>();

        _avsSequence = GetComponent<AVSSequence>();
        if (_avsSequence == null)
            _avsSequence = gameObject.AddComponent<AVSSequence>();

        // This is the StageType -> IStageHandler registration map SequenceRunner uses to dispatch Enter/Exit and completion checks.
        // It's primary use is to pass the sequencer instance to the handlers, so they can access CountdownThisSection (main segment) / startPlayground, etc.
        // We do it like this, instead of using singletons, to prevent null references and other issues.
        _calibrationHandler = new CalibrationStageHandler(this);
        _openingHandler = new OpeningStageHandler(this);
        _playgroundHandler = new PlaygroundStageHandler(this, _avsSequence);
        _savasanaHandler = new SavasanaStageHandler(this);
        _tutorialHandler = new TutorialStageHandler(this);
        _startCountdownHandler = new StartCountdownStageHandler(this);
        _setMenuHandler = new SetMenuStageHandler(this);
        _musicPlaylistHandler = new MusicPlaylistStageHandler(this);
        _inquiryHandler = new InquiryStageHandler(this);
        _endHandler = new EndStageHandler(this);
        _linearAudioHandler = new LinearAudioStageHandler(this);
        sequenceRunner.SetHandlers(new IStageHandler[] { _calibrationHandler, _openingHandler, _startCountdownHandler, _playgroundHandler, _savasanaHandler, _tutorialHandler, _setMenuHandler, _musicPlaylistHandler, _inquiryHandler, _endHandler, _linearAudioHandler });
    }

    void OnEnable()
    {
        // If this object is enabled after load, UIManager may already exist — subscribe when possible (no retry loop).
        TrySubscribeEndThisSequenceStageUi();
    }

    void OnDisable()
    {
        UnsubscribeEndThisSequenceStageUi();
    }

    void Start()
    {
        if (TimeTrackerScript.instance == null)
            DbgLogSequencer("Sequencer: TimeTrackerScript.instance is null in Start(); session countdown is unavailable until the tracker exists. Add a TimeTrackerScript to the scene.", true);

        // Runs after all Awake() on enabled objects this frame, so UIManager.Instance is usually set if UIManager is in the scene (order vs Sequencer does not need to be "UI first").
        TrySubscribeEndThisSequenceStageUi();
    }

    private void TrySubscribeEndThisSequenceStageUi()
    {
        var ui = UIManager.Instance;
        if (ui == null)
            return;
        ui.OnEndThisSequenceStagePress -= HandleEndThisSequenceStageUi;
        ui.OnEndThisSequenceStagePress += HandleEndThisSequenceStageUi;
    }

    private void UnsubscribeEndThisSequenceStageUi()
    {
        var ui = UIManager.Instance;
        if (ui != null)
            ui.OnEndThisSequenceStagePress -= HandleEndThisSequenceStageUi;
    }

    /// <summary>Forwarded from <see cref="UIManager.OnEndThisSequenceStagePress"/> through <see cref="HandleSequenceCommand"/>.</summary>
    private void HandleEndThisSequenceStageUi()
    {
        HandleSequenceCommand(SequenceCommand.EndThisSequenceStage);
    }


// if(DevelopmentMode.Instance != null && DevelopmentMode.Instance.developmentMode)
 // {   //do something  }
    
   
    // Update is called once per frame
    void Update()
    {
        // Sequencing now advances via SequenceRunner + stage handlers.
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
    //Adjunctive Sequence
    //====================================================================================================
    //TODO:
    // [ ] AkSoundEngine.PostEvent("Play_sfx_EndInteractive", gameObject); for when the mic goes off.
    // [ ] Missing: `FadeOut()` (Environment mode + Dark color)
    // [ ] CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End(180f));
    // [ ] tutorial.StopTutorial(), once we add the dynamic tutorial.






    /// <summary>Dispatches cue to current handler if watching. Returns true if handled; false means no active stage consumed it.</summary>
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

    // Debug helper: set to true to advance PlaygroundStageHandler waits (countdown or step)
    private bool _forceSequenceAdvanceRequested = false;

    /// <summary>For PlaygroundStageHandler and debug stepping. Get/set the force-advance flag.</summary>
    public bool ForceSequenceAdvanceRequested { get => _forceSequenceAdvanceRequested; set => _forceSequenceAdvanceRequested = value; }

    /// <summary>
    /// Advances active PlaygroundStageHandler waits. Call from InputReferences or elsewhere for debug stepping.
    /// </summary>
    public void ForceSequenceAdvance()
    {
        _forceSequenceAdvanceRequested = true;
        DbgLogSequencer("Sequencer: ForceSequenceAdvance() called - advancing to next step.");
    }

    public void FadeOut()
    {
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Environment);
        LightControl.instance?.SetPreferredColor("Dark", 18f);
    }
    
    

    //====================================================================================================
    //PUBLIC METHODS
    //====================================================================================================

    /// <summary>Stops all <see cref="AVSSequence"/> program coroutines (Dynamic Drop, Drop-to-Delta) and clears tracked director queue indices.</summary>
    public void StopAllAvsPrograms() => _avsSequence?.StopAllAvsPrograms();

    [Obsolete("StartTrueStart is deprecated. Use SequenceRunner.StartFromCurrentCsvSession().")]
    public void StartTrueStart() // Deprecated compatibility wrapper for dev UI and legacy scene hooks.
    {
        DbgLogSequencer("Sequencer: StartTrueStart is deprecated; forwarding to SequenceRunner.StartFromCurrentCsvSession().", true);
        if (sequenceRunner != null)
        {
            sequenceRunner.StartFromCurrentCsvSession();
        }
        else
        {
            string reason = sequenceRunner == null ? "sequenceRunner is null" : "unknown";
            DbgLogSequencer("Sequencer: Cannot start opening sequence because " + reason + ".", true);
        }
    }


    /// <summary>
    /// Facade helper for handlers/UI: Adjunctive branch entry that starts interactive sequence.
    /// Canonical sequence-start ownership remains in <see cref="SequenceRunner"/>.
    /// </summary>
    public void StartProtocolStacksInteractiveSequence()
    {
        if (sequenceRunner == null)
        {
            DbgLogSequencer("Sequencer: Cannot start Adjunctive interactive sequence because sequenceRunner is null.", true);
            return;
        }
        sequenceRunner.StartProtocolStacksInteractiveSequence();
    }

    /// <summary>
    /// Facade helper for handlers/UI: Adjunctive branch entry that starts the 60-minute music playlist sequence.
    /// Canonical sequence-start ownership remains in <see cref="SequenceRunner"/>.
    /// </summary>
    public void StartProtocolStacksMusicPlaylist60mSequence()
    {
        if (sequenceRunner == null)
        {
            DbgLogSequencer("Sequencer: Cannot start Adjunctive 60m music playlist sequence because sequenceRunner is null.", true);
            return;
        }
        sequenceRunner.StartProtocolStacksMusicPlaylist60mSequence();
    }

    /// <summary>
    /// Facade helper for handlers/UI: Adjunctive branch entry that starts the 40-minute music playlist sequence.
    /// Canonical sequence-start ownership remains in <see cref="SequenceRunner"/>.
    /// </summary>
    public void StartProtocolStacksMusicPlaylist40mSequence()
    {
        if (sequenceRunner == null)
        {
            DbgLogSequencer("Sequencer: Cannot start Adjunctive 40m music playlist sequence because sequenceRunner is null.", true);
            return;
        }
        sequenceRunner.StartProtocolStacksMusicPlaylist40mSequence();
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
        LightControl.instance?.StartLights();
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
            LightControl.instance?.SetColorWorldByType("Red", 0.0f);
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
            LightControl.instance?.StartLights();
        }
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
