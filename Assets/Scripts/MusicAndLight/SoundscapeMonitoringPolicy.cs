using System.Collections.Generic;

/// <summary>
/// Block 8 / Stage 3b.0: per-soundscape headroom on the MicMixer voice bus (not per-frame monitoringSource scaling).
/// Each soundscape (sound world or music loop) carries its own dB addition; unknown / null / silence
/// (e.g. MusicLoopSilent during Savasana / Linear) → 0.
/// Per-soundscape dB is baked here (playtest-tuned 2026-06-04). DirectVoiceMonitoring.ApplySoundscapeMonitoring warns once
/// per soundscape if one is set with no entry, so newly added soundscapes can't silently default to 0.
/// </summary>
public static class SoundscapeMonitoringPolicy
{
    public const string ContributionName = "SoundscapeMonitoring";

    public const float DefaultSoundWorldDb = 0f;

    /// <summary>Default dB for music loops (sound worlds use <see cref="DefaultSoundWorldDb"/>). Playtest-tuned: +3 dB.</summary>
    public const float DefaultMusicLoopDb = 3f;

    public const float DefaultLerpDurationSeconds = 10f;

    static readonly Dictionary<string, float> BakedMonitoringDbBySoundscape = new Dictionary<string, float>
    {
        { "SonoFlore", DefaultSoundWorldDb },
        { "Shadow", DefaultSoundWorldDb },
        { "Gentle", DefaultSoundWorldDb },
        { "Shruti", DefaultSoundWorldDb },
        { "ShiftingEarth", DefaultMusicLoopDb },
        { "SitarAmbience", DefaultMusicLoopDb },
        { "PinkNoiseAtmosphere", DefaultMusicLoopDb },
    };

    public static IReadOnlyDictionary<string, float> BakedMonitoringDbMap => BakedMonitoringDbBySoundscape;

    /// <summary>dB added to the voice-bus MicMixer for the active soundscape (unknown / null / silence → 0).</summary>
    public static float MonitoringDbForSoundscape(string soundscape, IReadOnlyDictionary<string, float> soundscapeDbMap = null)
    {
        if (string.IsNullOrEmpty(soundscape))
        {
            return 0f;
        }

        var map = soundscapeDbMap ?? BakedMonitoringDbBySoundscape;
        return map.TryGetValue(soundscape, out float db) ? db : 0f;
    }

    public static bool IsKnownSoundscape(string soundscape)
    {
        return !string.IsNullOrEmpty(soundscape) && BakedMonitoringDbBySoundscape.ContainsKey(soundscape);
    }
}
