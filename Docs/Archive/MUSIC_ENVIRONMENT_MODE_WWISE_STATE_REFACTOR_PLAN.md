# Music / Environment: Wwise State Refactor Plan

## Purpose

Refactor how Unity drives **environment audio** so it matches the Wwise model: global State group **`MusicEnvironmentMode`**, explicit **play/stop** events for the ambient bed, and **asymmetric crossfades** (~40 s into environment, ~10 s back to music). Integration lives in `MusicSystem1`; gameplay entry today is **a single path** (`Sequencer.FadeOut` ← `PlaygroundStageHandler`), so the implementation scope is small.

---

## Scope boundaries

- **Lights:** Do not change `Sequencer.FadeOut` light behavior (`LightControl.SetPreferredColor("Dark", 18f)`). This effort is **audio only**.
- **Sleep timer doc:** The old `ENVIRONMENT_TRANSITION_REFACTOR_PLAN.md` has been **deleted**; do not implement that MusicLoops-based sleep path. Environment audio is defined **only** by this State + play/stop contract.

---

## Wwise contract (authoritative)

| Concept | Unity usage |
|--------|-------------|
| State group | `MusicEnvironmentMode` — `AkSoundEngine.SetState("MusicEnvironmentMode", stateValue)` |
| State values | **`Environment`** and **`Music`** (confirmed) |
| Enter environment | Set State → **`Environment`**, then post **`Play_AMBIENT_ENVIRONMENT_LOOP`** |
| Exit environment | Set State → **`Music`**, then post **`Stop_AMBIENT_ENVIRONMENT_LOOP`** (correct spelling—designer typo ignored) after **~10 s** to match the exit crossfade tail |
| Crossfade ownership | **Wwise** owns audible blends; Unity owns **call order** and **delayed stop** |

**Why Play must fire:** The ambient loop event must post when entering the environment State so the voice exists for the graph to process (designer: “Play game call needs to be initiated”). Wwise events are **not** idempotent—do not stack `Play` without a matching lifecycle.

**Play/stop lifecycle (no stacking):**

- **Play** when transitioning **into** `MusicEnvironmentMode = Environment`.
- **Stop** once, **~10 s after** State returns to **`Music`** (not immediately on State change), so the tail matches Wwise.
- **Re-entry:** After Stop has effectively run (delayed stop completed), a later entry may **Play** again. If the user re-enters environment **before** the delayed Stop fires, cancel that pending Stop and reconcile so you never double-Play—only one ambient instance at a time.

Replace **`InteractiveMusicMode_Switch → Environment`** with **only** `MusicEnvironmentMode` State—**State is sufficient**; remove that switch write from the environment path (Option A). `RecoverInteractiveMusicModeFromInteractionType()` (and similar “restore interactive switch after Environment”) is **no longer needed** for leaving environment; exiting is **State → Music** plus delayed Stop, then normal interactive/music-loop routing continues per existing non-environment logic.

---

## Crossfade semantics

| Transition | Target | Unity |
|------------|--------|--------|
| → Environment | ~40 s (Wwise) | Set State `Environment`, post Play; no duplicate 40 s in Unity unless audio asks |
| → Music | ~10 s (Wwise) | Set State `Music`; schedule Stop ambient ~10 s later; cancel/restart if re-entering environment |

---

## Current behavior (baseline)

In `SetMusicModeTo(MusicMode.Environment)` today:

- Internal flags and **`SetBreathworkCycle(true)`**. **Whenever** environment is triggered (today only FadeOut; future call sites possible), decide deliberately whether breathwork should run—do not assume it always matches audio intent.
- **`EnvironmentInitializations()`** posts **`Play_AMBIENT_ENVIRONMENT_LOOP`** **once per process** (`initializeEnvironmentFlag`).
- **`InteractiveMusicMode_Switch`** set to **`Environment`** via `SetSwitchRestoreToningV3`—**to be removed** in favor of State only.
- **No** `Stop_AMBIENT_*` on exit.

---

## Gameplay call graph (why this stays simple)

| Step | Code |
|------|------|
| Entry | **`Sequencer.FadeOut()`** → `SetMusicModeTo(Environment)` + lights (unchanged) |
| Caller | **`PlaygroundStageHandler`** (Standard path) → **`FadeOut()`** near end of playground |

No other `SetMusicModeTo(MusicMode.Environment)` sites under `Assets/Scripts`. **Every exit** from `MusicMode.Environment` still flows through **`SetMusicModeTo`** when switching to another mode—implement **one** exit choke point there (State → Music + cancelable delayed Stop).

---

## Proposed implementation (`MusicSystem1`)

1. **`EnterMusicEnvironmentAudio()`** — `SetState(MusicEnvironmentMode, Environment)` → `PostEvent(Play_AMBIENT_ENVIRONMENT_LOOP)`. Replace `initializeEnvironmentFlag` “once ever” with proper enter/restart rules above.
2. **`ExitMusicEnvironmentAudio()` / scheduled stop** — `SetState(MusicEnvironmentMode, Music)` → coroutine or `Invoke` for **`Stop_AMBIENT_ENVIRONMENT_LOOP`** at **10 s**; **cancel** pending stop if re-entering environment before it fires.
3. **`SetMusicModeTo`:** On entering **`MusicMode.Environment`**, call **Enter** (+ existing breathwork/flags per product decision). On leaving Environment → any other mode, call **Exit** once (before or alongside other mode logic; avoid duplicate interactive-switch “recovery” for environment—State handles the audio side).
4. **Remove** `Button_PlayAmbientEnvironmentLoop` (and any inspector/debug hooks)—unused; avoids inconsistent State.
5. **Constants** — centralize strings: state group, `Environment`, `Music`, Play/Stop event names.

---

## `MusicMode.Environment` enum

Keep **`MusicMode.Environment`** as the gameplay flag (breathwork, mode checks) unless a later pass prefers a bool—**not** required for this audio refactor.

---

## Implementation phases

1. **Strings** — Confirm Stop event name in generated Wwise C# matches `Stop_AMBIENT_ENVIRONMENT_LOOP` (or project spelling).
2. **Enter/Exit helpers** — Implement with cancelable delayed stop; wire **`SetMusicModeTo`** enter/exit; drop **`InteractiveMusicMode_Switch` Environment** path; simplify exit (no environment-specific `RecoverInteractiveMusicMode…`).
3. **Verify** — Profiler: State transitions; single ambient instance; exit tail ~10 s; optional rapid re-entry cancel test.
4. **Docs** — `ENVIRONMENT_TRANSITION_REFACTOR_PLAN.md` has been **removed** (superseded by this plan). Any remaining doc links to it should be updated.

---

## Testing checklist

- [ ] `MusicEnvironmentMode`: `Environment` on FadeOut entry; `Music` when leaving environment mode.
- [ ] One ambient bed—no stacked Plays.
- [ ] Stop fires ~10 s after State → Music; cancel behavior if re-entering early.
- [ ] Leaving environment: music graph sounds correct; no reliance on old **`InteractiveMusicMode_Switch` Environment**.
- [ ] Regression: `SetSoundscape` / `SetMusicLoop` while not in environment unchanged.

---

## Summary

- Use **`MusicEnvironmentMode`** states **`Environment`** / **`Music`**, **`Play_AMBIENT_ENVIRONMENT_LOOP`** on enter, **`Stop_AMBIENT_ENVIRONMENT_LOOP`** ~10 s after exit State change, with cancel/re-enter handling.
- Drop **InteractiveMusicMode_Switch Environment**, redundant **recovery** on environment exit, **`Button_PlayAmbientEnvironmentLoop`**, and the **sleep timer** doc.
- **Single gameplay entry** today: **`FadeOut`**; exits centralized in **`SetMusicModeTo`**. Breathwork remains a **per-trigger** product decision.
