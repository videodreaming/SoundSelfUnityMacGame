using System.Collections;
using System.Collections.Generic;
using Palmmedia.ReportGenerator.Core;
using UnityEngine;
using UnityEngine.UIElements;

public class MusicSystem1 : MonoBehaviour
{

    //Setting up our variables
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    private int fundamentalNote;
    private Dictionary<int, (float ActiveTimer, bool Active, float ChangeFundamentalTimer)> NoteTracker = new Dictionary<int, (float ActiveTimer, bool Active, float ChangeFundamentalTimer)>();
    private Dictionary<int, bool> Fundamentals = new Dictionary<int, bool>();
    private Dictionary<int, bool> Harmonies = new Dictionary<int, bool>();
    private bool musicToneActive;
    private float musicNoteInputRaw;
    private float musicNoteInput;
    private float _constWiggleRoom = 0.5f;
    int nextNote = 0;
    private bool thisIsTheHighestActivateTimer = false;


    // Update is called once per frame
    void Update()
    {
        musicNoteInputRaw = imitoneVoiceInterpreter.note_st % 12; //which note is being played, modulo 12 to get the remainder
        if(Mathf.Abs(musicNoteInputRaw - fundamentalNote) < 1.5 +- _constWiggleRoom)
        {
            musicNoteInput = fundamentalNote;
        }
        else if (Mathf.Abs(musicNoteInputRaw - fundamentalNote + 6) < 0.5 +- _constWiggleRoom)
        {
            musicNoteInput = fundamentalNote + 5; // or 7, depending...
        }
        else
        {
            musicNoteInput = musicNoteInputRaw;
        }
        
        float _noteTrackerThreshold;

        
        //Now let's turn this into usable note controls, starting by evening out the chaoatic input stream.
        //First, set the threshold used below. The intention of this is to make sure that as soon as toneActive becomes true, we have a note that we can use.

        if(imitoneVoiceInterpreter.toneActive) //if tone is active (i.e. the player is singing a note)
        {
            _noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold1; //the noteTrackethreshold is set to positiveActiveThreshold so that it changes the note after X amount of time singing in that note
        }
        else
        {
            _noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold1 / 4; //if there is no note, then we need to quickly select the note in quarter of the time.
        }

        if(imitoneVoiceInterpreter.imitoneActive) //whenever imitone gets any input,
        {
            foreach (var note in NoteTracker) //for each of the 12 notes in the NoteTracker dictionary
            {
                if (Mathf.Round(musicNoteInput) == note.Key) //round our input value to the closest integer and find the note that is closest to the music note input and return the Key of that note.
                {
                    //add the time that the note has been active to track how long the note has been active.
                    NoteTracker[note.Key] = (NoteTracker[note.Key].ActiveTimer + Time.deltaTime, NoteTracker[note.Key].Active, NoteTracker[note.Key].ChangeFundamentalTimer); 
                    //when the note has been active for a longer than the Threshold,
                    if (NoteTracker[note.Key].ActiveTimer >= _noteTrackerThreshold)
                    {
                        if (thisIsTheHighestActivateTimer)
                        {
                            nextNote = note.Key;
                        }
                    }
                }
                //if the tone is active and the next note is the same as the note in the dictionary
                if(imitoneVoiceInterpreter.toneActive && nextNote == note.Key) 
                {
                    //set the note to active and deactivate other notes in the dictionary
                    var currentValueKey = NoteTracker[note.Key];
                    NoteTracker[note.Key] = (currentValueKey.ActiveTimer, true, currentValueKey.ChangeFundamentalTimer);
                    DeactivateOtherNotesInThisDictionary();
                    SetOtherActivateTimersToZero();

                    if(NoteTracker[note.Key].ActiveTimer >= _noteTrackerThreshold)
                    {
                        //add the time that the note has been active to track how long the note has been active
                        NoteTracker[note.Key] = (currentValueKey.ActiveTimer, currentValueKey.Active, currentValueKey.ChangeFundamentalTimer + Time.deltaTime);
                    }
                }
                else if(!imitoneVoiceInterpreter.toneActiveConfident)
                {
                    //if the tone is not active, reset the active timer and set the note to inactive
                    var currentValueKey = NoteTracker[note.Key];
                    NoteTracker[note.Key] = (0, false, currentValueKey.ChangeFundamentalTimer);
                }
            }
        }

        //This function deactivates all other notes in the dictionary
        void DeactivateOtherNotesInThisDictionary()
        {
            foreach (var note in NoteTracker)
            {
                if (note.Key != nextNote)
                {
                    var currentValueKey = NoteTracker[note.Key];
                    NoteTracker[note.Key] = (currentValueKey.ActiveTimer, false, currentValueKey.ChangeFundamentalTimer);
                }
            }
        }
        //This function sets all other notes in the dictionary ActiveTimer to zero
        void SetOtherActivateTimersToZero()
        {
            foreach (var note in NoteTracker)
            {
                if (note.Key != nextNote)
                {
                    var currentValueKey = NoteTracker[note.Key];
                    NoteTracker[note.Key] = (currentValueKey.ActiveTimer, false, currentValueKey.ChangeFundamentalTimer);
                }
            }
        }
        checkHarmonyProgram();
    }


    void checkHarmonyProgram()
    {

    }
}
