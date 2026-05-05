# Step 3a Bug: imitone non-responsive (audio-thread feed produces no power / no pitch)

> **Status:** Main fix verified (H1e: `bypassEffects = false`). Two follow-on issues active: F1 (imitone-feed latency) and F2 (speaker leak). Awaiting the sixth test run.
> **Branch:** `WorkingWwise`.
> **Step 3a + H1e fix committed:** `839a224c` (`feat(step3a): migrate imitone feed to OnAudioFilterRead + diagnose/fix bypassEffects bug`).
> **Owner doc for this bug; once F1 + F2 are resolved, append the resolution and link the closing commit(s), then collapse this into a Step 3a Developer note.**

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

#### Follow-on issue F1: imitone responsiveness is sluggish

User report: "slow to detect, and slow to release. Very slow." Earlier observation (prior test, when `bypassEffects = true` and the speaker path was the bypass route) had a measured-by-perception ~3-second delay between toning and hearing it played back. With `bypassEffects = false`, the audio engine's intermediate buffering plus the AudioSource's read-vs-write-position gap is now the **end-to-end imitone-feed latency**.

Pre-3a, imitone was fed by `microphoneBuffer.GetData(...)` reading directly to whatever the mic driver had just written → very low latency (~10–50 ms). Post-3a, imitone is fed via the AudioSource pipeline whose read position is set when `captureSource.Play()` is called and stays a constant gap behind `Microphone.GetPosition` from then on. If that initial gap is large, all subsequent imitone feeds inherit that latency permanently.

`loopLengthSeconds = 6`, so the AudioClip is 6 seconds long — large enough that read-position vs write-position gap can range from ~16 ms to several seconds depending on when `Play()` happens to be called.

**Diagnostic for next test:** add telemetry comparing `captureSource.timeSamples` vs `Microphone.GetPosition` modulo clip length. The result is the imitone-feed latency in samples and ms. If it's > 100 ms or so, we have to fix the read-vs-write alignment before Step 3a can pass acceptance.

**Subsidiary hypothesis F1-drift (added 2026-05-05):** the user reported the responsiveness "thought it was not only slow, but getting slower and slower." Not confirmed yet — needs a multi-second test with the gap telemetry. If true, that's not a static `Play()`-time alignment problem — that's **read-vs-write position drift**, where the AudioSource's read clock and the Microphone driver's write clock disagree by a small fraction (e.g. driver runs at 47999.x Hz vs engine at 48000 Hz) and the gap accumulates over time.

| `currentTestCaptureToMicGapMs` over a 30 s session | Diagnosis |
|---|---|
| Constant (within ±10 ms) | Static `Play()`-time alignment problem. Fix: align read to write at Play. |
| Monotonically growing (e.g. 200 ms → 400 ms → 800 ms) | F1-drift confirmed. Sample-clock mismatch. Fix: periodic `captureSource.timeSamples` re-sync (snap read close to write every N seconds), OR drop the AudioSource-pipeline approach and read directly from the AudioClip via a different pattern. |
| Wraps/jumps (e.g. 200 → 5800 → 200 → 5800) | Read position is wrapping around the 6 s loop while the gap measurement is naive about wrap. Compute gap modulo clip length; the underlying value is constant. |

**Likely fix candidates** (deferred until we measure):
- Use `AudioSource.timeSamples` to align read position close to write position right after `Play()`.
- Periodic re-sync if F1-drift is real.
- Reduce `loopLengthSeconds` (only matters if our latency is bounded by clip length).
- Or accept that the audio-thread AudioSource pipeline has structurally higher latency than direct `Microphone.GetData`, and weigh that against the jitter benefits of audio-thread feed (the rearchitecture's whole motivation).

#### Follow-on issue F2: voice leaks to speakers (audible playback after the H1e fix)

User report after the H1e fix: "I *DO* hear myself through the speakers, but VERY delayed (almost 3 seconds)." With `Array.Clear(data, 0, data.Length)` running at the end of `OnAudioFilterRead`, the captureSource's filter-chain output is zeroed before going to *any* listener. So if voice is reaching speakers, the voice is taking a path that does not go through our filter.

##### AudioListener inventory (2026-05-05, supplied by user)

User searched the scene hierarchy for audio listeners and reported the following:

| Component | GameObject | User commentary |
|---|---|---|
| `AkAudioListener` (Wwise) | `GameObjectSystem2Listener` (positioned at world coords ~`(1475, 809, 85)`) | "Does not pass on the voice to Wwise." |
| `AkAudioListener` (Wwise) | `Camera` | "Does not pass on the voice to Wwise." |
| `AudioListener` (Unity built-in) | `Camera` | The single Unity audio listener — only listener that can route Unity AudioSources to the system speakers. |

**Implication:** Wwise's listener layer is **not** carrying the voice (per user). The audible voice is going through **Unity's** standard `AudioSource → AudioListener (Camera)` path. So F2 is NOT a Wwise-takeover issue — F2 is a **second Unity-side AudioSource** somewhere in the scene that's playing back the mic clip and reaching the Camera's `AudioListener` independently of our captureSource.

##### The prime F2 suspect: `MicrophonePlayback` GameObject

User's hierarchy screenshot shows:

```
MainGame
  ├ EventSystem
  ├ Camera                       ← AudioListener here (system output)
  ├ Global_WwiseRenderer
  ├ ...
  └ SoundSelfAudioVisualControl
      ├ Imitone                  ← our captureSource lives here
      ├ VoiceLogic
      ├ MicrophonePlayback       ← ★ F2 PRIME SUSPECT ★
      ├ MusicSystem
      ├ LightControl
      └ ...
```

The GameObject literally named **`MicrophonePlayback`** under `SoundSelfAudioVisualControl` is by far the most likely culprit. The name suggests an AudioSource that plays back the mic clip for monitoring purposes — a feature the codebase had at some point and may still have on its own loop. It would route its audio through the Camera's `AudioListener` independently of our captureSource and is unaffected by our `Array.Clear`.

##### Possibilities, revised

- **F2a (most likely):** `MicrophonePlayback` GameObject has its own AudioSource (or its own internal logic that drives an AudioSource on a child / on `Camera` / on the listener itself) playing back the mic clip. That source's filter chain (if any) doesn't include our `Array.Clear`, so voice reaches the speakers.
- **F2b:** Some other component — `DirectVoiceMonitoring`, `VoiceLogic`, etc. — is doing live mic playback. Less likely than F2a but worth a quick check given that `DirectVoiceMonitoring` is already noisy in the Console.
- **F2c (least likely now):** Our captureSource has a filter or listener-effects route we missed. If F2a/F2b come up empty, double-check `bypassListenerEffects` / `outputAudioMixerGroup` interactions on Unity 2022.3.

##### Diagnostic for next test

1. **Inspect `MicrophonePlayback` in the Inspector during Play.** What components does it have? Any `AudioSource`? Any custom mic-playback `MonoBehaviour`?
2. **Disable `MicrophonePlayback` (uncheck the GameObject)** and re-run the test. If the leak goes away → F2a confirmed. If the leak persists → it's coming from elsewhere; investigate F2b.
3. **Cleanup decision** once F2 source is identified:
   - If `MicrophonePlayback` is a debug/dev tool that can simply be removed from the scene → fastest fix.
   - If it's serving an actual product feature (mic monitoring during onboarding, etc.) → either re-route it to feed from our audio-thread ring, or clean up its routing so it's silent by default.

**This is NOT blocking 3a's correctness** — the imitone feed is now fed real voice. The leak is an audio-routing aesthetic issue (we don't want the user to hear themselves through speakers in normal play) but doesn't affect pitch tracking. **Triage decision:** measure F1 first (latency telemetry was added to the same commit as this update); if F2 source is confirmed-by-disabling to be `MicrophonePlayback`, the cleanup is likely a simple disable / remove and lands cheaply alongside F1's fix.

### 2026-05-05 — sixth test run (F1 latency telemetry + F2 source identification) — IN PROGRESS

**What's being tested:**
1. **F1 latency:** the new `currentTestCaptureToMicGapMs` / `currentTestCaptureToMicGapSamples` telemetry on the `CURRENT TEST` block reports the live AudioSource-read vs Microphone-write gap. User tones, reports the value while toning, and **also** reports whether it appears stable or growing over a 10–30 s session (testing F1-drift).
2. **F2 source:** user inspects `MicrophonePlayback` GameObject under `SoundSelfAudioVisualControl`, lists its components, and tries disabling it to see if the leak goes away.

**Decision tree for F1:**
- Gap < 50 ms, stable → AudioSource read is well-aligned. Sluggishness is from somewhere else (imitone's own lock-on time, tone-active timer thresholds, or perceptual artifact). **Step 3a passes — F1 closed as "not a bug."**
- Gap 50–250 ms, stable → typical AudioSource buffering. Acceptable for tone tracking but worth a Play()-time alignment fix to reduce perceived lag. **Step 3a borderline; minor fix lands either in 3a's commit or as a 3a-followup commit.**
- Gap > 500 ms, stable → bad. Need a Play()-time alignment fix before 3a. **Step 3a blocked.**
- Gap > 1000 ms, stable → matches the user's earlier ~3 s perception. Confirms F1 root cause is initial Play()-time alignment.
- Gap **growing over time** → F1-drift confirmed. Sample-clock mismatch. Need periodic re-sync. **Step 3a blocked until fix.**

**Decision tree for F2:**
- Disabling `MicrophonePlayback` silences the leak → F2a confirmed. Cleanup: decide if `MicrophonePlayback` is needed (likely a dev/debug tool that can be removed) or re-route its source.
- Disabling `MicrophonePlayback` does NOT silence the leak → look elsewhere (F2b candidates: `DirectVoiceMonitoring`, `VoiceLogic`).

Awaiting test results.

## Open questions

- After this is fixed, do we need to harden Step 3a's test checklist with a "peak abs is non-zero while toning" line item? (Probably yes — Step 1's verification missed this gap.)
- Should the Step 3 prep section's "mic recovery rebootstrap" verification (forced unplug/replug) be done separately from this bug, or combined with the next test pass?

## Cleanup queue (after fix)

- Decide whether `audioCallbackFeedPeakAbsVolatile` stays as permanent telemetry (it's small and useful for "is the audio thread alive AND seeing voice?") or gets removed once the bug is closed.
- Address the `FAIL_AUDIO_GC_ALLOC_DETECTED` false positive properly: bump `audioCallbackGcSuspectMsThreshold` from 3 ms to 15 ms (large enough to ignore imitone's normal compute time, small enough to still catch real GC pauses against the 21 ms callback budget). Document in plan doc Step 3a Developer notes.
