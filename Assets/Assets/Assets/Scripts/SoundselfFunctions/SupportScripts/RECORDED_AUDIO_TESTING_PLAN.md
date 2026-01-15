# RecordedAudioPlaybackTest - Testing Plan

## Overview
This script manages microphone recording, on-disk storage, slot rotation, and playback of user-generated audio clips keyed by fundamental note. The system records voice input, saves clips to disk, manages a slot-based rotation system (3 main slots + 1 HOLD slot per note), and plays back recordings.

---

## Component Summary

### 1. **Initialization & Storage Management**
   - Singleton setup
   - AudioSource initialization (recording vs playback)
   - Folder structure creation (`InitRecordingFolders`)
   - Clip slot initialization (12 notes × 4 slots each)
   - Session folder management

### 2. **Recording System**
   - **StartRecordingLoop**: Checks mic availability, starts recording coroutine
   - **RecordingCoroutine**: Main recording flow:
     - Waits for tone activity
     - Captures fundamental at start
     - Records for target duration (12s)
     - Waits for rest/timeout
     - Trims and saves
   - **StartRecording**: Begins mic capture
   - **StopRecordingAndGetTrimmedClip**: Stops mic, trims buffer
   - **StopAndSaveRecording**: Saves to slot system
   - **StopAndDeleteRecording**: Cancels recording without saving **NEED TO TEST**
   - **TestForFailure**: Guards against unwanted states (currently disabled)

### 3. **Slot Management System**
   - **SaveRecording**: Complex slotting logic:
     - Rule 1: Fill empty main slot (0-2)
     - Rule 2: Overwrite oldest non-playing main slot
     - Rule 3: If oldest is playing, mark for deletion, save to HOLD
     - Rule 4: If waiting for slot, overwrite HOLD
   - **FindEmptyMainSlot**: Finds first empty slot (0-2)
   - **FindOldestMainSlot**: Finds oldest slot by timestamp
   - **ProcessPendingDeletions**: Deletes safe files, promotes HOLD→main

### 4. **Playback System**
   - **PlaybackUpdate**: Starts playback loop
   - **PlaybackLoop**: Main playback coroutine:
     - Waits for idle source
     - Finds next available WAV
     - Loads and plays
     - Waits for completion
   - **FindFirstAvailableWav**: Scans note folders for oldest WAV
   - **LoadAndPlayWav**: Loads WAV from disk, plays it
   - **IsFileCurrentlyPlaying**: Checks if file is active

### 5. **File Management**
   - **MakeWavPath**: Generates unique WAV paths
   - **DeleteFileIfExists**: Safe file deletion
   - **TryFindSlotByPath**: Locates slot by file path
   - **ClearSlot**: Clears a slot
   - **DeleteAllRecordings**: Wipes session folder

### 6. **External Controls**
   - **SetRecordMode**: Toggles recording mode
   - **SetPlaybackMode**: Toggles playback mode

---

## Testing Protocol

### Test Setup Requirements
1. **Temporarily disable dependencies** for isolated testing:
   - Set `TestForFailure()` to always return `false` (already done via `forceSuccess = true`)
   - Mock or bypass `imitoneVoiceInterpreter.toneActive` for manual control
   - Mock `musicSystem1.fundamentalNote` for controlled note testing
   - Consider bypassing `respirationTracker` checks

2. **Add keyboard commands** to `InputReferences.cs` (see below)

3. **Enable detailed logging** at key decision points

---

## Test Cases

### TEST 1: Basic Recording & Saving // CHECK
**Goal**: Verify a single recording can be captured and saved to disk.

**Setup**:
- Disable playback mode 
- Set `recordMode = true` manually or via keyboard
- Mock `toneActive = true` or use real mic input

**Keyboard Commands**:
- `R` - Toggle record mode
- `T` - Simulate tone active (for testing without mic)

**Expected Behavior**:
- Recording starts when tone detected
- Records for ~12 seconds
- Saves to first empty main slot (slot 0)
- File appears in correct note folder

**Logs to Add**:
```csharp
// In StartRecording:
Debug.Log($"[TEST] StartRecording: fundamental={fundamental}, device={deviceName}");

// In StopAndSaveRecording:
Debug.Log($"[TEST] StopAndSaveRecording: note={noteToSave}, trimmed length={trimmed.length}");

// In SaveRecording:
Debug.Log($"[TEST] SaveRecording: note={note}, slot={slotIndex}, path={filePath}");
```

**Validation**:
- Check file exists in `Application.persistentDataPath/RecordedClips/[timestamp]/[NoteName]/`
- Verify file is playable
- Check slot 0 is populated in `clipSlots`

---

### TEST 2: Multiple Recordings - Sequential Fill // CHECKED
**Goal**: Verify multiple recordings fill slots 0, 1, 2 in order.

**Setup**:
- Start with empty slots
- Record 3 clips for same note

**Keyboard Commands**:
- `R` - Toggle record mode
- `1` - Force fundamental to C (note 0)
- `2` - Force fundamental to D (note 2)
- `3` - Force fundamental to E (note 4)

**Expected Behavior**: CHECKED
- First recording → slot 0
- Second recording → slot 1
- Third recording → slot 2
- Fourth recording → overwrites oldest (slot 0)
- 

**Logs to Add**:
```csharp
// In FindEmptyMainSlot:
Debug.Log($"[TEST] FindEmptyMainSlot: found={emptyMain}, note={note}");

// In FindOldestMainSlot:
Debug.Log($"[TEST] FindOldestMainSlot: oldest={best}, timestamp={oldest}, note={note}");
```

**Validation**:
- Verify 3 files exist for the note
- Check timestamps match slot order
- Verify slot 3 (HOLD) remains empty until needed

---

### TEST 3: Playback - Basic Single Clip // CHECKED
**Goal**: Verify a saved clip can be loaded and played.

**Setup**:
- Record 1 clip manually or via TEST 1
- Enable playback mode

**Keyboard Commands**:
- `P` - Toggle playback mode
- `L` - List all saved clips (debug helper)

**Expected Behavior**:
- Playback finds the saved WAV
- Loads and plays it
- Audio is audible
- After completion, waits for next clip

**Logs to Add**:
```csharp
// In FindFirstAvailableWav:
Debug.Log($"[TEST] FindFirstAvailableWav: found={nextPath}");

// In LoadAndPlayWav:
Debug.Log($"[TEST] LoadAndPlayWav: loading={fullPath}, clip length={clip.length}");

// In PlaybackLoop:
Debug.Log($"[TEST] PlaybackLoop: isPlaying={playbackSource.isPlaying}, currentPath={currentPlayingFilePath}");
```

**Validation**:
- Audio plays correctly
- File path matches expected location
- Playback stops when clip ends

---

### TEST 4: Playback - Multiple Clips (Oldest First) CHECKED
**Goal**: Verify playback selects oldest clip first.

**Setup**:
- Record 3 clips for same note (creates files with different timestamps)
- Enable playback

**Expected Behavior**:
- First playback → oldest file (slot 0)
- Second playback → next oldest (slot 1)
- Third playback → newest (slot 2)
- Fourth playback -> oldest

**Logs to Add**:
```csharp
// In FindFirstAvailableWav, before sorting:
Debug.Log($"[TEST] FindFirstAvailableWav: found {wavFiles.Length} files before sort");

// After sorting:
Debug.Log($"[TEST] FindFirstAvailableWav: selected={wavFiles[0]}, all files={string.Join(", ", wavFiles)}");
```

**Validation**:
- Playback order matches file creation order
- Each clip plays to completion
- No clips are skipped

---

### TEST 5: Overwrite Logic - Non-Playing Slot CHECKED
**Goal**: Verify oldest non-playing slot is overwritten when all slots full.

**Setup**:
- Fill all 3 main slots
- Record 4th clip (should overwrite slot 0 if not playing)

**Expected Behavior**:
- Finds oldest slot (slot 0)
- Checks if playing (should be false)
- Deletes old file
- Saves new file to slot 0

**Logs to Add**:
```csharp
// In SaveRecording, Rule 2/3 section:
Debug.Log($"[TEST] SaveRecording: oldest={oldest}, isPlaying={IsFileCurrentlyPlaying(oldSlot.filePath)}, filePath={oldSlot.filePath}");
```

**Validation**:
- Old file deleted
- New file saved to correct slot
- Timestamp updated

---

### TEST 6: HOLD Slot - Playing Clip Protection CHECK
**Goal**: Verify HOLD slot is used when oldest slot is currently playing.

**Setup**:
- Fill all 3 main slots
- Start playback of slot 0
- Record new clip while slot 0 is playing

**Expected Behavior**:
- Detects slot 0 is playing
- Marks slot 0 for deletion
- Saves new clip to HOLD slot (slot 3)
- Sets `pendingFillTarget` to slot 0
- After playback ends, HOLD moves to slot 0

**Logs to Add**:
```csharp
// In SaveRecording, Rule 3 section:
Debug.Log($"[TEST] SaveRecording: RULE 3 triggered - oldest playing, saving to HOLD, pendingFillTarget[{note}]={oldest}");

// In ProcessPendingDeletions:
Debug.Log($"[TEST] ProcessPendingDeletions: promoting HOLD to slot {targetSlot} for note {note}");
```

**Validation**:
- HOLD slot contains new clip
- Slot 0 marked for deletion
- After playback, HOLD moves to slot 0
- Old slot 0 file deleted

---

### TEST 7: HOLD Overwrite - Waiting for Slot CHECK
**Goal**: Verify HOLD is overwritten if already waiting for a slot.

**Setup**:
- Fill all slots, start playback of slot 0
- Record clip 1 → goes to HOLD
- Record clip 2 while still waiting → should overwrite HOLD

**Expected Behavior**:
- First clip → HOLD, `pendingFillTarget` set
- Second clip → overwrites HOLD (Rule 4)
- After slot 0 frees, only newest clip moves to main slot

**Logs to Add**:
```csharp
// In SaveRecording, Rule 4 section:
Debug.Log($"[TEST] SaveRecording: RULE 4 triggered - overwriting HOLD, waiting for slot {pendingFillTarget[note]}");
```

**Validation**:
- Only newest clip in HOLD
- Old HOLD file deleted
- Correct clip moves to main slot when available

---


### TEST 7b: Playback - Mulitple Clips with Overwriting CHECK
Test that playback works correctly the following conditions:
- Recording four has overwritten recording one (when recording one was not on HOLD)
- Recording four has overwritten recording one (when recording one was on HOLD)

- Recording five has overwritten recording two (when recording two was not on HOLD)
- Recording five has overwritten recording two (when recording two was on HOLD)

---

### TEST 7c - Recording interruption works TestForFailure() CHECK

---

### TEST 8: Multiple Notes - Isolation CHECK
**Goal**: Verify recordings are isolated per note.

**Setup**:
- Record clips for different notes (C, D, E)
- Verify each note has its own slots

**Keyboard Commands**:
- `1-9` - Set fundamental note (0-8)
- `0` - Set fundamental note (9-11, or use modifier)

**Expected Behavior**:
- Each note maintains separate slot arrays
- Files saved to correct note folders
- Playback can switch between notes

**Logs to Add**:
```csharp
// In SaveRecording, at start:
Debug.Log($"[TEST] SaveRecording: note={note}, slotsForNote has {slotsForNote.Count} slots");
```

**Validation**:
- Files in correct note subfolders
- Slots independent per note
- No cross-contamination

---

### TEST 9: Deletion Safety - File Currently Playing CHECK
**Goal**: Verify files aren't deleted while playing.

**Setup**:
- Start playback
- Trigger deletion process
- Verify file isn't deleted until playback ends

**Expected Behavior**:
- `IsFileCurrentlyPlaying` returns true during playback
- File remains in `pendingDeletePaths` until safe
- File deleted after playback completes

**Logs to Add**:
```csharp
// In ProcessPendingDeletions:
Debug.Log($"[TEST] ProcessPendingDeletions: checking {path}, isPlaying={IsFileCurrentlyPlaying(path)}");

// In IsFileCurrentlyPlaying:
Debug.Log($"[TEST] IsFileCurrentlyPlaying: path={path}, currentPlaying={currentPlayingFilePath}, isPlaying={playbackSource.isPlaying}");
```

**Validation**:
- No file deletion errors
- Files deleted only when safe
- Playback completes without interruption

---

### TEST 10: Cleanup & Memory Management CHECK
**Goal**: Verify memory is freed after saving.

**Setup**:
- Record multiple clips
- Monitor memory usage

**Expected Behavior**:
- `Destroy(recordedClip)` called after saving
- `clip` field in ClipSlot is null after save
- No memory leaks

**Logs to Add**:
```csharp
// In SaveRecording, after Destroy:
Debug.Log($"[TEST] SaveRecording: destroyed clip, slot.clip is now null={slotsForNote[slotIndex].clip == null}");
```

**Validation**:
- Memory doesn't grow unbounded
- Clips destroyed after save
- Only file paths stored in slots

---

### TEST 11: Delete All Recordings CHECK
**Goal**: Verify `DeleteAllRecordings` clears everything.

**Keyboard Commands**:
- `Delete` or `X` - Delete all recordings

**Expected Behavior**:
- Playback stops
- All files deleted
- All slots cleared
- Pending operations cleared

**Logs to Add**:
```csharp
// In DeleteAllRecordingsCoroutine:
Debug.Log($"[TEST] DeleteAllRecordings: clearing {pendingDeletePaths.Count} pending deletes, {pendingFillTarget.Count} pending fills");
```

**Validation**:
- Session folder deleted or empty
- All slots reset
- No orphaned files

---

### TEST 12: Edge Cases
**Goal**: Test error conditions and edge cases.

**Test Cases**:
1. **No microphone**: Should log warning, not crash
2. **Invalid fundamental**: Should handle gracefully
3. **Empty folder on playback**: Should wait, not crash
4. **File deletion failure**: Should log error, continue
5. **Concurrent recording attempts**: Guard should prevent

**Logs to Add**:
```csharp
// In StartRecordingLoop:
if (Microphone.devices.Length == 0)
    Debug.LogError("[TEST] StartRecordingLoop: No microphone detected!");

// In RecordingCoroutine, guard check:
if (recordingLoopGuard)
    Debug.LogWarning("[TEST] RecordingCoroutine: Already running, skipping");
```

---

## Recommended Keyboard Commands for InputReferences.cs

Add these to `InputReferences.cs` Update() method:

```csharp
// Recording Controls
if (Input.GetKeyDown(KeyCode.R))
{
    if (RecordedAudioPlaybackTest.Instance != null)
    {
        bool newMode = !RecordedAudioPlaybackTest.Instance.recordMode;
        RecordedAudioPlaybackTest.Instance.SetRecordMode(newMode);
        Debug.Log($"[TEST] Record mode toggled: {newMode}");
    }
}

// Playback Controls
if (Input.GetKeyDown(KeyCode.P))
{
    if (RecordedAudioPlaybackTest.Instance != null)
    {
        bool newMode = !RecordedAudioPlaybackTest.Instance.playMode;
        RecordedAudioPlaybackTest.Instance.SetPlaybackMode(newMode);
        Debug.Log($"[TEST] Playback mode toggled: {newMode}");
    }
}

// Delete All Recordings
if (Input.GetKeyDown(KeyCode.X))
{
    if (RecordedAudioPlaybackTest.Instance != null)
    {
        RecordedAudioPlaybackTest.Instance.DeleteAllRecordings();
        Debug.Log("[TEST] DeleteAllRecordings called");
    }
}

// Open Recording Folder (for manual inspection)
if (Input.GetKeyDown(KeyCode.F))
{
    if (RecordedAudioPlaybackTest.Instance != null)
    {
        // Use reflection or make OpenRecordingFolder public
        Debug.Log("[TEST] Use Context Menu 'Open Recording Folder' to view files");
    }
}

// Force Fundamental Note (for testing different notes)
// Note: This requires access to musicSystem1 - may need to expose or mock
if (Input.GetKeyDown(KeyCode.Alpha1))
{
    // Set fundamental to C (0)
    // Implementation depends on MusicSystem1 API
}
// ... similar for Alpha2-9 for other notes
```

---

## Temporary Code Modifications for Testing

### 1. Bypass Tone Detection (for manual testing)
Add to `RecordingCoroutine()`:
```csharp
// TEMP: Bypass tone detection for testing
bool bypassToneDetection = true; // Set to false for real testing
if (bypassToneDetection)
{
    Debug.Log("[TEST] Bypassing tone detection - starting recording immediately");
    yield return new WaitForSeconds(0.1f); // Small delay
}
else
{
    while(imitoneVoiceInterpreter.toneActive == false)
    {
        // ... existing code
    }
}
```

### 2. Force TestForFailure to Always Pass
Already done (`forceSuccess = true`), but verify:
```csharp
private bool TestForFailure (bool forceSuccess = true) 
{
    // forceSuccess defaults to true, so gate is disabled
    // For testing, keep this true
}
```

### 3. Add Manual Recording Trigger
Add public method for keyboard-triggered recording:
```csharp
[ContextMenu("Manual Test Recording")]
public void ManualTestRecording()
{
    if (Microphone.devices.Length == 0)
    {
        Debug.LogError("[TEST] No microphone!");
        return;
    }
    
    // Force a test recording
    NoteName testNote = NoteName.C; // or get from musicSystem1
    StartRecording(testNote);
    
    StartCoroutine(ManualTestRecordingCoroutine(testNote));
}

private IEnumerator ManualTestRecordingCoroutine(NoteName note)
{
    yield return new WaitForSeconds(5f); // Record for 5 seconds
    StopAndSaveRecording();
}
```

### 4. Add Slot Status Debug Method
```csharp
[ContextMenu("Print Slot Status")]
public void PrintSlotStatus()
{
    Debug.Log("=== SLOT STATUS ===");
    for (int n = 0; n < clipSlots.Count; n++)
    {
        var note = (NoteName)n;
        var slots = clipSlots[n];
        Debug.Log($"Note {note}:");
        for (int s = 0; s < slots.Count; s++)
        {
            var slot = slots[s];
            Debug.Log($"  Slot {s}: Empty={slot.IsEmpty}, Path={slot.filePath}, Created={slot.createdAtIso}");
        }
    }
    Debug.Log("===================");
}
```

---

## Testing Order Recommendation

1. **Start with isolated components**:
   - TEST 1 (Basic Recording)
   - TEST 3 (Basic Playback)

2. **Then test slot management**:
   - TEST 2 (Sequential Fill)
   - TEST 5 (Overwrite Logic)

3. **Then test complex interactions**:
   - TEST 6 (HOLD Slot)
   - TEST 7 (HOLD Overwrite)

4. **Then test multi-note**:
   - TEST 8 (Multiple Notes)

5. **Finally test edge cases**:
   - TEST 9 (Deletion Safety)
   - TEST 10 (Memory Management)
   - TEST 11 (Delete All)
   - TEST 12 (Edge Cases)

---

## Success Criteria

- ✅ All recordings save to correct locations
- ✅ Playback selects oldest clip first
- ✅ Slots fill in order (0→1→2)
- ✅ Oldest non-playing slot overwrites correctly
- ✅ HOLD slot protects playing clips
- ✅ Files not deleted while playing
- ✅ Memory freed after saving
- ✅ Multiple notes isolated correctly
- ✅ No crashes on edge cases

