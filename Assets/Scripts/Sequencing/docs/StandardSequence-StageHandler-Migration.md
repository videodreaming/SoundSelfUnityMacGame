# Migration: Standard Sequence → Stage Handler System

This document captures the plan to refactor the legacy **standard sequence** (`StandardSequenceUpdate`, `LastMinute`) into the **stage handler** pattern used by `PlaygroundStageHandler` / `SavasanaStageHandler`, using **`SkillsTraining`** and **`Integration`** (and related) sequence definition assets.

---

## Context — Where Things Live Today

| Piece | Role |
|--------|------|
| **`CSVLoader.UsesStandardSequenceUpdate`** | Removed in Phase 4. Standard sessions now advance only through `SequenceRunner` + stage handlers. |
| **`StandardSequenceUpdate()`** | `TimeSincePlaygroundStart`: 60s / 300s; `CountdownThisSection`: 300 / 180 / 60 / 0 — world shuffler, director, `_avsSequence.StartDynamicDropEnd`, starts **`LastMinute()`**, at 0: **Silent + `PlayThematicSavasana()`**. |
| **`LastMinute()`** | `EnsureLegacyCoroutineCountdown`, `SetAllowTransitionFromEnvironmentToFreeplay(false)`, `StopShuffle`, `Play_sfx_EndInteractive`, tone `WaitUntil`s, director activate/disable, **FrozenFreeplay**, wait ≤15s, **`FadeOut()`**, **`tutorial.StopTutorial()`**, wait until `CountdownThisSection` is 0. |
| **`PlaygroundStageHandler.ProtocolStacksPlaygroundCoroutine`** | Protocol Stacks–style **countdown waits** + steps; ends at `CountdownThisSection == 0` → **`MarkComplete()`** (no standard-sequence milestones, no LastMinute, no thematic VO). |
| **`SavasanaStageHandler.Enter`** | Always **`PlayAscendingClosing()`** + AVS drop — matches **Protocol Stacks ascending**, not Integration/Skills **thematic** closing. |
| **`SkillsTraining.asset` / `Integration.asset`** | Sequence definition assets; verify stage lists and opening variants match **`OpeningStageHandler`** (Skills / Integration / Preparation, not necessarily `Opening_PS_Ascending`). |
| **`Sequencer` inspector defs** | `peaceDefinition`, `narrativeDefinition`, `surrenderDefinition`, `firefliesDefinition`, `kindnessDefinition`, `mettaDefinition` — to be wired by `gameMode` / `contentPack` like `GetSequenceDefinitionForProtocolStacks()`. |

---

## Target Architecture

1. **Integration** and **Skills Training** use **`SequenceDefinition`** assets — **no** `StandardSequenceUpdate()` in `Update()`.
2. **Standard playground behavior** (milestones + **LastMinute**) lives in **one coroutine** in **`PlaygroundStageHandler`**, mirroring **`ProtocolStacksPlaygroundCoroutine`** (`yield` / `WaitUntil` on `TimeTrackerScript`, shared countdown helpers, `ForceSequenceAdvance` where appropriate). **Developer Comment:** the last minute behaviors should kick in as soon as we are in the last minute, which may involve skipping a bunch of steps.
3. **`PlayThematicSavasana()`** is invoked from **`SavasanaStageHandler`** for the **standard / thematic** savasana variant — **not** from `Sequencer` when `CountdownThisSection <= 0` (single responsibility, no duplicate VO path).
4. **`PlayAscendingClosing()`** remains for **Protocol Stacks ascending** (`Savasana_PsAscending` or equivalent).
5. **Cues → command system:** Any **Wwise cues** (or other triggers) that affect **Playground**, **Savasana**, **Tutorial**, or other migrated sections must go through the **sequence command** path — not bespoke `Sequencer` callbacks or silent side effects. Flow: emit a **`SequenceCommand`** → **`Sequencer.HandleSequenceCommand(...)`** → **`SequenceRunner.TryExecuteSequenceCommand(...)`** → the active (and optionally transitioning-out) **`IStageHandler`** that **`WatchesSequenceCommand`** / **`ExecuteSequenceCommand`**. New or updated cues for Integration / Skills Training should add or reuse entries in **`SequenceCommand`** (`SequenceTools.cs`) and the relevant handler(s) (`PlaygroundStageHandler`, `SavasanaStageHandler`, `TutorialStageHandler`, …) so behavior stays **stage-aware** and **variant-aware** (`StageVariant`). 

**Note:** For standard (thematic) sessions, VO closing may consolidate around something like a single `PlaySavasanaSequence(string savasanaSequenceType)` (parallel to `PlayOpeningSequence`), while still respecting handler ownership for when it fires.

> **Developer comment:** We should refactor  `WwiseVOManager.PlayThematicSavasana()` and `WwiseVOManager.PlaySAscendingClosing` into a single function, behaving analagous to `PlayOpeningSequence()` with an equivalent function to `StopOpeningSequence()` as well.

---

## Phase 1 — Sequence Selection and Assets

1. **Map CSV → `SequenceDefinition`**
   - Add a resolver (e.g. `GetSequenceDefinitionForStandardModes()`) that, given `CSVLoader.gameMode` + `contentPack`, returns:
     - **Integration** → `firefliesDefinition` / `kindnessDefinition` / `mettaDefinition` (and/or a single **`Integration`** asset during rollout).
     - **Skills Training** → `peaceDefinition` / `narrativeDefinition` / `surrenderDefinition` (and/or **`SkillsTraining`** asset).
     - **Preparation** → align with Skills Training family or a dedicated asset per design. **Developer Note:** "Preparation" and "Preperation" were old names for "Skills Training", we only need "Skills Training". This should be kept in mind for other changes throughout this document.
2. **Wire session start** so **`sequenceRunner.StartSequence(def)`** uses that resolver (same entry points as `StartTrueStart` / opening / `GameValues` as appropriate).
3. **Asset hygiene**
   - Unique `displayName` per asset (e.g. fix **`Integration.asset`** if it still copies “Skills Training Sequence”).
   - Confirm **stage types / variants** match **`Opening_SkillsTraining`**, **`Opening_Integration`**, **`Opening_Preparation`** as intended — not **`Opening_PS_Ascending`** unless deliberate.
4. **Cues:** Audit Wwise (or other) posts that used to drive **standard sequence** behavior; each should call into **`HandleSequenceCommand`** with the right **`SequenceCommand`** (see **Target Architecture**, item 5).

**Exit criteria:** Starting a session from CSV selects the correct definition; Playground runs with **`Playground_Standard`** (or explicit variants if split by mode).

---

## Phase 2 — `PlaygroundStageHandler`: Standard Coroutine

1. **Branch in `Enter()`**
   - **Protocol Stacks ascending path** (current): `ProtocolStacksPlaygroundCoroutine(skipToEnd)`.
   - **Standard path** (Integration / Skills / Preparation): new **`StandardSequencePlaygroundCoroutine()`** when variant is **`Playground_Standard`** (or new enum values if added).
2. **Port logic** from **`StandardSequenceUpdate`** into the new coroutine:
   - Replace `standardSequenceMilestone*` flags with **coroutine flow** (`yield` / `WaitUntil`).
   - **Start1 (60s since playground):** `worldShuffler.ResetSoundscapeExclusions()`.
   - **Start2 (300s):** `worldShuffler.ResetColorWorlds()`.
   - **End1 (≤300s):** Shadow / Shruti exclusions (as today).
   - **End2 (≤180s):** Shruti queue, transition sound, `CloseSoundscapeQueue`, `_avsSequence.StartDynamicDropEnd(180f)`.
   - **End3 (≤60s):** run **LastMinute** behavior (SFX, tone waits, director, FrozenFreeplay, ≤15s, FadeOut, tutorial stop, drain to 0).
3. **Helpers**
   - Keep fallback countdown bootstrap local to handlers (small helper in `PlaygroundStageHandler`) to avoid reintroducing legacy `Sequencer` timeline ownership.
   - **`FadeOut()`** — keep on `Sequencer` as a public helper if the handler calls it, or duplicate minimally with a comment pointing to the canonical behavior.
4. **Completion:** When the coroutine finishes the standard timeline, **`MarkComplete()`** → runner advances to **Savasana**.

**Exit criteria:** Behavior matches legacy standard sequence under playtest while running entirely through handler coroutines.

**Developer Note:** I'll need a variant that skips to the "last minute" behaviors, mirroring "Skip Ascending"


---

## Phase 3 — `SavasanaStageHandler`: Thematic vs Ascending VO

1. **Branch `Enter(StageVariant variant)`**
   - **`Savasana_PsAscending`:** keep **`PlayAscendingClosing()`** (current Protocol Stacks behavior).
   - **`Savasana_Standard`:** call **`PlayThematicSavasana()`** instead of **`PlayAscendingClosing()`**; keep fundamental lock, director, music mode, AVS steps as required — **design pass** for Silent/music transitions so they happen once (either end of Playground coroutine or Enter, not both inconsistently).
2. **Remove** `wwiseVOManager.PlayThematicSavasana()` from **`Sequencer.StandardSequenceUpdate`** once Savasana owns it.
3. **`WaitForTimerToEnd()`:** confirm semantics still match **section end** for thematic sessions.

**Exit criteria:** One thematic VO path for standard sessions; Protocol Stacks ascending unchanged.

---

## Phase 4 — Remove Legacy `Update` Path and Clean Up `Sequencer`

1. Remove **`StandardSequenceUpdate()`** and from **`Update()`**; delete **`StandardSequenceUpdate`**, **`LastMinute`**, and milestone fields (or leave stubs only if external callers exist — grep first). Also delete `Sequencer.ProtocolStacksCoroutine` and connected legacy behaviors.
2. **`CSVLoader.UsesStandardSequenceUpdate`:** remove or narrow to a temporary shim; end state is **no** parallel timeline in `Update`.
3. **`ResetStandardSequenceMilestones()`:** delete or trim to what **`StartSequence`** still needs.

---

## Phase 5 — Testing Checklist

- **Integration** (+ each **content pack** if multiple defs): milestones, DynamicDropEnd, LastMinute, fade, tutorial stop, **single** thematic VO at savasana.
- **Skills Training** + **Preparation:** same.
- **Protocol Stacks** ascending: Playground + Savasana still use **Ascending** closing; no regression.
- **ForceSequenceAdvance** / dev skip: still advances standard coroutine waits.
- **Restart / `StartSequence`:** no stuck flags; time-since-playground and milestones reset correctly.
- **Cues:** For each Wwise (or external) cue that touches these flows, confirm it dispatches a **`SequenceCommand`** and that the intended handler receives it (no orphaned direct calls that bypass **`HandleSequenceCommand`**).

---

## Risks and Decisions

1. **Duplicate VO / music:** Legacy End4 **`PlayThematicSavasana`** can overlap with **`PlayAscendingClosing`** in Savasana — migration must assign **thematic** only to **`Savasana_Standard`** and remove the old End4 block. **Developer Note:** The savasanas are designed to play only one at a time, not the two separately. They shouldn't overlap.
2. **Asset stage variants:** Openings must match **`OpeningStageHandler`** (Skills / Integration / Preparation).
3. **`ProtocolStacksPlaygroundStart` + `Sequencer.ProtocolStacksCoroutine`:** Removed in Phase 4; Protocol Stacks flow now runs only via `SequenceDefinition` stages and handlers.
4. **Cues bypassing commands:** If a cue still calls `Sequencer` or music/VO directly, **`IStageHandler`** will not see it — hard-to-debug desyncs (wrong stage, wrong variant). Migration should eliminate those stragglers.

---

## Reference Files

| Area | File |
|------|------|
| Legacy standard sequence | `Assets/Scripts/Sequencing/Sequencer.cs` — `StandardSequenceUpdate`, `LastMinute`, `FadeOut` |
| Playground coroutine pattern | `Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs` — `ProtocolStacksPlaygroundCoroutine` |
| Savasana VO | `Assets/Scripts/Sequencing/Handlers/SavasanaStageHandler.cs` |
| Mode gate | `Assets/Scripts/Sequencing/SequenceRunner.cs` — `GetSequenceDefinitionForCurrentCsvSession()` |
| Sequence definitions | `Assets/Scripts/Sequencing/SkillsTraining.asset`, `Integration.asset`, `Sequencer` inspector defs |
| Runner lifecycle | `Assets/Scripts/Sequencing/SequenceRunner.cs` — `StartSequence`, `ResetStandardSequenceMilestones` / time reset |
| Command dispatch | `Assets/Scripts/Sequencing/SequenceTools.cs` — `SequenceCommand`; `Sequencer.HandleSequenceCommand`; `SequenceRunner.TryExecuteSequenceCommand` |
