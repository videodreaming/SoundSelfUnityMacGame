using UnityEngine;

/// <summary>
/// Block 8 / Stage 3b.1: pure envelope math for the stacked microphone-monitoring ADSR.
/// Holds only the NON-TRIVIAL rules worth an isolated EditMode test (the rise blend + the decay runway) plus the
/// shared tuning constants. Trivial one-liner rules (release gate, attack edge, sum-clamp, BoardFader mapping) are
/// inlined in DirectVoiceMonitoring per our "lean policy" rule of thumb (see soundself-director-mode rule).
/// The per-instance state machine lives in DirectVoiceMonitoring (one instance per confident onset).
/// This replaces the legacy "chantPresence" path (a single <c>_chantLerpSlow</c> multiply), which felt sluggish.
/// </summary>
public static class MonitoringAdsrPolicy
{
    /// <summary>Peak each instance attacks toward (not full 1.0 — leaves headroom for stacking).</summary>
    public const float AttackTarget = 0.9f;

    /// <summary>Sustain level after decay (the S of ADSR). Inspector-tunable default on DirectVoiceMonitoring.</summary>
    public const float DefaultSustainLevel = 0.4f;

    /// <summary>Decay is never faster than this, even at low charge.</summary>
    public const float MinDecaySeconds = 3f;

    /// <summary>Decay runway at full <c>_chantCharge</c> (charge == 1). Decay time = max(MinDecaySeconds, charge01 * this).</summary>
    public const float ChargeRunwayAtFullChargeSeconds = 10f;

    /// <summary>Release ramp-to-0 duration. Chosen to feel like the slow chant-down — NOT a literal <c>_chantLerpSlow</c> follow.</summary>
    public const float ReleaseSeconds = 2.5f;

    /// <summary>BoardFader high anchor (ADSR sum == 1 → full level).</summary>
    public const float BoardFaderHighDb = 0f;

    /// <summary>BoardFader low anchor (ADSR sum == 0 floor). Inspector-tunable default on DirectVoiceMonitoring.</summary>
    public const float DefaultBoardFaderLowDb = -18f;

    /// <summary>
    /// Blended chant reference that shapes the attack curve.
    /// Playful (<paramref name="modeMeditativeLerp"/> = 0): snappy = <paramref name="chantLerpFast"/>.
    /// Meditative (= 1): 0.5 * (fast + slow). Blended by the smoothed lerp, never the raw mode bool.
    /// </summary>
    public static float RiseReference(float chantLerpFast, float chantLerpSlow, float modeMeditativeLerp)
    {
        float meditative = 0.5f * (chantLerpFast + chantLerpSlow);
        return Mathf.Lerp(chantLerpFast, meditative, Mathf.Clamp01(modeMeditativeLerp));
    }

    /// <summary>Decay duration from the charge runway ("time until charge hits 0"), floored at <see cref="MinDecaySeconds"/>.</summary>
    public static float DecayDurationSeconds(float chantCharge01)
    {
        float runway = Mathf.Clamp01(chantCharge01) * ChargeRunwayAtFullChargeSeconds;
        return Mathf.Max(MinDecaySeconds, runway);
    }

    // Inlined in DirectVoiceMonitoring (single-expression rules, per the "lean policy" rule of thumb):
    //   • Release trigger (option C):     !toneActiveConfident && !toneActiveBiasTrue   ("finger off the piano key")
    //   • Attack-spawn edge (no re-attack): armed && confidentNow && !confidentLastFrame
    //   • Stacking clamp:                 Mathf.Clamp01(summedLevels)
    //   • Presence mapping:               AudioLevelUtilities.BoardFader(Clamp01(sum), low, BoardFaderHighDb)
}
