using SoundSelf.Sequence;

/// <summary>
/// Pure session resolution rules for Hummingbird <c>session_params.csv</c> strings (normalization, flags, pack/sequence startup contract).
/// <see cref="CSVLoader"/> and Test Runner tests should share this type.
/// </summary>
public static class HummingbirdSessionCsvPolicy
{
    /// <summary>Decrypted CSV session flag: only <c>"1"</c> is true (same as <see cref="CSVLoader"/>).</summary>
    public static bool ParseSessionFlag(string decrypted) => decrypted == "1";

    /// <summary>True when <paramref name="normalizedGameMode"/> is one of the four canonical <see cref="CSVLoader"/> game modes (after <see cref="CSVLoader.NormalizeGameMode"/>).</summary>
    public static bool IsKnownGameMode(string normalizedGameMode)
    {
        if (string.IsNullOrEmpty(normalizedGameMode))
            return false;
        return normalizedGameMode == CSVLoader.GameModeSonoflore
               || normalizedGameMode == CSVLoader.GameModeActivation
               || normalizedGameMode == CSVLoader.GameModeAdjunctive
               || normalizedGameMode == CSVLoader.GameModeAlbums;
    }

    /// <summary>True when <paramref name="normalizedContentPack"/> is a canonical pack for <paramref name="normalizedGameMode"/> (after <see cref="CSVLoader.NormalizeContentPack"/>).</summary>
    public static bool IsKnownContentPack(string normalizedGameMode, string normalizedContentPack)
    {
        if (string.IsNullOrEmpty(normalizedContentPack))
            return false;

        if (normalizedGameMode == CSVLoader.GameModeAlbums)
            return normalizedContentPack == CSVLoader.ContentPackAlbumSonoflore;

        if (normalizedGameMode == CSVLoader.GameModeAdjunctive)
            return normalizedContentPack == CSVLoader.ContentPackDualStage
                   || normalizedContentPack == CSVLoader.ContentPackSingleStage;

        if (normalizedGameMode == CSVLoader.GameModeSonoflore
            || normalizedGameMode == CSVLoader.GameModeActivation)
        {
            return normalizedContentPack == CSVLoader.ContentPackMindfulnessAndJoy
                   || normalizedContentPack == CSVLoader.ContentPackPsychologicalFlexibility
                   || normalizedContentPack == CSVLoader.ContentPackSurrenderResponse
                   || normalizedContentPack == CSVLoader.ContentPackSelfCompassion
                   || normalizedContentPack == CSVLoader.ContentPackLovingKindness
                   || normalizedContentPack == CSVLoader.ContentPackTransitionsGriefAndAppreciation;
        }

        return false;
    }

    /// <summary>Human-readable list of expected content packs for warnings (matches <see cref="IsKnownContentPack"/>).</summary>
    public static string GetExpectedContentPacksDescription(string normalizedGameMode)
    {
        if (normalizedGameMode == CSVLoader.GameModeAlbums)
            return CSVLoader.ContentPackAlbumSonoflore;
        if (normalizedGameMode == CSVLoader.GameModeAdjunctive)
            return CSVLoader.ContentPackDualStage + " or " + CSVLoader.ContentPackSingleStage;
        if (normalizedGameMode == CSVLoader.GameModeSonoflore)
            return "Mindfulness and Joy, Psychological Flexibility, or Surrender Response";
        if (normalizedGameMode == CSVLoader.GameModeActivation)
            return "Self Compassion, Loving Kindness, or Transitions (Grief and Appreciation)";
        return "(resolve gameMode first)";
    }

    /// <summary>Warning when decrypted CSV gameMode does not normalize to a canonical mode.</summary>
    public static string BuildUnknownGameModeWarning(string normalizedGameMode, string decryptedGameMode) =>
        "CSVLoader: Unrecognized gameMode \"" + normalizedGameMode + "\" after normalization"
        + (string.IsNullOrEmpty(decryptedGameMode) ? "." : " (decrypted: \"" + decryptedGameMode + "\").")
        + " Expected one of: " + CSVLoader.GameModeSonoflore + ", " + CSVLoader.GameModeActivation + ", "
        + CSVLoader.GameModeAdjunctive + ", " + CSVLoader.GameModeAlbums + ".";

    /// <summary>Warning when decrypted CSV contentPack does not normalize to a canonical pack for the effective gameMode.</summary>
    public static string BuildUnknownContentPackWarning(string normalizedGameMode, string normalizedContentPack, string decryptedContentPack) =>
        "CSVLoader: Unrecognized contentPack \"" + normalizedContentPack + "\" for gameMode \"" + normalizedGameMode + "\""
        + (string.IsNullOrEmpty(decryptedContentPack) ? "." : " (decrypted: \"" + decryptedContentPack + "\").")
        + " Expected for this mode: " + GetExpectedContentPacksDescription(normalizedGameMode) + ".";

    /// <summary>Normalized Hummingbird session keys + boolean flags from decrypted CSV fields.</summary>
    public readonly struct SessionParams
    {
        public string GameMode { get; }
        public string ContentPack { get; }
        public bool IsFirstTimeUser { get; }
        public bool IsLayingDown { get; }
        public bool IsVibroacoustic { get; }

        public SessionParams(string gameMode, string contentPack, bool isFirstTimeUser, bool isLayingDown, bool isVibroacoustic)
        {
            GameMode = gameMode;
            ContentPack = contentPack;
            IsFirstTimeUser = isFirstTimeUser;
            IsLayingDown = isLayingDown;
            IsVibroacoustic = isVibroacoustic;
        }
    }

    /// <summary>Expected first sequence stage + SetMenu Enter contract for a registry-listed pack at session startup.</summary>
    public readonly struct StartupSequenceExpectation
    {
        public string PackAssetName { get; }
        public string SequenceAssetName { get; }
        public StageType FirstStageType { get; }
        public StageVariant FirstStageVariant { get; }
        public SetMenuStagePolicy.SetMenuEnterExpectation SetMenuEnter { get; }

        public StartupSequenceExpectation(
            string packAssetName,
            string sequenceAssetName,
            StageType firstStageType,
            StageVariant firstStageVariant,
            SetMenuStagePolicy.SetMenuEnterExpectation setMenuEnter)
        {
            PackAssetName = packAssetName;
            SequenceAssetName = sequenceAssetName;
            FirstStageType = firstStageType;
            FirstStageVariant = firstStageVariant;
            SetMenuEnter = setMenuEnter;
        }
    }

    /// <summary>Build normalized <see cref="SessionParams"/> from decrypted CSV strings (before encryption in production).</summary>
    public static SessionParams ParseSessionParams(
        string decryptedGameMode,
        string decryptedContentPack,
        string decryptedFirstTimeUser,
        string decryptedLayingDown,
        string decryptedVibroacoustic)
    {
        string gameMode = CSVLoader.NormalizeGameMode(decryptedGameMode);
        string contentPack = CSVLoader.NormalizeContentPack(decryptedContentPack, gameMode);
        return new SessionParams(
            gameMode,
            contentPack,
            ParseSessionFlag(decryptedFirstTimeUser),
            ParseSessionFlag(decryptedLayingDown),
            ParseSessionFlag(decryptedVibroacoustic));
    }

    /// <summary>VO timing delta applied to post-unguided duration (seconds); mirrors <see cref="CSVLoader.VOInitializations"/>.</summary>
    public static float GetPostUnguidedVoTimingAdjustmentSeconds(string normalizedGameMode, bool isFirstTimeUser)
    {
        if (normalizedGameMode == CSVLoader.GameModeSonoflore && !isFirstTimeUser)
            return -CSVLoader.ClosingGoodbyeShortVersusLongDeltaSeconds;
        if (normalizedGameMode == CSVLoader.GameModeActivation)
            return -CSVLoader.ClosingGoodbyeShortVersusLongDeltaSeconds;
        return 0f;
    }

    /// <summary>Whether returning-session VO path applies (not first-time Sonoflore).</summary>
    public static bool UsesReturningSessionVoPath(string normalizedGameMode, bool isFirstTimeUser)
    {
        if (normalizedGameMode == CSVLoader.GameModeSonoflore)
            return !isFirstTimeUser;
        if (normalizedGameMode == CSVLoader.GameModeActivation)
            return true;
        if (normalizedGameMode == CSVLoader.GameModeAdjunctive)
            return true;
        return false;
    }

    public static bool TryResolvePack(
        HummingbirdContentPackRegistry registry,
        SessionParams session,
        out HummingbirdContentPackDefinition pack)
    {
        pack = null;
        if (registry == null)
            return false;
        return registry.TryGetDefinition(session.GameMode, session.ContentPack, out pack);
    }

    public static bool TryGetFirstStage(SequenceDefinition definition, out StageType stageType, out StageVariant variant)
    {
        stageType = default;
        variant = default;
        if (definition == null)
            return false;
        var stages = definition.StagesOrEmpty;
        if (stages == null || stages.Length == 0)
            return false;
        stageType = stages[0].type;
        variant = stages[0].variant;
        return true;
    }

    /// <summary>Canonical registry rows: decrypted CSV strings → pack SO name → startup sequence + first-stage Enter contract.</summary>
    public static readonly StartupSequenceExpectation[] CanonicalStartupExpectations =
    {
        new StartupSequenceExpectation(
            "HB_Sonoflore_MindfulnessAndJoy",
            "Sonoflore",
            StageType.SetMenu,
            StageVariant.Menu_Welcome_PreCalibration,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, true, false)),
        new StartupSequenceExpectation(
            "HB_Sonoflore_PsychologicalFlexibility",
            "Sonoflore",
            StageType.SetMenu,
            StageVariant.Menu_Welcome_PreCalibration,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, true, false)),
        new StartupSequenceExpectation(
            "HB_Sonoflore_SurrenderResponse",
            "Sonoflore",
            StageType.SetMenu,
            StageVariant.Menu_Welcome_PreCalibration,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, true, false)),
        new StartupSequenceExpectation(
            "HB_Activation_SelfCompassion",
            "Activation",
            StageType.SetMenu,
            StageVariant.Menu_Welcome_PreCalibration,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, true, false)),
        new StartupSequenceExpectation(
            "HB_Activation_LovingKindness",
            "Activation",
            StageType.SetMenu,
            StageVariant.Menu_Welcome_PreCalibration,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, true, false)),
        new StartupSequenceExpectation(
            "HB_Activation_Transitions",
            "Activation",
            StageType.SetMenu,
            StageVariant.Menu_Welcome_PreCalibration,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, true, false)),
        new StartupSequenceExpectation(
            "HB_Adjunctive_DualStage",
            "Adjunctive_Dualstage_Start",
            StageType.SetMenu,
            StageVariant.Menu_Welcome_PreCalibration,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, true, false)),
        new StartupSequenceExpectation(
            "HB_Adjunctive_SingleStage",
            "Adjunctive_Singlestage",
            StageType.SetMenu,
            StageVariant.Menu_Welcome_PreCalibration,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.WelcomePreCalibration, true, false)),
        new StartupSequenceExpectation(
            "HB_Albums_Sonoflore",
            "Album_Welcome",
            StageType.SetMenu,
            StageVariant.Menu_AlbumChoice,
            new SetMenuStagePolicy.SetMenuEnterExpectation(SetMenuEnterKind.AlbumChoice, true, false)),
    };

    /// <summary>CSV keys sent by Hummingbird for each canonical pack (matches <see cref="HummingbirdContentPackRegistry"/> rows).</summary>
    public readonly struct CsvKeySample
    {
        public string RawGameMode { get; }
        public string RawContentPack { get; }
        public StartupSequenceExpectation Expectation { get; }

        public CsvKeySample(string rawGameMode, string rawContentPack, StartupSequenceExpectation expectation)
        {
            RawGameMode = rawGameMode;
            RawContentPack = rawContentPack;
            Expectation = expectation;
        }
    }

    public static readonly CsvKeySample[] CanonicalCsvKeySamples =
    {
        new CsvKeySample(CSVLoader.GameModeSonoflore, CSVLoader.ContentPackMindfulnessAndJoy, CanonicalStartupExpectations[0]),
        new CsvKeySample(CSVLoader.GameModeSonoflore, CSVLoader.ContentPackPsychologicalFlexibility, CanonicalStartupExpectations[1]),
        new CsvKeySample(CSVLoader.GameModeSonoflore, CSVLoader.ContentPackSurrenderResponse, CanonicalStartupExpectations[2]),
        new CsvKeySample(CSVLoader.GameModeActivation, CSVLoader.ContentPackSelfCompassion, CanonicalStartupExpectations[3]),
        new CsvKeySample(CSVLoader.GameModeActivation, CSVLoader.ContentPackLovingKindness, CanonicalStartupExpectations[4]),
        new CsvKeySample(CSVLoader.GameModeActivation, CSVLoader.ContentPackTransitionsGriefAndAppreciation, CanonicalStartupExpectations[5]),
        new CsvKeySample(CSVLoader.GameModeAdjunctive, CSVLoader.ContentPackDualStage, CanonicalStartupExpectations[6]),
        new CsvKeySample(CSVLoader.GameModeAdjunctive, CSVLoader.ContentPackSingleStage, CanonicalStartupExpectations[7]),
        new CsvKeySample(CSVLoader.GameModeAlbums, CSVLoader.ContentPackAlbumSonoflore, CanonicalStartupExpectations[8]),
    };
}
