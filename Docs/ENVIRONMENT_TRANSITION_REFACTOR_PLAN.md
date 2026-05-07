# Environment Transition Refactor Plan

## Intent

Refactor inactivity-driven "go to environment" behavior inside `Assets/Scripts/MusicAndLight/MusicSystem1.cs` using a **Sleep Timer** flow based on MusicLoops, not legacy `MusicMode.Environment`.

This plan applies only to environment transitions caused by inactivity ("going to sleep"), not sequence-authored environment behavior.

---

## Core Direction

- Keep logic in `MusicSystem1` as the new Sleep Timer system (behavior is mostly already there; refactor and formalize it).
- Use Wwise switches:
  - `InteractiveMusicMode_Switch = MusicLoops`
  - `MusicLoops_Switch = Environment` (confirm exact Wwise value name in project)
- Treat this as a special inactivity soundscape state.
- Remove/deprecate `MusicMode.Environment` for this inactivity path.

---

## Required Sleep Timer Behavior

### Sleep Entry (inactivity threshold reached)

1. Mark player as asleep (Sleep Timer state).
2. Pause/guard both Director and WorldShuffler behavior while asleep:
   - Do not fully disable systems.
   - Systems should check sleep state before activating/changing worlds.
3. Switch to environment via MusicLoops:
   - `InteractiveMusicMode_Switch = MusicLoops`
   - `MusicLoops_Switch = Environment`
4. Start ambient VO event for sleep environment:
   - `Play_VO_LinearHumsAhhs`
5. Use longer transition timing for this entry.
6. Use Play_VO_LinearHumsAhhs (this should be a repeating behavior. Stop the repeating behavior when no longer in sleep)

### Sleep Wake (player tones again)

Wake priority order:

1. If Director is enabled and has a queued soundscape change:
   - activate Director
2. Else if WorldShuffler is enabled:
   - shuffle world immediately
3. Else if Sleep Timer is enabled:
   - restore previous pre-sleep soundscape

Additional wake actions:

- Stop `Play_VO_LinearHumsAhhs`.
- Clear asleep state and unblock Director/WorldShuffler checks.
- Use longer transition timing for this exit.

---

## Enable / Disable Contract

Sleep Timer enabled/disabled should mirror Director enabled/disabled call sites exactly (initial implementation target):

- wherever Director is enabled -> Sleep Timer enabled
- wherever Director is disabled -> Sleep Timer disabled

This keeps lifecycle consistent until/unless a later decoupling is needed.

---

## WorldShuffler Coordination Requirements

- WorldShuffler must know that current world is sleep-environment world while asleep.
- While asleep:
  - no normal shuffle actions should run
  - no director-driven world change should fire
- On wake:
  - first eligible path in wake priority order should own the first post-sleep transition.

---

## Functional Requirements

- Inactivity environment is fundamental-agnostic.
- Sleep path transitions are tunable and longer than normal soundscape changes.
- Sequence-driven environment behavior (e.g. Savasana) remains separate and deterministic.
- Sleep path should not regress normal interactive world transitions when not asleep.

---

## Technical Checklist

- [ ] Add/formalize Sleep Timer state in `MusicSystem1`:
  - [ ] `sleepTimerEnabled`
  - [ ] asleep flag/state
  - [ ] pre-sleep soundscape cache
- [ ] Replace inactivity use of `MusicMode.Environment` with MusicLoops switch path.
- [ ] Add Director/WorldShuffler "check sleep before activating" guards.
- [ ] Implement wake priority logic exactly as specified.
- [ ] Implement `Play_VO_LinearHumsAhhs` play on sleep entry.
- [ ] Implement stop behavior for `Play_VO_LinearHumsAhhs` on wake.
- [ ] Add separate transition duration controls for:
  - [ ] sleep entry
  - [ ] sleep wake
- [ ] Ensure Sleep Timer enable/disable mirrors Director call sites.
- [ ] Add logs for sleep enter/wake reason and selected wake branch.

---

## Open Questions (For You + Lorna)

1. **Exact `MusicLoops_Switch` value name**
   - Is it exactly `Environment` in Wwise, or a different canonical value?

2. **VO stop semantics**
   - Is there a dedicated stop event for `Play_VO_LinearHumsAhhs`?
   - If not, what is the safest stop strategy that does not affect unrelated VO?

3. **Sleep while WorldShuffler disabled**
   - Current rule says restore previous soundscape if no director action and shuffler disabled.
   - Confirm if this should always happen or only when previous soundscape is valid/playable.

4. **Sequence interaction**
   - If sequence intentionally drives environment while sleep state is active, which system has priority?
   - Current assumption: sequence-owned behavior should remain separate and authoritative.

5. **Suppressed music edge case**
   - Any state where music is suppressed but sleep-environment should still play?
   - Current assumption: no.

---

## Risks

- Hidden dependencies on legacy `MusicMode.Environment` behavior.
- Competing activations between Director, WorldShuffler, and sleep wake logic.
- Abrupt audio transitions if new long-duration settings are not consistently applied.

---

## Suggested Implementation Phases

### Phase 1 - Wire State + Guards

- Add sleep flags/timer + pre-sleep cache.
- Add guard checks in Director/WorldShuffler paths.
- Keep output behavior mostly unchanged except logging.

### Phase 2 - Flip Inactivity Path

- Route inactivity entry through MusicLoops `Environment` switch path.
- Play/stop `Play_VO_LinearHumsAhhs`.

### Phase 3 - Wake Priority + Timing

- Implement wake branch priority order.
- Add dedicated long transition timings for sleep in/out.

### Phase 4 - Cleanup

- Remove/deprecate inactivity use of `MusicMode.Environment`.
- Keep sequence-based environment behavior explicit and separate.
