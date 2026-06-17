# PlaygroundStageHandler Code Review

## Summary

The `PlaygroundStageHandler` refactor correctly mirrors the original `ProtocolStacksCoroutine` logic for the timed steps (20 min → 19:30 → 16 → 13 → 12 → 10 → 4 → 60 → 0). A few bugs and robustness issues were identified.

---

## ✅ Correct Behavior (vs Original)

| Aspect | Original | New Handler | Status |
|--------|----------|-------------|--------|
| Step thresholds | 20 min, 19:30, 16, 13, 12, 10, 4 min, 60s, 0 | Same | ✅ |
| Step 1 actions | StopBreathworkCycle, Freeplay, ShiftingEarth, StartPlayground, ExcludeShadow | Same | ✅ |
| Step 2–8 actions | AddActionToQueue, BeginShuffle, ExcludeSoundscape, StopShuffle, etc. | Same | ✅ |
| Countdown source | `_countdownToSavasana` (set by CSVLoader, decremented in Sequencer.Update) | Same | ✅ |
| ForceSequenceAdvance | `_forceSequenceAdvanceRequested` | `ForceSequenceAdvanceRequested` property | ✅ |
| calibrationMenu null check | None (could throw) | Added null check before warning | ✅ Improved |
| Savasana logic | In coroutine | Intentionally in SavasanaStageHandler (Phase 3.4) | ✅ Per plan |

---

## ⚠️ Bugs & Issues

### 1. **Exit() does not stop the coroutine (zombie coroutine risk)**

**Problem:** If `Exit()` is called while the Playground coroutine is still running (e.g. manual `AdvanceToStage(0)` or `AdvanceToStage(2)`), the coroutine continues. This can cause duplicate `AddActionToQueue`, `BeginShuffle`, etc., while another stage is active.

**Original:** `ProtocolStacksCoroutine` was started by `ProtocolStacksPlaygroundStart()` and never stopped; there was no stage-pipeline concept, so this case didn’t exist.

**Fix:** Store the coroutine reference and stop it in `Exit()`:

```csharp
private Coroutine _playgroundCoroutine;

public void Enter(string variant)
{
    // ...
    _playgroundCoroutine = _sequencer.StartCoroutine(PlaygroundCoroutine());
}

public void Exit()
{
    if (_playgroundCoroutine != null && _sequencer != null)
    {
        _sequencer.StopCoroutine(_playgroundCoroutine);
        _playgroundCoroutine = null;
    }
}
```

---

### 2. **No re-entry guard (double coroutine if Enter called twice)**

**Problem:** Unlike `OpeningStageHandler`, there is no `_hasEntered` guard. If `Enter()` is called twice without `Exit()` in between (e.g. `AdvanceToStage(1)` when already at stage 1), two coroutines run in parallel.

**Fix:** Add a guard similar to `OpeningStageHandler`, or rely on fixing issue #1 so `Exit()` stops the previous coroutine before re-entry. The `Exit()` fix above is the primary solution; a guard is optional extra safety.

---

### 3. **`_countdownToSavasana` initialization dependency**

**Problem:** The handler assumes `_countdownToSavasana` is set by `CSVLoader.TimeLeftInitializations()` before Playground starts. Default is `1000000.0f`. If CSVLoader never sets it (e.g. wrong game mode or load order), the handler waits for countdown ≤ 20 min, which would take ~11.5 days.

**Original:** Same dependency; `ProtocolStacksCoroutine` also relied on CSVLoader.

**Mitigation:** Ensure Phase 4 wires CSVLoader/SequenceDefinition so countdown is set before Playground. The existing `Sequencer.Start()` warning when `_countdownToSavasana >= 999999` remains useful.

---

### 4. **`savasanaCountdownCompleteFlag` not set by handler**

**Problem:** `Sequencer.Update()` sets `savasanaCountdownCompleteFlag = true` when `_countdownToSavasana <= 0`. The handler does not set it. In the original, the coroutine set `_countdownToSavasana = -1` at the end, and Update then set the flag.

**New flow:** Handler sets `_isComplete = true` when countdown ≤ 0. Update still sees `_countdownToSavasana <= 0` and sets the flag. So behavior is consistent.

**Status:** No change needed; Update handles it.

---

## 🔍 Edge Cases Checked

| Scenario | Result |
|----------|--------|
| `calibrationMenu` null | Handler warns; original would throw. ✅ Improved |
| `director`, `worldShuffler`, `lightControl` null | Both throw; no regression |
| Sequencer destroyed mid-coroutine | Both throw; no regression |
| `ForceSequenceAdvanceRequested` reset | Reset after each wait; matches original |
| Countdown decrement | Still in `Sequencer.Update()` when `calibrationMenu.startedExperience`; unchanged |
| `startButtonFlag` / `StartTrueStart()` | Still in `Sequencer.Update()`; unchanged |

---

## Recommendations

1. **High:** Implement the `Exit()` fix to stop the coroutine and avoid zombie coroutines.
2. **Medium:** Add a re-entry guard in `Enter()` for extra safety.
3. **Low:** Document the CSVLoader/countdown dependency in the handler or plan.

---

## Phase 4 Integration Notes

When wiring `Cue_StartInteractive` → `sequenceRunner.AdvanceToNextStage()`:

- Ensure `CSVLoader` has run `TimeLeftInitializations()` before the user reaches Playground (current Protocol Stacks flow does this).
- `SavasanaStageHandler` (Phase 3.4) should perform: `SetFundamentalContentLock`, `ActivateQueue`, `Disable`, `SetMusicModeTo(MusicLoopSilent)`, `PlayAscendingClosing`, and `_countdownToSavasana = -1`.
