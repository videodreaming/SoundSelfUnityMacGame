# Session content pack definitions (ScriptableObject) — refactor plan

This document plans moving **per–content-pack** tuning and references into **Unity ScriptableObjects (SOs)** while keeping the **Hummingbird CSV string contract** in **code** as the single source of truth for what the launcher may send.

---

## Phase execution process

Use this workflow for **Phase 1–4**:

1. **Confirm** — Start a phase only after you explicitly agree (no silent phase jumps).
2. **Implement** — Execute that phase’s scoped code/doc changes.
3. **Report** — Summarize what changed, plus questions or concerns.
4. **Unity owner steps** — List every manual Editor/asset task for you explicitly (assign references, create assets, folders, …).
5. **Review pass** — Prompt you for review; after you confirm, walk the changes again with a **regression lens** (missed references, behavior drift, …).

---

## Goals

1. **Editor clarity**: One asset per content pack; open it to see game mode, **`SequenceDefinition`** launch target, timing, VO mapping, UI title/description.
2. **Contract in code**: Canonical `gameMode` and `contentPack` strings remain defined in C#; **Strategy B** wires **`HummingbirdContentPackDefinition`** assets via a **`HummingbirdContentPackRegistry`** **ScriptableObject** (serialized rows), same spirit as `SequenceRunner`’s existing `SequenceDefinition` references.
3. **Single SO type**: Only **one** ScriptableObject **type** for content packs (no separate “game mode SO”).
4. **Game mode on pack**: Each pack belongs to **exactly one** game mode in Hummingbird; the SO exposes **`GameMode`** as a **single enum value** (not a list).
5. **No behavior drift on day one**: First milestone can **mirror** current `CSVLoader` VO / `TimeLeftInitializations` behavior; then delete duplicated literals from `CSVLoader` once wired.
6. **Easy to extend**: Adding a pack or mode should follow a **short checklist** at the end of this doc.

---

## Conceptual model

| Layer | Responsibility |
|--------|----------------|
| **Code contract** | Exact strings Hummingbird sends for `gameMode` and `contentPack`; `NormalizeGameMode` / `NormalizeContentPack` (unchanged semantics). |
| **Content pack SO** | Unity-facing configuration for **that** pack (`HummingbirdContentPackDefinition`): which **single** `GameMode` it belongs to, **`SequenceDefinition`** used when this pack drives startup, timing, VO kind, UI copy, etc. |
| **Runtime lookup** | After CSV sets `gameMode` + `contentPack`, resolve the **`HummingbirdContentPackDefinition` SO** for that pack and apply VO/timing/sequence from it instead of huge `if/else` chains. |

**Game mode** still drives **session-level** behavior that is not pack-specific (e.g. Sonoflore first-time-user VO rules). That can stay in code or a tiny mode-only table until it proves painful.

---

## ScriptableObject shape (single type)

**Type name**: **`HummingbirdContentPackDefinition`** (chosen).

### Required / recommended fields

| Field | Purpose |
|--------|--------|
| **`gameMode`** | Enum: `Sonoflore`, `Activation`, `Adjunctive`, `Albums`. Must match Hummingbird for this pack. |
| **`contentPackKey`** | **Strategy A only**: string on the asset. **Strategy B** (chosen): canonical key lives **only in code** next to the SO reference in the registry—use a **custom drawer** or asset naming so the Inspector still shows which pack an asset is for. |
| **`title`** | (NEW) String to populate a title field in the UI |
| **`description`** | (NEW) String to populate a description field in the UI |
| **`postUnguidedSeconds`** | `float` or nullable / sentinel (e.g. `-1` = not set / stub). Replaces hard-coded `TimeLeftInitializations` values per pack. |
| **`voKind`** | Enum mapping to existing `WwiseVOManager.SetTo…` paths (Peace, Narrative, Surrender, Fireflies, Kindness, Metta, EsketamineAscending, …, **None** for stubs). |
| **`sequenceDefinition`** | On this **content pack SO**: the **`SequenceDefinition`** that should be **launched** when this pack is the resolved session pack (same asset `SequenceRunner` uses after CSV resolution—not a separate “game mode” asset). Required once the pack is fully wired; missing or wrong wiring should surface as **errors/warnings** from validation or runtime checks—not a separate “I am a stub” flag on the SO. |

### Optional later

- Display name / notes for designers (if different from `contentPackKey`). **This is now in the plan**
- Links to documentation or QA checklist.

### What stays out of the SO (unless you later decide otherwise)

- **Normalize** rules (still code).
- **Encrypt/decrypt** and CSV path layout (`session_params.csv`).
- **Global** “unknown game mode” handling.

---

## Contract + SO alignment (avoid drift)

**Chosen strategy: B — Code-only registry + references** (matches how `SequenceRunner` already assigns `SequenceDefinition` assets in the Inspector; **keys stay in code** so CSV ↔ registry checks stay obvious).

**B — Code-only registry + references** (chosen)  
- The registry is its own **`ScriptableObject`** asset (e.g. **`HummingbirdContentPackRegistry`** — name TBD): a **serialized list of rows** `(gameMode, contentPackKey, HummingbirdContentPackDefinition)` plus lookup helpers. **Decision:** registry **is not** embedded ad hoc on `CSVLoader`; it is a dedicated asset designers/engineers assign where needed (e.g. `CSVLoader` holds a reference to the registry asset).  
- No duplicate pack string on the pack SO unless useful for display—identify packs via **registry row + asset reference**.

**A — String on SO + validation** (not chosen)  
- SO contains `contentPackKey`.  
- Custom Editor **or** `OnValidate` **or** startup check: key must match an entry in a **static readonly** registry in code.

**Game mode consistency**:

- **Editor / asset validation**: each registry row’s **`SO.gameMode`** must match the **game mode** for that row’s `contentPackKey`.
- **Runtime (resolved session vs SO)** — **required behavior**: Compare using the **effective session** only — i.e. **`CSVLoader.gameMode`** and **`CSVLoader.contentPack`** **after** all normalization and **after** applying the dev **content-pack override** (if any). Resolve the SO from that effective `(gameMode, contentPack)`, then compare the **effective** `gameMode` string to the SO’s declared game mode (enum mapped to canonical string, e.g. `SessionGameMode.Sonoflore` → `"Sonoflore"`).  
  - **If they do not match**: **`Debug.LogError`**. **Do not** compare against raw on-disk CSV strings when an override has replaced the effective session — that would false-positive whenever devs impersonate a pack.

---

## Repository layout (Unity assets)

| Location | Purpose |
|----------|---------|
| **`Assets/Definitions/HummingbirdCalls/`** | **`HummingbirdContentPackDefinition`** assets — **one asset per Hummingbird content pack**, plus the **`HummingbirdContentPackRegistry`** (or chosen name) **registry** ScriptableObject that lists rows linking `(gameMode, contentPackKey)` → pack SO. |
| **`Assets/Definitions/Sequences/`** | Existing **`SequenceDefinition`** assets (moved from former `Assets/Scripts/Sequencing/Definitions/`). Referenced by SOs and by `SequenceRunner` where applicable. |

**Migration note:** Sequence definition `.asset` files live under **`Assets/Definitions/Sequences/`** (moved from former `Assets/Scripts/Sequencing/Definitions/`). Unity resolves references by **GUID** (in `.meta`), not by folder path—**scene and inspector references stay valid** when `.meta` files move with assets.

Spot-check: `MainGame.unity` still references these GUIDs correctly:

| Asset | GUID | Inspector field on `SequenceRunner` |
|-------|------|-------------------------------------|
| `SkillsTraining.asset` | `9a78b1de728019a4a9c291ba8c474436` | `sonofloreDefinition` |
| `Integration.asset` | `9b567c6347fbe724bad4f5b90cc05e7e` | `activationDefinition` |
| `ProtocolStacksCalibration.asset` | `18c54947d36ae6f498f9287453fecc31` | `protocolStacksCalibrationDefinition` |
| `ProtocolStacksInteractive.asset` | `011ef49db7b2eb9458d137e50527f632` | `protocolStacksInteractiveDefinition` |

Re-scan after any manual OS-level move **without** copying `.meta` files.

**`Assets/Definitions/HummingbirdCalls/`** — folder for **new** content-pack session SOs (may be empty until Phase 1 assets are created).

---

## Debug-only: dual overrides (`CSVLoader` + `SequenceRunner`)

**Status:** **Not implemented in code yet** — behavior below is the target; existing prototype code (if any) may still reference an older single-location override until refactor work lands.

Two separate debug-only mechanisms work together:

### 1. Content pack override — `CSVLoader`

| Item | Detail |
|------|--------|
| **File** | `Assets/Scripts/CSVUtility/HummingBirdCommunications/CSVLoader.cs` |
| **Inspector** | One **`SerializeField`** referencing a **`HummingbirdContentPackDefinition`** (field name TBD, e.g. *content pack override*). When assigned, session setup behaves **as if** Hummingbird’s CSV had resolved to **that** pack (VO, timing, pack-level semantics once those live on the SO — same code paths as real CSV resolution via the registry). |
| **Error when active** | When non-null and honored (**`UNITY_EDITOR`** only): **`Debug.LogError`** — must never apply in **players**. |

### 2. Definition override — `SequenceRunner`

| Item | Detail |
|------|--------|
| **File** | `Assets/Scripts/Sequencing/SequenceRunner.cs` |
| **Rename** | The development-only inspector slot currently used as **start sequence** (e.g. `startDefinition`) becomes **Definition Override** — still a **`SequenceDefinition`** reference that forces which sequence asset **starts** when used in dev. |
| **Error when active** | When non-null and honored (**`UNITY_EDITOR`** only): **`Debug.LogError`** — same intent as CSV-side override. |

### 3. Both overrides set

Apply in order:

1. **`CSVLoader`** content-pack override: establish session/pack behavior **as that `HummingbirdContentPackDefinition`** (including whatever **default starting sequence** the pack SO implies once wired).
2. **`SequenceRunner`** **Definition Override**: if **also** set, **replace only the starting `SequenceDefinition`** for this run — i.e. the sequence that actually starts — layered on top of step 1.

So: pack/session impersonation lives on **`CSVLoader`**; optional forced sequence asset lives on **`SequenceRunner`**. Behavior matches the earlier idea (“pretend CSV chose this pack”), but the pack SO field belongs on **`CSVLoader`**, not on **`SequenceRunner`**.

### Production / shipped players

Overrides must **not** run in shipped builds. **Decision:** gate with **`UNITY_EDITOR`** so Edit Mode / Play Mode in the Editor behave as designed; **standalone players** (including dev builds) should **not** honor these fields unless you explicitly add a separate policy later. Implementation: `#if UNITY_EDITOR` around override logic (and optionally strip or ignore serialized values in player — detail left to implementation).

---

## Registry (`ScriptableObject`)

**Decision:** the registry is its **own** asset type — e.g. **`HummingbirdContentPackRegistry`** — holding **every** allowed `(gameMode, contentPackKey)` → **`HummingbirdContentPackDefinition`** row.

Used for:

- **Editor validation**: rows consistent; each referenced SO’s **`gameMode`** matches its row; optional orphan SO sweep.
- **Runtime lookup**: resolve effective `(gameMode, contentPack)` → pack SO (via reference from **`CSVLoader`** or another bootstrap to this registry asset).
- **Optional** runtime assert after normalize: effective pair must appear in the registry — **`Debug.LogError`** if not.

This duplicates **intent** only at the level of “these pairs exist”; numbers and VO live on pack SOs.

### Consideration: separate “VO / Wwise” registry SO?

**Optional future:** a second ScriptableObject mapping **`ContentPackVoKind`** → Wwise/event wiring if designers need to edit routing without recompiling. **For this refactor:** keep **`voKind`** on each **`HummingbirdContentPackDefinition`** and **dispatch in code** (no fallback paths — see Open decisions). Revisit a dedicated VO registry SO only if editing burden proves painful.

---

## Refactor phases

*(Everything discussed for this refactor should land in one of the phases below — including registry asset, migration from interim types, dual overrides, and sequence-resolution rules.)*

### Phase 1 — Types, registry asset, pack assets, validation (no full runtime switch yet)

1. **Rename / replace** interim **`ContentPackSessionDefinition`** (if present) with **`HummingbirdContentPackDefinition`**; align **`SessionGameMode`**, **`ContentPackVoKind`** with `CSVLoader` / `WwiseVOManager` (exhaustive mapping planned in Phase 2).
2. Implement **`HummingbirdContentPackRegistry`** (`ScriptableObject`): serialized rows `(gameMode, contentPackKey)` → **`HummingbirdContentPackDefinition`** + editor validation (`OnValidate` / custom inspector): row/SO **`gameMode`** alignment, unknown pairs, orphans.
3. **Pack assets**: create/fill **`HummingbirdContentPackDefinition`** assets **incrementally** if needed — the system must stay **obvious** via **comments**, **tooltips**, and **Editor** workflow (checklist + registry rows) so finishing assets after design work is straightforward.
4. **Reference wiring**: e.g. **`CSVLoader`** gets a **`SerializeField`** reference to the **registry** asset (and document in code where to assign it).
5. Optional stub: **read-only** preview or validation-only path — **no requirement** to flip all runtime behavior until Phase 2.

### Phase 2 — Resolve pack SO at runtime (`CSVLoader` + effective session)

1. After effective `gameMode` / `contentPack` are known (normalize + **content-pack override** applied first when editor-gated), resolve **`HummingbirdContentPackDefinition`** via **registry** lookup.
2. **Drift check (effective session only)**: compare **effective** `gameMode` to resolved SO’s **`gameMode`**; **`Debug.LogError`** on mismatch (see **Contract + SO alignment**).
3. **`VOInitializations`** / **`TimeLeftInitializations`**: dispatch from resolved SO **`voKind`**, **`postUnguidedSeconds`**, etc.; preserve Sonoflore first-time-user rules in code; stubs remain **warnings** until complete.
4. **Implement full `ContentPackVoKind` → `WwiseVOManager` dispatch** in code (no fallback — every path covered).
5. Remove duplicated per-pack literals from **`CSVLoader`** once parity is verified.

### Phase 3 — `SequenceRunner` + starting definition + dual overrides + migration

1. **`SequenceRunner`** resolves the **starting `SequenceDefinition`** from the **resolved** **`HummingbirdContentPackDefinition.sequenceDefinition`** when present.
2. **Fallback when `sequenceDefinition` is null** on the SO: keep **existing** mode/pack branches (e.g. Adjunctive calibration, Sonoflore/Activation refs) until each pack asset is wired — **narrow** over time.
3. **Editor-only (`UNITY_EDITOR`)** — **`CSVLoader`**: **content pack override** (`HummingbirdContentPackDefinition`); **`SequenceRunner`**: rename dev **`startDefinition`** → **Definition Override**; **`Debug.LogError`** when either override is honored.
4. **Both overrides:** apply **CSVLoader** pack impersonation first; if **Definition Override** is also set, it **replaces only** the starting sequence for that run.
5. **Migration:** remove interim fields (**e.g.** `csvSessionOverride` on **`SequenceRunner`** if it exists); single pack impersonation path lives on **`CSVLoader`** only.

#### Sequence resolution (clarification — answers “what starts?”)

| Situation | What runs first (production logic) |
|-----------|--------------------------------------|
| **Definition Override** set (`UNITY_EDITOR` only) | Use that **`SequenceDefinition`** for startup (after pack session is established from CSV/override). |
| No Definition Override; pack SO has **`sequenceDefinition`** | Start that asset. |
| No Definition Override; pack SO **`sequenceDefinition`** is **null** | Use **Phase 3 fallback** (today’s hard-coded resolver paths) until the SO is filled in. |

### Phase 4 — Cleanup

1. Delete dead private fields (e.g. unused `layingDown` if still unused).
2. Consider moving **mode-only** rules to a tiny static helper if still readable.
3. Final pass: warnings/errors consistent; no editor-only override paths in **player** builds.

---

## Extension checklist

### New **content pack** (Hummingbird + Unity)

1. Add **`public const string`** (and update **`NormalizeContentPack`** branch if needed — same rules as today).
2. Add row to **`HummingbirdContentPackRegistry`** `(gameMode, packKey)` → SO reference.
3. Create **new SO asset** under **`Assets/Definitions/HummingbirdCalls/`**; set SO fields (game mode, VO, timing, `sequenceDefinition`, UI, etc.) and **link it in the code registry** for `(gameMode, packKey)`.
4. Run validation / fix orphans.

### New **game mode**

1. Add **`GameMode`** constant + **`NormalizeGameMode`** entry.
2. Add **`SessionGameMode`** enum value + any **mode-only** rules in code.
3. Add pack constants + SOs **for each pack** under that mode (often multiple assets).

---

## Risks / mitigations

| Risk | Mitigation |
|------|------------|
| SO string ≠ code constant | **N/A for strategy B**; use **registry** validation + orphan-SO sweeps |
| **Debug overrides** in shipped players | **`UNITY_EDITOR`** gate only; players ignore override fields |
| Merge conflicts on many `.asset` files | Prefer additive assets; avoid reshuffling large arrays |
| CSV effective `gameMode` ≠ SO `gameMode` for resolved pack | **`LogError`** at runtime (**effective session**); editor validation on registry + assets |

---

## Open decisions (fill in during implementation)

- Exact **namespace** and script **file paths** for `HummingbirdContentPackDefinition` and **`HummingbirdContentPackRegistry`** (asset folder **`Assets/Definitions/HummingbirdCalls/`** — see Repository layout).
- Whether **`voKind`** covers every `SetTo*` path or a subset with **fallback** to code for rare cases. **Decision: `voKind` should cover every path, no fallback.**
- Whether **Sonoflore first-time user** stays hard-coded or becomes a bool on SO (likely stays code for clarity). **Decision: keep it in code**
- **Migration:** Any interim types/fields (`ContentPackSessionDefinition`, `SequenceRunner` pack-only override, etc.) — **Phase 3**.
- **Optional:** dedicated **VO registry SO** — **not** in scope for v1 (see **Registry** → *Consideration*); revisit if needed.

---

## Success criteria

- Adding a pack is: **constant + registry SO row + one pack SO**, without editing long `if` ladders in `CSVLoader`.
- Play mode parity with **current** VO + timing for all non-stub packs.
- Stub packs still **warn** as they do today until flags cleared on SO.
