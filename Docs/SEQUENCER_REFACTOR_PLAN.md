# Sequencer Refactoring Plan: Stage Pipeline (Option 1)

## Overview

This plan migrates the Protocol Stacks Ascending sequence to a **Stage Pipeline** architecture. The goal is to make sequences declarative, easy to extend, and UI-friendly.

---

## Current Protocol Stacks Ascending Flow (Reference)

| Order | Stage | Trigger | Key Actions |
|-------|-------|---------|-------------|
| 0 | **Calibration** | User in CalibrationMenu | (Pre-sequence; user clicks Start) |
| 1 | **Opening** | `PlayFirstSequence()` | Light init, `PlayOpeningSequence("Ascending")`, `AVS_Program_DynamicDrop_Start()` |
| 2 | **Tutorial** | Wwise cue TBD | Various tests, similar to current tutorial sequence called by standard sequence |
| 3 | **Playground** | Wwise cue `Cue_StartInteractive` → `ProtocolStacksPlaygroundStart()` | `ProtocolStacksCoroutine()` runs timed steps |
| 4 | **Savasana** | Countdown reaches 0 | `PlayAscendingClosing()` |
| 5 | **WaitForInput** | Wwise cue TBD | User presses a button to play music |
| 6 | **Music** | User presses the button in the pause section | No action, simple countdown timer to end.

(Note that steps 2, 4, and 5 are considered, but not built yet. We should create stub scripts for them, to be completed later)

**Playground sub-steps (countdown-based):**
- ≤20 min: Start interactive music, ShiftingEarth, StartPlayground
- 19:30: Queue SitarAmbience
- 16 min: Queue Shadow + Blue
- 13 min: Queue PinkNoiseAtmosphere
- 12 min: BeginShuffle
- 10 min: ExcludeSoundscape(SonoFlore)
- 4 min: StopShuffle, queue SonoFlore
- 0: Transition to Savasana (Playground completes)

---

## Target Architecture

```
SequenceRunner (MonoBehaviour)
  └── Runs SequenceDefinition
        └── List of (StageType, Variant)
  └── Holds IStageHandler instances
  └── Fires OnStageChanged for UI

IStageHandler
  └── Enter(variant)
  └── Exit()
  └── IsComplete / OnComplete callback
```

---

## Phase 1: Create Core Types (No Behavior Change)

### 1.1 Create `StageType` enum and `SequenceStage` struct

**File:** `Assets/Scripts/WwiseManagers/Sequence/StageTypes.cs` (new)

```csharp
namespace SoundSelf.Sequence
{
    public enum StageType
    {
        Calibration,   // Pre-sequence; user hasn't started
        Opening,
        Tutorial,     // Optional; Protocol Stacks skips
        Playground,
        Savasana,
        WaitForInput,
        MusicPlaylist
    }

    [System.Serializable]
    public struct SequenceStage
    {
        public StageType type;
        public string variant;  // e.g. "Ascending", "Preparation_Long", null = default
    }
}
```

### 1.2 Create `IStageHandler` interface

**File:** `Assets/Scripts/WwiseManagers/Sequence/IStageHandler.cs` (new)

```csharp
namespace SoundSelf.Sequence
{
    public interface IStageHandler
    {
        StageType StageType { get; }
        void Enter(string variant);
        void Exit();
        bool IsComplete { get; }
    }
}
```

### 1.3 Create `SequenceDefinition` (ScriptableObject)

**File:** `Assets/Scripts/WwiseManagers/Sequence/SequenceDefinition.cs` (new)

```csharp
using UnityEngine;

namespace SoundSelf.Sequence
{
    [CreateAssetMenu(fileName = "NewSequence", menuName = "SoundSelf/Sequence Definition")]
    public class SequenceDefinition : ScriptableObject
    {
        public string displayName = "Protocol Stacks Ascending";
        public SequenceStage[] stages;
    }
}
```

**Unity Editor:**
1. Create folder: `Assets/Scripts/WwiseManagers/Sequence/`
2. After adding the script, create asset: **Right-click in Project → Create → SoundSelf → Sequence Definition**
3. Name it `ProtocolStacksAscending`
4. Set stages:
   - [0] type=Opening, variant="Ascending"
   - [1] type=Playground, variant="" (or null)
   - [2] type=Savasana, variant="Ascending"

---

## Phase 2: Create `SequenceRunner`

### 2.1 Create `SequenceRunner` MonoBehaviour

**File:** `Assets/Scripts/WwiseManagers/Sequence/SequenceRunner.cs` (new)

```csharp
using System;
using UnityEngine;

namespace SoundSelf.Sequence
{
    public class SequenceRunner : MonoBehaviour
    {
        [SerializeField] private SequenceDefinition definition;
        [SerializeField] private bool autoStartOnAwake;

        public int CurrentStageIndex { get; private set; } = -1;
        public StageType? CurrentStage => CurrentStageIndex >= 0 && definition != null && CurrentStageIndex < definition.stages.Length
            ? definition.stages[CurrentStageIndex].type
            : null;

        public event Action<int, StageType> OnStageChanged;


        private IStageHandler[] _handlers;  // Populated by Sequencer via SetHandlers() — prefer script-based setup over Inspector

        public void SetDefinition(SequenceDefinition def) => definition = def;
        public void SetHandlers(IStageHandler[] handlers) => _handlers = handlers;

        public void AdvanceToStage(int index)
        {
            if (definition == null || index < 0 || index >= definition.stages.Length) return;
            // Exit current, enter next (implementation in Phase 3)
        }
    }
}
```

**Unity Editor:**
- Do **not** add SequenceRunner to the scene yet. It will be integrated in Phase 4.

---

## Phase 3: Extract Stage Handlers from Sequencer

### 3.1 Create `OpeningStageHandler`

**File:** `Assets/Scripts/WwiseManagers/Sequence/Handlers/OpeningStageHandler.cs` (new)

**Responsibilities (extracted from `PlayFirstSequence`):**
- Light init
- Call `wwiseVOManager.PlayOpeningSequence(variant)` (e.g. "Ascending")
- Start `AVS_Program_DynamicDrop_Start()` — **keep this coroutine in Sequencer**; handler calls `sequencer.StartOpeningAVSProgram()` to avoid moving 200+ lines of AVS logic
- **Completion:** Wwise cue `Cue_StartInteractive` (event-driven; handler sets `IsComplete` when cue received)

**Dependencies:** Sequencer (for AVS coroutine + refs), LightControl, WwiseVOManager. Handler will need a reference to register for the Wwise callback.

### 3.2 Create `PlaygroundStageHandler`

**File:** `Assets/Scripts/WwiseManagers/Sequence/Handlers/PlaygroundStageHandler.cs` (new)

**Responsibilities (extracted from `ProtocolStacksCoroutine`):**
- All countdown-based steps (20 min → 19:30 → 16 → 13 → 12 → 10 → 4 → 0)
- **Completion:** `_countdownToSavasana <= 0`

**Implementation approach:** Keep as coroutine internally, but wrapped in handler. Handler's `IsComplete` returns true when coroutine finishes.

### 3.3 Create stub handlers (Tutorial, WaitForInput, MusicPlaylist)

**Files:** `TutorialStageHandler.cs`, `WaitForInputStageHandler.cs`, `MusicPlaylistStageHandler.cs` (new)

Create stub implementations that satisfy `IStageHandler` but do minimal work (e.g. `Enter` logs, `IsComplete` returns true immediately or after a short delay). To be completed later.

### 3.4 Create `SavasanaStageHandler`

**File:** `Assets/Scripts/WwiseManagers/Sequence/Handlers/SavasanaStageHandler.cs` (new)

**Responsibilities (extracted from end of `ProtocolStacksCoroutine`):**
- `MusicSystem1.instance.SetFundamentalContentLock(NoteName.C)`
- `director.ActivateQueue(15f)`, `director.Disable()`
- `MusicSystem1.instance.SetMusicModeTo(MusicLoopSilent)`
- `wwiseVOManager.PlayAscendingClosing()`
- **Completion:** When closing VO ends (Wwise callback) or after a timeout

---

## Phase 4: Integrate SequenceRunner with Sequencer

### 4.1 Add SequenceRunner to Sequencer

**In Sequencer.cs:**
- Add `[SerializeField] SequenceRunner sequenceRunner;` — or have Sequencer get/add the component at runtime to minimize Editor setup
- Add `[SerializeField] SequenceDefinition protocolStacksAscendingDefinition;` — or resolve from CSVLoader at runtime

**In `StartTrueStart()` (Protocol Stacks path):**
- Instead of calling `PlayFirstSequence()` directly, call `sequenceRunner.StartSequence(protocolStacksAscendingDefinition)` (or similar)
- SequenceRunner advances through stages; each handler performs the work

### 4.2 Wire Wwise Cue to SequenceRunner

**In WwiseVOManager.cs:**
- When `Cue_StartInteractive` fires: call `sequenceRunner.AdvanceToNextStage()` (advance to next stage)
- **Validation:** If the next stage is not Playground, log a warning (e.g. `Debug.LogWarning("Cue_StartInteractive: Expected next stage Playground, got " + nextStage)`)
- The OpeningStageHandler registers for this cue and sets `IsComplete = true`, which triggers SequenceRunner to advance

### 4.3 Wire CSVLoader to Sequence Definition

**In CSVLoader.cs:**
- After `ReadSessionParams()`, select the appropriate `SequenceDefinition` based on `gameMode` and `subGameMode`
- Pass it to Sequencer/SequenceRunner before the experience starts

**Unity Editor:**
- Create a `SequenceDefinition` asset for "Protocol Stacks Ascending"
- Assign it in CSVLoader or a bootstrap component (e.g. `[SerializeField] SequenceDefinition protocolStacksAscending;`)

---

## Phase 5: Implement SequenceRunner Logic

### 5.1 Stage lifecycle

```
AdvanceToStage(n):
  1. If current handler exists: handler.Exit()
  2. CurrentStageIndex = n
  3. Get handler for stages[n].type
  4. handler.Enter(stages[n].variant)
  5. Fire OnStageChanged(n, stages[n].type)
  6. Start polling handler.IsComplete (or subscribe to completion)
  7. When complete: AdvanceToStage(n+1) or end sequence
```

### 5.2 Handler registration

- Sequencer creates and holds the handlers (they need refs to Director, MusicSystem1, etc.)
- Sequencer passes handlers to SequenceRunner via `SetHandlers()` — **prefer script-based setup**; minimize Unity Editor wiring
- SequenceRunner looks up handler by `StageType` when advancing

---

## Phase 6: Add OnStageChanged for UI

### 6.1 UI subscription (deferred)

**For now:** Use `Debug.Log` in SequenceRunner when `OnStageChanged` fires (e.g. `Debug.Log($"Sequence: Entered stage {index} ({stageType})")`). This allows verification without building UI.

**Future:** Create `SequenceProgressUI` component that subscribes to `sequenceRunner.OnStageChanged` and updates canvas elements (stage labels, progress dots) based on `(index, stageType)`. Assign `sequenceRunner` reference in Inspector.

---

## Phase 7: Remove Old Code (After Verification)

### 7.1 Deprecate and remove

- Remove `ProtocolStacksCoroutine()` from Sequencer (logic now in PlaygroundStageHandler)
- Remove `ProtocolStacksPlaygroundStart()` (replaced by stage transition)
- Simplify `PlayFirstSequence()` to only run when not using SequenceRunner, or remove if fully migrated
- Remove `StandardSequenceUpdate()` Protocol Stacks guard (no longer needed)

---

## Detailed Step-by-Step Migration (Protocol Stacks Ascending)

### Step 1: Create folder structure and core types

1. **Unity Editor:** Create folder `Assets/Scripts/WwiseManagers/Sequence/`
2. **Unity Editor:** Create subfolder `Assets/Scripts/WwiseManagers/Sequence/Handlers/`
3. Add `StageTypes.cs`, `IStageHandler.cs`, `SequenceDefinition.cs` as in Phase 1
4. **Unity Editor:** Create ScriptableObject: Right-click → Create → SoundSelf → Sequence Definition → name `ProtocolStacksAscending`
5. In Inspector for `ProtocolStacksAscending`:
   - Size = 3
   - [0] type=Opening, variant=Ascending
   - [1] type=Playground, variant=
   - [2] type=Savasana, variant=Ascending

### Step 2: Create SequenceRunner skeleton

1. Add `SequenceRunner.cs` as in Phase 2
2. Implement `AdvanceToStage`, `StartSequence`, and handler lookup (stub handlers for now)

### Step 3: Implement OpeningStageHandler

1. Create `OpeningStageHandler.cs` in Handlers/
2. Inject Sequencer (or LightControl, WwiseVOManager) via constructor or `[SerializeField]`
3. `Enter(variant)`: Call `lightControl.LightSettingsInitialization(5f)`, `wwiseVOManager.PlayOpeningSequence(variant)`, `sequencer.StartCoroutine(AVS_Program_DynamicDrop_Start())`
4. `IsComplete`: Initially false. Set true when Wwise fires `Cue_StartInteractive` (handler must register for this—either via WwiseVOManager callback or a static/event)
5. **Wire:** In WwiseVOManager, when `Cue_StartInteractive` fires, call `openingStageHandler.MarkComplete()` or similar (handler exposes this method)

### Step 4: Implement PlaygroundStageHandler

1. Create `PlaygroundStageHandler.cs`
2. Move the entire body of `ProtocolStacksCoroutine()` into a private coroutine inside the handler
3. `Enter(variant)`: Start the coroutine
4. `IsComplete`: True when the coroutine finishes (use a flag set at the end of the coroutine)
5. Handler needs refs: Sequencer (for `_countdownToSavasana`, `StartPlayground`, etc.), Director, MusicSystem1, WorldShuffler, LightControl, WwiseVOManager
6. **Important:** Pass these via a context struct or inject through Sequencer

### Step 5: Create stub handlers and implement SavasanaStageHandler

1. Create stub handlers: `TutorialStageHandler.cs`, `WaitForInputStageHandler.cs`, `MusicPlaylistStageHandler.cs` — each implements `IStageHandler` with minimal logic (log on Enter, IsComplete returns true)
2. Create `SavasanaStageHandler.cs`
3. `Enter(variant)`: Run the logic from the end of ProtocolStacksCoroutine (SetFundamentalContentLock, ActivateQueue, Disable, SetMusicModeTo, PlayAscendingClosing)
4. `IsComplete`: True immediately (savasana plays to end; no need to block) or when closing callback fires

**PRogrammer Comment:** Note that for now, any stubs, including tutorial stage handler, should simply skip until code is added.

### Step 6: Integrate into Sequencer

1. Add to Sequencer:
   ```csharp
   [SerializeField] private SequenceRunner sequenceRunner;
   [SerializeField] private SequenceDefinition protocolStacksAscendingDefinition;
   private OpeningStageHandler _openingHandler;
   private PlaygroundStageHandler _playgroundHandler;
   private SavasanaStageHandler _savasanaHandler;
   ```
2. In `Awake` or `Start`: Instantiate handlers, inject dependencies, call `sequenceRunner.SetHandlers(...)`
3. In `StartTrueStart()` when gameMode == "Protocol Stacks": Call `sequenceRunner.StartSequence(protocolStacksAscendingDefinition)` instead of `PlayFirstSequence()`
4. Update WwiseVOManager: `Cue_StartInteractive` → call `_openingHandler.MarkComplete()` or `sequenceRunner.NotifyStageComplete()`

**Unity Editor (minimize where possible):**
- Add `SequenceRunner` component to the same GameObject as Sequencer (or have Sequencer add it in `Awake` if null)
- Assign `protocolStacksAscendingDefinition` — or have Sequencer/CSVLoader resolve it by gameMode at runtime

### Step 7: Test and verify

1. Run with Protocol Stacks Ascending (ensure CSVLoader has `gameMode = "Protocol Stacks"`, `subGameMode = "Ascending"`)
2. Verify: Calibration → Start → Opening plays → Cue_StartInteractive → Playground runs → Countdown 0 → Savasana (Ascending Closing)
3. Use `ForceSequenceAdvance()` during Playground to ensure it still works (may need to expose it through the handler)

### Step 8: Add stage-change logging (UI deferred)

1. In SequenceRunner, when `OnStageChanged` fires: `Debug.Log($"Sequence: Entered stage {index} ({stageType})")`
2. (Future) Create `SequenceProgressUI.cs` and wire to canvas when ready

### Step 9: Cleanup

1. Remove `ProtocolStacksCoroutine`, `ProtocolStacksPlaygroundStart` from Sequencer
2. Remove or simplify `PlayFirstSequence` (keep only for non–Protocol Stacks if not yet migrated)
3. Remove Protocol Stacks–specific branches from `StandardSequenceUpdate` if redundant

---

## Dependency Injection Approach

Handlers need many refs. Two options:

**A) Pass Sequencer to handlers**  
Handlers call `sequencer.director`, `sequencer.MusicSystem1`, etc. Simpler but couples handlers to Sequencer.

**B) Create `SequenceContext` struct**  
```csharp
public struct SequenceContext
{
    public Sequencer Sequencer;
    public Director Director;
    public MusicSystem1 MusicSystem;
    public WorldShuffler WorldShuffler;
    public LightControl LightControl;
    public WwiseVOManager WwiseVO;
    public CalibrationMenu CalibrationMenu;
    public float CountdownToSavasana;  // Ref or getter
}
```
Handlers receive `SequenceContext` in `Enter()`. Cleaner, more testable.

**Recommendation:** Start with (A) for speed; refactor to (B) later if needed.

---

## Files to Create (Summary)

| File | Purpose |
|------|---------|
| `Sequence/StageTypes.cs` | StageType enum, SequenceStage struct |
| `Sequence/IStageHandler.cs` | Interface |
| `Sequence/SequenceDefinition.cs` | ScriptableObject |
| `Sequence/SequenceRunner.cs` | Orchestrator |
| `Sequence/Handlers/OpeningStageHandler.cs` | Opening stage |
| `Sequence/Handlers/PlaygroundStageHandler.cs` | Playground stage |
| `Sequence/Handlers/SavasanaStageHandler.cs` | Savasana stage |
| `Sequence/Handlers/TutorialStageHandler.cs` | Stub (to complete later) |
| `Sequence/Handlers/WaitForInputStageHandler.cs` | Stub (to complete later) |
| `Sequence/Handlers/MusicPlaylistStageHandler.cs` | Stub (to complete later) |
| `Sequence/SequenceProgressUI.cs` | (Future) UI — use logs for now |

---

## Files to Modify

| File | Changes |
|------|---------|
| `Sequencer.cs` | Add SequenceRunner, handlers; change StartTrueStart/PlayFirstSequence; remove ProtocolStacksCoroutine |
| `WwiseVOManager.cs` | Cue_StartInteractive → notify stage complete instead of ProtocolStacksPlaygroundStart |
| `CSVLoader.cs` | Select SequenceDefinition by gameMode/subGameMode; pass to Sequencer/SequenceRunner |

---

## Rollback Plan

- Keep `ProtocolStacksCoroutine` and `ProtocolStacksPlaygroundStart` in a `#if LEGACY_SEQUENCER` block until fully verified
- Use a `[SerializeField] bool useNewSequenceRunner` toggle on Sequencer to switch between old and new paths for testing

---

## Estimated Effort

| Phase | Time |
|-------|------|
| Phase 1 (Core types) | 30 min |
| Phase 2 (SequenceRunner) | 45 min |
| Phase 3 (Handlers) | 2–3 hours |
| Phase 4 (Integration) | 1 hour |
| Phase 5 (Runner logic) | 1 hour |
| Phase 6 (Logging; UI deferred) | 15 min |
| Phase 7 (Cleanup) | 30 min |
| **Total** | **~6–7 hours** |
