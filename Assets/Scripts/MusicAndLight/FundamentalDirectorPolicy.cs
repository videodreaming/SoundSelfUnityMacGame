/// <summary>
/// Pure, testable rules for the Director's synchresis flourish accounting (Block 7 / Stage 9).
///
/// The Director pairs every activation across modalities: if an activation produced audio but no visual it
/// adds a visual flourish, and vice-versa (<c>Director.ActivateQueue</c>). 9a pins that two-branch decision
/// in <see cref="FlourishDecision"/> so no phantom flourish can creep back, and adds a 5s anti-clutter gate
/// (<see cref="ShouldAddFlourish"/>) so rapid changes still propagate without flourish spam.
///
/// EditMode tests: <c>Block7FundamentalDirectorPolicyEditModeTests</c>.
/// </summary>
public static class FundamentalDirectorPolicy
{
    /// <summary>Default anti-clutter window for the flourish *add* (the existing transition-sound cooldown is separate).</summary>
    public const float FlourishWindowSeconds = 5.0f;

    public enum FlourishAdd
    {
        None,
        AddAudio,
        AddVisual,
    }

    /// <summary>
    /// Which (if any) flourish completes synchresis for an activation that realized
    /// <paramref name="countAudio"/> audio and <paramref name="countVisual"/> visual events:
    /// audio-only ⇒ add a visual; visual-only ⇒ add an audio; <c>0/0</c> or both ⇒ none.
    /// The <c>0/0 ⇒ None</c> case is the property the 9c slot relies on (a change that drifted back to the
    /// master realizes nothing ⇒ no flourish).
    /// </summary>
    public static FlourishAdd FlourishDecision(int countAudio, int countVisual)
    {
        if (countAudio == 0 && countVisual != 0)
            return FlourishAdd.AddAudio;
        if (countVisual == 0 && countAudio != 0)
            return FlourishAdd.AddVisual;
        return FlourishAdd.None;
    }

    /// <summary>
    /// The 5s anti-clutter gate: a flourish may be added only once <paramref name="timeSinceLastFlourish"/>
    /// has reached <paramref name="window"/>. Queued actions still execute every activation — only the
    /// flourish *add* is suppressed, so rapid changes propagate without flourish spam.
    /// </summary>
    public static bool ShouldAddFlourish(float timeSinceLastFlourish, float window = FlourishWindowSeconds)
        => timeSinceLastFlourish >= window;
}
