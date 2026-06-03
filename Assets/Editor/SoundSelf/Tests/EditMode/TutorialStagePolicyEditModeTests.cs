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
}
