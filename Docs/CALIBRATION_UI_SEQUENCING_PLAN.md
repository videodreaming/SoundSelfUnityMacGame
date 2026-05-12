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
| 6 | Each stage names a **recommended LLM** at the **top** of that stage; **before starting work on a new stage, switch the chat to that model** (Composer 2 vs Opus 4.7). |
| 7 | After each major coding pass: **`git commit`** with a clear title, then **append the full commit hash** (and short 7-char prefix) to this document under that stage’s **Commit recorded** subsection. |
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
- `CalibrationStageHandler` wires **`UIManager` next-step events** and jumps **Headphone → Microphone → Vibro → LightGlasses → choice screen**, with **no** `Play_Calibration_Sequence`, **no** `Calibration_Sequence` switches, **no** intro/conclusion screens, **no** cue bridge, **no** `MarkComplete()`.
- `UIManager.SetCalibrationScreen` **does not handle** `CalibrationUI.Introduction` or `CalibrationUI.Conclusion` (enum exists; switch is incomplete).
- `OpeningStageHandler` already calls **`calibrationMenu.StopCalibrationSequence()`** on `Enter` — keep that contract so Opening tears down calibration audio.

**`TutorialPortions` enum** remains at the bottom of `CalibrationMenu.cs` and still matches the Wwise switch strings — reuse or relocate to a sequencing namespace when implementing (avoid duplication).

---

## Target architecture (concise)

1. **`CalibrationStageHandler`** owns: definition of **ordered calibration steps** (from `StageVariant` or a small data object), **Wwise switch index** aligned with `TutorialPortions` (subset for “NoVibro”), **subscriptions** to `UIManager` **next / back / conclusion / troubleshoot** events, **`MarkComplete()`** only when **both** conclusion conditions are met (see **Decisions**), **`LocalCleanup()`**: unsubscribe, **stop calibration VO/sequence**, stop any calibration-specific listeners.
2. **Wwise music-sync callbacks** should **not** silently apply gameplay side effects. They should forward into **`Sequencer.HandleSequenceCommand(...)`** for commands the **calibration handler watches** (matches `StandardSequence-StageHandler-Migration.md`). Side effects (`ImitoneVoiceInterpreter`, `LightControl`) then run **inside the handler** (or a single thin helper it calls) so behavior stays **stage-scoped**.
3. **UI** remains dumb: buttons invoke `UIManager` methods → **events only**. Optional later: `UIManager` methods to set **“interaction locked”** visual state when the handler asks for “waiting on cue” (you implement visuals).
4. **Background bed (“environment” / linear menu music)** per your convention: **start on entry** to **Welcome / SetMenu** and/or **Calibration** (idempotent API); **stop on entry** to stages that should not hear it (**Opening**, **Tutorial**, **Playground**, etc. — explicit list in implementation). Extract ambient lifecycle from `MusicSystem1` into **`LinearMusic`**; see existing **`Docs/MUSIC_ENVIRONMENT_MODE_WWISE_STATE_REFACTOR_PLAN.md`** for Wwise contract (State `MusicEnvironmentMode`, `Play_AMBIENT_ENVIRONMENT_LOOP`, delayed stop) — **`LinearMusic`** should encapsulate that contract without duplicating “double Play” bugs.
5. **Sequence variants**: add **`StageVariant`** entries (e.g. `Calibration_Album`, `Calibration_NoVibro`) and **two `SequenceDefinition` ScriptableObject stubs** in Unity that only differ by **which Calibration variant** is used for the Calibration stage (full wiring later).
6. **Back navigation:** UI step back must pair with **Wwise rewind / re-post** (not switch-only); implementation details in Stage C once timeline behavior is validated in Wwise.

---

## Decisions (locked)

| Topic | Decision |
|--------|-----------|
| **Back vs Wwise** | **Rewind / re-post** in Wwise is required when going back—not UI + `Calibration_Sequence` switch alone. |
| **Introduction / Conclusion** | **No new prefab assets required**; use the same pattern as other calibration screens (serialized `GameObject` roots on `UIManager`, toggled like Headphone/Mic). Stage B adds the fields and `SetCalibrationScreen` cases; you wire hierarchy that already lives on the calibration canvas. |
| **`EndThisSequenceStage` vs calibration** | Pick the **most elegant overall** design: **recommended** — keep **Next Step / Back** as **`UIManager` `Action`s** subscribed **only** by **`CalibrationStageHandler`**; add **`SequenceCommand`** entries for **Wwise-driven** calibration cues (and conclusion “VO done” when known). Avoid overloading **`EndThisSequenceStage`** for per-section Next unless it clearly reduces complexity. Revisit only if a single global button must mean one thing across stages. |
| **`MarkComplete()` (calibration exit)** | **Both** required: **(1)** user has pressed the **final** conclusion control **and** **(2)** conclusion **VO has finished**. Note: **often the button will be pressed before VO ends**—the handler must track **both** flags and only call **`MarkComplete()`** when **both** are true (order-independent). |
| **Conclusion “VO done” cue** | A cue **exists**; exact **`userCueName` TBD**—discover via logging / Wwise test in Stage C or E. |

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

---

### Stage A — Documentation + Unity-only prep

| **Recommended LLM** | **Opus 4.7** |
|---------------------|--------------|

**Before you start:** Set this chat session to **Opus 4.7** before doing Stage A work (Unity checklist, naming, hierarchy).

**Goal:** Align editor-facing naming and hierarchy with the plan. **Stage A is mostly Unity layout**; a **naming pass** for **Next Step** / **LightGlasses** may already exist in the repo from implementation—confirm in Editor after pull.

**Spelling**

- **Calibration** = correct (one **l**). **Callibration** is a common misspelling.
- Hierarchy name **Lightglasses** (plural) is fine for a GameObject; code uses **`CalibrationUI.LightGlasses`** and **`lightGlassesScreen`** on `UIManager` to match.

**Repo reference (optional — already applied in this branch)**

- `UIManager`: `*NextStepButtonPress`, `On*NextStepPress`, `CalibrationUI.LightGlasses`, `lightGlassesScreen`.
- `CalibrationScreen`: `NextStepButtonPress`, `OnNextStepButtonPress`.
- `CalibrationStageHandler`: subscribes to the `On*NextStepPress` events.
- Scenes / prefab: `MainGame.unity`, `Menu.unity`, `UIManager.prefab`, `Sandbox-Test.unity` — button method names and **Next Step** copy where listed; **`Menu.unity`** object **`Section Calibration Lightglasses`** (matches your rename from Lightglass).

**You in Unity**

- Confirm every calibration primary button still calls the right **`UIManager`** method (**`HeadphoneNextStepButtonPress`**, **`MicrophoneNextStepButtonPress`**, **`VibroAcousticNextStepButtonPress`**, **`LightGlassesNextStepButtonPress`**) — Unity should keep serialized links after YAML renames; if any show **Missing**, reassign.
- Confirm **`CalibrationScreen`** components: **On Next Step Button Press** (was “Next Screen”) still lists the right persistent calls; re-wire if Unity cleared a slot when renaming the `UnityEvent` field.
- Add a **Back** button to the shared calibration chrome **except** on **Section Calibration Start** (see **Stage A — layout notes** below); leave unwired until Stage B unless you add a stub method with permission.
- Create **empty** (or duplicate) **`SequenceDefinition`** assets: e.g. `Sequence_Calibration_Album_Stub`, `Sequence_Calibration_NoVibro_Stub` — **do not** switch the live pack to them until Stage F.
- **Introduction / Conclusion:** no new prefab required—ensure **screen roots** (empty **GameObjects**) exist like `headphoneScreen` so Stage B can assign them on `UIManager`.

### Stage A — layout notes (dots, Start, Conclusion)

**Dots parent**

1. Under your shared calibration chrome (same level as the section cards), create an empty **RectTransform** GameObject, e.g. **`Dots`**.
2. Add **Horizontal Layout Group** (optional): child alignment **Middle Center**, spacing **8–12**, child force expand **off** so dot width stays fixed.
3. Pick **one** strategy (code in a later stage will match what you choose):
   - **A — Empty container:** leave **`Dots`** with **no** children; we will **instantiate** a small dot prefab at runtime from script, or duplicate a template dot.
   - **B — Fixed pool:** add e.g. **8** dot **Image** children, **disable** extras by default; code will **SetActive** only the first *N* and set “filled” vs “empty” sprite/color.
4. Keep **one** child or prefab that represents a single dot (filled + empty states can be two sprites or color tint)—design can refine later.

**Section Calibration Start**

- **No Back button** on this screen: either omit the Back object from this section’s hierarchy or place it **inactive** here only. Other sections show Back when Stage B enables it per step.

**Section Calibration Conclusion**

- Same structure as other calibration sections: **root** (for `UIManager` **conclusion** reference in B), **title/body**, primary control labeled **Next Step** or a dedicated **Done / Continue** if you prefer (wire in B to the right `UIManager` method).
- Per locked decisions: completion needs **button + VO-done cue**; Stage E adds gating—you only need layout + button reference here.

**Tests:** N/A (editor layout); after confirming wiring, quick Play Mode: one **Next Step** press per section still fires logs / screen change.

**Regression review:** N/A.

**Suggested git commit message:** `feat(ui): calibration Next Step naming and LightGlasses wiring` (or split docs vs Unity as you prefer).

**Commit recorded**

- `a7f0dfc094f12d6455ee27e036b5264a5dd345dd` (`a7f0dfc`) — *new plan for refactor of calibration*
- `f4d43dd8f77d3c6a265bdfc8872f03a96c11526e` (`f4d43dd`) — *feat(ui): calibration Next Step naming, LightGlasses, plan Stage A notes*

---

### Stage B — `UIManager` + `CalibrationStageHandler` contract (screens, events, no Wwise yet)

| **Recommended LLM** | **Composer 2** |
|-----------------------|----------------|

**Before you start:** Set this chat session to **Composer 2** before the Stage B coding pass.

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

**Suggested git commit message:** `feat(ui): calibration screens, back event, progress dots API`.

**Commit recorded**

- *(not yet — paste full `git rev-parse HEAD` after committing this stage)*

---

### Stage C — Wwise parity: play/stop, switches, cue → `SequenceCommand`

| **Recommended LLM** | **Opus 4.7** |
|---------------------|--------------|

**Before you start:** Set this chat session to **Opus 4.7** before the Stage C pass (Wwise + cross-cutting sequencing).

**Goal:** Restore **`Play_Calibration_Sequence`** / **`Stop_Calibration_Sequence`** / **`Calibration_Sequence`** switch names from legacy; move cue handling to **sequence commands**; implement **back** as **rewind / re-post** per locked decision (coordinate with Wwise authoring).

**Code (with permission)**

- Add **`SequenceCommand`** values for calibration cues you want centralized (minimal set mirroring legacy):  
  e.g. `CueCalibrationMicrophoneOn`, `CueCalibrationMicrophoneOff`, `CueCalibrationAvsStart`, `CueCalibrationAvsEnd` (exact names to match Wwise `userCueName` strings / team convention).
- **`CalibrationStageHandler`**: `WatchesSequenceCommand` includes those; add **`EndThisSequenceStage`** only if still needed for a global control—prefer the locked “elegant” split (see **Decisions**).
- Implement a **small MonoBehaviour** (e.g. `CalibrationWwiseMusicSyncRelay`) on the same object that posts `Play_Calibration_Sequence`, registered for **`AK_MusicSyncUserCue`**, calling `_sequencer.HandleSequenceCommand` — **or** extend an existing Wwise bridge if you already have a pattern on `WwiseVOManager` (prefer **one** clear owner).
- **`CalibrationMenu`**: keep as **thin façade** if useful (`StartCalibrationSequence` / `StopCalibrationSequence` call Wwise) so **`OpeningStageHandler`** does not need a signature change; **or** move posts entirely into **`CalibrationStageHandler`** and update **`OpeningStageHandler`** to call **`Stop_Calibration_Sequence`** via sequencer/handler — pick one; avoid two unrelated owners posting Play/Stop.

**You in Unity**

- Add **`CalibrationWwiseMusicSyncRelay`** (or chosen component) to the object that should receive Ak callbacks (often the object posting the event).
- Verify **AkGameObj** / listener routing matches previous working setup.

**Tests:** Play mode: start calibration → hear sequence; advance portions → switch changes in Wwise remote; fire mic cues → **Imitone** toggles; AVS cues → **lights**; stop on exit calibration / enter Opening; **back** invokes rewind/re-post and stays in sync with UI.

**Regression review:** No other handlers accidentally watch the new commands; Opening still stops calibration.

**Suggested git commit message:** `feat(sequence): calibration Wwise play/stop, switches, cue commands`.

**Commit recorded**

- *(not yet — paste full hash after committing this stage)*

---

### Stage D — `LinearMusic` extraction + entry-based start/stop

| **Recommended LLM** | **Opus 4.7** |
|---------------------|--------------|

**Before you start:** Set this chat session to **Opus 4.7** before the Stage D pass (audio lifecycle refactor).

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

**Suggested git commit message:** `feat(audio): LinearMusic for environment bed; menu/calibration entry hooks`.

**Commit recorded**

- *(not yet — paste full hash after committing this stage)*

---

### Stage E — Button “waiting for cue” + conclusion gating (behavior hooks; visuals yours)

| **Recommended LLM** | **Composer 2** |
|-----------------------|----------------|

**Before you start:** Set this chat session to **Composer 2** before the Stage E pass.

**Goal:** When user presses **Next Step**, handler enters **“pending advance”** state: optionally notify UI to **disable / gray** (you implement styling); **do not** advance Wwise portion / UI until **allowed** (either immediately if no gating cue, or when **`SequenceCommand`** from Wwise arrives). **Conclusion:** track **button pressed** and **VO-done cue** (name from test); **`MarkComplete()`** only when **both** are true (see **Decisions**).

**Code (with permission)**

- Handler state machine: `CanAdvance`, `RequestAdvance()`, `OnCalibrationCueAllowAdvance()` (names illustrative).
- Optional `UIManager` hooks: `SetCalibrationNextButtonInteractable(bool)` or event `OnCalibrationNavigationStateChanged`.

**You in Unity**

- Hook interactable / CanvasGroup / animator to the above when you are ready.

**Tests:** Spam Next during VO → **no** double skip; when cue fires → exactly one step advances; conclusion only completes sequence when **VO done + button** both satisfied.

**Regression review:** `UIManagerTiming` debounce still OK with interactable false (ensure no stuck state).

**Suggested git commit message:** `feat(sequence): calibration next/conclusion gated on Wwise cues`.

**Commit recorded**

- *(not yet — paste full hash after committing this stage)*

---

### Stage F — Variants: `Calibration_Album`, `Calibration_NoVibro` + shorter progress dots

| **Recommended LLM** | **Composer 2** |
|-----------------------|----------------|

**Before you start:** Set this chat session to **Composer 2** before the Stage F pass.

**Goal:** **`StageVariant`** entries + handler reads **which portions exist** (skip Vibro = remove that step and **do not** visit that switch). **Dots** `total` = step count. **SequenceDefinition** stubs point at these variants.

**Code (with permission)**

- `SequenceTools.cs`: add variants (spelling: **`Calibration_NoVibro`**, not “Callibration”).
- `CalibrationStageHandler`: build step list from variant; **back** respects skipped steps.

**You in Unity**

- Assign **`SequenceDefinition`** stub assets on packs **only when testing** those flows.

**Tests:** NoVibro: never shows vibro screen; Wwise never receives `Vibration` switch (verify in Wwise log); dot count matches.

**Regression review:** Default `Calibration_Default` matches current full path.

**Suggested git commit message:** `feat(sequence): calibration stage variants and sequence stubs`.

**Commit recorded**

- *(not yet — paste full hash after committing this stage)*

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

We **reproduce legacy Wwise** (`Play_Calibration_Sequence`, `Calibration_Sequence` switches, four user cues) under **`CalibrationStageHandler`**, route cues through **`SequenceCommand`**, complete **`UIManager`** calibration screens (intro/conclusion, back, dots), extract **environment bed** to **`LinearMusic`**, add **cue-gated navigation** and **dual-condition conclusion completion**, then add **variants + SO stubs**. **Back** requires **Wwise rewind/re-post**. You handle **Unity wiring and visuals**; approved code lands in staged commits with hashes recorded above.

**Current focus:** **Stage A** — Unity prep + doc; next coding stage after A is complete is **Stage B** (switch chat to **Composer 2** before starting B).
