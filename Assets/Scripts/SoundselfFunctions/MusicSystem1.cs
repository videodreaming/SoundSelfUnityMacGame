using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MusicSystem1 : MonoBehaviour
{
    // Setting up our variables
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    private int fundamentalNote;
    private Dictionary<int, (float ActiveTimer, bool Active, float ChangeFundamentalTimer)> NoteTracker = new Dictionary<int, (float ActiveTimer, bool Active, float ChangeFundamentalTimer)>();
    private bool musicToneActive;
    private float musicNoteInputRaw;
    private float musicNoteInput;
    private float _constWiggleRoom = 0.0f;
    private int nextNote = 0;
    private bool thisIsTheHighestActivateTimer = true;

    void Start()
    {
        // Initialize the NoteTracker dictionary with 12 keys for each note
        for (int i = 0; i < 12; i++)
        {
            NoteTracker.Add(i, (0f, false, 0f));
        }
    }

    void Update()
    {
        musicNoteInputRaw = imitoneVoiceInterpreter.note_st % 12; // Modulo 12 to get the remainder 
        if (Mathf.Abs(musicNoteInputRaw - fundamentalNote) < 1.5 +- _constWiggleRoom)
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

        float noteTrackerThreshold;

        // Determine the threshold based on whether tone is active
        if (imitoneVoiceInterpreter.toneActive)
        {
            noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold1;
        }
        else
        {
            noteTrackerThreshold = imitoneVoiceInterpreter.positiveActiveThreshold1 / 4;
        }

        var updates = new Dictionary<int, (float, bool, float)>();
        var activations = new Dictionary<int, bool>();

        if (imitoneVoiceInterpreter.imitoneActive)
        {
            foreach (var note in NoteTracker)
            {
                float newActiveTimer = note.Value.ActiveTimer;
                bool isActive = note.Value.Active;

                if (Mathf.Round(musicNoteInput) == note.Key)
                {
                    newActiveTimer += Time.deltaTime;
                    if (newActiveTimer >= noteTrackerThreshold && thisIsTheHighestActivateTimer)
                    {
                        nextNote = note.Key;
                        isActive = true;
                    }
                    updates[note.Key] = (newActiveTimer, isActive, note.Value.ChangeFundamentalTimer);
                    activations[note.Key] = isActive;
                }
                else if (!imitoneVoiceInterpreter.toneActiveConfident)
                {
                    updates[note.Key] = (0, false, note.Value.ChangeFundamentalTimer);
                }
            }

            // Apply updates to NoteTracker
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

    private void DeactivateOtherNotes(Dictionary<int, bool> activations)
    {
        foreach (var note in activations)
        {
            if (note.Key != nextNote && note.Value == true)
            {
                var currentValue = NoteTracker[note.Key];
                Debug.Log(currentValue);
                NoteTracker[note.Key] = (currentValue.ActiveTimer, false, currentValue.ChangeFundamentalTimer);
            }
        }
    }
}