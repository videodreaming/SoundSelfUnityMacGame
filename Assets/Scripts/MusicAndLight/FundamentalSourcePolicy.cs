using ConversionUtilities;

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

    /// <summary>
    /// Block 7 / 9c Chunk 3 — the clean-slate-vs-honor rule for a <c>SetFundamentalSource</c> switch (drives whether the
    /// master-apply resets InputDriven's per-note charge). Only an InputDriven switch carrying a REAL seed note is a
    /// "clean slate" (wipe charge): the caller is explicitly (re)seeding the sung-pitch loop. The InputDriven adopt path
    /// (<paramref name="firstFundamental"/> == None) is the "honor, don't wipe" warm-handoff — it preserves any
    /// behind-the-curtain charge build so a sung pitch resumes live instead of snapping/resetting (the shadow-tracker
    /// bug fix). A non-InputDriven switch never owns the charge dict, so it is never a clean slate either.
    /// Pinned by <c>Block7FundamentalPolicyEditModeTests</c>.
    /// </summary>
    public static bool ShouldCleanSlate(FundamentalSource source, NoteName firstFundamental)
        => source == FundamentalSource.InputDriven && firstFundamental != NoteName.None;

    /// <summary>
    /// Block 7 / 9c Chunk 4 — how a <c>SetFundamentalSource</c> switch commits its adopted intent to the master. Only a
    /// switch that <i>moves</i> the master while the Director is enabled becomes a Director beat (pairs one flourish):
    /// <list type="bullet">
    /// <item><b>None</b> — the intent is <see cref="NoteName.None"/> (the source has no preferred yet): nothing to apply.</item>
    /// <item><b>Raw</b> — apply directly via <c>ApplyMasterFundamentalRaw</c> (no Director beat, no flourish). This covers
    /// (a) the Director-disabled cases (Opening/Savasana/Playground-off — keeps Savasana's C-pin working) and (b) a
    /// <i>benign</i> switch where the intent already equals the master: it re-posts the same note (parity with the legacy
    /// direct apply / startup Wwise+binaural re-init) but, since the master doesn't move, produces no flourish.</item>
    /// <item><b>Director</b> — the master actually moves and the Director is enabled: route through the slot + an activation
    /// so the switch is a counted audio beat that pairs exactly one visual flourish (a source switch that moves the master
    /// is a Director beat — uniform across sources, no Sequence carve-out).</item>
    /// </list>
    /// Pinned by <c>Block7FundamentalPolicyEditModeTests</c>.
    /// </summary>
    public static FundamentalSwitchCommit SwitchCommitDisposition(NoteName intent, NoteName master, bool directorDisabled)
    {
        if (intent == NoteName.None)
        {
            return FundamentalSwitchCommit.None;
        }
        if (intent == master || directorDisabled)
        {
            return FundamentalSwitchCommit.Raw;
        }
        return FundamentalSwitchCommit.Director;
    }
}

/// <summary>How a source switch commits its adopted intent to the master (Block 7 / 9c Chunk 4).</summary>
public enum FundamentalSwitchCommit
{
    /// <summary>Nothing to apply — the adopted intent is None (the source has no preferred yet).</summary>
    None,
    /// <summary>Apply raw (no Director beat / flourish): Director disabled, or a benign same-note re-post.</summary>
    Raw,
    /// <summary>Master moves with the Director enabled — route through the slot + activation so the switch pairs a flourish.</summary>
    Director,
}
