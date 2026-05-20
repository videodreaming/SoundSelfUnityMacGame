using UnityEngine;
using System.IO;
using System;
using UnityEngine.Serialization;
using SoundSelf.Sequence;

public class CSVLoader : MonoBehaviour
{
    public static CSVLoader instance { get; private set; }
    public Sequencer sequencer;

    public WwiseVOManager wwiseVOManager;

    /// <summary>
    /// Maps normalized (gameMode, contentPack) to pack SOs. Create via <b>Assets → Create → SoundSelf → Hummingbird → Hummingbird Content Pack Registry</b>;
    /// store under <c>Assets/Definitions/HummingbirdCalls/</c>. Required for session VO/timing resolution at startup.
    /// </summary>
    [SerializeField] private HummingbirdContentPackRegistry hummingbirdContentPackRegistry;

    /// <summary>Registry asset listing every supported pack row. Assign under <c>Assets/Definitions/HummingbirdCalls/</c>.</summary>
    public HummingbirdContentPackRegistry ContentPackRegistry => hummingbirdContentPackRegistry;

    /// <summary>Set during <see cref="Awake"/> when the registry resolves the active session pack.</summary>
    private HummingbirdContentPackDefinition _resolvedSessionPack;

#if UNITY_EDITOR
    [Header("Debug (Editor only)")]
    [Tooltip("Registry-listed pack to impersonate. Effective session keys come from that pack's registry row. Ignored in player builds.")]
    [SerializeField] private HummingbirdContentPackDefinition hummingbirdContentPackOverride;
#endif

    /// <summary>Resolved pack for this session after CSV (+ editor override). Consumers include sequencing.</summary>
    public HummingbirdContentPackDefinition ResolvedSessionPack => _resolvedSessionPack;

    /// <summary>One of: <see cref="GameModeSonoflore"/>, <see cref="GameModeActivation"/>, <see cref="GameModeAdjunctive"/>, <see cref="GameModeAlbums"/>.</summary>
    public string gameMode { get; private set; }

    /// <summary>
    /// For <b>Sonoflore</b> / <b>Activation</b>: one of the six thematic content packs.
    /// For <b>Adjunctive</b>: <c>Dual Stage</c> or <c>Single Stage</c>.
    /// For <b>Albums</b>: <c>AlbumSonoflore</c> (only pack for now).
    /// </summary>
    public string contentPack { get; private set; }

    public bool IsFirstTimeUser { get; private set; }
    public bool IsLayingDown { get; private set; }
    public bool IsVibroacoustic { get; private set; }
    public float timeToPlayClosingGoodbye;
    public float totalTimeOfPostUnguidedVocalizationContent;
    public static int currentSessionNumber = 0;
    private string baseSessionsFolderPath = "";
    public string encryptedReadyCheck;
    public string decryptedReadyCheck;
    private string encryptedSessionNumber;
    [SerializeField] private string encryptedGameMode;
    [FormerlySerializedAs("encryptedSubGameMode")]
    [SerializeField] private string encryptedContentPack;
    [SerializeField] private string decryptedGameMode;
    [FormerlySerializedAs("decryptedSubGameMode")]
    [SerializeField] private string decryptedContentPack;
    [SerializeField] private string encryptedFirstTimeUser;
    [SerializeField] private string decryptedFirstTimeUser;
    [SerializeField] private string encryptedLayingDown;
    [SerializeField] private string decryptedLayingDown;
    [SerializeField] private string encryptedVibroacoustic;
    [SerializeField] private string decryptedVibroacoustic;

    public const string GameModeSonoflore = "Sonoflore";
    public const string GameModeActivation = "Activation";
    public const string GameModeAdjunctive = "Adjunctive";
    public const string GameModeAlbums = "Albums";

    public const string ContentPackMindfulnessAndJoy = "Mindfulness and Joy";
    public const string ContentPackPsychologicalFlexibility = "Psychological Flexibility";
    public const string ContentPackSurrenderResponse = "Surrender Response";
    public const string ContentPackSelfCompassion = "Self Compassion";
    public const string ContentPackLovingKindness = "Loving Kindness";
    /// <summary>Full label including grief/appreciation framing.</summary>
    public const string ContentPackTransitionsGriefAndAppreciation = "Transitions (Grief and Appreciation)";

    public const string ContentPackDualStage = "Dual Stage";
    public const string ContentPackSingleStage = "Single Stage";
    public const string ContentPackAlbumSonoflore = "AlbumSonoflore";

    /// <summary>Seconds shorter than pack baseline when Wwise <c>VO_ClosingGoodbye</c> is Short (returned from <see cref="VOInitializations"/> as negative).</summary>
    public const float ClosingGoodbyeShortVersusLongDeltaSeconds = 53f;

    /// <summary>Hummingbird sends exactly these modes (case-insensitive); unknown strings pass through unchanged.</summary>
    public static string NormalizeGameMode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        string t = raw.Trim();
        if (t.Equals(GameModeSonoflore, StringComparison.OrdinalIgnoreCase))
            return GameModeSonoflore;
        if (t.Equals(GameModeActivation, StringComparison.OrdinalIgnoreCase))
            return GameModeActivation;
        if (t.Equals(GameModeAdjunctive, StringComparison.OrdinalIgnoreCase))
            return GameModeAdjunctive;
        if (t.Equals(GameModeAlbums, StringComparison.OrdinalIgnoreCase))
            return GameModeAlbums;
        return t;
    }

    /// <summary>
    /// Maps Hummingbird content-pack strings to canonical <see cref="contentPack"/> constants (case-insensitive).
    /// Expected inputs are fixed per mode; unknown strings pass through unchanged.
    /// </summary>
    public static string NormalizeContentPack(string raw, string normalizedGameMode)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        string t = raw.Trim();

        if (normalizedGameMode == GameModeAlbums)
        {
            if (t.Equals(ContentPackAlbumSonoflore, StringComparison.OrdinalIgnoreCase))
                return ContentPackAlbumSonoflore;
            return t;
        }

        if (normalizedGameMode == GameModeAdjunctive)
        {
            if (t.Equals(ContentPackDualStage, StringComparison.OrdinalIgnoreCase))
                return ContentPackDualStage;
            if (t.Equals(ContentPackSingleStage, StringComparison.OrdinalIgnoreCase))
                return ContentPackSingleStage;
            return t;
        }

        // Sonoflore + Activation (six thematic packs from Hummingbird)
        if (t.Equals(ContentPackMindfulnessAndJoy, StringComparison.OrdinalIgnoreCase))
            return ContentPackMindfulnessAndJoy;
        if (t.Equals(ContentPackPsychologicalFlexibility, StringComparison.OrdinalIgnoreCase))
            return ContentPackPsychologicalFlexibility;
        if (t.Equals(ContentPackSurrenderResponse, StringComparison.OrdinalIgnoreCase))
            return ContentPackSurrenderResponse;
        if (t.Equals(ContentPackSelfCompassion, StringComparison.OrdinalIgnoreCase))
            return ContentPackSelfCompassion;
        if (t.Equals(ContentPackLovingKindness, StringComparison.OrdinalIgnoreCase))
            return ContentPackLovingKindness;
        if (t.Equals(ContentPackTransitionsGriefAndAppreciation, StringComparison.OrdinalIgnoreCase))
            return ContentPackTransitionsGriefAndAppreciation;

        return t;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        DontDestroyOnLoad(gameObject);

#if UNITY_STANDALONE_OSX
        string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        baseSessionsFolderPath = System.IO.Path.Combine(userFolder, "Appdata", "Roaming", "Hummingbird");
#elif UNITY_STANDALONE_WIN
        baseSessionsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hummingbird", "StreamingAssets", "Resources");
        Debug.Log("Base path: " + baseSessionsFolderPath);
#else
        Debug.LogError("Unsupported platform");
        return;
#endif

        Directory.CreateDirectory(baseSessionsFolderPath);
        string sessionsCsvPath = Path.Combine(baseSessionsFolderPath, "sessions.csv");

        if (File.Exists(sessionsCsvPath))
        {
            using (StreamReader reader = new StreamReader(sessionsCsvPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split(',');
                    if (columns.Length >= 2)
                    {
                        encryptedReadyCheck = columns[1].Trim();
                        decryptedReadyCheck = EncryptionHelper.Decrypt(encryptedReadyCheck);
                        if (decryptedReadyCheck == "ready")
                        {
                            encryptedSessionNumber = columns[0].Trim();
                            if (int.TryParse(EncryptionHelper.Decrypt(encryptedSessionNumber), out int sessionNumber))
                            {
                                currentSessionNumber = sessionNumber;
                                break;
                            }
                        }
                    }
                }
            }
        }

        ReadSessionParams();
#if UNITY_EDITOR
        ApplyEditorContentPackOverrideIfPresent();
#endif
        ResolveSessionPackDefinition();
        TimeLeftInitializations(VOInitializations());
    }

    void ReadSessionParams()
    {
        if (currentSessionNumber != 0)
        {
            string sessionsParams = Path.Combine(baseSessionsFolderPath, $"session_{currentSessionNumber}", "session_params.csv");
            if (File.Exists(sessionsParams))
            {
                Debug.Log("CSV file found at: " + sessionsParams);
                string[] data = File.ReadAllText(sessionsParams).Split(new string[] { ",", "\n" }, StringSplitOptions.None);
                if (data.Length < 5)
                {
                    Debug.LogError("CSVLoader: session_params.csv is malformed. Expected at least 5 comma/newline-separated values (gameMode, contentPack, firstTimeUser, layingDown, vibroacoustic), but got " + data.Length + ". Path: " + sessionsParams);
                    return;
                }
                encryptedGameMode = data[0].Trim();
                encryptedContentPack = data[1].Trim();
                encryptedFirstTimeUser = data[2].Trim();
                encryptedLayingDown = data[3].Trim();
                encryptedVibroacoustic = data[4].Trim();

                Debug.Log("Encrypted Game Mode: " + encryptedGameMode);
                Debug.Log("Encrypted Content Pack: " + encryptedContentPack);
                Debug.Log("Encrypted First Time User: " + encryptedFirstTimeUser);
                Debug.Log("Encrypted Laying Down: " + encryptedLayingDown);
                Debug.Log("Encrypted Vibroacoustic: " + encryptedVibroacoustic);

                decryptedFirstTimeUser = EncryptionHelper.Decrypt(encryptedFirstTimeUser);
                decryptedLayingDown = EncryptionHelper.Decrypt(encryptedLayingDown);
                decryptedVibroacoustic = EncryptionHelper.Decrypt(encryptedVibroacoustic);
                decryptedGameMode = EncryptionHelper.Decrypt(encryptedGameMode);
                decryptedContentPack = EncryptionHelper.Decrypt(encryptedContentPack);

                gameMode = NormalizeGameMode(decryptedGameMode);
                contentPack = NormalizeContentPack(decryptedContentPack, gameMode);

                IsFirstTimeUser = decryptedFirstTimeUser == "1";
                IsLayingDown = decryptedLayingDown == "1";
                IsVibroacoustic = decryptedVibroacoustic == "1";
            }
            else
            {
                Debug.LogError("CSV file not found at: " + sessionsParams);
            }
        }
        else
        {
            Debug.LogError("CSVLoader: No session number found");
        }
        Debug.Log("CSVLoader: modes set to: Game Mode(" + GetCurrentMode() + ") Content Pack(" + GetCurrentContentPack() + ")");
    }

#if UNITY_EDITOR
    private void ApplyEditorContentPackOverrideIfPresent()
    {
        if (hummingbirdContentPackOverride == null)
            return;
        if (hummingbirdContentPackRegistry == null)
        {
            Debug.LogError("CSVLoader: hummingbirdContentPackOverride is set but HummingbirdContentPackRegistry is not assigned.");
            return;
        }

        if (!hummingbirdContentPackRegistry.TryGetCsvKeysForPackDefinition(hummingbirdContentPackOverride, out string gm, out string cp))
        {
            Debug.LogError("CSVLoader: Content pack override asset is not listed in the registry — cannot impersonate session.");
            return;
        }

        Debug.LogError(
            "CSVLoader: Content pack override is active (Editor only). Session behaves as gameMode=\"" + gm + "\", contentPack=\"" + cp + "\". Clear override before shipping.");
        gameMode = gm;
        contentPack = cp;
    }
#endif

    private void ResolveSessionPackDefinition()
    {
        _resolvedSessionPack = null;
        if (hummingbirdContentPackRegistry == null)
        {
            Debug.LogError("CSVLoader: Assign HummingbirdContentPackRegistry (Assets/Definitions/HummingbirdCalls/). Session pack not resolved.");
            return;
        }

        if (!hummingbirdContentPackRegistry.TryGetDefinition(gameMode, contentPack, out var pack))
        {
            Debug.LogError(
                "CSVLoader: No registry row for effective session (gameMode=\"" + gameMode + "\", contentPack=\"" + contentPack + "\"). Check session_params and registry.");
            return;
        }

        _resolvedSessionPack = pack;
        if (!SessionGameModeMapping.Matches(pack.GameMode, gameMode))
        {
            Debug.LogError(
                "CSVLoader: Resolved pack SO \"" + pack.name + "\" declares GameMode " + pack.GameMode + " (CSV \""
                + SessionGameModeMapping.ToCsvGameModeString(pack.GameMode) + "\") but effective session gameMode is \"" + gameMode + "\".");
        }
    }

    private static void ApplyContentPackVoKind(ContentPackVoKind kind, WwiseVOManager vo)
    {
        switch (kind)
        {
            case ContentPackVoKind.None:
                break;
            case ContentPackVoKind.Fireflies:
                vo.SetToFireflies();
                break;
            case ContentPackVoKind.Kindness:
                vo.SetToKindness();
                break;
            case ContentPackVoKind.Metta:
                vo.SetToMetta();
                break;
            case ContentPackVoKind.Peace:
                vo.SetToPeace();
                break;
            case ContentPackVoKind.Narrative:
                vo.SetToNarrative();
                break;
            case ContentPackVoKind.Surrender:
                vo.SetToSurrender();
                break;
            case ContentPackVoKind.EsketamineAscending:
                vo.SetToEsketamineAscending();
                break;
            default:
                Debug.LogError("CSVLoader: Unhandled ContentPackVoKind " + kind + ".");
                break;
        }
    }

    private float VOInitializations() //returns adjustment to the savasana timing
    {
        if (wwiseVOManager == null)
        {
            Debug.LogError("CSVLoader: VOInitializations() - wwiseVOManager is null! Cannot initialize VO.");
            return 0f;
        }

        if (_resolvedSessionPack == null)
        {
            Debug.LogWarning("CSVLoader: VOInitializations skipped — session pack was not resolved (registry miss or unset registry).");
            return 0f;
        }

        var pack = _resolvedSessionPack;

        ApplyContentPackVoKind(pack.VoKind, wwiseVOManager);
        if (gameMode == GameModeSonoflore)
        {

            if (!IsFirstTimeUser)
            {
                wwiseVOManager.notFirstTimeUser();
                return -ClosingGoodbyeShortVersusLongDeltaSeconds;
            }
            else
            {
                wwiseVOManager.firstTimeUser();
                return 0f;
            }
        }
        else if (gameMode == GameModeActivation)
        {
            wwiseVOManager.notFirstTimeUser();
            return -ClosingGoodbyeShortVersusLongDeltaSeconds;
        }
        else if (gameMode == GameModeAdjunctive)
        {
            wwiseVOManager.notFirstTimeUser(); //Do we even need this? Ask Lorna.
            return 0f;
        }
        else if (gameMode == GameModeAlbums)
        {
            return 0f;
        }
        return 0f;

    }

    /// <summary>
    /// Hydrates <see cref="TimeTrackerScript"/> from <see cref="ResolvedSessionPack"/> (post-unguided duration only).
    /// Session countdown value and ticking start only when the sequence runs the StartCountdown stage (<see cref="TimeTrackerScript.BeginCountdownPair"/>).
    /// </summary>
    private void TimeLeftInitializations(float savasanaTimingAdjustment)
    {
        if (TimeTrackerScript.instance == null)
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - TimeTrackerScript.instance is null. Timing behaviors will not work properly.");
            return;
        }

        var tracker = TimeTrackerScript.instance;
        totalTimeOfPostUnguidedVocalizationContent = 0f;
        bool recognizedGameMode = false;

        if (_resolvedSessionPack != null)
        {
            float secs = _resolvedSessionPack.PostUnguidedSeconds;
            if (secs < 0f)
            {
                totalTimeOfPostUnguidedVocalizationContent = 0f;
                recognizedGameMode = false;
                Debug.LogWarning("CSVLoader: TimeLeftInitializations() — post-unguided duration is less than 0. Post-unguided duration not set from CSV.");
            }
            else
            {
                totalTimeOfPostUnguidedVocalizationContent = secs;
                recognizedGameMode = true;
            }
        }
        else
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() — session pack not resolved; post-unguided duration left at 0.");
            recognizedGameMode = false;
        }

        float packBaselineSeconds = totalTimeOfPostUnguidedVocalizationContent;

        if (!Mathf.Approximately(savasanaTimingAdjustment, 0f))
        {
            if (packBaselineSeconds <= 0f)
            {
                Debug.LogWarning(
                    "CSVLoader: Savasana timing adjustment "
                    + savasanaTimingAdjustment.ToString("+0.#;-0.#;0")
                    + " s skipped — pack post-unguided baseline is "
                    + packBaselineSeconds + " s.");
            }
            else
            {
                float before = packBaselineSeconds;
                totalTimeOfPostUnguidedVocalizationContent = Mathf.Max(0f, before + savasanaTimingAdjustment);
                Debug.Log(
                    "CSVLoader: Applied post-unguided timing adjustment (Short VO_ClosingGoodbye): "
                    + before + " s → " + totalTimeOfPostUnguidedVocalizationContent + " s ("
                    + savasanaTimingAdjustment.ToString("+0.#;-0.#;0") + " s). gameMode=\""
                    + gameMode + "\" IsFirstTimeUser=" + IsFirstTimeUser + ".");
                if (before > 0f && totalTimeOfPostUnguidedVocalizationContent <= 0f)
                {
                    Debug.LogWarning(
                        "CSVLoader: Post-unguided timing adjustment clamped duration to 0 (baseline was "
                        + before + " s, delta " + savasanaTimingAdjustment + " s).");
                }
            }
        }

        tracker.SetTotalTimeOfPostUnguidedVocalizationContent(totalTimeOfPostUnguidedVocalizationContent);

        if (totalTimeOfPostUnguidedVocalizationContent <= 0f)
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - totalTimeOfPostUnguidedVocalizationContent is 0. That can be intentional. StartCountdown \"ClosingDuration\" and \"Nm with savasana\" need a positive value when you use those variants.");
        }

        if (recognizedGameMode)
            tracker.MarkSessionTimingInitializedFromCsv();
        else
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - SessionTimingInitializedFromCsv not set (unrecognized gameMode or contentPack). Fix session_params so StartCountdown inputs are trustworthy.");

        Debug.Log(
            "CSVLoader: TimeLeftInitializations() - tracker inputs set. packBaseline="
            + packBaselineSeconds + " s totalTimeOfPostUnguidedVocalizationContent="
            + totalTimeOfPostUnguidedVocalizationContent + " s savasanaAdjustment="
            + savasanaTimingAdjustment.ToString("+0.#;-0.#;0")
            + " s. Session countdown is unchanged until StartCountdown → BeginCountdownPair (current [CountdownThisSection]="
            + tracker.CountdownThisSection + " [CountdownFull]=" + tracker.CountdownFull
            + "). SessionTimingInitializedFromCsv=" + tracker.SessionTimingInitializedFromCsv + ".");
    }

    public string GetCurrentMode()
    {
        return gameMode;
    }

    public string GetCurrentContentPack()
    {
        return contentPack;
    }
}
