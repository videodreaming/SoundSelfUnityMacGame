# Mic / voice ingest — findings log

Concise reference for debugging **stuck analysis** vs **what you hear**. Update this doc as new evidence lands.

---

## Plain English (no DSP jargon)

**What the game does:** Each frame, the script asks Unity: *“How much **new** microphone audio is there since **my last bookmark**?”* It copies only that **new** slice into analysis (imitone, levels, etc.). If Unity answers **“zero new,”** that frame does **no** copy and **no** imitone update — even though the mic hardware is still picking up your voice.

**Why that can feel wrong:** Your ears care about **sound in the headphones**. Our “stuck” flag cares about **“did we get a new slice for *this specific script bookmark* this frame?”** Those **should** match if there is only one simple path. If they **don’t**, something else is going on (second path to the speakers, different timing, or we’re not looking at the same moment) — **we have not fully closed that gap with one measurement yet.**

**Do we “fully” understand the problem?**  
- **Yes, for half of it:** when telemetry is stuck, we know the script is often being told **“zero new bytes”** (`unread_zero`).  
- **Not yet, for the other half:** your experience (**current** voice in ears while numbers say “not reading”) still needs **one** careful check so we’re not mixing up **different taps** or **different clocks** (game frame vs audio callback).

---

## Symptoms (confirmed)

- **Telemetry / game voice path** can show **no new raw chunk for ~1 s+** (not a fast strobe).
- **Worse while toning** (user: definite, not subtle).
- **Hearing**: voice sounds **current**, **~250 ms lag**, through **DirectVoiceMonitoring** processing — **not** “minutes of old buffer.”

---

## Architecture (current stack)

- **`MicPipeline`**: `Microphone.GetPosition` vs `micPosRead` → unread count → `GetData` into `latestRawFrame`; ring buffers; **`EnsureFrameUpdated` once per `Time.frameCount`**.
- **`ImitoneVoiceIntepreter`**: `TryCopyLatestRawFrame` → only if `rawSampleCount > 0` runs filters, `_dbMicrophone`, **`imitone.InputAudio`**, **`GetState`**. If count **0**, **none** of that runs for that frame.
- **Pre-refactor** (`OLD_ImitoneVoiceInterpreterForDebugComparison.cs`): same **ring math** + `if (capturedInput.Length > 0)` — same **class** of failure; refactor **moved** code, did not invent Unity’s pattern.

---

## What we measured (debug / profiler)

| Item | Result |
|------|--------|
| **`debugMicLastExitReason`** during bad spell | Often **`unread_zero`** |
| **Write vs read** | **`micPosWrite == micPosRead`**, **`latestRawSampleCount == 0`** |
| **`gainRidingGatePipelineReady`** | **True** while consumption false — capture “ready,” not “no device” |
| **Script order** | **`MicPipeline.Update` very early** in frame (~0.04 ms in one profile capture) |
| **Profiler (example frame)** | **~5 ms** main thread — that frame **not** “CPU pegged”; spikes exist **elsewhere** on timeline |
| **Profiler (sample ~42813)** | **`MicPipeline.Update` ~0.09 ms**; total CPU **~9.8 ms** — mic update **not** heavy |
| **Monitoring underflow / starvation during `unread_zero`** | User: cumulative totals **often do not climb** during stuck ingest — **consistent** with ring + latency (see interpretation) |
| **PC restart** | Problem **persists** |
| **Bluetooth** | **Not** in use (wired) |

---

## Interpretation (short)

- **Stuck data** = many frames where Unity reports **no new samples** between **script read head** and **`GetPosition`** (`unread_zero`).
- **Execution order** is **unlikely** the root cause (mic already first; cheap that frame).
- **“Hear current voice while debug says no read”** needs a **clean reconciliation** (same instant, same stream, same definition of “read”) — **open**; do not assume “old ring playback” without proof.

**`DirectVoiceMonitoring` (code):** `OnAudioFilterRead` fills output from **`MicPipeline.ReadNormalizedSamples` / `ReadRawSamples`** (ring cursors). Underflow/starvation increment only when the callback asks the ring for a full output buffer and **`copied < frameCount`** (starvation when **`copied <= 0`**). **`unread_zero`** means **no new clip slice was merged into the pipeline’s capture/read head that main-thread frame**; the **ring can still hold older audio**, and the monitoring read cursor can stay **behind** the ring write head with **latency**, so **many** `unread_zero` frames **need not** produce new underflow events. Session totals (e.g. 19 / 18) can be **startup / earlier stress**, not proof of activity during every stuck spell.

**`AudioSource.clip`** is bound to **`MicrophoneBuffer`** but the filter path **drives** what goes to the bus from **ring reads** — reconcile “what I hear” with **which stream** (`MonitoringStreamSource`: normalized vs raw) and **buffered latency ms**.

---

## What we did **not** prove

- That it is **only** “bad hardware.”
- That **main-thread overload alone** causes **multi-second** `unread_zero` (spikes may **worsen** toning; long zeros need their own explanation).
- Exact **why** hear-path vs ingest-path **diverge** (if they truly do at the same timestamps).

---

## Ideas discussed (not all implemented)

| Idea | Note |
|------|------|
| **Capture hardening** | Watchdog / re-`Start` mic on sustained `unread_zero` — **click risk** on hard restart. |
| **Hot-path CPU** | Less JSON / alloc / toning work — helps **spikes**; may not fix long `unread_zero`. |
| **Second `GetPosition` poll** | Low click risk; helps only if **timing**, not **frozen head**. |
| **dB from monitoring tap** | Good for **“what I hear”** meter; **risky** as drop-in for **raw ingest / imitone** semantics unless **two meters** and clear contracts. |
| **`CSVWriter.GetStatus`** | **`ReadAllText` every frame** when status file exists — **heavy**; separate from mic ring; fix later. |

---

## Open actions (pick order)

0. **Unified Inspector panel**: Add **`MicVoiceIngestDebugAggregate`** to the same GameObject as **`MicPipeline` + `ImitoneVoiceIntepreter`** (or assign refs manually). It copies **mic ingest** + **interpreter TryCopy / raw consumed** + optional **`DirectVoiceMonitoring`** cumulative transport totals in **`LateUpdate`** so you read one block together. `DirectVoiceMonitoring` is resolved on self or children if unset.
1. **One timed capture**: while telemetry says **stuck**, note **frame**, **`aggMicExitReason`** (e.g. **`unread_zero`**), **`aggRawConsumedThisFrame` / `aggInterpTryCopyTrue`**. Monitoring underflow/starvation **may stay flat** (ring + latency); optional **before/after** totals only if you want to confirm **no** new transport stress during a long spell.
2. **Profiler**: one **toning spike frame** (yellow bar high) vs one **stuck + short frame**.
3. **Decide product path**: **soft** capture recovery vs **CPU** vs **two-level meters** (ingest vs monitor) — then implement **small**, reviewable changes.

---

## Run log (aggregate Inspector)

| When | Frame | Mic | Interpreter | Monitoring (cumulative) | Notes |
|------|-------|-----|-------------|-------------------------|--------|
| 2026-05-01 (user capture) | **6965** | `unread_zero`, unread **0**, latest raw **0**, write **==** read **152320**, stalled head **1** | Raw consumed **false**, TryCopy **false**, count **0**, mic ready **true**, mic dB **~-42**, imitone dB **~-42** | Underflow **19** / samples **18720**, overflow **2** / **18208**, starvation **18** | Tone counters **10** / confident **1** — toning context. **dB with zero copy** = values **not refreshed this frame** (carry-over from last ingest); does **not** contradict `unread_zero`. |
| 2026-05-01 (user follow-up) | Profiler **~42813** | — | — | **Underflow / starvation totals flat** while ingest stuck | **`MicPipeline.Update` ~0.09 ms** on that frame; monitoring counters **not** required to rise per `unread_zero` frame (ring buffer + read lag). |

---

*Last updated from conversation thread — amend as you test.*
