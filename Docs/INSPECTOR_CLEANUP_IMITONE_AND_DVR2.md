# Inspector cleanup — ImitoneVoiceIntepreter & DirectVoiceMonitoring

**Status: complete** (May 2026). Checklist work is finished; this file is the record of what was done.

**Components:** `ImitoneVoiceIntepreter` (+ MicIngest / AudioThread partials), `DirectVoiceMonitoring`, custom editors in `Assets/Scripts/Voice/Editor/`, dual-stage gray-out in `SequencerEditor`.

**Do not include:** Script / `UnityEngine.Object` references — those stay editable in the inspector.

---

## Summary

| Approach | Meaning |
|----------|---------|
| **Lock (gray)** | Field in `ReadOnlyPropertyNames`; visible but not editable in custom inspector; runtime code can still write it. |
| **Deserialize** | Plain private field; code default is source of truth; not shown in inspector. |
| **Awake override** | Force startup value each session while keeping field serialized where noted. |
| **Unlock** | Telemetry mirrors and debug log toggles left editable intentionally. |

---

## ImitoneVoiceIntepreter

**Frozen:** `gameOn`, `pitch_hz`, `note_st`, `toneActiveBiasTrueTimer`, `_dbThreshold`, noise-floor config, band-pass cutoffs, normalization + gain-riding config.

**Deserialized:** mic ingest / audio-thread config, filter enable toggles, `preferredDeviceName`, `exceptionFlag`.

**Other:** `_pitchDifference` deleted (unused). Timer/breath accumulators zeroed in `Awake()`. HPF/LPF under **`Band Pass Filter`** header. **`gameOn`**: serialized + frozen; **`Awake()` forces `true`**. **`_dbValue`**: serialized + unlocked; scene **`-80`**.

**Debug toggles (editable, scene may differ from code):** `debugAllowGameOnLogs`, `debugAllowNoiseFloorLogs`.

---

## DirectVoiceMonitoring

**Deserialized:** transport tuning, click mitigation, reliability counters, transition audit, `micMixerVolumeParameterName`, chant presence shaping, reliability telemetry numeric tuning.

**Frozen:** `monitoringVolume` (**`0.357`**), `monitoringAttenuated`, `monitoringAttenuationDb` (**`0` dB** — flag-only path, no extra cut), `micMixerInitializationVolumeDb`.

**Awake forces:** `monitoringStreamSource = Normalized`, `dynamicVolumeEnabled = true`, `monitoringEnabled = true`.

**Renamed:** Runtime Dynamic Volume Debug → **`Dynamic Volume Telemetry`** (`telemetry*` fields + `[FormerlySerializedAs]`).

**Debug toggles (editable):** `enableReliabilityLogs`, `logWindowWarnings`, `logHealthSummary`, `debugAllowMonitoringLogs`, `debugAllowMonitoringWarnings`.

**Editable tuning:** `gameOnRiseSpeed`, `gameOnFallSpeed`, `chargeRiseSpeed`, `chargeFallSpeed`.

---

## Sequencer

**Frozen:** `dualstageStage`, `dualstageSecondStageIsMusic`, `dualstageSecondStageIsSoundSelf`.

---

## Notes

- Orphan YAML keys from deserialized fields drop off the next time Unity saves the scene.
- Avoid saving `MainGame.unity` after Play Mode with dirty voice components unless intentional (runtime drift can bake into YAML).
