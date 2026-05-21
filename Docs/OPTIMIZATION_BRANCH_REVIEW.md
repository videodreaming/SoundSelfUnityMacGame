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

## Product Context (informs every verdict below)

**SoundSelf is an audio-focused experience.** The bulk of the user-facing content is Wwise-driven audio (music, voice guidance, ambient soundscapes, breath effects). Visuals matter primarily for **UI** (sequencing transitions, calibration screens, fades, breath-display) — there are no high-fidelity 3D scenes, no real-time-twitch gameplay, and no requirement for high-FPS visual smoothness. Sessions can run **30+ minutes** on thermally constrained laptops.

This framing materially affects the H1, H2, H3 verdicts:

- A 30 FPS cap is a *normal* optimization choice for audio-focused apps. Many DAWs and audio meditation apps cap visual frame rate to reduce CPU/GPU contention with audio threads, lower thermal output (= quieter fans = better audio experience), and extend battery life.
- Quality preset reductions are similarly normal for audio-first apps where shader/particle/shadow fidelity is not a design priority.
- The recent audio refactor work explicitly designed FAIL_OBSERVATION around **wall-clock time, not frame counts** (see audit below). Lowering FPS does not break any automated audio reliability alarm.

---

## Verdict Summary Table (updated post-audio-focus reframe)

Each change is rated by impact: **HIGH-CONCERN** (deliberate design change to consciously accept), **MEDIUM** (subtle semantics worth understanding), **LOW** (safe-to-keep wins).

| # | Change | Verdict |
|---|---|---|
| H1 | `Application.targetFrameRate = 30` in `WwiseBGManager.Awake` | **Keep concept, refactor home.** The actual `targetFrameRate` setting moves out of `WwiseBGManager` and into a new `PowerAwareFrameRate` component (see `H1b`). |
| H1a | `stalledWriteHeadFrameThreshold = 120` (constant in `MicIngest.cs`) | **Refactor to time-based** (`stalledWriteHeadTimeoutSeconds = 2f`). Option B from chat — keep existing int frame counter as diagnostic, drive the actual trigger off a seconds accumulator. |
| H1b | New `PowerAwareFrameRate` MonoBehaviour (battery-aware framerate) | **Add.** Caps frame rate to `30` when plugged in, `20` when on battery. Polls `SystemInfo.batteryStatus` every 5 s, mirrors `UIManager.RefreshBatteryUiFromSystem` semantics. Replaces the hardcoded `Application.targetFrameRate = 30` in `WwiseBGManager.Awake`. |
| H2 | `QualitySettings` Standalone preset 5 → 0 ("Very Low") | **Inspect visually first** — but the audio-focus framing makes "Very Low" entirely defensible if UI still looks right. |
| H3 | `scriptingBackend: Standalone = 1` (IL2CPP) + `managedStrippingLevel: Standalone = 2` | **Skip — revert.** Minor perf savings for an audio-focused app where the heavy lifting is already native (Wwise + imitone dylib), against 5–20× longer build times and real risk of silent build-only breakage from reflection-stripping. Not worth the development headache right now. Revisit later as a deliberate, separate exercise with proper build-validation budget. |
| M1 | `imitone.GetState()` raw-string short-circuit in `GetRawVoiceData` | **Keep.** No noise-floor system interaction; the one theoretical `imitoneActive` lag edge case is structurally zero-frequency at 20–30 FPS targets. |
| L1 | Reusable `_rawMicKeysToRemove` list (eliminates per-frame alloc) | Keep — aligned with recent GC-hygiene work. |
| L2 | Rolling-sum caches for `volumes1s` and `anomalyBaselineVolumes` (replaces LINQ `.Average()`) | Keep — correct math, real perf win. |
| L3 | Skip redundant `AkSoundEngine.SetSwitch("ToneActive", ...)` calls | Keep — pure CPU/Wwise-traffic win. |
| L4 | Skip redundant `AkSoundEngine.SetRTPCValue("Unity_Inhale", ...)` calls | Keep — same shape as L3. |
| F1 | `DataOutput.cs` — buffered CSV writes without periodic flush | **Skip.** Robin accepts the crash-data-loss risk. Keep the buffered-write optimization as-is. |

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

> **Verdict revised 2026-05-21 after audio-focus reframe and frame-dependency audit.** Original concern was overstated. See full audit below.

**This is a deliberate optimization request from Robin, with strong rationale for an audio-focused experience.** A 30 FPS cap on a UI-only-visual app is a standard technique for:

- Lower CPU/GPU usage → less thermal throttling → steadier audio thread
- Quieter machine (less fan noise) — **direct audio-quality benefit during meditation**
- Longer battery life on laptops (relevant for 30+ minute sessions)
- More headroom for Wwise + imitone audio processing

#### Frame-dependency audit (the question that mattered)

I searched the codebase for code that depends on frame count rather than wall-clock time. Findings:

**1. Every FAIL_OBSERVATION trigger is wall-clock-based, not frame-based.** Verified in `MicVoiceIngestDebugAggregate.cs`. Every flag (`FAIL_AUDIO_CALLBACK_FROZEN`, `FAIL_AUDIO_CALLBACK_RATE_LOW`, `FAIL_AUDIO_CALLBACK_GAP_HIGH`, `FAIL_AUDIO_LOCK_CONTENTION`, `FAIL_IMITONE_NOT_FED`, `FAIL_IMITONE_FEED_RATIO_LOW`, `FAIL_MONITORING_STARVATION_GROWING`, `FAIL_MIC_NOT_READY`, `FAIL_DB_TEAR_DETECTED`) drives off `Time.realtimeSinceStartup` or rolling 1-second wall-clock windows. **None of them care about frame rate.** The audio reliability architecture is already FPS-agnostic by design.

**2. `mainThreadFramesSinceLastImitoneStateChange` (`imitoneStateStallFrames` in logs) is diagnostic-only,** not a threshold trigger. It appears only in the log output for human eyes. Tooltip: *"Stays near 0 in normal operation; climbs only if imitone is hung."* No automated alarm depends on it. **Cosmetic only:** historical log values from higher-FPS sessions need 2× mental adjustment when read against new 30 FPS logs.

**3. `AUDIO_RELIABILITY_FLOW_AND_CLICK_ANALYSIS.md` Phase 7 explicitly lists "20-30 FPS"** as a deliberate *test scenario* for pressure-testing reliability. Re-reading the original wording: the intent was "make sure the system tolerates low FPS," not "30 FPS is forbidden." The architecture was pre-validated against this case.

**4. One real frame-based threshold worth adjusting** (split out as `H1a` below): `stalledWriteHeadFrameThreshold = 120` in `MicIngest.cs:22`. At 60 FPS that's a 2-second mic-stall recovery timeout; at 30 FPS it becomes 4 seconds. Not broken, just slower recovery. Easy fix.

**5. All coroutines using `yield return null`** in `Sequencing/`, `Tutorial.cs`, `Director.cs`, `AVSSequence.cs`, etc. either use `Time.deltaTime` accumulators (FPS-independent ✅) or poll state changes (33 ms vs 16 ms detection latency — perceptually invisible).

**6. `Time.frameCount` usages** are all benign: same-frame duplicate-call detection in `LerpUtilities.DampTool`, debug log throttles (`% 30`, `% 600`), and `UpdateMicReadFrame` idempotency.

#### Estimated savings (qualitative — no profiling done)

For a UI-heavy audio app going from 60+ FPS uncapped to 30 FPS:

| Resource | Estimated impact |
|---|---|
| CPU (main-thread Update/LateUpdate/coroutine ticks) | ~50% fewer ticks → ~20–40% lower main-thread CPU load |
| GPU | ~50% lower draw rate → comparable GPU savings |
| Thermal / fan noise | Substantial reduction. **Direct audio-experience benefit** (quieter machine during meditation). |
| Battery (laptops) | Meaningful — ~20–40% longer session battery life is plausible. |
| Audio reliability (clicks, FAIL_OBSERVATION) | **No regression expected.** Audio thread is FPS-independent. Reduced thermal throttling may slightly *help* by keeping CPU clocks steadier. |
| UI smoothness | Drops to 30 FPS. For SoundSelf's slow fade/breath-display style, almost certainly imperceptible. |

#### Caveats to be aware of (not blockers)

1. **`stalledWriteHeadFrameThreshold`** — see `H1a` below.
2. **UI feel** — verify visual smoothness by eye, especially on screen fades, calibration UI, and breath-display visualization. If anything feels janky, the cap can be raised to 60 (still plenty of room).
3. **`imitoneStateStallFrames` log values** — when comparing logs from before/after the cap, mentally divide pre-cap values by ~2 to compare like-for-like (or just compare against the same-session baseline).

**Recommendation: Keep the 30 FPS cap.** Apply the `H1a` fix to `stalledWriteHeadFrameThreshold` alongside. Optionally promote `targetFrameRate` to a SerializeField on `WwiseBGManager` so you can A/B compare without code edits.

**Robin's comments:** _(add notes here)_

---

### H1a. `stalledWriteHeadFrameThreshold = 120` — refactor to time-based (Option B)

Adjacent finding from the audit, surfaced as its own item because it's the *only* genuinely frame-dependent setting that interacts with the framerate cap.

```22:22:Assets/Scripts/Voice/ImitoneVoiceIntepreter.MicIngest.cs
    [SerializeField] private int stalledWriteHeadFrameThreshold = 120;
```

This drives the mic-stall recovery timeout in `UpdateMicReadFrame`:
- At 60 FPS: 120 frames = ~2 seconds before recovery is scheduled.
- At 30 FPS: 120 frames = ~4 seconds.
- At 20 FPS (on battery — see `H1b`): 120 frames = ~6 seconds. **Way too slow.**

**Resolution: convert the trigger to wall-clock seconds (Option B from chat).** Keep the existing int frame counter so the existing `debugMicLastStalledWriteHeadFrameCount` telemetry continues to work unchanged — but add a parallel float accumulator that drives the actual recovery trigger.

#### Code change shape

In the field block of `ImitoneVoiceIntepreter.MicIngest.cs`:

```csharp
[Tooltip("How long (seconds) with no microphone write-head movement before mic recovery is scheduled. " +
         "Replaces the previous frame-count threshold so behavior is FPS-independent. " +
         "Default 2 s preserves the original 60 FPS / 120-frame behavior.")]
[SerializeField] private float stalledWriteHeadTimeoutSeconds = 2f;

private float stalledWriteHeadStallSeconds;
```

At the existing stall-counter increment site:

```csharp
if (lastMicWritePosition == micPosWrite)
{
    stalledWriteHeadStallSeconds += Time.unscaledDeltaTime;
    stalledWriteHeadFrameCount++;   // kept for telemetry only
}
else
{
    stalledWriteHeadStallSeconds = 0f;
    stalledWriteHeadFrameCount = 0;
}
```

At the trigger site:

```csharp
// Was: if (stalledWriteHeadFrameCount >= Mathf.Max(5, stalledWriteHeadFrameThreshold)) { ... }
if (stalledWriteHeadStallSeconds >= Mathf.Max(0.1f, stalledWriteHeadTimeoutSeconds))
{
    ScheduleRecoveryAttempt(...);
    // ... existing handler body unchanged ...
}
```

The `stalledWriteHeadFrameThreshold` SerializeField is removed; the new `stalledWriteHeadTimeoutSeconds` replaces it. `Time.unscaledDeltaTime` is used to make this resilient to any future `Time.timeScale` changes (the mic should still recover on real time, not game time).

#### Reset points

`stalledWriteHeadStallSeconds` must reset to 0 wherever `stalledWriteHeadFrameCount` resets. Audit shows two existing reset sites in `MicIngest.cs` (around lines 1096 and 1277, plus the `else` branch above). Both get a parallel `stalledWriteHeadStallSeconds = 0f;` line.

#### Behavior preservation

- Default `2f` seconds matches the original intent (120 frames at 60 FPS = 2 s).
- The int `stalledWriteHeadFrameCount` is preserved for `MicVoiceIngestDebugAggregate.aggMicStalledWriteHeadFrames` and `debugMicLastStalledWriteHeadFrameCount` telemetry. No aggregator code change required.
- `Mathf.Max(0.1f, stalledWriteHeadTimeoutSeconds)` floor preserves the spirit of the old `Mathf.Max(5, ...)` floor (don't let the user inspector-edit it to a ridiculously small value).

**Robin's comments:** _(add notes here)_

---

### H1b. New `PowerAwareFrameRate` MonoBehaviour — battery-aware FPS cap

New component to replace the hardcoded `Application.targetFrameRate = 30` in `WwiseBGManager.Awake`. Adapts FPS to whether the laptop is plugged in.

#### Why this lives in its own component

`WwiseBGManager`'s single responsibility is calling `AkSoundEngine.RenderAudio()` each frame. Framerate adaptation is a separate concern with its own poll cadence and SerializeFields. Decoupling keeps both readable and lets you remove either independently.

#### Battery detection semantics — matches `UIManager.RefreshBatteryUiFromSystem`

The existing battery UI (`UIManager.cs:976-997`) treats only `BatteryStatus.Discharging` as "on battery"; `Charging`, `Full`, `NotCharging`, and `Unknown` are all treated as "plugged or no battery." This component uses the **identical** semantics so the battery icon UI and the framerate cap are always in sync — a single source of truth interpreted consistently.

In practice this means: in the Unity Editor (where `batteryStatus = Unknown`), you'll always get the plugged-in framerate. On a desktop without a battery, same thing. Only on a real laptop running on battery does the lower framerate kick in.

#### Code shape

New file: `Assets/Scripts/PowerAwareFrameRate.cs`

```csharp
using UnityEngine;

/// <summary>
/// Caps the application frame rate based on whether the system is plugged in or on battery.
/// Polls SystemInfo.batteryStatus on a slow cadence. Uses the same "Discharging only = on battery"
/// semantics as UIManager.RefreshBatteryUiFromSystem so the battery icon and the FPS cap agree.
/// </summary>
public class PowerAwareFrameRate : MonoBehaviour
{
    [Tooltip("Target frame rate when on external power (or when battery status is Unknown).")]
    [SerializeField] [Range(15, 120)] private int targetFrameRatePlugged = 30;

    [Tooltip("Target frame rate when running on battery (SystemInfo.batteryStatus == Discharging).")]
    [SerializeField] [Range(15, 120)] private int targetFrameRateBattery = 20;

    [Tooltip("How often to re-check SystemInfo.batteryStatus (seconds). Cheap call, slow cadence is fine.")]
    [SerializeField] [Range(1f, 30f)] private float pollIntervalSeconds = 5f;

    [Tooltip("If true, also forces vSyncCount = 0 in Awake so the targetFrameRate cap actually applies.")]
    [SerializeField] private bool disableVSync = true;

    private bool _initialized;
    private bool _lastAppliedOnBattery;

    void Awake()
    {
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }
        ApplyForCurrentBatteryStatus();
    }

    void OnEnable() => InvokeRepeating(nameof(ApplyForCurrentBatteryStatus), pollIntervalSeconds, pollIntervalSeconds);
    void OnDisable() => CancelInvoke(nameof(ApplyForCurrentBatteryStatus));

    private void ApplyForCurrentBatteryStatus()
    {
        // Same semantics as UIManager.RefreshBatteryUiFromSystem: only Discharging counts as "on battery."
        bool onBattery = SystemInfo.batteryStatus == BatteryStatus.Discharging;
        int target = onBattery ? targetFrameRateBattery : targetFrameRatePlugged;

        if (!_initialized || onBattery != _lastAppliedOnBattery || Application.targetFrameRate != target)
        {
            Application.targetFrameRate = target;
            _lastAppliedOnBattery = onBattery;
            _initialized = true;
        }
    }
}
```

#### `WwiseBGManager` change

Remove the framerate / vSync lines from `WwiseBGManager.Awake`; they're owned by `PowerAwareFrameRate` now.

```csharp
// Before:
void Awake()
{
    DontDestroyOnLoad(gameObject);
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = 30;
}

// After:
void Awake()
{
    DontDestroyOnLoad(gameObject);
}
```

#### Unity Editor setup (Robin to do after code lands)

- [ ] Open the scene that contains the `WwiseBGManager` GameObject (find it via `Ctrl+T` / Hierarchy search).
- [ ] Add the new `PowerAwareFrameRate` component to that same GameObject.
- [ ] Confirm `Target Frame Rate Plugged = 30`, `Target Frame Rate Battery = 20`, `Poll Interval Seconds = 5`, `Disable VSync = true` in the Inspector.
- [ ] Save the scene.
- [ ] Save Project.

**Why on the same GameObject?** `WwiseBGManager` already calls `DontDestroyOnLoad(gameObject)`, so any sibling component on it survives scene loads and stays a single global instance. Adding `PowerAwareFrameRate` there means it gets the same lifecycle for free.

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

> **Verdict revised 2026-05-21 after audio-focus reframe.** For a UI-only-visual audio app, "Very Low" is a defensible choice; the only check needed is "does the UI still look right?"

Wholesale switch from "Ultra" to "Very Low" for Standalone builds. This swaps every render-quality knob: shadows, anti-aliasing, texture quality, particle counts, post-processing budget, per-preset vSync, LOD bias, etc.

**Audio-focus framing:** SoundSelf has no high-fidelity 3D scenes, no real-time-twitch gameplay, no shader-heavy effects that depend on Ultra. The visuals that matter are 2D UI, screen fades, breath-display animation, calibration screens — none of which need Ultra-tier rendering. Pairs naturally with the 30 FPS cap as a unified "low GPU load" optimization profile.

**Real concerns:**
- Some 2D UI text/icon rendering can look soft or jaggy at "Very Low" anti-aliasing settings — eyeball test recommended.
- Some quality presets in Unity have their own vSync setting, which can override `WwiseBGManager`'s `vSyncCount = 0` in Awake depending on initialization order. The vSync field on the "Very Low" preset should be confirmed as "Don't Sync."

**Recommendation:** Open Project Settings → Quality and look at the "Very Low" preset's actual settings (shadow distance, AA mode, vSync). If text/UI rendering looks crisp in Editor play at quality 0, it's good. If it looks soft, bump to "Low" or "Medium" instead.

**Robin's comments:** _(add notes here)_

---

### H3. Standalone `scriptingBackend = IL2CPP`, `managedStrippingLevel = 2` — DECISION: REVERT

```diff
  scriptingBackend:
+   Standalone: 1
  managedStrippingLevel:
+   Standalone: 2
```

> **Decision 2026-05-21:** Revert both. The combination is "minor perf savings for an audio-focused app, against medium-sized development headache and a serious risk of build-only breakage." Not worth the cost right now.

#### What the optimization does

- `scriptingBackend: Standalone = 1` switches Standalone builds from **Mono** (just-in-time C# runtime) to **IL2CPP** (ahead-of-time compilation: C# → C++ → native binary).
- `managedStrippingLevel: Standalone = 2` sets stripping to **"Medium"** — aggressive removal of "unused" .NET code at build time, including unused methods within used classes.

**Both only affect Standalone builds.** Editor play mode does not exercise either of these — all in-Editor testing on `WorkingWwise+optimization-test` would pass even if these settings were silently broken.

#### Why revert, not keep

| Aspect | Cost | Benefit | Net for SoundSelf |
|---|---|---|---|
| **IL2CPP build time** | 5–20× longer Standalone builds (clean) | Marginal runtime CPU savings | Painful during iteration. |
| **IL2CPP runtime perf** | None directly | Single-digit % CPU savings on managed code | Real but small — Wwise + imitone dylib already do the heavy lifting in native code. |
| **Medium stripping** | Real risk of silent build-only breakage (reflection-stripping) | Smaller binary, faster startup | Risk > benefit. |
| **Combined** | Build time + reflection-stripping risk | Modest perf gains | **Not obviously worth it.** |

#### Specific risks for this codebase (the reason "skip" is the right call)

1. **Imitone dylib marshalling** — Imitone is a native plugin. The C# wrapper class (`ImitoneVoice`) uses `[DllImport]` to call into the dylib. IL2CPP has stricter rules around callback marshalling (delegates passed to native code need `[MonoPInvokeCallback]`). **Unknown whether this codebase complies.** The optimization-branch author may not have tested this. Failure mode: built player launches, mic does nothing, imitone returns junk.
2. **Reflection-stripping** — Medium stripping analyzes call graphs and removes anything that "looks unused." Any code accessed reflectively (`Type.GetType`, `Assembly.GetType`, dynamic method invocation) is at risk of being silently removed. Failure mode: built player throws `NullReferenceException` / `TypeLoadException` at runtime where Editor played fine.
3. **`JSONObject`** (used in `GetRawVoiceData` to parse imitone output) is hand-rolled, not reflection-based, so probably safe — but "probably" is not the same as "validated."
4. **Wwise** is generally IL2CPP-safe (AudioKinetic ships a `link.xml` protecting their types from stripping), but should still be verified in a build.

These risks are **invisible until you build and run a Standalone player.** Editor testing gives a false sense of security.

#### Why bundling these two changes together is normal

These two settings travel together because Medium stripping is the recommended default *when* using IL2CPP. The optimization-branch author bundling them is conventional; the bundling is not the problem — the runtime risk is.

#### Resolution (handled in Phase 2 of the integration plan)

Revert these two diff hunks in `ProjectSettings/ProjectSettings.asset`:

```diff
  scriptingBackend:
-   Standalone: 1
+   (back to empty / default Mono)
  managedStrippingLevel:
-   Standalone: 2
+   (back to empty / default Low)
```

This way SoundSelf keeps all the real wins from the optimization branch (30 FPS cap, imitone optimizations, Wwise call deduplication, GC alloc reductions) without committing to the IL2CPP build pipeline before it's been validated. IL2CPP can be revisited later as its own dedicated effort.

**Robin's comments:** Let's skip this.

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

This skips the entire JSON parse + state-update block when `GetState()` returns the same string two frames in a row.

> **Section revised 2026-05-21** after Robin flagged a concern about the noise floor system being designed to work when imitone reports empty tones. Initial analysis overstated the risk — the noise floor *calibration* is fully independent of `GetRawVoiceData`. See "Noise floor system is unaffected" below.

#### What still works correctly

- `mainThreadFramesSinceLastImitoneStateChange` (your `imitoneStateStallFrames`) increments and resets correctly in both paths. Diagnostic telemetry preserved.
- When voice is being detected normally, imitone's power readings fluctuate enough that the state string differs every frame — short-circuit rarely fires, and behavior is identical to before.
- The pre-existing comment confirms this: *"In normal operation the counter stays near 0 because real input induces tiny power fluctuations every callback."* The optimization rides on that same observation.
- `_lastImitoneStatePower` / `_lastImitoneStatePitchHz` are not updated in the short-circuit path. Fine: since `rawState` matched, the parsed floats would also have been the same.

#### Noise floor system is unaffected

The noise floor system is in three independent pieces. None of them are gated by `GetRawVoiceData`'s parse block:

| Piece | Where | What it does | M1 impact |
|---|---|---|---|
| `SetNoiseFloorThreshold` | `ImitoneVoiceIntepreter.cs:613`, called from `Update` line 417 **before** `GetRawVoiceData` | Per-frame jump-detector. Reads `_dbMicrophone` directly. Launches/restarts the calibration coroutine when mic rises above the local floor. | **None.** Runs every frame regardless of M1. |
| `MeasureNoiseFloorCoroutine` | `ImitoneVoiceIntepreter.cs:665` | The actual calibration. Waits for `_dbMicrophone` to drop below the drop-exit threshold (silence), holds, samples for 1.5 s, computes median, applies `_noiseFloorThreshold` and `micIsNearNoiseFloor`. | **None.** Runs as its own coroutine; reads `_dbMicrophone` via its own `yield return null` poll loop. The "works during silence" semantics come from `_dbMicrophone`-based silence detection inside the coroutine, NOT from imitone's empty-tones branch. |
| `UpdateToneActiveTelemetryInspector` | `ImitoneVoiceIntepreter.cs:444`, called from `Update` line 420 **after** `GetRawVoiceData` | Refreshes `micIsNearNoiseFloor = _dbMicrophone <= _noiseFloorThreshold;` (line 461) every frame. | **None.** Overwrites `micIsNearNoiseFloor` ~microseconds after M1 either skips it or sets it. The field is never stale beyond one method-call boundary. |

So the answer to "the noise floor system is designed to work when imitone reports empty tones — will M1 break it?" is **no**. The calibration is driven by `_dbMicrophone` (continuously updated by the audio thread), not by imitone's empty-tones state. The empty-tones branch in `GetRawVoiceData` was incidentally writing `micIsNearNoiseFloor`, but that line is redundant — `UpdateToneActiveTelemetryInspector` re-writes it every frame anyway.

#### Other fields the empty-tones branch sets

When silence starts (rawState changes from tone-bearing → empty-tones), the parse runs once and sets `pitch_hz = 0`, `imitoneActiveRaw = false`, `imitoneActive = false`, `_dbValue = PowerToDb(0f)`, `_level = 0`. On subsequent string-stable silence frames, M1 short-circuits and these values stay pinned. **This is correct behavior** — there's nothing to refresh because the inputs (no tone reported) imply identical zeros.

#### The one theoretical `imitoneActive` lag — quantified, structurally cannot trigger at target framerates

`imitoneActive` is set only inside `GetRawVoiceData`, and during a tone-bearing state its value depends on `micIsNearNoiseFloor`:

```csharp
imitoneActive = gameOn && !micIsNearNoiseFloor;
```

For `imitoneActive` to lag by N frames, **three conditions must all be true simultaneously:**

1. Imitone reports a **tone-bearing** state (the empty-tones branch hardcodes `imitoneActive = false`, so silence is unaffected).
2. `imitone.GetState()` returns the **byte-identical JSON string** across consecutive main-thread frames.
3. `_dbMicrophone` crosses the noise floor threshold during those frames.

Condition 2 is the structural constraint. There are exactly two ways it can happen:

##### Way A: Audio thread did not produce new analysis between main-thread reads

The audio thread runs imitone at ~46 Hz (~21 ms per callback). The main thread reads `GetState()` once per `Update`. If main FPS > audio FPS, some main frames sample imitone *between* audio callbacks and see the same state both times.

| Main FPS | Frame time | Audio callbacks per main frame | Identical-state frame rate |
|---|---|---|---|
| 144 FPS (uncapped) | 6.9 ms | ~0.33 | ~66% of frames |
| 60 FPS | 16.7 ms | ~0.78 | ~22% of frames |
| 30 FPS (plugged target) | 33.3 ms | ~1.55 | **0%** (always ≥1 new callback) |
| 20 FPS (battery target) | 50 ms | ~2.33 | **0%** |

**At the target framerates, Way A is mathematically impossible.** Every main-thread frame sees at least one new audio callback's worth of imitone state.

##### Way B: Audio thread produced new analysis, but the result was byte-identical

Requires the underlying audio buffer to produce **bit-identical analysis output** through imitone's algorithm across consecutive callbacks. With real microphone input — ambient noise, ADC noise, mic preamp noise, the chaotic micro-fluctuations of human voice — this is effectively impossible during active toning. The pre-existing code comment in `ImitoneVoiceIntepreter.cs:885` confirms: *"real input induces tiny power fluctuations every callback."*

The only realistic Way B scenario is **imitone is hung** (audio-thread feed dead) — which is already caught and reported by the existing `FAIL_OBSERVATION` machinery (`FAIL_AUDIO_CALLBACK_FROZEN`, `FAIL_IMITONE_NOT_FED`, etc.). Different problem, different system.

##### Even if it triggered, downstream debouncing absorbs the lag

Look at what consumes `imitoneActive`. `CheckToning` runs the value through 50 ms / 200 ms timer-based debouncing (`positiveActiveThreshold1` / `negativeActiveThreshold1`) before flipping the user-facing `toneActive` / `toneActiveConfident` flags. A 1–2 main-frame lag at 20–30 FPS (50–100 ms) would propagate identically into the timers, but the existing debounce thresholds are designed to absorb exactly this scale of jitter. **User-perceptible behavior of `toneActive` would be unchanged** even in the impossible-trigger case.

##### Counterintuitive insight

The optimization gets **safer** the lower the main framerate goes, because at lower main FPS each `Update` reads imitone state after multiple new audio callbacks — eliminating the Way A path entirely.

#### Verdict

**Keep it.** No noise floor system interaction. The theoretical `imitoneActive` lag is structurally zero-frequency at the target 20–30 FPS framerates, and downstream debouncing absorbs it even in scenarios where it cannot actually trigger.

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

### F1. `DataOutput.cs` — buffered CSV writes without periodic flush — DECISION: SKIP (accept risk)

```csharp
filePath = Path.Combine(Application.streamingAssetsPath, "SessionData.csv");
writer = new StreamWriter(filePath, false) { AutoFlush = false };
```

> **Decision 2026-05-21:** Robin accepts the crash-data-loss risk. No code change. The buffered-write optimization stays as the optimization branch ships it.

Buffering session data is a fine I/O optimization. `OnDisable` calls `writer.Close()` which flushes, so clean shutdowns are safe.

The hole on a crash, force-quit, or process kill: the unflushed buffer is lost. The session writes ~1 line/sec via `InvokeRepeating`; with the default ~1 KB buffer that's ~5–10 lines of data at risk per crash. For SoundSelf's research/telemetry CSV this is acceptable — crash sessions are already an irregular data point and a few seconds of missing tail-end CSV isn't analytically meaningful.

If this stance ever changes, the fix is one line at the end of `WriteSessionData()`:

```csharp
writer.Flush();
```

**Robin's comments:** Crash data loss risk is ok

---

## Cross-reference: does any of this undo recent audio work?

**Short answer: no.** Originally I flagged that the 30 FPS cap would interact with the recent audio work; after the frame-dependency audit, that concern is dropped.

- The `STEP_3A_F1_HYBRID_RING_FEED_PLAN` work moved imitone's feed to the audio thread. None of the optimization changes touch that file (`ImitoneVoiceIntepreter.AudioThread.cs`) or the ring-buffer code. ✅
- The `MIC_PIPELINE_REFACTOR_PLAN` work introduced `MicPipeline` and a clean update-order contract. None of the optimization changes touch `MicPipeline`, `DirectVoiceMonitoring`, or `RecordedAudioPlayback`. ✅
- The recent FAIL_OBSERVATION machinery (`MicVoiceIngestDebugAggregate`) is untouched. Every alarm uses wall-clock time, not frame counts — the 30 FPS cap does not affect any automated reliability check. ✅
- The CSV recording flow / replay alignment is untouched. ✅
- The audio thread itself is FPS-independent (fires at ~46 Hz / 21 ms regardless), so imitone analysis cadence and the imitone-feed ring-read pattern are unchanged at 30 FPS. ✅

The **only** real interaction between the 30 FPS cap and recent audio work is the one frame-based threshold flagged in `H1a` (`stalledWriteHeadFrameThreshold`), which is a small Inspector tweak.

---

## Integration Plan (step-by-step)

This plan is **the operational checklist** for getting the optimization branch merged into `WorkingWwise` safely. Each step is a checkbox; complete in order.

Branch context at plan time:
- Current test branch: `WorkingWwise+optimization-test`
- Already contains: the `5fc94800` + `e1fba291` optimization commits merged into the latest `WorkingWwise` (commit `b1fd42d7`).
- `WorkingWwise` itself is untouched and `origin/WorkingWwise` is in sync with local.

---

### Phase 1 — Baseline Editor smoke test (Robin, before any code edits)

Goal: confirm the merge-as-is doesn't break anything in Editor before we start refining.

- [x] Open Unity on `WorkingWwise+optimization-test`. Wait for full reimport.
- [x] Open the Main scene. Press Play.
- [x] **Imitone responsiveness:** Tone briefly into the mic. Verify pitch tracking responds in real time (no multi-second lag).
- [x] **FAIL_OBSERVATION:** Inspect `MicVoiceIngestDebugAggregate` in the Inspector during Play. All `FAIL_*` flags should remain false in silence and during normal voicing.
- [x] **Audio behavior:** Listen for clicks, ToneActive switch drift, RTPC stuck values. None should be present.
- [~] **UI feel:** Note any visible stutter on screen fades, calibration screens, breath display. (At 30 FPS some softening is expected; "still feels fine" is the pass bar.)
- [~] **Note any anomalies** in `Robin's comments` on this doc.

**Stop here if any of the above fail.** Bring the issue to me before continuing.

**Robin Observations** The UI controlled by InputLevelChantingEllipsesVisual seems much slower to me, especially in how it responds to gameOn and toneActive. I could be imaginging it but I don't think so. 

---

### Phase 1 Findings — Chant feel framerate-dependence (root-caused & fixed)

Robin's Phase 1 observation was correct and traced to **three pre-existing, compounding framerate-dependent bugs** in the chant feel code, all silently exposed by capping `Application.targetFrameRate` to 30. The first two were found and fixed before Robin's second round of testing; the third was uncovered in Robin's second-round observation that the color fade still felt slow.

**Bug A — Color-lag ring buffer was frame-count-based, not time-based**

In `Assets/Jinnbyte/SoundSelfUI/Scripts/InputLevelChantingEllipsesVisual.cs`, `PushAndSampleColorLag` converted `lagSeconds` to a ring-buffer offset with a hardcoded `Mathf.RoundToInt(lagSeconds * 60f)`. The ring was filled one sample per `LateUpdate`, so the actual perceived lag scaled inversely with framerate:

| Ellipse preset | Intended lag | At 60 FPS | At 30 FPS | At 20 FPS |
|---|---|---|---|---|
| 0 (`colorLagSeconds = 0.2f`) | 0.2 s | 0.2 s | 0.4 s ❌ | 0.6 s ❌ |
| 1 (`colorLagSeconds = 0f`) | 0 s | 0 s | 0 s | 0 s |
| 2 (`colorLagSeconds = 0.4f`) | 0.4 s | 0.4 s | 0.8 s ❌ | 1.2 s ❌ |

**Bug B — `LerpUtilities.DampTool` applied a constant per-frame Lerp factor**

In `Assets/Scripts/Utilities/LerpUtilities.cs`, both damp stages did `Mathf.Lerp(current, target, damp)` once per frame with the raw `damp` value and **no `Time.deltaTime` scaling**. That's classic frame-rate-dependent exponential decay: half-life scales linearly with frame interval.

For a representative `damp = 0.1` tuning:

| FPS | Half-life | Ratio to 60-FPS design |
|---|---|---|
| 60 | ~110 ms | 1.0× (design target) |
| 30 | ~220 ms | 2.0× slower |
| 20 | ~330 ms | 3.0× slower |

The codebase-wide blast radius for `DampTool` is small and concentrated in chant feel:
- `GameValues.cs`: `_meanToneLengthLerp` (chant-rate scheduler input) and `_chantChargeContributions` (chant charge buildup / decay).
- `InputLevelChantingEllipsesVisual.cs`: scale damp, color damp, spin damp.

The comment in `GameValues` even calls this out: `"the quicker values were painstakingly set to match the original soundself defaults"` — the values are hand-tuned for 60 FPS frame cadence.

**Bug C — `GameValues.handlecChanting()` inlined the same anti-pattern**

Lines 243–271 of `Assets/Scripts/Voice/GameValues.cs` are the canonical source of `_chantLerpSlow` and `_chantLerpFast` — the values that drive the ellipses color/scale, the `Unity_ChantLerpFast` / `Unity_ChantLerpSlow` Wwise RTPCs, and `LightControl`'s strobe-tone display. They are **not** computed via `DampTool`; the math is inlined as bare `Mathf.Lerp(value, target, damp)` calls per Update tick, with per-frame depletion (`_chantLerpSlowDepletion`, `_chantLerpFastDepletion`) subtracted flat, plus a per-frame linear creep (`_chantLerpLinear = 0.0001f`). Same anti-pattern as the old `DampTool`, but the earlier fix did not reach it.

This is the dominant cause of the **color fade (to dark blue)** still feeling slow after Bugs A & B were fixed: even though the ellipses' own damps were corrected, their *input* (`_chantLerpFast`) was still decaying at half-rate at 30 FPS.

Codebase-wide audit of other `Mathf.Lerp` / damp call sites was performed (`LightControl`, `MusicBinauralBeats`, `UIManager`, `ScreenFadeEffect`, `HorizontalSlideFadeEffect`, `AudioLevelUtilities`, `RespirationTracker`, `DirectVoiceMonitoring`, `RecordedAudioPlayback`, `VoiceInterpreter`, `CurveUtility`, `ImitoneVoiceIntepreter`). All are either (a) time-based already (`t = elapsed / duration`), (b) value-mapping lerps (not over-time damping), (c) per-audio-buffer crossfades on a different timebase, (d) explicitly continuous-time-correct (`alpha = 1 - exp(-dt/tau)` in `DirectVoiceMonitoring`), or (e) archived/commented-out code. **`handlecChanting` was the only remaining anti-pattern.**

**Combined effect on what Robin observed**

At 30 FPS, the entire chant ellipses visual system responded with **roughly 2× the design-intent lag** along three independent axes that all compounded on the same visual output (color-lag ring, ellipse damps, and chant-lerp source values). After Bugs A & B fixes, the source values (Bug C) remained the residual cause of the still-slow color fade.

**Fixes applied (✅ all three done before Phase 2)**

1. **Bug A — `LerpUtilities.DampTool`** — Now scales `damp1`, `damp2`, and `linear` by `Time.unscaledDeltaTime * 60f`:
   - At 60 FPS the multiplier is 1.0 → **mathematically identical to legacy behavior** (no chant-feel regression for anyone running at the design framerate).
   - At lower framerates `effDamp = 1 - (1 - damp)^multiplier`, which is the correct exponential decay for the actual frame interval. 30 FPS now produces the same wall-clock response as 60 FPS did.
   - Guards: zero-dt early exit (editor pause / first frame), `Mathf.Clamp01` on damp inputs.
2. **Bug B — `InputLevelChantingEllipsesVisual.PushAndSampleColorLag`** — Rewritten to walk the ring **by wall-clock timestamp** (`Time.unscaledTime`) instead of by fixed frame count. Adds parallel `_colorLagTime` and `_colorLagCount` arrays. Worst case O(96) reads per ellipse per frame — negligible cost. Behavior at 60 FPS is unchanged; behavior at 30/20 FPS now matches the `colorLagSeconds` design intent.
3. **Bug C — `GameValues.handlecChanting()`** — Introduced a local `frScale60 = Time.unscaledDeltaTime * 60f` at the top of the function and computes `effSlowDamp1/2`, `effFastDamp1/2`, `effSlowDamp1Down`, `effFastDamp1Down`, `effSlowDepletion`, `effFastDepletion`, and `effLinear` once per tick using the same `1 - (1 - damp)^multiplier` formula (and `linear * multiplier` for the depletion and creep). These are substituted into the existing `Mathf.Lerp` / `Mathf.Clamp` calls in both the `toneActive` and fade-out branches. Same 60-FPS-identical / lower-FPS-corrected guarantee as Bugs A & B. Guards: zero-dt early exit (Wwise RTPCs still pushed even on the skipped tick to avoid stale audio values).

All three files passed lint. To be re-verified in Phase 4 by repeating the Phase 1 smoke test, with a specific chant-feel responsiveness check (see Phase 4).

**Dormant fields removed (P1D)**

`GameValues._tChantLerp` and `GameValues._tRestLerp` were `public { get; private set; }` accumulators whose write sites (originally at the bottom of `handlecChanting`) used `Time.fixedDeltaTime` inside `Update()` — also FPS-dependent in a different wrong way (returns the fixed-timestep constant, 0.02s, not actual elapsed time). The field comment read `"not currently referenced, but might be useful for WWise"`.

Confirmed dormant by exhaustive search: no readers anywhere in `Assets/`, no references in any `.unity`, `.prefab`, `.asset`, `.json`, `.xml`, `.wwu`, `.bnk`, `.txt`, or `.csv` file under the project, no reflection-style access (no `nameof`, no `GetProperty`, no debug-menu inclusions), and the fields are not `[SerializeField]`'d so Unity would not serialize them. Both property declarations and both write-site lines were deleted from `GameValues.cs` to remove the dead code path. If the accumulators are ever genuinely needed for a Wwise hookup later, reintroduce them with `Time.deltaTime` (or `Time.unscaledDeltaTime`) — never with `Time.fixedDeltaTime` in `Update()`.

---

### Phase 2 — Code changes (I apply; Robin approves first)

I will NOT touch code until Robin gives explicit go-ahead per workflow rule.

The following changes are queued. The code edits will all go in **one commit** (they are all part of the same coherent "make framerate cap work properly + drop IL2CPP" change). The commit message will be drafted before commit and shown for approval.

**Code edits:**
- [ ] **H1a — `Assets/Scripts/Voice/ImitoneVoiceIntepreter.MicIngest.cs`:** Apply the Option B refactor from this doc (`stalledWriteHeadFrameThreshold` → `stalledWriteHeadTimeoutSeconds` driving the trigger; keep the int frame counter for telemetry).
- [ ] **H1b — `Assets/Scripts/PowerAwareFrameRate.cs` (NEW):** Create the new component per the code shape in this doc.
- [ ] **H1 — `Assets/WwiseBGManager.cs`:** Remove `QualitySettings.vSyncCount = 0;` and `Application.targetFrameRate = 30;` from `Awake` (ownership moves to `PowerAwareFrameRate`).
- [x] **P1A — `Assets/Scripts/Utilities/LerpUtilities.cs`:** Made `DampTool` frame-rate-independent (multiplier = `Time.unscaledDeltaTime * 60f`, exponential decay via `Mathf.Pow`). Behavior at 60 FPS is mathematically identical; at 30/20 FPS the wall-clock response now matches the 60-FPS design tuning. Fixes Bug A in Phase 1 Findings.
- [x] **P1B — `Assets/Jinnbyte/SoundSelfUI/Scripts/InputLevelChantingEllipsesVisual.cs`:** Rewrote `PushAndSampleColorLag` to walk the ring by wall-clock timestamp instead of fixed frame count. Adds `_colorLagTime` and `_colorLagCount` parallel arrays; updates `EnsureColorArrays` accordingly. Fixes Bug B in Phase 1 Findings (Bug A in original numbering; bugs were renumbered when Bug C was discovered).
- [x] **P1C — `Assets/Scripts/Voice/GameValues.cs`:** Introduced `frScale60 = Time.unscaledDeltaTime * 60f` and precomputed `effSlowDamp1/2`, `effFastDamp1/2`, `effSlowDamp1Down`, `effFastDamp1Down`, `effSlowDepletion`, `effFastDepletion`, `effLinear` at the top of `handlecChanting`. Substituted into the existing `Mathf.Lerp` / `Mathf.Clamp` calls in both branches. Fixes Bug C in Phase 1 Findings. **This is the dominant fix for the chant color fade still feeling slow after P1A/P1B.**
- [x] **P1D — `Assets/Scripts/Voice/GameValues.cs`:** Removed dormant `_tChantLerp` / `_tRestLerp` accumulator properties and their `Time.fixedDeltaTime`-in-`Update()` write sites at the bottom of `handlecChanting`. Verified zero readers across `Assets/` (no scenes/prefabs/assets/json/xml/Wwise files reference them; no reflection-style access; not `[SerializeField]`'d). Eliminates a latent FPS-dependent dead code path.
- [x] **P1E — `Assets/Jinnbyte/SoundSelfUI/Scripts/InputLevelChantingEllipsesVisual.cs` (color chain tuning):** Discovered during Phase 1 re-test that with the data layer now FPS-correct, the color fade still felt sluggish. Root cause: the visual was re-damping `_chantLerpFast` (already a twice-damped value from `GameValues`) through another two-stage `DampTool` using the *slow* chant damp rates — combined "feels-done" time of ~3–4 seconds at 60 FPS by accidental design. Per Robin's call ("since these are already tracking a damped value, we don't need to redamp it"), the color path was simplified: each ellipse now reads a wall-clock-time-lagged sample of `_chantLerpFast` directly from its ring buffer and lerps `ChantTeal` → `ChantWhite` with no additional damp. Visual variation between the three ellipses now comes entirely from their `colorLagSeconds` (0.0 / 0.2 / 0.4) — no redundant damp knob. Code changes: removed the `_colorDamped` state array; removed `ColorDampKey`; removed `colorChantDampRateMultiplier` from `EllipsePreset` and all three preset entries; simplified the color block in `LateUpdate`; updated `EnsureColorArrays` and `OnDisable`; rewrote the class-level XML doc summary.
- [x] **P1F — `Assets/Scripts/Voice/GameValues.cs` (inspector cleanup):** Removed `[SerializeField]` from `_chantLerpSlowDamp1`, `_chantLerpSlowDamp2`, `_chantLerpFastDamp1`, `_chantLerpFastDamp2` (the four chant damp fields). These are recomputed every `Update()` from `_meanToneLengthLerp` via `LerpAndInverse`, so serializing them was misleading — any inspector edit would be overwritten on the next frame. The public `ChantLerpSlowDamp1` / `ChantLerpSlowDamp2` getters used by scale/spin damp paths in `InputLevelChantingEllipsesVisual` are unaffected. Existing zero-valued entries in `Assets/Scenes/MainGame.unity` (`_chantLerpSlowDamp1: 0` etc.) become orphan tags that Unity will clean up automatically on next scene save. Made `_chantLerpFast` / `_chantLerpSlow` inspector-visible via `[field: SerializeField]` (P1E-companion change for live-diagnosis of chant feel).
- [x] ~~**F1 — `Assets/Scripts/CSVUtility/DataOutput.cs`:** Add `writer.Flush();`~~ → **Decided: skip.** No code change. Crash-data-loss risk accepted.

**ProjectSettings revert (H3):**
- [ ] **H3 — `ProjectSettings/ProjectSettings.asset`:** Revert the `scriptingBackend: Standalone = 1` and `managedStrippingLevel: Standalone = 2` entries back to the defaults present on the pre-merge `WorkingWwise` tip (commit `8c321f7f`). This keeps Standalone builds on Mono with default Low stripping. **Important:** Unity may also rewrite this file on next open if any other Player Settings differ; the revert should be done with Unity closed, then verified by `git diff` before any further Unity actions.

**Verification:**
- [ ] Run linter / no new warnings on any of the C# files above.
- [ ] `git diff` review: confirm only the expected files are touched (`MicIngest.cs`, `PowerAwareFrameRate.cs`, `WwiseBGManager.cs`, `LerpUtilities.cs`, `InputLevelChantingEllipsesVisual.cs`, `GameValues.cs`, plus `ProjectSettings.asset`). No stray edits. (Note: `MainGame.unity` may show orphan `_chantLerpSlowDamp1:` / `_chantLerpSlowDamp2:` / `_chantLerpFastDamp1:` / `_chantLerpFastDamp2:` keys at first open due to the P1F `[SerializeField]` removal; Unity will clean those up on the next scene save. Per workflow rule, do not stage scene changes without explicit save+permission.)

**Awaiting Robin's go-ahead for this phase.** _(write "go ahead on phase 2" when ready)_

---

### Phase 3 — Unity Editor setup (Robin, after Phase 2 lands)

Code is in but the new component is not yet attached to any GameObject. Robin does this.

- [ ] Open Unity. The new `PowerAwareFrameRate.cs` should compile cleanly (check Console for errors).
- [ ] Open the Main scene.
- [ ] Find the GameObject that has the `WwiseBGManager` component on it (Hierarchy search for "Wwise" → it should be obvious; commonly named something like "AkInitializer" / "WwiseBGManager" / a global manager root).
- [ ] **Add Component → `Power Aware Frame Rate`** onto that same GameObject.
- [ ] In the Inspector, confirm defaults:
   - `Target Frame Rate Plugged` = `30`
   - `Target Frame Rate Battery` = `20`
   - `Poll Interval Seconds` = `5`
   - `Disable VSync` = `true` (checked)
- [ ] Save the scene (`Ctrl+S`).
- [ ] File → Save Project.
- [ ] Press Play. Check the **Stats** overlay (in Game view, click "Stats" button) — frame rate should read ~30 FPS in Editor (since `batteryStatus == Unknown` → treated as plugged).
- [ ] Stop Play. Notify Cursor agent for Phase 4.

---

### Phase 4 — Verification (Robin runs, I help interpret)

Repeat the Phase 1 Editor smoke test, but now with the refactored code in place.

- [ ] Imitone responsiveness still good.
- [ ] FAIL_OBSERVATION flags all stay false in normal session.
- [ ] **Specifically watch `aggMicStalledWriteHeadFrames`** in the Inspector — it should mostly stay at 0; brief blips are tolerable, sustained climbing means the new time-based threshold isn't resetting correctly (Robin: report to Cursor if this happens).
- [ ] No new audio clicks introduced.
- [ ] UI smoothness acceptable.
- [ ] **Chant ellipses responsiveness (Phase 1 regression check):** Tone briefly into the mic and watch the chanting ellipses (`InputLevelChantingEllipsesVisual`). All three of the following should feel **comparable to the pre-cap 60-FPS behavior** — not the sluggish ~2× lag observed earlier:
  - Scale-up on `gameOn`.
  - Color shift toward white during toning.
  - **Color fade back to dark blue/teal after toning stops** (this is the Bug C symptom — should now match design intent).

  If anything still feels sluggish, the P1A/P1B/P1C fixes did not take hold correctly (Robin: report to Cursor).

**On laptop (optional but recommended)** if available:
- [ ] Run the app on a laptop with the charger plugged in. Verify Stats overlay shows ~30 FPS.
- [ ] Unplug the charger. Within ~5 seconds (the poll interval), the framerate should drop to ~20 FPS. Verify by Stats overlay.
- [ ] Plug back in. Within ~5 seconds, the framerate should return to ~30 FPS.

---

### Phase 5 — Quality preset decision (Robin)

H3 (IL2CPP + stripping) was decided in chat — revert, handled in Phase 2. The only remaining quality-related decision is H2.

- [ ] **H2 (Quality preset):** Open Project Settings → Quality. Look at the "Very Low" preset configuration. Press Play. Eyeball test:
   - UI text: crisp or soft?
   - Screen fades: smooth?
   - Battery icon / calibration UI: legible?

   If acceptable, **keep** the change. If not, revert `ProjectSettings/QualitySettings.asset` (could be a small follow-up commit, or amended into the Phase 6 work).

---

### Phase 6 — Commit & merge (gated by Robin's approval)

- [ ] All decisions above made. Robin says "go ahead, commit and merge."
- [ ] I commit the code changes from Phase 2 to `WorkingWwise+optimization-test` (commit message proposed first for approval).
- [ ] If any `ProjectSettings/*.asset` was reverted in Phase 5, that goes in either the same commit or a follow-up commit per Robin's preference.
- [ ] **Merge `WorkingWwise+optimization-test` → `WorkingWwise`:**
  ```powershell
  git checkout WorkingWwise
  git merge WorkingWwise+optimization-test   # fast-forward
  ```
- [ ] Verify `git status` shows clean and up-to-date.
- [ ] Push to origin (Robin's call — I won't push without explicit go-ahead).
- [ ] Delete the test branch:
  ```powershell
  git branch -d WorkingWwise+optimization-test
  ```

---

### Phase 7 — Documentation finalization (post-merge)

- [ ] This doc moves from "review document" to "historical record." Add a final `Outcome` section at the top summarizing which items were kept / reverted / tuned, and the commit hash that integrated everything.
- [ ] Per workflow rule: the commit hash is recorded **in the same commit as the work**, not in a separate "update doc with hash" commit.

---

## Open Questions for Robin

- ~~**F1 (`DataOutput` flush):** include in Phase 2, or skip?~~ → **Decided: skip, accept crash-data-loss risk.**
- **H2 (Quality preset):** decide after Phase 5 visual inspection.
- ~~**H3 (IL2CPP):** decide after Phase 5 build test.~~ → **Decided: revert. Handled in Phase 2.**
- **Phase 6 push:** push immediately after merge, or hold for further local testing first?

**Robin's comments:** _(add notes here)_
