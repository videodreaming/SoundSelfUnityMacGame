using NUnit.Framework;
using SoundSelf.Sequence;

/// <summary>Test Runner (EditMode) tests for Block 3 — see PLAYTEST_NOTES_ORGANIZED.md Block 3.</summary>
public class Block3PolicyEditModeTests
{
    // ---- gameOn (GameOnPolicy) ----

    [Test]
    public void SilentMode_AssignsGameOnFalse()
    {
        Assert.That(GameOnPolicy.GetGameOnAssignmentForMusicMode(MusicSystem1.MusicMode.Silent), Is.False);
    }

    [Test]
    public void FreeplayMode_AssignsGameOnTrue()
    {
        Assert.That(GameOnPolicy.GetGameOnAssignmentForMusicMode(MusicSystem1.MusicMode.Freeplay), Is.True);
    }

    [Test]
    public void FrozenFreeplayMode_AssignsGameOnFalse()
    {
        Assert.That(GameOnPolicy.GetGameOnAssignmentForMusicMode(MusicSystem1.MusicMode.FrozenFreeplay), Is.False);
    }

    [Test]
    public void MusicLoopSilentMode_DoesNotAssignGameOn()
    {
        Assert.That(GameOnPolicy.GetGameOnAssignmentForMusicMode(MusicSystem1.MusicMode.MusicLoopSilent), Is.Null);
    }

    [Test]
    public void OpeningEnter_AlwaysSetsGameOnTrue()
    {
        Assert.That(GameOnPolicy.OpeningEnterSetsGameOnTrue, Is.True);
    }

    [TestCase("16:15", 16 * 60 + 15)]
    [TestCase("15:37", 15 * 60 + 37)]
    [TestCase("20:00", 20 * 60)]
    public void CountdownClock_ParsesToSecondsRemaining(string clock, float expectedSeconds)
    {
        Assert.That(GameOnPolicy.TryParseCountdownClock(clock, out float seconds), Is.True);
        Assert.That(seconds, Is.EqualTo(expectedSeconds).Within(0.001f));
    }

    [Test]
    public void DualStacksPlayground_AtLornaReportClocks_ExpectsGameOn()
    {
        Assert.That(GameOnPolicy.TryParseCountdownClock("16:15", out float at1615), Is.True);
        Assert.That(GameOnPolicy.TryParseCountdownClock("15:37", out float at1537), Is.True);
        Assert.That(GameOnPolicy.ExpectGameOnDuringDualStacksPlayground(at1615), Is.True);
        Assert.That(GameOnPolicy.ExpectGameOnDuringDualStacksPlayground(at1537), Is.True);
    }

    [Test]
    public void DualStacksPlayground_BeforeStep1_DoesNotExpectGameOn()
    {
        float beforeStep1 = GameOnPolicy.PlaygroundProtocolStacksStep1ThresholdSeconds + 60f;
        Assert.That(GameOnPolicy.ExpectGameOnDuringDualStacksPlayground(beforeStep1), Is.False);
    }

    [Test]
    public void DualStacksPlayground_AtStep1Threshold_ExpectsGameOn()
    {
        Assert.That(GameOnPolicy.ExpectGameOnDuringDualStacksPlayground(
            GameOnPolicy.PlaygroundProtocolStacksStep1ThresholdSeconds), Is.True);
    }

    [Test]
    public void SavasanaAscending_DelayedMicOff_Is120Seconds()
    {
        Assert.That(GameOnPolicy.SavasanaAscendingDelayedMicOffSeconds, Is.EqualTo(120f));
    }

    // ---- normalization raise-freeze (MicNormalizationStagePolicy) ----

    [TestCase(StageType.Opening, true)]
    [TestCase(StageType.Savasana, true)]
    [TestCase(StageType.Tutorial, false)]
    [TestCase(StageType.Playground, false)]
    public void StageEnter_FreezeRaisesOnOpeningAndSavasanaOnly(StageType stage, bool expectFrozen)
    {
        Assert.That(MicNormalizationStagePolicy.ShouldFreezeGainRidingRaisesOnEnter(stage), Is.EqualTo(expectFrozen));
    }

    [TestCase(StageType.Calibration, false)]
    [TestCase(StageType.SetMenu, false)]
    public void StageEnter_DoesNotConfigureRaiseFreezeOnOtherStages(StageType stage, bool _)
    {
        Assert.That(MicNormalizationStagePolicy.StageEnterSetsRaiseFrozen(stage), Is.False);
    }

    [Test]
    public void GainRidingRaise_WhenFrozen_BlockedEvenIfToneGatesOpen()
    {
        Assert.That(MicNormalizationStagePolicy.AllowsGainRidingRaise(
            raiseFrozen: true,
            toneActiveConfident: true,
            raiseWindowOpen: true,
            raiseNoiseFloorClear: true), Is.False);
    }

    [Test]
    public void GainRidingRaise_WhenUnfrozenAndGatesOpen_Allowed()
    {
        Assert.That(MicNormalizationStagePolicy.AllowsGainRidingRaise(
            raiseFrozen: false,
            toneActiveConfident: true,
            raiseWindowOpen: true,
            raiseNoiseFloorClear: true), Is.True);
    }

    [Test]
    public void GainRidingRaise_WhenUnfrozenButGateClosed_Blocked()
    {
        Assert.That(MicNormalizationStagePolicy.AllowsGainRidingRaise(
            raiseFrozen: false,
            toneActiveConfident: false,
            raiseWindowOpen: true,
            raiseNoiseFloorClear: true), Is.False);
    }
}
