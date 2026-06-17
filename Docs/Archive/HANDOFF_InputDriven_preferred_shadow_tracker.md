# Handoff — InputDriven `preferred` is never shadow-tracked (the master-fundamental staleness bug)

**Written:** 2026-06-05 · **Branch:** `WorkingWwise` · **Author context:** Block 7 / Stage 4 "active-source fundamental authority" work.

> **This document is self-contained.** It does **not** depend on `Docs/BLOCKS_4_5_7_PLAN.md` — that plan doc is **being rebuilt right now in a parallel chat**, so treat it as temporarily unreliable and **do not edit it**. Everything you need is inline below.

---

## 0. Coordination & constraints (read first)

- **Two chats are live in the same working tree.** This chat is rebuilding `Docs/BLOCKS_4_5_7_PLAN.md` (docs only). You will be editing **C# code**. To avoid collisions: **you edit code; do not touch `BLOCKS_4_5_7_PLAN.md`.** This handoff is yours to update freely.
- **There is a large UNCOMMITTED change set already in the working tree** — the "Stage 4e" bundle (the active-source migration). Your fix will **stack on top of it**. The relevant already-modified files are:
  - `Assets/Scripts/MusicAndLight/MusicSystem1.cs`
  - `Assets/Scripts/MusicAndLight/MusicSystem1.FundamentalAuthority.cs`
  - `Assets/Scripts/MusicAndLight/MusicSystem1.InputDrivenFundamental.cs`
  - `Assets/Scripts/MusicAndLight/FundamentalSourcePolicy.cs`
  - `Assets/Scripts/Sequencing/Tutorial.cs`, `Assets/Scripts/Sequencing/Handlers/SavasanaStageHandler.cs`, `Assets/Scripts/Sequencing/Handlers/OpeningStageHandler.cs`, `Assets/Scripts/WwiseManagers/WwiseVOManager.cs`
  - `Assets/Editor/SoundSelf/Tests/EditMode/Block7FundamentalPolicyEditModeTests.cs`
  - **Decision needed (flag to Robin):** does this fix fold into the same 4e commit, or land as its own follow-up commit? It is conceptually part of the same active-source work.
- **NEVER touch `Assets/Scenes/MainGame.unity`** (another dev owns it; editing/committing risks conflicts). No `.prefab` edits without prompting Robin to save Unity.
- **Do NOT commit** without Robin's explicit say-so. Propose message + files first.
- **PowerShell**: no `&&` chaining. Single-line `git commit -m` or `-F`.
- **EditMode (Test Runner) tests first**, then playtests. Run via Window → General → Test Runner → EditMode (Unity open) or `Tools/run-editmode-tests.ps1` (Unity closed).
- **Console filter tag is `B457`** — any log you want Robin to read during a playtest must contain `B457`.
- **Line numbers below are approximate** (uncommitted working tree); search by symbol name to be safe.

---

## 1. TL;DR — the bug

In the active-source fundamental model, the **InputDriven** source carries a `preferred` note that is supposed to **track the player's sung pitch even while InputDriven is not the active source** (a "shadow tracker"), so that when the master is later handed (back) to InputDriven, it resumes on the right note.

**That shadow-tracking was never implemented.** `preferred[InputDriven]` is written in only two places — at `Start` (seed) and on a tutorial correction-resume (seed = current master). The live voice-tracking loop drives the *master* but **never updates `preferred[InputDriven]`**. As a result, every code path that switches the active source **to InputDriven by "adopting its preferred"** snaps the master to a **stale** note (usually the startup `C`) and **wipes the per-note charge**, instead of continuing the pitch the user was actually singing.

This is a **real gap vs the agreed design**, not a tuning preference. Robin confirmed the shadow-tracker is intended.

---

## 2. Background — the active-source fundamental model (enough to work without the plan doc)

The "master fundamental" (`MusicSystem1.fundamentalNoteName`) is the one note the whole music system tunes to (Wwise `...FundamentalOnly` switch + binaural center frequency). Exactly **one source** owns/drives it at a time, with a **debug override** that can sit on top.

```csharp
// MusicSystem1.FundamentalAuthority.cs
public enum FundamentalSource
{
    InputDriven,  // sung-pitch voice tracking (the "ladder" in MusicSystem1.InputDrivenFundamental.cs)
    MusicBed,     // the music-loop bed's key (fed by Cue_Key_* cues — wired later, in 4g)
    Sequence,     // the sequencer/stage pins the note (e.g. Tutorial/Savasana pin C)
}
```

State (in `MusicSystem1.FundamentalAuthority.cs`):

```csharp
private FundamentalSource activeFundamentalSource = FundamentalSourcePolicy.StartupSource; // = Sequence
private readonly Dictionary<FundamentalSource, NoteName> preferredFundamentalBySource = new() {
    { FundamentalSource.InputDriven, NoteName.None },
    { FundamentalSource.MusicBed,    NoteName.None },
    { FundamentalSource.Sequence,    NoteName.None },
};
private NoteName? debugFundamentalOverride = null; // override on top of the active source
```

The public API (same file):
- `SetFundamentalSource(FundamentalSource source, NoteName firstFundamental = None)` — switch active source. `firstFundamental == None` ⇒ **adopt that source's existing `preferred`** (no charge reset); a real note ⇒ set that source's `preferred` to it (and for InputDriven, **`ResetFundamentalTimers()`** = "clean slate"). Writes the master (via `ApplyMasterFundamental`) iff no debug override and the chosen preferred ≠ None.
- `SetFundamentalForSource(FundamentalSource source, NoteName note)` — update a source's `preferred`; writes master only if it's the active source and no override.
- `SetDebugFundamentalOverride(NoteName? note)` — set/clear the override (clearing restores the active source's preferred).

The master write goes through one private mechanism:

```csharp
// MusicSystem1.cs
private void ApplyMasterFundamental(NoteName newFundamental)
{
    // ... None guard ...
    director.ClearQueueOfType("fundamentalChange");
    fundamentalNoteName = newFundamental;                              // <-- THE MASTER
    SetSwitchRestoreToningV3("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", ...);
    MusicBinauralBeats.instance?.ChangeCenterFrequency(NoteUtils.NoteToFrequencyA440(fundamentalNoteName));
    ResetFundamentalTimers();                                          // <-- WIPES per-note charge
    directorStoredFundamental = newFundamental;
}
// NOTE: ApplyMasterFundamental does NOT touch preferredFundamentalBySource at all.
```

The pure rules live in `FundamentalSourcePolicy`:

```csharp
public static bool IsTrackingMode(MusicSystem1.MusicMode mode)            // tracking modes = where DynamicMusicSystem runs
    => mode == InteractiveTutorial || mode == Freeplay;
public static bool ShouldWriteMaster(FundamentalSource requesting, FundamentalSource active, bool hasDebugOverride)
    => !hasDebugOverride && requesting == active;
public static bool CanInputDrivenWriteMaster(FundamentalSource active, bool hasDebugOverride)
    => ShouldWriteMaster(FundamentalSource.InputDriven, active, hasDebugOverride);
public static FundamentalSource SourceForInteractionType(MusicSystem1.InteractionType it) // soundscape → owner
    => it == MusicLoop ? MusicBed : InputDriven;
```

---

## 3. The InputDriven ladder (where the bug lives)

`FundamentalUpdate()` (in `MusicSystem1.InputDrivenFundamental.cs`) runs **only inside `DynamicMusicSystem()`**, which runs **only in tracking modes** (`InteractiveTutorial`, `Freeplay`). It accumulates per-note "charge" (`fundamentalChargeByNote`) as the user sings, and when a note charges enough, the **trigger ladder** fires a change.

**Charge accumulation is UN-gated** — it happens every frame the loop runs, regardless of which source is active:

```csharp
// FundamentalUpdate(): the increment + write-back are NOT gated by who is active
newChangeFundamentalTimer += Time.deltaTime * _newChangeMultiplier;
// ...
updates[key] = newChangeFundamentalTimer;
// later: fundamentalChargeByNote[update.Key] = update.Value;
```

The **trigger ladder** is where the active-source gate is applied today:

```csharp
// MusicSystem1.InputDrivenFundamental.cs  — TryApplyFundamentalChangeTriggers(...)
bool isHighestFundamentalTimer = newChangeFundamentalTimer >= highestFundamentalTimer;
bool retriggerTest = (fundamentalTimeSinceLastTrigger >= fundamentalRetriggerThreshold);

// >>> THE PROBLEM IS HERE: the write gate is baked straight into `test`,
//     which gates the ENTIRE ladder. So when InputDriven is NOT the active source,
//     `test` is false and the ladder does NOTHING — no commit to preferred, nothing. <<<
bool test = FundamentalSourcePolicy.CanInputDrivenWriteMaster(activeFundamentalSource, debugFundamentalOverride.HasValue)
            && retriggerTest && isHighestFundamentalTimer;

bool directorMatchTest = directorStoredFundamental != changeTarget;
bool highThresholdPass            = newChangeFundamentalTimer >= _initiateImminentFundamentalChangeThreshold;
bool highThresholdPass_variation  = newChangeFundamentalTimer >= (_initiateImminentFundamentalChangeThreshold - 5.0f);
bool lowThresholdPass             = newChangeFundamentalTimer >= _queueFundamentalChangeThreshold;

bool longTest    = test && highThresholdPass;                                   // immediate
bool longishTest = test && highThresholdPass_variation && firstFrameActive;     // immediate
bool shortTest   = test && lowThresholdPass && directorMatchTest && firstFrameActive; // queued via Director

if (longTest || longishTest) { ChangeFundamental(changeTarget); director.ActivateQueue(5.0f); }
else if (shortTest)          { /* enqueue Action_ChangeFundamental via director */ }
```

And the master-write gate (only reached from the ladder above + the director-queued action):

```csharp
// MusicSystem1.cs
public void ChangeFundamental(NoteName newFundamental)
{
    if (FundamentalSourcePolicy.CanInputDrivenWriteMaster(activeFundamentalSource, debugFundamentalOverride.HasValue))
        SetFundamentalDirect(newFundamental);   // -> ApplyMasterFundamental  (does NOT update preferred[InputDriven])
    else
        // "shouldn't happen" warning
}
```

**So the two facts that cause the bug:**
1. When InputDriven **is** active and the ladder commits a change, the master moves but **`preferred[InputDriven]` is not updated**.
2. When InputDriven is **not** active (but the loop is still running in a tracking mode), the ladder is fully short-circuited by `test == false`, so it does **not** do the intended "silent commit to `preferred`."

The only writers of `preferred[InputDriven]`:
```csharp
// MusicSystem1.Start():            preferredFundamentalBySource[InputDriven] = fundamentalNoteName;  // startup seed (~C)
// ResumeFundamentalAfterCorrectionPin(): SetFundamentalSource(InputDriven, fundamentalNoteName);    // seed = current master
```

---

## 4. Why it matters — the affected production flows

Two entry points switch the active source to InputDriven by **adopting `preferred` (no seed)** — these snap to the stale value **and** reset charge (because the adopt path calls `ApplyMasterFundamental(preferred)`, which calls `ResetFundamentalTimers()`):

```csharp
// MusicSystem1.cs — SetMusicModeTo(Freeplay) : entering the playground
SetFundamentalSource(FundamentalSourcePolicy.SourceForInteractionType(currentInteractionType)); // no firstFundamental → adopt

// MusicSystem1.cs — SetSoundWorld(...) : a SoundWorld is voice-tracked
SetFundamentalSource(FundamentalSource.InputDriven);                                             // no firstFundamental → adopt
```

Consequences:
- **Tutorial → Freeplay (playground entry):** if the user ends the tutorial singing E, the master can snap back to the last seed (often C) instead of continuing at E.
- **Freeplay world↔loop↔world shuffle:** leaving a SoundWorld (master driven to E by tracking) for a MusicLoop (MusicBed) and back to a SoundWorld re-adopts `preferred[InputDriven]` (stale C) → master jumps to C, charge wiped → "sticky" reset; the user must re-sing from scratch.

Legacy behavior (before this refactor) re-derived the resume note from the **accumulated charge** (`ResolveFundamentalOnUnlock`, now deleted), so it tended to follow what was actually being sung. The shadow-tracked `preferred` was meant to be the replacement; it's missing.

---

## 5. The agreed design (what `preferred[InputDriven]` is SUPPOSED to do)

These are the design rules (verbatim intent from the Block 7 design; reproduced here so you don't need the plan doc):

**Two gates, not one:**
- **Tracking gate** = `DynamicMusicSystem` is running (mode ∈ {`InteractiveTutorial`, `Freeplay`}). InputDriven runs its **full ladder** here **even when it is not the active source** — it is a **shadow tracker**: per-note charge accumulates, and when the **long test** passes behind the curtain it does a **silent commit** — `preferred[InputDriven] = changeTarget` + `ResetFundamentalTimers()` — but **does not** touch the master / Wwise / binaural / Director. The **short test is skipped behind the curtain** (it needs the Director, which the behind-curtain case must not touch). On reactivation, `preferred` already holds the behind-the-curtain result and the master adopts it directly.
- **Write gate** = active source == InputDriven **and** no debug override. Only then does the ladder push to the master.

**Commit semantics — two forms:**
- **Audible commit** (InputDriven active, master actually moves): apply master (Wwise + binaural) **and record `preferred[InputDriven] = note`** + `ResetFundamentalTimers()`. So `preferred` is always the last actually-committed note.
- **Silent commit** (InputDriven shadow tracker behind the curtain): `preferred[InputDriven] = changeTarget` + `ResetFundamentalTimers()` **only** — no master / Wwise / binaural / Director.

**Warm-handoff on re-entry to InputDriven (honor, don't wipe):** when switching back to InputDriven via the **adopt path (`firstFundamental == None`)**, the master adopts `preferred[InputDriven]` but must **NOT** `ResetFundamentalTimers()` — the `None` path is the *honor* path (preserve any in-progress charge build); only a **real note** is the *clean-slate* path that wipes. This lets a short-level build that was accumulating behind the curtain survive and continue live.

**Behind-the-curtain threshold behavior (per-threshold):**

| Threshold crossed behind curtain | Immediate effect (inactive) | On reactivation (adopt / `None`) |
|---|---|---|
| **Short** (`_queueFundamentalChangeThreshold`) | **No-op** (needs the Director, which is silenced behind the curtain). `preferred` & master unchanged; **charge NOT reset** (keeps building). | Master adopts `preferred` (last committed note); **charge preserved** → the short-build continues live and commits on the next beat. |
| **Longish** (`…−5` + just-activated) | **Silent commit**: `preferred = changeTarget` + `ResetFundamentalTimers()`; no master/Director. | Master adopts `preferred` directly; charge ~0 → ladder quiet. |
| **Long** (`_initiateImminentFundamentalChangeThreshold`, any frame) | **Silent commit** (same as longish). | Master adopts `preferred` directly; ladder quiet. |

**Behind-the-curtain Director rule:** InputDriven must **NOT** enqueue anything to the Director while behind the curtain. (The short-test path enqueues to the Director, so it stays gated by the write gate; only the long/longish silent-commit-to-`preferred` runs behind the curtain.)

---

## 6. The gap (implemented vs intended), precisely

| Aspect | Intended | Currently in code |
|---|---|---|
| `preferred[InputDriven]` while InputDriven active & committing | updated to each committed note (audible commit records preferred) | **not updated** |
| `preferred[InputDriven]` while in tracking mode but NOT active | shadow-tracked via long/longish **silent commit** | **never updated** (ladder fully gated off) |
| Short test behind the curtain | inert, but **charge keeps building** | inert (charge does keep building — this part is fine) |
| Re-entry via adopt (`None`) | **preserve** charge (honor, don't wipe) | **wipes** charge (adopt path calls `ApplyMasterFundamental` → `ResetFundamentalTimers`) |

---

## 7. Proposed fix shape (scope this with Robin first)

The minimal, design-faithful fix is to **split the single `test` gate** and add the two commit behaviors + the honor-not-wipe re-entry. Concretely:

1. **Split the gate** in `TryApplyFundamentalChangeTriggers`:
   - `ladderArmed = retriggerTest && isHighestFundamentalTimer` (no write gate).
   - `canWrite = FundamentalSourcePolicy.CanInputDrivenWriteMaster(activeFundamentalSource, debugFundamentalOverride.HasValue)`.
   - `longTest/longishTest` now use `ladderArmed` (not `test`).
   - On long/longish pass:
     - if `canWrite` → **audible commit** (today's `ChangeFundamental(changeTarget)` + `director.ActivateQueue`), **and ensure `preferred[InputDriven]` is set to the committed note**.
     - else (behind the curtain, still in a tracking mode) → **silent commit**: `preferredFundamentalBySource[InputDriven] = changeTarget; ResetFundamentalTimers();` — **no** master/Director. Emit `B457 FUND-SHADOW ...`.
   - `shortTest` stays **write-gated** (only enqueues to the Director when `canWrite`) — never behind the curtain.
2. **Audible commit must record `preferred`.** Do this in the **InputDriven path only** (e.g. in `ChangeFundamental` when InputDriven is the active writer, or right after the `ChangeFundamental` call in the ladder). **Do NOT** put it inside `ApplyMasterFundamental` — that method is shared by Sequence/MusicBed/Debug, and you must not stamp `preferred[InputDriven]` when another source commits. (If you want the general "commit records `preferred[activeSource]`" rule from the design, gate it on the active source and exclude the debug-override apply.)
3. **Honor-not-wipe on adopt re-entry.** The adopt path (`SetFundamentalSource(InputDriven, None)`) currently writes the master via `ApplyMasterFundamental`, which resets charge. Per the design, the `None` (honor) path must **preserve** charge. You'll need an apply that updates the master + Wwise + binaural **without** `ResetFundamentalTimers()` for this case (the design calls this `ApplyMasterFundamentalRaw` minus the reset), OR restructure so the adopt path doesn't reset. Be careful: `ApplyMasterFundamental` also does `ClearQueueOfType("fundamentalChange")` and sets `directorStoredFundamental` — preserve whatever is correct for a handoff.

### Sub-issues / gotchas to handle
- **`fundamentalChargeByNote` is private to `MusicSystem1`** but the ladder and authority are all `partial class MusicSystem1`, so you can touch it directly.
- **`ResetFundamentalTimers()`** also resets `fundamentalTimeSinceLastTrigger = 0`. Silent commit should mirror the audible commit's reset (the design says silent commit resets charge). For the **short-build preserve** case, you must NOT reset.
- **Director coupling:** keep the short-test Director enqueue gated by `canWrite`. Don't let behind-curtain activity reach the Director.
- **`directorStoredFundamental`** is an InputDriven dedupe memo. There's a larger redesign for it (the "`targetNextFundamental` slot") in the broader Stage 9 work — **don't** pull that in unless Robin wants it; just keep it coherent.

### Scope decision to put to Robin
- **(Minimal)** Just fix `preferred[InputDriven]` sync (audible + silent commit) + honor-not-wipe re-entry. This makes adopt-preferred entries resume correctly. Smallest change; no Director/synchresis changes.
- **(Full)** Implement the whole Stage 9 "`targetNextFundamental` slot" + Director realized-effect + flourish work at the same time. Bigger; overlaps synchresis/flourish concerns. Likely a separate, later effort. **Recommend: do the minimal preferred-sync fix now; leave the Director slot to Stage 9.**

---

## 8. Tests (EditMode-first; `MusicSystem1` can't be instantiated headless — too many Wwise deps)

Pin the **decisions** as pure policy + unit tests; verify the **stateful integration** with `B457` log lines in a playtest.

Recommended pure policy to extract (makes the whole routing testable):
- `FundamentalTriggerPolicy.RouteTrigger(which, isActiveWriter, ...) → { None | SilentCommit | ImmediateAudible | DeferredAudible }`
  - Long/Longish + `!isActiveWriter` → **SilentCommit**
  - Long/Longish + active → **ImmediateAudible**
  - Short + `!isActiveWriter` → **None** (skipped behind curtain)
  - Short + active (+ not deduped) → **DeferredAudible**
- `FundamentalSourcePolicy.ShouldCleanSlate(source, firstFundamental) = source == InputDriven && firstFundamental != None` — pins **honor (`None`, preserve charge) vs clean-slate (`realNote`, wipe)**.
- Keep the existing green `Block7FundamentalPolicyEditModeTests` (`IsTrackingMode`, `ShouldWriteMaster`, `CanInputDrivenWriteMaster`, `SourceForInteractionType`, `StartupSource`).

Suggested `B457` verification logs for the playtest:
- `B457 FUND-SHADOW InputDriven silent-commit preferred=<note> (master unchanged, behind curtain)`
- `B457 FUND-HANDOFF restored preferred=<X> master <Y>→<X>` (on adopt re-entry)
- `B457 FUND-COMMIT src=<source> <from>→<to>` (audible commit)

Playtest acceptance: enter Tutorial→Freeplay and shuffle world→loop→world while singing; confirm the master **continues on the sung pitch** rather than snapping to C, and that a behind-the-curtain build resumes correctly.

---

## 9. Quick file/symbol index

- `MusicSystem1.InputDrivenFundamental.cs` — `FundamentalUpdate`, `TryApplyFundamentalChangeTriggers` (**the gate to split**), `ResolveFundamentalChangeTarget`, `TryGetSustainedFundamentalShiftTarget`.
- `MusicSystem1.FundamentalAuthority.cs` — `FundamentalSource` enum, `preferredFundamentalBySource`, `activeFundamentalSource`, `debugFundamentalOverride`, `SetFundamentalSource`, `SetFundamentalForSource`, `SetDebugFundamentalOverride`, `ResumeFundamentalAfterCorrectionPin`.
- `MusicSystem1.cs` — `ApplyMasterFundamental` (private master write), `ChangeFundamental` (write gate), `SetFundamentalDirect` (public shim), `ResetFundamentalTimers`, `SetSoundWorld` / `SetMusicLoop` / `SetMusicModeTo` (the adopt-preferred call sites), `Start` (the `preferred` seed).
- `FundamentalSourcePolicy.cs` — pure rules (`IsTrackingMode`, `ShouldWriteMaster`, `CanInputDrivenWriteMaster`, `SourceForInteractionType`, `StartupSource`).
- `Assets/Editor/SoundSelf/Tests/EditMode/Block7FundamentalPolicyEditModeTests.cs` — existing policy tests; add `RouteTrigger` / `ShouldCleanSlate` here.

---

## 10. One-paragraph summary to paste into the other chat

> In the Block 7 active-source fundamental model, `preferred[InputDriven]` is supposed to be a "shadow tracker": while the InputDriven voice loop runs in a tracking mode (Tutorial/Freeplay) but isn't the active source, it should keep its `preferred` up to date via silent commits (long/longish test → `preferred = target` + reset charge, no master move), and when it *is* active and commits the master it should also record `preferred`. Neither happens today — the single `test` gate in `TryApplyFundamentalChangeTriggers` bakes in the write gate and short-circuits the whole ladder when InputDriven isn't active, and `ApplyMasterFundamental` never writes `preferred`. So `preferred[InputDriven]` only ever holds the startup seed (~C) or the last tutorial-correction seed, and the two adopt-preferred entry points (`SetMusicModeTo(Freeplay)` and `SetSoundWorld`) snap the master to that stale note and wipe charge — instead of continuing the sung pitch. Fix: split the gate so the ladder runs behind the curtain and does a silent commit to `preferred` (+ reset), make the audible commit record `preferred[InputDriven]`, and make the adopt re-entry (`SetFundamentalSource(InputDriven, None)`) preserve charge (honor, don't wipe). Don't touch `MainGame.unity`, don't edit `BLOCKS_4_5_7_PLAN.md` (being rebuilt elsewhere), and there's an uncommitted "4e" change set already in the tree that your fix stacks on.
