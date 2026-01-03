using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Networking;
using ConversionUtilities;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;

[System.Serializable]
public class ClipSlot
{
    // In-memory clip (we null this after persisting to disk)
    public AudioClip clip;
    // Note name this clip belongs to
    public NoteName fundamental;
    // ISO timestamp for ordering/debugging
    public string createdAtIso;   // Unity-serializable
    // Duration in seconds
    public float duration;
    // Flag to delete after playback completes
    public bool markedForDeletion;
    // Optional scoring hook
    public int score;
    // Absolute path to the saved wav file
    public string filePath;

    // Convenience: slot is empty if nothing in memory and no file path
    public bool IsEmpty => clip == null && string.IsNullOrEmpty(filePath);
}

/// <summary>
/// Manages microphone recording, on-disk storage, slot rotation, and playback of
/// user-generated clips, keyed by fundamental note. Designed to run
/// continuously: record when permitted, save to disk, and replay available clips.
/// </summary>
public class RecordedAudioPlaybackTest : MonoBehaviour
{

    // Singleton for easy access from other systems
    public static RecordedAudioPlaybackTest Instance {get; private set;}
    
    [Header("Core References")]
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; // detects tone activity/pitch
    public Director director; // drives experience state
    public DevelopmentMode developmentMode; // feature flags/dev controls
    public MusicSystem1 musicSystem1; // supplies current fundamental note
    public AudioSource ThisObjectAudioSource; // holds live mic recording buffer
    public RespirationTracker respirationTracker; // gates recording on player breath
    
    [Header("Controls")]

    [SerializeField] public bool recordMode = false; // when true, recording loop runs
    [SerializeField] public bool playMode = false;   // when true, playback loop runs
    private bool playbackLoopGuard = false;          // prevents concurrent playback coroutines
    private bool recordingLoopGuard = false;         // prevents concurrent recording coroutines
    
    [Header("Audio Devices")]

    [SerializeField] private AudioSource playbackSource; // dedicated playback source
    private string deviceName;                           // selected microphone name
    private Coroutine playbackRoutine;                   // running playback coroutine
    public List<List<ClipSlot>> clipSlots = new List<List<ClipSlot>>(); // master slots per note
    
    [Header("Audio Storage")]
    [SerializeField] private string recordingsRootFolder = "RecordedClips";
    [SerializeField] private bool createNewSessionFolderEachRun = true;
    private string currentPlayingFilePath = null;             // path currently playing
    private readonly List<string> pendingDeletePaths = new List<string>(); // files to delete when safe
    private string currentSessionFolder;                      // active session folder
    
    [Header("Clip Data")]
    private readonly Dictionary<NoteName, int> pendingFillTarget = new Dictionary<NoteName, int>(); // HOLD -> target mapping
    private NoteName _activeRecordingFundamental; // note captured at recording start
    private bool _hasActiveRecordingFundamental = false;
    private AudioClip currentMicClip; // raw mic buffer (trimmed later)
    private string[] noteNames = { "C", "Cs", "D", "Ds", "E", "F", "Fs", "G", "Gs", "A", "As", "B" };
    public string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // session stamp

    [Header("Recording Settings")]
    private int maxNotes = 12; 
    private float _recordingDurationTarget = 12f; // record this long, then wait for rest
    private int _recordingDurationBuffer = 35;    // max capture window before forced stop

    private int maxClipsPerNote = 4;
    private const int MAIN_CAPACITY = 3;      // slots 0..2
    private const int HOLD_SLOT = 3;          // slot 3 (the “4th” holding zone)


    
    /// <summary>
    /// Initializes singleton, ensures audio sources, prepares clip slots, and creates session folders.
    /// </summary>
    private void Awake()
    {
        // Basic singleton guard
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure we have an AudioSource to hold the mic buffer
        if(ThisObjectAudioSource == null)
        {
            ThisObjectAudioSource = GetComponent<AudioSource>();
        }

        // Create a dedicated playback AudioSource if not assigned
        if (playbackSource == null)
        {
            playbackSource = gameObject.AddComponent<AudioSource>();
            playbackSource.playOnAwake = false;

            // Route playback through the same mixer group as the recorder if present
            if (ThisObjectAudioSource != null)
                playbackSource.outputAudioMixerGroup = ThisObjectAudioSource.outputAudioMixerGroup;
        }

        // Warn if recording and playback accidentally share the same source
        if (ThisObjectAudioSource == playbackSource)
        {
            Debug.LogWarning("Recording/Playback are sharing the same AudioSource. Assign a separate playbackSource to avoid conflicts.");
        }

        // Prepare slot structure: 12 notes * 4 slots each (0-2 main, 3 HOLD)
        clipSlots.Clear();
        for (int i = 0; i < noteNames.Length; i++)
        {
           var slotsForNote = new List<ClipSlot>(maxClipsPerNote);
            for (int j = 0; j < maxClipsPerNote; j++)
            {
                slotsForNote.Add(new ClipSlot()); // placeholder objects
            }
            clipSlots.Add(slotsForNote);
        }

        InitRecordingFolders();
    }

    /// <summary>
    /// Per-frame maintenance: delete safely-queued files, then spin up recording/playback loops when enabled.
    /// </summary>
    void Update()
    {
        // Clean up any files/slots that became deletable since last frame
        ProcessPendingDeletions();

        // Recording loop: only start if allowed and no coroutine is already running
        if(recordMode)
        {
            if(!TestForFailure() && !recordingLoopGuard)
            {  
                StartRecordingLoop();
            }
        }
        // Playback loop: only start one coroutine at a time
        if(playMode)
        {
            if(!playbackLoopGuard)
            {
                PlaybackUpdate();
            }
        }
    }

    
    //===================================================
    //STORAGE MANAGEMENT
    //===================================================
    /// <summary>
    /// Sets up the session folder structure under persistentDataPath and one subfolder per note.
    /// </summary>
    private void InitRecordingFolders()
    {
        string rootFolderPath = Path.Combine(Application.persistentDataPath, recordingsRootFolder);

        if (createNewSessionFolderEachRun)
        {
            timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            currentSessionFolder = Path.Combine(rootFolderPath, timestamp);
        }
        else
        {
            currentSessionFolder = rootFolderPath;
        }

        Directory.CreateDirectory(currentSessionFolder);

        foreach(var note in noteNames)
        {
            Directory.CreateDirectory(Path.Combine(currentSessionFolder, note));
        }

        Debug.Log($"Recording: Initialized recording folders at {currentSessionFolder}");

        if(!Directory.Exists(currentSessionFolder))
        {
            Debug.LogError($"Recording: Failed to create recording folder at {currentSessionFolder}");
        }
    }

    [ContextMenu("Open Recording Folder")]
    /// <summary>
    /// Opens the current session folder in the OS file browser.
    /// </summary>
    private void OpenRecordingFolder()
    {
        if (Directory.Exists(currentSessionFolder))
        {
            Application.OpenURL(currentSessionFolder);
        }
        else
        {
            Debug.LogWarning("Recording: Current session folder does not exist.");
        }
    }

    /// <summary>
    /// Public entry to wipe all recordings for the current session.
    /// </summary>
    public void DeleteAllRecordings()
    {
        StartCoroutine(DeleteAllRecordingsCoroutine());
    }

    /// <summary>
    /// Coroutine that stops playback, clears slot state, then deletes the session folder.
    /// </summary>
    private IEnumerator DeleteAllRecordingsCoroutine()
    {
        // Stop playback immediately
        if (playbackSource != null)
            playbackSource.Stop();

        currentPlayingFilePath = null;

        // Clear any pending deletes/transfers so they don't re-delete later
        pendingDeletePaths.Clear();
        pendingFillTarget.Clear();

        // Optional: clear in-memory slots too
        for (int n = 0; n < clipSlots.Count; n++)
            for (int s = 0; s < clipSlots[n].Count; s++)
                clipSlots[n][s] = new ClipSlot();

        // Wait a frame so audio system settles
        yield return null;

        if (!string.IsNullOrEmpty(currentSessionFolder) && Directory.Exists(currentSessionFolder))
        {
            Directory.Delete(currentSessionFolder, true);
            Debug.Log($"Recording: Deleted all recordings in {currentSessionFolder}");
        }
        else
        {
            Debug.LogWarning("Recording: Current session folder does not exist.");
        }
    }





    //=======================================================================================================
    //RECORDING LOGIC
    //=======================================================================================================
    
    //When recordMode is true, we are basically always recording (or looking for an opportunity to record) the user's voice. 
    //Some of those recordings are viable, some are thrown away mid-recording (i.e. if we change fundamental mid-recording, or if there is not enough player input)
    //At the end of the first recording for a note, we turn the replay system on for that note.
    //When a recording ends, we start a new one, as soon as the next tone starts.

    //TODO: Look for and secure memory leaks in recording.
    
    // Kick off the coroutine that waits for tone activity and captures audio
    /// <summary>
    /// Starts the recording coroutine if a microphone device is available.
    /// </summary>
    private void StartRecordingLoop()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0]; // Use the first microphone device
            Debug.Log("Recording: Recording Loop starting...");
            StartCoroutine(RecordingCoroutine());
        }
        else
        {
            Debug.LogWarning("Recording: No microphone detected!");
        }
    }

     //A coroutine that is used to control audio recording - it records for a set amount of time and then stops recording
    // Recording loop:
    // 1) Wait for tone activity
    // 2) Capture fundamental at start, start mic recording
    // 3) Record for target duration
    // 4) Wait for rest (or timeout) then trim and save
    /// <summary>
    /// Waits for tone onset, records for a target window, waits for rest/timeout, then trims and saves.
    /// </summary>
    private IEnumerator RecordingCoroutine()
    {
        if (recordingLoopGuard)
        {
            Debug.LogWarning("Recording: Recording coroutine loop already running, skipping...");
            yield break;
        }
        SetRecordingLoopGuard (true);

        // Step 1: wait until user is toning
        Debug.Log("Recording: Coroutine start, Waiting for moment to record...");
        while(imitoneVoiceInterpreter.toneActive == false)
        {
            if(TestForFailure())
            {
                StopAndDeleteRecording();
                yield break;
            }
            yield return null;
        }

        // Step 2: capture fundamental at start and begin mic capture
        if (!NoteUtils.TryIntToNote(musicSystem1.fundamentalNote, out var fundamentalAtRecordingStart))
        {
            Debug.LogWarning($"Recording: Invalid fundamental note int at start: {musicSystem1.fundamentalNote}");
            SetRecordingLoopGuard(false);
            yield break;
        }

        StartRecording(fundamentalAtRecordingStart);

        // Step 3: record for the target window
        float _t = 0.0f;
        while (_t < _recordingDurationTarget)
        {
            _t += Time.deltaTime;
            if(TestForFailure())
            {
                StopAndDeleteRecording();
                yield break;
            }
            yield return null;
        }

        // Step 4: wait for the user to stop toning or timeout
        Debug.Log("Recording: Now wait for breath to stop recording or buffer to run out...");
        _t = 0.0f;
        float timeout = Mathf.Max((_recordingDurationBuffer - _recordingDurationTarget), 0f);
        while (imitoneVoiceInterpreter._tThisRest < 0.5f && _t < timeout)
        {
            if (TestForFailure())
            {
                StopAndDeleteRecording();
                yield break;
            }

            _t += Time.deltaTime;
            yield return null;
        }

        // Trim and persist
        StopAndSaveRecording();
    } 

    // Stop mic capture, trim to the recorded length, then slot/save the clip.
    /// <summary>
    /// Stops mic capture, trims to recorded length, and persists via slotting rules.
    /// </summary>
    private void StopAndSaveRecording()
    {
        Debug.Log("Recording: Stopping and saving the current recording.");

        // If we somehow lost the starting fundamental, discard this take.
        if (!_hasActiveRecordingFundamental)
        {
            Debug.LogWarning("Recording: No active fundamental captured for this recording. Deleting instead.");
            StopAndDeleteRecording();
            return;
        }

        NoteName noteToSave = _activeRecordingFundamental;
        _hasActiveRecordingFundamental = false;
        SetRecordingLoopGuard(false);

        AudioClip trimmed = StopRecordingAndGetTrimmedClip();
        
        if (trimmed == null)
        {
            Debug.LogWarning("Recording: trimmed clip is null in StopAndSaveRecording");
            return;
        }

        SaveRecording(trimmed, noteToSave /*, score */);
    }

     // Slot selection and persistence logic for a recorded clip.
     // Rules:
     // 1) If a main slot is pending replacement, overwrite HOLD and keep waiting.
     // 2) Otherwise, place in the first empty main slot (0-2).
     // 3) If full, overwrite the oldest main slot that is NOT currently playing.
     // 4) If the oldest is playing, mark it for deletion, save to HOLD, and queue transfer.
    /// <summary>
    /// Applies slotting rules (main/HOLD) for the clip and writes it to disk, freeing in-memory audio.
    /// </summary>
    private void SaveRecording(AudioClip recordedClip, NoteName note, int score = 0)
    {
        if (recordedClip == null)
        {
            Debug.LogWarning("Recording: recordedClip is null, not saving.");
            return;
        }
        if (string.IsNullOrEmpty(currentSessionFolder))
        {
            Debug.LogWarning("Recording: currentSessionFolder not set. Did you call InitRecordingFolders()?");
            return;
        }

        // CHOOSE THE RIGHT SLOT 
        var slotsForNote = clipSlots[(int)note];

        // RULE 4: If we are already waiting for a main slot to free, overwrite HOLD and keep waiting.
        if (pendingFillTarget.ContainsKey(note))
        {
            // Overwrite HOLD (slot 3)
            var hold = slotsForNote[HOLD_SLOT];
            DeleteFileIfExists(hold.filePath);

            string holdPath = MakeWavPath(note, HOLD_SLOT, isHold: true);
            SavWav.Save(holdPath, recordedClip);

            float duration = recordedClip.length;

            slotsForNote[HOLD_SLOT] = new ClipSlot
            {
                clip = null, // free in-memory copy; playback uses disk
                fundamental = note,
                createdAtIso = DateTime.Now.ToString("o"),
                duration = duration,
                markedForDeletion = false,
                score = score,
                filePath = holdPath
            };

            Debug.Log($"Recording: HOLD overwrite for {note} -> {holdPath} (waiting for slot {pendingFillTarget[note]})");
            Destroy(recordedClip);
            return;
        }

        // RULE 1: Search empty among main slots 0..2
        int emptyMain = FindEmptyMainSlot(slotsForNote);
        if (emptyMain != -1)
        {
            string newPath = MakeWavPath(note, emptyMain, isHold: false);
            SavWav.Save(newPath, recordedClip);

            float duration = recordedClip.length;

            slotsForNote[emptyMain] = new ClipSlot
            {
                clip = null, // avoid keeping trimmed clip in memory
                fundamental = note,
                createdAtIso = DateTime.Now.ToString("o"),
                duration = duration,
                markedForDeletion = false,
                score = score,
                filePath = newPath
            };

            Debug.Log($"Recording: Saved {note} to main slot {emptyMain} -> {newPath}");
            Destroy(recordedClip);
            return;
        }

        // RULE 2/3: No empty main slots, target oldest main slot
        int oldest = FindOldestMainSlot(slotsForNote);
        var oldSlot = slotsForNote[oldest];

        // If it isn't playing, delete and write into that slot (RULE 2)
        if (!string.IsNullOrEmpty(oldSlot.filePath) && !IsFileCurrentlyPlaying(oldSlot.filePath))
        {
            DeleteFileIfExists(oldSlot.filePath);

            string newPath = MakeWavPath(note, oldest, isHold: false);
            SavWav.Save(newPath, recordedClip);

            float duration = recordedClip.length;

            slotsForNote[oldest] = new ClipSlot
            {
                clip = null,
                fundamental = note,
                createdAtIso = DateTime.Now.ToString("o"),
                duration = duration,
                markedForDeletion = false,
                score = score,
                filePath = newPath
            };

            Debug.Log($"Recording: Overwrote oldest main slot {oldest} for {note} -> {newPath}");
            Destroy(recordedClip);
            return;
        }

        // Otherwise it's playing (RULE 3): mark + queue deletion, save into HOLD, and remember target slot.
        if (!string.IsNullOrEmpty(oldSlot.filePath))
        {
            oldSlot.markedForDeletion = true;
            if (!pendingDeletePaths.Contains(oldSlot.filePath))
                pendingDeletePaths.Add(oldSlot.filePath);
        }

        pendingFillTarget[note] = oldest;

        // Write to HOLD slot 3 (temporary holding)
        var holdSlot = slotsForNote[HOLD_SLOT];
        DeleteFileIfExists(holdSlot.filePath);

        string holdPath2 = MakeWavPath(note, HOLD_SLOT, isHold: true);
        SavWav.Save(holdPath2, recordedClip);

        float durationHold = recordedClip.length;

        slotsForNote[HOLD_SLOT] = new ClipSlot
        {
            clip = null,
            fundamental = note,
            createdAtIso = DateTime.Now.ToString("o"),
            duration = durationHold,
            markedForDeletion = false,
            score = score,
            filePath = holdPath2
        };

        Debug.Log($"Recording: {note} oldest slot {oldest} is playing. Saved to HOLD -> {holdPath2} (will transfer later).");
        Destroy(recordedClip);
    }


        /*
          1. Search for empty slot amongst slot 1 2 3, save there.
          2. Failing there being an empty spot there, delete the oldest slot if it's not playing, and write there
          3. If it's playing, mark that one for deletion, then write to slot 4 (temporary holding zone), and we need to have it copy from 4 to the newly deleted space when it frees up (using a coroutine?)
          4. if 4 is already filled, and it's waiting for a slot to be deleted, then overwrite 4. 
            (Perhaps this could be really easy, as in we just have the coroutine begin when slot4 is occupied, and when the space opens up in slots 1 / 2 / 3 we move the contents of slot 4 over right away, regardless of what they are)
        */



    /// <summary>
    /// Cancels the current recording, releases buffers, and resets state without saving.
    /// </summary>
    private void StopAndDeleteRecording()
    {
        Debug.Log("Recording: Stopping and deleting the current recording.");

        SetRecordingLoopGuard(false);
        _hasActiveRecordingFundamental = false;

        AudioClip trimmed = StopRecordingAndGetTrimmedClip();

        ThisObjectAudioSource.clip = null;

        if (trimmed != null)
            Destroy(trimmed);
    }
    
    /// <summary>
    /// Begins microphone capture and tags the current fundamental for the take.
    /// </summary>
    private void StartRecording(NoteName fundamental)
    {
        
        _activeRecordingFundamental = fundamental;
        _hasActiveRecordingFundamental = true;
        Debug.Log("Recording: Begin recording on new tone...");
        currentMicClip = Microphone.Start(deviceName, false, _recordingDurationBuffer, 44100);
        ThisObjectAudioSource.clip = currentMicClip;
    }


    
    /// <summary>
    /// Stops the microphone, trims the captured buffer to the actual length, and returns a new AudioClip.
    /// </summary>
    private AudioClip StopRecordingAndGetTrimmedClip()
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            if (Microphone.devices.Length > 0) deviceName = Microphone.devices[0];
            else
            {
                Debug.LogWarning("Recording: No microphone devices found when trying to stop.");
                return null;
            }
        }

        if (!Microphone.IsRecording(deviceName))
        {
            Debug.LogWarning("Recording: Stop requested, but microphone is not recording.");
            return null;
        }

        int position = Microphone.GetPosition(deviceName); // BEFORE End
        Microphone.End(deviceName); //TODO: IMPORTANT Where does this clip get saved? Double Check
    
        // Use the mic clip we started with (do NOT trust AudioSource.clip)
        AudioClip fullClip = currentMicClip;
        if (fullClip == null || position <= 0)
        {
            Debug.LogWarning("Recording: No audio captured (mic clip null or position <= 0).");
            CleanupMicClip();
            return null;
        }

        int channels = fullClip.channels;
        float[] data = new float[position * channels];
        fullClip.GetData(data, 0);

        AudioClip trimmed = AudioClip.Create(
            fullClip.name + "_trimmed",
            position,
            channels,
            fullClip.frequency,
            false
        );

        trimmed.SetData(data, 0);

        // IMPORTANT cleanup: release the big 600s mic buffer
        CleanupMicClip();

        return trimmed;
    }

    /// <summary>
    /// Clears references to the mic clip and destroys the buffer to free memory.
    /// </summary>
    private void CleanupMicClip()
    {
        if (ThisObjectAudioSource != null) ThisObjectAudioSource.clip = null;

        if (currentMicClip != null)
        {
            Destroy(currentMicClip);
            currentMicClip = null;
        }
    }

    // Remove queued files once they are safe and promote HOLD clips into freed slots.
    /// <summary>
    /// Removes files queued for deletion when safe, and moves HOLD clips into newly freed main slots.
    /// </summary>
    private void ProcessPendingDeletions()
    {
        if (pendingDeletePaths.Count == 0) return;

        // Iterate backwards so RemoveAt is safe
        for (int i = pendingDeletePaths.Count - 1; i >= 0; i--)
        {
            string path = pendingDeletePaths[i];

            // Skip if the file is still playing
            if (IsFileCurrentlyPlaying(path))
                continue;

            DeleteFileIfExists(path);

            // Clear the slot and, if we were waiting, promote HOLD -> main slot
            if (TryFindSlotByPath(path, out var note, out var slotIndex))
            {
                ClearSlot(note, slotIndex);

                if (pendingFillTarget.TryGetValue(note, out int targetSlot) && targetSlot == slotIndex)
                {
                    var slotsForNote = clipSlots[(int)note];
                    var hold = slotsForNote[HOLD_SLOT];

                    if (!hold.IsEmpty)
                    {
                        string newPath = MakeWavPath(note, targetSlot, isHold: false);

                        if (!string.IsNullOrEmpty(hold.filePath) && File.Exists(hold.filePath))
                        {
                            try
                            {
                                if (File.Exists(newPath)) File.Delete(newPath);
                                File.Move(hold.filePath, newPath);
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"Recording: Failed to move HOLD -> main slot: {e.Message}");
                                newPath = hold.filePath; // fallback keeps HOLD filename
                            }
                        }

                        slotsForNote[targetSlot] = new ClipSlot
                        {
                            clip = hold.clip,
                            fundamental = hold.fundamental,
                            createdAtIso = hold.createdAtIso,
                            duration = hold.duration,
                            markedForDeletion = false,
                            score = hold.score,
                            filePath = newPath
                        };

                        ClearSlot(note, HOLD_SLOT);
                    }

                    pendingFillTarget.Remove(note);
                }
            }

            pendingDeletePaths.RemoveAt(i);
        }
    }


    // Guards recording against unwanted states (absorption/tone length/etc.).
    // NOTE: forceSuccess currently defaults to true, so gating is effectively disabled.
    /// <summary>
    /// Returns true if recording should abort based on absorption/rest/tone/mode checks.
    /// NOTE: forceSuccess currently defaults to true, effectively disabling the gate.
    /// </summary>
    private bool TestForFailure (bool forceSuccess = true) 
    {
        bool testAbsorption = respirationTracker._absorption > 0.1f; //ROBIN: We want to only record if player is "absorbed"
        bool testRest = imitoneVoiceInterpreter._tThisRest <= 20f; //ROBIN: We want to only record if player is consistently toning
        bool testTone = imitoneVoiceInterpreter._tThisTone <= 40f; //ROBIN: a tone longer than 40 seconds is obviously a refrigerator.
        bool testMode = recordMode; //ROBIN: We want to break recording if the recordMode turns off.
        bool testFundamental = true; //REEF: We want to break recording if the fundamental changes. 
        //NEW NOTE: WE SHOULD --NOT-- BREAK RECORDING IF THE FUNDAMENTAL CHANGE IS A PERFECT FIFTH
        //TODO: add fundamental logic

        if(forceSuccess || (testAbsorption && testRest && testTone && testFundamental && testMode))
        {
            Debug.Log("Recording: TestForFailure FALSE");
            return false;
        }
        else
        {
            Debug.Log("Recording: TestForFailure TRUE");
            return true;
        }
    }

    private void SetRecordingLoopGuard (bool value)
    {
        recordingLoopGuard = value;
        Debug.Log("Recording: recordingLoopGuard set to " + value);
    }

    //=======================================================================================================
    //PLAYBACK LOGIC
    //=======================================================================================================

    //Replay system starts when playMode = true. When playMode = false, the replay system stops.

    //Replay system starts "off" per note.
    //If there is a viable recording in the current fundamental's index, then we play it back.
    //When the playback ends, we look at the current viable recordings in this fundamental. We pick one, and play it back. EDIT FROM PREVIOUS PSEUDOCODE: maybe we don't need to just pick the most recent one, if we have three recording slots, we can just pick one of them, so there's more variety. Just an idea...

    //When the fundamental changes, fade out then stop current playback. Start playback of the new fundamental.

    //TODO: Look for and secure memory leaks in playback.

    
    private void SetPlaybackLoopGuard (bool value)
    {
        playbackLoopGuard = value;
        Debug.Log("Recording: playbackLoopGuard set to " + value);
    }
    
    /// <summary>
    /// Starts the playback loop if not already running.
    /// </summary>
    private void PlaybackUpdate()
    {
        // Ensure only one playback loop is running
        if (playbackLoopGuard) return;
        if (playbackRoutine != null) StopCoroutine(playbackRoutine);
        playbackRoutine = StartCoroutine(PlaybackLoop());
    }

    // Main playback loop: wait for an idle source, pick next wav, play, then wait for completion.
    /// <summary>
    /// Repeatedly waits for idle playback, selects the next wav, plays it, and waits for completion.
    /// </summary>
    private IEnumerator PlaybackLoop()
    {
        playbackLoopGuard = true;
        try
        {
            while (playMode) // loop while playback mode is enabled
            {
                // If something is already playing, wait for it to finish
                if (playbackSource != null && playbackSource.isPlaying)
                {
                    yield return null;
                    continue;
                }

                // Find next wav (scans notes C->B)
                string nextPath = FindFirstAvailableWav();
                if (string.IsNullOrEmpty(nextPath))
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                // Load and play
                yield return LoadAndPlayWav(nextPath);

                // Optionally wait until this clip ends before next iteration
                while (playbackSource != null && playbackSource.isPlaying)
                    yield return null;
            }
        }
        finally
        {
            playbackLoopGuard = false;
        }
    }

    /// <summary>
    /// Scans note folders (C->B) for the first non-HOLD wav and returns its path, or null if none.
    /// </summary>
    private string FindFirstAvailableWav()
    {
        if (string.IsNullOrEmpty(currentSessionFolder) || !Directory.Exists(currentSessionFolder))
            return null;

        foreach (NoteName note in Enum.GetValues(typeof(NoteName)))
        {
            string noteFolder = Path.Combine(currentSessionFolder, note.ToString());
            if (!Directory.Exists(noteFolder)) continue;

            var wavFiles = Directory.GetFiles(noteFolder, "*.wav", SearchOption.TopDirectoryOnly);
            if (wavFiles.Length == 0) continue;

            // Don’t accidentally play HOLD files
            wavFiles = Array.FindAll(wavFiles, p => !p.Contains("_HOLD", StringComparison.OrdinalIgnoreCase));
            if (wavFiles.Length == 0) continue;

            Array.Sort(wavFiles, StringComparer.OrdinalIgnoreCase);
            return wavFiles[0];
        }

        Debug.Log("PlaybackUpdate: No wav files found in any note folder (C->B).");
        return null;
    }

    // Loads a wav from disk into the playback source and starts playback.
    /// <summary>
    /// Loads a wav from disk into the playback AudioSource, disposing any prior clip, then plays it.
    /// </summary>
    private IEnumerator LoadAndPlayWav(string fullPath)
    {
        if (playbackSource == null)
        {
            Debug.LogWarning("PlaybackUpdate: playbackSource is not assigned.");
            yield break;
        }

        string uri = new Uri(fullPath).AbsoluteUri;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"PlaybackUpdate: Failed to load WAV: {req.error}\nPath: {fullPath}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);

            playbackSource.Stop();
            //TODO:
            //Right after it stops, it should check "am I marked for deletion", and if so, ()
            if(playbackSource.clip != null && playbackSource.clip != clip)
            {
                Destroy(playbackSource.clip);
            }
            playbackSource.clip = clip;
            playbackSource.Play();

            currentPlayingFilePath = fullPath;

            Debug.Log($"PlaybackUpdate: Playing {fullPath}");
        }
    }

    
    //=======================================================================================================
    //EXTERNAL CONTROL SYSTEMS
    //=======================================================================================================


    //ROBIN: See "Sequencer.cs" and "MusicSystem1.cs" and "Tutorial.cs" for where these are called. I have RecordMode turned on before Playback Mode, so we can actually start capturing recordings during the tutorial, even if we aren't playing them yet. The system will have to be able to handle, therefore, the recording system "filling up" before playback begins, but I think the way you've built it works for that.
    /// <summary>
    /// External toggle for record mode; stopping clears the current recording.
    /// </summary>
    public void SetRecordMode(bool localRecordMode) //this is now triggered in Tutorial, as well, so we can start recording a little earlier.
    {
        if (localRecordMode)
        {
            recordMode = true;
            //StartRecordingLoop();
        }
        else
        {
            StopAndDeleteRecording(); //TODO: Replace this with StopAndDeleteRecording
            recordMode = false;
        }
    }

    /// <summary>
    /// External toggle for playback mode.
    /// </summary>
    public void SetPlaybackMode(bool localPlayMode)
    {
        if (localPlayMode)
        {
            playMode = true;
        }
        else
        {
            playMode = false;
        }
    }

    //Helper Functions

    /// <summary>
    /// Returns the first empty main slot index (0-2) or -1 if none are empty.
    /// </summary>
    private int FindEmptyMainSlot(List<ClipSlot> slots)
    {
        for (int i = 0; i < MAIN_CAPACITY; i++)
        {
            if(slots[i].IsEmpty)
            {
                Debug.Log("Recording: SAVE returning empty slot " + i);
                return i;
            }
        }
        Debug.Log("Recording: SAVE no empty slots, returning -1");
        return -1;
    }

    /// <summary>
    /// Returns the main slot index (0-2) with the oldest timestamp, or 0 if none found.
    /// </summary>
    private int FindOldestMainSlot(List<ClipSlot> slots)
    {
        int best = -1;
        DateTime oldest = DateTime.MaxValue;

        for(int i = 0; i < MAIN_CAPACITY; i++)
        {
            if(slots[i].IsEmpty) continue;

            // Find the oldest slot or whatever we want to use to delete
            if(!DateTime.TryParse(slots[i].createdAtIso, out var ts))
            {
                ts = DateTime.MinValue;
            }

            if(ts<oldest)
            {
                oldest = ts;
                best = i;
            }
        }

        if(best != -1)
        {
            Debug.Log("Recording: SAVE returning oldest slot, " + best);
            return best;
        }
        else
        {
            Debug.LogWarning("Recording: Couldn't find a best (oldest) slot, returning 0 arbitrarily.");
            return 0;
        }
    }

    /// <summary>
    /// True if the playback source is currently playing the given file path.
    /// </summary>
    private bool IsFileCurrentlyPlaying(string path)
    {
        return playbackSource != null &&
            playbackSource.isPlaying &&
            !string.IsNullOrEmpty(currentPlayingFilePath) &&
            string.Equals(currentPlayingFilePath, path, StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Deletes the file at path if it exists, with error logging on failure.
    /// </summary>
    private void DeleteFileIfExists(string path)
    {
        if(string.IsNullOrEmpty(path)) return;
        if(!File.Exists(path)) return;

        try{File.Delete(path);}
        catch(Exception ex)
        {
            Debug.LogError($"Recording: Failed to delete file: {path}\nException: {ex}");
        }
    }

    /// <summary>
    /// Builds a unique wav path for a note/slot, tagging HOLD files with _HOLD.
    /// </summary>
    private string MakeWavPath(NoteName note, int slotIndex, bool isHold)
    {
        string noteFolder = Path.Combine(currentSessionFolder, note.ToString());
        Directory.CreateDirectory(noteFolder);

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        string holdTag = isHold ? "_HOLD" : "";
        return Path.Combine(noteFolder, $"{note}_{stamp}_slot{slotIndex}{holdTag}.wav");
    }

    /// <summary>
    /// Searches all slots for a matching file path; returns note and slot index if found.
    /// </summary>
    private bool TryFindSlotByPath(string path, out NoteName note, out int slotIndex)
    {
        foreach (NoteName n in Enum.GetValues(typeof(NoteName)))
        {
            var slots = clipSlots[(int)n];
            for (int i = 0; i < slots.Count; i++)
            {
                if (!string.IsNullOrEmpty(slots[i].filePath) &&
                    string.Equals(slots[i].filePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    note = n;
                    slotIndex = i;
                    return true;
                }
            }
        }

        note = default;
        slotIndex = -1;
        return false;
    }

    /// <summary>
    /// Clears a slot in the clipSlots list.
    /// </summary>
    private void ClearSlot(NoteName note, int slotIndex)
    {
        clipSlots[(int)note][slotIndex] = new ClipSlot();
    }
}

