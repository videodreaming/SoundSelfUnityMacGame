# Comprehensive Soundscape & Fundamental Locking System Testing Plan

---
✅ ## Test Suite 1: Basic Soundscape Functionality [PASS]
### Test 1.1: Direct SoundWorld Calls
**Objective**: Verify existing SoundWorld calls still work
**Steps**:
1. Start in Freeplay mode
2. Call `MusicSystem1.instance.SetSoundscape("SonoFlore")`
3. Call `MusicSystem1.instance.SetSoundscape("Shadow")`
4. Call `MusicSystem1.instance.SetSoundscape("Gentle")`
5. Call `MusicSystem1.instance.SetSoundscape("Shruti")`
**Expected Results**:
- ✅ Each soundscape plays correctly
- ✅ `currentInteractionType` is set to `InteractionType.SoundWorld`
- ✅ Wwise state "InteractiveMusicMode" is set to "InteractiveMusicSystem"
- ✅ Wwise state "SoundWorldMode" is set to the correct soundscape name
- ✅ Debug logs show correct soundscape names
- ✅ **Content lock is cleared** (SoundWorlds work with any fundamental)
**Check Console For**:
- "MUSIC: Soundscape Set To: [name] (SoundWorld)"
- "MUSIC: Fundamental Content Unlocked" (when switching from MusicLoop)
- No warnings about unknown soundscape types

---
✅### Test 1.2: Direct MusicLoop Calls  [PASS]
**Objective**: Verify MusicLoop calls work and set content locks correctly
**Steps**:
1. Start in Freeplay mode
2. Call `MusicSystem1.instance.SetSoundscape("ShiftingEarth")`
3. Call `MusicSystem1.instance.SetSoundscape("SitarAmbience")`
4. Call `MusicSystem1.instance.SetSoundscape("PinkNoiseAtmosphere")`
**Expected Results**:
- ✅ Each music loop plays correctly
- ✅ `currentInteractionType` is set to `InteractionType.MusicLoop`
- ✅ Wwise state "InteractiveMusicMode" is set to "MusicLoops"
- ✅ Wwise switch "MusicLoops_Switch" is set to the correct loop name
- ✅ Debug logs show correct soundscape names
- ✅ **Content lock is set to the correct fundamental** for each MusicLoop:
  - "ShiftingEarth" → locks to C
  - "SitarAmbience" → locks to C
  - "PinkNoiseAtmosphere" → locks to A#/Bb
- ✅ **Fundamental changes to match the content lock** (if no higher priority locks)
**Check Console For**:
- "MUSIC: Soundscape Set To: [name] (MusicLoop)"
- "MUSIC: Content lock set to [NoteName] for MusicLoop '[name]'"
- "MUSIC: Fundamental Content Locked to [NoteName]"
- No warnings about unknown soundscape types

---

## TEST NOTENAME CHANGES TO MUSICSYSTEM


### Step 4.4: Comprehensive Testing Checklist

**Test Areas**:

1. **Fundamental Note Changes**
   - [ ] Fundamental changes correctly when unlocked
   - [ ] Fundamental respects all lock types (Debug, Content, Mode)
   - [ ] Wwise switch updates correctly
   - [ ] Binaural beats frequency updates correctly

2. **Harmony System**
   - [ ] Harmony sequences play correctly
   - [ ] Harmony wraps around octave correctly
   - [ ] Harmony updates when fundamental changes

3. **Note Detection**
   - [ ] Voice input correctly detects notes
   - [ ] NoteTracker updates correctly
   - [ ] Activation timers work correctly

4. **Mode Switching**
   - [ ] Tutorial mode locks fundamental correctly
   - [ ] Freeplay mode unlocks fundamental correctly
   - [ ] MusicLoop content locks work correctly

5. **Edge Cases**
   - [ ] None/Invalid note handling
   - [ ] Negative interval handling
   - [ ] Wrapped distance calculations
   - [ ] Modulo 12 operations

6. **The Notes are Mapped Correctly** 
- [ ] Subjective test of a C fundamental to a sung C
- [ ] Subjective test of an A fundamental to a sung A
- [ ] Test that harmonies change correctly when in a SoundWorld

**Dependencies**: All phases complete  
**Risk**: Medium - Requires thorough testing

---
## Test Suite 8: Fundamental Locking System - Mode Lock
---
### Test All Locking Systems.
**AI Instructions**
- Create a coroutine that makes it easy to perform this series of tests. The coroutine should be started with the space bar, and should proceed through different stages with the spacebar, as well as using debug logs to tell the tester instructions, and what they should be looking for at this stage. The tester should have another set of buttons they can press to interact with the environment, in this case by setting the fundamental using ChangeFundamental(), to two different options.
**Objective** Verify all three locks (mode lock, content lock, debug lock) behaviors, outlined below. Integrate all of the tests from 8.1 to 9.1.
1. Turn on/off the different mode locks, (on, off, on, off, next mode lock)
2. Give tester a way to manually change the fundamental, to test the lock.
3. Print out the fundamental, and the lock status (of each lock)
4. Then try different combinations of locks, that unlock in different orders, to test that the resolving of the fundamental works correctly, including setting a director.

### Test 8.1: Mode Lock Basic Functionality
**Objective**: Verify mode lock sets and clears correctly
**Steps**:
1. Call `MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C)`
2. Verify lock is set
3. Call `MusicSystem1.instance.SetFundamentalModeLock(false)`
4. Verify lock is cleared
**Expected Results**:
- ✅ **Mode lock is set** (`fundamentalModeLock = NoteName.C`)
- ✅ **Fundamental changes to C** (if no higher priority locks)
- ✅ Debug log: "MUSIC: Fundamental Mode Locked to C"
- ✅ **Mode lock is cleared** (`fundamentalModeLock = null`)
- ✅ Debug log: "MUSIC: Fundamental Mode Unlocked"
- ✅ **`ResolveFundamentalOnUnlock()` is called**
**Check Console For**:
- Lock/unlock debug logs
- Fundamental change logs (if applicable)

---
### Test 8.2: Mode Lock Redundant Calls
**Objective**: Verify redundant lock/unlock calls are handled gracefully
**Steps**:
1. Call `SetFundamentalModeLock(true, NoteName.C)` twice
2. Call `SetFundamentalModeLock(false)` twice
**Expected Results**:
- ✅ **First lock call**: Sets lock and changes fundamental
- ✅ **Second lock call**: Logs "MUSIC: Fundamental Mode relocked to C" (no duplicate change)
- ✅ **First unlock call**: Clears lock and resolves fundamental
- ✅ **Second unlock call**: Logs "MUSIC: Tried to unlock fundamental mode, but it was already unlocked"
**Check Console For**:
- "MUSIC: Fundamental Mode relocked to C"
- "MUSIC: Tried to unlock fundamental mode, but it was already unlocked"

---
### Test 8.3: Mode Lock with Different Notes
**Objective**: Verify mode lock can be changed to different notes
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Change mode lock to D: `SetFundamentalModeLock(true, NoteName.D)`
3. Verify lock updates
**Expected Results**:
- ✅ **Lock changes from C to D**
- ✅ Debug log: "MUSIC: Fundamental Mode Lock changed from C to D"
- ✅ **Fundamental changes to D** (if mode lock is active)
**Check Console For**:
- "MUSIC: Fundamental Mode Lock changed from C to D"



---
## Test Suite 9: Fundamental Locking System - Content Lock
### Test 9.1: Content Lock Basic Functionality
**Objective**: Verify content lock sets and clears correctly
**Steps**:
1. Call `MusicSystem1.instance.SetFundamentalContentLock(NoteName.C)`
2. Verify lock is set
3. Call `MusicSystem1.instance.SetFundamentalContentLock(null)`
4. Verify lock is cleared
**Expected Results**:
- ✅ **Content lock is set** (`fundamentalContentLock = NoteName.C`)
- ✅ **Fundamental changes to C** (if no debug lock)
- ✅ Debug log: "MUSIC: Fundamental Content Locked to C"
- ✅ **Content lock is cleared** (`fundamentalContentLock = null`)
- ✅ Debug log: "MUSIC: Fundamental Content Unlocked"
- ✅ **`ResolveFundamentalOnUnlock()` is called**
**Check Console For**:
- Lock/unlock debug logs
- Fundamental change logs (if applicable)

---
### Test 9.2: MusicLoop Sets Content Lock
**Objective**: Verify MusicLoop automatically sets content lock to correct fundamental
**Steps**:
1. Set MusicLoop "ShiftingEarth" (requires C)
2. Verify content lock is set to C
3. Set MusicLoop "PinkNoiseAtmosphere" (requires A#)
4. Verify content lock updates to A#
**Expected Results**:
- ✅ **Content lock set to C** for "ShiftingEarth"
- ✅ **Fundamental changes to C** (if no higher priority locks)
- ✅ **Content lock updates to A#** for "PinkNoiseAtmosphere"
- ✅ **Fundamental changes to A#** (if no higher priority locks)
- ✅ Debug log: "MUSIC: Content lock set to [NoteName] for MusicLoop '[name]'"
**Check Console For**:
- "MUSIC: Content lock set to C for MusicLoop 'ShiftingEarth'"
- "MUSIC: Fundamental Content Lock changed from C to A#" (when switching)

---
### Test 9.3: SoundWorld Clears Content Lock
**Objective**: Verify SoundWorld clears content lock
**Steps**:
1. Set MusicLoop "ShiftingEarth" (content lock set to C)
2. Set SoundWorld "SonoFlore"
3. Verify content lock is cleared
**Expected Results**:
- ✅ **Content lock is cleared** when switching to SoundWorld
- ✅ Debug log: "MUSIC: Fundamental Content Unlocked"
- ✅ **`ResolveFundamentalOnUnlock()` is called**
- ✅ **Fundamental may change** based on tracking data (if threshold met)
**Check Console For**:
- "MUSIC: Fundamental Content Unlocked"
- "MUSIC: Fundamental Changed Immediately on Unlock" or "MUSIC: New Fundamental Queued on Unlock" (if threshold met)

---
### Test 9.4: Switching Between MusicLoops Updates Lock
**Objective**: Verify switching between MusicLoops updates content lock correctly
**Steps**:
1. Set MusicLoop "ShiftingEarth" (C)
2. Set MusicLoop "SitarAmbience" (C)
3. Set MusicLoop "PinkNoiseAtmosphere" (A#)
4. Verify lock updates at each step
**Expected Results**:
- ✅ **Lock stays C** when switching C → C (no change needed)
- ✅ Debug log: "MUSIC: Fundamental Content relocked to C"
- ✅ **Lock updates to A#** when switching C → A#
- ✅ Debug log: "MUSIC: Fundamental Content Lock changed from C to A#"
- ✅ **Fundamental changes to A#** (if content lock is active)
**Check Console For**:
- "MUSIC: Fundamental Content relocked to C" (when same note)
- "MUSIC: Fundamental Content Lock changed from C to A#" (when different note)

---
### Test 9.5: Content Lock Safety Checks
**Objective**: Verify content lock rejects invalid inputs
**Steps**:
1. Call `SetFundamentalContentLock(NoteName.None)`
2. Verify lock is not set
**Expected Results**:
- ✅ **Warning logged**: "MUSIC: Cannot set content lock to NoteName.None - ignoring request"
- ✅ **Lock unchanged** (remains null or previous value)
- ✅ No crash or exception
**Check Console For**:
- "MUSIC: Cannot set content lock to NoteName.None - ignoring request"

---
### Test 9.6: MusicLoop with Invalid Fundamental
**Objective**: Verify MusicLoop handles invalid fundamental gracefully
**Steps**:
1. Temporarily modify `GetMusicLoopFundamental()` to return `NoteName.None` for a MusicLoop
2. Call `SetMusicLoop()` for that MusicLoop
3. Verify behavior
**Expected Results**:
- ✅ **Warning logged**: "MUSIC: GetMusicLoopFundamental() returned NoteName.None for '[name]' - clearing content lock to avoid stale lock"
- ✅ **Content lock is cleared** (prevents stale lock)
- ✅ MusicLoop still sets (audio may play, but fundamental not locked)
**Check Console For**:
- "MUSIC: GetMusicLoopFundamental() returned NoteName.None for '[name]' - clearing content lock to avoid stale lock"
- "MUSIC: Fundamental Content Unlocked"

---
## Test Suite 10: Fundamental Locking System - Priority System
### Test 10.1: Debug Lock Overrides Content Lock
**Objective**: Verify debug lock takes highest priority
**Steps**:
1. Set content lock to C: `SetFundamentalContentLock(NoteName.C)`
2. Set debug lock to D: `SetFundamentalDebugLock(NoteName.D)` (via OnValidate or direct call)
3. Verify debug lock is active
4. Clear debug lock
5. Verify content lock becomes active
**Expected Results**:
- ✅ **Content lock set to C** - fundamental changes to C
- ✅ **Debug lock set to D** - fundamental changes to D (overrides content lock)
- ✅ Debug log: "MUSIC: Content lock set to C, but higher priority lock active (D) - fundamental unchanged" (when setting content lock with debug lock active)
- ✅ **Debug lock cleared** - content lock becomes active
- ✅ **Fundamental changes back to C** (content lock active)
**Check Console For**:
- "MUSIC: Debug lock set and locked fundamental to D (DEVELOPMENT ONLY - highest priority)"
- "MUSIC: Content lock set to C, but higher priority lock active (D) - fundamental unchanged"
- "MUSIC: Lower priority lock active (C) - fundamental set accordingly" (when debug lock cleared)

---
### Test 10.2: Content Lock Overrides Mode Lock
**Objective**: Verify content lock takes priority over mode lock
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Set content lock to D: `SetFundamentalContentLock(NoteName.D)`
3. Verify content lock is active
4. Clear content lock
5. Verify mode lock becomes active
**Expected Results**:
- ✅ **Mode lock set to C** - fundamental changes to C
- ✅ **Content lock set to D** - fundamental changes to D (overrides mode lock)
- ✅ Debug log: "MUSIC: Mode lock set to C, but higher priority lock active (D) - fundamental unchanged" (when setting mode lock with content lock active)
- ✅ **Content lock cleared** - mode lock becomes active
- ✅ **Fundamental changes back to C** (mode lock active)
**Check Console For**:
- "MUSIC: Mode lock set to C, but higher priority lock active (D) - fundamental unchanged"
- "MUSIC: Lower priority lock active (C) - fundamental set accordingly" (when content lock cleared)

---
### Test 10.3: Mode Lock Works When Others Are Null
**Objective**: Verify mode lock functions correctly when no other locks exist
**Steps**:
1. Ensure no locks are active
2. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
3. Verify mode lock is active
**Expected Results**:
- ✅ **Mode lock is active** (`GetLockedFundamental()` returns C)
- ✅ **Fundamental changes to C**
- ✅ Debug log: "MUSIC: Fundamental Mode Locked to C"
**Check Console For**:
- "MUSIC: Fundamental Mode Locked to C"
- Fundamental change logs

---
### Test 10.4: All Three Locks Active (Priority Verification)
**Objective**: Verify priority order when all locks are active
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Set content lock to D: `SetFundamentalContentLock(NoteName.D)`
3. Set debug lock to E: `SetFundamentalDebugLock(NoteName.E)`
4. Verify debug lock is active
5. Clear debug lock - verify content lock becomes active
6. Clear content lock - verify mode lock becomes active
**Expected Results**:
- ✅ **Debug lock is active** (`GetLockedFundamental()` returns E)
- ✅ **Fundamental is E** (debug lock priority)
- ✅ **When debug lock cleared**: Content lock becomes active (D)
- ✅ **When content lock cleared**: Mode lock becomes active (C)
**Check Console For**:
- Priority logs showing which lock is active
- "MUSIC: Lower priority lock active ([Note]) - fundamental set accordingly" (on each unlock)

---
## Test Suite 11: Fundamental Locking System - Unlock Resolution
### Test 11.1: ResolveFundamentalOnUnlock with Lower Priority Lock
**Objective**: Verify unlock resolution applies lower priority locks correctly
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Set content lock to D: `SetFundamentalContentLock(NoteName.D)` (content lock active)
3. Clear content lock: `SetFundamentalContentLock(null)`
4. Verify mode lock becomes active
**Expected Results**:
- ✅ **Content lock cleared**
- ✅ **`ResolveFundamentalOnUnlock()` is called**
- ✅ **Mode lock is detected** as lower priority lock
- ✅ **Fundamental changes to C** (mode lock active)
- ✅ Debug log: "MUSIC: Lower priority lock active (C) - fundamental set accordingly"
**Check Console For**:
- "MUSIC: Fundamental Content Unlocked"
- "MUSIC: Lower priority lock active (C) - fundamental set accordingly"

---
### Test 11.2: ResolveFundamentalOnUnlock with No Locks (Immediate Threshold)
**Objective**: Verify unlock resolution triggers immediate change when threshold is high
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Tone into note D for extended period (build up `ChangeFundamentalTimer` >= 22.0f)
3. Clear mode lock: `SetFundamentalModeLock(false)`
4. Verify fundamental changes immediately to D
**Expected Results**:
- ✅ **Mode lock cleared**
- ✅ **`ResolveFundamentalOnUnlock()` is called**
- ✅ **No lower priority locks** (all locks cleared)
- ✅ **Timer >= immediate threshold** (22.0f)
- ✅ **Fundamental changes immediately to D**
- ✅ Debug log: "MUSIC: Fundamental Changed Immediately on Unlock (high threshold): D"
- ✅ **`director.ActivateQueue(5.0f)` is called**
**Check Console For**:
- "MUSIC: Fundamental Changed Immediately on Unlock (high threshold): D"
- Director queue activation logs

---
### Test 11.3: ResolveFundamentalOnUnlock with No Locks (Queue Threshold)
**Objective**: Verify unlock resolution queues change when threshold is medium
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Tone into note D for medium period (build up `ChangeFundamentalTimer` >= 12.0f but < 22.0f)
3. Clear mode lock: `SetFundamentalModeLock(false)`
4. Verify fundamental change is queued
**Expected Results**:
- ✅ **Mode lock cleared**
- ✅ **`ResolveFundamentalOnUnlock()` is called**
- ✅ **No lower priority locks**
- ✅ **Timer >= queue threshold but < immediate threshold** (12.0f <= timer < 22.0f)
- ✅ **Fundamental change is queued** (not immediate)
- ✅ Debug log: "MUSIC: New Fundamental Queued on Unlock: D"
- ✅ **`directorStoredFundamental` is set to D**
**Check Console For**:
- "MUSIC: New Fundamental Queued on Unlock: D"
- Director queue logs showing queued action

---
### Test 11.4: ResolveFundamentalOnUnlock with No Locks (Below Threshold)
**Objective**: Verify unlock resolution does nothing when threshold not met
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Tone into note D briefly (build up `ChangeFundamentalTimer` < 12.0f)
3. Clear mode lock: `SetFundamentalModeLock(false)`
4. Verify no fundamental change
**Expected Results**:
- ✅ **Mode lock cleared**
- ✅ **`ResolveFundamentalOnUnlock()` is called**
- ✅ **No lower priority locks**
- ✅ **Timer < queue threshold** (< 12.0f)
- ✅ **No fundamental change** (queued or immediate)
- ✅ **Fundamental remains C** (or current value)
**Check Console For**:
- "MUSIC: Fundamental Mode Unlocked"
- No fundamental change logs

---
### Test 11.5: ResolveFundamentalOnUnlock with Invalid Note
**Objective**: Verify unlock resolution handles invalid notes gracefully
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Clear NoteTracker (or ensure no valid notes tracked)
3. Clear mode lock: `SetFundamentalModeLock(false)`
4. Verify no crash
**Expected Results**:
- ✅ **Mode lock cleared**
- ✅ **`ResolveFundamentalOnUnlock()` is called**
- ✅ **No valid fundamental found** (`newFundamental == -1`)
- ✅ **Warning logged**: "MUSIC: Threshold met but no valid fundamental found in NoteTracker - skipping queue"
- ✅ **No fundamental change**
- ✅ **No crash or exception**
**Check Console For**:
- "MUSIC: Threshold met but no valid fundamental found in NoteTracker - skipping queue"

---
## Test Suite 12: Fundamental Locking System - Tracking Behavior
### Test 12.1: Tracking Continues When Locked
**Objective**: Verify `ChangeFundamentalTimer` increments even when fundamental is locked
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Tone into note D for extended period
3. Check `NoteTracker[D].ChangeFundamentalTimer` value
4. Verify timer is incrementing
**Expected Results**:
- ✅ **Fundamental is locked to C** (cannot change)
- ✅ **`ChangeFundamentalTimer` for D increments** (tracking continues)
- ✅ **Fundamental does NOT change** (locked)
- ✅ **Timer persists** across lock duration
**Check Console For**:
- No fundamental change logs (locked)
- Timer values can be checked via debug logs or inspector

---
### Test 12.2: Changes Queued When Unlocked (High Timer)
**Objective**: Verify high timer triggers immediate change on unlock
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Tone into note D until `ChangeFundamentalTimer >= 22.0f`
3. Clear mode lock: `SetFundamentalModeLock(false)`
4. Verify fundamental changes immediately to D
**Expected Results**:
- ✅ **Timer >= immediate threshold** (22.0f)
- ✅ **Fundamental changes immediately** on unlock
- ✅ **`ChangeFundamental()` is called** (not queued)
- ✅ **`director.ActivateQueue(5.0f)` is called**
**Check Console For**:
- "MUSIC: Fundamental Changed Immediately on Unlock (high threshold): D"
- Director queue activation

---
### Test 12.3: Changes Queued When Unlocked (Medium Timer)
**Objective**: Verify medium timer queues change on unlock
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Tone into note D until `ChangeFundamentalTimer >= 12.0f` but < 22.0f
3. Clear mode lock: `SetFundamentalModeLock(false)`
4. Verify fundamental change is queued
**Expected Results**:
- ✅ **Timer >= queue threshold but < immediate threshold** (12.0f <= timer < 22.0f)
- ✅ **Fundamental change is queued** (not immediate)
- ✅ **`director.AddActionToQueue()` is called** with "fundamentalChange" type
- ✅ **`directorStoredFundamental` is set to D**
**Check Console For**:
- "MUSIC: New Fundamental Queued on Unlock: D"
- Director queue logs showing queued action

---
### Test 12.4: Timers Persist Across Lock/Unlock Cycles
**Objective**: Verify timers are not reset when locks are set/cleared
**Steps**:
1. Tone into note D (build up timer)
2. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
3. Continue toning into D (timer should continue incrementing)
4. Clear mode lock: `SetFundamentalModeLock(false)`
5. Verify timer value persisted
**Expected Results**:
- ✅ **Timer increments before lock**
- ✅ **Timer continues incrementing during lock** (not reset)
- ✅ **Timer persists after unlock** (not reset)
- ✅ **Timer value is preserved** across lock/unlock cycle
**Check Console For**:
- No timer reset logs during lock/unlock
- Timer values persist (can verify via debug logs)

---
### Test 12.5: Timers Reset Only on Fundamental Change
**Objective**: Verify timers reset only when fundamental actually changes
**Steps**:
1. Tone into note D (build up timer)
2. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)` (fundamental changes to C)
3. Verify D's timer is reset (fundamental changed)
4. Tone into D again (build up timer)
5. Set content lock to C: `SetFundamentalContentLock(NoteName.C)` (fundamental already C, no change)
6. Verify D's timer is NOT reset (fundamental didn't change)
**Expected Results**:
- ✅ **Timer reset when fundamental changes** (lock sets fundamental to C)
- ✅ **Timer NOT reset when fundamental unchanged** (lock sets to already-active C)
- ✅ **`ResetFundamentalTimers()` only called from `SetFundamentalDirect()`**
**Check Console For**:
- "MUSIC 8: Key(D): ChangeFundamentalTimer reset" (when fundamental changes)
- No reset logs when fundamental unchanged

---
## Test Suite 13: Fundamental Locking System - Debug Lock
### Test 13.1: Debug Lock Basic Functionality
**Objective**: Verify debug lock sets correctly
**Steps**:
1. Set `permanentlySetFundamental = NoteName.D` in inspector
2. Trigger `OnValidate()` (or call `SetFundamentalDebugLock(NoteName.D)` directly)
3. Verify debug lock is set
**Expected Results**:
- ✅ **Debug lock is set** (`fundamentalDebugLock = NoteName.D`)
- ✅ **Fundamental changes to D** (even if other locks exist)
- ✅ **Other locks are preserved** (but inactive due to priority)
- ✅ Debug log: "MUSIC: Debug lock set and locked fundamental to D (DEVELOPMENT ONLY - highest priority)"
**Check Console For**:
- "MUSIC: Debug lock set and locked fundamental to D (DEVELOPMENT ONLY - highest priority)"

---
### Test 13.2: Debug Lock Overrides All Other Locks
**Objective**: Verify debug lock takes absolute priority
**Steps**:
1. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
2. Set content lock to D: `SetFundamentalContentLock(NoteName.D)`
3. Set debug lock to E: `SetFundamentalDebugLock(NoteName.E)`
4. Verify debug lock is active
**Expected Results**:
- ✅ **Debug lock is active** (`GetLockedFundamental()` returns E)
- ✅ **Fundamental is E** (debug lock priority)
- ✅ **Other locks preserved** but inactive
- ✅ Debug logs show other locks are overridden
**Check Console For**:
- "MUSIC: Debug lock set and locked fundamental to E (DEVELOPMENT ONLY - highest priority)"
- "MUSIC: Mode lock set to C, but higher priority lock active (E) - fundamental unchanged"
- "MUSIC: Content lock set to D, but higher priority lock active (E) - fundamental unchanged"

---
### Test 13.3: Debug Lock Safety Checks
**Objective**: Verify debug lock rejects invalid inputs
**Steps**:
1. Call `SetFundamentalDebugLock(NoteName.None)`
2. Verify lock is not set
**Expected Results**:
- ✅ **Warning logged**: "MUSIC: Cannot set debug lock to NoteName.None - ignoring request"
- ✅ **Lock unchanged** (remains null or previous value)
- ✅ No crash or exception
**Check Console For**:
- "MUSIC: Cannot set debug lock to NoteName.None - ignoring request"

---
### Test 13.4: Debug Lock Optimization (Redundant Calls)
**Objective**: Verify debug lock skips work if already set to same note
**Steps**:
1. Set debug lock to D: `SetFundamentalDebugLock(NoteName.D)`
2. Set debug lock to D again: `SetFundamentalDebugLock(NoteName.D)`
3. Verify optimization works
**Expected Results**:
- ✅ **First call**: Sets lock and changes fundamental
- ✅ **Second call**: Logs "MUSIC: Debug lock already set to D - skipping update"
- ✅ **No duplicate work** (no fundamental change, no lock restoration)
**Check Console For**:
- "MUSIC: Debug lock already set to D - skipping update"

---

## Test Suite 2: Director Queue Priority System
### Test 2.1: ReplaceActionInQueue Clears SoundscapeShuffle
**Objective**: Verify `ReplaceActionInQueue()` clears "SoundscapeShuffle" actions when replacing with "Soundscape"
**Steps**:
1. Start WorldShuffler shuffling (should queue "SoundscapeShuffle" action)
2. Wait for shuffle to be queued (check Director queue logs)
3. Use `ReplaceActionInQueue()` to replace with "Soundscape": 
   `director.ReplaceActionInQueue(MusicSystem1.instance.Action_SetSoundscape("Shruti"), "Soundscape", "SoundscapeShuffle", true, false, 180.0f, true)`
4. Check Director queue
**Expected Results**:
- ✅ "SoundscapeShuffle" action is cleared from queue
- ✅ "Soundscape" action is added to queue
- ✅ Expiration time matching: shortest time from cleared actions is used (if shorter than newMaximumTimeLimit)
- ✅ Both old and new action types are cleared before adding new one
**Check Console For**:
- Director queue logs showing removal of shuffle action
- Director queue logs showing addition of soundscape action
- Expiration time should be minimum of: cleared "SoundscapeShuffle" time, cleared "Soundscape" time (if any), and 180.0f

---
### Test 2.2: SoundscapeShuffle Rejected When Soundscape Exists
**Objective**: Verify "SoundscapeShuffle" cannot be added if "Soundscape" exists (WorldShuffler check)
**Steps**:
1. Queue a "Soundscape" action first (using `ReplaceActionInQueue()` or `AddActionToQueue()`)
2. Attempt to queue a "SoundscapeShuffle" action (via WorldShuffler: `worldShuffler.BeginShuffle(false)`)
3. Check Director queue
**Expected Results**:
- ✅ "SoundscapeShuffle" action is NOT added
- ✅ "Soundscape" action remains in queue
- ✅ Debug log: "WorldShuffler: Attempted to queue SoundscapeShuffle, but director queue already has a specific SoundScape in it"
- ✅ WorldShuffler's `SearchQueueForType("Soundscape")` check works correctly
**Check Console For**:
- Warning message from WorldShuffler (line 165)
- Director queue logs showing only "Soundscape" action

---
### Test 2.3: ReplaceActionInQueue Expiration Time Matching
**Objective**: Verify `ReplaceActionInQueue()` preserves shortest expiration time correctly
**Steps**:
1. Queue a "SoundscapeShuffle" with 60s expiration: `director.AddActionToQueue(worldShuffler.Action_ShuffleSoundscape(), "SoundscapeShuffle", true, false, 60.0f, true, 1)`
2. Queue another "SoundscapeShuffle" with 30s expiration
3. Use `ReplaceActionInQueue()` with newMaximumTimeLimit of 120s
4. Check the expiration time of the new "Soundscape" action
**Expected Results**:
- ✅ "Soundscape" action uses 30s expiration (minimum of: 30s, 60s, 120s)
- ✅ `ReplaceActionInQueue()` correctly calculates: `Mathf.Min(shortestTimeOld, shortestTimeNew, newMaximumTimeLimit)`
- ✅ Both old action types are cleared
**Check Console For**:
- Director queue logs showing correct expiration time (30s)
- Verify the math: `Mathf.Min(30f, 60f, 120f) = 30f`

---
### Test 2.4: ReplaceActionInQueue with No Old Actions
**Objective**: Verify `ReplaceActionInQueue()` works when no old actions exist
**Steps**:
1. Ensure no "SoundscapeShuffle" actions in queue
2. Use `ReplaceActionInQueue()` to add "Soundscape"
3. Check expiration time
**Expected Results**:
- ✅ "Soundscape" action is added successfully
- ✅ Expiration time is newMaximumTimeLimit (since no old actions to match)
- ✅ No errors or warnings
**Check Console For**:
- Director queue logs showing addition
- Expiration time matches newMaximumTimeLimit

---
## Test Suite 3: WorldShuffler Integration
### Test 3.1: WorldShuffler Basic Functionality
**Objective**: Verify WorldShuffler still works correctly
**Steps**:
1. Start WorldShuffler: `worldShuffler.BeginShuffle(false)` (with queue)
2. Let it queue a shuffle action
3. Wait for shuffle action to execute
4. Verify soundscape changes
**Expected Results**:
- ✅ Shuffle action is queued with type "SoundscapeShuffle"
- ✅ Shuffle executes and changes soundscape
- ✅ New soundscape is different from previous
- ✅ WorldShuffler tracking is updated
- ✅ **If MusicLoop is shuffled to, content lock is set correctly**
- ✅ **If SoundWorld is shuffled to, content lock is cleared**
**Check Console For**:
- "WorldShuffler: Queuing World Shuffle"
- "Director Queue: Added [id] SoundscapeShuffle to director queue"
- Shuffle execution logs
- Content lock logs (if applicable)

---
### Test 3.2: WorldShuffler Respects Queue Closure (Robin says we probably don't need this)
**Objective**: Verify WorldShuffler respects closed queue
**Steps**:
1. Close soundscape queue: `worldShuffler.CloseSoundscapeQueue()`
2. Attempt to queue shuffle: `worldShuffler.BeginShuffle(false)`
**Expected Results**:
- ✅ Warning logged: "WorldShuffler: Attempted to queue sound world shuffle, but music queue is closed"
- ✅ No shuffle action queued

---
## Test Suite 4: Sequencer Integration
### Test 4.1: Sequencer Calls SoundWorlds
**Objective**: Verify Sequencer can call SoundWorlds
**Steps**:
1. Trigger Sequencer sequence that calls a SoundWorld (e.g., "Shruti" at line 245)
2. Verify soundscape changes
**Expected Results**:
- ✅ SoundWorld plays correctly
- ✅ Action type is "Soundscape" (not "SoundWorld")
- ✅ `currentInteractionType` is `InteractionType.SoundWorld`
- ✅ **Content lock is cleared** (if MusicLoop was active before)
**Check Console For**:
- "MUSIC: Soundscape Set To: [name] (SoundWorld)"
- "MUSIC: Fundamental Content Unlocked" (if switching from MusicLoop)

---
### Test 4.2: Sequencer Calls MusicLoops
**Objective**: Verify Sequencer can call MusicLoops
**Steps**:
1. Modify Sequencer to call a MusicLoop (e.g., "ShiftingEarth")
2. Trigger the sequence
3. Verify soundscape changes
**Expected Results**:
- ✅ MusicLoop plays correctly
- ✅ Action type is "Soundscape"
- ✅ `currentInteractionType` is `InteractionType.MusicLoop`
- ✅ **Content lock is set to the correct fundamental**
**Check Console For**:
- "MUSIC: Soundscape Set To: [name] (MusicLoop)"
- "MUSIC: Content lock set to [NoteName] for MusicLoop '[name]'"

---
## Test Suite 5: MusicMode Transitions
### Test 5.1: Environment → Freeplay (SoundWorld)
**Objective**: Verify transition preserves SoundWorld InteractionType
**Steps**:
1. Set soundscape to a SoundWorld (e.g., "SonoFlore")
2. Enter Environment mode
3. Transition to Freeplay mode (by toning)
4. Check Wwise states
**Expected Results**:
- ✅ `currentInteractionType` remains `InteractionType.SoundWorld`
- ✅ Wwise state "InteractiveMusicMode" is set to "InteractiveMusicSystem"
- ✅ SoundWorld continues playing
- ✅ Debug log: "MUSIC: Interactive Music Mode Recovered to InteractiveMusicSystem because Interaction Type is SoundWorld"
- ✅ **No fundamental locks are active** (SoundWorlds don't require locks)

---
### Test 5.2: Environment → Freeplay (MusicLoop)
**Objective**: Verify transition preserves MusicLoop InteractionType and content lock
**Steps**:
1. Set soundscape to a MusicLoop (e.g., "ShiftingEarth")
2. Enter Environment mode
3. Transition to Freeplay mode (by toning)
4. Check Wwise states and fundamental locks
**Expected Results**:
- ✅ `currentInteractionType` remains `InteractionType.MusicLoop`
- ✅ Wwise state "InteractiveMusicMode" is set to "MusicLoops"
- ✅ MusicLoop continues playing
- ✅ Debug log: "MUSIC: Interactive Music Mode Recovered to MusicLoops because Interaction Type is MusicLoop"
- ✅ **Content lock is still active** (MusicLoop requires specific fundamental)
- ✅ **Fundamental matches the content lock** (e.g., C for "ShiftingEarth")

---
### Test 5.3: Freeplay → Environment (SoundWorld)
**Objective**: Verify Environment mode behavior with SoundWorld
**Steps**:
1. Set soundscape to a SoundWorld in Freeplay
2. Stop toning for >30 seconds (trigger Environment mode)
3. Check Wwise states
**Expected Results**:
- ✅ Wwise state "InteractiveMusicMode" is set to "Environment"
- ✅ Warning logged: "MUSIC: Changing SoundWorld to '[name]', but current mode is 'Environment'..."
- ✅ SoundWorld change is not audible (as expected)
- ✅ **No fundamental locks active** (SoundWorlds don't require locks)

---
### Test 5.4: Freeplay → Environment (MusicLoop)
**Objective**: Verify Environment mode behavior with MusicLoop and content lock persistence
**Steps**:
1. Set soundscape to a MusicLoop in Freeplay
2. Stop toning for >30 seconds (trigger Environment mode)
3. Check Wwise states and fundamental locks
**Expected Results**:
- ✅ Wwise state "InteractiveMusicMode" is set to "Environment"
- ✅ Warning logged: "MUSIC: Changing MusicLoop to '[name]', but current mode is 'Environment'..."
- ✅ MusicLoop change is not audible (as expected)
- ✅ **Content lock remains active** (persists across mode transitions)
- ✅ **Fundamental remains locked** to the MusicLoop's required note

---
### Test 5.5: Switching Between SoundWorld and MusicLoop in Freeplay
**Objective**: Verify switching between types works correctly and updates locks
**Steps**:
1. Start with SoundWorld (e.g., "SonoFlore")
2. Switch to MusicLoop (e.g., "ShiftingEarth")
3. Switch back to SoundWorld (e.g., "Shadow")
4. Switch to different MusicLoop (e.g., "PinkNoiseAtmosphere")
5. Check `currentInteractionType`, Wwise states, and fundamental locks at each step
**Expected Results**:
- ✅ `currentInteractionType` updates correctly at each step
- ✅ Wwise states update correctly:
  - SoundWorld → "InteractiveMusicSystem"
  - MusicLoop → "MusicLoops"
- ✅ Each soundscape plays correctly
- ✅ **Content lock behavior**:
  - SoundWorld → Content lock cleared
  - MusicLoop → Content lock set to correct fundamental
  - Different MusicLoop → Content lock updated to new fundamental
- ✅ **Fundamental changes** to match content lock when MusicLoop is set (if no higher priority locks)
**Check Console For**:
- "MUSIC: Fundamental Content Unlocked" (when switching to SoundWorld)
- "MUSIC: Content lock set to [NoteName] for MusicLoop '[name]'" (when switching to MusicLoop)
- "MUSIC: Fundamental Content Lock changed from [old] to [new]" (when switching between MusicLoops with different fundamentals)

---
## Test Suite 6: Silent Mode Behavior
### Test 6.1: Silent Mode with SoundWorld
**Objective**: Verify Silent mode behavior
**Steps**:
1. Set soundscape to SoundWorld in Freeplay
2. Enter Silent mode: `MusicSystem1.instance.SetMusicModeTo(MusicMode.Silent)`
3. Change soundscape while in Silent mode
4. Check Wwise states and console
**Expected Results**:
- ✅ `StopInteractiveMusic()` is called
- ✅ `RecoverInteractiveMusicModeFromInteractionType()` is called (verify if this is correct behavior)
- ✅ Soundscape changes are not audible
- ✅ Warning logged if soundscape is changed: "MUSIC: Changing SoundWorld to '[name]', but current mode is 'Silent'..."
- ✅ **No fundamental locks active** (Silent mode doesn't require locks)
**⚠️ Potential Issue**: `RecoverInteractiveMusicModeFromInteractionType()` sets Wwise states even in Silent mode. Verify if this is intended.

---
### Test 6.2: Silent Mode with MusicLoop
**Objective**: Verify Silent mode with MusicLoop and content lock persistence
**Steps**:
1. Set soundscape to MusicLoop in Freeplay
2. Enter Silent mode
3. Change soundscape while in Silent mode
4. Check behavior and locks
**Expected Results**:
- ✅ Same as Test 6.1, but with MusicLoop warnings
- ✅ **Content lock persists** (even though audio is silent)
- ✅ **Fundamental remains locked** to MusicLoop's required note

---
### Test 6.3: Changing Soundscapes in Silent Mode
**Objective**: Verify soundscape changes don't play audio but update locks correctly
**Steps**:
1. Enter Silent mode
2. Call `SetSoundscape("SonoFlore")` (SoundWorld)
3. Call `SetSoundscape("ShiftingEarth")` (MusicLoop)
4. Call `SetSoundscape("Shadow")` (SoundWorld)
5. Verify no audio plays but locks update
**Expected Results**:
- ✅ Warnings logged for each change
- ✅ No audio plays
- ✅ `currentInteractionType` still updates (for when exiting Silent mode)
- ✅ **Content lock updates correctly**:
  - SoundWorld → Lock cleared
  - MusicLoop → Lock set
  - SoundWorld → Lock cleared again

---
## Test Suite 7: Tutorial and FrozenFreeplay Modes (Fundamental Locking Integration)
### Test 7.1: Tutorial Mode - Mode Lock Behavior
**Objective**: Verify Tutorial mode sets mode lock correctly
**Steps**:
1. Start in Freeplay mode with SoundWorld (no locks)
2. Enter Tutorial mode: `MusicSystem1.instance.SetMusicModeTo(MusicMode.Tutorial)`
3. Check fundamental lock state
4. Verify fundamental is locked to C
5. Exit Tutorial mode
6. Verify mode lock is released
**Expected Results**:
- ✅ **Mode lock is set** when entering Tutorial mode
- ✅ **Fundamental is locked to C** (`fundamentalModeLock = NoteName.C`)
- ✅ Debug log: "MUSIC: Fundamental Mode Locked to C"
- ✅ **Fundamental changes to C** (if no higher priority locks)
- ✅ **Mode lock is cleared** when exiting Tutorial mode
- ✅ Debug log: "MUSIC: Fundamental Mode Unlocked"
- ✅ **`ResolveFundamentalOnUnlock()` is called** to handle unlock
**Check Console For**:
- "MUSIC: Fundamental Mode Locked to C" (on entry)
- "MUSIC: Fundamental Mode Unlocked" (on exit)
- "MUSIC: Fundamental Changed Immediately on Unlock" or "MUSIC: New Fundamental Queued on Unlock" (if threshold met)

---
### Test 7.2: FrozenFreeplay Mode - Mode Lock Behavior
**Objective**: Verify FrozenFreeplay mode sets mode lock correctly
**Steps**:
1. Start in Freeplay mode
2. Enter FrozenFreeplay mode: `MusicSystem1.instance.SetMusicModeTo(MusicMode.FrozenFreeplay)`
3. Check fundamental lock state
4. Verify fundamental is locked to C
5. Exit FrozenFreeplay mode
6. Verify mode lock is released
**Expected Results**:
- ✅ **Mode lock is set** when entering FrozenFreeplay mode
- ✅ **Fundamental is locked to C** (`fundamentalModeLock = NoteName.C`)
- ✅ Debug log: "MUSIC: Fundamental Mode Locked to C"
- ✅ **Fundamental changes to C** (if no higher priority locks)
- ✅ **Mode lock is cleared** when exiting FrozenFreeplay mode
- ✅ **`ResolveFundamentalOnUnlock()` is called** to handle unlock
**Check Console For**:
- "MUSIC: Fundamental Mode Locked to C" (on entry)
- "MUSIC: Fundamental Mode Unlocked" (on exit)

---
### Test 7.3: Tutorial Mode Transitions with Content Lock
**Objective**: Verify Tutorial mode lock priority over content lock
**Steps**:
1. Set soundscape to MusicLoop (e.g., "ShiftingEarth") - content lock set to C
2. Enter Tutorial mode - mode lock set to C
3. Verify both locks exist but mode lock is active
4. Exit Tutorial mode
5. Verify content lock becomes active again
**Expected Results**:
- ✅ **Content lock remains set** (MusicLoop still active)
- ✅ **Mode lock is set** (Tutorial mode active)
- ✅ **Mode lock takes priority** (both are C, so fundamental stays C)
- ✅ **When Tutorial exits**: Mode lock cleared, content lock becomes active
- ✅ **Fundamental remains C** (content lock still active)
**Check Console For**:
- "MUSIC: Fundamental Mode Locked to C" (Tutorial entry)
- "MUSIC: Fundamental Mode Unlocked" (Tutorial exit)
- "MUSIC: Lower priority lock active (C) - fundamental set accordingly" (if ResolveFundamentalOnUnlock finds content lock)

---
### Test 7.4: Tutorial Mode with Different Content Lock
**Objective**: Verify mode lock overrides content lock when different
**Steps**:
1. Set soundscape to MusicLoop with different fundamental (e.g., "PinkNoiseAtmosphere" - A#)
2. Enter Tutorial mode (mode lock to C)
3. Verify fundamental changes to C (mode lock priority)
4. Exit Tutorial mode
5. Verify fundamental changes back to A# (content lock priority)
**Expected Results**:
- ✅ **Content lock is set to A#** (PinkNoiseAtmosphere)
- ✅ **Mode lock is set to C** (Tutorial)
- ✅ **Mode lock takes priority** - fundamental changes to C
- ✅ Debug log: "MUSIC: Content lock set to A#, but higher priority lock active (C) - fundamental unchanged" (when setting content lock)
- ✅ **When Tutorial exits**: Mode lock cleared, content lock becomes active
- ✅ **Fundamental changes back to A#** (content lock active)
**Check Console For**:
- "MUSIC: Fundamental Mode Locked to C" (Tutorial entry)
- "MUSIC: Lower priority lock active (A#) - fundamental set accordingly" (Tutorial exit)

---
### Test 7.5: Tutorial Correction Flow - Lock/Unlock Cycle
**Objective**: Verify Tutorial correction flow locks and unlocks correctly
**Steps**:
1. Enter Tutorial mode (mode lock set)
2. Trigger correction flow: `tutorial.StartCorrectionFlow()` (or equivalent)
3. Verify mode lock is temporarily released
4. Verify mode lock is restored after correction
**Expected Results**:
- ✅ **Mode lock is cleared** during correction flow
- ✅ Debug log: "MUSIC: Fundamental Mode Unlocked"
- ✅ **`ResolveFundamentalOnUnlock()` is called**
- ✅ **Mode lock is restored** after correction
- ✅ Debug log: "MUSIC: Fundamental Mode Locked to C"
**Check Console For**:
- Lock/unlock logs from Tutorial correction flow
- Verify `SetFundamentalModeLock(false)` and `SetFundamentalModeLock(true, NoteName.C)` calls

## Test Suite 14: Edge Cases and Error Handling
### Test 14.1: Rapid Soundscape Changes
**Objective**: Verify rapid changes don't cause issues
**Steps**:
1. Rapidly call `SetSoundscape()` with different soundscapes (SoundWorlds and MusicLoops)
2. Monitor for errors or state inconsistencies
**Expected Results**:
- ✅ No crashes or exceptions
- ✅ `currentInteractionType` always matches last set soundscape type
- ✅ Wwise states are consistent
- ✅ **Content locks update correctly** (set/cleared as appropriate)
- ✅ **No stale locks** remain

---
### Test 14.2: Director Queue Under Load
**Objective**: Verify queue handles multiple actions correctly
**Steps**:
1. Queue multiple "Soundscape" actions
2. Queue "SoundscapeShuffle" actions
3. Verify priority system works correctly
**Expected Results**:
- ✅ Priority rules are enforced
- ✅ No queue corruption
- ✅ Actions execute in correct order
- ✅ **Fundamental locks update correctly** as soundscapes change

---
### Test 14.3: WorldShuffler Exclusion
**Objective**: Verify excluded soundscapes aren't shuffled
**Steps**:
1. Exclude a soundscape: `worldShuffler.ExcludeSoundscape("SonoFlore")`
2. Trigger shuffle
3. Verify excluded soundscape is not selected
**Expected Results**:
- ✅ Excluded soundscape is not shuffled
- ✅ Other soundscapes can still be shuffled
- ✅ **Content locks update correctly** when shuffled soundscape is MusicLoop

---
### Test 14.4: Rapid Lock/Unlock Cycles
**Objective**: Verify rapid lock/unlock cycles don't cause issues
**Steps**:
1. Rapidly call `SetFundamentalModeLock(true, NoteName.C)` and `SetFundamentalModeLock(false)` multiple times
2. Monitor for errors or state inconsistencies
**Expected Results**:
- ✅ No crashes or exceptions
- ✅ Lock state is consistent
- ✅ `ResolveFundamentalOnUnlock()` handles rapid unlocks correctly
- ✅ No duplicate fundamental changes

---
### Test 14.5: Lock Set While Change Is Queued
**Objective**: Verify lock behavior when fundamental change is queued
**Steps**:
1. Tone into note D (build up timer >= 12.0f)
2. Unlock mode lock (queues change to D)
3. Set mode lock to C: `SetFundamentalModeLock(true, NoteName.C)`
4. Verify behavior
**Expected Results**:
- ✅ **Change to D is queued** (from unlock)
- ✅ **Mode lock is set to C**
- ✅ **Queued change is cleared** (when lock is set, `SetFundamentalDirect()` clears queue)
- ✅ **Fundamental changes to C** (lock takes effect)
**Check Console For**:
- "MUSIC: New Fundamental Queued on Unlock: D"
- "MUSIC: Fundamental Mode Locked to C"
- Director queue cleared logs

---
## Test Suite 15: Backward Compatibility
### Test 15.1: Old Method Calls (If Still Exist)
**Objective**: Verify backward compatibility if old methods exist
**Steps**:
1. Check if `SetSoundWorld()` and `SetMusicLoop()` are still callable directly
2. Verify they work correctly
**Expected Results**:
- ✅ Direct method calls still work (if methods exist)
- ✅ `currentInteractionType` is set correctly
- ✅ Wwise states are set correctly
- ✅ **Content locks update correctly** (set/cleared as appropriate)

---
### Test 15.2: Dropdown UI (If Used)
**Objective**: Verify dropdown still works
**Steps**:
1. Use soundscape dropdown UI (if available)
2. Select different soundscapes
3. Verify changes work
**Expected Results**:
- ✅ Dropdown updates `soundscapeDropdown` variable
- ✅ Soundscape changes correctly
- ✅ Both SoundWorlds and MusicLoops appear in dropdown (if applicable)
- ✅ **Content locks update correctly** based on selection

---
## Test Suite 16: Integration with Other Systems
### Test 16.1: Sequencer ReplaceActionInQueue
**Objective**: Verify Sequencer's ReplaceActionInQueue works (this is the main priority mechanism)
**Steps**:
1. Let WorldShuffler queue a shuffle (creates "SoundscapeShuffle" action)
2. Trigger Sequencer sequence that uses `ReplaceActionInQueue` (line 245 - End2 behavior at 180s)
3. Verify replacement works
**Expected Results**:
- ✅ "SoundscapeShuffle" is replaced with "Soundscape"
- ✅ New soundscape ("Shruti") plays correctly
- ✅ No duplicate actions in queue
- ✅ Expiration time is correctly calculated (minimum of cleared shuffle time and 180.0f)
- ✅ This is the primary mechanism for Soundscape priority over SoundscapeShuffle
- ✅ **Content locks update correctly** when soundscape changes
**Check Console For**:
- "Sequencer: Triggering End2 Behaviors: Queue Shruti, Close Music Queue, Start AVS End Sequence"
- Director queue logs showing replacement
- Content lock logs (if applicable)

---
### Test 16.2: Director Queue Logging
**Objective**: Verify queue logging shows correct types
**Steps**:
1. Queue various actions (Soundscape, SoundscapeShuffle, fundamentalChange, etc.)
2. Check Director queue logs
**Expected Results**:
- ✅ Action types are logged correctly
- ✅ Queue state is accurate
- ✅ No type name inconsistencies
- ✅ **Fundamental change actions** are logged correctly

---
### Test 16.3: Tutorial Correction Flow Integration
**Objective**: Verify Tutorial correction flow integrates with locking system
**Steps**:
1. Enter Tutorial mode (mode lock set)
2. Trigger correction flow (unlocks temporarily)
3. Complete correction (locks restored)
4. Verify fundamental lock behavior
**Expected Results**:
- ✅ **Mode lock cleared** during correction
- ✅ **`ResolveFundamentalOnUnlock()` called** (may queue change if threshold met)
- ✅ **Mode lock restored** after correction
- ✅ **Fundamental returns to C** (mode lock active)
**Check Console For**:
- Tutorial correction flow logs
- Lock/unlock logs
- Fundamental change logs (if applicable)

---
## Critical Issues to Watch For
### ⚠️ Issue 1: Silent Mode State Recovery
**Location**: `MusicSystem1.cs` line 504
**Problem**: `RecoverInteractiveMusicModeFromInteractionType()` is called in Silent mode, which may set Wwise states unnecessarily
**Question**: Should Silent mode set interactive music states, or should it be skipped?

### ✅ Issue 2: Director Priority Logic - RESOLVED
**Location**: `Sequencer.cs` line 245, `Director.cs` `ReplaceActionInQueue()` method
**Solution**: Using `ReplaceActionInQueue()` instead of priority logic in `AddActionToQueue()`
**How it works**: 
- `ReplaceActionInQueue()` clears both old type ("SoundscapeShuffle") and new type ("Soundscape")
- Takes minimum expiration time from cleared actions and newMaximumTimeLimit
- Sequencer uses this when replacing shuffle actions with direct soundscape changes

### ✅ Issue 3: Expiration Time Matching - IMPLEMENTED
**Location**: `Director.cs` `ReplaceActionInQueue()` method (lines 165-169)
**Implementation**: `ReplaceActionInQueue()` uses `ClearQueueOfType()` return values to calculate minimum expiration time
**How it works**: `newTimeLimit = Mathf.Min(shortestTimeOld, shortestTimeNew, newMaximumTimeLimit)`

### ✅ Issue 4: Action Type Naming Consistency - VERIFIED
**Location**: `WorldShuffler.cs` line 161, `Sequencer.cs` line 245
**Status**: All references use "SoundscapeShuffle" consistently

### ⚠️ Issue 5: Fundamental Change While Locked
**Location**: `MusicSystem1.cs` `ChangeFundamental()` method
**Problem**: If `ChangeFundamental()` is called while locked, it logs a warning but doesn't change
**Question**: Verify this is the correct behavior (should be rare, indicates logic flaw)

### ⚠️ Issue 6: Timer Threshold Values
**Location**: `MusicSystem1.cs` `_queueFundamentalChangeThreshold` and `_initiateImminentFundamentalChangeThreshold`
**Problem**: Verify threshold values are appropriate (12.0f and 22.0f)
**Action**: Check if these values make sense for gameplay

---
## Testing Execution Order
**Recommended Order**:
1. **Pre-Testing Checklist** - Verify code issues first
2. **Test Suite 1** - Basic functionality (foundation)
3. **Test Suite 8-13** - Fundamental locking system (new feature, test early)
4. **Test Suite 2** - Director priority (critical feature)
5. **Test Suite 3** - WorldShuffler (integration)
6. **Test Suite 4** - Sequencer (integration)
7. **Test Suite 5** - MusicMode transitions (complex behavior, includes lock checks)
8. **Test Suite 6** - Silent mode (edge case)
9. **Test Suite 7** - Tutorial/FrozenFreeplay (includes lock checks)
10. **Test Suite 11** - Unlock resolution (critical lock behavior)
11. **Test Suite 12** - Tracking behavior (verifies lock doesn't break tracking)
12. **Test Suite 14** - Edge cases (robustness)
13. **Test Suite 15** - Backward compatibility (safety)
14. **Test Suite 16** - Integration (end-to-end)

---
## Success Criteria
**All tests pass if**:
- ✅ No crashes or exceptions
- ✅ All soundscapes play correctly
- ✅ Director priority system works
- ✅ WorldShuffler functions correctly
- ✅ Mode transitions preserve InteractionType
- ✅ Silent mode behaves correctly
- ✅ **Fundamental locking system works correctly**:
  - Mode locks set/clear correctly
  - Content locks set/clear correctly
  - Debug locks work correctly
  - Priority system functions correctly
  - Unlock resolution works correctly
  - Tracking continues when locked
  - Changes queue/apply when unlocked
- ✅ No regressions in existing functionality

---
## Notes
- **InteractionType System**: This is a key feature - it tracks whether SoundWorld or MusicLoop is active and restores correct Wwise states on mode transitions. This is well-designed.
- **Soundscape vs SoundWorld**: Good decision to keep SoundWorld as a specific type while Soundscape is the umbrella term.
- **Fundamental Locking System**: Three-tier priority system (DebugLock > ContentLock > ModeLock) provides flexible control over fundamental changes. Content locks automatically set/clear based on MusicLoop requirements.
- **Testing Focus**: Pay special attention to mode transitions, Director queue priority, and fundamental locking interactions, as these are the most complex interactions.
- **Lock Priority**: DebugLock (highest) > ContentLock > ModeLock (lowest). This allows development overrides, content requirements, and gameplay constraints to coexist.
- **Tracking Persistence**: `ChangeFundamentalTimer` continues incrementing even when locked, ensuring smooth transitions when locks are released.
