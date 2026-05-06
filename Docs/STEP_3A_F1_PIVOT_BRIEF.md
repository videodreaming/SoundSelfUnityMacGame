# Step 3a / F1 — Architectural pivot brief (handoff to fresh AI)

> **Status (2026-05-06):** External-AI consultation handoff. Eight test runs of `Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md` produced decisive evidence that the V3/V5 pattern (AudioSource plays mic clip, OnAudioFilterRead reads `data[]`) imposes a ~2.13 s latency floor on this Unity 2022.3 / Wwise / Windows stack and cannot be tuned below it. This brief recommends pivoting away from V3/V5 to a hybrid that keeps the audio-thread cadence benefit but reads imitone's input from the existing main-thread-fed ring buffer instead of from `data[]`. Rough scope: 80 to 150 lines of edits in `ImitoneVoiceIntepreter_AudioThread.cs` plus cleanup. Reversible.

---

## Read these first (skim, do not deep-read)

1. **`Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md`** — the bug being pivoted away from. Priority sections: "Status snapshot for fresh perspective" (top of doc) and "Architectural pivot considerations" (search for that header). The chronological findings log is dense; the eighth test run's console output is the key data point but you can skim the rest.
2. **`Docs/MIC_VOICE_INGEST_FIX_PLAN.md`** — the parent rearchitecture plan. Priority: "AI pair programmer instructions" (early section), Section A "The problem", and Section B "The solution / Architecture". V3, V5, V10 in the Vulnerabilities subsection establish the original V3/V5 reasoning; this pivot revises V3/V5 specifically, leaves the rest intact.
3. **`Docs/AUDIO_RELIABILITY_FLOW_AND_CLICK_ANALYSIS.md`** — adjacent context. The "Reliability-First Target Architecture" section is most relevant: it already advocates buffered pull from the ring for monitoring, which is the same shape we're applying to the imitone feed.

You don't need to read `MIC_PIPELINE_REFACTOR_PLAN.md` or `ENVIRONMENT_TRANSITION_REFACTOR_PLAN.md` for this pivot.

---

## TL;DR

Imitone's audio-thread feed currently reads from `OnAudioFilterRead`'s `data[]` parameter. That depends on `captureSource.Play()` of the streaming microphone AudioClip. Unity's audio engine enforces a fixed read-vs-write distance on streaming clips (measured at exactly 100 × `dspBufferSize` = 102400 samples = 2133 ms) that we can't override. Eight test runs confirm `captureSource.timeSamples` assignments are honored at T+0 but snapped back within one Unity frame.

The pivot keeps the audio-thread cadence benefit (steady ~21 ms callbacks, immune to main-thread jitter) but stops feeding imitone from `data[]`. Instead, OnAudioFilterRead reads from the existing `rawRingBuffer` using the same pattern `DirectVoiceMonitoring.OnAudioFilterRead` already uses. The captureSource is repurposed to play a silent dummy clip just to keep the audio callback firing.

Latency becomes whatever offset we set the audio-thread read cursor to behind the main-thread write head. Target: 64 ms. Tunable: 16 to 250 ms.

---

## Why V3/V5 can't reach the latency target

The 8th test run (`Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md`, search "eighth test run") produced this console sequence in one Play session:

```
[Step3a-F1fix] captureSource read aligned: writePos=19456, targetRead=16384,
                                            latencyBudget=3072sa (~64.0ms)
[Step3a-F1diag] T+0 (immediate): gap=3072sa   (~64.0ms)     <-- our assignment took
[Step3a-F1diag] T+1frame:        gap=102400sa (~2133.3ms)   <-- engine snapped it
[Step3a-F1diag] T+0.5s:          gap=102400sa (~2133.3ms)   <-- and held
```

The stable gap is exactly **100 × dspBufferSize** (1024 samples here = 102400 samples = 2133 ms). Too clean to be coincidence. This is internal Unity / FMOD streaming-source scheduling policy, not a tuning knob exposed to user code.

Four hypotheses were live (see "Active hypotheses" in the bug doc). The pivot sidesteps all of them by not depending on the AudioSource-plays-streaming-clip mechanism at all. Don't re-litigate which sub-hypothesis is "right" — the architectural decision is independent of that.

---

## Why the pivot works

Both clocks (Microphone write head, audio-thread read cursor) advance at the same sample rate (48 kHz on both sides — `Microphone.Start` is called with `frequency = AudioSettings.outputSampleRate` per V10 / M7). If we control where the audio-thread cursor sits in the ring, the gap stays where we put it.

We bypass the V3/V5 mechanism entirely:
- No `captureSource.timeSamples` to fight.
- No streaming-clip lookahead policy to negotiate.
- The audio thread's only job becomes "read the next N samples from a stable buffer at fixed cadence." That's exactly what `OnAudioFilterRead` is designed for.

Main thread continues to write to `rawRingBuffer` at whatever cadence Update gives it (bursty under jitter is fine — the writes accumulate, they don't fail). Audio thread reads at steady ~21 ms cadence. Imitone gets continuous, real-time-paced input regardless of main-thread spikes.

---

## Why this is cheaper than the bug doc framed it

The bug doc described Option 1 ("main-thread `Microphone.GetData` → ring → audio-thread reads from ring → feeds imitone") as a "drastic pivot." It really isn't, because most of the infrastructure already exists:

| What's needed | Where it lives now |
|---|---|
| Main-thread mic reads | `ImitoneVoiceIntepreter_MicIngest.cs` `UpdateMicReadFrame` (already running) |
| Mono ring buffer | `rawRingBuffer` (already populated by `WriteRawFrameToRingBuffer`) |
| Audio-thread-safe ring read pattern | `DirectVoiceMonitoring.OnAudioFilterRead` already uses `ReadRawSamples` |
| Audio-thread health telemetry | Built (`audioCallbackTotal`, lock-miss counters, GC-alloc detection) |
| Cross-thread atomic counters | Already in `AudioThreadHealthSnapshot` |

The pivot connects existing pieces. It doesn't introduce new infrastructure. The only genuinely new code is a `TryEnter`-based ring read variant for the audio thread (the existing `ReadRawSamples` uses blocking `lock`, fine for main-thread callers but not audio-thread).

---

## What changes (intent only — your plan doc owns the implementation)

### `ImitoneVoiceIntepreter_AudioThread.cs`

**`EnsureCaptureAudioSourceConfigured`**: still creates `captureSource`, but the clip is a silent dummy (1024 samples of zeros, looping), not the mic buffer. The AudioSource exists solely to keep `OnAudioFilterRead` firing.

**`WaitMicPositionThenPlayCapture`**: simplifies dramatically. No mic-buffer clip assignment. No `timeSamples` math. Initialize a new audio-thread read cursor (`audioThreadFeedReadPosition`) to be `audioThreadFeedLatencyMs` behind the current main-thread `rawWritePosition`. Log the new gap once per session. Drop the three F1 diagnostic logs (T+0 / T+1 / T+0.5s) — the engine isn't scheduling our reads anymore.

**`OnAudioFilterRead`**: keep the callback bookkeeping (counters, gap-tracking, priming-frames-skip, GC-alloc timer, final `Array.Clear(data)`). Replace the `data[]` → mono-mix → `imitone.InputAudio` path with a ring read:

```csharp
int copied = TryReadRawSamplesFromAudioThread(
    imitoneFeedBuffer,
    ref audioThreadFeedReadPosition,
    ref audioThreadFeedReadTotalSamples,
    out int overflowDropped,
    out bool lockTaken);

if (!lockTaken) Interlocked.Increment(ref audioCallbackLockMissTotal);

// Underrun fill. imitone's docs (imitone.cs:80) say feeding ~1/8 second of zeros
// when audio is non-continuous is the recommended way to signal a gap.
if (copied < frames) Array.Clear(imitoneFeedBuffer, copied, frames - copied);

// Same peak-abs telemetry as before — still useful as a "feed has voice" signal.
ComputePeakAbsAndStoreVolatile(imitoneFeedBuffer, frames);

if (!stillPrimingThisCallback && imitone != null) {
    try { imitone.InputAudio(imitoneFeedBuffer); /* counter increment */ }
    catch (Exception ex) { /* same deferred-log pattern */ }
}
```

The mono-mix loop is gone (ring is already mono). The `audioThreadRing` parallel ring is gone (was for Step 3b's DSP move; can be reintroduced cleanly when 3b lands).

### `ImitoneVoiceIntepreter_MicIngest.cs`

Add `TryReadRawSamplesFromAudioThread(...)` — same logic body as the existing `ReadRawSamples(destination, ref readPosition, ref readTotalSamples, out overflowDropped)` overload, but with `Monitor.TryEnter(rawBufferLock, 0)` instead of blocking `lock`. Returns 0 + sets `lockTaken = false` on miss; audio-thread caller fills with zeros for that callback.

Don't refactor the existing `ReadRawSamples` overloads — DirectVoiceMonitoring and RecordedAudioPlayback depend on them.

### Inspector / debug aggregate (`MicVoiceIngestDebugAggregate.cs`)

The CURRENT TEST block needs a rewrite for the new test ("verify audio-thread feed cursor stays at the configured latency offset; verify imitone responds to voice"). The existing fields `currentTestCaptureToMicGapMs`, `currentTestCaptureToMicGapSamples`, `currentTestCaptureTimeSamples`, `currentTestMicWritePosition` get repurposed or replaced with cursor-vs-write-head equivalents.

Keep the FAIL OBSERVATION block as-is — the failure modes (callback frozen, imitone not fed, callback rate low, etc.) are unchanged in shape. The trigger logic might need light review since "imitone not fed" is now coupled to ring availability, not AudioSource state.

### Inspector field rename

`audioCapturePlayAlignmentBufferCount` becomes unused. Replace with `audioThreadFeedLatencyMs` (`[SerializeField, Range(16f, 250f)] float = 64f`). Tooltip: "Latency budget for the audio-thread imitone feed. The audio-thread read cursor is initialized this far behind the main-thread mic write head, and stays there. Higher = more robust against main-thread write bursts; lower = faster pitch response. Default 64 ms is the sweet spot for sustained voice."

### Cleanup

- Delete `audioThreadRing`, `audioRingWriteLock`, `audioRingWritePosition`, `audioRingWriteTotalSamples` (dormant infrastructure for Step 3b that we'll rebuild later if needed).
- Delete the mono-mix scratch loop in `OnAudioFilterRead`.
- Delete the F1 diagnostic log lines (T+0, T+1, T+0.5s).
- Remove `audioCapturePlayAlignmentBufferCount` SerializeField.

---

## Honest risks and limits

**Solves:** imitone-feed continuity (steady audio-thread cadence into imitone, regardless of main-thread jitter) and the 2.13 s latency floor.

**Does not solve:** the root cause of the original "multi-second hang" if it lives downstream of imitone in main-thread game logic. Imitone's analysis output will be timely, but the *game's response* to it (visuals, Wwise tone-driven effects, breath coroutines) is still subject to whatever main-thread jitter caused the original symptom. That investigation is out of scope for this pivot. Track separately if it persists after the pivot.

**Lock contention.** Adds a third `rawBufferLock` consumer (audio-thread imitone feed alongside DirectVoiceMonitoring's audio thread and main-thread writer). The audio thread uses `TryEnter(0)` and degrades gracefully (one underrun-filled callback, increment lock-miss counter, move on). Watch `audioCallbackLockMissTotal` after the change; sustained high values would indicate the main-thread writer is holding the lock too long.

**Catastrophic main-thread hang.** If the main thread freezes longer than the ring length (currently 6 s = 288000 samples at 48 kHz), data is lost. Detection already exists via `MicCaptureEpoch` plus `rawWriteTotalSamples` flatline; recovery exists via `stalledWriteHeadFrameCount` plus `ScheduleRecoveryAttempt` in MicIngest.

**Click profile may shift.** Replacing a streaming-clip read with a fixed-offset ring read changes audio-boundary characteristics. New clicks unlikely (we're reading from a stable ring instead of a streaming source with internal scheduling) but the click testing protocol in `MIC_VOICE_INGEST_FIX_PLAN.md` Appendix should be re-run after the change.

**Loop length.** Robin set `loopLengthSeconds = 6` historically because shorter clips produced clicks. With the pivot, the AudioSource no longer plays the mic clip — it plays a silent dummy. The 6 s loop on the underlying mic clip can stay (it's a defensive buffer for the main-thread writer; longer = more headroom for main-thread hangs) or be shortened. Don't change it as part of this pivot; that's a separate experiment.

---

## Optional: cheap H1 confirmation experiment before pivoting

Not required. The pivot is correct regardless of which sub-hypothesis fits. But if you want a clean datapoint on whether H1 (engine maintains exactly 100 DSP buffers gap) is the actual mechanism, ten minutes of work:

1. Open Project Settings → Audio → DSP Buffer Size. Note current value (likely "Best Performance" = 1024 samples, or "Good Latency" = 512).
2. Change to "Best Latency" (~256 samples).
3. Run the existing F1 test.
4. If the steady-state gap settles at ~25600 samples (100 × 256 = ~533 ms), H1 confirmed exactly. If it stays at ~102400 samples regardless of buffer size, the constant is samples-not-buffers and H1 needs adjustment.
5. Restore the original DSP Buffer Size.

Informational only. Don't gate the pivot on it.

---

## Test bar / acceptance for the pivot

A run is good when:

- `currentTestAudioThreadFeedToWriteHeadGapMs` (or whatever you name the post-pivot field) reads consistently in the **40 to 90 ms** range while toning, with drift < 20 ms across 60 s of session time.
- `currentTestFeedPeakAbs` jumps to >0.05 promptly when the user tones, drops to ~0 when silent (already worked post-H1e fix; should still work).
- `currentTestDbValue` and `currentTestPitchHz` track voice in real time. Perceptually responsive — Robin will validate by ear.
- `aggAudioCallbackHzRolling` stays close to expected (~46.875 Hz at 1024 samples / 48 kHz).
- `aggAudioCallbackLockMissTotal` does not climb at a sustained rate. Brief bursts (e.g., during scene-load main-thread spikes) are tolerable.
- No new clicks audible across the click testing protocol (`MIC_VOICE_INGEST_FIX_PLAN.md` Appendix → "Click testing protocol").
- `FAIL_AUDIO_GC_ALLOC_DETECTED` may still be on (separate cleanup item, not blocking).
- `DirectVoiceMonitoring` underflow / starvation warnings unchanged from pre-pivot baseline (they're known noise, not new).

---

## Working agreement

Same as the parent plan's "AI pair programmer instructions" section:

1. **No code changes without explicit permission.** Outline → ask → wait for go-ahead → edit.
2. **Multi-pass breakdown.** Propose breaking the change into reviewable passes (e.g. "pass 1: add silent dummy clip + new cursor + `TryReadRawSamplesFromAudioThread`; pass 2: rewire OnAudioFilterRead to use the new path; pass 3: cleanup of old code paths and telemetry rename"). Get Robin's sign-off on the breakdown before pass 1.
3. **Mandatory review pass before each commit.** Walk every file touched, look for regressions, threading bugs, audio-thread allocations, stale references, telemetry mismatches.
4. **Surface decisions** in your plan doc as you go. The doc lives alongside the code; future sessions read it.

LLM selection: this whole pivot is threading-sensitive refactor work. Stay on Opus 4.7 throughout, including review passes.

---

## First task

**Before doing anything else, write your own approach doc.**

Create `Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md`. The intent is for you to digest this brief, work through your own version of the change plan, surface refinements or concerns, and produce an artifact that becomes the durable plan for this work.

Your plan doc should include:

- **Your own one-paragraph TL;DR** of the pivot. Don't copy-paste this brief's; restate in your own words after you've read the code.
- **Your read of the code surface.** Specific file paths and line ranges that need to change. Point at actual code, not abstractions. (e.g. `ImitoneVoiceIntepreter_AudioThread.cs:268-314 — EnsureCaptureAudioSourceConfigured needs ...`)
- **Your proposed pass breakdown.** How many passes, what each does, what the test bar is for each.
- **Refinements or pushbacks** on this brief's recommendation. If you see something off — a threading concern this brief glossed over, a simpler shape, an incorrect claim about the existing code — call it out. This is genuinely useful; the brief was written from outside Cursor's view of the code.
- **Questions for Robin** if you want to flag decision points before starting. (e.g., "Should the silent dummy clip be procedural or asset-backed?" or "Is `audioThreadFeedLatencyMs = 64` the right default, or is 32 worth trying first?")

Do not edit code yet. After the doc is drafted, share it with Robin and wait for the go-ahead to proceed to pass 1.
