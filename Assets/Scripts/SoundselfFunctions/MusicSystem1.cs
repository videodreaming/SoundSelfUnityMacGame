using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicSystem1 : MonoBehaviour
{

    //setting up our variables
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
        musicNoteInputRaw = imitoneVoiceInterpreter.note_st % 12; //which note?
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

        if(imitoneVoiceInterpreter.toneActive)
        {
            //Choose one of these below, depending on if our music system is using toneActive or toneActiveConfident. We may want to change that in future.
            //toneActiveConfident is is higher latency, higher accuracy
            _noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold1; //for toneActive
            //_noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold2; //for toneActiveConfident
        }
        else
        {
            _noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold1 / 4; //for toneActive
            //_noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold2 / 4; //for toneActiveConfident
        }

        if(imitoneVoiceInterpreter.imitoneActive)
        {
            foreach (var note in NoteTracker)
            {
                if (Mathf.Round(musicNoteInput) == note.Key) //finding the dictionary key, i.e. one of the 12 notes
                {
                     NoteTracker[note.Key] = (NoteTracker[note.Key].ActiveTimer + Time.deltaTime, NoteTracker[note.Key].Active, NoteTracker[note.Key].ChangeFundamentalTimer);
                    if (NoteTracker[note.Key].ActiveTimer >= _noteTrackerThreshold)
                    {
                        if (thisIsTheHighestActivateTimer)
                        {
                            nextNote = note.Key;
                        }
                    }
                }
                if(imitoneVoiceInterpreter.toneActive && nextNote == note.Key) // we may want this to use toneActiveConfident instead... will take experimentation. toneActiveConfident is is higher latency, higher accuracy
                {
                    var currentValueKey = NoteTracker[note.Key];
                    NoteTracker[note.Key] = (currentValueKey.ActiveTimer, true, currentValueKey.ChangeFundamentalTimer);
                    DeactivateOtherNotesInThisDictionary();
                    SetOtherActivateTimersToZero();

                    if(NoteTracker[note.Key].ActiveTimer >= _noteTrackerThreshold)
                    {
                        NoteTracker[note.Key] = (currentValueKey.ActiveTimer, currentValueKey.Active, currentValueKey.ChangeFundamentalTimer + Time.deltaTime);
                    }
                }
                else if(!imitoneVoiceInterpreter.toneActiveConfident)
                {
                    var currentValueKey = NoteTracker[note.Key];
                    NoteTracker[note.Key] = (0, false, currentValueKey.ChangeFundamentalTimer);
                    // NoteTracker[note.Key].ActiveTimer = 0;
                    // NoteTracker[note.Key].Active = false;
                }
            }
        }

        void DeactivateOtherNotesInThisDictionary()
        {
            foreach (var note in NoteTracker)
            {
                if (note.Key != nextNote)
                {
                    var currentValueKey = NoteTracker[note.Key];
                    NoteTracker[note.Key] = (currentValueKey.ActiveTimer, false, currentValueKey.ChangeFundamentalTimer);
                    //NoteTracker[note.Key].Active = false;
                }
            }
        }
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
    }
}
