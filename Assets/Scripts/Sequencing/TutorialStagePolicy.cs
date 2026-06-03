using SoundSelf.Sequence;

/// <summary>Block 9 — tutorial variant and long-tutorial vocalization bookkeeping.</summary>
public static class TutorialStagePolicy
{
    public const int LongGuidanceCountSwitchToAhh = 5;
    public const int LongGuidanceCountSwitchToOhh = 10;
    public const int LongGuidanceCountSwitchToAdvanced = 14;
    public const int LongGuidanceTotal = 19;

    /// <summary>
    /// Returning Sonoflore users get <see cref="StageVariant.Tutorial_Short"/> even when the sequence asset lists
    /// <see cref="StageVariant.Tutorial_Long"/> (Activation already uses Short in its asset).
    /// </summary>
    public static StageVariant ResolveEffectiveVariant(
        StageVariant assetVariant,
        string normalizedGameMode,
        bool isFirstTimeUser)
    {
        if (assetVariant == StageVariant.Tutorial_Long
            && normalizedGameMode == CSVLoader.GameModeSonoflore
            && !isFirstTimeUser)
        {
            return StageVariant.Tutorial_Short;
        }

        return assetVariant;
    }

    /// <summary>Long tutorial: Hum / Ahh / Ohh / Advanced from how many guidance lines have been posted so far.</summary>
    public static string GetLongVocalizationTypeForGuidanceCount(int guidanceCountSoFar)
    {
        if (guidanceCountSoFar < LongGuidanceCountSwitchToAhh)
            return "Hum";
        if (guidanceCountSoFar < LongGuidanceCountSwitchToOhh)
            return "Ahh";
        if (guidanceCountSoFar < LongGuidanceCountSwitchToAdvanced)
            return "Ohh";
        return "Advanced";
    }

    // Correction-only vocalization transition points, keyed directly on wwiseVOManager.TutorialGuidanceCount.
    // Pre-set to reproduce prior behavior; tweak each independently until corrections line up with the audible VO.
    public const int CorrectionSwitchToAhh = 5;
    public const int CorrectionSwitchToOhh = 11;
    public const int CorrectionSwitchToAdvanced = 14;

    /// <summary>
    /// Long correction: vocalization to repair, keyed directly on <paramref name="guidanceCount"/>
    /// (<c>wwiseVOManager.TutorialGuidanceCount</c>). Independent of the main guidance thresholds so each
    /// transition can be tuned without affecting which guidance line the main thread plays.
    /// </summary>
    public static string GetCorrectionVocalizationType(int guidanceCount)
    {
        if (guidanceCount < CorrectionSwitchToAhh)
            return "Hum";
        if (guidanceCount < CorrectionSwitchToOhh)
            return "Ahh";
        if (guidanceCount < CorrectionSwitchToAdvanced)
            return "Ohh";
        return "Advanced";
    }
}
