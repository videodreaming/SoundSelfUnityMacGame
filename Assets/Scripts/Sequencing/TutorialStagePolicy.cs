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

    /// <summary>
    /// Vocalization the player is being tested on after guidance line <paramref name="guidanceCountAfterLinePosted"/>
    /// was posted (Wwise increments count when the line starts).
    /// </summary>
    public static string GetVocalizationTypeUnderTestForLong(int guidanceCountAfterLinePosted)
    {
        int indexBeforeLine = guidanceCountAfterLinePosted > 0 ? guidanceCountAfterLinePosted - 1 : 0;
        return GetLongVocalizationTypeForGuidanceCount(indexBeforeLine);
    }
}
