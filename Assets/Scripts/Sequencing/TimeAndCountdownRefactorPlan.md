# Time keeping & StartCountdown stage — refactor plan

This document is the **execution checklist**. Say **“run phase N”** to implement that phase in order. Phases are numbered by **dependency-safe implementation order** (not topic clusters).

---

## Background — root cause (why countdown shows `1000000`)

`PlaygroundStageHandler` waits on **`TimeTrackerScript.CountdownSeconds`**, which only decrements after **`BeginCountdown()`**. If that never runs before Playground, or the configured value was never set, the wait can appear stuck (historically ~**1e6** default). Centralizing timing and calling **`BeginCountdown()`** from **`StartCountdown`** fixes that.

---

## Resolved decisions (quick reference)

| Topic | Decision |
|--------|----------|
| Elapsed time | **`TotalElapsedTime`** only (merges old **`TotalElapsedTime`** + **`Sequencer._timeSinceStart`**). |
| Session “time left” | **`countdown`** only (merges **`_countdownToSavasana`** + **`_timeLeftSeconds`**). |
| **`DisplayTime`** | Formatted from **`TotalElapsedTime`** only. |
| Savasana / finished latch | Live on **`TimeTrackerScript`** (migrated from **`savasanaCountdownCompleteFlag`**). |
| Standard sequence one-shots | Stay on **`Sequencer`**, renamed with a **standard-sequence** prefix (e.g. `standardSequenceMilestoneStart1` … `End4`). |
| **`timeInUnguidedVocalization`** | **Delete** (unused); clean serialized refs in scenes/prefabs when touched. |
| **`calibrationMenu.startedExperience`** | **Will not** gate countdown tick after migration (no longer needed for that). |
| CSV vs countdown | CSV loads **inputs** only; **value + ticking** start only when **`StartCountdown`** runs. **`TotalElapsedTime`** ticks for “how long the session has been open”; **countdown** ticks only after **`BeginCountdown()`** from that stage. |
| Last-minute / post-unguided HUD | **Do not** call `SetTimeLeftSeconds` from **`Sequencer`** like today. Add a separate **`StartCountdown`** variant **`ClosingDuration`** (length = closing duration as established by **`CSVLoader`** / tracker inputs). |
| **`SetCountdownToSavasana`** | **Deprecate**; normal path is stage logic only. |
| **`60m` vs CSV** | Discuss in detail when that decision blocks implementation (see Phase 7). |
| **Playground elapsed (was “time since tutorial”)** | **`TimeSincePlaygroundStart`** on **`TimeTrackerScript`**. **Starts / resets to 0** only in **`PlaygroundStageHandler.Enter`** via **`OnPlaygroundStageEntered()`**. **`TutorialStageHandler`** does **not** touch it. **`ResetTimeSincePlaygroundStart()`** clears to 0 and **stops** accumulation until the next playground enter (reserved for future hooks). **`SetTimeSincePlaygroundStart`** remains for dev/cheat paths (e.g. **`Sequencer.StartPlayground`**). **`StandardSequenceUpdate`** milestones use **`TimeSincePlaygroundStart`**. |

---

## Phase 1 — `TimeTrackerScript` foundation + shims

**Intent:** Expand the tracker and wire **temporary shims** so existing `Sequencer` code can still compile and run while you migrate. Do **not** remove `Sequencer` timing fields yet; dual-read or delegate until Phase 5.

### Tasks

- Add / merge fields: **`TotalElapsedTime`** (session-open clock, always ticks in `Update` unless you later add a pause — document choice), **`countdown`**, **`TimeSincePlaygroundStart`** (playground-scoped elapsed: **`OnPlaygroundStageEntered()`** resets to 0 and arms tick; **`ResetTimeSincePlaygroundStart()`** clears and pauses; optional **`SetTimeSincePlaygroundStart`** for cheats), **`totalTimeOfPostUnguidedVocalizationContent`** (if moved from `CSVLoader`), savasana-finished latch (replacement for **`savasanaCountdownCompleteFlag`**).
- **`countdown`** does **not** decrement until **`BeginCountdown()`** has been called (Phase 2 will call it from **`StartCountdown`**).
- Implement **`ConfigureCountdownSeconds(float)`** — set initial countdown **without** starting the tick (per locked design).
- Implement **`BeginCountdown()`** intended to be called **only** from **`StartCountdown`** stage: apply the **calculated** start value and start decrement. **If called again while a countdown is already active**, log **both** the current value (before change) and the new start value (developer visibility).
- Move **`debugAllowTimingLogs`** + last-tick time to tracker; 1 Hz tick logs for **`TotalElapsedTime`** / session debug only.
- **`DisplayTime`**: update from **`TotalElapsedTime`** only.
- Doc comment at top of file: authoritative vs display-only fields.
- **`Sequencer`:** Optional shim — e.g. `Sequencer` reads/writes tracker for tests, or leaves old fields as duplicates until Phase 5 (pick one approach and note it here when done).

### Developer notes

- **`ConfigureCountdownSeconds`:** Keep it — useful for staging values before **`BeginCountdown`**.
- **`BeginCountdown`:** Not idempotent in the sense of “second call no-ops”; second call should **log old vs new** values for debugging unexpected double-starts.

### Deliverable

Project builds; playground may still use old `Sequencer` field until later phases; tracker APIs exist and are testable in isolation.

---

## Phase 2 — `StartCountdown` stage (enum, handler, variants)

**Intent:** Sequence can **compute and start** the main countdown at the right narrative moment.

### Tasks

- Add **`StageType.StartCountdown`** in **`SequenceTools.cs`**.
- Add **`StartCountdownStageHandler`**, register in **`Sequencer.Awake`** with other handlers.
- **`Enter(variant)`** — normalize case/whitespace. Supported variants:

| Variant | Seconds (conceptually) |
|---------|-------------------------|
| `Nm simple` | `[CountdownThisSection]` = `[CountdownFull]` = N×60 (CSV post-unguided not used) |
| `Nm with savasana` | `[CountdownThisSection]` = N×60, `[CountdownFull]` = N×60 + post-unguided |
| `ClosingDuration` | Closing duration only, as established by **`CSVLoader`** / tracker (replaces **`Sequencer`** calling `SetTimeLeftSeconds` for post-unguided). |

- **`ClosingDuration` variant:** After setting **`countdown`**, **log** the value **before** vs **after** applying this variant. If the absolute difference is **more than 10 seconds**, **`Debug.LogWarning`** — likely math/logic error somewhere.
- Handler: compute seconds → **`ConfigureCountdownSeconds`** / direct set per your API → **`BeginCountdown()`** → **`MarkComplete()`** (one-shot stage unless you later change design).
- Update **one** reference **`SequenceDefinition`** as an example. **Ordering:** you generally want **`StartCountdown`** **before** **`Tutorial`** so the main session countdown runs during tutorial; finalize any extra reset/secondary timer behavior when implementing.

### Developer notes

- Main variants + **`ClosingDuration`** are both part of this phase’s contract.
- **`ClosingDuration`** diff logging (more than 10s → warning) is mandatory for catching bad transitions.

### Deliverable

You can hit **`StartCountdown`** in-editor / dev build and see **`countdown`** decrease; sequence advances to the next stage.

### Unity editor — add `StartCountdown` to your Sequence Definition assets

After **`StageType.StartCountdown`** and **`StartCountdownStageHandler`** exist in code and the handler is registered on **`Sequencer`**:

1. Open each **SequenceDefinition** ScriptableObject you ship (e.g. Protocol Stacks Ascending, Skills Training, Integration, Peace, etc.).
2. Insert a **`StartCountdown`** stage with the correct **variant** for that product (see the table above, e.g. `60m simple`,
   `60m with savasana`, **`ClosingDuration`**).
3. **Placement:** Put **`StartCountdown`** at the narrative moment the session “time left” clock should start — **usually
   immediately before `Tutorial`** so the main countdown runs during the tutorial, or **right after `Opening`** when
   that definition has no Tutorial stage. It must run **before `Playground`** so waits on **`CountdownSeconds`** see a
   decreasing value.
4. Ensure **CSV / tracker inputs** for **`ClosingDuration`** and **`Nm with savasana`** (post-unguided duration) are loaded **before** this stage when you use those variants. **`Nm simple`** does not use post-unguided for the pair (both timers = N×60).
5. **Do not** assume CSV alone starts the clock; only **`StartCountdown`** → **`BeginCountdownPair`** starts the tick.

---

## Phase 3 — Playground & Savasana handlers use tracker ✅

**Intent:** `PlaygroundStageHandler` / `SavasanaStageHandler` use **`TimeTrackerScript`** for session countdown (reads go through the tracker in handlers; **`Sequencer.CountdownSeconds`** remains a convenience getter for the rest of the project).

### Tasks

- Handlers use **`CountdownSeconds`** / tracker APIs instead of the removed **`Sequencer`** countdown field.
- **`PlaygroundStageHandler`:** on **`MarkComplete()`**, call **`ForceSetCountdownSecondsAndStop(-1f)`** so leaving Playground marks countdown finished before Savasana.
- Confirm Playground wait loops see a **decrementing** countdown once Phases 1–2 are active in your test sequence.

### Implemented

- **`PlaygroundStageHandler`:** `PlaygroundCoroutine` waits on **`SessionCountdownSeconds()`** (reads **`TimeTrackerScript`** first). Startup warns if the tracker is missing or if countdown is not running and still looks like the **~1e6** default (points to missing **`StartCountdown`**). **`MarkComplete()`** calls **`ForceSetCountdownSecondsAndStop(-1f)`**. Legacy **`calibrationMenu`** warnings that implied countdown tick were removed — tick is tracker-driven.
- **`SavasanaStageHandler`:** **`Enter`** calls **`ForceSetCountdownSecondsAndStop(-1f)`** after starting ascending closing; error logs use the same tracker-backed snapshot.

### Developer notes

- **Savasana** also sets **`-1`** on the tracker — defer **why** and desired end-state until **Phase 7** if product wants a different finished value.

### Deliverable

Playground no longer stuck at 1e6 **when** `StartCountdown` has run and **`BeginCountdown`** is active.

---

## Phase 4 — `CSVLoader` slim-down ✅

**Intent:** CSV only **hydrates** tracker **inputs**; it does **not** set or start live **`countdown`**.

### Tasks

- **`TimeLeftInitializations()`** (or successor): write **`totalTimeOfPostUnguidedVocalizationContent`**, configured session inputs, etc. to **`TimeTrackerScript`**.
- **Remove** **`sequencer.SetCountdownToSavasana(...)`** and any “calculated countdown at CSV time” from CSV.
- **`Nm with savasana`** / **`ClosingDuration`** need a positive post-unguided duration on the tracker when used; **`Nm simple`** does not.

### Implemented

- **`TimeLeftInitializations()`** sets **`totalTimeOfPostUnguidedVocalizationContent`** on the CSVLoader instance and **`TimeTrackerScript.SetTotalTimeOfPostUnguidedVocalizationContent`**, then **`MarkSessionTimingInitializedFromCsv()`**. No **`SetTimeLeftSeconds`**, no **`SetCountdownToSavasana`**, no derived “timeLeft − closing” session countdown. Sequencer **`Start()`** warning text updated: **~1e6** until **`StartCountdown`** is expected after Phase 4.

### Risk

Anything that assumed “after CSV, countdown is already running” **breaks** until sequence hits **`StartCountdown`** — intentional.

### Deliverable

CSV load no longer touches session countdown value; only tracker inputs.

---

## Phase 5 — `Sequencer` migration + coroutine guards ✅

**Intent:** Remove duplicate clocks and gates; rename standard-only flags; add debug fallbacks for coroutines.

### Tasks

- Remove **`_countdownToSavasana`** decrement and **`savasanaCountdownCompleteFlag`** from **`Sequencer.Update`**; use tracker only.
- Remove **`_timeSinceStart`**; **`TotalElapsedTime`** is tracker-only.
- Move **`debugAllowTimingLogs`** off **`Sequencer`** if any copies remain.
- Remove **`tutorial.tutorialComplete`** coupling for “post-tutorial” elapsed — playground elapsed lives on the tracker (**Phase 6**); **`StandardSequenceUpdate`** reads **`TimeSincePlaygroundStart`** only.
- Rename **`flagTriggerStart1` … `End4`** → **`standardSequenceMilestoneStart1`**, **`Start2`**, **`End1`** … **`End4`** (or one consistent naming scheme).
- **Deprecate** **`SetCountdownToSavasana`** — thin forwarder for emergency debug only, or delete if unused.
- **Coroutine guard:** Each **`IEnumerator`** in **`Sequencer.cs`** that depends on countdown: at **start**, if **`!IsCountdownRunning`**, **`Debug.LogError`** (“use **`StartCountdown`** in sequence”) then **`EnsureCountdownRunningWithFallback(float)`** with a per-coroutine documented fallback table.
- Remove **`calibrationMenu.startedExperience`** as gate for **countdown** tick (it will not be needed for that).

### Implemented

- **`Update`:** Already had no duplicate countdown decrement / **`_timeSinceStart`**; **`StandardSequenceUpdate`** uses **`CountdownSeconds`** + **`TimeSincePlaygroundStart`** only.
- **Milestones:** **`standardSequenceMilestoneStart1` … `End4`** replace **`flagTrigger*`**.
- **`EnsureLegacyCoroutineCountdown`:** **`ProtocolStacksCoroutine`** uses **`20×60+5`** s fallback (same dev idea as **`PlaygroundStageHandler`**); **`LastMinute`** uses **`90`** s fallback. **`AVS_Program_*`** coroutines only log **`CountdownSeconds`** (no gate).
- **Calibration:** Removed **`startedExperience`** warning that claimed the legacy Protocol Stacks coroutine would not progress (countdown was never gated on it). Removed extra **`startedExperience`** log from **`ProtocolStacksPlaygroundStart`**.
- **`SetCountdownToSavasana`:** Kept for **`StartRightBeforeSavasana`** / **`StartSavasana`**; documented as debug/legacy with **`StartCountdownStageHandler`** as production path.
- Stale **`tutorial.tutorialComplete`** comment lines removed from **`StartPlayground`** / **`StartRightBeforeSavasana`**.

### Deliverable

Single source of truth for clocks in **`TimeTrackerScript`**; **`Sequencer`** orchestrates behavior, not global tick math.

---

## Phase 6 — Playground elapsed (`TimeSincePlaygroundStart`) ✅

**Intent:** The clock formerly framed as “time since tutorial” is **time since the Playground stage actually starts** — not tied to tutorial completion flags.

### Implemented behavior (current)

- **`TimeTrackerScript`:** **`TimeSincePlaygroundStart`** ticks in **`Update`** only while the playground timer is armed.
- **`PlaygroundStageHandler.Enter`:** **`OnPlaygroundStageEntered()`** — set elapsed to **0** and **start** accumulation.
- **`TutorialStageHandler`:** **Does not** reset, start, or stop this clock (no coupling on Enter / Exit / LocalCleanup).
- **`ResetTimeSincePlaygroundStart()`:** set to **0** and **stop** ticking until the next **`OnPlaygroundStageEntered()`** (available for future exits / sequence resets; not required for the main path yet).
- **`SetTimeSincePlaygroundStart(float)`:** dev / cheat only; does not re-arm a stopped clock by itself.
- **`StandardSequenceUpdate`** milestones (e.g. 60s / 300s) use **`TimeSincePlaygroundStart`**.

### Historical note

- Legacy code used **`timeSinceTutorial`** / **`tutorial.tutorialComplete`** and started the timer from tutorial teardown. **Dropped** in favor of **playground entry** as the single narrative start so “time in free play” aligns with the **Playground** stage, not tutorial cleanup timing.

### Deliverable

Playground elapsed semantics are explicit in **`TimeTrackerScript`** + **`PlaygroundStageHandler`**, documented here, and consistent with **`StandardSequenceUpdate`** milestones.

---

## Phase 7 — Regression sweep, UI, open threads ✅

### Checklist

1. **UI** (`TimerUIScript`, `UI_ProgressBarScript`, `Canvas4UIScript`, `StrobeFrequencyChanger`, etc.) — route “time left” to **`countdown`** API; remove **`_timeLeftSeconds`** as a separate concept.
2. **`ProtocolStacksPlaygroundStart` / `ProtocolStacksCoroutine`** — already guarded in Phase 5; verify in playmode.
3. **`SavasanaStageHandler`** **`-1`** on countdown — research original intent; **ask** what to do; implement on tracker (finished state) accordingly.
4. **Scenes / prefabs:** remove **`timeInUnguidedVocalization`** serialized field where present.
5. **`60m` variants vs CSV** — discuss in detail when it becomes blocking (per developer note).

### Implemented (code / assets)

- **`LastMinute`:** After main session countdown hits 0, calls **`BeginCountdown(post-unguided)`** when CSV closing duration is positive (aligned with **`StartCountdownStageHandler`** ClosingDuration). Replaces **`SetTimeLeftSeconds`**-only configure (HUD now ticks for closing phase).
- **`StandardSequenceUpdate`:** Removed per-frame legacy warning and redundant Protocol Stacks inner guard (caller already **`UsesStandardSequenceUpdate`**).
- **`GetTimeLeftFormattedToMinutesAndSeconds`:** Clamps negative countdown (e.g. **`ForceSetCountdownSecondsAndStop(-1)`**) to zero for display.
- **`SavasanaStageHandler`:** Comment documents **`-1`** as finished sentinel / non-ticking.
- **`Canvas4UIScript`:** Time-left display no longer requires **`Sequencer`** reference ( **`TimeTrackerScript`** only).
- **Sandbox-Test.unity:** Removed orphan **`timeInUnguidedVocalization`** YAML on **`Sequencer`** (field no longer on script).
- **Still manual / product:** playmode verification of legacy protocol coroutine (#2); **`60m` vs CSV** (#5).

### Deliverable

No duplicate “time left” in UI; no stray references to removed **`Sequencer`** fields; open threads closed or ticketed.

---

DEVELOPER: YOU GOT HERE — Phase 7 sweep done in repo; remaining items are **playmode verify** and **60m / CSV** when blocking.

---

## What stays on `Sequencer` (never the tracker)

- **`startButtonFlag`** — flow (“have we called `StartTrueStart`”).
- **Coroutine-local timers** in AVS / **`DynamicDrop_*`** — local choreography only.

---

## Appendix — `BeginCountdown` logging (spec)

On **second or subsequent** **`BeginCountdown`** while countdown is already considered “running” or non-initial:

- Log **previous** countdown value (before applying new start).
- Log **new** start value.
- Use this to catch double stage entry or wrong order.

(Exact condition — “already running” vs “any re-entry” — implement to match Phase 1 API.)
