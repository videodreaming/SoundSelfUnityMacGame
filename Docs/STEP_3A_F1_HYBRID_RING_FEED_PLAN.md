# Step 3a / F1 — Hybrid ring-feed pivot plan (Cursor's draft)

> **Status (2026-05-06):** **Pass 1 play-tested (Robin).** Perceptual bar cleared (latency vastly improved vs pre-pivot). **Commit when ready**; follow-ups: overflow-drop counter, optional GC threshold, hybrid gap vs 64 ms target. Pass 2+ not started.
>
> **Owner:** This doc. The brief stays as the external-AI artifact; this doc supersedes its implementation prescriptions where they diverge. Behavioral intent (audio-thread cadence + ring-fed imitone, ~64 ms latency target) is unchanged.

---

## Inspector / CURRENT TEST protocol (persistent)

**Rule:** The `CURRENT TEST` block holds **only** what the **active** play test needs, tight enough for **one** Inspector screengrab. When the test changes, **replace** the fields (do not accumulate). Anything else lives in the headed sections below; scroll there for depth.

- Rewrite header, `currentTestDescription`, field list, and `LateUpdate` mirrors in the same edit when the test changes.
- Cross-reference: `Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md` (Debugging Process Convention).

**Pass 1 fields (minimal):** `currentTestSessionTimeSeconds`, `currentTestFailure`, `currentTestHybridFeedGapMs`, `currentTestAudioCallbackHzRolling`, `currentTestImitoneInputToCallbackRatio`, `currentTestAudioFeedOverflowDroppedTotal`, `currentTestMicExitReason`, `currentTestFeedPeakAbs`, `currentTestDbValue`, `currentTestPitchHz`, `currentTestKnownFalsePositive_GcAlloc`, plus `currentTestDescription` (tooltip has the full bar text).

---

## Discoveries, hypotheses & progress log

Append dated rows as we learn things. Keeps handoff and review focused.

| Date | Type | Note |
|------|------|------|
| 2026-05-06 | Decision | Locked: use existing 4-arg `ReadRawSamples`; dummy clip **`stream: false`**; prime cursor then `Play()` same coroutine turn; skip `imitone.InputAudio` when `copied == 0`. |
| 2026-05-06 | Code | Pre-pass-1 grep: `CaptureToMicGap*` and related interpreter APIs used only by `MicVoiceIngestDebugAggregate` under `Assets` (besides definitions). Safe to repoint in pass 2. |
| 2026-05-06 | Hypothesis | Stable ~2.13 s latency on old path = engine streaming-source policy (~100× DSP buffer), not fixable via `timeSamples`. Pivot sidesteps by not feeding imitone from streaming `data[]`. |
| 2026-05-06 | Observation | `audioCallbackLockMissTotal` still counts **`audioRingWriteLock`** misses (parallel ring) through pass 1; not raw-buffer contention. Repoint or supplement in pass 2/3. |
| 2026-05-06 | Implementation | Pass 1: `WaitMicPositionThenPlayCapture` waits on `rawWriteTotalSamples`, primes via `TryCreateRawReadCursorBehindMs`, `[Step3a-pivot]` log. `OnAudioFilterRead` reads raw ring every callback; imitone only when `copied > 0` and past priming. |
| 2026-05-06 | Process | CURRENT TEST trimmed to a **minimal** field set for one screengrab; non-essential mirrors removed (scroll lower sections). |
| 2026-05-06 | **Play test** | **Perceptual:** much improved vs ~2 s floor. **`[Step3a-pivot]`:** gap=3072 sa (~64 ms) at prime — correct. **Hybrid gap** in CURRENT TEST ~181–203 ms (above 40–90 bar but orders of magnitude below old engine floor). **Input/callback ratio** ~0.98 — good. **Hz rolling** ~46–47 — good. **FAILURE** true with **KnownFalsePositive GC** latched — treat as expected until optional pass 4 threshold bump; confirm no other FAIL_* in expanded block. **First grab:** `micExitReason=unread_zero` while pitch/db still moved — likely one-frame ingest branch vs aggregate snapshot timing; **second grab** `copied_samples` when silent — healthy. **Overflow dropped = 32032** — **follow-up:** `ReadRawSamples` overflow guard firing (cumulative samples skipped); investigate whether startup-only or steady-state; may interact with hybrid gap > 64 ms. **Perceptual minor:** `_dbValue` lags slightly after stop toning — likely main-thread analysis/smoothing, not F1 feed path. |

---

`OnAudioFilterRead` keeps firing on the audio thread, but stops reading from `data[]` (the streaming AudioSource path that imposes the 100-DSP-buffer lookahead). Instead, the audio thread pulls from `rawRingBuffer` using the same `Monitor.TryEnter`-based reader that `DirectVoiceMonitoring` already uses. A read cursor is primed on the main thread to sit `audioThreadFeedLatencyMs` (default 64) behind the mic write head; the audio thread advances it at steady cadence. The captureSource loses its mic-clip role and plays a tiny in-memory silent clip just to keep `OnAudioFilterRead` ticking. Most of the work is wiring existing primitives together; the actual new code surface is much smaller than the brief implied.

---

## Pushbacks on the brief (read these first)

I want to surface five places where my read of the code disagrees with what the brief proposes. Some are minor; one is significant.

### 1. (Significant.) No new ring-read method is needed.

The brief says:
> Add `TryReadRawSamplesFromAudioThread(...)` ... The existing `ReadRawSamples` uses blocking `lock`, fine for main-thread callers but not audio-thread.

That's true of the **2-arg overload** (`ImitoneVoiceIntepreter.MicIngest.cs:404`), but **not** the 4-arg overload at `ImitoneVoiceIntepreter.MicIngest.cs:537-630`. The 4-arg overload already uses `Monitor.TryEnter(rawBufferLock, 0)`, already zero-fills + advances cursor on lock miss, already overflow-drops if the consumer falls behind. It is already called from the audio thread by `DirectVoiceMonitoring.OnAudioFilterRead` at line 667. We just call it from our `OnAudioFilterRead` too. Zero new code in MicIngest.cs.

This collapses ~50 lines of "new method" the brief proposed into "use the existing one." Materially shrinks pass scope.

### 2. (Significant.) Cursor must be primed on the main thread, not the audio thread.

The brief proposes initializing `audioThreadFeedReadPosition` inside `WaitMicPositionThenPlayCapture` to "be `audioThreadFeedLatencyMs` behind the current main-thread `rawWritePosition`." That coroutine runs on the main thread, so this is fine in spirit, but the way to do it cleanly is to call the existing helper `TryCreateRawReadCursorBehindMs(delayMs, out readPosition, out readTotalSamples)` (`ImitoneVoiceIntepreter.MicIngest.cs:674-696`). It already grabs `rawBufferLock`, computes the cursor relative to write head, and clamps. It is the same primitive `DirectVoiceMonitoring.PrimeBufferedReadCursorForSource` uses (line 393).

Importantly: do **not** try to lazy-prime inside `OnAudioFilterRead`. The existing 4-arg `ReadRawSamples` snaps an uninitialized cursor to the current write head (zero latency), which is the wrong starting position. Primer goes on main thread before Play.

### 3. (Important detail.) The dummy clip must use `stream:false`.

The brief just says "1024 samples of zeros, looping." Specify `AudioClip.Create(name, lengthSamples, 1, sampleRate, stream:false)`. With `stream:true` we re-engage the same FMOD streaming-clip scheduling policy we are trying to escape (the 100-DSP-buffer lookahead). With `stream:false` the clip is a static in-memory zero buffer the engine just loops; no scheduling lookahead. This is the whole reason the pivot works.

### 4. (Refinement.) Lock-miss policy: skip the InputAudio call, don't feed zeros.

`imitone.cs:80` is explicit:
> Continuous, un-processed audio is usually best. If audio is not continuous, feed imitone about 1/8 second worth of silence (ie, an array of zeroes whose size is sampleRate/8).

The brief proposed feeding zero-padded buffers on lock miss. **Locked decision: skip** — see [Skip vs zero-padded (what that means)](#skip-vs-zero-padded-what-that-means) below.

For **underrun** (lock taken, partial copy because the cursor caught up to the write head momentarily), the 4-arg `ReadRawSamples` already zero-fills the tail; call `imitone.InputAudio` with that buffer so imitone still sees **real samples in the prefix** plus silence only where data was missing. That is different from lock miss (no real samples at all this tick).

#### Skip vs zero-padded (what that means)

**Context:** On lock miss, `ReadRawSamples(..., TryEnter 0)` fails. The helper still **zero-fills** `destination` and **advances** the read cursor by `destination.Length` so the consumer stays time-aligned (same as `DirectVoiceMonitoring`). It returns **`toCopy == 0`**: no samples were copied from the ring.

| Policy | What you do when `toCopy == 0` | Effect on imitone |
|--------|--------------------------------|-------------------|
| **Skip** (chosen) | Do **not** call `imitone.InputAudio` this callback. | One analysis tick is omitted (~21 ms at 1024/48 kHz). No fake "frame of audio" is presented. Internal pitch/power state is not fed a misleading all-zero buffer. |
| **Zero-padded** (rejected for lock miss) | Call `imitone.InputAudio(imitoneFeedBuffer)` anyway; buffer is all zeros from the helper. | Imitone receives a **full** callback of explicit silence as if it were real input. That is a sharp, artificial discontinuity; the docs ask for ~1/8 s of zeros for gaps, not a single DSP buffer of zeros. Risk: extra noise-floor / onset confusion versus simply missing one tick. |

**Summary:** "Skip" = no `InputAudio` call when nothing was read from the ring. "Zero-padded" = still call `InputAudio` with an all-zero frame. They are not the same: zero-padded asserts continuity in the API (a full frame exists) but the content is silence; skip asserts nothing and lets one cadence step pass without analysis input.

**Telemetry:** Preserve **accurate** raw-ring lock-miss counting after pass 3 removes the parallel ring: today's `audioCallbackLockMissTotal` is tied to **`audioRingWriteLock`**, not `rawBufferLock`. Plan: repoint or replace in pass 2/3 so the Inspector reflects contention on the buffer the imitone feed actually shares with `DirectVoiceMonitoring` and the main-thread writer.

### 5. (Refinement.) Inspector latency floor should be 32 ms, not 16 ms.

The brief proposes `[Range(16f, 250f)]`. One audio callback at 1024 samples / 48 kHz is ~21.3 ms. At 16 ms latency, the read cursor is behind the write head by less than one callback interval, which means: (a) any time the main-thread writer is even slightly late, the cursor catches up to the write head and we underrun, and (b) the lock-miss fall-through (which advances the cursor by `destination.Length` to maintain real-time pacing) immediately reads ahead of the write head on the next attempt. Practical floor is one callback period plus headroom, so ~32 ms. 64 ms default still gives ~3 callbacks of headroom which is sane.

---

## Code surface (where the changes land)

Mostly contained in two files plus a debug-aggregate update.

### `Assets/Scripts/Voice/ImitoneVoiceIntepreter.AudioThread.cs`

| Location | Lines | What changes |
|---|---|---|
| Field block | 19-25 | Replace `audioCapturePlayAlignmentBufferCount` with `audioThreadFeedLatencyMs` (`[Range(32f, 250f)] float = 64f`). Tooltip rewritten. |
| Field block | 113-117 | `monoScratch`, `audioThreadRing`, `audioRingWriteLock`, `audioRingWritePosition`, `audioRingWriteTotalSamples` deleted (pass 3). Pass 1 leaves them in place to keep telemetry intact during testing. |
| New fields | (insert near line 31) | Add `audioThreadFeedReadPosition` (int), `audioThreadFeedReadTotalSamples` (long), `imitoneFeedDummyClip` (AudioClip), and an audio-thread overflow counter (`audioFeedOverflowDroppedTotal`, optional) for telemetry. |
| `BootstrapAudioThreadCapturePath` | 174-221 | Drop the `audioThreadRing` allocation block (lines 189-192). Otherwise unchanged. Dummy clip is created in `EnsureCaptureAudioSourceConfigured`. |
| `EnsureCaptureAudioSourceConfigured` | 268-314 | Create the silent dummy clip on first call (or destroy + recreate on rebootstrap if AudioConfig changed). Set `captureSource.clip = imitoneFeedDummyClip`. The `bypassEffects = false` requirement and other source settings stay. |
| `WaitMicPositionThenPlayCapture` | 316-382 | Strip the `timeSamples` math (335-379) and the three F1 diagnostic log blocks. New body: wait for `rawWriteTotalSamples >= audioConfigOutputSampleRate * (audioThreadFeedLatencyMs / 1000) + audioConfigDspBufferSize`, then call `TryCreateRawReadCursorBehindMs(audioThreadFeedLatencyMs, out audioThreadFeedReadPosition, out audioThreadFeedReadTotalSamples)`, then `captureSource.Play()` **in that order** (prime then `Play`, same coroutine turn, no `yield` between — avoids the first `OnAudioFilterRead` running before the cursor exists). One-line `[Step3a-pivot]` log of the primed cursor + computed gap. |
| `OnAudioFilterRead` | 447-620 | Delete the `data[]` → mono-mix block (488-512) and the audio-thread ring write block (526-559). Replace with `imitoneFeedBuffer` realloc-if-needed + 4-arg `ReadRawSamples(imitoneFeedBuffer, ref audioThreadFeedReadPosition, ref audioThreadFeedReadTotalSamples, out overflowDropped)`. Increment overflow counter if dropped. Skip `imitone.InputAudio` on `copied <= 0`; feed normally otherwise. Keep the priming-frames-skip gate, peakAbs telemetry, GC-suspect timer, deferred-exception path, and final `Array.Clear(data, 0, data.Length)`. |
| `CaptureToMicGapSamples` / `CaptureToMicGapMs` | 75-100 | Repurpose. New properties: `AudioThreadFeedToWriteHeadGapSamples` (returns `(int)(rawWriteTotalSamples - audioThreadFeedReadTotalSamples)`) and the `Ms` flavor. Old captureSource-vs-mic gap properties get deleted along with the `data[]` path. |
| `GetAudioThreadHealthSnapshot` | 150-172 | Drop `audioRingWriteTotalSamples`, `audioRingWriteLastClipReadStart`, `audioRingWriteLastClipReadCount` from the snapshot once their producers go away in pass 3. Add `audioThreadFeedReadTotalSamples` and `audioFeedOverflowDroppedTotal` for new telemetry. |
| `StopAudioThreadCapture` | 432-445 | Add `Destroy(imitoneFeedDummyClip)` (or null-check + `null` assign if Destroy is already covered by the OnDisable / scene-teardown path). |

### `Assets/Scripts/Voice/ImitoneVoiceIntepreter.MicIngest.cs`

**No code changes needed.** The 4-arg `ReadRawSamples(...)` (537-630) and `TryCreateRawReadCursorBehindMs(...)` (674-696) already exist with the exact semantics we need. This was a pleasant surprise.

### `Assets/Scripts/Voice/MicVoiceIngestDebugAggregate.cs`

| Location | Lines | What changes |
|---|---|---|
| CURRENT TEST header | 19-21 | Rewrite tooltip for the new test (verify cursor stays at configured offset, verify imitone responds). |
| CURRENT TEST fields | 28-40 | `currentTestCaptureToMicGapMs/Samples` rename to `currentTestAudioThreadFeedToWriteHeadGapMs/Samples`. `currentTestCaptureTimeSamples` and `currentTestMicWritePosition` become meaningless (no AudioSource read position to compare); drop or repurpose to show `audioThreadFeedReadTotalSamples` and `rawWriteTotalSamples`. |
| Audio thread health block | 116-118 | Remove `aggAudioRingWriteTotalSamples`, `aggAudioRingWriteLastClipReadStart`, `aggAudioRingWriteLastClipReadCount` (pass 3, after the producers in AudioThread.cs are deleted). |
| Imitone feed observability | 124-136 | Add `aggAudioThreadFeedToWriteHeadGapMs/Samples` and `aggAudioFeedOverflowDroppedTotal`. |
| Aggregate copy block | 307-339 | Update to read new snapshot fields, drop reads of removed fields, mirror new values into CURRENT TEST. |
| FAIL trigger logic | 472-509 | The `FAIL_IMITONE_FEED_RATIO_LOW` rolling-window trigger may need its 0.95 threshold relaxed: lock-miss skips will dent the ratio under sustained main-thread bursts. Watch in pass 1 testing; tune in pass 2 if it false-positives. |

Other touch points:

- `LinearAudioStageHandler.cs` (open in workspace, possibly relevant): not affected by the pivot. Skipped.
- `ImitoneVoiceIntepreter.cs:380` calls `MicIngestMainThreadTick` from Update; that flow stays unchanged.

---

## Pass breakdown

Three passes plus an optional pass 4. Each pass is independently testable.

### Pass 1 — Cutover

**Scope:** Make `OnAudioFilterRead` read from the ring instead of `data[]`. Plumb the new cursor + dummy clip. Leave all to-be-deleted infrastructure (audioThreadRing, F1 diagnostic logs, captureSource gap properties) physically in place to minimize blast radius and keep telemetry comparable across pre/post test screenshots.

**Files touched:** `AudioThread.cs` only (and a one-line capture-source-clip swap inside `EnsureCaptureAudioSourceConfigured`).

**Concrete work:**

1. Add `audioThreadFeedLatencyMs` SerializeField (`[Range(32f, 250f)] float = 64f`). Keep `audioCapturePlayAlignmentBufferCount` for one pass so the Inspector value isn't lost (mark `[Obsolete]` / Tooltip "deprecated, ignored").
2. Add `audioThreadFeedReadPosition` (int, init -1), `audioThreadFeedReadTotalSamples` (long), `audioFeedOverflowDroppedTotal` (long), `imitoneFeedDummyClip` (AudioClip).
3. In `EnsureCaptureAudioSourceConfigured`, create the dummy clip if null: `imitoneFeedDummyClip = AudioClip.Create("ImitoneFeedDummy", Mathf.Max(audioConfigDspBufferSize, 1024), 1, audioConfigOutputSampleRate, stream:false);`. AudioClip.Create initializes with zeros. Set `captureSource.clip = imitoneFeedDummyClip;` here.
4. Rewrite `WaitMicPositionThenPlayCapture` body:
   - Wait for `rawWriteTotalSamples >= audioConfigOutputSampleRate * (audioThreadFeedLatencyMs / 1000f) + audioConfigDspBufferSize` (yield null until met).
   - Call `TryCreateRawReadCursorBehindMs(audioThreadFeedLatencyMs, out audioThreadFeedReadPosition, out audioThreadFeedReadTotalSamples)`.
   - Then `captureSource.Play();` **immediately** (same coroutine turn, no `yield` between prime and `Play`).
   - One log: `[Step3a-pivot] cursor primed: latency={audioThreadFeedLatencyMs}ms, readPos={...}, writePos={...}, ringFill={...}sa`.
5. In `OnAudioFilterRead`, replace the imitone-feed segment (the block under the `if (!stillPrimingThisCallback && imitone != null && frames > 0)` gate, currently lines 568-610):
   - Realloc `imitoneFeedBuffer` if `imitoneFeedBuffer.Length != frames`.
   - `int copied = ReadRawSamples(imitoneFeedBuffer, ref audioThreadFeedReadPosition, ref audioThreadFeedReadTotalSamples, out int overflowDropped);`
   - If `overflowDropped > 0`, `Interlocked.Add(ref audioFeedOverflowDroppedTotal, overflowDropped);`
   - If `copied <= 0`, **skip** `imitone.InputAudio` (see [Skip vs zero-padded](#skip-vs-zero-padded-what-that-means)).
   - **Telemetry note:** Today `audioCallbackLockMissTotal` increments only on **`audioRingWriteLock`** miss (parallel ring write). It does **not** measure `rawBufferLock` contention. Pass 1 still runs that block for A/B; pass 2/3 should **repoint or replace** this counter so it reflects **`rawBufferLock` TryEnter miss** on the imitone ring-read path (e.g. thin wrapper around the read, or `out bool lockTaken` on a MicIngest overload if we accept a tiny API touch). `toCopy == 0` alone is ambiguous (lock miss vs empty ring under lock).
   - Compute peakAbs as before, feed `imitone.InputAudio(imitoneFeedBuffer)` if `copied > 0`, swallow exception via existing deferred-log path.
6. Leave the old `data[] → monoScratch → audioThreadRing` write block in place for pass 1 (does no harm, keeps existing telemetry alive for the comparison).
7. Leave the F1 diagnostic logs in `WaitMicPositionThenPlayCapture` deleted (replaced by step 4).

**Test bar:**
- Console: `[Step3a-pivot]` once per Play (again after mic recovery).
- **One screengrab** of **CURRENT TEST** (toning + silent). Fields there are minimal by design; use lower Inspector sections for callback totals, lock misses, config, monitoring, etc.
- Bar (see CURRENT TEST tooltips): `currentTestHybridFeedGapMs` ~40–90 while toning; `currentTestAudioCallbackHzRolling` ≈ sampleRate/dspSize; `currentTestImitoneInputToCallbackRatio` ~1 after priming; `currentTestFeedPeakAbs` >0.05 on voice; `currentTestFailure` false; `currentTestAudioFeedOverflowDroppedTotal` 0; `currentTestMicExitReason` typically `copied_samples`.
- Perceptual: real-time pitch/power.
- Clicks: protocol in `MIC_VOICE_INGEST_FIX_PLAN.md` Appendix.

### Pass 2 — Telemetry rename + new gap measurement

**Scope:** Replace the captureSource-vs-mic gap properties with cursor-vs-write-head equivalents; rewire CURRENT TEST block so the Inspector tells the truth about the new architecture.

**Files touched:** `AudioThread.cs` (telemetry properties), `MicVoiceIngestDebugAggregate.cs` (CURRENT TEST + aggregate fields).

**Concrete work:**

1. In `AudioThread.cs`, replace `CaptureToMicGapSamples` / `CaptureToMicGapMs` (75-100) with `AudioThreadFeedToWriteHeadGapSamples` (computed from `rawWriteTotalSamples - audioThreadFeedReadTotalSamples`) and `AudioThreadFeedToWriteHeadGapMs`. Delete `CaptureSourceTimeSamples`, `MicrophoneWritePositionSamples`, `CaptureSourceIsPlaying`, `CaptureSourceHasClip`, `AudioSourceCountOnGameObjectAtBootstrap` properties (no longer meaningful).
2. In `GetAudioThreadHealthSnapshot`, add `audioThreadFeedReadTotalSamples`, `audioFeedOverflowDroppedTotal`, and the new gap value. (Note: gap is computed from snapshot fields, so the snapshot itself doesn't need to carry the derived gap value, just the cursor position.)
3. In `MicVoiceIngestDebugAggregate.cs` CURRENT TEST block: rename the gap fields, rewrite the tooltip with the new test description ("verify cursor sits at configured latency, verify imitone responds"), drop `currentTestCaptureTimeSamples` and `currentTestMicWritePosition` (or repurpose to show `audioThreadFeedReadTotalSamples` and `rawWriteTotalSamples` directly).
4. Add `aggAudioThreadFeedToWriteHeadGapMs/Samples` and `aggAudioFeedOverflowDroppedTotal` fields under "Imitone feed observability" header.
5. Wire those up in the aggregate-copy block.
6. Reassess `FAIL_IMITONE_FEED_RATIO_LOW` threshold (0.95). If lock-miss skips dent the ratio in normal play, tune to 0.85 or change the trigger to "ratio below threshold AND `audioCallbackLockMissTotal` not climbing" (i.e. catch real feed-side breakage, not natural lock contention).

**Test bar:**
- `currentTestAudioThreadFeedToWriteHeadGapMs` reads in 40-90 ms band while toning.
- Drift across 60 s of session time stays < 20 ms.
- `aggAudioFeedOverflowDroppedTotal` does NOT climb in normal use (overflow only fires when the cursor lags > 250 ms behind, which means main thread froze).
- All Pass 1 test bar items still pass.

### Pass 3 — Cleanup

**Scope:** Delete dormant audio-thread-ring infrastructure, monoScratch, deprecated SerializeFields, and all traces of the old captureSource-as-mic-clip path. Make the codebase honest about the new architecture.

**Files touched:** `AudioThread.cs`, `MicVoiceIngestDebugAggregate.cs`. No behavior change.

**Concrete work:**

1. Delete `audioThreadRing`, `audioRingWriteLock`, `audioRingWritePosition`, `audioRingWriteTotalSamples` fields. Delete the audio-thread ring write block in OnAudioFilterRead (currently 526-559). Delete the `audioRingWriteLastClipReadStart/Count` volatile fields.
2. Delete `monoScratch` (no longer needed; ring is mono and we copy directly into `imitoneFeedBuffer`). Delete the mono-mix block (488-512) which is also gone in pass 1's new OnAudioFilterRead body.
3. Delete `audioCapturePlayAlignmentBufferCount` field.
4. Remove `audioRingWriteTotalSamples`, `audioRingWriteLastClipReadStart`, `audioRingWriteLastClipReadCount` from `AudioThreadHealthSnapshot`.
5. Remove `aggAudioRingWriteTotalSamples`, `aggAudioRingWriteLastClipReadStart`, `aggAudioRingWriteLastClipReadCount` from `MicVoiceIngestDebugAggregate.cs`.
6. (Optional, if pass 1/2 testing shows it adds nothing.) Delete `audioCallbackPrimingFramesRemaining` and `audioCallbackPrimingFramesToSkip`. With the ring-feed path, the FMOD startup garbage from `data[]` doesn't reach imitone anyway. Keeping it as a defensive guard is cheap (one int decrement per callback) so this is more about signal-clarity than performance.

**Test bar:**
- Project builds clean.
- All Pass 1 and Pass 2 telemetry behaves as before.
- Inspector has no missing-script-reference warnings on the aggregate component.
- A 5-minute toning session shows no regressions in any FAIL_* flag, no new clicks, no console errors.

### Optional Pass 4 — `audioCallbackGcSuspectMsThreshold` cleanup

The bug doc's cleanup queue (line 635) flags this: bump the threshold from 3 ms to 15 ms to stop `FAIL_AUDIO_GC_ALLOC_DETECTED` false-positives caused by imitone's normal compute time. One-line change in `AudioThread.cs:17`. Not strictly part of the pivot but obviously belongs in the same review pass since we are touching the same file and the same test surface.

**Test bar:** `FAIL_AUDIO_GC_ALLOC_DETECTED` stays false during a 60s toning session.

---

## Risks I want to flag

The brief's risk section is solid. Two additions:

**1. `TryCreateRawReadCursorBehindMs` clamp behavior at startup.** If the wait condition in the new `WaitMicPositionThenPlayCapture` is wrong and we prime the cursor before the ring has `latencyMs * sampleRate + dspBufferSize` samples written, the helper clamps `samplesBehind` to `ringLength - 1` and `readTotalSamples` to 0 (per `MicIngest.cs:686-689, 693`). The first audio callback would then read a chunk of zero-initialized ring positions (the array is allocated zero-filled by `new float[ringSize]`). Result: imitone gets ~64 ms of zeros at session start, then real audio. Not catastrophic; imitone's docs say zeros are the right "non-continuous" signal. But the `[Step3a-pivot]` startup log should report the actual cursor offset so we can detect this in testing.

**2. The replaced `CaptureToMicGapMs` is read by anyone outside the aggregate?** Need to confirm. Quick grep before pass 2 — if it's only the aggregate, clean deletion. If something external (dialog system? tutorial overlays?) is reading it, deprecate first.

**3. Mic recovery (`captureEpoch` tick).** `TryRebootstrapAudioThreadCaptureIfMicRecovered` stops and rebootstraps the audio-thread path; `StartMicrophoneCapture` rebuilds `rawRingBuffer` and resets write totals. **Pass 1 review:** confirm the imitone-feed cursor is re-primed against the new ring (via `WaitMicPositionThenPlayCapture` + `TryCreateRawReadCursorBehindMs`) and the first post-recovery callback never uses a stale epoch's cursor. Expected OK: prime reads `rawWritePosition` under lock at prime time.

---

## Decisions (locked in)

| Topic | Decision |
|-------|----------|
| Ring read API | Use existing **4-arg** `ReadRawSamples` only; **no** new `TryReadRawSamplesFromAudioThread` (brief was wrong about blocking lock on that overload). |
| Cursor prime | **`TryCreateRawReadCursorBehindMs`** inside **`WaitMicPositionThenPlayCapture`**, not in `OnAudioFilterRead`. Same pattern as `DirectVoiceMonitoring.PrimeBufferedReadCursorForSource`. |
| Dummy clip | **`stream: false`** on `AudioClip.Create` (critical). |
| Lock miss (`toCopy == 0`) | **Skip** `imitone.InputAudio` for that callback. **Do not** feed a full zero frame. **Telemetry:** existing `audioCallbackLockMissTotal` counts **`audioRingWriteLock`** misses only; repoint or supplement in pass 2/3 for **`rawBufferLock`** misses on the imitone read path. |
| Partial read (`0 < toCopy < frames`) | **Feed** — buffer has real samples in the prefix; tail is zero-filled by the helper. |
| Inspector latency | **Floor 32 ms**, **default 64 ms** (`[Range(32f, 250f)]`). |
| `audioCallbackPrimingFramesToSkip` | **Keep** through pass 1–2; **decide** keep vs remove in pass 3 review. |
| Default `audioThreadFeedLatencyMs` | **64 ms** (48 ms optional experiment later, not default). |
| Optional pass 4 (GC threshold) | **Separate commit**, same session as pivot work. |
| H1 DSP-buffer experiment | **Skip** (informational only; does not gate pivot). |
| Prime vs `Play` order | **Prime then `Play`**, same coroutine step, no `yield` between (avoids first-callback race). |

---

## Working agreement reminders

- No code edits without explicit go-ahead per pass.
- After each pass: review-pass walk of every touched file (regressions, threading bugs, audio-thread allocations, stale references, telemetry mismatches), then commit.
- This doc gets updated as decisions land. **During play tests:** append rows to [Discoveries, hypotheses & progress log](#discoveries-hypotheses--progress-log); keep [Inspector / CURRENT TEST protocol](#inspector--current-test-protocol-persistent) accurate whenever the active test changes.
- Future sessions read it.
- All pass review on Opus 4.7 throughout, per Robin's instruction.
