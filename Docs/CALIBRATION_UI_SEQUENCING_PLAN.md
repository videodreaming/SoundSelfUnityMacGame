# Calibration UI and sequencing upgrade — implementation plan

## Purpose

Move calibration from the legacy `CalibrationMenu` flow (commented reference in `Assets/CalibrationMenu.cs`) to the **sequence runner + stage handler** model, driven by **`CalibrationStageHandler`**, **`UIManager.SetCalibrationScreen`**, and **Wwise** behavior preserved from the legacy script. Support **forward and back** navigation, **variable-length** calibration definitions (progress dots), **two sequence-definition stubs** for future variants, **linear ambient / environment bed** via a new **`LinearMusic`** component (extracted from `MusicSystem1`), and a **later** UX pass for **next/conclusion buttons waiting on cues** (you own gray-out / animation).

This document is the agreed roadmap. **No code changes** should be made until you approve the stage you are about to execute.

---

## Ground rules (from you)

| # | Rule |
|---|------|
| 0 | You work **one stage at a time**. |
| 1 | I ask **decision questions** when there are real forks. |
| 2 | **No code changes** without your explicit permission for that pass. |
| 3 | I **direct Unity Editor steps** (prefabs, buttons, references, ScriptableObjects) where needed. |
| 4 | Each implementation stage ends with a **test round** (Editor play mode + Wwise profiler / logs as applicable). |
| 5 | Each coding pass ends with a **regression review** (grep + behavior checklist below). |
| 6 | Each stage recommends **Composer 2** vs **Opus 4.7** (see per-stage notes). |
| 7 | After each major coding pass: **`git commit`** with a clear title (I perform the commit when you approve the pass). |
| 8 | Nothing is “assumed done” unless it appears **in a stage**. |
| 9 | Prefer **simple, readable** designs over clever abstractions. |

---

## Source-of-truth audit (what the legacy script actually did)

From the **commented** `CalibrationMenu` (`Assets/CalibrationMenu.cs`):

| Concern | Legacy behavior |
|--------|------------------|
| Start calibration audio | `AkSoundEngine.PostEvent("Play_Calibration_Sequence", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, CalibrationCallBackFunction, null)` |
| Stop calibration audio | `AkSoundEngine.PostEvent("Stop_Calibration_Sequence", gameObject)` |
| Tutorial / portion progression | `AkSoundEngine.SetSwitch("Calibration_Sequence", portionName, gameObject)` where `portionName` is the **`TutorialPortions` enum** string (`Intro`, `Volume`, `Mic`, `Vibration`, `Lights`, `End`, `TechnicalIssues`) |
| First entry into config UI | On “start config”: `SetSwitch("Calibration_Sequence", "Intro", ...)` |
| Music-sync user cues | **`Cue_Microphone_ON` / `Cue_Microphone_OFF`** → `ImitoneVoiceIntepreter.SetGameOn(true/false)`; **`Cue_AVS_Calibration_Start` / `Cue_AVS_Calibration_End`** → `LightControl` strobes / init |
| UI | Old canvas / marks / diagrams / `nextButton` visibility (especially hide Next at `TutorialPortions.End`) |

**Current code today**

- `CalibrationMenu` is a **stub** (`StartCalibrationSequence` / `StopCalibrationSequence` log only).
- `CalibrationStageHandler` wires **`UIManager` next events** and jumps **Headphone → Microphone → Vibro → LightGlass → choice screen**, with **no** `Play_Calibration_Sequence`, **no** `Calibration_Sequence` switches, **no** intro/conclusion screens, **no** cue bridge, **no** `MarkComplete()`.
- `UIManager.SetCalibrationScreen` **does not handle** `CalibrationUI.Introduction` or `CalibrationUI.Conclusion` (enum exists; switch is incomplete).
- `OpeningStageHandler` already calls **`calibrationMenu.StopCalibrationSequence()`** on `Enter` — keep that contract so Opening tears down calibration audio.

**`TutorialPortions` enum** remains at the bottom of `CalibrationMenu.cs` and still matches the Wwise switch strings — reuse or relocate to a sequencing namespace when implementing (avoid duplication).

---

## Target architecture (concise)

1. **`CalibrationStageHandler`** owns: definition of **ordered calibration steps** (from `StageVariant` or a small data object), **Wwise switch index** aligned with `TutorialPortions` (subset for “NoVibro”), **subscriptions** to `UIManager` **next / back / conclusion / troubleshoot** events, **`MarkComplete()`** only when **conclusion** requirements are met (see open questions), **`LocalCleanup()`**: unsubscribe, **stop calibration VO/sequence**, stop any calibration-specific listeners.
2. **Wwise music-sync callbacks** should **not** silently apply gameplay side effects. They should forward into **`Sequencer.HandleSequenceCommand(...)`** for commands the **calibration handler watches** (matches `StandardSequence-StageHandler-Migration.md`). Side effects (`ImitoneVoiceInterpreter`, `LightControl`) then run **inside the handler** (or a single thin helper it calls) so behavior stays **stage-scoped**.
3. **UI** remains dumb: buttons invoke `UIManager` methods → **events only**. Optional later: `UIManager` methods to set **“interaction locked”** visual state when the handler asks for “waiting on cue” (you implement visuals).
4. **Background bed (“environment” / linear menu music)** per your convention: **start on entry** to **Welcome / SetMenu** and/or **Calibration** (idempotent API); **stop on entry** to stages that should not hear it (**Opening**, **Tutorial**, **Playground**, etc. — explicit list in implementation). Extract ambient lifecycle from `MusicSystem1` into **`LinearMusic`**; see existing **`Docs/MUSIC_ENVIRONMENT_MODE_WWISE_STATE_REFACTOR_PLAN.md`** for Wwise contract (State `MusicEnvironmentMode`, `Play_AMBIENT_ENVIRONMENT_LOOP`, delayed stop) — **`LinearMusic`** should encapsulate that contract without duplicating “double Play” bugs.
5. **Sequence variants**: add **`StageVariant`** entries (e.g. `Calibration_Album`, `Calibration_NoVibro`) and **two `SequenceDefinition` ScriptableObject stubs** in Unity that only differ by **which Calibration variant** is used for the Calibration stage (full wiring later).

---

## Decision questions (please answer when we reach the relevant stage)

1. **Back button vs Wwise**  
   When the user goes **back** one UI step, should we **only** move UI + set `Calibration_Sequence` to the **previous** portion name, or does Wwise **music timeline** need a dedicated **“rewind” / re-post** event? (This depends on how `Play_Calibration_Sequence` is authored; if unsure, we add logging first, then you run a Wwise test.)

2. **Introduction / Conclusion screens**  
   Do you want **dedicated prefab sections** under the calibration canvas (mirroring Headphone/Mic/…) for **Introduction** and **Conclusion**, or reuse **Welcome** for intro copy and only add **Conclusion** as a new screen?

3. **`EndThisSequenceStage` vs calibration-only commands**  
   Today, `UIManager.EndThisSequenceStageButtonPress` feeds **`SequenceCommand.EndThisSequenceStage`** (used globally). Should **“Next Section”** ever map to that command, or should calibration use **only** dedicated `SequenceCommand` values (e.g. advance / back) so **Opening** never accidentally consumes a stray `EndThisSequenceStage` during calibration? (Recommendation: keep **next/back** as `UIManager` **Actions** subscribed only by **`CalibrationStageHandler`**; add **new `SequenceCommand`s** only for **Wwise-driven** calibration cues.)

4. **Completion rule**  
   Confirm: **`MarkComplete()`** when **(A)** conclusion VO has finished **and** **(B)** user pressed the final button — or is **(B)** alone sufficient if VO always ends before the button is enabled?

5. **“End” cue for conclusion**  
   If Wwise does not expose a clear user cue name for “conclusion VO done,” we stage **logging** of `AK_MusicSyncUserCue` during conclusion and you capture the real cue name from a test session.

---

## Regression review checklist (run after each coding pass)

- `OpeningStageHandler.Enter` still stops calibration (`StopCalibrationSequence` / equivalent).
- No double subscription of `UIManager` events after **re-entering** Calibration or **domain reload** edge cases.
- `HandleSequenceCommand` for new calibration commands returns **handled** only when **Calibration** is active (no accidental `Sequencer` warning spam when another stage is current).
- **Imitone** `SetGameOn` matches cue parity with legacy (mic on/off cues).
- **LightControl** AVS calibration cues match legacy (null / inactive guards preserved).
- **Music**: entering **Opening** / **Playground** / etc. does not leave **ambient** running if the design says stop; **no double `Play_AMBIENT_ENVIRONMENT_LOOP`** (respect delayed-stop cancellation from environment refactor doc).
- **SequenceDefinition** packs: default session still runs; **stub** definitions are not referenced until you assign them.

---

## Stages (implementation order)

### Stage A — Documentation + Unity-only prep (no C# behavior change unless you allow tiny enum/switch completion)

**Goal:** Align editor-facing naming and hierarchy with the plan; optionally complete obvious gaps with your approval.

**You in Unity**

- Rename button text **“Next Session” → “Next Section”** on all calibration section cards that show it.
- Add a **Back** button to the shared calibration chrome (or per section, if designers prefer), **not** wired to logic yet (or wire to a new `UIManager` method that only fires an `Action` — only if you approve micro-code in this stage).
- Under **Dots**, prepare a **single parent** that will hold **runtime-spawned** dot instances *or* a fixed max with enable/disable — your call in editor; the code stage will drive **count + active index**.
- Create **empty** (or duplicate) **`SequenceDefinition`** assets: e.g. `Sequence_Calibration_Album_Stub`, `Sequence_Calibration_NoVibro_Stub` — **do not** switch the live pack to them until Stage F.
- If **Introduction** / **Conclusion** prefabs do not exist: duplicate one calibration section as a scaffold and strip content for designer pass.

**Tests:** N/A (editor layout).

**Regression review:** N/A.

**Git commit:** `docs/chore: calibration upgrade plan and Unity copy (Next Section)` (if only Docs + you handle Unity separately, commit may be docs-only).

**LLM:** **Opus 4.7** — structured Unity checklist and naming consistency.

---

### Stage B — `UIManager` + `CalibrationStageHandler` contract (screens, events, no Wwise yet)

**Goal:** `SetCalibrationScreen` supports **Introduction** and **Conclusion**; **Back** event exists; optional **`SetCalibrationProgressDots(int current, int total)`** API (or a tiny `CalibrationProgressView` component referenced by handler). **`CalibrationStageHandler`** owns a **step list** (headphone → … → conclusion) but can still **stub** Wwise calls behind `#if` or `// TODO` if you want to split.

**Code (with permission)**

- `UIManager`: complete `CalibrationUI` switch; add `OnCalibrationBackPress` (and if needed `OnCalibrationConclusionConfirmPress` separate from generic next).
- Serialize **introduction / conclusion** `GameObject` references like other screens.
- `CalibrationStageHandler`: replace hardcoded chain with **data-driven step index**; forward **next/back** to screen + (later) switch.

**You in Unity**

- Assign new serialized fields on **`UIManager`**.
- Wire **Back** button to new `UIManager` method.
- Wire **Conclusion** primary button to the appropriate `UIManager` method.

**Tests:** Enter calibration dev override → verify **every** screen toggles, **back** decreases step without skipping indices, dots **count** matches list length.

**Regression review:** Other screens (`SetWelcomeScreen`, choice screen) unchanged.

**Git commit:** `feat(ui): calibration screens, back event, progress dots API`.

**LLM:** **Composer 2** — localized UI/handler edits.

---

### Stage C — Wwise parity: play/stop, switches, cue → `SequenceCommand`

**Goal:** Restore **`Play_Calibration_Sequence`** / **`Stop_Calibration_Sequence`** / **`Calibration_Sequence`** switch names from legacy; move cue handling to **sequence commands**.

**Code (with permission)**

- Add **`SequenceCommand`** values for calibration cues you want centralized (minimal set mirroring legacy):  
  e.g. `CueCalibrationMicrophoneOn`, `CueCalibrationMicrophoneOff`, `CueCalibrationAvsStart`, `CueCalibrationAvsEnd` (exact names to match Wwise `userCueName` strings / team convention).
- **`CalibrationStageHandler`**: `WatchesSequenceCommand` includes those + `EndThisSequenceStage` if still needed for global button.
- Implement a **small MonoBehaviour** (e.g. `CalibrationWwiseMusicSyncRelay`) on the same object that posts `Play_Calibration_Sequence`, registered for **`AK_MusicSyncUserCue`**, calling `_sequencer.HandleSequenceCommand` — **or** extend an existing Wwise bridge if you already have a pattern on `WwiseVOManager` (prefer **one** clear owner).
- **`CalibrationMenu`**: keep as **thin façade** if useful (`StartCalibrationSequence` / `StopCalibrationSequence` call Wwise) so **`OpeningStageHandler`** does not need a signature change; **or** move posts entirely into **`CalibrationStageHandler`** and update **`OpeningStageHandler`** to call **`Stop_Calibration_Sequence`** via sequencer/handler — pick one; avoid two unrelated owners posting Play/Stop.

**You in Unity**

- Add **`CalibrationWwiseMusicSyncRelay`** (or chosen component) to the object that should receive Ak callbacks (often the object posting the event).
- Verify **AkGameObj** / listener routing matches previous working setup.

**Tests:** Play mode: start calibration → hear sequence; advance portions → switch changes in Wwise remote; fire mic cues → **Imitone** toggles; AVS cues → **lights**; stop on exit calibration / enter Opening.

**Regression review:** No other handlers accidentally watch the new commands; Opening still stops calibration.

**Git commit:** `feat(sequence): calibration Wwise play/stop, switches, cue commands`.

**LLM:** **Opus 4.7** — cross-cutting Wwise + enum + handler dispatch.

---

### Stage D — `LinearMusic` extraction + entry-based start/stop

**Goal:** New **`Assets/Scripts/MusicAndLight/LinearMusic.cs`** (MonoBehaviour, likely singleton or referenced from `Sequencer`) owns **environment ambient** entry/exit currently implemented as **`EnterMusicEnvironmentAudio` / `ExitMusicEnvironmentAudio`** in `MusicSystem1` (see grep locations ~2388–2436). **`MusicSystem1.SetMusicModeTo(Environment)`** should delegate to **`LinearMusic`** so all existing callers keep working.

**Code (with permission)**

- Implement **`LinearMusic`**: idempotent **EnsureEnvironmentAmbientPlaying** / **StopEnvironmentAmbient** (or mirror naming in music refactor doc), **cancel pending delayed stop** on re-entry.
- **`MusicSystem1`**: replace direct ambient state+post with calls into **`LinearMusic`**.
- **`SetMenuStageHandler.Enter`** (variants that show welcome / pre-calibration UI) and/or **`CalibrationStageHandler.Enter`**: call **idempotent start**.
- **`OpeningStageHandler.Enter`** (and other agreed stage handlers): call **stop** for linear menu bed **on entry** (not exit), per your convention.

**You in Unity**

- Add **`LinearMusic`** component to the appropriate persistent scene object; wire reference if not singleton.

**Tests:** Welcome → Opening: ambient stops; re-enter welcome: ambient starts once (no stacked Play); `FadeOut`/Environment gameplay path still works if it still routes through `MusicSystem1`.

**Regression review:** Read **`Docs/MUSIC_ENVIRONMENT_MODE_WWISE_STATE_REFACTOR_PLAN.md`** invariants; grep for `Play_AMBIENT_ENVIRONMENT_LOOP` — single ownership path.

**Git commit:** `feat(audio): LinearMusic for environment bed; menu/calibration entry hooks`.

**LLM:** **Opus 4.7** — careful audio lifecycle refactor.

---

### Stage E — Button “waiting for cue” + conclusion gating (behavior hooks; visuals yours)

**Goal:** When user presses **Next Section**, handler enters **“pending advance”** state: optionally notify UI to **disable / gray** (you implement styling); **do not** advance Wwise portion / UI until **allowed** (either immediately if no gating cue, or when **`SequenceCommand`** from Wwise arrives). Same pattern for **Conclusion** final confirm once **`Cue_*`** name is known from tests.

**Code (with permission)**

- Handler state machine: `CanAdvance`, `RequestAdvance()`, `OnCalibrationCueAllowAdvance()` (names illustrative).
- Optional `UIManager` hooks: `SetCalibrationNextButtonInteractable(bool)` or event `OnCalibrationNavigationStateChanged`.

**You in Unity**

- Hook interactable / CanvasGroup / animator to the above when you are ready.

**Tests:** Spam Next during VO → **no** double skip; when cue fires → exactly one step advances.

**Regression review:** `UIManagerTiming` debounce still OK with interactable false (ensure no stuck state).

**Git commit:** `feat(sequence): calibration next/conclusion gated on Wwise cues`.

**LLM:** **Composer 2** — state machine + UI flags.

---

### Stage F — Variants: `Calibration_Album`, `Calibration_NoVibro` + shorter progress dots

**Goal:** **`StageVariant`** entries + handler reads **which portions exist** (skip Vibro = remove that step and **do not** visit that switch). **Dots** `total` = step count. **SequenceDefinition** stubs point at these variants.

**Code (with permission)**

- `SequenceTools.cs`: add variants (fix spelling: **`CalibrationNoVibro`** not “Callibration”).
- `CalibrationStageHandler`: build step list from variant; **back** respects skipped steps.

**You in Unity**

- Assign **`SequenceDefinition`** stub assets on packs **only when testing** those flows.

**Tests:** NoVibro: never shows vibro screen; Wwise never receives `Vibration` switch (verify in Wwise log); dot count matches.

**Regression review:** Default `Calibration_Default` matches current full path.

**Git commit:** `feat(sequence): calibration stage variants and sequence stubs`.

**LLM:** **Composer 2** — enum + data-driven steps.

---

## Testing notes (shared)

- Use **SequenceRunner** definition override in Editor (already logs a banner) for fast iteration.
- Capture **Wwise** user cue names with **`Debug.Log`** in the relay during Stage C/E if names are uncertain.
- After each pass: one **full linear run** from session start through calibration into **Opening** (or next real stage).

---

## Files likely touched (for navigation)

| Area | Files |
|------|--------|
| Legacy reference | `Assets/CalibrationMenu.cs` |
| Handler | `Assets/Scripts/Sequencing/Handlers/CalibrationStageHandler.cs` |
| UI | `Assets/Jinnbyte/SoundSelfUI/Scripts/UIManager.cs` |
| Menu stage | `Assets/Scripts/Sequencing/Handlers/SetMenuStageHandler.cs` |
| Opening / stop calibration | `Assets/Scripts/Sequencing/Handlers/OpeningStageHandler.cs` |
| Commands / variants | `Assets/Scripts/Sequencing/SequenceTools.cs` |
| Sequencer | `Assets/Scripts/Sequencing/Sequencer.cs` |
| Music | `Assets/Scripts/MusicAndLight/MusicSystem1.cs`, **new** `Assets/Scripts/MusicAndLight/LinearMusic.cs` |
| Migration guidance | `Assets/Scripts/Sequencing/docs/StandardSequence-StageHandler-Migration.md` |
| Environment contract | `Docs/MUSIC_ENVIRONMENT_MODE_WWISE_STATE_REFACTOR_PLAN.md` |

---

## Open note: `SetMenuStageHandler`

Current **`Menu_Ps_InteractiveOrMusic`** path **auto-completes** and branches — unrelated to calibration music. Stage D should only add **idempotent ambient start** to variants that actually show **welcome / pre-calibration** UI (`Menu_Welcome_PreCalibration` and any future menu variants you list), without changing adjunctive stub behavior unless you explicitly approve.

---

## Summary

We **reproduce legacy Wwise** (`Play_Calibration_Sequence`, `Calibration_Sequence` switches, four user cues) under **`CalibrationStageHandler`**, route cues through **`SequenceCommand`**, complete **`UIManager`** calibration screens (intro/conclusion, back, dots), extract **environment bed** to **`LinearMusic`**, add **cue-gated navigation** in a dedicated pass, then add **variants + SO stubs**. You handle **Unity wiring and visuals**; I handle **approved code** in staged commits with tests and regression review each time.

When you are ready, **approve Stage A or B** (or ask to reorder), and answer **Decision questions** that matter for the first code stage you open.
