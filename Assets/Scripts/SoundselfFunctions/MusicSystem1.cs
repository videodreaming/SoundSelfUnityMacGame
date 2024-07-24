using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public class MusicSystem1 : MonoBehaviour
{
    public WwiseVOManager wwiseVOManager;
    public WwiseInteractiveMusicManager wwiseInteractiveMusicManager;
    private bool debugAllowLogs = true;
    // Variables
    public bool allowFundamentalChange = true;
    public bool enableMusicSystem = true;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; // Reference to an object that interprets voice to musical notes

    private Dictionary<int, (float ActivationTimer, bool Active, float ChangeFundamentalTimer)> NoteTracker = new Dictionary<int, (float, bool, float)>();
    // Tracks information for each musical note:
    // ActivationTimer: Time duration the note has been active
    // Active: Whether the note is currently active
    // ChangeFundamentalTimer: Timer for changing the fundamental note

    private Dictionary<int, bool> Fundamentals = new Dictionary<int, bool>(); // Tracks if a note is a fundamental tone
    private Dictionary<int, bool> Harmonies = new Dictionary<int, bool>(); // Tracks if a note is a harmony
    
    // IMITONE INTERPRETATION
    private float musicNoteInputRaw; // The raw note input from voice interpretation
    private float musicNoteInput; // Adjusted musical note input after processing
    public int musicNoteActivated = -1; // The note that has been activated (while we are toneActiveBiasTrue), -1 if no note is activated    
    private float _constWiggleRoomPerfect = 0.5f; // Tolerance for note variation
    private float _constWiggleRoomUnison = 1.5f;
    private float _changeFundamentalThreshold = 5f;
    private int nextNote = -1; // Next note to activate
    private float highestActivationTimer = 0.0f;

    // FUNDAMENTAL AND HARMONY CONTROL
    private bool musicToneActiveFrame; // Turns on the frame that a vocalization is interpreted by the music system.
    private bool musicToneActiveFrameGuard  = false; 
    private int musicToneActivationCount = 0; // Number of times we have tracked a vocalization
    public int fundamentalNote; // Base note around which other notes are calculated
    private int fundamentalNoteCompare = -1; //this is used to catch changes that are not triggered in this script, and to compare with the previous fundamentalNote for the purpose of changing the fundamental
    public int harmonyNote; // Note that plays in harmony with the fundamental note
    private float fundamentalTimeSinceLastTrigger   = 0f;
    private float harmonyTimeSinceLastTrigger = 0f;
    private float fundamentalRetriggerThreshold = 6f; // minimum time between fundamental retriggering
    private float harmonyRetriggerThreshold = 6f; // minimum time between harmony retriggering

    //HARMONY SEQUENCES
    List<int> harmonySequence1 = new List<int> {5, 7, 5, 7, 5, 7, 5, 7};
    List<int> harmonySequence2 = new List<int> {5, 5, 5, 5};
    List<int> harmonySequence3 = new List<int> {7, 7, 7, 7};
    List<int> harmonySequence4 = new List<int> {5, 7, 12, 5, 7, 12, 5, 7, 12, 5, 7, 12};

    List<List<int>> sequences;

    System.Random random = new System.Random();
        
    int currentSequenceIndex;
    int currentHarmonyIndex = 0;

    void Start()
    {
        // Initialize the NoteTracker dictionary with 12 keys for each note in an octave
        for (int i = 0; i < 12; i++)
        {
            NoteTracker.Add(i, (0f, false, 0f));
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
        if(enableMusicSystem)
        {
            InterpretImitone();
            //set musicToneActiveFrame to true for the first frame that toneActiveBiasTrue is true, and false for all other frames.
            if (!imitoneVoiceInterpreter.toneActiveBiasTrue)
            {
                musicToneActiveFrame = false;
                musicToneActiveFrameGuard = false;
            }
            else if (imitoneVoiceInterpreter.toneActiveBiasTrue && !musicToneActiveFrameGuard)
            {
                musicToneActiveFrame = true;
                musicToneActiveFrameGuard = true;
                musicToneActivationCount++;
            }
            else
            {
                musicToneActiveFrame = false;
            }
    


            //FUNDAMENTAL
            //Send commands to WWise to play the fundamental, either on new tone, or on fundamental change
            fundamentalTimeSinceLastTrigger += Time.deltaTime;
            harmonyTimeSinceLastTrigger += Time.deltaTime;
            bool fundamentalTimeTest    = fundamentalTimeSinceLastTrigger >= fundamentalRetriggerThreshold;
            bool fundamentalRetriggerTest = (musicToneActiveFrame && fundamentalTimeTest);
            bool fundamentalChangeTest = fundamentalNoteCompare != fundamentalNote;
            if (fundamentalRetriggerTest || fundamentalChangeTest)
            {
                string fundamentalInttoNote = ConvertIntToNote(fundamentalNote);
                //wwiseInteractiveMusicManager.userToningToChangeFundamental(fundamentalInttoNote); //UNCOMMENT THIS TO CONTROL WWISE                                          
                fundamentalNoteCompare = fundamentalNote;
                if (debugAllowLogs)
                {
                    Debug.Log("MUSIC: Fundamental Played: " + ConvertIntToNote(fundamentalNote) + " ~ LOGIC: musicToneActiveFrame (" + musicToneActiveFrame + ") fundamentalTimeTest (" + fundamentalTimeTest + ") fundamentalRetriggerTest (" + fundamentalRetriggerTest + ") fundamentalChangeTest (" + fundamentalChangeTest + ")");
                }
                fundamentalTimeSinceLastTrigger = 0f;
            }

            //HARMONY
            if(musicToneActiveFrame)
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
                }

                //Now play the tone
                               
                harmonyNote = (fundamentalNote + harmonization) % 12;
                wwiseInteractiveMusicManager.changeHarmony(ConvertIntToNote(harmonyNote)); //UNCOMMENT THIS TO CONTROL WWISE
                if (debugAllowLogs)
                {
                    Debug.Log("MUSIC: Harmony Played: " + ConvertIntToNote(harmonyNote) + " ~ (fundamentalNote + " + harmonization + ")");
                }
            }
        }
    }

    private void InterpretImitone()
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
        var updates = new Dictionary<int, (float, bool, float)>();
        var activations = new Dictionary<int, bool>();
        var fundamentalChanges = new Dictionary<int, bool>();

        // Process each note only if the imitone system is active
        if (imitoneVoiceInterpreter.imitoneActive)
        {
            foreach (var scaleNote in NoteTracker)
            {
                float newActivationTimer = scaleNote.Value.ActivationTimer;
                float newChangeFundamentalTimer = scaleNote.Value.ChangeFundamentalTimer;
                bool isActive = scaleNote.Value.Active;
                bool isHighestActivationTimer = false;
                // Increment active timer if current note input matches the tracker note
                if (Mathf.Round(musicNoteInput) == scaleNote.Key)
                {
                    if(debugAllowLogs && (newActivationTimer == 0 || (Time.frameCount % 30 == 0)))
                    {
                        //Debug.Log("MUSIC 1: [COMPARE TONES] Key(" + scaleNote.Key + ") from musicNoteInputRaw (" + musicNoteInputRaw + ") ~~~~~ isActive(" + isActive + ") ActivationTimer(" + newActivationTimer + ") isHighestActivationTimer (" + isHighestActivationTimer + ")");
                    }
                    newActivationTimer += Time.deltaTime;
                        
                    if (newActivationTimer >= highestActivationTimer && newActivationTimer != 0.0f)
                    {
                        if(debugAllowLogs)
                        {
                            //Debug.Log("MUSIC 2: [ACTIVATION TIMER FOR " + ConvertIntToNote(note.Key) + "] " + newActivationTimer + " >= " + highestActivationTimer + " && " + newActivationTimer + " != 0.0f");
                        }
                        highestActivationTimer = newActivationTimer;
                        isHighestActivationTimer = true;
                    }
                    
                    if (newActivationTimer >= noteTrackerThreshold && isHighestActivationTimer)
                    {
                        if (debugAllowLogs && nextNote != scaleNote.Key)
                        {
                            Debug.Log("MUSIC 3: nextNote changed to (" + scaleNote.Key + ") Activation Timer(" + newActivationTimer + ") >= Threshold(" + noteTrackerThreshold + ")");
                        }
                        nextNote = scaleNote.Key;
                        if (imitoneVoiceInterpreter.toneActiveBiasTrue) //now we change the actual tone!
                        {
                            if(debugAllowLogs && !isActive)
                            {
                                Debug.Log("MUSIC 4: Voice Input Key (" + scaleNote.Key + ")!");
                            }
                            bool firstFrameActive = !isActive; //this will only be true on the first frame that the note is activated
                            isActive = true;
                            musicNoteActivated = scaleNote.Key;
                            activations[scaleNote.Key] = isActive;
                
                            // ===== FUNDAMENTAL CHANGING LOGIC =====
                            if (scaleNote.Key != fundamentalNote)
                            {
                                newChangeFundamentalTimer += Time.deltaTime;

                                if (firstFrameActive)
                                {
                                    if(debugAllowLogs)
                                    {
                                        Debug.Log("MUSIC 5: Change Fundamental Timer for " + ConvertIntToNote(scaleNote.Key) + ": " + newChangeFundamentalTimer);
                                    }
                                    bool checkForNewTone = imitoneVoiceInterpreter._tThisToneBiasTrue < 2.0f;
                                    bool checkForThreshold = newChangeFundamentalTimer >= _changeFundamentalThreshold;
                                    if (checkForThreshold && checkForNewTone && allowFundamentalChange)
                                    {
                                        fundamentalNote = scaleNote.Key;
                                        fundamentalChanges[scaleNote.Key] = true;
                                        wwiseInteractiveMusicManager.userToningToChangeFundamental(ConvertIntToNote(fundamentalNote));
                                        if(debugAllowLogs)
                                        {
                                            Debug.Log("MUSIC 6: Fundamental Note Changed to " + ConvertIntToNote(fundamentalNote));
                                        }
                                    
                                    }
                                }
                            }
                        }
                    }
                    updates[scaleNote.Key] = (newActivationTimer, isActive, newChangeFundamentalTimer);
                }
                else if (!imitoneVoiceInterpreter.toneActiveBiasTrue)
                {
                    updates[scaleNote.Key] = (0, false, scaleNote.Value.ChangeFundamentalTimer);
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
                        NoteTracker[scaleNote.Key] = (0.0f, false, currentValue.ChangeFundamentalTimer);
                        if(debugAllowLogs)
                        {
                            Debug.Log("MUSIC 7: Key(" + scaleNote.Key + ": deactivated (and highestActivationTimer reset)");
                        }
                    }
                }
                highestActivationTimer = 0.0f;
            }

            // Resets all ChangeFundamentalTimers in the NoteTracker dictionary to 0 if a fundamental change has been made
            if (fundamentalChanges.ContainsValue(true))
            {
                var keys = new List<int>(NoteTracker.Keys);

                foreach (var key in keys)
                {
                    var currentValue = NoteTracker[key];
                    NoteTracker[key] = (currentValue.ActivationTimer, currentValue.Active, 0.0f);
                    if(debugAllowLogs)
                    {
                        Debug.Log("MUSIC 8: Key(" + key + ": ChangeFundamentalTimer reset");
                    }
                }
                
            }
        }
        
        else if (!imitoneVoiceInterpreter.toneActiveBiasTrue)
        {
            musicNoteActivated = -1;
        }
    }

    public enum NoteName
    {
        C,
        Cs,
        D,
        Ds,
        E,
        F,
        Fs,
        G,
        Gs,
        A,
        As,
        B
    }

    public string ConvertIntToNote(int noteNumber)
    {
        if (noteNumber >= 0 && noteNumber <= 11)
        {
            return Enum.GetName(typeof(NoteName), noteNumber);
        }
        else
        {
            Debug.Log(noteNumber);
            throw new ArgumentException("Invalid noteNumber value (MusicSystem1.cs)");
            
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

//REFERENCE FOR MIDI NOTES
//C = 0
//C# = 1
//D = 2
//D# = 3
//E = 4
//F = 5
//F# = 6
//G = 7
//G# = 8
//A = 9
//A# = 10
//B = 11


//GAME CALLS FOR WWISE THAT WE MIGHT WANT TO USE
// Game Call for Playing (turns on fundamental): AkSoundEngine.PostEvent("Play_Toning3_FundamentalOnly", gameObject);
// Game Call for Playing (turns on harmony): AkSoundEngine.PostEvent("Play_Toning3_HarmonyOnly", gameObject);

// Game Call for changing the Fundamental: AkSoundEngine.SetState("SoundWorldMode", currentToningState);
// Game Call for changing the Harmony: AkSoundEngine.SetState("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", currentToningState);
// Game Call for changing the Switch: AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup", currentSwitchState (This takes in an arguement the form of "A" or "B"), gameObject);

