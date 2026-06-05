using NUnit.Framework;
using SoundSelf.Sequence;

/// <summary>Audiometer feedback button is shown only during the interactive Tutorial / Playground stages.</summary>
public class AudiometerFeedbackStagePolicyEditModeTests
{
    [TestCase(StageType.Tutorial, true)]
    [TestCase(StageType.Playground, true)]
    [TestCase(StageType.Opening, false)]
    [TestCase(StageType.Calibration, false)]
    [TestCase(StageType.Savasana, false)]
    [TestCase(StageType.SetMenu, false)]
    [TestCase(StageType.MusicPlaylist, false)]
    [TestCase(StageType.Inquiry, false)]
    [TestCase(StageType.LinearAudio, false)]
    [TestCase(StageType.StartCountdown, false)]
    [TestCase(StageType.Code, false)]
    [TestCase(StageType.End, false)]
    public void ShouldShowButton_MatchesTutorialAndPlaygroundOnly(StageType stageType, bool expected)
    {
        Assert.That(AudiometerFeedbackStagePolicy.ShouldShowButton(stageType), Is.EqualTo(expected));
    }
}
