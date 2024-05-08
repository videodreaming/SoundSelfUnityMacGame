using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

//REEF TO-DO
// change the logic for initializing the system from happening in Start() to happening in the Audio Manager, as soon as we start listening for a tone.
// change "allowFundamentalChange" to false during the part of the training period where Jaya is toning, and then back to true afterwards. I think this is vo_test14tone, I think, but please verify first.

public class MusicSystem1 : MonoBehaviour
{
    private bool debugAllowLogs = true;
    // Variables
    public bool allowFundamentalChange = true; 
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; // Reference to an object that interprets voice to musical notes
    private int fundamentalNote; // Base note around which other notes are calculated
    private int fundamentalNoteOld = -1;

    private Dictionary<int, (float ActivationTimer, bool Active, float ChangeFundamentalTimer)> NoteTracker = new Dictionary<int, (float, bool, float)>();
    // Tracks information for each musical note:
    // ActivationTimer: Time duration the note has been active
    // Active: Whether the note is currently active
    // ChangeFundamentalTimer: Timer for changing the fundamental note

    private Dictionary<int, bool> Fundamentals = new Dictionary<int, bool>(); // Tracks if a note is a fundamental tone
    private Dictionary<int, bool> Harmonies = new Dictionary<int, bool>(); // Tracks if a note is a harmony
    
    private bool musicToneActive; // Indicates if a music tone is currently active
    private float musicNoteInputRaw; // The raw note input from voice interpretation
    private float musicNoteInput; // Adjusted musical note input after processing
    
    private float _constWiggleRoomPerfect = 0.5f; // Tolerance for note variation
    private float _constWiggleRoomUnison = 1.5f;
    private float _changeFundamentalThreshold = 12f;
    private int nextNote = -1; // Next note to activate
    private float highestActivationTimer = 0.0f;

    void Start()
    {
        // Initialize the NoteTracker dictionary with 12 keys for each note in an octave
        for (int i = 0; i < 12; i++)
        {
            NoteTracker.Add(i, (0f, false, 0f));
        }

        //SET THE FUNDAMENTAL NOTE TO A AND THE HARMONY TO E. All of this should be done in Audio Manager
        AkSoundEngine.PostEvent("Play_Toning3_FundamentalOnly", gameObject);
        AkSoundEngine.PostEvent("Play_Toning3_HarmonyOnly", gameObject);
        fundamentalNote = 9;
        AkSoundEngine.SetState("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", "E");
        Debug.Log("MUSIC: Fundamental and Harmony set to A and E, respectively. THIS SHOULD BE PERFORMED IN THE AUDIO MANAGER, NOT HERE. PLEASE EDIT THE CODE IN START() IN MUSICSYSTEM1.CS WHEN IT'S PROPERLY IMPLEMENTED");
        
    }

    void Update()
    {
        InterpretImitone();

        //Change Fundamental 
        if (fundamentalNoteOld != fundamentalNote)
        {
            AkSoundEngine.SetState("InteractiveMusicSwitchGroup_12Pitches_FundamentalOnly", ConvertIntToNote(fundamentalNote));
            fundamentalNoteOld = fundamentalNote;
            Debug.Log("MUSIC: WWise Fundamental: " + ConvertIntToNote(fundamentalNote));
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
        //MORE CONFIDENT TONING MAKES THE SYSTEM SLOWER TO RESPOND TO TONE CHANGES
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
            foreach (var note in NoteTracker)
            {
                float newActivationTimer = note.Value.ActivationTimer;
                float newChangeFundamentalTimer = note.Value.ChangeFundamentalTimer;
                bool isActive = note.Value.Active;
                bool isHighestActivationTimer = false;
            
                //THERE HAS TO BE SOME WAY TO SET HIGHESTActivationTimer DOWN TO ZERO AGAIN, OR EVEN JUST LOWER IT=

                // Increment active timer if current note input matches the tracker note
                if (Mathf.Round(musicNoteInput) == note.Key)
                {
                    //if(debugAllowLogs && (newActivationTimer == 0 || (Time.frameCount % 30 == 0)))
                    //{
                    //    Debug.Log("MUSIC LOG 1: [COMPARE TONES] Key(" + note.Key + ") from musicNoteInputRaw (" + musicNoteInputRaw + ") ~~~~~ isActive(" + isActive + ") ActivationTimer(" + newActivationTimer + ") isHighestActivationTimer (" + isHighestActivationTimer + ")");
                    //}
                    newActivationTimer += Time.deltaTime;
                        
                    if (newActivationTimer >= highestActivationTimer && newActivationTimer != 0.0f)
                    {
                        //Debug.Log(newActivationTimer + " >= " + highestActivationTimer + " && " + newActivationTimer + " != 0.0f");
                        highestActivationTimer = newActivationTimer;
                        isHighestActivationTimer = true;
                    }
                    
                    if (newActivationTimer >= noteTrackerThreshold && isHighestActivationTimer)
                    {
                        //if (debugAllowLogs && nextNote != note.Key)
                        //{
                        //    Debug.Log("MUSIC LOG 2: nextNote changed to (" + note.Key + ") Activation Timer(" + newActivationTimer + ") >= Threshold(" + noteTrackerThreshold + ")");
                        //}
                        nextNote = note.Key;
                        if (imitoneVoiceInterpreter.toneActiveBiasTrue) //now we change the actual tone!
                        {
                            if(debugAllowLogs && !isActive)
                            {
                                Debug.Log("MUSIC: Voice Input Key (" + note.Key + ")!");
                            }
                            bool firstFrameActive = !isActive; //this will only be true on the first frame that the note is activated
                            isActive = true;
                            activations[note.Key] = isActive;

                            // ===== FUNDAMENTAL CHANGING LOGIC =====
                            if (note.Key != fundamentalNote)
                            {
                                newChangeFundamentalTimer += Time.deltaTime;
                                if(debugAllowLogs && firstFrameActive)
                                {
                                    Debug.Log("MUSIC: Change Fundamental Timer for " + ConvertIntToNote(note.Key) + ": " + newChangeFundamentalTimer);
                                }

                                if(allowFundamentalChange)
                                {
                                    if (newChangeFundamentalTimer >= _changeFundamentalThreshold)
                                    {
                                        if (firstFrameActive)
                                        {
                                            fundamentalNote = note.Key;
                                            fundamentalChanges[note.Key] = true;
                                            if(debugAllowLogs)
                                            {
                                                Debug.Log("MUSIC: Fundamental Note Changed to " + ConvertIntToNote(fundamentalNote));
                                            }
                                        }
                                    }
                                }
                            }

                            



                            // ===== HARMONY CHANGING LOGIC =====
                            //REEF - in here, we should change the harmony whenever this block (the if statement) is activated.
                            // this will be either (the fundamental + 5 semitones), or (the fundamental + 7 semitones) (Modulo 12)
                            // each activation of this block will switch between the +5 and +7 variety, back and forth, so if you tone the same note again and again, you will get a different harmony, back and forth forever
                            // We send that note to WWise to switch the harmony note in this fashion
                            // this logic needs to work with the possibility of the fundamental changing mid-tone, so that the harmony is always in tune with the fundamental, even if the fundamental changes

                            // Game Call for Playing (turns on fundamental): AkSoundEngine.PostEvent("Play_Toning3_FundamentalOnly", gameObject);
                            // Game Call for Playing (turns on harmony): AkSoundEngine.PostEvent("Play_Toning3_HarmonyOnly", gameObject);
                            // Game Call for changing the Harmony: AkSoundEngine.SetState("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", currentToningState (aruement is "A" or "B" etc. or "AsharpBflat" or "FsharpGflat" "GsharpAflat"));
                            // Game Call for changing the Fundamental : AkSoundEngine.SetState("InteractiveMusicSwitchGroup_12Pitches_FundamentalOnly", currentToningState (argument is "A" or "B" etc.));


                        }
                    }
                    updates[note.Key] = (newActivationTimer, isActive, newChangeFundamentalTimer);
                }
                else if (!imitoneVoiceInterpreter.toneActiveBiasTrue)
                {
                    updates[note.Key] = (0, false, note.Value.ChangeFundamentalTimer);
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
                foreach (var note in activations)
                {
                    //When one note becomes active, deactivate others.
                    if (note.Key != nextNote && note.Value == true)
                    {
                        var currentValue = NoteTracker[note.Key];
                        NoteTracker[note.Key] = (0.0f, false, currentValue.ChangeFundamentalTimer);
                        //if(debugAllowLogs)
                        //{
                        //    Debug.Log("MUSIC LOG 4: Key(" + note.Key + ": deactivated (and highestActivationTimer reset)");
                        //}
                    }
                }
                highestActivationTimer = 0.0f;
            }

            // Resets all ChangeFundamentalTimers to 0 if a fundamental change has been made
            if (fundamentalChanges.ContainsValue(true))
            {
                foreach (var note in fundamentalChanges)
                {   
                    var currentValue = NoteTracker[note.Key];
                    NoteTracker[note.Key] = (currentValue.ActivationTimer, currentValue.Active, 0.0f);
                    if(debugAllowLogs)
                    {
                        Debug.Log("MUSIC: Key(" + note.Key + ": ChangeFundamentalTimer reset");
                    }
                }
            }
        }
    }

  

    public enum NoteName
    {
        C,
        CsharpDflat,
        D,
        DsharpEflat,
        E,
        F,
        FsharpGflat,
        G,
        GsharpAflat,
        A,
        AsharpBflat,
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
            throw new ArgumentException("Invalid noteNumber value");
        }
    }

    // Reef needs to know what note should be playing back at all times, fundamental and harmony wise, at any given time. Needs to know what Midi Note we should be on. (Robin will be able to give Reef midi note values) [REEF- THIS SHOULD BE ACCOMPLISHED BY THE NOTES IN THIS DOCUMENT]

     // ===== TRIGGERING ONE-SHOTS =====
    //REEF - the one-shot sounds in WWise should be triggered by toneActiveBiasTrue. Whenever toneActiveBiasTrue comes on, it triggers a one-shot (in the fundamental and harmony).
    //Lorna may want to have some logic in place for NOT triggering a one shot if it's only been a short period of time since the last one-shot of this tone was triggered. This is a decision up to her, but I'd be grateful if you'd make it clear to her that she might want to do that, once you talk through this implementation with her, because we could be triggering a one-shot on the fundamental again and again and again, only every few seconds, if the player is pumping a few short tones, for example. 
    
    // ===== REWARD THUMPS =====
    //REEF, We will send the reward thump to WWise once chantCharge reaches 1.0 (or perhaps chantCharge rises above 0.9, test it out, I don't remember if it's finicky to actually reach 1.0 due to inerpolation rules)

    // ===== WHEN TO ACTUALLY TURN ON (AND OFF) THE FUNDAMENTAL AND THE HARMONY =====
    //REEF, I am opening a conversation in #soundselfv1inunity about this. Basically, unless Lorna says otherwise, you should turn ON the fundamental and harmony both when the tutorial sequence starts listening for player vocalization. Not the query or the sigh, but actually listening for an "ahh", at the end of the voice and breath meditation. 
    //Likewise, it should turn off a little before the savasana sequence begins, when Lorna's closing track plays.

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

