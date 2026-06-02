using SoundSelf.Sequence;

/// <summary>
/// Block 3: which stages freeze normalization gain-riding <b>raises</b> (lowers still allowed).
/// Stage handlers call <see cref="ApplyRaiseFrozenOnStageEnter"/> on Enter.
/// </summary>
public static class MicNormalizationStagePolicy
{
    /// <summary>Stages that set raise-frozen on Enter. Others leave the prior value unchanged.</summary>
    public static bool StageEnterSetsRaiseFrozen(StageType stage) =>
        stage == StageType.Opening
        || stage == StageType.Savasana
        || stage == StageType.Tutorial
        || stage == StageType.Playground;

    /// <summary>Opening and savasana: no normalization creep up on quiet breath/noise; lowers OK if user tones.</summary>
    public static bool ShouldFreezeGainRidingRaisesOnEnter(StageType stage) =>
        stage == StageType.Opening || stage == StageType.Savasana;

    public static void ApplyRaiseFrozenOnStageEnter(ImitoneVoiceIntepreter interpreter, StageType stage)
    {
        if (interpreter == null || !StageEnterSetsRaiseFrozen(stage))
            return;
        interpreter.SetNormalizationGainRidingRaiseFrozen(ShouldFreezeGainRidingRaisesOnEnter(stage));
    }

    /// <summary>Gain riding may raise normalization only when not frozen and tone gates allow.</summary>
    public static bool AllowsGainRidingRaise(
        bool raiseFrozen,
        bool toneActiveConfident,
        bool raiseWindowOpen,
        bool raiseNoiseFloorClear) =>
        !raiseFrozen && toneActiveConfident && raiseWindowOpen && raiseNoiseFloorClear;
}
