# Calibration UI and sequencing upgrade — implementation plan

## Purpose

Move calibration from the legacy `CalibrationMenu` flow (commented reference in `Assets/CalibrationMenu.cs`) to the **sequence runner + stage handler** model, driven by **`CalibrationStageHandler`**, **`UIManager.SetCalibrationScreen`**, and **Wwise** behavior preserved from the legacy script. Support **forward and back** navigation, **variable-length** calibration definitions (progress dots), **`StageVariant`** stubs for alternate calibration flows (**Album**, **NoVibro**) on the **same** calibration stage, **linear ambient / environment bed** via a new **`LinearMusic`** component (extracted from `MusicSystem1`), and a **later** UX pass for **next/conclusion buttons waiting on cues** (you own gray-out / animation).

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
| 7 | After each major coding pass: **`git commit`** with a clear title **only after the user explicitly confirms in chat**; optionally note the hash in this document **in that same commit** (no separate “hash only” commits — see project rule **`.cursor/rules/soundself-agent-workflow.mdc`**). |
| 8 | Nothing is “assumed done” unless it appears **in a stage**. |
| 9 | Prefer **simple, readable** designs over clever abstractions. |
| 10 | **`.prefab` on disk:** prompt the user to **save Unity** before editing; ask permission if the edit could conflict with open prefab work. |
| 11 | **Calibration alternate flows** (`Album`, `NoVibro`) are **`StageVariant`** values on **`StageType.Calibration`**, not separate sequence-definition assets unless product requires a full alternate sequence. |
| 12 | **`.unity` scene files on disk:** **never** edit without **(1)** prompting the user to **save Unity** (all scenes + project) **and** **(2)** their **explicit permission** for **that** scene file (or an explicit “all listed scenes” scope). |
| 13 | Each stage’s **Checklist** uses **`[ ]` / `[x]`**; set **`[x]`** only when that line is **done and verified**. The agent updates checkboxes when completing work in-repo; you update them when finishing Unity-only steps. |

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
- `CalibrationStageHandler` (Stage B+) wires **`OnCalibrationNextStepPress` / `OnCalibrationBackPress` / `OnCalibrationConclusionConfirmPress`**, owns a **variant step list** (Start → … → Conclusion; `NoVibro` omits Vibro), and **does not** `MarkComplete()` until Stage E (button + VO-done). Still **no** Wwise parity in handler until Stage C.
- `UIManager.SetCalibrationScreen` handles **`CalibrationUI.Start`** and **`CalibrationUI.Conclusion`** via serialized `startScreen` / `conclusionScreen` roots (assign in Unity after Stage B code lands).
- `OpeningStageHandler` already calls **`calibrationMenu.StopCalibrationSequence()`** on `Enter` — keep that contract so Opening tears down calibration audio.

**`TutorialPortions` enum** remains at the bottom of `CalibrationMenu.cs` and still matches the Wwise switch strings — reuse or relocate to a sequencing namespace when implementing (avoid duplication).

---

## Target architecture (concise)

1. **`CalibrationStageHandler`** owns: definition of **ordered calibration steps** (from `StageVariant` or a small data object), **Wwise switch index** aligned with `TutorialPortions` (subset for “NoVibro”), **subscriptions** to `UIManager` **next / back / conclusion / troubleshoot** events, **`MarkComplete()`** only when **both** conclusion conditions are met (see **Decisions**), **`LocalCleanup()`**: unsubscribe, **stop calibration VO/sequence**, stop any calibration-specific listeners.
2. **Wwise music-sync callbacks** should **not** silently apply gameplay side effects. They should forward into **`Sequencer.HandleSequenceCommand(...)`** for commands the **calibration handler watches** (matches `StandardSequence-StageHandler-Migration.md`). Side effects (`ImitoneVoiceInterpreter`, `LightControl`) then run **inside the handler** (or a single thin helper it calls) so behavior stays **stage-scoped**.
3. **UI** remains dumb: buttons invoke `UIManager` methods → **events only**. Optional later: `UIManager` methods to set **“interaction locked”** visual state when the handler asks for “waiting on cue” (you implement visuals).
4. **Background bed (“environment” / linear menu music)** per your convention: **start on entry** to **Welcome / SetMenu** and/or **Calibration** (idempotent API); **stop on entry** to stages that should not hear it (**Opening**, **Tutorial**, **Playground**, etc. — explicit list in implementation). Extract ambient lifecycle from `MusicSystem1` into **`LinearMusic`**; see existing **`Docs/MUSIC_ENVIRONMENT_MODE_WWISE_STATE_REFACTOR_PLAN.md`** for Wwise contract (State `MusicEnvironmentMode`, `Play_AMBIENT_ENVIRONMENT_LOOP`, delayed stop) — **`LinearMusic`** should encapsulate that contract without duplicating “double Play” bugs.
5. **Calibration alternate flows**: use **`StageVariant`** entries **`Calibration_Album`** and **`Calibration_NoVibro`** (see `SequenceTools.cs`) with **`CalibrationStageHandler`** choosing step lists / Wwise behavior. **Do not** introduce separate **`SequenceDefinition`** assets for these unless the product needs an entirely different stage graph—not just skipping a calibration subsection.
6. **Back navigation:** UI step back must pair with **Wwise rewind / re-post** (not switch-only); implementation details in Stage C once timeline behavior is validated in Wwise.

---

## Decisions (locked)

| Topic | Decision |
|--------|-----------|
| **Back vs Wwise** | **Rewind / re-post** in Wwise is required when going back—not UI + `Calibration_Sequence` switch alone. |
| **Start / Conclusion screens** | **No new prefab assets required**; use the same pattern as other calibration screens (serialized `GameObject` roots on `UIManager`, toggled like Headphone/Mic). Stage B renames the enum entry `Introduction → Start` to match the **Section Calibration Start** GameObject, adds the `startScreen` / `conclusionScreen` fields, and extends `SetCalibrationScreen`. You wire hierarchy that already lives on the calibration canvas. |
| **`EndThisSequenceStage` vs calibration** | Pick the **most elegant overall** design: **recommended** — keep **Next Step / Back** as **`UIManager` `Action`s** subscribed **only** by **`CalibrationStageHandler`**; add **`SequenceCommand`** entries for **Wwise-driven** calibration cues (and conclusion “VO done” when known). Avoid overloading **`EndThisSequenceStage`** for per-section Next unless it clearly reduces complexity. Revisit only if a single global button must mean one thing across stages. |
| **`MarkComplete()` (calibration exit)** | **Both** required: **(1)** user has pressed the **final** conclusion control **and** **(2)** conclusion **VO has finished**. Note: **often the button will be pressed before VO ends**—the handler must track **both** flags and only call **`MarkComplete()`** when **both** are true (order-independent). |
| **Conclusion “VO done” cue** | A cue **exists**; exact **`userCueName` TBD**—discover via logging / Wwise test in Stage C or E. |
| **Next-Step button wiring (hybrid)** | **Generic** `UIManager.NextStepButtonPress` (+ `OnCalibrationNextStepPress`) used by **Start, Headphone, Microphone, VibroAcoustic, LightGlasses**; the handler advances based on its **current step index** (single source of truth). **Conclusion** uses a **dedicated** `UIManager.CalibrationConclusionConfirmButtonPress` (+ `OnCalibrationConclusionConfirmPress`) so the dual-condition completion (button + VO-done) is unambiguous. Retire the four per-section `*NextStepButtonPress` methods/events. **Conclusion no longer uses `EndThisSequenceStageButtonPress`.** |
| **Step progression storage** | **Centralized in one place** inside `CalibrationStageHandler` as a single ordered step list (one per `StageVariant`: `Default`, `Album`, `NoVibro`). Plain C# (no `.asset` needed for ~3 variants); readable enough that the full ordering for each variant is visible side-by-side. Revisit `.asset`-based step definitions only if variant count grows or non-engineers need to edit ordering. |
| **Welcome-screen music** | **`Play_Calibration_Sequence` stays at the Calibration stage** (handler-owned, starts in `CalibrationStageHandler.Enter`). The **Welcome menu uses Stage D's `LinearMusic` ambient bed**, not `Play_Calibration_Sequence`. So the user hears the ambient bed while reading the Welcome screen and continues to hear it under calibration (idempotent re-entry); the calibration's own Wwise music container layers in on top when Calibration begins. |

---

## Regression review checklist (run after each coding pass)

- `OpeningStageHandler.Enter` still stops calibration (`StopCalibrationSequence` / equivalent).
- No double subscription of `UIManager` events after **re-entering** Calibration or **domain reload** edge cases.
- `HandleSequenceCommand` for new calibration commands returns **handled** only when **Calibration** is active (no accidental `Sequencer` warning spam when another stage is current).
- **Imitone** `SetGameOn` matches cue parity with legacy (mic on/off cues).
- **LightControl** AVS calibration cues match legacy (null / inactive guards preserved).
- **Music**: entering **Opening** / **Playground** / etc. does not leave **ambient** running if the design says stop; **no double `Play_AMBIENT_ENVIRONMENT_LOOP`** (respect delayed-stop cancellation from environment refactor doc).
- **SequenceDefinition** packs: default session still runs; **`Calibration_Album` / `Calibration_NoVibro`** are chosen via the **Calibration** row’s **variant** on the pack’s sequence, not via extra stub sequence assets.

---

## Stages (implementation order)

### Stage checklist convention

- Each stage uses a **Checklist** with **`[ ]`** = not done, **`[x]`** = done and verified.
- The **agent** marks **`[x]`** for completed repo work; **you** mark **`[x]`** for Unity-only steps when finished.
- When a stage is finished, its checklist should be all **`[x]`** (or cancelled lines noted).

---

### Stage A — Documentation + Unity-only prep

| **Recommended LLM** | **Opus 4.7** |
|---------------------|--------------|

**Before you start:** Set this chat session to **Opus 4.7** before doing Stage A work (Unity checklist, naming, hierarchy).

**Goal:** Align editor-facing naming and hierarchy with the plan. **Stage A is mostly Unity layout**; a **naming pass** for **Next Step** / **LightGlasses** may already exist in the repo from implementation—confirm in Editor after pull.

**Spelling**

- **Calibration** = correct (one **l**). **Callibration** is a common misspelling.
- Hierarchy name **Lightglasses** (plural) is fine for a GameObject; code uses **`CalibrationUI.LightGlasses`** and **`lightGlassesScreen`** on `UIManager` to match.

#### Checklist — repo / agent

- [x] `UIManager`: `CalibrationUI.Start` / `lightGlassesScreen`; Stage B adds generic Next/Back/Conclusion events + `startScreen` / `conclusionScreen` (see Stage B checklist)
- [x] `CalibrationScreen`: `NextStepButtonPress`, `OnNextStepButtonPress`; XML note on UnityEvent type name
- [x] `CalibrationStageHandler`: variant step list + generic UI events (Stage B); `_activeCalibrationVariant` + `LogCalibrationVariantStub`
- [x] `SequenceTools.cs`: `StageVariant.Calibration_Album`, `Calibration_NoVibro`
- [x] Plan + **`.cursor/rules/soundself-agent-workflow.mdc`** (commits, **`.unity` permission**, prefab save prompt, no hash-only commits)
- [x] `MainGame.unity` / `Menu.unity`: `MicrophoneCalibrationScreen` → `CalibrationScreen` on UnityEvent targets *(landed before strict per-file `.unity` rule; **further `.unity` edits** require save + explicit permission — plan row 12)*

#### Checklist — Unity (Editor)

- [x] **Back** where needed; **omitted or inactive** on **Section Calibration Start**
- [x] **`Dots`** (`line`/`dot` pool, start disabled); optional **Horizontal Layout Group** on `Dots`
- [x] **Section Calibration Start** + **Section Calibration Conclusion** GameObject roots exist under the calibration canvas, ready for Stage B to add `startScreen` / `conclusionScreen` `[SerializeField]` slots on `UIManager` that you’ll drag these into *(see clarification below)*
> **Unity wiring (Stage B — you):** Rewire primary Next on Start / Headphone / Mic / Vibro / LightGlasses to **`UIManager.NextStepButtonPress`**, Conclusion to **`UIManager.CalibrationConclusionConfirmButtonPress`**, Back to **`UIManager.BackStepButtonPress`**. Until then, obsolete **`HeadphoneNextStepButtonPress`** etc. still forward to **`NextStepButtonPress`** so old inspector bindings keep working.
- [x] ~~Each existing section primary button calls the matching per-section UIManager method~~ — **deferred to Stage B.** Verifying temporary per-section wiring is busywork because Stage B rewires every section button to the single generic `UIManager.NextStepButtonPress`. As long as the current wiring isn't throwing `Missing`/`NullReference` errors at runtime, Stage A doesn't need it.
- [x] ~~Each `CalibrationScreen.OnNextStepButtonPress` → intended UIManager method~~ — **deferred to Stage B.** Same reason; the per-section UnityEvent fan-out goes away when the handler becomes the single owner of step progression.
- [x] ~~Play Mode narrow check (Headphone/Mic/Vibro/LightGlasses Next Step advances + logs)~~ — **deferred to Stage B.** The full-flow Play Mode test in Stage B (Start → Headphone → … → Conclusion via the generic Next button, with Back working) supersedes this narrower test.

> **What “roots ready for Stage B refs” means:** every other calibration screen (Headphone, Mic, …) is a child **GameObject** under the calibration canvas that `UIManager.SetCalibrationScreen(...)` toggles on/off via a `[SerializeField] private GameObject xxxScreen;` slot. **Start** and **Conclusion** need the same: a parent GameObject per screen (containing their layout + buttons) so that in Stage B we can add `startScreen` / `conclusionScreen` fields on `UIManager` and you can drag those GameObjects in. You’ve already re-added them, so this item is checked.

#### Layout notes (dots, Start, Conclusion)

**Dots (`UIManager` → `Dots`)** — Alternating **`line (n)`** / **`dot (n)`**; later code enables a prefix for **K** steps and highlights current; shorter variants leave extras disabled.

**Section Calibration Start** — No **Back** (omit or inactive).

**Section Calibration Conclusion** — Section pattern; Stage B wires; Stage E gates VO + button.

#### Tests / regression (Stage A)

- [ ] Play / wiring checks above (when applicable)
- [x] No formal code regression for layout-only scope until first Play pass

**Suggested git commit message:** `feat(ui): calibration Next Step naming and LightGlasses wiring` (or split docs vs Unity as you prefer).

**Commit recorded**

- `a7f0dfc094f12d6455ee27e036b5264a5dd345dd` (`a7f0dfc`) — *new plan for refactor of calibration*
- `c226a665c24101a5453ae54787916eba50a71006` (`c226a665`) — *feat(ui): calibration Next Step naming, LightGlasses, plan Stage A notes*

---

### Known issue — Choice Welcome ghosted/doubled text on first activation (handoff to other dev team)

**Status:** Unresolved. Being handed off to a separate dev team for investigation. Not blocking Stage B work, but Stage B's first Play Mode test will hit this and any visible "double text" should be assumed to be this same bug until proven otherwise.

#### Symptom

When `MainGame.unity` is played with **`Choice Welcome` deactivated in the Editor at scene-author time** (which is the intended state — `m_IsActive: 0` in scene YAML), the first time `UIManager.SetWelcomeScreen()` activates Choice Welcome via the sequencer, the **Game view** shows the title and the description text rendered **doubled**:

- The prefab/scene-default text values (e.g. title `Welcome to SoundSelf`, description `When you are ready, we will begin.`) render alongside
- The `HummingbirdPackTextBinder`-injected text values from the resolved Hummingbird pack (e.g. title `Adjunctive Dual-Stage`, description `Minimal VO for somatic focus...`)

The HummingbirdCall text appears **without proper spacing**, suggesting layout sizes were computed against the original (default) text length before the override was applied.

The doubling **persists indefinitely until the user manually disables and re-enables the Choice Welcome GameObject** in the Inspector during Play Mode — after one manual toggle, the screen renders correctly for the rest of the session.

**Scene view is unaffected** — only the Game view shows the doubling.

#### Reproduction

1. Open `Assets/Scenes/MainGame.unity`.
2. Verify `Canvas/UIManager/Choice Welcome` is **inactive** (`m_IsActive: 0`) — it should be, by serialization.
3. Press **Play**.
4. Wait for the sequencer to advance to `SetMenu` stage with variant `Menu_Welcome_PreCalibration` — `SetMenuStageHandler.Enter()` will call `UIManager.SetWelcomeScreen()`, which activates Choice Welcome.
5. Observe the Game view at the moment Choice Welcome fades in: title and description are rendered with overlapping default + HummingbirdCall text.

#### Workaround (confirms the diagnosis)

- In the Editor, manually set `Choice Welcome` `m_IsActive: 1` in the Hierarchy before pressing Play.
- Play Mode then loads with Choice Welcome already active; `HummingbirdPackTextBinder.OnEnable` fires during scene initialization (not mid-session), and the fade-in and text both render correctly with **no doubling**.

#### What's been ruled out (so the next team doesn't re-tread)

- **Not a duplicate `Choice Welcome` GameObject** — grep confirms only one instance of the string `Welcome to SoundSelf` in `MainGame.unity` (at the Choice Welcome prefab override).
- **Not a duplicate Choice Screen prefab being rendered** — there are 4 instances of the `Choice Screen.prefab` in the scene (`Choice Welcome`, `Choose Tutorial Type`, `Choice SS or Music`, `Choice Linear Music Length`); all four have `m_IsActive: 0` at scene load.
- **Not another stage handler also calling `SetWelcomeScreen()`** — only `SetMenuStageHandler.Enter(Menu_Welcome_PreCalibration)` does.
- **Not a Wwise / audio thread issue** — purely visual.
- **`UnsetAllScreens()` already runs before `welcomeScreen.SetActive(true)`** — and the other screens are already inactive at scene start.
- **Header Text uses legacy `UnityEngine.UI.Text`, not `TMP_Text`** (confirmed in `Choice Screen.prefab` — the `m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text` on the Header Text component). TMP-specific fixes (e.g. `ForceMeshUpdate`) therefore don't apply to the title; the description is also legacy Text.
- **No `Shadow` / `Outline` components** on the Header Text in the prefab.

#### Fixes that were tried and did **not** work

Both attempts were reverted in this commit's range so the next team has clean code to investigate from. Listing them here so they aren't repeated:

1. **`UIManager.ActivateScreen` refactor** that called `LayoutRebuilder.ForceRebuildLayoutImmediate` on the screen's `RectTransform` + `Canvas.ForceUpdateCanvases()` on every `Set*Screen` activation. No effect on the doubling.
2. **One-frame deferral** of the *first* screen activation after scene load (plus `UnsetAllScreens()` in `UIManager.Awake()` to clean the frame-0 state). No effect on the doubling.
3. **`HummingbirdPackTextBinder` forcing `tmp.ForceMeshUpdate()` + `LayoutRebuilder.ForceRebuildLayoutImmediate` on the parent rect** after text assignment. No effect on the doubling. (Plausible reason it didn't help: the text in question is legacy `Text`, not TMP; the legacy code path got the layout rebuild but the doubling still occurred.)

#### Suspicion / suggested next steps for the next team

- Investigate Unity's `CanvasUpdateRegistry` and the legacy `Graphic.OnPopulateMesh` invalidation path when a `Graphic`'s `GameObject` is enabled mid-frame while its parent's `CanvasGroup.alpha` is being animated by `ScreenFadeEffect.OnEnable` (alpha 0 → 1 over `fadeDuration`). The interaction between `OnEnable`-driven text mutation, `ContentSizeFitter`, and a same-frame `CanvasGroup.alpha = 0 → animating` may be what's leaving the original-vertex draw call alive in the canvas batch.
- Worth checking whether disabling `ScreenFadeEffect` on Choice Welcome eliminates the doubling — if yes, the fade interaction is the culprit; if no, look closer at `HummingbirdPackTextBinder.OnEnable` execution order vs. `Graphic` initial layout.
- Worth checking the description GameObject's `ContentSizeFitter` (VerticalFit: PreferredSize) — if removed temporarily, does the description still double? Isolates whether ContentSizeFitter is part of the chain.
- The legacy `UnityEngine.UI.Text` path inside `HummingbirdPackTextBinder` is one line — `legacy.text = value;`. If TMP migration were on the table, switching these texts to TMP and ensuring `ForceMeshUpdate()` runs after assignment is a candidate; but doing that purely to mask this bug is wrong — the underlying cause should be found first.

#### Files / lines relevant to the investigation

- `Assets/Scripts/CSVUtility/HummingBirdCommunications/HummingbirdPackTextBinder.cs` — the binder that injects text in `OnEnable`.
- `Assets/Jinnbyte/SoundSelfUI/Scripts/UIManager.cs` — `SetWelcomeScreen()` / `UnsetAllScreens()`.
- `Assets/Jinnbyte/SoundSelfUI/Scripts/ScreenFadeEffect.cs` — drives `CanvasGroup.alpha` 0 → 1 in `OnEnable`.
- `Assets/Scripts/Sequencing/Handlers/SetMenuStageHandler.cs` — the stage that calls `SetWelcomeScreen()` in the `Menu_Welcome_PreCalibration` variant.
- `Assets/Jinnbyte/SoundSelfUI/Prefab/Choice Screen.prefab` — Header Text definition + Card layout.

---

### Stage B — `UIManager` + `CalibrationStageHandler` contract (screens, events, no Wwise yet)

| **Recommended LLM** | **Composer 2** |
|-----------------------|----------------|

**Before you start:** Set this chat session to **Composer 2** before the Stage B coding pass.

**Goal:** `SetCalibrationScreen` covers **Start / Headphone / Mic / VibroAcoustic / LightGlasses / Conclusion**; introduce **generic Next Step** + **Back** + **Conclusion Confirm** events; `CalibrationStageHandler` becomes the single owner of step progression (centralized ordered list per variant). No Wwise changes yet.

**Why this shape (recap of locked design):** With multiple calibration variants (`Calibration_Default`, `Calibration_Album`, `Calibration_NoVibro`), **what Next/Back do has to change per variant** — e.g. in `NoVibro`, pressing Next on Headphone must skip Vibro and go straight to LightGlasses; in `Default` it goes to Vibro. The buttons themselves can't encode this (otherwise every variant would need its own button set), so the buttons stay **dumb and generic** and the **handler** consults the active variant's ordered step list to decide what "next" means each press. Conclusion stays dedicated because its press has different semantics (sets a button-pressed flag that pairs with VO-done in Stage E, rather than advancing).

#### Checklist — code (with permission)

- [x] `UIManager`: rename `CalibrationUI.Introduction` → `CalibrationUI.Start`; serialize `startScreen` and `conclusionScreen`; extend `SetCalibrationScreen` for **Start** and **Conclusion** (null-safe unset; log error if roots unassigned when selected)
- [x] `UIManager`: **generic** `NextStepButtonPress()` + `OnCalibrationNextStepPress` (debounce unchanged)
- [x] `UIManager`: `BackStepButtonPress()` + `OnCalibrationBackPress`
- [x] `UIManager`: `CalibrationConclusionConfirmButtonPress()` + `OnCalibrationConclusionConfirmPress`
- [x] `UIManager`: **`[Obsolete]`** per-section `*NextStepButtonPress` methods forward to **`NextStepButtonPress`**; removed the four `On*NextStepPress` **events** (no remaining subscribers — use **`OnCalibrationNextStepPress`** only)
- [x] `CalibrationStageHandler`: central step list per variant; subscribe to the **3** new events + troubleshoot; advance/back by index; `_conclusionButtonPressed` stub (Stage E: VO-done + `MarkComplete`)
- [x] Handler no longer subscribes to removed per-section events; **`WatchesSequenceCommand` returns `false`** for this handler until Stage E wires completion via cues / flags (previously `EndThisSequenceStage` could complete calibration prematurely)

#### Checklist — Unity (after code lands; permission required per `.unity` rule)

- [x] Add `startScreen` / `conclusionScreen` references on `UIManager` (drag the Section Calibration Start / Conclusion roots)
- [x] **Rewire all per-section primary buttons** (Start, Headphone, Mic, Vibro, LightGlasses) to call **`UIManager.NextStepButtonPress`**
- [x] **Rewire Conclusion primary button** to call **`UIManager.CalibrationConclusionConfirmButtonPress`** (no longer `EndThisSequenceStageButtonPress`)
- [x] Wire **Back** buttons (where present) to **`UIManager.BackStepButtonPress`**; confirm Start has no Back active

#### Tests / regression

- [x] Dev override: Start → Headphone → Mic → Vibro → LightGlasses → Conclusion all reachable via the **single** generic Next button
- [x] **Back** decreases step without skipping; disabled on Start
- [x] Conclusion primary → **`CalibrationConclusionConfirmButtonPress`**: **no visible / audible change in Play Mode is expected at this stage** — handler only sets an internal `_conclusionButtonPressed` flag and logs a stub line; **`MarkComplete()`** waits for Stage E (VO-done + button). **Optional check:** Console should show `CalibrationStageHandler: Conclusion confirm recorded...` once per valid press when wired correctly.
- [x] **`NextScreenButtonPress`:** grep under `Assets/` → **0 matches** (fully retired from YAML).
- [ ] Per-section **`m_MethodName: *NextStepButtonPress`** (obsolete forwards still work; rewire to **`NextStepButtonPress`** for a clean tree + no obsolete inspector bindings): **`MainGame.unity`** — `VibroAcousticNextStepButtonPress` ×1; **`Menu.unity`** — `HeadphoneNextStepButtonPress`, `MicrophoneNextStepButtonPress`, `VibroAcousticNextStepButtonPress`, `LightGlassesNextStepButtonPress` ×1 each; **`UIManager.prefab`** — `HeadphoneNextStepButtonPress`, `MicrophoneNextStepButtonPress` ×1 each. *(Agent grep, repo state as of this edit.)*
- [x] `SetWelcomeScreen` / choice screen / other stages’ subscriptions unchanged

**Audio / VO:** No calibration background bed or VO yet — **expected until Stage C** (Wwise parity) and later Stage E (conclusion VO-done cue). Stage B is UI + handler step index only. `feat(ui): generic calibration Next/Back + Conclusion confirm; centralized step list in handler`.

**Commit recorded**

- Stage B code + plan checklist update: *feat(ui): Stage B calibration — generic Next/Back/Conclusion, Start screen, variant step list* (run `git log -1 --oneline` on branch tip for hash)

---

### Stage C — Wwise parity: play/stop, switches, cue → `SequenceCommand`

| **Recommended LLM** | **Opus 4.7** |
|---------------------|--------------|

**Before you start:** Set this chat session to **Opus 4.7** before the Stage C pass (Wwise + cross-cutting sequencing).

**Goal:** Legacy Wwise play/stop/switches; cues → **`SequenceCommand`**; **back** = **rewind / re-post**.

#### Decisions (locked)

- **Relay shape:** fold into existing **`CalibrationMenu`** (one owner). Keeps `Sequencer.calibrationMenu` reference path; no new component to wire.
- **`Sequencer` ref into relay:** handler passes itself into `CalibrationMenu.StartCalibrationSequence(Sequencer)`. No inspector field, no `.unity` edits required.
- **Back behavior:** **`Stop_Calibration_Sequence` → `SetSwitch("Calibration_Sequence", portion)` → `Play_Calibration_Sequence`** in the same frame. Back **does not** wait for any cue — UI step decrement is immediate, audio rewinds underneath.
- **Forward behavior (Stage C):** advance step + `SetSwitch` only. **Cue-gating of the Next button is Stage E.**
- **Portion mapping (CalibrationUI → Wwise switch state):** `Start→Intro`, `Headphone→Volume`, `Microphone→Mic`, `VibroAcoustic→Vibration`, `LightGlasses→Lights`, `Conclusion→End`.
- **Cue routing:** `CalibrationMenu`'s `AK_MusicSyncUserCue` callback translates user-cue names to `SequenceCommand` and calls `Sequencer.HandleSequenceCommand`. Side-effects (Imitone gameOn, lights) live in `CalibrationStageHandler.ExecuteSequenceCommand` — relay does **not** touch gameplay state.

#### Checklist — code (with permission)

- [x] New **`SequenceCommand`** values matching Wwise `userCueName`: `CalibrationMicrophoneOn`, `CalibrationMicrophoneOff`, `CalibrationAvsStart`, `CalibrationAvsEnd`
- [x] **`CalibrationMenu`** = the relay: posts `Play_Calibration_Sequence` with `AK_MusicSyncUserCue` callback, `Stop_Calibration_Sequence`, `SetCalibrationPortionSwitch(portion)`, `RestartFromPortion(portion)`; idempotent Play; callback → `Sequencer.HandleSequenceCommand`
- [x] `CalibrationStageHandler`: watches the 4 new commands; `ExecuteSequenceCommand` does Imitone + `LightControl` side-effects; sets switch on **Enter** (`Intro`), **Next** (advance step → `SetSwitch`), **Back** (decrement step → `RestartFromPortion`)
- [x] Opening still stops calibration (no change — `OpeningStageHandler.Enter` already calls `_sequencer.calibrationMenu.StopCalibrationSequence()`)
- [x] `EndThisSequenceStage` **not** watched by `CalibrationStageHandler` (Stage E owns completion via VO-done + conclusion press)

#### Checklist — Unity

- [x] None required for code parity (no new components, no new inspector refs). Verify in Play Mode after pull.

#### Tests / regression

- [x] Play mode: full audio + switch + mic + lights + stop + **back** (Stop+Switch+Play) sync
- [x] No orphan `HandleSequenceCommand` warnings on the 4 new commands
- [ ] `OpeningStageHandler.Enter` still stops calibration cleanly (no double-Stop noise)
- [ ] Back during Vibro on `Calibration_NoVibro` variant skips correctly (step list omits Vibro)

**Suggested git commit message:** `feat(sequence): calibration Wwise play/stop, switches, cue commands`.

**Commit recorded**

- *(not yet — paste full hash after committing this stage)*

---

### Stage D — `LinearMusic` extraction + entry-based start/stop

| **Recommended LLM** | **Opus 4.7** |
|---------------------|--------------|

**Before you start:** Set this chat session to **Opus 4.7** before the Stage D pass (audio lifecycle refactor).

**Goal:** **`LinearMusic`**, delegate from **`MusicSystem1`**; idempotent start on menu/calibration **Enter**; stop on other stages **Enter**.

#### Checklist — code (with permission)

- [ ] **`LinearMusic`** (idempotent play, cancel delayed stop on re-entry)
- [ ] **`MusicSystem1`** delegates ambient lifecycle to **`LinearMusic`**
- [ ] **`SetMenuStageHandler.Enter(Menu_Welcome_PreCalibration)`**: start the bed (this is the "music starts at Welcome" requirement — `LinearMusic`, **not** `Play_Calibration_Sequence`)
- [ ] **`CalibrationStageHandler.Enter`**: re-assert the bed idempotently (no double-Play if it's already running from Welcome)
- [ ] **`OpeningStageHandler`** (+ others agreed: `Tutorial`, `Playground`, `Savasana`, etc.): stop on **Enter**

#### Checklist — Unity

- [ ] **`LinearMusic`** component + references

#### Tests / regression

- [ ] Welcome ↔ Opening; no double `Play_AMBIENT_ENVIRONMENT_LOOP`; `FadeOut` path OK
- [ ] **`MUSIC_ENVIRONMENT_MODE_WWISE_STATE_REFACTOR_PLAN.md`** invariants

**Suggested git commit message:** `feat(audio): LinearMusic for environment bed; menu/calibration entry hooks`.

**Commit recorded**

- *(not yet — paste full hash after committing this stage)*

---

### Stage E — Button “waiting for cue” + conclusion gating (behavior hooks; visuals yours)

| **Recommended LLM** | **Composer 2** |
|-----------------------|----------------|

**Before you start:** Set this chat session to **Composer 2** before the Stage E pass.

**Goal:** **Next Step** gated on cue; **Conclusion**: **`MarkComplete()`** only when **button + VO-done**.

#### Checklist — code (with permission)

- [ ] Handler pending-advance + cue unlock
- [ ] Optional `UIManager` hooks for interactable / `CanvasGroup` (you style)

#### Checklist — Unity

- [ ] Wire visuals to hooks when ready

#### Tests / regression

- [ ] Spam Next: no double skip; cue advances one step
- [ ] Conclusion dual condition
- [ ] No stuck state with debounce + disabled buttons

**Suggested git commit message:** `feat(sequence): calibration next/conclusion gated on Wwise cues`.

**Commit recorded**

- *(not yet — paste full hash after committing this stage)*

---

### Stage F — Variants: `Calibration_Album`, `Calibration_NoVibro` + progress dots

| **Recommended LLM** | **Composer 2** |
|-----------------------|----------------|

**Before you start:** Set this chat session to **Composer 2** before the Stage F pass.

**Goal:** Step list + **dots K** from **`StageVariant`**; pack uses same **Calibration** row, different **variant**.

#### Checklist — code (with permission)

- [ ] `CalibrationStageHandler`: **NoVibro** omits vibro step + Wwise; **Album** when specified; **back** respects list
- [ ] `UIManager`: e.g. **`SetCalibrationProgressVisual`** for `Dots` line/dot children

#### Checklist — Unity / data

- [ ] Pack **`SequenceDefinition`**: Calibration **variant** for test builds

#### Tests / regression

- [ ] NoVibro: no vibro UI/switch; dot **K** correct
- [ ] **`Calibration_Default`** = full path

**Suggested git commit message:** `feat(sequence): calibration variants drive step list and dots`.

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

We **reproduce legacy Wwise** (`Play_Calibration_Sequence`, `Calibration_Sequence` switches, four user cues) under **`CalibrationStageHandler`**, route cues through **`SequenceCommand`**, complete **`UIManager`** calibration screens (Start, sections, Conclusion, back, **`Dots`** line/dot pool), extract **environment bed** to **`LinearMusic`**, add **cue-gated navigation** and **dual-condition conclusion completion**, then implement **`StageVariant.Calibration_Album`** / **`Calibration_NoVibro`** on the **same** calibration stage (pack sequence sets **variant**, not a separate stub sequence asset). **Back** requires **Wwise rewind/re-post**.

**Process:** **`.cursor/rules/soundself-agent-workflow.mdc`** (commits only after chat confirmation; **never edit `.unity` without save prompt + explicit permission**; prefab save prompt; no hash-only commits). **This plan:** each stage **Checklist** uses **`[ ]` / `[x]`** (see **Stage checklist convention**).

You handle **Unity wiring and visuals**; approved code lands in commits **after you confirm** in chat.

**Current focus:** Finish **Stage A** Unity checks; next coding stage is **Stage B** (switch chat to **Composer 2** before starting B).
