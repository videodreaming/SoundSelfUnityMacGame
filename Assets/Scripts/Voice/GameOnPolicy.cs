using SoundSelf.Sequence;

/// <summary>
/// Central rules for when <see cref="ImitoneVoiceIntepreter.gameOn"/> should be true.
/// EditMode tests in <c>Assets/Editor/SoundSelf/Tests/EditMode</c> lock Block 3 expectations.
/// See <c>Docs/PLAYTEST_NOTES_ORGANIZED.md</c> Block 3.
/// </summary>
public static class GameOnPolicy
{
    /// <summary>Dual Stacks: interactive step 1 when main countdown reaches this many seconds remaining.</summary>
    public const float PlaygroundProtocolStacksStep1ThresholdSeconds = 20f * 60f;

    /// <summary>Savasana PS Ascending: <c>Cue_Stop_Interactive_3m</c> → delayed mic off (seconds).</summary>
    public const float SavasanaAscendingDelayedMicOffSeconds = 120f;

    /// <summary>
    /// After <see cref="MusicSystem1.SetMusicModeTo"/>, sequencing calls <see cref="Sequencer.ApplyGameOnPolicy"/>
    /// when session intent requires <c>gameOn</c> to follow the mode.
    /// <c>null</c> = leave unchanged (e.g. MusicLoopSilent — Ascending keeps prior state).
    /// </summary>
    public static bool? GetGameOnAssignmentForMusicMode(MusicSystem1.MusicMode mode)
    {
        switch (mode)
        {
            case MusicSystem1.MusicMode.Silent:
                return false;
            case MusicSystem1.MusicMode.Freeplay:
                return true;
            case MusicSystem1.MusicMode.FrozenFreeplay:
                return false;
            case MusicSystem1.MusicMode.MusicLoopSilent:
            case MusicSystem1.MusicMode.InteractiveTutorial:
            case MusicSystem1.MusicMode.Environment:
            default:
                return null;
        }
    }

    /// <summary>
    /// <see cref="OpeningStageHandler"/> Enter sets <c>gameOn</c> true for all variants (not via Silent mode policy).
    /// </summary>
    public const bool OpeningEnterSetsGameOnTrue = true;

    /// <summary>
    /// During Adjunctive Dual Stacks playground, mic should stay on from Step 1 (Freeplay) through main segment.
    /// Lorna reports at 16:15 and 15:37 on the countdown clock — both are inside this window.
    /// </summary>
    public static bool ExpectGameOnDuringDualStacksPlayground(float countdownThisSectionSeconds) =>
        countdownThisSectionSeconds > 0f
        && countdownThisSectionSeconds <= PlaygroundProtocolStacksStep1ThresholdSeconds;

    /// <summary>Parse playtest clock strings (time remaining), e.g. <c>16:15</c> → 975 seconds.</summary>
    public static bool TryParseCountdownClock(string clock, out float secondsRemaining)
    {
        secondsRemaining = 0f;
        if (string.IsNullOrWhiteSpace(clock))
            return false;

        var parts = clock.Trim().Split(':');
        if (parts.Length != 2)
            return false;
        if (!int.TryParse(parts[0], out int minutes) || !int.TryParse(parts[1], out int seconds))
            return false;
        if (minutes < 0 || seconds < 0 || seconds >= 60)
            return false;

        secondsRemaining = minutes * 60f + seconds;
        return true;
    }
}
