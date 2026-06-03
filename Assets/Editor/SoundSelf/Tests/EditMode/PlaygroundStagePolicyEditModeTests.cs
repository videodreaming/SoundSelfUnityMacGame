using NUnit.Framework;
using SoundSelf.Sequence;

/// <summary>Stage 0 — Playground_Debug parked variant; see Docs/BLOCKS_4_5_7_PLAN.md.</summary>
public class PlaygroundStagePolicyEditModeTests
{
    [Test]
    public void Playground_Debug_IsParkedAndSkipsTimelineCoroutine()
    {
        Assert.That(PlaygroundStagePolicy.IsParkedDebugVariant(StageVariant.Playground_Debug), Is.True);
        Assert.That(PlaygroundStagePolicy.StartsPlaygroundTimelineCoroutine(StageVariant.Playground_Debug), Is.False);
    }

    [Test]
    public void Playground_Standard_UsesTimelineCoroutine()
    {
        Assert.That(PlaygroundStagePolicy.IsParkedDebugVariant(StageVariant.Playground_Standard), Is.False);
        Assert.That(PlaygroundStagePolicy.StartsPlaygroundTimelineCoroutine(StageVariant.Playground_Standard), Is.True);
    }

    [Test]
    public void Playground_Ascending_UsesTimelineCoroutine()
    {
        Assert.That(PlaygroundStagePolicy.StartsPlaygroundTimelineCoroutine(StageVariant.Playground_Ascending), Is.True);
    }
}
