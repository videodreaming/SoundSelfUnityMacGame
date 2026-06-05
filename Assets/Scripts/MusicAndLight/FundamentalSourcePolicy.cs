/// <summary>
/// Pure, testable rules for the active-source fundamental model (Block 7 / Stage 4d).
///
/// The active-source model replaces the old priority lock stack (Debug > Content > Mode): exactly one
/// <see cref="FundamentalSource"/> is active at a time and drives the master fundamental, with a separate
/// debug override on top. These rules are the policy half; the state + apply path live in
/// <c>MusicSystem1.FundamentalAuthority.cs</c>. EditMode tests: <c>Block7FundamentalPolicyEditModeTests</c>.
/// </summary>
public static class FundamentalSourcePolicy
{
    /// <summary>
    /// Tracking modes are the modes in which <c>DynamicMusicSystem()</c> runs and the InputDriven
    /// (sung-pitch) loop accumulates charge / can drive the master. Today: InteractiveTutorial + Freeplay.
    /// Switching the active source to InputDriven outside these modes is honored but warned (B457).
    /// </summary>
    public static bool IsTrackingMode(MusicSystem1.MusicMode mode)
        => mode == MusicSystem1.MusicMode.InteractiveTutorial
        || mode == MusicSystem1.MusicMode.Freeplay;

    /// <summary>
    /// The master-write rule: a debug override always wins (nothing else writes while it is held);
    /// otherwise only the currently active source may write the master.
    /// </summary>
    public static bool ShouldWriteMaster(FundamentalSource sourceRequestingWrite, FundamentalSource activeSource, bool hasDebugOverride)
        => !hasDebugOverride && sourceRequestingWrite == activeSource;

    /// <summary>
    /// InputDriven write gate: the sung-pitch loop may write the master only when InputDriven is the
    /// active source and no debug override is held. (In 4d the production gate is still the legacy
    /// <c>IsFundamentalLocked()</c>; this is the equivalent active-source form the gate flips to in 4e.)
    /// </summary>
    public static bool CanInputDrivenWriteMaster(FundamentalSource activeSource, bool hasDebugOverride)
        => ShouldWriteMaster(FundamentalSource.InputDriven, activeSource, hasDebugOverride);
}
