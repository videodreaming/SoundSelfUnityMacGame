# Cue System & Cue-Watching Review

**Date:** March 18, 2025  
**Scope:** Cue firing, `HandleCue`, `TryNotifyCue`, handler `WatchesCue`/`NotifyCue`, and legacy fallbacks.

---

## 1. Architecture Overview

### 1.1 Cue Flow

```
Wwise cue fires
    → WwiseVOManager (VOCallbackFunction or ClosingCallBackFunction)
    → sequencer.HandleCue(CueType)
    → [if StartInteractive && not in sequence] ProtocolStacksPlaygroundStart()
    → [else] sequenceRunner.TryNotifyCue(cue)
        → currentHandler.WatchesCue(cue)?
            → handler.NotifyCue(cue) → MarkComplete()
    → [if Break_Tests && HandleCue returned false] tutorial.EndTutorialNaturally()
```

### 1.2 Cue Types (`StageTypes.cs`)

| CueType | Wwise Cue Name | Handlers That Watch | Legacy Fallback |
|---------|----------------|---------------------|-----------------|
| `StartInteractive` | Cue_StartInteractive | OpeningStageHandler | ProtocolStacksPlaygroundStart (when not in sequence) |
| `Break_Tests` | Cue_Wwise_Tutorial_Break_All_Tests | TutorialStageHandler | tutorial.EndTutorialNaturally() |
| `WaitForButton` | Cue_WaitForButton | WaitForInputStageHandler | None |
| `ThematicSavasana_End` | Cue_ThematicSavasana_End | (none) | None |

---

## 2. Verified Correct Behaviors

### 2.1 StartInteractive

- **When not in sequence** (`sequenceRunner == null` or `CurrentStageIndex < 0`): `HandleCue` calls `ProtocolStacksPlaygroundStart()` and returns `true`. Correct.
- **When in Opening stage**: OpeningStageHandler watches it; `NotifyCue` → `MarkComplete`; sequence advances. Correct.
- **When in Playground/Savasana**: Handler doesn't watch; `TryNotifyCue` returns false; `HandleCue` returns false. WwiseVOManager ignores the return value and does nothing. Correct—we're already past Opening.
- **WwiseVOManager** does not need to check the return value; legacy is handled inside `HandleCue`.

### 2.2 Break_Tests

- **When in Tutorial stage**: TutorialStageHandler watches it; `NotifyCue` → `MarkComplete`. Correct.
- **When not in Tutorial** (or not in sequence): `HandleCue` returns false; WwiseVOManager calls `tutorial.EndTutorialNaturally()`. Correct legacy fallback.
- **Protocol Stacks Ascending** has no Tutorial stage; Break_Tests would only fire in Preparation/Integration flows (legacy). Correct.

### 2.3 WaitForButton

- **When in WaitForInput stage**: WaitForInputStageHandler watches it; `NotifyCue` → `MarkComplete`. Correct.
- **When not in WaitForInput**: `HandleCue` returns false; no legacy. Cue is effectively ignored. Acceptable for current flows.

### 2.4 ThematicSavasana_End

- **SavasanaStageHandler** does not watch it; completes immediately on Enter.
- Cue fires from ClosingCallBackFunction; `HandleCue` returns false; no legacy. Cue is for future use (e.g., early exit from Savasana). Acceptable.

### 2.5 Null Safety

- `WwiseVOManager`: All cue paths check `sequencer != null` before calling `HandleCue`.
- `HandleCue`: Uses `sequenceRunner ??= sequencer.GetComponent<SequenceRunner>()`; checks `sequenceRunner == null` before use.
- `TryNotifyCue`: Checks `CurrentStageIndex < 0` and `handler == null` before calling `WatchesCue`/`NotifyCue`.

---

## 3. Potential Issues & Edge Cases

### 3.1 StartInteractive: sequenceRunner Lazy-Init Timing

**Location:** `Sequencer.HandleCue`

```csharp
sequenceRunner ??= sequencer.GetComponent<SequenceRunner>();
```

`sequenceRunner` is assigned in `HandleCue` via null-coalescing. If `Sequencer.Awake` has not yet run, or if `SequenceRunner` is added at runtime, the first `HandleCue` call will fetch it. This is safe as long as `SequenceRunner` is present when a sequence is expected. No bug, but worth noting for debugging.

### 3.2 Break_Tests: Double Action When Tutorial Is Implemented

**Future concern:** When TutorialStageHandler is fully implemented, `NotifyCue(Break_Tests)` will call `MarkComplete()`. The WwiseVOManager legacy path calls `tutorial.EndTutorialNaturally()` when `HandleCue` returns false. So:

- **In Tutorial stage**: Handler handles cue → `MarkComplete`; WwiseVOManager does NOT run legacy (HandleCue returns true). Good.
- **Not in Tutorial stage**: HandleCue returns false → legacy runs `EndTutorialNaturally()`. Good.

No double action. The logic is correct.

### 3.3 Cue Firing During Stage Transition

**Scenario:** Cue fires in the same frame as `AdvanceToNextStage()` or between `MarkComplete()` and the next `Update()` poll.

- `MarkComplete()` is idempotent; calling it twice is safe.
- `SequenceRunner.Update` polls `IsComplete` and advances. If the cue fires after we've already advanced, the new handler would receive it. If the new handler doesn't watch that cue, we get the "not being watched" warning. This is acceptable—no crash, just a possible spurious log.

### 3.4 Multiple Handlers Watching Same Cue

**Current state:** Only one handler is active at a time (current stage). No overlap. If you add a cue that multiple stages could watch, only the current one receives it. Correct by design.

### 3.5 ClosingCallBackFunction vs VOCallbackFunction

- **VOCallbackFunction**: Opening/tutorial sequences → `Cue_StartInteractive`, `Cue_Break_Tests`, `Cue_WaitForButton`.
- **ClosingCallBackFunction**: Thematic Savasana, Ascending Closing → `Cue_ThematicSavasana_End`.

Cues are routed correctly. No cross-wiring found.

---

## 4. Minor Observations

### 4.1 Logging

- `HandleCue` logs a warning when a cue is "not being watched." This can be noisy if cues fire in stages that don't care (e.g., ThematicSavasana_End in Savasana). Consider reducing to `Debug.Log` or making it configurable for known "future" cues.

### 4.2 Cue_StartInteractive in WwiseVOManager

- The comment "This is hard coded for Protocol Stacks right now" is accurate. The flow is correct; the comment could be updated when other modes are supported.

### 4.3 Stub Handlers

- **TutorialStageHandler** and **WaitForInputStageHandler** complete immediately (`MarkComplete()` in Enter). Cue-watching is wired for when real logic is added. No bug.

---

## 5. Summary

| Area | Status | Notes |
|------|--------|-------|
| Cue routing | OK | VOCallbackFunction vs ClosingCallBackFunction correct |
| HandleCue logic | OK | Legacy for StartInteractive when not in sequence; TryNotifyCue otherwise |
| TryNotifyCue | OK | Null checks, WatchesCue/NotifyCue flow correct |
| Handler implementations | OK | WatchesCue/NotifyCue consistent |
| Null safety | OK | sequencer, sequenceRunner checked |
| Break_Tests legacy | OK | No double action when Tutorial is implemented |
| Edge cases | OK | Stage transitions, idempotent MarkComplete |

**Conclusion:** No bugs or unexpected behaviors identified. The cue system and cue-watching logic are sound. Optional improvement: reduce log verbosity for "not being watched" when the cue is intentionally unused (e.g., ThematicSavasana_End).
