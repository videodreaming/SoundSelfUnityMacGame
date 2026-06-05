using NUnit.Framework;
using static FundamentalDirectorPolicy;

/// <summary>
/// Test Runner (EditMode) tests for Block 7 / Stage 9a — the pure <see cref="FundamentalDirectorPolicy"/>.
/// Pins the synchresis flourish accounting (<see cref="FundamentalDirectorPolicy.FlourishDecision"/>) so no
/// phantom flourish creeps back, and the 5s anti-clutter gate (<see cref="FundamentalDirectorPolicy.ShouldAddFlourish"/>).
/// The Director's flourish *add* calls Wwise/light so it can't run headless — we test the decision, not the
/// side effect. See Docs/BLOCKS_4_5_7_PLAN.md Appendix G.
/// </summary>
public class Block7FundamentalDirectorPolicyEditModeTests
{
    // ---- FlourishDecision ----

    [Test]
    public void FlourishDecision_AudioOnly_AddsVisual()
        => Assert.That(FlourishDecision(1, 0), Is.EqualTo(FlourishAdd.AddVisual));

    [Test]
    public void FlourishDecision_VisualOnly_AddsAudio()
        => Assert.That(FlourishDecision(0, 1), Is.EqualTo(FlourishAdd.AddAudio));

    [Test]
    public void FlourishDecision_Neither_AddsNone()
        // The property the 9c slot relies on: a change that realized nothing pairs no flourish.
        => Assert.That(FlourishDecision(0, 0), Is.EqualTo(FlourishAdd.None));

    [Test]
    public void FlourishDecision_Both_AddsNone()
        => Assert.That(FlourishDecision(2, 3), Is.EqualTo(FlourishAdd.None));

    // ---- ShouldAddFlourish (5s anti-clutter) ----

    [Test]
    public void ShouldAddFlourish_AtWindow_True()
        => Assert.That(ShouldAddFlourish(FlourishWindowSeconds), Is.True);

    [Test]
    public void ShouldAddFlourish_PastWindow_True()
        => Assert.That(ShouldAddFlourish(FlourishWindowSeconds + 1f), Is.True);

    [Test]
    public void ShouldAddFlourish_WithinWindow_False()
        => Assert.That(ShouldAddFlourish(FlourishWindowSeconds - 0.01f), Is.False);

    [Test]
    public void ShouldAddFlourish_JustActivated_False()
        => Assert.That(ShouldAddFlourish(0f), Is.False);
}
