using SoundSelf.Sequence;
using UnityEngine;

/// <summary>
/// Block 5 — single authority for binaural BASE bus volume, keyed by <see cref="StageType"/>. Applied once per stage
/// in <see cref="SoundSelf.Sequence.SequenceRunner.AdvanceToStage"/>. Mode no longer sets binaural volume; it only
/// toggles attenuation on top of this base (see <see cref="MusicSystem1.SetMusicModeFlags"/> / <see cref="BinauralAttenuationPolicy"/>).
/// </summary>
public static class BinauralStagePolicy
{
    /// <summary>Audible base for the interactive meditation stages (Tutorial / Playground).</summary>
    public const float AudibleVolume = 100f;

    public const float MutedVolume = 0f;

    /// <summary>Default fade applied at stage entry (avoids abrupt binaural on/off at stage boundaries). Single source
    /// of truth lives on <see cref="MusicBinauralBeats.DefaultLerpDurationSeconds"/> so base + attenuation fades match.</summary>
    public const float DefaultLerpDurationSeconds = MusicBinauralBeats.DefaultLerpDurationSeconds;

    public static bool ShouldBinauralBeAudible(StageType stageType) =>
        stageType == StageType.Tutorial || stageType == StageType.Playground;

    public static float GetTargetVolume(StageType stageType) =>
        ShouldBinauralBeAudible(stageType) ? AudibleVolume : MutedVolume;

    /// <summary>Sets binaural for this stage: Play + volume when audible; volume fade then Stop when muted. Called from <see cref="SoundSelf.Sequence.SequenceRunner.AdvanceToStage"/>.</summary>
    public static void ApplyBinauralVolumeForStage(StageType stageType, float lerpDurationSeconds = DefaultLerpDurationSeconds)
    {
        var beats = MusicBinauralBeats.instance;
        if (beats == null)
        {
            Debug.LogWarning("BinauralStagePolicy: MusicBinauralBeats.instance is null — cannot apply volume for " + stageType + ".");
            return;
        }

        float target = GetTargetVolume(stageType);
        beats.ApplyStageTargetVolume(target, lerpDurationSeconds);
    }
}
