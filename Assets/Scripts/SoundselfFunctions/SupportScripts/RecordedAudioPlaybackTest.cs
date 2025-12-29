using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Networking;
using ConversionUtilities;

public class RecordedAudioPlaybackTest : MonoBehaviour
{
    [Header("Core References")]
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; 
    public Director director; 
    public DevelopmentMode developmentMode; 
    public MusicSystem1 musicSystem1; 
    public AudioSource ThisObjectAudioSource; 
    public RespirationTracker respirationTracker; 
    private string deviceName;

    
    private int maxNotes = 12; 
    private float _recordingDurationMin = 60f;
    private int _recordingDurationBuffer = 110; // Duration of the recording in seconds, plus time for user to stop toning
    private float _fadeTime = 5f;
    private bool recordMode = false;
    private bool playMode = false;
    private bool recordingLoopGuard = false;

    private string[] noteNames = { "C", "Cs", "D", "Ds", "E", "F", "Fs", "G", "Gs", "A", "As", "B" };

    public List<List<AudioClip>> audioClips = new List<List<AudioClip>>(); 
    private int maxClipsPerNote = 3; // Maximum clips per note
    private float _recordingDurationTarget = 90f; // Duration of the recording in seconds
    
    private void Awake()
    {
        audioClips.Clear();



        int noteCount = Enum.GetValues(typeof(NoteName)).Length;
        // Initialize the audioClips list for each note
        for (int i = 0; i < noteNames.Length; i++)
        {
            List<AudioClip> clipsForNote = new List<AudioClip>(maxClipsPerNote);
            for (int j = 0; j < maxClipsPerNote; j++)
            {
                clipsForNote.Add(null); // Add placeholders for the audio clips
            }
            audioClips.Add(clipsForNote); // Add the list to the main list
        }
    }    

    //=======================================================================================================
    //RECORDING LOGIC
    //=======================================================================================================
    
    //When recordMode is true, we are basically always recording (or looking for an opportunity to record) the user's voice. 
    //Some of those recordings are viable, some are thrown away mid-recording (i.e. if we change fundamental mid-recording, or if there is not enough player input)
    //At the end of the first recording for a note, we turn the replay system on for that note.
    //When a recording ends, we start a new one, as soon as the next tone starts.
    
        // public void StartRecordingLoop()
        // {
        //     if (Microphone.devices.Length > 0)
        //     {
        //         deviceName = Microphone.devices[0]; // Use the first microphone device
        //         StartCoroutine(RecordingCoroutine());
        //         Debug.Log("Recording: Recording Loop started...");
        //     }
        //     else
        //     {
        //         Debug.LogWarning("Recording: No microphone detected!");
        //     }
        // }

    //INFO NEEDED ON HOW TO DELETE OTHER RECORDED CLIPS // ROBIN: I took no steps here, not sure if you needed something from me.
    public void SaveRecording(AudioClip recordedClip, NoteName note)
    {
        int index = (int)note;
        List<AudioClip> clipsForNote = audioClips[index];

        bool saved = false;
        for (int i = 0; i < maxClipsPerNote; i++)
        {
            if (clipsForNote[i] == null)
            {
                clipsForNote[i] = recordedClip;
                saved = true;
                Debug.Log($"Recording saved for note {note} in slot {i}.");
                break;
            }
        }

        if (!saved)
        {
            clipsForNote[0] = recordedClip; // overwrite oldest (your current behavior)
            Debug.Log($"No free slot for note {note}. Overwriting the oldest clip.");
        }
    }

    public void SaveRecording(AudioClip recordedClip, string noteName)
    {
        // Keep this only if other code still passes strings around
        if (!NoteUtils.TryParseNote(noteName, out var note))
        {
            Debug.LogWarning($"Recording: Invalid note name '{noteName}', not saving.");
            return;
        }

        SaveRecording(recordedClip, note);
    }

    
    public void StopRecording()
    {
        Debug.Log(musicSystem1.fundamentalNote);
        if (Microphone.IsRecording(null))
        {
            // Stop the microphone recording
            Microphone.End(null);
            Debug.Log("Recording: stopped.");

            // Save a reference to the current audioSource clip
            AudioClip recordedClip = ThisObjectAudioSource.clip;
        }
    }

    //A coroutine that is used to control audio recording - it records for a set amount of time and then stops recording
    private IEnumerator RecordingCoroutine()
    {
        if (recordingLoopGuard)
        {
            Debug.LogWarning("Recording: Recording loop already running, skipping...");
            yield break;
        }
        recordingLoopGuard = true;

        //Step 1: Wait For Moment To Record
        Debug.Log("Recording: Coroutine start, Waiting for moment to record...");
        while(imitoneVoiceInterpreter.toneActive == false)
        {
            yield return null;
        }

        //Step 2: Start Recording
        FadeRecordingUp();
        Debug.Log("Recording: Begin recording...");
        ThisObjectAudioSource.clip = Microphone.Start(deviceName, false, _recordingDurationBuffer, 44100); // Record for up to 10 seconds OR this should be a dynamic number based on the length of the breath.
        float _t = 0.0f;
        //wait for the "duration" amount of seconds
        while (_t < _recordingDurationTarget)
        {
            _t += Time.deltaTime;
            if(testForFailure())
            {
                break;
            }
            yield return null;
        }

        //Step 3: Wait for the user to stop toning
        Debug.Log("Recording: Now wait for breath to stop recording...");
        _t = 0.0f;
        while(imitoneVoiceInterpreter._tThisRest < 0.5f || _t < Mathf.Max((_recordingDurationBuffer - _recordingDurationTarget - _fadeTime), 0f))
        {
            if(testForFailure())
            {
                break;
            }

            _t += Time.deltaTime;
            yield return null;
        }
        while(!FadeRecordingDown()) //THIS IS DEFINITELY IMPLEMENTED WRONG, BUT PUTTING HERE AS PSEUDOCODE - robin
        {
            yield return null;
        }
        StopRecording();
        if (NoteUtils.TryIntToNote(musicSystem1.fundamentalNote, out var note))
        {
            SaveRecording(ThisObjectAudioSource.clip, note);
        }
        else
        {
            Debug.LogWarning($"Recording: Invalid fundamental note int: {musicSystem1.fundamentalNote}");
        }
        recordingLoopGuard = false;
        StartCoroutine(RecordingCoroutine());
    }

    private bool testForFailure ()
    {
        bool testAbsorption = respirationTracker._absorption > 0.1f; //ROBIN: We want to only record if player is "absorbed"
        bool testRest = imitoneVoiceInterpreter._tThisRest <= 15f; //ROBIN: We want to only record if player is consistently toning
        bool testMode = recordMode; //ROBIN: We want to break recording if the recordMode turns off.
        bool testFundamental = director.queue.Count > 0; //REEF: We want to break recording if the fundamental changes.


        if(testAbsorption && testRest && testFundamental && testMode)
        {
            return false;
        }
        else
        {
            StopRecording();
            Debug.Log("Recording: failed test, stopping recording.");
            recordingLoopGuard = false;
            StartCoroutine(RecordingCoroutine());
            return true;
        }
    }
    public void SetRecordReplayMode (bool localRecordReplayMode)
    {
        if (localRecordReplayMode)
        {
            FadeRecordingUp();
        }
        else
        {
            FadeRecordingDown();
        }
    }
    private void FadeRecordingUp() //NOT CODED YET, I'M NOT SURE HOW - Robin
    {
        Debug.Log("Recording: Fading audio up in the recording, so it doesn't hard-switch on...");
    }

    private bool FadeRecordingDown()//NOT CODED YET, I'M NOT SURE HOW - Robin
    {
        Debug.Log("Recording: Fading audio down in the recording, so it doesn't hard-switch off...");
        bool theFadeIsComplete = true;
        if(theFadeIsComplete)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //=======================================================================================================
    //PLAYBACK LOGIC
    //=======================================================================================================

    //Replay system starts when playMode = true. When playMode = false, the replay system stops.

    //Replay system starts "off" per note.
    //If there is a viable recording in the current fundamental's index, then we play it back.
    //When the playback ends, we look at the current viable recordings in this fundamental. We pick one, and play it back. EDIT FROM PREVIOUS PSEUDOCODE: maybe we don't need to just pick the most recent one, if we have three recording slots, we can just pick one of them, so there's more variety. Just an idea...

    //When the fundamental changes, fade out then stop current playback. Start playback of the new fundamental.
    
    //=======================================================================================================
    //EXTERNAL CONTROL SYSTEMS
    //=======================================================================================================


    //ROBIN: See "Sequencer.cs" and "MusicSystem1.cs" and "Tutorial.cs" for where these are called. I have RecordMode turned on before Playback Mode, so we can actually start capturing recordings during the tutorial, even if we aren't playing them yet. The system will have to be able to handle, therefore, the recording system "filling up" before playback begins, but I think the way you've built it works for that.
    // public void SetRecordMode(bool localRecordMode) //this is now triggered in Tutorial, as well, so we can start recording a little earlier.
    // {
    //     if (localRecordMode)
    //     {
    //         recordMode = true;
    //         StartRecordingLoop();
    //     }
    //     else
    //     {
    //         StopRecording();
    //         recordingLoopGuard = false;
    //         recordMode = false;
    //     }
    // }

    // public void SetPlaybackMode(bool localPlayMode)
    // {
    //     if (localPlayMode)
    //     {
    //         playMode = true;
    //     }
    //     else
    //     {
    //         playMode = false;
    //     }
    // }
}

