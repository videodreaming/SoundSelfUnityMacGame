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
    private bool debugAllowLogs = false;
    public Sequencer sequencer;
    public WwiseVOManager wwiseVOManager;
    public WorldShuffler worldShuffler;
    public RespirationTracker respirationTracker;
    public Director director;
    public GameValues gameValues;
    public LightControl lightControl;
    public AudioSource monitoringAudioSource;
    public Tutorial tutorial;
    public SavasanaPlayer SavasanaPlayer;
   // public RecordedAudioPlaybackTest recordedAudioPlaybackTest;
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
    float _gameOnLerp = 0.0f;
    float _chargeLerp = 0.0f;

    
    // FUNDAMENTAL LOCKING SYSTEM
    // Three separate lock types with priority: DebugLock > ContentLock > ModeLock
    private NoteName? fundamentalModeLock = null;      // Mode-based lock (Tutorial, FrozenFreeplay)
    private NoteName? fundamentalContentLock = null;    // Content-based lock (MusicLoop compatibility)
    private NoteName? fundamentalDebugLock = null;      // Debug lock (development mode)
    private float UserNotToningThreshold = 30.0f; //controls environment shift.
    public MusicMode currentMusicMode;
    public InteractionType currentInteractionType = InteractionType.SoundWorld; // so when we shift into a mode that plays interactive music, we are using the right sub-system. This is getting complicated. Will be less so when we use environment as a musicLoop or something. 
    private bool interactiveMusicFlag = false;
    private bool initializeEnvironmentFlag = false;
    public string currentSwitchState = "C";

    //PLAYBACK AND INITIALIZATION
    //private bool environmentFlag = false;
    private bool interactiveFlag = false;
    
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

    public NoteName permanentlySetFundamental = NoteName.None;


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
            Debug.Log("MUSIC: AkGameObj component already exists");
        }

         if (soundscapeDropdown != null)
            soundscapeDropdown.onValueChanged.AddListener(OnSoundscapeDropdownChanged);
        
    }

    private void OnDestroy()
    {
        if (soundscapeDropdown != null)
            soundscapeDropdown.onValueChanged.RemoveListener(OnSoundscapeDropdownChanged);
    }

    public void OnPermanentlySetFundamentalChanged(int index)
    {
        switch(index)
        {
            case 1:
                SetFundamentalDebugLock(NoteName.C);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to C");
                break;
            case 2:
                SetFundamentalDebugLock(NoteName.Cs);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to Cs");
                break;
            case 3:
                SetFundamentalDebugLock(NoteName.D);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to D");
                break;
            case 4:
                SetFundamentalDebugLock(NoteName.Ds);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to Ds");
                break;
            case 5:
                SetFundamentalDebugLock(NoteName.E);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to E");
                break;
            case 6:
                SetFundamentalDebugLock(NoteName.F);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to F");
                break;
            case 7:
                SetFundamentalDebugLock(NoteName.Fs);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to Fs");
                break;
            case 8:
                SetFundamentalDebugLock(NoteName.G);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to G");
                break;
            case 9:
                SetFundamentalDebugLock(NoteName.Gs);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to Gs");
                break;
            case 10:
                SetFundamentalDebugLock(NoteName.A);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to A");
                break;
            case 11:
                SetFundamentalDebugLock(NoteName.As);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to As");
                break;
            case 12:
                SetFundamentalDebugLock(NoteName.B);
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to B");
                break;
            default:
                SetFundamentalDebugLock(null); // Clear debug lock
                Debug.Log("MUSIC: Permanently Set Fundamental Changed to None (debug lock cleared)");
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
    }

    void Update()
    {
        localToneOn = imitoneVoiceInterpreter.toneActiveBiasTrue;
        localBassSynthToneOn = imitoneVoiceInterpreter.toneActiveConfident; // BassSynth uses toneActiveConfident

        if (currentMusicMode == MusicMode.Silent)
        {
            //PUT STUFF HERE IF NECESSARY
        }
        else if (currentMusicMode == MusicMode.Tutorial)
        {
            DynamicMusicSystem();
        }
        else if(currentMusicMode == MusicMode.Freeplay) 
        { 
            DynamicMusicSystem();
            InactivitySwitchToEnvironment();
        }
        else if (currentMusicMode == MusicMode.FrozenFreeplay)
        {
            //PUT STUFF HERE IF NECESSARY
        }
        else if (currentMusicMode == MusicMode.Environment)
        {
            if(!tutorial.inTutorial && !SavasanaPlayer.playedThematicSavasana && !sequencer.lastMinuteTriggered) 
            {
                //only if ALL the following: we are not in the tutorial, last minute not triggered, and savasana has not begun.
                CheckForModeSwitchToFreeplay();
            }
        }

        DirectVoiceMonitoring();
        ThumpUpdate();
    }

    private void DynamicMusicSystem()
    {
        InterpretImitoneUpdate();
        BasicToningUpdate();
        BassSynthUpdate();
        FundamentalUpdate();
        HarmonyUpdate();
    }

    private void DirectVoiceMonitoring()
    {
        //first get a lerp for the gameOn state
        if(imitoneVoiceInterpreter.gameOn)
        {
            _gameOnLerp += Time.deltaTime * 2f;
            _gameOnLerp = Mathf.Clamp(_gameOnLerp, 0.0f, 1.0f);
        }
        else
        {
            _gameOnLerp -= Time.deltaTime * 0.5f;
            _gameOnLerp = Mathf.Clamp(_gameOnLerp, 0.0f, 1.0f);
        }

        //then get a lerp for the charge state
        if(imitoneVoiceInterpreter.toneActive)
        {
            if (gameValues._chantCharge > _chargeLerp)
            {
                _chargeLerp += Time.deltaTime;
                _chargeLerp = Mathf.Clamp(_chargeLerp, 0.0f, gameValues._chantCharge);
            }
            else if (gameValues._chantCharge < _chargeLerp)
            {
                _chargeLerp -= Time.deltaTime;
                _chargeLerp = Mathf.Clamp(_chargeLerp, gameValues._chantCharge, 1.0f);
            }
            else
            {
                _chargeLerp = gameValues._chantCharge;
            }
        }
        else
        {
            _chargeLerp -= Time.deltaTime;
            _chargeLerp = Mathf.Clamp(_chargeLerp, 0.0f, 1.0f);
        }

        monitoringAudioSource.volume = _gameOnLerp * (1.0f - _chargeLerp * 0.5f) * gameValues._chantLerpFast;
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
                            Debug.LogWarning($"MUSIC: GetWrappedDistance() returned -1 (indicating None was passed) for key={key}, fundamentalNoteName={fundamentalNoteName} - distance calculation may be incorrect");
                        }

                        // Change timer rate based on absorption and wrapped distance from fundamental
                        float _slowWhenHighAbsorption = Mathf.Pow(2, Mathf.Clamp(respirationTracker._absorption, 0, 1) * -1);
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
                            if (debugAllowLogs)
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
                            director.AddActionToQueue(Action_ChangeFundamental(key), "fundamentalChange", true, false, 9999f, false, 2);
                            directorStoredFundamental = key;

                            if (debugAllowLogs)
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
                Debug.Log("MUSIC: New Harmony Sequence Selected:" + currentSequenceIndex);
            }

            //Now play the tone
                            
            // Conversion point: NoteName arithmetic - AddInterval() handles NoteName enum internally
            // Converts to int for modulo arithmetic, then back to NoteName enum
            // Calculate harmony note by adding interval to fundamental note
            harmonyNote = NoteUtils.AddInterval(fundamentalNoteName, harmonization);
            if (harmonyNote == NoteName.None)
            {
                Debug.LogWarning($"MUSIC: AddInterval() returned NoteName.None for fundamentalNoteName={fundamentalNoteName}, harmonization={harmonization} - harmony will not play correctly");
            }
            changeHarmony(harmonyNote); 
            if (debugAllowLogs)
            {
                Debug.Log("MUSIC: Harmony Played: " + NoteUtils.NoteToWwiseString(harmonyNote) + " ~ (fundamentalNoteName + " + harmonization + ")");
            }
        }
    }

    private void ThumpUpdate ()
    {
        if(gameValues._chantCharge < 0.5f)
        {
            impactSoundFlag = false;
        }

        if(gameValues._chantCharge >= 0.95f)
        {
            if(!impactSoundFlag)
            {
                if(debugAllowLogs)
                {
                    Debug.Log("MUSIC: impact");
                }
                AkSoundEngine.PostEvent("Play_sfx_Impact",gameObject);
                impactSoundFlag = true;   
                lightControl.FXWave(0.8f, 1.5f, 0.1f, true);
            }
        }
    }
    
    //private void MusicModeUpdate()
    private void InactivitySwitchToEnvironment()
    {
        if(imitoneVoiceInterpreter._tThisRestConfident > UserNotToningThreshold && currentInteractionType != InteractionType.MusicLoop)
        {
            Debug.Log("MUSIC: Environment Mode : because " + imitoneVoiceInterpreter._tThisRestConfident + " > " + UserNotToningThreshold);
            SetMusicModeTo(MusicMode.Environment);
        }
    }

    private void CheckForModeSwitchToFreeplay()
    {
        if (!interactiveFlag && imitoneVoiceInterpreter.toneActiveVeryConfident)
        {
            Debug.Log("MUSIC: Interactive Music System Mode because toneActiveVeryConfident");
            SetMusicModeTo(MusicMode.Freeplay);
        }
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
        Tutorial,
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
        Debug.Log("MUSIC: Please set Music Mode to " + mode + "...");
        
        // Stop breathwork cycle when transitioning from Environment to any other mode
        if (currentMusicMode == MusicMode.Environment && mode != MusicMode.Environment)
        {
            StopBreathworkCycle();
        }
        
        switch (mode)
        {

            case MusicMode.MusicLoopSilent:
            currentMusicMode = mode;
            if(!modeMusicLoopSilentFlag)
            {
                SetMusicModeFlags(false, false, false, false, false, true);
                Debug.Log("MUSIC: Music Mode Set to MusicLoopSilent, which is a temporary mode for a musicloop version of silent mode, before we merge the two silent modes.");
                //imitoneVoiceInterpreter.gameOn = false; //peculaiarity of Ascending/Descending, we are keeping gameOn true for now. This will have to be addressed in the future.
                // Set state to MusicLoops and ensure interaction type is MusicLoop (required for this mode)
                //RecoverInteractiveMusicModeFromInteractionType(); //this may be necessary in futrue...
                currentInteractionType = InteractionType.MusicLoop;
                AkSoundEngine.SetState("InteractiveMusicMode", "MusicLoops");
                AkSoundEngine.SetSwitch("MusicLoops_Switch", "Silence", gameObject);
            }
            else
            {
                Debug.LogWarning("MUSIC: Tried to set Music Mode to MusicLoopSilent, but it was already set to MusicLoopSilent");
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
                Debug.Log("MUSIC: Music Mode Set to Silent (WWise: " + currentInteractionType + ")");
            }
            else
            {
                Debug.LogWarning("MUSIC: Tried to set Music Mode to Silent, but it was already set to Silent");
            }
            break;
            
            case MusicMode.Tutorial:
            currentMusicMode = mode;
            if(!modeTutorialFlag)
            {
                SetMusicModeFlags(false, true, false, false, false);    
                Debug.Log("MUSIC: Music Mode Set to Tutorial");

                StartInteractiveMusic(); 
                
                SetFundamentalModeLock(true, NoteName.C);
                
                RecoverInteractiveMusicModeFromInteractionType();
                
                SetMusicSilentLayerVolume(_silentVolumeHigh, 30f);

            }
            else
            {
                Debug.LogWarning("MUSIC: Tried to set Music Mode to Tutorial, but it was already set to Tutorial");
            }
            break;
            
            case MusicMode.Freeplay:
            currentMusicMode = mode;
            if(!modeFreeplayFlag)
            {
                SetMusicModeFlags(false, false, true, false, false);    
                Debug.Log("MUSIC: Music Mode Set to Freeplay");

                SetFundamentalModeLock(false);
                StartInteractiveMusic();
                imitoneVoiceInterpreter.gameOn = true;
                SetMusicSilentLayerVolume(_silentVolumeHigh, 40f);  

                RecoverInteractiveMusicModeFromInteractionType();
            }
            else
            {
                Debug.LogWarning("MUSIC: Tried to set Music Mode to Freeplay, but it was already set to Freeplay");
            }
            break;
            
            case MusicMode.FrozenFreeplay:
            //TODO: likely we don't need this mode anymore, and this can be a bespoke implementation.
            currentMusicMode = mode;
            if(!modeFrozenFreeplayFlag)
            {
                SetMusicModeFlags(false, false, false, true, false);    
                Debug.Log("MUSIC: Music Mode Set to FrozenFreeplay");

                SetFundamentalModeLock(true, NoteName.C);
                imitoneVoiceInterpreter.gameOn = false;
                
                RecoverInteractiveMusicModeFromInteractionType();
            }
            else
            {
                Debug.LogWarning("MUSIC: Tried to set Music Mode to FrozenFreeplay, but it was already set to FrozenFreeplay");
            }
            break;
            case MusicMode.Environment:
            currentMusicMode = mode;
            if(!modeEnvironmentFlag)
            {
                SetMusicModeFlags(false, false, false, false, true);    
                Debug.Log("MUSIC: Music Mode Set to Environment  (WWise: Environment)");

                EnvironmentInitializations();
                AkSoundEngine.SetState("InteractiveMusicMode", "Environment");
                PlayBreathworkCycle();
            }
            else
            {
                Debug.LogWarning("MUSIC: Tried to set Music Mode to Environment, but it was already set to Environment");
            }
            break;
            
            default:
            currentMusicMode = mode;
                Debug.Log("MUSIC: Invalid Music Mode: " + mode);
            break;
        }
    }

    private void RecoverInteractiveMusicModeFromInteractionType()
    {
        if(currentInteractionType == InteractionType.SoundWorld)
        {
            AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
            Debug.Log("MUSIC: Interactive Music Mode Recovered to InteractiveMusicSystem because Interaction Type is SoundWorld");
        }
        else if(currentInteractionType == InteractionType.MusicLoop)
        {
            AkSoundEngine.SetState("InteractiveMusicMode", "MusicLoops");
            Debug.Log("MUSIC: Interactive Music Mode Recovered to MusicLoops because Interaction Type is MusicLoop");
        }
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
            Debug.LogWarning("MUSIC: Unknown soundscape type requested: " + soundscape + " is neither soundWorld or musicLoop.");
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
        
        Debug.Log($"MUSIC TEST: currentInteractionType after SetSoundscape: {currentInteractionType}");
    }

    //public Action Action_SetSoundWorld(string soundWorld)
    //{
    //    return () => SetSoundWorld(soundWorld);
    //}

    public void SetSoundWorld(string soundWorld) //NOTE: this will currently break the MusicLoopSilent mode, which is a temporary mode. 
    {
        if(currentMusicMode != MusicMode.Environment)
        {
            AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
        }
        else
        {
            Debug.LogWarning($"MUSIC: Changing SoundWorld to '{soundWorld}', but current mode is '{currentMusicMode}' (Environment) -- this change will not be audible.");
        }
        
        currentInteractionType = InteractionType.SoundWorld;
        AkSoundEngine.SetState("SoundWorldMode", soundWorld);
        worldShuffler.SetCurrentSoundscape(soundWorld);
        
        // Clear content lock since SoundWorlds work with any fundamental
        SetFundamentalContentLock(null);
        Debug.Log("MUSIC: Soundscape Set To: " + soundWorld + " (SoundWorld)");
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
            Debug.LogWarning($"MUSIC: MusicLoop '{musicLoop}' not found in musicLoops dictionary - aborting SetMusicLoop()");
            return;
        }
        
        if(currentMusicMode != MusicMode.Environment)
        {
            AkSoundEngine.SetState("InteractiveMusicMode", "MusicLoops");
        }
        else
        {
            Debug.LogWarning($"MUSIC: Changing MusicLoop to '{musicLoop}', but current mode is '{currentMusicMode}' (Environment) -- this change will not be audible.");
        }
        currentInteractionType = InteractionType.MusicLoop;
        AkSoundEngine.SetSwitch("MusicLoops_Switch", musicLoop, gameObject);
        worldShuffler.SetCurrentSoundscape(musicLoop);
        
        // Set content lock to the required fundamental for this MusicLoop
        NoteName requiredNote = GetMusicLoopFundamental(musicLoop);
        if (requiredNote == NoteName.None)
        {
            Debug.LogWarning($"MUSIC: GetMusicLoopFundamental() returned NoteName.None for '{musicLoop}' - clearing content lock to avoid stale lock");
            // Clear content lock since we can't determine the required fundamental
            SetFundamentalContentLock(null);
        }
        else
        {
            SetFundamentalContentLock(requiredNote);
            Debug.Log($"MUSIC: Content lock set to {requiredNote} for MusicLoop '{musicLoop}'");
        }
        
        Debug.Log("MUSIC: Soundscape Set To: " + musicLoop + " (MusicLoop)");
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
            if (debugAllowLogs)
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
            Debug.LogWarning("MUSIC: Attempted to set fundamental to None - ignoring");
            return;
        }

        if(debugAllowLogs)
        {
            Debug.Log("MUSIC 6: Fundamental Note Changing to " + NoteUtils.NoteToWwiseString(newFundamental));
        }
        
        director.ClearQueueOfType("fundamentalChange");
        fundamentalNoteName = newFundamental;

        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", NoteUtils.NoteToWwiseString(fundamentalNoteName), gameObject);
        if (MusicBinauralBeats.instance != null)
        {
            MusicBinauralBeats.instance.ChangeCenterFrequency(NoteUtils.NoteToFrequencyA440(fundamentalNoteName));
        }
        else
        {
            Debug.LogWarning("MUSIC: MusicBinauralBeats.instance is null - binaural beats not initialized yet.");
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
            Debug.LogWarning("MUSIC: Tried to change the fundamental, but it was locked" + lockInfo + ". This shouldn't happen, and probably indicates a logic flaw in the code.");
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
                Debug.Log("MUSIC FUNDAMENTAL-DEBUG-LOCK: Debug lock cleared");
                
                // Resolve fundamental: apply lower priority locks or queue based on tracking
                ResolveFundamentalOnUnlock();
            }
            else
            {
                Debug.Log("MUSIC FUNDAMENTAL-DEBUG-LOCK: Tried to clear debug lock, but it was already cleared");
            }
            return;
        }
        
        NoteName lockNote = note.Value;
        
        // Safety check: don't allow locking to None
        if (lockNote == NoteName.None)
        {
            Debug.LogWarning("MUSIC FUNDAMENTAL-DEBUG-LOCK: Cannot set debug lock to NoteName.None - ignoring request");
            return;
        }
        
        // Optimization: if debug lock is already set to the requested note, skip work
        if (currentlyLocked && fundamentalDebugLock.Value == lockNote)
        {
            Debug.Log($"MUSIC FUNDAMENTAL-DEBUG-LOCK: Debug lock already set to {lockNote} - skipping update");
            return;
        }
        
        // Set debug lock (highest priority)
        fundamentalDebugLock = lockNote;
        
        // Use SetFundamentalDirect instead of ChangeFundamental because we're the lock system
        // requesting the change - we need to bypass the lock check
        // Debug lock has highest priority, so it always takes effect
        SetFundamentalDirect(lockNote);
        
        Debug.Log($"MUSIC FUNDAMENTAL-DEBUG-LOCK: Debug lock set and locked fundamental to {lockNote} (DEVELOPMENT ONLY - highest priority)");
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
                Debug.LogWarning("MUSIC FUNDAMENTAL-CONTENT-LOCK: Cannot set content lock to NoteName.None - ignoring request");
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
                Debug.Log($"MUSIC FUNDAMENTAL-CONTENT-LOCK: Content lock set to {lockNote}, but higher priority lock active ({activeLock.Value}) - fundamental unchanged");
            }

            if (currentlyLocked && !wasLockedTo)
                Debug.Log($"MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Lock changed from {oldLockValue.Value} to {lockNote}");
            else if (!currentlyLocked)
                Debug.Log($"MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Locked to {lockNote}");
            else
                Debug.Log($"MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content relocked to {lockNote}");
        }
        else if (!note.HasValue && currentlyLocked)
        {
            // Unlock: Clear the lock
            fundamentalContentLock = null;
            Debug.Log("MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Unlocked");
            
            // Resolve fundamental: apply lower priority locks or queue based on tracking
            ResolveFundamentalOnUnlock();
        }
        else if (!note.HasValue && !currentlyLocked)
        {
            Debug.Log("MUSIC FUNDAMENTAL-CONTENT-LOCK: Tried to unlock fundamental content, but it was already unlocked");
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
                Debug.Log($"MUSIC FUNDAMENTAL-MODE-LOCK: Mode lock set to {note}, but higher priority lock active ({activeLock.Value}) - fundamental unchanged");
            }

            if (currentlyLocked && !wasLockedTo)
                Debug.Log($"MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Lock changed from {oldLockValue.Value} to {note}");
            else if (!currentlyLocked)
                Debug.Log($"MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Locked to {note}");
            else
                Debug.Log($"MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode relocked to {note}");
        }
        else if (!doLock && currentlyLocked)
        {
            // Unlock: Clear the lock
            fundamentalModeLock = null;
            Debug.Log("MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Unlocked");
            
            // Resolve fundamental: apply lower priority locks or queue based on tracking
            ResolveFundamentalOnUnlock();
        }
        else if (!doLock && !currentlyLocked)
        {
            // Already unlocked
            Debug.Log("MUSIC FUNDAMENTAL-MODE-LOCK: Tried to unlock fundamental mode, but it was already unlocked");
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
            Debug.Log($"MUSIC FUNDAMENTAL MODE UNLOCK: Lower priority lock active ({activeLock.Value}) - fundamental set accordingly");
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
                    Debug.Log("MUSIC FUNDAMENTAL MODE UNLOCK: Fundamental Changed Immediately on Unlock (high threshold): " + NoteUtils.NoteToWwiseString(newFundamental.Value));
                }
                else
                {
                    // Timer is above queue threshold but below immediate threshold - queue it
                    director.ClearQueueOfType("fundamentalChange");
                    director.AddActionToQueue(Action_ChangeFundamental(newFundamental.Value), "fundamentalChange", true, false, 120f, true, 2);
                    directorStoredFundamental = newFundamental.Value;
                    Debug.Log("MUSIC FUNDAMENNTAL MODE UNLOCK: New Fundamental Queued on Unlock: " + NoteUtils.NoteToWwiseString(newFundamental.Value));
                }
            }
            else if (highestFundamentalTimer >= _queueFundamentalChangeThreshold && !newFundamental.HasValue)
            {
                Debug.LogWarning("MUSIC FUNDAMENTAL MODE UNLOCK: Threshold met but no valid fundamental found in NoteTracker - skipping queue");
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
            if(debugAllowLogs)
            {
                Debug.Log("MUSIC: Post Toning Events to Wwise");
            }

            PostTheToningEvents();

        } else if (!localToneOn && previousLocalToneOn)
        {
            if(debugAllowLogs)
            {
                Debug.Log("MUSIC: Post Toning Events STOP to Wwise");
            }
            StopWwiseToning();
        }

        previousLocalToneOn = localToneOn;
    }

    private void BassSynthUpdate()
    {
        // BassSynth control based on toneActiveConfident
        if(localBassSynthToneOn && !previousLocalBassSynthToneOn)
        {
            if(debugAllowLogs)
            {
                Debug.Log("MUSIC: BassSynth Start (toneActiveConfident)");
            }
            PostTheBassSynthEvent();
        }
        else if (!localBassSynthToneOn && previousLocalBassSynthToneOn)
        {
            if(debugAllowLogs)
            {
                Debug.Log("MUSIC: BassSynth Stop (toneActiveConfident)");
            }
            // Stop BassSynth when toneActiveConfident becomes false
            if (bassSynthPlaying)
            {
                AkSoundEngine.PostEvent("Stop_BassSynth", gameObject);
                bassSynthPlaying = false;
                currentBassSynthPitch = null;
                if(debugAllowLogs)
                {
                    Debug.Log("MUSIC: BassSynth stopped (toneActiveConfident became false)");
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
                    // Set pitch switch BEFORE playing (ensures correct pitch on start)
                    AkSoundEngine.SetSwitch("BassSynth_PitchSwitch", NoteUtils.NoteToWwiseString(targetPitch), gameObject);
                    currentBassSynthPitch = targetPitch;
                    AkSoundEngine.PostEvent("Play_BassSynth", gameObject);
                    bassSynthPlaying = true;
                    
                    if(debugAllowLogs)
                    {
                        Debug.Log("MUSIC: BassSynth started with pitch " + NoteUtils.NoteToWwiseString(targetPitch) + " (delayed start - InteractionType: " + currentInteractionType + ")");
                    }
                }
                else
                {
                    // BassSynth is already playing, update pitch if it changed
                    // UpdateBassSynthPitchSwitch() will skip if pitch hasn't changed
                    UpdateBassSynthPitchSwitch(targetPitch);
                }
            }
            // Edge case: If targetPitch is None, we skip pitch update (use last valid pitch)
            // This handles cases where musicNoteActivated or fundamentalNoteName is None
        }

        previousLocalBassSynthToneOn = localBassSynthToneOn;
    }

    public void StopWwiseToning()
    {
        AkSoundEngine.PostEvent("Stop_Toning",gameObject);

        // Note: BassSynth is now controlled separately by toneActiveConfident, not stopped here
        // BassSynth will stop automatically when toneActiveConfident becomes false
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
            Debug.LogWarning("MUSIC: fundamentalNoteName is None in InterpretImitoneUpdate - using raw input without harmonic adjustments");
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
            noteTrackerThreshold = imitoneVoiceInterpreter._activeThreshold3; //0.75f
        }   
        else if (imitoneVoiceInterpreter.toneActiveConfident)
        {
            noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold2; // 0.2f
        }
        else if (imitoneVoiceInterpreter.toneActive)
        {
            noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold1; //0.05f
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
                float localActivationTimer = scaleNote.Value.ActivationTimer;
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
                    Debug.LogWarning($"MUSIC: FloatToNoteName() returned NoteName.None for musicNoteInput={musicNoteInput} - note detection may be incorrect");
                }
                if (musicNoteInputNote == scaleNote.Key)
                {
                    if(debugAllowLogs && (localActivationTimer == 0 || (Time.frameCount % 30 == 0)))
                    {
                        //musicNoteActivated = scaleNote.Key; 
                        //Debug.Log("MUSIC 1: [COMPARE TONES] Key(" + scaleNote.Key + ") from musicNoteInputRaw (" + musicNoteInputRaw + ") ~~~~~ isActive(" + isActive + ") ActivationTimer(" + localActivationTimer + ") isHighestActivationTimer (" + isHighestActivationTimer + ")");
                    }
                    localActivationTimer += Time.deltaTime; // Increment active timer if current note input matches the tracker note

                    if (localActivationTimer >= highestActivationTimer && localActivationTimer != 0.0f)
                    {
                        if(debugAllowLogs)
                        {
                            //Debug.Log("MUSIC 2: [ACTIVATION TIMER FOR " + ConvertIntToNote(note.Key) + "] " + localActivationTimer + " >= " + highestActivationTimer + " && " + localActivationTimer + " != 0.0f");
                        }
                        highestActivationTimer = localActivationTimer;
                        isHighestActivationTimer = true;
                    }
                    
                    if (localActivationTimer >= noteTrackerThreshold && (anyNoteActive || isHighestActivationTimer))
                    {
                        if (debugAllowLogs && nextNote != scaleNote.Key)
                        {
                            Debug.Log("MUSIC 3: nextNote changed to (" + scaleNote.Key + ") Activation Timer(" + localActivationTimer + ") >= Threshold(" + noteTrackerThreshold + ")");
                        }
                        nextNote = scaleNote.Key;
                        if (imitoneVoiceInterpreter.toneActiveBiasTrue) //now we change the actual tone!
                        {
                            if(debugAllowLogs && !isActive)
                            {
                                Debug.Log("MUSIC 4: Voice Input Key (" + scaleNote.Key + ")!");
                            }
                            firstFrameActive = !isActive; //this will only be true on the first frame that the note is activated
                            isActive = true;
                            musicNoteActivated = scaleNote.Key;
                            activations[scaleNote.Key] = isActive;
                        }
                    }
                    updates[scaleNote.Key] = (localActivationTimer, isActive, firstFrameActive, scaleNote.Value.ChangeFundamentalTimer);
                }
                else if (!imitoneVoiceInterpreter.toneActiveBiasTrue)
                {
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
                        if(debugAllowLogs)
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
        if(debugAllowLogs)
        {
            Debug.Log("MUSIC: Post Toning Events to Wwise");
        }
        AkSoundEngine.PostEvent("Play_Toning_v3_FundamentalOnly",gameObject);
        AkSoundEngine.PostEvent("Play_Toning_v3_HarmonyOnly",gameObject);
    }

    public void PostTheBassSynthEvent()
    {
        // Early exit if BassSynth is already playing (most common case - prevents unnecessary pitch determination)
        if (bassSynthPlaying)
        {
            if(debugAllowLogs)
            {
                Debug.LogWarning("MUSIC: BassSynth is already playing - skipping duplicate Play_BassSynth event");
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
                if(debugAllowLogs)
                {
                    Debug.LogWarning("MUSIC: Cannot start BassSynth in MusicLoop mode - musicNoteActivated is None. Will start when note is detected.");
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
                if(debugAllowLogs)
                {
                    Debug.LogWarning("MUSIC: Cannot start BassSynth in SoundWorld mode - fundamentalNoteName is None. Will start when fundamental is set.");
                }
                // Don't start BassSynth yet - delayed start logic will handle starting it when a valid fundamental is set
                return;
            }
        }
        else
        {
            // Defensive check: InteractionType should only be MusicLoop or SoundWorld
            if(debugAllowLogs)
            {
                Debug.LogWarning("MUSIC: Unknown InteractionType: " + currentInteractionType + " - cannot determine BassSynth pitch");
            }
            return;
        }

        // Validate that we have a valid pitch before proceeding
        if (initialPitch == NoteName.None)
        {
            if(debugAllowLogs)
            {
                Debug.LogWarning("MUSIC: initialPitch is None after determining pitch - cannot start BassSynth");
            }
            return;
        }

        // Set initial pitch switch BEFORE playing (ensures correct pitch on start)
        AkSoundEngine.SetSwitch("BassSynth_PitchSwitch", NoteUtils.NoteToWwiseString(initialPitch), gameObject);
        currentBassSynthPitch = initialPitch;

        // Post Play_BassSynth event
        AkSoundEngine.PostEvent("Play_BassSynth", gameObject);
        bassSynthPlaying = true;

        if(debugAllowLogs)
        {
            Debug.Log("MUSIC: BassSynth started with pitch " + NoteUtils.NoteToWwiseString(initialPitch) + " (InteractionType: " + currentInteractionType + ")");
        }
    }
    
    private void changeHarmony(NoteName harmonyNote)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", NoteUtils.NoteToWwiseString(harmonyNote), gameObject);
        if(debugAllowLogs)
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
            if(debugAllowLogs && !bassSynthNonePitchWarningLogged)
            {
                Debug.LogWarning("MUSIC: Attempted to set BassSynth pitch to NoteName.None - ignoring (this warning will only appear once)");
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
        if (currentBassSynthPitch.HasValue && currentBassSynthPitch.Value == newPitch)
        {
            // Pitch hasn't changed, no need to update
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
            if(debugAllowLogs)
            {
                Debug.Log("MUSIC: Stopping BassSynth to change pitch switch");
            }
        }

        // Set the pitch switch
        AkSoundEngine.SetSwitch("BassSynth_PitchSwitch", NoteUtils.NoteToWwiseString(newPitch), gameObject);
        
        // Update current pitch
        currentBassSynthPitch = newPitch;

        if(debugAllowLogs)
        {
            Debug.Log("MUSIC: BassSynth pitch switch set to " + NoteUtils.NoteToWwiseString(newPitch));
        }

        // Play BassSynth again if it was playing before
        // Note: If wasPlaying was false, we just update the switch for when BassSynth does start
        if (wasPlaying)
        {
            AkSoundEngine.PostEvent("Play_BassSynth", gameObject);
            if(debugAllowLogs)
            {
                Debug.Log("MUSIC: Restarting BassSynth after pitch change");
            }
        }
        // Note: We don't update bassSynthPlaying here because:
        // - If wasPlaying was true, bassSynthPlaying is still true (we're restarting)
        // - If wasPlaying was false, bassSynthPlaying stays false (we're just setting switch for future start)
    }

  

    
    public void SetMusicSilentLayerVolume(float _target, float fadeDuration = 0.1f)
    {
        int ms = (int) Mathf.RoundToInt(fadeDuration * 1000);

        AkSoundEngine.SetRTPCValue("SILENT_Volume", _target, gameObject, ms);
        Debug.Log("MUSIC: Set Silent Volume to " + _target);
    }

    public void SetMusicToningLayerVolume(float _target, float fadeDuration = 0.1f)
    {
        int ms = (int) Mathf.RoundToInt(fadeDuration * 1000);

        AkSoundEngine.SetRTPCValue("TONING_Volume", _target, gameObject, ms);
        //Debug.Log("MUSIC: Set Toning Volume to " + _target);
        //TONING_Volume (opposite of SILENT_Volume) is set in ImitoneVoiceInterpreter, and is dynamic with player volume.

    }

    public void PlayBreathworkCycle()
    {
        AkSoundEngine.PostEvent("Play_sfx_breathworkcycle", gameObject);
        Debug.Log("MUSIC: Play_sfx_breathworkcycle");
    }

    public void StopBreathworkCycle()
    {
        AkSoundEngine.PostEvent("Stop_sfx_breathworkcycle", gameObject);
        Debug.Log("MUSIC: Stop_sfx_breathworkcycle");
    }

    
    //====================================================================================================
    //PLAYGROUND
    //====================================================================================================

    
    private void StartInteractiveMusic()
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
            Debug.Log("MUSIC: InteractiveMusic started");
        }
        else
        {
            Debug.LogWarning("MUSIC: InteractiveMusic is already started");
        }
    }

    private void StopInteractiveMusic(bool suppressStoppingMusicLoops = false)
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
            Debug.Log("MUSIC: InteractiveMusic stopped" + (suppressStoppingMusicLoops ? " (MusicLoops not stopped by request)" : ""));
        }
        else
        {
            Debug.LogWarning("MUSIC: InteractiveMusic is not started");
        }
    }

    private void EnvironmentInitializations()
    {
        if(!initializeEnvironmentFlag)
        {
            initializeEnvironmentFlag = true;
            AkSoundEngine.PostEvent("Play_AMBIENT_ENVIRONMENT_LOOP",gameObject);
        }
        else
        {
            Debug.LogWarning("MUSIC: Environment is already initialized");
        }
    }

    public void OnSoundscapeDropdownChanged(int index)
    {
        Debug.Log("MUSIC DROPDOWN: OnSoundscapeDropdownChanged triggered with index: " + index);
        if(soundscapeDropdown != null)
        {
            switch (index)
            {
                case 0: SetSoundscape("SonoFlore"); Debug.Log("MUSIC DROPDOWN: SonoFlore"); break;
                case 1: SetSoundscape("Shadow"); Debug.Log("MUSIC DROPDOWN: Shadow"); break;
                case 2: SetSoundscape("Gentle"); Debug.Log("MUSIC DROPDOWN: Gentle"); break;
                case 3: SetSoundscape("Shruti"); Debug.Log("MUSIC DROPDOWN: Shruti"); break;
                case 4: SetSoundscape("ShiftingEarth"); Debug.Log("MUSIC DROPDOWN: ShiftingEarth"); break;
                case 5: SetSoundscape("SitarAmbience"); Debug.Log("MUSIC DROPDOWN: SitarAmbience"); break;
                case 6: SetSoundscape("PinkNoiseAtmosphere"); Debug.Log("MUSIC DROPDOWN: PinkNoiseAtmosphere"); break;
                case 7: worldShuffler.ShuffleWorldsNow(); Debug.Log("MUSIC DROPDOWN: ShuffleWorldsNow triggered"); break;
                default: SetSoundscape("SonoFlore"); break;
            }
        }
    }

    
    // ===== REWARD THUMPS =====
    //REEF, We will send the reward thump to WWise once chantCharge reaches 1.0 (or perhaps chantCharge rises above 0.9, test it out, I don't remember if it's finicky to actually reach 1.0 due to inerpolation rules)
    
    // ===== LIMITING THE FUNDAMENTAL DURING THE TRAINING PERIOD =====
    //REEF, we need to have a way of setting the fundamental from your audio manager, to limit the behavior during the training period.
    //It should be set, initially, to match the tone of Jaya's vocalizations. Lorna can tell you what that is. (update: A, which is 9, from Slack)
    //We should not allow the fundamentla to change, until we have reached the last "test" (vo_test14tone, I think, but please verify) that includes one of Jaya's tones in it. 
    //At that point, the fundamental should change to whatever tone has the highest ChangeFundamentalTimer at that time (and all timers reset)
    //I think it's okay for us to disregard, for this behavior, the possibility of needing to enter a repair cycle, where Jaya does indeed tone... but if you want to be thorough, you can switch back to Jaya's fundamental (A = Key.[9]), temporarily, for the repair sequence.
 

}

