/// <summary>
/// Pure, testable rules for the active-source fundamental model (Block 7 / Stage 4d).
///
/// The active-source model replaces the old priority lock stack (Debug > Content > Mode): exactly one
/// <see cref="FundamentalSource"/> is active at a time and drives the master fundamental. These rules are
/// the policy half; the state + apply path live in
/// <c>MusicSystem1.FundamentalAuthority.cs</c>. EditMode tests: <c>Block7FundamentalPolicyEditModeTests</c>.
/// </summary>
public static class FundamentalSourcePolicy
{
    /// <summary>
    /// The source that owns the master fundamental on scene load (Block 7 / 4e zone 1): the sequencer pins it.
    /// Single source of truth for both <c>MusicSystem1.activeFundamentalSource</c>'s initializer and the explicit
    /// startup declaration in <c>MusicSystem1.Start</c>; pinned by <c>Block7FundamentalPolicyEditModeTests</c>.
    /// </summary>
    public const FundamentalSource StartupSource = FundamentalSource.Sequence;

    /// <summary>
    /// Tracking modes are the modes in which <c>DynamicMusicSystem()</c> runs and the InputDriven
    /// (sung-pitch) loop accumulates charge / can drive the master. Today: InteractiveTutorial + Freeplay.
    /// Switching the active source to InputDriven outside these modes is honored but warned (B457).
    /// </summary>
    public static bool IsTrackingMode(MusicSystem1.MusicMode mode)
        => mode == MusicSystem1.MusicMode.InteractiveTutorial
        || mode == MusicSystem1.MusicMode.Freeplay;

    /// <summary>
    /// The fundamental source a soundscape hands ownership to (Block 7 / 4e zones 2+3): a SoundWorld is voice-tracked
    /// (InputDriven); a MusicLoop's bed owns the key (MusicBed). Used when entering Freeplay (soundscape-driven) and
    /// mirrors what <c>SetSoundWorld</c> / <c>SetMusicLoop</c> set directly. Pinned by Block7FundamentalPolicyEditModeTests.
    /// </summary>
    public static FundamentalSource SourceForInteractionType(MusicSystem1.InteractionType interactionType)
        => interactionType == MusicSystem1.InteractionType.MusicLoop
            ? FundamentalSource.MusicBed
            : FundamentalSource.InputDriven;

    /// <summary>
    /// The master-write rule: only the currently active source may write the master.
    /// </summary>
    public static bool ShouldWriteMaster(FundamentalSource sourceRequestingWrite, FundamentalSource activeSource)
        => sourceRequestingWrite == activeSource;

    /// <summary>
    /// InputDriven write gate: the sung-pitch loop may write the master only when InputDriven is the active source.
    /// </summary>
    public static bool CanInputDrivenWriteMaster(FundamentalSource activeSource)
        => ShouldWriteMaster(FundamentalSource.InputDriven, activeSource);
}
