# Tutorial ↔ Opening music handoff refactor — plan

Status: **draft / not implemented**. This document captures the current behavior, what the user asked for, the proposed design, and a file-by-file change list. No production code is edited as part of this plan.

---

## 0. Goals

- Decouple `TutorialStageHandler` from the specific opening variant that ran before it.
- Make it elegant to add a **new tutorial variant**: the variant only needs to declare *which music it starts*, and *whether it should wait for opening music to end first*.
- Allow any `Opening_*` → any `Tutorial_*` → any `Playground_*` combination to be valid from a music-ownership perspective, not just the two combinations baked into the asset definitions today.
- Keep the handoff from Tutorial to Playground working as it does today: Playground enters and is allowed to fully own `MusicSystem1` state.

---

## 1. Current behavior — confirmation

This section confirms the user's description of the existing flow, with code citations.

### 1.1 Where each variant is used today

From the sequence asset files in `Assets/Definitions/Sequences/` (type ids match `StageType` and variant ids match `StageVariant` in `Assets/Scripts/Sequencing/SequenceTools.cs`):

- `Activation.asset` and `Sonoflore.asset` — `Opening_Activation` / `Opening_Sonoflore` → **`Tutorial_Long`** → `Playground_Standard`.
- `Dualstage_StageInteractive.asset` — `Opening_PS_Ascending` → **`Tutorial_Short`** → `Playground_Ascending`.

So there is currently a hard implicit pairing:

| Opening variant | Followed by | Followed by |
|---|---|---|
| `Opening_PS_Ascending` | `Tutorial_Short` | `Playground_Ascending` |
| `Opening_Activation` / `Opening_Sonoflore` | `Tutorial_Long` | `Playground_Standard` |

### 1.2 Tutorial_Long (after Activation / Sonoflore opening)

Confirmed against `Assets/Scripts/Sequencing/Handlers/TutorialStageHandler.cs`:

```88:95:Assets/Scripts/Sequencing/Handlers/TutorialStageHandler.cs
            if (variant == StageVariant.Tutorial_Long)
            {
                _sequencer.tutorial.SetTestVocalizationType("Hum");
                MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.InteractiveTutorial);
                _sequencer.tutorial.StartTutorial("Long");
                _sequencer.wwiseVOManager.SetTestRepairSwitch("A");
                variantWatchesWwiseVOCuesForCompletion = true;
            }
```

- The user's description matches: Long **assumes the opening music has already ended** by the time it enters, and starts the interactive music system immediately via `MusicMode.InteractiveTutorial` (which internally calls `StartInteractiveMusic()` — see `Assets/Scripts/MusicAndLight/MusicSystem1.cs` around the `case MusicMode.InteractiveTutorial:` block).
- "The opening music has ended" is *implicit* — there is no explicit synchronization. It relies on the Wwise `PREPARATION_OPENING_SEQUENCE_*` / `INTEGRATION_OPENING_SEQUENCE_*` containers being authored so that their musical bed has finished (or is fading out under `Stop_*_OPENING_SEQUENCE_*`) before `Cue_StartTutorial` / `Cue_StartInteractive` is posted to Unity.

### 1.3 Tutorial_Short (after Opening_PS_Ascending)

```96:102:Assets/Scripts/Sequencing/Handlers/TutorialStageHandler.cs
            else if (variant == StageVariant.Tutorial_Short)
            {
                _sequencer.tutorial.SetTestVocalizationType("Ahh");
                MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Silent);
                _sequencer.tutorial.StartTutorial("Short");
                variantWatchesWwiseVOCuesForCompletion = false;
            }
```

- The user's description matches. Short **does not start interactive music**: `MusicMode.Silent` calls `StopInteractiveMusic()` (which only affects the *Interactive Music System* events — it does **not** stop the separate `Play_ASCENDING_OPENING` event still emitting the opening's musical bed).
- The "music loop" replacement is not started by the tutorial itself today. It is started by the **opening handler** while the opening is still in its transition-out tail:

```226:259:Assets/Scripts/Sequencing/Handlers/OpeningStageHandler.cs
        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.StartInteractive
            || sequenceCommand == SequenceCommand.StartTutorial
            || sequenceCommand == SequenceCommand.FirstVocalizationStart
            || sequenceCommand == SequenceCommand.MusicTrackEnding
            || sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            ...
            if(sequenceCommand == SequenceCommand.MusicTrackEnding && _variantTransitionsToMusicLoop)
            {
                Debug.Log("OpeningStageHandler: Transitioning to Music Loop");
                TransitionToAlternativeMusic("MusicLoop");
            }
        }
```

```261:274:Assets/Scripts/Sequencing/Handlers/OpeningStageHandler.cs
        public void TransitionToAlternativeMusic(string alternativeMusicVariant)
        {
            if(alternativeMusicVariant == "MusicLoop")
            {
                _sequencer.StartPlayground(false, false, true, 30.0f, false, false);
                MusicSystem1.instance.SetSoundscape("ShiftingEarth");

            }
            else
            ...
        }
```

`_variantTransitionsToMusicLoop` is only set to `true` for `Opening_PS_Ascending`. The Wwise cue that lands here is `Cue_Music_Ending` (`SequenceCommand.MusicTrackEnding`) — see `Assets/Scripts/WwiseManagers/WwiseVOManager.cs`:

```289:292:Assets/Scripts/WwiseManagers/WwiseVOManager.cs
            case "Cue_Music_Ending":
                Debug.Log("WWise_VO_CUE: Cue_Music_Ending");
                VoTrySequencerCommand(cue, SequenceCommand.MusicTrackEnding, "HandleSequenceCommand(MusicTrackEnding)");
                break;
```

The reason this works during tutorial: `SequenceRunner.TryExecuteSequenceCommand` dispatches commands to **both** the current handler and the transitioning-out handler (`Assets/Scripts/Sequencing/SequenceRunner.cs`, around `bool handled = false;` in `TryExecuteSequenceCommand`), so even though `Tutorial_Short` is the active stage when `Cue_Music_Ending` fires, the still-tailing opening receives it too.

### 1.4 Tutorial → Playground handoff (confirmation that this part is already clean)

`Tutorial.LocalCleanup()` only does:

- `MusicSystem1.SetTutorialMonitoringOverride(false)` (restores monitoring-attenuation rules).
- `tutorial.StopTutorial()` (kills VO test coroutines; idempotent — fires `TutorialPassed` only if `inTutorial`).

It does **not** force any `MusicMode`. `PlaygroundStageHandler.Enter` is the one that sets `MusicMode.Freeplay`, enables the director, etc.:

```76:81:Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.Enable();
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
            MusicSystem1.instance.SetBreathworkCycle(false);
            MusicSystem1.instance.SetAllowThumpAlways(false);
            MusicSystem1.instance.SetAllowThumpWhenModeIsPlayful(true);
```

**Conclusion**: the user's intuition is correct — *any* tutorial variant already hands off cleanly to *any* playground variant, because the playground variant fully reconfigures `MusicSystem1` on entry. The thing that does **not** work today is the reverse: choosing tutorial variant independently of opening variant. That is what this refactor addresses.

### 1.5 Risks / fragilities in the current setup

1. **Opening still drives playback after it is `IsComplete`.** `OpeningStageHandler` reacts to `MusicTrackEnding` while in transition-out tail. If the tutorial took an independent action on the music in the meantime, ordering matters and is variant-coupled.
2. **No explicit "opening music is/isn't playing" state.** Tutorial cannot ask the sequence "is there opening music I shouldn't trample?" — it has to *know* by virtue of being `Tutorial_Short` vs `Tutorial_Long`.
3. **If `Cue_Music_Ending` never fires** (e.g. because Opening_PS_Ascending was force-skipped), `TransitionToAlternativeMusic("MusicLoop")` never runs. The opening's `Exit()` does call `StopOpeningSequence()`, but no replacement music is started in that path.
4. **Mixing Long with PS_Ascending** would immediately start `InteractiveTutorial` over the still-playing `Play_ASCENDING_OPENING`. Mixing Short with Activation/Sonoflore would leave Tutorial_Short waiting for a `Cue_Music_Ending` that may never come from `PREPARATION_*` / `INTEGRATION_*` containers.

---

## 2. Proposed design

The general idea: **make "opening music is/isn't playing" a first-class piece of sequencer state**, and let the tutorial handler express its desired next-music as a variant-local decision that triggers either immediately or on the opening's "music ended" signal.

### 2.1 New state on `Sequencer`

Add to `Assets/Scripts/Sequencing/Sequencer.cs`:

- A `public bool IsOpeningMusicPlaying { get; private set; }` flag (default `false`).
- A `public event Action OnOpeningMusicEnded` event (or equivalent — see "Event vs polling" below).
- Two internal setters used by `OpeningStageHandler`:
  - `internal void NotifyOpeningMusicStarted()` — sets the flag `true`, logs through `DbgLogSequencer`.
  - `internal void NotifyOpeningMusicEnded()` — sets the flag `false`, logs, and invokes `OnOpeningMusicEnded` once if it transitioned `true → false`.
- These should be called only by handlers we control. Marking them `internal` (same assembly) keeps them out of UI / random scene-script accidental use; `IsOpeningMusicPlaying` stays `public` for reads.

### 2.2 `OpeningStageHandler` responsibilities

All `Opening_*` variants set `IsOpeningMusicPlaying = true` on `Enter`. They differ only in *which signal* flips it back to `false`.

| Variant | Sets flag on `Enter`? | Flag flipped to `false` by… | Notes |
|---|---|---|---|
| `Opening_PS_Ascending` | Yes | `MusicTrackEnding` cue (`Cue_Music_Ending` from `Play_ASCENDING_OPENING`) | Has a real long musical tail that outlives the opening stage. |
| `Opening_Preparation` / `Opening_Sonoflore` | Yes | The same cue that triggers `MarkComplete` — i.e. `StartInteractive` **or** `StartTutorial` (handled together in `ExecuteSequenceCommand`) | Bed audio is part of the VO container and is treated as "done" at the same moment the opening hands off to tutorial. |
| `Opening_Activation` | Yes | Same as Preparation/Sonoflore — flip on `StartInteractive` / `StartTutorial`. | Same rationale. |
| `Exit()` / `OnSessionSkipFromUi` / `EndThisSequenceStage` | n/a | Always — force the flag to `false` (force-stop path). | Belt-and-braces; covers force-skip, jumps, or sequence restart. |

Concrete behavior changes:

- In `Enter(variant)`:
  - Call `_sequencer.NotifyOpeningMusicStarted()` for **every** valid variant (after the variant validity branch passes). Doing it unconditionally also future-proofs against a future variant that needs the flag.
- In `ExecuteSequenceCommand`:
  - On `StartInteractive` or `StartTutorial`: in addition to today's `MarkComplete()`, call `_sequencer.NotifyOpeningMusicEnded()` **before** `MarkComplete()`. This is the standard-variant "music has effectively ended" path.
  - On `MusicTrackEnding`: call `_sequencer.NotifyOpeningMusicEnded()`. **Stop calling `TransitionToAlternativeMusic("MusicLoop")` from here.** That responsibility moves to the tutorial. (PS_Ascending's flag was still `true` until this cue; standard variants already flipped it on the `StartInteractive`/`StartTutorial` cue, so this is a no-op for them — `NotifyOpeningMusicEnded` is idempotent.)
- In `BeginTransitionOut`:
  - No change. Do not touch the flag here. PS_Ascending intentionally keeps the flag `true` after `MarkComplete` so that Tutorial_Short, now entering, can subscribe to `OnOpeningMusicEnded`.
- In `LocalCleanup` / `Exit` / `OnSessionSkipFromUi`:
  - Call `_sequencer.NotifyOpeningMusicEnded()` (idempotent — only invokes the event on a real true→false transition). This satisfies the user's requirement: *"If it is forced to `exit()`, it should stop it as part of the exit behavior."*
  - `wwiseVOManager.StopOpeningSequence()` should also be called here to actually silence the Wwise side. (It already does the four `Stop_*_OPENING_SEQUENCE_*` posts including `Stop_ASCENDING_OPENING`.) Today `LocalCleanup` only calls `StopOpeningSequence()`; add the `NotifyOpeningMusicEnded()` alongside it. Recommended: wrap both in a small Sequencer helper — see §3.
- Remove the `_variantTransitionsToMusicLoop` field and `TransitionToAlternativeMusic` method from `OpeningStageHandler`. The "what music plays next" decision lives in the tutorial.

### 2.3 `TutorialStageHandler` responsibilities

Replace the variant-specific `MusicMode` calls with a two-step pattern:

1. On `Enter(variant)`:
   - Set up everything music-independent (UI, vocalization type, lights, `StartTutorial(...)`, `variantWatchesWwiseVOCuesForCompletion`, monitoring override, etc.) as today.
   - Read `_sequencer.IsOpeningMusicPlaying`:
     - If `false` (Long-style — or "Short after a no-music opening"): call `StartTutorialMusicForVariant(variant)` immediately.
     - If `true` (Short-style): **subscribe** to `_sequencer.OnOpeningMusicEnded`. The callback calls `StartTutorialMusicForVariant(variant)` once, then unsubscribes itself.
2. In `BeginTransitionOut`, `Exit`, and `OnSessionSkipFromUi`:
   - Always unsubscribe from `OnOpeningMusicEnded` (idempotent). This satisfies the user's "unless the stage is in the long tail" requirement — once we are transitioning toward Playground, a late `MusicTrackEnding` must not still cause us to mutate `MusicSystem1`.

`StartTutorialMusicForVariant(StageVariant variant)` is a single private switch in the tutorial handler that encapsulates the "post-opening-music" intent of each variant. Today:

```csharp
private void StartTutorialMusicForVariant(StageVariant variant)
{
    switch (variant)
    {
        case StageVariant.Tutorial_Long:
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.InteractiveTutorial);
            break;

        case StageVariant.Tutorial_Short:
            // Equivalent to today's OpeningStageHandler.TransitionToAlternativeMusic("MusicLoop").
            // Eventually we should give the tutorial its own clearly-named API for this
            // instead of leaning on Sequencer.StartPlayground from inside tutorial.
            _sequencer.StartPlayground(false, false, true, 30.0f, false, false);
            MusicSystem1.instance.SetSoundscape("ShiftingEarth");
            break;

        default:
            Debug.LogError("TutorialStageHandler: No music intent defined for variant " + variant + ".");
            break;
    }
}
```

Adding a new tutorial variant later (e.g. a "Tutorial_Album" that wants a `MusicLoopSilent` style behavior) is then a one-place change: add the variant id in `StageVariant`, add its `case` to this switch, and decide whether its opening pair leaves music playing or not. Nothing else has to move.

### 2.4 Event vs polling

Two acceptable implementations of the "opening music ended" signal:

- **A. Event** (recommended). `Sequencer.OnOpeningMusicEnded` C# `event`. Tutorial subscribes in `Enter`, unsubscribes in `BeginTransitionOut`/`Exit`/`OnSessionSkipFromUi`. No `Update` loops in handlers.
  - Pros: deterministic, zero per-frame cost, easy to reason about.
  - Cons: must be careful to unsubscribe in *every* exit path (rule applies regardless — handlers already follow this pattern with `LocalCleanup`).
- **B. Polling**. `Sequencer.IsOpeningMusicPlaying` only, no event. Tutorial would need its own per-frame check, which the handler currently does not have (it relies on `ExecuteSequenceCommand`). Would require either coroutine in the handler or moving the check into an existing `MonoBehaviour` (e.g. `Tutorial.cs`). Less clean.

Plan adopts **A**.

### 2.5 What flips the flag for the standard (Activation / Sonoflore / Preparation) variants

**Decision:** standard variants **do** set the flag on `Enter` and clear it on the cue that triggers `MarkComplete` (i.e. `StartInteractive` or `StartTutorial`). See §2.2 for the full table.

This is more symmetric than the "never set the flag" alternative and gives every opening variant an honest lifecycle. Functionally:

- For a Long tutorial after a standard opening: the cue arrives, `OpeningStageHandler` calls `NotifyOpeningMusicEnded()` → `MarkComplete()`. By the time `TutorialStageHandler.Enter` runs on the next runner tick, `IsOpeningMusicPlaying` is `false`, so Long starts `InteractiveTutorial` immediately — identical audible behavior to today.
- For a hypothetical "Short after a standard opening" pairing: same — tutorial would see `false` on entry and run its `StartTutorialMusicForVariant` body immediately (equivalent to the music-loop transition firing without any wait, since standard openings don't emit `Cue_Music_Ending`). That is the cleanest available behavior for that combination.

### 2.6 Defensive stage-enter cleanup (other handlers)

Several non-Opening / non-Tutorial stages today *implicitly* assume the opening sequence is no longer playing audio. To make that explicit and to harden against jumps / dev sequence variants that bypass the natural Opening → Tutorial path, the following handlers should defensively stop opening audio and clear the flag on `Enter`:

- `Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs`
- `Assets/Scripts/Sequencing/Handlers/SavasanaStageHandler.cs`
- `Assets/Scripts/Sequencing/Handlers/EndStageHandler.cs`
- `Assets/Scripts/Sequencing/Handlers/MusicPlaylistStageHandler.cs`
- `Assets/Scripts/Sequencing/Handlers/SetMenuStageHandler.cs`
- `Assets/Scripts/Sequencing/Handlers/LinearAudioStageHandler.cs`

(`Inquiry` is a stub and `Calibration` runs *before* any opening — neither needs this.)

Each of those `Enter()` methods already calls `_sequencer.StopCalibrationInteractiveMusicFromStageEnter()` for the parallel "stop calibration audio on entry" guarantee. Add a sibling call — recommended name `_sequencer.StopOpeningAudioFromStageEnter()` — which:

1. Calls `wwiseVOManager.StopOpeningSequence()` (posts the four `Stop_*_OPENING_SEQUENCE_*` events; idempotent and harmless when nothing is playing).
2. Calls `NotifyOpeningMusicEnded()` (idempotent; only fires the event on a real true→false transition, so a Tutorial still listening will get its `OnOpeningMusicEnded` callback — which is the correct behavior for a session that jumps directly from Opening to Playground without Tutorial actually running its music intent, even though that scenario is unusual).

In the normal Opening → Tutorial → Playground flow this is a redundant no-op (opening audio was already stopped during Opening's `LocalCleanup` and Tutorial unsubscribed in `BeginTransitionOut`). In jump / skip / dev-variant flows it guarantees opening audio doesn't leak into the new stage. Two failure modes this closes:

- A sequence asset that skips the Tutorial stage entirely (Opening → Playground) — opening audio that needed `MusicTrackEnding` to stop would otherwise keep playing.
- A debug jump to a later stage — the new stage now actively silences the opening Wwise events rather than relying on luck or `Exit()` cleanup from a possibly-not-yet-exited opening handler.

---

## 3. File-by-file change list

Production code — needs no scene/prefab edits, so per the workspace rule this is editable without saving Unity first (but the user should still be on a clean working state before patches land):

- `Assets/Scripts/Sequencing/Sequencer.cs`
  - Add `bool IsOpeningMusicPlaying` (public read, private set).
  - Add `event Action OnOpeningMusicEnded`.
  - Add `internal void NotifyOpeningMusicStarted()` and `internal void NotifyOpeningMusicEnded()` with debug logging via `DbgLogSequencer` (Q3 — log every transition through this gate when `debugAllowLogsSequencer` is on).
  - Add `public void StopOpeningAudioFromStageEnter()` helper that calls `wwiseVOManager?.StopOpeningSequence()` then `NotifyOpeningMusicEnded()`. Mirrors the existing `StopCalibrationInteractiveMusicFromStageEnter()` shape and lets defensive stage handlers express intent with one call.
  - On `Awake`/sequence restart paths, reset the flag to `false`.

- `Assets/Scripts/Sequencing/Handlers/OpeningStageHandler.cs`
  - In `Enter`: after the variant-validity check passes (and audio actually starts), call `_sequencer.NotifyOpeningMusicStarted()` for **all** variants.
  - In `ExecuteSequenceCommand` for `StartInteractive` or `StartTutorial`: call `_sequencer.NotifyOpeningMusicEnded()` **before** `MarkComplete()`. This flips the flag at the same instant the standard openings effectively hand off.
  - In `ExecuteSequenceCommand` for `MusicTrackEnding`: call `_sequencer.NotifyOpeningMusicEnded()`. **Remove** the `TransitionToAlternativeMusic("MusicLoop")` call from this code path. (Calling `NotifyOpeningMusicEnded()` here for a standard variant is a no-op since it was already cleared.)
  - In `LocalCleanup` (called by `Exit` and `OnSessionSkipFromUi`): call `_sequencer.NotifyOpeningMusicEnded()` after `StopOpeningSequence()` (idempotent). Force-stop path.
  - Remove `_variantTransitionsToMusicLoop` field and `TransitionToAlternativeMusic` method (now unused). `Grep` confirms no external callers.
  - Keep the existing `WatchesSequenceCommand` entry for `MusicTrackEnding` — we still react to it, just differently.

- `Assets/Scripts/Sequencing/Handlers/TutorialStageHandler.cs`
  - Replace the two inline `SetMusicModeTo(...)` calls in `Enter` with the conditional subscribe/immediate-start pattern from §2.3.
  - Add `StartTutorialMusicForVariant(StageVariant variant)` (Q2 — Tutorial_Short keeps calling `_sequencer.StartPlayground(...)` internally; that name is awkward from a tutorial caller, but it stays in this refactor).
  - Add a private `Action _onOpeningMusicEndedHandler` field plus a `bool _subscribedToOpeningMusicEnded` guard so unsubscribe is idempotent.
  - In `LocalCleanup`: unsubscribe from `_sequencer.OnOpeningMusicEnded` (idempotent).
  - Keep the rest of `Enter` (UI calls, monitoring override, `StartTutorial`, `gameOn = true`, lights, etc.) exactly as today.

- Defensive `Enter`-time cleanup in the following handlers (see §2.6). Each one adds a single call alongside the existing `StopCalibrationInteractiveMusicFromStageEnter()`:

  - `Assets/Scripts/Sequencing/Handlers/PlaygroundStageHandler.cs`
  - `Assets/Scripts/Sequencing/Handlers/SavasanaStageHandler.cs`
  - `Assets/Scripts/Sequencing/Handlers/EndStageHandler.cs`
  - `Assets/Scripts/Sequencing/Handlers/MusicPlaylistStageHandler.cs`
  - `Assets/Scripts/Sequencing/Handlers/SetMenuStageHandler.cs`
  - `Assets/Scripts/Sequencing/Handlers/LinearAudioStageHandler.cs`

  Pattern (per handler — single line in `Enter` after null-checks pass):

  ```csharp
  _sequencer.StopCalibrationInteractiveMusicFromStageEnter();
  _sequencer.StopOpeningAudioFromStageEnter();
  ```

No changes needed in:

- `Assets/Scripts/WwiseManagers/WwiseVOManager.cs` — the `Cue_Music_Ending → MusicTrackEnding` plumbing stays exactly the same.
- `Assets/Scripts/Sequencing/SequenceTools.cs` — no new `SequenceCommand`s required.
- `Assets/Scripts/Sequencing/SequenceRunner.cs` — dispatch logic unchanged.
- `Assets/Scripts/Sequencing/Tutorial.cs` — `Tutorial` MonoBehaviour does not need to know about opening music.
- `Assets/Scripts/MusicAndLight/MusicSystem1.cs` — no API changes.
- Any sequence definition assets (`Assets/Definitions/Sequences/*.asset`) — variant assignments stay the same.

---

## 4. Behavior matrix after the refactor

| Opening variant | Sets `IsOpeningMusicPlaying` on enter? | Flag goes `false` on… | Tutorial_Long after it | Tutorial_Short after it |
|---|---|---|---|---|
| `Opening_PS_Ascending` | **Yes** | `MusicTrackEnding` cue, or opening `Exit` (force) | Starts `InteractiveTutorial` **only after** `MusicTrackEnding` (i.e. plays over the natural music tail, not on top of it) | Starts `StartPlayground(... MusicLoop ... )` + `ShiftingEarth` **after** `MusicTrackEnding` — same audible result as today |
| `Opening_Preparation` / `Opening_Sonoflore` / `Opening_Activation` | **Yes** | The same `StartInteractive` / `StartTutorial` cue that drives `MarkComplete` (also any force-exit path) | Tutorial enters with flag already `false`; starts `InteractiveTutorial` immediately on `Enter` — identical audible behavior to today | Tutorial enters with flag already `false`; immediately runs the MusicLoop-style transition (this combination did not work cleanly before) |

So any opening × tutorial pairing becomes coherent. Playground variant choice is independent and already coherent today.

Defensive cleanup on the post-Opening stages (Playground, Savasana, End, MusicPlaylist, SetMenu, LinearAudio) means: if a sequence asset skips Tutorial entirely (or a debug jump bypasses Opening's normal exit), the entered stage still silences any leftover opening Wwise audio on `Enter`.

---

## 5. Edge cases to verify in implementation

1. **Skip / force-advance during opening's main phase.** `OnSessionSkipFromUi` runs `LocalCleanup` → `NotifyOpeningMusicEnded` → tutorial (if already entered) gets the event and starts its music. If tutorial has not entered yet, the flag is just `false` by the time it does, so it starts music immediately. Both paths are clean.
2. **`EndThisSequenceStage` on opening.** Same as above — runs through `MarkComplete` → handler exits → `NotifyOpeningMusicEnded` from `LocalCleanup`.
3. **Race: `MusicTrackEnding` arrives between opening `Enter` and tutorial `Enter`.** `IsOpeningMusicPlaying` is already `false` when tutorial enters; tutorial reads it and starts music immediately. Subscription is never installed, so no leak.
4. **Race: `MusicTrackEnding` arrives during tutorial's transition-out tail.** Tutorial has already unsubscribed in `BeginTransitionOut`. Late event hits no listeners. Playground music intent is preserved.
5. **`Cue_Music_Ending` never fires (Wwise authoring issue) for a PS_Ascending session.** Opening's `Exit` (or any `OnSessionSkipFromUi`/`EndThisSequenceStage`) eventually calls `NotifyOpeningMusicEnded`. If tutorial is still listening at that point, its music starts then; if tutorial has already moved on to transition-out, the event is no-op. Either way, the music is forcibly stopped via `StopOpeningSequence()`.
6. **Multiple subscribers** — only the tutorial handler is expected to subscribe. Sequencer code should be defensive (null-check on invoke) but does not need a list-management layer beyond what C# events already provide.

---

## 6. Decisions (resolved with user)

- **Q1 — resolved.** *All* opening variants set `IsOpeningMusicPlaying = true` on `Enter`. Standard variants (`Activation` / `Sonoflore` / `Preparation`) flip it back to `false` on the cue that triggers `MarkComplete` (`StartInteractive` or `StartTutorial`). `PS_Ascending` keeps the flag `true` until `MusicTrackEnding` (or a force-exit path). See §2.2.
- **Q2 — resolved.** Keep `Sequencer.StartPlayground(...)` as the music start for Tutorial_Short. (Rename / API extraction is a separate cleanup, not part of this refactor.)
- **Q3 — resolved.** Yes: `NotifyOpeningMusicStarted` / `NotifyOpeningMusicEnded` log every transition through `DbgLogSequencer` (so the existing `debugAllowLogsSequencer` toggle gates it).
- **Q4 — resolved.** Yes, multiple other stages implicitly assume opening music has stopped. We make that explicit by adding a defensive `Sequencer.StopOpeningAudioFromStageEnter()` call in `Enter` for: Playground, Savasana, End, MusicPlaylist, SetMenu, LinearAudio. See §2.6 and the file-by-file list in §3.

---

## 7. Out of scope (intentionally)

- Sequence definition `.asset` files — no variant ids change.
- Wwise authoring — no new cues required, no events renamed.
- Tutorial-to-Playground handoff — already clean, see §1.4.
- The `StandardSequencePlaygroundCoroutine` legacy `tutorial.StopTutorial()` near the playground tail — separate issue, tracked in `Docs/TUTORIAL_LONG_RESTORE_PLAN.md`.
- Merging `MusicMode.Silent` and `MusicMode.MusicLoopSilent` — tracked in `Assets/Scripts/MusicAndLight/MusicSystem1.cs` comment near the `MusicMode` enum.

---

## 8. Implementation order (suggested)

1. Land §3 changes in `Sequencer.cs` (state + event + notify methods + `StopOpeningAudioFromStageEnter` helper) with no callers yet. Compile.
2. Switch `OpeningStageHandler` to call the notifiers in all three sites (`Enter`, `ExecuteSequenceCommand` for `StartInteractive`/`StartTutorial`, `ExecuteSequenceCommand` for `MusicTrackEnding`, `LocalCleanup`) and remove `TransitionToAlternativeMusic` / `_variantTransitionsToMusicLoop`.
3. Switch `TutorialStageHandler` to the subscribe-or-immediate pattern with `StartTutorialMusicForVariant`.
4. Smoke test each pair listed in §4. Pay particular attention to PS_Ascending → Short (existing behavior should be audibly identical) and Activation/Sonoflore → Long (also identical).
5. Add the single-line defensive `_sequencer.StopOpeningAudioFromStageEnter()` call to the six stage handlers listed in §2.6 / §3. Smoke test a dev jump straight from Opening → Playground (or Opening → MusicPlaylist) to confirm opening audio gets silenced on stage enter.
6. Optional cleanup of any remaining dead references in a separate commit.

Each step is independently shippable and easy to revert.
