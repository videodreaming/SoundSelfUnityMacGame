using UnityEngine;
using System.IO;
using System;
using UnityEngine.Serialization;

public class CSVLoader : MonoBehaviour
{
    public static CSVLoader instance { get; private set; }
    public Sequencer sequencer;

    public WwiseVOManager wwiseVOManager;

    /// <summary>One of: <see cref="GameModeSonoflore"/>, <see cref="GameModeActivation"/>, <see cref="GameModeAdjunctive"/>.</summary>
    public string gameMode { get; private set; }

    /// <summary>
    /// For <b>Sonoflore</b> / <b>Activation</b>: one of the six thematic content packs.
    /// For <b>Adjunctive</b>: <c>DualStage</c>, <c>OneStage</c>, or <c>Descending</c>.
    /// </summary>
    public string contentPack { get; private set; }

    public bool IsFirstTimeUser { get; private set; }
    public bool IsLayingDown { get; private set; }
    public bool IsVibroacoustic { get; private set; }
    public float timeToPlayClosingGoodbye;
    public float totalTimeOfPostUnguidedVocalizationContent;
    private bool layingDown = true;
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

    public const string ContentPackMindfulnessAndJoy = "Mindfulness and Joy";
    public const string ContentPackPsychologicalFlexibility = "Psychological Flexibility";
    public const string ContentPackSurrenderResponse = "Surrender Response";
    public const string ContentPackSelfCompassion = "Self Compassion";
    public const string ContentPackLovingKindness = "Loving Kindness";
    /// <summary>Full label including grief/appreciation framing.</summary>
    public const string ContentPackTransitionsGriefAndAppreciation = "Transitions (Grief and Appreciation)";

    public const string ContentPackDualStage = "DualStage";
    public const string ContentPackOneStage = "OneStage";
    public const string ContentPackDescending = "Descending";

    /// <summary>Legacy session / launcher labels that map to <see cref="GameModeSonoflore"/> (Preparation aliases and pre-Sonoflore &quot;Skills Training&quot;).</summary>
    public static bool IsLegacySonofloreLabel(string mode) =>
        string.Equals(mode, "Preparation", StringComparison.Ordinal) ||
        string.Equals(mode, "Preperation", StringComparison.Ordinal) ||
        string.Equals(mode, "Skills Training", StringComparison.OrdinalIgnoreCase);

    /// <summary>Legacy launcher label before Adjunctive rename; normalized to <see cref="GameModeAdjunctive"/>.</summary>
    public static bool IsLegacyAdjunctiveLabel(string mode) =>
        string.Equals(mode, "Protocol Stacks", StringComparison.OrdinalIgnoreCase);

    /// <summary>Legacy Adjunctive content-pack label before DualStage rename; normalized to <see cref="ContentPackDualStage"/>.</summary>
    public static bool IsLegacyDualStageContentPackLabel(string raw) =>
        string.Equals(raw, "Ascending", StringComparison.OrdinalIgnoreCase);

    /// <summary>Maps decrypted CSV labels to the three supported game modes.</summary>
    public static string NormalizeGameMode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        string t = raw.Trim();
        if (IsLegacySonofloreLabel(t))
            return GameModeSonoflore;
        if (t.Equals(GameModeSonoflore, StringComparison.OrdinalIgnoreCase))
            return GameModeSonoflore;
        if (t.Equals(GameModeActivation, StringComparison.OrdinalIgnoreCase))
            return GameModeActivation;
        // Legacy Hummingbird label before Activation rename
        if (string.Equals(t, "Integration", StringComparison.OrdinalIgnoreCase))
            return GameModeActivation;
        if (IsLegacyAdjunctiveLabel(t))
            return GameModeAdjunctive;
        if (t.Equals(GameModeAdjunctive, StringComparison.OrdinalIgnoreCase))
            return GameModeAdjunctive;
        return t;
    }

    /// <summary>Maps legacy sub-mode names to canonical <see cref="contentPack"/> strings.</summary>
    public static string NormalizeContentPack(string raw, string normalizedGameMode)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        string t = raw.Trim();

        if (normalizedGameMode == GameModeAdjunctive)
        {
            if (IsLegacyDualStageContentPackLabel(t))
                return ContentPackDualStage;
            if (t.Equals(ContentPackDualStage, StringComparison.OrdinalIgnoreCase))
                return ContentPackDualStage;
            if (t.Equals(ContentPackDescending, StringComparison.OrdinalIgnoreCase))
                return ContentPackDescending;
            if (t.Equals(ContentPackOneStage, StringComparison.OrdinalIgnoreCase))
                return ContentPackOneStage;
            return t;
        }

        // Sonoflore + Activation thematic packs (legacy + canonical)
        if (t.Equals(ContentPackMindfulnessAndJoy, StringComparison.Ordinal)
            || t.Equals("Peace", StringComparison.OrdinalIgnoreCase))
            return ContentPackMindfulnessAndJoy;

        if (t.Equals(ContentPackPsychologicalFlexibility, StringComparison.Ordinal)
            || t.Equals("Narrative", StringComparison.OrdinalIgnoreCase))
            return ContentPackPsychologicalFlexibility;

        if (t.Equals(ContentPackSurrenderResponse, StringComparison.Ordinal)
            || t.Equals("Surrender", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Psychedelic Preparation", StringComparison.OrdinalIgnoreCase))
            return ContentPackSurrenderResponse;

        if (t.Equals(ContentPackSelfCompassion, StringComparison.Ordinal)
            || t.Equals("Fireflies", StringComparison.OrdinalIgnoreCase))
            return ContentPackSelfCompassion;

        if (t.Equals(ContentPackLovingKindness, StringComparison.Ordinal)
            || t.Equals("Kindness", StringComparison.OrdinalIgnoreCase))
            return ContentPackLovingKindness;

        if (t.Equals(ContentPackTransitionsGriefAndAppreciation, StringComparison.Ordinal)
            || t.Equals("Metta", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Transitions", StringComparison.OrdinalIgnoreCase))
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
        VOInitializations();
        TimeLeftInitializations();
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

    private void VOInitializations()
    {
        if (wwiseVOManager == null)
        {
            Debug.LogError("CSVLoader: VOInitializations() - wwiseVOManager is null! Cannot initialize VO.");
            return;
        }

        if (gameMode == GameModeSonoflore)
        {
            if (contentPack == ContentPackMindfulnessAndJoy)
                wwiseVOManager.SetToPeace();
            else if (contentPack == ContentPackPsychologicalFlexibility)
                wwiseVOManager.SetToNarrative();
            else if (contentPack == ContentPackSurrenderResponse)
                wwiseVOManager.SetToSurrender();
            else
                Debug.LogWarning("CSVLoader: VOInitializations() - Unknown contentPack '" + contentPack + "' for gameMode '" + gameMode + "'. VO content not set.");

            if (IsFirstTimeUser)
                wwiseVOManager.firstTimeUser();
            else
                wwiseVOManager.notFirstTimeUser();
        }
        else if (gameMode == GameModeActivation)
        {
            wwiseVOManager.notFirstTimeUser();

            if (contentPack == ContentPackSelfCompassion)
                wwiseVOManager.SetToFireflies();
            else if (contentPack == ContentPackLovingKindness)
                wwiseVOManager.SetToKindness();
            else if (contentPack == ContentPackTransitionsGriefAndAppreciation)
                wwiseVOManager.SetToMetta();
            else
                Debug.LogWarning("CSVLoader: VOInitializations() - Unknown contentPack '" + contentPack + "' for gameMode '" + gameMode + "'. VO content not set.");
        }
        else if (gameMode == GameModeAdjunctive)
        {
            wwiseVOManager.notFirstTimeUser();

            if (contentPack == ContentPackDualStage)
                wwiseVOManager.SetToEsketamineAscending();
            else if (contentPack == ContentPackDescending)
                wwiseVOManager.SetToEsketamineDescending();
            else if (contentPack == ContentPackOneStage)
            {
                // TODO: Wire Adjunctive OneStage VO / Wwise switches once sequence and audio path are defined.
            }
            else
                Debug.LogWarning("CSVLoader: VOInitializations() - Unknown contentPack '" + contentPack + "' for gameMode '" + gameMode + "'. Expected DualStage, OneStage, or Descending.");
        }
        else
        {
            Debug.LogWarning("CSVLoader: VOInitializations() - Unknown gameMode '" + gameMode + "'. No VO initialization performed.");
        }
    }

    /// <summary>
    /// Phase 4: Hydrates <see cref="TimeTrackerScript"/> inputs only (e.g. post-unguided duration).
    /// Session countdown value and ticking start only when the sequence runs the StartCountdown stage (<see cref="TimeTrackerScript.BeginCountdownPair"/>).
    /// </summary>
    private void TimeLeftInitializations()
    {
        if (TimeTrackerScript.instance == null)
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - TimeTrackerScript.instance is null. Timing behaviors will not work properly.");
            return;
        }

        var tracker = TimeTrackerScript.instance;
        totalTimeOfPostUnguidedVocalizationContent = 0f;
        bool recognizedGameMode = true;

        if (gameMode == GameModeSonoflore)
        {
            if (contentPack == ContentPackMindfulnessAndJoy)
                totalTimeOfPostUnguidedVocalizationContent = (14.0f * 60.0f) + 0.0f;
            else if (contentPack == ContentPackPsychologicalFlexibility)
                totalTimeOfPostUnguidedVocalizationContent = 900.0f;
            else if (contentPack == ContentPackSurrenderResponse)
                totalTimeOfPostUnguidedVocalizationContent = (12.0f * 60.0f) + 06.0f;
            else
            {
                Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Unknown contentPack '" + contentPack + "' for gameMode '" + gameMode + "'. totalTimeOfPostUnguidedVocalizationContent not set.");
                recognizedGameMode = false;
            }
        }
        else if (gameMode == GameModeActivation)
        {
            if (contentPack == ContentPackSelfCompassion)
                totalTimeOfPostUnguidedVocalizationContent = 415.0f;
            else if (contentPack == ContentPackLovingKindness)
                totalTimeOfPostUnguidedVocalizationContent = 349.0f;
            else if (contentPack == ContentPackTransitionsGriefAndAppreciation)
                totalTimeOfPostUnguidedVocalizationContent = 597.0f;
            else
            {
                Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Unknown contentPack '" + contentPack + "' for gameMode '" + gameMode + "'. totalTimeOfPostUnguidedVocalizationContent not set.");
                recognizedGameMode = false;
            }
        }
        else if (gameMode == GameModeAdjunctive)
        {
            if (contentPack == ContentPackDualStage || contentPack == ContentPackDescending)
                totalTimeOfPostUnguidedVocalizationContent = 900.0f;
            else if (contentPack == ContentPackOneStage)
            {
                // TODO: Set duration when OneStage sequence timing is defined (currently matches DualStage as placeholder).
                totalTimeOfPostUnguidedVocalizationContent = 900.0f;
            }
            else
            {
                Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Unknown contentPack '" + contentPack + "' for gameMode '" + gameMode + "'. totalTimeOfPostUnguidedVocalizationContent not set.");
                recognizedGameMode = false;
            }
        }
        else
        {
            Debug.LogWarning("CSVLoader: TimeLeftInitializations() - Unknown gameMode '" + gameMode + "'. Post-unguided duration left at 0.");
            recognizedGameMode = false;
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

        Debug.Log("CSVLoader: TimeLeftInitializations() - tracker inputs set. totalTimeOfPostUnguidedVocalizationContent=" + totalTimeOfPostUnguidedVocalizationContent + " s. Session countdown is unchanged until StartCountdown → BeginCountdownPair (current [CountdownThisSection]=" + tracker.CountdownThisSection + " [CountdownFull]=" + tracker.CountdownFull + "). SessionTimingInitializedFromCsv=" + tracker.SessionTimingInitializedFromCsv + ".");
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
