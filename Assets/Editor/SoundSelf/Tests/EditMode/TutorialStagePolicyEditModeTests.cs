using NUnit.Framework;
using SoundSelf.Sequence;

/// <summary>Test Runner (EditMode) tests for Block 9 — see PLAYTEST_NOTES_ORGANIZED.md Block 9.</summary>
public class TutorialStagePolicyEditModeTests
{
    [Test]
    public void ResolveEffectiveVariant_SonofloreRepeatUser_MapsLongToShort()
    {
        var resolved = TutorialStagePolicy.ResolveEffectiveVariant(
            StageVariant.Tutorial_Long,
            CSVLoader.GameModeSonoflore,
            isFirstTimeUser: false);
        Assert.That(resolved, Is.EqualTo(StageVariant.Tutorial_Short));
    }

    [Test]
    public void ResolveEffectiveVariant_SonofloreFirstTime_KeepsLong()
    {
        var resolved = TutorialStagePolicy.ResolveEffectiveVariant(
            StageVariant.Tutorial_Long,
            CSVLoader.GameModeSonoflore,
            isFirstTimeUser: true);
        Assert.That(resolved, Is.EqualTo(StageVariant.Tutorial_Long));
    }

    [Test]
    public void ResolveEffectiveVariant_ActivationLong_Unchanged()
    {
        var resolved = TutorialStagePolicy.ResolveEffectiveVariant(
            StageVariant.Tutorial_Long,
            CSVLoader.GameModeActivation,
            isFirstTimeUser: false);
        Assert.That(resolved, Is.EqualTo(StageVariant.Tutorial_Long));
    }

    [TestCase(0, "Hum")]
    [TestCase(4, "Hum")]
    [TestCase(5, "Ahh")]
    [TestCase(9, "Ahh")]
    [TestCase(10, "Ohh")]
    [TestCase(13, "Ohh")]
    [TestCase(14, "Advanced")]
    public void GetLongVocalizationTypeForGuidanceCount_MatchesBlock9Thresholds(int count, string expected)
    {
        Assert.That(TutorialStagePolicy.GetLongVocalizationTypeForGuidanceCount(count), Is.EqualTo(expected));
    }

    [Test]
    public void GetVocalizationTypeUnderTestForLong_AtOhhBoundary_StillAhhNotOhh()
    {
        // Last Ahh line posted → count 10; correction must target Ahh, not the next Ohh segment.
        Assert.That(TutorialStagePolicy.GetVocalizationTypeUnderTestForLong(10), Is.EqualTo("Ahh"));
    }
}
