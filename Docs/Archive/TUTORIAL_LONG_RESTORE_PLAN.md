# Long tutorial restore — working plan

This document tracks restoring **Long** tutorial behavior after the sequence refactor, without changing **Short** tutorial behavior (Short is the current known-good path).

**Rules (from project owner)**

- Do **not** edit production code files without explicit permission in chat.
- After each approved code pass, **verify Short** still matches its intended behavior (smoke test / comparison checklist below).

**Status:** Short works. Long needs step-by-step fixes; issues will be collected as they appear.

---

## 1. What changed architecturally

### Before (reference: `Docs/REFERENCE_Tutorial_from_commit_a7be839.cs`)

- `Tutorial` lived under `Assets/Scripts/WwiseManagers/` and **owned the tail of the experience**: `EndTutorialNaturally()` called `StopTutorial()`, switched music to **Freeplay**, started **world shuffle**, and **enabled the director** — effectively jumping the product into “playground-like” mode from inside the tutorial script.
- Tutorial “completion” was not expressed as a first-class **sequence stage** transition; it was a bundle of game setting changes.

### After (current system)

- **Ordered stages** are driven by **`SequenceRunner`** + **`IStageHandler`** implementations per `StageType` (see `Assets/Scripts/Sequencing/SequenceTools.cs` for `StageType`, `StageVariant`, and `SequenceCommand`).
- **`Sequencer.HandleSequenceCommand`** forwards commands to whichever handler(s) declare they watch that command (`TryExecuteSequenceCommand`); see `Assets/Scripts/Sequencing/Sequencer.cs`.
- **Tutorial** is now a **`StageType.Tutorial`** stage. Completing that stage is **`TutorialStageHandler`** responsibility: it sets `IsComplete` when the right **`SequenceCommand`**(s) fire, then the runner advances to the next CSV-defined stage (typically **Playground**).
- **Playground** is owned by **`PlaygroundStageHandler`**, which applies the Freeplay / director / shuffle / countdown-driven behavior for standard and adjunctive variants — i.e. the “what happens when we are actually in Playground” logic moved **out** of `Tutorial` and into the Playground handler.

**Implication for Long fixes:** restoring Long is not only “fix `Tutorial.cs`”; it is aligning **Wwise cues → `SequenceCommand` → `TutorialStageHandler` → next stage**, while keeping **Short** on its existing completion path (`TutorialPassed` from guidance count / `StopTutorial`, with Wwise completion cues **ignored** by the handler).

---

## 2. Key files to keep in view

| Area | Path | Role |
|------|------|------|
| Tutorial logic (voice tests, corrections, variants) | `Assets/Scripts/Sequencing/Tutorial.cs` | Long vs Short branching; fires `TutorialPassed` where appropriate |
| Tutorial stage lifecycle | `Assets/Scripts/Sequencing/Handlers/TutorialStageHandler.cs` | Enters tutorial variant, wires music/monitoring; **which commands complete the stage** differs Long vs Short |
| Playground stage | `Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs` | What runs **after** tutorial stage completes: Freeplay, director, shuffle, timers, etc. |
| Command vocabulary | `Assets/Scripts/Sequencing/SequenceTools.cs` | `SequenceCommand` meanings (`StartInteractive`, `Break_Tests`, `TutorialPassed`, …) |
| Wwise → sequence | `Assets/Scripts/WwiseManagers/WwiseVOManager.cs` | Music sync user cues → `sequencer.HandleSequenceCommand(...)` (e.g. `Cue_StartInteractive`, `Cue_Break_Tests`, vocalization type cues) |
| Sequence dispatch | `Assets/Scripts/Sequencing/Sequencer.cs` | `HandleSequenceCommand` entry point |
| Historical snapshot | `Docs/REFERENCE_Tutorial_from_commit_a7be839.cs` | Pre-refactor `Tutorial` (old path: `WwiseManagers/Tutorial.cs` at commit `a7be839`) |

Optional context when debugging stage order: sequence definitions / CSV session rows that reference `Tutorial_Long` vs `Tutorial_Short` and the following `Playground_*` variant.

---

## 3. How `TutorialStageHandler` and `PlaygroundStageHandler` interact with Long vs Short

### `TutorialStageHandler`

- **`Enter(StageVariant.Tutorial_Long)`**  
  - Sets initial vocalization via `SetTestVocalizationType("Hum")`.  
  - Sets `MusicSystem1.MusicMode.InteractiveTutorial`.  
  - Calls `tutorial.StartTutorial("Long")`.  
  - Sets `variantWatchesWwiseVOCuesForCompletion = true`.  
  - **Stage completion** can occur on: `SequenceCommand.StartInteractive`, `Break_Tests`, **`TutorialPassed`**, or `EndThisSequenceStage`. When `variantWatchesWwiseVOCuesForCompletion` is true, `StartInteractive` / `Break_Tests` call `MarkComplete()` (see `ExecuteSequenceCommand`).

- **`Enter(StageVariant.Tutorial_Short)`**  
  - `SetTestVocalizationType("Ahh")`, `MusicMode.Silent`, `StartTutorial("Short")`.  
  - `variantWatchesWwiseVOCuesForCompletion = false` — so **`StartInteractive` / `Break_Tests` do not complete the stage** (warning log if they fire).  
  - Short is expected to end via **`TutorialPassed`** from `Tutorial` (guidance count path or `StopTutorial`).

- **Cleanup:** `BeginTransitionOut` / `Exit` → `LocalCleanup()` → `tutorial.StopTutorial()` and clears tutorial monitoring override. `StopTutorial()` in `Tutorial.cs` issues `TutorialPassed` when still `inTutorial`; handler may already be complete — sequencing must remain safe/idempotent.

### `PlaygroundStageHandler`

- Runs **after** the Tutorial stage is marked complete and the runner advances.
- **`Enter`**: e.g. standard path enables **director**, **Freeplay** music mode, shuffle (standard), countdown checks, session UI headers — the organized home for behavior that **`EndTutorialNaturally()`** used to partially duplicate.
- **Notable:** `StandardSequencePlaygroundCoroutine` still calls `tutorial.StopTutorial()` near its tail (fade / last minute). Treat as **legacy** until removed (see Issue Log: remove Playground tail `StopTutorial`).

### Wwise cues (relevant to Long)

In `WwiseVOManager`, tutorial-related music sync cues include:

- **`Cue_FreePlay`** — still sets UI session string, silent layer, **`director.Enable()`** (side effect **before** Playground stage may run — ordering / double-enable risk; see Issue Log).
- **`Cue_Break_Tests`** — `HandleSequenceCommand(Break_Tests)` (legacy `EndTutorialNaturally` call is commented).
- **`Cue_StartInteractive`** — `HandleSequenceCommand(StartInteractive)`.
- Vocalization progression cues (`Cue_ChangeVocalizationType…`, **Advanced** also starts shuffle in Wwise path).

Long’s design assumption: **Wwise** remains authoritative for **how many prompts** and **when** the tutorial stage should complete (`StartInteractive` / `Break_Tests`), while **`Tutorial.cs`** still drives **per-prompt** test/correction loops.

---

## 4. Intended behavior: Long vs Short (product intent)

| Dimension | **Long** (target / original spirit) | **Short** (locked — do not regress) |
|-----------|--------------------------------------|-------------------------------------|
| Prompt count | Many (~18-step feel): prompt → test → success → **next prompt quickly** | Fewer prompts; each test window is **longer** |
| Test timing | Shorter “fail” window (`failThreshold` 8s in code); on success, advance **soon** after breath / line logic (no fixed multi-breath **minimum** test window in the old reference) | Longer fail threshold (24s); after `testSuccess`, **still waits** until `_sectionTimer` reaches `failThreshold` before “success path” continuation — fixed **minimum** window per prompt |
| Guidance playback | `PlayTutorialGuidance(testVocalizationType)` — full Wwise-driven tutorial content | `PlayTutorialGuidance("Lite")`; stage completes when **`guidanceCount >= 4`** → `TutorialPassed` |
| Stage completion | Primarily **Wwise** `StartInteractive` / `Break_Tests` (handler watches these); `TutorialPassed` also completes (e.g. `StopTutorial`, low-time watchdog) | **`TutorialPassed` only** from Short logic / stop; Wwise “interactive” cues **must not** complete the stage |

**Critical implementation detail in `Tutorial.cs`:** the **`variant == "Short"`** block after `testSuccess` (extra wait + Lite guidance + count) must remain behaviorally identical when touching shared coroutine code. Prefer **explicit `if (variant == "Long")` / `else if (variant == "Short")`** branches over shared timer logic unless carefully proven equivalent.

---

## 5. Code comparison: reference `Tutorial` vs current `Tutorial.cs`

Reference: `Docs/REFERENCE_Tutorial_from_commit_a7be839.cs` (old `Assets/Scripts/WwiseManagers/Tutorial.cs`).  
Current: `Assets/Scripts/Sequencing/Tutorial.cs`.

### 5.1 Removed or relocated (high impact)

- **`EndTutorialNaturally()`** — removed. Freeplay, shuffle, director enable are **not** tutorial’s job anymore; **`PlaygroundStageHandler`** (and Wwise cues like `Cue_FreePlay`) cover parts of this. Any Long fix must not “re-inline” full playground setup into `Tutorial` unless product explicitly asks (would fight the stage model).
- **`TutorialCallBackFunction` (Ak callback)** — removed from `Tutorial`; Wwise music-sync cue handling lives in **`WwiseVOManager`** (and routes to `sequencer.HandleSequenceCommand` / `tutorial.SetTestVocalizationType`, etc.).
- **`StartTutorial()` no-parameter API** — replaced by **`StartTutorial(string startVariant)`** requiring `"Long"` or `"Short"`; music mode and light init are **not** started inside `Tutorial` the way the reference did (`MusicMode.Tutorial` in the old snapshot — that enum value is gone; Long now uses `MusicMode.InteractiveTutorial` from `TutorialStageHandler`, see Issue 2). Reference also called `sequencer.InitializeLights()`. **Lights:** Opening likely initializes them today (Short works); we still want **idempotent** light init when Opening is skipped (dev / CSV jumps) — see Issue Log.
- **Public `active` / `tutorialComplete`** — removed or commented; external code should not rely on those flags from `Tutorial`.
- **`Start()` default `testVocalizationType = "Hum"`** — reference initialized in `Start()`; current script uses **`Awake`** (mostly empty) and **`TutorialStageHandler`** sets type for each variant.

### 5.2 New or changed behaviors (current only)

- **`variant`** (`"Long"` / `"Short"`) drives **`failThreshold`** (8 vs 24) and coroutine branches.
- **Short-only post-success wait:** after `testSuccess`, Short waits until `_sectionTimer` catches up to `failThreshold` before “wait for breath” and next guidance — this is the **fixed minimum test duration** behavior you described; **Long does not take this branch**.
- **Long-only timer reset nuance:** in the main `while (!testSuccess)` loop, **`_sectionTimer` resets when `toneActiveBiasTrue` only if `variant == "Long"`**. For Short, lack of reset during singing is part of the different timer semantics (aligns with the longer integrated window). **Touching this `if`/`else` chain is high regression risk for Short.**
- **`guidanceCount`** and **`PlayTutorialGuidance("Lite")`** with **`guidanceCount >= 4` → `sequencer.HandleSequenceCommand(TutorialPassed)`** — Short completion path only.
- **`StopTutorial()`** now calls **`sequencer.HandleSequenceCommand(SequenceCommand.TutorialPassed)`** when stopping from an active tutorial — important for **force advance**, UI, and the **`Update`** watchdog (below).
- **`Update` watchdog:** when `inTutorial && SessionCountdownThisSection() <= 10f`, logs and **`StopTutorial()`** — forces sequence forward if Wwise / guidance never completes. **`<= 10f` is intentional** (not `0`); `TutorialStageHandler` class summary documents this (Issue 1).

### 5.3 What stayed similar

- **`Update`**: still sets `testSuccess` from `_tThisToneBiasTrue >= testThreshold`; still handles **Advanced** silent layer change when vocalization type changes.
- **Core loop shape:** `VoiceTestCoroutine` → wait → `gameOn` → test loop → optional correction → next iteration; **`ProvideCorrection`** still uses classic `_failTimer` reset while tone active (parallel to old reference’s main test loop style).
- **`SetTestVocalizationType`** validation list unchanged (`Hum` / `Ahh` / `Ohh` / `Advanced`).

### 5.4 Subtle risks when fixing Long

- **Double responsibility:** `Cue_FreePlay` in **`WwiseVOManager`** still enables **director**; **`PlaygroundStageHandler`** also enables director. Usually idempotent, but Long may expose wrong phase / integration debt — see Issue Log (**`Cue_FreePlay` vs stage completion**).
- **`Break_Tests` handler:** if Long Wwise posts `Break_Tests` when the **Tutorial** stage is no longer the one consuming the command, `HandleSequenceCommand` may log as unhandled — watch for race between cue and `MarkComplete`.
- **Music mode (Long):** The reference’s **`MusicMode.Tutorial`** no longer exists on `MusicSystem1`; the tutorial interactive-music slot is **`MusicMode.InteractiveTutorial`** (see Issue 2 — not drift). Any audio difference vs an old build is general engine/Wwise evolution, not a missing enum rename to “fix.”

---

## 6. Process: how we work through Long without breaking Short

1. **Reproduce** the Long issue with a minimal session (CSV row with `Tutorial_Long` → appropriate Playground variant). Capture logs: `Tutorial:`, `TutorialStageHandler:`, `WWise_VO_CUE:`, `PlaygroundStageHandler:`.
2. **Classify** the failure: (A) voice test / correction timing, (B) Wwise cue → `SequenceCommand` routing, (C) stage never completes or completes too early, (D) Playground entry state wrong, (E) countdown watchdog.
3. **Design** the smallest change; **default** to branching **`if (variant == "Long")`** / preserving Short paths **verbatim** where possible.
4. **Get explicit approval** before editing tracked code files.
5. **Regression check Short** (minimum):
   - Start Short tutorial stage; complete a full Short flow.
   - Confirm **`guidanceCount`** path still fires `TutorialPassed` at the right time.
   - Confirm **`failThreshold` 24** and post-success wait still produce the “three long breaths” feel.
   - Spam-check: `StopTutorial` / low time still advances without hanging.
6. **Document** each issue + resolution under **§7 Issue log** (append new `###` entries as work proceeds).

---

## 7. Issue log

Add a new `### N. Short title` block per issue. Use the **bold** labels below inside each entry so scans stay consistent.

---

### 1. Long tutorial: testing / correction starts while VO still playing (`gameOn` gate)

**Context:** On Long, after `PlayTutorialGuidance` (or correction VO), `VoiceTestCoroutine` waits 3s, then `while (!imitoneVoiceInterpreter.gameOn)` (comment: “wait for the previous guidance to end”), then logs `Testing...` and runs the fail timer (`failThreshold` 8s with silence → `TEST FAIL`). Logs show **~3s from “About to test” to “Testing…”** and **~8s to fail** — i.e. the `while (!gameOn)` loop adds **no real wait**, then the user is silent during the **still-playing** guidance line, so `_sectionTimer` / `_failTimer` hits threshold. Same pattern in `ProvideCorrection` (1s pad + `while (!gameOn)` + 8s correction fail).

**Likely mechanism (code):**

- `while (!gameOn)` **exits immediately when `gameOn` is already `true`** — it only blocks when `gameOn` is `false`.
- `TutorialStageHandler.Enter` sets **`imitoneVoiceInterpreter.gameOn = true`** unconditionally, so unless something sets `gameOn` false during the new line, the gate is a **no-op**.
- In `WwiseVOManager.VOCallbackFunction`, **`gameOn` is toggled for tutorial-ish VO by** `Cue_VO_GuidedVocalization_Start` → `gameOn = false` and `Cue_VO_GuidedVocalization_End` → `gameOn = true` (plus `Cue_Microphone_ON` / `Cue_Microphone_OFF` via `SetGameOn`). If **Long tutorial guidance events in Wwise do not emit those cues** (or not on the same bus / callback path as this handler), **`gameOn` never goes false** during the line → testing starts right after the fixed 3s (or 1s) delay while VO is still running.

**Reference parity:** Old `REFERENCE_Tutorial_from_commit_a7be839.cs` uses the **same** `while (!imitoneVoiceInterpreter.gameOn)` pattern after the 3s wait — so this is not a new C# regression in that line alone; the **Wwise cue contract** (or handler forcing `gameOn` true) must match for the gate to work.

**Hypothesis / next steps:** Confirm in Wwise + Unity logs whether `Cue_VO_GuidedVocalization_Start` / `_End` (or mic on/off cues) fire on **Long** tutorial `PlayTutorialGuidance` / correction events. If not, add or remap cues, **or** gate on an explicit “VO line finished” cue (e.g. user-mentioned `Cue_VO_GuidedVocalization_End` only helps if it actually fires on that content). Optionally add **temporary logs**: `gameOn` when entering/exiting the `while (!gameOn)` loops.

**Resolution (Long, code):** `Tutorial.cs` now calls `imitoneVoiceInterpreter.SetGameOn(false)` immediately before **`PlayTutorialGuidance`** (Long / non-Short branch) and before **`PlayCorrectionGuidance`** when `variant == "Long"`, restoring “mic off at line start” without the lost Wwise cue. **`Cue_VO_GuidedVocalization_End`** (or other cues) should still turn **`gameOn`** back **on** when the line opens the test window.

**Status:** Long — mitigated in Unity; confirm in playtests. Short parity — see **### 21**.

**Short regression:** This pass does **not** change Short; run Short smoke anyway after pull.

---

### 3. Long voice-test pacing vs Short (core product fix)

**Context:** Long should feel like **many** prompts with **short** inter-prompt rhythm; Short uses **fewer** prompts, **longer** minimum test window (`failThreshold` 24 + post-success wait), and `PlayTutorialGuidance("Lite")`.

**Goal:** Restore Long’s “prompt → test → success → next prompt quickly” loop without touching Short branches (`variant == "Short"` paths must remain behaviorally identical).

**Status:** Open — primary gameplay work in `Tutorial.cs` (+ Wwise content if timing is VO-driven).

**Short regression:** Mandatory full Short pass after any shared coroutine edit.

---

---

### 12. Lights: idempotent init when Opening is skipped

**Context:** Reference `StartTutorial` called `sequencer.InitializeLights()`. Current `Tutorial` does not. Short works in normal flows — likely lights come from **`OpeningStageHandler`** or earlier init.

**Goal:** Ensure lights are **idempotently** initialized when entering Tutorial if Opening was skipped (dev jumps, alternate CSV). Mirror “safe if already done” behavior rather than duplicating heavy setup.

**Status:** Open — trace Opening → Tutorial handoff; add minimal init call (probably from `TutorialStageHandler.Enter` or `Tutorial.StartTutorial`) only where proven needed.

**Short regression:** Run Short with normal Opening; run Short (or dev shortcut) **without** Opening — lights must still be correct.



---

### 14. `Cue_FreePlay` — director / session vs stage model

**Context:** `WwiseVOManager` still applies UI + silent layer + **`director.Enable()`** on `Cue_FreePlay`. `PlaygroundStageHandler` also enables the director. Integration is **not** clearly “stage owns transition.”

**Hypothesis:** Long term, **`Cue_FreePlay` might need to align with stage completion** (or defer side effects until Playground `Enter`) — but that is a design change with Wwise ordering constraints.

**Status:** Open — investigate cue order vs `StartInteractive` / `Break_Tests` / `TutorialPassed`; any change must **not** alter Short (Short ignores Wwise completion cues on the handler, but Wwise callbacks may still run).

**Short regression:** Full Short run + confirm no early director / UI oddities if `Cue_FreePlay` is touched.

---

### 15. Remove legacy `tutorial.StopTutorial()` from Playground standard coroutine

**Context:** `PlaygroundStageHandler.StandardSequencePlaygroundCoroutine` calls `StopTutorial()` near fade / last minute. Countdown-based tutorial exit and stage boundaries should make this redundant.

**Goal:** Remove after confirming tutorial is never logically active during Playground, or only benign “long tail” no-ops remain; document if anything still relied on this side effect.

**Status:** Open — verify with logs / breakpoints, then delete call.

**Short regression:** Standard session through Playground tail; confirm no double-`TutorialPassed` or handler warnings.

---

### 16. `Break_Tests` / `StartInteractive` race / unhandled command

**Context:** If Wwise fires `Break_Tests` or `StartInteractive` when Tutorial is **no longer** the active stage, `HandleSequenceCommand` may not consume the command (logged as unhandled).

**Goal:** Reproduce on Long; decide whether to no-op safely, buffer, or fix Wwise placement.

**Status:** Open — monitor during Long QA.

**Short regression:** Confirm Short still ignores these for **completion** (handler flag) even if cues fire in edge builds.

---



### 19. `StopTutorial` / `TutorialPassed` idempotency

**Context:** `StopTutorial` always raises `TutorialPassed` when `inTutorial`; cleanup paths include `TutorialStageHandler.LocalCleanup` → `StopTutorial` after stage may already be complete.

**Goal:** Confirm no double-advance, duplicate logs, or handler warnings when cleanup races; tighten only if a bug appears.

**Status:** Open — verify during Long + forced-skip tests.

**Short regression:** Force `StopTutorial` / low-time watchdog on Short; stage should advance once, cleanly.

---

### 20. `Advanced` vocalization cue starts shuffle in `WwiseVOManager`

**Context:** On `Cue_ChangeVocalizationTypeFromOhhToAdvanced`, **`worldShuffler.BeginShuffle()`** runs from the Wwise callback. Playground standard flow **also** starts shuffle on `Enter`. Depending on cue timing vs stage transition, Long may get **early shuffle**, duplicate calls, or ordering that differs from the reference-era “tutorial then playground” mental model.

**Goal:** Map cue order for Long; confirm `BeginShuffle` is idempotent / desirable here; adjust only if Long or Short shows wrong soundscape behavior.

**Status:** Open — investigate with Long QA; may be “working as designed.”

**Short regression:** Short path still hits vocalization / playground transitions without soundscape glitches.

---

### 21. Short tutorial: parity `SetGameOn(false)` when posting guidance VO (technical correctness)

**Context:** Long now sets **`gameOn` false** immediately before **`PlayTutorialGuidance`** / (when in correction) **`PlayCorrectionGuidance`**, because **`Cue_VO_GuidedVocalization_Start`** is missing in Wwise and the **`while (!gameOn)`** gate in `VoiceTestCoroutine` / `ProvideCorrection` otherwise does not block during VO. Short was not changed: **`failThreshold`** is larger (24s) and post-success timing differs, so the same bug is **less noticeable** but **still technically wrong** if `gameOn` stays true through Lite guidance lines.

**Goal:** Apply the same “**`SetGameOn(false)`** right before Unity posts tutorial/correction VO” pattern for **Short** (and re-test Short end-to-end), or restore equivalent Wwise cues on Short content if preferred.

**Status:** Open — parity / QA after Long is stable.

**Short regression:** Full Short tutorial pass after implementing.

---

*Last updated: Long `gameOn` fix at VO post; ### 21 Short parity.*
