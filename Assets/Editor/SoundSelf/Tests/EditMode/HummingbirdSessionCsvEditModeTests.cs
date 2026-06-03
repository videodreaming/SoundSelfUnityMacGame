using NUnit.Framework;
using SoundSelf.Sequence;
using UnityEditor;

/// <summary>
/// Test Runner (EditMode): Hummingbird session_params strings → pack SO → startup sequence → first-stage SetMenu Enter contract.
/// Policy: <see cref="HummingbirdSessionCsvPolicy"/>, <see cref="SetMenuStagePolicy"/>.
/// </summary>
public class HummingbirdSessionCsvEditModeTests
{
    const string RegistryAssetPath = "Assets/Definitions/HummingbirdCalls/_HummingbirdContentPackRegistry.asset";

    HummingbirdContentPackRegistry _registry;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _registry = AssetDatabase.LoadAssetAtPath<HummingbirdContentPackRegistry>(RegistryAssetPath);
        Assert.That(_registry, Is.Not.Null, "Missing registry at " + RegistryAssetPath);
    }

    // ---- session flag parsing ----

    [TestCase("1", true)]
    [TestCase("0", false)]
    [TestCase("", false)]
    [TestCase("true", false)]
    public void ParseSessionFlag_OnlyOneIsTrue(string decrypted, bool expected)
    {
        Assert.That(HummingbirdSessionCsvPolicy.ParseSessionFlag(decrypted), Is.EqualTo(expected));
    }

    [TestCase("NotAMode", false)]
    [TestCase("", false)]
    [TestCase(CSVLoader.GameModeAdjunctive, true)]
    public void IsKnownGameMode_RecognizesCanonicalModesOnly(string normalized, bool expected)
    {
        Assert.That(HummingbirdSessionCsvPolicy.IsKnownGameMode(normalized), Is.EqualTo(expected));
    }

    [TestCase(CSVLoader.GameModeAdjunctive, "Single Stage", true)]
    [TestCase(CSVLoader.GameModeAdjunctive, "NotAPack", false)]
    [TestCase(CSVLoader.GameModeAlbums, CSVLoader.ContentPackAlbumSonoflore, true)]
    [TestCase(CSVLoader.GameModeAlbums, "Dual Stage", false)]
    [TestCase(CSVLoader.GameModeSonoflore, CSVLoader.ContentPackMindfulnessAndJoy, true)]
    [TestCase("UnknownMode", "anything", false)]
    public void IsKnownContentPack_RecognizesCanonicalPacksPerMode(string gameMode, string contentPack, bool expected)
    {
        Assert.That(HummingbirdSessionCsvPolicy.IsKnownContentPack(gameMode, contentPack), Is.EqualTo(expected));
    }

    [Test]
    public void CanonicalCsvKeySamples_AreKnownAfterNormalization()
    {
        foreach (var sample in HummingbirdSessionCsvPolicy.CanonicalCsvKeySamples)
        {
            var session = HummingbirdSessionCsvPolicy.ParseSessionParams(
                sample.RawGameMode, sample.RawContentPack, "0", "0", "0");
            Assert.That(HummingbirdSessionCsvPolicy.IsKnownGameMode(session.GameMode), Is.True, sample.RawGameMode);
            Assert.That(HummingbirdSessionCsvPolicy.IsKnownContentPack(session.GameMode, session.ContentPack), Is.True,
                sample.RawGameMode + " / " + sample.RawContentPack);
        }
    }

    [Test]
    public void ParseSessionParams_NormalizesGameModeAndContentPack()
    {
        var session = HummingbirdSessionCsvPolicy.ParseSessionParams(
            " adjunctive ",
            " SINGLE STAGE ",
            "1",
            "0",
            "1");
        Assert.That(session.GameMode, Is.EqualTo(CSVLoader.GameModeAdjunctive));
        Assert.That(session.ContentPack, Is.EqualTo(CSVLoader.ContentPackSingleStage));
        Assert.That(session.IsFirstTimeUser, Is.True);
        Assert.That(session.IsLayingDown, Is.False);
        Assert.That(session.IsVibroacoustic, Is.True);
    }

    [TestCase(CSVLoader.GameModeSonoflore, true, 0f)]
    [TestCase(CSVLoader.GameModeSonoflore, false, -CSVLoader.ClosingGoodbyeShortVersusLongDeltaSeconds)]
    [TestCase(CSVLoader.GameModeActivation, true, -CSVLoader.ClosingGoodbyeShortVersusLongDeltaSeconds)]
    [TestCase(CSVLoader.GameModeAdjunctive, false, 0f)]
    [TestCase(CSVLoader.GameModeAlbums, true, 0f)]
    public void PostUnguidedVoTimingAdjustment_MatchesGameModeAndFirstTimeUser(
        string gameMode, bool isFirstTimeUser, float expectedDelta)
    {
        Assert.That(
            HummingbirdSessionCsvPolicy.GetPostUnguidedVoTimingAdjustmentSeconds(gameMode, isFirstTimeUser),
            Is.EqualTo(expectedDelta).Within(0.001f));
    }

    [TestCase(CSVLoader.GameModeSonoflore, true, false)]
    [TestCase(CSVLoader.GameModeSonoflore, false, true)]
    [TestCase(CSVLoader.GameModeActivation, true, true)]
    [TestCase(CSVLoader.GameModeAdjunctive, false, true)]
    public void UsesReturningSessionVoPath_MatchesGameModeAndFirstTimeUser(
        string gameMode, bool isFirstTimeUser, bool expectedReturning)
    {
        Assert.That(
            HummingbirdSessionCsvPolicy.UsesReturningSessionVoPath(gameMode, isFirstTimeUser),
            Is.EqualTo(expectedReturning));
    }

    // ---- SetMenu Enter policy (handler contract) ----

    [TestCase(StageVariant.Menu_Welcome_PreCalibration, SetMenuEnterKind.WelcomePreCalibration, true, false)]
    [TestCase(StageVariant.Menu_AlbumChoice, SetMenuEnterKind.AlbumChoice, true, false)]
    [TestCase(StageVariant.Menu_Ps_InteractiveOrMusic, SetMenuEnterKind.PsInteractiveOrMusic, false, false)]
    [TestCase(StageVariant.Menu_Default, SetMenuEnterKind.UndefinedStubAutoComplete, false, true)]
    public void SetMenuStagePolicy_EnterExpectation_MatchesHandlerContract(
        StageVariant variant,
        SetMenuEnterKind kind,
        bool startsLinearBed,
        bool completesImmediately)
    {
        Assert.That(SetMenuStagePolicy.TryGetEnterExpectation(variant, out var exp), Is.True);
        Assert.That(exp.Kind, Is.EqualTo(kind));
        Assert.That(exp.StartsLinearAmbientBed, Is.EqualTo(startsLinearBed));
        Assert.That(exp.CompletesImmediately, Is.EqualTo(completesImmediately));
    }

    // ---- registry + pack + sequence (asset-backed) ----

    [Test]
    public void Registry_HasExactlyNineCanonicalRows()
    {
        Assert.That(HummingbirdSessionCsvPolicy.CanonicalCsvKeySamples.Length, Is.EqualTo(9));
        Assert.That(_registry.Entries.Count, Is.GreaterThanOrEqualTo(9));
    }

    [Test, TestCaseSource(nameof(CanonicalCsvKeyCases))]
    public void CanonicalCsvKeys_ResolvePackSequenceAndFirstStage(HummingbirdSessionCsvPolicy.CsvKeySample sample)
    {
        var session = HummingbirdSessionCsvPolicy.ParseSessionParams(
            sample.RawGameMode,
            sample.RawContentPack,
            "0",
            "0",
            "0");

        Assert.That(HummingbirdSessionCsvPolicy.TryResolvePack(_registry, session, out var pack), Is.True,
            $"Registry miss for ({session.GameMode}, {session.ContentPack}).");
        Assert.That(pack.name, Is.EqualTo(sample.Expectation.PackAssetName));

        var sequence = pack.SequenceDefinition;
        Assert.That(sequence, Is.Not.Null,
            $"Pack '{pack.name}' must assign sequenceDefinition (broken GUID blocks startup).");
        Assert.That(sequence.name, Is.EqualTo(sample.Expectation.SequenceAssetName));

        Assert.That(HummingbirdSessionCsvPolicy.TryGetFirstStage(sequence, out var stageType, out var variant), Is.True);
        Assert.That(stageType, Is.EqualTo(sample.Expectation.FirstStageType));
        Assert.That(variant, Is.EqualTo(sample.Expectation.FirstStageVariant));

        Assert.That(SetMenuStagePolicy.TryGetEnterExpectation(variant, out var setMenuEnter), Is.True);
        Assert.That(setMenuEnter.Kind, Is.EqualTo(sample.Expectation.SetMenuEnter.Kind));
        Assert.That(setMenuEnter.StartsLinearAmbientBed, Is.EqualTo(sample.Expectation.SetMenuEnter.StartsLinearAmbientBed));
        Assert.That(setMenuEnter.CompletesImmediately, Is.EqualTo(sample.Expectation.SetMenuEnter.CompletesImmediately));
    }

    [Test]
    public void AdjunctiveSingleStage_CaseInsensitiveCsvKeys_ResolveSamePack()
    {
        var session = HummingbirdSessionCsvPolicy.ParseSessionParams("ADJUNCTIVE", "single stage", "1", "1", "0");
        Assert.That(HummingbirdSessionCsvPolicy.TryResolvePack(_registry, session, out var pack), Is.True);
        Assert.That(pack.name, Is.EqualTo("HB_Adjunctive_SingleStage"));
        Assert.That(pack.SequenceDefinition.name, Is.EqualTo("Adjunctive_Singlestage"));
        Assert.That(HummingbirdSessionCsvPolicy.TryGetFirstStage(pack.SequenceDefinition, out var type, out var variant), Is.True);
        Assert.That(type, Is.EqualTo(StageType.SetMenu));
        Assert.That(variant, Is.EqualTo(StageVariant.Menu_Welcome_PreCalibration));
    }

    [Test]
    public void EveryRegistryRow_MatchesCanonicalStartupExpectation()
    {
        foreach (var sample in HummingbirdSessionCsvPolicy.CanonicalCsvKeySamples)
        {
            Assert.That(_registry.TryGetDefinition(sample.RawGameMode, sample.RawContentPack, out var pack), Is.True);
            Assert.That(pack.name, Is.EqualTo(sample.Expectation.PackAssetName));
        }
    }

    [Test]
    public void EveryPackSequenceDefinition_IsNonNullAndHasAtLeastOneStage()
    {
        foreach (var sample in HummingbirdSessionCsvPolicy.CanonicalCsvKeySamples)
        {
            Assert.That(_registry.TryGetDefinition(sample.RawGameMode, sample.RawContentPack, out var pack), Is.True);
            var def = pack.SequenceDefinition;
            Assert.That(def, Is.Not.Null, sample.Expectation.PackAssetName);
            Assert.That(def.StagesOrEmpty.Length, Is.GreaterThan(0), def.name);
        }
    }

    static System.Collections.IEnumerable CanonicalCsvKeyCases()
    {
        foreach (var sample in HummingbirdSessionCsvPolicy.CanonicalCsvKeySamples)
            yield return new TestCaseData(sample).SetName($"{sample.RawGameMode}_{sample.RawContentPack}");
    }
}
