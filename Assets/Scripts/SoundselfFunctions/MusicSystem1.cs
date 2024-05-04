using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MusicSystem1 : MonoBehaviour
{
    private bool debugAllowLogs = true;
    // Variables
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; // Reference to an object that interprets voice to musical notes
    private int fundamentalNote; // Base note around which other notes are calculated
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
    private int nextNote = 0; // Next note to activate
    private float highestActivationTimer = 0.0f;

    void Start()
    {
        // Initialize the NoteTracker dictionary with 12 keys for each note in an octave
        for (int i = 0; i < 12; i++)
        {
            NoteTracker.Add(i, (0f, false, 0f));
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            debugAllowLogs = !debugAllowLogs;
        }
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

        // Process each note only if the imitone system is active
        if (imitoneVoiceInterpreter.imitoneActive)
        {
            foreach (var note in NoteTracker)
            {
                float newActivationTimer = note.Value.ActivationTimer;
                bool isActive = note.Value.Active;
                bool isHighestActivationTimer = false;
                //THERE HAS TO BE SOME WAY TO SET HIGHESTActivationTimer DOWN TO ZERO AGAIN, OR EVEN JUST LOWER IT=

                // Increment active timer if current note input matches the tracker note
                if (Mathf.Round(musicNoteInput) == note.Key)
                {
                    if(debugAllowLogs && (newActivationTimer == 0 || (Time.frameCount % 30 == 0)))
                    {
                        Debug.Log(note.Key + ": from musicNoteInputRaw: " + musicNoteInputRaw);
                    }
                    newActivationTimer += Time.deltaTime;
                    if (newActivationTimer >= highestActivationTimer && highestActivationTimer != 0.0f)
                    {
                        highestActivationTimer = newActivationTimer;
                        if(debugAllowLogs && isHighestActivationTimer)
                        {
                            Debug.Log(note.Key + ": Activation Timer (" + highestActivationTimer + ") is the highest");
                        }
                        isHighestActivationTimer = true;
                    }
                    
                    if (newActivationTimer >= noteTrackerThreshold && isHighestActivationTimer)
                    {
                        nextNote = note.Key;
                        if (debugAllowLogs)
                        {
                            Debug.Log(note.Key + ": sets nextNote. Activation (" + newActivationTimer + ") >= Threshold(" + noteTrackerThreshold + ")");
                        }
                        if (imitoneVoiceInterpreter.toneActive) //now we change the actual tone!
                        {
                            if(debugAllowLogs && !isActive)
                            {
                                Debug.Log(note.Key + ": IS NOW ACTIVE!");
                            }
                            
                            isActive = true;
                            activations[note.Key] = isActive;
                        }
                    }
                    updates[note.Key] = (newActivationTimer, isActive, note.Value.ChangeFundamentalTimer);
                }
                else if (!imitoneVoiceInterpreter.toneActiveConfident)
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
                DeactivateOtherNotes(activations);
            }
        }
    }

    // Function to deactivate all notes except the newly active one
    private void DeactivateOtherNotes(Dictionary<int, bool> activations)
    {
        foreach (var note in activations)
        {
            if (note.Key != nextNote && note.Value == true)
            {
                var currentValue = NoteTracker[note.Key];
                NoteTracker[note.Key] = (0.0f, false, currentValue.ChangeFundamentalTimer);
                if(debugAllowLogs)
                {
                    Debug.Log(note.Key + ": deactivated");
                }
            }
        }
        highestActivationTimer = 0.0f;
        if(debugAllowLogs)
        {
            Debug.Log("highestActivationTimer reset to 0.0f");
        }
    }
}

//GAME CALLS FOR WWISE THAT WE MIGHT WANT TO USE
// Game Call for Playing (turns on fundamental): AkSoundEngine.PostEvent("Play_Toning3_FundamentalOnly", gameObject);
// Game Call for Playing (turns on harmony): AkSoundEngine.PostEvent("Play_Toning3_HarmonyOnly", gameObject);

// Game Call for changing the Fundamental: AkSoundEngine.SetState("SoundWorldMode", currentToningState);
// Game Call for changing the Switch: AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup", currentSwitchState (This takes in an arguement the form of "A" or "B"), gameObject);


    //==== NEW NOTES FOR REEF ====//

    //REEF - We have several note messages that we are sending to WWise:
    //FOR FUNDAMENTALS -
    //We START the fundamental LOOPING layer for the fundamental note as soon as the fundamental changes to that note  : 
    //We STOP the fundamental LOOPING layer for the fundamental note as soon as the fundamental changes to another note (and another note starts)
    //We START the fundamental ONE-SHOT layer for the fundamental note when the player starts toning (toneActiveBiasTrue)
    //We STOP the fundamental ONE-SHOT layer for the fundamental note when the player stops toning (toneActiveBiasTrue)
    //FOR HARMONIES -
    //We START the harmony LOOPING layer for the harmony note as soon as the harmony changes to that note
    //We STOP the harmony LOOPING layer for the harmony note as soon as the harmony changes to another note (and another note starts)
    //We START the harmony ONE-SHOT layer for the harmony note when the player starts toning (toneActiveBiasTrue)
    //We STOP the harmony ONE-SHOT layer for the harmony note when the player stops toning (toneActiveBiasTrue)

    //REEF, We will send the reward thump to WWise once chantCharge reaches 1.0 (or perhaps chantCharge rises above 0.9, test it out, I don't remember if it's finicky to actually reach 1.0)
