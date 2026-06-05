using NUnit.Framework;

/// <summary>
/// Test Runner (EditMode) tests for Block 7 / Stage 4d — the pure <see cref="FundamentalSourcePolicy"/>.
/// Pins the active-source authority rules: which modes track, and who may write the master.
/// Full shim-equivalence / real-flow end-states ride the playtest (instantiating MusicSystem1 in
/// EditMode isn't practical — too many Wwise deps). See Docs/BLOCKS_4_5_7_PLAN.md §Stage 4 (4d).
/// </summary>
public class Block7FundamentalPolicyEditModeTests
{
    // ---- IsTrackingMode ----

    [TestCase(MusicSystem1.MusicMode.InteractiveTutorial, true)]
    [TestCase(MusicSystem1.MusicMode.Freeplay, true)]
    [TestCase(MusicSystem1.MusicMode.Silent, false)]
    [TestCase(MusicSystem1.MusicMode.FrozenFreeplay, false)]
    [TestCase(MusicSystem1.MusicMode.MusicLoopSilent, false)]
    [TestCase(MusicSystem1.MusicMode.Environment, false)]
    public void IsTrackingMode_TrueOnlyForTutorialAndFreeplay(MusicSystem1.MusicMode mode, bool expected)
    {
        Assert.That(FundamentalSourcePolicy.IsTrackingMode(mode), Is.EqualTo(expected));
    }

    // ---- ShouldWriteMaster (debug override beats all; otherwise only the active source writes) ----

    [Test]
    public void ShouldWriteMaster_ActiveSourceNoOverride_Writes()
    {
        Assert.That(FundamentalSourcePolicy.ShouldWriteMaster(
            FundamentalSource.Sequence, FundamentalSource.Sequence, hasDebugOverride: false), Is.True);
    }

    [Test]
    public void ShouldWriteMaster_NonActiveSource_DoesNotWrite()
    {
        Assert.That(FundamentalSourcePolicy.ShouldWriteMaster(
            FundamentalSource.MusicBed, FundamentalSource.Sequence, hasDebugOverride: false), Is.False);
    }

    [Test]
    public void ShouldWriteMaster_DebugOverrideHeld_BlocksEvenActiveSource()
    {
        Assert.That(FundamentalSourcePolicy.ShouldWriteMaster(
            FundamentalSource.Sequence, FundamentalSource.Sequence, hasDebugOverride: true), Is.False);
    }

    // ---- CanInputDrivenWriteMaster (the input write-gate, active-source form) ----

    [Test]
    public void CanInputDrivenWriteMaster_InputDrivenActiveNoOverride_True()
    {
        Assert.That(FundamentalSourcePolicy.CanInputDrivenWriteMaster(
            FundamentalSource.InputDriven, hasDebugOverride: false), Is.True);
    }

    [TestCase(FundamentalSource.Sequence)]
    [TestCase(FundamentalSource.MusicBed)]
    public void CanInputDrivenWriteMaster_OtherSourceActive_False(FundamentalSource active)
    {
        Assert.That(FundamentalSourcePolicy.CanInputDrivenWriteMaster(active, hasDebugOverride: false), Is.False);
    }

    [Test]
    public void CanInputDrivenWriteMaster_DebugOverrideHeld_False()
    {
        Assert.That(FundamentalSourcePolicy.CanInputDrivenWriteMaster(
            FundamentalSource.InputDriven, hasDebugOverride: true), Is.False);
    }
}
