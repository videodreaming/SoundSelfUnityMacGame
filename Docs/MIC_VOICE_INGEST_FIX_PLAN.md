# Voice ingest rearchitecture: plan and rationale

**Status:** Ready for implementation
**Goal:** Eliminate per-frame jitter in voice analysis. Imitone receives steady, real-time-paced input regardless of main-thread variance. Visual and audio response feels fresh every frame, not just "tolerable."

## Environment

- **Unity:** `2022.3.12f1` (LTS). Source of truth: `ProjectSettings/ProjectVersion.txt`. Update this line if/when the project upgrades.
- **Platform:** Windows (Editor + builds). Folder name `SoundSelfUnityMacGame` is legacy; project is not Mac-targeted right now.
- **Audio middleware:** Wwise (`AkSoundEngine` + Ak components throughout the project).
- **Planned upgrade (no scheduled date):** Unity 6.x — substantial migration, deferred. Audio-engine internals changed between 2022 and 6, so any audio-thread / `OnAudioFilterRead` behavior assertions in this plan should be **re-validated** post-upgrade. If a step's verification fails after that upgrade, treat the step's audio-engine claims as 2022.3-specific until re-confirmed.

---

## AI pair programmer instructions (read this first, every step)

This document is co-authored by the engineer and an AI pair programmer. The rules below govern how we work through the implementation plan together. **The agent re-reads this section at the start of every new step.** Technical reminders (class-name typo, "never call `Microphone.*` from audio thread", etc.) live in the "Notes for the Composer agent" section near the end of the document and complement these process rules.

### 1. The code is core; the bug is well-understood

- The voice / mic ingest path is **core to the game**. Changes are made carefully, not willy-nilly.
- This plan exists because the system is **demonstrably broken** (multi-second pitch-tracking freezes during toning). Refactoring whole pieces of architecture is expected and explicitly sanctioned. "Carefully" does **not** mean "timidly" — the architectural changes in Steps 1, 3, and 5 are the entire point.

### 2. No code changes without explicit permission

Before any code-changing pass:
1. **Outline what you intend to do at a high level**, concisely (so it's skim-able for the engineer).
2. **Flag any decision points** (so the engineer can weigh in before they get baked in).
3. **Ask for permission** to begin the changes.
4. **Wait for the engineer's go-ahead** before editing files.

This applies to **every** substantive change pass, including the second-pass review described in rule 6.

### 3. Before beginning any step, consider a multi-pass breakdown

- Propose breaking the step into multiple passes for safety and process sanity. Each pass should be small enough to review and verify on its own.
- Tell the engineer your proposed pass breakdown before starting. The engineer may approve as-is, refine, or override.
- Re-read this working agreement and the step's tasks / Notes & considerations before proposing the pass breakdown.

### 4. Changes outside the plan

If something arises during implementation that wasn't already tasked in the plan:
- **First, compare against the rest of the plan.** If a change is planned for a later step, defer it — don't pre-empt later steps. Also check for cross-step interactions (a change in Step 1 may have implications for Step 5 that aren't immediately obvious).
- **Research it appropriately.** Don't guess on subtle Unity / threading / DSP behavior.
- **If we're going down a rabbit hole, or if the question needs online deep research, stop and prompt the engineer to open a new agent for that research.** Keep this thread focused. When the research returns, the agent summarizes the findings into the relevant V / M / step / appendix section here (not into a separate document).

### 5. Mandatory review pass before each commit

Before committing any step, the **last** action is an explicit review pass for regressions, bugs, and unexpected behaviors. **This is a separate step from the first-pass changes** and is **not optional**.

Review pass protocol:
1. **Prompt the engineer before starting the review.** ("Ready to begin Step N's review pass?") Wait for confirmation.
2. **Assess issues thoroughly.** Walk every file you touched. Re-read the diff. Look for:
   - Regressions in adjacent code paths.
   - Threading bugs (the audio thread is unforgiving).
   - Allocations on the audio thread.
   - Stale references, missing null-checks, incomplete cleanup.
   - Inspector references that should have been re-wired.
   - Telemetry that's wrong, missing, or duplicated.
   - FAIL OBSERVATION flags whose triggers may no longer match the new behavior.
3. **Surface findings to the engineer first.** Do **not** begin fixes immediately; wait for the engineer to weigh in.
4. **Fix step by step**, with permission per fix.
5. **Log decisions not to fix** in the step's Developer notes — capture the issue, why we chose not to fix it now, and any follow-up needed later.

### 6. Living document protocol

This document is our **primary living artifact** for the rearchitecture. Keep it in sync as we go:

- **Tick checkboxes** the moment a task is completed and verified. **Don't batch** — tick as you go. If a task ends up unnecessary, leave it unticked with a one-line note explaining why (per the existing convention near the top of Section C).
- **Log new open questions** in the affected step's Developer notes.
- **Log new findings or decisions** that shift the plan in the affected sections (V / M / S / step tasks / FAIL OBSERVATION flags / appendices). Don't bury cross-cutting changes in a single step's Developer notes — if it affects multiple steps, update the master sections.
- **Document decision points and their reasoning.** When the engineer makes a choice (architectural, tradeoff, "good enough"), capture the decision and the reasoning in the relevant section so the rationale is durable.
- **Summarize external research.** If a separate research agent is run, fold the summary into the relevant V / M / step / appendix section here.

### 7. Re-read this section at the start of every new step

The agent's first action when starting a new step is to re-read this "AI pair programmer instructions" section. Process rules drift if not actively reinforced; this is the reinforcement.

### 8. LLM selection per step

Each step has a **Recommended LLM for this step** block at the top, immediately under the step header. Read it before doing anything else in that step.

- **Why it matters:** Steps differ in difficulty. The threading-sensitive / refactor-heavy ones (Steps 0.7, 1, 3, 5, 7) want the thinking model (Opus 4.7); the mechanical / boilerplate ones (Steps 0.5, 2, 4, 6, and most of Step 0) are fine on Composer 2 (full). Using the right tool per step keeps cost down on mechanical work and quality up on the bug-prone work.
- **Engineer's responsibility:** Confirm the recommended model is selected in Cursor **before** reading the step's tasks. Switch models if needed.
- **Agent's responsibility:** When starting a step, the agent's outline (per rule 2) explicitly names the recommended LLM and asks the engineer to confirm the active model matches before proceeding.
- **Always Opus 4.7 for the review pass.** The mandatory review-pass (rule 5) is on Opus 4.7 regardless of which model did the first-pass. **Switch back to Opus 4.7 before beginning every review pass.** The agent prompts the engineer to switch if the first-pass was on a different model.
- **Mid-step switches are allowed.** If a step starts on Composer 2 and the work surfaces something subtle (a threading question, an unexpected click, a tear-detection telemetry hit), stop and switch to Opus 4.7 before continuing. Note the switch and the trigger in the step's Developer notes.

### 9. Active-bug debugging convention: CURRENT TEST Inspector block

When an active diagnostic loop needs the engineer to read values out of the Editor and report them back, the agent maintains a **`CURRENT TEST`** header at the very top of `MicVoiceIngestDebugAggregate`'s Inspector.

The convention exists because debugging-by-screenshot needs the values to be in **one** spot, named clearly, with the test instructions next to them — not scattered across nested Inspector sections that the engineer has to scroll, expand, and remember the meaning of.

Rules:

- **One block, top of Inspector.** The `CURRENT TEST` block sits above all permanent telemetry sections in `MicVoiceIngestDebugAggregate`. The engineer reads only this block while a test is active.
- **ONLY contains values needed for the current test.** No accumulation across tests. Fields from earlier tests that aren't needed for the active test get **removed**, not left around "in case." If a Pass 3b test tracks five fields and a Pass 4 test tracks four (overlapping with three of the original five), the block ends up with the new four — not nine. This is what makes one-screengrab debugging-by-Inspector actually work.
- **Mirrors, not new sources.** Each field in the block is a **copy** of a value that lives somewhere else in the Inspector (or on `ImitoneVoiceIntepreter`). Duplicates are intentional and expected. Permanent fields **do not** live under `CURRENT TEST`.
- **`currentTestDescription`** sits at the top of the block. It carries the test name, what to report back, and the decision tree — accessible via the Inspector tooltip. The engineer can read the whole protocol without leaving the Inspector.
- **Field naming:** `currentTest*` so they sort together and are obviously disposable.
- **Rewrite BEFORE each testing round** — not during, not after. When a step's first-pass code lands, the same edit batch updates the `CURRENT TEST` block (header, `currentTestDescription`, field set, `LateUpdate` mirror copies, tooltip bar) to match what that round needs. The engineer never enters Play mode against a stale CURRENT TEST. If the test protocol changes mid-investigation (e.g. a hypothesis shifts and demands different values), the rewrite happens **before** the next Editor session, in its own commit if needed.
- **Disposable.** When the test / bug closes, the entire block is removed. Any field that proved valuable long-term is **moved down** into the appropriate permanent header section before the block is deleted — it doesn't stay under `CURRENT TEST`.
- **Bug doc / step plan owns the deeper protocol.** The block surfaces values; the active bug document (`Docs/STEP_<n>_BUG_<name>.md`) or the relevant step section in this plan doc owns the hypotheses, evidence log, decision tree, and pass-level test bar. The two cross-reference each other.

This convention lives **outside** any single bug — keep it across the rest of the rearchitecture and beyond.

---

## Source documents and prior investigation

This plan supersedes two prior investigation notes (now deleted):

- `Docs/MIC_VOICE_INGEST_FINDINGS.md` — concise running log of evidence and provisional code added during diagnosis.
- `Docs/MIC_VOICE_INGEST_HANDOFF_ALTERNATIVE_APPROACH.md` — long-form handoff summarizing the problem, user-validated perceptual evidence, and earlier hypotheses.

Cleanup actions, baseline numbers, and "provisional code to delete" lists from those documents are folded into Section C and the appendices below.

### Environment

- **Engine:** Unity **2022.3.12f1** (project also targets Mac).
- **Mic:** Wired USB (not Bluetooth).
- **Persistence:** Symptoms reproduce across PC restart and across multiple sessions.
- **Project Settings → Script Execution Order (verified 2026-05-04):** No `MicPipeline` entry — **confirmed removed** from the list in the Editor (not stored in versioned YAML). Order is `TMPro.TextMeshPro` (−105) → `ImitoneVoiceIntepreter` (−104) → `GameValues` (−103) → `DirectVoiceMonitoring` (−102) → `RecordedAudioPlayback` (−101). Wwise's `AkInitializer` runs much earlier at −108. Class-level `[DefaultExecutionOrder(50)]` on `ImitoneVoiceIntepreter` is still overridden by Project Settings −104 until Step 5b cleanup.

  **Cleanup decision (this rearchitecture):** the C# `[DefaultExecutionOrder(...)]` class attributes will be **removed** during the rearchitecture so there is exactly one source of truth (Project Settings). Before removing any attribute, **verify** the Project Settings entry exists for that class — see Step 5b (for the remaining classes). The agent should *prompt the user to open Project Settings → Script Execution Order and confirm* before deleting an attribute, so we don't end up with a class running at default order 0 by accident.

### Critical constraint (do not dismiss)

The user has **categorically** validated that during multi-second `unread_zero` spells they hear their **own** voice in monitoring with low latency (~250 ms range), including deliberately strange vocal patterns reproduced in the headphones **in step with what they are doing** — not "old tape" and not unrelated ambience.

The diagnosis in Section A (audio capture is being done on the wrong thread; main-thread jitter starves imitone while monitoring's ring read stays current) must remain consistent with this perceptual evidence. If during implementation a measurement appears to contradict it, **stop and reconcile** rather than assuming the user's perceptual test is wrong. The canonical "is the ring still being fed" metric is `MicVoiceIngestDebugAggregate.aggMicRawRingWriteTotalSamples` / `aggMicNormRingWriteTotalSamples` — keep those alive through the rearchitecture.

### Filename and class-name pitfall (MUST READ before any rename)

The actual file and class on disk contains a typo that was introduced during the original setup of this system:

- File: `Assets/Scripts/Voice/ImitoneVoiceIntepreter.cs` (note: **"Intepreter"** — missing the second "r")
- Class: `public class ImitoneVoiceIntepreter : MonoBehaviour`

**Both spellings appear throughout the codebase.** A repo-wide search shows the typo'd form `ImitoneVoiceIntepreter` is dominant (the actual class name and most references use it), but a small number of files use the correct spelling `ImitoneVoiceInterpreter` — those are likely stale references, comments, or unused dangling code. **We are not fixing the typo today.** Renaming the class would risk breaking serialized GameObject references in `MainGame.unity`, prefabs, and every consumer file — an unrelated refactor with its own migration pass.

For this plan: this document uses both spellings somewhat interchangeably for readability, but **the canonical class identifier is `ImitoneVoiceIntepreter`** (typo intact). When writing or moving code, match whatever spelling is already in use in that file. If you genuinely need to add a new reference and have a choice, use the typo'd form so we don't add new "correct" stragglers that would also need migration if the typo is ever fixed.

### Files in scope

| File | Role after rearchitecture |
|------|---------------------------|
| `Assets/Scripts/Voice/MicPipeline.cs` | **Deleted** in Step 0.7d (responsibilities absorbed into `ImitoneVoiceIntepreter` in Step 0.7c) |
| `Assets/Scripts/Voice/ImitoneVoiceIntepreter.cs` | New owner of capture + imitone feed (audio thread, post-Step-3) and game logic (main thread). Step 0.7 absorbs `MicPipeline`; Step 5b deletes the legacy main-thread mic-ingest block once the audio thread is feeding imitone. |
| `Assets/Scripts/Voice/DirectVoiceMonitoring.cs` | Unchanged in shape; ring-read call sites repointed at `ImitoneVoiceIntepreter` in Step 0.7b. Click hardening (M1/M2/M6) added in Step 5a. |
| `Assets/Scripts/Voice/MicVoiceIngestDebugAggregate.cs` | Field references migrated to `ImitoneVoiceIntepreter` in Step 0.7b; FAIL OBSERVATION block added in Step 0.5; obsolete fields removed in Step 5b / Step 6. |
| `Assets/Scripts/Voice/Reference/OLD_ImitoneVoiceInterpreterForDebugComparison.cs` | Safety-net reference; archive (not delete) until Step 7 passes |
| `Assets/Scripts/Utilities/RecordedAudioPlayback.cs` | Consumer of `MicPipeline`'s normalized ring (`GetComponent<MicPipeline>()` on the imitone GameObject); migrate refs to `ImitoneVoiceIntepreter` in Step 0.7b |
| `Assets/Scenes/MainGame.unity` | Holds serialized references to `MicPipeline` and `ImitoneVoiceIntepreter` GameObjects/components; reassign in Step 0.7b |

---

## A. The problem

### Symptom

During toning, imitone's analysis quality degrades. Pitch tracking feels stuck or laggy. The SoundSelf feedback loop (voice → imitone → lights/sound → felt response) breaks in a way the user perceives but the system doesn't crash or error.

The bug existed in the old monolithic `ImitoneVoiceInterpreter` at lower severity. The split into `MicPipeline` + `ImitoneVoiceInterpreter` made it dramatically worse, even though both versions use identical mic-reading math.

### Root cause

**Audio capture is being done on the wrong thread.**

Unity's main thread runs `Update()` at variable cadence. Frame times stretch and compress depending on what else is happening (Wwise events, JSON parsing, breath coroutines, list allocations, etc.). `Microphone.GetPosition` is consulted once per `Update()` to decide how many samples to read. Two compounding problems result:

1. **`unread_zero` frames.** Some `Update()` calls land too close to the previous one, before the OS has advanced the mic write head. `GetPosition` returns the same value as last frame. The pipeline reads zero samples that frame.
2. **Burst frames.** The next `Update()` after one or more `unread_zero` frames reads everything that accumulated. Imitone gets fed (for example) 0, 0, 0, big, 0, 0, big — instead of a steady stream.

Imitone's pitch tracker is real-time-paced internally. Bursty input violates its assumption that audio arrives in continuous time. The internal 1-second sliding analysis window becomes inconsistent, and pitch tracking degrades.

### Why the new architecture is worse than the old one

The new architecture introduced more per-frame main-thread work without changing the underlying threading model:

- Two ring buffer writes per `copied_samples` frame (raw + normalized), each a sample-by-sample loop under a lock.
- Normalization peak loop over the full frame.
- `UpdateNormalizationGainRiding` + `UpdateNormalizationTelemetry` every frame.
- Audio-thread `TryEnter` contention against the main-thread locks 60–100 times per second.
- A producer/consumer seam between two MonoBehaviours, each running its own logic per frame.

Higher per-frame main-thread cost produces more variable frame times, which produces more `unread_zero` frames and longer burst windows on recovery. Same underlying jitter, amplified.

### Critical clarification

The user's perceptual test of "I can hear my voice with low latency in monitoring during stuck spells" is consistent with the diagnosis. `DirectVoiceMonitoring` reads from the ring buffer (which keeps moving as long as ANY frame succeeds at reading samples), and produces audio output independently from imitone's analysis path. So monitoring stays healthy while imitone's input gets choppy. Both consume the same ring buffer; only imitone's pitch tracker is sensitive to input timing.

---

## A.5. Diagnostic strategy in the new architecture

A reasonable concern with the rearchitecture: **if `unread_zero` is gone, how do we observe that the fix actually worked, beyond "feels better"?**

The `unread_zero` counter was a *symptom-specific* telemetry — it directly measured the failure mode of the old architecture. The new architecture has different failure modes (audio thread starvation, lock contention, missed callbacks, atomicity tears) and needs different telemetry. The `MicVoiceIngestDebugAggregate` script becomes the single Inspector panel where this evidence lives.

### What healthy and broken look like (high level)

In the new architecture, **healthy** has a clear signature:

- Audio callback total climbs at a near-constant rate (~46.875 Hz at 1024 samples / 48 kHz, or whatever the DSP buffer size dictates).
- Time gap between consecutive callbacks stays close to nominal (~21 ms for 1024 samples).
- Every callback feeds imitone — the imitone-input counter tracks the callback counter 1:1.
- Ring write totals climb continuously at the sample rate (~48000 samples/second).
- Lock-miss counters stay near zero.
- `_dbMicrophone` updates smoothly with voice; no Inspector flicker / impossible values.

**Broken** has equally clear signatures. The "broken-pattern → likely cause" map is in Step 6's interpretation guide.

### What gets added to `MicVoiceIngestDebugAggregate`

A new top-of-panel section plus two diagnostic-detail sections:

1. **FAIL OBSERVATION** *(new top-of-panel section, added in Step 0.5 — see below)* — a single all-caps boolean `FAILURE` that the user can glance at once to see categorical broken-vs-not, with a small set of all-caps subsidiary booleans underneath that categorize *what kind* of failure. Always present, always at the top of the Inspector. The set of subsidiary flags evolves with the architecture; the top-level `FAILURE` boolean always means the same thing: "something is wrong."
2. **Audio thread health** — callback rate, gap, samples-per-callback, GC alloc detection. *(Added in Step 1.)*
3. **Cross-thread atomicity** — tear-detection flags for any field shared between audio thread (writer) and main thread (reader). *(Added in Step 3.)*

A fourth section (**Imitone feed**) groups InputAudio and GetState counters so it's obvious at a glance whether the audio thread and main thread are both doing their respective jobs.

The exact fields are defined in **Step 0.5** (the FAIL OBSERVATION block, initial set against the current architecture), **Step 1** (audio-thread health + Phase 2 fail flags), **Step 3** (atomicity + imitone-feed fail flags), and **Step 6** (final consolidation, naming pass, and interpretation guide). **Step 0.7** does not change the FAIL OBSERVATION set — it just repoints the aggregate's snapshot reads from `MicPipeline` to `ImitoneVoiceIntepreter`, so the same Phase 1 flags continue to fire on the same conditions throughout the merge.

### The FAIL OBSERVATION block — design principle

This is the user's primary categorical observability tool. Every new failure mode introduced during the rearchitecture **must** register itself as a subsidiary flag and feed into the top-level `FAILURE` boolean. Every failure mode that gets retired (e.g. `unread_zero` after Step 5b) **must** have its subsidiary flag deleted so the panel doesn't accumulate dead checkboxes.

**Naming convention:** all FAIL OBSERVATION fields are `[SerializeField]`-exposed `bool`s, named in `ALL_CAPS_WITH_UNDERSCORES`. The top-level field is `FAILURE`. Subsidiary fields begin with `FAIL_` for sort order.

**Truth rule:** `FAILURE` is `true` if and only if at least one subsidiary `FAIL_*` flag is `true`. No other input feeds `FAILURE` directly — it is a pure OR of categorized sub-flags. This guarantees the user can always click into a `true` `FAILURE` and find at least one specific cause.

**Threshold convention:** every `FAIL_*` flag is set by a clearly-defined trigger: usually "metric X has been in condition C for >= N consecutive aggregate updates OR >= T wall-clock seconds." Each trigger's thresholds are themselves serialized fields (with `[Header("FAIL OBSERVATION — thresholds")]`) so the user can tune sensitivity without recompiling.

The full list of `FAIL_*` flags by phase is enumerated in Step 0.5 (Phase 1, current architecture), Step 1 (Phase 2 additions), Step 3 (Phase 3 additions), and Step 5b/6 (Phase 4 retirements). Step 0.7 (the `MicPipeline` merge) leaves the flag *set* unchanged — only the field locations move.

### Telemetry consolidation rule

`MicVoiceIngestDebugAggregate` is the single Inspector panel for cross-cutting diagnostics. Each individual file should keep **only**:

- Telemetry that concerns that file's internal behavior and is useful when debugging that file *in isolation* (e.g., `DirectVoiceMonitoring` keeps its monitoring-specific underflow / starvation / gain / clip-state fields; that's its self-concern and shouldn't be moved).
- The relevant `toneActive` telemetry needed in-place for game-logic decision making.

Everything else — anything cross-cutting, anything diagnosing the producer/consumer relationship, anything aggregating across files — lives in `MicVoiceIngestDebugAggregate` only. **Do not duplicate** the same metric in two places: it diverges and creates confusion. If you ever find yourself adding the same field to both a feature file and the aggregate, prefer the aggregate and have the feature file's Inspector help-text point at the aggregate instead.

---

## B. The solution

### Architecture

Move audio capture and imitone feeding from the main thread to the audio thread, using Unity's `OnAudioFilterRead` callback. The main thread becomes purely a *reader* of analysis state.

**Final shape: two scripts.**

1. **`ImitoneVoiceInterpreter`** (existing file, expanded responsibilities)
   - **`OnAudioFilterRead` (audio thread, ~21ms cadence):** reads mic clip, writes ring buffer, calls `imitone.InputAudio`, applies filters, computes `_dbMicrophone`.
   - **`Update` (main thread, per frame):** calls `imitone.GetState`, parses JSON, runs `CheckToning`, `TrackMicVolume`, Wwise events, breath coroutines.
   - **`Start` (main thread, once):** calls `Microphone.Start`, sets up imitone, allocates buffers.
2. **`DirectVoiceMonitoring`** (existing file, unchanged)
   - Continues to consume the ring buffer via `OnAudioFilterRead`. Already correct in shape.
3. **`MicPipeline`** (existing file, deleted)
   - All responsibilities absorbed into `ImitoneVoiceInterpreter`.

### Why this works

- **Audio thread is deterministic.** `OnAudioFilterRead` fires at the DSP system's fixed cadence (typically 1024 samples = ~21ms at 48kHz), regardless of frame rate or main-thread state. There is no `unread_zero` category at all.
- **`imitone.InputAudio` is documented thread-safe.** `imitone.cs` line 77: "Unlike other functions, this can be called from a different thread." The C# wrapper does no allocations per call (NativeArray pre-allocated at construction).
- **Imitone's internal sliding window stays continuously fresh.** New audio flows in every 21ms. By the time any render frame fires `GetState`, the analysis is current to within 21ms.
- **Main thread responsiveness improves.** Capture cost moves off `Update()`. Per-frame work shrinks to: one `GetState` call, JSON parse, game logic.

### Vulnerabilities and limitations

These need to be tested for or navigated during implementation.

#### V1: `OnAudioFilterRead` allocation hazards

The audio thread has zero tolerance for blocking. Inside `OnAudioFilterRead`:

- **No allocations.** No `new`, no `string` operations, no `List.Add`, no boxing, no `Mathf.Abs` (it's fine, just an example of confirming it's allocation-free), no LINQ.
- **No Unity API calls.** `Microphone.GetPosition`, `Time.deltaTime`, `Debug.Log`, `AudioSettings.dspTime` (some happen to work, but Unity makes no guarantee — verify each one used).
- **No locks held by the main thread.** Use `Monitor.TryEnter(lock, 0)` with a clean fallback if the lock isn't immediately available. The existing `MicPipeline` ring buffer code already does this correctly.

**Mitigation:** Allocate every buffer in `Start()`. Pre-size the mono downmix scratch (V11) and any ring-write staging at startup. Mirror the existing `Monitor.TryEnter` pattern for ring writes. Note: the existing main-thread chunking buffer (`_imitoneChunkBuffer`) and the `new float[chunkSize]` allocation in the chunking branch (`ImitoneVoiceIntepreter.cs` ~lines 738–750) go away with V4 / Step 3 — those are not buffers we re-pre-allocate, they are buffers we delete.

#### V2: `imitone.GetState` thread safety is not guaranteed

The imitone docs explicitly call out `InputAudio` as thread-safe, but say nothing about `GetState`. Treat `GetState` as main-thread-only. This means:

- `InputAudio` runs on audio thread.
- `GetState` runs on main thread.
- These two calls happen on different threads but touch the same imitone DLL instance.

**Mitigation:** This is the standard imitone usage pattern (per the example provided), so the library appears to handle it internally. We do not need to add our own lock around imitone calls. If we observe corruption or crashes during `GetState`, we'd revisit. Worth a brief sanity test: stress-test with rapid `InputAudio`/`GetState` calls during early implementation to confirm stability.

#### V3: Do not call `Microphone.*` or `AudioClip.GetData` from the audio thread

(Updated based on follow-up research; this section now states the canonical answer rather than presenting alternatives.)

`Microphone.GetPosition` and `AudioClip.GetData` are **not safe to call from `OnAudioFilterRead`** and they are also **not necessary**. Three reasons, in order of severity:

1. **Wrong timeline.** `Microphone.GetPosition` returns a position into the mic clip's ring buffer **at the microphone's sample rate**. The buffer Unity hands you in `OnAudioFilterRead` has already been resampled to `AudioSettings.outputSampleRate` and re-channeled to the mixer's channel count. Mixing those two timelines produces drift and seam artifacts (a plausible click / glitch source). The truth at the audio-thread layer is `data[]` itself; the only position counter that makes sense there is the cumulative `data.Length / channels` you maintain yourself.
2. **No documented thread-safety contract.** `Microphone.GetPosition`, `Microphone.IsRecording`, `Microphone.Start`, `Microphone.End`, and `AudioClip.GetData` are all native icalls into Unity's audio runtime. Unity's docs make no thread-safety guarantee for any of them. Absence of a guarantee should be read as "do not rely on it." Community reports across Unity 2018-2023 mention occasional hangs and stale values when these are called off the main thread.
3. **`AudioClip.GetData` is the more dangerous of the two**: it allocates and copies, and on a streamed `Microphone` clip the engine is concurrently writing the same ring buffer with no documented snapshot guarantee.

**Canonical pattern (used by Lasp, Oculus Lipsync, Wwise, FMOD):** route the mic clip *through* an AudioSource. Set `audioSource.clip = microphoneClip` and `audioSource.Play()` so Unity itself plays the mic clip through the audio graph. The audio thread receives the resampled, mixer-rate buffer in `data[]` for free. Read `data[]`, write to the ring, feed imitone, optionally clear `data[]` to silence playback. No `Microphone.*` or `AudioClip.GetData` calls anywhere on the audio thread.

**Position tracking on the audio thread:** maintain a `long _samplesWritten` field; increment by `data.Length / channels` each callback. That counter is sample-accurate, lock-free, allocation-free, and version-agnostic.

**`Microphone.Start` (main thread, once at startup):** call with `frequency = AudioSettings.outputSampleRate` so Unity's resampler is a no-op — eliminating resampler-boundary artifacts as a click source. (See V10 and click prevention M7.)

#### V4: Audio buffer size vs imitone's expected chunk size

Imitone's `feed_buffer` is sized to one second (`sampleRate` samples). It can accept smaller chunks. Audio thread typically delivers 1024 samples per callback at 48kHz. We should confirm the actual DSP buffer size at runtime via `AudioSettings.GetConfiguration().dspBufferSize` (call once at start, on main thread).

**Mitigation:** No special handling needed if buffer size ≤ 1 second. The existing chunking code (which splits buffers > 1 second into chunks) becomes unnecessary at audio-thread cadence and can be removed.

#### V5: AudioSource setup for OnAudioFilterRead

(Updated based on follow-up research; the AudioSource is no longer a "silent driver" — it actively plays the mic clip through the audio graph so `OnAudioFilterRead` receives mic samples in `data[]` for free.)

`OnAudioFilterRead` only fires on a `MonoBehaviour` that lives on a GameObject with an active, playing AudioSource. The canonical capture pattern (used by Lasp, Oculus Lipsync, Wwise, FMOD) is to **set `audioSource.clip = microphoneClip` and play it**. Unity's audio graph then resamples the mic clip to mixer rate and hands the buffer to `OnAudioFilterRead` via the `data[]` parameter — sample-accurate, allocation-free, no `Microphone.*` calls on the audio thread.

**Decision (made for this rearchitecture):** `ImitoneVoiceIntepreter` (capture / imitone feed) and `DirectVoiceMonitoring` live on **separate GameObjects**, each with its own dedicated AudioSource. This is the canonical pattern (used by Lasp, Oculus Lipsync, etc.) and matches SoundSelf's existing scene layout (`Imitone` GameObject and `DirectVoiceMonitoring` GameObject under `SoundSelfAudioVisualControl`).

- `ImitoneVoiceIntepreter`'s AudioSource (on `Imitone` GameObject): plays the mic clip silently. Reads samples from `data[]` in `OnAudioFilterRead`, writes the ring, feeds imitone. Optionally clears `data[]` to zero so this AudioSource doesn't double-output mic to the speaker bus.
- `DirectVoiceMonitoring`'s AudioSource (on `DirectVoiceMonitoring` GameObject): writes the user-facing monitoring audio into its own `data[]` from the consolidated ring buffer (existing pattern).

**Why separate GameObjects (not co-located):** Unity's `OnAudioFilterRead` chain logic fires the callback once **per AudioSource's chain on the same GameObject**. If both MonoBehaviours lived on one GameObject with two AudioSources, each callback would fire twice per buffer (once for each chain) — and half of those calls would receive the wrong source's `data[]` (e.g., `ImitoneVoiceIntepreter.OnAudioFilterRead` running with the monitoring source's data). Separate GameObjects with one AudioSource each = unambiguous chain routing. The deterministic write-before-read property (M3) we actually need comes from **Project Settings → Script Execution Order**, not GameObject co-location — see below.

**Required AudioSource configuration on the imitone capture source:**

```csharp
// In ImitoneVoiceIntepreter.Start() — VERIFY exact property names against Unity 2022.3 ScriptReference at implementation time
captureSource.clip = microphoneClip;          // microphoneClip from Microphone.Start(...) — see V3
captureSource.loop = true;
captureSource.volume = 0f;                    // NOT mute = true — see "critical gotcha" below
captureSource.bypassEffects = true;
captureSource.bypassListenerEffects = true;
captureSource.bypassReverbZones = true;
captureSource.spatialBlend = 0f;
captureSource.playOnAwake = false;            // we control the timing of Play()
captureSource.Play();
```

**Critical gotcha: `mute = true` kills `OnAudioFilterRead`.** Setting `AudioSource.mute = true` prevents the audio graph from invoking `OnAudioFilterRead` at all on that source, which silently breaks the entire capture path. Use `volume = 0f` (plus `bypassEffects = true`) to silence the bus output while keeping the callback alive. (Alternative: route through an AudioMixer group with the volume snapshot at silent.)

**Sample-rate match — mandatory** (V10 / click prevention M7): when calling `Microphone.Start`, pass `frequency = AudioSettings.outputSampleRate` so Unity's resampler is a no-op. Resampler boundaries are a known click source.

**How deterministic write-before-read is achieved (M3):** Unity fires `OnAudioFilterRead` callbacks across MonoBehaviours in **Project Settings → Script Execution Order** — same-GameObject *or* different-GameObject. With `ImitoneVoiceIntepreter` at −104 and `DirectVoiceMonitoring` at −102 (already correct in this project), the capture's `OnAudioFilterRead` runs first on every audio buffer; it writes the ring; then the monitoring's `OnAudioFilterRead` fires and reads the freshly-written ring. No jitter at the seam.

**Verification requirements (each is a checkbox in Step 1 / Step 5b):**
- `Imitone` GameObject has exactly **one** AudioSource (the new capture one). Multiple AudioSources reintroduce the chain-routing ambiguity described above.
- `DirectVoiceMonitoring` GameObject has exactly **one** AudioSource (the existing monitoring one).
- Project Settings → Script Execution Order has `ImitoneVoiceIntepreter` < `DirectVoiceMonitoring` (currently −104 < −102).

#### V6: Filter state across audio callbacks

The high-pass and low-pass filters in `ImitoneVoiceIntepreter` are sample-by-sample IIR filters (biquads). They maintain a small amount of state across calls (the previous one or two input/output samples). Currently this state lives in main-thread fields. Moving filtering to the audio thread means filter state must be touched only from the audio thread.

**DSP answer to the natural concern "will they even work on samples so short?":** Yes — completely. IIR filters process **one sample at a time**, using the filter's persistent state and that single sample. The buffer length passed in does not affect the filter math; whether you call the filter on a 1-sample buffer or a 1024-sample buffer, the per-sample arithmetic is identical and the output is correct. This is also where filters live in every standard audio engine — the audio thread *is* the canonical home for sample-by-sample DSP. The only requirement is **state continuity across calls**: the filter's internal state must persist between buffers, untouched, on the same thread. That's the requirement we're enforcing here.

**Mitigation:** Verify filter state fields are only read/written inside `OnAudioFilterRead` after the move. If main-thread code ever reads filtered samples, that code path needs to either move to the audio thread or read from a thread-safe snapshot. **Do not initialize / reset filter state mid-stream** — see click prevention M4.

#### V7: `_dbMicrophone` is read by main thread, written by audio thread

Currently `_dbMicrophone` is computed and consumed on the main thread. After the move, it's computed on the audio thread but consumed by `CheckToning`, `SetNoiseFloorThreshold`, etc., on the main thread.

**Mitigation:** A single float read across threads in C# is *almost* atomic on x64 (32-bit aligned float reads are atomic on x86/x64 Mono in practice), but the language spec does not guarantee it. Two safer options:

- **`volatile float`** for the field. This guarantees the compiler doesn't reorder reads / writes around it and that the read sees the latest committed value. Minimal cost; sufficient for "show this dB value in the Inspector / use it for thresholding."
- **`Interlocked.Exchange(ref _dbMicrophoneRaw, value)`** (with the field as `long` or `int` storing the float bits via `BitConverter.SingleToInt32Bits`). Stronger guarantee with full memory barrier; use only if `volatile` proves insufficient.

Default for this rearchitecture: **start with `volatile float`** for `_dbMicrophone` and any other audio-written / main-read float (e.g., normalized peak meter, monitoring gain reading). Escalate to `Interlocked` only if the tear-detection telemetry below shows a problem.

**Visibility in `MicVoiceIngestDebugAggregate` (created in Step 3, finalized in Step 6):** A dedicated **"Cross-thread atomicity"** section in the Inspector panel will surface:

- `aggDbMicrophoneSnapshot` (float) — the value as read by main thread this frame.
- `aggDbMicrophoneTearDetectedTotal` (long) — counter of detected torn reads. **Detection method:** once per `LateUpdate`, read `_dbMicrophone`; if the value is `NaN`, ±`Infinity`, or outside a plausible dB range (e.g. `-120f ≤ x ≤ +24f`), increment the counter. The reasoning: a "torn read" of a `float` happens *within* a single read instruction — you load half-of-old-bits and half-of-new-bits, producing a corrupted value. That corruption very often manifests as impossible bit-patterns (`NaN`, infinities, or wildly out-of-range magnitudes), so a value-bounds sanity check is the right way to detect it. Any non-zero count here is a flag to escalate that field from `volatile` to `Interlocked`. (Note: this heuristic catches corrupted-bit-pattern torn reads, not "the writer changed the value during the read" — that's a normal race rather than torn data, and `volatile` already addresses it.)
- `aggCrossThreadFieldsUsingVolatile` (string, read-only label) — lists which shared fields are currently `volatile` vs `Interlocked`. Quick reference for the user.
- `aggCrossThreadFieldsUsingInterlocked` (string, read-only label) — same.

**How to interpret what you see:**
- `aggDbMicrophoneTearDetectedTotal` stays at **0** across long sessions → `volatile` is sufficient; no action needed.
- `aggDbMicrophoneTearDetectedTotal` climbs (any non-zero value over time) → an atomicity bug; escalate that field to `Interlocked` and re-test.
- The `Volatile` / `Interlocked` labels show *which mitigation is currently active*. They are set once in `Start()` based on the actual code and are read-only thereafter. This lets you see at a glance whether you're running the cheap or strong variant.

#### V8: Telemetry and debug fields

`MicVoiceIngestDebugAggregate` reads many `MicPipeline` debug fields. After collapse, these fields move to `ImitoneVoiceInterpreter` or are deleted. The aggregate panel needs updating.

**Mitigation:** Audit `MicVoiceIngestDebugAggregate`, update field references to point at the new locations, delete fields that no longer apply (e.g., `unread_zero` exit reason — the entire concept goes away).

#### V9: Execution order coupling

Project Settings no longer lists `MicPipeline` (removed in 0.7d; verified 2026-05-04). `ImitoneVoiceIntepreter` remains at −104, `GameValues` at −103, `DirectVoiceMonitoring` at −102. With audio-thread capture, main-thread execution order matters less for the voice path — `Update()` ordering only affects when `GetState` is called relative to game logic (and any consumer of voice state, e.g., `GameValues`).

**Mitigation:** Keep `ImitoneVoiceIntepreter` at its current −104 (already before `GameValues` at −103 and before `DirectVoiceMonitoring` at −102, so all current consumers of `toneActive`, `pitch_hz`, etc. are downstream as intended). `DirectVoiceMonitoring` order no longer matters for ingest; keep it at −102.

#### V10: Latency budget

Audio thread cadence is ~21 ms (1024 samples at 48 kHz). This means imitone's input is up to 21 ms behind the live mic. Combined with imitone's own analysis latency and the render frame's time-to-react, total perceived latency is a few tens of ms. This should be imperceptible for the SoundSelf feedback loop, but worth confirming during testing — especially against a baseline where the user is paying close attention to responsiveness.

**WASAPI floor (Windows context):** Unity / FMOD on Windows uses shared-mode WASAPI, which has a latency floor equal to the OS shared engine period (~10 ms on Windows 10/11). You cannot beat this without a native plugin in exclusive mode — and you do not need to for pitch tracking. The audio-thread tap pattern in V3/V5 is the right destination for imitone.

**Mitigation if latency feels too high:** reduce DSP buffer size in Project Settings → Audio.

| DSP buffer setting | Approx. samples | Approx. latency at 48 kHz | Trade-off |
|----|----|----|----|
| Best latency | 256 | ~5 ms | Highest CPU; more frequent callbacks; tighter no-allocation discipline required |
| Good latency | 512–1024 | ~10–21 ms | **Sweet spot for imitone-class analysis** |
| Best performance | 2048+ | ~43 ms+ | Lowest CPU; perceptible feedback lag |

Default for this project: **Good latency** (512 or 1024 samples). Move to Best latency only if the SoundSelf feedback loop measurably benefits, and only after confirming no audio-thread allocations or callback-rate drops in the FAIL OBSERVATION block.

#### V11: Microphone and mixer channel counts

The `data[]` buffer in `OnAudioFilterRead` is at the **mixer's** channel count, not the mic clip's. On Windows desktop with default settings the mixer is typically stereo (2 channels), so even a mono USB mic arrives as a 2-channel buffer because Unity's audio graph upmixes when the AudioSource plays the mic clip.

The existing `MicPipeline` already establishes the contract: the ring buffer and imitone feed are **mono**. Multi-channel mics are downmixed at ingest time (`MicPipeline.cs:1133` logs a warning and downmixes). The new architecture must honor the same contract.

**Default behavior:** sum-then-divide downmix in `OnAudioFilterRead` before ring write / imitone feed.

```csharp
// Inside OnAudioFilterRead(float[] data, int channels):
int frames = data.Length / channels;
for (int i = 0; i < frames; i++)
{
    float sum = 0f;
    int baseIdx = i * channels;
    for (int c = 0; c < channels; c++) sum += data[baseIdx + c];
    monoScratch[i] = sum / channels;
}
// Now feed monoScratch to the ring and to imitone
```

**Why this works in the common case:** when the mic is mono and the mixer is stereo, Unity's upmix writes the mono signal identically to both stereo channels. `(L + R) / 2` recovers the original mono signal exactly — no information loss, no phase issues.

**For true stereo or multi-channel mics:** sum-mono is the conventional fallback and matches the existing `MicPipeline` contract. SoundSelf controls the supported hardware (mono USB mic in 99% of cases), so the sum-mono fallback is not a quality concern in practice — it just exists as a defensive default for the rare case where a user runs the app with a different mic plugged in.

**Telemetry:** `MicVoiceIngestDebugAggregate` should show `aggMicClipChannels` (the source clip's channel count, set once at startup) and `aggMixerChannels` (the value of the `channels` parameter from the most recent `OnAudioFilterRead`, captured atomically). If they ever differ, that's expected — Unity's audio graph is upmixing for us. If `aggMixerChannels` ever changes mid-session, that's unusual and worth investigating.

**Composer agent attention:**
- The `monoScratch` buffer is pre-allocated in `Start()` to a size large enough for the maximum expected `data.Length / channels`. Use `AudioSettings.GetConfiguration().dspBufferSize` as the reference for sizing.
- Don't assume `channels == 2`. The user's audio configuration could be mono, stereo, 5.1, 7.1, or anything else. The loop above works in all cases.
- Don't conflate `microphoneClip.channels` with the `channels` parameter of `OnAudioFilterRead` — they describe different boundaries.

---

## C. Implementation plan

Each step is sized to be testable and committable on its own. After each step: run the project, verify expected behavior, check for unexpected regressions, commit to git. Working on `main` directly (single editor; no branch needed).

**How to use the checkboxes:** every task in every step is a checkbox. Tick `[x]` when you have completed and verified that task. Move on to the next step only when every checkbox in the current step is ticked. If a checkbox stays unchecked because the task turned out to be unnecessary, leave a one-line note next to it explaining why — don't silently skip it.

**Developer notes field:** every step ends with a `**Developer notes:**` field that defaults to `_none_`. Use it as a running journal during execution — observations, surprises, decisions, follow-up questions, anything worth flagging. When you want me to consider or integrate something you've added, just tell me and I'll review the relevant step's notes and fold it into the plan (new tasks, V/M sections, FAIL OBSERVATION flags, etc., per the "must be in a step" rule).

### Step 0: Baseline measurement and safety net

> **Recommended LLM for this step:**
> - **First-pass: Either model is fine** — Step 0 is mostly engineer-driven observation (recordings, Project Settings inspection, Developer-notes capture). The agent role is light.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5)
>
> *Confirm the right model is selected before continuing. Switch to Opus 4.7 when the first-pass is complete and the mandatory review pass begins.*

Before any changes, capture current behavior so we can A/B compare during and after.

**Tasks:**
- [x] Verify `OLD_ImitoneVoiceInterpreterForDebugComparison.cs` is present and runnable as a fallback.
- [skipping] Take screen recordings of toning sessions on the current code: one calm baseline, one deliberately heavy session (sustained loud toning, unusual pitch patterns).
- [x] Record values from `MicVoiceIngestDebugAggregate` during a stuck spell: `aggMicExitReason` (look for `unread_zero`), `aggMicRawRingWriteTotalSamples`, `aggMicNormRingWriteTotalSamples`, `aggInterpTryCopyTrue`, `aggRawConsumedThisFrame`, monitoring underflow / starvation totals.
- [x] Note current Project Settings audio config: sample rate, DSP buffer size (Project Settings → Audio). Save these somewhere referencable for Step 1.
- [x] Note current Project Settings → Script Execution Order entries for ~~`MicPipeline`~~ *(removed in 0.7d)*, `ImitoneVoiceIntepreter`, `DirectVoiceMonitoring`, `RecordedAudioPlayback`. (Used to confirm they're still set correctly after Step 0.7d / Step 5b / Step 6 attribute removal passes.)
- [~] Confirm `ImitoneVoiceIntepreter` and `DirectVoiceMonitoring` are on **separate** GameObjects in `MainGame.unity`, each with **exactly one AudioSource** (per V5's corrected decision). Co-location is *not* desired — it would reintroduce multi-AudioSource chain-routing ambiguity. The deterministic write-before-read property comes from Project Settings → Script Execution Order, which is already correct (`ImitoneVoiceIntepreter` at −104 < `DirectVoiceMonitoring` at −102).

**Reference baseline values from prior captures (carry-forward from the old `FINDINGS.md`):**

| When | Frame | Mic ingest | Interpreter | Monitoring (cumulative) | Notes |
|------|-------|------------|-------------|-------------------------|--------|
| 2026-05-01 user capture | **6965** | `unread_zero`, unread **0**, latest raw **0**, write **==** read **152320**, stalled head **1**, clip **288000** samples | Raw consumed **false**, TryCopy **false**, count **0**, mic ready **true**, mic dB ~**−42** (stale carry-over), imitone dB ~**−42** | Underflow **19** / samples **18720**, overflow **2** / **18208**, starvation **18** | Tone counters **10** / confident **1** — toning context. dB-with-zero-copy means values were not refreshed this frame; consistent with `unread_zero`. |
| 2026-05-01 follow-up | Profiler **~42813** | — | — | Underflow / starvation totals **flat** while ingest stuck | `MicPipeline.Update` ~**0.09 ms** on a ~**9.8 ms** CPU frame. Mic update is **not** the spike sink; supports the diagnosis that the variance lives elsewhere on the main thread. |

These numbers are not targets; they are the "what bad looks like" anchor against which Step 7's final validation will be A/B compared.

**Notes & considerations:**
- This step is documentation / observation only; no source files are modified.
- Take the baseline *with* Step 0.5's FAIL OBSERVATION block already in place if you can sequence it that way — it gives you a cleaner categorical baseline ("how often does `FAILURE` go true?") than counting raw `unread_zero` events.

**Test:** None. Establishes baseline.

**Commit:** `chore: baseline recordings before voice rearchitecture`

**Developer notes:**

*Project Settings → Script Execution Order (captured 2026-05-04, authoritative for the rearchitecture):*

| Script | Order |
|--------|-------|
| `UnityEngine.EventSystems.EventSystem` | −1000 |
| `TMPro.TextContainer` | −110 |
| `AkInitializer` | −108 |
| `TMPro.TextMeshPro` | −105 |
| `ImitoneVoiceIntepreter` | **−104** ← absorbs legacy mic-ingest responsibilities (Step 0.7c); order unchanged |
| `GameValues` | **−103** ← downstream consumer of voice state |
| `DirectVoiceMonitoring` | **−102** ← unchanged; audio-thread ring consumer |
| `RecordedAudioPlayback` | **−101** ← unchanged |
| `TMPro.TextMeshProUGUI` | −100 |
| `UnityEngine.InputSystem.PlayerInput` | −100 |
| `AkBank` | −75 |
| `AkAudioListener` | −50 |
| `AkGameObj` | −25 |
| `AkState` | −20 |
| `AkSwitch` | −10 |
| `TimeLeftScript` | −9 |
| `TimeTrackerScript` | −8 |
| `CSVLoader` | −5 |
| `DevelopmentMode` | −4 |
| `CalibrationMenu` | −3 |
| `Sequencer` | −2 |
| `UnityEngine.UI.ToggleGroup` | 10 |
| `CSVWriter` | 50 |
| `AkTerminator` | 100 |

*`MicPipeline` is not listed in Script Execution Order (removed Step 0.7d; verified absent 2026-05-04).*

*Pending changes for the voice rearchitecture (informational; actual changes are tasked in Steps 0.7/5/6):*
- ~~**Remove:** `MicPipeline (−105)` entry — Step 0.7d.~~ **Done** — Script Execution Order **verified**: `MicPipeline` is **not** listed (2026-05-04).
- **Adjust:** `ImitoneVoiceIntepreter (−104)` — number unchanged; the script absorbed mic ingest (Step 0.7c). Class-level `[DefaultExecutionOrder(50)]` is removed during Step 5b's `[DefaultExecutionOrder]` cleanup pass.
- **No change:** `DirectVoiceMonitoring (−102)`, `RecordedAudioPlayback (−101)`.

*GameObject layout decision (settled 2026-05-04):*

`ImitoneVoiceIntepreter` lives on the `Imitone` GameObject and `DirectVoiceMonitoring` lives on the `DirectVoiceMonitoring` GameObject (under `MicrophonePlayback`). They share the parent `SoundSelfAudioVisualControl` but are on different GameObjects.

**This separation is correct and intentional.** V5 / M3 originally suggested co-locating them on the same GameObject; that recommendation has been walked back because Unity's `OnAudioFilterRead` chain logic fires the callback once per AudioSource's chain on a GameObject — which means co-locating with two AudioSources would make each callback fire twice per buffer, with half the calls receiving the wrong source's `data[]`. Separate GameObjects with one AudioSource each = unambiguous chain routing. Deterministic write-before-read (M3) is achieved by Project Settings → Script Execution Order alone, which already orders `ImitoneVoiceIntepreter (−104)` before `DirectVoiceMonitoring (−102)`.

**Required: each GameObject must have exactly one AudioSource (after Step 1).** Re-verified at Step 1 setup and Step 5b. Adding a second AudioSource to either GameObject reintroduces the chain-routing ambiguity.

*Current AudioSource baseline (captured 2026-05-04):*
- **`Imitone` GameObject:** **0 AudioSources** ← correct for pre-Step-1 state. `ImitoneVoiceIntepreter` (mic ingest merged in 0.7c-ii) polls `Microphone.GetPosition` / `AudioClip.GetData` on the main thread and does not need an AudioSource until Step 1 adds the dedicated capture AudioSource on top.
- **`DirectVoiceMonitoring` GameObject:** **1 AudioSource** ← correct. `DirectVoiceMonitoring.OnAudioFilterRead` requires an active AudioSource on the same GameObject to fire at all.

After Step 1, the `Imitone` GameObject will have exactly 1 AudioSource and `DirectVoiceMonitoring` will continue to have exactly 1 — the canonical target state.

*Observations:*
- **`GameValues (−103)`** sits between `ImitoneVoiceIntepreter` and `DirectVoiceMonitoring` and was not previously enumerated in the plan. It runs *after* `ImitoneVoiceIntepreter` on the main thread, which is the correct position if it consumes voice state (`toneActive`, `pitch_hz`, `_dbMicrophone`, etc.). If it ever turns out to *produce* voice-state inputs that `ImitoneVoiceIntepreter` reads, that would be a circular dependency and we'd need to revisit. Not flagging as an action — just a thing to be aware of during Step 0.7b's "audit consumers" pass and Step 5b's `[DefaultExecutionOrder]` cleanup.
- **`AkInitializer (−108)`** runs well before any voice script, which is correct — Wwise must be initialized before any voice-driven Wwise events fire.
- **`TMPro.TextMeshPro` at −105** is now alone in that slot after `MicPipeline` was removed from execution order (0.7d).

*Project Settings → Audio (captured 2026-05-04, authoritative):*

| Setting | Value | Plan implication |
|---------|-------|------------------|
| Global Volume | 1 | — |
| Volume Rolloff Scale | 1 | — |
| Doppler Factor | 1 | — |
| Default Speaker Mode | **Stereo** | Mixer is 2-channel. Confirms V11's expected `aggMixerChannels == 2` for the healthy-reading column. |
| System Sample Rate | **48000** | Unity *requests* 48 kHz; the OS device wins at runtime. Plan's `Microphone.Start(deviceName, true, 1, audioConfigOutputSampleRate)` adapts (Step 1). |
| DSP Buffer Size | **Best performance** (= 1024 samples) | ~21 ms per buffer, ~43 ms round-trip. User decision: keep at "Best performance" for now (CPU / battery headroom > minor latency win). Step 4's V10 tuning is optional. |
| Max Virtual Voices | 512 | Wwise-managed; not relevant to voice-ingest path. |
| Max Real Voices | 32 | Wwise-managed; not relevant. |
| Spatializer Plugin | None | — |
| Ambisonic Decoder Plugin | None | — |
| **Disable Unity Audio** | **Unchecked** ✓ | **Critical for plan: must remain unchecked.** If checked, `OnAudioFilterRead` does not fire and the entire audio-thread architecture fails. Step 1 includes a defensive verification task. |
| Enable Output Suspension (editor only) | Checked | Editor-only; suspends audio when Editor loses focus. No production impact. |
| Virtualize Effects | Checked | Unity-side voice virtualization; Wwise mostly handles audio so this is mostly inert for SoundSelf's path. |

*Cross-check vs. plan assumptions:*
- 48 k request matches plan ✓
- DSP buffer 1024 matches plan's healthy-reading reference (~46.9 Hz callback rate at 1024 / 48 k) ✓
- Speaker mode stereo matches V11's expected upmix from mono mic ✓
- Unity audio enabled — required for the plan to work at all ✓ (verified again at runtime in Step 1)

---

### Step 0.5: Add the FAIL OBSERVATION block to `MicVoiceIngestDebugAggregate` (current architecture)

> **Recommended LLM for this step:**
> - **First-pass: Composer 2 (full)** — mostly mechanical: Inspector field additions, threshold values, main-thread trigger logic in `LateUpdate`. No audio-thread or cross-thread subtleties yet.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5)
>
> *Confirm the right model is selected before continuing. Switch to Opus 4.7 when the first-pass is complete and the mandatory review pass begins.*

This step lands the **FAIL OBSERVATION** Inspector section before any architectural changes start. The benefit is observability *now*, against the existing failure modes (`unread_zero`, gentle recovery firing, monitoring starvation, etc.). Every later step will extend or trim this block; the top-level `FAILURE` boolean is the user's persistent anchor across the whole rearchitecture.

**Tasks:**

- [x] Add the FAIL OBSERVATION inspector section at the **very top** of `MicVoiceIngestDebugAggregate.cs` (above the existing `[Header("References ...")]` so it is the first thing visible when the GameObject is selected).
- [x] Add the Phase 1 subsidiary `FAIL_*` flag fields (snippet below).
- [x] Add the `[Header("FAIL OBSERVATION — thresholds")]` block with serialized threshold fields (snippet below).
- [x] Implement per-flag tracking state as private (non-serialized) fields: consecutive-frame counters, last-known total snapshots for monotonic counters, scene-start time.
- [x] Implement the trigger-condition logic for each flag in `LateUpdate`, **after** the existing aggregate copies (so the flags read freshly-aggregated data).
- [x] Compute the top-level `FAILURE` as a pure OR of all subsidiary flags, **last** in the `LateUpdate` (so any flag set this frame propagates immediately).
- [x] Add `[Tooltip("...")]` attributes to each flag summarizing its trigger condition. (Hovering the flag in Inspector should show the trigger without opening source.)
- [x] Add a sticky-flag clear mechanism: either a `[ContextMenu]` editor method "Clear FAIL OBSERVATION sticky flags", or a `[SerializeField] private bool clearFailObservationStickyFlags;` that, when ticked, clears all sticky flags and immediately ticks itself off. Pick one and stay consistent.

*Add a new Inspector section at the very top of `MicVoiceIngestDebugAggregate.cs`* (above the existing `[Header("References ...")]`):

```csharp
[Header("FAIL OBSERVATION (glance here first)")]
[SerializeField] private bool FAILURE;
[SerializeField] private bool FAIL_UNREAD_ZERO_SUSTAINED;
[SerializeField] private bool FAIL_INGEST_RING_STALLED;
[SerializeField] private bool FAIL_INTERPRETER_NOT_CONSUMING;
[SerializeField] private bool FAIL_GENTLE_RECOVERY_FIRED;
[SerializeField] private bool FAIL_MONITORING_STARVATION_GROWING;
[SerializeField] private bool FAIL_MIC_NOT_READY;
```

*Add a thresholds section (also serialized so the user can tune at runtime):*

```csharp
[Header("FAIL OBSERVATION — thresholds")]
[SerializeField] private int failUnreadZeroSustainedFrameThreshold = 30;
[SerializeField] private float failUnreadZeroSustainedSecondsThreshold = 0.5f;
[SerializeField] private int failIngestRingStalledFrameThreshold = 30;
[SerializeField] private int failInterpreterNotConsumingFrameThreshold = 30;
[SerializeField] private float failMonitoringStarvationWindowSeconds = 2f;
[SerializeField] private float failMicNotReadyGracePeriodSeconds = 2f;
```

*Compute each flag in `LateUpdate` (after the existing aggregate copies):*

| Flag | Triggers when |
|------|---------------|
| `FAIL_UNREAD_ZERO_SUSTAINED` | `aggMicExitReason` stays in the unread-zero family (`unread_zero` **or** `unread_zero_gentle_restart`, since the restart is itself a symptom) for >= `failUnreadZeroSustainedFrameThreshold` consecutive `LateUpdate` calls **OR** for >= `failUnreadZeroSustainedSecondsThreshold` wall-clock seconds. The segment only resets when `MicPipeline` reports something *outside* this family (e.g. `copied_samples`). |
| `FAIL_INGEST_RING_STALLED` | `aggMicRawRingWriteTotalSamples` has not advanced for >= `failIngestRingStalledFrameThreshold` consecutive `LateUpdate` calls. |
| `FAIL_INTERPRETER_NOT_CONSUMING` | `aggRawConsumedThisFrame == false` for >= `failInterpreterNotConsumingFrameThreshold` consecutive frames **while** mic is ready (`aggInterpMicReady == true` and not in the startup grace period). |
| `FAIL_GENTLE_RECOVERY_FIRED` | `aggMicGentleUnreadZeroRecoveryTotal` has incremented since the last clear. (Sticky for a tunable window or until the user clears it manually; see implementation note below.) |
| `FAIL_MONITORING_STARVATION_GROWING` | `aggMonStarvationEvents` increased within the last `failMonitoringStarvationWindowSeconds` seconds. |
| `FAIL_MIC_NOT_READY` | `aggInterpMicRefNull == true` OR `aggInterpMicReady == false`, **after** `failMicNotReadyGracePeriodSeconds` since scene start. |

`FAILURE = FAIL_UNREAD_ZERO_SUSTAINED || FAIL_INGEST_RING_STALLED || FAIL_INTERPRETER_NOT_CONSUMING || FAIL_GENTLE_RECOVERY_FIRED || FAIL_MONITORING_STARVATION_GROWING || FAIL_MIC_NOT_READY;` — pure OR. Computed last, so any sub-flag set this frame propagates immediately.

**Notes & considerations:**
- Naming is intentionally loud (all caps). Resist the temptation to "soften" — the user has chosen this style for at-a-glance categorical visibility.
- Do **not** put rate-limited `Debug.Log` calls behind these flags. The flags exist for visual observation in the Inspector; logging is a separate concern and would noise up the console.
- Threshold defaults are starting points. Expect to tune them after the first stuck-spell capture during baseline recording — for example, `failUnreadZeroSustainedSecondsThreshold` should be small enough to fire on the kind of stuck spell that's actually painful (anything ≥ 0.3 s is probably worth flagging).
- This step's commit precedes any architectural change. After Step 5b/6, the Phase 1 flags `FAIL_UNREAD_ZERO_SUSTAINED` and `FAIL_GENTLE_RECOVERY_FIRED` will be removed; new flags from Steps 1, 3, and 6 take their place. The top-level `FAILURE` boolean stays. Step 0.7 (the merge) doesn't change this set — it just repoints the aggregate's reads onto the merged `ImitoneVoiceIntepreter`.

**Test:**
- [x] Run the scene normally; `FAILURE` is `false` during clean operation.
- [x] Reproduce a stuck spell (or wait for one to occur naturally during toning); `FAILURE` flips to `true` with at least one `FAIL_*` flag identifying the category (almost certainly `FAIL_UNREAD_ZERO_SUSTAINED` or `FAIL_INTERPRETER_NOT_CONSUMING`).
- [x] Verify the thresholds feel right relative to perceived bug severity; tune the serialized threshold fields if the flags trigger too eagerly or too sluggishly.
- [x] Verify the sticky-flag clear mechanism works (tick `FAIL_GENTLE_RECOVERY_FIRED` in your head, clear, confirm it un-sticks).
- [x] Use this block during Step 0's baseline recording sessions to characterize "how often does `FAILURE` go true and which sub-flags trigger?" — this is a cleaner baseline metric than chasing individual `unread_zero` counts.

**Commit:** `feat: add FAIL OBSERVATION block to MicVoiceIngestDebugAggregate (Phase 1)`

**Developer notes:** Implemented 2026-05-04. Sticky clear is the Inspector-only `clearFailObservationStickyFlags` tick (processed **after** aggregate copies so gentle-recovery baseline matches this frame). Ring-stall counter resets when clear runs so the next frame re-baselines write totals. Interpreter not-consuming increments only while `aggInterpMicReady` is true (resets when mic not ready).

Review-pass changes (Opus 4.7, same day):
- **R1 (fixed):** `FAIL_UNREAD_ZERO_SUSTAINED` segment now treats both `unread_zero` and `unread_zero_gentle_restart` as "still in trouble," so periodic gentle restarts don't reset the consecutive-frame counter and mask a chronic stuck spell. Segment resets only on a *different* exit reason (e.g. `copied_samples`).
- **R2 (skipped, defer to play-mode):** all-caps `FAIL_*` field display in the Inspector — observe whether Unity's `NicifyVariableName` keeps the loud caps or title-cases them; if title-cased, add `[InspectorName(...)]` per flag.
- **M1 (fixed):** tooltip on `FAIL_GENTLE_RECOVERY_FIRED` now names the actual clearing field (`clearFailObservationStickyFlags`).
- **M2 (fixed):** added an inline comment near `Time.timeSinceLevelLoad` documenting the deliberate scene-reload reset.
- **M3 (kept current):** `FAIL_INTERPRETER_NOT_CONSUMING` resets when mic momentarily not-ready (lenient). A "freeze, don't reset" stricter variant is logged for future consideration if Phase 1 telemetry shows we're missing real failures.
- **M4:** plan/code consistency — no change needed.

Recovery note (2026-05-04): a `git reset` during merge resolution wiped this work from the working tree once; it was re-applied verbatim from this conversation's context. Lesson: commit step deltas before any merge work.

---

### Step 0.7: Merge `MicPipeline` into `ImitoneVoiceIntepreter` (behavior-preserving refactor)

> **Recommended LLM for this step: mostly Opus 4.7, with one Composer 2 exception.**
> - **0.7a (facade scaffold), 0.7b (consumer repointing), 0.7c-ii (state + lifecycle cut-over), 0.7c-iii (audit gate), 0.7d (deletion):** Opus 4.7 first-pass. This step moves mic capture, ring buffer ownership, and mic-thread-adjacent state. Even though it's mechanically a refactor with no behavior change, the surface area is wide (multiple consumer files, scene re-wiring, Project Settings) and the code being moved is the same code we're trying to fix. Use the thinking model.
> - **0.7c-i (type relocation + debug field rename):** Composer 2 first-pass. This is the one purely-cosmetic sub-pass — symbol renames and a nested type move with no runtime path changes. Composer 2 is faster and cheaper for this kind of work.
> - **All review passes: Opus 4.7** (always; see working agreement rule 5).
>
> *Each sub-pass has its own LLM recommendation in its header. Confirm the right model is selected at the top of each sub-pass before starting.*

**Why this step exists at this point in the plan:** the original plan placed the `MicPipeline` → `ImitoneVoiceIntepreter` collapse at Step 5 (after the audio-thread work was built). That ordering had a hidden contradiction: Step 1 wanted to call `Microphone.Start` while `MicPipeline` was still owning the device — Unity's `Microphone.Start` returns null on a device that's already recording. Reordering the merge to here gives us **one mic owner** (the merged `ImitoneVoiceIntepreter`) before any audio-thread work begins. Step 1 then layers the audio thread on top of the merged file's already-open clip with no parallel-mic problem. Decision Point #1 from the original Step 1 outline goes away entirely.

**Trade-off accepted:** we lose the file-level fallback during Steps 1-4 (you can't simply disable `MicPipeline.cs` to revert). We retain Git as the safety net, the legacy main-thread block is still alive *inside* the merged file (commenting it out is a fallback), and `OLD_ImitoneVoiceInterpreterForDebugComparison.cs` remains.

**Crucial rule for this step:** every sub-pass is **commit-before-moving-on**. The recent rollback incident (Step 0.5 was wiped by a `git reset` during merge resolution) is a reminder that uncommitted refactor work is fragile. Each sub-pass below ends with a commit step — do not start the next sub-pass until the previous one is committed.

This step is a **behavior-preserving refactor**. Zero perceptual change. The same `unread_zero` failures exist after Step 0.7 as before (and the FAIL OBSERVATION block continues to surface them). What changes is **who owns the mic capture code**.

---

**Sub-pass 0.7a — Add facade methods on `ImitoneVoiceIntepreter` that delegate to `MicPipeline` (purely additive):**

The goal is to give external consumers a single public interface on `ImitoneVoiceIntepreter` that returns the same data they currently get from `MicPipeline`. Internally, the facade just forwards each call. This pass adds API surface without moving any state — the easiest, safest possible starting point.

- [x] In `ImitoneVoiceIntepreter.cs`, audit existing public properties: `MicrophoneBuffer`, `MicrophoneDeviceName`, `MicrophoneSampleRate` already exist as pass-throughs to `micPipeline.X`. Good — they're already part of the facade. Note them.
- [x] Add public methods that consumers will need (each delegates to the corresponding `MicPipeline` method internally). **Decisions made (see Developer notes below):** scope was minimal-driven-by-actual-consumers, snapshot type stays in `MicPipeline.cs` for 0.7a (moves with state in 0.7c), `Mic`-prefixed naming on properties for clarity (`IsMicReady` etc.).
  - **Properties added:** `IsMicReady`, `MicChannels`, `MicCaptureEpoch`.
  - **Methods added:** `ReadRawSamples` (4-param), `ReadNormalizedSamples` (4-param), `ReadNormalizedSamples` (2-param), `CreateNormalizedReadPositionBehindMs`, `TryCreateRawReadCursorBehindMs`, `TryCreateNormalizedReadCursorBehindMs`, `GetMicIngestDebugSnapshot` (returns `MicPipeline.MicIngestDebugSnapshot`).
  - **Deliberately skipped** (no external caller; revisit if 0.7b finds a gap): `NormalizationStateChanged` event, `Get/SetNormalizationState`, `SetNormalizationEnabled/GainDb/HardClampEnabled/ClampAbs`, `GetNormalizationGainLinear`, `TryCopyLatestRawFrame`, `TryCopyLatestNormalizedFrame`, `InitializeMicrophone`, `ReadRawSamples` (2-param overload), `CreateRawReadPositionBehindMs`.
- [x] Audit `MicPipeline`'s public surface: enumerated above; minimal facade covers the consumer-driven subset.
- [x] **Do not yet repoint any consumer.** External code still calls `micPipeline.X` directly; the facade is just additive sugar.
- [x] Run scene; confirm zero behavior change. Inspector still shows the same MicVoiceIngestDebugAggregate values it always did.

*Compile + run + smoke test. Commit before moving on.* — **smoke test passed (2026-05-04). Commit pending.**

**Commit (0.7a):** `refactor: add facade methods on ImitoneVoiceIntepreter delegating to MicPipeline` — SHA `39482da19ccb969c6cf8198b2f09ef4d1825dfd6` (2026-05-04).

*Developer notes for 0.7a:*
- **Decisions logged:** (1) `Mic`-prefixed property names (`IsMicReady`, `MicChannels`, `MicCaptureEpoch`) chosen over `IsReady`/`Channels`/`CaptureEpoch` to disambiguate from the interpreter's own state. (2) `MicIngestDebugSnapshot` type definition stays in `MicPipeline.cs` for 0.7a; moves to `ImitoneVoiceIntepreter` in 0.7c with the rest of the state. (3) Minimal-driven-by-actual-consumers facade scope — see "Deliberately skipped" list above for what we left out and why.
- **Review-pass findings (accepted as-is):**
  - **R1 (minor, accepted):** Null-path on the 4-param `ReadRawSamples` / `ReadNormalizedSamples` doesn't advance `readPosition` / `readTotalSamples` like `MicPipeline`'s lock-miss path does. Benign — the only consumer (`DirectVoiceMonitoring`) gates on `IsMicReady` upstream. If we ever add a consumer that doesn't gate, revisit. Fix would be `readTotalSamples += destination.Length` in the null branch.
  - **R2 (very minor, accepted):** `GetMicIngestDebugSnapshot()` null-path returns `default(MicPipeline.MicIngestDebugSnapshot)`, so `lastExitReason` is `null` instead of `""` (which is what `MicPipeline` always normalizes to). Functionally inert — the Phase 1 `FAIL_UNREAD_ZERO_SUSTAINED` string compares evaluate to `false` either way. Fix would be `return new MicPipeline.MicIngestDebugSnapshot { lastExitReason = "" };`.

---

**Sub-pass 0.7b — Repoint all external consumers from `MicPipeline` to `ImitoneVoiceIntepreter`:**

Now that the facade exists, every consumer can read from `ImitoneVoiceIntepreter` instead of `MicPipeline`. Internally `ImitoneVoiceIntepreter` is still delegating to `MicPipeline`, so data flow is identical. After this sub-pass, **no external file calls `MicPipeline` directly anymore**.

*Repoint each consumer:*
- [x] `Assets/Scripts/Voice/DirectVoiceMonitoring.cs` — collapsed the serialized `MicPipeline micPipeline` field into the existing `imitoneVoiceInterpreter` reference (Decision 1). All `micPipeline.X` calls now go through the facade. Lazy `GetComponent<MicPipeline>()` resolves removed in `InitializeMonitoring` and `TryRecoverSetupIfNeeded`.
- [x] `Assets/Scripts/Voice/MicVoiceIngestDebugAggregate.cs` — removed the `MicPipeline micPipeline` field, kept the existing `interpreter` field as the single source. `LateUpdate` snapshot copy now calls `interpreter.GetMicIngestDebugSnapshot()`. Awake auto-fill simplified. Phase 1 FAIL OBSERVATION flags read the same fields through the new owner — verified working in smoke test.
- [x] `Assets/Scripts/Utilities/RecordedAudioPlayback.cs` — removed the private `MicPipeline micPipeline` cache and its lazy `GetComponent<MicPipeline>()` resolves. All ring reads, channel/sample-rate reads, and cursor-creation calls now go through `imitoneVoiceInterpreter` (existing public Inspector field).
- [x] `Assets/Scenes/MainGame.unity` — Inspector references reassigned by the user; smoke test passed in Play mode with zero "missing script" / "missing component" warnings.
- [x] **Verified with `rg`:** outside `MicPipeline.cs` itself and `ImitoneVoiceIntepreter.cs` (facade owner), the only remaining textual `MicPipeline` references are intentional transitional doc comments and the single `MicPipeline.MicIngestDebugSnapshot` type qualifier in `MicVoiceIngestDebugAggregate.cs` (Decision 3, accepted; type relocates in 0.7c).

*Compile + run + smoke test. Phase 1 FAIL OBSERVATION should still surface stuck spells exactly as before. Commit before moving on.* — **smoke test passed (2026-05-04). Commit pending.**

**Commit (0.7b):** `refactor: repoint mic-ingest consumers from MicPipeline to ImitoneVoiceIntepreter` — SHA `f78b0de40f69195fa70833e8446cb6727a409716` (2026-05-04).

*Developer notes for 0.7b:*
- **Decisions logged:** (1) Collapsed the per-consumer `MicPipeline micPipeline` field into the consumer's existing `ImitoneVoiceIntepreter` reference where one already existed (`DirectVoiceMonitoring`, `RecordedAudioPlayback`); did a true field-level removal in `MicVoiceIngestDebugAggregate` where only the standalone `interpreter` field was kept. (2) Kept the existing variable-name pattern per file (`imitoneVoiceInterpreter` in consumers, `interpreter` in the aggregate) — match-the-file-style over fleet-wide consistency. (3) `MicVoiceIngestDebugAggregate.cs` retains a single `MicPipeline.MicIngestDebugSnapshot` type qualifier; this is the only remaining type-level dependency on `MicPipeline` in any consumer file and resolves in 0.7c when the type relocates.
- **Scene-config requirement (post-0.7b):** Any scene or prefab that uses `DirectVoiceMonitoring` or `RecordedAudioPlayback` must now wire the `imitoneVoiceInterpreter` Inspector field explicitly. The previous lazy `GetComponent<MicPipeline>()` fallback path is gone (Decision 1). Smoke test confirmed `MainGame.unity` is correctly wired. Worth flagging to anyone authoring a new scene that consumes the voice path.
- **Review-pass findings:**
  - **R1 (cosmetic, fixed):** `aggMicExitReason` could briefly serialize as `null` (string) in transient pipeline states because the facade's null-path returns `default(MicPipeline.MicIngestDebugSnapshot)` (which has `lastExitReason = null`, not `""`). Fixed inline in `MicVoiceIngestDebugAggregate.LateUpdate` with a `?? ""` null-coalesce. Comparable to 0.7a R2 — but here the consumer-side normalization is the cheaper fix.
  - **R2 (improvement, accepted):** `FAIL_INGEST_RING_STALLED` gate widened from "aggregate-side `micPipeline` assigned" to "interpreter assigned". This is a tightening of detection — if the interpreter is set but its internal pipeline goes unhealthy, the stall counter ticks up and the FAIL flag fires. Smoke test did not produce false positives on cold-start. If false positives appear later, mirror the existing `failMicNotReadyGracePeriodSeconds` grace into the stall check.
  - **R3 (accepted, documented above):** Lazy `GetComponent<MicPipeline>()` resolves removed in two consumers — pre-approved by Decision 1. Consequence captured in the "Scene-config requirement" bullet above.
  - **M1 (cosmetic, fixed):** Stale comment in `MicVoiceIngestDebugAggregate.cs` LateUpdate ("resets when MicPipeline reports anything else") rewritten as "resets when ingest reports anything else" — producer-agnostic.
  - **M2 (deferred, pre-existing):** `RecordedAudioPlayback.StartRecording` leaves `_hasActiveRecordingFundamental = true` on the early-return error path. Pre-existing, not introduced by 0.7b. Tracked here so it isn't lost; not actioned.
  - **M3 (cosmetic, fixed):** Tooltip on the aggregate's `interpreter` field was forward-looking ("after the 0.7 merge"). Rewritten to be more precise about the multi-step nature of the merge.

---

**Sub-pass 0.7c — Migrate state ownership from `MicPipeline` into `ImitoneVoiceIntepreter`:**

This is the substantive sub-pass — the actual refactor. State (mic device handle, ring buffers, gentle recovery, `UpdateMicReadFrame`, the entire main-thread mic-ingest loop) moves from `MicPipeline.cs` into `ImitoneVoiceIntepreter.cs`. The facade collapses: where 0.7a's facade methods called `micPipeline.X`, they now read local fields on `this`.

**Decisions logged before 0.7c (locked in 2026-05-04):**
- (1) **`MicIngestDebugSnapshot` placement:** relocates as a **nested public struct on `ImitoneVoiceIntepreter`** (`ImitoneVoiceIntepreter.MicIngestDebugSnapshot`), not as a top-level type. Keeps the namespace tidy and matches the existing `RawVoicePathDebugSnapshot` pattern.
- (2) **Misleading debug fields renamed in 0.7c, not deferred to Step 6.** `debugInterpreterMicPipelineRefNull` → `debugInterpreterMicRefNull`; `debugInterpreterMicPipelineReady` → `debugInterpreterMicReady`. Same renames in the `RawVoicePathDebugSnapshot` struct (`interpreterMicPipelineRefNull` → `interpreterMicRefNull`; `interpreterMicPipelineReady` → `interpreterMicReady`). Consumer-side aggregate field names (`aggInterpMicRefNull` / `aggInterpMicReady`) are already mic-correct — only their right-hand sides change.
- (3) **0.7a facade stays consumer-transparent.** External consumers (`DirectVoiceMonitoring`, `RecordedAudioPlayback`, `MicVoiceIngestDebugAggregate`) keep calling `imitoneVoiceInterpreter.X(...)` exactly as they do today. Only the *internal* implementation of those facade methods changes — from `return micPipeline.X(...)` (in 0.7a/0.7b) to `return this.X(...)` (in 0.7c-ii). No consumer-side rewrites needed.

**Three internal passes (locked in 2026-05-04):**

| Pass | Scope | Risk | LLM |
|---|---|---|---|
| **0.7c-i** | Cosmetic prep: type relocation + debug field rename. No runtime path touched. | Low (pure renames + type move) | Composer 2 first-pass; Opus 4.7 review |
| **0.7c-ii** | Substantive cut-over: state + private methods + lifecycle (`Microphone.Start`/`End`) + facade rewire + `MicPipeline.cs` reduced to empty stub. | High (threading + lifecycle) | Opus 4.7 first-pass; Opus 4.7 review |
| **0.7c-iii** | Verification + audit gate before 0.7d. Minimal/no code changes; click test + Phase 1 FAIL OBSERVATION sanity check + `rg`/`using` sweep. | Low (verification only) | Opus 4.7 |

Each sub-pass commits independently. The three together constitute 0.7c.

**This is where threading subtleties have the most opportunity to creep in.** Pay attention to:

- **Lock objects** must move with the ring buffers they protect. Don't accidentally split a lock from its data.
- **`Microphone.Start` ownership** transfers in 0.7c-ii. After 0.7c-ii completes, `ImitoneVoiceIntepreter.Start()` calls `Microphone.Start(deviceName, true, lengthSec, sampleRate)` and assigns the returned clip to its own private field. `MicPipeline` no longer touches `Microphone.*` at all.
- **Update / LateUpdate ordering** must remain correct. `ImitoneVoiceIntepreter`'s `Update` now does what `MicPipeline.Update` used to do (the mic read loop) plus what it always did (the imitone GetState, game logic, etc.). Order matters: mic read first, then game logic that depends on the read.
- **`[DefaultExecutionOrder]` attributes:** `MicPipeline` has `[DefaultExecutionOrder(-500)]`. `ImitoneVoiceIntepreter` has `[DefaultExecutionOrder(50)]`. Both are overridden by Project Settings, but the `-500` on `MicPipeline` was suggestive — it ran **before** `ImitoneVoiceIntepreter`'s `[DefaultExecutionOrder(50)]` *if* Project Settings entries were missing. After the merge, both sets of work happen inside one `MonoBehaviour` so the inter-class order question goes away. Project Settings → Script Execution Order will retire the `MicPipeline (−105)` entry in 0.7d.

---

**Sub-pass 0.7c-i — Decoupled prep (type relocation + debug field rename):**

Goal: Get the cosmetic / decoupled changes out of the way before touching the runtime path. After this pass, type names and field names align with their post-0.7c semantics, but the runtime mic-ingest path still flows through `MicPipeline.cs` exactly as in 0.7b.

> **Recommended LLM for this step: Composer 2 (Cursor).**
> - Mechanical type relocation + symbol renames. No threading or DSP concerns. Composer 2 is faster and cheaper for this kind of refactor.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5).
>
> *Confirm Composer 2 is selected before starting; switch to Opus 4.7 before the review pass.*

*Tasks:*

- [x] **Move `MicIngestDebugSnapshot` type** from `MicPipeline.cs` into `ImitoneVoiceIntepreter.cs` as a nested public struct: `public struct MicIngestDebugSnapshot { ... }` directly inside the `ImitoneVoiceIntepreter` class body. Copy field declarations exactly; preserve declaration order so `default(...)` initialization gives the same byte layout. Delete the original definition from `MicPipeline.cs`.
- [x] **Update `MicPipeline.GetMicIngestDebugSnapshot()`** to return `ImitoneVoiceIntepreter.MicIngestDebugSnapshot` instead of the now-deleted `MicPipeline.MicIngestDebugSnapshot`. Internal `new MicIngestDebugSnapshot { ... }` initializers update accordingly. (This method itself is removed in 0.7c-ii along with the rest of `MicPipeline`'s body — for now we just need it to compile.)
- [x] **Update `ImitoneVoiceIntepreter`'s 0.7a facade method `GetMicIngestDebugSnapshot()`** to return the new nested type. Inside the class body, `MicIngestDebugSnapshot` resolves without qualifier; the explicit `MicPipeline.MicIngestDebugSnapshot` in the 0.7a null-path `default(...)` becomes `default(MicIngestDebugSnapshot)`.
- [x] **Update consumer `MicVoiceIngestDebugAggregate.cs`** to reference `ImitoneVoiceIntepreter.MicIngestDebugSnapshot` instead of `MicPipeline.MicIngestDebugSnapshot`. Remove the transitional doc comment about "type still nested in MicPipeline until 0.7c."
- [x] **Rename debug fields on `ImitoneVoiceIntepreter`:**
  - `debugInterpreterMicPipelineRefNull` → `debugInterpreterMicRefNull`
  - `debugInterpreterMicPipelineReady` → `debugInterpreterMicReady`
  - Update `[Tooltip(...)]` strings on these fields to reflect post-0.7c semantics: e.g. "True if the interpreter has no usable mic this frame" / "True if the interpreter's mic is initialized and reading samples this frame".
- [x] **Rename matching fields in `RawVoicePathDebugSnapshot` struct** on `ImitoneVoiceIntepreter`:
  - `interpreterMicPipelineRefNull` → `interpreterMicRefNull`
  - `interpreterMicPipelineReady` → `interpreterMicReady`
  - Update the snapshot construction site inside `ImitoneVoiceIntepreter` (the `GetRawVoicePathDebugSnapshot()` method body) to use the new field names.
- [x] **Update consumer `MicVoiceIngestDebugAggregate.cs`** read sites (`v.interpreterMicPipelineRefNull` / `v.interpreterMicPipelineReady`) to the new names. The `agg*` field names on the aggregate side stay the same (they were already mic-correct).
- [x] **Verify with `rg`:** running `rg -n "MicIngestDebugSnapshot" Assets/Scripts/` should show **only** the `ImitoneVoiceIntepreter` definition and references to `ImitoneVoiceIntepreter.MicIngestDebugSnapshot` (or unqualified, inside the class body). Zero matches for `MicPipeline.MicIngestDebugSnapshot`.
- [x] **Verify with `rg`:** running `rg -n "InterpreterMicPipeline|interpreterMicPipeline" Assets/Scripts/` should return zero matches — **exception:** `[FormerlySerializedAs("debugInterpreterMicPipelineRefNull")]` / `"…Ready"` attribute strings still contain the old names by design (migration hooks).

*Compile + run + smoke test (Phase 1 FAIL OBSERVATION still surfaces stuck spells exactly as in 0.7b — no behavior changed). Commit before moving on.* — **Smoke test passed (2026-05-04).**

**Commit (0.7c-i):** `refactor: relocate MicIngestDebugSnapshot to ImitoneVoiceIntepreter and rename interpreter mic-debug fields` — SHA `519810f2b6082f34a723167034fc53df9369c2e2` (2026-05-04).

**Developer notes for 0.7c-i:**
- Added `[FormerlySerializedAs("debugInterpreterMicPipelineRefNull")]` / `[FormerlySerializedAs("debugInterpreterMicPipelineReady")]` on the renamed `[SerializeField]` debug booleans so prefabs/scenes deserialize cleanly; `MainGame.unity` YAML keys updated to the new names explicitly.
- Type relocation only — runtime mic path unchanged (`MicPipeline.GetMicIngestDebugSnapshot()` still builds the snapshot from MicPipeline internals).
- Play-mode smoke test passed after commit `519810f2b6082f34a723167034fc53df9369c2e2`.

---

**Sub-pass 0.7c-ii — Substantive cut-over (state + methods + lifecycle):**

Goal: The actual state and ownership migration. After this pass, `MicPipeline.cs` is reduced to an empty stub class with no fields and no methods. All mic-ingest behavior has moved into `ImitoneVoiceIntepreter`.

> **Recommended LLM for this step: Opus 4.7 (strongly).**
> - Threading-subtlety pass. Locks moving with their data, `Microphone.Start` ownership transferring, `Update` ordering preservation, no-allocation discipline. Opus 4.7's reasoning is worth the cost here.
> - **Review pass: Opus 4.7** (always).
>
> *Confirm Opus 4.7 is selected before continuing. Stay on Opus through both first-pass and review for this step.*

*Tasks:*

- [x] **Move private state.** All `MicPipeline` private fields move into `ImitoneVoiceIntepreter` as private members. Be exhaustive — ring buffers, lock objects, normalization state (filter coefficients, hp/lp state, gain/clamp config), gentle recovery counters, double-poll bookmark fields, `_samplesWritten`-style position trackers, debug telemetry fields, capture epoch counter, mic device name, microphone buffer `AudioClip` reference, sample rate, channel mode. **Do not** drop the gentle recovery fields yet — those get deleted in Step 5b. Behavior is preserved.
- [x] **Move private methods.** `UpdateMicReadFrame`, `PerformGentleUnreadZeroCaptureRestart`, ring-write helpers, normalization helpers, telemetry update methods, `InitializeMicrophone` private internals. Add a one-line `// runs on: main thread` annotation header on each (none are audio-thread-bound yet — that comes in Step 3).
- [x] **Rewire the 0.7a facade.** Each method added in 0.7a as `return micPipeline.X(...)` becomes a direct call to `this.X(...)` (or inline if the body is small). The 0.7a null-safety guards become unnecessary — remove them. **0.7a R1 (cursor-advance asymmetry) resolves naturally** because the lock-miss path is now the canonical implementation. **0.7a R2 (`lastExitReason = null` in null-path) and 0.7b R1 (the `?? ""` null-coalesce in `MicVoiceIngestDebugAggregate`) both resolve naturally** because the snapshot is built directly here from real state, not from a `default(...)`. Remove the `?? ""` from `MicVoiceIngestDebugAggregate.LateUpdate` once the facade is rewired.
- [x] **Remove the `[SerializeField] private MicPipeline micPipeline` field** on `ImitoneVoiceIntepreter`. The Inspector slot disappears. Any place inside `ImitoneVoiceIntepreter` that previously did `micPipeline.X` (e.g. `InitializeMicrophoneAndImitone`, `GetRawVoiceData`) becomes `this.X` or inlined. The 0.7a properties (`MicrophoneBuffer`, `MicrophoneDeviceName`, `MicrophoneSampleRate`, `IsMicReady`, `MicChannels`, `MicCaptureEpoch`) become trivial accessors over local fields instead of delegations.
- [x] **Update `Start()`.** Move `MicPipeline.Start`'s mic-open logic (`Microphone.Start`, clip assignment, `IsReady` set, capture-epoch increment) into `ImitoneVoiceIntepreter.Start()` (or its existing `InitializeMicrophoneAndImitone` coroutine, whichever the merged file uses). Order: open mic → wait for `Microphone.GetPosition > 0` → mark `IsReady = true` → continue with imitone init that depends on mic. Match the existing `MicPipeline.Start` ordering exactly so behavior is preserved.
- [x] **Update `Update()`.** Call the migrated `UpdateMicReadFrame` from `ImitoneVoiceIntepreter.Update()` **first**, before any imitone work that depends on mic data. The legacy assumption ("`MicPipeline.Update` runs at −105 in Project Settings, before `ImitoneVoiceIntepreter.Update` at −104") is preserved by inlining the mic read at the top of the merged Update.
- [x] **Update `OnDisable` / `OnDestroy`.** `Microphone.End(deviceName)` now lives in `ImitoneVoiceIntepreter`'s teardown path. Move it. Mirror the existing `MicPipeline.OnDisable` ordering (set `IsReady = false` first, then `Microphone.End`).
- [x] **`MicPipeline.cs` reduced to empty stub.** All public surface and private state gone. File becomes `public class MicPipeline : MonoBehaviour { }` with a header comment ("Empty post-0.7c-ii; deleted in 0.7d. Do not add new code here."). Remove the `[DefaultExecutionOrder(-500)]` attribute. Keeping the empty stub on disk (rather than deleting now) preserves any leftover Inspector component references in `MainGame.unity` until 0.7d's deletion + scene cleanup.
- [x] **`using` statement audit.** `ImitoneVoiceIntepreter.cs` may need new `using` directives that previously lived in `MicPipeline.cs` only — common candidates: `System.Threading` (for `Monitor`/`Interlocked`), `System.Collections` (for `IEnumerator` if methods used coroutines). Add as needed; the compiler will surface any misses.
- [x] **Phase 1 FAIL OBSERVATION sanity check:** the flags in `MicVoiceIngestDebugAggregate` (`FAIL_UNREAD_ZERO_SUSTAINED`, `FAIL_INGEST_RING_STALLED`, `FAIL_INTERPRETER_NOT_CONSUMING`, `FAIL_GENTLE_RECOVERY_FIRED`, `FAIL_MONITORING_STARVATION_GROWING`, `FAIL_MIC_NOT_READY`) all read fields that now live on `ImitoneVoiceIntepreter`. The aggregate's snapshot field reads should be data-equivalent to before — i.e. the flags continue to fire on the same conditions as they did in Step 0.5 / 0.7b.

*Compile + run + click test (full 5-scenario click test from the click prevention appendix, since this is when `Microphone.Start` ownership moves) + Phase 1 FAIL OBSERVATION verification. This is the most thorough verification gate of the merge. Commit before moving on.* — **Smoke test passed (2026-05-04).**

**Commit (0.7c-ii):** `refactor: migrate mic state, methods, and Microphone.Start ownership from MicPipeline to ImitoneVoiceIntepreter` — SHA `d0c35a4c4dbfcac20ad2f4cf20741bb24d192a82` (2026-05-04).

**Developer notes for 0.7c-ii:**
- Implementation uses partial file `ImitoneVoiceIntepreter.MicIngest.cs`. `MainGame.unity`: `MicPipeline` component removed from the Imitone GameObject; mic serialized fields merged onto `ImitoneVoiceIntepreter` (`micCaptureSampleRate`, etc.). Stub `MicPipeline.cs` retained until 0.7d.
- `rg "\bmicPipeline\b" Assets/Scripts/` → zero matches.

---

**Sub-pass 0.7c-iii — Verification + audit gate:**

Goal: Final audit before 0.7d's deletion. Catch leftover references, stale `using` directives, stale Inspector serialized data, etc. No or minimal code changes.

> **Recommended LLM for this step: Opus 4.7.**
> - Verification pass — see working agreement rule 5. Always Opus 4.7 for review-shaped work.
>
> *Confirm Opus 4.7 is selected.*

*Tasks:*

- [x] **`rg` sweep on type:** `rg -n "MicPipeline" Assets/Scripts/` — matches `MicPipeline.cs` (stub), `[FormerlySerializedAs("debugInterpreterMicPipelineRefNull")]` / `…Ready` on `ImitoneVoiceIntepreter` (YAML migration strings; keep), and no other code references. *(Doc comment in `DirectVoiceMonitoring` updated to avoid the type name.)*
- [x] **`rg` sweep on private field name:** `rg -n "\bmicPipeline\b" Assets/Scripts/` → **zero** matches.
- [x] **`using` statement audit on `ImitoneVoiceIntepreter.cs`.** `System.Threading` lives on `ImitoneVoiceIntepreter.MicIngest.cs` only (partial with locks); the main partial does not need it.
- [x] **Inspector audit on `MainGame.unity`.** The Imitone GameObject carries **`ImitoneVoiceIntepreter` only** for mic ingest (no `MicPipeline` component there after 0.7c-ii); merged mic fields appear on the interpreter. *(Older plan text assumed the empty stub would still be on the GO — implementation removed it during merge.)*
- [x] **Phase 1 FAIL OBSERVATION** — smoke test passed after 0.7c-ii; full **`unread_zero` repro** remains the regression oracle before trusting Step 1+.
- [x] **Click test** — smoke passed (2026-05-04); run the **full 5-scenario appendix** before 0.7d if not already.

*This pass commits only if anything needed touching during the audit. Otherwise it's a verify-and-move-on gate.*

**Commit (0.7c-iii, optional):** `chore: post-merge audit cleanup` — only if audit found anything; otherwise skip directly to 0.7d.

**Developer notes for 0.7c-iii:**
- Removed redundant `?? ""` on `aggMicExitReason` in `MicVoiceIngestDebugAggregate` (snapshot always supplies `lastExitReason` from live ingest state).
- Recorded 0.7c-ii SHA `d0c35a4c4dbfcac20ad2f4cf20741bb24d192a82`.
- 0.7c-iii doc/audit commit: `bbf467c31b2e9aa8483361cef93a54530ababb27` (2026-05-04).

---

**Sub-pass 0.7d — Delete `MicPipeline.cs` + Project Settings cleanup:**

- [x] **Script Execution Order:** `MicPipeline` row **removed** — **verified absent** in the Editor (2026-05-04). *Execution order is not stored in versioned YAML in this repo.* `TMPro.TextMeshPro` at −105 stays.
- [x] **Stub file** had no `[DefaultExecutionOrder(-500)]` (already removed in 0.7c-ii).
- [x] Delete `Assets/Scripts/Voice/MicPipeline.cs`. **Done.**
- [x] Delete `Assets/Scripts/Voice/MicPipeline.cs.meta`. **Done.**
- [x] **`rg -n "MicPipeline" Assets/`** — expect matches only in `[FormerlySerializedAs("…MicPipeline…")]` on `ImitoneVoiceIntepreter` (YAML field migration strings). **No** `public class MicPipeline` remains.
- [x] **Open `MainGame.unity`** after pull — console should show no missing `MicPipeline` component (component was already removed in 0.7c-ii).
- [x] **Step 0 pending list** — `MicPipeline (−105)` removal marked done (see table above).

*Compile + run + full click test (all 5 scenarios from the click prevention appendix) before committing 0.7d. The architecture is now a single mic owner — Steps 1, 3, and 5 will build on this foundation.*

**Commit (0.7d):** `refactor: delete MicPipeline.cs and remove Project Settings entry` — SHA `13e44f2e2a77581cc9997bf96e8a2696f76231b3` (2026-05-04). *(Prior doc pass: `bbf467c31b2e9aa8483361cef93a54530ababb27`.)*

**Developer notes for 0.7d:**
- `MicPipeline.cs` + `.meta` removed from the repo. **Script Execution Order:** confirmed **`MicPipeline` is not present** in the list (2026-05-04) — no further action.
- `rg` cannot go to absolute zero for `MicPipeline` while `FormerlySerializedAs("debugInterpreterMicPipelineRefNull")` / `…Ready` exist — that is intentional for scene/prefab migration.

---

**Notes & considerations:**

- **Inspector references in scenes / prefabs are serialized GUID refs, not text refs.** Reassigning them in 0.7b is mechanical but easy to miss. Watch the console carefully on first scene load after each sub-pass.
- **The class-name typo (`ImitoneVoiceIntepreter`, missing the second "r") is preserved.** Don't fix it during this refactor — see the "Filename and class-name pitfall" section near the top. New code should match.
- **Don't pre-emptively delete provisional code** (gentle recovery, double-poll, etc.) during the merge. Step 5b deletes those after the audio-thread architecture is in place; pre-emptive deletion before audio-thread work risks losing the legacy fallback path within the merged file.
- **Phase 1 FAIL OBSERVATION flags should continue to surface real failures.** The whole point of the merge being behavior-preserving is that the bug we're trying to fix is still reproducible afterwards. If any Phase 1 flag *stops* firing during a known-bug session, we've introduced a regression and need to back up.
- **Watch for `using` statement drift.** Moving methods between files often requires importing namespaces in the new file. If `MicPipeline.cs` had `using System.Threading;` (for `Monitor`/`Interlocked`) or `using System.Collections;` (for `IEnumerator`), `ImitoneVoiceIntepreter.cs` may need them too.

**Test (after all four sub-passes):**

- [ ] Project compiles with no errors.
- [ ] All scenes load with no missing-script / missing-component warnings.
- [ ] `rg -n "MicPipeline" Assets/` returns only `[FormerlySerializedAs("…MicPipeline…")]` strings on `ImitoneVoiceIntepreter` (or zero if those are ever renamed). No `MicPipeline` **type** or script file.
- [ ] Voice path works end-to-end (toning, monitoring, visuals, Wwise) — **identical** subjective experience to pre-merge.
- [ ] `MicVoiceIngestDebugAggregate` shows valid values for all existing sections.
- [ ] Phase 1 FAIL OBSERVATION flags still fire on stuck spells (the bug is still reproducible after the merge — that's the proof we didn't accidentally hide it).
- [ ] No new audible clicks introduced.
- [ ] No console warnings or errors on scene load.

**Commit (overall step):** the per-sub-pass commits above are the atomic units. There is no separate "step 0.7 commit" — the four sub-passes together constitute the step.

**Developer notes:** _none_

---

### Step 1: Add audio-thread capture to `ImitoneVoiceIntepreter` (new code, not yet wired)

> **Recommended LLM for this step: Opus 4.7 (strongly).**
> - **First-pass: Opus 4.7** — first time the audio thread comes online. `OnAudioFilterRead`, ring buffer write under `Monitor.TryEnter`, `Interlocked` counters, `volatile` shared fields, no-allocation discipline. Subtle bugs introduced here cascade into Steps 3 and 5; the thinking model is worth the cost.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5)
>
> *Confirm Opus 4.7 is selected before continuing. Stay on Opus through both the first-pass and the review pass for this step.*

Add `OnAudioFilterRead` to `ImitoneVoiceIntepreter` alongside the existing main-thread mic-ingest block (the merged code from Step 0.7). Do NOT remove the legacy main-thread block yet (Step 5b deletes it after audio-thread feed is proven). Do NOT call `imitone.InputAudio` from the new path yet. Goal of this step: prove the audio thread can read mic samples and write to a ring buffer cleanly, **and that we can observe its health from the Inspector**, without breaking anything.

**Pre-condition from Step 0.7:** `ImitoneVoiceIntepreter` is now the sole mic owner (the merge is complete). It already calls `Microphone.Start` in `Start()` and holds the resulting clip in a private field (e.g. `microphoneBuffer`). Step 1 reuses that already-open clip — there is **no second `Microphone.Start` call** anywhere.

**Tasks:**

*Audio config snapshot (per V4 / V10 / V11 — capture once on main thread, used by everything downstream):*
- [x] In `Start()` (main thread), call `AudioSettings.GetConfiguration()` once and stash `audioConfigOutputSampleRate` (int), `audioConfigDspBufferSize` (int), and `audioConfigSpeakerMode` (`AudioSpeakerMode`) as fields. *(in `BootstrapAudioThreadCapturePath()`, called from `Start()` after imitone init.)*
- [x] Mark these fields effectively-immutable: read-only after `Start()` completes, or `volatile` if any path could possibly write them again. Audio thread reads them; never writes. *(private; written only inside `BootstrapAudioThreadCapturePath`.)*
- [x] Add `aggAudioConfigOutputSampleRate` and `aggAudioConfigDspBufferSize` to `MicVoiceIngestDebugAggregate` as read-only Inspector fields, so the user can confirm at a glance that mic and mixer rates match (M7) and buffer size matches expectations.
- [x] Confirm there is **no** call to `AudioSettings.GetConfiguration()` from `OnAudioFilterRead` (audio-thread Unity API call — forbidden per V3). *(Verified in review pass — single call in `BootstrapAudioThreadCapturePath` only.)*
- [x] **Defensive check (user, in Editor):** verify Project Settings → Audio → "Disable Unity Audio" is **unchecked**. If checked, `OnAudioFilterRead` will never fire and the audio-thread architecture is dead in the water. (At runtime, the symptom is `aggAudioCallbackTotal` permanently at 0; this would also trip `FAIL_AUDIO_CALLBACK_FROZEN`. But it's much cheaper to verify the setting once than to debug this from the symptom side.) *(Verified by user during Step 1 testing, 2026-05-04.)*

*Capture-path code (per V3 / V5 — canonical pattern):*
- [x] **Verify the `Imitone` GameObject does NOT already have an AudioSource (user, in Editor scene `MainGame.unity`).** If one exists (e.g., a leftover from previous experimentation), audit what it does. The plan requires exactly **one** AudioSource on the `Imitone` GameObject — the new dedicated capture AudioSource configured below. Multiple AudioSources reintroduce `OnAudioFilterRead` chain-routing ambiguity (see V5 / M3). *(Code defensively logs a warning if `>1` AudioSource is found at bootstrap; verify visually anyway.)* *(Verified by user 2026-05-04: 0 AudioSource components in edit mode, exactly 1 added at runtime by `EnsureCaptureAudioSourceConfigured()` with the correct V5 config — clip = `Microphone`, Mute off, Bypass Effects / Listener Effects / Reverb Zones on, Play On Awake off, Loop on, Volume 0, Spatial Blend 0.)*
- [x] **Verify the `DirectVoiceMonitoring` GameObject has exactly one AudioSource (user, in Editor scene `MainGame.unity`)** (the existing monitoring source). Same reason as above. *(Verified by user 2026-05-04.)*
- [x] On the `Imitone` GameObject, add the dedicated capture AudioSource (in code: `captureSource = gameObject.AddComponent<AudioSource>()` if not present) and configure per V5: `loop = true`, `volume = 0f` (NOT `mute = true` — see V5 critical gotcha / M8), `bypassEffects = true`, `bypassListenerEffects = true`, `bypassReverbZones = true`, `spatialBlend = 0f`, `playOnAwake = false`. *(in `EnsureCaptureAudioSourceConfigured()`; also explicitly sets `mute = false`.)*
- [x] **No new `Microphone.Start` call.** Step 0.7 made `ImitoneVoiceIntepreter` the mic owner. The mic is already open and the clip is already held in a private field (`microphoneBuffer` or whatever name was chosen during 0.7c). Step 1 just consumes it. *(`rg "Microphone.Start" Assets/Scripts/Voice/` → single match in `MicIngest.cs:1097`.)*
- [x] After the existing `Microphone.Start` succeeds in `Start()` (the call migrated in 0.7c), assign `captureSource.clip = microphoneBuffer` and capture `aggMicClipChannels = microphoneBuffer.channels` once. *(channels cached in `BootstrapAudioThreadCapturePath`; clip assigned in `WaitMicPositionThenPlayCapture` coroutine after readiness gate.)*
- [x] Wait for `Microphone.GetPosition(microphoneDeviceName) > 0` before calling `captureSource.Play()`, so the source doesn't begin on a silent ring. The existing main-thread block already does this kind of check for `IsReady` purposes; sequence the new `captureSource.Play()` after `IsReady` becomes true so they share the readiness gate.
- [x] Add a new ring buffer dedicated to the audio-thread path. **Do not** modify the legacy main-thread ring buffer (the one migrated in 0.7c) yet — the legacy block still uses it. The two rings will coexist for Steps 1-4 inside the same merged file. Step 3 (or Step 5b) consolidates them. Pre-allocate the new ring at ≥ 1 second worth of samples to absorb read-side starvation gracefully. *(`audioThreadRing`, sized at `max(outputSampleRate*2, micCaptureSampleRate*2, 8192)` — 2 s minimum.)*
- [x] Pre-allocate `monoScratch` (float[]) in `Start()` to a size of at least `audioConfigDspBufferSize` (per V11). No allocations on the audio thread. *(sized `max(dspBufferSize*2, 8192)`.)*
- [x] Add a serialized priming-window field: `[SerializeField] private int audioCallbackPrimingFramesToSkip = 8;` and a runtime counter `audioCallbackPrimingFramesRemaining` initialized to it (per M9). *(Step 1 maintains the counter only; the imitone-feed gate that consumes it lands in Step 3.)*
- [x] Implement `OnAudioFilterRead(float[] data, int channels)`:
  - [x] Compute `int frames = data.Length / channels;`.
  - [x] **Do not** call `Microphone.*` or `microphoneBuffer.GetData(...)` from this method (V3 firm rule). *(Verified in review.)*
  - [x] Downmix to mono per V11 into `monoScratch`: `monoScratch[i] = sum(data[i*channels + c] for c in 0..channels) / channels`. (For our mono USB mic going through a stereo mixer, this exactly recovers the original signal; for any other mic it produces a defensive sum-mono fallback.) *(plus mono fast-path via `Array.Copy`.)*
  - [x] Set `aggMixerChannels = channels;` (volatile int).
  - [x] Acquire write lock with `Monitor.TryEnter(ringWriteLock, 0)`; on miss, `Interlocked.Increment(ref audioCallbackLockMissTotal)` and bail this callback (do not block). Use `try / finally` to release.
  - [x] Inside the lock: copy `monoScratch[0..frames]` into the new audio-thread ring buffer.
  - [x] `Interlocked.Add(ref audioRingWriteTotalSamples, frames)` (the canonical position counter). *(Single counter — `audioRingWriteTotalSamples` covers both roles the plan originally split into `audioRingWriteTotalSamples` + `_samplesWritten`.)*
  - [x] `Interlocked.Increment(ref audioCallbackTotal)` and `Interlocked.Add(ref audioCallbackSamplesProcessedTotal, frames)`.
  - [x] Decrement `audioCallbackPrimingFramesRemaining` while it is > 0.
  - [x] Optionally `System.Array.Clear(data, 0, data.Length)` so this AudioSource doesn't double-output mic to the speaker bus. *(Always cleared.)*
- [x] ~~Add a temporary rate-limited `Debug.Log` inside the callback to confirm cadence during local testing.~~ **Skipped** — Phase 2 telemetry (`aggAudioCallbackTotal` / `aggAudioCallbackHzRolling` in the Inspector) is the cadence-confirmation surface; a temporary log would be redundant and risk lingering in code.
- [x] Confirm the existing **legacy main-thread mic-ingest block** (migrated in 0.7c — `UpdateMicReadFrame`, gentle recovery, double-poll, etc., now living inside `ImitoneVoiceIntepreter`) is untouched and continues to drive imitone. Steps 1–4 run **two paths inside one file**: the legacy main-thread block (still feeding imitone) and the new audio-thread block (just observing for now). Step 3 switches the imitone feed; Step 5b deletes the legacy block. *(Verified by `git diff HEAD~2 HEAD -- ImitoneVoiceIntepreter.cs ImitoneVoiceIntepreter.MicIngest.cs` — only 3 inserts: `AudioThreadHealthSnapshot` struct, `BootstrapAudioThreadCapturePath()` call in `Start()`, `UpdateAudioThreadHealthOnMainThread()` call in `Update()`, and `StopAudioThreadCapture()` line in `OnDisable()`.)*

*Telemetry — add the new "Audio thread health" section to `MicVoiceIngestDebugAggregate`:*

| Field (on the new audio-thread owner; aggregate copies to `agg*`) | Type | Updated by | Inspector meaning |
|----|----|----|----|
| `audioCallbackTotal` → `aggAudioCallbackTotal` | `long` (use `Interlocked.Increment`) | audio thread | Monotonic count of `OnAudioFilterRead` invocations. Should climb visibly while scene plays. |
| `audioCallbackSamplesProcessedTotal` → `aggAudioCallbackSamplesProcessedTotal` | `long` (use `Interlocked.Add`) | audio thread | Sum of `data.Length / channels` per callback. Climbs at the project's audio sample rate (~48000 / sec). |
| `audioCallbackLastSamplesPerCallback` → `aggAudioCallbackLastSamplesPerCallback` | `int` (volatile) | audio thread | Most recent `data.Length / channels`. Should be a stable value (e.g. 1024). |
| `audioCallbackHzRolling` → `aggAudioCallbackHzRolling` | `float` (volatile) | computed on main thread from totals + `Time.unscaledTime` | Rolling-average callback rate over the last second. Expected near `sampleRate / samplesPerCallback`. |
| `audioCallbackMaxGapMsLastSecond` → `aggAudioCallbackMaxGapMsLastSecond` | `float` (volatile) | computed on main thread from per-callback timestamps | Largest interval between any two consecutive callbacks observed in the last second. Should hover near nominal buffer time (~21 ms at 1024 / 48k). |
| `audioCallbackLockMissTotal` → `aggAudioCallbackLockMissTotal` | `long` (use `Interlocked.Increment`) | audio thread | Count of times the audio-thread `Monitor.TryEnter(ringWriteLock, 0)` failed. Should be near zero. |
| `audioCallbackGCAllocSuspectTotal` → `aggAudioCallbackGCAllocSuspectTotal` | `long` | audio thread + main-thread heuristic | Best-effort: counts callbacks whose duration anomalously spiked beyond a threshold (a proxy for hidden allocations). Real verification still happens in the Profiler. |
| `audioRingWriteTotalSamples` → `aggAudioRingWriteTotalSamples` | `long` | audio thread | New ring write total (separate ring in this step; consolidates with existing in Step 3 or Step 5b). |
| `audioRingWriteLastClipReadStart` / `audioRingWriteLastClipReadCount` | `int` (volatile) | audio thread | Last position read from the mic clip and how many samples this callback. For sanity. |
| `aggMicClipChannels` | `int` | set once in `Start()` (main thread) | Channel count of the mic clip Unity gave us. Typically 1 for a USB mic. Informational. |
| `aggMixerChannels` | `int` (volatile) | audio thread | Channel count of the `data[]` Unity hands the callback. Typically 2 on Windows desktop. If it differs from `aggMicClipChannels`, Unity's audio graph is upmixing as expected. |

*Telemetry — keep the existing aggregate fields too:* the old `unread_zero` / ring-write / interpreter-try-copy fields stay alive in this step because the parallel `MicPipeline` is still running. We will retire them in Step 6.

*Telemetry — wire the new audio-thread counters into the aggregate:*
- [x] Add an `[Header("Audio thread health (new path)")]` block in `MicVoiceIngestDebugAggregate` and add the matching `agg*` fields from the table above. *(Header reads "Audio thread health (Step 1 — parallel path)".)*
- [x] In `LateUpdate`, copy each audio-thread counter into its `agg*` mirror (atomic reads on the long counters via `Interlocked.Read` if needed). *(via `interpreter.GetAudioThreadHealthSnapshot()`, which uses `Interlocked.Read` internally for all longs.)*
- [x] Compute `aggAudioCallbackHzRolling` on the main thread from `audioCallbackTotal` deltas vs. `Time.unscaledTime`. *(`UpdateAudioThreadHealthOnMainThread`, 1 s sliding window — fixed in review-pass follow-up commit to be allocation-free.)*
- [x] Compute `aggAudioCallbackMaxGapMsLastSecond` on the main thread from per-callback timestamp samples (record at most one timestamp per callback to keep the audio thread cheap; main thread reads & windows them). *(Per-callback `Stopwatch.GetTimestamp()` deltas pushed via lock-free CAS-max into a cross-thread accumulator; main thread drains via `Interlocked.Exchange` once per 1 s window. Fixed in review-pass follow-up — initial implementation sampled gaps from the main thread, which bounded the metric to the frame interval.)*
- [x] Leave the existing aggregate sections (mic ingest, interpreter raw path, monitoring transport) intact for now — they are still backed by the `MicPipeline` parallel path. *(Verified.)*

*Extend the FAIL OBSERVATION block (Phase 2 — audio-thread failure modes):*

- [x] Add the new Phase 2 `FAIL_*` fields **above** the existing Phase 1 flags (sort order: new audio-thread flags first, since they describe the new code under test). Use the snippet below.
- [x] Add the matching threshold fields to the `[Header("FAIL OBSERVATION — thresholds")]` section.
- [x] Implement the trigger conditions in `LateUpdate` (table below). *(Includes a startup-grace window; GC sticky baseline is seeded at the grace boundary so cold-start JIT/warm-up spikes don't latch the flag.)*
- [x] Update the `FAILURE = ...` OR expression to include the new flags.

Add the following subsidiary flags:

```csharp
[SerializeField] private bool FAIL_AUDIO_CALLBACK_FROZEN;
[SerializeField] private bool FAIL_AUDIO_CALLBACK_RATE_LOW;
[SerializeField] private bool FAIL_AUDIO_CALLBACK_GAP_HIGH;
[SerializeField] private bool FAIL_AUDIO_LOCK_CONTENTION;
[SerializeField] private bool FAIL_AUDIO_GC_ALLOC_DETECTED;
```

Add corresponding thresholds to the `[Header("FAIL OBSERVATION — thresholds")]` section:

```csharp
[SerializeField] private float failAudioCallbackFrozenSeconds = 0.2f;
[SerializeField] private float failAudioCallbackRateLowFraction = 0.75f;   // < 75% of expected
[SerializeField] private float failAudioCallbackGapHighMultiplier = 2.0f;  // > 2× nominal
[SerializeField] private long failAudioLockMissPerSecondThreshold = 50;
```

Triggers:

| Flag | Triggers when |
|------|---------------|
| `FAIL_AUDIO_CALLBACK_FROZEN` | `aggAudioCallbackTotal` has not advanced for >= `failAudioCallbackFrozenSeconds` wall-clock seconds. |
| `FAIL_AUDIO_CALLBACK_RATE_LOW` | `aggAudioCallbackHzRolling` < `failAudioCallbackRateLowFraction × expectedHz` for at least 1 second (`expectedHz` derived from `AudioSettings.GetConfiguration()` — capture once at start). |
| `FAIL_AUDIO_CALLBACK_GAP_HIGH` | `aggAudioCallbackMaxGapMsLastSecond` > `failAudioCallbackGapHighMultiplier × nominalGapMs`. |
| `FAIL_AUDIO_LOCK_CONTENTION` | `aggAudioCallbackLockMissTotal` increment rate > `failAudioLockMissPerSecondThreshold` over the last second. |
| `FAIL_AUDIO_GC_ALLOC_DETECTED` | `aggAudioCallbackGCAllocSuspectTotal > 0` (sticky; clearable via the same sticky-flag clearing mechanism added in Step 0.5). |

These flags are observable from the moment Step 1's parallel audio-thread path comes online, even though imitone is still being fed from the main thread. They will catch any structural problem with the new capture path before it becomes the primary one in Step 3.

**Notes & considerations (rules and guardrails — read before coding):**
- **No `Microphone.*` or `AudioClip.GetData` calls on the audio thread, ever.** Position is tracked via `_samplesWritten += data.Length / channels`. Mic samples come from the `data[]` parameter. (V3 — firm rule, not negotiable.)
- **No allocations inside `OnAudioFilterRead`.** Pre-allocate every buffer in `Start()` (ring, scratch downmix buffer, anything else). Verify with the Unity Profiler's GC alloc column on the audio thread.
- **No Unity APIs other than carefully verified ones inside the callback.** No `Time.*` (use a local `System.Diagnostics.Stopwatch` plus `Interlocked` to record per-callback ticks if you need wall time), no `Debug.Log` in production, no `Microphone.IsRecording`, no `Transform` / `GameObject` API.
- **Counters are atomic.** Every audio-thread counter uses `Interlocked.Increment` / `Interlocked.Add`. Every audio-thread float / int that the main thread reads is `volatile`. No exceptions.
- **AudioSource must be set up correctly.** If `OnAudioFilterRead` doesn't fire after first attempt, check: AudioSource exists, has the mic clip assigned, is `Play()`ing, is **not** muted (use `volume = 0f`, never `mute = true` — see V5 critical gotcha / M8), is on the same GameObject as the `MonoBehaviour` declaring `OnAudioFilterRead`.
- **Verify exact Unity API names against the 2022.3 ScriptReference** at implementation time. The follow-up research that informed V3 / V5 / V10 was assembled without live web access; engineering conclusions are robust but specific property names (e.g. `bypassListenerEffects` vs `bypassListener`) should be confirmed before pasting code.

**Test:**
- [x] `aggAudioCallbackTotal` climbs steadily while the scene plays. *(Observed 3493 → climbing.)*
- [x] `aggAudioCallbackHzRolling` stabilizes near expected (e.g. 46.9 Hz at 1024 / 48 k, or 187.5 Hz at 256 / 48 k). *(Observed 46.957 vs 46.875 nominal — within 0.18 %.)*
- [x] `aggAudioCallbackMaxGapMsLastSecond` stays near nominal (~21 ms at 1024 / 48 k). *(Observed up to 41.5 ms — just under the 42.6 ms FAIL threshold; `FAIL_AUDIO_CALLBACK_GAP_HIGH` did not trip. See Developer notes below for root-cause analysis.)*
- [x] `aggAudioCallbackLastSamplesPerCallback` is a stable value matching the configured DSP buffer size. *(Observed 1024.)*
- [x] `aggAudioRingWriteTotalSamples` climbs at ~ sample rate (~48000 / sec). *(Observed 3,576,832 — exactly matches `aggAudioCallbackSamplesProcessedTotal`, confirming every callback wrote the ring.)*
- [x] `aggAudioCallbackLockMissTotal` stays near zero. *(Observed 0.)*
- [x] `aggMicClipChannels` and `aggMixerChannels` populate sensibly (typically `1` and `2` on Windows desktop). *(Observed 1 and 2 — V11 upmix happening as predicted.)*
- [x] Existing voice / imitone path still works as before (toning still triggers visuals, etc.). *(Subjectively identical to pre-Step-1.)*
- [x] Profiler: no GC allocations attributed to the audio thread. *(Verified — `OnAudioFilterRead` shows 0 B GC alloc on `Audio Record Thread`. `aggAudioCallbackGCAllocSuspectTotal = 0`. The 2.7 KB/frame on `ImitoneVoiceIntepreter.Update()` is **main thread**, not audio thread, and is pre-existing — see Developer notes.)*
- [x] FAIL OBSERVATION: Phase 2 flags surface real failures of the new path; Phase 1 flags continue to surface real failures of the legacy path. Both kinds should be observable independently. *(Phase 2 stayed false throughout. Phase 1 + `FAILURE` continue to fire on the legacy path during normal toning exactly as they did before Step 1 — this is the regression oracle: the bug we're migrating to fix is still reproducible. The original "`FAILURE` stays false during normal operation" wording in this checkbox was overly optimistic; the legacy path's bug pre-dates Step 1 and is exactly what Step 1 is structurally not allowed to fix.)*

**Commit:** `feat: add audio-thread mic capture path with health telemetry (parallel, not yet wired)` — SHA `3c04ecf1` (2026-05-04). *(Plus follow-up commits: `4e0b97ec` review-pass fixes — per-callback gap timestamps + GC baseline at grace boundary; `068ce1fd` `Debug` ambiguity fix + plan-doc box checkmarks.)*

**Developer notes:**

- **Known limitation surfaced during Step 1 review pass — must be addressed at the start of Step 3.** Mic recovery paths in `ImitoneVoiceIntepreter.MicIngest.cs` (`PerformGentleUnreadZeroCaptureRestart`, the device-unavailable / invalid-position / stalled-write-head exits in `UpdateMicReadFrame`, and the `MicIngestMainThreadTick` recovery branch) call `StopMicrophoneCapture()` and `InitializeMicrophone()` but do NOT call `StopAudioThreadCapture()` / `BootstrapAudioThreadCapturePath()`. After a recovery, `captureSource.clip` still references the previous (now-ended) `AudioClip`, so the audio-thread ring stops receiving samples until the next scene reload. **For Step 1 (parallel, not yet wired) this only stalls the new Phase 2 telemetry after a recovery — legacy main-thread ingest still drives imitone, so voice continues to work.** **For Step 3 (when imitone moves to the audio thread) this becomes a hard regression: a single mic recovery would silently kill voice input.** Fix at the top of Step 3: tear down and rebootstrap the audio capture path on every successful `InitializeMicrophone()` retry, not just at startup.

- **Pre-existing main-thread GC pressure surfaced during Step 1 Profiler verification — should be confronted in Step 3 / Step 4.** The Profiler reports **~2.7 KB / frame GC alloc** attributed to `ImitoneVoiceIntepreter.Update()`. This is **not introduced by Step 1** (`UpdateAudioThreadHealthOnMainThread()` and `BootstrapAudioThreadCapturePath()` are both allocation-free per-frame); the almost-certain culprit is `GetRawVoiceData()` calling `imitone.GetState()` (returns a heap-allocated JSON string) and `JsonUtility.FromJson` (further allocations). At 60 fps that's ~162 KB/s, sufficient to trigger Gen-0 GC roughly once per second. **This is almost certainly the source of the once-per-second `aggAudioCallbackMaxGapMsLastSecond ≈ 41 ms` spikes** (one doubled gap per second; 41.5 ms ≈ 2 × 21.3 ms nominal) — main-thread GC pauses can briefly deschedule the audio thread. Migration impact: Step 3 deletes the chunking block (per-frame `new float[chunkSize]`) and Step 5b deletes the legacy main-thread ingest block, both of which should reduce main-thread allocations. The imitone-JSON allocation is a separate concern that Step 4 ("verify and tune") should explicitly address: candidate solutions include caching a reusable string buffer if the imitone API allows, parsing without `JsonUtility` (manual parser into pre-allocated structs), or reducing `GetState()` call frequency below per-frame.

- **Step 1 Editor verification session — gate values for future regression comparison.** Captured 2026-05-04 in Editor at default 1024-sample / 48 kHz buffer: `aggAudioCallbackTotal` climbing at 46.957 Hz (vs 46.875 nominal), `aggAudioCallbackMaxGapMsLastSecond` 41.5 ms peak, `aggAudioCallbackLockMissTotal` 0, `aggAudioCallbackGCAllocSuspectTotal` 0, `aggMicClipChannels` 1, `aggMixerChannels` 2, `aggAudioConfigOutputSampleRate` 48000, `aggAudioConfigDspBufferSize` 1024. `Audio.Thread (4.67 ms)` per Unity frame on the `Audio Record Thread` (this includes Wwise + Unity mixer alongside our `OnAudioFilterRead`; our callback's contribution is small and allocation-free). Subjective voice/toning experience identical to pre-Step-1. Phase 1 / `FAILURE` continue to fire on the legacy path during normal toning (regression oracle confirmed).

---

### Step 2: Confirm imitone is safe to feed from audio thread (stress test)

> **Scaffolding commit:** `feat(step2): add imitone audio-thread stress test scaffolding` — SHA `a8a36abe` (2026-05-04).
>
> **Follow-up commits (same branch, methodology fixes — apply after scaffold):**
> - `fix(step2): exclusive audio-thread InputAudio — concurrent feed crashed Unity` — SHA **`4bbfb643`**. First scaffold called `InputAudio` from **both** main-thread chunking and `OnAudioFilterRead`; **immediate Editor crash** — `imitone.cs` uses one `feed_buffer` per `ImitoneVoice`; concurrent `InputAudio` races native analyze. **Correct stress pattern:** while stress is active for the session, **skip main-thread `InputAudio`**; **only** audio thread calls `InputAudio`; main still calls `GetState()` (matches Step 3).
> - `fix(step2): freeze stress session at Start; mute Phase 2 GC FAIL during stress` — SHA **`95e201b5`**. Checkbox snapshot at **`Start()`** → `step2StressTestSessionActive` (mid-Play toggles ignored). Intentional per-callback `new float[frames]` in stress path + longer callbacks tripped Phase 2 **`FAIL_AUDIO_GC_ALLOC_DETECTED`** on calm baseline — false positive. While stress session: do not increment `audioCallbackGCAllocSuspectTotal` from **duration** heuristic; **`FAIL_AUDIO_GC_ALLOC_DETECTED` forced false** in aggregate. Restore full Phase 2 GC behavior when stress code is removed.
> - `docs(step2): link scaffolding commit SHA in plan` — SHA **`de1e690a`**.
> - `chore(step2): remove imitone audio-thread stress scaffolding after verified pass` — SHA **`1fc84a4b`** (2026-05-04). Step 2 stress test passed all six conditions with zero stress failure totals; restored pre-Step-2 main-thread chunking `InputAudio` + single `GetState()`. All `step2StressTestSessionActive`, `enableAudioThreadImitoneFeedStressTest`, four stress counters + getters, four `aggStress*` mirrors, audio-thread stress `InputAudio` block, and `Step2StressTestSessionActive` branch in `MicVoiceIngestDebugAggregate` removed. Phase 2 `FAIL_AUDIO_GC_ALLOC_DETECTED` returns to single sticky-latch path.
>
> **`step2StressTestSessionActive`** mirrors the checkbox **each `Update()`** (and from `Start()` for callbacks before the first `Update`); **`volatile`** for audio-thread reads. **Prefer enabling before Play** for a clean first frame; you can also enable during Play. When stress is **on**: **main-thread chunking `InputAudio` is skipped**; **only** `OnAudioFilterRead` calls `InputAudio`; main thread still calls `GetState()` each frame. Watch the four `aggStress*` fields on `MicVoiceIngestDebugAggregate`, run the six stress conditions below. **Ignore pitch / tone quality.** Phase 2 **`FAIL_AUDIO_GC_ALLOC_DETECTED` is disabled** during a stress session (intentional per-callback `new float[frames]` trips the GC-suspect heuristic — not meaningful for pass/fail). **`FAILURE` may still be true** during stress if **Phase 1** legacy-ingest flags fire — that regression oracle is unrelated to Step 2 pass/fail; judge Step 2 only on the four `aggStress*` failure totals + crash-free / stable Editor. When all pass, remove stress code in a follow-up commit per the plan.

> **Recommended LLM for this step:**
> - **First-pass: Composer 2 (full)** — boilerplate stress-test scaffolding (counters, toggle, six test conditions, `try/catch` wraps, cleanup). Mostly mechanical; no new architectural reasoning required.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5)
>
> *Confirm the right model is selected before continuing. Switch to Opus 4.7 when the first-pass is complete and the mandatory review pass begins.*

Before redirecting the entire imitone feed, run an explicit stress test to confirm `InputAudio` is genuinely safe from the audio thread under our specific conditions, and that `InputAudio` (audio thread) and `GetState` (main thread) co-existing on the same imitone DLL instance does not corrupt or crash. The imitone docs explicitly mark `InputAudio` as cross-thread safe (`imitone.cs` line 77) but say nothing about `GetState`, so we verify empirically before relying on it.

**Tasks:**

*Temporary stress-test code (Step 2 — **removed from codebase** after manual pass, 2026-05-04; checklist below is historical record):*
- [x] Add a temporary `[SerializeField] private bool enableAudioThreadImitoneFeedStressTest;` to `ImitoneVoiceIntepreter`. Mark every added line in this step with a comment like `// STRESS TEST (Step 2) — REMOVE IN SAME COMMIT`.
- [x] When stress flag is true: **`OnAudioFilterRead` is the sole `imitone.InputAudio` source** — main-thread chunking `InputAudio` is skipped. (Original plan suggested double-feed; **concurrent `InputAudio` from two threads races imitone's native `feed_buffer` and crashed Unity on toggle** — see Developer notes.) Analysis output remains unreliable while the flag is on.
- [x] When true, in `Update` (main thread): keep the existing per-frame `imitone.GetState()` call at its natural rate. No artificial loop — we want realistic main-thread cadence, not synthetic load.
- [x] Add temporary stress-test counters:
  - `stressAudioThreadInputAudioCallTotal` (long, `Interlocked.Increment` from audio thread).
  - `stressMainThreadGetStateCallTotal` (long, `Interlocked.Increment` from main thread).
  - `stressAudioThreadInputAudioFailureTotal` (long) — increments if `InputAudio` throws (wrap in `try / catch`, log once, count).
  - `stressMainThreadGetStateFailureTotal` (long) — same pattern around `GetState`.
- [x] Surface the four stress counters in `MicVoiceIngestDebugAggregate` as read-only `agg*` mirrors so the user can watch them live during the test session.
- [x] **Stress flag source:** `step2StressTestSessionActive` mirrors `enableAudioThreadImitoneFeedStressTest` each **`Update()`** (and in `Start()` for pre-first-Update callbacks); field is **`volatile`** for audio-thread reads. Prefer enabling before Play for a clean first frame; turning off mid-Play returns main-thread `InputAudio` — use caution. (Original plan used `Start()`-only snapshot + `95e201b5` GC mute; live sync added after Editor observation.)
- [x] **Phase 2 `FAIL_AUDIO_GC_ALLOC_DETECTED` during stress:** the stress path allocates `new float[frames]` per callback (required: `imitone.InputAudio` uses `audio.Length` as sample count). That trips the callback-duration GC-suspect heuristic and latched `FAIL_AUDIO_GC_ALLOC_DETECTED` on calm baseline — **false positive**. While `Step2StressTestSessionActive`, skip duration-based increments to `audioCallbackGCAllocSuspectTotal` and **force** `FAIL_AUDIO_GC_ALLOC_DETECTED` false in the aggregate. (Commit `95e201b5`.)
- [x] **Exclusive `InputAudio` (commit `4bbfb643`):** do **not** call `InputAudio` from main thread and audio thread concurrently — **instant crash**; see Developer notes.
- [x] **Calm baseline:** scene running, no toning, no input. Counters increment as expected; no failures.
- [x] **Steady toning:** sustained tone, calm volume. No failures.
- [x] **Loud toning:** sustained loud tone, varied pitch. No failures.
- [x] **Rapid onset / offset:** fast voice on/off cycles (~2 Hz). Stresses imitone state transitions. No failures.
- [x] **Silence after toning:** stop abruptly. Watch for crashes during the silent decay.
- [x] **Long-haul:** 5+ minutes of mixed activity. Failure counters stay at zero throughout.

*Pass criteria — all must hold:*
- [x] `stressAudioThreadInputAudioFailureTotal == 0` across all six conditions. *(Verified 2026-05-04.)*
- [x] `stressMainThreadGetStateFailureTotal == 0` across all six conditions. *(Verified 2026-05-04.)*
- [x] No Unity console exceptions tagged "imitone" or thrown from imitone-adjacent code. *(Verified 2026-05-04.)*
- [x] No editor freezes or audio dropouts attributable to the test. *(Verified 2026-05-04.)*
- [x] `aggAudioCallbackTotal` continues climbing throughout (the test path doesn't starve the audio thread). *(Verified 2026-05-04.)*
- [x] The Editor process does not crash. *(Verified 2026-05-04.)*
- **Note:** Step 2 pass/fail is **not** "`FAILURE` stays false" — Phase 1 flags may still fire during stress (legacy ingest bug). **Note:** Other subsystems (e.g. Wwise "Voice Starvation", missing InteractiveMusic switch defaults) may log errors during Play — triage separately unless clearly caused by enabling stress mode.

*Cleanup (completed 2026-05-04 — stress scaffolding removed; proceed to Step 3):*
- [x] Remove the stress-test serialized toggle, **`step2StressTestSessionActive`** field + **`Start()` snapshot line**, all four stress counters + public getters, the audio-thread `InputAudio` stress block, the main-thread **`if (!step2StressTestSessionActive)`** guard around chunking (restore unconditional chunking as today when stress removed), **`Step2StressTestSessionActive`** branches in **`MicVoiceIngestDebugAggregate`** (restore single GC latch path), and the `try / catch` wraps if they were added only for the test.
- [x] Remove the corresponding `agg*` mirrors from `MicVoiceIngestDebugAggregate`.
- [x] Confirm no `// STRESS TEST` comments remain.
- [x] Confirm no commented-out stress-test code remains. Nothing left behind.

**Notes & considerations:**
- Output will be wrong while the flag is on — expected. We're testing native stability (`InputAudio` on audio thread + `GetState` on main), not tone tracking.
- **If the stress test fails, stop** before Step 3. Plan B: queue buffers audio thread → main and call `InputAudio` only on main (only if even the exclusive audio-thread feed + main `GetState` pattern proves unsafe).
- The removal commit's message is below; the merged repo must end with **zero** stress-test code.

**Commit:** `test: confirm imitone safe from audio thread under stress, revert in same commit`

**Developer notes:**

- **2026-05-04 — Unity crashed immediately on enabling the Step 2 checkbox.** First implementation called `imitone.InputAudio` from **both** the main-thread chunking loop and `OnAudioFilterRead` **concurrently**. The managed wrapper (`imitone.cs`) uses a **single** `feed_buffer` and copies `audio[]` into it before every native `imi_AnalyzeF32` — interleaved `InputAudio` from two threads races that buffer. The doc line *"can be called from a different thread"* means you may **relocate** `InputAudio` to one non-main thread, **not** that two threads may call `InputAudio` on the same `ImitoneVoice` at the same time. **Fix:** when the stress flag is on, **skip** the main-thread `InputAudio` loop; only the audio thread calls `InputAudio`. Main thread still calls `GetState()` — the pattern Step 3 targets.

- **2026-05-04 — `FAIL_AUDIO_GC_ALLOC_DETECTED` during calm baseline Step 2.** The stress path uses `new float[frames]` each audio callback (required because `imitone.InputAudio` uses `audio.Length` as sample count). That alloc + longer callbacks trips the Phase 2 GC-suspect heuristic (`audioCallbackGcSuspectMsThreshold`) and latches the sticky FAIL flag — **not** a Step 3 readiness signal. **Fix:** freeze stress mode at session `Start()` (`step2StressTestSessionActive`); ignore mid-Play checkbox toggles; while stress session is active, **do not** increment `audioCallbackGCAllocSuspectTotal` from callback-duration heuristic, and **`FAIL_AUDIO_GC_ALLOC_DETECTED` is forced false** in `MicVoiceIngestDebugAggregate`. Re-enable Phase 2 GC observation after stress scaffolding is removed. **(Stress scaffolding and aggregate GC exception removed from codebase after Step 2 sign-off, 2026-05-04.)**

---

### Step 3: Move imitone feeding and DSP to the audio thread

> **Recommended LLM for this step: Opus 4.7 (strongly).**
> - **First-pass: Opus 4.7** — most threading-sensitive step in the plan. DSP (HPF/LPF) migration to audio thread without resetting state, `_dbMicrophone` cross-thread atomicity, tear-detection logic, deletion of the chunking block. A missed `Interlocked` or a wrongly-timed filter-state init creates Heisenbugs that hide for hours.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5)
>
> *Confirm Opus 4.7 is selected before continuing. Stay on Opus through both the first-pass and the review pass for **each** sub-step (3a and 3b are committed independently).*

> **Split into two sub-passes (3a + 3b)** so failure modes bisect cleanly. 3a moves the imitone feed and deletes the chunking block; 3b moves DSP + `_dbMicrophone`. 3a's verification is "is the audio-thread feed alive?" with telemetry. 3b's is "are filters click-free and is `_dbMicrophone` torn?" with the click protocol + tear detection. If a regression appears, the bisection between 3a and 3b is unambiguous.
>
> A small **prep fix** (mic-recovery rebootstrap) lands at the top of 3a so 3a/3b regressions can't be confused with a stale audio-thread capture coroutine after a recovery.

Now redirect imitone's input from main thread to audio thread. This is the core change. Filter state and `_dbMicrophone` move with it.

#### Step 3 prep: mic-recovery rebootstrap (lands with 3a)

The Step 1 audio-thread capture path is started once in `Start()` via `BootstrapAudioThreadCapturePath`. Mic-recovery (legacy ingest restart) currently does **not** call `StopAudioThreadCapture` / `BootstrapAudioThreadCapturePath` — so after a recovery, audio-thread telemetry dies and (after 3a) imitone stops getting fed. Land this fix first; without it, 3a regressions look like "audio-thread feed is broken" when they're really "the audio-thread capture coroutine was never restarted." (See Step 1 Developer notes for the original observation.)

- [x] In whatever code path triggers mic recovery (legacy ingest restart), call `StopAudioThreadCapture()` first, then `BootstrapAudioThreadCapturePath()` after the new microphone is `IsRecording`. *Implementation: detect via `captureEpoch` tick rather than per-call-site instrumentation — `TryRebootstrapAudioThreadCaptureIfMicRecovered()` runs at the end of `MicIngestMainThreadTick` and rebootstraps when `captureEpoch != audioThreadLastBootstrappedCaptureEpoch`. Catches both gentle restart (inside `EnsureFrameUpdated → UpdateMicReadFrame`) and scheduled recovery (via `InitializeMicrophone`) through one chokepoint.*
- [ ] Verify `aggAudioCallbackTotal` resumes climbing after a forced recovery (e.g. unplug / replug or programmatic restart).

---

#### Step 3a: Imitone feed migration + chunking deletion + feed observability

> **Goal of this pass:** make `OnAudioFilterRead` the **sole** `imitone.InputAudio` source, delete the now-dead chunking block, and add the telemetry / FAIL flags that prove the audio-thread feed is alive. **DSP filtering and `_dbMicrophone` stay on the main thread for now**; they move in 3b.

**Tasks:**

*Imitone feed:*
- [x] In `OnAudioFilterRead`: after the ring write, call `imitone.InputAudio(monoScratch)` with the same mono buffer just written to the ring. (3a feeds the **unfiltered** mono signal — DSP is still main-thread; 3b moves the filtering ahead of this call.) *Implementation: copy `frames` samples from `monoScratch` into a reusable `imitoneFeedBuffer` of exactly `frames` length before the call. Required because `imitone.InputAudio` uses `audio.Length` as the sample count, and `monoScratch` is sized to `scratchFrames >= frames`.*
- [x] Gate the feed on the priming window: do **not** call `imitone.InputAudio` while `audioCallbackPrimingFramesRemaining > 0`. (Continue ring-writing during the priming window — only the imitone feed is gated. FMOD's record buffer often delivers silence or garbage in the first 4–8 callbacks; feeding that to imitone teaches the analyzer to lock onto silence and pollutes noise-floor calibration.) *Implementation: snapshot `stillPrimingThisCallback = audioCallbackPrimingFramesRemaining > 0` BEFORE the decrement so the very last priming callback (remaining: 1 → 0) also gets skipped, matching the spec's intent of "skip 8" rather than "skip 7."*
- [x] In `GetRawVoiceData` (main thread): remove the `imitone.InputAudio` call. Keep the `imitone.GetState` call and downstream JSON parsing.

*Delete the chunking block in `ImitoneVoiceIntepreter.cs`* (currently around lines 750–780 after Step 2 cleanup; header comment `// CHUNKING: imitone's feed_buffer holds max 1 second...`):
- [x] Remove the chunking `for` loop and its `chunkSize = Math.Min(sampleRate, ...)` math.
- [x] Remove the `_imitoneChunkBuffer` field declaration and any conditional allocation. The audio-thread path always delivers small buffers (~21 ms / 1024 samples at 48 k); no chunking needed.
- [x] Remove the `chunkToPass = new float[chunkSize]` branch — this is a **per-frame main-thread allocation** in the current code, almost certainly contributing to existing main-thread variance. Its removal is an incidental win against jitter, independent of the threading move.
- [x] Remove the `// TO REVERT: remove the chunking block below and restore: imitone.InputAudio(capturedInput);` comment (now stale).
- [x] Confirm there is now exactly **one** `imitone.InputAudio(...)` call in the codebase, and it lives inside `OnAudioFilterRead`. *Verified via grep: live codebase has exactly one call (in `OnAudioFilterRead`); the two other matches are in `Reference/OLD_ImitoneVoiceInterpreterForDebugComparison.cs` (renamed class, not part of the active interpreter) and `ImitonePackage/Imitone/ExampleImitoneBehavior.cs` (package example, not game logic).*

*Imitone feed telemetry (lands with 3a so 3a's tests can verify):*
- [x] Add `aggImitoneInputAudioCallTotal` (long, `Interlocked.Increment` from audio thread) to the aggregate.
- [x] Add `aggImitoneGetStateCallTotal` (long, incremented from `Update`) to the aggregate.
- [x] Add `aggImitoneInputToCallbackRatio` (float, computed in `LateUpdate` as `aggImitoneInputAudioCallTotal / aggAudioCallbackTotal` over a rolling window). *Implementation note: the inspector field shows the **cumulative** ratio (cheap glance for "is this ~1.0?"); the **rolling 1-second window** is computed inside the `FAIL_IMITONE_FEED_RATIO_LOW` trigger and not separately surfaced. If the rolling-window value becomes useful for in-Editor debugging, lift it to its own field in 3b cleanup.*
- [x] Add `aggMainThreadFramesSinceLastImitoneStateChange` (int) — increments each frame; resets when `imitone.GetState` returns a meaningfully different state. *Implementation: state-change proxy is `(power, pitch_hz)` compared via `Mathf.Approximately` (NaN-safe). Counter only advances on frames where `GetRawVoiceData` actually called `imitone.GetState` — frames where the early-return paths fired (mic not ready, etc.) don't increment, by design.*

*Phase 3 FAIL\_\* flags landing in 3a (feed-side):*

```csharp
[SerializeField] private bool FAIL_IMITONE_NOT_FED;
[SerializeField] private bool FAIL_IMITONE_FEED_RATIO_LOW;
[SerializeField] private bool FAIL_RING_OVERFLOW_GROWING;
```

```csharp
[SerializeField] private float failImitoneNotFedSeconds = 0.2f;
[SerializeField] private float failImitoneFeedRatioMin = 0.95f;   // input/callback ratio
[SerializeField] private float failRingOverflowWindowSeconds = 2f;
```

| Flag | Triggers when |
|------|---------------|
| `FAIL_IMITONE_NOT_FED` | `aggImitoneInputAudioCallTotal` has not advanced for >= `failImitoneNotFedSeconds` wall-clock seconds while `aggAudioCallbackTotal` is advancing. (Distinguishes "audio thread is alive but feed is broken" from "audio thread is dead.") |
| `FAIL_IMITONE_FEED_RATIO_LOW` | `aggImitoneInputAudioCallTotal / aggAudioCallbackTotal` < `failImitoneFeedRatioMin` over the last second. |
| `FAIL_RING_OVERFLOW_GROWING` | `aggMicRingOverflowSkipTotal` (new audio-thread counter — increment in `OnAudioFilterRead` when a ring write would overrun the consumer position) increased within the last `failRingOverflowWindowSeconds` seconds. |

- [x] Add the three new fields + threshold fields to `MicVoiceIngestDebugAggregate.cs` and implement triggers in `LateUpdate`. *Implementation: `FAIL_IMITONE_NOT_FED` requires both feed-stale ≥ threshold AND callback-NOT-stale (else FAIL_AUDIO_CALLBACK_FROZEN owns that case); `FAIL_IMITONE_FEED_RATIO_LOW` uses a rolling 1-second window with sticky-between-rolls semantics; `FAIL_RING_OVERFLOW_GROWING` matches the existing `FAIL_MONITORING_STARVATION_GROWING` pattern.*
- [x] Add `aggMicRingOverflowSkipTotal` (long, `Interlocked.Increment` from audio thread) and increment it from `OnAudioFilterRead` whenever the ring write is skipped due to overflow (distinct from `audioCallbackLockMissTotal`, which is lock contention). Mirror it on the aggregate. *Implementation: counter, snapshot field, and FAIL trigger wiring are in place. The audio thread does not yet increment this counter — the audio-thread ring has no consumer position to overrun in 3a, so the overflow condition isn't observable until Step 5b adds a real consumer. Field stays at 0 in 3a; placeholder for Step 5b.*
- [x] Update the `FAILURE = ...` OR expression to include the three new feed-side flags. **Do not** remove `FAIL_INTERPRETER_NOT_CONSUMING` from the OR yet — it still describes the legacy main-thread consumption path until Step 5b retires that path.

**Notes & considerations (3a):**
- DSP filtering (HPF/LPF) and `_dbMicrophone` calculation **stay on the main thread** in 3a. The audio thread feeds imitone the **unfiltered mono** buffer; the main thread keeps filtering its copy from the ring for its own dB metering. Imitone gets the filtered signal again in 3b once filters move ahead of `imitone.InputAudio` in `OnAudioFilterRead`. (Briefly feeding imitone unfiltered audio for the duration of 3a is acceptable: HPF/LPF here are gentle voice band-passes, not pitch-altering; the test bar is "tone tracking still works," not "tone tracking is identical to pre-3a.")
- Be wary of double-counting samples. If the legacy main-thread ring writer is still running while we now feed imitone from the audio thread, that's fine for this step (parallel paths) — the test below confirms imitone is responsive without depending on the main-thread copy.

**Test (3a):**
- [ ] Tone normally; pitch tracking responsive every frame.
- [ ] `toneActive` fires reliably (true on voicing, false on silence).
- [ ] No regression in visuals or Wwise audio response (modulo the unfiltered-feed caveat above — judge "responsive" not "identical").
- [ ] `aggImitoneInputAudioCallTotal` tracks `aggAudioCallbackTotal` 1:1 (or close to it; ratio ≥ `failImitoneFeedRatioMin`).
- [ ] `aggImitoneGetStateCallTotal` climbs once per `Update()`.
- [ ] Forced mic recovery: telemetry resumes climbing afterward (validates the prep fix).
- [ ] Profiler: no GC allocations on the audio thread.
- [ ] FAIL OBSERVATION: `FAILURE` stays `false` during normal operation. None of the three new feed-side Phase 3 flags trigger.

**Click testing protocol is deferred to 3b** — DSP hasn't moved yet, so 3a can't break filter continuity by definition.

**Commit (3a):** `feat(step3a): feed imitone from audio thread; delete chunking; add feed telemetry + FAIL_* flags`

Actual landed commit: **`839a224c`** — `feat(step3a): migrate imitone feed to OnAudioFilterRead + diagnose/fix bypassEffects bug`. Bundles the Step 3 prep rebootstrap + Step 3a feed migration + a follow-on bug fix (H1e: `captureSource.bypassEffects` was diverting audio around the filter chain) + diagnostic infrastructure used to find that bug. Bug investigation log: `Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md`.

**Developer notes (3a):**

- **First-pass implementation status (pre-test):** all task checkboxes above are `[x]`; test checklist below is what gets ticked during the run. Lints clean across the four touched files.

- **Follow-on issues from the original 3a commit:**
  - **F1 — imitone-feed latency. RESOLVED 2026-05-06 via hybrid ring-feed pivot.** Investigation history in `Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md` (now archival) — eight test runs disambiguated three live hypotheses and concluded the V3/V5 streaming-clip pattern was engine-bound to a ~100-DSP-buffer (~2133 ms at 1024/48 k) minimum read-vs-write distance. **Fix architecture: hybrid ring-feed.** `captureSource` plays a silent in-memory dummy `AudioClip` (`stream: false` — critical) solely to drive `OnAudioFilterRead` cadence; the audio thread reads imitone-feed samples directly from `rawRingBuffer` (main-thread writer, audio-thread reader via the existing 4-arg `ReadRawSamples` with `Monitor.TryEnter(rawBufferLock, 0)`). Read cursor primed via `TryCreateRawReadCursorBehindMs(audioThreadFeedLatencyMs=64ms)` on the main thread before `captureSource.Play()`. Skip `imitone.InputAudio` when `copied == 0` (lock miss / empty ring) per imitone.cs:80. Pre-pivot 2.13 s feed gap → post-pivot ~150 ms (Pass 2 telemetry: 106–149 ms gap-band, drift 15 ms / 60 s, overflow drops = 0 over 153 s). **Closing commits:** `f68cacb3` (Pass 1 cutover), `bf670660` (Pass 2 telemetry rename + cursor-vs-write-head gap measurement), `d537ed9b` (Pass 3 cleanup + `FAIL_AUDIO_LOCK_CONTENTION` repointed to `aggRawRingReadLockMissTotal`). **Forward pointer for *remaining* perceptual lag** (downstream of feed — imitone smoothing, noise-floor gate via legacy main-thread `_dbMicrophone`, debounce thresholds): see [Appendix: Voice-onset latency lever inventory](#appendix-voice-onset-latency-lever-inventory) (commit `cae32729`). The full F1 plan + decisions + Pass 1/2/3 progress log lives in `Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md` (also archival as of closeout). Optional Pass 4 (bump `audioCallbackGcSuspectMsThreshold` from 3 ms to 15 ms to clear the `FAIL_AUDIO_GC_ALLOC_DETECTED` known-false-positive) and a `failAudioLockMissPerSecondThreshold` tune (from 50/sec to ~5/sec to fit the post-Pass-2 baseline of ~0.04 misses/sec on `rawBufferLock`) are deferred as small drive-bys, not part of F1 closeout.
  - **F2 — voice leaks to speakers** (audible mic-playback path that bypasses our `Array.Clear`). **Moved out of the bug doc into its own Step 3c above** — it's an audio-routing cleanup, not part of the imitone-feed correctness loop. Bug doc retains a pointer.

- **Review-pass fix #1 — deferred audio-thread logging.** First-pass `OnAudioFilterRead` catch logged the imitone exception via `UnityEngine.Debug.LogWarning($"... {ex.Message}")`. The `$"..."` interpolation is a managed alloc on the audio thread → trips `audioCallbackGCAllocSuspectTotal` → latches `FAIL_AUDIO_GC_ALLOC_DETECTED` permanently on the very first imitone exception of any session. Same false-positive class the user spent Step 2 commits cleaning up (`95e201b5`). **Fixed:** audio thread now `Interlocked.CompareExchange`-publishes the exception reference (no formatting); main thread drains via `Interlocked.Exchange` from `MicIngestMainThreadTick` (`DrainImitoneInputAudioPendingException()`), formats the log message there, and sets a `volatile bool` latch so the audio thread stops capturing further exceptions.

- **Review-pass fix #2 — `FAIL_IMITONE_NOT_FED` recovery flicker.** First-pass trigger compared `Time.realtimeSinceStartup - _lastImitoneInputAudioAdvanceRealtime` against the threshold without accounting for mic recovery. After every recovery, the new audio-thread capture path enters its priming window (`audioCallbackPrimingFramesToSkip = 8` ≈ 168 ms), during which callbacks fire but the imitone feed is gated. With callbacks advancing and feed not, the trigger would fire briefly on every recovery. Pre-3a had no imitone-side FAIL flag, so this is a new false alarm. **Fixed:** aggregate tracks `interpreter.MicCaptureEpoch` and resets `_lastImitoneInputAudioAdvanceRealtime = Time.realtimeSinceStartup` (and re-initializes the rolling-ratio window) when the epoch ticks. Deliberately does **not** reset `_lastAudioCallbackTotalAdvanceRealtime` — if callbacks fail to resume after recovery, `FAIL_AUDIO_CALLBACK_FROZEN` must still fire as the primary signal.

- **Cross-thread risk flagged for 3b's V7 audit:** the `imitone` field is now read from the audio thread (line ~405 of `ImitoneVoiceIntepreter.AudioThread.cs`) but is not declared `volatile`. Pre-3a it was main-thread-only. The single-write-in-Start happens-before-Play synchronization makes this safe in practice, but formally underspecified. 3b should mark `imitone` `volatile` along with the other audio-written / main-read fields it audits.

- **Behavioral note (intended per plan, not a regression):** imitone now consumes the Unity-mixer's resampled audio (`monoScratch`) instead of the main-thread `latestRawFrame` (`capturedInput`). If `micCaptureSampleRate == audioConfigOutputSampleRate` (typical at 48 kHz), the resampling is a passthrough and the two paths are sample-identical. Otherwise, imitone sees a slightly different waveform; pitch tracking should still be responsive. `_dbMicrophone` (main-thread) and `_dbValue` (imitone-power-derived) now sample slightly different points; they unify in 3b when DSP / dB metering move to the audio thread.

---

#### Step 3b: DSP migration + cross-thread atomicity + tear detection

> **Goal of this pass:** move HPF / LPF state and `_dbMicrophone` to the audio thread without resetting filter state (M4) or tearing floats (V7). Tear detector + cross-thread Inspector labels land here so atomicity violations are observable.

**Tasks:**

*DSP migration (filters + dB):*
- [x] Move HPF / LPF state fields (previous-sample memory `xn1, yn1, ...`) and the per-sample filter functions into `OnAudioFilterRead`. After this step, filter state is touched **only** from the audio thread. (See V6 — IIR filters work identically on small buffers; the audio thread is their natural home.)
- [x] **M4 click-prevention:** when relocating filter state, do **not** reset to zero. Move the existing values along with the logic; the filter must run continuously across the relocation boundary.
- [x] In `OnAudioFilterRead`, run filtering **before** `imitone.InputAudio(monoScratch)` so imitone sees the filtered signal (matches pre-Step-3 behavior; reverses 3a's interim "unfiltered feed" arrangement). Update the 3a ordering comment to match.
- [x] Move `_dbMicrophone` calculation into `OnAudioFilterRead`. Compute from the same buffer just fed to imitone, post-filter. Remove the now-redundant main-thread dB metering path (which was only there because filtering was main-thread in 3a).
- [x] Mark `_dbMicrophone` as `volatile float` per V7. Audit every read site on the main thread to confirm none rely on multi-step atomicity (only one of these reads matters per frame, so volatile is sufficient unless tear-detection telemetry says otherwise).
- [x] **Audit every other audio-written / main-read float and mark each `volatile` too.** Likely candidates: normalized peak meter, monitoring gain readout, any other DSP-derived value the audio thread computes and the main thread / Inspector consumes. For each one identified, mark it `volatile float` and append it to the `aggCrossThreadFieldsUsingVolatile` label string (next task) so the user can see at a glance which fields are under cross-thread protection.
- [x] Add `aggDbMicrophoneTearDetectedTotal` (long) to the aggregate. The tear detector runs once per `LateUpdate`: read `_dbMicrophone`; if the value is `NaN`, ±`Infinity`, or outside a plausible dB range (e.g. `-120f ≤ x ≤ +24f`), `Interlocked.Increment(ref aggDbMicrophoneTearDetectedTotal)`. (See V7 — this catches torn-read corruption on the float bits, which manifests as impossible bit-patterns.)

*Cross-thread atomicity Inspector labels (per V7 / Section A.5):*
- [x] Add `aggDbMicrophoneSnapshot` (float, read-only Inspector mirror) to `MicVoiceIngestDebugAggregate`. In `LateUpdate`, read `_dbMicrophone` once and copy into `aggDbMicrophoneSnapshot` so the user sees the value the main thread read this frame. (This is the source of the value the tear detector sanity-checks above.)
- [x] Add `aggCrossThreadFieldsUsingVolatile` (string) to `MicVoiceIngestDebugAggregate`. Initialize at startup to a comma-separated list of audio-thread fields currently behind `volatile` (e.g. `"_dbMicrophone, audioCallbackHzRolling, aggMixerChannels"`, plus any others identified in the audit task above). Read-only; surfaces the cross-thread contract for the user at a glance.
- [x] Add `aggCrossThreadFieldsUsingInterlocked` (string) for fields under `Interlocked` (e.g. `"audioCallbackTotal, audioRingWriteTotalSamples, aggImitoneInputAudioCallTotal, audioCallbackLockMissTotal, aggMicRingOverflowSkipTotal"`).
- [ ] If `aggDbMicrophoneTearDetectedTotal` ever becomes non-zero during testing, escalate `_dbMicrophone` from `volatile` to `Interlocked.Exchange` and update the labels accordingly. *(Awaits play-test result — see test bar below.)*

*Phase 3 FAIL\_\* flag landing in 3b (atomicity-side):*

```csharp
[SerializeField] private bool FAIL_DB_TEAR_DETECTED;
```

| Flag | Triggers when |
|------|---------------|
| `FAIL_DB_TEAR_DETECTED` | `aggDbMicrophoneTearDetectedTotal > 0` (sticky; the user must clear manually, since the cure is to escalate `_dbMicrophone` to `Interlocked` and we don't want this to silently go quiet on its own). |

- [x] Add the field to `MicVoiceIngestDebugAggregate.cs` and implement the trigger in `LateUpdate`.
- [x] Update the `FAILURE = ...` OR expression to include `FAIL_DB_TEAR_DETECTED`.

**Notes & considerations (3b):**
- **`_dbMicrophone` is now written from audio thread, read from main.** Default to `volatile float` per V7. Watch `aggDbMicrophoneTearDetectedTotal` during testing; escalate to `Interlocked` only if non-zero.
- **Filter state (HPF / LPF) must move to audio thread without resetting.** Carry the values across the move. M4 in the click appendix.
- **Leave the legacy main-thread ring buffer write alone for this step.** Step 3 only moves imitone feeding and DSP, not full ring buffer ownership. The legacy main-thread mic-ingest block (already inside `ImitoneVoiceIntepreter` since Step 0.7c) gets deleted in Step 5b.
- **Be wary of double-counting samples.** If the legacy main-thread mic-ingest path is still running and writing to its ring while we now read independently from the audio thread, that's fine for this step (parallel paths), but watch sample positions carefully — both paths should produce identical waveforms when compared.

**Test (3b):**
- [ ] Tone normally; pitch tracking still responsive every frame, `_dbMicrophone` updating smoothly.
- [ ] **Click testing protocol passes (all 5 scenarios from the click prevention appendix).** Filter migration and dB-from-audio-thread are exactly the kind of changes that introduce clicks if state is reset or atomicity tears.
- [ ] `aggDbMicrophoneTearDetectedTotal` stays at 0 across a full session including loud / silent / rapid on-off transitions.
- [ ] `aggDbMicrophoneSnapshot` matches the value the main thread is consuming (sanity).
- [ ] `aggCrossThreadFieldsUsingVolatile` and `aggCrossThreadFieldsUsingInterlocked` populate sensibly (read them in the Inspector and confirm the lists match the actual code).
- [ ] `MicVoiceIngestDebugAggregate` may show stale or weird values for legacy `unread_zero`-related fields — expected; cleaned up in Step 5b/6.
- [ ] Profiler: no GC allocations on the audio thread.
- [ ] FAIL OBSERVATION: `FAILURE` stays `false` during normal operation. None of the four Phase 3 flags trigger.

**Commit (3b):** `feat(step3b): move HPF/LPF + _dbMicrophone to audio thread; add tear detection + cross-thread labels`

**Developer notes (3b):**

*Decisions taken at the start of 3b implementation (Opus 4.7, agreed with user):*

- **D1 — `_dbMicrophone` on lock-miss (`copied == 0`):** filter+dB block is **skipped** entirely; `_dbMicrophone` retains its last value. Running the IIR over a zero-padded buffer would inject the filter's transient response into the imitone feed AND drag the dB readout to floor on every miss — both bad. Lock misses were already near-zero in F1 telemetry, and `Monitor.TryEnter(0)` makes them brief; preserving the last value is the right behavior.
- **D2 — `_dbMicrophone` during priming callbacks:** filter+dB **does run** (priming reads + advances every callback, just skips `imitone.InputAudio`). Pre-loads the dB readout so the noise-floor gate is already settled when imitone gates open at end-of-priming.
- **D3 — Volatility of filter `SerializeField`s:** marked `volatile bool` / `volatile float` for `_highPassFilterEnabled`, `_highPassCutoffHz`, `_lowPassFilterEnabled`, `_lowPassCutoffHz`. Main thread writes (Inspector edits at runtime), audio thread reads each callback. Strictly speaking the V7 audit only mandates `volatile` for audio-write / main-read floats (`_dbMicrophone`), but the runtime-tunability case is symmetric and the cost is negligible. Without this, the audio thread could cache the value indefinitely and Inspector tweaks would not take effect.
- **D4 — `capturedInput` and `tryCopyOk` gate:** retired. The legacy main-thread copy-and-process path was the only writer of `capturedInput` and `tryCopyOk`'s only consumer. With filter+dB on the audio thread, the gate has nothing to gate. `TryCopyLatestRawFrame` itself is left in place (Step 5b owns its retirement along with the rest of the legacy ingest block).
- **D5 — Dead `debugInterpreter*` / `telemetryRawVoiceDataConsumedThisFrame` fields:** deleted (5 `SerializeField`s in `ImitoneVoiceIntepreter.cs` + matching members of `RawVoicePathDebugSnapshot` + the consumer fields in `MicVoiceIngestDebugAggregate.cs`). Mic-readiness liveness is now sourced directly from `interpreter.IsMicReady` (already a public property), eliminating one round-trip via a debug-mirror field. **Follow-on:** `FAIL_INTERPRETER_NOT_CONSUMING` retired in the same pass — its trigger was `aggRawConsumedThisFrame` (always-false post-3b, since the gate it tracked is gone). The audio-thread feed health flags (`FAIL_IMITONE_NOT_FED`, `FAIL_IMITONE_FEED_RATIO_LOW`) cover the actual liveness signal directly.
- **D6 — Single-pass commit:** all 3b code lands in one commit (this commit). The implementation pieces are tightly coupled (the filter+dB move requires the cross-thread atomicity work to be safe; the tear detector is the validation tool for the volatile choice). Splitting would create an intermediate state where 3b is half-correct.

*Sentinel handling for tear detection:*
- `_dbMicrophone` initializes to `-999f`. The audio thread's `LinearToDb` clamps amplitude to `1e-6f` → `-120 dB` floor, so it will never write a value below `-120`. The tear detector explicitly excludes `-999f` (pre-init sentinel) from triggering, so `FAIL_DB_TEAR_DETECTED` does not latch during the audio-thread warm-up window.

*`FAIL_MIC_NOT_READY` simplification:*
- Trigger expression changed from `aggInterpMicRefNull || !aggInterpMicReady` → `!aggInterpMicReady`. The first clause was always-false in practice (the source `debugInterpreterMicRefNull` was never assigned `true` anywhere) and contributed no information. Sourcing `aggInterpMicReady` from `interpreter.IsMicReady` directly drops the round-trip via `RawVoicePathDebugSnapshot` and matches the canonical public liveness signal.

*Filter alpha + sample rate:*
- The audio-thread filter uses `sampleRate` (= `micCaptureSampleRate`, set in `Start()` before any `OnAudioFilterRead` callback fires). This matches the pre-3b filter alpha exactly. M4 click prevention requires the filter coefficient to be identical across the relocation boundary; using the same field (rather than recomputing from `audioConfigOutputSampleRate`) preserves that.

*Files changed:*
- `Assets/Scripts/Voice/ImitoneVoiceIntepreter.cs` — `_dbMicrophone` → `volatile`; filter `SerializeField`s → `volatile`; `capturedInput`, `imitoneMaxFeedFramesAt60FpsEquivalent`, `_hpPrev*` / `_lpPrevOutput`, `ApplyHighPassFilter` / `ApplyLowPassFilter`, `telemetryRawVoiceDataConsumedThisFrame`, four `debugInterpreter*` fields all deleted; `RawVoicePathDebugSnapshot` trimmed to the two unclamped-dB fields; `GetRawVoiceData` rewritten to drop the `tryCopyOk` gate and dead debug-field assignments.
- `Assets/Scripts/Voice/ImitoneVoiceIntepreter.AudioThread.cs` — added `audioThreadHpPrevInput` / `audioThreadHpPrevOutput` / `audioThreadLpPrevOutput` (audio-thread-only state); `ApplyHighPassFilterOnAudioThread` / `ApplyLowPassFilterOnAudioThread` helpers; filter+dB block in `OnAudioFilterRead` between `ReadRawSamples` and the `peakAbs` telemetry; `peakAbs` comment updated to clarify it is now post-filter when `copied > 0`; top-of-file summary comment updated for 3b.
- `Assets/Scripts/Voice/MicVoiceIngestDebugAggregate.cs` — added Cross-thread atomicity block (`aggDbMicrophoneSnapshot`, `aggDbMicrophoneTearDetectedTotal`, `aggCrossThreadFieldsUsingVolatile`, `aggCrossThreadFieldsUsingInterlocked`); added `FAIL_DB_TEAR_DETECTED` (sticky); added tear detector and FAIL trigger logic; retired `FAIL_INTERPRETER_NOT_CONSUMING` and `failInterpreterNotConsumingFrameThreshold`; simplified `FAIL_MIC_NOT_READY`; rewrote CURRENT TEST block for 3b's test bar (drops `currentTestHybridFeedGapMs` + `currentTestMicExitReason`, adds `currentTestDbMicrophoneSnapshot` + `currentTestDbMicrophoneTearDetectedTotal`); deleted four dead `agg*` fields tied to the deleted snapshot members.
- `Docs/MIC_VOICE_INGEST_FIX_PLAN.md` — task list ticks (this section); developer notes (this block).

*Play-test results (3b, 2026-05-06):*

User played the click-testing protocol and reported:

- **Tear detector clean.** `currentTestDbMicrophoneTearDetectedTotal = 0` throughout the session. The `volatile float` choice for `_dbMicrophone` is sufficient on this platform — **no escalation to `Interlocked.Exchange` needed.** V7 default vindicated for the primary cross-thread float surface.
- **`currentTestDbMicrophoneSnapshot` responsive.** Audio-thread writer + main-thread reader path is healthy.
- **Subjectively responsive.** DSP migration introduced no perceptible feed-side lag; pitch and dB still alive on voice with no regression vs F1 closeout.
- **`currentTestFeedPeakAbs` lower than the F1-era bar.** Voice ~0.02, silent ~0.001 (vs the "voice ~0.05–0.5" tooltip text inherited from F1). **This is expected post-3b** — the F1-era bar measured peakAbs on the *unfiltered* audio-thread feed, while 3b measures it post-HPF/LPF. The 80–520 Hz band-pass attenuates voice harmonics above 520 Hz and rumble below 80 Hz, so post-filter peak is naturally smaller. Voice-vs-silent contrast (20×) still confirms the feed is alive and post-filter signal is healthy. **Bar updated:** the F1-era "voice ~0.05–0.5" guidance applied to unfiltered samples; post-3b, expect "voice ~0.01–0.1, silent < 0.005, contrast > 10×". (Tooltip text touch-up deferred — not worth a code change just for that.)
- **`FAILURE = TRUE` from the known GC false-positive — retired the flag entirely (not just bumped the threshold).** First attempt: bumped `audioCallbackGcSuspectMsThreshold` from 3 ms → 15 ms (commit `fd124291`). User retested and reported the flag *still* sticky-on (`starts on, stays on`), which forced the deeper read: imitone analysis on this hardware occasionally tail-spikes into the 15–20 ms range (still under the 21 ms callback budget but past the heuristic). The duration heuristic was **structurally redundant** post-3b — `FAIL_AUDIO_CALLBACK_RATE_LOW` and `FAIL_AUDIO_CALLBACK_FROZEN` already catch real audio-thread starvation directly and reliably, and they were added after this Phase 2 heuristic. **Action taken (next commit):** retired `FAIL_AUDIO_GC_ALLOC_DETECTED` along with its sticky-latch state (`_gcAllocStickyLatched` / `_gcSuspectBaselineAtClear` / `_audioGCBaselineInitialized`), the trigger logic, the FAILURE OR clause, and `currentTestKnownFalsePositive_GcAlloc` from the CURRENT TEST block. Kept `aggAudioCallbackGCAllocSuspectTotal` and `audioCallbackGcSuspectMsThreshold` as diagnostic-only telemetry — useful as a "how often does the audio thread spike above N ms" counter, just not as a FAIL trigger. Closes the deferred Optional Pass 4 from the archived F1 plan, structurally rather than via threshold tuning.
- **Very occasional minor click at toning onset — deferred to Step 3c (F2 speaker leak).** Not a 3b regression: filter state was preserved across the relocation (M4 click-prevention) and the bit-identical filter alpha math means the audio path through imitone is unchanged. The click is most likely the F2 speaker-leak path responding to voice onset (the leak path bypasses our `Array.Clear` zero-fill at the end of `OnAudioFilterRead`). 3c's `MicrophonePlayback` GameObject investigation is exactly the right venue: if disabling that GameObject silences the click, F2 explains both. Logged as a Step 3c observation rather than a 3b follow-up.

---

#### Step 3c: Audio routing cleanup (silence the speaker leak surfaced by 3a)

> **Goal of this pass:** identify and silence the audible mic-playback path that became audible after 3a's H1e fix. The leak was masked pre-3a by `captureSource.bypassEffects = true`. With `bypassEffects = false` (required for the imitone feed to work — see Step 3a Developer notes / `Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md`), our captureSource's filter chain now zeroes its own output via `Array.Clear(data, ...)` at the end of `OnAudioFilterRead`, but a separate audio path is still routing the mic clip to the system output. This step finds and removes that path.
>
> **Recommended LLM for this step: Composer 2 (full)** — primarily investigation + scene cleanup, minimal new code. **Switch to Opus 4.7** for the review pass.
>
> **Ordering:** independent of 3b's DSP migration. Either order works. If F2 turns out to be entangled with `DirectVoiceMonitoring` (already log-spamming pre-3a), do 3c first to clear the audio-routing picture before 3b adds another moving part.

**Background (consolidated from `Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md` "F2"):**

Audible mic playback persists post-3a even though our captureSource is silenced via `Array.Clear(data, 0, data.Length)` at the end of `OnAudioFilterRead`. The leak takes a path that doesn't go through our filter.

AudioListener inventory done by user (2026-05-05):

| Component | GameObject | Carries voice to speakers? |
|---|---|---|
| `AudioListener` (Unity built-in) | `Camera` | YES — the only Unity listener in scene; routes Unity AudioSources to system output. |
| `AkAudioListener` (Wwise) | `Camera` | NO (per user — Wwise listener does not carry the mic voice). |
| `AkAudioListener` (Wwise) | `GameObjectSystem2Listener` | NO (per user). |

Implication: voice is reaching the Camera's `AudioListener` via a **second AudioSource** somewhere in the scene that's playing the mic clip independently of our captureSource. **Prime suspect:** `SoundSelfAudioVisualControl/MicrophonePlayback` GameObject (the name is conspicuous, and it's in the audio-related branch of the hierarchy).

Possibilities, in order of likelihood:
- **F2a:** `MicrophonePlayback` has its own `AudioSource` (or drives one on a child / on the Camera) playing the mic clip. Most likely.
- **F2b:** Some other component (`DirectVoiceMonitoring`, `VoiceLogic`, legacy `MicPipeline` left-overs) is doing live mic playback.
- **F2c (least likely now):** Our captureSource has a filter or listener-effects route we missed. Re-inspect `bypassListenerEffects` / `outputAudioMixerGroup` interactions if F2a/F2b come up empty.

**Tasks:**

*Identify the leak source:*
- [ ] In Play mode, inspect `SoundSelfAudioVisualControl/MicrophonePlayback` in the Inspector. List all components on it. Check whether any `AudioSource` on it has `clip` referencing the mic clip and `isPlaying = true`.
- [ ] **Disable test:** uncheck the `MicrophonePlayback` GameObject during a Play session. If the audible voice goes away → F2a confirmed (jump to "Address the leak source"). If the leak persists → continue to the next probe.
- [ ] If `MicrophonePlayback` is not the source: search the scene for any other `AudioSource` whose `clip` references the microphone clip. Use Hierarchy search `t:AudioSource` and check each one.
- [ ] If no other AudioSource is playing the mic clip: re-check our captureSource's routing — `outputAudioMixerGroup`, `bypassListenerEffects`, any Wwise-side path that might intercept the filter output before our `Array.Clear` runs.

*Address the leak source (post-identification):*
- [ ] If `MicrophonePlayback` is a dev/debug tool no longer needed → remove it from the scene (and any code that references it) in this commit.
- [ ] If `MicrophonePlayback` serves an actual product feature (e.g., onboarding mic-test, dev-mode monitoring) → either gate it behind an explicit Inspector toggle that defaults to off, or re-route it to feed from our audio-thread ring instead of an independent AudioSource.
- [ ] Whatever the disposition: document the call in this step's Developer notes.

**Notes & considerations (3c):**
- This step does not touch the audio-thread feed path. It's purely audio-routing cleanup.
- If the source ends up being something harder to remove cleanly (e.g., a Wwise monitoring path that's actually wanted), document the finding here and consider whether the speaker leak is acceptable for the test build until a larger refactor.
- Cross-check `DirectVoiceMonitoring` since it's already log-spamming "Buffered transport underflow / starvation" pre-3a. If it's not actively used, retire it in this step.

**Test (3c):**
- [ ] Tone normally; you should **not** hear your own voice through the speakers.
- [ ] No regression in any other audio (Wwise music, lights audio-reactivity, monitoring features that should be on).
- [ ] No new FAIL flags trigger.

**Commit (3c):** `chore(step3c): silence speaker leak surfaced by 3a's H1e fix`

**Developer notes (3c):**

*Forwarded from Step 3b play-test (2026-05-06):*
- **Onset click symptom:** user reports a "very occasional minor click when I begin toning" during 3b play-test. Not a 3b regression (filter state preservation + bit-identical filter alpha math rule that out). Likely an F2 speaker-leak path responding to voice onset — the leak bypasses `Array.Clear` at the end of `OnAudioFilterRead`, so it carries whatever the original mic clip's first non-silent samples are. **First diagnostic during 3c:** disable the suspected `MicrophonePlayback` GameObject during a Play session and tone several times. If the onset click goes away alongside the steady-state speaker leak, F2a is confirmed and one fix addresses both. If the click survives the F2 fix, escalate to per-callback profiling at toning onset to find the path.

---

### Step 4: Verify and tune

> **Recommended LLM for this step:**
> - **First-pass: Composer 2 (full)** — primarily testing and tuning, minimal new code. Use Composer for any quick code adjustments that emerge.
> - **Switch to Opus 4.7 mid-step if testing surfaces issues** that need analysis (e.g., unexpected click, audio-thread spike, tearing telemetry firing). Diagnostic reasoning is Opus's strength.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5)
>
> *Confirm the right model is selected before continuing. Switch to Opus 4.7 the moment any non-trivial issue surfaces, and always for the review pass.*

Step back, run extended testing, address any issues that emerge before moving to the bigger refactor.

**Tasks:**

*Extended testing:*
- [ ] Long toning session (5+ minutes). No degradation over time.
- [ ] Heavy toning (loud, sustained, varied). No degradation under load.
- [ ] Edge cases: very quiet input, very loud input, sudden silence, sudden onset, breath sounds, whistle.
- [ ] Compare A/B against Step 0 baseline recordings (subjective: feel; objective: aggregate counters).
- [ ] Watch the Unity Profiler for any audio-thread spikes, GC allocations, or unexpected costs.
- [ ] Check latency: feedback feels as fast or faster than baseline.
- [ ] FAIL OBSERVATION: `FAILURE` stays `false` for the entire extended-testing window.

*Click testing protocol (run all 5 scenarios from the click prevention appendix; explicit, do not skip):*
- [ ] Scenario 1: Quiet baseline (30 s silence) — no periodic clicks.
- [ ] Scenario 2: Sustained tone (30 s steady note) — no clicks at any cadence.
- [ ] Scenario 3: Onset / offset (rapid voice on / off) — no clicks at phonation start / end.
- [ ] Scenario 4: Heavy load (tone + simulated heavy CPU work) — no clicks correlated with CPU spikes.
- [ ] Scenario 5: Long session (5+ min) — no clicks emerging over time.

*Optional V10 latency tuning (only if latency feels too high):*
- See [Appendix: Voice-onset latency lever inventory](#appendix-voice-onset-latency-lever-inventory) for the full set of knobs across the entire voice → `toneActive` chain. The DSP-buffer-size knob below is **one slice** of that inventory (Stage 5). Tuning is subjective; the inventory is structured so that Step 4 / Step 5b's tuning passes can walk it stage-by-stage.
- [ ] Measure current latency subjectively. Default Project Settings → Audio → DSP Buffer Size is typically `Best (latency)` = 256, `Good (latency)` = 512, `Default` = 1024 samples.
- [ ] If too laggy and `aggAudioCallbackHzRolling` and `aggAudioCallbackMaxGapMsLastSecond` are both healthy on the current setting, try lowering one notch (e.g. Default → Good, or Good → Best) and re-run the extended-testing checklist above.
- [ ] After lowering, re-verify in `MicVoiceIngestDebugAggregate`: `aggAudioCallbackHzRolling` still near nominal (now higher: e.g. 187.5 Hz at 256 / 48 k); `aggAudioCallbackMaxGapMsLastSecond` still near new nominal (~5.3 ms at 256 / 48 k); `aggAudioCallbackLockMissTotal` still near zero; `aggAudioCallbackGCAllocSuspectTotal` still 0.
- [ ] If the lower buffer size produces audible glitches, callback rate drops, or lock contention, revert to the previous setting and accept the latency.
- [ ] If you keep the lower buffer size, update the captured `audioConfigDspBufferSize` baseline (Step 0) and re-derive ring buffer sizing if appropriate.

**Notes & considerations:**
- This step is testing and tuning, not new features. If issues are found, debug them here before adding more changes on top.
- **Common issues to watch for and what they mean:**
  - *Imitone state going stale at startup:* probably `GetState` being called before any `InputAudio` has run on the audio thread (race). Add a guard that skips `GetState`-driven game logic until `aggImitoneInputAudioCallTotal > 0`.
  - *Audio thread skipping callbacks:* work inside `OnAudioFilterRead` exceeding buffer time. Profile the audio thread and reduce per-callback work.
  - *Filter glitches:* filter state was touched from both threads at some point during Step 3. Audit; the filter must be audio-thread-only after Step 3.
  - *Pitch tracker laggy under load:* check `aggMainThreadFramesSinceLastImitoneStateChange` — if it climbs, the main thread is starved (game logic problem, not audio-thread problem).
- The DSP buffer size knob is **optional**. The Step 3 architecture works at any reasonable DSP buffer size; this is a polish step, not a fix.

**Test:** Multiple recorded sessions, A/B compared.

**Commit:** `fix: address [specific issue]` for each issue found, OR `chore: verify audio-thread imitone feed stable over extended use` if no fixes needed.

**Developer notes:** _none_

---

### Step 5: Delete legacy main-thread mic-ingest block + monitoring click-hardening

> **Recommended LLM for this step: Opus 4.7.**
> - **First-pass: Opus 4.7** for both sub-passes — click mitigation in the monitoring path (M1/M2/M6) and provisional code deletion inside the merged `ImitoneVoiceIntepreter`. The sample skips, fades, and gain interpolation in M1/M2/M6 are sample-level DSP that's easy to get subtly wrong; the legacy-block deletion (5b) needs careful surgery inside a file that now also runs the audio-thread architecture. Use the thinking model.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5) — regression risk is real even though the refactor footprint is smaller than it was in the original plan ordering.
>
> *Confirm Opus 4.7 is selected before continuing. Stay on Opus through both sub-passes and the final review pass for this step.*

By the time we reach this step, Step 0.7 has already merged `MicPipeline` into `ImitoneVoiceIntepreter` and Steps 1–4 have built the audio-thread path on top. The merged file currently runs **two paths in parallel** inside one MonoBehaviour: the legacy main-thread mic-ingest block (the `UpdateMicReadFrame` / gentle recovery / double-poll machinery migrated in 0.7c) and the new audio-thread block (`OnAudioFilterRead` and friends from Step 1, feeding imitone since Step 3). The legacy block is no longer load-bearing — Step 3 redirected the imitone feed to the audio thread — but it's still alive as a fallback.

Step 5 finishes the job: harden the monitoring path against clicks, then **delete the legacy main-thread block** from inside `ImitoneVoiceIntepreter`. After Step 5b, the file contains only the audio-thread architecture.

**Sub-pass 5a — Harden the monitoring path against clicks (M1/M2/M6):**

The audio-thread architecture should already make underflow / overflow rare, but the fallback paths are still there and must not click when they do fire. M1 and M2 address sample-discontinuity at underflow/overflow seams; M6 addresses gain-step clicks.

*Click prevention M3 (deterministic write-before-read) — verify, no code change expected:*
- [ ] Confirm `ImitoneVoiceIntepreter` and `DirectVoiceMonitoring` are on **separate** GameObjects (per V5), each with **exactly one** AudioSource. Multiple AudioSources on the same GameObject would reintroduce `OnAudioFilterRead` chain-routing ambiguity and break the architecture's correctness guarantees.
- [ ] Confirm Project Settings → Script Execution Order: `ImitoneVoiceIntepreter` (capture, at −104) runs **before** `DirectVoiceMonitoring` (at −102). Unity fires `OnAudioFilterRead` callbacks across MonoBehaviours in script-execution-order regardless of GameObject co-location, so this is sufficient to guarantee the ring is freshly written before monitoring reads it every callback. No change needed unless these values have drifted since Step 0.

*Click prevention M1 (click-free underflow handling in `DirectVoiceMonitoring`):*
- [ ] Audit `DirectVoiceMonitoring.OnAudioFilterRead`'s underflow / lock-contention path. Today it most likely fills with `0f` (silence) when the ring has no fresh samples. Hard zero from a non-zero last sample = audible click.
- [ ] Replace the silence-fill with one of:
  - **Option A — DC hold:** continue outputting the last-known sample for the duration of the underflow region. Simple; works well for short underflows.
  - **Option B — short fade:** linearly fade from the last-known sample toward `0f` over ~32 samples (~0.7 ms at 48 k), then hold at `0f` if the underflow continues. Slightly more code; eliminates the DC-buildup artifact if underflow lasts a long time.
- [ ] Pick one and implement consistently. Document the chosen behavior in a header comment on the underflow branch.

*Click prevention M2 (click-free overflow handling in `DirectVoiceMonitoring`):*
- [ ] Audit `DirectVoiceMonitoring.OnAudioFilterRead`'s overflow / sample-skip path (when read cursor is forced to jump forward because writes have lapped reads).
- [ ] Replace the hard cut with a short crossfade between the "old read position about to be abandoned" sample and the "new read position" sample over ~32 samples. Keep the implementation branch clearly commented.
- [ ] Verify `aggMonitoringOverflowTotal` still increments correctly so the user can still see overflow happening — the mitigation makes it inaudible, not invisible.

*Click prevention M5 (clip not reassigned mid-session):*
- [ ] Audit every code path that touches `monitoringSource.clip`. After this sub-pass, `monitoringSource.clip` is assigned exactly once at startup and never reassigned. Remove `ConfigureMonitoringSourceClip`-style mid-session reassignment paths, or gate them to startup-only with a clear comment.

*Click prevention M6 (smooth gain interpolation):*
- [ ] Audit how `DirectVoiceMonitoring` applies `monitoringVolume × dynamicScale` to the buffer. Confirm the gain is interpolated across the buffer (e.g. linearly from the previous-buffer end value to the current target) rather than applied as a step change at buffer start.
- [ ] If a step change is found, change to per-sample interpolation. The cost is one multiply-add per sample; the benefit is no clicks when gain or scale changes (e.g. on `toneActive` transitions).

*Compile + run + click test before sub-pass 5b.*

**Commit (5a):** `feat: harden DirectVoiceMonitoring against clicks (M1/M2/M5/M6)`

---

**Sub-pass 5b — Delete the legacy main-thread mic-ingest block from `ImitoneVoiceIntepreter`:**

This sub-pass removes the cruft listed in the cleanup appendix below from inside the merged file. It is **not** optional — leaving it in place creates two recovery stories and risks a future surprise `Microphone.End / Start` in production. By the end of 5b, `ImitoneVoiceIntepreter.cs` contains only the audio-thread architecture (capture, ring, imitone feed, DSP, telemetry) plus the main-thread game logic that was always there (CheckToning, TrackMicVolume, Wwise events, etc.). The legacy main-thread mic-ingest block is gone.

*Delete entirely from `ImitoneVoiceIntepreter.cs` (see Appendix "Provisional code to delete" for the exhaustive symbol list, which still applies — just inside the merged file rather than `MicPipeline.cs`):*
- [ ] The gentle `unread_zero` recovery family (7 inspector fields, 5 internal state fields, `PerformGentleUnreadZeroCaptureRestart()`, the `unread_zero_gentle_restart` branch, and 3 snapshot fields). All originally lived in `MicPipeline`; they live in the merged file now and are deleted from there.
- [ ] The bookmark / double-poll machinery (`micWriteHeadDoublePoll`, `micPosRead` / `micPosWrite`, `stalledWriteHeadFrameCount` / `stalledWriteHeadFrameThreshold`, the `stalled_capture_stopped` / `unread_zero` / `unread_zero_gentle_restart` exit-reason strings).
- [ ] `UpdateMicReadFrame` (the legacy main-thread mic read loop) and any `Update()` call site for it. The audio-thread `OnAudioFilterRead` is now the sole writer to the ring(s).
- [ ] Any remaining code that calls `Microphone.GetPosition` from `Update`. (Position tracking happens via `_samplesWritten += data.Length / channels` on the audio thread.)
- [ ] If Step 1's "new audio-thread ring" and the legacy main-thread ring are both still present, **consolidate to exactly one raw ring and one normalized ring** as part of this sub-pass. Reuse the larger / better-sized buffer if they differ. Audit normalization logic during the consolidation:
  - [ ] If normalization writes samples that `imitone.InputAudio` consumes, the writing code **must** live on the audio thread (right next to capture).
  - [ ] If a function is only a telemetry meter (e.g. surface a peak for the Inspector), it may stay on the main thread.
  - [ ] Annotate each surviving function header with one of: `// runs on: audio thread` or `// runs on: main thread`. No ambiguity.
- [ ] Confirm there are no `#if false` blocks, no `// TODO restore later` stubs, no commented-out method bodies. Provisional experiments are deleted, not parked.

*Phase 4 — retire obsoleted FAIL OBSERVATION flags in `MicVoiceIngestDebugAggregate.cs`:*
- [ ] Delete `FAIL_UNREAD_ZERO_SUSTAINED` and its threshold fields (`failUnreadZeroSustainedFrameThreshold`, `failUnreadZeroSustainedSecondsThreshold`) and per-flag tracking state.
- [ ] Delete `FAIL_GENTLE_RECOVERY_FIRED` and any sticky-clear plumbing tied specifically to it.
- [ ] Delete `FAIL_INTERPRETER_NOT_CONSUMING` (the main-thread `aggRawConsumedThisFrame` path is gone; the audio-thread analog is `FAIL_IMITONE_NOT_FED`, added in Step 3).
- [ ] Delete `FAIL_INGEST_RING_STALLED` if its trigger no longer maps onto the new architecture. (After 5b consolidation, `aggMicRawRingWriteTotalSamples` is fed exclusively by `OnAudioFilterRead`; a stalled ring write means a stalled audio thread, which is what `FAIL_AUDIO_CALLBACK_FROZEN` already covers. Verify that's true and delete the redundancy if so.)
- [ ] Update the `FAILURE = ...` OR expression to remove these terms.
- [ ] Verify in the Inspector that the FAIL OBSERVATION section now reads, top-to-bottom: top-level `FAILURE`; Phase 2 audio-thread flags; Phase 3 imitone-feed / atomicity flags; surviving Phase 1 flags (likely `FAIL_MIC_NOT_READY` and `FAIL_MONITORING_STARVATION_GROWING` only).
- [ ] Confirm the top-level `FAILURE` boolean's name, position, and OR semantics are unchanged. That continuity is the user's anchor across the rearchitecture.

*Do **not** delete* (see appendix "Keep through the rearchitecture"):
- [ ] The canonical `aggMicRawRingWriteTotalSamples` / `aggMicNormRingWriteTotalSamples` ring-write totals.
- [ ] The ring buffer infrastructure itself (the consolidated rings, the lock object, the `Monitor.TryEnter` pattern in the audio-thread writer).
- [ ] `OnAudioFilterRead` and everything it calls.
- [ ] The `MicIngestDebugSnapshot` accessor, retitled where appropriate to reflect the new field set.

*Telemetry consolidation tasks (per Section A.5):*
- [ ] Confirm `MicVoiceIngestDebugAggregate` is the central panel for cross-cutting metrics (audio-thread health, ring-write rates, lock-miss counts, atomicity / tear flags). No duplicates elsewhere.
- [ ] Confirm `DirectVoiceMonitoring.cs` retains its monitoring-specific self-concern fields (underflow / starvation / monitoring gain / clip-state). Those describe the file's internal behavior and are useful in isolation.
- [ ] Confirm `ImitoneVoiceIntepreter.cs` retains the relevant `toneActive` / pitch / dB telemetry needed in-place for game logic. Aggregate may surface read-only mirrors but source of truth stays in the interpreter.
- [ ] For every metric in the aggregate, search for duplicates in individual files; delete the duplicate if the aggregate is now authoritative.

*`[DefaultExecutionOrder]` cleanup tasks (per the doc's Environment section):*
- [ ] Identify every `[DefaultExecutionOrder(...)]` class attribute remaining in the voice path (`ImitoneVoiceIntepreter`, `DirectVoiceMonitoring`, `RecordedAudioPlayback`, `MicVoiceIngestDebugAggregate`).
- [ ] For each, prompt the user to open Project Settings → Script Execution Order and confirm there is an explicit entry for that class.
- [ ] Once confirmed, remove the class-level attribute. Do not silently drop attributes without verifying the Project Settings entry.

*Compile + run + full click test protocol (all 5 scenarios from the click prevention appendix) before committing.*

**Commit (5b):** `chore: delete legacy main-thread mic-ingest block from ImitoneVoiceIntepreter`

---

**Notes & considerations:**
- **The merged file is now smaller, not larger, after 5b.** A successful 5b deletes 100s of lines from `ImitoneVoiceIntepreter` (the entire `UpdateMicReadFrame` family, gentle recovery, double-poll, etc.). If the file isn't shrinking visibly, something has been missed.
- **One sub-pass at a time, with a compile + run + click check between each.** The sub-pass boundaries are not decorative; they are the rollback points if something breaks.
- **Watch for "the legacy block was secretly load-bearing" surprises.** Even after Step 3 redirected the imitone feed to the audio thread, the legacy block may still be doing something subtle (debug telemetry, a side-effect that another file depends on). The Phase 4 retirement of `FAIL_INTERPRETER_NOT_CONSUMING` is one such — it depends on the legacy block existing. Audit before deleting.
- If anything is unclear or compiles wrong, stop and surface the issue rather than improvising.

**Test (after both sub-passes):**
- [ ] Project compiles with no errors.
- [ ] Voice path works end-to-end (toning, monitoring, visuals, Wwise).
- [ ] `MicVoiceIngestDebugAggregate` shows valid values for the audio-thread health section, the cross-thread atomicity section, and the ring-write totals.
- [ ] FAIL OBSERVATION: Phase 1 obsolete flags are gone; surviving flags read sensibly; `FAILURE` stays `false` during normal operation.
- [ ] `rg -n "UpdateMicReadFrame|gentleUnreadZero|micWriteHeadDoublePoll|stalledWriteHeadFrame|unread_zero" Assets/Scripts/` returns zero matches (or only inside comments / docstrings).
- [ ] Click testing protocol (all 5 scenarios from the click prevention appendix) passes cleanly. **Pay especially close attention** to the M1 / M2 / M6 mitigations introduced in 5a — deliberately stress underflow (e.g. heavy CPU spike), overflow (e.g. simulate a brief pause in the monitoring AudioSource), and gain transitions (e.g. rapid `toneActive` flips).

**Developer notes:** _none_

---

### Step 6: Clean up debug telemetry, finalize aggregate Inspector layout, document interpretation

> **Recommended LLM for this step:**
> - **First-pass: Composer 2 (full)** — Inspector reorganization, deleting obsolete fields, reordering headers, writing the interpretation guide. Mostly mechanical edits inside `MicVoiceIngestDebugAggregate`.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5)
>
> *Confirm the right model is selected before continuing. Switch to Opus 4.7 when the first-pass is complete and the mandatory review pass begins.*

After Step 5b the architecture no longer has `unread_zero`, `stalled_capture_stopped`, gentle recovery, or `Microphone.GetPosition` polling on the main thread. Step 6 retires the now-meaningless fields, finalizes the new `MicVoiceIngestDebugAggregate` layout, and writes down the **interpretation guide** so the user can tell at a glance whether the system is healthy or broken.

**Sub-step 6a — Remove obsolete fields:**

- [ ] In `MicVoiceIngestDebugAggregate.cs`, delete `aggMicExitReason` (the old `unread_zero` / `stalled_capture_stopped` / etc. enum). No source after Step 5b.
- [ ] In `MicVoiceIngestDebugAggregate.cs`, delete `aggMicLastUnreadComputed` and `aggMicLastWriteHeadStallFrameCount`. No source after Step 5b.
- [ ] Sweep the aggregate for any other field that mirrored a removed legacy field (gentle recovery counters, double-poll fields, etc.) and delete each.
- [ ] In `ImitoneVoiceIntepreter.cs`, audit any `debugMic*` field carried over from the legacy structure; delete the ones no longer applicable. (Mostly addressed in Step 5b but verify nothing was missed.)
- [ ] In `DirectVoiceMonitoring.cs`, **leave self-concern fields alone** (monitoring underflow / starvation / gain / clip state — useful when debugging that file in isolation). Remove only fields now duplicated by the aggregate's audio-thread health section.

**Sub-step 6b — Finalize aggregate sections and the field set:**

- [ ] Reorder / rename `[Header(...)]` blocks in `MicVoiceIngestDebugAggregate` to match the layout below.
- [ ] Add any aggregate fields from the layout below that don't yet exist.
- [ ] Verify the full Inspector layout walking top-to-bottom matches the spec exactly.

The aggregate Inspector should end up organized like this (all `[Header(...)]` blocks, in the listed order):

*Header: "FAIL OBSERVATION (glance here first)"* — **always first; the user's primary categorical observability anchor.**

| Field | Type | Healthy reading | Broken reading |
|----|----|----|----|
| `FAILURE` | bool | Stays `false` | `true` if any subsidiary flag is `true` |
| `FAIL_AUDIO_CALLBACK_FROZEN` | bool | `false` | `true` — audio thread stopped firing |
| `FAIL_AUDIO_CALLBACK_RATE_LOW` | bool | `false` | `true` — callbacks firing but at degraded rate |
| `FAIL_AUDIO_CALLBACK_GAP_HIGH` | bool | `false` | `true` — long gap between callbacks (jitter) |
| `FAIL_AUDIO_LOCK_CONTENTION` | bool | `false` | `true` — `TryEnter` failures climbing |
| `FAIL_AUDIO_GC_ALLOC_DETECTED` | bool | `false` | `true` (sticky) — allocation suspected on audio thread |
| `FAIL_IMITONE_NOT_FED` | bool | `false` | `true` — audio thread alive but imitone feed broken |
| `FAIL_IMITONE_FEED_RATIO_LOW` | bool | `false` | `true` — some callbacks skipping the imitone feed |
| `FAIL_DB_TEAR_DETECTED` | bool | `false` | `true` (sticky) — `_dbMicrophone` cross-thread tearing; escalate to `Interlocked` |
| `FAIL_RING_OVERFLOW_GROWING` | bool | `false` | `true` — ring read falling behind ring write |
| `FAIL_MIC_NOT_READY` | bool | `false` | `true` — mic device unavailable past startup grace |
| `FAIL_MONITORING_STARVATION_GROWING` | bool | `false` | `true` — `DirectVoiceMonitoring` starvation events climbing (audible clicks likely) |

(Followed by the `[Header("FAIL OBSERVATION — thresholds")]` block of tunable threshold fields and the sticky-flag clear toggle.)

*Header: "Audio thread health"*

| Field | Type | Healthy reading | Broken reading |
|----|----|----|----|
| `aggAudioCallbackTotal` | long | Climbs visibly while scene plays | Frozen for >100 ms |
| `aggAudioCallbackHzRolling` | float (volatile) | Near `sampleRate / samplesPerCallback` (e.g. ~46.9 Hz at 1024 / 48k) | Drops below ~75% of expected, or jitters wildly |
| `aggAudioCallbackMaxGapMsLastSecond` | float (volatile) | Near nominal buffer time (~21 ms at 1024 / 48k) | Spikes >2× nominal — audio thread starvation or DSP overload |
| `aggAudioCallbackLastSamplesPerCallback` | int (volatile) | Stable value matching DSP buffer size | Variable (rare; typically a config event) |
| `aggAudioCallbackLockMissTotal` | long | Stays near zero (well under 1% of `aggAudioCallbackTotal`) | Climbs continuously — main-thread lock contention |
| `aggAudioCallbackGCAllocSuspectTotal` | long | Stays at 0 | Any nonzero — investigate; profile the audio thread |
| `aggMicClipChannels` | int (set once at startup) | Matches the user's mic device (typically 1) | — (informational) |
| `aggMixerChannels` | int (volatile) | Matches the user's audio config (typically 2 on Windows desktop); stable | Changes mid-session — unusual; investigate |

*Header: "Ring buffer flow"*

| Field | Type | Healthy reading | Broken reading |
|----|----|----|----|
| `aggMicRawRingWriteTotalSamples` | long | Climbs at sample rate (~48000 / sec) | Flat while callbacks fire — capture broken (clip not advancing or write code broken) |
| `aggMicNormRingWriteTotalSamples` | long | Climbs alongside raw ring | Flat while raw climbs — normalization path broken |
| `aggMicRingReadTotalSamplesByMonitoring` | long | Climbs alongside writes | Flat — monitoring not consuming (probable click source) |
| `aggMicRingReadTotalSamplesByImitone` | long (or implicit via callback total) | 1:1 with writes | Lags writes — imitone not being fed |
| `aggMicRingOverflowSkipTotal` | long | Stays at 0 (well-sized ring) | Climbs — ring undersized or read is stalled |

*Header: "Imitone feed"*

| Field | Type | Healthy reading | Broken reading |
|----|----|----|----|
| `aggImitoneInputAudioCallTotal` | long (audio thread, `Interlocked.Increment`) | Tracks `aggAudioCallbackTotal` 1:1 | Lags callbacks — feed path is dropping callbacks |
| `aggImitoneGetStateCallTotal` | long (main thread) | Climbs once per `Update()` | Flat — main-thread frozen, or `Update` stopped running |
| `aggImitoneInputToCallbackRatio` | float (computed) | ~1.0 | <1.0 — InputAudio is being skipped on some callbacks |
| `aggMainThreadFramesSinceLastImitoneStateChange` | int | Rarely exceeds 2-3 frames during voicing | Stays high during sustained voicing — pitch tracker not advancing |

*Header: "Cross-thread atomicity"*

| Field | Type | Healthy reading | Broken reading |
|----|----|----|----|
| `aggDbMicrophoneSnapshot` | float | Smoothly varying with voice | Spikes to impossible values like `1e30f` or `NaN` |
| `aggDbMicrophoneTearDetectedTotal` | long | Stays at 0 | Any nonzero — escalate `_dbMicrophone` to `Interlocked` |
| `aggCrossThreadFieldsUsingVolatile` | string label | Lists fields under `volatile` mitigation | — |
| `aggCrossThreadFieldsUsingInterlocked` | string label | Lists fields under `Interlocked` mitigation | — |

*Header: "Interpreter / game logic"*

(Keep the relevant `toneActive` / pitch / dB read-only mirrors here for one-glance debugging. Source of truth remains in `ImitoneVoiceIntepreter`.)

| Field | Type | Healthy reading | Broken reading |
|----|----|----|----|
| `aggToneActive` | bool (mirror) | Tracks user voice on/off | Stuck while user is voicing — or never resets when user stops |
| `aggPitchHz` | float (mirror) | Updates smoothly during voicing | Stuck — likely upstream feed issue, check Imitone feed section |
| `aggDbMicrophone` | float (mirror) | Tracks voice intensity | Stuck — ditto |
| `aggToneActiveTrueCount` / `aggToneActiveFalseCount` | long (rolling counters) | Both grow during a normal session | One frozen — toning detection biased |

*Header: "Monitoring (cumulative; consumed from `DirectVoiceMonitoring`)"*

(Read-only mirrors of `DirectVoiceMonitoring`'s self-concern counters, surfaced for cross-cutting context.)

| Field | Type | Healthy reading | Broken reading |
|----|----|----|----|
| `aggMonitoringUnderflowTotal` | long | Stays low (some at startup is OK) | Climbs continuously — capture not feeding ring fast enough |
| `aggMonitoringStarvationTotal` | long | Stays low | Climbs — hard underflow events; clicks likely |
| `aggMonitoringOverflowTotal` | long | Stays low | Climbs — read cursor falling behind write cursor |

**Sub-step 6c — Document the interpretation guide:**

- [ ] Add the layout table above as either a tooltip / `[TextArea]` field at the top of the aggregate Inspector OR a `// MARK: Interpretation` comment block at the top of `MicVoiceIngestDebugAggregate.cs`. Pick one and stay consistent.
- [ ] Verify the user can see "what does healthy vs broken look like for this metric" without leaving the Inspector — i.e., no need to open the source file to remember a healthy threshold.

**Sub-step 6d — Archive the OLD reference file (decision only; not yet executed):**

- [ ] Confirm `Assets/Scripts/Voice/Reference/OLD_ImitoneVoiceInterpreterForDebugComparison.cs` is still present (it is the rollback safety net through Step 7).
- [ ] Defer the actual move to `Assets/Scripts/Voice/Reference/Archive/` until Step 7 passes. Do **not** delete or move it during Step 6.

**Notes & considerations:**
- **Don't strip debug fields aggressively.** The whole point of Step 6 is to make broken-vs-healthy *more* visible than `unread_zero` ever was, not less. Add what's needed; keep what's used.
- **Inspector layout matters.** The user reads these live during dev. Group with `[Header(...)]` attributes per the section list above. Order matters — keep the most diagnostic sections at the top.
- **Don't duplicate.** If a metric ends up in both an individual file and the aggregate, pick one source of truth and have the other reference it (no double-bookkeeping).
- **Verify Project Settings entries before removing any remaining `[DefaultExecutionOrder]` attributes.** Specifically, before removing `[DefaultExecutionOrder(50)]` from `ImitoneVoiceIntepreter`, prompt the user to open Project Settings → Script Execution Order and confirm `ImitoneVoiceIntepreter` has an explicit entry (currently −104). Same for any other class-level attribute removed in this step.

**Test:**
- [ ] Run a normal session. Walk through every Inspector section; every field has a sensible value matching the "Healthy reading" column.
- [ ] Deliberately stress the system (heavy CPU work elsewhere in the scene) and confirm the "Broken reading" patterns are observable when expected (e.g. brief gap-time spikes under heavy load) — and absent during normal operation.
- [ ] Run a long session (5+ min) and confirm no accumulator (overflow / underflow / lock-miss / tear-detected) climbs unexpectedly.
- [ ] Verify FAIL OBSERVATION section is in the canonical layout (top of Inspector, top-level `FAILURE` first, sub-flags grouped by phase).

**Commit:** `chore: finalize aggregate telemetry layout and interpretation guide`

**Developer notes:** _none_

---

### Step 7: Final validation

> **Recommended LLM for this step: Opus 4.7.**
> - **First-pass: Opus 4.7** — final A/B comparison against the Step 0 baseline, analysis of the new aggregate readings, sign-off against success criteria. If anything unexpected surfaces during the 30-minute / 5-minute observation runs, the thinking model is what you want for diagnosing it.
> - **Review pass: Opus 4.7** (always; see working agreement rule 5) — for Step 7 the "review pass" is largely the validation itself, but a final pass over the document and any final code adjustments still applies.
>
> *Confirm Opus 4.7 is selected before continuing. Stay on Opus through this final step.*

Confirm the goal is met: responsive every frame, no jitter, better than the OLD baseline. Use the new aggregate Inspector (Step 6) as the source of truth for "is this healthy?" instead of subjective feel.

**Tasks:**

*Subjective:*
- [ ] Long toning session (10+ min).
- [ ] Heavy toning under load (run other Unity work simultaneously if possible — particle systems, heavy game logic).
- [ ] Subjective check: system feels responsive every frame; pitch tracking feels "alive"; system reliably detects when you stop toning.

*Objective — `MicVoiceIngestDebugAggregate` walkthrough during sustained toning + load:*
- [ ] **FAIL OBSERVATION (hard pass criterion):** `FAILURE` stays `false` for the entire validation session, including under heavy load. Every subsidiary `FAIL_*` flag stays `false` (or, for sticky flags, hasn't been triggered since the session began). If `FAILURE` flips `true` even once, identify which sub-flag caused it, fix the underlying issue, and re-run the entire validation pass.
- [ ] **Audio thread health:** `aggAudioCallbackHzRolling` near nominal; `aggAudioCallbackMaxGapMsLastSecond` near buffer time; `aggAudioCallbackLockMissTotal` near zero; `aggAudioCallbackGCAllocSuspectTotal` at 0.
- [ ] **Ring buffer flow:** all four ring counters climb at sample rate; no flat windows; `aggMicRingOverflowSkipTotal` stays at 0.
- [ ] **Imitone feed:** `aggImitoneInputAudioCallTotal` ≈ `aggAudioCallbackTotal`; `aggImitoneGetStateCallTotal` climbs once per frame; `aggMainThreadFramesSinceLastImitoneStateChange` stays low during voicing.
- [ ] **Cross-thread atomicity:** `aggDbMicrophoneTearDetectedTotal` stays at 0 across the entire session.
- [ ] **Monitoring:** underflow / starvation / overflow totals stay flat (allow brief startup transients).

*Click testing protocol — full pass (all 5 scenarios from the click prevention appendix):*
- [ ] Scenario 1: Quiet baseline (30 s silence) — no periodic clicks.
- [ ] Scenario 2: Sustained tone (30 s steady note) — no clicks at any cadence.
- [ ] Scenario 3: Onset / offset (rapid voice on / off) — no clicks at phonation start / end.
- [ ] Scenario 4: Heavy load (tone + simulated heavy CPU work) — no clicks correlated with CPU spikes.
- [ ] Scenario 5: Long session (5+ min) — no clicks emerging over time.

*A/B vs Step 0 baseline:*
- [ ] Compare against Step 0 baseline recordings. New architecture demonstrably eliminates the multi-second `unread_zero` windows (which no longer exist as a category) and produces responsive toning detection where the baseline produced jitter / stuck pitch.
- [ ] Where `OLD_ImitoneVoiceInterpreterForDebugComparison.cs` is usable as a fallback, A/B against it: the new architecture must be **at least as good** during normal use and **demonstrably better** during heavy toning.

*Profiler:*
- [ ] No GC allocations attributable to the audio thread.
- [ ] No main-thread spikes attributable to voice code.

*Reference file decision (deferred from Step 6d):*
- [ ] Confirm `OLD_ImitoneVoiceInterpreterForDebugComparison.cs` remains in place. **Do not delete** even after validation passes — the user may want it as a long-term reference.
- [ ] If (and only if) the user explicitly says so, move it to `Assets/Scripts/Voice/Reference/Archive/` and update its `.meta`. Otherwise leave it.

**Notes & considerations:**
- This is final validation. Issues at this stage are typically subtle and require careful instrumentation rather than obvious code fixes. Use the aggregate's "Broken reading" patterns from Step 6 as the diagnostic starting point.
- The aggregate's FAIL OBSERVATION block is the canonical pass / fail oracle; treat its `FAILURE = false` invariant as the single non-negotiable pass criterion.

**Test:** Recorded comparison session with screen + audio + aggregate Inspector capture. The aggregate fields should be visible in the recording.

**Commit:** `chore: final validation of voice rearchitecture`

**Developer notes:** _none_

---

## Notes for the Composer agent

(Process rules — when to ask permission, review-pass protocol, living-doc protocol, etc. — live in the **"AI pair programmer instructions"** section near the top of the document. The notes below are the *technical reminders* that complement those process rules.)

A few cross-cutting reminders that apply throughout implementation:

- **The user is not a DSP expert.** When asking clarifying questions, frame them in terms of observable behavior or architecture decisions, not in DSP jargon.
- **Preserve naming and inspector references.** The class identifier on disk is `ImitoneVoiceIntepreter` (typo intact); see the "Filename and class-name pitfall" section near the top. Do not rename. Other public-facing field names should be preserved unless renaming is genuinely needed.
- **Test after every step. Don't batch.** The user has explicitly committed to a test/commit cycle per step. Respect that cadence even for "small" changes. Step 0.7 has explicit sub-pass boundaries (0.7a → 0.7b → 0.7c → 0.7d), with 0.7c itself split internally into three passes (0.7c-i → 0.7c-ii → 0.7c-iii); Step 5 has two sub-passes (5a → 5b). Honor every boundary.
- **The biggest risk is breaking something during Step 0.7 (the merge).** Take that step slowly. Compile and run after every sub-pass, not just at the end of the step. Commit between sub-passes — the recent rollback incident is a reminder that uncommitted refactor work is fragile.
- **Threading bugs are worse than functional bugs.** A dropped sample is invisible. A race condition is a Heisenbug. When in doubt about thread safety, default to the safer pattern (more locking, more `volatile`, more `Interlocked`) and optimize only if profiling shows it matters. The `aggDbMicrophoneTearDetectedTotal` counter (V7 / Step 6) is your canary; watch it.
- **Firm rule: never call `Microphone.*` or `AudioClip.GetData` from the audio thread.** Position is tracked via `_samplesWritten += data.Length / channels`. Mic samples come from the `data[]` parameter of `OnAudioFilterRead`. The mic clip is played through the AudioSource so Unity itself does the resampling. (V3 — non-negotiable.)
- **The OLD file is the safety net.** If something goes catastrophically wrong, `OLD_ImitoneVoiceInterpreterForDebugComparison.cs` is a known-working reference for the legacy behavior. Keep it through Step 7. Move to `Reference/Archive/` only if the user explicitly says so.
- **Prompt the user before removing `[DefaultExecutionOrder]` attributes.** Always have them open Project Settings → Script Execution Order and confirm an explicit entry exists for the class first. Don't silently drop the attribute.

---

## Success criteria

The rearchitecture is successful when:

1. Imitone-driven analysis (`pitch_hz`, `toneActive`, `_dbValue`) updates every render frame with current data.
2. Toning feels responsive every frame, not "tolerable but jittery."
3. **Audio monitoring is free of audible clicks, pops, and distortion across all conditions** — including sustained toning, heavy CPU load, sudden volume changes, startup, capture changes, and long sessions. This was the original symptom that surfaced the deeper jitter issue; both must be resolved.
4. No audio thread GC allocations (verify in profiler).
5. No main-thread spikes from voice code.
6. `DirectVoiceMonitoring` produces clean, continuous monitoring audio with stable latency.
7. The system performs at least as well as `OLD_ImitoneVoiceInterpreterForDebugComparison.cs` did before the refactor — and demonstrably better during heavy toning.
8. **`MicVoiceIngestDebugAggregate.FAILURE` stays `false`** through a full validation session (Step 7), including under heavy load. Every Phase 2 / Phase 3 / surviving Phase 1 subsidiary `FAIL_*` flag also stays `false` (or, for sticky flags, hasn't been triggered since the session began).

Anything less than this isn't done.

---

## Appendix: click and pop prevention

The original symptom was audible clicking in monitoring. Several places in this plan could introduce or worsen clicks if not handled carefully. Clicks come from sample-value discontinuities in the output waveform — anywhere two buffers get joined with a value mismatch at the seam.

### Click sources to watch for

**S1: Underflow → silence fill.** When `OnAudioFilterRead` can't get enough samples from the ring, it fills the rest with zeros. The transition from non-zero to zero (and back) produces clicks. Existing `DirectVoiceMonitoring` does this on `copied < frameCount`. Audio-thread capture should make underflow rare-to-impossible, but the fallback path remains.

**S2: Lock contention silence fill.** `ReadNormalizedSamples` clears the buffer to silence on `Monitor.TryEnter` failure. Same click pattern as S1, triggered by thread collision instead of timing.

**S3: Partial-write race.** If the audio thread reads while the producer is mid-write without proper locking, garbage samples appear in the output = loud click. The existing `lock (rawBufferLock)` pattern prevents this and must be preserved.

**S4: AudioSource clip reassignment.** Reassigning `monitoringSource.clip` mid-playback can click. Currently happens on capture restart. Audio-thread capture should remove these restart events; verify no remaining paths reassign the clip during playback.

**S5: Filter state reset.** If HPF/LPF internal state gets cleared mid-stream, the filter output jumps and rings briefly. Happens currently on config changes.

**S6: Sample skip on overflow.** If the ring overflows and the read cursor jumps forward by `skipSamples`, the discontinuity is a click. Audio-thread capture should make overflow rare; verify under load.

**S7: Inconsistent read/write ordering.** With both capture (write) and monitoring (read) on the audio thread, if their execution order varies callback-to-callback, monitoring latency jitters = clicks. Constant latency is fine; variable latency is not.

**S8: Resampler boundary artifacts.** If `Microphone.Start` is called with a `frequency` that does not match `AudioSettings.outputSampleRate`, Unity's audio graph runs an internal resampler at the AudioSource boundary. Resampler boundaries are a well-known click source on Unity 2022.3, especially under varying CPU load. (See research note that informed V3/V5/V10.)

**S9: Cross-timeline position arithmetic.** Calling `Microphone.GetPosition` from the audio thread returns a position in the mic's source sample timeline, while `OnAudioFilterRead`'s `data[]` is in the mixer's resampled timeline. Mixing positions across these timelines for any indexing / boundary calculation produces drift and seam artifacts that manifest as clicks. (Don't do this — V3.)

### Mitigations

**M1: Click-free underflow handling.** Instead of filling with absolute zero, fill with the last-good sample value (DC hold) or fade quickly to zero over a few samples. A linear fade over 32 samples (~0.7ms at 48kHz) is inaudible but eliminates the click. Apply both to underflow fill AND to lock-contention fallback.

**M2: Click-free overflow handling.** When sample skipping is necessary, crossfade between old and new positions over a small window rather than hard-cutting.

**M3: Deterministic execution order on audio thread.** Both capture and monitoring run on the audio thread. To guarantee write-before-read every callback, the chosen approach (per V5) is **separate GameObjects** (one AudioSource each, no chain ambiguity) **plus Project Settings → Script Execution Order** with `ImitoneVoiceIntepreter` (−104) ahead of `DirectVoiceMonitoring` (−102). Unity fires `OnAudioFilterRead` callbacks across MonoBehaviours in script-execution-order regardless of GameObject co-location, so co-location is unnecessary and would in fact reintroduce a multi-AudioSource chain-routing ambiguity. Co-location is *not* the mitigation; script execution order is.

**M4: Filter state preserved across moves.** When relocating filter logic between threads (Step 3), ensure filter state fields are not reset to zero. Move state with the logic.

**M5: Avoid clip reassignment in steady state.** Audit `ConfigureMonitoringSourceClip` calls. After Step 5a, `monitoringSource.clip` should be assigned exactly once at startup and never reassigned during a session.

**M6: Smooth gain changes.** If `effectiveMonitoringGain` changes mid-callback (e.g., user adjusts volume), interpolate across the buffer rather than applying a step change. The existing `monitoringVolume × dynamicScale` chain may already do this; verify.

**M7: Mixer-rate mic capture (no resampler).** Always call `Microphone.Start(deviceName, true, lengthSec, AudioSettings.outputSampleRate)`. Matching rates makes Unity's resampler a no-op and removes S8 entirely as a class of click. Read `AudioSettings.outputSampleRate` once on the main thread (it's effectively immutable at runtime).

**M8: Use `volume = 0f`, not `mute = true`, on the capture AudioSource.** `mute = true` disables `OnAudioFilterRead` invocation on that source — the entire capture path goes silent without warning. `volume = 0f` plus `bypassEffects = true` keeps the callback firing while silencing the bus output. (V5 critical gotcha.)

**M9: Skip the first few callbacks at startup.** FMOD's record buffer often returns silence or partial garbage for the first 4–8 callbacks while the device primes. Gate the imitone feed (and noise-floor calibration if applicable) until the priming window has passed. The ring can still be written during priming — only the analyzer-facing path is gated.

### Click testing protocol

After each implementation step, do a click check:

1. **Quiet baseline:** silence for 30 seconds in monitoring. Listen for periodic clicks (indicates timer-based artifact, e.g., capture restart).
2. **Sustained tone:** hold a steady note for 30 seconds. Listen for clicks at any cadence (indicates buffer boundary issue).
3. **Onset/offset:** rapid voice on/off. Listen for clicks at the start and end of phonation (indicates filter state or gate boundary issue).
4. **Heavy load:** tone while running other heavy work in scene. Listen for clicks correlated with CPU spikes (indicates lock contention or underflow).
5. **Long session:** tone for 5+ minutes. Listen for clicks that emerge over time (indicates accumulated drift or overflow).

If clicks appear, isolate by toggling code paths off (e.g., disable filters, disable normalization) until the offending path is identified.

### What this means for each step

- **Step 0.7 (merge):** Behavior-preserving refactor. Click profile after Step 0.7 should match pre-Step-0.7 exactly. New clicks here indicate something was lost in translation during the merge.
- **Steps 1-2:** Audio-thread path is observing only; legacy main-thread block (now inside the merged file) still drives imitone. Don't change DirectVoiceMonitoring. The click profile should remain at baseline level.
- **Step 3:** Moving imitone feed to audio thread shouldn't affect monitoring path. If new clicks appear here, it indicates the audio thread is doing too much work in the callback or filter state moved incorrectly.
- **Step 4:** Run the click testing protocol explicitly before moving on. Don't skip.
- **Step 5a:** Hardens the monitoring path against clicks (M1/M2/M5/M6). Run the click testing protocol thoroughly — this is where audible underflow / overflow / gain-step clicks should *disappear*.
- **Step 5b:** Deletes the legacy main-thread mic-ingest block. Lower click risk than Step 5a since the audio-thread path has already been carrying the load since Step 3, but verify with the protocol anyway.
- **Step 7:** Final validation must include all 5 click test scenarios passing cleanly.

---

## Appendix: Voice-onset latency lever inventory

Comprehensive list of every knob, tunable, threshold, architectural choice, and "non-lever" affecting the perceived time between voice onset and `toneActive` flipping (and the visualizer / Wwise event responding). Compiled 2026-05-06 after the Step 3a F1 hybrid ring-feed pivot closed: F1 dropped imitone-feed latency from ~2100 ms (engine-imposed) to ~150 ms (mic ADC + audio-thread feed cursor + analysis), so what remains is the sum of everything *else* in the chain. **Use this during Step 4 / Step 5b subjective tuning** to walk the chain stage-by-stage rather than guessing.

**End-to-end path:** voice → mic ADC → driver → Unity Microphone clip → *(legacy main-thread polling now / audio-thread direct in Step 5b)* → `rawRingBuffer` → audio-thread feed cursor → pre-imitone filtering → `imitone.InputAudio` → imitone analysis → main-thread `imitone.GetState` → `imitoneActiveRaw`/`_dbValue`/`pitch_hz` → noise-floor gate → debounce → `toneActive` → game logic → Wwise/visuals.

**Total perceived lag is the sum across stages.** Each lever below is tagged with **scope** indicating when it's available: `[Now]` = a knob you can turn today; `[Step 5b]` = unlocks after the legacy main-thread mic-ingest block is deleted; `[Project setting]` = Editor / Project Settings change, no code; `[Architectural]` = significant work; `[Imitone-internal]` = opaque to us, would require reading imitone DLL or its docs; `[Out-of-scope]` = listed for completeness but not actionable here.

### Stage 1 — Mic hardware / OS driver (~5–30 ms)

| Lever | Current | Tradeoff | Scope |
|---|---|---|---|
| Mic device choice (built-in vs USB vs Bluetooth) | Default device | USB and built-in typically lowest latency; Bluetooth often 100–300 ms | `[Now]` (player choice / instructions) |
| Windows audio service path (WASAPI shared / WASAPI exclusive / WDM-KS / ASIO) | WASAPI shared (Unity default) | Exclusive / ASIO can drop driver buffer ~10–30 ms but requires user to configure outside Unity | `[Out-of-scope]` |
| Mic-side OS sample-rate / buffer-size settings | OS default | Driver-buffer depth is the main adder here | `[Out-of-scope]` |

### Stage 2 — Unity Microphone API

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| `micCaptureSampleRate` | `48000` | `ImitoneVoiceIntepreter.MicIngest.cs:15` | Match to `outputSampleRate` to keep Unity's resampler a no-op (we already do). Higher rates would increase compute without analysis benefit at human voice frequencies | `[Now]` (don't touch unless output rate changes) |
| `loopLengthSeconds` | `6` | `ImitoneVoiceIntepreter.MicIngest.cs:16` | Length of `microphoneBuffer` clip. Doesn't affect steady-state latency; affects max stall window before recovery | `[Now]` |
| `microphoneDeviceName` (selected device) | First available | Set in `MicIngestInitialize` | Same as Stage 1 device choice | `[Now]` |

### Stage 3 — Main-thread legacy mic-ingest jitter (the `unread_zero` source)

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| **Step 5b: delete legacy mic ingest entirely** | Pending | This doc § Step 5 | Audio thread becomes the writer of `rawRingBuffer` (replaces main-thread poll). **Eliminates `unread_zero` as a concept.** Removes the `_dbMicrophone`-via-legacy-path lag (see Stage 9). | `[Step 5b]` |
| Frame rate / Update tick cadence | Variable | (engine) | Higher fps → smaller `unread_zero` windows. Up to 64 ms windows are hidden by the audio-thread headroom; beyond that, imitone gets stale samples | `[Now]` (project-wide perf) |
| `gentleUnreadZeroRecoveryEnabled` | `false` | `ImitoneVoiceIntepreter.MicIngest.cs:26` | Sustained-`unread_zero` recovery; left for experiments. Not a latency lever per se; recovery, not steady state. Slated for deletion in Step 5b (see [Appendix: provisional code to delete](#appendix-provisional-code-to-delete-cleanup-checklist)) | `[Now]` (default off, leave) |

### Stage 4 — Audio-thread feed cursor headroom (post-F1)

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| `audioThreadFeedLatencyMs` | `64f` (`[Range(32f, 250f)]`) | `ImitoneVoiceIntepreter.AudioThread.cs:26` | Direct latency knob — every ms shaved here is a ms shaved off the total. **Floor `~32 ms` = one DSP callback period at 1024/48 k.** Below 64 ms eats the safety margin against clock skew + main-thread polling jitter; expect `audioFeedOverflowDroppedTotal` to start climbing in long sessions if too tight. After Step 5b the floor relaxes (no main-thread jitter) | `[Now]` |
| `maxRealtimeLagSamples` overflow guard | `Mathf.Max(destination.Length * 2, micCaptureSampleRate * 0.25f)` (≈ 250 ms) | `ImitoneVoiceIntepreter.MicIngest.cs:497, 593` | Upper bound where `ReadRawSamples` drops samples to clamp lag. Lower → faster bounded lag but more drop events; higher → silent drift to whatever value before drop fires | `[Now]` (rarely touched) |

### Stage 5 — Engine DSP cadence (Project Settings → Audio) — biggest single knob

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| **`dspBufferSize` / Audio latency preset** | "Best Latency" / "Good Latency" / "Best Performance" — confirm in Project Settings | Project Settings → Audio → DSP Buffer Size | Lower → callbacks fire more often → cursor advances in smaller steps → freshness improves; **also** drops the floor on `audioThreadFeedLatencyMs` (the floor is one callback period). At 256 samples / 48 k → ~5 ms callback period. Costs: more callbacks/sec, more CPU, more lock-acquisition overhead, less data per imitone analysis tick (potential pitch-resolution impact). **Test in conjunction with imitone behavior at small buffer sizes.** | `[Project setting]` |
| `outputSampleRate` | `48000` | Project Settings → Audio → System Sample Rate | Halving sample rate halves callback period in time units but also halves analysis resolution. 48 k is the sweet spot for human voice. Don't change | `[Project setting]` (don't change) |
| `numRealVoices`, `speakerMode` | (defaults) | Project Settings → Audio | Not voice-onset latency levers; mixer-side | `[Out-of-scope]` |

### Stage 6 — Pre-imitone filtering (audio thread)

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| `_highPassFilterEnabled` | `true` | `ImitoneVoiceIntepreter.cs:316` | First-order RC HPF → group delay ≈ 1–3 samples (negligible at 48 k). Not a meaningful latency lever, but disabling avoids any phase distortion | `[Now]` (leave on) |
| `_highPassCutoffHz` | `80f` | `ImitoneVoiceIntepreter.cs:317` | Cutoff frequency. Higher → more rumble removed, more group delay around the cutoff. Voice-fundamental relevant range. | `[Now]` (perceptual tune) |
| `_lowPassFilterEnabled` | `true` | `ImitoneVoiceIntepreter.cs:323` | Same — first-order LPF, negligible delay | `[Now]` (leave on) |
| `_lowPassCutoffHz` | `520f` | `ImitoneVoiceIntepreter.cs:324` | Affects which harmonics make it to imitone; not really a latency lever, but pitch-detection responsiveness can change with harmonic content | `[Now]` (perceptual tune) |

### Stage 7 — Imitone analysis (opaque)

| Lever | Current | Tradeoff | Scope |
|---|---|---|---|
| Imitone's internal FFT window size / pitch-detector lock-in | Opaque | Smaller analysis windows → faster pitch lock-on but lower frequency resolution | `[Imitone-internal]` |
| Imitone's internal power threshold (when `state.power > 0` is reported) | Opaque | Sets the floor below which `imitoneActiveRaw` won't flip on | `[Imitone-internal]` |
| imitone-side sensitivity / config (if such an API exists) | Unknown | Worth one read of `imitone.cs` / its docs to confirm before subjective tuning starts | `[Investigate]` (small task) |

### Stage 8 — `imitoneActiveRaw` / `_dbValue` / `pitch_hz` flip (main thread)

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| `imitone.GetState()` polling cadence | Once / Update | `ImitoneVoiceIntepreter.cs:786` | Higher fps → fresher state read into `_dbValue`/`pitch_hz`. Bounded by frame rate | `[Now]` (project-wide perf) |
| `forceImitoneActive` / `forceImitoneInactive` | Both `false` | `ImitoneVoiceIntepreter.cs:284` | Debug-only overrides; not real levers for production tuning | `[Out-of-scope]` |

### Stage 9 — Noise-floor gate (the second hidden lag source)

The gate at `ImitoneVoiceIntepreter.cs:818-819`: `imitoneActive = gameOn && !micIsNearNoiseFloor`, where `micIsNearNoiseFloor = _dbMicrophone <= _noiseFloorThreshold`. **Critical:** `_dbMicrophone` is computed by the legacy main-thread mic-ingest path. As long as that path is alive (pre-Step-5b), `_dbMicrophone` carries the same `unread_zero` jitter that affects the legacy path generally — and **it directly gates `imitoneActive`**, even though the imitone power detection path is now audio-thread-fed and fast. This is the subtle reason Step 3a F1 alone can't fully eliminate the lock-on lag.

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| `_noiseFloorThreshold` | `-52f` dB | `ImitoneVoiceIntepreter.cs:144` | Higher (e.g., -45 dB) → mic noise floor must be louder to gate, so more soft-voice rejection; lower (e.g., -60 dB) → softer voice flips `imitoneActive` faster, but ambient hum can also flip it | `[Now]` (perceptual tune) |
| `_noiseFloorMeasurementTime` | `1.5f` s | `ImitoneVoiceIntepreter.cs:140` | Calibration window length. Affects startup, not steady-state latency | `[Now]` (don't touch unless calibration feels off) |
| **Step 5b: move `_dbMicrophone` compute to audio thread** | Pending | This doc § Step 5 | Removes the legacy-path jitter from the noise-floor gate. Currently the largest remaining latency source post-F1 if you measure `imitoneActive` flip time | `[Step 5b]` |

### Stage 10 — Tone-active debouncing (the most direct knob)

These thresholds operate on `_imitoneActiveTimer` (accumulating frame time while `imitoneActiveRaw` is true).

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| `positiveActiveThreshold1` (→ `toneActive`) | `0.05f` s | `ImitoneVoiceIntepreter.cs:59, 922` | Lower → `toneActive` flips faster on voice onset (best ~ one DSP callback period, ~21 ms); higher → more false-positive rejection of transients (cough, mic bump) | `[Now]` |
| `positiveActiveThreshold2` (→ `toneActiveConfident`) | `0.2f` s | `ImitoneVoiceIntepreter.cs:60, 929` | Same tradeoff at the "confident" tier (used by GameValues / RespirationTracker for game-logic flips) | `[Now]` |
| `negativeActiveThreshold1` (→ `toneActive` release) | `0.2f` s | `ImitoneVoiceIntepreter.cs:61, 940` | Lower → `toneActive` falls faster after voice stops (perceptual: less "trailing" lock); higher → smoother visualizer during brief breath gaps | `[Now]` |
| `negativeActiveThreshold2` (→ `toneActiveConfident` release) | `0.4f` s | `ImitoneVoiceIntepreter.cs:62, 945` | Same | `[Now]` |
| `_activeThreshold3` (→ `toneActiveVeryConfident`) | `0.75f` s | `ImitoneVoiceIntepreter.cs:63` | Used for respiration rate, not perceptual lock-on. Tune separately if respiration scoring needs it | `[Now]` (respiration domain) |

### Stage 11 — Game-side response

| Lever | Current | Where | Tradeoff | Scope |
|---|---|---|---|---|
| `GameValues` / `RespirationTracker` consumption cadence | Per Update | `Assets/Scripts/Voice/GameValues.cs`, `RespirationTracker.cs` | Reads `toneActive*` once per frame. Frame-rate bounded; no extra layer added here | `[Now]` (frame-rate dependent) |
| Wwise event scheduling on `toneActive` flips | Engine-side | `MusicSystem*` / various | Wwise has internal event-scheduling latency; varies by Wwise project config. Worth measuring once if the perceptual lag survives all upstream tuning | `[Investigate]` |
| Visual smoothing / lerp constants on the visualizer reading `toneActive`/`pitch_hz` | Various | Visual scripts | Any `Lerp(...)` or `MoveTowards(...)` in the visual response chain adds perceived lag independent of the analysis stack. Audit whichever visualizer is in scope when tuning | `[Now]` (per-visualizer) |

### Stage 12 — Cross-cutting / rendering

| Lever | Current | Tradeoff | Scope |
|---|---|---|---|
| Frame rate (project-wide perf budget) | Variable | Higher fps → smaller Update jitter, fresher `imitone.GetState` polling, smaller visual frame lag | `[Now]` (project-wide) |
| VSync / monitor refresh | Engine config | Affects visual response only; ~1 frame at most | `[Out-of-scope]` |

### Architectural / out-of-current-scope (kept for completeness)

| Lever | Tradeoff | Scope |
|---|---|---|
| Unity 6.x upgrade | Audio internals changed; F1's exact 100-DSP-buffer engine policy may not apply, allowing different latency floors. Re-validation required on any audio assertion in this doc | `[Architectural]` |
| Native plugin / direct OS mic capture (bypass `Microphone` API) | Full control over driver buffer depth; would need a Windows + Mac native side. Significant build/maintenance cost | `[Architectural]` |
| Predictive activation (anticipate tone onset from earlier signal) | Could shave debounce latency by predicting onset before threshold cross. Highly experimental | `[Architectural]` |

### Non-levers (listed so they don't get re-debated)

- **Engine DSP callback rate itself** — fixed = `outputSampleRate / dspBufferSize`. Adjust via Stage 5 levers.
- **Imitone compute time per `InputAudio` call** — fixed for given input size. Constant compute, not a lever.
- **Per-callback scratch buffers (`monoScratch`/`imitoneFeedBuffer`)** — Step 3a investigation already confirmed these don't accumulate latency. Pass 3 deleted `monoScratch` entirely; only `imitoneFeedBuffer` remains and is sized to one DSP callback.
- **`captureSource.timeSamples` / mic-clip read alignment** — Step 3a F1 investigation confirmed the audio engine ignores user assignments for streaming clips. The F1 pivot eliminated this as a relevant lever entirely.

### Tuning priority — where to look first

If the perceptual lock-on is too slow (post-F1), the levers in rough priority order of "easy to turn AND likely to matter":

1. **Stage 10 thresholds** (`positiveActiveThreshold1`/`2`, `negativeActiveThreshold1`/`2`) — most direct, code-only, no architectural risk.
2. **Stage 9 `_noiseFloorThreshold`** — direct, gates `imitoneActive`, code-only.
3. **Stage 5 `dspBufferSize`** (Project Settings audio latency preset) — single biggest knob; affects multiple downstream stages.
4. **Stage 4 `audioThreadFeedLatencyMs`** — direct knob; needs Step 5b first to be safely tightened below ~50 ms.
5. **Stage 6 filter cutoffs** — minimal latency impact, but pitch-detection feel is sensitive to harmonic content.
6. **Step 5b** — unlocks the two "post-Step-5b" levers above; doubles as the structural removal of `unread_zero`-induced gate jitter (Stage 9).

After exhausting 1–6: investigate Stage 7 (imitone internals), then consider Stage 11 (Wwise / visualizer smoothing).

---

## Appendix: provisional code to delete (cleanup checklist)

The previous investigation added several "provisional" mechanisms inside `MicPipeline.cs` that exist **only** to mitigate `unread_zero` from the main-thread architecture. The audio-thread architecture eliminates the entire `unread_zero` category, so all of these should be deleted cleanly during **Step 5b** (no commented-out leftovers, no toggled-off feature flags). After Step 0.7 these symbols live inside `ImitoneVoiceIntepreter.cs` (the merged file) — the symbol names below still apply, just in their new location. This list is exhaustive based on the codebase as of the start of this rearchitecture.

### Gentle `unread_zero` recovery family — DELETE entirely

All of the following originally lived in `Assets/Scripts/Voice/MicPipeline.cs`; after Step 0.7c they live inside `ImitoneVoiceIntepreter.cs` (the merged file). They exist solely to detect long `unread_zero` streaks and call `Microphone.End` / re-`Start`. The mechanism was never proved useful and is incompatible with the new architecture.

**Inspector / serialized fields (delete):**

- `gentleUnreadZeroRecoveryEnabled`
- `gentleUnreadZeroConsecutiveFramesThreshold`
- `gentleUnreadZeroRecoveryCooldownSeconds`
- `gentleUnreadZeroStallSuppressFramesFromHard`
- `gentleUnreadZeroWallClockSeconds`
- `gentleUnreadZeroWallMinConsecutiveFrames`
- `gentleUnreadZeroBypassStallSuppressionAfterFrames`

**Internal state fields (delete):**

- `consecutiveUnreadZeroFrames`
- `unreadZeroStreakWallStartUnscaled`
- `lastGentleUnreadZeroRecoveryUnscaledTime`
- `debugGentleUnreadZeroConsecutiveFrames`
- `debugGentleUnreadZeroRecoveryCount`

**Methods / branches (delete):**

- `PerformGentleUnreadZeroCaptureRestart()`
- The `unread_zero_gentle_restart` branch and its `debugMicLastExitReason = "unread_zero_gentle_restart"` write
- All call sites inside `UpdateMicReadFrame` that increment / reset / gate the gentle-recovery counters

**Snapshot fields in `MicIngestDebugSnapshot` (delete):**

- `gentleUnreadZeroConsecutiveFrames`
- `gentleUnreadZeroRecoveryTotal`
- `gentleUnreadZeroRecoveryEnabled`

### Main-thread polling helpers — DELETE (no longer applicable)

Audio-thread capture does not consult `Microphone.GetPosition` per `Update()`, so the bookmark / double-poll machinery is dead.

- `micWriteHeadDoublePoll` field and the second `GetPosition` call it gates.
- `micPosRead` / `micPosWrite` bookmark math — replaced by audio-thread sample tracking (see Step 1, Option B in V3).
- `debugMicLastExitReason` enum strings that no longer apply: `not_ready`, `device_unavailable`, `invalid_mic_position`, `stalled_capture_stopped`, `unread_zero`, `unread_zero_gentle_restart`. Replace with audio-thread-relevant exit reasons (e.g. `callback_no_lock`, `callback_clip_unset`, `copied_samples`) or remove the field entirely.
- `stalledWriteHeadFrameCount` / `stalledWriteHeadFrameThreshold` and any stall-detection logic that observes a frozen `GetPosition` head.

### Keep through the rearchitecture (DO NOT DELETE)

These are still useful and should survive Step 0.7 / Step 5b / Step 6, possibly relocated:

- `MicVoiceIngestDebugAggregate` (the unified Inspector panel) — preserve the GameObject and update field references.
- The aggregate metrics `aggMicRawRingWriteTotalSamples` and `aggMicNormRingWriteTotalSamples` — these are the canonical "is the ring still being fed" signals and remain meaningful in the audio-thread architecture.
- The ring buffer infrastructure itself (raw + normalized rings, `ReadRawSamples` / `ReadNormalizedSamples`, lock pattern with `Monitor.TryEnter(lock, 0)`).
- `OLD_ImitoneVoiceInterpreterForDebugComparison.cs` — keep until Step 7 passes; it is the legacy-behavior safety net.

---

## Appendix: what we did not prove (carry-forward skepticism)

These open questions from the prior investigation remain unsettled by the diagnosis alone. Implementation should not silently assume them away.

- **Multi-second `unread_zero` is not fully explained by main-thread CPU spikes alone.** Profiler captures showed `MicPipeline.Update` at ~0.09 ms on ~9.8 ms frames during stuck spells — i.e. the mic update itself is not the spike sink. The diagnosis (jitter elsewhere on the main thread starves capture) is plausible but was not isolated to a single mechanism. Step 4 / Step 7 validation should still A/B against the prior baseline values rather than accept "feels better" alone.
- **The OLD vs new toning regression** (user reported the new split stack made toning worse than the OLD monolithic interpreter, despite identical `% clipSamples` math) was not isolated to a specific call-graph difference. If the rearchitecture eliminates the regression, that's the proof; if it doesn't, the regression mechanism still needs identification.
- **Hardware was not ruled out as a contributor** — only ruled out as the sole cause. Symptoms persisted across reboot and across Bluetooth-vs-wired, but a different USB device or different Windows audio driver path has not been compared.
- **The perceptual / instrumentation gap** (user hears live voice while ingest reports `unread_zero`) is explained by the diagnosis as "monitoring reads from the ring while ingest's bookmark stalls," but a single timestamped capture correlating monitor output to ring-write totals during a stuck spell was never recorded. If a stuck spell reproduces during Step 0 baselining, take that capture — it strengthens the proof and serves as a reference for Step 7.