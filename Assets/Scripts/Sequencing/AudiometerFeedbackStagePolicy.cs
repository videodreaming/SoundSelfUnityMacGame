using SoundSelf.Sequence;
using UnityEngine;

/// <summary>
/// Single authority for whether the audiometer / "audit microphone feedback" button is shown, keyed by
/// <see cref="StageType"/>. Applied once per stage in <see cref="SoundSelf.Sequence.SequenceRunner.AdvanceToStage"/>.
/// The button (and its mic-monitoring popup) is only meaningful while the user is actively vocalizing into the
/// interactive stages, so it shows during Tutorial and Playground and is hidden everywhere else.
/// </summary>
public static class AudiometerFeedbackStagePolicy
{
    public static bool ShouldShowButton(StageType stageType) =>
        stageType == StageType.Tutorial || stageType == StageType.Playground;

    /// <summary>Shows/hides the audiometer feedback button for this stage. Called from <see cref="SoundSelf.Sequence.SequenceRunner.AdvanceToStage"/>.</summary>
    public static void ApplyAudiometerFeedbackButtonForStage(StageType stageType)
    {
        if (UIManager.Instance == null)
            return;
        UIManager.Instance.ShowAuditMicrophoneFeedbackButton(ShouldShowButton(stageType));
    }
}
