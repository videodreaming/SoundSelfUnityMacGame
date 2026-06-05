# Blocks 4 / 5 / 7 (+ 8) — Music & Voice Hardening Plan

**Scope:** [Block 4 (Wwise switch hygiene)](PLAYTEST_NOTES_ORGANIZED.md#block-4--wwise-switch-hygiene-safety-fixes), [Block 5 (binaural + stage gating)](PLAYTEST_NOTES_ORGANIZED.md#block-5--binaural--stage-gating-medium-scoped), [Block 7 (music system: fundamental, pitch, silent loops)](PLAYTEST_NOTES_ORGANIZED.md#block-7--music-system-fundamental-pitch-silent-loops-large), and [Block 8 (microphone volume envelope)](PLAYTEST_NOTES_ORGANIZED.md#block-8--microphone-volume-envelope-large-unity-side) — worked as **one** staged plan below. *(Playtest “Block 8” = mic envelope; plan “Stage 8” later = Block 7 lock-C / 15:00 — different numbering.)*

**Companion doc:** [`PLAYTEST_NOTES_ORGANIZED.md`](PLAYTEST_NOTES_ORGANIZED.md). **Workflow:** [`soundself-director-mode.mdc`](../.cursor/rules/soundself-director-mode.mdc).

---

## Status at a glance

> **(a) what's done · (b) what's left** — at a glance. Legend: ✅ done · ▶ in progress · ◑ partial · ⬜ to do. Full detail for every stage lives in the **Appendices** (linked from §Active work and below).

| Stage | State | Commit | What |
|---|---|---|---|
| 0 — Debug / test harness | ✅ | `c40e044b` | `Playground_Debug` harness + keyboard music controls |
| 1 — Director queue + shuffle + sound-worlds | ✅ | `ab63eb4f`, `816218e3` | Director self-removal fix; shuffle/transition audit; SoundWorld-switch-not-audible fix |
| 2 — Binaural stage gating | ✅ | `da074617` | binaural off on MusicPlaylist / LinearAudio |
| 2b — Binaural single authority | ✅ | `dd1b7c0a` | stage owns base volume; mode only attenuates 30% |
| 3 — Wwise switch hygiene | ◑ | `dd1b7c0a`, `816218e3` | MusicLoops→Silence before interactive (done); one-frame switch ordering still open |
| 3b — Block 8 mic envelope | ✅ | `3dc74f22` | per-soundscape MicMixer dB + stacked ADSR monitoring |
| 4a — dead-field cleanup | ✅ | `18651457` | remove `fundamentalNoteCompare` + dead harmony-retrigger fields |
| 4b — NoteTracker split | ✅ | `d18b002e` | → `voiceActivity` + `fundamentalChargeByNote` |
| 4c — partial-class split | ✅ | `0fced712` | `MusicSystem1.InputDrivenFundamental/Harmony.cs` |
| 4d — active-source authority | ✅ | `e80da993` | `FundamentalSource` + `FundamentalSourcePolicy`; lock setters as shims |
| 4e — migrate call sites to `SetFundamentalSource` (+ retire legacy lock stack & debug override) | ▶ | _(uncommitted)_ | gate flip + zones 1–6 + `ResolveFundamentalOnUnlock` removed; **Step 0 done in tree — whole lock stack + debug override + dev force-note surfaces deleted, policies lost `hasDebugOverride`**; shadow-tracker preferred-sync **deferred to 9c** (not standalone); pending Test Runner → Opus regression → one commit |
| 4f — `HarmonyRunPolicy` | ⬜ | — | harmony in Tutorial/Freeplay + Savasana tail (`MusicLoopSilent && gameOn`) |
| 4g — MusicBed `Cue_Key_*` listener | ⬜ | — | (folds old Stage 5 Unity side) binaural follows the bed key |
| 4h — retire `FrozenFreeplay` | ⬜ (optional) | — | collapse to Freeplay + `gameOn=false` + Sequence(C); gated on a gameOn audit |
| 5 — Lorna external Wwise embedding | ⬜ (external) | — | cue→`NoteName` contract; end-to-end verify |
| 6 — pitch / 5ths / harmony audit | ⬜ | — | consonance + `changeHarmony` guards |
| 7 — interactive fade / silent loops / Stop_Toning | ⬜ | — | fade feel; silent-loop persistence; Wwise-paced stop |
| 8 — lock C before savasana + 15:00 | ⬜ | — | pin C ~60s pre-savasana; smooth 15:00 crossfade |
| 9 — Director ↔ fundamental "goblin" | ⭐ NEXT (design settled) | — | synchresis/timing unification + `targetNextFundamental` slot + **shadow-tracker preferred-sync (deferred from 4e)**; 9a then 9c |

---

## Active work — what's left to do

> **Execution order is driven by [`BLOCKS_4_5_7_BUILD_CHECKLIST.md`](BLOCKS_4_5_7_BUILD_CHECKLIST.md)** (adopted 2026-06-05): finish 4e → the **Stage 9 goblin spine (next)** → 4g / 4f → cleanups → musical polish → Lorna external. That checklist has the step-by-step boxes + the "how it works / why" primer; this section is the prose summary.

**▶ Now — finish Stage 4e** (active-source call-site migration **+ Step 0: retire the legacy lock stack & debug override**). The bundle below is **uncommitted** in `WorkingWwise`, on top of 4d `e80da993`:

- **Done in the working tree:** gate flip (both InputDriven gates → `FundamentalSourcePolicy.CanInputDrivenWriteMaster`); zone migrations (startup = Sequence; `SetSoundWorld`→InputDriven / `SetMusicLoop`→MusicBed; `SetMusicModeTo` modes; Tutorial A/C-hum correction; Savasana); `ResolveFundamentalOnUnlock` + `IsFundamentalLocked` deleted; `SourceForInteractionType` EditMode test added. *(detail: [Appendix D](#appendix-d--stage-4-block-7-fundamental-active-source-design-spec--4e-status--decision-log) §"4e IN-PROGRESS STATUS")*
- **Step 0 done this session (Robin 2026-06-05 — "delete and forever forget the debug override"):** the **entire legacy lock stack** is gone — `debugFundamentalOverride` + `SetDebugFundamentalOverride`, the three lock fields + `SetFundamentalDebugLock`/`ContentLock`/`ModeLock`, `GetLockedFundamental`, and every dev force-note surface (`OnPermanentlySetFundamentalChanged`; the commented `InputReferences` I/O/K/L/N/M keys; the `MusicDebugHarness` `LockFundamentalToC`/`UnlockFundamentalLocks` actions + L/U keys). `FundamentalSourcePolicy.ShouldWriteMaster`/`CanInputDrivenWriteMaster` lost the `hasDebugOverride` param; both gate call sites + Block7/harness EditMode tests updated. No production behavior change (all debug-only after the migration). **Bundled into the single 4e commit.**
- ⬜ **Shadow-tracker `preferred`-sync fix — DEFERRED to 9c (not standalone).** `preferred[InputDriven]` is never updated by live tracking, so adopt-preferred entries can snap the master to a stale note / wipe charge. A standalone attempt at this fix failed (confusing half-machinery), so it now lands as **Layer 1 of the 9c commit semantics** (silent/audible commit + honor-not-wipe built together). 4e ships with the bounded interim (InputDriven entry continues from current master). Design spec: [`HANDOFF_InputDriven_preferred_shadow_tracker.md`](HANDOFF_InputDriven_preferred_shadow_tracker.md) (consumed by 9c).
- ⬜ **One bundled 4e commit (migration + Step 0):** Test Runner (EditMode) green → Opus regression pass → Robin's go → commit → record hash in [Appendix A](#appendix-a--commit-log).

**⬜ Then — recommended order (adopted 2026-06-05; boxes in the [build checklist](BLOCKS_4_5_7_BUILD_CHECKLIST.md)):** the Stage 9 goblin spine comes **next** — it's the architecture everything else sits on — then the sources, cleanups, polish, and external. One-line goal each (full spec in the linked appendix):

- **⭐ 9 — Director ↔ fundamental "goblin" (NEXT — the spine):** `targetNextFundamental` slot + `ApplyMasterFundamentalRaw`/announce + disabled-bypass + **structural** realized-effect + flush-on-switch + **shadow-tracker preferred-sync + warm handoff** (the deferred 4e fix, Layer 1 of commit semantics). **9a** (long-test enqueue-then-activate + 5s flourish suppression) then **9c** (the slot + commit semantics). *(Appendix G)*
- **4g — MusicBed `Cue_Key_*` listener** in `MusicSystem1` (first consumer of the slot/announce path): post `Play_MusicLoops` with the cue callback flag → `TryHandleMusicKeyCue` → MusicBed source; binaural follows the master; cue→`NoteName` map. *(Appendix D + Appendix E Stage 5)*
- **4f — `HarmonyRunPolicy.ShouldRun(mode, gameOn)`** gating `HarmonyUpdate`: run in Tutorial, Freeplay, and `MusicLoopSilent && gameOn` (Savasana toning tail; Linear off via `gameOn=false`). Independent — can slot in early as a quick win. *(Appendix D)*
- **4h (optional) — retire `FrozenFreeplay`**: collapse the 4 call sites to `Freeplay` + `SetGameOn(false)` + `Sequence(C)`; gated on a `gameOn` audit. *(Appendix D)*
- **Stage 3 leftover — one-frame switch ordering** + duplicate-post guard. *(Appendix C Stage 3)*
- **6 — pitch / 5ths / harmony audit**: consonance + `changeHarmony` `None`-guard; revisit the harmony `gameOn` gate. *(Appendix E)*
- **7 — interactive fade / silent loops / Stop_Toning**: fade-in feel; silent loops persist when toning stops; Wwise-paced stop. *(Appendix E)*
- **8 — lock C before savasana + 15:00 transition** (after the slot, so it queues through the finished contract): pin C ~60s before savasana; smooth 15:00 crossfade. *(Appendix E)*
- **5 — Lorna external**: she embeds `Cue_Key_*` in Wwise; verify end-to-end (Unity side is 4g). *(Appendix E)*

---

## Standing rules (apply to every stage)

1. **No code is written until Robin confirms** the plan, and confirms each stage as we reach it.
2. **Each stage header names the implementation AI** — **Composer 2.5 fast** or **Opus 4.8**.
3. **Every stage ends with a required Opus 4.8 regression pass** — a read-only review of the diff for regressions across music / voice / sequencing before commit. This happens even when implementation was done by Composer.
4. **Each stage ends with a commit** — agent proposes message + files; Robin approves per the git rule.
5. **Tests favor minimal listening + minimal moving around.** Drive everything from the keyboard in a `Playground_Debug` harness; paste one-line state logs into chat.
6. **Block order inside each stage:** Test Runner tests (EditMode) first, then Playtests.
7. **Do NOT touch `Assets/Scenes/MainGame.unity`.** Another developer is actively working on it in git; editing or committing it risks merge conflicts. Do not edit, stage, or commit this scene as part of any stage. If a change *seems* to require it, stop and flag it to Robin instead.
8. **Add Test Runner (EditMode) tests as we move through the system — for regression coverage, not just the current task.** Whenever we touch a behavior with a stable rule (thresholds, switch order/values, stage→flag maps, queue mechanics, cue→note maps), add or extend an EditMode test that pins it, so future changes that break it fail a test rather than a playtest. Prefer a small pure **policy** class wired through production so the rule is unit-testable. Goal: a growing regression net that runs without Robin's ears.
9. **Record commit hashes in this plan (and the relevant doc) for each committed change.** Add the short hash next to the stage/fix it implements in the **Commit log** below. Per the repo rule, **never make a commit whose only purpose is writing a hash into a markdown file** — embed the hash in the same commit as the work it describes, or add it in the *next* commit that carries real work (or when Robin pastes it).
10. **Single Console filter tag: `B457`.** During any playtest in this series, Robin filters the Unity Console by the one string **`B457`** to see only what matters and hide noise. Every log we want Robin to read during a playtest **must contain `B457`**; incidental/distracting logs **must not**. When instrumenting a block's playtest, prefix the needed logs with `B457` (the `MusicDebugHarness` state line / `GUIDED` prompts already do; tagged production signals so far: Director queue activation/empty in [`Director.cs`](../Assets/Scripts/Sequencing/Director.cs), binaural target volume in [`MusicBinauralBeats.cs`](../Assets/Scripts/MusicAndLight/MusicBinauralBeats.cs)) and leave distractions untagged. A short-lived per-investigation tag may be layered on top (e.g. `SWAUDIT`) and removed at that investigation's commit; `B457` is the durable series filter.

**Ordering rationale:** Director / shuffle queue mechanics are foundational and come first (after the harness). Then easy knock-outs (binaural gating), then switch hygiene, then **Block 8 mic monitoring** (including the guided-playtest MusicLoop headroom fix) **before** the fundamental-system split, then the musically sensitive Block 7 items in order of how fundamental they are to the music sounding good.

---

# Appendices

> Reference + history. The **live plan** is §Status / §Active work / §Standing rules above; everything below is preserved detail, faithfully moved (nothing deleted).

# Appendix A — Commit log

Short hashes for each committed stage/fix (standing rule 9). Newest at the bottom. `origin/WorkingWwise`.

| Commit | Stage / change |
|--------|----------------|
| `c40e044b` | Stage 0 — Add `Playground_Debug` harness + keyboard music controls (editor/dev only) |
| `ab63eb4f` | Stage 1 — Director queue self-removal fix; shuffle + sound-world transition audit |
| `da074617` | Stage 2 — Block 5: gate binaural off on MusicPlaylist/LinearAudio stages |
| `dd1b7c0a` | Stage 2b + Stage 3 first fix — single-authority binaural + Block 4 first switch fix (marked UNTESTED) |
| `daca6470` | Investigation start — `SOUNDWORLD_SWITCH_NOT_AUDIBLE.md` + guided playtest / binaural WIP checkpoint |
| `816218e3` | **SOUNDWORLD_SWITCH_NOT_AUDIBLE resolved** — `SetSoundWorld` now posts `SoundWorldMode_Switch` (was gated out by the `!ToningV3WasAlreadyRestored` guard); via `InteractiveMusicSwitchPolicy.SetSoundWorldPosts` + EditMode test; plan standing rules 8/9 + commit log added |
| `3dc74f22` | **Stage 3b — Block 8 mic envelope** — per-soundscape MicMixer dB (worlds 0 / loops +3); stacked monitoring ADSR; `SoundscapeMonitoringPolicy` + `MonitoringAdsrPolicy` + EditMode tests; guided playtest; inspector cleanup |
| `18651457` | **Stage 4a — dead-field cleanup** — remove write-only `fundamentalNoteCompare`, `harmonyRetriggerThreshold`, `harmonyTimeSinceLastTrigger`; Stage 4 active-source design folded into plan (incl. former Stage 5 → 4g) |
| `0fced712` | **Stage 4c — cosmetic partial-class split** of `MusicSystem1.cs`: `FundamentalUpdate`/`TryApplyFundamentalChangeTriggers`/shift helpers → `MusicSystem1.InputDrivenFundamental.cs`; `HarmonyUpdate`/`changeHarmony` → `MusicSystem1.InputDrivenHarmony.cs`; same class via `partial`, zero behavior change; EditMode parity green |
| `d18b002e` | **Stage 4b — split + rename `NoteTracker`** → `voiceActivity` (`VoiceActivity { ActiveSeconds; IsActive; JustActivated }`, activation half) + `fundamentalChargeByNote` (`Dictionary<NoteName,float>`, charge half); pure data-structure split, zero logic change; EditMode parity green |
| `e80da993` | **Stage 4d — active-source fundamental authority** — `FundamentalSource` enum + per-source preferred + debug override (`MusicSystem1.FundamentalAuthority.cs`); `SetFundamentalDirect` body → private `ApplyMasterFundamental` (public shim kept); legacy lock setters route inner master-write through the source API (DebugLock→override, Content/ModeLock→`Sequence`), production gate still `IsFundamentalLocked()`; `FundamentalSourcePolicy` + `Block7FundamentalPolicyEditModeTests` (9, green). Behavior-preserving (Opus 4d regression pass: shim-equivalent) |

---

# Appendix B — Director-mode framing & test harness

## How director mode helps these blocks (and where it doesn't)

Director mode's core move: **extract the rule-based part into a policy + EditMode test so it never needs Robin's ears, and isolate the irreducibly perceptual part into the smallest possible listening checklist.** Most of Blocks 4/5/7 is rule-based (cue→note maps, lock precedence, stage→binaural gating, switch ordering, queue mechanics, timing constants), so ~70–80% becomes "read a log line and move on." The harness *is* director mode applied: park in `Playground_Debug`, press a key, paste one state line.

**What it does NOT do:** it cannot tell us the music sounds good. Green tests ≠ harmonious. The dissonance / clash / fade-feel complaints are perceptual — no EditMode test substitutes for toning in headphones. Beware false confidence: a correct cue map doesn't mean the chosen pitches are musical, only that the plumbing is right.

Each stage below states a **Listening load** so the ear-requirement is explicit up front:

- **Logs only** — sign off from the state line / console; no listening.
- **Logs + optional ear** — objective via logs, with a quick perceptual sanity check.
- **Real ear check** — irreducibly subjective; headphones required.

---

## The test harness (built first, used by every later stage)

Editor/dev-only; **no production behavior change**, so it can land first and de-risk all later subjective testing.

- New **`StageVariant.Playground_Debug`** (next free int id, e.g. `36` — append, do not renumber existing ids).
- [`PlaygroundStageHandler`](../Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs) handles `Playground_Debug` like Freeplay (director + `gameOn` on) but **does not start the timeline coroutine** — it parks in a controllable state.
- A dev component (extend [`InputReferences`](../Assets/Scripts/Utilities/InputReferences.cs) or new `MusicDebugHarness`) with keys to:
  - **Simulate `Cue_Key_*`** by calling the shared cue handler directly (test the whole Unity key path without waiting on Lorna's Wwise build). Includes stepping through Lorna's example timeline.
  - Cycle **sound worlds** (SonoFlore / Shadow / Gentle / Shruti) and **music loops** (ShiftingEarth / SitarAmbience / PinkNoiseAtmosphere / Silence).
  - **Lock / unlock fundamental**, force **lock C** (savasana sim), jump to **15:00** / **savasana** milestone states.
  - **Director repro:** queue a single `ActivateEntireQueueOnNextTone` item, force-expire its timer, then start a tone — to reproduce the self-removal bug deterministically.
  - Toggle **binaural** play / volume.
  - Print **one consolidated state line** on demand: `mode | fundamental | harmony | soundworld | binaural center Hz | gameOn`.
- [`DebugSequence.asset`](../Assets/Definitions/Sequences/DebugSequence.asset)'s Playground stage repointed to `Playground_Debug` (`.asset` edit — prompt Robin to save Unity first). `MainGame.unity` already wires `definitionOverride → DebugSequence`.

> Only non-`.cs` touches: the `StageVariant` enum value and `DebugSequence.asset`. No `.unity` / `.prefab` edits planned.

---

# Appendix C — Completed stages 0–3b: detail & playtest results

## Stage 0 — Debug / test harness

- **Implement with: Composer 2.5 fast** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Logs only — confirm the state line and that nothing auto-advances.

**Goal / acceptance:** From `DebugSequence` + `Playground_Debug`, drive cues / worlds / loops / locks / binaural / director-repro from the keyboard and get a one-line state dump. Zero change to production sequence behavior.

**Test Runner tests (EditMode):** harness key→action map is a thin switch; assert `Playground_Debug` routes to the parked handler (no timeline coroutine).

**Playtests:** enter debug playground; press state-dump key; confirm log line; confirm no auto-advance.

**Must not break:** existing `StageVariant` int ids; normal `DebugSequence` flow when `Playground_Debug` unused.

**Commit:** `Add Playground_Debug harness + keyboard music controls (editor/dev only).`

### Stage 0 — Playtests (when implemented)

| Inspector | Value |
|-----------|--------|
| `CSVLoader` → Definition Override | `DebugSequence` (MainGame default) |
| Enter Play Mode | Advance/skip to **Playground** (variant `Playground_Debug`) |

| Step | Pass criteria |
|------|----------------|
| Parked playground | Console: `Playground_Debug — parked`; stage does not auto-complete |
| **P** | One-line `[MusicDebugHarness] STATE mode=… \| fundamental=… \| …` |
| **;** | Steps Lorna `Cue_Key_*` timeline; fundamental updates in state line |
| **R** | Director repro queued; after tone, either repro action log **or** (pre–Stage 1) empty-queue bug log |

**Harness keys:** P=state · **E=end stage** · **G=guided audio audit (worlds/loops + Stage 1+2)** · [=world · ]=loop · ;=key cue · … (Shift+E in InputReferences; avoid Shift+Q — Unity steals Q in Scene view)

### Guided subjective playtest (Parts A/B/C) — **G** key

Editor-only coroutine [`MusicDebugGuidedPlaytest`](../Assets/Scripts/Debug/MusicDebugGuidedPlaytest.cs). **G** starts; **G** again aborts. **Single Console filter: `B457`** (standing rule 10).

**Session entry:** Play Mode → `DebugSequence` → land on **Playground_Debug** (first stage if DebugSequence starts there). **Headphones required.**

| Step | What happens |
|------|----------------|
| **G** | Coroutine starts; `B457` `>>> … <<<` prompts — **you advance each step** (no auto timers) |
| **Sections** | **ENTERING PART A/B/C** banners state what we are testing + pass criteria; **MOVING ON TO PART …** between sections (Space/Return to acknowledge) |
| **Advance** | **Space** or **Return** between listening steps (toning while listening does **not** advance). Director on-tone steps: **sustained tone**, then release, then Space. Named keys (**E**, **]**, **[**) where prompted |
| Part A | Four sound worlds — distinct + clean switches (`SetSoundWorld` / Block 4) |
| Part B | Loop bed then world — loop must go **silent** under world (MusicLoops→Silence hygiene) |
| Part C | Director on tone (no empty queue) + binaural in on Playground / out on Linear |
| Attenuation (optional) | Harness sets **MusicLoopSilent** then **Freeplay** (not `]`/`[` cycle) — expect `binauralAtt=on`, `binauralOut≈70` while silent |
| **E** (when prompted) | Leave Playground → `Linear_Nature`; advance after fade-out judged |
| End | Paste Console (single filter `B457`) + subjective notes |

**Pass (Stage 1):** repro action executes; no *"queue is empty"* on whole-queue activation; soundscape/shuffle/transition fire on tone. Part C clears playground **SoundscapeShuffle** / **ColorWorldShuffle** first; repro step: **release tone** → timer auto-expires → Space → **then** tone.

**Pass (Stage 2):** Playground `binauralOut≈70–100`; after **E** binaural fades out; optional MusicLoopSilent → `binauralAtt=on`.

### Guided playtest results (2026-06-04, Robin)

| Part | Result | Notes |
|------|--------|--------|
| **A** | **PASS** | All four worlds distinct + clean (Space-paced) |
| **B** | **PASS** | Loop bed audible; no bleed under Shadow; Gentle mid-wait = playground auto-shuffle (OK) |
| **C Director** | **PASS** | Repro + Shadow + transition + shuffle; `Activating entire queue with tone`; no empty-queue |
| **C Binaural in** | **PASS (weak)** | Already at 100 on enter — target met, fade-in not observable |
| **C Binaural out** | **PASS** | Fade-out heard after **E** (double-**E** skipped ahead in sequence) |
| **C Attenuation** | **Not validated** (harness) | Prior prompt used `]` = cycle loop, not MusicLoopSilent — **fixed in harness**; re-run optional step only |

**Deferred → Stage 3b.0:** MusicLoops monitoring too quiet — fix via per-soundscape **MicMixer `SoundscapeMonitoring`** (loops seeded +8 dB, 10 s lerp; silence → 0); see [3b.0](#3b0--per-soundscape-gain-on-micmixer-bus-agreed-2026-06-04--do-first-implemented-awaiting-playtest).

**Harness follow-ups in working tree (uncommitted):** Space-only advance + section banners; repro tone-release + shuffle queue clear; MusicLoopSilent via `ApplyMusicLoopSilentMode` / `ApplyFreeplayMode`.

**Stages 0–3 product sign-off (guided + EditMode):** OK to proceed plan-wise; optional: one short re-run of Part C attenuation + repro “stop toning” prompts after harness pull.

---

## Stage 1 — Director queue + shuffle + sound-world transitions

- **Implement with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Logs only — queue mechanics are pure logic; optional quick ear check that a shuffle/transition feels intentional.

**Goal / acceptance:**
- **Director self-removal bug fixed.** When an item expires with `ActivateEntireQueueOnNextTone`, it must remain a member of the queue it activates (so it fires) rather than being removed before activation. Subsumes the existing Block 7 *"Shuffle expired + empty queue"* item.
- Only one dominant sound world at a time; Shadow / Shruti transitions feel intentional; shuffle exclusion windows (≤300s / ≤180s) correct.

**Root cause (already located):** [`Director.QueueUpdate`](../Assets/Scripts/Sequencing/Director.cs) unconditionally adds every expired key to `keysToRemove`, including the `ActivateEntireQueueOnNextTone` case. The coroutine `ActivateQueueOnTone` doesn't capture the triggering action — it activates whatever's left in the queue on the next tone — so the trigger has already removed itself (and if it was the only item, the queue is empty → *"Queue activation requested but queue is empty (may have been cleared)"*). `ActivateThisActionOnNextTone` is safe (action captured); `ExpireWithoutExecuting` genuinely just expires.

**Fix shape (design carefully in-stage):**
- Don't add `ActivateEntireQueueOnNextTone` items to `keysToRemove`; mark them "pending activation" (tuple flag or side set) so `QueueUpdate` doesn't re-trigger a new coroutine every frame while `timeLeft ≤ 0`; let `ActivateQueue()`'s final `queue.Clear()` remove them when they fire.
- Handle multiple same-frame `ActivateEntireQueueOnNextTone` expiries (none lost).
- Decide behavior if the tone never arrives before the stage ends (currently they'd linger).

**Files:** [`Director.cs`](../Assets/Scripts/Sequencing/Director.cs), [`WorldShuffler.cs`](../Assets/Scripts/MusicAndLight/WorldShuffler.cs), [`PlaygroundStageHandler.cs`](../Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs).

**Test Runner tests (EditMode):** `Block7DirectorQueueEditModeTests` — expire an `ActivateEntireQueueOnNextTone` item → its action still executes on activation; multiple same-frame expiries all fire; `ActivateThisActionOnNextTone` and `ExpireWithoutExecuting` unchanged; shuffle-expired-with-nonempty-queue still activates; exclusion windows.

**Playtests:** harness director-repro key → confirm log shows the item firing (not "queue is empty"); cycle worlds, watch shuffle logs.

**Must not break:** `ActivateThisActionOnNextTone` capture behavior; `fundamentalChange` short-path (`ExpireWithoutExecuting`); `activateQueueOnToneRunning` guard against duplicate coroutines.

**Note (cross-stage):** sound-world transitions touch fundamental content locks (`SetSoundWorld` clears content lock; `SetMusicLoop` sets it). Keep the transition audit light here; deep content-lock interaction is revisited in Stage 4.

**Open investigation (spun out) — RESOLVED 2026-06-04:** During the Stage 1+2 guided playtest the **sound-world change was not audible**. Root cause was Unity-side (not Wwise): [`MusicSystem1.SetSoundWorld`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) gated the `SoundWorldMode_Switch` post behind `!ToningV3WasAlreadyRestored`, which was true in the audible path, so the world switch never reached Wwise. Fixed by extracting [`InteractiveMusicSwitchPolicy.SetSoundWorldPosts`](../Assets/Scripts/MusicAndLight/InteractiveMusicSwitchPolicy.cs) (world→Silence→InteractiveMusicSystem, one toning restore) + EditMode test; verified audible + Wwise-confirmed. Details: [`SOUNDWORLD_SWITCH_NOT_AUDIBLE.md`](SOUNDWORLD_SWITCH_NOT_AUDIBLE.md).

**Commit:** `Block 7: fix Director queue self-removal on whole-queue activation; shuffle + sound-world transition audit.`

---

## Stage 2 — Block 5: binaural stage gating (easy knock-out)

- **Implement with: Composer 2.5 fast** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Logs only — state line shows binaural muted/audible per stage.

**Goal / acceptance:** Binaural silent on `MusicPlaylist` + `LinearAudio` stage types; present in `Tutorial` + `Playground`.

**Approach (Robin: stage `Enter()`, not `SetMusicModeFlags`):**

- Small **`BinauralStagePolicy`** — pure rule: which `StageType`s should have audible binaural (e.g. target volume **70** vs **0**). Handlers and tests call the policy; **do not** add stage-type branching inside [`MusicSystem1.SetMusicModeFlags`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) (that method stays mode-driven only: tutorial/freeplay/frozen/environment/MusicLoopSilent).
- Apply in **`Enter()`** on the relevant handlers via a shared helper (e.g. `BinauralStagePolicy.ApplyBinauralVolumeForStage(StageType)` → `MusicBinauralBeats.instance.SetVolume(...)`):
  - **Mute:** [`MusicPlaylistStageHandler`](../Assets/Scripts/Sequencing/Handlers/MusicPlaylistStageHandler.cs) — today never touches binaural; playlist can inherit **70** from a prior playground.
  - **Mute:** [`LinearAudioStageHandler`](../Assets/Scripts/Sequencing/Handlers/LinearAudioStageHandler.cs) — generalize beyond `Linear_Nature` only (all linear variants).
  - **On:** [`TutorialStageHandler`](../Assets/Scripts/Sequencing/Handlers/TutorialStageHandler.cs) and [`PlaygroundStageHandler`](../Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs) — explicit **Enter** apply so binaural is on even if mode flags run in a different order.
- **`Exit` / `LocalCleanup`:** only if a stage can leave binaural in a wrong state for the *next* stage without that stage’s `Enter` fixing it; default is “next stage `Enter` owns volume.”

**Test Runner tests (EditMode):** `Block5BinauralPolicyEditModeTests` — policy maps playlist/linear → off, tutorial/playground → on (no play mode / Wwise).

**Playtests:** harness **P** on debug playground → `binauralVol` trends to **70** (30s lerp on Enter). **Shift+Q** to **Linear_Nature** → after lerp completes, `binauralVol=0` (or watch Console: `Binaural Beats: New Volume is 0`). Optional: **MusicPlaylist** stage — same mute target.

**Commit:** `Block 5: gate binaural off on MusicPlaylist/LinearAudio stages.`

---

## Stage 2b — Binaural single-authority consolidation (follow-up to Stage 2 regression review)

- **Implemented with: Opus 4.8** · **Regression pass: Opus 4.8 (this stage was itself the regression pass)**
- **Listening load:** Logs only — harness state line shows `binauralBase`, `binauralAtt`, `binauralOut`.

**Why:** The Stage 2 regression review found binaural volume had **two competing default authorities** — `MusicSystem1.SetMusicModeFlags` (mode → 70/50/0) and `BinauralStagePolicy` (stage → 70/0). They disagreed for `MusicLoopSilent` (mode wanted 50) on stages we want muted, so `Linear_Nature` landed at **50 instead of 0** depending on call order. Robin's decision: **one master authority = stage-based**, with a mode-driven **attenuation** toggle layered on top.

**Design (single authority + orthogonal attenuation):**

- **Base bus volume is stage-owned.** [`BinauralStagePolicy.GetTargetVolume(StageType)`](../Assets/Scripts/Sequencing/BinauralStagePolicy.cs) (`Tutorial`/`Playground` → **70**, everything else → **0**) is applied **once**, centrally, in [`SequenceRunner.AdvanceToStage`](../Assets/Scripts/Sequencing/SequenceRunner.cs) right after the re-entrancy guard. Per-handler `Enter()` calls were **removed** (Playground, Tutorial ×3, MusicPlaylist, LinearAudio).
- **Mode only attenuates.** [`MusicSystem1.SetMusicModeFlags`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) no longer sets binaural volume; it calls `MusicBinauralBeats.SetBinauralAttenuated(modeMusicLoopSilentFlag)`. When attenuated, the bus output is reduced by **30%** (`70 × 0.7 ≈ 49`, reproducing the old "50 during MusicLoopSilent", now layered on whatever base the stage set).
- **One RTPC writer.** [`MusicBinauralBeats`](../Assets/Scripts/MusicAndLight/MusicBinauralBeats.cs) holds `_volume` (base, lerped by `lerpVolume`) and `_attenuationFactor` (lerped by `lerpAttenuation`); both funnel through a single `ApplyBusVolume()` = `BinauralAttenuationPolicy.Apply(base, factor)`. Output is always the product, so the two lerps never fight.
- **Pure math extracted** to [`BinauralAttenuationPolicy`](../Assets/Scripts/MusicAndLight/BinauralAttenuationPolicy.cs) for EditMode testing.

**Decisions (Robin):** Savasana = **0**, Linear (all variants) = **0**, every non-Tutorial/Playground stage = **0**. Mid-stage mode flips no longer touch the base (they only attenuate).

**Intentional behavioral deltas to verify in playtest (all consistent with "binaural only audible in Tutorial/Playground"):**
- *Savasana → FrozenFreeplay* (CueStopInteractive) used to raise binaural to 70; now stays **0** (per decision).
- *Playground end `FadeOut()` → Environment* used to snap binaural to 0; now binaural lerps **70 → 0** gracefully as Savasana enters (base 0, 30s).
- *WwiseVO `Cue_Stop_Interactive` fallback → FrozenFreeplay* no longer nudges binaural; base stays where the stage set it.

**Test Runner tests (EditMode):** `BinauralAttenuationPolicyEditModeTests` — `GetFactor(true)=0.7`, `GetFactor(false)=1.0`, `Apply(70,true)=49`, `Apply(0,*)=0`, clamps to 0–100. `Block5BinauralPolicyEditModeTests` unchanged (still valid).

**Playtests:** harness **P** → debug playground: `binauralBase=70 binauralAtt=off binauralOut=70`. **Shift+E** to `Linear_Nature`: after lerp, `binauralBase=0 binauralAtt=on binauralOut=0`. To exercise attenuation visibly, enter a state where base is 70 and mode is `MusicLoopSilent` → `binauralOut≈49`.

**Commit:** `Block 5 follow-up: single-authority binaural (stage owns base volume, mode only attenuates 30%).`

---

## Stage 3 — Block 4: Wwise switch hygiene (safety fixes)

- **Implement with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Logs only — pass criterion is a switch-warning-free console (objective). Fade *feel* is deferred to Stage 7.

**Goal / acceptance:** No `SoundWorldMode_Switch` / `MusicLoops_Switch` warnings on first interactive entry; one sound world; `MusicLoops → Silence` set before interactive entry; sound-world switch posted one frame before the mode switch (robust to same-frame / back-to-back triggers).

**Files:** [`MusicSystem1`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) — `SetMusicModeTo`, `StartInteractiveMusic`, `SetSoundWorld`, `SetMusicLoop`; [`WwiseVOManager.SetToEsketamineAscending`](../Assets/Scripts/WwiseManagers/WwiseVOManager.cs) path.

**Test Runner tests (EditMode):** `Block4SwitchOrderEditModeTests` — world-switch precedes mode-switch; `MusicLoops → Silence` before interactive; no duplicate switch posts in one frame (via a thin testable switch-order policy / log buffer).

**Playtests (Activation + Adjunctive):** console clean of switch warnings through opening → first toning. (Fade-feel / `Stop_Toning` subjective items handled in Stage 7.)

**Commit:** `Block 4: order sound-world/mode switches; MusicLoops→Silence before interactive.`

### Stage 3 — first fix (done): `MusicLoops → Silence` before `InteractiveMusicSystem`

- **Implemented with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**

**Fix:** Lorna's top Block 4 item — *"when switching to InteractiveMusicSystem (not MusicLoops), set Music Loop switch to Silence first."* When entering interactive music for a **SoundWorld** interaction, the music-loop bed (`Play_MusicLoops` from `StartInteractiveMusic`) could bleed through because nothing forced `MusicLoops_Switch → Silence`. Now it's silenced **before** the `InteractiveMusicMode_Switch → InteractiveMusicSystem` post.

**Seam (regression-safe):** the `SoundWorld` branch of [`RecoverInteractiveMusicModeFromInteractionType`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) (called from Tutorial / Freeplay / FrozenFreeplay entry). The **`MusicLoop`** interaction path is deliberately **unchanged** (its loop is chosen by `SetMusicLoop`, so we must not force Silence there). `MusicLoopSilent` mode already sets Silence itself and does not call this method.

**Design:** switch order extracted to a pure, testable policy — [`InteractiveMusicSwitchPolicy.RecoverInteractiveModeSteps(InteractionType)`](../Assets/Scripts/MusicAndLight/InteractiveMusicSwitchPolicy.cs) returns the ordered `InteractiveMusicSwitchOp` list; `MusicSystem1` executes each op (preserving the `RunWithToningRestoredAfterInteractiveSwitch` wrapper + logs).

**Test Runner tests (EditMode):** `Block4SwitchOrderEditModeTests` — SoundWorld silences MusicLoops *before* InteractiveMusicSystem; SoundWorld never routes to MusicLoops; MusicLoop routes to MusicLoops and does **not** force Silence.

**Still open in Stage 3 (next candidates):** sound-world switch one frame before mode switch (same-frame robustness); ~~apply the same policy/Silence at the other InteractiveMusicSystem entry ([`SetSoundWorld`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs))~~ **done in `816218e3`** — `SetSoundWorld` now routes through `InteractiveMusicSwitchPolicy.SetSoundWorldPosts`, which posts `MusicLoops_Switch → Silence` before `InteractiveMusicMode_Switch → InteractiveMusicSystem`; duplicate-post-in-one-frame guard.

**Commit:** `Block 4 (first fix): MusicLoops→Silence before InteractiveMusicSystem via switch-order policy + EditMode tests.`

**Next implementation step (Robin confirmed 2026-06-04):** ~~Stage 3b~~ **done** → **[Stage 4 — Block 7 fundamental split](#stage-4--block-7-core-break-apart-the-fundamental-system-discuss-first)** (design discussion first; enables MusicLoop `Cue_Key_*` → fundamental). Stage 3 one-frame switch ordering can run in parallel if desired.

---

## Stage 3b — Block 8: Microphone volume envelope (**done 2026-06-04 — playtest signed off**)

- **Implement with: Opus 4.8** (envelope) · **3b.0 spike may be Composer 2.5 fast** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Real ear check — headphones required for all playtests in this stage.
- **Source:** [`PLAYTEST_NOTES_ORGANIZED.md` — Block 8](PLAYTEST_NOTES_ORGANIZED.md#block-8--microphone-volume-envelope-large-unity-side). Unity-side only (no bypass of custom Unity audio this pass).

**Goal / acceptance (full Block 8):** Headphone mic monitoring loud enough to guide breath across **calibration → opening → playground → savasana** without harsh jumps or endless slow creep; ADSR-style rise/decay/release replaces sluggish simple multiply where specified in playtest notes.

### 3b.0 — Per-soundscape gain on MicMixer bus (**done 2026-06-04**)

**Problem (guided Part B):** On a **MusicLoops** bed, headphone monitoring is too quiet vs **SoundWorld** — user must push harder to hear themselves.

**Architecture (agreed):** [`DirectVoiceMonitoring`](../Assets/Scripts/Voice/DirectVoiceMonitoring.cs) **`micMixerVolumeContributionsDb`** → summed → Wwise **`MicProcessingVolume`** is the **whole voice-channel output** (direct monitoring today; **future recordings** on the same bus too). Code comments state that. Session/soundscape offsets that should affect “the voice” holistically belong here — **not** per-frame `monitoringSource` scaling alone.

**Remove dead path (done):** `AttenuateMonitoring` / `monitoringAttenuationDb` / `MusicSystem1.SetMonitoringAttenuationOnce` / `NotifyMonitoringAttenuationChangedExternally` — **deleted** (was **0 dB** no-op). Cleaned call sites: `MusicSystem1`, `TutorialStageHandler`, `CalibrationStageHandler`.

**Per-soundscape boost (replaces the interaction-type idea; Robin 2026-06-04):** Keyed on **the soundscape**, not `InteractionType`. When a soundscape is set (`SetSoundWorld` / `SetMusicLoop`), lerp the named MicMixer contribution **`SoundscapeMonitoring`** to that soundscape’s dB over **10 s** (via `LerpMicMixerVolumeContributionTo`). **`MusicLoopSilent` (Savasana / Linear) and any silence → 0** — explicit `ApplySoundscapeMonitoring(null)` so quiet stages get **no** boost. Unknown soundscape → 0. Per-soundscape dB is an **Inspector list** on `DirectVoiceMonitoring` (`soundscapeMonitoringDbs`), seeded **music loops +8, sound worlds 0** — this list is the **long-term source of truth** (Robin, 2026-06-04), tuned by ear. `ApplySoundscapeMonitoring` **warns once per soundscape** if a soundscape is set with no entry (catches new soundscapes added to `MusicSystem1` `soundWorlds`/`musicLoops` without a monitoring value; silence/null is intentional 0, no warning). Tutorial/calibration overrides still block soundscape-driven lerps while they own monitoring.

**Do not** use Wwise `TONING_Volume` for this (music bed RTPC, not voice bus).

**Guided harness — MicMixer A/B coroutine (G playtest, after the 3b.1 ADSR tune):** On `Playground_Debug`, walk **every** soundscape (4 worlds + 3 loops) in **alternating world↔loop** order so each step is an A/B transition Robin can set by ear:

1. SonoFlore (world)  
2. ShiftingEarth (loop)  
3. Shadow (world)  
4. SitarAmbience (loop)  
5. Gentle (world)  
6. PinkNoiseAtmosphere (loop)  
7. Shruti (world)  

**Linear with forward/back stepping:** `Space`/`Return`/`→` = next step, `←`/`Backspace` = previous step — so Robin walks the list in order but can step back and forth between an adjacent world↔loop pair to A/B while tuning (clamps at step 1; finishing the last step ends the part). Logs `SoundscapeMonitoringDb` + `micMixerSumDb` per step. Pass: each soundscape feels usable after tuning `soundscapeMonitoringDbs`.

**3b.0 acceptance:** Part B loop step comfortable; state line shows `SoundscapeMonitoring` ≈ each soundscape’s dB during that soundscape and **0 during MusicLoopSilent**; smooth 10 s crossfades, no clicks.

**3b.0 Test Runner (EditMode):** `SoundscapeMonitoringPolicyEditModeTests` — known soundscape → mapped dB; unknown / null / empty (silence) → 0; null map → 0; lerp duration **10 s**; contribution-name constant matches `DirectVoiceMonitoring`.

**3b.0 files:** `SoundscapeMonitoringPolicy.cs`, `DirectVoiceMonitoring.cs` (per-soundscape list + apply), `MusicSystem1.cs`, `WorldShuffler.cs` (`CurrentSoundscape` getter), `TutorialStageHandler.cs` / `CalibrationStageHandler.cs` (removed `AttenuateMonitoring`), [`MusicDebugGuidedPlaytest.cs`](../Assets/Scripts/Debug/MusicDebugGuidedPlaytest.cs) + `MusicDebugHarness.cs` (A/B cycle + state line), `SoundscapeMonitoringPolicyEditModeTests.cs`.

**3b.1 — Stacked mic monitoring ADSR (**done 2026-06-04 — playtest signed off**)**

**Implementation notes (as built):** Pure math in [`MonitoringAdsrPolicy.cs`](../Assets/Scripts/MusicAndLight/MonitoringAdsrPolicy.cs); per-instance state machine (`MonitoringAdsrVoice`) + per-frame `UpdateMonitoringAdsr()` in `DirectVoiceMonitoring`. Constants: `AttackTarget` **0.9**, `MinDecaySeconds` **3 s**, `ChargeRunwayAtFullChargeSeconds` **10 s** (decay = `max(3, charge01 × 10)` — an **approximation** of "time until charge hits 0", refine later if needed), `ReleaseSeconds` **2.5 s** ease-out (feel of slow chant-down, **not** a literal `_chantLerpSlow` follow), `BoardFaderHighDb` **0**. Inspector knobs in the single box: `monitoringAdsrSustainLevel` (0.5) + `monitoringAdsrBoardFaderLowDb` (−18); telemetry `debugMonitoringAdsrSum` / `debugMonitoringAdsrVoiceCount` (state line: `adsrSum` / `adsrVoices`). `chantPresence` fully retired on the monitoring path; calibration override forces `_adsrPresenceScale` → 1f.

**Problem:** Today `chantPresence` ≈ `BoardFader(_chantLerpSlow)` in [`ApplyMonitoringVolume`](../Assets/Scripts/Voice/DirectVoiceMonitoring.cs) — one slow lerp tracks `toneActive`, so monitoring creeps and feels sluggish.

**Replacement (agreed 2026-06-04):** **Retire `chantPresence` on the monitoring path entirely.** `ApplyMonitoringVolume` uses **only** summed ADSR (clamp → `BoardFader`) for voice presence — **no** multiply with `_chantLerpSlow` / legacy `chantPresence`. `_chantLerpFast` / `_chantLerpSlow` in [`GameValues.handlecChanting`](../Assets/Scripts/Voice/GameValues.cs) may remain for **lights / gameplay** if still referenced; they must **not** drive headphone monitoring after 3b.1.

**Imitone gates (same stream, different debounce in `ImitoneVoiceIntepreter.CheckToning`):**

| Signal | Lock on | Lock off | Role in 3b.1 |
|--------|---------|----------|----------------|
| `toneActive` | 0.05 s | 0.20 s | Legacy chant lerps; **not** ADSR attack trigger |
| `toneActiveConfident` | 0.20 s | 0.40 s | **Attack** trigger (0→1); **no new burst** until this has gone **false** again (“finger off the piano key”) |
| `toneActiveBiasTrue` | Set with `toneActive` on | Cleared with confident off (~0.4 s) | Part of **release** trigger |

**Per-instance state machine (one instance per confident onset):**

1. **Attack (A):** Spawn on rising edge of **`toneActiveConfident`** only if confident was **false** since the previous instance on this burst (blocks re-attack while still “on”).
2. **Decay (D):** After attack peak (~**0.9** target — see rise, below), lerp instance level down to **sustain** over time derived from **`chantCharge` runway** (“how much time until charge hits 0?”), with a **minimum 3 s**. Sustain level default **0.5** (the **S** of ADSR — hold at decay floor, not “infinite full level”).
3. **Sustain (S):** Hold at decay floor until release trigger.
4. **Release (R):** On the **first frame** where **`!toneActiveConfident && !toneActiveBiasTrue`** (Robin: **option C**), **immediately** leave decay/sustain and run release to **0** — do not wait for decay to finish first. Release **curve family** should match the *feel* of slow chant down in [`GameValues.handlecChanting`](../Assets/Scripts/Voice/GameValues.cs) (`_chantLerpSlow` behavior), **not** a literal sample-by-sample follow of `_chantLerpSlow`.

**Stacking (monitoring only):** All non-finished instances **sum** each frame → **clamp to 1.0** → [`AudioLevelUtilities.BoardFader`](../Assets/Scripts/Utilities/AudioLevelUtilities.cs) (high **0 dB**, low tunable) → existing `ApplyMonitoringVolume` chain (`gameOn`, `chargeDuck`, etc.). ADSRs are **additive layers over time**, not a single shared `_chantLerpSlow` multiply.

**Inspector tuning (one box — playtest together, then bake):** On [`DirectVoiceMonitoring`](../Assets/Scripts/Voice/DirectVoiceMonitoring.cs) (or a single adjacent component), one **`[Header]`** group for all Block 8 envelope knobs Robin tunes by ear:

| Field (playtest) | Starting default | When to tune | After sign-off |
|------------------|------------------|--------------|----------------|
| ADSR sustain level after decay (**S**) | **0.5** | **InteractiveSoundSystem** playtest (opening / tutorial toning) | Bake into code; **remove** Inspector field |
| `BoardFader` low (dB) | **−18** | Same session | Bake; **remove** Inspector field |
| `BoardFader` high | **0 dB** | Fixed unless playtest says otherwise | Code constant |
| **DEBUG** meditative-mode override (`meditativeModeDebugOverride`: None / ForcePlayful / ForceMeditative) — **editable in this same box on `DirectVoiceMonitoring`**; pushed each frame to `RespirationTracker.instance` | **None** | Force a mode by ear to compare snappy (playful) vs blended (meditative) ADSR rise without waiting for `_absorption` hysteresis | **Temporary** — **remove** enum + runtime field + override branch (`RespirationTracker`) + field/push (`DirectVoiceMonitoring`) at 3b.1 cleanup |

**Single tuning box (Robin's requirement):** all knobs Robin touches during the Block 8 playtest live in **one** Inspector box — the `=== BLOCK 8 PLAYTEST TUNING ===` group on [`DirectVoiceMonitoring`](../Assets/Scripts/Voice/DirectVoiceMonitoring.cs). He never switches components mid-test. The meditative override is **edited here** even though the logic lives in `RespirationTracker` (DVM pushes it to `RespirationTracker.instance` each frame). 3b.1 adds the ADSR sustain + BoardFader-low fields to this same box.

Playtest instructions must tell Robin to adjust **both** sustain level and BoardFader low in that **same** Inspector box during InteractiveSoundSystem before sign-off.

**Rise (A) shape (agreed 2026-06-04):**

- **Mode gate:** [`RespirationTracker`](../Assets/Scripts/Voice/RespirationTracker.cs) `modeMeditative` / `modePlayful` (hysteresis on `_absorption`: meditative when **> 0.25**, playful when **< 0.1**; posts Wwise `AbsorptionMode` state on the **instant** bool flip).
- **Init:** `modeMeditative` = **`false`** at startup (already true in code); **`modeMeditativeLerp`** = **0** at startup.
- **Attack driver (per instance):** Lerp from 0 → **~0.9** using a **blended chant reference** each frame:
  - **Snappy (playful):** `chantLerpFast` only.
  - **Meditative:** `0.5 * (chantLerpFast + chantLerpSlow)`.
  - **Blend weight:** **`modeMeditativeLerp`** ∈ [0, 1] — **not** the raw bool (no audible jump when mode flips).
- **`modeMeditativeLerp` (add in `RespirationTracker.cs`):** There is **no** existing smoothed value today. Add a public field in the **same file** as the bool; each frame move toward target **1** when `modeMeditative`, **0** when `modePlayful`, over **60 s** wall-clock (full 0↔1 traverse ≈ **60 s**). Monitoring ADSR and any future “meditative vs playful” Unity audio math read **`modeMeditativeLerp`**, not `modeMeditative`.
- **Implementation TODO (in the lerp update block):** *Talk to Lorna — Unity `modeMeditativeLerp` is 60 s but Wwise `AbsorptionMode` still flips on the instant bool; align or document intentional split.*
- **Attack spawn** still gated on **`toneActiveConfident`** rising edge (see state machine above); chant lerps supply the curve shape during the attack phase.

**Future recording (not 3b.1):** Pre-record voice gate is a **separate** path — likely `toneActiveConfident` + `toneActiveBiasTrue` with a **ring buffer** to catch attack; **hard on/off, no decay**. Documented in the record/replay TODO at top of [`DirectVoiceMonitoring.cs`](../Assets/Scripts/Voice/DirectVoiceMonitoring.cs).

**Primary files:** [`RespirationTracker.cs`](../Assets/Scripts/Voice/RespirationTracker.cs) (`modeMeditativeLerp`), [`GameValues.handlecChanting`](../Assets/Scripts/Voice/GameValues.cs) (chant lerps), [`DirectVoiceMonitoring.ApplyMonitoringVolume`](../Assets/Scripts/Voice/DirectVoiceMonitoring.cs), optional small `MicMonitoringAdsrPolicy.cs` for EditMode tests.

**Telemetry (when debugging):** **Voice bus:** `debugMicMixerVolumeSumDb` / MicMixer contributions (includes `SoundscapeMonitoring`). **Headphone tap:** `telemetryEffectiveMonitoringGain` ≈ `monitoringVolume` × `dynamicScale` × `monitoringSource.volume` (**ADSR sum** × `chargeDuck` × `gameOn` after 3b.1 — **not** legacy `chantPresence`) — **no** `attenuationScale` after 3b.0 removal. Not in either formula: imitone `normalizationGainDb`.

**Test Runner tests (EditMode):**

- **3b.0:** `SoundscapeMonitoringPolicyEditModeTests` — known soundscape → mapped dB; unknown / null / silence → 0; null map → 0; 10 s lerp constant.
- **3b.1:** [`MonitoringAdsrPolicyEditModeTests`](../Assets/Editor/SoundSelf/Tests/EditMode/MonitoringAdsrPolicyEditModeTests.cs) — only the **non-trivial** policy rules + constants (lean-policy rule of thumb): rise blend `Lerp(fast, mean(fast,slow), modeMeditativeLerp)` incl. clamp; decay duration `max(3s, charge01 × 10)` (floored / mid / full); constant defaults (0.9 / 0.5 / 3 / 0 / −18). The single-expression rules (release `!confident && !bias`, attack edge `armed && now && !last`, `Clamp01` sum, BoardFader presence mapping) are **inlined in `DirectVoiceMonitoring`** and covered by playtest, not EditMode. **Written 2026-06-04 — run in Test Runner before playtest.**

**Run:** Test Runner → EditMode, or `.\Tools\run-editmode-tests.ps1`.

**Playtests:**

Guided `G` order (after Parts A/B): **3b.1 ADSR first, then 3b.0 MicMixer A/B** — dial the envelope shape before tuning per-soundscape headroom on top of it.

| Step | Pass criteria |
|------|----------------|
| **3b.1 — InteractiveSoundSystem tune** | Headphones; SonoFlore. 5 guided steps (playful rise → meditative rise → decay/sustain → release → stacking). In **one Inspector box**: tune **ADSR sustain after decay** (start 0.5) and **BoardFader low** (start −18 dB) until attack/decay/release feel right; paste `B457` telemetry (`adsrSum` / `adsrVoices`) |
| **3b.1 — Meditative vs playful** | Use `meditativeModeDebugOverride` (same box) — override **snaps** `modeMeditativeLerp`: playful rise = snappy (fast-only), meditative = rounder (mean fast/slow); no click on switch. Reset override to **None** after |
| **3b.1 — Opening / playground / savasana** | After bake: attack not sluggish; decay to sustain not endless creep; release on finger-off (both gates false) not harsh |
| **3b.0 — MicMixer A/B cycle** | Guided sequence visits **all** soundscapes alternating world↔loop: SonoFlore → ShiftingEarth → Shadow → SitarAmbience → Gentle → PinkNoiseAtmosphere → Shruti. Linear with **forward/back** stepping (`→`/`Space` next, `←`/`Backspace` back) to A/B adjacent pairs. Tune **`soundscapeMonitoringDbs`** (Inspector) per soundscape until each ≈ usable; loops seeded **+8** |
| **3b.0 — MusicLoopSilent check** | Enter Savasana / Linear (MusicLoopSilent): `SoundscapeMonitoringDb` shows **0** (no boost on quiet stages) |
| **3b.0 — Part B re-check** | Guided Part B loop step: comfortable monitoring under world after bake |
| **Calibration → opening → tutorial** | Per [Block 8 playtest table](PLAYTEST_NOTES_ORGANIZED.md#block-8--microphone-volume-envelope-large-unity-side): level across stages; `debugMicMixerVolumeSumDb` + `telemetry Effective Monitoring Gain` |

**Must not break:** Calibration `SetChantBasedAttenuationOverride` (chant duck on **monitoringSource** path, separate from MicMixer); `CalibrationMicrophone` MicMixer contribution; `Cue_Microphone_ON` / `OFF` gating; Block 3 noise-floor work ([`PLAYTEST_NOTES` Block 3](PLAYTEST_NOTES_ORGANIZED.md#block-3--calibration--lights-medium)).

**Commit:** `3dc74f22` — per-soundscape MicMixer monitoring + stacked ADSR envelope; bake playtest tuning; policy tests.

---

# Appendix D — Stage 4 (Block 7 fundamental): active-source design spec + 4e status & decision log

## Stage 4 — Block 7 core: break apart the fundamental system (discuss first)

- **Implement with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**
- **This stage starts with a design discussion, not code.** Agree the split before editing.
- **Listening load:** Logs only — lock precedence / mode gates verified from the state line.

**Status (2026-06-05): design agreed with Robin — replacing the priority-ranked lock stack with an explicit *active-source* model.** Implementation not started; this section is the agreed design + remaining open questions.

**Goal / acceptance:** The fundamental is driven by exactly **one active source** at a time, switched explicitly. **Debug** is the only override on top. No priority ranking (`Debug > Content > Mode` retired); `None` is not a valid source. Each source tracks its own preferred fundamental; harmony + interpreted input continue to derive from the master fundamental. **Stage 5 (`Cue_Key_*` Unity listener) is folded in here as sub-stage 4g** — the listener lives in `MusicSystem1` and feeds the MusicBed source, so it belongs to this system.

### Sub-stage ordering (each ends with a regression test)

Behavior-**preserving** refactor first (4a–4d), then behavior-**changing** wiring (4e–4g). A careful regression checkpoint follows **every** sub-stage. **4e is itself broken into reviewed sub-sub-stages** (one source-changing zone at a time).

| Sub-stage | What | Behavior change? | Regression |
|---|---|---|---|
| **4a** | Dead-field cleanup (`fundamentalNoteCompare`, `harmonyRetriggerThreshold`, `harmonyTimeSinceLastTrigger`) | none | EditMode green; compiles |
| **4b** | Split + rename `NoteTracker` → `voiceActivity` + `fundamentalChargeByNote` (still inside `MusicSystem1`) | none | EditMode parity |
| **4c** | **Cosmetic `partial class` file split** of the oversized `MusicSystem1.cs`: move the fundamental + harmony method bodies into `MusicSystem1.InputDrivenFundamental.cs` / `MusicSystem1.InputDrivenHarmony.cs` (same class, fields stay declared in the core file) | none (pure file reorg; no new objects, no scene wiring, no `MainGame.unity` touch) | EditMode parity; logs unchanged |
| **4d** | Active-source authority: `FundamentalSource`, `FundamentalSourcePolicy`, `SetFundamentalSource` / `SetFundamentalForSource` / `SetDebugFundamentalOverride`, private `ApplyMasterFundamental`; legacy lock setters become **thin shims** | none (shims provably identical for the real flows) | EditMode: policy + shim-equivalence |
| **4e** | **Migrate call sites to `SetFundamentalSource` — its own carefully-staged sub-stage, broken into reviewed sub-sub-stages, one source-changing *zone* at a time** (Robin reviews each). Start from a **clean commit**. Zones: (1) startup = Sequence on `Awake`; (2) `SetSoundWorld`→InputDriven / `SetMusicLoop`→MusicBed; (3) `SetMusicModeTo` Tutorial/Freeplay/Frozen; (4) `Tutorial.cs` A/C-hum correction; (5) `WwiseVOManager` unlock cue; (6) `SavasanaStageHandler`. Each zone: remove the relevant shim, add EditMode end-state test, Robin review, commit. Then remove `ResolveFundamentalOnUnlock`. No blanket Freeplay gate; last-writer-wins ordering. | **yes** (genuinely source-driven) | per-zone EditMode end-state tests + **subjective (Round 1)** |
| **4f** | `HarmonyRunPolicy.ShouldRun(mode, gameOn)` gating `HarmonyUpdate` (in `MusicSystem1.InputDrivenHarmony.cs`): run in `InteractiveTutorial`, `Freeplay`, and `MusicLoopSilent && gameOn` (Savasana tail; Linear stays off via `gameOn=false`). Scope = harmony only. | **yes** (small: harmony adds the savasana toning tail) | EditMode `HarmonyRunPolicy` + **subjective (Round 1)** |
| **4g** | (folds former Stage 5) Cue listener in `MusicSystem1`: post `Play_MusicLoops` with the `AK_MusicSyncUserCue` flag → `TryHandleMusicKeyCue` → MusicBed source; same shared handler at VO/closing callbacks; binaural follows master (free via `ApplyMasterFundamental`); fix the binaural retune coalescing/handle no-op for rapid cues | **yes** (new: cues drive key) | EditMode cue-map; **subjective (Round 2)** |
| **4h** | **Retire `FrozenFreeplay`** (optional follow-up): collapse its 4 call sites to `SetMusicModeTo(Freeplay)` + `SetGameOn(false)` + Sequence(C); delete the enum value, `GameOnPolicy` case, `Update()` branch, `modeFrozenFreeplayFlag`, `Block3` test. Gated on a **`gameOn` audit** (see §below). | **yes** (mode removed; behavior intended-equivalent) | EditMode (gameOn map, `HarmonyRunPolicy`) + **subjective (Round 3, short)** |

**Subjective testing — two rounds:**
- **Round 1 (after 4f):** behavior-change pass for the Unity-side refactor — dynamic source switching, who-won state line, fundamental tracking feel across the migrated zones, and harmony now playing during the Savasana toning tail. 4a–4d are behavior-preserving so they ride a quick *parity* confirmation that nothing changed; 4e–4f are the first real listening.
- **Round 2 (after 4g):** cue pass — MusicBed `Cue_Key_*` driving the fundamental + binaural following the key, rapid-cue coalescing. (Needs the harness cue simulation; full Wwise-embedded verification waits on Lorna.)

So: **two rounds**, not one — split at the cue boundary (4g), because the cue listener adds a distinct new audible behavior worth isolating.

### 4h — Retire `FrozenFreeplay` (optional follow-up after the active-source model lands)

**Why it becomes removable.** `FrozenFreeplay` today has only two jobs (its whole footprint is tiny — `modeFrozenFreeplayFlag` is effectively write-only): (1) **pin the fundamental to C** (`SetFundamentalModeLock(true, C)`), and (2) **stop the voice→music pipeline** by *not* calling `DynamicMusicSystem()` in its `Update()` branch. `gameOn` is then set false by `GameOnPolicy`. There's already a `//TODO: likely we don't need this mode anymore` at `MusicSystem1.cs:975`.

- **Job (1) dissolves into the active-source model:** "frozen fundamental" = active source **Sequence(C)**. An InputDriven source would only *track*, never *write*, so the freeze is expressed by authority, not by stopping the loop. (Already mapped: `Enter FrozenFreeplay → Sequence(C)`.)
- **Job (2) is fully downstream of `gameOn`** (verified 2026-06-05): every `DynamicMusicSystem` sub-update collapses when `imitoneVoiceInterpreter.gameOn` is false —
  - `imitoneActive = gameOn && !nearNoiseFloor` (`ImitoneVoiceIntepreter.cs:991`), forced false with no tone (`:1021`); tone flags clear in the `!imitoneActive` branch (`:1114`, `:1118-1119`); `toneActiveBiasTrueFrame` follows (`:1226`).
  - `FundamentalUpdate` gated by `if (imitoneActive)` (`MusicSystem1.cs:643`); `HarmonyUpdate` by `if (toneActiveBiasTrueFrame)` (`:742`); `InterpretImitoneUpdate` note-activation by `if (imitoneActive)` (`:2056`) (the lines above only compute local fields, no Wwise/no `NoteTracker` mutation).
  - `BasicToningUpdate` / `BassSynthUpdate` are edge-triggered on the tone flags; the **falling edge** actively calls `StopWwiseToning()` (`:1728`) / `Stop_BassSynth` (`:1809`), then no-ops. So `gameOn=false` doesn't just skip work — it *stops* the audio, arguably cleaner than today (FrozenFreeplay relies on a separate stop).

So the collapse is: **callers do `SetMusicModeTo(Freeplay)` + `SetGameOn(false)` + Sequence(C)** instead of `SetMusicModeTo(FrozenFreeplay)`.

**The one real watch-item — `gameOn` audit (gates 4h).** Collapsing into `Freeplay` means `GameOnPolicy` maps the mode → `true`, so the call sites must explicitly drive `gameOn=false`, **and nothing in the `Freeplay` entry path may re-assert `gameOn=true`.** This audit — not the music updates — is what makes or breaks 4h.

Call sites to convert (the 4 `FrozenFreeplay` callers): `PlaygroundStageHandler.cs` (×2, ~137 & ~384), `SavasanaStageHandler.cs` (~165), `WwiseVOManager.cs` fallback (~347).

**Checklist:**
- [ ] Confirm `Freeplay` entry (`SetMusicModeTo(Freeplay)` path) does not set `gameOn=true` after the caller sets it false (audit `StartInteractiveMusic`, `RecoverInteractiveMusicModeFromInteractionType`, `ApplyGameOnPolicy`).
- [ ] Convert the 4 call sites to `Freeplay` + explicit `SetGameOn(false)` + `SetFundamentalSource(Sequence, C)`.
- [ ] `HarmonyRunPolicy`: key the Freeplay case on `gameOn` for coherence (harmony already can't fire — gated on `toneActiveBiasTrueFrame` — but make the policy honest).
- [ ] Delete enum value `FrozenFreeplay`, its `Update()`/`SetMusicModeTo` branches, `modeFrozenFreeplayFlag`, the `GameOnPolicy` case, and `Block3PolicyEditModeTests.FrozenFreeplayMode_AssignsGameOnFalse`.
- [ ] Accept the **release-tail** difference (tone flags decay over the normal negative thresholds rather than hard-cutting) — equivalent to releasing a tone; confirm `CueStopInteractive` doesn't need an instant freeze.

**Caveats (intended-equivalent, not byte-identical):** `DynamicMusicSystem()` now *runs as a no-op* in Freeplay+gameOn=false (only harmless timers — `fundamentalTimeSinceLastTrigger`, `bassSynthCooldownTimer` — advance) rather than not being called; and the stop is via release tail. Both are audibly equivalent to today, but worth a short **Round 3** listening pass.

**Sequencing:** keep this **out of Stage 4 proper** — it touches `gameOn` semantics and three sequence handlers, which would muddy the behavior-preserving→behavior-adding arc. Do it after the active-source model is in and proven.

### The model: one active source + a Debug override

```
enum FundamentalSource { InputDriven, MusicBed, Sequence }   // active source (Debug is a separate override)
```

- **InputDriven** — sung-pitch tracking (the decision ladder; owns the per-note **charge memory**). Keeps tracking its `preferredFundamental` even when it is **not** the active source (so a later handoff is meaningful).
- **MusicBed** — the bed's key. Fed by `Cue_Key_*` user cues embedded in the MusicLoops (wired in Stage 5); the old static `musicLoops`→note table (`SetMusicLoop` → `SetFundamentalContentLock`) is **replaced by cues**. Holds the last cued note as its preferred.
- **Sequence** — the sequencer/stage pins the note. Replaces today's *mode lock* **and** savasana's use of the *content lock*. Tutorial / FrozenFreeplay / Savasana pin **C**; Freeplay hands off to InputDriven.
- **Debug override** — dev keys / harness. Sits above the active source; clearing it restores `activeSource.preferred`. Replaces `fundamentalDebugLock`.

### Per-source preferred + the write API

Each source carries its own `preferredFundamental`. Methods:
- `SetFundamentalSource(FundamentalSource source, NoteName firstFundamental = None)` — switch active source. `None` → adopt that source's existing preferred (no reset). A real note → set that source's preferred to it. **For `InputDriven`, a real note also wipes the per-note charge memory ("clean slate").**
- `SetFundamentalForSource(FundamentalSource source, NoteName note)` — update a source's preferred; writes master only if it's the active source and no Debug override, else staged for when it next becomes active. **If `source == InputDriven`, this also runs the clean-slate reset** (Robin, 2026-06-05).
- `SetDebugFundamentalOverride(NoteName? note)` — set/clear the override.

`SetFundamentalDirect` becomes the **private** apply mechanism (`ApplyMasterFundamental`): clear `fundamentalChange` queue → set `fundamentalNoteName` → Wwise `...FundamentalOnly` switch → binaural retune → reset activation/charge → `directorStoredFundamental`. No external bypass. `ResolveFundamentalOnUnlock` is **removed** — "unlock" becomes "switch active source," resolving to that source's preferred.

### Director ↔ fundamental "goblin" → moved to [Stage 9](#stage-9--block-7-director--fundamental-unification-the-synchresistiming-goblin)

The Director/fundamental interaction (synchresis flourish accounting, queue timing delegation, `directorStoredFundamental` redesign, flush-on-source-switch, disabled-bypass) is substantial enough to be its own stage. **Full writeup, variables, hypotheses, tangles, and open questions: [Stage 9](#stage-9--block-7-director--fundamental-unification-the-synchresistiming-goblin).** It rides with/after 4g (when MusicBed first competes for the master); a couple of small fixes inside it could land earlier.

### InputDriven vs `DynamicMusicSystem` gating (Robin, 2026-06-05)

`FundamentalUpdate()` **is** the InputDriven source, and it only lives inside `DynamicMusicSystem()` (runs only in `InteractiveTutorial` + `Freeplay`). So InputDriven is only *alive* in those "tracking modes." Two distinct gates:

- **Tracking gate** = `DynamicMusicSystem` running (mode ∈ {`InteractiveTutorial`, `Freeplay`}). InputDriven runs its **full ladder** here **even when it is not the active source** — it is a **shadow tracker** (Robin 2026-06-05): per-note charge accumulates, and when the **long test** passes behind the curtain it does a **silent commit** — `preferred[InputDriven] = changeTarget` + `ResetFundamentalTimers()` — but **does not** touch the master / Wwise / binaural / Director. So `preferred` *does* track behind the curtain (via long commits); the master is the only thing silenced. The short test is skipped behind the curtain (it needs the Director, which is silenced). On reactivation, `preferred` already holds the behind-the-curtain result and the master adopts it directly.
- **Write gate** = active source == InputDriven **and** no Debug override. Only then does `FundamentalUpdate` push to the master via `ApplyMasterFundamental`.

This reframes today's behavior exactly: in Tutorial, `FundamentalUpdate` already *tracks* but the mode-lock C blocks the *write*; in the new model the write is blocked because Sequence (not InputDriven) is the active source. Same observable result.

**Switching active source → InputDriven while `DynamicMusicSystem` is not running ⇒ `Debug.LogWarning` (B457).** Nothing would be running to track/drive the master, so it almost always indicates a sequencing mistake. In normal flows InputDriven only becomes active in Freeplay/Tutorial (both tracking modes), so the warning is purely a guardrail. **Behavior: warn-and-honor** — we still set the source and apply `firstFundamental` once (the master won't *track* until a tracking mode resumes). _(Refusing could cause a worse silent failure; revisit if it bites.)_

**Sequence / MusicBed / Debug are NOT gated by `DynamicMusicSystem`** — they are direct setters and apply the master fundamental immediately in any mode.

Policy: `FundamentalSourcePolicy.IsTrackingMode(MusicMode)` (true for `InteractiveTutorial`/`Freeplay`) backs both the InputDriven write gate and the warning; covered by `Block7FundamentalPolicyEditModeTests`.

### File split (Robin 2026-06-05 — purely cosmetic `partial class`)

The driver for 4c is that `MusicSystem1.cs` (~2,900 lines) is **cumbersomely large**, not that the fundamental/harmony need their own objects. So 4c is a **`partial class MusicSystem1`** file reorganization — *not* new MonoBehaviours or plain classes, no back-reference seam, no Unity wiring, no `MainGame.unity` edit. The compiled type is identical; behavior is provably unchanged (method bodies cut-and-pasted into another file of the same class).

- **`MusicSystem1.InputDrivenFundamental.cs`** (`public partial class MusicSystem1`) — `FundamentalUpdate`, `TryApplyFundamentalChangeTriggers`, `TryGetSustainedFundamentalShiftTarget`, `ResolveFundamentalChangeTarget`.
- **`MusicSystem1.InputDrivenHarmony.cs`** (`public partial class MusicSystem1`) — `HarmonyUpdate`, `changeHarmony`.
- **Fields stay declared in the core `MusicSystem1.cs`** (the compact field block) — partials share all members, so nothing needs accessors. The apply path (`ChangeFundamental` / `SetFundamentalDirect` / locks / `ResetFundamentalTimers` / `ResolveFundamentalOnUnlock`) stays in the core file (it becomes `ApplyMasterFundamental` in 4d).
- `DynamicMusicSystem()` keeps calling `FundamentalUpdate()` then `HarmonyUpdate()` exactly as today (same class, methods now defined in sibling files).

**Note on the original component design:** partials are *cosmetic only* — they shrink the file but do not enforce an object boundary. That matches the stated intent. The genuine architectural work (single-writer active-source authority) and the **testable pure rules** still land in 4d/4f via `FundamentalSourcePolicy` / `HarmonyRunPolicy`, which is where unit-test value lives regardless of file layout. If a real object boundary is ever wanted, a partial can be promoted to its own class later.

### NoteTracker split (per-note charge memory) — DONE in 4b (`d18b002e`)

Today `NoteTracker` packs two unrelated jobs in one tuple `(ActivationTimer, Active, FirstFrameActive, ChangeFundamentalTimer)`:
- **Activation half** (`ActivationTimer/Active/FirstFrameActive`) — written by `InterpretImitoneUpdate`, read by **toning** (`musicNoteActivated`).
- **Charge half** (`ChangeFundamentalTimer`) — used **only** by the fundamental decision.

→ **Split the tuple** into two dictionaries, **both declared in `MusicSystem1`** (4c is a cosmetic partial-class split, so the fundamental method bodies move to a sibling file but the fields stay in the core file — no object owns the charge dict).

**Rename both (Robin 2026-06-04 — `NoteTracker` is uselessly generic for a music system):** as built in 4b —
- Activation tracker: `voiceActivity` : `Dictionary<NoteName, VoiceActivity>` where `VoiceActivity { float ActiveSeconds; bool IsActive; bool JustActivated; }` (renames `ActivationTimer→ActiveSeconds`, `Active→IsActive`, `FirstFrameActive→JustActivated`).
- Charge memory: `fundamentalChargeByNote` : `Dictionary<NoteName, float>` (renames `ChangeFundamentalTimer` → the dictionary value).

Confirm names before coding.

### Sequence source (mode lock + savasana content-lock) — careful refactor

Ownership is today *implicit* (priority stack) and set/cleared in scattered places: `SetMusicModeTo` (Tutorial/Frozen pin C, Freeplay unlock), `Tutorial.cs` (A/C-hum correction pins C then releases), a VO unlock cue, and `SavasanaStageHandler` (content-lock C via `MusicLoopSilent`, not the mode lock). The refactor makes ownership **explicit single-writer**: each stage/mode declares the active source on entry. Full refactor expected (Robin 2026-06-04). The safe sequencing is to land the authority + InputDriven/Harmony split first with the lock setters kept as **thin shims** over `SetFundamentalSource` (provably identical), then delete the shims and migrate call sites. **The shim-equivalence is itself baked into EditMode tests** (Robin 2026-06-04): each old call (`SetFundamentalModeLock(true,C)`, `SetFundamentalContentLock(C)`, unlock) must produce the same active-source + master + preferred state as the direct `SetFundamentalSource` call it forwards to — so the migration provably never changes behavior.

**4d implementation — Option A (Robin 2026-06-05, most behavior-preserving):**
- New partial file `MusicSystem1.FundamentalAuthority.cs` holds the `FundamentalSource` enum, the active-source / per-source-preferred / debug-override state, and `SetFundamentalSource` / `SetFundamentalForSource` / `SetDebugFundamentalOverride`.
- `SetFundamentalDirect`'s body becomes private **`ApplyMasterFundamental`**; `SetFundamentalDirect` stays a public one-line shim (external callers `MusicKeyCuePolicy` 4g + `InputReferences` debug keys still compile; migrated later).
- **Lock-SET** paths keep their existing conditional/priority/logging logic; only the inner master-write (`SetFundamentalDirect(x)` *inside the active-lock guard*) routes through the source API: `ModeLock→Sequence`, `ContentLock→Sequence` (interim — observably identical to MusicBed in 4d since loops have no cues yet; `SetMusicLoop` splits to MusicBed in 4e/4g), `DebugLock→SetDebugFundamentalOverride`. Old lock fields stay synced.
- **Lock-CLEAR** paths are unchanged (still `ResolveFundamentalOnUnlock`), so the **input write-gate stays `IsFundamentalLocked()`** in 4d. The active-source state is *recorded* but the gate flips to active-source as call sites migrate in 4e (and `ResolveFundamentalOnUnlock` is removed at the end of 4e).
- EditMode `Block7FundamentalPolicyEditModeTests` covers the **pure `FundamentalSourcePolicy`** (`IsTrackingMode`, the source-write rule, mode→source mapping); full shim-equivalence/real-flow end-states ride the playtest + the pure-policy pins (instantiating `MusicSystem1` in EditMode isn't practical — too many Wwise deps).

### Active-source transitions (startup + regression-proof plan)

Startup + the explicit switch points (Robin 2026-06-04). **Default: `Sequence` on `Awake`** (preferred = the startup `fundamentalNoteName`).

| Trigger | Call site | New action |
|---|---|---|
| Scene load | `MusicSystem1.Awake` | active source = **Sequence** (preferred = startup fundamental) |
| Soundscape set to a **SoundWorld** (InteractiveMusicSystem, not a loop) | `SetSoundWorld` | → **InputDriven** |
| Soundscape set to a **MusicLoop** | `SetMusicLoop` | → **MusicBed** (interim: preferred = static loop key until Stage 5 cues feed it) |
| Enter Tutorial | `SetMusicModeTo(InteractiveTutorial)` | → **Sequence** (C) *(replaces `SetFundamentalModeLock(true,C)`)* |
| Enter Freeplay | `SetMusicModeTo(Freeplay)` | → soundscape-driven (**InputDriven** for world / **MusicBed** for loop) *(replaces `SetFundamentalModeLock(false)`)* |
| Enter FrozenFreeplay | `SetMusicModeTo(FrozenFreeplay)` | → **Sequence** (C) *(replaces `SetFundamentalModeLock(true,C)`)* |
| A/C-hum correction | `Tutorial.cs` | → **Sequence** (C), then restore prior source *(replaces lock(true,C)/false)* |
| VO unlock cue | `WwiseVOManager` | → restore soundscape-driven source *(replaces `SetFundamentalModeLock(false)`)* |
| Enter Savasana | `SavasanaStageHandler.Enter` | → **Sequence** (C) *(replaces `SetFundamentalContentLock(C)`)* |

**Ordering (regression-proof via explicit sets, NOT a mode gate) — Robin 2026-06-04:** There is **no** blanket "only in Freeplay" suppression (that earlier proposal was wrong). Source changes are legitimate in multiple modes — including **Tutorial** and the **awkward adjunctive-savasana tail**. Each switch point sets the source **explicitly** and **last-writer-wins** ordering decides the outcome (a stage that must hold C sets `Sequence(C)` after any soundscape set in the same entry). The regression-proof contract is the transition table above + EditMode tests that assert the **resulting** active-source/master/preferred state for each real flow (tutorial entry + hum correction + release; freeplay world↔loop shuffle; savasana tail), not a mode-gated guard.

### 4e IN-PROGRESS STATUS (2026-06-05, branch WorkingWwise, on top of 4d `e80da993`) — UNCOMMITTED working tree

> **LIVE status is §Active work at the top of this doc** (it supersedes the "Immediate next step" / "NOT yet done" wording below, which predates the regression pass). The detail below is the per-zone build record.
>
> **SUPERSEDED by Step 0 (2026-06-05):** the per-zone notes below describe the 4d **shim** era — lock setters still present, `GetLockedFundamental` kept, gate calls threading `debugFundamentalOverride.HasValue`, DebugLock CLEAR → `SetDebugFundamentalOverride(null)`. **Step 0 deleted all of that** (entire lock stack + debug override + dev force-note surfaces; policies lost `hasDebugOverride`). Read the zone notes for *intent/history*; the current code has no lock stack. See §Active work "Step 0" at the top.
>
> **Shadow-tracker `preferred`-sync is no longer a 4e item — it is DEFERRED to 9c** (a standalone attempt failed; it's Layer 1 of the goblin commit semantics). Spec consumed by 9c: [`HANDOFF_InputDriven_preferred_shadow_tracker.md`](HANDOFF_InputDriven_preferred_shadow_tracker.md).

**Done in the working tree (NOT yet committed — Robin is reviewing zone-by-zone; all zones commit together after a log/subjective pass):**
- **Zone 1 (startup = Sequence):** `FundamentalSourcePolicy.StartupSource` const (= Sequence); `activeFundamentalSource` initializer + `MusicSystem1.Start` both use it; `Start` declares ownership via `SetFundamentalSource(StartupSource, fundamentalNoteName)` (in `Start`, not `Awake`, because the write path iterates `fundamentalChargeByNote`). EditMode pin `StartupSource_IsSequence`. **Ran green** (Robin, zone 1).
- **Zone 2 (soundscape):** `SetSoundWorld` → `SetFundamentalSource(InputDriven)`; `SetMusicLoop` → `SetFundamentalSource(MusicBed, startNote)`. `GetMusicLoopFundamental` reframed as the **start note** (seed-before-cues, kept permanently — see 4g note). Content-lock calls removed from both.
- **Zone 3 (mode):** `SetMusicModeTo` Tutorial/Frozen → `SetFundamentalSource(Sequence, C)`; Freeplay → `SetFundamentalSource(FundamentalSourcePolicy.SourceForInteractionType(currentInteractionType))` (new pure policy: MusicLoop→MusicBed, else InputDriven). Last-writer-wins confirmed (Tutorial entry sets no soundscape after).
- **Preparatory soundscape (Robin 2026-06-05):** new `SetSoundscapeWithoutChangingFundamentalSource(soundscape)` + a `changeFundamentalSource = true` param threaded through `SetSoundscape`/`SetSoundWorld`/`SetMusicLoop`. Preparatory (false): SoundWorld touches nothing; MusicLoop only **remembers** the start note via `SetFundamentalForSource(MusicBed, startNote)` (stored, no master write, source stays Sequence). `OpeningStageHandler`'s 3 calls (`Shadow`/`ShiftingEarth`/`SonoFlore`) use it — Opening pre-stages while Silent without seizing the fundamental (matches legacy: legacy `SetSoundWorld` content-lock-clear was a no-op for a fresh world; this stays silent + source-stable). The earlier `warnIfNotTracking` flag on `SetFundamentalSource` was **reverted** (preparatory path no longer calls it; B457 stays a pure guardrail).
- **Enum TODO** added above the `soundWorlds`/`musicLoops` string tables (future refactor, out of scope for 4e).
- **Step 1 — GATE FLIP (done):** both InputDriven gates now use `FundamentalSourcePolicy.CanInputDrivenWriteMaster(activeFundamentalSource, debugFundamentalOverride.HasValue)` — the ENQUEUE gate (`MusicSystem1.InputDrivenFundamental.cs` `TryApplyFundamentalChangeTriggers`) and the WRITE gate (`MusicSystem1.cs` `ChangeFundamental`). `ChangeFundamental`'s else-branch warning reframed from "locked (`GetLockedFundamental`)" to "InputDriven is not the active source (active=…, debugOverride=…)".
- **Step 2 — Zone 4 (`Tutorial.cs`) (done):** correction pin (was `SetFundamentalModeLock(true, C)`) → `SetFundamentalSource(Sequence, C)` **guarded** to run only when `currentInteractionType == SoundWorld` (a MusicLoop bed owns the key — leave MusicBed). The two non-"Hum" releases (was `SetFundamentalModeLock(false)`) → new `ResumeFundamentalAfterCorrectionPin()`. New helper in `MusicSystem1.FundamentalAuthority.cs`: SoundWorld → `SetFundamentalSource(InputDriven, fundamentalNoteName)` (clean-slate resume from the pinned note, NOT the stale shadow preferred); MusicLoop → `SetFundamentalSource(MusicBed)` (adopt the bed's preferred). Deliberately different from playground entry (which adopts preferred with no seed).
- **Step 3 — Zone 5 (done):** `WwiseVOManager.VoTryFundamentalModeUnlock` was DEAD CODE (zero callers; its job moved to the Tutorial guidanceCount release) → **deleted**.
- **Step 4 — Zone 6 (`SavasanaStageHandler`) (done):** `SetFundamentalContentLock(NoteName.C)` → `SetFundamentalSource(FundamentalSource.Sequence, NoteName.C)`.
- **Step 5 — `ResolveFundamentalOnUnlock` removed + 3 lock-CLEAR sites rewired + dead `IsFundamentalLocked()` deleted (done; Robin chose Option A):** `ResolveFundamentalOnUnlock()` and `IsFundamentalLocked()` deleted (kept `GetLockedFundamental()` — still read by content/mode SET-path logging). DebugLock CLEAR → clear legacy field + `SetDebugFundamentalOverride(null)` (exact active-source equivalent: restores active source preferred). Content/Mode lock CLEAR → clear legacy field + log only, **no source switch / no master write** (production resume is owned by the explicit `SetFundamentalSource` calls in zones 2/3/4/6; the only remaining callers of these CLEAR paths are the debug harness; full debug rewire onto the source API is 4g). Subtle debug-only change flagged: legacy debug-unlock could re-queue a tracking change; the new path snaps to active-source preferred immediately.
- **Step 6 — test + doc (done):** added `SourceForInteractionType` EditMode test (MusicLoop→MusicBed, SoundWorld→InputDriven) to `Block7FundamentalPolicyEditModeTests`; this status block updated.

**Files touched (uncommitted):** `FundamentalSourcePolicy.cs`, `MusicSystem1.FundamentalAuthority.cs`, `MusicSystem1.cs`, `MusicSystem1.InputDrivenFundamental.cs`, `Assets/Scripts/Sequencing/Tutorial.cs`, `Assets/Scripts/WwiseManagers/WwiseVOManager.cs`, `Assets/Scripts/Sequencing/Handlers/SavasanaStageHandler.cs`, `OpeningStageHandler.cs`, `Assets/Editor/SoundSelf/Tests/EditMode/Block7FundamentalPolicyEditModeTests.cs`, this plan.

**Status (superseded — see the LIVE-status banner above):** the call-site migration (gate flip + zones 4/5/6 + `ResolveFundamentalOnUnlock` removal + test) is in the working tree, **plus Step 0** (the whole legacy lock stack + debug override deleted — 2026-06-05). **Outstanding for the 4e bundle is only the commit ceremony:** Robin runs Test Runner (EditMode) → Opus regression pass → explicit go → single bundled 4e commit (record hash below). The **shadow-tracker `preferred`-sync fix is no longer a 4e item** — it was deferred to **9c** (a standalone attempt failed; it's Layer 1 of the goblin commit semantics). Spec: [`HANDOFF_InputDriven_preferred_shadow_tracker.md`](HANDOFF_InputDriven_preferred_shadow_tracker.md).

**GATE-FLIP FINDING (2026-06-05) — supersedes the earlier "gate flip standalone, zones 4/5/6 after" idea:** the two input gates are `IsFundamentalLocked()` at `MusicSystem1.InputDrivenFundamental.cs:55` (ENQUEUE) and `MusicSystem1.cs` `ChangeFundamental` (WRITE); both flip to `FundamentalSourcePolicy.CanInputDrivenWriteMaster(activeFundamentalSource, debugFundamentalOverride.HasValue)`. `ChangeFundamental` is only ever called by the InputDriven ladder + `ResolveFundamentalOnUnlock`, so gating it on the active-source rule is exact. **But** the remaining lock-setter CLEAR sites are **"unlock → resume voice tracking"** flows whose resume is done *today* by `ResolveFundamentalOnUnlock`:
  - **Zone 4 — `Tutorial.cs`:** `SetFundamentalModeLock(true, C)` at :551 (pin C for A/C-hum correction), then `SetFundamentalModeLock(false)` at :723 / :1005 **only when moving to a non-"Hum" vocalization** → resume tracking. So Tutorial is NOT statically Sequence(C): it pins C for correction, then **releases to InputDriven**.
  - **Zone 5 — `WwiseVOManager.VoTryFundamentalModeUnlock` :99:** `SetFundamentalModeLock(false)` on a VO cue → resume soundscape-driven source.
  - **Zone 6 — `SavasanaStageHandler` :87:** `SetFundamentalContentLock(C)` — a pin (SET), no release; the 4d shim already routes SET → `SetFundamentalSource(Sequence, C)`, so it works un-migrated, but migrate for cleanliness.

  Consequence: once the gate is active-source and `ResolveFundamentalOnUnlock` is gone, a bare field-clear leaves the source stuck (e.g. `Sequence(C)`) and **input never resumes**. So **the gate flip MUST be bundled with zones 4 & 5 (and 6) migration + `ResolveFundamentalOnUnlock` removal**: each release site must call `SetFundamentalSource(InputDriven)` (Tutorial release / VO unlock = resume tracking; Tutorial release happens in tutorial = tracking mode, so no B457). After that, the only remaining lock-setter callers are **debug** (`InputReferences` :315/323/333/341, `MusicDebugHarness` :306/316/317, `OnPermanentlySetFundamentalChanged` → `SetFundamentalDebugLock`); decide whether the debug CLEAR paths switch to InputDriven or are migrated (these overlap 4g). The lock setters' SET paths keep working via the 4d shim until then.

**Immediate next step (2026-06-05, UPDATED — all chunked Steps 1–6 done in the working tree, UNCOMMITTED):** the full 4e-finish is implemented and lint-clean (gate flip; zone 4 Tutorial pin SoundWorld-guarded + releases via `ResumeFundamentalAfterCorrectionPin()`; zone 5 dead `VoTryFundamentalModeUnlock` deleted; zone 6 Savasana → `SetFundamentalSource(Sequence, C)`; `ResolveFundamentalOnUnlock` + `IsFundamentalLocked` removed with the 3 CLEAR sites rewired per Robin's Option A; `SourceForInteractionType` EditMode test added). **Resume here:** full diff → Robin runs Test Runner (EditMode) → Opus regression pass → Robin's explicit go → single bundled 4e commit (record the hash in the commit log below). No EditMode/Test Runner run was done during implementation (chunk rule).

#### 4e-finish chunked steps (Robin 2026-06-05 — review-driven, NOT bundled)

Robin wants the 4e-finish work done as **logically-comprehensible chunks he reviews one at a time** (these are sensitive code paths), **not** a single bundle. Intermediate chunks **need not compile / be in a working state** — only the **end of the bundle** is coherent; **no EditMode/Test Runner run until all chunks are done**. Debug lock-setter CLEAR behavior is **deferred for discussion at Step 5** (not pre-decided). The dead `IsFundamentalLocked()` is to be **deleted** (Robin) once both gates are flipped.

**STATUS (2026-06-05): all 6 steps DONE in the working tree (uncommitted).** Step 5 debug-CLEAR decision = **Option A** (clear legacy field only; no source switch on Content/Mode CLEAR; DebugLock CLEAR → `SetDebugFundamentalOverride(null)`).

1. **Gate flip (conceptual core).** Both InputDriven gates `IsFundamentalLocked()` → `FundamentalSourcePolicy.CanInputDrivenWriteMaster(activeFundamentalSource, debugFundamentalOverride.HasValue)`: ENQUEUE gate `MusicSystem1.InputDrivenFundamental.cs` `TryApplyFundamentalChangeTriggers` (`test`), WRITE gate `MusicSystem1.cs` `ChangeFundamental`. Reframe `ChangeFundamental`'s else-branch warning from "locked (`GetLockedFundamental`)" to "InputDriven is not the active source (active=…, debugOverride=…)". *After this, production release sites are not yet migrated → input won't resume; fixed in steps 2–4.*
2. **Zone 4 — `Tutorial.cs`.** Pin `:551` → `SetFundamentalSource(Sequence, C)`; non-"Hum" releases `:723` / `:1005` → `SetFundamentalSource(InputDriven)` (resume tracking; tutorial = tracking mode, no B457).
3. **Zone 5 — `WwiseVOManager.VoTryFundamentalModeUnlock:99`.** VO unlock cue → `SetFundamentalSource(InputDriven)`.
4. **Zone 6 — `SavasanaStageHandler:87`.** Pin → `SetFundamentalSource(Sequence, C)` (cleanliness; 4d shim already routed content-lock SET here).
5. **Remove `ResolveFundamentalOnUnlock` + rewire its 3 lock-CLEAR call sites; delete dead `IsFundamentalLocked()`.** **STOP and discuss the debug lock-setter CLEAR behavior with Robin here** (DebugLock / ContentLock / ModeLock clears now have only debug callers — `InputReferences` I/O/K/L/N/M, `MusicDebugHarness` 306/316/317; decide switch-to-InputDriven vs minimal field-clear vs defer to 4g).
6. **Tests + doc.** Add `SourceForInteractionType` EditMode test (MusicLoop→MusicBed, else→InputDriven); mark this status block's gate flip + zones 4/5/6 done; record the commit hash here on Robin's explicit go.

Then: full diff → Robin runs Test Runner (EditMode) → Opus regression pass → wait for Robin's explicit go → single bundled commit.

### `HarmonyUpdate` run-gate — `HarmonyRunPolicy` (Robin 2026-06-04)

Harmony runs when `HarmonyRunPolicy.ShouldRun(MusicMode mode, bool gameOn)` is true (still also honoring `enableHarmonyTracking`):
- `InteractiveTutorial` → true
- `Freeplay` → true
- `MusicLoopSilent` → **`gameOn`** (the Savasana toning tail — see note)
- everything else (Silent / FrozenFreeplay / Environment) → false

**Why `MusicLoopSilent && gameOn` = the Savasana tail:** `MusicLoopSilent` is shared by **Savasana** and **Linear**, but `LinearAudioStageHandler` forces `SetGameOn(false)` while the adjunctive Savasana keeps `gameOn` true until its delayed mic-off. So `gameOn` cleanly distinguishes the savasana toning tail (harmony on) from Linear (off) — no need for `MusicSystem1` to know `StageType`. `FrozenFreeplay` (standard savasana after `CueStopInteractive`) stays **off**.

This is the **one deliberate behavior change** to harmony: vs. today it *additionally* runs during the Savasana tail while the user is still toning. Pure policy → EditMode test (incl. the documented assumption that `MusicLoopSilent && gameOn` ⇒ savasana, so a future non-savasana `MusicLoopSilent && gameOn` stage would trip the test). **Scope = harmony only** (the `HarmonyUpdate` gate); the `DynamicMusicSystem` mode branch for fundamental/toning is unchanged. Decoupled from the active fundamental source (reads the master fundamental whoever set it).

> 4c just relocates `HarmonyUpdate` (preserving today's exact gate `InteractiveTutorial`/`Freeplay`); **4f** then swaps that gate for `HarmonyRunPolicy` (adds the `MusicLoopSilent && gameOn` savasana case).

### MusicBed cues — Wwise wiring note (Stage 5, informs MusicBed here)

`AK_MusicSyncUserCue` callbacks are **per `PostEvent` instance**, not per-gameObject: cues arrive only if the Play event was posted with the callback flag + delegate. VO events already do (`WwiseVOManager.VOCallbackFunction`). **`Play_MusicLoops` (`MusicSystem1`) and `Play_MusicPlaylist` (`WwiseVOManager`) are posted WITHOUT the flag** → `Cue_Key_*` in the bed won't reach Unity until Stage 5 adds the flag + a handler feeding the **MusicBed** source. `MusicSystem1` posts `Play_MusicLoops` on its own gameObject, so the handler can live there.

### Dead-field cleanup (do FIRST)

- `fundamentalNoteCompare` — never read → **delete** (first commit).
- `harmonyRetriggerThreshold` / `harmonyTimeSinceLastTrigger` — seeded + incremented but **never gated** (intended 6 s min between harmony retriggers, never wired). **Delete both** (Robin 2026-06-04: cut, do not wire).

### Test Runner tests (EditMode): `Block7FundamentalPolicyEditModeTests`

- Source-write rule: Debug override beats any active source; only the active source writes; `None` rejected as a source.
- Input-driven mode gate `IsTrackingMode`/`CanInputDrivenChange` (`InteractiveTutorial`/`Freeplay` track; not Frozen / MusicLoopSilent / Environment / Silent).
- InputDriven-source-outside-tracking-mode: `SetFundamentalSource(InputDriven, …)` in a non-tracking mode warns (B457) and warn-and-honors (source set, note applied once); Sequence/MusicBed/Debug are ungated by tracking mode.
- `HarmonyRunPolicy.ShouldRun(mode, gameOn)`: true for `InteractiveTutorial`, `Freeplay`, and `MusicLoopSilent && gameOn`; false otherwise (incl. `MusicLoopSilent && !gameOn` = Linear, and `FrozenFreeplay`).
- `NoteName.None` note rejection in the apply path.
- Clean-slate reset on `SetFundamentalSource(InputDriven, note)` and `SetFundamentalForSource(InputDriven, …)`.
- Mode→source mapping regression pin (Tutorial/Frozen → Sequence C; Freeplay → soundscape-driven).
- Shim-equivalence: each legacy lock call yields the same active-source/master/preferred state as the `SetFundamentalSource` it forwards to.
- Real-flow end-state (no blanket Freeplay gate): tutorial entry/hum/release, freeplay world↔loop shuffle, savasana tail each land on the documented active-source/master/preferred.

### Playtests (one inspector block + single `b457` filter)

- **One** Inspector block (harness / `MusicSystem1`) tracking: **active source, master fundamental, each source's preferred, harmony, debug override** — Robin watches only this block.
- Harness keys to switch active source dynamically and set/clear the debug override; STATE line (`b457`) shows who won.
- **`b457` console hygiene:** audit existing `b457`-tagged logs and quiet the incidental ones (binaural volume `B457 Binaural Beats: New Volume…`, Director queue activation) so the fundamental test console shows only the harness STATE line + source switches.

### Commits (one per sub-stage, each after its regression test)

- **4a** `Block 7: remove dead fundamental/harmony fields (fundamentalNoteCompare, harmony retrigger).`
- **4b** `Block 7: split + rename NoteTracker → voiceActivity + fundamentalChargeByNote.`
- **4c** `Block 7: cosmetic partial-class split of MusicSystem1 (InputDrivenFundamental + InputDrivenHarmony files).`
- **4d** `Block 7: active-source fundamental authority + FundamentalSourcePolicy (lock setters as shims) + tests.`
- **4e** (multiple commits, one per reviewed zone) `Block 7: migrate <zone> to SetFundamentalSource …` — startup, soundscape, mode, tutorial, VO, savasana.
- **4f** `Block 7: HarmonyRunPolicy — harmony runs in Tutorial/Freeplay + Savasana tail (MusicLoopSilent && gameOn).`
- **4g** `Block 7: MusicBed Cue_Key_* listener (Play_MusicLoops callback) + binaural follows key + retune coalescing.`
- **4h** (optional follow-up) `Block 7: retire FrozenFreeplay — Freeplay + gameOn=false + Sequence(C) (gameOn audit).`

---

# Appendix E — Future stages 5–8: detail

## Stage 5 — Block 7: `Cue_Key_*` listener (Unity side folded into Stage 4g) + Lorna external embedding

> **Restructured (Robin 2026-06-04):** the Unity-side cue listener moved **into Stage 4 sub-stage 4g** (the listener lives in `MusicSystem1` and feeds the **MusicBed** source — it's part of the same system). What remains as "Stage 5" is the **external** dependency: Lorna embedding the cues in Wwise, then end-to-end verification. The cue→`NoteName` map + handler spec below are the 4g contract.

- **Implement with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Logs + optional ear — state line confirms binaural Hz matches the cued note (objective); optional headphone check that bed/binaural sit in key.

**Goal / acceptance (4g contract):** Shared `TryHandleMusicKeyCue(cue)` called from the `Play_MusicLoops` callback in `MusicSystem1` **and** at the top of [`VOCallbackFunction`](../Assets/Scripts/WwiseManagers/WwiseVOManager.cs) / `ClosingCallBackFunction`; cue→`NoteName` map per Lorna's spellings; routes to the **MusicBed** source (`SetFundamentalForSource(MusicBed, note)`); **binaural center frequency follows** automatically via `ApplyMasterFundamental`; unknown `Cue_Key_*` logged at warning. This is the fix for "binaural tracking the wrong sound" — in MusicLoops the fundamental was content-locked and never tracked the bed's key changes.

**Cue → `NoteName` map:** `Cue_Key_C..B` → naturals; `Cue_Key_Gsharp` → Gs; `Cue_Key_Bflat` → As; `Cue_Key_Aflat` → Gs; `Cue_Key_Eflat` → Ds. Single dictionary keyed by full cue name; no duplicated switch cases across the two callbacks.

**Test Runner tests (EditMode):** `Block7MusicKeyCueEditModeTests` — every Lorna string → expected `NoteName`; unknown → warn + no change.

**Playtests (keyboard, minimal listening):** simulate each `Cue_Key_*`; state line shows fundamental + binaural Hz matching; optional headphone check that bed / binaural sit in key.

**External dependency:** Lorna embeds the cues in Wwise ([Appendix A](PLAYTEST_NOTES_ORGANIZED.md#appendix-a--externallorna--non-unity-batch-together)). The harness verifies the Unity side now. Note: `Play_MusicPlaylist` is posted **without** the music-sync callback flag — if bed key cues fire there, that post needs the flag added.

**The loop→key table is the bed's START note, not a "required" lock (Robin 2026-06-05 — keep it permanently):** 4e zone 2 has `SetMusicLoop` call `SetFundamentalSource(MusicBed, GetMusicLoopFundamental(loop))`. `GetMusicLoopFundamental` is reframed as the loop's **start note** — the preferred we seed `preferred[MusicBed]` to **before any `Cue_Key_*` arrives**, so the bed opens in-key and there's **no wrong-key window / rapid switch** when the first cue lands. When 4g cues go live they **UPDATE** `preferred[MusicBed]` via `SetFundamentalForSource(MusicBed, cuedNote)`; the start note is **not removed** — it just stops being the only source of truth (cues take over after the first one). A loop with no start note (`None`) hands ownership to MusicBed at its existing preferred and waits for a cue.

**Commit:** `Block 7: shared Cue_Key_* handler; binaural follows master fundamental.`

---

## Stage 6 — Block 7: pitch / 5ths / harmony audit

- **Implement with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Real ear check — interval math is tested, but consonance ("does it sound harmonious") requires headphones.

**Goal / acceptance:** Harmonious pitches around 5ths (Fundamental + Harmony); `changeHarmony` `NoteName.None` guard solid. (The dead harmony-retrigger threshold was deleted in Stage 4; if a retrigger floor is wanted, decide + wire it here.) Also revisit whether `HarmonyUpdate` should additionally gate on `gameOn` (deferred from Stage 4).

**Test Runner tests (EditMode):** harmony interval selection guards; `NoteName.None` guard.

**Playtests:** harness: set fundamental, trigger harmony, confirm intervals; subjective consonance check.

**Commit:** `Block 7: pitch/5ths + harmony guards audit.`

---

## Stage 7 — Block 7 + Block 4: interactive fade, silent loops, Stop_Toning

- **Implement with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Real ear check — silent-loop persistence is tested objectively, but fade-in feel and `Stop_Toning` pacing require headphones.

**Goal / acceptance:** Interactive fade-in not abrupt / too loud (`SILENT_Volume` + switch order from Stage 3); silent loops persist when toning stops; `Stop_Toning` Wwise-paced, not an instant Unity cut.

**Files:** [`MusicSystem1`](../Assets/Scripts/MusicAndLight/MusicSystem1.cs) — `BasicToningUpdate`, `StopWwiseToning`, silent-layer RTPCs; coordinate Wwise fades with Lorna ([Appendix A](PLAYTEST_NOTES_ORGANIZED.md#appendix-a--externallorna--non-unity-batch-together)).

**Test Runner tests (EditMode):** silent-loop persistence invariant (stop events target toning v3 layers, not `Stop_MusicLoops`).

**Playtests:** harness: start toning, stop, confirm bed remains; subjective fade-in / out feel (headphones).

**Commit:** `Block 4/7: interactive fade, silent-loop persistence, Stop_Toning pacing.`

---

## Stage 8 — Block 7: lock C before savasana + 15:00 transition *(plan stage 8 — not playtest Block 8)*

- **Implement with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Real ear check — lock timing / gate are tested, but crossfade smoothness at 15:00 and into savasana requires headphones.

**Goal / acceptance:** Fundamental locks to **C** ~60s before savasana; **15:00** milestone crossfade smooth with pitches locked before the transition.

**Files:** [`PlaygroundStageHandler.ProtocolStacksPlaygroundCoroutine`](../Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs) (milestones ~16×60 / 15:00), [`Director.cs`](../Assets/Scripts/Sequencing/Director.cs), [`SavasanaStageHandler.cs`](../Assets/Scripts/Sequencing/Handlers/SavasanaStageHandler.cs).

**Test Runner tests (EditMode):** lock-C-before-savasana timing constant; pitch-lock-before-15:00 gate.

**Playtests:** harness: jump to 15:00 sim and savasana sim; confirm lock + smooth handoff.

**Commit:** `Block 7: lock C before savasana; smooth 15:00 transition.`

---

# Appendix F — External (Lorna / Wwise) + cross-cutting regression risks

## External (Lorna / Wwise) — tracked, not stages

See [Appendix A](PLAYTEST_NOTES_ORGANIZED.md#appendix-a--externallorna--non-unity-batch-together):

- Embed `Cue_Key_*` in Wwise per her timelines (opening / closing / loops).
- Celestial Dreamscape → stop when Chakapa begins.
- Wwise-authored `Stop_Toning` fades (coordinate after Stage 3 / Stage 7).

The harness lets us finish and verify the Unity side independently of Lorna's build.

---

## Cross-cutting regression risks (watch each Opus pass)

- The 4s binaural stop / retune under rapid cue changes.
- `SetMusicLoop` / `SetSoundWorld` vs `MusicLoopSilent` incompatibility (explicit comments in `MusicSystem1`).
- Lock precedence interactions when cues, content locks, and director all want the fundamental.
- `Play_MusicPlaylist` posts **without** the music-sync callback flag — bed key cues there won't reach Unity unless the flag is added.
- Director queue: don't reintroduce the self-removal bug when touching shuffle / fundamental queueing in later stages.

---

# Appendix G — Stage 9 ("goblin"): Director ↔ fundamental unification (design exploration)

## Stage 9 — Block 7: Director ↔ fundamental unification (the synchresis/timing "goblin")

- **Implement with: Opus 4.8** · **Regression pass: Opus 4.8 (required)**
- **Listening load:** Real ear+eye check — flourish pairing, anti-clutter pacing, and "logic-only changes are silent" all need headphones + watching the lights.
- **Status: DESIGN SETTLED (2026-06-05).** The `targetNextFundamental` slot + shadow-tracker model is agreed end-to-end (see *Consolidated design*); no code yet. Rides **with/after 4g** (first time a second source — MusicBed — genuinely competes for the master); 9a can land earlier. Only remaining choice is scope sub-question (b). The earlier `F1–F9` + *Proposed design synthesis* are kept as the **reasoning trail**; the *Consolidated design* is the build spec and supersedes their mechanisms where noted.
- **Depends on:** 4d (active-source authority — DONE, `ApplyMasterFundamental` + `FundamentalSource` + `FundamentalSourcePolicy`), and conceptually on 4e (call sites migrated to sources).

### The situation

The Director does two jobs for the fundamental:
1. **Timing** — it binds audiovisual change to *player behavior*. `ActivateQueue` is driven mostly by behavior detection ([`GameValues.cs:412`](../Assets/Scripts/Voice/GameValues.cs)) and tone onset ([`ImitoneVoiceIntepreter.cs:545`](../Assets/Scripts/Voice/ImitoneVoiceIntepreter.cs)), **not** by the fundamental logic. Queuing *without* immediate activation means: "Director, commit this **soon** — you choose the moment."
2. **Synchresis** — every activation tries to make the change both *heard* and *seen*. [`ActivateQueue`](../Assets/Scripts/Sequencing/Director.cs) counts audio vs visual events and **adds the missing modality**: audio-only ⇒ add a visual flourish (`NextPreferredColorWorld` + `FXWave`); visual-only ⇒ add an audio flourish (`TweakAudio` + `PlayTransitionSound`). A queued fundamental change is flagged `isAudioAction=true, isVisualAction=false`, so today it always pulls a **visual** flourish to pair the pitch shift.

**The core mismatch:** those `isAudioAction`/`isVisualAction` flags are **static at enqueue**, but in the active-source model a fundamental change is only **audible** when it actually moves the master — i.e. `source == active && !debugOverride && target != master`. A change "behind a closed curtain" (e.g. InputDriven's queued change firing after a loop bed has become the active source) is a **logic-only** update to that source's `preferred`: it must **not** retune **and must not** pull a synchresis flourish. A static `audio=true` on such an item yields a **phantom flourish** (lights shift with nothing heard).

### Variables / state in play

- **Authority:** active source, each source's `preferred`, the debug override, the current master `fundamentalNoteName`.
- **InputDriven tracking:** per-note charge (`fundamentalChargeByNote`), `fundamentalTimeSinceLastTrigger`, `fundamentalRetriggerThreshold`, the long/longish/short thresholds; the **tracking gate** (`DynamicMusicSystem` running) vs the **write gate** (active==InputDriven && !debug).
- **Director:** the `fundamentalChange` queue items + their static audio/visual flags; `timeSinceLastActivation`; the transition-sound 5s cooldown (`canPlayTransitionSound`); `director.disable`.
- **`directorStoredFundamental`** (the dedupe memo — see below).
- **Who fires activation:** behavior detection, tone onset, stage transitions — external to the fundamental logic.

### The two key insights (recorded earlier)

- **Synchresis point:** audibility (and thus flourish pairing) is a property of *whether the master actually moved*, not of the action's identity. Silent logic updates must be invisible to the Director's audio/visual accounting.
- **Timing point (Robin):** for a **deferred** change (queued, not immediately activated), *whether it's audible* and *what note it commits* are **fire-time** properties — the Director owns *when*, and conditions can change before it fires. So the deferred action should be **late-bound** ("commit the active source's *current* preferred to master, if it differs"), evaluated at fire time — which also tracks the player's evolving intent rather than freezing a stale target.

### Hypotheses explored

1. **Per-source Director categories** (different `fundamentalChange` event types per source). *Rejected* — the unified rule (below) makes the Director see only "the master moved (audible)," one category, one pairing. No new categories needed.
2. **Unified rule (favored):** *the decision to make an audible change* ⇒ enqueue one `fundamentalChange` audio action then activate (immediately, or on next tone). The master changes as the **queued action's effect**, not its trigger. Silent (preferred-only) updates never touch the Director. Audibility gate at the decision point: `source==active && !debug && target!=master`; `target==master` ⇒ no enqueue/activate.
3. **Late-bound action (favored for deferred):** queued action = "commit active source's current preferred at fire," not a frozen note.
4. **Realized-effect reporting (favored — upgraded from fallback by the timing point):** the fundamental queue item reports what it *actually did* (did the master move?); `ActivateQueue` counts **realized** audio/visual for flourish accounting. This is the honest fix for the deferred phantom-flourish.
5. **Flush-pending-on-source-switch (favored — correctness guard):** clear pending `fundamentalChange` items whenever the active source switches, so a stale cross-source change can't fire/stomp after the switch. Complementary to (4).
6. **Robin's source-owned-enqueue + clear-on-switch + the switch-*back* question:** each source's logic owns enqueuing its own change and clearing it on switch-away. The tangle: *when we switch back to a source, do we restore its previously-queued Director action?*
   - **Proposed elegant answer: don't restore the queue — regenerate from durable source state.** The Director queue is **ephemeral and timing-bound**; resurrecting a stale item after an arbitrary gap would fire it at a moment unrelated to when it was created (defeating the timing job). What *persists* across the inactive period is the **source state** (`preferred` + per-note charge). For InputDriven the **tracking gate keeps charge accumulating even while inactive** (warm handoff), so on switch-back the normal short/long-test logic **re-queues naturally from the live charge** — correctly timed to *current* behavior. So flush-on-switch is complete on its own; **no restoration mechanism, no "saved previous queue."**
   - **InputDriven must NOT enqueue to the Director while behind the curtain (Robin 2026-06-05).** Two gates, not one: the **charge-accumulation** gate is the tracking gate (`DynamicMusicSystem` running) — charge keeps building while inactive — but the **enqueue** gate (the short/long-test trigger ladder that adds `fundamentalChange` to the Director) is `active source == InputDriven && !debugOverride`, same as the write gate. So while another source owns the master, InputDriven silently builds charge but **adds nothing to the Director** — no curtain-side queue churn, nothing to flush later. The **only "restore"-like moment** is *entering* InputDriven: at that instant the normal trigger ladder evaluates the **live** charge and may enqueue then (a strongly-built note can even long-test immediately on takeover — a clean warm handoff). That is just normal logic running on current state, not a replayed/saved queue.
   - This also dovetails with Robin's note that **source switches are usually (not always) Director-tied:** a switch that *moves* the master is itself an audible change ⇒ queue-then-activate (it *is* a Director beat); a switch that does **not** move the master (new source's preferred == master) is silent ⇒ no Director event. The "not always" cases self-handle via the `target!=master` gate.
7. **Director-disabled ⇒ bypass (required):** the Director is disabled during **Opening** ([`OpeningStageHandler`](../Assets/Scripts/Sequencing/Handlers/OpeningStageHandler.cs)), **Savasana** ([`SavasanaStageHandler`](../Assets/Scripts/Sequencing/Handlers/SavasanaStageHandler.cs)), and toggled around **Playground** ([`PlaygroundStageHandler`](../Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs)). Routing through the queue while disabled would **drop** the change. So when `director.disable`, audible changes **bypass to `ApplyMasterFundamentalRaw` immediately**. (Savasana pins C *and* disables the Director — must keep working.)
8. **5s flourish anti-clutter (required):** only the **transition sound** has a 5s cooldown today; the **visual flourish** + `TweakAudio` have none. Extend a `timeSinceLastActivation`-gated suppression (~5s) to the *add-an-effect* (flourish) blocks so rapid re-activations still propagate changes through the Director **without** flourish spam.
9. **Re-entrancy split (required):** `ApplyMasterFundamentalRaw` (retune only — the queued action's effect) vs the "announce" path (enqueue + activate). The queued action uses the raw form so activation never re-enqueues.

### The tangles (why this isn't trivial)

- Static enqueue flags vs dynamic fire-time audibility (the core).
- Re-entrancy if "apply" both retunes and enqueues.
- A queued change that goes silent at fire (source switched, or preferred drifted back to master) must produce **no** flourish.
- The Director can be disabled exactly in stages that still pin the fundamental (Savasana).
- Rapid changes (esp. once MusicBed cues arrive) risk flourish clutter; only sound is cooled-down today.
- `directorStoredFundamental` conflates "applied master" and "queued target" (see below).
- Long-test bug: immediate apply isn't counted, so it gets no pairing (and no-ops if queue empty).
- MusicBed/Sequence currently bypass the Director entirely ⇒ audible source-driven shifts get **no** visual pairing today.
- Side effects (`ResetFundamentalTimers`, `ClearQueueOfType`) must **not** fire on a silent preferred-only update.

### `directorStoredFundamental` — redesign + cleanup

Today it's a single `NoteName?` written in three places (`ApplyMasterFundamental` = applied master; short-test + `ResolveFundamentalOnUnlock` = queued target) and **read in exactly one** — the short-test `directorMatchTest` dedupe (don't re-queue a weak change to a note that's already queued/current). The long/longish tests ignore it. **Two issues:**
1. **It conflates "applied master" with "queued target,"** which diverge once multiple sources exist. Redesign: make the dedupe **master-relative** (the `target!=master` gate already does most of this) and/or **per-source** (each source remembers its own pending target). Decide as part of the late-binding design (what the deferred action commits).
2. **Cleanup (Robin 2026-06-05):** if its job is purely an **InputDriven** dedupe, the field should **move into `MusicSystem1.InputDrivenFundamental.cs`** (same partial class) and be **renamed descriptively** — e.g. `inputDrivenQueuedFundamental` or `pendingInputDrivenChangeTarget`. (It currently lives in the core field block at `MusicSystem1.cs:78`.)

### Proposed design synthesis (for the new context to refine)

- **Two apply primitives:** `ApplyMasterFundamentalRaw(note)` (retune + side effects, no Director) and an **announce** helper `RequestAudibleFundamental(timing)` that enqueues a late-bound `fundamentalChange` whose action commits the active source's current preferred (if it differs), then activates (immediate) or defers (on-tone).
- **Sources call the announce helper** only when authoritative and the master would move; otherwise they just update their `preferred` (immediate, logic-only).
- **Director gains realized-effect accounting** for `fundamentalChange` (count what actually happened), plus the **5s flourish suppression** and **disabled-bypass**.
- **Flush pending `fundamentalChange` on every active-source switch**; never restore — regenerate from source state.
- **Retire/relocate `directorStoredFundamental`** per above.

### Design resolution (2026-06-05, Opus 4.8 — grounded in the current code)

Read of the live code (`Director.cs`, `MusicSystem1.InputDrivenFundamental.cs`, `ApplyMasterFundamental`) tightened the plan. Findings + decisions:

**F1 — What the long-test bug actually is.** Today (`InputDrivenFundamental.cs:76-77`) long/longish test does `ChangeFundamental(changeTarget); director.ActivateQueue(5.0f);`. `ChangeFundamental` → `ApplyMasterFundamental`, which **itself** calls `director.ClearQueueOfType("fundamentalChange")` and retunes — the change happens **outside** the queue. So when `ActivateQueue` runs, the fundamental move is **not a queue item** → not counted (`countAudioEvents=0`). If the queue is empty it early-returns (no flourish at all); if some *visual* item is present it would even add a **phantom audio** flourish (`TweakAudio`+`PlayTransitionSound`). Net: the audible long-test change gets no reliable visual pairing. The short-test path is fine today because it enqueues a counted `isAudio=true` item that `ActivateQueue` later executes + counts.

**F2 — 9a fix = enqueue-then-activate, but it REQUIRES the disabled-bypass.** The minimal fix is to make the long-test change a counted `fundamentalChange` item then `ActivateQueue` (so it's counted → pairs a visual). **But** `AddActionToQueue` returns `-1` and `ActivateQueue` early-returns when `director.disable` is true — and `FundamentalUpdate` (InputDriven) can run in **Freeplay while Playground has toggled the Director off** (`PlaygroundStageHandler` enable :82 / disable :383). Naive enqueue-then-activate would **drop** the change there (regression vs today's direct apply). So **9a must branch on `director.disable`: disabled ⇒ apply raw (today's behavior); enabled ⇒ enqueue-then-activate.** This is the disabled-bypass (hypothesis 7) surfacing already in 9a, and it makes 9a the clean **seed of the 9c announce helper**.

**F3 — Re-entrancy is benign for 9a.** `ActivateQueue` iterates a **copy** (`queuedItems`) and `queue.Clear()`s at the end; the executed action's inner `ClearQueueOfType("fundamentalChange")` only touches the live dict (harmless to the in-flight copy). So 9a does **not** strictly need the `Raw`/announce split to be correct — but introducing a tiny `AnnounceFundamental(target, immediate)` helper now (raw-when-disabled; else enqueue, activate iff immediate) is the right seed and lets long + short share one path.

**F4 — 5s flourish suppression = dedicated timer.** Do **not** reuse `timeSinceLastActivation` (it also drives `activateWhenEmptyThreshold = 25s`). Add a dedicated `timeSinceLastFlourish`; gate **both** the audio-flourish and visual-flourish *add* blocks (`ActivateQueue` ~435-454) at ~5s; reset it **only when a flourish is actually added**. Queued actions still execute every activation — only the *flourish add* is suppressed, so rapid changes propagate without flourish spam. Extract the predicate to `FundamentalDirectorPolicy.ShouldAddFlourish(timeSinceLastFlourish, window)`. (The existing `PlayTransitionSound` 5s cooldown stays; it independently protects direct calls.)

**F5 — Realized-effect mechanism (resolves Open Q3): nullable `Func<bool>` on the queue item.** Add `Func<bool> realizedAudioAction` (nullable) to `DirectorQueueItem`. When present, `ActivateQueue` executes **it** in place of `action` and counts realized audio from its **bool return** (visual still from `isVisualAction`); when null, behavior is exactly as today (every existing item unaffected). The `fundamentalChange` announce item supplies this func; it returns whether the master **actually moved**. Chosen over re-reading state after a void action (one execution, no ordering ambiguity) and over per-source event categories (rejected hypothesis 1).

**F6 — Late-bound deferred (resolves Open Q1): late-bound, precisely defined.** The queued action commits **`preferred[activeSource]` → master iff** `target != master && ShouldWriteMaster(active, active, hasDebug) && no debug override`, and **reports realized audio** = "did the master move." Implemented with a `SetSourcePreferredNoApply(source, note)` (records *intent* without writing master) at enqueue + the announce action that applies it at fire. For the **current** InputDriven ladder this is observably equivalent to a frozen target (preferred only moves via a re-queue, which is `firstFrameActive`-gated), so **no regression** — but it (a) fixes the `target==master`-at-fire phantom-flourish + wasteful re-tune, and (b) is correct once MusicBed competes. Frozen-target + a realized-gate is a simpler near-equivalent; late-bound is preferred for future-proofing.

**F7 — `directorStoredFundamental` redesign (resolves Open Q2): per-source pending memo + master-relative gate.** It currently does a **dual** job — written by `ApplyMasterFundamental` (= applied master) **and** by short-test (= queued target), read only by the short-test dedupe (`directorStoredFundamental != changeTarget`). **9b = pure mechanical move + rename** into `MusicSystem1.InputDrivenFundamental.cs`, behavior identical (keep both writes + the one read). **9c** then sharpens it to a pure `NoteName? inputDrivenQueuedFundamental` (null = nothing pending): set on short-test enqueue, **cleared on fire / flush-on-switch / long-apply**; the "already current" half of the dedupe becomes a direct `changeTarget != fundamentalNoteName` check (master-relative). Name: `inputDrivenQueuedFundamental`.

**F8 — Sequence routes RAW, only InputDriven + MusicBed announce (new decision — confirm).** Sequence changes are **stage choreography, not player-behavior beats**: they must not defer-to-tone or pull a synchresis flourish. The Director is even **enabled during Tutorial** (re-enabled after Opening via `WwiseVOManager`/`Sequencer`), where Sequence pins C — routing that through the announce path would flourish on tutorial entry. So **Sequence always applies raw (`ApplyMasterFundamentalRaw`, immediate, no Director)**; only **InputDriven** and **MusicBed** use the announce path (with disabled-bypass). Robin's "a source switch that moves the master is a Director beat" applies to InputDriven/MusicBed; Sequence is the deliberate exception. (Where Sequence pins happen with the Director disabled anyway — Opening/Savasana — raw and announce-bypass coincide, so this only changes the Tutorial-enabled case.)

**F9 — Flush + two-gate confirmed consistent.** InputDriven's **enqueue gate == write gate** (`active==InputDriven && !debug`), so behind the curtain it builds charge but adds nothing to the Director — normally **nothing to flush**. Flush-pending-`fundamentalChange`-on-every-source-switch remains as the correctness guard for any item in flight at the instant of the switch; **never restore** — on re-entry the ladder re-queues from live charge.

### Consolidated design — the `targetNextFundamental` slot (Robin 2026-06-05) ⭐ supersedes F5–F8 mechanism

Robin's reframing collapses the machinery into **one nullable field** and makes realized-effect *structural*. This is the favored 9c shape; F5–F8 above are kept as the reasoning trail but their **mechanisms are superseded** by this.

**The field.** `MusicSystem1` gets `private NoteName? targetNextFundamental` — "the master's next commit target." There is exactly **one** (there is one master); it is always "the active source's pending target." Per-source durable memory stays in `preferredFundamentalBySource`; this slot is the transient hand-off to the Director.

**Director consults the slot (the core change).** At the top of `ActivateQueue`, before iterating:
- if `targetNextFundamental.HasValue && targetNextFundamental.Value != fundamentalNoteName`: apply it (raw + normal side-effects), `countAudioEvents++`, then `targetNextFundamental = null`. (Applied *first*, matching the existing fundamentalChange-to-front prioritization.)
- The empty-queue **early-return must become slot-aware**: proceed if the slot is pending even when `queue.Count == 0` (else a slot-only immediate activation early-returns and is lost).

**Realized-effect is structural (supersedes F5).** A counted audio event is injected **only when the slot actually moves the master**. A deferred change that drifted back to the master commits nothing → with no other items, `countAudio==0 && countVisual==0` → **no flourish** (confirmed from `Director.cs:435-454`). No `Func<bool>` needed.

**Late-binding for free (F6).** The deferred change is "whatever the slot holds when the Director fires," read at fire time — inherently late-bound, no per-item closure.

**`directorStoredFundamental` is retired, not relocated (supersedes F7).** The single slot is also the short-test dedupe memo: `directorMatchTest` becomes `changeTarget != targetNextFundamental && changeTarget != fundamentalNoteName`. (So **9b's relocate/rename is dropped** if the slot lands.)

**Two trigger paths (replace today's long/short bodies):**
- **Immediate** (long/longish test): set `targetNextFundamental = changeTarget`; then **disabled-bypass**: if `director.disable` → `ApplyMasterFundamentalRaw(changeTarget)` (today's behavior when disabled); else `director.ActivateQueue()` (slot applied + counted → visual flourish pairs).
- **Deferred** (short test): set `targetNextFundamental = changeTarget`; **do not** activate. The next external beat (`GameValues:412` behavior / `ImitoneVoiceIntepreter:545` tone) picks up the slot. **No "BlankAction" placeholder needed** — the slot *is* the pending marker, single-pending and self-deduping (Robin's BlankAction works too; the slot-aware early-return makes it unnecessary).

**Source switch = flush + adopt + commit (F8 revised — uniform, no Sequence carve-out):**
1. Clear the previous source's in-flight Director items (`ClearQueueOfType("fundamentalChange")`) and `targetNextFundamental = null` (flush — F9).
2. `activeFundamentalSource = newSource`; adopt intent = `firstFundamental` (real) or `preferred[newSource]`; for InputDriven + real note, clean-slate `ResetFundamentalTimers()`.
3. `targetNextFundamental = intent`; then **disabled-bypass**: if `director.disable` → `ApplyMasterFundamentalRaw(intent)` (iff differs); else `director.ActivateQueue()`.
4. Benign switches self-protect: if `intent == master`, the slot isn't pending → nothing fires, no flourish. **So Sequence can use this same path** — a Tutorial C-pin only flourishes if the master wasn't already C (watch in playtest); Savasana's C-pin runs through the disabled-bypass. The F8 "Sequence-always-raw" carve-out is no longer needed.

**Re-enter InputDriven with `None` (warm handoff) — adopt without wiping (Robin 2026-06-05):** the reactivation commit moves master → `preferred[InputDriven]` and syncs, but **does NOT `ResetFundamentalTimers()`** (`None` = the *honor* path; `realNote` = the *clean-slate* path that wipes). This is essential so an in-progress **sub-long (short-level) build** behind the curtain survives and continues live. Because InputDriven is a **shadow tracker**, `preferred` already holds the result of any behind-the-curtain long/longish silent commit (charge already ~0 there), so those cases land **directly on the right note, ladder quiet** — no `master → stale → corrected` flash. A short-only build lands master on the last committed note and continues the build live (commits on the next beat). The `FUND-HANDOFF` log captures the handoff. Restore confirmed (sub-question (a) resolved).

**Behind-the-curtain thresholds → reactivation (per-threshold, Robin 2026-06-05):**

| Threshold crossed behind curtain | Immediate (inactive) | On reactivation (`None`, honor) |
|---|---|---|
| **Short** (`_queueFundamentalChangeThreshold`, deferred path) | **No-op** — needs the Director (silenced) + the slot belongs to the active source. `preferred` & master unchanged; **charge NOT reset** (keeps building) | Master adopts `preferred` (last *committed* note, unchanged); **charge preserved** → the short-built note is live and commits on the next beat / when it reaches long. Honored. |
| **Longish** (`…−5` + just-activated, immediate branch) | **Silent commit** — `preferred = changeTarget` + `ResetFundamentalTimers()`; no master/Director | Master adopts `preferred` = longish target **directly**; charge ~0 → ladder quiet. Clean. |
| **Long** (`_initiateImminentFundamentalChangeThreshold`, any frame) | **Silent commit** (same as longish) | Master adopts `preferred` = long target directly; ladder quiet. Clean. |

A sustained behind-the-curtain tone progresses **short (inert) → builds → long (silent commit + reset)** — so it ends as the Long row.

**Move out of InputDriven:** the flush in switch-step 1 clears any in-flight InputDriven item + nulls the slot (usually nothing, per the two-gate enqueue rule).

**Apply primitive still needed:** `ApplyMasterFundamentalRaw(note)` = today's `ApplyMasterFundamental` **without** the `ClearQueueOfType("fundamentalChange")` self-clear (the Director owns the queue during activation / the bypass doesn't need it). Keep `ResetFundamentalTimers` (charge reset on commit is current behavior).

**Commit semantics — two commit forms.** A commit always records **`preferred[activeSource] = note`** + `ResetFundamentalTimers()`, *unless* it's a **debug-override** apply (the override sits on top and must not overwrite the source's real preferred). Two forms:
- **Audible commit** (active source, Director enabled or via the raw-bypass): `ApplyMasterFundamentalRaw(note)` = master + Wwise + binaural + `ResetFundamentalTimers` + `preferred[activeSource]=note`. Covers the **long-test immediate**, the **deferred short-test fire**, and the **disabled raw-bypass** uniformly, so `preferred` is always the last actually-committed note and the warm-handoff restore is correct. Charge reset here is parity with legacy (legacy reset at the long-test apply and when the queued short-test action ran — at fire, not at queue).
- **Silent commit** (InputDriven shadow-tracker behind the curtain — Robin 2026-06-05): `preferred[InputDriven] = changeTarget` + `ResetFundamentalTimers()` **only** — no master/Wwise/binaural/Director. This is what a behind-the-curtain **long test** does so `preferred` tracks while inactive.

**Why not update `preferred` on the short-test *detection*:** a deferred intent may drift back or be flushed before firing — `preferred` should track what actually committed, not an uncommitted intent. (Behind the curtain the short test is skipped entirely; only the long test silent-commits.)

**Open sub-questions for Robin (when back):**
- (a) Re-entry restore vs ladder-only (warm-handoff intermediate note) — **resolved: keep the restore** (Robin 2026-06-05), with a B457 log so the handoff is verifiable (see *Verification logs*).
- (b) Sub-stage recut: land 9a minimal now (long-test bypass + 5s flourish), and do the **whole slot design as 9c with 4e/4g**? Or introduce the slot for InputDriven-only in 9a (cleaner, slightly bigger, low-risk since the slot defaults null)? — lean: slot in 9c, keep 9a minimal; drop 9b if 9c lands.
- (c) BlankAction vs slot-alone — **resolved: slot alone** (the slot-aware early-return makes the placeholder unnecessary; `FUND-SLOT` gives the debug visibility).

### Behind-the-curtain guard (Robin 2026-06-05 — design principle, soft)

A non-active source updating its `preferred` is **logic-only** and must **never move the master** (the slot is only set/committed for the active source). That part is already enforced by `ShouldWriteMaster`. The new guardrail: in the expected design **a change should always be accompanied by a change in source**, so a *behind-the-curtain* preferred update on **MusicBed or Sequence** (those are cue-/stage-driven and should arrive with their switch) almost certainly indicates a sequencing mistake → **B457 warning, warn-and-honor** (store the preferred, don't move the master, just flag).

- **InputDriven is the exception — it is a *shadow tracker* behind the curtain.** While inactive but in a tracking mode it runs its full ladder: charge accumulates, and a behind-the-curtain **long test** does a **silent commit** (`preferred[InputDriven] = changeTarget` + `ResetFundamentalTimers()`) — updating its own `preferred` **without** touching master / Wwise / binaural / Director. So `preferred` legitimately changes behind the curtain (that's the "tracking changes to honor on reactivation"); only the master + Director are silenced. On reactivation the master adopts `preferred` directly. Because this never routes through `SetFundamentalForSource`, InputDriven **never trips the warning**.
- **What the warning is actually for:** MusicBed/Sequence preferred updates go through `SetFundamentalForSource`; those sources are cue-/stage-driven and should always arrive *with* their source switch, so a non-active update there is the suspicious case.
- Pure rule: `FundamentalSourcePolicy.ShouldWarnBehindCurtainUpdate(source, activeSource)` = `source != activeSource && (source == MusicBed || source == Sequence)`. Wired in `SetFundamentalForSource`. Soft principle, **not** a hard rule (legit exceptions allowed; revisit if it false-positives).

### Verification logs (B457) — "tests with logs" (Robin 2026-06-05)

Where instantiating `MusicSystem1` in EditMode isn't practical, behavior is pinned by **structured B457 log lines** a scripted playtest (or log capture) can assert against — Robin's "lots of things here can be added to a test with logs." Emit one line per relevant event:

- **Master commit:** `B457 FUND-COMMIT src=<source> <from>→<to> path=<raw|director> flourish=<none|audio|visual>` — the single line that makes **single-source parity** and **director-off parity** verifiable (diff the sequence of lines against today's behavior; `path=raw` must appear when `director.disable`).
- **Slot set (deferred):** `B457 FUND-SLOT set=<note> (deferred, awaiting beat)` / **slot cleared on commit/flush**.
- **Silent commit (shadow tracker):** `B457 FUND-SHADOW InputDriven silent-commit preferred=<note> (master unchanged, behind curtain)` — confirms the behind-the-curtain long test updates `preferred` + resets charge without moving the master.
- **Warm handoff (InputDriven re-entry):** `B457 FUND-HANDOFF restored preferred=<X> master <Y>→<X>; ladder re-evaluating from charge` — so the restore-then-ladder path and any brief intermediate note are visible.
- **Behind-the-curtain warning:** `B457 FUND-CURTAIN WARNING <source> preferred=<note> updated while not active (no master move) — changes are expected to accompany a source switch`.

### Sub-stages (proposed — confirm scope)

- **9a (small, can land early):** fix the **long-test bug** + add the **5s flourish suppression**. The fix is **enqueue-then-activate when the Director is enabled, apply raw when `director.disable`** (F2 — naive enqueue-then-activate would drop the change in Freeplay-with-Director-off). Introduce the tiny `AnnounceFundamental(target, immediate)` seed (raw-when-disabled; else enqueue counted `fundamentalChange`, `ActivateQueue` iff immediate) and route long/longish (immediate) + short (deferred) through it. Add dedicated `timeSinceLastFlourish` + `FundamentalDirectorPolicy.ShouldAddFlourish` (F4). Pure Director/InputDriven, no new sources. Behavior-improving; pin the flourish-suppression predicate with an EditMode test (the Director's flourish *add* calls Wwise/light so it can't run headless — test the **decision**, not the side effect).
- **9b: likely DROPPED.** The consolidated slot design **retires** `directorStoredFundamental` rather than relocating it (the single `targetNextFundamental` slot subsumes its dedupe job). Only keep a 9b if 9c is deferred for a long time and a cosmetic relocate is wanted in the interim.
- **9c (rides with 4e/4g): the `targetNextFundamental` slot design** (see *Consolidated design* above). Director consults the slot in `ActivateQueue` (slot-aware early-return + apply-first + `countAudio++`); realized-effect is **structural** (inject a counted audio event only when `slot != master`); late-binding for free; **retire `directorStoredFundamental`**; immediate vs deferred trigger paths; **source switch = flush + adopt + commit** with **disabled-bypass** (uniform across sources — no Sequence carve-out); warm-handoff on InputDriven re-entry; add `ApplyMasterFundamentalRaw`.

### Test Runner tests (EditMode) — regression net for the goblin

Strategy: `MusicSystem1` can't be instantiated headless (Wwise deps), so we **extract every multi-step decision into a pure policy and unit-test it**, then **log-assert** the few genuinely stateful integrations. The single most valuable extraction is the **trigger ladder** — it turns the whole change-decision matrix (parity + shadow-tracker + behind-curtain→reactivation) into pure unit tests.

**A. Pure policies to extract + unit-test (the regression backbone)**

1. **`FundamentalTriggerPolicy.WhichTest(charge, highestCharge, retriggerReady, firstFrameActive, longThreshold, longishOffset≈5, shortThreshold)` → `{None | Short | Longish | Long}`** — pure threshold math, the **parity anchor**. Write it FIRST as a *characterization of the current ladder* (so 9a and 9c are both provably parity), then route production through it. Cases: below-short → None; `[short, long−5)` + firstFrame → Short; `[long−5, long)` + firstFrame → Longish; `≥ long` **any frame** → Long; `≥ long` precedence over Longish/Short; `!retriggerReady` → None; `charge < highestCharge` (not the leader) → None; not-firstFrame with sub-long charge → None (only Long fires off-activation). *(The lock/active gate is applied by the caller — `IsFundamentalLocked()` in 9a, the active-source/shadow routing in 9c — so `WhichTest` stays stable across both.)*
2. **`FundamentalTriggerPolicy.RouteTrigger(which, isActiveWriter, slotEqualsTarget, targetEqualsMaster)` → `{None | SilentCommit | ImmediateAudible | DeferredAudible}`** — the active/shadow/dedupe routing (the heart of the new model). Cases encode the *Behind-the-curtain thresholds* table: Long/Longish + `!isActiveWriter` → **SilentCommit**; Long/Longish + active → **ImmediateAudible**; Short + `!isActiveWriter` → **None** (skipped behind curtain); Short + active + (`targetEqualsMaster` || `slotEqualsTarget`) → **None** (dedupe); Short + active + neither → **DeferredAudible**; None → None.
3. **`FundamentalDirectorPolicy.FlourishDecision(countAudio, countVisual)` → `{None | AddAudio | AddVisual}`** — `0/0 → None` (the property the slot relies on), audio-only → AddVisual, visual-only → AddAudio, both → None. Pins the two-branch logic so no phantom flourish creeps back.
4. **`FundamentalDirectorPolicy.ShouldAddFlourish(timeSinceLastFlourish, window≈5s)`** — the 5s anti-clutter gate (F4); pins the constant.
5. **`FundamentalSourcePolicy.ShouldWarnBehindCurtainUpdate(source, activeSource)`** = non-active MusicBed/Sequence only; InputDriven never warns; active source never warns.
6. **`FundamentalSourcePolicy.ShouldCleanSlate(source, firstFundamental)`** = `source == InputDriven && firstFundamental != None` — pins **honor (`None`, preserve charge) vs clean-slate (`realNote`, wipe)**; the explicit "honor, don't wipe" rule.
7. *(inline, optionally pinned)* `UseRawBypass(directorDisabled)` — trivial branch; can ride a `FundamentalDirectorPolicy` test.
8. **Existing `FundamentalSourcePolicy`** (4d): `IsTrackingMode`, `ShouldWriteMaster`, `CanInputDrivenWriteMaster` — already green; keep.

9. **`FundamentalTriggerPolicy.Effects(disposition)` → `(writesPreferred, resetsCharge, writesMaster, setsSlot, touchesDirector)`** — the side-effect *contract* per disposition, so each case's state effects are pinned without instantiating `MusicSystem1`:
   - `None` → `(false, false, false, false, false)` — **the Case-A-immediate guard: charge is NOT reset.**
   - `SilentCommit` → `(true, **true**, false, false, false)` — preferred written + charge reset, **no master/slot/Director** (Cases B & C immediate).
   - `DeferredAudible` → `(false, false, false, true, false)` — slot set only; **preferred untouched** (the "don't update preferred on short detection" rule).
   - `ImmediateAudible` → `(—, —, —, true, true)` — sets slot + requests activation; the *commit* effects (preferred write + charge reset + master move) then follow the **audible-commit** contract downstream.

Together (1)+(2)+(9) make the **entire change decision and its per-case side-effect signature** a pure unit matrix, independent of Wwise and the Director.

**B. Behind-the-curtain case coverage (A/B/C — explicit per Robin 2026-06-05).** Each row is one assertion; "unit" = pure policy, "log" = `Verification logs` capture.

| Case | Phase | Assertion | How |
|---|---|---|---|
| **A Short** | immediate | `RouteTrigger(Short, isActiveWriter=false, …) == None` | unit |
| **A Short** | immediate | `Effects(None).resetsCharge == false` (charge **kept**, keeps building); no preferred/master/slot/Director | unit |
| **A Short** | reactivation `None` | `ShouldCleanSlate(InputDriven, None) == false` (charge **preserved**) | unit |
| **A Short** | reactivation `None` | master adopts `preferred` (audible iff ≠ departed master); the preserved short-build then commits live on the next beat | log (`FUND-HANDOFF`, then `FUND-COMMIT` when it re-triggers) |
| **B Longish** | immediate | `RouteTrigger(Longish, isActiveWriter=false, …) == SilentCommit` | unit |
| **B Longish** | immediate | `Effects(SilentCommit) == (writesPreferred, resetsCharge, **no** master/slot/Director)` | unit |
| **B Longish** | immediate | silent commit observed: `preferred` updated, master unchanged | log (`FUND-SHADOW`, no `FUND-COMMIT`) |
| **B Longish** | reactivation `None` | master adopts `preferred` directly; charge ~0 → ladder quiet (no follow-up commit) | log (`FUND-HANDOFF`, no trailing `FUND-COMMIT`) |
| **C Long** | immediate | `RouteTrigger(Long, isActiveWriter=false, …) == SilentCommit` (Long resolved by `WhichTest` **any frame**) | unit |
| **C Long** | immediate / reactivation | same effect + log assertions as Case B | unit + log |
| **progression** | — | sustained behind-curtain tone: `WhichTest` returns Short while sub-long, then Long once `charge ≥ longThreshold` (→ Case C) | unit |

(Active-source counterparts — Short→DeferredAudible, Long/Longish→ImmediateAudible — are the `RouteTrigger(isActiveWriter=true)` rows under section A, giving the single-source parity decisions.)

**C. Log-asserted integrations (stateful; can't unit-test — verify via the `Verification logs`)**

- **Single-source parity:** drive InputDriven-only (Freeplay/SoundWorld); the sequence of `FUND-COMMIT` lines must match today's master-fundamental sequence (the `WhichTest` characterization guarantees the *decisions*; the log confirms the *applied* result).
- **Director-off parity:** every change in a disabled-Director stage logs `path=raw flourish=none` and yields the same master change as today's direct `ChangeFundamental` (no enqueue, no flourish).
- **Shadow tracker:** behind-the-curtain Long → `FUND-SHADOW` with `preferred` updated and **master unchanged** (no `FUND-COMMIT`, no flourish); behind-curtain Short → neither line (inert).
- **Warm handoff:** `FUND-HANDOFF` on re-entry; charge **preserved** for a short-build (a subsequent `FUND-COMMIT` follows when it re-triggers); long/longish re-entry shows the handoff with no follow-up commit (ladder quiet).
- **Slot dedupe / single-pending:** a repeated short to the same note doesn't re-set the slot (no duplicate `FUND-SLOT`); `slot != master` is the only thing that commits.
- **Flush on switch:** switching source emits the flush and **no stale flourish** fires afterward.
- **5s flourish suppression:** rapid back-to-back commits → changes still apply (`FUND-COMMIT` each) but flourishes are gated to ~5s (no spam).
- **Savasana C-pin with Director disabled:** C is pinned via the raw-bypass; `path=raw`.

### Playtests

- Headphones + lights. Confirm: (1) an InputDriven change while a loop bed owns the master is **silent and unlit** (logic-only); (2) an audible change pairs a visual flourish exactly once; (3) rapid changes don't spam flourishes (5s); (4) Savasana C-pin still works with the Director disabled; (5) switch away from and back to InputDriven resumes correctly without a stale/late flourish.

### Must not break

- Savasana's C pin under a disabled Director.
- The existing "weak changes don't churn the queue" dedupe behavior.
- No re-introduction of the Director self-removal bug (see Cross-cutting risks).

**Commit(s):** `Block 7 (9a): long-test enqueue-then-activate (disabled-bypass) + 5s flourish suppression + FundamentalTriggerPolicy/FundamentalDirectorPolicy + tests.` · *(9b dropped — slot retires `directorStoredFundamental`)* · `Block 7 (9c): targetNextFundamental slot — Director-consulted commit, structural realized-effect, shadow-tracker silent commit, source-switch flush/adopt/commit, disabled-bypass, warm-handoff honor-not-wipe + policy tests.`

---

# Appendix H — Stage map

## Stage map

```mermaid
flowchart TD
  S0[Stage 0 Harness]
  S1[Stage 1 Director queue + shuffle + worlds]
  S2[Stage 2 Binaural gating]
  S3[Stage 3 Switch hygiene]
  S3b[Stage 3b Block 8 Mic envelope]
  S4[Stage 4 Fundamental split + cue listener 4a-4g; 4h retire FrozenFreeplay]
  S5[Stage 5 Lorna external Wwise embedding + e2e]
  S6[Stage 6 Pitch / 5ths / harmony]
  S7[Stage 7 Interactive fade / silent loops / Stop_Toning]
  S8[Stage 8 Lock C / 15:00]
  S9[Stage 9 Director ↔ fundamental unification: synchresis + timing goblin; rides with 4g]
  S0 --> S1 --> S2 --> S3 --> S3b --> S4 --> S5 --> S6 --> S7 --> S8
  S4 -.-> S9
```

*Plan: Blocks 4 / 5 / 7 / 8. No code until confirmed; each stage: Composer or Opus implement → required Opus regression pass → commit.*
