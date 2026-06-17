# Step 3a Bug: imitone non-responsive (audio-thread feed produces no power / no pitch)

> **Status (2026-05-06): CLOSED.** Both phases resolved.
> 1. **Original "imitone non-responsive" bug** resolved by **H1e** fix (`captureSource.bypassEffects = false`). Commit `839a224c` (`feat(step3a): migrate imitone feed to OnAudioFilterRead + diagnose/fix bypassEffects bug`). Imitone correctly tracks pitch and power when fed voice on the audio-thread path.
> 2. **F1 — imitone-feed latency (~2133 ms)** resolved by the **hybrid ring-feed pivot** (Step 3a / F1). Pivoted away from the V3/V5 streaming-clip pattern (which the eight-test-run investigation in this doc proved was engine-bound to a ~100-DSP-buffer minimum read-write gap) to a hybrid: `captureSource` plays a silent in-memory dummy clip just to drive `OnAudioFilterRead` cadence, while the audio thread reads imitone-feed samples from `rawRingBuffer` (main-thread writer, audio-thread reader, primed `audioThreadFeedLatencyMs = 64 ms` behind the write head). Pass 1, 2, 3 commits: `f68cacb3` / `bf670660` / `d537ed9b`. Lever inventory for the remaining downstream-of-feed perceptual lag: `cae32729`. Pass 2 telemetry: gap 106–149 ms, drift 15 ms / 60 s, overflow drops = 0 over 153 s.
>
> **Branch:** `WorkingWwise`.
> **F2 (speaker leak):** moved to plan **Step 3c** (this doc retains a pointer in the historical narrative).
>
> **This doc is now archival.** The canonical "what F1 was, and what fixed it" record lives in `Docs/MIC_VOICE_INGEST_FIX_PLAN.md` § **Step 3a Developer notes (3a)** + § **Appendix: Voice-onset latency lever inventory**. The detailed investigation log below is preserved for archaeology — eight test runs of disambiguation evidence, three live hypotheses (H1: engine-enforced read-write distance; H2: `Microphone.GetPosition` unreliable in first frame; H3: Wwise routing latency), and the architectural-pivot consideration that became the F1 fix. Future work on the *remaining* perceptual lag (downstream of feed: imitone smoothing, noise-floor gate, debounce thresholds) is **not** tracked here — see the plan doc and lever inventory.

---

## Status snapshot for fresh perspective (read this if you're new to the bug)

**One-paragraph TL;DR:** SoundSelf is migrating microphone ingest to the audio thread (Unity 2022.3 + Wwise, Windows). The canonical V3/V5 pattern is in place: `Microphone.Start` → AudioClip (6 s loop) → AudioSource → `OnAudioFilterRead(data[])` → mono-mix → feed to imitone pitch detector. After fixing `bypassEffects` to `false` (H1e), the audio-thread feed receives real voice and pitch tracking works — but with ~2.13 seconds of latency. We tried to reduce that latency by setting `captureSource.timeSamples = (Microphone.GetPosition - 64ms)` right after `Play()`. Diagnostic logs prove the assignment took effect at T+0 (gap = 64 ms), but **within one frame** the gap jumped to ~2133 ms (= exactly 100 DSP buffers @ 1024-sample buffer / 48 kHz output) and stayed there for the rest of the session. We're stuck on whether the audio engine is enforcing a minimum read-vs-write distance for streaming clips, whether `Microphone.GetPosition` is unreliable in the first frame after `Microphone.Start` (data suggests it is — its reported value jumped 2.92 s in a single Unity frame), or both.

**Active hypotheses for the 2.13 s gap (full text in the [Active hypotheses (current)](#active-hypotheses-current-2026-05-06) section below):**

- **H1: Audio engine enforces a minimum read-vs-write distance** (~102400 samples = 100 DSP buffers) for AudioSources playing streaming/Microphone clips. Setting `timeSamples` after `Play()` is honored momentarily but overridden within one frame.
- **H2: `Microphone.GetPosition` is unreliable in the very first call after `Microphone.Start`.** Returns lagged/uninitialized values; "true" mic write head is much further ahead. Our `targetRead = laggedWritePos - 64ms` was therefore reading INTO THE FUTURE relative to the actual buffer state, and the engine corrected by snapping read to a "safe" position behind the true write head.
- **H3: Wwise integration adds ~2 s of latency** somewhere in the audio-routing chain. Plausible but unconfirmed; Wwise typically operates lower-latency than that.

**Open architectural question (raised 2026-05-06):** is the V3/V5 pattern (AudioSource + `OnAudioFilterRead`) actually the right approach for low-latency mic ingest in Unity 2022.3? Alternatives we've considered are listed in the [Architectural pivot considerations](#architectural-pivot-considerations-2026-05-06) section below.

**What we want a fresh perspective on:**

1. Disambiguation of H1 vs H2 (so we don't burn another test cycle to learn which).
2. Whether the V3/V5 pattern can give us < 100 ms latency in Unity 2022.3 + Wwise + Windows, or if a different architecture is required.
3. Recommended fix or pivot, with rationale.

**Where we are in the project:** Step 3a's main goal (move imitone feed to audio thread) is mostly done. The "Test (3a)" checklist in `Docs/MIC_VOICE_INGEST_FIX_PLAN.md` is unticked because of F1 above. F2 (speaker leak) was forked off to plan Step 3c. The investigation has produced two committable improvements (the H1e fix and the diagnostic infrastructure) plus one uncommitted F1 fix attempt + diagnostic. We have not yet reverted the F1 fix; it's still in the live script.

**For the fresh AI:** the chronological [Findings log](#findings-log-chronological) below documents every step of the investigation (eight test runs). Sections [Active hypotheses (current)](#active-hypotheses-current-2026-05-06), [Architectural pivot considerations](#architectural-pivot-considerations-2026-05-06), and [Where we paused — fresh AI handoff](#where-we-paused--fresh-ai-handoff-2026-05-06) at the bottom are the freshest material.

---

## Environment

- **Unity:** `2022.3.12f1` (LTS). Source of truth: `ProjectSettings/ProjectVersion.txt`. Update this line if the project upgrades.
- **Platform:** Windows (Editor + builds). The folder name `SoundSelfUnityMacGame` is legacy; project is not Mac-targeted right now.
- **Audio middleware:** Wwise (via `AkSoundEngine` calls in `ImitoneVoiceIntepreter` + project-wide).
- **Planned upgrade (not yet scheduled):** Unity 6.x. Substantial migration; deferred. Diagnoses in this doc may need re-validation after that upgrade — Unity 6 changed audio internals in ways that *could* affect the volume-0 short-circuit behavior we're hypothesizing about. **If post-Unity-6-upgrade testing shows behavior different from this doc's findings, treat this doc's audio-engine assertions as 2022.3-specific, not absolute.**

If this bug is closed before the Unity 6 upgrade and the upgrade later resurfaces similar symptoms, re-read the **Active hypothesis** section below as a starting point — most of the H1a/H1b/H1c framing is engine-version-independent.

## Debugging Process Convention (read first)

To minimize friction when the user (Robin) tests in the Editor, the agent maintains a **`CURRENT TEST`** header at the very top of `MicVoiceIngestDebugAggregate`'s Inspector. Rules:

- The `CURRENT TEST` block contains every value the user needs to report back for the **active diagnostic step**, mirrored from wherever they live elsewhere in the Inspector. Duplicates are OK and expected — the user reads only the top section.
- A `currentTestDescription` field at the top of the block carries the test name, what to report, and the decision tree, accessible via tooltip.
- The agent **rewrites this block every time the active diagnostic test changes** (new hypothesis to probe, new telemetry, etc.). Field names follow `currentTest*` naming.
- The block is intentionally disposable: when this bug is closed, the section is removed entirely and any permanent telemetry from the investigation either keeps its existing home in headed sections below, or is deleted.
- Permanent fields **do not** live under `CURRENT TEST`. If a field added during debugging proves valuable long-term, move it down into the appropriate permanent header section before closing the bug.

## Symptom (what the user observed)

While toning normally:

- `_dbMicrophone` (main-thread, computed from legacy mic-ingest `capturedInput`) **DOES** respond to voice. → mic is captured, legacy ingest is alive, main-thread filtering / dB metering is fine.
- `_dbValue` (main-thread, computed from `imitone.GetState()` → `power` field) **does NOT** respond. Stays at the initial value (or `PowerToDb(0)`).
- `pitch_hz` (main-thread, from `imitone.GetState()` → `tones[0].frequency_hz`) **does NOT** respond. Stays at 0 / -1.
- `toneActive`, visuals, and Wwise tone-driven response all dead.
- `FAIL_AUDIO_GC_ALLOC_DETECTED` is `true` (latched).

The Step 3a test is a **fail** at the first criterion ("Tone normally; pitch tracking responsive every frame").

## Confirmed evidence (so far)

| Probe | Value | Conclusion |
|-------|-------|-----------|
| `_dbMicrophone` while toning | responsive | Legacy main-thread mic path delivers real audio → `microphoneBuffer` (the AudioClip) IS being filled by `Microphone.Start`. Mic device is alive. |
| `_dbValue` while toning | unresponsive (stuck at initial) | imitone reports `power ≈ 0`. → audio-thread feed is delivering silence, OR imitone is broken. |
| `pitch_hz` while toning | unresponsive (0 / -1) | Consistent with imitone seeing silence (no tones array entries → pitch_hz = 0 in the no-tone branch). |
| Console: deferred `Step 3a: imitone.InputAudio threw...` warning | **Not present** | The audio-thread `imitone.InputAudio` calls are NOT throwing exceptions. Feed code path runs to completion every callback. |
| Aggregate: `aggAudioConfigOutputSampleRate` | 48000 | matches `micCaptureSampleRate` → sample-rate mismatch hypothesis ruled out. |
| Aggregate: `MicrophoneSampleRate` (interpreter Inspector field) | 48000 | (same — see above) |
| Aggregate: `FAIL_AUDIO_GC_ALLOC_DETECTED` | true | Same Step 2 false-positive class. `imitone.InputAudio`'s normal compute time exceeds the 3 ms `audioCallbackGcSuspectMsThreshold`. Not actually a GC alloc; the heuristic was tuned for an OnAudioFilterRead that did almost nothing. **Note for fix queue (separate from this bug):** bump threshold to ~15 ms once the responsiveness bug is solved. |
| Console (unrelated): `DirectVoiceMonitoring: Buffered transport underflow / starvation` | spam during session | `DirectVoiceMonitoring` is on a different mic-ingest path (the live monitoring loop, not imitone). Same path was warning pre-3a. **Triage as separate**, not part of this bug. |

## Ruled out

- **Sample-rate mismatch (audio engine vs imitone init).** Both 48000.
- **`imitone.InputAudio` throwing every callback.** Deferred-log latch never fires; no warning in Console.
- **Phase 1 (legacy mic ingest) totally dead.** `_dbMicrophone` proves legacy ingest reads voice samples.
- **`captureEpoch` thrashing every frame.** Would manifest as `aggAudioCallbackTotal` not climbing — but `FAIL_AUDIO_GC_ALLOC_DETECTED` only latches once `audioCallbackGCAllocSuspectTotal` exceeds baseline, which requires the callback to actually fire and exceed 3 ms. So callbacks ARE firing.

## Active hypothesis (highest probability first)

**H1 — `OnAudioFilterRead`'s `data[]` parameter contains silence (or near-silence), even though the same `microphoneBuffer` AudioClip is delivering voice to the legacy main-thread path via `microphoneBuffer.GetData`.**

Sub-hypotheses for **why** the audio thread sees silence:

- **H1a — `captureSource` is not actually `Play()`-ing the mic clip.** `WaitMicPositionThenPlayCapture` may have been stuck or short-circuited (e.g. the `MicIngestIsReady` check at the end caused early `yield break` after some race).
- **H1b — There is more than one `AudioSource` on the `ImitoneVoiceIntepreter` GameObject, and `OnAudioFilterRead` is firing for *both* — the captureSource (mic audio) AND another source (silence). The mono-mixdown logic doesn't distinguish, so monoScratch oscillates between voice and silence per-callback. imitone's pitch tracker doesn't lock on to that.** `EnsureCaptureAudioSourceConfigured` warns about `existing.Length > 1` but proceeds anyway. The plan's Step 1 work assumed exactly one AudioSource on the GameObject (V-rules).
- **H1c — Unity short-circuits OnAudioFilterRead when `volume = 0f`** on this Unity version. (Mac/Wwise stack — non-trivial behavior matrix.) If true, `data[]` arrives empty/zero. Workaround: keep volume = 1, set output to a muted AudioMixerGroup, or use `mute = true` (some Unity versions do skip filter when muted; YMMV).
- **H1d — Wwise replaces Unity's audio mixer in a way that silences mic-clip reads through Unity AudioSources.** Wwise integration is known to take over the audio output path. Less likely because `OnAudioFilterRead` runs at the AudioSource filter-chain level, before mixer routing.
  - **Note (2026-05-05):** project actually runs on Windows, not Mac. The folder name `SoundSelfUnityMacGame` is legacy / unfixed. Removes the Mac-Wwise-listener-takeover sub-case from this hypothesis but doesn't eliminate Wwise interference entirely.

**H2 — Audio thread IS receiving voice, but imitone's analyzer isn't producing power.**

Possible causes:
- imitone's internal feed_buffer state was constructed before the audio thread feed started, and the rate-of-feed differs in a way that confuses the pitch detector.
- imitone requires continuous feed; if there's any per-callback gap (priming-window snapshot quirk, lock contention dropping a frame), the analyzer may reset.

Lower probability than H1 because:
- Step 2 stress proved audio-thread `InputAudio` doesn't crash — but it fed **zeros**, so it didn't prove the analyzer **functions** under audio-thread feed.
- `imitone.cs` documents `InputAudio` as "Unlike other functions, this can be called from a different thread" — explicit thread-safety guarantee.

## Diagnostic added (this commit)

A single audio-thread-side telemetry probe to **decide between H1 and H2 in one test run**:

- `audioCallbackFeedPeakAbsVolatile` (volatile float, audio-thread writer / main-thread reader, no Interlocked needed for diagnostics):
  - Computed in `OnAudioFilterRead` immediately before `imitone.InputAudio(imitoneFeedBuffer)`.
  - `peak = max(|imitoneFeedBuffer[i]|)` over `i ∈ [0, frames)`.
  - Stored verbatim each callback (no rolling — Inspector polling at LateUpdate ~60 Hz is fast enough to see jumps).
- Surfaced via `AudioThreadHealthSnapshot.audioCallbackFeedPeakAbsLastCallback`.
- Mirrored on `MicVoiceIngestDebugAggregate.aggAudioThreadFeedPeakAbsLastCallback`.

### Decision tree (during next test run)

| `aggAudioThreadFeedPeakAbsLastCallback` while toning | Verdict |
|----|----|
| ≈ 0 (e.g. < 0.001) | **H1 confirmed.** Audio thread is being fed silence. Investigate AudioSource state next (count of AudioSources on GameObject, `captureSource.isPlaying`, `captureSource.clip`). |
| 0.05 — 0.5 (matches voice amplitude) | **H1 ruled out, H2 confirmed.** Audio thread has voice, imitone isn't pitching. Investigate imitone init order, feed continuity, sample-frame size relative to imitone's analysis window. |
| In between (sometimes voice, sometimes 0) | **H1b confirmed (multi-AudioSource pollution)** OR intermittent pause in captureSource. Inspect AudioSource list on the GameObject. |

## Tests to run (with telemetry above in place)

1. **Toning silent → tone**: peak should jump from ≈ 0 to substantial (>0.05) the moment voice begins. If it stays ≈ 0 in both states → H1 confirmed.
2. **Inspect the GameObject in the Editor while in Play mode:**
   - How many `AudioSource` components on the `ImitoneVoiceIntepreter` GameObject?
   - Is the captureSource showing the "playing" state (small play icon in component header)?
   - Is its `clip` field assigned (should show the microphone clip, often named `Microphone` or similar)?
3. **If H1a/b/c suspected** and we want to dodge the AudioSource path entirely: temporarily wire imitone feed to read from the audio-thread *ring* (which we know is being written from `data[]`) — but that just relocates the silence problem to a different reader, so this isn't a useful experiment unless H2 is the real cause.

## Findings log (chronological)

### 2026-05-05 — first test run (commits c8d29bf2 + Step 3a implementation)
- User ran Step 3a test for the first time. Reported "system is fully non-responsive."
- `FAIL_AUDIO_GC_ALLOC_DETECTED` on (expected for 3a — same class as Step 2 stress; deferred to fix queue).
- Confirmed: rates match (48k both); no deferred imitone exception; `_dbMicrophone` responds (legacy ingest alive); `_dbValue` and `pitch_hz` frozen.
- This doc + the peak-abs telemetry are the diagnostic step. Awaiting the next test run for the verdict on H1 vs H2.

### 2026-05-05 — second test run (peak-abs telemetry added)
**Result: H1 confirmed. H2 ruled out.**

| Probe | Value | Conclusion |
|---|---|---|
| `currentTestFeedPeakAbs` while toning | `0` | Audio thread feeds imitone **silence**. |
| `currentTestImitoneInputCalls` | `548` | Feed code path runs every post-priming callback. |
| `currentTestAudioCallbacks` | `556` | Audio thread alive. Ratio 548 / 556 = 98.6% = exactly the 8-callback priming gate. |
| `currentTestDbMicrophone` while toning | `-43.5` | Legacy main-thread ingest delivers real voice samples (control signal). |
| `currentTestDbValue` | `-120` | imitone reports power ≈ 0 — consistent with silent input, NOT broken pitch detection. |
| `currentTestPitchHz` | `0` | Same — no tones to report. |

**Smoking gun:** the same `microphoneBuffer` AudioClip is delivering voice to `microphoneBuffer.GetData` on main thread and silence to `OnAudioFilterRead`'s `data[]` parameter via the AudioSource path. The bug is on the AudioSource path between the AudioClip and `OnAudioFilterRead`.

Now narrowing among H1a / H1b / H1c (see **Active hypothesis** above).

**Reasoning to narrow:**
- H1a says `captureSource` isn't playing. If true, callbacks for the captureSource don't fire — something else must be firing the 556 callbacks. Possible only if there's a second AudioSource on the GameObject (which would also be H1b).
- H1b would mean ≥ 2 AudioSources on the GameObject. The bootstrap warning `"Expected at most one AudioSource on {name}..."` would have fired, but the user did NOT report it in their warning list (only the unrelated `DirectVoiceMonitoring` warnings). Suggests H1b is unlikely — but not yet ruled out (warning could have been filtered or scrolled past).
- H1c (volume=0 short-circuit) fits all observed evidence cleanly: callbacks fire, feed code runs, but `data[]` is silent because Unity's audio engine optimizes out the clip-read for an inaudible source. Pre-3a we never noticed because Step 1 / 2 didn't measure peak amplitude — Step 1 just counted callbacks, Step 2 stress fed zeros on purpose.

**Decision tree refinement (for next test):** replace CURRENT TEST with `currentTestAudioSourceCount` and `currentTestCaptureSourceIsPlaying`. Both pulled from interpreter at LateUpdate. With those + the still-useful peak signal, one more test discriminates the three cleanly.

| Scenario | count | isPlaying | peakAbs | Diagnosis |
|---|---|---|---|---|
| H1c (volume=0 short-circuit) | 1 | true | 0 | Likely fix: `captureSource.volume = 1f` and rely on the existing `Array.Clear(data, ...)` at the end of `OnAudioFilterRead` to silence speaker output. |
| H1b (multi-AudioSource) | ≥ 2 | (either) | 0 | Likely fix: move the captureSource onto its own dedicated GameObject so `OnAudioFilterRead` only fires for the one source we control. |
| H1a (capture not playing) | 1 | false | 0 | Investigate `WaitMicPositionThenPlayCapture` (e.g. yield-break race), or the rebootstrap fix's interaction. |
| Unexpected | 1 | true | nonzero | H1 reverses; recheck reading methodology. |

### 2026-05-05 — third test run (H1 sub-discrimination)
**Result: H1a and H1b ruled out. H1c is the only live hypothesis. Trial fix applied (not yet verified).**

| Probe | Value | Conclusion |
|---|---|---|
| `currentTestAudioSourceCount` | `0` | Snapshot bug — taken BEFORE `AddComponent` in `EnsureCaptureAudioSourceConfigured`. Effective count is 1 (the one we just added). Snapshot moved AFTER `AddComponent` for next test; will read `1` in a healthy state. |
| `currentTestCaptureSourceIsPlaying` | `true` | captureSource IS playing. Rules out **H1a**. |
| `currentTestCaptureSourceHasClip` | `true` | captureSource has the mic clip assigned. Rules out the "clip never assigned" sub-case. |
| `currentTestFeedPeakAbs` | `0` | Bug unchanged (as expected — no fix yet at the time of the test). |
| `currentTestAudioCallbacks` | `1062` | Phase 2 alive. |
| `FAIL_AUDIO_GC_ALLOC_DETECTED` | latched (after one flicker) | Known Step 2 false-positive class. Cleanup-queue item. |

**Multi-AudioSource ruled out:** count was 0 *pre*-bootstrap (no other component on the GameObject creates one), so post-bootstrap count is exactly 1 (the captureSource we created). No second AudioSource is firing `OnAudioFilterRead` with silence. **H1b ruled out.**

**Sole live hypothesis: H1c.** Trial fix applied in this commit:

```diff
- captureSource.volume = 0f;
+ captureSource.volume = 1f;
```

Speaker output is still silenced by the existing `Array.Clear(data, 0, data.Length)` at the end of `OnAudioFilterRead`, which runs BEFORE Unity's volume scaling and listener mixing.

**Why this is the prime suspect:** Unity's audio engine has a long-known (lightly-documented) optimization that skips clip-read for AudioSources whose effective output is silent. The audio callback may still fire for the source's filter chain, but the data buffer arrives zero-filled. With `volume = 1f` the engine reads the mic clip into `data[]`; we use `Array.Clear` to ensure nothing reaches speakers.

Awaiting next test to confirm.

**Side-note from this run (logged for triage, not part of this bug):** `DirectVoiceMonitoring: Buffered transport underflow / callback starvation` warnings continue. Same path as pre-3a; tracked separately.

**Windows correction:** project runs on Windows, not Mac (folder name is legacy). Updated H1d accordingly. Doesn't change the H1c diagnosis.

### 2026-05-05 — fourth test run (H1c trial fix verification)
**Result: H1c rejected. New hypothesis H1e (bypassEffects=true diverts audio around our filter).**

| Probe | Value | Conclusion |
|---|---|---|
| `currentTestFeedPeakAbs` while toning | `0` | **H1c rejected** — the volume change did NOT cause `data[]` to receive voice. Volume was not the cause of the silent feed. |
| `currentTestDbValue` | `-120` | Still flat (consistent with feed still silent). |
| `currentTestDbMicrophone` | `-70.0` | Legacy ingest still alive (low value because the user was speaking quietly at the moment of the screenshot). |
| `currentTestPitchHz` | `0` | Same. |
| `currentTestAudioSourceCount` | `1` | Snapshot fix worked — now reading post-AddComponent count. |
| `currentTestAudioCallbacks` | `2758` | Phase 2 alive. |
| **Speaker output** | **Plays user's voice with ~3s delay** | This is the **NEW, decisive evidence**: the AudioSource IS producing audible mic audio at the listener, but `OnAudioFilterRead`'s `data[]` is zero-filled. There is a parallel audio path bypassing our filter. |

**Reasoning the user-hears-themselves observation forces a new hypothesis:**

The volume change from 0 → 1 unmuted speaker output from a path that was *already* delivering voice — i.e. the AudioSource clip-read DID happen even at volume = 0 (otherwise we'd never hear voice now); the audio just got muted at the post-filter scaling step in the previous test. So Unity is NOT short-circuiting the clip-read on volume = 0; it's only short-circuiting the speaker output. H1c is wrong.

But `peakAbs` is still 0. So `OnAudioFilterRead` is still receiving zero-filled `data[]` even though the audio engine clearly IS reading the clip (otherwise the speakers would be silent). This means the clip-read is happening on a path that **does not flow through our `OnAudioFilterRead` filter**. The speaker path bypasses us.

#### H1e (new): `bypassEffects = true` diverts the AudioSource's audio around OnAudioFilterRead

A user `MonoBehaviour` with `OnAudioFilterRead` is registered as a filter component (Unity inserts it into the source's DSP chain). Unity's [`AudioSource.bypassEffects`](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/AudioSource-bypassEffects.html) is documented as "Bypass effects (Applied from filter components or global listener filters)." With `bypassEffects = true`, the AudioSource's output is routed around its filter chain (including our script) directly to the next stage, while the audio engine still calls `OnAudioFilterRead` for filter-chain bookkeeping — but with zero-filled `data[]` because the real audio has already been diverted past us.

This explains every observation:
- ✅ Speaker plays voice (bypass route delivers audio to listener).
- ✅ `OnAudioFilterRead` callbacks fire (filter chain still alive for bookkeeping).
- ✅ `data[]` is zero-filled (real audio was diverted before reaching the filter).
- ✅ `Array.Clear(data, ...)` at end of OnAudioFilterRead has no effect on speaker output (because that buffer is never used downstream).
- ✅ Pre-3a this didn't matter — main-thread ingest fed imitone via `Microphone.GetData`, the AudioSource was unused. Step 1 added the AudioSource but only counted callbacks, not signal level — `bypassEffects = true` was dormant/silent until Step 3a actually started reading `data[]` for imitone.

**Trial fix applied this commit:**

```diff
- captureSource.bypassEffects = true;
+ captureSource.bypassEffects = false;
```

Audio now flows through the filter chain (our script). The existing `Array.Clear(data, 0, data.Length)` at the end of `OnAudioFilterRead` becomes the speaker silencer — it zeros the buffer that's the only path to output.

**Side-observation: 3-second speaker delay.** Independent of this bug. Likely Unity 2022.3 long-loop-AudioClip read-position lag (or AudioListener latency on Wwise integration). Tracked separately; not blocking 3a.

**If this fix succeeds**, capture in Step 3a Developer notes that `bypassEffects = false` is **required** when using `OnAudioFilterRead` for audio-thread mic ingest, and consider whether the Step 1 V-rules need to be amended with this constraint.

**If this fix fails** (peakAbs still 0): try also setting `bypassListenerEffects = false`. If THAT fails, look for Wwise hooks intercepting the audio path.

### 2026-05-05 — fifth test run (H1e trial fix verification)
**Result: H1e CONFIRMED. Imitone-feed side of Step 3a is functionally working. Two follow-on issues identified for separate investigation.**

| Probe | Pre-fix | Post-fix | Verdict |
|---|---|---|---|
| `currentTestFeedPeakAbs` while toning | `0` | `0.0255` | ✅ Audio thread now sees voice. |
| `currentTestDbValue` | `-120` | `-39.02` | ✅ Power moves on voice (close to `_dbMicrophone = -35.5`; couple-dB difference is the unfiltered-feed delta noted in Step 3a plan). |
| `currentTestPitchHz` | `0` | `116.6` | ✅ Pitch tracks voice. ~116 Hz = low male voice fundamental. |
| `currentTestAudioCallbacks` | climbing | climbing | unchanged (Phase 2 alive). |
| `FAIL_AUDIO_GC_ALLOC_DETECTED` | latched | **NOT latched** in this run | Worth noting — possibly the bypass-route-bypassing-our-filter pattern was making the callback-elapsed-time measurement misleading. **Do not declare this fixed yet** — it'll likely re-latch when imitone runs through full toning sessions. Keep on cleanup queue. |

**H1e CONFIRMED:** `bypassEffects = true` was diverting audio around `OnAudioFilterRead`. With `bypassEffects = false`, the audio flows through our filter; we read it for imitone and `Array.Clear(data)` silences the speaker output of *that* path.

#### Follow-on issue F1: imitone-feed latency (CONFIRMED static ~2.4 s capture-to-mic gap)

**Root cause (confirmed 2026-05-05, sixth test run):**

`captureSource.Play()` starts reading the mic clip from clip-time 0. By the time `WaitMicPositionThenPlayCapture` calls `Play()`, the Microphone has already written some amount of audio into the 6 s loop clip (in our case, ~2.4 s worth). The captureSource's read position therefore starts ~2.4 s behind the Microphone's write position, and **stays there**, because both the read and write clocks advance at the same sample rate from then on. F1-drift is ruled out — the gap was effectively constant across the user's test (2417 ms → 2378 ms over the captured interval; within measurement noise).

This 2.4 s static lag is the entire perceived sluggishness. Imitone is correctly tracking pitch and power — it's just being fed audio from 2.4 s ago.

**Why the mic write position is so high at `Play()` time:** on Unity 2022.3 Windows, `Microphone.GetPosition` can stay at 0 for many frames after `Microphone.Start` while the audio driver buffers, then jump to a large value the first time it reports non-zero. `WaitMicPositionThenPlayCapture` breaks out on the first non-zero value and calls `Play()` immediately — but by then the write position is wherever the driver's first non-zero report happened to land. There's no causal relationship between "Microphone has started" and "Play() is starting from a position close to the write head."

**Recommended fix (option A, minimal change):**

In `WaitMicPositionThenPlayCapture`, immediately after `captureSource.Play()`, set:

```csharp
captureSource.timeSamples = (Microphone.GetPosition(microphoneDeviceName)
                             - intentionalLatencySamples + clipSampleCount)
                            % clipSampleCount;
```

with `intentionalLatencySamples` ≈ 2 audio frames (~2048 samples ≈ 43 ms at 48 kHz / 1024 dsp buffer). This snaps the read position to be `intentionalLatencySamples` behind the write position, leaving just enough headroom that the audio thread's read doesn't catch up to and overrun the mic driver's write head between updates.

The `(... + clipSampleCount) % clipSampleCount` handles the wraparound case where mic position is small enough that subtracting the latency budget would otherwise produce a negative read position.

**Alternative fixes considered (NOT recommended for the F1 close-out):**

- **Periodic re-sync:** robust against drift, but the data shows no drift. Don't pre-emptively add complexity. Revisit only if a future test surfaces gap growth.
- **Drop the AudioSource pipeline:** out of scope; the V3/V5 pattern (AudioSource + `OnAudioFilterRead`) is the rearchitecture's whole point. Reverting would re-introduce the main-thread polling bug.
- **Reduce `loopLengthSeconds`:** doesn't fix F1; just shrinks the maximum possible misalignment. Doesn't help if the typical misalignment is already smaller than the clip length (which it is here — gap is 2.4 s in a 6 s clip).

**Acceptance criterion for the F1 fix:**

After the fix, with the same telemetry (`currentTestCaptureToMicGapMs`), the gap should read consistently in the **20–100 ms** range and not change measurably over a session. Imitone responsiveness should perceptually match pre-3a (i.e., feel "real-time" while toning).

#### Follow-on issue F2: voice leaks to speakers — MOVED to plan Step 3c

After the H1e fix, the user reported audible mic playback through speakers (~3 s delayed) even though our `Array.Clear(data, 0, data.Length)` zeros the captureSource's filter-chain output. The leak takes a separate path that doesn't go through our filter.

The full F2 investigation — AudioListener inventory, the `MicrophonePlayback`-GameObject prime suspect, the diagnostic-and-cleanup task list — has been **moved to plan Step 3c (`Docs/MIC_VOICE_INGEST_FIX_PLAN.md`)** because it's an audio-routing cleanup task, not part of the imitone-feed correctness loop that owns this doc.

This doc retains only the brief mention because:
- F2 was *discovered* during this bug's investigation (the H1e fix made it audible — pre-fix the leak was masked by `bypassEffects = true`).
- The 5th-test-run findings entry below still references F2 as part of the historical narrative of why we added `Array.Clear`-related diagnostics.

For F2 from here on out: **see plan Step 3c.**

### 2026-05-05 — sixth test run (F1 latency telemetry)

**Result: F1 root cause confirmed — large static capture-to-mic gap (~2.4 s) reproduced across two independent Play sessions. F1-drift NOT ruled out from this data alone (would need two readings from the same Play session); it's likely-not-drift but not proven, and either way the same fix applies.**

User report: "Very unresponsive. I was able to get `Current Test Db Value` to change, but not at all responsively. Very very very very sluggish, laggy."

**Important note on test methodology** (added 2026-05-05 after user clarification): the silent and toning readings below are from **two separate Play sessions** (Editor stopped between them, then restarted). They are therefore two independent `Play()`-time alignment snapshots — not a single-session timeline. Cannot use the deltas between them to reason about within-session drift.

| Probe | Silent session | Toning session | Conclusion |
|---|---|---|---|
| `currentTestCaptureToMicGapMs` | **2417 ms** | **2378 ms** | Two independent Play sessions both produced a ~2.4 s gap. Consistent reproduction → the gap is determined by something repeatable in the `Microphone.Start` → first-non-zero-`GetPosition` → `Play()` sequence. |
| `currentTestCaptureToMicGapSamples` | 116032 | 114176 | Same in samples. |
| `currentTestCaptureTimeSamples` (read pos) | 98048 | 172544 | Each session's snapshot is independent; no cross-session math. |
| `currentTestMicWritePosition` (write pos) | 214080 | 286720 | Same — independent per-session snapshots. |
| `currentTestFeedPeakAbs` | 0.0019 | 0.0002 | Tiny — but expected if what's being read is voice from 2.4 s ago. The "toning" screenshot caught a quiet moment from 2.4 s prior. |
| `currentTestDbValue` | -120 | -49.76 | Imitone IS picking up voice in the toning session — just 2.4 s delayed. |
| `currentTestPitchHz` | 0 | 114.5 | Same — imitone tracks voice when it arrives at its input, with the 2.4 s lag baked in. |

**Diagnosis (F1 root cause):**

`captureSource.Play()` starts reading from clip-time 0. By the time `WaitMicPositionThenPlayCapture` calls `Play()`, the Microphone has already written ~2.4 s of audio into the 6 s loop clip. The captureSource's read position therefore starts ~2.4 s behind the Microphone's write position — and stays there for the rest of the session, because both clocks advance at the same sample rate.

This reproduces consistently across Play sessions (2417 ms in one, 2378 ms in another), strongly suggesting the gap is set deterministically by the `Microphone.Start` → first-non-zero-`GetPosition` → `Play()` sequence rather than by chance. **The 2.4 s lag is the entire perceived sluggishness.** Imitone's own lock-on time is not the issue; the audio it's being fed is just 2.4 s old.

**On F1-drift specifically:** can't rule it out from this two-screenshot test (separate sessions). Likely not drift — the AudioSource and Microphone share the audio engine's sample clock, so steady drift would be unusual. If post-fix testing surfaces a slowly growing gap over a long session, escalate to Option B (periodic re-sync) below.

**Why the mic write position is so high when `Play()` is called:** `WaitMicPositionThenPlayCapture` waits for `Microphone.GetPosition > 0`, then calls `Play()`. On Unity 2022.3 Windows, `Microphone.GetPosition` can stay at 0 for the first hundred-or-more ms after `Microphone.Start`, then jump to a large value once the audio driver has buffered enough data. The first non-zero `pos` may already be many seconds into the clip if there's any startup delay between `Microphone.Start` and the first non-zero position read.

**Fix candidates for F1 (deferred to next user decision):**

- **A — Play()-time alignment (minimal change, recommended):** in `WaitMicPositionThenPlayCapture`, after `captureSource.Play()`, immediately set `captureSource.timeSamples = Microphone.GetPosition(deviceName) - intentionalLatencySamples`, where `intentionalLatencySamples` is a small budget (e.g. 2 audio frames ≈ 2048 samples ≈ 43 ms at 48 kHz / 1024 dsp buffer). This snaps the read position close to the write position, leaving just enough headroom to avoid read-overruns-write underflow.
- **B — Periodic re-sync (more robust, only if drift surfaces later):** every N seconds, check the gap. If it has grown beyond a threshold, snap `timeSamples` back into alignment. The current data says drift is not happening, so don't pre-emptively add this; revisit only if a future test shows growth.
- **C — Drop the AudioSource pipeline entirely:** out of scope; the rearchitecture's whole point is using `OnAudioFilterRead`'s data parameter (V3/V5 pattern), and that requires an AudioSource playing the mic clip.

Recommend A. Lands as a small targeted commit (one line plus a comment) and unblocks 3a's "Test (3a)" checklist.

**Side observation: `currentTestKnownFalsePositive_GcAlloc` latched again in this run.** Same Step 2 false-positive class as before. Cleanup-queue item; not relevant to F1.

### 2026-05-05 — seventh test run: F1 fix applied — DID NOT WORK AS EXPECTED

**Result: F1 fix code ran successfully and the math was right, but the resulting gap stabilized at ~2.1 s instead of the intended 64 ms. Drift definitively ruled out (gap identical across two screenshots 32 s apart in the same session).**

**Console (one log line — one Play() event, no rebootstrap during session):**

```
[Step3a-F1fix] captureSource read aligned: writePos=20480, targetRead=17408, latencyBudget=3072sa (~64.0ms), clipSamples=288000
```

**Test screenshots (same Play session):**

| Probe | Test 1 (sessionTime 29.62s, frame 4265) | Test 2 (sessionTime 61.44s, frame 9189) |
|---|---|---|
| `currentTestCaptureToMicGapMs` | **2090.667** | **2090.667** (identical) |
| `currentTestCaptureToMicGapSamples` | 100352 | 100352 (identical) |
| `currentTestCaptureTimeSamples` | 29440 | 116224 |
| `currentTestMicWritePosition` | 129792 | 216576 |
| `currentTestFeedPeakAbs` | 0.0031 | 0.0036 |
| `currentTestDbValue` | -120 (silent) | -53.13 (toning) |
| `currentTestPitchHz` | 0 | 108.18 |

User perceptual: "not at ALL responsive."

**Math analysis (confirms F1 fix code DID execute correctly, but didn't achieve the intended gap):**

- Read advanced from `targetRead=17408` (set by F1 fix at Play() time): consistent with 48 kHz advance + clip-wraparound math, indicates `T_play ≈ 5.4 s` into the session.
- Write advanced from `writePos=20480`: at sessionTime 29.62s should be at `(20480 + 24.25 * 48000) mod 288000 = 32480` — but actual is **129792**. Discrepancy: ~97,300 samples = **~2 s extra advancement of write** that wall-clock-from-Play() does not account for.
- Drift: write advanced exactly 86,784 samples between Test 1 and Test 2; read advanced exactly 86,784 samples in the same interval. **No drift.** Whatever caused the gap to grow from 64 ms → 2.1 s happened *once* (between Play() and the first measurement), then both clocks stayed perfectly in sync.

**Hypotheses for why the fix didn't take (most → least likely):**

- **H_F1_fail_1: Unity pre-buffers ~2 s of streaming-clip audio at `Play()` time, and the `timeSamples` assignment after `Play()` doesn't reposition the pre-buffered chunks.** The audio engine had already queued ~2 s of audio from clip-time 0 forward when `Play()` was called. Our `timeSamples=17408` updated the *next-fetch* pointer, but the queued-up samples drained first. Once they drained, the read settled at "always 2 s behind write" because that's where the queue caught up to. The reported `captureSource.timeSamples` reflects the *engine's notion* of read pointer, which evolves consistently from 17408 — but what `OnAudioFilterRead` actually delivers is from the queue, ~2 s behind.
- **H_F1_fail_2: Mic-recovery happened mid-session, bumping write position forward 2 s without rebootstrap.** Ruled out — `BootstrapAudioThreadCapturePath` always goes through `WaitMicPositionThenPlayCapture` which always logs `[Step3a-F1fix]`. User pasted only one log line. No rebootstrap occurred.
- **H_F1_fail_3: `Microphone.GetPosition` reports a position that's ~2 s ahead of where the AudioSource actually reads from** (Wwise / Windows-driver quirk). Possible but less likely than H_F1_fail_1.

**Diagnostic to disambiguate (next test):**

Add three timed reads of `captureSource.timeSamples` + `Microphone.GetPosition` to `WaitMicPositionThenPlayCapture`, immediately after the existing assignment:

1. Immediate (same coroutine tick, same frame).
2. After `yield return null` (1 frame later).
3. After `yield return new WaitForSeconds(0.5f)` (audio engine fully primed).

**Decision tree:**

| Immediate gap | +1 frame gap | +0.5 s gap | Diagnosis |
|---|---|---|---|
| ~64 ms | ~64 ms | ~2 s | **H_F1_fail_1 confirmed.** Unity pre-buffer drains and resettles at ~2 s. Fix path: switch to a main-thread-fed ring (read mic via `Microphone.GetData`, populate our ring, have `OnAudioFilterRead` read from our ring instead of `data[]`) — bypasses the AudioSource pre-buffer entirely. |
| ~2 s | ~2 s | ~2 s | `timeSamples` assignment was effectively ignored. Try `captureSource.time` (seconds) workaround; if also no-op, same fix path as H_F1_fail_1. |
| ~64 ms | ~64 ms | ~64 ms | Fix worked at the AudioSource-pointer level! Then the user-reported sluggishness has a different root cause — investigate downstream (e.g., a buffer in the imitone-feed→imitone-state pipeline). |
| Other patterns | | | Refine hypothesis based on what we see. |

**Diagnostic added (2026-05-05, uncommitted) in same edit pass that promoted the latency budget to a SerializeField:**

- `audioCapturePlayAlignmentBufferCount` (`[SerializeField, Range(1, 6)] int = 3`): the latency budget, now Inspector-tunable in dsp-buffer units. Each unit ≈ one OnAudioFilterRead callback period (~21 ms @ 1024 / 48 kHz). Default 3 (~64 ms) is the sweet spot for sustained voice. Tooltip carries the full tradeoff explanation. Promotion does NOT affect CPU/battery — pure latency-vs-jitter-robustness tradeoff.
- Three `[Step3a-F1diag]` log lines added immediately after the existing F1 fix's `captureSource.timeSamples = targetRead` assignment in `WaitMicPositionThenPlayCapture`:
  - **T+0 (immediate, same coroutine tick):** read, write, gap.
  - **T+1 frame** (after `yield return null`): read, write, gap.
  - **T+0.5 s** (after `yield return new WaitForSeconds(0.5f)`): read, write, gap.
- Each line reports gap in samples and ms.
- Pure observation; no behavioral change. Removable in one block once F1 root cause is locked.





**Code changes (uncommitted):**

1. `ImitoneVoiceIntepreter.AudioThread.cs:WaitMicPositionThenPlayCapture` — after `captureSource.Play()`, snap `captureSource.timeSamples` to `Microphone.GetPosition - latencyBudget`, where `latencyBudget = Math.Max(audioConfigDspBufferSize * 3, 2048)` samples (~64 ms at 48 kHz / 1024 dsp buffer; sample-rate portable). Always-positive modulo handles the wraparound case (`writePos < latencyBudget`). One-shot `UnityEngine.Debug.Log` `[Step3a-F1fix]` per session for verification.
2. `MicVoiceIngestDebugAggregate.cs` CURRENT TEST block — added `currentTestSessionTimeSeconds` (`Time.time`) and `currentTestSessionFrame` (`Time.frameCount`) so the user can report when in the session a screenshot was taken (enables true within-session drift comparison). Tooltip rewritten as the F1 fix verification protocol.

**Test protocol (for user):**

Same Play session, two screenshots:

- Screenshot A: ~2–5 s after entering Play, while toning. Report `sessionTimeSeconds`, `sessionFrame`, `gapMs`, `gapSamples`, `feedPeakAbs`, `dbValue`, `pitchHz`.
- Screenshot B: 20–30 s later, still in same Play session, still toning. Same fields.
- Console: paste the `[Step3a-F1fix]` log line (one per session).
- Perceptual: how does it feel? Real-time, still laggy, in between?

**Acceptance:**

- A's `gapMs` in 20–100 ms range → F1 fix worked. Static gap is now at the intentional budget.
- A vs B `gapMs` delta < 20 ms → no drift; static alignment is the whole story; F1 closes.
- A vs B `gapMs` delta > 100 ms → drift exists; escalate to Option B (periodic re-sync) as a follow-on commit.
- A's `gapMs` > 500 ms → fix failed or didn't apply; check the `[Step3a-F1fix]` log line was actually printed.

### 2026-05-06 — eighth test run: F1 fix applied + 3-timed-reads diagnostic — DECISIVE EVIDENCE

**Result: F1 fix's `timeSamples` assignment is honored at T+0 but overridden by the audio engine within a single Unity frame (≤ ~16 ms wall clock). Static gap settles at exactly 102400 samples = 100 DSP buffers @ 1024 / 48 kHz = 2133.33 ms. Plus: `Microphone.GetPosition` is suspicious in the very first frame after `Microphone.Start` — it jumped 140288 samples (2.92 s of audio) in one Unity frame, which is impossible at real-time playback.**

**Console output (one Play session, in order, copy-paste from Editor):**

```
[Step3a-F1fix] captureSource read aligned: writePos=19456, targetRead=16384, latencyBudget=3072sa (~64.0ms; 3 dsp buffers), clipSamples=288000
[Step3a-F1diag] T+0 (immediate):  read=16384,  write=19456,  gap=3072sa   (~64.0ms)
[Step3a-F1diag] T+1frame:         read=57344,  write=159744, gap=102400sa (~2133.3ms)
[Step3a-F1diag] T+0.5s:           read=79872,  write=182272, gap=102400sa (~2133.3ms)
```

**The two impossible-without-engine-magic numbers:**

1. **Read advance T+0 → T+1f: `57344 - 16384 = 40960` samples = 853 ms of audio in one Unity frame.** Wall clock for one frame at ~60 fps is ~16.7 ms. The audio engine fast-forwarded the read pointer by ~50× wall clock to "catch up" to where it wanted the read head to be.
2. **Write advance T+0 → T+1f: `159744 - 19456 = 140288` samples = 2922 ms of audio in one Unity frame.** Even more dramatic. Either:
   - The Microphone driver buffered ~2.9 s of audio before reporting any non-zero position (consistent with the original ~2.4 s gap pre-F1-fix — the symptom that prompted F1 in the first place); the first call returned a stale/lagged value, the second call returned the true write head.
   - OR `Microphone.GetPosition` returns wall-clock-relative values, not sample-buffer-relative, with some startup quirk.

**Stable gap = exactly 100 DSP buffers.** `audioConfigDspBufferSize = 1024` (visible in earlier telemetry); `gap = 102400 = 1024 × 100`. This is too clean to be coincidence — it strongly suggests the engine maintains a fixed read-vs-write distance (in DSP-buffer units) for streaming-clip AudioSources, regardless of what we set `timeSamples` to.

**Math verifies T+0 was real:** `(targetRead - 0) = 16384` matches `targetRead` from the F1 fix log. `gap T+0 = 3072` matches `latencyBudget = 3072`. So the assignment WAS applied at T+0; the engine then snapped both pointers within one frame.

**No drift across the session — confirmed twice now.** Test 7's two screenshots at sessionTime 29.6 s vs 61.4 s gave identical `gap = 100352 samples = ~2090 ms` (very close to test 8's 102400 = 2133 ms — the small difference is likely because test 7 used a different audioConfigDspBufferSize at that exact reading, or a fractional offset; both readings represent the same "engine-enforced steady-state gap" behavior).

**Implications for hypotheses:**

- **H_F1_fail_1 (Unity pre-buffers ~2 s at Play() time, drains, then settles):** Partially confirmed. The gap DOES settle at a large value within one frame. But the steady-state value is *exact 100 DSP buffers*, not "however much was pre-buffered" — so it's not a one-time pre-buffer drain, it's an ongoing engine policy.
- **H_F1_fail_2 (mic recovery mid-session):** Already ruled out (only one `[Step3a-F1fix]` log, no rebootstrap).
- **H_F1_fail_3 (`Microphone.GetPosition` reports value ahead of where AudioSource reads):** Now **strongly supported** by the 140288-sample jump in one frame. Either the first call returns a value that doesn't reflect the current driver buffer state, OR the value it returns is what the driver had ~2.4 s ago. We can't tell from these readings alone which.

**Refined active hypothesis set (post-test-8) — see `## Active hypotheses (current, 2026-05-06)` below.**

User perceptual: "not at ALL responsive."

**Code state:** F1 fix + 3-timed-reads diagnostic + `audioCapturePlayAlignmentBufferCount` SerializeField are all uncommitted in working tree. Decision on what to commit / revert is deferred until the fresh AI consultation completes.

### 2026-05-06 — F1 RESOLVED via hybrid ring-feed pivot — bug closed

**Result: F1 closed. The 100-DSP-buffer engine-enforced gap (H1) was real and unfixable via `captureSource.timeSamples` on a streaming clip; pivoted away from V3/V5 entirely for the imitone feed.**

**Architecture (hybrid ring-feed):**

- `captureSource` no longer plays the mic clip. It plays a tiny in-memory silent `AudioClip.Create(..., stream: false)` — `stream: false` is critical (re-engaging streaming would re-trip the same engine policy this pivot is escaping). Its only job is keeping `OnAudioFilterRead` ticking at the engine's DSP cadence.
- The audio thread no longer reads `data[]` for imitone. It reads samples directly from `rawRingBuffer` (the existing main-thread mic-write ring) using the existing 4-arg `ReadRawSamples(...)` overload, which uses `Monitor.TryEnter(rawBufferLock, 0)` and is already audio-thread-safe (also already used by `DirectVoiceMonitoring.OnAudioFilterRead`).
- A read cursor is primed by the main thread before `captureSource.Play()` via the existing `TryCreateRawReadCursorBehindMs(audioThreadFeedLatencyMs, ...)` helper — same pattern `DirectVoiceMonitoring.PrimeBufferedReadCursorForSource` uses. Default latency target: **64 ms** (`[Range(32, 250)]`).
- `imitone.InputAudio` is skipped when `copied == 0` (lock miss / empty ring). Per `imitone.cs:80` ("if audio is not continuous, feed about 1/8 second of silence"), feeding a single full DSP-buffer of zeros for one missed read is wrong — skip the call instead.

**Disambiguation outcome on the prior hypothesis set:**

- **H1 (engine-enforced read-write distance):** confirmed as a fundamental property of streaming `Microphone`-backed AudioSources at this Unity version. Not configurable below the ~100-DSP-buffer floor on the V3/V5 path.
- **H2 (`Microphone.GetPosition` first-frame unreliability):** likely real but moot — the pivot doesn't depend on `Microphone.GetPosition`-vs-`captureSource.timeSamples` alignment any more.
- **H3 (Wwise routing latency):** ruled out as the dominant cause; the 100-DSP-buffer cleanliness (`102400 = 1024 × 100`) was Unity-engine native, not a Wwise unit.

**Telemetry (Pass 2, 153 s session over 4 grabs):** gap 106–149 ms (Pass 1 was 181–203, Pass 2's coherent-pair snapshot fix shaved ~50 ms of apparent gap that was actually display tearing); drift 15.3 ms / 60 s (passes the < 20 ms / 60 s bar); overflow drops = 0; lock misses ~0.04/sec on rawBufferLock; pitch + power alive on tone. Residual gap and drift attributed to mic ADC vs engine output clock skew (~0.025 % oscillator mismatch — hardware floor, self-limited at 250 ms by the `ReadRawSamples` overflow guard).

**Closing commits:** `f68cacb3` (Pass 1 cutover — hybrid ring-feed imitone path); `bf670660` (Pass 2 — telemetry rename: `CaptureToMicGap*` → `AudioThreadFeedToWriteHeadGap*`, snapshot coherent-pair fix); `d537ed9b` (Pass 3 — cleanup: deleted `audioThreadRing`/`audioRingWriteLock`/`audioRingWritePosition`/`audioRingWriteTotalSamples`/`audioRingWriteLastClipReadStart`/`Count`/`monoScratch`/`audioCapturePlayAlignmentBufferCount`/`audioCallbackLockMissTotal`, repointed `FAIL_AUDIO_LOCK_CONTENTION` to `aggRawRingReadLockMissTotal`).

**Forward pointer:** the *remaining* perceptual lock-on lag Robin observed in Pass 3 play-test (`Feed Peak Abs` immediate, `_dbValue` / `pitch_hz` lag, `Mic Exit Reason = unread_zero` while imitone was tracking) is **downstream of feed** — the imitone analysis windowing, the noise-floor gate (which reads `_dbMicrophone` on the legacy main-thread path that still has `unread_zero` jitter, gating `imitoneActive`), and the `positiveActiveThreshold1/2` debounce. Tracked from here in `Docs/MIC_VOICE_INGEST_FIX_PLAN.md` § Step 3a Developer notes (3a) and the new § Voice-onset latency lever inventory appendix (commit `cae32729`). **Step 5b** is the structural fix for the Stage-9 `_dbMicrophone`-via-legacy-path source.

**Companion plan doc:** `Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md` (also archival as of 2026-05-06) — Pass 1, 2, 3 progress log, decisions, test bars, and the "open issues from Pass 1 play test" section that walked the Pass 2 disambiguation.

**Bug doc closed.** Archival as of 2026-05-06.

### 2026-05-05 — F2 (speaker leak) handed off to plan Step 3c

F2 (audible mic playback through speakers post-3a) is no longer tracked in this doc. It's been moved to the main plan as **Step 3c: Audio routing cleanup** (`Docs/MIC_VOICE_INGEST_FIX_PLAN.md`). Reasoning:

- F2 is an audio-routing cleanup task, not part of the imitone-feed correctness investigation that owns this doc.
- The diagnostic for F2 is "inspect & disable `MicrophonePlayback`" — a discrete, scope-bounded task that fits the plan-step model better than a bug-investigation log.
- The AudioListener inventory and prime-suspect reasoning for F2 lived briefly in this doc (5th test run + the F2 follow-on subsection) and have been **consolidated into Step 3c's Background section** in the plan doc. No information lost.

If the F2 investigation surprises us (e.g., leak persists after disabling `MicrophonePlayback` and the source turns out to be subtle), Step 3c may sprout its own bug doc at that point. Until then, treat the plan-step entry as authoritative.

## Active hypotheses (current, 2026-05-06)

These are the live hypotheses for why the imitone-feed gap settles at ~2133 ms and we can't shrink it. Listed in current order of subjective probability. **Not yet disambiguated** — that's the question we want a fresh perspective on.

### H1: Unity audio engine enforces a minimum read-vs-write distance for AudioSources playing streaming/Microphone-fed clips

**Strongest hypothesis.** Stable gap = exactly **100 DSP buffers** (`102400 = 1024 × 100`) — too clean to be accidental. The engine appears to maintain a configured "scheduling lookahead" between an AudioSource's read pointer and the streaming source's write pointer. When `timeSamples` is set to a value that violates this lookahead, the engine snaps the read pointer back to satisfy it within one frame.

**Why it'd be 100 buffers specifically:** Unity 2022.3's audio engine config has `dsp buffer count` (latency presets — Best Latency = 2 buffers, Good Latency = 4, Best Performance = 8, default ~ 4–8). 100 buffers is far above any documented preset, so this might be a Microphone-clip-specific or Wwise-integration-specific lookahead. We do not know the source.

**Implications if true:**

- `captureSource.timeSamples = X` is effectively ignored for streaming clips. The engine treats it as a *suggestion* and snaps to its enforced distance.
- We can't shrink F1 below ~2133 ms via the V3/V5 pattern as currently configured.
- Possible levers: `AudioConfiguration.dspBufferSize`, audio latency project setting, Wwise integration config, or AudioSource-level settings we haven't tried (`bypassListenerEffects`, `outputAudioMixerGroup`, etc.).

**What would confirm:** changing `dspBufferSize` and watching whether the steady-state gap scales proportionally. If we double `dspBufferSize` from 1024 to 2048 and the gap stays at exactly 100 × new buffer size = 204800 samples, H1 confirmed. If the gap stays at 102400 samples regardless, the constant is samples-not-buffers and the hypothesis needs adjustment.

### H2: `Microphone.GetPosition` is unreliable in the first frame after `Microphone.Start`, and the F1 fix asked the engine to read INTO THE FUTURE

**Possibly co-occurring with H1.** The 140288-sample jump in `Microphone.GetPosition` between T+0 and T+1f (one Unity frame, ~16.7 ms wall clock) is impossible if the call always returns the true buffer write position. Either:

- The first call returns a stale / lagged / cached value (e.g., last reported value from before the audio thread fully started).
- The first call returns ~0 worth of audio "since Microphone.Start" but reported in a coordinate system that's still un-initialized.

**Implications if true:**

- Our F1 fix computed `targetRead = laggedWritePos - 64ms`. If `laggedWritePos` was actually behind the *true* write head by ~2 s, then `targetRead` was effectively pointing 2.06 s behind the true write head. The engine then "corrected" the read pointer to the next valid read position relative to the true write — coincidentally the same ~2133 ms gap H1 would produce.
- A simple fix: `yield return new WaitForSeconds(N)` (where N ≥ ~50–100 ms) before reading `Microphone.GetPosition`, so the driver has time to populate position data. Then snap `timeSamples` based on the *true* write head.

**What would confirm:** add a 4th diagnostic read at T+1.0s and T+2.0s with no `timeSamples` reassignment. If `Microphone.GetPosition` keeps advancing at real-time rates after a brief startup, H2 is the issue. If it advances at exactly real-time *but the gap stays at 100 DSP buffers regardless of when we read*, H1 dominates.

**Caveat:** the strongest evidence for H2 is the "impossible jump" in T+0 → T+1f. But the engine read pointer also advanced at >real-time during the same window. So both the read AND write clocks were doing weird things in the first frame after `Play()`. Hard to tease apart from the test 8 data alone.

### H3: Wwise integration adds latency in the audio routing chain

**Lowest probability but plausible.** Wwise is in the chain (project uses `AkSoundEngine` widely; there's an `AkAudioListener` on the Camera and on `GameObjectSystem2Listener`). Wwise's integration with Unity AudioSources can add buffering to handle effect chain processing.

**Why probability is low:**

- Wwise typically operates in single-digit-ms latency for its own routing.
- 2133 ms = 100 DSP buffers is a *Unity* unit, not a Wwise unit.
- The user verified neither Wwise listener carries voice to speakers (per Step 3c F2 inventory).

**Couldn't cleanly rule out:** Wwise's Unity integration might add an internal buffer queue we're not aware of, especially for AudioSource paths that go through `OnAudioFilterRead`.

### H4 (low probability, kept for completeness): there's a buffer in OUR pipeline contributing to the latency

The current path: `Microphone` → `microphoneBuffer` (AudioClip) → `captureSource` → `data[]` in `OnAudioFilterRead` → `monoScratch` (mono mix) → `imitoneFeedBuffer` → `imitone.InputAudio`.

`monoScratch` and `imitoneFeedBuffer` are per-callback scratch buffers that don't accumulate across callbacks (overwritten each call). They cannot contribute to a 2133 ms persistent gap. The audio-thread ring buffer is also not in the imitone feed path (it's parallel infrastructure for Step 3b's DSP move). Confirmed by code inspection.

**This was raised by the user as a sanity-check question on 2026-05-06**: see `## Architectural pivot considerations` for the full discussion.

## Architectural pivot considerations (2026-05-06)

User raised a sharp question on 2026-05-06: *"Why are we writing into a buffer and then into imitone? Why don't we just send the frame's samples directly into imitone (via the filters... eventually)?"*

The question has two parts: (a) the specific concern about buffer indirection in our current code, and (b) the broader architectural concern about whether the V3/V5 (AudioSource + `OnAudioFilterRead`) pattern is the right call at all.

### (a) Per-callback buffer indirection in current code

The current audio-thread feed path inside `OnAudioFilterRead`:

```
data[]                          // input from engine (interleaved stereo @ output sample rate)
  → monoScratch[]              // mono-mix scratch buffer (pre-allocated, overwritten each callback)
  → imitoneFeedBuffer[]        // resized scratch buffer, exact frame count for imitone
  → imitone.InputAudio(...)    // synchronous call
```

**Each of these buffers exists for a real reason:**

- `monoScratch` — Unity's `data[]` is interleaved stereo (or whatever channel count the output is). imitone wants mono. We collapse left+right → mono into `monoScratch` before the call.
- `imitoneFeedBuffer` — `imitone.InputAudio` documentation indicates a specific buffer layout / size. We size and copy into a known shape so imitone is fed correctly.
- The audio-thread ring buffer (`audioRingBuffer`) — **NOT in this path.** That's a separate parallel structure for Step 3b's DSP migration. It's currently being written from `data[]` but is not read by imitone.

**Could we feed `imitone.InputAudio` directly from `data[]` (or skip `imitoneFeedBuffer`)?** Probably yes, if imitone tolerates non-mono input or if `monoScratch` could double as `imitoneFeedBuffer`. That would save one memcpy per callback (~21 ms cadence; sub-microsecond saving). **It would not change the latency** — the latency is upstream of `OnAudioFilterRead` entirely. It's in how the AudioSource gets its `data[]` in the first place.

**Verdict on (a):** these per-callback buffers are not the cause of F1. Streamlining them is a worthwhile micro-optimization for Step 3a's review pass but won't move the needle on latency.

### (b) Is V3/V5 (AudioSource + `OnAudioFilterRead`) the right architecture at all?

**This is the harder, more interesting question.** The V3/V5 pattern was chosen because it's the *canonical* way to read mic audio at the engine's mixer rate without resampling on the main thread. The original Step 1 plan rationale: get sample-rate-correct mic audio on the audio thread, then move imitone feed (Step 3a) and DSP (Step 3b) onto the same thread.

But the cost of using V3/V5 for *Microphone-backed clips specifically* may be the engine-enforced read-write distance we're hitting in H1 above. If that's a fundamental property of the path — and not configurable below ~100 DSP buffers — then V3/V5 imposes a floor latency around 2 s on this hardware/Unity-version combination.

**Alternative architectures we could consider:**

1. **Main-thread `Microphone.GetData` polling, audio-thread reads from our ring buffer.**
   - Main thread polls `Microphone.GetPosition` + `Microphone.GetData(samples, offset)` each Update / FixedUpdate, writes into our existing `audioRingBuffer`.
   - `OnAudioFilterRead` reads from the ring (instead of `data[]`) and feeds imitone.
   - We *also* still have a captureSource if Step 3b's DSP needs to be in the audio chain — but the imitone path bypasses it entirely.
   - **Pros:** breaks free of any AudioSource-imposed latency; mic-to-imitone latency = main-thread polling cadence + ring depth (controllable, can be ~20 ms).
   - **Cons:** main-thread polling re-introduces the original Phase 1 problem of "what if the main thread blocks for >mic loop length?" — though the mic loop is 6 s long, so in practice this is only an issue if the main thread stalls for >6 s. Mitigatable with main-thread health monitoring.
   - **Honest cons-2:** sample-rate question. `Microphone.GetData` returns at the mic capture rate, not the audio engine's output rate. We'd need to resample (or check that they're identical — they are right now: both 48 kHz).

2. **Hybrid: AudioSource for DSP chain (Step 3b), main-thread polling for imitone feed.**
   - imitone gets a low-latency path via main-thread polling.
   - DSP filters can still go in `OnAudioFilterRead` once Step 3b lands, fed from the same ring (or the AudioSource path) — DSP outputs into the speaker chain (or gets discarded for monitoring purposes), but doesn't affect imitone.
   - **Pros:** low imitone latency without abandoning the V3/V5 pattern entirely.
   - **Cons:** two sources of truth for "what audio is being processed right now."

3. **Native plugin (out of scope for now).** Build a native audio capture plugin that reads from the OS mic device directly, bypasses Unity's `Microphone` API entirely. Would give full control over latency but is a significant build/maintenance cost. **Not seriously on the table** unless options 1 & 2 prove inadequate.

4. **Accept the latency and move it elsewhere in the perceived timeline.** E.g., if SoundSelf can buffer the user's tone and play it back through the visualizer with an offset that's "behind the user's voice but reactive to it," the latency might be aesthetically acceptable. **Out of scope for the rearchitecture; this is a product-design decision.**

**Verdict on (b):** if H1 is confirmed and the engine-enforced gap can't be configured below ~2 s, options 1 or 2 (main-thread polling for imitone feed) become the most likely path forward. We were avoiding them because they re-introduce some of the failure modes V3/V5 was designed to avoid, but with the audio-thread ring buffer infrastructure already in place from Step 1, the failure-mode mitigation is much cheaper than it was before Step 1.

## Where we paused — fresh AI handoff (2026-05-06)

After the eighth test run we have decisive evidence about the **mechanism** (engine snaps the gap to 102400 samples within one frame after `Play()`, regardless of `timeSamples` assignment) but lack engine-internals knowledge to disambiguate H1 vs H2 cleanly, and we're unsure whether the V3/V5 pattern is even tractable for low-latency mic ingest in this engine version. We're at the limit of what brute-force diagnostic-and-fix iteration can give us without burning more test cycles per learning.

**What we'd find most valuable from a fresh perspective:**

1. **H1 vs H2 disambiguation.** Is the 100-DSP-buffer minimum-gap-for-streaming-clips a real Unity 2022.3 behavior? Documented anywhere? Is it tied to `dspBufferSize`, latency preset, or something Microphone-specific? Or is the more parsimonious explanation actually H2 (Microphone.GetPosition unreliable for first ~1 frame)?
2. **Architectural pivot recommendation.** Given H1 may be insurmountable for V3/V5: should we pivot to "main-thread `Microphone.GetData` → ring buffer → audio-thread reads from ring → feeds imitone", and what are the gotchas with that approach in Unity 2022.3 + Wwise + Windows?
3. **Concrete fix or next experiment.** What's the smallest test that would let us decide between H1, H2, and the architectural pivot, with maximum information per test cycle?

**What's available to the fresh AI:**

- This document (you're reading it).
- `Docs/MIC_VOICE_INGEST_FIX_PLAN.md` (project-level rearchitecture plan; Step 3a is the active phase, this bug blocks its acceptance).
- The full source for the audio-thread feed: `Assets/Scripts/Voice/ImitoneVoiceIntepreter.AudioThread.cs` (especially `OnAudioFilterRead`, `BootstrapAudioThreadCapturePath`, `WaitMicPositionThenPlayCapture`).
- `Assets/Scripts/Voice/ImitoneVoiceIntepreter.MicIngest.cs` — legacy main-thread mic path that proves microphone capture itself is healthy.
- `Assets/Scripts/Voice/MicVoiceIngestDebugAggregate.cs` — in-Editor telemetry surface.
- The eight-test-run findings log above.

**What's NOT yet available** but the fresh AI could reasonably ask for:

- A minimal repro project isolating just the AudioSource + Microphone path (we haven't built one — would take ~1 hour).
- Profiler captures from a Play session (haven't taken any).
- Specific Audio Project Settings values (`AudioConfiguration` defaults, latency preset). Easy for us to surface on request.
- Wwise project configuration details (mostly opaque to us; could be retrieved if necessary).

**Code state at handoff (uncommitted):**

- `ImitoneVoiceIntepreter.AudioThread.cs:WaitMicPositionThenPlayCapture` — F1 fix (timeSamples assignment after Play) + 3-timed-reads diagnostic.
- `ImitoneVoiceIntepreter.AudioThread.cs` — `audioCapturePlayAlignmentBufferCount` SerializeField (Inspector-tunable latency budget).
- `MicVoiceIngestDebugAggregate.cs` CURRENT TEST block — F1 verification fields.

The fresh AI should feel free to recommend reverting any/all of the above as part of the pivot.

## Open questions

- After this is fixed, do we need to harden Step 3a's test checklist with a "peak abs is non-zero while toning" line item? (Probably yes — Step 1's verification missed this gap.)
- Should the Step 3 prep section's "mic recovery rebootstrap" verification (forced unplug/replug) be done separately from this bug, or combined with the next test pass?

## Cleanup queue (after fix)

- Decide whether `audioCallbackFeedPeakAbsVolatile` stays as permanent telemetry (it's small and useful for "is the audio thread alive AND seeing voice?") or gets removed once the bug is closed.
- Address the `FAIL_AUDIO_GC_ALLOC_DETECTED` false positive properly: bump `audioCallbackGcSuspectMsThreshold` from 3 ms to 15 ms (large enough to ignore imitone's normal compute time, small enough to still catch real GC pauses against the 21 ms callback budget). Document in plan doc Step 3a Developer notes.
