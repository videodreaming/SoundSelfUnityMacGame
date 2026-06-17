# Mic Pipeline Refactor Plan

## Goal

Create a single microphone capture pipeline that:

1. Captures mic input once.
2. Feeds raw audio to `ImitoneVoiceIntepreter`.
3. Produces a normalized audio path for monitoring and record/replay.
4. Removes unused audio components and duplicate buffer logic.

---

## Current Baseline (Confirmed)

- Only one active mic capture source is in `Assets/Scripts/Voice/ImitoneVoiceIntepreter.cs` via `Microphone.Start(...)`.
- `DirectVoiceMonitoring` uses shared mic data and an `AudioSource` for monitoring output.
- `RecordedAudioPlayback` uses shared mic data and no longer depends on the old holder `AudioSource` reference (`ThisObjectAudioSource`).
- `Assets/ImitonePackage/Imitone/ExampleImitoneBehavior.cs` is not used in production flow.

---

## Non-Negotiable Invariants

- Never run multiple `Microphone.Start(...)` calls in parallel.
- `ImitoneVoiceIntepreter` must continue receiving raw mic data (pre-normalization).
- Monitoring and recording paths must consume normalized audio.
- Long-term target: `MicPipeline` becomes the only mic read owner and publishes both raw and normalized streams.
- Monitoring audio callback thread must never call Unity mic APIs (`Microphone.*`, `AudioClip.GetData`, `Time.frameCount` reads that depend on main-thread update flow).
- Microphone channel contract is explicit: input is treated as mono (`1` channel) for this pipeline.
- Refactor should be reversible in small steps.

---

## Step-by-Step Plan

## Step 0 - Preflight + Safety

- [ ] Create a feature branch for mic pipeline changes.
- [ ] Capture a short baseline test checklist:
  - [ ] app launches
  - [ ] imitone detects pitch
  - [ ] monitoring audible
  - [ ] recording saves and replay works
- [ ] Add debug counters/log toggles for audio frame flow before major edits.

## Step 1 - Clean Scene/Object Responsibilities

- [x] In `RecordedAudioPlayback`, removed dependence on parent `ThisObjectAudioSource` as a mic-clip holder.
- [x] Kept `playbackSource` as the dedicated replay `AudioSource`.
- [x] Kept `DirectVoiceMonitoring` `AudioSource` for live monitoring output.
- [x] Remove the unnecessary parent `AudioSource` component from `MicrophonePlayback` object (scene-level cleanup).
- [x] Verified no null-reference regressions in `Awake`, recording start/stop, and cleanup paths.

## Step 2 - Introduce `MicPipeline` As Central Owner (Long-Term Architecture)

- [x] Create `MicPipeline` on the same GameObject as `ImitoneVoiceIntepreter`.
- [x] Move mic ownership into `MicPipeline`:
  - [x] device selection
  - [x] `Microphone.Start(...)`
  - [x] write/read position tracking
- [x] Publish two outputs:
  - [x] raw frame stream for imitone
  - [x] normalized frame stream for monitoring and recording (currently pass-through, no gain shaping yet)
- [x] Update `ImitoneVoiceIntepreter` to consume raw frames from `MicPipeline` (no direct `Microphone.GetData` calls).
- [ ] Keep behavior parity before enabling normalization gain changes.
- [ ] Run in-Unity verification pass for monitoring + recording + replay after scene wiring.

### Step 2 Unity Implementation Instructions

1. Open the GameObject that currently has `ImitoneVoiceIntepreter` (same root used by microphone runtime scripts).
2. Add component: `MicPipeline`.
3. On `ImitoneVoiceIntepreter`, assign the new `MicPipeline` reference:
   - If on same GameObject, this should auto-resolve at runtime; assigning explicitly is still recommended.
4. Configure `MicPipeline` inspector values:
   - `Preferred Device Name`: leave blank to auto-pick first detected mic, or set exact device name.
   - `Sample Rate`: `48000` (matches current behavior).
   - `Loop Length Seconds`: `6` (matches current behavior).
5. Script Execution Order (`Project Settings -> Script Execution Order`):
   - Ensure `MicPipeline` runs before `ImitoneVoiceIntepreter`.
  - Ensure `RecordedAudioPlayback` and `DirectVoiceMonitoring` run after `ImitoneVoiceIntepreter`.
6. Scene cleanup check:
   - Confirm parent `MicrophonePlayback` object no longer has a redundant `AudioSource`.
   - Keep child `Playback` and child `DirectVoiceMonitoring` `AudioSource` components.
7. Play Mode smoke test:
   - Confirm mic initializes without errors.
   - Confirm imitone responds to voice.
   - Confirm direct monitoring still plays.
   - Confirm record/replay flow still starts and writes clips.

## Step 3 - Introduce Mic Normalization Controls

- [x] Add normalization controls in `MicPipeline`:
  - [x] gain in dB (e.g. `normalizationGainDb`)
  - [x] optional hard clamp toggle (`normalizationHardClampEnabled`, `normalizationClampAbs`)
- [x] Expose runtime controls so values can be ridden from code.
- [x] Keep a feature flag to disable normalization quickly for troubleshooting.

### Step 3 Notes

- `MicPipeline` now exposes runtime API:
  - `GetNormalizationState()`
  - `SetNormalizationState(...)`
  - `SetNormalizationEnabled(bool)`
  - `SetNormalizationGainDb(float)`
  - `SetNormalizationHardClampEnabled(bool)`
  - `SetNormalizationClampAbs(float)`
  - `GetNormalizationGainLinear()`
- `NormalizationStateChanged` event is raised when settings change.
- Raw stream remains unchanged; normalization is applied only to normalized output stream.

## Step 4 - Route Monitoring Through Normalized Path

- [x] Ensure `DirectVoiceMonitoring` output uses normalized stream from `MicPipeline`.
- [x] Maintain existing mixer routing (`MicMixer`) for final output staging.
- [ ] Fix thread model so audio callback is read-only:
  - [x] `MicPipeline` writes normalized ring buffer on main thread only.
  - [x] `DirectVoiceMonitoring` audio callback only reads buffered samples (no mic polling/update calls).
- [~] Add guard rails:
  - [x] prevent clipping spikes (via `MicPipeline` normalization hard clamp)
  - [x] log/track normalized peak levels for tuning (Inspector telemetry in `MicPipeline`)

### Step 4 Notes

- `DirectVoiceMonitoring` now uses a streaming `AudioClip` and reads normalized samples through:
  - `MicPipeline.ReadNormalizedSamples(...)`
- `DirectVoiceMonitoring` now supports A/B stream source switching for debugging:
  - normalized stream: `MicPipeline.ReadNormalizedSamples(...)`
  - raw stream: `MicPipeline.ReadRawSamples(...)`
  - source toggle: `monitoringStreamSource` (`Normalized` / `Raw`)
- Shipping guard rail:
  - `DirectVoiceMonitoring` logs a warning if monitoring starts in `Raw` mode or is switched to `Raw` at runtime.
- Monitoring no longer plays the raw shared microphone clip directly.
- Existing `AudioSource` mixer routing remains inspector-controlled, so `MicMixer` assignment should continue to work unchanged.
- Remaining Step 4 hardening:
  - tune safety-buffer depth for target hardware
  - current default monitoring safety buffer: `150 ms`
  - underruns now emit runtime error logs from main thread

## Step 5 - Route Record/Replay Input Through Normalized Path

- [x] Change recording capture in `RecordedAudioPlayback` to sample normalized frames.
- [x] Keep file writing / slot logic unchanged initially (isolate risk).
- [ ] Confirm replayed clips audibly match monitored normalized tone.

## Step 6 - Performance + Allocation Pass

- [x] Replace per-frame `new float[]` allocations with reusable buffers/ring buffers.
- [ ] Profile GC allocations in Play Mode.
- [~] Validate no frame spikes under worst-case recording + playback + monitoring load.
- [ ] Keep chunking logic compatible with imitone frame-size limits.
- [ ] Add producer/consumer headroom strategy for monitoring stream:
  - [x] keep normalized ring buffer sized with safety margin (current default: `6s`, exceeds 250-500ms target).
  - [x] when consumer catches up, policy is explicit (inject silence for missing samples).
  - [x] add underrun counters/logs for tuning under <45 FPS (rate-limited logs in `DirectVoiceMonitoring`).
- [~] Validate monitoring latency and quality under target framerates:
  - [~] verify perceived monitoring latency is acceptable with `150 ms` safety buffer (basic manual pass; pressure test pending).
  - [~] verify no clicks/dropouts at low FPS stress cases (no noticeable clicks/dropouts in basic testing; low-FPS stress still pending).
  - [ ] tune buffer depth per hardware if needed.

### Step 6 Notes

- `MicPipeline` now reads microphone data in reusable chunks (`micReadChunkSize`) and avoids per-frame exact-size allocations for output buffers.
- `RecordedAudioPlayback` now caps per-tick normalized catch-up work (`maxNormalizedReadChunksPerTick`) to reduce long-frame spikes after hitches.
- `DirectVoiceMonitoring` underrun logs are rate-limited (`underrunLogIntervalSeconds`) to avoid log spam under sustained stress.
- Current manual validation (not pressure test): both `Raw` and `Normalized` monitoring modes are working and no noticeable clicks/dropouts were heard in basic testing.
- Open follow-up: still need low-FPS/stress pressure testing before considering latency/quality validation complete.

## Step 7 - Hardening + Failure Modes

- [x] Handle missing mic device and hot-unplug/replug scenarios.
- [x] Handle invalid `Microphone.GetPosition(...)` or stalled write-head cases.
- [x] Add safe fallbacks when normalized path is unavailable.
- [x] Confirm clean shutdown/restart behavior.
- [x] Enforce channel contract explicitly:
  - [x] declare/serialize `MicPipeline` channel mode as mono.
  - [x] validate input clip channels at init and log warning/fallback if not mono.
  - [x] ensure monitoring/recording consumers assume channel count from contract, not implicit defaults.

### Step 7 Notes

- `MicPipeline` now retries initialization when no mic is available, and attempts recovery after hot-unplug/invalid position/stalled write-head detection.
- `MicPipeline` channel contract is explicitly serialized as mono; multi-channel mic input is downmixed to mono with a warning.
- `DirectVoiceMonitoring` now assumes channel count from `MicPipeline.Channels`, and re-primes monitoring read position when `MicPipeline` capture restarts.
- `MicPipeline` now supports cleaner restart behavior via `OnEnable` re-init and consolidated shutdown cleanup in `StopMicrophoneCapture()`.

## Step 8 - Final Cleanup

- [~] Remove dead code, obsolete comments, and old test-only pathways.
- [x] Rename `RecordedAudioPlaybackTest` to production name once stable (`RecordedAudioPlayback`).
- [ ] Document final architecture and ownership in this file (or a dedicated runtime audio doc).
- [ ] Final regression run across gameplay modes/scenes.

---

## Suggested Ownership Boundaries

- `MicPipeline` (new, same GameObject as `ImitoneVoiceIntepreter`)
  - Owns mic device selection/start and all mic reads.
  - Publishes raw and normalized frames.
- `ImitoneVoiceIntepreter`
  - Consumes raw frames from `MicPipeline` for analysis.
- `DirectVoiceMonitoring`
  - Consumes normalized frames from `MicPipeline` and plays monitoring stream to mixer/output.
- `RecordedAudioPlayback` (renamed from test)
  - Consumes normalized frames from `MicPipeline`, records them, and manages replay storage/slotting.

---

## Verification Checklist (Use Each Milestone)

- [ ] No duplicate mic capture instances.
- [ ] Imitone still responds correctly to pitch and volume.
- [~] Monitoring path active and controllable (raw/normalized A/B works in manual testing; extended pressure testing pending).
- [ ] Normalization gain changes are audible and stable.
- [ ] Recording captures normalized signal.
- [ ] Replay sound level/quality aligned with design intent.
- [ ] No new null refs, audio glitches, or runaway allocations.

---

## Rollback Strategy

- Keep each step in isolated commits.
- If a step destabilizes audio, revert only that step and keep prior stable steps.
- Preserve old code paths behind temporary toggles until replacement path is validated.

---

## Implementation Notes (Step 2 Starter API)

These are suggested names/signatures to reduce ambiguity while implementing.

### `MicPipeline` Suggested Responsibilities

- Own microphone lifecycle:
  - `InitializeMicrophone()`
  - `StartMicrophoneCapture()`
  - `StopMicrophoneCapture()`
- Read shared mic clip once per frame:
  - `UpdateMicReadFrame()`
- Publish both frame types:
  - raw frame (for imitone)
  - normalized frame (for monitoring/recording)

### Suggested Public Surface

- State/readiness:
  - `bool IsReady { get; }`
  - `string MicrophoneDeviceName { get; }`
  - `int SampleRate { get; }`
  - `int Channels { get; }`
- Normalization controls:
  - `void SetNormalizationEnabled(bool enabled)`
  - `void SetNormalizationGainDb(float gainDb)`
  - `void SetHardLimitEnabled(bool enabled)`
  - `MicNormalizationState GetNormalizationState()`
- Frame access (pick one model and stay consistent):
  - Pull model:
    - `int CopyLatestRawFrame(float[] destination)`
    - `int CopyLatestNormalizedFrame(float[] destination)`
  - Event model:
    - `event Action<float[], int> OnRawFrameReady`
    - `event Action<float[], int> OnNormalizedFrameReady`

### Consumer Integration Notes

- `ImitoneVoiceIntepreter`:
  - Should consume raw frame from `MicPipeline`.
  - Should no longer call `Microphone.GetPosition(...)` or clip `GetData(...)` directly after migration.
- `DirectVoiceMonitoring`:
  - Should consume normalized frame from `MicPipeline`.
  - Keep mixer routing and playback latency tuning local to monitoring script.
- `RecordedAudioPlayback`:
  - Should append normalized frame samples from `MicPipeline`.
  - Keep existing file/slot logic unchanged during first migration pass.

### Update Order Contract

For deterministic behavior each frame:

1. `MicPipeline` reads mic and updates raw frame.
2. `MicPipeline` computes normalized frame.
3. Consumers run:
   - imitone consumes raw
   - monitoring + recording consume normalized

If needed, enforce this via Script Execution Order during migration.

- [ ] Unity setup requirement: make sure `MicPipeline` executes before `ImitoneVoiceIntepreter` in Script Execution Order.
- [ ] Unity setup requirement: make sure `RecordedAudioPlayback` and `DirectVoiceMonitoring` execute after `ImitoneVoiceIntepreter`.
