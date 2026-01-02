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
    public AudioClip clip;
    public NoteName fundamental;
    public string createdAtIso;   // Unity-serializable
    public float duration;
    public bool markedForDeletion;
    public int score;
    public string filePath;

    public bool IsEmpty => clip == null && string.IsNullOrEmpty(filePath);
}

public class RecordedAudioPlaybackTest : MonoBehaviour
{

    public static RecordedAudioPlaybackTest Instance {get; private set;}
    
    [Header("Core References")]
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; 
    public Director director; 
    public DevelopmentMode developmentMode; 
    public MusicSystem1 musicSystem1; 
    public AudioSource ThisObjectAudioSource; //This is the recordingsource
    public RespirationTracker respirationTracker; 
    
    [Header("Controls")]

    [SerializeField] public bool recordMode = false;
    [SerializeField] public bool playMode = false;
    private bool playbackLoopGuard = false;
    private bool recordingLoopGuard = false;
    
    [Header("Audio Devices")]

    [SerializeField] private AudioSource playbackSource; // used only for playback
    private string deviceName;
    private Coroutine playbackRoutine;
    
    
    [Header("Audio Storage")]
    [SerializeField] private string recordingsRootFolder = "RecordedClips";
    [SerializeField] private bool createNewSessionFolderEachRun = true;
    private string currentPlayingFilePath = null;
    private readonly List<string> pendingDeletePaths = new List<string>();
    private string currentSessionFolder;
    
    [Header("Clip Data")]
    private readonly Dictionary<NoteName, int> pendingFillTarget = new Dictionary<NoteName, int>();
    private NoteName _activeRecordingFundamental;
    private bool _hasActiveRecordingFundamental = false;
    private AudioClip currentMicClip;
    public string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    //clip slots is a list of lists of ClipSlot objects, organized by NoteName. So clipSlots is the master list
    public List<List<ClipSlot>> clipSlots = new List<List<ClipSlot>>();

    [Header("Recording Settings")]
    private int maxNotes = 12; 
    private float _recordingDurationTarget = 12f;// 90f; // Duration of the recording in seconds
    private int _recordingDurationBuffer = 35;//110; // Duration of the recording in seconds, plus time for user to stop toning

    private int maxClipsPerNote = 4;
    private const int MAIN_CAPACITY = 3;      // slots 0..2
    private const int HOLD_SLOT = 3;          // slot 3 (the “4th” holding zone)


    
    private void Awake()
    {

        //checks out whether this is a singleton
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if(ThisObjectAudioSource == null) //used for storing recordings
        {
            //Sets this Component as the ThisObjectAudioSource NOTE that this is used later. Remember that this is the component that is attached to the GO
            ThisObjectAudioSource = GetComponent<AudioSource>();
        }


        if (playbackSource == null) //used for storing playback
        {
            //This is adding a different Audio Source TODO : Check how necessary this really is?
            playbackSource = gameObject.AddComponent<AudioSource>();
            //Ensuring that this playbacksource does not play on awake
            playbackSource.playOnAwake = false;

            if (ThisObjectAudioSource != null)
                playbackSource.outputAudioMixerGroup = ThisObjectAudioSource.outputAudioMixerGroup;
        }

        //Ensures that these two are not the same AudioSource
        if (ThisObjectAudioSource == playbackSource)
        {
            Debug.LogWarning("Recording/Playback are sharing the same AudioSource. Assign a separate playbackSource to avoid conflicts.");
        }

        //Master list should be cleared of all lists
        clipSlots.Clear();

        // Initialize the clipSlots list for each note
        int noteCount = Enum.GetValues(typeof(NoteName)).Length;
        for (int i = 0; i < noteCount; i++)
        {
            //Create the new list
           var slotsForNote = new List<ClipSlot>(maxClipsPerNote);
            for (int j = 0; j < maxClipsPerNote; j++)
            {
                slotsForNote.Add(new ClipSlot()); // Add placeholders for the clip slots
            }
            clipSlots.Add(slotsForNote); // Add the list to the main list
        }

        InitRecordingFolders();
    }

    private void Start()
    {
        if (imitoneVoiceInterpreter == null || musicSystem1 == null || respirationTracker == null)
        {
            Debug.LogError("Recording: Missing required references! Disabling recording system.");
            enabled = false;
        }
    }
    
    private void OnDestroy()
    {
        // Stop microphone if recording
        if (!string.IsNullOrEmpty(deviceName) && Microphone.IsRecording(deviceName))
            Microphone.End(deviceName);
        
        // Stop all coroutines
        StopAllCoroutines();
        
        // Cleanup playback
        CleanupPlaybackClip();
        CleanupMicClip();
        
        // Destroy all AudioClips in slots
        foreach (var slotsForNote in clipSlots)
        {
            foreach (var slot in slotsForNote)
            {
                if (slot?.clip != null)
                    Destroy(slot.clip);
            }
        }
        clipSlots.Clear();
        
        // Clear pending state
        pendingDeletePaths.Clear();
        pendingFillTarget.Clear();
    }
    void Update()
    {
        ProcessPendingDeletions();
        if(recordMode)
        {
            if(!TestForFailure() && !recordingLoopGuard)
            {  
                StartRecordingLoop();
            }
        }
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

        foreach(var note in Enum.GetNames(typeof(NoteName)))
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

    public void DeleteAllRecordings()
    {
        StartCoroutine(DeleteAllRecordingsCoroutine());
    }

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
            {
                ClearSlot((NoteName)n, s);
            }
                

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
    
    private void StartRecordingLoop()
    {
        if (Microphone.devices.Length > 0)
        {
            deviceName = Microphone.devices[0]; // Use the first microphone device TODO Double check whether Audio clip is Microphone.devices
            Debug.Log("Recording: Recording Loop starting...");
            StartCoroutine(RecordingCoroutine());
        }
        else
        {
            Debug.LogWarning("Recording: No microphone detected!");
        }
    }

    private void StartRecording(NoteName fundamental)
        {
            
            _activeRecordingFundamental = fundamental;
            _hasActiveRecordingFundamental = true;
            Debug.Log("Recording: Begin recording on new tone...");
            currentMicClip = Microphone.Start(deviceName, false, _recordingDurationBuffer, 44100);
            //Set THIS audioSource's clip to currentMicClip so it is now stores in the ThisObjectAudioSource.clip
            ThisObjectAudioSource.clip = currentMicClip;
        }

     //A coroutine that is used to control audio recording - it records for a set amount of time and then stops recording
    private IEnumerator RecordingCoroutine()
    {
        if (recordingLoopGuard)
        {
            Debug.LogWarning("Recording: Recording coroutine loop already running, skipping...");
            yield break;
        }
        SetRecordingLoopGuard (true);

        //Step 1: Wait For Moment To Record when player starts toning
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

        // Step 2: Start Recording (capture fundamental at START)
        if (!NoteUtils.TryIntToNote(musicSystem1.fundamentalNote, out var fundamentalAtRecordingStart))
        {
            Debug.LogWarning($"Recording: Invalid fundamental note int at start: {musicSystem1.fundamentalNote}");
            SetRecordingLoopGuard(false);
            yield break;
        }

        StartRecording(fundamentalAtRecordingStart);

        float _t = 0.0f;
        //wait for the "duration" amount of seconds
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

        //Step 3: Wait for the user to stop toning
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

        
        StopAndSaveRecording();
        //StartCoroutine(RecordingCoroutine()); // COMMENTING THIS OUT BECAUSE UPDATE() WILL TAKE CARE OF IT NOW THAT THE LOOPGUARD IS OFF
    } 

   

     //INFO NEEDED ON HOW TO DELETE OTHER RECORDED CLIPS // ROBIN: I took no steps here, not sure if you needed something from me.
    


        /*
          1. Search for empty slot amongst slot 1 2 3, save there.
          2. Failing there being an empty spot there, delete the oldest slot if it's not playing, and write there
          3. If it's playing, mark that one for deletion, then write to slot 4 (temporary holding zone), and we need to have it copy from 4 to the newly deleted space when it frees up (using a coroutine?)
          4. if 4 is already filled, and it's waiting for a slot to be deleted, then overwrite 4. 
            (Perhaps this could be really easy, as in we just have the coroutine begin when slot4 is occupied, and when the space opens up in slots 1 / 2 / 3 we move the contents of slot 4 over right away, regardless of what they are)
        */



    private void StopAndSaveRecording()
    {
        //TODO: organize the location of the recording appropriately

        //HERE WE NEED SOME LOGIC FOR WHERE TO PUT THE RECORDING - BASED ON THE TONE, AND HOW MANY OTHER RECORDINGS THERE ARE THERE. WE MAY HAVE TO DELETE A RECORDING, IF NEED BE, BUT OBV. NOT IF IT'S CURRENTLY PLAYING. 
        Debug.Log("Recording: Stopping and saving the current recording at ");

        //Checks to see if there is an Active Fundamental that is being corded
        if (!_hasActiveRecordingFundamental)
        {
            //if not, then get the fuck out of this function
            Debug.LogWarning("Recording: No active fundamental captured for this recording. Deleting instead.");
            StopAndDeleteRecording();
            return;
        }

        //find the _active Recording fundamental 
        NoteName noteToSave = _activeRecordingFundamental;
        //Alright, now that we know we need to be here. Let's reset the _hasActiveRecordingFundamental to false for next time that we need to check it. 
        _hasActiveRecordingFundamental = false;
        SetRecordingLoopGuard(false);

        AudioClip trimmed = StopRecordingAndGetTrimmedClip();
        //Got the trimmed clip

        if (trimmed == null)
        {
            Debug.LogWarning("Recording: trimmed clip is null in StopAndSaveRecording");
            return;
        }

        //Now Save it!
        SaveRecording(trimmed, noteToSave /*, score */);
    }

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

        int position = Microphone.GetPosition(deviceName); // BEFORE End Position is used to check how many frames are valid in our audio recording. THEN you can go ahead and end it on the same frame so that the End matches where teh position of the microphone is.
        Microphone.End(deviceName); //TODO IMPORTANT Where does this clip get saved? Double Check
    
        // Use the mic clip we started with (do NOT trust AudioSource.clip)
        //create the AudioClip called fullclip and set is as currentMicClip which is currently //THIS AudioSource's Mic Clip.
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

        // CHOOSE THE RIGHT SLOT in the master list based on the note that is being passed into this. 
        var slotsForNote = clipSlots[(int)note];

        // RULE 4: If we are already waiting for a main slot to free, overwrite HOLD and keep waiting.
        if (pendingFillTarget.ContainsKey(note))
        {
            // Overwrite HOLD (slot 3)
            var holdSlot = slotsForNote[HOLD_SLOT];
            if (holdSlot.clip != null) Destroy(holdSlot.clip);
            DeleteFileIfExists(holdSlot.filePath);

            string holdPath = MakeWavPath(note, HOLD_SLOT, isHold: true);
            try
            {
                SavWav.Save(holdPath, recordedClip);
            }
            catch (Exception e)
            {
                Debug.LogError($"Recording: Failed to save WAV to {holdPath}: {e.Message}");
                if (recordedClip != null) Destroy(recordedClip);
                return;
            }

            slotsForNote[HOLD_SLOT] = new ClipSlot
            {
                clip = recordedClip,
                fundamental = note,
                createdAtIso = DateTime.Now.ToString("o"),
                duration = recordedClip.length,
                markedForDeletion = false,
                score = score,
                filePath = holdPath
            };

            Debug.Log($"Recording: HOLD overwrite for {note} -> {holdPath} (waiting for slot {pendingFillTarget[note]})");
            return;
        }
        //TODO: Check this
        //Robin says - it looks like this logic is unnecessary with Rule 3 below.

        // RULE 1: Search empty among main slots 0..2
        int emptyMain = FindEmptyMainSlot(slotsForNote);
        //FindEmptyMainSlot will return the index of an empty main slot or -1 if none are empty
        if (emptyMain != -1)
        {
            string newPath = MakeWavPath(note, emptyMain, isHold: false);
            try
            {
                SavWav.Save(holdPath, recordedClip);
            }
            catch (Exception e)
            {
                Debug.LogError($"Recording: Failed to save WAV to {holdPath}: {e.Message}");
                if (recordedClip != null) Destroy(recordedClip);
                return;
            }

            slotsForNote[emptyMain] = new ClipSlot
            {
                clip = recordedClip,
                fundamental = note,
                createdAtIso = DateTime.Now.ToString("o"),
                duration = recordedClip.length,
                markedForDeletion = false,
                score = score,
                filePath = newPath
            };
            Debug.Log($"Recording: Saved {note} to main slot {emptyMain} -> {newPath}");
            return;
        }

        // RULE 2/3: No empty main slots, target oldest main slot
        //FindOldestMainSlot will target the number of the oldest slot
        int oldest = FindOldestMainSlot(slotsForNote);
        //oldSlot will reference the actual ClipSlot that is in the oldest position.
        var oldSlot = slotsForNote[oldest];

        // If it isn't playing, delete and write into that slot (RULE 2)
        if (!oldSlot.IsEmpty && !IsFileCurrentlyPlaying(oldSlot.filePath))
        {

            if (oldSlot.clip != null) Destroy(oldSlot.clip);
            DeleteFileIfExists(oldSlot.filePath);

            string newPath = MakeWavPath(note, oldest, isHold: false);
            try
            {
                SavWav.Save(holdPath, recordedClip);
            }
            catch (Exception e)
            {
                Debug.LogError($"Recording: Failed to save WAV to {holdPath}: {e.Message}");
                if (recordedClip != null) Destroy(recordedClip);
                return;
            }

            slotsForNote[oldest] = new ClipSlot
            {
                clip = recordedClip,
                fundamental = note,
                createdAtIso = DateTime.Now.ToString("o"),
                duration = recordedClip.length,
                markedForDeletion = false,
                score = score,
                filePath = newPath
            };

            Debug.Log($"Recording: Overwrote oldest main slot {oldest} for {note} -> {newPath}");
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
        // Set the HoldSlot to the index in the slotsforThisSpecificNote's HOLD_SLOT which should match the regular slot index.
        var hold = slotsForNote[HOLD_SLOT];
        if (hold.clip != null) Destroy(hold.clip);
        DeleteFileIfExists(hold.filePath);


        //Create the path of where this Wav is going to be held and save it there.
        string holdPath2 = MakeWavPath(note, HOLD_SLOT, isHold: true);
        SavWav.Save(holdPath2, recordedClip);

        slotsForNote[HOLD_SLOT] = new ClipSlot
        {
            clip = recordedClip,
            fundamental = note,
            createdAtIso = DateTime.Now.ToString("o"),
            duration = recordedClip.length,
            markedForDeletion = false,
            score = score,
            filePath = holdPath2
        };

        Debug.Log($"Recording: {note} oldest slot {oldest} is playing. Saved to HOLD -> {holdPath2} (will transfer later).");
    }

    private void ProcessPendingDeletions()
    {
        //Pending Delete Paths is empty, ignore this function
        if (pendingDeletePaths.Count == 0) return;

        // Go through each pendingDeletePaths in reverse order to safely remove items while iterating
        for (int i = pendingDeletePaths.Count - 1; i >= 0; i--)
        {
            string path = pendingDeletePaths[i];

            // Check if the file is currently playing; if so, skip deletion
            if (IsFileCurrentlyPlaying(path))
                continue;

            // Delete file
            DeleteFileIfExists(path);

            // Clear the slot that referenced it
            if (TryFindSlotByPath(path, out var note, out var slotIndex))
            {
                ClearSlot(note, slotIndex);

                // If we were waiting to fill this slot, transfer from HOLD now.
                if (pendingFillTarget.TryGetValue(note, out int targetSlot) && targetSlot == slotIndex)
                {
                    var slotsForNote = clipSlots[(int)note];
                    var hold = slotsForNote[HOLD_SLOT];

                    if (!hold.IsEmpty)
                    {
                        string newPath = MakeWavPath(note, targetSlot, isHold: false);

                        // Move hold file into the main slot filename (so playback won't accidentally skip it)
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
                                // If move fails, keep it where it is (but note: it will still be tagged _HOLD)
                                newPath = hold.filePath;
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
                        hold.clip = null;
                        // Clear HOLD
                        ClearSlot(note, HOLD_SLOT);
                    }

                    pendingFillTarget.Remove(note);
                }
            }

            pendingDeletePaths.RemoveAt(i);
        }
    }

   private void CleanupMicClip()
    {
        if (ThisObjectAudioSource != null) ThisObjectAudioSource.clip = null;

        if (currentMicClip != null)
        {
            Destroy(currentMicClip);
            currentMicClip = null;
        }
    }
    ///TODO: make the forceSuccess = false
    // TODO: Test "TestForFailure" with a keyboard command or something to make sure it's working right in the logic here.
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
    private void PlaybackUpdate()
{
    // Only start once
    if (playbackRoutine != null) return;
    playbackRoutine = StartCoroutine(PlaybackLoopCoroutine());
}

    private IEnumerator PlaybackLoopCoroutine()
    {
        SetPlaybackLoopGuard(true);

        while (playMode)
        {
            if (string.IsNullOrEmpty(currentSessionFolder) || !Directory.Exists(currentSessionFolder))
            {
                Debug.LogWarning("Playback: currentSessionFolder invalid. Did you call InitRecordingFolders()?");
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 1) Follow current fundamental
            if (!NoteUtils.TryIntToNote(musicSystem1.fundamentalNote, out var currentFundamental))
            {
                yield return null;
                continue;
            }

            // 2) Pick a playable wav for that note (exclude HOLD)
            string wavPath = PickRandomMainWav(currentFundamental);
            if (string.IsNullOrEmpty(wavPath))
            {
                // Nothing recorded for this fundamental yet
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            // 3) Load + play
            yield return LoadAndPlayWav(wavPath);

            // 4) Wait until finished OR fundamental changes
            while (playMode && playbackSource != null && playbackSource.isPlaying)
            {
                if (NoteUtils.TryIntToNote(musicSystem1.fundamentalNote, out var nowFundamental) &&
                    nowFundamental != currentFundamental)
                {
                    playbackSource.Stop();
                    break;
                }
                yield return null;
            }

            // Cleanup runtime playback clip to avoid leaks
            CleanupPlaybackClip();
            yield return null;
        }

        // shutdown
        CleanupPlaybackClip();
        SetPlaybackLoopGuard(false);
        playbackRoutine = null;
    }

    private IEnumerator LoadAndPlayWav(string fullPath)
    {
        if (playbackSource == null)
        {
            Debug.LogWarning("Playback: playbackSource is not assigned.");
            yield break;
        }

        string uri = new Uri(fullPath).AbsoluteUri;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Playback: Failed to load WAV: {req.error}\nPath: {fullPath}");
                yield break;
            }

            AudioClip newClip = DownloadHandlerAudioClip.GetContent(req);

            // Stop old playback + destroy old runtime clip
            if (playbackSource.isPlaying) playbackSource.Stop();
            if (playbackSource.clip != null) Destroy(playbackSource.clip);

            playbackSource.clip = newClip;
            playbackSource.Play();

            currentPlayingFilePath = fullPath;
            Debug.Log($"Playback: Playing {fullPath}");
        }
    }

    private string PickRandomMainWav(NoteName note)
    {
        string noteFolder = Path.Combine(currentSessionFolder, note.ToString());
        if (!Directory.Exists(noteFolder)) return null;

        var wavFiles = Directory.GetFiles(noteFolder, "*.wav", SearchOption.TopDirectoryOnly);
        wavFiles = Array.FindAll(wavFiles, p => !p.Contains("_HOLD", StringComparison.OrdinalIgnoreCase));
        if (wavFiles.Length == 0) return null;

        return wavFiles[UnityEngine.Random.Range(0, wavFiles.Length)];
    }

    private void CleanupPlaybackClip()
    {
        if (playbackSource == null) return;

        playbackSource.Stop();
        currentPlayingFilePath = null;

        if (playbackSource.clip != null)
        {
            Destroy(playbackSource.clip);
            playbackSource.clip = null;
        }
    }

    
    //=======================================================================================================
    //EXTERNAL CONTROL SYSTEMS
    //=======================================================================================================


    //ROBIN: See "Sequencer.cs" and "MusicSystem1.cs" and "Tutorial.cs" for where these are called. I have RecordMode turned on before Playback Mode, so we can actually start capturing recordings during the tutorial, even if we aren't playing them yet. The system will have to be able to handle, therefore, the recording system "filling up" before playback begins, but I think the way you've built it works for that.
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

    private bool IsFileCurrentlyPlaying(string path)
    {
        return playbackSource != null &&
            playbackSource.isPlaying &&
            !string.IsNullOrEmpty(currentPlayingFilePath) &&
            string.Equals(currentPlayingFilePath, path, StringComparison.OrdinalIgnoreCase);
    }


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

    private string MakeWavPath(NoteName note, int slotIndex, bool isHold)
    {
        string noteFolder = Path.Combine(currentSessionFolder, note.ToString());
        Directory.CreateDirectory(noteFolder);

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
        string holdTag = isHold ? "_HOLD" : "";
        return Path.Combine(noteFolder, $"{note}_{stamp}_slot{slotIndex}{holdTag}.wav");
    }

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

    private void ClearSlot(NoteName note, int slotIndex)
    {
        var slot = clipSlots[(int)note][slotIndex];
        if (slot != null && slot.clip != null)
        {
            // Don't destroy the playbackSource clip (separate runtime clip)
            if (playbackSource == null || playbackSource.clip != slot.clip)
                Destroy(slot.clip);
        }

        clipSlots[(int)note][slotIndex] = new ClipSlot();
    }
}

