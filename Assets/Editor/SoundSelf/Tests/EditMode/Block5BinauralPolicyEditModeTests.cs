using NUnit.Framework;
using SoundSelf.Sequence;

/// <summary>Block 5 — binaural stage gating; see Docs/BLOCKS_4_5_7_PLAN.md Stage 2.</summary>
public class Block5BinauralPolicyEditModeTests
{
    [TestCase(StageType.MusicPlaylist, false, 0f)]
    [TestCase(StageType.LinearAudio, false, 0f)]
    [TestCase(StageType.Tutorial, true, BinauralStagePolicy.AudibleVolume)]
    [TestCase(StageType.Playground, true, BinauralStagePolicy.AudibleVolume)]
    public void GetTargetVolume_StageTypes_MatchBlock5Rules(StageType stageType, bool audible, float expectedVolume)
    {
        Assert.That(BinauralStagePolicy.ShouldBinauralBeAudible(stageType), Is.EqualTo(audible));
        Assert.That(BinauralStagePolicy.GetTargetVolume(stageType), Is.EqualTo(expectedVolume).Within(0.001f));
    }

    [Test]
    public void OpeningStage_NotInAudibleSet()
    {
        Assert.That(BinauralStagePolicy.ShouldBinauralBeAudible(StageType.Opening), Is.False);
        Assert.That(BinauralStagePolicy.GetTargetVolume(StageType.Opening), Is.EqualTo(BinauralStagePolicy.MutedVolume));
    }
}
