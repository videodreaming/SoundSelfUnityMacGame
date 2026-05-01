using UnityEngine;

/// <summary>
/// Utility helpers for converting and shaping audio levels.
/// Keep dB/linear conversion logic centralized so gain behavior stays consistent.
/// </summary>
public static class AudioLevelUtilities
{
    /// <summary>
    /// Converts gain in dB to linear amplitude.
    /// </summary>
    public static float DbToLinear(float db)
    {
        return Mathf.Pow(10f, db / 20f);
    }

    /// <summary>
    /// Converts linear amplitude to dB, clamped by epsilon for safety.
    /// </summary>
    public static float LinearToDb(float linear, float epsilon = 0.000001f)
    {
        float safe = Mathf.Max(epsilon, linear);
        return 20f * Mathf.Log10(safe);
    }

    /// <summary>
    /// Maps a normalized control input [0..1] to a gain-linear scale:
    /// - Uses dB ramp from minGainDb to maxGainDb.
    /// - Applies an additional floor multiplier in [0..linearFloorRange] so output truly reaches 0.
    /// This avoids "hard cut-in" feel near onset while still using musical dB scaling.
    /// </summary>
    public static float BoardFader(
        float normalizedControl,
        float minGainDb = -35f,
        float maxGainDb = 0f,
        float linearFloorRange = 0.125f)
    {
        float x = Mathf.Clamp01(normalizedControl);
        float floorRange = Mathf.Clamp(linearFloorRange, 0.0001f, 1f);

        float dbT = Mathf.InverseLerp(floorRange, 1f, x);
        float gainDb = Mathf.Lerp(minGainDb, maxGainDb, dbT);
        float dbGainLinear = DbToLinear(gainDb);

        float floorMultiplier = Mathf.InverseLerp(0f, floorRange, x);
        return dbGainLinear * floorMultiplier;
    }
}
