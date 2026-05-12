using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using AK.Wwise;
using TMPro;
using ConversionUtilities;
using System.Linq;

//todo: when we are in a MusicLoop, we should not be able to transition into Environment mode.
//todo: and if we transtion into a MusicLoop, we should transition out of Environment mode.
//todo: ultimately, though, environment mode should just be run as a MusicLoop.
//todo: and we should use it in conjunction with other MusicLoop type content.
public class MusicSystem1 : MonoBehaviour
{
    public static MusicSystem1 instance {get; private set;}
    
    // Debug log category flags
    private bool debugAllowBassSynthLogs = false;
    private bool debugAllowBasicToningLogs = false;
    private bool debugAllowFundamentalLockLogs = true;
    private bool debugAllowFundamentalLogicLogs = true;
    private bool debugAllowFundamentalChangeLogs = true;
    private bool debugAllowHarmonyLogicLogs = false;
    private bool debugAllowHarmonyChangeLogs = true;
    private bool debugAllowMusicModeLogs = true;
    private bool debugAllowSoundscapeLogs = true;
    private bool debugAllowImitoneUpdateLogs = false;
    private bool debugAllowMixVolumeLogs = true;
    private bool debugAllowSFXLogs = false; // Includes ThumpUpdate and Breathwork
    private bool debugAllowInitializationLogs = true;
    private bool debugAllowWarnings = true; // Warnings show if this OR the category flag is true
    
    
    public Sequencer sequencer;
    public WwiseVOManager wwiseVOManager;
    public WorldShuffler worldShuffler;
    public RespirationTracker respirationTracker;
    public Director director;
    public LightControl lightControl;
    [SerializeField] private DirectVoiceMonitoring directVoiceMonitoring;
    public Tutorial tutorial;
    public SavasanaPlayer SavasanaPlayer;
   // public RecordedAudioPlayback recordedAudioPlayback;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; // Reference to an object that interprets voice to musical notes
    private Dictionary<NoteName, (float ActivationTimer, bool Active, bool FirstFrameActive, float ChangeFundamentalTimer)> NoteTracker = new Dictionary<NoteName, (float, bool, bool, float)>();
    // Tracks information for each musical note:
    // ActivationTimer: Time duration the note has been active
    // Active: Whether the note is currently active
    // ChangeFundamentalTimer: Timer for changing the fundamental note
    
    // IMITONE INTERPRETATION AND BASIC TONES
    private float musicNoteInputRaw; // The raw note input from voice interpretation
    private float musicNoteInput; // Adjusted musical note input after processing
    public NoteName musicNoteActivated {get; private set;} = NoteName.None; // The note that has been activated (while we are toneActiveBiasTrue), None if no note is activated    
    private float _constWiggleRoomPerfect = 0.5f; // Tolerance for note variation
    private float _constWiggleRoomUnison = 1.5f;
    private NoteName? directorStoredFundamental = null;
    private NoteName nextNote = NoteName.None; // Next note to activate
    private float highestActivationTimer = 0.0f;
    public bool localToneOn {get; private set;} = false;
    /// <summary>Wwise Play_Toning_v3_FundamentalOnly / Stop_Toning_v3_FundamentalOnly state (see also ToningV3HarmonyPlaying).</summary>
    public bool ToningV3FundamentalPlaying { get; private set; } = false;
    /// <summary>Wwise Play_Toning_v3_HarmonyOnly / Stop_Toning_v3_HarmonyOnly state.</summary>
    public bool ToningV3HarmonyPlaying { get; private set; } = false;
    private bool previousLocalToneOn = false;
    private bool localBassSynthToneOn = false; // Controls BassSynth based on toneActiveConfident
    private bool previousLocalBassSynthToneOn = false;

    
    public float _silentVolumeLow = 65f; //this was 50f, Robin changed it on 4/4/2025
    public float _silentVolumeHigh = 80f;

    // FUNDAMENTAL AND HARMONY CONTROL
    private float _queueFundamentalChangeThreshold = 12f;
    private float _initiateImminentFundamentalChangeThreshold = 22f; //was 35f, changed on 4/4/2025
    public NoteName fundamentalNoteName = NoteName.A; // Base note around which other notes are calculated
    private NoteName? fundamentalNoteCompare = null; // Used to catch changes that are not triggered in this script, and to compare with the previous fundamentalNoteName for the purpose of changing the fundamental
    public NoteName harmonyNote = NoteName.None; // Note that plays in harmony with the fundamental note
    private float fundamentalTimeSinceLastTrigger   = 0f;
    private float harmonyTimeSinceLastTrigger = 0f;
    private float fundamentalRetriggerThreshold = 18f; // minimum time between fundamental retriggering
    private float harmonyRetriggerThreshold = 6f; // minimum time between harmony retriggering

    // BASSSYNTH CONTROL AND OTHER SUBACOUSTIC SOUNDS
    private bool bassSynthPlaying = false; // Whether BassSynth is currently playing
    private NoteName? currentBassSynthPitch = null; // Current pitch switch value for BassSynth
    private bool bassSynthNonePitchWarningLogged = false; // Track if we've already logged the None pitch warning
    private bool impactSoundFlag = false; // Track if the impact sound has been played
    
    // BassSynth cooldown system (4 second cooldown between start/pitch changes)
    private const float BASS_SYNTH_COOLDOWN = 4.0f;
    private float bassSynthCooldownTimer = 0f; // Time since last start/pitch change action
    private bool pendingBassSynthStart = false; // Start action pending during cooldown
    private NoteName? pendingBassSynthPitch = null; // Pitch change pending during cooldown

    //HARMONY SEQUENCES
    List<int> harmonySequence1 = new List<int> {5, 7, 5, 7, 5, 7, 5, 7};
    List<int> harmonySequence2 = new List<int> {5, 12, 5, 12, 5, 12, 5, 12};
    List<int> harmonySequence3 = new List<int> {7, 12, 7, 12, 7, 12, 7, 12};
    List<int> harmonySequence4 = new List<int> {5, 7, 12, 5, 7, 12, 5, 7, 12, 5, 7, 12};
    List<List<int>> sequences;
    System.Random random = new System.Random();
    int currentSequenceIndex;
    int currentHarmonyIndex = 0;

    //DIRECT MONITORING

    
    // FUNDAMENTAL LOCKING SYSTEM
    // Three separate lock types with priority: DebugLock > ContentLock > ModeLock
    private NoteName? fundamentalModeLock = null;      // Mode-based lock (Tutorial, FrozenFreeplay)
    private NoteName? fundamentalContentLock = null;    // Content-based lock (MusicLoop compatibility)
    private NoteName? fundamentalDebugLock = null;      // Debug lock (development mode)
    public MusicMode currentMusicMode;
    public InteractionType currentInteractionType = InteractionType.SoundWorld; // so when we shift into a mode that plays interactive music, we are using the right sub-system. This is getting complicated. Will be less so when we use environment as a musicLoop or something. 
    private bool interactiveMusicFlag = false;
    // Stage D: ambient bed lifecycle (play/stop/idempotency/delayed-stop) extracted to MusicSystemLinear.
    // EnterMusicEnvironmentAudio / ExitMusicEnvironmentAudio below delegate to MusicSystemLinear.instance.

    public string currentSwitchState = "C";

    //PLAYBACK AND INITIALIZATION
    //private bool environmentFlag = false;
    
    private bool modeSilentFlag = false;
    private bool modeTutorialFlag = false;
    private bool modeFreeplayFlag = false;
    private bool modeFrozenFreeplayFlag = false;
    private bool modeEnvironmentFlag = false;
    private bool modeMusicLoopSilentFlag = false;
    public TMP_Dropdown soundscapeDropdown;

    //SOUNDSCAPE LISTS
    // SoundWorlds work with any fundamental note
    private static readonly List<string> soundWorlds = new List<string>
    {
        "SonoFlore",
        "Shadow",
        "Gentle",
        "Shruti"
    };

    // MusicLoops only work with specific fundamental notes
    // Dictionary maps MusicLoop name to its required fundamental NoteName
    private static readonly Dictionary<string, NoteName> musicLoops = new Dictionary<string, NoteName>
    {
        { "ShiftingEarth", NoteName.C },      // TODO: Set correct fundamental for each MusicLoop
        { "SitarAmbience", NoteName.C },      // TODO: Set correct fundamental for each MusicLoop
        { "PinkNoiseAtmosphere", NoteName.As }  // TODO: Set correct fundamental for each MusicLoop
    };

    private bool haveSetSoundWorldFlag = false;
    public NoteName permanentlySetFundamental = NoteName.None;
    
    // SIMPLIFICATION SWITCHES - Debug flags to disable systems for troubleshooting
    private bool enableFundamentalTracking = true;
    private bool enableHarmonyTracking = true;
    private bool enableBassSynth = true;
    private bool enableBasicToning = true;
    private bool enableDirectVoiceMonitoring = true;
    private bool monitoringAttenuationApplied = false;
    private bool tutorialMonitoringOverrideActive = false;
    private bool calibrationMonitoringOverrideActive = false;
    private bool enableThumpSFX = true;
    private bool enableImitoneInterpretation = true;

    //BREATHWORK CYCLE AND SFX
    private bool breathworkCyclePlaying = false;
    private bool allowThumpAlways = true;
    private bool allowThumpWhenModeIsPlayful = true;
    private bool allowThump = true;


    void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Ensure AkGameObj component exists for Wwise registration
        if (GetComponent<AkGameObj>() == null)
        {
            gameObject.AddComponent<AkGameObj>();
        } else 
        {
            if(debugAllowInitializationLogs)
            {
                Debug.Log("MUSIC: AkGameObj component already exists");
            }
        }

         if (soundscapeDropdown != null)
            soundscapeDropdown.onValueChanged.AddListener(OnSoundscapeDropdownChanged);

        //INITIALIZE SWITCHES — both 12-pitch groups need a value before any event uses them (otherwise Wwise: "No default Switch value selected").
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", NoteUtils.NoteToWwiseString(fundamentalNoteName), gameObject);
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", "C", gameObject);
    }



    public void OnPermanentlySetFundamentalChanged(int index)
    {
        switch(index)
        {
            case 1:
                SetFundamentalDebugLock(NoteName.C);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to C");
                }
                break;
            case 2:
                SetFundamentalDebugLock(NoteName.Cs);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to Cs");
                }
                break;
            case 3:
                SetFundamentalDebugLock(NoteName.D);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to D");
                }
                break;
            case 4:
                SetFundamentalDebugLock(NoteName.Ds);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to Ds");
                }
                break;
            case 5:
                SetFundamentalDebugLock(NoteName.E);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to E");
                }
                break;
            case 6:
                SetFundamentalDebugLock(NoteName.F);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to F");
                }
                break;
            case 7:
                SetFundamentalDebugLock(NoteName.Fs);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to Fs");
                }
                break;
            case 8:
                SetFundamentalDebugLock(NoteName.G);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to G");
                }
                break;
            case 9:
                SetFundamentalDebugLock(NoteName.Gs);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to Gs");
                }
                break;
            case 10:
                SetFundamentalDebugLock(NoteName.A);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to A");
                }
                break;
            case 11:
                SetFundamentalDebugLock(NoteName.As);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to As");
                }
                break;
            case 12:
                SetFundamentalDebugLock(NoteName.B);
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to B");
                }
                break;
            default:
                SetFundamentalDebugLock(null); // Clear debug lock
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC: Debug Override Fundamental Changed to None (debug lock cleared)");
                }
                break;
        }
    }

    void Start()
    {

        // Initialize the NoteTracker dictionary with 12 keys for each note in an octave
        for (NoteName note = NoteName.C; note <= NoteName.B; note++)
        {
            NoteTracker.Add(note, (0f, false, false, 0f));
        }
        //Set these so they can be triggered right away
        fundamentalTimeSinceLastTrigger = fundamentalRetriggerThreshold;
        harmonyTimeSinceLastTrigger = harmonyRetriggerThreshold;
        
        //Initialize harmony sequences
        sequences = new List<List<int>>
        {
            harmonySequence1,
            harmonySequence2,
            harmonySequence3,
            harmonySequence4
        };

        currentSequenceIndex = random.Next(sequences.Count);

        // If the sound world has not yet been set, set it to Gentle to prevent bug.
        if (!haveSetSoundWorldFlag)
        {
            SetSwitchRestoreToningV3("SoundWorldMode_Switch", "Gentle");
            SetSoundWorldFlag();
        }
    }

    void Update()
    {
        localToneOn = imitoneVoiceInterpreter.toneActiveBiasTrue;
        localBassSynthToneOn = imitoneVoiceInterpreter.toneActiveConfident; // BassSynth uses toneActiveConfident

        if (currentMusicMode == MusicMode.Silent)
        {
            //PUT STUFF HERE IF NECESSARY
        }
        else if (currentMusicMode == MusicMode.InteractiveTutorial)
        {
            DynamicMusicSystem();
        }
        else if(currentMusicMode == MusicMode.Freeplay) 
        { 
            DynamicMusicSystem();
        }
        else if (currentMusicMode == MusicMode.FrozenFreeplay)
        {
            //PUT STUFF HERE IF NECESSARY
        }

        if(enableThumpSFX)
        {
            ThumpUpdate();
        }

        // Keyboard shortcuts for toggling systems (Keys 1-7)
        //HandleKeyboardToggles();
    }

    private bool TryResolveDirectVoiceMonitoring()
    {
        if (directVoiceMonitoring == null)
        {
            directVoiceMonitoring = FindObjectOfType<DirectVoiceMonitoring>();
        }

        return directVoiceMonitoring != null;
    }

    private void SetMonitoringAttenuationOnce(bool attenuate)
    {
        if (!TryResolveDirectVoiceMonitoring())
        {
            return;
        }

        if (monitoringAttenuationApplied == attenuate)
        {
            return;
        }

        directVoiceMonitoring.AttenuateMonitoring(attenuate);
        monitoringAttenuationApplied = attenuate;
    }

    /// <summary>
    /// Allows external owners (e.g. TutorialStageHandler) to keep attenuation cache in sync
    /// when they directly call DirectVoiceMonitoring.AttenuateMonitoring(...).
    /// </summary>
    public void NotifyMonitoringAttenuationChangedExternally(bool attenuated)
    {
        monitoringAttenuationApplied = attenuated;
    }

    private void OnInteractionTypeChanged(InteractionType newInteractionType)
    {
        bool changed = currentInteractionType != newInteractionType;
        currentInteractionType = newInteractionType;
        if (!changed || IsAnyMonitoringAttenuationOverrideActive())
        {
            return;
        }

        // Edge-triggered attenuation: ON for SoundWorld (quieter), OFF for MusicLoop (louder).
        SetMonitoringAttenuationOnce(currentInteractionType == InteractionType.SoundWorld);
    }

    public void SyncMonitoringAttenuationFromInteractionType()
    {
        if (IsAnyMonitoringAttenuationOverrideActive())
        {
            SetMonitoringAttenuationOnce(false);
            return;
        }

        // Used when playground (Freeplay) starts to re-apply interaction-based attenuation once.
        // Attenuate on SoundWorld (quieter), not on MusicLoop (louder) — see OnInteractionTypeChanged.
        SetMonitoringAttenuationOnce(currentInteractionType == InteractionType.SoundWorld);
    }

    public void SetTutorialMonitoringOverride(bool tutorialActive)
    {
        if (tutorialMonitoringOverrideActive == tutorialActive)
        {
            return;
        }

        tutorialMonitoringOverrideActive = tutorialActive;
        if (IsAnyMonitoringAttenuationOverrideActive())
        {
            // Tutorial (or calibration) owns attenuation while active; MusicSystem only blocks its own interaction-driven writes.
            return;
        }

        // Priority lifted: immediately apply interaction-based attenuation once.
        SyncMonitoringAttenuationFromInteractionType();
    }

    /// <summary>
    /// Calibration sibling of <see cref="SetTutorialMonitoringOverride"/>: while active, forces monitoring unattenuated (louder)
    /// and blocks <see cref="OnInteractionTypeChanged"/> / <see cref="SyncMonitoringAttenuationFromInteractionType"/> from
    /// re-driving attenuation. Calibration and tutorial never overlap in normal flow, but the override flags coexist safely
    /// (both must be released before interaction-based attenuation resumes).
    /// </summary>
    public void SetCalibrationMonitoringOverride(bool calibrationActive)
    {
        if (calibrationMonitoringOverrideActive == calibrationActive)
        {
            return;
        }

        calibrationMonitoringOverrideActive = calibrationActive;
        if (IsAnyMonitoringAttenuationOverrideActive())
        {
            return;
        }

        SyncMonitoringAttenuationFromInteractionType();
    }

    private bool IsAnyMonitoringAttenuationOverrideActive()
    {
        return tutorialMonitoringOverrideActive || calibrationMonitoringOverrideActive;
    }

    /// <summary>
    /// Handles keyboard input for toggling system features (Keys 1-7)
    /// </summary>
    private void HandleKeyboardToggles()
    {
        // Key 1: Toggle Fundamental Tracking
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetFundamentalTrackingEnabled(!enableFundamentalTracking);
            Debug.Log($"MUSIC KEYBOARD: Fundamental Tracking toggled to {(enableFundamentalTracking ? "ON" : "OFF")}");
        }

        // Key 2: Toggle Harmony Tracking
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetHarmonyTrackingEnabled(!enableHarmonyTracking);
            Debug.Log($"MUSIC KEYBOARD: Harmony Tracking toggled to {(enableHarmonyTracking ? "ON" : "OFF")}");
        }

        // Key 3: Toggle BassSynth
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetBassSynthEnabled(!enableBassSynth);
            Debug.Log($"MUSIC KEYBOARD: BassSynth toggled to {(enableBassSynth ? "ON" : "OFF")}");
        }

        // Key 4: Toggle Basic Toning
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetBasicToningEnabled(!enableBasicToning);
            Debug.Log($"MUSIC KEYBOARD: Basic Toning toggled to {(enableBasicToning ? "ON" : "OFF")}");
        }

        // Key 5: Toggle Direct Voice Monitoring
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            enableDirectVoiceMonitoring = !enableDirectVoiceMonitoring;
            if (directVoiceMonitoring == null)
            {
                directVoiceMonitoring = FindObjectOfType<DirectVoiceMonitoring>();
            }

            if (directVoiceMonitoring != null)
            {
                directVoiceMonitoring.SetDirectVoiceMonitoringEnabled(enableDirectVoiceMonitoring);
            }

            LogSimplificationStatus();
            Debug.Log($"MUSIC KEYBOARD: Direct Voice Monitoring toggled to {(enableDirectVoiceMonitoring ? "ON" : "OFF")}");
        }

        // Key 6: Toggle Thump SFX
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SetThumpSFXEnabled(!enableThumpSFX);
            Debug.Log($"MUSIC KEYBOARD: Thump SFX toggled to {(enableThumpSFX ? "ON" : "OFF")}");
        }

        // Key 7: Toggle Imitone Interpretation
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SetImitoneInterpretationEnabled(!enableImitoneInterpretation);
            Debug.Log($"MUSIC KEYBOARD: Imitone Interpretation toggled to {(enableImitoneInterpretation ? "ON" : "OFF")}");
        }
    }

    private void DynamicMusicSystem()
    {
        if(enableImitoneInterpretation)
        {
            InterpretImitoneUpdate();
        }
        if(enableBasicToning)
        {
            BasicToningUpdate();
        }
        if(enableBassSynth)
        {
            BassSynthUpdate();
        }
        if(enableFundamentalTracking)
        {
            FundamentalUpdate();
        }
        if(enableHarmonyTracking)
        {
            HarmonyUpdate();
        }
    }

    //Take the fundamental behaviors in the InterpretImitonUpdate method and move them here for clarity
    private void FundamentalUpdate()
    {
        var updates = new Dictionary<NoteName, (float, bool, bool, float)>();
        List<float> fundamentalTimerValues = new List<float>();
        float highestFundamentalTimer = 0;

        // Cache keys to avoid modifying the dictionary while iterating
        List<NoteName> noteKeys = new List<NoteName>(NoteTracker.Keys);

        if (imitoneVoiceInterpreter.imitoneActive)
        {
            // First get the highest fundamental timer at the start
            foreach (NoteName key in noteKeys)
            {
                fundamentalTimerValues.Add(NoteTracker[key].ChangeFundamentalTimer);
                if (NoteTracker[key].ChangeFundamentalTimer > highestFundamentalTimer)
                {
                    highestFundamentalTimer = NoteTracker[key].ChangeFundamentalTimer;
                }
            }

            // Perform the updates
            foreach (NoteName key in noteKeys)
            {
                var scaleNote = NoteTracker[key];
                float newChangeFundamentalTimer = scaleNote.ChangeFundamentalTimer;

                if (scaleNote.Active)
                {
                    // Use fundamentalNoteName field directly
                    if (key != fundamentalNoteName)
                    {
                        // Conversion point: NoteName -> int for distance calculation
                        // GetWrappedDistance() handles NoteName enum internally, converts to int for arithmetic
                        // Calculate the wrapped distance between key and fundamentalNoteName using utility function
                        int d = NoteUtils.GetWrappedDistance(key, fundamentalNoteName);
                        if (d < 0)
                        {
                            if(debugAllowWarnings || debugAllowFundamentalLogicLogs)
                            {
                                Debug.LogWarning($"MUSIC: GetWrappedDistance() returned -1 (indicating None was passed) for key={key}, fundamentalNoteName={fundamentalNoteName} - distance calculation may be incorrect");
                            }
                        }

                        // Change timer rate based on absorption and wrapped distance from fundamental
                        float _slowWhenHighAbsorption = Mathf.Pow(2, Mathf.Clamp(RespirationTracker.instance._absorption, 0, 1) * -1);
                        float _fastWhenVeryDifferent = (d > 4 ? 2.0f : 1.0f);
                        float _newChangeMultiplier = _slowWhenHighAbsorption * _fastWhenVeryDifferent;
                        newChangeFundamentalTimer += (Time.deltaTime * _newChangeMultiplier);

                        // Fundamental change conditions
                        bool isHighestFundamentalTimer = newChangeFundamentalTimer >= highestFundamentalTimer;
                        bool retriggerTest = (fundamentalTimeSinceLastTrigger >= fundamentalRetriggerThreshold);
                        bool test = !IsFundamentalLocked() && retriggerTest && isHighestFundamentalTimer;
                        bool directorMatchTest = directorStoredFundamental != key;

                        bool highThresholdPass = newChangeFundamentalTimer >= (_initiateImminentFundamentalChangeThreshold);
                        bool highThresholdPass_variation = newChangeFundamentalTimer >= (_initiateImminentFundamentalChangeThreshold - 5.0f);
                        bool lowThresholdPass = newChangeFundamentalTimer >= (_queueFundamentalChangeThreshold);

                        bool longTest = test && highThresholdPass;
                        bool longishTest = test && highThresholdPass_variation && scaleNote.FirstFrameActive;
                        bool shortTest = test && lowThresholdPass && directorMatchTest && scaleNote.FirstFrameActive;

                        if (longTest || longishTest)
                        {
                            if (debugAllowFundamentalLogicLogs)
                            {
                                if (longTest)
                                    Debug.Log("MUSIC: Long Test Instantly Triggering Fundamental Change to " + NoteUtils.NoteToWwiseString(key));
                                else
                                    Debug.Log("MUSIC: Longish Test Instantly Triggering Fundamental Change to " + NoteUtils.NoteToWwiseString(key));
                            }

                            ChangeFundamental(key);
                            director.ActivateQueue(5.0f);
                        }
                        else if (shortTest)
                        {
                            director.ClearQueueOfType("fundamentalChange");
                            director.AddActionToQueue(Action_ChangeFundamental(key), "fundamentalChange", true, false, 9999f, DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.ReplaceAllOfType);
                            directorStoredFundamental = key;

                            if (debugAllowFundamentalLogicLogs)
                            {
                                Debug.Log("MUSIC: Short Test New Fundamental Queued: " + NoteUtils.NoteToWwiseString(key));
                            }
                        }
                    }
                    else
                    {
                        // Reduce timers on other notes when current note is fundamental
                        foreach (NoteName otherKey in noteKeys)
                        {
                            if (otherKey != key)
                            {
                                var otherNote = NoteTracker[otherKey];
                                float newChangeFundamentalTimerOther = Mathf.Max(0, otherNote.ChangeFundamentalTimer - Time.deltaTime * 0.075f);
                                updates[otherKey] = (otherNote.ActivationTimer, otherNote.Active, otherNote.FirstFrameActive, newChangeFundamentalTimerOther);
                            }
                        }
                    }
                }

                // Save updated state
                updates[key] = (scaleNote.ActivationTimer, scaleNote.Active, scaleNote.FirstFrameActive, newChangeFundamentalTimer);
            }

            // Apply all updates at once
            foreach (var update in updates)
            {
                NoteTracker[update.Key] = update.Value;
            }
        }

        fundamentalTimeSinceLastTrigger += Time.deltaTime;
        harmonyTimeSinceLastTrigger += Time.deltaTime;
    }


    private void HarmonyUpdate()
    {
        if(imitoneVoiceInterpreter.toneActiveBiasTrueFrame)
        {
            //Choose a tone based on a sequence
            List<int> currentSequence = sequences[currentSequenceIndex];
            int harmonization = currentSequence[currentHarmonyIndex];

            // Move to the next note in the sequence
            currentHarmonyIndex++;

            // If we've reached the end of the sequence, select a new sequence
            if (currentHarmonyIndex >= currentSequence.Count)
            {
                currentSequenceIndex = random.Next(sequences.Count);
                currentHarmonyIndex = 0;
                if(debugAllowHarmonyLogicLogs)
                {
                    Debug.Log("MUSIC: New Harmony Sequence Selected:" + currentSequenceIndex);
                }
            }

            //Now play the tone
                            
            // Conversion point: NoteName arithmetic - AddInterval() handles NoteName enum internally
            // Converts to int for modulo arithmetic, then back to NoteName enum
            // Calculate harmony note by adding interval to fundamental note
            harmonyNote = NoteUtils.AddInterval(fundamentalNoteName, harmonization);
            if (harmonyNote == NoteName.None)
            {
                if(debugAllowWarnings || debugAllowHarmonyChangeLogs)
                {
                    Debug.LogWarning($"MUSIC: AddInterval() returned NoteName.None for fundamentalNoteName={fundamentalNoteName}, harmonization={harmonization} - harmony will not play correctly");
                }
            }
            changeHarmony(harmonyNote); 
            if (debugAllowHarmonyChangeLogs)
            {
                Debug.Log("MUSIC: Harmony Played: " + NoteUtils.NoteToWwiseString(harmonyNote) + " ~ (fundamentalNoteName + " + harmonization + ")");
            }
        }
    }

    private void ThumpUpdate ()
    {
        if(GameValues.instance._chantCharge < 0.5f)
        {
            impactSoundFlag = false;
        }

        if(GameValues.instance._chantCharge >= 0.99f)
        {
            allowThump = allowThumpAlways || (allowThumpWhenModeIsPlayful && respirationTracker.modePlayful);
            if(!impactSoundFlag && allowThump)
            {
                if(debugAllowSFXLogs)
                {
                    Debug.Log("MUSIC: impact");
                }
                AkSoundEngine.PostEvent("Play_sfx_Impact",gameObject);
                impactSoundFlag = true;   
                lightControl.FXWave(0.8f, 1.5f, 0.1f, true);
            }
        }
    }

    public void SetAllowThumpAlways(bool allow)
    {
        if(debugAllowSFXLogs)
        {
            Debug.Log("MUSIC: Setting allowThumpAlways to " + allow);
        }
        allowThumpAlways = allow;
    }

    public void SetAllowThumpWhenModeIsPlayful(bool allow)
    {
        if(debugAllowSFXLogs)
        {
            Debug.Log("MUSIC: Setting allowThumpWhenModeIsPlayful to " + allow);
        }
        allowThumpWhenModeIsPlayful = allow;
    }

    //TODO: SOME IMPPORTANT CLEAN-UP WORK
    //Right now, "playground" turns on, but "playground" includes "Environment".
    //These need to be brought together.
    //As far as MusicSystem1 is concerned, and the below method, there should basically just be three modes: "Environment", "InteractiveMusicSystem", and "Silent".
    //(Possibly also a "tutorial" mode)
    //Without the hierarchy of "playground" (formerly "interactive") over "environment" / "interactive" (which is confusing)

    //freeplay (bool)
    // -> environment (not even set - interacts straight with WWise)
    // -> interactiveMusicMode (not even set - interacts straight with WWise)

    // ~~~~
    public enum MusicMode
    {
        Silent,
        InteractiveTutorial,
        Freeplay,
        FrozenFreeplay,
        Environment,
        MusicLoopSilent //this is a temporary mode for a musicloop version of silent mode, before we merge the two silent modes.
    }

    public enum InteractionType
    {
        SoundWorld,
        MusicLoop
    }
 

    public void SetMusicModeTo(MusicMode mode)
    {
        if(debugAllowMusicModeLogs)
        {
            Debug.Log("MUSIC: Setting Music Mode to " + mode + "...");
        }
        
        // Leaving environment: breathwork off, Wwise MusicEnvironmentMode → Music + delayed ambient stop
        if (currentMusicMode == MusicMode.Environment && mode != MusicMode.Environment)
        {
            SetBreathworkCycle(false);
            ExitMusicEnvironmentAudio();
        }

        switch (mode)
        {

            case MusicMode.MusicLoopSilent:
            currentMusicMode = mode;
            if(!modeMusicLoopSilentFlag)
            {
                SetMusicModeFlags(false, false, false, false, false, true);
                if(debugAllowMusicModeLogs)
                {
                    Debug.Log("MUSIC: Music Mode Set to MusicLoopSilent, which is a temporary mode for a musicloop version of silent mode, before we merge the two silent modes.");
                }
                //imitoneVoiceInterpreter.gameOn = false; //peculaiarity of Ascending/Descending, we are keeping gameOn true for now. This will have to be addressed in the future.
                // Set state to MusicLoops and ensure interaction type is MusicLoop (required for this mode)
                //RecoverInteractiveMusicModeFromInteractionType(); //this may be necessary in futrue...
                OnInteractionTypeChanged(InteractionType.MusicLoop);
                RunWithToningRestoredAfterInteractiveSwitch(() =>
                {
                    AkSoundEngine.SetSwitch("InteractiveMusicMode_Switch", "MusicLoops", gameObject);
                    AkSoundEngine.SetSwitch("MusicLoops_Switch", "Silence", gameObject);
                });
            }
            else
            {
                if(debugAllowWarnings || debugAllowMusicModeLogs)
                {
                    Debug.LogWarning("MUSIC: Tried to set Music Mode to MusicLoopSilent, but it was already set to MusicLoopSilent");
                }
            }
            break;

            case MusicMode.Silent:
            currentMusicMode = mode;
            if(!modeSilentFlag)
            {
                SetMusicModeFlags(true, false, false, false, false);      
                
                imitoneVoiceInterpreter.gameOn = false;
                StopInteractiveMusic();
                //RecoverInteractiveMusicModeFromInteractionType();
                if(debugAllowMusicModeLogs)
                {
                    Debug.Log("MUSIC: Music Mode Set to Silent (WWise: " + currentInteractionType + ")");
                }
            }
            else
            {
                if(debugAllowWarnings || debugAllowMusicModeLogs)
                {
                    Debug.LogWarning("MUSIC: Tried to set Music Mode to Silent, but it was already set to Silent");
                }
            }
            break;
            
            case MusicMode.InteractiveTutorial:
            currentMusicMode = mode;
            if(!modeTutorialFlag)
            {
                SetMusicModeFlags(false, true, false, false, false);    
                if(debugAllowMusicModeLogs)
                {
                    Debug.Log("MUSIC: Music Mode Set to Tutorial");
                }

                StartInteractiveMusic(); 
                
                SetFundamentalModeLock(true, NoteName.C);
                
                RecoverInteractiveMusicModeFromInteractionType();
                
                SetMusicSilentLayerVolume(_silentVolumeHigh, 30f);

            }
            else
            {
                if(debugAllowWarnings || debugAllowMusicModeLogs)
                {
                    Debug.LogWarning("MUSIC: Tried to set Music Mode to Tutorial, but it was already set to Tutorial");
                }
            }
            break;
            
            case MusicMode.Freeplay:
            currentMusicMode = mode;
            if(!modeFreeplayFlag)
            {
                SetMusicModeFlags(false, false, true, false, false);    
                if(debugAllowMusicModeLogs)
                {
                    Debug.Log("MUSIC: Music Mode Set to Freeplay");
                }

                SetFundamentalModeLock(false);
                StartInteractiveMusic();
                imitoneVoiceInterpreter.gameOn = true;
                SetMusicSilentLayerVolume(_silentVolumeHigh, 40f);  

                RecoverInteractiveMusicModeFromInteractionType();
                SyncMonitoringAttenuationFromInteractionType();
            }
            else
            {
                if(debugAllowWarnings || debugAllowMusicModeLogs)
                {
                    Debug.LogWarning("MUSIC: Tried to set Music Mode to Freeplay, but it was already set to Freeplay");
                }
            }
            break;
            
            case MusicMode.FrozenFreeplay:
            //TODO: likely we don't need this mode anymore, and this can be a bespoke implementation.
            currentMusicMode = mode;
            if(!modeFrozenFreeplayFlag)
            {
                SetMusicModeFlags(false, false, false, true, false);    
                if(debugAllowMusicModeLogs)
                {
                    Debug.Log("MUSIC: Music Mode Set to FrozenFreeplay");
                }

                SetFundamentalModeLock(true, NoteName.C);
                imitoneVoiceInterpreter.gameOn = false;
                
                RecoverInteractiveMusicModeFromInteractionType();
            }
            else
            {
                if(debugAllowWarnings || debugAllowMusicModeLogs)
                {
                    Debug.LogWarning("MUSIC: Tried to set Music Mode to FrozenFreeplay, but it was already set to FrozenFreeplay");
                }
            }
            break;
            case MusicMode.Environment:
            currentMusicMode = mode;
            if(!modeEnvironmentFlag)
            {
                SetMusicModeFlags(false, false, false, false, true);    
                if(debugAllowMusicModeLogs)
                {
                    Debug.Log("MUSIC: Music Mode Set to Environment (MusicEnvironmentMode State + ambient)");
                }

                EnterMusicEnvironmentAudio();
                //SetBreathworkCycle(true);
            }
            else
            {
                if(debugAllowWarnings || debugAllowMusicModeLogs)
                {
                    Debug.LogWarning("MUSIC: Tried to set Music Mode to Environment, but it was already set to Environment");
                }
            }
            break;
            
            default:
            currentMusicMode = mode;
                if(debugAllowWarnings || debugAllowMusicModeLogs)
                {
                    Debug.LogWarning("MUSIC: Invalid Music Mode: " + mode);
                }
            break;
        }
    }

    private void RecoverInteractiveMusicModeFromInteractionType()
    {
        RunWithToningRestoredAfterInteractiveSwitch(() =>
        {
            if(currentInteractionType == InteractionType.SoundWorld)
            {
                AkSoundEngine.SetSwitch("InteractiveMusicMode_Switch", "InteractiveMusicSystem", gameObject);
                if(debugAllowSoundscapeLogs)
                {
                    Debug.Log("MUSIC: Interactive Music Mode Recovered to InteractiveMusicSystem because Interaction Type is SoundWorld");
                }
            }
            else if(currentInteractionType == InteractionType.MusicLoop)
            {
                AkSoundEngine.SetSwitch("InteractiveMusicMode_Switch", "MusicLoops", gameObject);
                if(debugAllowSoundscapeLogs)
                {
                    Debug.Log("MUSIC: Interactive Music Mode Recovered to MusicLoops because Interaction Type is MusicLoop");
                }
            }
        });
    }

    //A method for easily setting the flags, to replace the code in each of the case statements above.
    private void SetMusicModeFlags(bool silent, bool tutorial, bool freeplay, bool frozenFreeplay, bool environment, bool musicLoopSilent = false)
    {
        modeSilentFlag = silent;
        modeTutorialFlag = tutorial;
        modeFreeplayFlag = freeplay;
        modeFrozenFreeplayFlag = frozenFreeplay;
        modeEnvironmentFlag = environment;
        modeMusicLoopSilentFlag = musicLoopSilent; //this is a temporary flag for a musicloop version of silent mode, before we merge the two silent modes.

        if(modeTutorialFlag || modeFreeplayFlag || modeFrozenFreeplayFlag)
        {
            MusicBinauralBeats.instance.SetVolume(70.0f);
        }
        else if(modeMusicLoopSilentFlag)
        {
            if(debugAllowMusicModeLogs)
            {
                Debug.Log("MUSIC: Setting Music Binaural Beats volume to 50.0f for MusicLoopSilent mode");
            }
            MusicBinauralBeats.instance.SetVolume(50.0f);
        }
        else
        {
            MusicBinauralBeats.instance.SetVolume(0f);
        }
    }
    
    public Action Action_SetSoundscape(string soundscape)
    {
        return () => SetSoundscape(soundscape);
    }   
    public void SetSoundscape(string soundscape)
    {
        // Check which type of soundscape this is
        bool isSoundWorld = soundWorlds.Contains(soundscape);
        bool isMusicLoop = musicLoops.ContainsKey(soundscape);
        
        if (!isSoundWorld && !isMusicLoop)
        {
            if(debugAllowWarnings || debugAllowSoundscapeLogs)
            {
                Debug.LogWarning("MUSIC: Unknown soundscape type requested: " + soundscape + " is neither soundWorld or musicLoop.");
            }
            return;
        }
        
        
        // Log the soundscape type for debugging
        if (isSoundWorld)
        {
            SetSoundWorld(soundscape);
        }
        else if (isMusicLoop)
        {
            SetMusicLoop(soundscape);
        }
        
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log($"MUSIC TEST: currentInteractionType after SetSoundscape: {currentInteractionType}");
        }
    }

    //public Action Action_SetSoundWorld(string soundWorld)
    //{
    //    return () => SetSoundWorld(soundWorld);
    //}

    public void SetSoundWorld(string soundWorld) //NOTE: this will currently break the MusicLoopSilent mode, which is a temporary mode. 
    {
        bool ToningV3WasAlreadyRestored = false;
        if(currentMusicMode != MusicMode.Environment)
        {
            SetSwitchRestoreToningV3("InteractiveMusicMode_Switch", "InteractiveMusicSystem");
            ToningV3WasAlreadyRestored = true;
        }
        else
        {
            if(debugAllowWarnings || debugAllowSoundscapeLogs)
            {
                Debug.LogWarning($"MUSIC: Changing SoundWorld to '{soundWorld}', but current mode is '{currentMusicMode}' (Environment) -- this change will not be audible.");
            }
        }
        if (!soundWorlds.Contains(soundWorld))
        {
            if (debugAllowWarnings || debugAllowSoundscapeLogs)
            {
                Debug.LogWarning($"MUSIC: SetSoundWorld called with invalid soundWorld '{soundWorld}'");
            }
            return;
        }
        OnInteractionTypeChanged(InteractionType.SoundWorld);
        if(!ToningV3WasAlreadyRestored)
        {
            SetSwitchRestoreToningV3("SoundWorldMode_Switch", soundWorld);
        }
        worldShuffler.SetCurrentSoundscape(soundWorld);
        SetSoundWorldFlag();
        
        // Clear content lock since SoundWorlds work with any fundamental
        SetFundamentalContentLock(null);
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC: Soundscape Set To: " + soundWorld + " (SoundWorld)");
        }
    }

    //public Action Action_SetMusicLoop(string musicLoop)
    //{
    //    return () => SetMusicLoop(musicLoop);
    //}
    public void SetMusicLoop(string musicLoop) //NOTE: this will currently break the MusicLoopSilent mode, which is a temporary mode. 
    {
        // Validate that this is a legitimate MusicLoop before proceeding
        if (!musicLoops.ContainsKey(musicLoop))
        {
            if(debugAllowWarnings || debugAllowSoundscapeLogs)
            {
                Debug.LogWarning($"MUSIC: MusicLoop '{musicLoop}' not found in musicLoops dictionary - aborting SetMusicLoop()");
            }
            return;
        }
        
        RunWithToningRestoredAfterInteractiveSwitch(() =>
        {
            if(currentMusicMode != MusicMode.Environment)
            {
                AkSoundEngine.SetSwitch("InteractiveMusicMode_Switch", "MusicLoops", gameObject);
            }
            else
            {
                if(debugAllowWarnings || debugAllowSoundscapeLogs)
                {
                    Debug.LogWarning($"MUSIC: Changing MusicLoop to '{musicLoop}', but current mode is '{currentMusicMode}' (Environment) -- this change will not be audible.");
                }
            }
            OnInteractionTypeChanged(InteractionType.MusicLoop);
            AkSoundEngine.SetSwitch("MusicLoops_Switch", musicLoop, gameObject);
        });
        worldShuffler.SetCurrentSoundscape(musicLoop);
        
        // Set content lock to the required fundamental for this MusicLoop
        NoteName requiredNote = GetMusicLoopFundamental(musicLoop);
        if (requiredNote == NoteName.None)
        {
            if(debugAllowWarnings || debugAllowFundamentalLockLogs || debugAllowSoundscapeLogs)
            {
                Debug.LogWarning($"MUSIC: GetMusicLoopFundamental() returned NoteName.None for '{musicLoop}' - clearing content lock to avoid stale lock");
            }
            // Clear content lock since we can't determine the required fundamental
            SetFundamentalContentLock(null);
        }
        else
        {
            SetFundamentalContentLock(requiredNote);
            if(debugAllowFundamentalLockLogs || debugAllowSoundscapeLogs)
            {
                Debug.Log($"MUSIC: Content lock set to {requiredNote} for MusicLoop '{musicLoop}'");
            }
        }
        
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC: Soundscape Set To: " + musicLoop + " (MusicLoop)");
        }
    }

    /// <summary>
    /// Gets the fundamental NoteName for a MusicLoop, or NoteName.None if not found
    /// </summary>
    public NoteName GetMusicLoopFundamental(string musicLoopName)
    {
        return musicLoops.TryGetValue(musicLoopName, out NoteName fundamental) ? fundamental : NoteName.None;
    }


    /// <summary>
    /// Checks if a soundscape name is a MusicLoop
    /// </summary>
    public bool IsMusicLoop(string soundscapeName)
    {
        return musicLoops.ContainsKey(soundscapeName);
    }

    
    // ====================================================================================================
    // FUNDAMENTAL CHANGE SYSTEM - Core Fundamental Change Methods
    // ====================================================================================================

    private void ResetFundamentalTimers()
    {
        var keys = new List<NoteName>(NoteTracker.Keys);

        foreach (var key in keys)
        {
            var currentValue = NoteTracker[key];
            NoteTracker[key] = (currentValue.ActivationTimer, currentValue.Active, currentValue.FirstFrameActive, 0.0f);
            if (debugAllowFundamentalLogicLogs)
            {
                Debug.Log("MUSIC 8: Key(" + key + ": ChangeFundamentalTimer reset");
            }
        }
        fundamentalTimeSinceLastTrigger = 0f;
    }

    /// <summary>
    /// Sets the fundamental note directly without checking locks.
    /// Used internally when we need to force a change (e.g., to match an active lock).
    /// Updates Wwise switches and binaural beats frequency based on the new fundamental.
    /// </summary>
    /// <param name="newFundamental">The NoteName to set as the fundamental. Must not be NoteName.None.</param>
    /// <remarks>
    /// Conversion point: NoteName enum is converted to string for Wwise API via NoteUtils.NoteToWwiseString().
    /// Conversion point: NoteName enum is converted to frequency (Hz) for binaural beats via NoteUtils.NoteToFrequencyA440().
    /// </remarks>
    public void SetFundamentalDirect(NoteName newFundamental)
    {
        // Validate that we're not setting fundamental to None
        if (newFundamental == NoteName.None)
        {
            if(debugAllowWarnings || debugAllowFundamentalChangeLogs)
            {
                Debug.LogWarning("MUSIC: Attempted to set fundamental to None - ignoring");
            }
            return;
        }

        if(debugAllowFundamentalChangeLogs)
        {
            Debug.Log("MUSIC 6: Fundamental Note Changing to " + NoteUtils.NoteToWwiseString(newFundamental));
        }
        
        director.ClearQueueOfType("fundamentalChange");
        fundamentalNoteName = newFundamental;

        SetSwitchRestoreToningV3("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", NoteUtils.NoteToWwiseString(fundamentalNoteName));
        if (MusicBinauralBeats.instance != null)
        {
            MusicBinauralBeats.instance.ChangeCenterFrequency(NoteUtils.NoteToFrequencyA440(fundamentalNoteName));
        }
        else
        {
            if(debugAllowWarnings || debugAllowFundamentalChangeLogs)
            {
                Debug.LogWarning("MUSIC: MusicBinauralBeats.instance is null - binaural beats not initialized yet.");
            }
        }

        ResetFundamentalTimers();
        directorStoredFundamental = newFundamental;
    }

    /// <summary>
    /// Creates an Action delegate that will change the fundamental note when invoked.
    /// Used for queuing fundamental changes in the Director system.
    /// </summary>
    /// <param name="scaleNoteKey">The NoteName to change the fundamental to.</param>
    /// <returns>An Action delegate that calls ChangeFundamental with the specified note.</returns>
    private Action Action_ChangeFundamental(NoteName scaleNoteKey)
    {
        return () => ChangeFundamental(scaleNoteKey);
    }
   
    /// <summary>
    /// Changes the fundamental note, respecting any active locks.
    /// If the fundamental is locked, logs a warning and does not change it.
    /// If unlocked, calls SetFundamentalDirect to perform the change.
    /// </summary>
    /// <param name="newFundamental">The NoteName to change the fundamental to. Must not be NoteName.None.</param>
    public void ChangeFundamental(NoteName newFundamental)
    {
        if(!IsFundamentalLocked())
        {
            SetFundamentalDirect(newFundamental);
        }
        else
        {
            NoteName? lockedNote = GetLockedFundamental();
            string lockInfo = lockedNote.HasValue ? $" (locked to {lockedNote.Value})" : " (unknown lock)";
            if(debugAllowWarnings || debugAllowFundamentalChangeLogs || debugAllowFundamentalLockLogs)
            {
                Debug.LogWarning("MUSIC: Tried to change the fundamental, but it was locked" + lockInfo + ". This shouldn't happen, and probably indicates a logic flaw in the code.");
            }
        }
    }

    // ====================================================================================================
    // FUNDAMENTAL LOCKING SYSTEM - Lock Setter Methods (Priority: Debug > Content > Mode)
    // ====================================================================================================
    
    /// <summary>
    /// Sets or updates the debug fundamental lock (highest priority - development mode only).
    /// This lock overrides all other locks and forces the fundamental to a specific note.
    /// Pass null to clear the debug lock.
    /// </summary>
    /// <param name="note">The NoteName to lock to, or null to unlock. Must not be NoteName.None.</param>
    public void SetFundamentalDebugLock(NoteName? note = null)
    {
        bool currentlyLocked = fundamentalDebugLock.HasValue;
        
        if (!note.HasValue)
        {
            // Clear the debug lock
            if (currentlyLocked)
            {
                fundamentalDebugLock = null;
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC FUNDAMENTAL-DEBUG-LOCK: Debug lock cleared");
                }
                
                // Resolve fundamental: apply lower priority locks or queue based on tracking
                ResolveFundamentalOnUnlock();
            }
            else
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log("MUSIC FUNDAMENTAL-DEBUG-LOCK: Tried to clear debug lock, but it was already cleared");
                }
            }
            return;
        }
        
        NoteName lockNote = note.Value;
        
        // Safety check: don't allow locking to None
        if (lockNote == NoteName.None)
        {
            if(debugAllowWarnings || debugAllowFundamentalLockLogs)
            {
                Debug.LogWarning("MUSIC FUNDAMENTAL-DEBUG-LOCK: Cannot set debug lock to NoteName.None - ignoring request");
            }
            return;
        }
        
        // Optimization: if debug lock is already set to the requested note, skip work
        if (currentlyLocked && fundamentalDebugLock.Value == lockNote)
        {
            if(debugAllowFundamentalLockLogs)
            {
                Debug.Log($"MUSIC FUNDAMENTAL-DEBUG-LOCK: Debug lock already set to {lockNote} - skipping update");
            }
            return;
        }
        
        // Set debug lock (highest priority)
        fundamentalDebugLock = lockNote;
        
        // Use SetFundamentalDirect instead of ChangeFundamental because we're the lock system
        // requesting the change - we need to bypass the lock check
        // Debug lock has highest priority, so it always takes effect
        SetFundamentalDirect(lockNote);
        
        if(debugAllowFundamentalLockLogs)
        {
            Debug.Log($"MUSIC FUNDAMENTAL-DEBUG-LOCK: Debug lock set and locked fundamental to {lockNote} (DEVELOPMENT ONLY - highest priority)");
        }
    }

    /// <summary>
    /// Sets or clears the content-based fundamental lock (for MusicLoop compatibility).
    /// This lock ensures the fundamental matches the required note for the current MusicLoop.
    /// Priority: Lower than debug lock, higher than mode lock.
    /// </summary>
    /// <param name="note">The NoteName to lock to, or null to unlock. Must not be NoteName.None.</param>
    public void SetFundamentalContentLock(NoteName? note)
    {
        bool currentlyLocked = fundamentalContentLock.HasValue;

        if (note.HasValue)
        {
            // Lock: Set content lock and change fundamental if needed
            NoteName lockNote = note.Value;
            
            // Safety check: don't allow locking to None
            if (lockNote == NoteName.None)
            {
                if(debugAllowWarnings || debugAllowFundamentalLockLogs)
                {
                    Debug.LogWarning("MUSIC FUNDAMENTAL-CONTENT-LOCK: Cannot set content lock to NoteName.None - ignoring request");
                }
                return;
            }

            // Always set or update the lock and change fundamental, even if already locked
            NoteName? oldLockValue = currentlyLocked ? fundamentalContentLock : null;
            bool wasLockedTo = currentlyLocked && oldLockValue.Value == lockNote;
            fundamentalContentLock = lockNote;

            // Only change fundamental if content lock is the active lock (not overridden by debug lock)
            // If there's a debug lock, it takes priority and we shouldn't change the fundamental
            NoteName? activeLock = GetLockedFundamental();
            bool contentLockIsActive = !fundamentalDebugLock.HasValue;
            
            if (contentLockIsActive)
            {
                // Use SetFundamentalDirect instead of ChangeFundamental because we're the lock system
                // requesting the change - we need to bypass the lock check
                SetFundamentalDirect(lockNote);
            }
            else if (activeLock.HasValue)
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log($"MUSIC FUNDAMENTAL-CONTENT-LOCK: Content lock set to {lockNote}, but higher priority lock active ({activeLock.Value}) - fundamental unchanged");
                }
            }

            if (currentlyLocked && !wasLockedTo)
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log($"MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Lock changed from {oldLockValue.Value} to {lockNote}");
                }
            }
            else if (!currentlyLocked)
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log($"MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Locked to {lockNote}");
                }
            }
            else
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log($"MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content relocked to {lockNote}");
                }
            }
        }
        else if (!note.HasValue && currentlyLocked)
        {
            // Unlock: Clear the lock
            fundamentalContentLock = null;
            if(debugAllowFundamentalLockLogs)
            {
                Debug.Log("MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Unlocked");
            }
            
            // Resolve fundamental: apply lower priority locks or queue based on tracking
            ResolveFundamentalOnUnlock();
        }
        else if (!note.HasValue && !currentlyLocked)
        {
            if(debugAllowFundamentalLockLogs)
            {
                Debug.Log("MUSIC FUNDAMENTAL-CONTENT-LOCK: Tried to unlock fundamental content, but it was already unlocked");
            }
        }
    }

    
    /// <summary>
    /// Sets or clears the mode-based fundamental lock (for Tutorial, FrozenFreeplay modes).
    /// This lock ensures the fundamental stays at a specific note during certain game modes.
    /// Priority: Lowest priority lock (overridden by content and debug locks).
    /// </summary>
    /// <param name="doLock">If true, locks to the specified note. If false, unlocks.</param>
    /// <param name="note">The NoteName to lock to (defaults to C). Must not be NoteName.None.</param>
    public void SetFundamentalModeLock(bool doLock, NoteName note = NoteName.C)
    {
        bool currentlyLocked = fundamentalModeLock.HasValue;

        if (doLock)
        {
            // Always set or update the lock and change fundamental, even if already locked
            NoteName? oldLockValue = currentlyLocked ? fundamentalModeLock : null;
            bool wasLockedTo = currentlyLocked && oldLockValue.Value == note;
            fundamentalModeLock = note;

            // Only change fundamental if mode lock is the active lock (not overridden by higher priority locks)
            // If there's a debug or content lock, they take priority and we shouldn't change the fundamental
            NoteName? activeLock = GetLockedFundamental();
            bool modeLockIsActive = !fundamentalDebugLock.HasValue && !fundamentalContentLock.HasValue;
            
            if (modeLockIsActive)
            {
                // Use SetFundamentalDirect instead of ChangeFundamental because we're the lock system
                // requesting the change - we need to bypass the lock check
                SetFundamentalDirect(note);
            }
            else if (activeLock.HasValue)
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log($"MUSIC FUNDAMENTAL-MODE-LOCK: Mode lock set to {note}, but higher priority lock active ({activeLock.Value}) - fundamental unchanged");
                }
            }

            if (currentlyLocked && !wasLockedTo)
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log($"MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Lock changed from {oldLockValue.Value} to {note}");
                }
            }
            else if (!currentlyLocked)
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log($"MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Locked to {note}");
                }
            }
            else
            {
                if(debugAllowFundamentalLockLogs)
                {
                    Debug.Log($"MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode relocked to {note}");
                }
            }
        }
        else if (!doLock && currentlyLocked)
        {
            // Unlock: Clear the lock
            fundamentalModeLock = null;
            if(debugAllowFundamentalLockLogs)
            {
                Debug.Log("MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Unlocked");
            }
            
            // Resolve fundamental: apply lower priority locks or queue based on tracking
            ResolveFundamentalOnUnlock();
        }
        else if (!doLock && !currentlyLocked)
        {
            // Already unlocked
            if(debugAllowFundamentalLockLogs)
            {
                Debug.Log("MUSIC FUNDAMENTAL-MODE-LOCK: Tried to unlock fundamental mode, but it was already unlocked");
            }
        }
    }

     
    // ====================================================================================================
    // FUNDAMENTAL LOCKING SYSTEM - Unified Lock Checks
    // ====================================================================================================
    
    /// <summary>
    /// Returns true if any fundamental lock is active
    /// </summary>
    private bool IsFundamentalLocked()
    {
        return GetLockedFundamental().HasValue;
    }

    /// <summary>
    /// Returns the locked fundamental note with priority: DebugLock > ContentLock > ModeLock
    /// Returns null if no lock is active
    /// </summary>
    private NoteName? GetLockedFundamental()
    {
        // Priority 1: Debug Lock (highest - development mode)
        if (fundamentalDebugLock.HasValue)
            return fundamentalDebugLock.Value;
        
        // Priority 2: Content Lock (MusicLoop compatibility requirement)
        if (fundamentalContentLock.HasValue)
            return fundamentalContentLock.Value;
        
        // Priority 3: Mode Lock (gameplay constraint)
        if (fundamentalModeLock.HasValue)
            return fundamentalModeLock.Value;
        
        return null; // Not locked
    }


    /// <summary>
    /// Resolves the fundamental when a lock is released.
    /// If lower priority locks are still active, sets the fundamental to the active lock's note.
    /// If no locks are active, queues a fundamental change based on tracking data.
    /// </summary>
    private void ResolveFundamentalOnUnlock()
    {
        NoteName? activeLock = GetLockedFundamental();
        
        if (activeLock.HasValue)
        {
            // A lower priority lock is active, set fundamental to it
            // Use SetFundamentalDirect to bypass lock check since we're setting to match the active lock
            SetFundamentalDirect(activeLock.Value);
            if(debugAllowFundamentalLockLogs)
            {
                Debug.Log($"MUSIC FUNDAMENTAL MODE UNLOCK: Lower priority lock active ({activeLock.Value}) - fundamental set accordingly");
            }
        }
        else
        {
            // No locks are active, queue fundamental change based on tracking data
            // Find the note with the highest ChangeFundamentalTimer
            NoteName? newFundamental = null;
            float highestFundamentalTimer = 0;
            
            foreach (var trackedNote in NoteTracker)
            {
                if (trackedNote.Value.ChangeFundamentalTimer > highestFundamentalTimer)
                {
                    highestFundamentalTimer = trackedNote.Value.ChangeFundamentalTimer;
                    newFundamental = trackedNote.Key;
                }
            }
            
            // Change fundamental if above threshold and valid note found
            if (highestFundamentalTimer >= _queueFundamentalChangeThreshold && newFundamental.HasValue)
            {
                // If timer is above immediate threshold, trigger change immediately
                // Otherwise, queue it for later execution
                if (highestFundamentalTimer >= _initiateImminentFundamentalChangeThreshold)
                {
                    // Timer is high enough for immediate change - trigger it now
                    ChangeFundamental(newFundamental.Value);
                    director.ActivateQueue(5.0f);
                    if(debugAllowFundamentalLockLogs || debugAllowFundamentalChangeLogs)
                    {
                        Debug.Log("MUSIC FUNDAMENTAL MODE UNLOCK: Fundamental Changed Immediately on Unlock (high threshold): " + NoteUtils.NoteToWwiseString(newFundamental.Value));
                    }
                }
                else
                {
                    // Timer is above queue threshold but below immediate threshold - queue it
                    director.ClearQueueOfType("fundamentalChange");
                    director.AddActionToQueue(Action_ChangeFundamental(newFundamental.Value), "fundamentalChange", true, false, 120f, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType);
                    directorStoredFundamental = newFundamental.Value;
                    if(debugAllowFundamentalLockLogs || debugAllowFundamentalChangeLogs)
                    {
                        Debug.Log("MUSIC FUNDAMENNTAL MODE UNLOCK: New Fundamental Queued on Unlock: " + NoteUtils.NoteToWwiseString(newFundamental.Value));
                    }
                }
            }
            else if (highestFundamentalTimer >= _queueFundamentalChangeThreshold && !newFundamental.HasValue)
            {
                if(debugAllowWarnings || debugAllowFundamentalLockLogs || debugAllowFundamentalChangeLogs)
                {
                    Debug.LogWarning("MUSIC FUNDAMENTAL MODE UNLOCK: Threshold met but no valid fundamental found in NoteTracker - skipping queue");
                }
            }
        }
    }

    //=================================================================================
    //BASIC TONING METHODS
    //=================================================================================



    private void BasicToningUpdate()
    {       
        if(localToneOn && !previousLocalToneOn)
        {
            PostTheToningEvents();

        } else if (!localToneOn && previousLocalToneOn)
        {
            StopWwiseToning();
        }

        previousLocalToneOn = localToneOn;
    }

    private void BassSynthUpdate()
    {
        // Update cooldown timer
        bassSynthCooldownTimer += Time.deltaTime;
        
        // Check if cooldown has expired and execute pending actions
        // Pitch change executes first (so start can use the updated pitch)
        if (bassSynthCooldownTimer >= BASS_SYNTH_COOLDOWN)
        {
            if (pendingBassSynthPitch != null)
            {
                // Execute pending pitch change first
                NoteName pitchToChange = pendingBassSynthPitch.Value;
                pendingBassSynthPitch = null;
                if(debugAllowBassSynthLogs)
                {
                    Debug.Log("Music: BassSynth pitch change executing after cooldown (to " + NoteUtils.NoteToWwiseString(pitchToChange) + ")");
                }
                UpdateBassSynthPitchSwitch(pitchToChange);
                // Note: UpdateBassSynthPitchSwitch resets cooldown timer, but we still want to check for pending start
                // If start is also pending, it should execute immediately after pitch change (no additional cooldown wait)
            }
            
            // Check for pending start separately (after pitch change if it executed)
            // This allows start to execute immediately after pitch change in the same frame
            if (pendingBassSynthStart)
            {
                // Execute pending start (after pitch change if it was pending)
                pendingBassSynthStart = false;
                if(debugAllowBassSynthLogs)
                {
                    Debug.Log("Music: BassSynth Start executing after cooldown");
                }
                PostTheBassSynthEvent();
            }
        }
        
        // BassSynth control based on toneActiveConfident
        if(localBassSynthToneOn && !previousLocalBassSynthToneOn)
        {
            if(debugAllowBassSynthLogs)
            {
                Debug.Log("Music: BassSynth Start requested (toneActiveConfident became true)");
            }
            
            // Check cooldown before starting
            if (bassSynthCooldownTimer >= BASS_SYNTH_COOLDOWN)
            {
                // Cooldown expired, start immediately
                PostTheBassSynthEvent();
            }
            else
            {
                // In cooldown, queue start for later (pitch change will execute first if also pending)
                pendingBassSynthStart = true;
                if(debugAllowBassSynthLogs)
                {
                    Debug.Log("Music: BassSynth Start queued (cooldown active, " + (BASS_SYNTH_COOLDOWN - bassSynthCooldownTimer).ToString("F2") + "s remaining)");
                }
            }
        }
        else if (!localBassSynthToneOn && previousLocalBassSynthToneOn)
        {
            if(debugAllowBassSynthLogs)
            {
                Debug.Log("Music: BassSynth Stop requested (toneActiveConfident became false)");
            }
            
            // Stop bypasses cooldown - execute immediately and clear pending actions
            pendingBassSynthStart = false;
            pendingBassSynthPitch = null;
            
            // Stop BassSynth when toneActiveConfident becomes false
            if (bassSynthPlaying)
            {
                AkSoundEngine.PostEvent("Stop_BassSynth", gameObject);
                string previousPitch = currentBassSynthPitch.HasValue ? NoteUtils.NoteToWwiseString(currentBassSynthPitch.Value) : "None";
                bassSynthPlaying = false;
                currentBassSynthPitch = null;
                if(debugAllowBassSynthLogs)
                {
                    Debug.Log("Music: BassSynth Stop event posted (previous pitch: " + previousPitch + ")");
                }
            }
            else
            {
                if(debugAllowBassSynthLogs)
                {
                    Debug.Log("Music: BassSynth Stop requested but BassSynth was not playing");
                }
            }
        }

        // Update BassSynth pitch while toneActiveConfident is active
        if (localBassSynthToneOn)
        {
            NoteName targetPitch = NoteName.None;
            
            if (currentInteractionType == InteractionType.MusicLoop)
            {
                // In MusicLoop mode, use musicNoteActivated (skip if None)
                if (musicNoteActivated != NoteName.None)
                {
                    targetPitch = musicNoteActivated;
                }
            }
            else if (currentInteractionType == InteractionType.SoundWorld)
            {
                // In SoundWorld mode, use fundamentalNoteName (skip if None)
                if (fundamentalNoteName != NoteName.None)
                {
                    targetPitch = fundamentalNoteName;
                }
            }

            // If we have a valid target pitch, update BassSynth
            if (targetPitch != NoteName.None)
            {
                // Check if BassSynth needs to be started (if it wasn't started in PostTheBassSynthEvent due to missing pitch)
                if (!bassSynthPlaying)
                {
                    // Start BassSynth with the current pitch
                    // Check cooldown before starting
                    if (bassSynthCooldownTimer >= BASS_SYNTH_COOLDOWN)
                    {
                        // Cooldown expired, start immediately
                        // Set pitch switch BEFORE playing (ensures correct pitch on start)
                        AkSoundEngine.SetSwitch("BassSynth_PitchSwitch", NoteUtils.NoteToWwiseString(targetPitch), gameObject);
                        currentBassSynthPitch = targetPitch;
                        AkSoundEngine.PostEvent("Play_BassSynth", gameObject);
                        bassSynthPlaying = true;
                        bassSynthCooldownTimer = 0f; // Reset cooldown after starting
                        if(debugAllowBassSynthLogs)
                        {
                            Debug.Log("Music: BassSynth Start event posted (delayed start - pitch: " + NoteUtils.NoteToWwiseString(targetPitch) + ", InteractionType: " + currentInteractionType + ")");
                        }
                    }
                    else
                    {
                        // In cooldown, queue both pitch switch and start (pitch change will execute first)
                        // Only log if we're not already queued (prevents spam every frame)
                       
                        if (!pendingBassSynthStart)
                        {
                            pendingBassSynthStart = true;
                            pendingBassSynthPitch = targetPitch;
                            if(debugAllowBassSynthLogs)
                            {
                                Debug.Log("Music: BassSynth Start queued (delayed start, cooldown active, " + (BASS_SYNTH_COOLDOWN - bassSynthCooldownTimer).ToString("F2") + "s remaining, pitch " + NoteUtils.NoteToWwiseString(targetPitch) + " will be set when cooldown expires)");
                            }
                        }
                        else
                        {
                            // Already queued, just update pitch if it changed
                            pendingBassSynthPitch = targetPitch;
                        }
                    }
                }
                else
                {
                    // BassSynth is already playing, update pitch if it changed
                    // Check if pitch actually changed before checking cooldown
                    if (!currentBassSynthPitch.HasValue || currentBassSynthPitch.Value != targetPitch)
                    {
                        // Pitch changed - check cooldown before updating
                        // Note: UpdateBassSynthPitchSwitch() will also check cooldown internally, but we check here to queue it properly
                        if (bassSynthCooldownTimer >= BASS_SYNTH_COOLDOWN)
                        {
                            // Cooldown expired, change pitch immediately
                            // UpdateBassSynthPitchSwitch() will handle the stop-change-play sequence and reset cooldown
                            UpdateBassSynthPitchSwitch(targetPitch);
                        }
                        else
                        {
                            // In cooldown, queue pitch change for later (will execute before pending start if both are pending)
                            pendingBassSynthPitch = targetPitch;
                            if(debugAllowBassSynthLogs)
                            {
                                Debug.Log("Music: BassSynth pitch change queued (from " + (currentBassSynthPitch.HasValue ? NoteUtils.NoteToWwiseString(currentBassSynthPitch.Value) : "None") + " to " + NoteUtils.NoteToWwiseString(targetPitch) + ", cooldown active, " + (BASS_SYNTH_COOLDOWN - bassSynthCooldownTimer).ToString("F2") + "s remaining)");
                            }
                        }
                    }
                }
            }
            // Edge case: If targetPitch is None, we skip pitch update (use last valid pitch)
            // This handles cases where musicNoteActivated or fundamentalNoteName is None
        }

        previousLocalBassSynthToneOn = localBassSynthToneOn;
    }

    public void StopWwiseToning()
    {
        if (ToningV3FundamentalPlaying)
        {
            AkSoundEngine.PostEvent("Stop_Toning_v3_FundamentalOnly", gameObject);
            ToningV3FundamentalPlaying = false;
        }
        if (ToningV3HarmonyPlaying)
        {
            AkSoundEngine.PostEvent("Stop_Toning_v3_HarmonyOnly", gameObject);
            ToningV3HarmonyPlaying = false;
        }
        if (debugAllowBasicToningLogs)
        {
            Debug.Log("MUSIC: Post Toning Events STOP to Wwise");
        }

        // Note: BassSynth is now controlled separately by toneActiveConfident, not stopped here
        // BassSynth will stop automatically when toneActiveConfident becomes false
    }

    /// <summary>Reposts Play for layers that were active before a SetSwitch on interactive-music switch groups (Wwise can drop those voices when switches change).</summary>
    private void RestartToningV3LayersAfterSwitchIfNeeded(bool wasFundamentalPlaying, bool wasHarmonyPlaying)
    {
        if (wasFundamentalPlaying)
        {
            AkSoundEngine.PostEvent("Play_Toning_v3_FundamentalOnly", gameObject);
            ToningV3FundamentalPlaying = true;
        }
        if (wasHarmonyPlaying)
        {
            AkSoundEngine.PostEvent("Play_Toning_v3_HarmonyOnly", gameObject);
            ToningV3HarmonyPlaying = true;
        }
    }

    private void SetSwitchRestoreToningV3(string switchGroup, string switchValue)
    {
        bool wasF = ToningV3FundamentalPlaying;
        bool wasH = ToningV3HarmonyPlaying;
        AkSoundEngine.SetSwitch(switchGroup, switchValue, gameObject);
        RestartToningV3LayersAfterSwitchIfNeeded(wasF, wasH);
    }

    /// <summary>Use when applying one or more SetSwitch calls on SoundWorldMode / InteractiveMusicMode / MusicLoops / 12-pitch groups; toning is restarted once afterward if it was playing.</summary>
    public void RunWithToningRestoredAfterInteractiveSwitch(Action apply)
    {
        bool wasF = ToningV3FundamentalPlaying;
        bool wasH = ToningV3HarmonyPlaying;
        apply?.Invoke();
        RestartToningV3LayersAfterSwitchIfNeeded(wasF, wasH);
    }

    /// <summary>
    /// Processes raw Imitone voice input and converts it to NoteName-based data for the music system.
    /// This is the primary conversion point from external float-based note input (Imitone) to internal NoteName representation.
    /// </summary>
    /// <remarks>
    /// Conversion points:
    /// - Input: Imitone provides note_st as float (0-11 range after modulo 12)
    /// - Internal: Uses NoteName enum for all note tracking and comparisons
    /// - Math operations: Temporarily converts NoteName to int (0-11) for distance calculations
    /// - Output: Sets musicNoteActivated as NoteName enum
    /// </remarks>
    private void InterpretImitoneUpdate()
    {
         // ========================================================
        // CONVERTS RAW IMITONE INTO DATA USABLE BY OUR MUSIC SYSTEM
        // ========================================================
        
        // Modulo 12 on the interpreted note to get the position within an octave
        musicNoteInputRaw = imitoneVoiceInterpreter.note_st % 12;

        // Safety check: if fundamentalNoteName is None, use raw input without harmonic adjustments
        if (fundamentalNoteName == NoteName.None)
        {
            if(debugAllowWarnings || debugAllowImitoneUpdateLogs)
            {
                Debug.LogWarning("MUSIC: fundamentalNoteName is None in InterpretImitoneUpdate - using raw input without harmonic adjustments");
            }
            musicNoteInput = musicNoteInputRaw;
            return;
        }

        // Conversion point: NoteName enum -> int for math operations (distance calculations)
        // This is a temporary conversion contained within this method for arithmetic only
        int fundamentalNoteInt = NoteUtils.NoteToInt(fundamentalNoteName);

        // IN CASE OF DISHARMONIC RELATIONSHIP, REPLACE WITH HARMONIC RELATIONSHIP
        if (Mathf.Abs(musicNoteInputRaw - fundamentalNoteInt) < _constWiggleRoomUnison) //CLOSE TO UNISON
        {
            musicNoteInput = fundamentalNoteInt;
        }
        else if (Mathf.Abs(musicNoteInputRaw - fundamentalNoteInt) > (12.0f - _constWiggleRoomUnison)) //CLOSE TO OCTAVE
        {
            musicNoteInput = fundamentalNoteInt;
        }
        else if (Mathf.Abs(musicNoteInputRaw - fundamentalNoteInt + 6) < _constWiggleRoomPerfect) //CLOSE TO TRITONE
        {
            musicNoteInput = fundamentalNoteInt + 5;
        }
        else
        {
            musicNoteInput = musicNoteInputRaw;
        }

        // Determine threshold for active note detection based on whether the tone is actively interpreted as being sung/spoken
        // MORE CONFIDENT TONING MAKES THE SYSTEM SLOWER TO RESPOND TO TONE CHANGES
        float noteTrackerThreshold;
        if (imitoneVoiceInterpreter.toneActiveVeryConfident)
        {
            noteTrackerThreshold = imitoneVoiceInterpreter._activeThreshold3 * 2.0f; //1.5f
        }   
        else if (imitoneVoiceInterpreter.toneActiveConfident)
        {
            noteTrackerThreshold = imitoneVoiceInterpreter._activeThreshold3; // 0.75f
        }
        else if (imitoneVoiceInterpreter.toneActive)
        {
            noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold2; //0.2f
        }
        else
        {
            noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold1 / 4;
        }
        // Temporary storage for updates to notes and their activations
        var updates = new Dictionary<NoteName, (float, bool, bool, float)>();
        var activations = new Dictionary<NoteName, bool>();
        var fundamentalChanges = new Dictionary<NoteName, bool>();

        // Process each note only if the imitone system is active
        if (imitoneVoiceInterpreter.imitoneActive)
        {
            foreach (var scaleNote in NoteTracker)
            {
                float thisActivationTimer = scaleNote.Value.ActivationTimer;
                //float newChangeFundamentalTimer = scaleNote.Value.ChangeFundamentalTimer;
                bool isActive = scaleNote.Value.Active;
                bool isHighestActivationTimer = false;
                bool firstFrameActive = false;
                bool anyNoteActive = false;

                foreach (var note in NoteTracker) //REEF: THIS IS NEW, WE NEED TO TEST.
                {
                    if (note.Value.Active)
                    {
                        anyNoteActive = true;
                        break;
                    }
                }

                // FIRST, WE ARE GOING TO WORK REALLY HARD TO MAKE SURE WE ARE ACTIVELY TRACKING THE NOTE
                // THE MOMENT THE MUSIC SYSTEM DETECTS IT... EVEN THOUGH THE CURRENT SYSTEM DOESN'T ACTUALLY
                // CARE WHAT THE NOTE IS EXCEPT FOR AS IT PERTAINS TO CHANGING THE FUNDAMENTAL.

                // Conversion point: float -> NoteName for comparison with NoteTracker keys (NoteName enum)
                // musicNoteInput is a float (0-11 range) from harmonic adjustment calculations
                // Converted to NoteName enum to match the NoteTracker dictionary key type
                NoteName musicNoteInputNote = NoteUtils.FloatToNoteName(musicNoteInput);
                if (musicNoteInputNote == NoteName.None)
                {
                    if(debugAllowWarnings || debugAllowImitoneUpdateLogs)
                    {
                        Debug.LogWarning($"MUSIC: FloatToNoteName() returned NoteName.None for musicNoteInput={musicNoteInput} - note detection may be incorrect");
                    }
                }
                if (musicNoteInputNote == scaleNote.Key)
                {
                    if(debugAllowImitoneUpdateLogs && (thisActivationTimer == 0 || (Time.frameCount % 30 == 0)))
                    {
                        //musicNoteActivated = scaleNote.Key; 
                        //Debug.Log("MUSIC 1: [COMPARE TONES] Key(" + scaleNote.Key + ") from musicNoteInputRaw (" + musicNoteInputRaw + ") ~~~~~ isActive(" + isActive + ") ActivationTimer(" + thisActivationTimer + ") isHighestActivationTimer (" + isHighestActivationTimer + ")");
                    }
                    thisActivationTimer += Time.deltaTime; // Increment active timer if current note input matches the tracker note

                    // FIRST: CHECK IF THIS IS THE HIGHEST ACTIVATION TIMER YET
                    if (thisActivationTimer >= highestActivationTimer && thisActivationTimer != 0.0f)
                    {
                        if(debugAllowImitoneUpdateLogs)
                        {
                            //Debug.Log("MUSIC 2: [ACTIVATION TIMER FOR " + ConvertIntToNote(note.Key) + "] " + thisActivationTimer + " >= " + highestActivationTimer + " && " + thisActivationTimer + " != 0.0f");
                        }
                        highestActivationTimer = thisActivationTimer;
                        isHighestActivationTimer = true;
                    }
                    
                    bool allowNewActivation = (anyNoteActive || isHighestActivationTimer); //either this one is highest, or we are already in an active state.
                    if (thisActivationTimer >= noteTrackerThreshold && allowNewActivation)
                    {
                        if (debugAllowImitoneUpdateLogs && nextNote != scaleNote.Key)
                        {
                            Debug.Log("MUSIC 3: nextNote changed to (" + scaleNote.Key + ") Activation Timer(" + thisActivationTimer + ") >= Threshold(" + noteTrackerThreshold + ")");
                        }
                        nextNote = scaleNote.Key;
                        if (imitoneVoiceInterpreter.toneActiveBiasTrue) //now we change the actual tone!
                        {
                            if(debugAllowImitoneUpdateLogs && !isActive)
                            {
                                Debug.Log("MUSIC 4: Voice Input Key (" + scaleNote.Key + ")!");
                            }
                            firstFrameActive = !isActive; //this will only be true on the first frame that the note is activated
                            isActive = true;
                            musicNoteActivated = scaleNote.Key;
                            activations[scaleNote.Key] = isActive;
                        }
                    }
                    updates[scaleNote.Key] = (thisActivationTimer, isActive, firstFrameActive, scaleNote.Value.ChangeFundamentalTimer);
                }
                else if (!imitoneVoiceInterpreter.toneActiveBiasTrue) 
                {
                    //When imitoneActive is true, but toneActiveBiasTrue is false (possible false positive state)
                    //Then for notes other than the note that imitone thinks we are toning,
                    //Reset the activation timer, active flat, and first frame active flat.

                    updates[scaleNote.Key] = (0, false, false, scaleNote.Value.ChangeFundamentalTimer);
                    musicNoteActivated = NoteName.None; 
                }
            }
            // Apply the accumulated updates to the NoteTracker
            foreach (var update in updates)
            {
                NoteTracker[update.Key] = update.Value;
            }

            // Deactivate other notes if a new note has become active
            if (activations.ContainsValue(true))
            {
                foreach (var scaleNote in activations)
                {
                    //When one note becomes active, deactivate others.
                    if (scaleNote.Key != nextNote && scaleNote.Value == true)
                    {
                        var currentValue = NoteTracker[scaleNote.Key];
                        NoteTracker[scaleNote.Key] = (0.0f, false, false, currentValue.ChangeFundamentalTimer);
                        if(debugAllowImitoneUpdateLogs)
                        {
                            Debug.Log("MUSIC 7: Key(" + scaleNote.Key + ": deactivated (and highestActivationTimer reset)");
                        }
                    }
                }
                highestActivationTimer = 0.0f;
            }
        }
        
        // REEF: WE NEED TO CHECK THIS
        // NOTE: For this to work clearly, in the below else if block, we have to reset the ActivationTimer, Active, FirstFrameActive, (but keep ChangeFundamentalTimer) for each note.
        else if (!imitoneVoiceInterpreter.toneActiveBiasTrue)
        {
            //RESET ALL TONE ACTIVE TIMERS
            //Optimization opportunity: add a flag here to just do this once, when the player stops toning.
            foreach (var key in NoteTracker.Keys.ToList())
            {
                var current = NoteTracker[key];
                NoteTracker[key] = (0f, false, false, current.ChangeFundamentalTimer);
            }
            musicNoteActivated = NoteName.None;
        }
    }

    public void PostTheToningEvents()
    {
        if (ToningV3FundamentalPlaying && ToningV3HarmonyPlaying)
        {
            return;
        }

        if(debugAllowBasicToningLogs)
        {
            Debug.Log("MUSIC: Post Toning Events to Wwise");
        }
        if (!ToningV3FundamentalPlaying)
        {
            AkSoundEngine.PostEvent("Play_Toning_v3_FundamentalOnly", gameObject);
            ToningV3FundamentalPlaying = true;
        }
        if (!ToningV3HarmonyPlaying)
        {
            AkSoundEngine.PostEvent("Play_Toning_v3_HarmonyOnly", gameObject);
            ToningV3HarmonyPlaying = true;
        }
    }

    public void PostTheBassSynthEvent()
    {
        // Early exit if BassSynth is already playing (most common case - prevents unnecessary pitch determination)
        if (bassSynthPlaying)
        {
            if(debugAllowWarnings || debugAllowBassSynthLogs)
            {
                Debug.LogWarning("Music: BassSynth Start requested but already playing - skipping duplicate Play_BassSynth event");
            }
            return;
        }

        // Start BassSynth with appropriate initial pitch based on interaction type
        NoteName initialPitch = NoteName.None;
        
        if (currentInteractionType == InteractionType.MusicLoop)
        {
            // In MusicLoop mode, use musicNoteActivated (if not None)
            if (musicNoteActivated != NoteName.None)
            {
                initialPitch = musicNoteActivated;
            }
            else
            {
                if(debugAllowBassSynthLogs)
                {
                    Debug.Log("Music: BassSynth Start requested but musicNoteActivated is None (MusicLoop mode) - will start when note is detected");
                }
                // Don't start BassSynth yet - delayed start logic will handle starting it when a valid note is detected
                return;
            }
        }
        else if (currentInteractionType == InteractionType.SoundWorld)
        {
            // In SoundWorld mode, use fundamentalNoteName
            if (fundamentalNoteName != NoteName.None)
            {
                initialPitch = fundamentalNoteName;
            }
            else
            {
                if(debugAllowWarnings || debugAllowBassSynthLogs)
                {
                    Debug.LogWarning("Music: BassSynth Start requested but fundamentalNoteName is None (SoundWorld mode) - will start when fundamental is set");
                }
                // Don't start BassSynth yet - delayed start logic will handle starting it when a valid fundamental is set
                return;
            }
        }
        else
        {
            // Defensive check: InteractionType should only be MusicLoop or SoundWorld
            if(debugAllowWarnings || debugAllowBassSynthLogs)
            {
                Debug.LogWarning("Music: BassSynth Start requested but unknown InteractionType: " + currentInteractionType + " - cannot determine pitch");
            }
            return;
        }

        // Validate that we have a valid pitch before proceeding
        if (initialPitch == NoteName.None)
        {
            if(debugAllowWarnings || debugAllowBassSynthLogs)
            {
                Debug.LogWarning("Music: BassSynth Start requested but initialPitch is None after determining pitch - cannot start");
            }
            return;
        }

        // Set initial pitch switch BEFORE playing (ensures correct pitch on start)
        AkSoundEngine.SetSwitch("BassSynth_PitchSwitch", NoteUtils.NoteToWwiseString(initialPitch), gameObject);
        currentBassSynthPitch = initialPitch;
        if(debugAllowBassSynthLogs)
        {
            Debug.Log("Music: BassSynth pitch switch set to " + NoteUtils.NoteToWwiseString(initialPitch) + " (before start)");
        }

        // Post Play_BassSynth event
        AkSoundEngine.PostEvent("Play_BassSynth", gameObject);
        bassSynthPlaying = true;
        bassSynthCooldownTimer = 0f; // Reset cooldown after starting
        if(debugAllowBassSynthLogs)
        {
            Debug.Log("Music: BassSynth Start event posted (pitch: " + NoteUtils.NoteToWwiseString(initialPitch) + ", InteractionType: " + currentInteractionType + ")");
        }
    }
    
    private void changeHarmony(NoteName harmonyNote)
    {
        // Wwise 12-pitch groups have no "none" switch; SetSwitch("…", "none") leaves the group invalid and triggers errors when silent loops resolve.
        if (harmonyNote == NoteName.None)
        {
            if(debugAllowWarnings || debugAllowHarmonyChangeLogs)
            {
                Debug.LogWarning("MUSIC: changeHarmony skipped — harmony is None (would be invalid Wwise switch 'none'); keeping previous harmony switch.");
            }
            return;
        }

        SetSwitchRestoreToningV3("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", NoteUtils.NoteToWwiseString(harmonyNote));
        if(debugAllowHarmonyChangeLogs)
        {
            Debug.Log("MUSIC: Harmony Note Set To: " + NoteUtils.NoteToWwiseString(harmonyNote));
        }
    }

    /// <summary>
    /// Updates the Wwise BassSynth_PitchSwitch to the new pitch.
    /// This method handles the stop-change-play sequence to ensure smooth pitch transitions.
    /// Called while toning is active to change pitch dynamically.
    /// </summary>
    /// <param name="newPitch">The NoteName to set the BassSynth pitch to. Must not be NoteName.None.</param>
    /// <remarks>
    /// Conversion point: NoteName enum is converted to string for Wwise API via NoteUtils.NoteToWwiseString().
    /// </remarks>
    private void UpdateBassSynthPitchSwitch(NoteName newPitch)
    {
        // Validate input - reject NoteName.None
        if (newPitch == NoteName.None)
        {
            if((debugAllowWarnings || debugAllowBassSynthLogs) && !bassSynthNonePitchWarningLogged)
            {
                Debug.LogWarning("Music: BassSynth pitch change requested but newPitch is None - ignoring (this warning will only appear once)");
                bassSynthNonePitchWarningLogged = true;
            }
            return;
        }
        
        // Reset warning flag if we successfully set a valid pitch (allows warning again if issue reoccurs)
        if (bassSynthNonePitchWarningLogged && newPitch != NoteName.None)
        {
            bassSynthNonePitchWarningLogged = false;
        }

        // Check if pitch has changed
        string currentPitchStr = currentBassSynthPitch.HasValue ? NoteUtils.NoteToWwiseString(currentBassSynthPitch.Value) : "None";
        string newPitchStr = NoteUtils.NoteToWwiseString(newPitch);
        
        if (currentBassSynthPitch.HasValue && currentBassSynthPitch.Value == newPitch)
        {
            // Pitch hasn't changed, no need to update
            return;
        }

        // Check cooldown before setting pitch switch (pitch switch changes are also limited by cooldown)
        if (bassSynthCooldownTimer < BASS_SYNTH_COOLDOWN)
        {
            // In cooldown - this shouldn't happen if called from pending execution, but handle it gracefully
            if(debugAllowWarnings || debugAllowBassSynthLogs)
            {
                Debug.LogWarning("Music: BassSynth pitch change requested during cooldown (this should be queued instead). Cooldown remaining: " + (BASS_SYNTH_COOLDOWN - bassSynthCooldownTimer).ToString("F2") + "s");
            }
            return;
        }

        // Store whether BassSynth was playing before the change
        bool wasPlaying = bassSynthPlaying;

        // Only perform stop/change/play sequence if BassSynth is actually playing
        // This prevents unnecessary Wwise calls and potential sound buildup
        if (wasPlaying)
        {
            // Stop BassSynth before changing pitch switch (prevents overlapping sounds)
            AkSoundEngine.PostEvent("Stop_BassSynth", gameObject);
            if(debugAllowBassSynthLogs)
            {
                Debug.Log("Music: BassSynth Stop event posted (changing pitch from " + currentPitchStr + " to " + newPitchStr + ")");
            }
        }

        // Set the pitch switch (cooldown check passed above)
        AkSoundEngine.SetSwitch("BassSynth_PitchSwitch", newPitchStr, gameObject);
        if(debugAllowBassSynthLogs)
        {
            Debug.Log("Music: BassSynth pitch switch changed from " + currentPitchStr + " to " + newPitchStr);
        }

        
        // Update current pitch
        currentBassSynthPitch = newPitch;


        // Play BassSynth again if it was playing before
        // Note: If wasPlaying was false, we just update the switch for when BassSynth does start
        if (wasPlaying)
        {
            AkSoundEngine.PostEvent("Play_BassSynth", gameObject);
            bassSynthCooldownTimer = 0f; // Reset cooldown after changing pitch and triggering Play_BassSynth
            if(debugAllowBassSynthLogs)
            {
                Debug.Log("Music: BassSynth Start event posted (pitch change complete - new pitch: " + newPitchStr + ")");
            }
        }
        // Note: If wasPlaying is false, we don't reset cooldown because no Play_BassSynth event was triggered
        // The switch is just set for when BassSynth starts later
        // Note: We don't update bassSynthPlaying here because:
        // - If wasPlaying was true, bassSynthPlaying is still true (we're restarting)
        // - If wasPlaying was false, bassSynthPlaying stays false (we're just setting switch for future start)
    }

  

    
    public void SetMusicSilentLayerVolume(float _target, float fadeDuration = 0.1f)
    {
        int ms = (int) Mathf.RoundToInt(fadeDuration * 1000);

        AkSoundEngine.SetRTPCValue("SILENT_Volume", _target, gameObject, ms);
        if(debugAllowMixVolumeLogs)
        {
            Debug.Log("MUSIC: Set Silent Volume to " + _target);
        }
    }

    public void SetMusicToningLayerVolume(float _target, float fadeDuration = 0.1f)
    {
        int ms = (int) Mathf.RoundToInt(fadeDuration * 1000);

        AkSoundEngine.SetRTPCValue("TONING_Volume", _target, gameObject, ms);
        //Debug.Log("MUSIC: Set Toning Volume to " + _target);
        //TONING_Volume (opposite of SILENT_Volume) is set in ImitoneVoiceInterpreter, and is dynamic with player volume.

    }




    
    //====================================================================================================
    //PLAYGROUND
    //====================================================================================================

    
    public void StartInteractiveMusic()
    {
        if(!interactiveMusicFlag)
        {
            interactiveMusicFlag = true;
            AkSoundEngine.PostEvent("Play_SilentLoops", gameObject); //this should do both fundamentals and harmonies
            //AkSoundEngine.PostEvent("Play_SilentLoops_v3_FundamentalOnly",gameObject);
            //AkSoundEngine.PostEvent("Play_SilentLoops_v3_HarmonyOnly",gameObject);
            AkSoundEngine.PostEvent("Play_MusicLoops", gameObject);
            // Note: BassSynth is now controlled by toneActiveConfident system, not started here
            // This prevents duplicate Play_BassSynth events that could cause sound buildup
            if(debugAllowSoundscapeLogs)
            {
                Debug.Log("MUSIC: InteractiveMusic started");
            }
        }
        else
        {
            if(debugAllowWarnings || debugAllowSoundscapeLogs)
            {
                Debug.LogWarning("MUSIC: InteractiveMusic is already started");
            }
        }
    }

    public void StopInteractiveMusic(bool suppressStoppingMusicLoops = false)
    {
        if(interactiveMusicFlag)
        {
            interactiveMusicFlag = false;
            AkSoundEngine.PostEvent("Stop_InteractiveMusicSystem", gameObject);
            if(!suppressStoppingMusicLoops)
            {
                AkSoundEngine.PostEvent("Stop_MusicLoops", gameObject);
            }
            //AkSoundEngine.PostEvent("Stop_BassSynth", gameObject);
            if(debugAllowSoundscapeLogs)
            {
                Debug.Log("MUSIC: InteractiveMusic stopped" + (suppressStoppingMusicLoops ? " (MusicLoops not stopped by request)" : ""));
            }
        }
        else
        {
            if(debugAllowSoundscapeLogs)
            {
                Debug.Log("MUSIC: InteractiveMusic tried to stop but did not stop because it is not started");
            }
        }
    }

    /// <summary>
    /// Stage D: thin delegator. Single owner of the ambient bed Wwise lifecycle is <see cref="MusicSystemLinear"/>
    /// (see <c>Docs/CALIBRATION_UI_SEQUENCING_PLAN.md</c> Stage D and <c>Docs/MUSIC_ENVIRONMENT_MODE_WWISE_STATE_REFACTOR_PLAN.md</c>).
    /// Called from <see cref="SetMusicModeTo"/> when entering <see cref="MusicMode.Environment"/>.
    /// </summary>
    private void EnterMusicEnvironmentAudio()
    {
        if (MusicSystemLinear.instance == null)
        {
            Debug.LogError("MusicSystem1: MusicSystemLinear.instance is null; cannot enter environment audio. Ensure a MusicSystemLinear component lives on a Wwise-registered GameObject (recommended: same GameObject as MusicSystem1).");
            return;
        }
        MusicSystemLinear.instance.Play();
        if (debugAllowMusicModeLogs)
        {
            Debug.Log("MUSIC: EnterMusicEnvironmentAudio — delegated to MusicSystemLinear.Play()");
        }
    }

    /// <summary>
    /// Stage D: thin delegator. See <see cref="EnterMusicEnvironmentAudio"/> notes.
    /// Called from <see cref="SetMusicModeTo"/> when leaving <see cref="MusicMode.Environment"/>.
    /// </summary>
    private void ExitMusicEnvironmentAudio()
    {
        if (MusicSystemLinear.instance == null)
        {
            Debug.LogError("MusicSystem1: MusicSystemLinear.instance is null; cannot exit environment audio.");
            return;
        }
        MusicSystemLinear.instance.Stop();
        if (debugAllowMusicModeLogs)
        {
            Debug.Log("MUSIC: ExitMusicEnvironmentAudio — delegated to MusicSystemLinear.Stop() (delayed exit handled inside the delegate).");
        }
    }

    public void OnSoundscapeDropdownChanged(int index)
    {
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC DROPDOWN: OnSoundscapeDropdownChanged triggered with index: " + index);
        }
        if(soundscapeDropdown != null)
        {
            switch (index)
            {
                case 0: SetSoundscape("SonoFlore"); if(debugAllowSoundscapeLogs) { Debug.Log("MUSIC DROPDOWN: SonoFlore"); } break;
                case 1: SetSoundscape("Shadow"); if(debugAllowSoundscapeLogs) { Debug.Log("MUSIC DROPDOWN: Shadow"); } break;
                case 2: SetSoundscape("Gentle"); if(debugAllowSoundscapeLogs) { Debug.Log("MUSIC DROPDOWN: Gentle"); } break;
                case 3: SetSoundscape("Shruti"); if(debugAllowSoundscapeLogs) { Debug.Log("MUSIC DROPDOWN: Shruti"); } break;
                case 4: SetSoundscape("ShiftingEarth"); if(debugAllowSoundscapeLogs) { Debug.Log("MUSIC DROPDOWN: ShiftingEarth"); } break;
                case 5: SetSoundscape("SitarAmbience"); if(debugAllowSoundscapeLogs) { Debug.Log("MUSIC DROPDOWN: SitarAmbience"); } break;
                case 6: SetSoundscape("PinkNoiseAtmosphere"); if(debugAllowSoundscapeLogs) { Debug.Log("MUSIC DROPDOWN: PinkNoiseAtmosphere"); } break;
                case 7: worldShuffler.ShuffleWorldsNow(); if(debugAllowSoundscapeLogs) { Debug.Log("MUSIC DROPDOWN: ShuffleWorldsNow triggered"); } break;
                default: SetSoundscape("SonoFlore"); break;
            }
        }
    }

    public void SetSoundWorldFlag() //THIS IS IMPORTANT, BECAUSE IF WE NEVER SET THE SOUND WORLD, WWISE WILL DEFAULT TO PLAYING ALL OF THEM AT ONCE.
    {
        haveSetSoundWorldFlag = true;
    }

    /// <summary>Sets Wwise switches for Adjunctive Dual Stage defaults (MusicLoops, Gentle). Call from WwiseVOManager when entering Dual Stage mode.</summary>
    public void SetProtocolStacksAscendingDefaults()
    {
        RunWithToningRestoredAfterInteractiveSwitch(() =>
        {
            AkSoundEngine.SetSwitch("InteractiveMusicMode_Switch", "MusicLoops", gameObject);
            AkSoundEngine.SetSwitch("SoundWorldMode_Switch", "Gentle", gameObject);
        });
        SetSoundWorldFlag();
    }

    /// <summary>Sets InteractiveMusicMode_Switch to MusicLoops and optionally MusicLoops_Volume. Call from InputReferences or debug controls.</summary>
    public void SetInteractiveMusicModeToMusicLoops(float volumePercent = 80f)
    {
        SetSwitchRestoreToningV3("InteractiveMusicMode_Switch", "MusicLoops");
        AkSoundEngine.SetRTPCValue("MusicLoops_Volume", volumePercent);
    }

    //====================================================================================================
    //SIMPLIFICATION SWITCHES
    //====================================================================================================
    
    /// <summary>
    /// Enables or disables fundamental tracking system
    /// </summary>
    public void SetFundamentalTrackingEnabled(bool enabled)
    {
        enableFundamentalTracking = enabled;
        LogSimplificationStatus();
    }
    
    /// <summary>
    /// Enables or disables harmony tracking and changes system
    /// </summary>
    public void SetHarmonyTrackingEnabled(bool enabled)
    {
        enableHarmonyTracking = enabled;
        if (!enabled)
        {
            // Reset harmony note when disabling (harmony won't play if system is disabled)
            harmonyNote = NoteName.None;
        }
        LogSimplificationStatus();
    }
    
    /// <summary>
    /// Enables or disables BassSynth system
    /// </summary>
    public void SetBassSynthEnabled(bool enabled)
    {
        enableBassSynth = enabled;
        if (!enabled)
        {
            // Stop BassSynth when disabling
            if (bassSynthPlaying)
            {
                AkSoundEngine.PostEvent("Stop_BassSynth", gameObject);
                bassSynthPlaying = false;
                currentBassSynthPitch = null;
                if(debugAllowBassSynthLogs)
                {
                    Debug.Log("Music: BassSynth stopped (system disabled)");
                }
            }
            // Clear pending actions
            pendingBassSynthStart = false;
            pendingBassSynthPitch = null;
        }
        LogSimplificationStatus();
    }
    
    /// <summary>
    /// Enables or disables basic toning behaviors (BasicToningUpdate)
    /// </summary>
    public void SetBasicToningEnabled(bool enabled)
    {
        enableBasicToning = enabled;
        if (!enabled)
        {
            // Stop toning when disabling
            StopWwiseToning();
        }
        LogSimplificationStatus();
    }
    
    /// <summary>
    /// Enables or disables Thump SFX system
    /// </summary>
    public void SetThumpSFXEnabled(bool enabled)
    {
        enableThumpSFX = enabled;
        if (!enabled)
        {
            // Reset impact sound flag when disabling
            impactSoundFlag = false;
        }
        LogSimplificationStatus();
    }
    
    /// <summary>
    /// Enables or disables Imitone interpretation system
    /// </summary>
    public void SetImitoneInterpretationEnabled(bool enabled)
    {
        enableImitoneInterpretation = enabled;
        if (!enabled)
        {
            // Reset note tracking when disabling
            foreach (var key in NoteTracker.Keys.ToList())
            {
                var current = NoteTracker[key];
                NoteTracker[key] = (0f, false, false, current.ChangeFundamentalTimer);
            }
            musicNoteActivated = NoteName.None;
        }
        LogSimplificationStatus();
    }
    
    /// <summary>
    /// Logs a list of all systems that are currently disabled
    /// </summary>
    private void LogSimplificationStatus()
    {
        List<string> disabledSystems = new List<string>();
        
        if (!enableFundamentalTracking) disabledSystems.Add("Fundamental Tracking");
        if (!enableHarmonyTracking) disabledSystems.Add("Harmony Tracking");
        if (!enableBassSynth) disabledSystems.Add("Bass Synth");
        if (!enableBasicToning) disabledSystems.Add("Basic Toning");
        if (!enableDirectVoiceMonitoring) disabledSystems.Add("Direct Voice Monitoring");
        if (!enableThumpSFX) disabledSystems.Add("Thump SFX");
        if (!enableImitoneInterpretation) disabledSystems.Add("Imitone Interpretation");
        
        if (disabledSystems.Count > 0)
        {
            Debug.Log("Music: SIMPLIFICATION - Disabled Systems: " + string.Join(", ", disabledSystems));
        }
        else
        {
            Debug.Log("Music: SIMPLIFICATION - All systems enabled");
        }
    }

    // TODO
    // Move Thump and Breathwork into an SFX system.
    // Possibly also separate out the different elements of the interactive music system (i.e. fundamental, etc)

    // ===== REWARD THUMPS =====
    //REEF, We will send the reward thump to WWise once chantCharge reaches 1.0 (or perhaps chantCharge rises above 0.9, test it out, I don't remember if it's finicky to actually reach 1.0 due to inerpolation rules)
    
    // ===== LIMITING THE FUNDAMENTAL DURING THE TRAINING PERIOD =====
    //REEF, we need to have a way of setting the fundamental from your audio manager, to limit the behavior during the training period.
    //It should be set, initially, to match the tone of Jaya's vocalizations. Lorna can tell you what that is. (update: A, which is 9, from Slack)
    //We should not allow the fundamentla to change, until we have reached the last "test" (vo_test14tone, I think, but please verify) that includes one of Jaya's tones in it. 
    //At that point, the fundamental should change to whatever tone has the highest ChangeFundamentalTimer at that time (and all timers reset)
    //I think it's okay for us to disregard, for this behavior, the possibility of needing to enter a repair cycle, where Jaya does indeed tone... but if you want to be thorough, you can switch back to Jaya's fundamental (A = Key.[9]), temporarily, for the repair sequence.
 

    // ====================================================================================================
    // UNITY BUTTON TEST METHODS - Public methods for testing Wwise events via Unity UI buttons
    // ====================================================================================================
    
    /// <summary>
    /// Plays the impact sound effect. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_PlayImpactSound()
    {
        AkSoundEngine.PostEvent("Play_sfx_Impact", gameObject);
        if(debugAllowSFXLogs)
        {
            Debug.Log("MUSIC BUTTON: Play_sfx_Impact");
        }
    }

    /// <summary>
    /// Plays the toning fundamental event. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_PlayToningFundamental()
    {
        if (ToningV3FundamentalPlaying)
        {
            return;
        }
        AkSoundEngine.PostEvent("Play_Toning_v3_FundamentalOnly", gameObject);
        ToningV3FundamentalPlaying = true;
        if(debugAllowBasicToningLogs)
        {
            Debug.Log("MUSIC BUTTON: Play_Toning_v3_FundamentalOnly");
        }
    }

    public void SetBreathworkCycle(bool play = true)
    {
        if(play && !breathworkCyclePlaying)
        {
            AkSoundEngine.PostEvent("Play_sfx_breathworkcycle", gameObject);
            breathworkCyclePlaying = true;
        }
        else if(!play && breathworkCyclePlaying)
        {
            AkSoundEngine.PostEvent("Stop_sfx_breathworkcycle", gameObject);
        }
    }

    /// <summary>
    /// Plays the toning harmony event. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_PlayToningHarmony()
    {
        if (ToningV3HarmonyPlaying)
        {
            return;
        }
        AkSoundEngine.PostEvent("Play_Toning_v3_HarmonyOnly", gameObject);
        ToningV3HarmonyPlaying = true;
        if(debugAllowBasicToningLogs)
        {
            Debug.Log("MUSIC BUTTON: Play_Toning_v3_HarmonyOnly");
        }
    }

    /// <summary>
    /// Stops all toning events. Can be called from Unity UI buttons.
    /// Note: This is the same as StopWwiseToning() which is already public.
    /// </summary>
    public void Button_StopToning()
    {
        StopWwiseToning();
        if(debugAllowBasicToningLogs)
        {
            Debug.Log("MUSIC BUTTON: Stop_Toning");
        }
    }

    /// <summary>
    /// Plays the BassSynth event. Can be called from Unity UI buttons.
    /// Note: This uses the same logic as PostTheBassSynthEvent() but is simpler for button testing.
    /// </summary>
    public void Button_PlayBassSynth()
    {
        PostTheBassSynthEvent();
        if(debugAllowBassSynthLogs)
        {
            Debug.Log("MUSIC BUTTON: Play_BassSynth");
        }
    }

    /// <summary>
    /// Stops the BassSynth event. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_StopBassSynth()
    {
        AkSoundEngine.PostEvent("Stop_BassSynth", gameObject);
        bassSynthPlaying = false;
        currentBassSynthPitch = null;
        if(debugAllowBassSynthLogs)
        {
            Debug.Log("MUSIC BUTTON: Stop_BassSynth");
        }
    }

    /// <summary>
    /// Plays the SilentLoops event. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_PlaySilentLoops()
    {
        AkSoundEngine.PostEvent("Play_SilentLoops", gameObject);
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC BUTTON: Play_SilentLoops");
        }
    }

    /// <summary>
    /// Plays the MusicLoops event. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_PlayMusicLoops()
    {
        AkSoundEngine.PostEvent("Play_MusicLoops", gameObject);
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC BUTTON: Play_MusicLoops");
        }
    }

    /// <summary>
    /// Stops the InteractiveMusicSystem event. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_StopInteractiveMusicSystem()
    {
        AkSoundEngine.PostEvent("Stop_InteractiveMusicSystem", gameObject);
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC BUTTON: Stop_InteractiveMusicSystem");
        }
    }

    /// <summary>
    /// Stops the MusicLoops event. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_StopMusicLoops()
    {
        AkSoundEngine.PostEvent("Stop_MusicLoops", gameObject);
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC BUTTON: Stop_MusicLoops");
        }
    }

    /// <summary>
    /// Starts interactive music (plays SilentLoops and MusicLoops). Can be called from Unity UI buttons.
    /// </summary>
    public void Button_StartInteractiveMusic()
    {
        StartInteractiveMusic();
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC BUTTON: StartInteractiveMusic");
        }
    }

    /// <summary>
    /// Stops interactive music. Can be called from Unity UI buttons.
    /// </summary>
    public void Button_StopInteractiveMusic()
    {
        StopInteractiveMusic();
        if(debugAllowSoundscapeLogs)
        {
            Debug.Log("MUSIC BUTTON: StopInteractiveMusic");
        }
    }

}

