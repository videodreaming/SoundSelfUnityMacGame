# Troubleshooting: Sound-world change is not audible

**Status:** Open investigation.
**Parent plan:** [`BLOCKS_4_5_7_PLAN.md`](BLOCKS_4_5_7_PLAN.md) — surfaced during the Stage 1 + Stage 2 guided playtest (`MusicDebugGuidedPlaytest`, **G** key).
**Related:** Block 4 (Wwise switch hygiene), Block 7 (sound-world transitions / `WorldShuffler`).

---

## Symptom

During the guided playtest, the Director queue fires correctly and Unity-side state reports the new sound world, but **the audible bed does not change** to the new (moodier/darker "Shadow") world. Wwise's Monitor confirms the bed does not switch (see Robin's capture).

This is **not** the Stage 1 Director bug (that is fixed and verified by these same logs — the action executes, the queue is not empty). The failure is **downstream**: between Unity posting the sound-world switch and Wwise actually re-routing/playing the new world.

---

## Desired behavior

When a sound-world change is committed (via `WorldShuffler` shuffle, a Playground director `Soundscape` item, or `MusicDebugHarness` `[`):

1. `MusicSystem1.SetSoundWorld(world)` posts `SoundWorldMode_Switch → <world>` (and ensures `InteractiveMusicMode_Switch → InteractiveMusicSystem`).
2. The **audible interactive bed transitions to the new world** within a musically reasonable time (next sync point / next tone), i.e. the player hears the new world's timbre.
3. Only **one** world is audible at a time (no "all worlds at once").
4. Toning (fundamental/harmony layers) continues correctly after the switch.

Acceptance: with the harness, pressing `[` to cycle worlds (or the guided shuffle step) produces an **audible** world change, and Wwise Monitor shows the corresponding `SoundWorldMode_Switch` value active and the matching content playing.

---

## Architecture / where it could go wrong

Unity → Wwise path for a sound-world change:

| # | Layer | Code | Notes |
|---|-------|------|-------|
| A | Trigger | `WorldShuffler.ShuffleSoundscape()` / Playground director `Soundscape` items / harness `[` | Picks the world string, calls `SetSoundscape` → `SetSoundWorld` |
| B | Routing decision | [`MusicSystem1.SetSoundscape`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) | Branches SoundWorld vs MusicLoop by dictionary membership |
| C | Switch posts | [`MusicSystem1.SetSoundWorld`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) | `SetSwitchRestoreToningV3("InteractiveMusicMode_Switch","InteractiveMusicSystem")` then `SetSwitchRestoreToningV3("SoundWorldMode_Switch", world)`; clears content lock; `SetSoundWorldFlag()` |
| D | Toning-layer restart | `SetSwitchRestoreToningV3` / `RunWithToningRestoredAfterInteractiveSwitch` → `RestartToningV3LayersAfterSwitchIfNeeded(wasF,wasH)` | Only restarts layers that were **playing at the instant of the switch** (`ToningV3FundamentalPlaying` / `…HarmonyPlaying`) |
| E | Wwise switch container | `SoundWorldMode_Switch` group → music/blend/switch container in the Wwise project | **Switch containers re-evaluate at their configured sync point / transition** — a mid-segment RTPC/switch set may not re-route already-playing content |
| F | What is actually audible | The InteractiveMusicSystem bed is heard **through the toning layers** (driven by `toneActiveConfident`) | If layers don't restart, or the new world's segment never (re)starts, the old world keeps sounding |

Likely fault zones, by suspicion:

- **E (Wwise-side):** the `SoundWorldMode_Switch` container only changes on a sync point that isn't being hit, or the switch transition is set to "continue to play" the old world; or the switch value name posted from Unity (e.g. `"Shadow"`) doesn't match the Wwise switch value exactly.
- **D (restart logic):** `RestartToningV3LayersAfterSwitchIfNeeded` doesn't actually retrigger the world content (it restarts fundamental/harmony pitch layers, not necessarily the world blend), so the new world is never (re)posted.
- **C (order/guards):** the `InteractiveMusicMode_Switch`/`SoundWorldMode_Switch` ordering, or `haveSetSoundWorldFlag`, leaves the container in a state where the new world isn't selected.
- **B (routing):** less likely — logs confirm it reaches `SetSoundWorld` and reports `SoundWorld`.

---

## Test surface (bisection targets)

The chain A→F can be split 50/50. Midpoint = **"did the correct `SoundWorldMode_Switch` value actually reach Wwise, and is the matching content even triggered?"**

- **Unity half (A–C):** Is `AkSoundEngine.SetSwitch("SoundWorldMode_Switch", "<world>")` called with the exact expected value, once, in the right order, with the flag set? (Instrument the actual `SetSwitch` call; compare value string to Wwise switch names.)
- **Wwise half (D–F):** Given the switch value is correct in Wwise Monitor, does the bed re-route? (Wwise Monitor: switch monitor + voice/Soundcaster — is Shadow content playing, or is the prior world still playing? Does it change on the next bar/cue?)

Tools:
- **Wwise Monitor / Profiler:** Switches view (what `SoundWorldMode_Switch` is set to and when), Voices Graph (what's actually playing), Events.
- **Unity logs:** add a one-line log at the literal `AkSoundEngine.SetSwitch` call sites (group, value, frame) — confirm value + cardinality.
- **Harness `[`** (CycleSoundWorld) for a clean, repeatable single switch with `P` state dumps.
- **EditMode:** only the Unity-side decision (B/C) is unit-testable (which switch ops, in what order, for a given world). E/F are perceptual + Monitor.

---

## Hypotheses (to confirm/kill by bisection)

- **H1 (Wwise sync point):** Switch is correct in Wwise, but the music switch container is set to transition only at a sync point (next bar/grid/exit cue) that rarely/never hits in this parked state, so the old world keeps playing. *Kill/confirm:* Wwise Monitor shows correct switch value but old content still in Voices.
- **H2 (layers not retriggered):** `RestartToningV3LayersAfterSwitchIfNeeded` restarts pitch (fundamental/harmony) layers but not the world blend, so the new world's content is never (re)started after the switch. *Kill/confirm:* compare what the restart actually posts vs what carries the world.
- **H3 (value mismatch):** The Unity world string (`"Shadow"`, `"SonoFlore"`, …) doesn't exactly match the Wwise `SoundWorldMode_Switch` value names. *Kill/confirm:* Monitor shows switch set to an unexpected/None value, or no switch event at all.
- **H4 (switch already equal / no-op):** In the guided test the auto-shuffle already landed on Shadow, so the explicit Shadow director item was a same-value no-op. The *real* question is whether **any** world→world change is audible. *Kill/confirm:* force a definite different world (e.g. SonoFlore → Shruti) via `[` and listen.
- **H5 (heard-through-toning gating):** World is only audible while toning; the switch lands but the world bed volume/-layer is gated such that the change isn't perceptible at current toning state. *Kill/confirm:* sustain a long tone after the switch and listen for the world emerging.

**First action should kill H4** (test design noise) so we trust the perceptual signal: from a known world, cycle to a clearly different world with `[` and verify in Monitor + by ear.

---

## Observations (append as we learn)

- **2026-06-04 (guided playtest, Robin):** Director queue fix confirmed working. Logs: `Director repro action executed`, `Activating entire queue with tone`, `Action MusicDebugHarness_Repro executed from process-all`, **no** "queue is empty". `SetSoundWorld` ran; `MUSIC: Soundscape Set To: Shadow (SoundWorld)`; state line `soundscape=Shadow | interaction=SoundWorld`. **Subjectively no audible change to a darker/moodier bed; Wwise Monitor confirms no switch to Shadow.**
- Note: in the guided script the auto-shuffle picked **Shadow** before the explicit Shadow director step, so that step was likely a same-value no-op (see H4) — the binding question is whether *any* world change is audible.
- **2026-06-04 (fix):** `MusicDebugGuidedPlaytest` now calls `EnsureBaselineBeforeSoundWorldTest` before the Shadow director step — if already on Shadow, it forces `SonoFlore` first so the Shadow queue is a real change.
- _(add Wwise Monitor findings: what value `SoundWorldMode_Switch` shows, and what's in the Voices graph, at each step)_
- **2026-06-04 (code read, Opus 4.8 — NOT yet Monitor-confirmed):** Static analysis of [`MusicSystem1.SetSoundWorld`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) (lines ~1142–1171) found the `SoundWorldMode_Switch` post is gated behind `if(!ToningV3WasAlreadyRestored)`, but `ToningV3WasAlreadyRestored` is set `true` in exactly the audible branch (`currentMusicMode != Environment`). So in the normal Playground path the method posts **only** `InteractiveMusicMode_Switch → InteractiveMusicSystem` and **never** `SoundWorldMode_Switch → <world>`. The only branch that posts the world value is the `Environment` branch, which the code itself logs as inaudible. This exactly predicts the symptom (Unity reports new world via `worldShuffler.SetCurrentSoundscape` / `OnInteractionTypeChanged`, but Wwise never switches). The `ToningV3WasAlreadyRestored` flag appears intended to dedupe the **toning-layer restart**, not to gate the world switch. **Prime suspect: layer C (guard), supersedes H3.** Confirm via Step 1 Monitor reading below before any code change.

---

## Test & fix plan (iterative 50/50 bisection)

Goal: localize the break to a single layer (A–F) by halving the surface each step, using Wwise Monitor as ground truth for "did Wwise get the right switch and play the right content".

- [ ] **Step 0 — Remove test noise (H4).** From a known world, press `[` to a clearly different world (e.g. SonoFlore → Shruti). Record: was it audible? Monitor switch value? This makes the signal trustworthy before bisecting.
- [ ] **Step 1 — Split Unity vs Wwise (midpoint C/E).** With Wwise Monitor open, do one `[` switch. 
  - If Monitor shows the **correct** `SoundWorldMode_Switch` value → the Unity half (A–C) is good; **descend into the Wwise half (D–F)**.
  - If Monitor shows **wrong/None/no** switch → **descend into the Unity half (A–C)**.
- [ ] **Step 2a — Wwise half (if Step 1 = switch correct).** In Monitor Voices/Soundcaster, is the new world's content playing or is the old world still sounding? Check the switch container's transition/sync settings (immediate vs next bar/cue vs exit). → isolates **H1** vs **H2/H5**.
- [ ] **Step 2b — Unity half (if Step 1 = switch wrong).** Log the literal `AkSoundEngine.SetSwitch("SoundWorldMode_Switch", value)` call (value + frame + count). Compare value string to Wwise switch names; check call order vs `InteractiveMusicMode_Switch` and `haveSetSoundWorldFlag`. → isolates **H3** vs **C ordering**.
- [ ] **Step 3 — Halve again** within whichever half Step 2 implicated, until a single cause remains.
- [ ] **Step 4 — Fix** at the identified layer:
  - Unity-side → extract a small **switch-order/value policy** (extends Block 4 `InteractiveMusicSwitchPolicy`) + EditMode test asserting the exact ops/values for a world change; wire production through it.
  - Wwise-side → adjust the switch container transition/sync (Lorna) and/or add a Unity retrigger of the world content after the switch; document the required Wwise setup.
- [ ] **Step 5 — Re-verify** with `[` and the guided shuffle step: audible change + Monitor confirmation; add a regression note here and in the parent plan.

**Bisection log:**
- **Step 1 (predicted from code read, awaiting Monitor):** Expect Wwise Monitor on one harness `[` switch to show `InteractiveMusicMode_Switch → InteractiveMusicSystem` change **but no `SoundWorldMode_Switch` change** (group stays at its prior/default `Gentle`). If confirmed → descend Unity half, cause = the `!ToningV3WasAlreadyRestored` guard in `SetSoundWorld` (layer C). If Monitor *does* show the correct `SoundWorldMode_Switch` value → my read is wrong, descend Wwise half (D–F).

---

## Handoff prompt (start a new Opus 4.8 chat)

> **Model: Opus 4.8.** Continue the investigation in `Docs/SOUNDWORLD_SWITCH_NOT_AUDIBLE.md` (read it first; it references `Docs/BLOCKS_4_5_7_PLAN.md`). Context: during the Stage 1+2 guided playtest (`MusicDebugGuidedPlaytest`, **G** key, parked in `StageVariant.Playground_Debug` on `DebugSequence`), the Director queue fix works (action fires, queue not empty, Unity reports `soundscape=Shadow | interaction=SoundWorld`), **but the sound-world change is not audible and Wwise Monitor confirms no switch to the new world.**
>
> Do **not** make broad changes. Work the **iterative 50/50 bisection** in that doc's "Test & fix plan", using **Wwise Monitor as ground truth**. Start at **Step 0** (kill H4: the guided script's auto-shuffle may have pre-selected Shadow, making the explicit Shadow step a no-op — so first confirm whether *any* world→world change via harness `[` is audible). Then **Step 1**: with Wwise Monitor open on one `[` switch, decide whether the correct `SoundWorldMode_Switch` value reaches Wwise, and split into the Unity half (A–C: `SetSoundscape`/`SetSoundWorld`/`SetSwitchRestoreToningV3` in `MusicSystem1.cs`) or the Wwise half (D–F: switch-container transition/sync + whether the world content retriggers).
>
> Rules from `BLOCKS_4_5_7_PLAN.md` apply: no code before I confirm; **do NOT touch `Assets/Scenes/MainGame.unity`** (another dev owns it); EditMode tests before production changes where a rule exists; prefer a small testable policy (extend `InteractiveMusicSwitchPolicy`) for any Unity-side switch-order/value fix; commit only on my explicit go. Tell me exactly which Wwise Monitor view to read and what to look for at each step; I'll paste Monitor findings + logs. Append every result to the doc's **Observations** and **Bisection log**.
