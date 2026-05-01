# Audio Reliability, Click Risk, and Future Flow (Draft 2)

## Implementation Plan (Reliability-First)

This plan is intentionally simple, staged, and reversible.  
Primary target: eliminate clicks/discontinuities in monitoring while preserving low-latency imitone analysis.

### Core Non-Negotiables (Applies to Every Step)

1. No hidden hard seek path in normal monitoring operation.
2. No hidden hard gain/attenuation step path on audible output.
3. Every transition path must be listed, owned, and smoothed.
4. Failures must be obvious in runtime logs/counters.

### Ordered Execution Plan

#### Phase 0 - Baseline + Invariants Lock

1. Capture baseline behavior (audio/video + profiler notes).
2. Baseline checklist:
   - monitoring start/stop,
   - tutorial override behavior,
   - MusicLoop/SoundWorld attenuation behavior,
   - raw/normalized debug switch behavior.
3. Add explicit invariants doc block in code comments:
   - "No hard seek in normal path"
   - "No hard gain step in audible path"
   - "Imitone raw path remains low latency."

#### Phase 1 - Observability Before Refactor

1. Add counters and thresholded warnings in `DirectVoiceMonitoring`:
   - seek corrections, max drift, re-primes/rebinds,
   - underflow/overflow (once transport changes),
   - callback starvation/silence fills.
2. Add reason codes (for example drift vs reprime).
3. Add dev summary log line (`seeks/min`, `maxDriftMs`, `restarts`, `underruns`, `overruns`).
4. Add counter reset API for test loops.

#### Phase 2 - Transition Path Audit (Hard-Path Elimination Gate #1)

1. Build a transition inventory table (must be exhaustive):
   - monitoring on/off,
   - attenuation toggles,
   - tutorial lock/unlock,
   - mode switches,
   - capture restart/re-prime.
2. For each path define:
   - where it is triggered,
   - whether it can touch playhead,
   - smoothing ramp duration and thread owner.
3. Fail this phase if any unowned path does hard seek/hard step.

#### Phase 3 - Hybrid Alternative 2 Setup (Unity + Code)

1. Verify existing mixer routing first (do not assume):
   - confirm `DirectVoiceMonitoring` output group assignment in scene.
2. Unity mixer setup:
   - dedicated monitoring mixer group (or confirm existing),
   - exposed parameters for monitoring attenuation/gain transition controls,
   - snapshots named `NormalMonitoring` and `AttenuatedMonitoring`.
3. Unity editor action checklist (required):
   - verify `AudioSource.outputAudioMixerGroup`,
   - create/expose parameters,
   - configure snapshot transition times,
   - validate references after reload.
4. Keep deterministic buffering and DSP continuity logic in code.

#### Phase 4 - Transport Refactor (Primary Reliability Fix)

1. Move monitoring transport to buffered pull from `MicPipeline` ring data.
2. Set fixed read offset (start 125-200 ms).
3. Remove routine `timeSamples` seek correction from normal path.
4. Underflow policy: short crossfaded silence fill + counter.
5. Overflow policy: drop oldest unread data + counter.
6. Keep git-based rollback strategy (accepted decision).

#### Phase 5 - Transition Smoothing (Hard-Path Elimination Gate #2)

1. Add/verify smoothing on all transition inventory paths.
2. Add short crossfades around unavoidable discontinuity boundaries:
   - capture restart re-prime,
   - attenuation/mode transitions.
3. Keep raw/normalized debug crossfade skipped (accepted).
4. Re-run transition inventory and reject any remaining hard path.

#### Phase 6 - Record/Replay Alignment Guardrails

1. Keep recording source = `MicPipeline` normalized stream.
2. Do not couple recording semantics to monitor output volume.
3. Define shared content-shaping stage interface for replay rebuild (chant envelope/voice presence policy).
4. No additional ring buffer unless required by asynchronous decoupling.

#### Phase 7 - Pressure + Soak Test Matrix

Run matrix with controlled pressure injections:

1. CPU pressure (heavy updates/particles/animation bursts).
2. Frame-time pressure (low FPS windows, for example 20-30 FPS).
3. Audio transition burst (rapid mode/attenuation/tutorial toggles).
4. I/O pressure (recording writes + scene transitions).
5. Device pressure (mic unplug/replug/default switch/restart).
6. Duration pressure (10/20/30+ minute soak runs).

Capture for each run:

- counters and threshold warnings,
- timestamped audible notes,
- frame-time/CPU envelope,
- recovery behavior (self-heal vs degrade).

#### Phase 8 - Acceptance Gate

Merge only if all pass:

1. No audible clicks/discontinuities in repeated pressure runs.
2. No threshold warning storms in standard soak suite.
3. Tutorial + mode attenuation policy always correct.
4. CPU/battery impact acceptable versus baseline.
5. Playback latency within accepted envelope (<=250 ms, target <=125 ms where feasible).
6. Mixer snapshot transitions verified across relevant scenes/stages.


# ORIGINAL NOTES AND DRAFT OF PLAN

## Purpose

This draft integrates review comments and answers all in-line questions from Draft 1.

Priority order for design decisions:

1. Reliability and consistency (no clicks/disruptions)
2. CPU/battery efficiency
3. Playback latency (125 ms target, up to 250 ms acceptable)

`ImitoneVoiceIntepreter` raw-analysis latency remains a separate hard requirement.

---

## Current End-to-End Audio Flow

## 1) Capture and ownership (`MicPipeline`)

- `MicPipeline` is the single mic capture owner (`Microphone.Start`).
- Per-frame on main thread (`UpdateMicReadFrame`), it:
  - reads new raw samples from the shared mic clip (mono contract),
  - writes raw samples into `rawRingBuffer`,
  - applies normalization (gain/clamp) to produce normalized samples,
  - writes normalized samples into `normalizedRingBuffer`.
- It also publishes:
  - latest raw frame (`TryCopyLatestRawFrame`) for imitone,
  - ring-buffer pull APIs (`ReadRawSamples`, `ReadNormalizedSamples`),
  - `CaptureEpoch` for restart detection.

**Callout A1 (ring buffer length):**  
Current ring buffer length is allocated as `sampleRate * loopLengthSeconds` (minimum 1024).  
With defaults (`48000`, `6`), that is ~288,000 mono samples (~6 seconds).

## 2) Analysis (`ImitoneVoiceIntepreter`)

- Pulls raw frames from `MicPipeline` (`TryCopyLatestRawFrame`).
- Computes mic dB (`_dbMicrophone`) and tone states (`toneActive*`).
- This path is intentionally independent from monitoring-latency decisions.

## 3) Monitoring (`DirectVoiceMonitoring`)

- Monitoring transport is currently the shared mic clip (`monitoringSource.clip = micPipeline.MicrophoneBuffer`).

**Callout A2 (what is a "mic clip" here?):**  
Unity mic capture returns an `AudioClip` that is continuously filled in a loop by the device driver.  
In this system, monitoring plays that same looping clip directly with an `AudioSource`, then applies additional processing (`OnAudioFilterRead`, volume/attenuation).

- Playback head is actively synchronized to mic write position (`SyncLegacyMonitoringPlaybackPosition`) using:
  - `monitoringSafetyBufferMs`
  - `syncSeekThresholdMs`
  - `syncSeekCooldownSeconds`

**Callout A3 (what these do):**

- `monitoringSafetyBufferMs`: target "distance behind live input" for monitoring read head (for example 100 ms).
- `syncSeekThresholdMs`: allowed drift band before correction is forced.
- `syncSeekCooldownSeconds`: minimum time between forced corrections.
- `SyncLegacyMonitoringPlaybackPosition`: compares `AudioSource.timeSamples` to desired target read position. If drift exceeds threshold and cooldown elapsed, it hard-sets `timeSamples` to target.

- In normalized mode, `OnAudioFilterRead` applies normalization gain/clamp in callback.
- Final volume and attenuation are still applied via `AudioSource.volume` in `ApplyMonitoringVolume`.

## 4) Recording / replay (current behavior, pre-rebuild)

- `RecordedAudioPlayback` reads normalized samples from `MicPipeline.ReadNormalizedSamples`.
- Recording content therefore comes from pipeline-normalized data, not monitoring output gain staging.

---

## What Is "Legacy/Recreated" vs New

## Legacy-like transport retained (for low perceived latency)

- Direct monitoring playback from shared mic clip.
- Periodic playback-head correction (seek/jump) against mic write head.

## New/refactored elements

- Centralized mic ownership and dual-stream publication in `MicPipeline`.
- Ring buffers for raw and normalized paths.
- A/B stream source selection for monitoring (`Raw` vs `Normalized`).
- Mic restart/recovery mechanics (`CaptureEpoch`, restart handling).
- Gain-riding controls in `MicPipeline`.
- Monitoring attenuation control and tutorial/mode ownership logic.

---

## Most Likely Click/Discontinuity Mechanisms

Given reports of loud clicks and apparent repeated short audio regions, most likely causes are:

## A) Forced playback-head seeks in monitoring (most likely)

- `SyncLegacyMonitoringPlaybackPosition` can hard-jump `timeSamples`.
- Any jump in a playing stream can create waveform discontinuity (audible click).
- Repeated jumps can sound like short-loop repetition.

**Callout B1 (what causes these jumps):**

- Main thread hitch or CPU spikes causing playback head drift.
- Device timing mismatch / scheduling jitter.
- Drift accumulating beyond threshold repeatedly under load.

**Callout B2 (why this can repeat exactly):**

If system repeatedly enters this pattern:
1) drift > threshold -> hard seek,
2) resumes with same timing pressure,
3) drifts again within cooldown cycle,
4) seeks again to a nearby earlier target,

you can hear near-identical short segments replayed multiple times, plus clicks at each correction boundary.

## B) Mic restart/rebind transitions

- On device hiccup/stall/restart, clip rebind and position reset can produce edges.
- Re-prime near non-zero crossing can click.

## C) Abrupt gain/attenuation control changes

- `AudioSource.volume` step changes can click.
- Rapid toggles can create perceptible discontinuities.

## D) Lower-confidence contributors

Cross-thread coherence and multi-owner attenuation-state issues are valid architectural risks, but likely not primary explanation for the specific "repeated quarter-second" symptom.

---

## Why Repeated Audio Segments Can Happen

The strongest fit is repeated seek correction:

1. playback drifts beyond threshold,
2. player is snapped backward/forward to target read position,
3. drift pattern repeats (timing/load dependent),
4. user hears short repeated segment(s) plus click edges.

This directly matches the reported symptom pattern.

---

## Reliability-First Target Architecture (Recommended)

If reliability is top priority (and 125-250 ms monitoring latency is acceptable):

## Monitoring path

- Move monitoring to buffered pull from `MicPipeline` ring data (instead of active `timeSamples` seeking on the shared mic clip).

**Callout C0 (is this suggestion or requirement, and why):**  
Yes, this is the primary recommendation (not yet implemented).  
It helps because buffered pull avoids routine hard jumps of playback position. Hard jumps are the top suspected cause of both clicks and repeated short segments. A monotonically advancing read head is much more continuity-safe. **Decision: ok, let's do it**

**Callout C1 (what "transport" means):**  
"Transport" means how audio is moved through time from source to output (read head/write head progression, correction policy, timing ownership).  
Current transport = Unity shared mic clip + hard seek correction.  
Proposed transport = ring-buffer consumer with fixed offset and no routine hard seeks.

- Use fixed-latency read head (for example 125-200 ms behind write head).

**Callout C2 (what happens now vs fixed-latency):**

- Now: it tries to stay near target offset but enforces it by periodic hard corrections.
- Fixed-latency buffered: read head always advances monotonically at output rate from a fixed offset, avoiding jumps.
- This trades some immediacy for much higher continuity/reliability, which aligns with requirements.

- Never hard-seek during normal operation.
- Handle underflow/overflow explicitly:
  - underflow: fill with short crossfaded silence,
  - overflow: drop oldest unread samples and increment a metric.

## Transition behavior

- Crossfade on:
  - attenuation/mode transitions,
  - capture restart re-prime.

**Callout C3 (raw/normalized debug switching):**  
Accepted. Skip crossfade work for raw/normalized debug switching for now.

- Use short ramps (5-20 ms) for gain path changes.

## Observability

Track and expose runtime counters/metrics:

- seek corrections (if any remain),
- underruns/overruns,
- capture restarts,
- max drift observed,
- callback starvation events.

**Callout C4 (what "counters" means):**  
Simple integer metrics incremented when an event happens (for example `seekCorrections++` on each forced correction).  
They make reliability measurable and regression-testable.

---

## Record/Replay Rebuild: Recommended Use of Data Flow

- Record from `MicPipeline` normalized stream only (single source of truth).
- Keep monitor output controls (`AudioSource.volume`, attenuation) out of recording-content decisions.

**Callout D1 (chant fade in record/replay):**  
Yes: this is mainly about `chantLerpFast` (or any future voice-presence envelope).  
That logic should live in one shared "content shaping" stage used by recording input, rather than being re-implemented separately in multiple consumers.

Monitoring can still apply playback-only controls on top (for example chantCharge-related attenuation and tutorial/musicloop attenuation), because those are output policy, not recording content semantics.

- If voice-only gating is desired, apply explicitly in a record-input preprocessing stage.

**Callout D1b (what this means, and relation to chantLerpFast):**  
It can include `chantLerpFast`, but is broader than that.  
Voice-only gating means "decide what should be stored as recording content."  
Examples: envelope-based fade, gentle gate/noise suppression, VAD mask, or combined rule-set.  
`chantLerpFast` can be one control signal inside that stage. **Got it. Yes, there will ultimately be a fade-in and fade-out. And I will need to start the recording "right before" a tone begins... not sure how best to achieve that. We don't need to have answers for this yet though, but good to gesture towards it.**

**Callout D2 (what is a dedicated recorder preprocessor):**  
A small processing stage that runs only on the signal sent to recorder (for example noise gate, envelope/VAD mask, fade policy), before samples are appended to recorded buffer.

**Callout D2b (does this require another ring buffer):**  
Not necessarily.  
Recommended first approach: process per chunk as recorder drains `ReadNormalizedSamples(...)`, then append processed samples directly to recording list/buffer.  
Only add another ring buffer if you need decoupled timing between preprocess and writer, or multistage asynchronous processing.

- Persist metadata (sample rate, normalization settings, optional envelope) so replay behavior is deterministic.

---

## Alternative 1: DAW-like bus/signal-flow graph

Pros:

- clear ownership and composable stages,
- easier test isolation,
- explicit routing contracts.

Cons:

- custom DSL/runtime is expensive and risky,
- threading/perf/debug complexity rises quickly.

Practical recommendation:

- If pursued later, start code-first (C# processors + buffers), not a new language.

**Decision:** cons outweigh pros for current phase.

---

## Alternative 2: Lean more on Unity audio tooling (expanded)

### Pros

- `AudioMixer` snapshots and exposed params provide robust smoothing and state transitions.
- Fewer custom control-edge bugs (less hand-rolled gain stepping).
- Better authoring/debug workflow for non-programmer tuning.
- Lower maintenance cost for routing policy changes.

### Cons

- Unity mixer/control layer does not replace deterministic buffering strategy by itself.
- Custom DSP needs (normalization/gating semantics) still require code.
- Hard playhead corrections can still click even with mixer smoothing.

### Best hybrid use

- Use Unity tooling for gain/routing/state transitions.
- Use code-managed ring-buffer transport for continuity guarantees.
- Keep imitone raw path independent and low latency.

**Decision:** go with this hybrid approach.  
Implementation note: include explicit "Unity editor actions required" in each execution phase (for example mixer routing, exposed parameters, snapshot setup), and gate code rollout behind those steps.

---

## Immediate Hardening Checklist (Short Horizon)

1. Reduce/replace forced monitoring seeks (highest click risk).
2. Add short crossfades around unavoidable jumps/restarts.
3. Ensure attenuation/gain transitions are always smoothed.
4. Add runtime counters for drift, jump count, underruns, restarts.
5. Run long soak tests (10-30 minutes) across tutorial/mode changes under load.

---

## Summary

Current system is functionally close, but reliability risk is concentrated in monitoring transport correction behavior (hard seek/jump), not raw imitone analysis.

Given your latency budget, the strongest path is fixed-latency buffered monitoring (no routine hard seeks), explicit smoothing on transition controls, and hard telemetry-based reliability gates before shipping.

