using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using AK.Wwise;
using TMPro;
using ConversionUtilities;
using System.Linq;

//TODO: Simplify this system by just using NoteName, and doing away with ints and strings.

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
    private Dictionary<int, (float ActivationTimer, bool Active, bool FirstFrameActive, float ChangeFundamentalTimer)> NoteTracker = new Dictionary<int, (float, bool, bool, float)>();
    // Tracks information for each musical note:
    // ActivationTimer: Time duration the note has been active
    // Active: Whether the note is currently active
    // ChangeFundamentalTimer: Timer for changing the fundamental note

    //THE TWO DICTIONARIES BELOW NOT USED ANY MORE, WE SHOULD DELETE THEM WHEN WE ARE SURE WE DON'T NEED THEM
    //private Dictionary<int, bool> Fundamentals = new Dictionary<int, bool>(); // Tracks if a note is a fundamental tone
    //private Dictionary<int, bool> Harmonies = new Dictionary<int, bool>(); // Tracks if a note is a harmony
    
    // IMITONE INTERPRETATION AND BASIC TONES
    private float musicNoteInputRaw; // The raw note input from voice interpretation
    private float musicNoteInput; // Adjusted musical note input after processing
    public int musicNoteActivated {get; private set;} = -1; // The note that has been activated (while we are toneActiveBiasTrue), -1 if no note is activated    
    private float _constWiggleRoomPerfect = 0.5f; // Tolerance for note variation
    private float _constWiggleRoomUnison = 1.5f;
    private int directorStoredFundamental = -1;
    private int nextNote = -1; // Next note to activate
    private float highestActivationTimer = 0.0f;
    public bool localToneOn {get; private set;} = false;
    private bool previousLocalToneOn = false;

    
    public float _silentVolumeLow = 65f; //this was 50f, Robin changed it on 4/4/2025
    public float _silentVolumeHigh = 80f;

    // FUNDAMENTAL AND HARMONY CONTROL
    private float _queueFundamentalChangeThreshold = 12f;
    private float _initiateImminentFundamentalChangeThreshold = 22f; //was 35f, changed on 4/4/2025
    public int fundamentalNote = 9; // Base note around which other notes are calculated
    public NoteName fundamentalNoteName = NoteName.A;
    private int fundamentalNoteCompare = -1; //this is used to catch changes that are not triggered in this script, and to compare with the previous fundamentalNote for the purpose of changing the fundamental
    public int harmonyNote; // Note that plays in harmony with the fundamental note
    private float fundamentalTimeSinceLastTrigger   = 0f;
    private float harmonyTimeSinceLastTrigger = 0f;
    private float fundamentalRetriggerThreshold = 18f; // minimum time between fundamental retriggering
    private float harmonyRetriggerThreshold = 6f; // minimum time between harmony retriggering

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

    //REFACTORED FROM SEQUENCER and WWISEVOMANAGER
    private bool lockFundamental = false;
    private bool thisTonesImpactPlayed = false;
    private float UserNotToningThreshold = 30.0f; //controls environment shift.
    public MusicMode currentMusicMode;
    public InteractionType currentInteractionType = InteractionType.SoundWorld; // so when we shift into a mode that plays interactive music, we are using the right sub-system. This is getting complicated. Will be less so when we use environment as a musicLoop or something. 
    private bool interactiveMusicFlag = false;
    private bool initializeEnvironmentFlag = false;
    public string currentSwitchState = "C";

    //PLAYBACK AND INITIALIZATION
    private bool environmentFlag = false;
    private bool interactiveFlag = false;
    
    private bool modeSilentFlag = false;
    private bool modeTutorialFlag = false;
    private bool modeFreeplayFlag = false;
    private bool modeFrozenFreeplayFlag = false;
    private bool modeEnvironmentFlag = false;

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

    // MusicLoops only work with specific fundamental notes (compatibility checking not implemented yet)
    private static readonly List<string> musicLoops = new List<string>
    {
        "ShiftingEarth",
        "SitarAmbience",
        "PinkNoiseAtmosphere"
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
    void Start()
    {
        //THESE DEVELOPMENT MODE INITIALIZATIONS ARE NOW DONE IN SEQUENCER, AND ARE PROBABLY NOT NECESSARY, BUT I'M LEAVING THEM HERE FOR NOW IN CASE THE TIMING OF START() MATTERS - ROBIN 12/16/2025
        
        //if(DevelopmentMode.instance.startAtStart) //NORMAL START
        //{
        //    SetMusicModeTo(MusicMode.Silent);
        //    SetMusicSilentLayerVolume(_silentVolumeLow, 0f);          
        //    director.disable = true;
        //}
        //else if (DevelopmentMode.instance.startInTutorial)
        //{
        //    SetMusicModeTo(MusicMode.Tutorial);          
        //    director.disable = true;
        //    SetMusicSilentLayerVolume(_silentVolumeHigh, 0f); //Robin thinks this is redundant. (It's not because it does it instantly here)
        //}
        //else if(DevelopmentMode.instance.startInPlayground || DevelopmentMode.instance.//startRightBeforeSavasana)
        //{
        //    SetMusicModeTo(MusicMode.Freeplay);          
        //    director.disable = false;
        //    SetMusicSilentLayerVolume(_silentVolumeHigh, 0f); //Robin thinks this is redundant. (It's not because it does it instantly here)
        //}
        

        // Initialize the NoteTracker dictionary with 12 keys for each note in an octave
        for (int i = 0; i < 12; i++)
        {
            NoteTracker.Add(i, (0f, false, false, 0f));
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
            CheckForModeSwitchToEnvironment();
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
        var updates = new Dictionary<int, (float, bool, bool, float)>();
        List<float> fundamentalTimerValues = new List<float>();
        float highestFundamentalTimer = 0;

        // Cache keys to avoid modifying the dictionary while iterating
        List<int> noteKeys = new List<int>(NoteTracker.Keys);

        if (imitoneVoiceInterpreter.imitoneActive)
        {
            // First get the highest fundamental timer at the start
            foreach (int key in noteKeys)
            {
                fundamentalTimerValues.Add(NoteTracker[key].ChangeFundamentalTimer);
                if (NoteTracker[key].ChangeFundamentalTimer > highestFundamentalTimer)
                {
                    highestFundamentalTimer = NoteTracker[key].ChangeFundamentalTimer;
                }
            }

            // Perform the updates
            foreach (int key in noteKeys)
            {
                var scaleNote = NoteTracker[key];
                float newChangeFundamentalTimer = scaleNote.ChangeFundamentalTimer;

                if (scaleNote.Active)
                {
                    if (key != fundamentalNote)
                    {
                        // Calculate the wrapped distance between key and fundamentalNote
                        int d = Mathf.Min(Mathf.Abs(key - fundamentalNote), 12 - Mathf.Abs(key - fundamentalNote));

                        // Change timer rate based on absorption and wrapped distance from fundamental
                        float _slowWhenHighAbsorption = Mathf.Pow(2, Mathf.Clamp(respirationTracker._absorption, 0, 1) * -1);
                        float _fastWhenVeryDifferent = (d > 4 ? 2.0f : 1.0f);
                        float _newChangeMultiplier = _slowWhenHighAbsorption * _fastWhenVeryDifferent;
                        newChangeFundamentalTimer += (Time.deltaTime * _newChangeMultiplier);

                        // Fundamental change conditions
                        bool isHighestFundamentalTimer = newChangeFundamentalTimer >= highestFundamentalTimer;
                        bool retriggerTest = (fundamentalTimeSinceLastTrigger >= fundamentalRetriggerThreshold);
                        bool test = !lockFundamental && retriggerTest && isHighestFundamentalTimer;
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
                                    Debug.Log("MUSIC: Long Test Instantly Triggering Fundamental Change to " + NoteUtils.IntToNoteString(fundamentalNote));
                                else
                                    Debug.Log("MUSIC: Longish Test Instantly Triggering Fundamental Change to " + NoteUtils.IntToNoteString(fundamentalNote));
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
                                Debug.Log("MUSIC: Short Test New Fundamental Queued: " + NoteUtils.IntToNoteString(key));
                            }
                        }
                    }
                    else
                    {
                        // Reduce timers on other notes when current note is fundamental
                        foreach (int otherKey in noteKeys)
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
                            
            harmonyNote = (fundamentalNote + harmonization) % 12;
            changeHarmony(NoteUtils.IntToNoteString(harmonyNote)); 
            if (debugAllowLogs)
            {
                Debug.Log("MUSIC: Harmony Played: " + NoteUtils.IntToNoteString(harmonyNote) + " ~ (fundamentalNote + " + harmonization + ")");
            }
        }
    }

    private void ThumpUpdate ()
    {
        if(gameValues._chantCharge < 1.0f)
        {
            thisTonesImpactPlayed = false;
        }

        if(gameValues._chantCharge >= 1.0f)
        {
            if(!thisTonesImpactPlayed)
            {
                if(debugAllowLogs)
                {
                    Debug.Log("MUSIC: impact");
                }
                AkSoundEngine.PostEvent("Play_sfx_Impact",gameObject);
                thisTonesImpactPlayed = true;   
                lightControl.FXWave(0.8f, 1.5f, 0.1f, true);
            }
        }
    }
    
    //private void MusicModeUpdate()
    private void CheckForModeSwitchToEnvironment()
    {
        if(!environmentFlag && imitoneVoiceInterpreter._tThisRestConfident > UserNotToningThreshold)
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
        Environment
    }

    public enum InteractionType
    {
        SoundWorld,
        MusicLoop
    }
 

    public void SetMusicModeTo(MusicMode mode)
    {
        Debug.Log("MUSIC: Please set Music Mode to " + mode + "...");
        switch (mode)
        {
            
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
                
                LockToC(true);
                
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

                LockToC(false);
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
            currentMusicMode = mode;
            if(!modeFrozenFreeplayFlag)
            {
                SetMusicModeFlags(false, false, false, true, false);    
                Debug.Log("MUSIC: Music Mode Set to FrozenFreeplay");

                LockToC(true);
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
    private void SetMusicModeFlags(bool silent, bool tutorial, bool freeplay, bool frozenFreeplay, bool environment)
    {
        modeSilentFlag = silent;
        modeTutorialFlag = tutorial;
        modeFreeplayFlag = freeplay;
        modeFrozenFreeplayFlag = frozenFreeplay;
        modeEnvironmentFlag = environment;

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
        bool isMusicLoop = musicLoops.Contains(soundscape);
        
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
    }

    //public Action Action_SetSoundWorld(string soundWorld)
    //{
    //    return () => SetSoundWorld(soundWorld);
    //}

    public void SetSoundWorld(string soundWorld)
    {
        if(currentMusicMode != MusicMode.Environment)
        {
            AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
        }
        else
        {
            Debug.LogWarning($"MUSIC: Changing SoundWorld to '{soundWorld}', but current mode is '{currentMusicMode}' (Environment or Silent) -- this change will not be audible.");
        }
        
        currentInteractionType = InteractionType.SoundWorld;
        AkSoundEngine.SetState("SoundWorldMode", soundWorld);
        worldShuffler.SetCurrentSoundscape(soundWorld);
        Debug.Log("MUSIC: Soundscape Set To: " + soundWorld + " (SoundWorld)");
    }

    //public Action Action_SetMusicLoop(string musicLoop)
    //{
    //    return () => SetMusicLoop(musicLoop);
    //}
    public void SetMusicLoop(string musicLoop)
    {
        if(currentMusicMode != MusicMode.Environment)
        {
            AkSoundEngine.SetState("InteractiveMusicMode", "MusicLoops");
        }
        else
        {
            Debug.LogWarning($"MUSIC: Changing MusicLoop to '{musicLoop}', but current mode is '{currentMusicMode}' (Environment or Silent) -- this change will not be audible.");
        }
        currentInteractionType = InteractionType.MusicLoop;
        AkSoundEngine.SetSwitch("MusicLoops_Switch", musicLoop, gameObject);
        worldShuffler.SetCurrentSoundscape(musicLoop);
        Debug.Log("MUSIC: Soundscape Set To: " + musicLoop + " (MusicLoop)");
    }

    private Action Action_ChangeFundamental(int scaleNoteKey)
    {
        return () => ChangeFundamental(scaleNoteKey);
    }

    private void ChangeFundamental(int newFundamental)
    {
        if(!lockFundamental)
        {
            if(debugAllowLogs)
            {
                Debug.Log("MUSIC 6: Fundamental Note Changing to " + NoteUtils.IntToNoteString(newFundamental));
            }
            
            director.ClearQueueOfType("fundamentalChange");
            fundamentalNote = newFundamental;
            if (!NoteUtils.TryIntToNote(newFundamental, out fundamentalNoteName))
            {
                Debug.LogWarning($"MUSIC: Invalid fundamental note int: {newFundamental}, defaulting to A");
                fundamentalNoteName = NoteName.A;
            }
            AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", NoteUtils.IntToNoteString(fundamentalNote), gameObject);
            if (MusicBinauralBeats.instance != null)
            {
                MusicBinauralBeats.instance.ChangeCenterFrequency(NoteUtils.NoteToFrequencyA440(NoteUtils.IntToNoteString(newFundamental)));
            }
            else
            {
                Debug.LogWarning("MUSIC: MusicBinauralBeats.instance is null - binaural beats not initialized yet.");
            }

            ResetFundamentalTimers();
            directorStoredFundamental = newFundamental;
        }
        else
        {
            Debug.LogWarning("MUSIC: Tried to change the fundamental, but it was locked. This shouldn't happen, and probably indicates a logic flaw in the code.");
        }
    }
    //TODO: ADD DEVELOPMENT MODE CHECK
    private void OnValidate()
    {
        if (permanentlySetFundamental == NoteName.None) return; // "null"
        PermanentlySetFundamental(permanentlySetFundamental);
    }

    private void PermanentlySetFundamental(NoteName note = NoteName.C)
    {
        lockFundamental = false; // 1) Unlock
        ChangeFundamental(NoteUtils.NoteToInt(note)); // 2) Change
        lockFundamental = true; // 3) Lock again
        Debug.Log($"MUSIC: Permanently set and locked fundamental to {note}, (DEVELOPMENT ONLY)");
    }

    
    public void LockToC(bool doLock = true)
    {
        if (doLock && !lockFundamental)
        {
            ChangeFundamental(NoteUtils.NoteToInt("C"));
            lockFundamental = true;
            Debug.Log("MUSIC: Fundamental Locked to C");
        }
        else if (!doLock && lockFundamental)
        {
            lockFundamental = false;
            //Queue the fundamental change for whichever note has the highest ChangeFundamentalTimer, provided that it is higher than _queueFundamentalChangeThreshold
            int newFundamental = -1;
            float highestFundamentalTimer = 0;
            Debug.Log("MUSIC: Fundamental Unlocked");
            foreach (var note in NoteTracker)
            {
                if (note.Value.ChangeFundamentalTimer > highestFundamentalTimer)
                {
                    highestFundamentalTimer = note.Value.ChangeFundamentalTimer;
                    newFundamental = note.Key;
                }
            }
            if (highestFundamentalTimer >= _queueFundamentalChangeThreshold)
            {
                director.ClearQueueOfType("fundamentalChange");
                director.AddActionToQueue(Action_ChangeFundamental(newFundamental), "fundamentalChange", true, false, 120f, true, 2);
                directorStoredFundamental = newFundamental;
                Debug.Log("MUSIC: New Fundamental Queued on Unlock: " + NoteUtils.IntToNoteString(newFundamental));
            }
        }
        else if (doLock == lockFundamental)
        {
            if(doLock)
            Debug.Log("MUSIC: Tried to lock fundamental, but it was already locked");
            else
            Debug.Log("MUSIC: Tried to unlock fundamental, but it was already unlocked");
        }
    }
     


    private void ResetFundamentalTimers()
    {
        var keys = new List<int>(NoteTracker.Keys);

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

    public void StopWwiseToning()
    {
        AkSoundEngine.PostEvent("Stop_Toning",gameObject);
    }

    private void InterpretImitoneUpdate()
    {
         // ========================================================
        // CONVERTS RAW IMITONE INTO DATA USABLE BY OUR MUSIC SYSTEM
        // ========================================================
        
        // Modulo 12 on the interpreted note to get the position within an octave
        musicNoteInputRaw = imitoneVoiceInterpreter.note_st % 12;

        // IN CASE OF DISHARMONIC RELATIONSHIP, REPLACE WITH HARMONIC RELATIONSHIP
        if (Mathf.Abs(musicNoteInputRaw - fundamentalNote) < _constWiggleRoomUnison) //CLOSE TO UNISON
        {
            musicNoteInput = fundamentalNote;
        }
        else if (Mathf.Abs(musicNoteInputRaw - fundamentalNote) > (12.0f - _constWiggleRoomUnison)) //CLOSE TO OCTAVE
        {
            musicNoteInput = fundamentalNote;
        }
        else if (Mathf.Abs(musicNoteInputRaw - fundamentalNote + 6) < _constWiggleRoomPerfect) //CLOSE TO TRITONE
        {
            musicNoteInput = fundamentalNote + 5;
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
        var updates = new Dictionary<int, (float, bool, bool, float)>();
        var activations = new Dictionary<int, bool>();
        var fundamentalChanges = new Dictionary<int, bool>();

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

                if (Mathf.Round(musicNoteInput) == scaleNote.Key)
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
                            musicNoteActivated = scaleNote.Key; //
                            activations[scaleNote.Key] = isActive;
                        }
                    }
                    updates[scaleNote.Key] = (localActivationTimer, isActive, firstFrameActive, scaleNote.Value.ChangeFundamentalTimer);
                }
                else if (!imitoneVoiceInterpreter.toneActiveBiasTrue)
                {
                    updates[scaleNote.Key] = (0, false, false, scaleNote.Value.ChangeFundamentalTimer);
                    musicNoteActivated = -1;
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
            musicNoteActivated = -1;
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
    
    private void changeHarmony(string harmonyNote)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", harmonyNote, gameObject);
        if(debugAllowLogs)
        {
            Debug.Log("MUSIC: Harmony Note Set To: " + harmonyNote);
        }
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

    
    //====================================================================================================
    //PLAYGROUND
    //====================================================================================================
    public void PlaygroundMode(bool on, float _transitionSecs = 0f)
    {
        if(on) //used to trigger playground mode
        {
        }
        else
        {
            Debug.Log("Sequencer: PLAYGROUND OFF");
            LockToC(true);
            imitoneVoiceInterpreter.gameOn = false;
            director.disable = true;
        }
    }

    
    private void StartInteractiveMusic()
    {
        if(!interactiveMusicFlag)
        {
            interactiveMusicFlag = true;
            AkSoundengine.PostEvent("Play_SilentLoops", gameObject); //this should do both fundamentals and harmonies
            //AkSoundEngine.PostEvent("Play_SilentLoops_v3_FundamentalOnly",gameObject);
            //AkSoundEngine.PostEvent("Play_SilentLoops_v3_HarmonyOnly",gameObject);
            AkSoundEngine.PostEvent("Play_MusicLoops", gameObject);
            //AkSoundEngine.PostEvent("Play_BassSynth", gameObject);
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

    private void OnSoundscapeDropdownChanged(int index)
    {
        if(soundscapeDropdown != null)
        {
            switch (index)
            {
                case 0: SetSoundscape("SonoFlore"); Debug.Log("Initialize called.");  break;
                case 1: SetSoundscape("Shadow"); Debug.Log("StartTrueStart called."); break;
                case 2: SetSoundscape("Gentle"); Debug.Log("StartTutorialSequence called.");  break;
                case 3: SetSoundscape("Shruti");Debug.Log("StartPlayground called.");  break;
                case 4: SetSoundscape("ShiftingEarth"); Debug.Log("StartShiftingEarth called."); break;
                case 5: SetSoundscape("SitarAmbience"); Debug.Log("StartSitarAmbience called."); break;
                case 6: SetSoundscape("PinkNoiseAtmosphere"); Debug.Log("StartPinkNoiseAtmosphere called."); break;
                default: SetSoundscape("SonoFlore");  break;
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

