using SoundSelf.Sequence;
using UnityEngine;

/// <summary>Block 5 — which <see cref="StageType"/>s should have audible binaural (applied in stage handler <c>Enter()</c>).</summary>
public static class BinauralStagePolicy
{
    /// <summary>Matches tutorial / freeplay level in <see cref="MusicSystem1.SetMusicModeFlags"/>.</summary>
    public const float AudibleVolume = 70f;

    public const float MutedVolume = 0f;

    /// <summary>Default fade when handlers call <see cref="ApplyBinauralVolumeForStage"/> on Enter (avoids abrupt binaural on/off at stage boundaries).</summary>
    public const float DefaultLerpDurationSeconds = 30f;

    public static bool ShouldBinauralBeAudible(StageType stageType) =>
        stageType == StageType.Tutorial || stageType == StageType.Playground;

    public static float GetTargetVolume(StageType stageType) =>
        ShouldBinauralBeAudible(stageType) ? AudibleVolume : MutedVolume;

    /// <summary>Sets binaural bus volume for this stage. Call from handler <c>Enter()</c> (and deferred music start if needed).</summary>
    public static void ApplyBinauralVolumeForStage(StageType stageType, float lerpDurationSeconds = DefaultLerpDurationSeconds)
    {
        var beats = MusicBinauralBeats.instance;
        if (beats == null)
        {
            Debug.LogWarning("BinauralStagePolicy: MusicBinauralBeats.instance is null — cannot apply volume for " + stageType + ".");
            return;
        }

        float target = GetTargetVolume(stageType);
        beats.SetVolume(target, lerpDurationSeconds);
    }
}
