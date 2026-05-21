# Optimization Branch Review

**Branch reviewed:** `origin/optimization` merged into `WorkingWwise+optimization-test`
**Commits reviewed:**
- `5fc94800` — *Optimize audio processing and reduce Wwise calls*
- `e1fba291` — *update player+quality settings*

**Reviewer:** Cursor agent (Claude)
**Review date:** 2026-05-21
**Review scope:** Compatibility of optimization changes with the recent audio-flow refactor work documented in:
- `Docs/STEP_3A_F1_PIVOT_BRIEF.md`
- `Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md`
- `Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md`
- `Docs/MIC_VOICE_INGEST_FIX_PLAN.md`
- `Docs/MIC_PIPELINE_REFACTOR_PLAN.md`
- `Docs/AUDIO_RELIABILITY_FLOW_AND_CLICK_ANALYSIS.md`

**Files touched by the optimization branch (5):**

```
Assets/Scripts/CSVUtility/DataOutput.cs            |  2 +-
Assets/Scripts/Voice/ImitoneVoiceIntepreter.cs     | 51 +++++++++++++++++++++-----
Assets/WwiseBGManager.cs                           |  4 +-
ProjectSettings/ProjectSettings.asset              |  4 +-
ProjectSettings/QualitySettings.asset              |  4 +-
```

**Author of the changes:** mudassar024 <mudassar.hussain@jinnbyte.com>, dated 2026-05-15.

---

## Verdict Summary Table

Each change is rated by impact: **HIGH-CONCERN** (deliberate design change to consciously accept), **MEDIUM** (subtle semantics worth understanding), **LOW** (safe-to-keep wins).

| # | Change | Verdict |
|---|---|---|
| H1 | `Application.targetFrameRate = 30` in `WwiseBGManager.Awake` | **Strip it out.** Keep the vSync disable, drop the targetFrameRate line. |
| H2 | `QualitySettings` Standalone preset 5 → 0 ("Very Low") | **Inspect visually first.** "Medium" or "Low" might be a better trade. |
| H3 | `scriptingBackend: Standalone = 1` (IL2CPP) + `managedStrippingLevel: Standalone = 2` | **Build-test before committing.** Don't ship without a full Standalone smoke test. |
| M1 | `imitone.GetState()` raw-string short-circuit in `GetRawVoiceData` | Keep. Watch for noise-floor-edge oddities. |
| L1 | Reusable `_rawMicKeysToRemove` list (eliminates per-frame alloc) | Keep — aligned with recent GC-hygiene work. |
| L2 | Rolling-sum caches for `volumes1s` and `anomalyBaselineVolumes` (replaces LINQ `.Average()`) | Keep — correct math, real perf win. |
| L3 | Skip redundant `AkSoundEngine.SetSwitch("ToneActive", ...)` calls | Keep — pure CPU/Wwise-traffic win. |
| L4 | Skip redundant `AkSoundEngine.SetRTPCValue("Unity_Inhale", ...)` calls | Keep — same shape as L3. |
| F1 | `DataOutput.cs` — buffered CSV writes without periodic flush | Optional follow-up: add `writer.Flush()` per write to avoid crash-data-loss. |

**Robin's comments:** _(add notes here)_

---

## HIGH-CONCERN: 3 items

### H1. `Application.targetFrameRate = 30` (in `WwiseBGManager.Awake`)

```csharp
void Awake()
{
    DontDestroyOnLoad(gameObject);
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = 30;
}
```

**This is the single biggest behavioral change in the branch.** It hard-caps the entire game to 30 FPS.

Implications versus the recent audio work:

- **FAIL_OBSERVATION counters are frame-based.** `mainThreadFramesSinceLastImitoneStateChange` (aka `imitoneStateStallFrames` in your logs) is documented in `MIC_VOICE_INGEST_FIX_PLAN.md` line 1454 as: *"if it climbs, the main thread is starved (game logic problem, not audio-thread problem)"*. At 30 FPS instead of 60+, every frame is twice as long in real time. Thresholds calibrated against your recent FAIL logs will fire at half the frequency they used to — or differently. Your `imitoneStateStallFrames` baselines (96, 9768, 29105 in `UnityLog_REVIEW.md`) become a different signal.
- **GC click risk could increase, not decrease.** A 33 ms main-thread frame gives garbage collection more room to land in. The `AUDIO_RELIABILITY_FLOW_AND_CLICK_ANALYSIS.md` Phase 7 explicitly calls out "Frame-time pressure (low FPS windows, for example 20-30 FPS)" as a *test scenario* — not a target operating mode.
- **Audio thread is unaffected** (it runs at ~21 ms / ~46 Hz callback rate regardless), so the imitone audio-thread feed is fine. But anything driven from `Update()` — breath coroutines, tone-active timers, RespirationTracker, light/Wwise breath display from `lightControl.Wwise_BreathDisplay(_waveValue)` — now runs half as often. Some of those use `Time.deltaTime` correctly and will be perceptually fine; others (anything with per-frame state machines or counters) may behave differently.
- **UI animation, screen fades, and stage transitions will visibly stutter.** The recent `TUTORIAL_OPENING_MUSIC_HANDOFF_PLAN.md` and `CALIBRATION_UI_SEQUENCING_PLAN.md` work was almost certainly authored and felt-tested at higher framerates.

**Recommendation:** Don't accept this line as-is. Either remove it (keep the optimization branch's other wins) or make it a `SerializeField` so you can A/B compare. The vSync disable is reasonable to keep; the 30 FPS cap is a much heavier commitment.

**Robin's comments:** _(add notes here)_

---

### H2. `QualitySettings` Standalone preset 5 → 0 ("Very Low")

```diff
- m_CurrentQuality: 5
+ m_CurrentQuality: 0
...
- Standalone: 5
+ Standalone: 0
```

Wholesale switch from "Ultra" to "Very Low" for Standalone builds. This swaps every render-quality knob: shadows, anti-aliasing, texture quality, particle counts, post-processing budget, per-preset vSync, LOD bias, etc.

- The recent audio reliability work was done at quality 5. CPU/GPU envelope is now very different.
- Visual fidelity is a separate concern from this review, but the change should be a deliberate choice.
- Some quality presets in Unity have their own vSync setting, which can override `WwiseBGManager`'s `vSyncCount = 0` in Awake depending on initialization order.

**Recommendation:** Inspect the Quality settings dialog before committing to this. If a lower quality is desired, picking "Medium" or "Low" intentionally is probably wiser than "Very Low."

**Robin's comments:** _(add notes here)_

---

### H3. Standalone `scriptingBackend = IL2CPP`, `managedStrippingLevel = 2`

```diff
  scriptingBackend:
+   Standalone: 1
  managedStrippingLevel:
+   Standalone: 2
```

- `scriptingBackend: Standalone = 1` switches Standalone builds from Mono to IL2CPP.
- `managedStrippingLevel: Standalone = 2` sets stripping to "Medium" — more aggressive than the default Low.

**Editor play mode does not exercise either of these.** All Editor testing on `WorkingWwise+optimization-test` will pass regardless of what these are set to. The first time you'll feel the difference is when you build a Standalone player.

Known risks specific to this codebase:
- **Imitone is a native plugin** (the dylib). The marshalling between C# and the dylib lives in `ImitoneVoice` etc. IL2CPP handles `[DllImport]` differently than Mono in some edge cases (struct marshalling, callbacks). Has not been validated in this repo.
- **JSON parsing** (`JSONObject` in `GetRawVoiceData`) uses reflection. At managedStrippingLevel 2, some reflective access can be stripped. Usually fine for `JSONObject` (it's pretty self-contained), but worth a build test.
- **Wwise** uses native interop heavily but is generally IL2CPP-safe.
- Build times will increase substantially (IL2CPP compiles C# → C++ → native).

**Recommendation:** Keep these changes only if there's commitment to shipping IL2CPP builds. If staying on Mono for now, revert to `scriptingBackend: {}` and `managedStrippingLevel: ...` without the Standalone entries. **Do a full Standalone build test before relying on this.**

**Robin's comments:** _(add notes here)_

---

## MEDIUM-CONCERN: 1 item

### M1. `imitone.GetState()` raw-string short-circuit in `GetRawVoiceData`

```csharp
string rawState = imitone.GetState();
imitoneGetStateCallTotal++;

if (rawState == _lastImitoneStateRaw)
{
    mainThreadFramesSinceLastImitoneStateChange++;
    return;
}
_lastImitoneStateRaw = rawState;
imitoneState = rawState;
```

This skips the entire JSON parse + state-update block when `GetState()` returns the same string two frames in a row. Reviewed carefully against `MIC_VOICE_INGEST_FIX_PLAN.md` line 1175, which documents the original counter design.

**What still works correctly:**
- `mainThreadFramesSinceLastImitoneStateChange` (your `imitoneStateStallFrames`) increments and resets correctly in both paths. Diagnostic telemetry preserved.
- When voice is being detected normally, imitone's power readings fluctuate enough that the state string differs every frame — short-circuit rarely fires, and behavior is identical to before.
- The pre-existing comment confirms this: *"In normal operation the counter stays near 0 because real input induces tiny power fluctuations every callback."* The optimization rides on that same observation.

**What changes subtly:**
- During silence (imitone reports empty tones), the side-effect block that re-evaluates `micIsNearNoiseFloor = _dbMicrophone <= _noiseFloorThreshold;` and `imitoneActive = gameOn && !micIsNearNoiseFloor;` no longer runs each frame. These can lag the current `_dbMicrophone` value by N frames until imitone's state string changes. `_dbMicrophone` is updated continuously on the audio thread, so the stale window is short, but at the noise-floor edge there's a theoretical 1–N frame delay before `micIsNearNoiseFloor` reflects ambient drift.
- During an imitone hang (FAIL_OBSERVATION case), the original code parsed the same stale state every frame; the new code freezes those side effects. **Arguably more correct** (don't act on stale data) and definitely cheaper.

**`_lastImitoneStatePower` / `_lastImitoneStatePitchHz` are not updated in the short-circuit path.** This is fine: since `rawState` matched, the parsed floats would also have been the same — comparing the eventual fresh parse against the stale float baseline yields the same result either way.

**Recommendation:** Keep it. The subtle staleness is unlikely to affect gameplay. Worth watching for any noise-floor-edge oddities during testing (`micIsNearNoiseFloor` flipping less responsively than before).

**Robin's comments:** _(add notes here)_

---

## LOW-CONCERN: 4 items — all keep

### L1. Reusable `_rawMicKeysToRemove` list

```diff
-       List<int> keysToRemove = new List<int>();
+       _rawMicKeysToRemove.Clear();
```

Eliminates a per-frame `List<int>` allocation. **Directly aligned with the recent audio refactor's GC-allocation hygiene priorities** (see `MIC_VOICE_INGEST_FIX_PLAN.md` Step 3b GC-alloc detection). Pure win.

**Robin's comments:** _(add notes here)_

---

### L2. Rolling-sum caches for `volumes1s` and `anomalyBaselineVolumes`

Replaces O(n) `LINQ .Average()` calls with O(1) `sum / count`. Every modification path was traced (`Add`, `RemoveAt`, `Clear`, and the special 75-entry "seed with highest volume" branch) — all correctly update the rolling sum. Math is preserved.

Caveat: floating-point drift in rolling sums accumulates over time. For `volumes1s` (1-second window, ~60–120 entries) drift is negligible. For `anomalyBaselineVolumes` (60-second window, ~600 entries) drift over a 30-minute session is bounded to small fractions of a dB. Not user-perceptible.

**Robin's comments:** _(add notes here)_

---

### L3. Wwise `SetSwitch("ToneActive", ...)` redundancy elimination

```csharp
if (_lastToneActiveSwitchState != true)
{
    AkSoundEngine.SetSwitch("ToneActive", "Toning", gameObject);
    _lastToneActiveSwitchState = true;
}
// ...
if (_lastToneActiveSwitchState != false)
{
    AkSoundEngine.SetSwitch("ToneActive", "Resting", gameObject);
    _lastToneActiveSwitchState = false;
}
```

Verified via grep that these two lines are the ONLY places the `ToneActive` switch is set in the live codebase (the third hit is `OLD_ImitoneVoiceInterpreterForDebugComparison.cs`, a reference copy). So the `_lastToneActiveSwitchState` cache is authoritative. Wwise treats redundant `SetSwitch` as idempotent, so skipping is a pure CPU/Wwise-traffic win.

**Minor footnote:** if Wwise ever re-initializes mid-session (scene reload that destroys/recreates the AkInitializer, Wwise plugin error recovery), the cache could be desynced from Wwise's internal state. Not a real risk in SoundSelf's current usage — no dynamic Wwise re-init happens — but worth knowing if that ever changes.

**Robin's comments:** _(add notes here)_

---

### L4. Wwise `SetRTPCValue("Unity_Inhale", ...)` redundancy elimination

Same shape as L3, same verdict. The `_lastInhaleRTPCValue` cache is the single source of truth, sole call site, and `Mathf.Approximately` is the correct comparison for floats. Note the `_waveValue` is computed from a smooth lerp, so it usually *does* change every frame — the short-circuit rarely fires in practice. It's free insurance against the rare frame where it doesn't change.

**Robin's comments:** _(add notes here)_

---

## ADDITIONAL: 1 item worth fixing as a follow-up

### F1. `DataOutput.cs` — buffered CSV writes without periodic flush

```csharp
filePath = Path.Combine(Application.streamingAssetsPath, "SessionData.csv");
writer = new StreamWriter(filePath, false) { AutoFlush = false };
```

Buffering session data is a fine I/O optimization. `OnDisable` calls `writer.Close()` which flushes, so clean shutdowns are safe.

**The hole:** on a crash, force-quit, or process kill, the unflushed buffer is lost. The session writes ~1 line/sec via `InvokeRepeating`; with the default ~1 KB buffer that's ~5–10 lines of data at risk per crash. For session telemetry/research data this matters more than for game runtime.

**Suggested follow-up fix** (small, low-risk):

```csharp
void WriteSessionData()
{
    // ... existing WriteLine call ...
    writer.Flush();
}
```

Or alternatively keep the optimization fully and accept the crash-data-loss risk.

**Robin's comments:** _(add notes here)_

---

## Cross-reference: does any of this undo recent audio work?

**Short answer: no, but H1 and H2 interact with it.**

- The `STEP_3A_F1_HYBRID_RING_FEED_PLAN` work moved imitone's feed to the audio thread. None of the optimization changes touch that file (`ImitoneVoiceIntepreter.AudioThread.cs`) or the ring-buffer code. ✅
- The `MIC_PIPELINE_REFACTOR_PLAN` work introduced `MicPipeline` and a clean update-order contract. None of the optimization changes touch `MicPipeline`, `DirectVoiceMonitoring`, or `RecordedAudioPlayback`. ✅
- The recent FAIL_OBSERVATION machinery (`MicVoiceIngestDebugAggregate`) is untouched. Telemetry still works. ✅
- The CSV recording flow / replay alignment is untouched. ✅
- HOWEVER: **the 30 FPS cap will reframe the meaning of every frame-based threshold** the recent work calibrated. That's not a code conflict, it's an *interpretation* conflict for diagnostics.

---

## Suggested Integration Plan

1. **Stay on `WorkingWwise+optimization-test` for now** — don't merge into `WorkingWwise` yet.
2. Open the project, test in Editor with the merge as-is. Confirm imitone responsiveness, FAIL_OBSERVATION counters, and breath/tone audio behave normally.
3. Once Editor-validated, prepare a small **cleanup commit** on this test branch that surgically:
   - Removes the `Application.targetFrameRate = 30` line from `WwiseBGManager.cs`.
   - Optionally reverts the `QualitySettings` change (or keeps it — decide after visual inspection).
   - Optionally reverts the `ProjectSettings` IL2CPP/stripping change (or keeps it).
   - Optionally adds `writer.Flush()` to `DataOutput.WriteSessionData()`.
4. After sign-off on the cleaned-up branch, fast-forward it into `WorkingWwise`.

This way the wins are cherry-picked and H1–H3 are not silently accepted.

**Robin's comments:** _(add notes here)_
