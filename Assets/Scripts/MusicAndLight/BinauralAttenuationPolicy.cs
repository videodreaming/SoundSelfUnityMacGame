using UnityEngine;

/// <summary>
/// Pure math for mid-session binaural attenuation. The stage layer (<see cref="BinauralStagePolicy"/>) owns the
/// <i>base</i> bus volume; <see cref="MusicSystem1.SetMusicModeFlags"/> toggles attenuation on top of it. When
/// attenuated, the binaural bus output is reduced by <see cref="AttenuationFraction"/> (reproduces the old
/// "MusicLoopSilent → 50" as 70 × 0.7 ≈ 49, but now layered on whatever base the current stage set).
/// </summary>
public static class BinauralAttenuationPolicy
{
    /// <summary>Fraction the bus output is reduced by when attenuated (Robin: "reduce by 30%").</summary>
    public const float AttenuationFraction = 0.30f;

    public const float AttenuatedFactor = 1f - AttenuationFraction; // 0.70
    public const float UnattenuatedFactor = 1f;

    /// <summary>Multiplier applied to the base volume for the given attenuation state.</summary>
    public static float GetFactor(bool attenuated) => attenuated ? AttenuatedFactor : UnattenuatedFactor;

    /// <summary>Bus output = base stage volume scaled by the attenuation factor. Clamped to the 0–100 RTPC range.</summary>
    public static float Apply(float baseVolume, float attenuationFactor) =>
        Mathf.Clamp(baseVolume * attenuationFactor, 0f, 100f);

    /// <summary>Convenience overload for the boolean attenuation state.</summary>
    public static float Apply(float baseVolume, bool attenuated) => Apply(baseVolume, GetFactor(attenuated));
}
