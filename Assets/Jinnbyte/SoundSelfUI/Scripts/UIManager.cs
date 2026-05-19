using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum CalibrationUI
{
    Start,
    Headphone,
    Microphone,
    VibroAcoustic,
    LightGlasses,
    Conclusion
}

/// <summary>Roots managed by <see cref="UIManager"/> choice-menu navigation stack (<see cref="UIManager.SetChoiceScreen"/>, <see cref="UIManager.ChoiceMenuBackButtonPress"/>). Calibration flows use <see cref="CalibrationUI"/> and <see cref="UIManager.SetCalibrationScreen"/> instead — see <see cref="SoundSelf.Sequence.CalibrationStageHandler"/>.</summary>
public enum ChoiceMenuScreen
{
    None = 0,
    ChoiceSsOrMusic = 1,
    ChoiceSonofloreMusicLength = 2,
    ChoiceAlbum = 3,
}

/// <summary>Target for <see cref="UIManager.SetChoiceScreen"/> (Inspector buttons, sequence handlers, forward navigation).</summary>
public enum ChoiceScreen
{
    SsOrMusic = 0,
    SonofloreMusicLength = 1,
    SonofloreMusicLengthFromSsOrMusic = 2,
    Album = 3,
}

/// <summary>Single visible row under Meditation Session — Headers (fades via <see cref="ScreenFadeEffect"/> when present).</summary>
public enum SessionSectionHeaderKind
{
    None = 0,
    OpeningMeditation = 1,
    Tutorial = 2,
    Playground = 3,
    Music = 4,
    Savasana = 5,
}

/// <summary>Dual-stage label above the section header; at most one of Stage1 / Stage2.</summary>
public enum SessionDualStageBannerKind
{
    None = 0,
    Stage1 = 1,
    Stage2 = 2,
}

/// <summary>Cooldown after any accepted button press, and minimum idle time after a screen with buttons becomes visible.</summary>
public static class UIManagerTiming
{
    public const float ButtonInteractionCooldownSeconds = 0.333f;
    /// <summary>Calibration mic level text refresh rate (Hz).</summary>
    public const float CalibrationMicLevelTextUpdatesPerSecond = 4f;
    public const float LocalTimeTextUpdateIntervalSeconds = 10f;
}

/// <summary>
/// Session UI: battery/timer, screen roots with <see cref="ScreenFadeEffect"/>, and button debounce.
/// <para><b>Choice menus</b> (<see cref="ChoiceScreen"/>, <see cref="ChoiceMenuScreen"/>): use <see cref="SetChoiceScreen"/>; forward navigation can push a return target onto <c>_choiceMenuBackStack</c>; Back uses <see cref="ChoiceMenuBackButtonPress"/>. <see cref="ChoiceScreen.SsOrMusic"/> arms a one-shot back anchor so child screens still record SS/Music even when that screen is not yet <c>activeSelf</c> during fades.</para>
/// <para><b>Session skip</b>: <see cref="EnableSkipButton"/> + <see cref="SkipButtonPress"/> → <see cref="OnSkipSessionButtonPress"/> (Sequencer invokes the current stage handler skip hook, then sequence command EndThisSequenceStage).</para>
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private Battery battery;
    [SerializeField] private Timer sessionStartTimer;
    [FormerlySerializedAs("microphoneStatusText")]
    [SerializeField] private Text micLevelText;
    [SerializeField] private Text headphoneStatusText;
    [SerializeField] private Text versionText;
    [Tooltip("Local system time, e.g. 11:11 am, CST — refreshed every 10 seconds.")]
    [SerializeField] private Text localTimeText;
    [SerializeField] private GameObject calibrationHeadText; // Set by CalibrationStageHandler per step; not all steps have instructions, so optional assignment.
    [SerializeField] private Sequencer sequencer;


    //Screens
    [Header("Choice Album")]
    [SerializeField] private GameObject choiceAlbumScreen;
    [Header("Choice SS or Music — dual-stage (assign Sequencer + copy/button roots)")]
    [SerializeField] private GameObject choiceSSOrMusicScreen;
    [SerializeField] private GameObject choiceSsOrMusicStage1Header;
    [SerializeField] private GameObject choiceSsOrMusicStage1Description;
    [SerializeField] private GameObject choiceSsOrMusicStage2Header;
    [SerializeField] private GameObject choiceSsOrMusicStage2Description;
    [SerializeField] private Button choiceSsOrMusicPlaySoundSelfButton;
    [SerializeField] private Button choiceSsOrMusicPlayAlbumButton;
    [SerializeField] private GameObject choiceSonofloreMusicLengthScreen;
    [SerializeField] private GameObject welcomeScreen;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject conclusionScreen;
    [SerializeField] private GameObject headphoneScreen; //calibration screens
    [SerializeField] private GameObject microphoneScreen; //calibration screens
    [Header("Calibration — voice test (microphone + vibroacoustic)")]
    [Tooltip("Assign the Microphone_Test_Card root. Mic level updates only while this object is active during the Microphone calibration step. If unset, any active Microphone calibration screen shows the level.")]
    [SerializeField] private GameObject calibrationMicrophoneTestCard;
    [Tooltip("Shows YES/NO from ImitoneVoiceIntepreter.toneActive during Microphone and Vibroacoustic calibration steps.")]
    [SerializeField] private Text micToneOnText;
    [SerializeField] private GameObject vibroAcousticScreen; //calibration screens
    [SerializeField] private GameObject lightGlassesScreen; // Section Calibration Lightglasses root
    [SerializeField] private GameObject startMeditationScreen;
    [SerializeField] private GameObject endMeditationScreen;

    [Header("Meditation session — skip (assign Skip Button root; add ScreenFadeEffect on same object for session fades)")]
    [SerializeField] private GameObject skipSessionButton;
    [SerializeField] private Text skipSessionButtonText;

    [Header("Meditation session — section headers (each row: CanvasGroup + ScreenFadeEffect, under Headers)")]
    [SerializeField] private GameObject sessionHeaderOpeningMeditation;
    [SerializeField] private GameObject sessionHeaderTutorial;
    [SerializeField] private GameObject sessionHeaderPlayground;
    [SerializeField] private GameObject sessionHeaderMusic;
    [SerializeField] private GameObject sessionHeaderSavasana;
    [SerializeField] private GameObject sessionHeaderStage1;
    [SerializeField] private GameObject sessionHeaderStage2;

    [Header("Calibration — progress indicators (one dot + one line per step position; line shows for the active step). Slots are indexed by position in the active variant's step list (variant-agnostic), not by CalibrationUI enum value.")]
    [SerializeField] private GameObject[] calibrationProgressDots = new GameObject[6];
    [SerializeField] private GameObject[] calibrationProgressLines = new GameObject[6];

    /// <summary>When moving forward between <see cref="ChoiceMenuScreen"/> roots, we push the parent so <see cref="ChoiceMenuBackButtonPress"/> can restore it. Not used for calibration — see <see cref="OnCalibrationBackPress"/> / <see cref="SoundSelf.Sequence.CalibrationStageHandler"/>.</summary>
    private readonly Stack<ChoiceMenuScreen> _choiceMenuBackStack = new Stack<ChoiceMenuScreen>();

    private SessionSectionHeaderKind _activeSessionSectionHeader = SessionSectionHeaderKind.None;
    private SessionDualStageBannerKind _activeDualStageBanner = SessionDualStageBannerKind.None;
    private int _sessionSectionHeaderTransitionToken;
    private int _sessionDualBannerTransitionToken;

    private bool _skipButtonSuppressTextChanges;
    private bool _skipSessionButtonFadeOutPending;

    private CalibrationUI _activeCalibrationUi = CalibrationUI.Start;
    private bool _calibrationVoiceTestUiWasActive;
    private float _nextCalibrationVoiceTestUiUpdateTime;
    private bool? _micToneOnTextLastToneOn;

    private static readonly Color CalibrationMicToneYesColor = new Color(246f / 255f, 255f / 255f, 177f / 255f, 1f); // #F6FFB1
    private static readonly Color CalibrationMicToneNoColor = new Color(79f / 255f, 94f / 255f, 97f / 255f, 1f);   // #4F5E61
    private static readonly Color CalibrationMicLevelLowColor = new Color(96f / 255f, 114f / 255f, 126f / 255f, 1f);
    private static readonly Color CalibrationMicLevelHighColor = new Color(189f / 255f, 245f / 255f, 255f / 255f, 1f);
    private const float CalibrationMicLevelColorMinPercent = 10f;
    private const float CalibrationMicLevelColorMaxPercent = 60f;

    /// <summary>Set when SS/Music choice UI is shown (<see cref="ShowChoiceSsOrMusicScreenCore"/>); cleared by <see cref="ClearChoiceMenuNavigationStack"/>. Lets child choice screens push SS/Music as Back target even if fades mean SS is not yet <see cref="GameObject.activeSelf"/>.</summary>
    private bool _pendingChoiceBackToSsOrMusic;

    public Action OnQuit;
    public Action OnStartSoundSelfPress;
    public Action OnEndThisSequenceStagePress;
    public Action OnPlayMusicPress;
    /// <summary>Generic Next Step during calibration; <see cref="CalibrationStageHandler"/> advances by variant step list.</summary>
    public Action OnCalibrationNextStepPress;
    /// <summary>Back one calibration step; no-op on <see cref="CalibrationUI.Start"/>.</summary>
    public Action OnCalibrationBackPress;
    /// <summary>Conclusion screen only — confirms finishing calibration; handler may wait for <c>Cue_Calibration_Instruction_OFF</c> before <c>MarkComplete</c> when VO is in progress.</summary>
    public Action OnCalibrationConclusionConfirmPress;
    public Action OnHeadphoneTroubleshootingPress;
    public Action OnSkipSessionButtonPress;
    public Action OnMeditationQuitPress;
    public Action OnSessionTimeEnds;

    // Screen Set Functions
    public void UnsetAllScreens(Action onComplete, bool keepCalibrationHead = false)
    {
        FadeOutScreen(() =>
        {
            choiceSSOrMusicScreen.SetActive(false);
            choiceSonofloreMusicLengthScreen.SetActive(false);
            if (choiceAlbumScreen != null)
                choiceAlbumScreen.SetActive(false);
            welcomeScreen.SetActive(false);
            startScreen.SetActive(false);
            conclusionScreen.SetActive(false);
            headphoneScreen.SetActive(false);
            microphoneScreen.SetActive(false);
            vibroAcousticScreen.SetActive(false);
            lightGlassesScreen.SetActive(false);
            startMeditationScreen.SetActive(false);
            endMeditationScreen.SetActive(false);
            onComplete?.Invoke();
        }, keepCalibrationHead);
    }

    void FadeOutScreen(Action onComplete, bool keepCalibrationHead = false)
    {
        //check which screen was active and call fade out on it
        if (choiceSSOrMusicScreen.activeSelf)
        {
            choiceSSOrMusicScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (welcomeScreen.activeSelf)
        {
            welcomeScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (choiceSonofloreMusicLengthScreen != null && choiceSonofloreMusicLengthScreen.activeSelf)
        {
            choiceSonofloreMusicLengthScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (choiceAlbumScreen != null && choiceAlbumScreen.activeSelf)
        {
            choiceAlbumScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (startScreen != null && startScreen.activeSelf)
        {
            if (!keepCalibrationHead)
                FadeOutCalibrationHead();
            startScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (conclusionScreen != null && conclusionScreen.activeSelf)
        {
            if (!keepCalibrationHead)
                FadeOutCalibrationHead();
            conclusionScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (headphoneScreen.activeSelf)
        {
            headphoneScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (microphoneScreen.activeSelf)
        {
            microphoneScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (vibroAcousticScreen.activeSelf)
        {
            vibroAcousticScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (lightGlassesScreen.activeSelf)
        {
            lightGlassesScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (startMeditationScreen.activeSelf)
        {
            startMeditationScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (endMeditationScreen.activeSelf)
        {
            endMeditationScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else
        {
            //no screen was active, just call onComplete immediately
            onComplete?.Invoke();
        }
    }
    /// <summary>Calibration step UI only — uses <see cref="OnCalibrationBackPress"/> for Back, not the choice-menu stack. Clears the choice stack so stale Back targets are not kept when leaving choice flows.</summary>
    public void SetCalibrationScreen(CalibrationUI screen)
    {
        // Choice stack is unrelated to calibration (see class summary); reset so Back on a later choice visit does not use stale targets.
        ClearChoiceMenuNavigationStack();
        _activeCalibrationUi = screen;
        UnsetAllScreens(() =>
        {
            switch (screen)
            {
                case CalibrationUI.Start:
                    if (startScreen != null)
                    {
                        startScreen.SetActive(true);
                        FadeInCalibrationHead();
                    }
                    else
                        Debug.LogError("UIManager.SetCalibrationScreen(Start): startScreen is not assigned. Assign Section Calibration Start in the inspector.");
                    break;
                case CalibrationUI.Headphone:
                    headphoneScreen.SetActive(true);
                    break;
                case CalibrationUI.Microphone:
                    microphoneScreen.SetActive(true);
                    break;
                case CalibrationUI.VibroAcoustic:
                    vibroAcousticScreen.SetActive(true);
                    break;
                case CalibrationUI.LightGlasses:
                    lightGlassesScreen.SetActive(true);
                    break;
                case CalibrationUI.Conclusion:
                    if (conclusionScreen != null)
                        conclusionScreen.SetActive(true);
                    else
                        Debug.LogError("UIManager.SetCalibrationScreen(Conclusion): conclusionScreen is not assigned. Assign Section Calibration Conclusion in the inspector.");
                    break;
            }
            if (screen == CalibrationUI.Start)
                EnableSkipButton(true, "Skip Calibration (Not Recommended)");
            else
                EnableSkipButton(false, null);
            ArmButtonInteractionCooldown();
        }, keepCalibrationHead: true);
    }

    /// <summary>Shows the in-session HUD (Meditation Session — Start). No-op if that root is already active (idempotent).</summary>
    public void SetMeditationScreen()
    {
        ClearChoiceMenuNavigationStack();
        if (startMeditationScreen != null && startMeditationScreen.activeSelf)
            return;
        UnsetAllScreens(() =>
        {
            startMeditationScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }
    /// <summary>Shows the session-end HUD (Meditation Session — End). No-op if that root is already active (idempotent).</summary>
    public void SetEndMeditationScreen()
    {
        ClearMeditationSessionSectionHeaders();
        ClearChoiceMenuNavigationStack();
        if (endMeditationScreen != null && endMeditationScreen.activeSelf)
            return;
        UnsetAllScreens(() =>
        {
            endMeditationScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }

    /// <summary>Fades out section + dual-stage headers (e.g. when leaving the in-session HUD).</summary>
    public void ClearMeditationSessionSectionHeaders()
    {
        SetSessionSectionHeader(SessionSectionHeaderKind.None);
        SetSessionDualStageBanner(SessionDualStageBannerKind.None);
    }

    /// <summary>Shows one section header row (Opening / Tutorial / …); fades the previous row out first when <see cref="ScreenFadeEffect"/> is present.</summary>
    public void SetSessionSectionHeader(SessionSectionHeaderKind next)
    {
        GameObject nextGo = GetSessionSectionHeaderObject(next);
        if (next != SessionSectionHeaderKind.None && nextGo == null)
        {
            Debug.LogWarning($"UIManager.SetSessionSectionHeader({next}): header GameObject is not assigned in the inspector.");
            return;
        }

        if (next == _activeSessionSectionHeader)
        {
            if (next == SessionSectionHeaderKind.None)
                return;
            if (nextGo != null && nextGo.activeSelf)
                return;
        }

        int token = ++_sessionSectionHeaderTransitionToken;
        GameObject prevGo = GetSessionSectionHeaderObject(_activeSessionSectionHeader);

        void ActivateNextAndSetState()
        {
            if (token != _sessionSectionHeaderTransitionToken)
                return;
            if (prevGo != null)
                prevGo.SetActive(false);
            _activeSessionSectionHeader = next;
            if (nextGo != null)
                nextGo.SetActive(true);
        }

        if (prevGo != null && prevGo.activeSelf && prevGo != nextGo)
            FadeOutSessionHeaderObject(prevGo, ActivateNextAndSetState);
        else
            ActivateNextAndSetState();
    }

    /// <summary>Shows Stage1 or Stage2 banner above the section header, or neither. Independent of <see cref="SetSessionSectionHeader"/>.</summary>
    public void SetSessionDualStageBanner(SessionDualStageBannerKind next)
    {
        GameObject nextGo = GetDualStageBannerObject(next);
        if (next != SessionDualStageBannerKind.None && nextGo == null)
        {
            Debug.LogWarning($"UIManager.SetSessionDualStageBanner({next}): banner GameObject is not assigned in the inspector.");
            return;
        }

        if (next == _activeDualStageBanner)
        {
            if (next == SessionDualStageBannerKind.None)
                return;
            if (nextGo != null && nextGo.activeSelf)
                return;
        }

        int token = ++_sessionDualBannerTransitionToken;
        GameObject prevGo = GetDualStageBannerObject(_activeDualStageBanner);

        void ActivateNextAndSetState()
        {
            if (token != _sessionDualBannerTransitionToken)
                return;
            if (prevGo != null)
                prevGo.SetActive(false);
            _activeDualStageBanner = next;
            if (nextGo != null)
                nextGo.SetActive(true);
        }

        if (prevGo != null && prevGo.activeSelf && prevGo != nextGo)
            FadeOutSessionHeaderObject(prevGo, ActivateNextAndSetState);
        else
            ActivateNextAndSetState();
    }

    /// <summary>Maps <see cref="Sequencer.dualstageStage"/> to Stage1 / Stage2 / none (only 1 and 2 show a banner).</summary>
    public void RefreshSessionDualStageBannerFromSequencer(Sequencer sequencer)
    {
        if (sequencer == null)
        {
            SetSessionDualStageBanner(SessionDualStageBannerKind.None);
            return;
        }

        switch (sequencer.dualstageStage)
        {
            case 1:
                SetSessionDualStageBanner(SessionDualStageBannerKind.Stage1);
                break;
            case 2:
                SetSessionDualStageBanner(SessionDualStageBannerKind.Stage2);
                break;
            default:
                SetSessionDualStageBanner(SessionDualStageBannerKind.None);
                break;
        }
    }

    private GameObject GetSessionSectionHeaderObject(SessionSectionHeaderKind kind)
    {
        switch (kind)
        {
            case SessionSectionHeaderKind.OpeningMeditation:
                return sessionHeaderOpeningMeditation;
            case SessionSectionHeaderKind.Tutorial:
                return sessionHeaderTutorial;
            case SessionSectionHeaderKind.Playground:
                return sessionHeaderPlayground;
            case SessionSectionHeaderKind.Music:
                return sessionHeaderMusic;
            case SessionSectionHeaderKind.Savasana:
                return sessionHeaderSavasana;
            default:
                return null;
        }
    }

    private GameObject GetDualStageBannerObject(SessionDualStageBannerKind kind)
    {
        switch (kind)
        {
            case SessionDualStageBannerKind.Stage1:
                return sessionHeaderStage1;
            case SessionDualStageBannerKind.Stage2:
                return sessionHeaderStage2;
            default:
                return null;
        }
    }

    private static void FadeOutSessionHeaderObject(GameObject go, Action onComplete)
    {
        if (go == null || !go.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        var fade = go.GetComponent<ScreenFadeEffect>();
        if (fade != null)
            fade.FadeOut(onComplete);
        else
        {
            go.SetActive(false);
            onComplete?.Invoke();
        }
    }

    /// <summary>Shows a choice-menu root (<see cref="ChoiceScreen"/>). Use from sequence handlers and Inspector wiring.</summary>
    /// <param name="secondStageVariant">Only used for <see cref="ChoiceScreen.SsOrMusic"/> — second dual-stage visit layout.</param>
    /// <param name="clearNavigationStack">When true, clears the choice Back stack before showing (e.g. <see cref="SoundSelf.Sequence.StageVariant.Menu_AlbumChoice"/> stage entry).</param>
    public void SetChoiceScreen(ChoiceScreen screen, bool secondStageVariant = false, bool clearNavigationStack = false)
    {
        Debug.Log($"[UIManager][Choice] SetChoiceScreen({screen}, secondStageVariant={secondStageVariant}, clearNavigationStack={clearNavigationStack})");
        if (clearNavigationStack)
            ClearChoiceMenuNavigationStack();
        switch (screen)
        {
            case ChoiceScreen.SsOrMusic:
                ShowChoiceSsOrMusicScreenCore(secondStageVariant);
                break;

            case ChoiceScreen.SonofloreMusicLength:
                if (choiceSonofloreMusicLengthScreen != null && choiceSonofloreMusicLengthScreen.activeSelf)
                    return;
                if (ShouldRecordChoiceBackToSsOrMusic())
                    _choiceMenuBackStack.Push(ChoiceMenuScreen.ChoiceSsOrMusic);
                ShowChoiceSonofloreMusicLengthScreenCore();
                break;

            case ChoiceScreen.SonofloreMusicLengthFromSsOrMusic:
                _pendingChoiceBackToSsOrMusic = false;
                _choiceMenuBackStack.Push(ChoiceMenuScreen.ChoiceSsOrMusic);
                ShowChoiceSonofloreMusicLengthScreenCore();
                break;

            case ChoiceScreen.Album:
                if (choiceAlbumScreen != null && choiceAlbumScreen.activeSelf)
                    return;
                if (ShouldRecordChoiceBackToSsOrMusic())
                    _choiceMenuBackStack.Push(ChoiceMenuScreen.ChoiceSsOrMusic);
                ShowChoiceAlbumScreenCore();
                break;

            default:
                Debug.LogWarning($"[UIManager][Choice] SetChoiceScreen: unhandled {screen}; opening SS or Music.");
                ShowChoiceSsOrMusicScreenCore(secondStageVariant);
                break;
        }
    }

    /// <summary>Shows Choice SS or Music from sequence (e.g. SetMenu). When <paramref name="secondStageVariant"/> is true (between-segment visit: <c>dualstageStage==1</c> after first choice), shows stage-2 copy and hides the disallowed choice via <see cref="GameObject.SetActive"/>.</summary>
    /// <param name="secondStageVariant">False: initial choice (<c>dualstageStage==0</c>). True: second visit — only Music if <see cref="Sequencer.dualstageSecondStageIsMusic"/>, only SoundSelf if <see cref="Sequencer.dualstageSecondStageIsSoundSelf"/> (set from first-choice button wiring).</param>
    public void SetChoiceSSOrMusicScreen(bool secondStageVariant = false) =>
        SetChoiceScreen(ChoiceScreen.SsOrMusic, secondStageVariant);

    /// <summary>Shows Choice Album (Sonoflore album length). Sequence entry: <see cref="SoundSelf.Sequence.StageVariant.Menu_AlbumChoice"/>.</summary>
    public void SetChoiceAlbumScreen() => SetChoiceScreen(ChoiceScreen.Album);

    /// <summary>Shows Welcome; clears choice back-stack.</summary>
    public void SetWelcomeScreen()
    {
        ClearChoiceMenuNavigationStack();
        UnsetAllScreens(() =>
        {
            welcomeScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }

    /// <summary>
    /// Shows Choice Sonoflore Music Length (dual-stage length). If coming from SS/Music (screen active or <see cref="SetChoiceSSOrMusicScreen"/> just ran), pushes SS/Music so <see cref="ChoiceMenuBackButtonPress"/> works.
    /// Prefer <see cref="ChoiceScreen.SonofloreMusicLengthFromSsOrMusic"/> when wiring forward navigation explicitly.
    /// </summary>
    public void SetChoiceSonofloreMusicLengthScreen() => SetChoiceScreen(ChoiceScreen.SonofloreMusicLength);

    /// <summary>
    /// Forward navigation: from Choice SS or Music to Choice Sonoflore Music Length, recording SS as the Back target (clears the one-shot pending flag from <see cref="SetChoiceSSOrMusicScreen"/>).
    /// </summary>
    public void NavigateChoiceMenuToSonofloreMusicLengthFromSsOrMusic() =>
        SetChoiceScreen(ChoiceScreen.SonofloreMusicLengthFromSsOrMusic);

    private bool ShouldRecordChoiceBackToSsOrMusic() =>
        (choiceSSOrMusicScreen != null && choiceSSOrMusicScreen.activeSelf)
        || _pendingChoiceBackToSsOrMusic;

    private void ClearChoiceMenuNavigationStack()
    {
        _choiceMenuBackStack.Clear();
        _pendingChoiceBackToSsOrMusic = false;
    }

    private void ShowChoiceSsOrMusicScreenCore(bool secondStageVariant = false)
    {
        Debug.Log($"[UIManager][ChoiceSsOrMusic] ShowChoiceSsOrMusicScreenCore(secondStageVariant={secondStageVariant})");
        UnsetAllScreens(() =>
        {
            choiceSSOrMusicScreen.SetActive(true);
            _pendingChoiceBackToSsOrMusic = true;
            ApplyChoiceSsOrMusicDualStagePresentation(secondStageVariant);
            ArmButtonInteractionCooldown();
        });
    }

    /// <summary>Inspector reference, with scene fallback so choice buttons still gate if the field was left empty.</summary>
    private Sequencer ResolveSequencerForDualStageChoiceUi(bool logSource)
    {
        if (sequencer != null)
        {
            if (logSource)
                Debug.Log("[UIManager][ChoiceSsOrMusic] ResolveSequencer: using inspector-assigned UIManager.sequencer.");
            return sequencer;
        }

        var found = FindObjectOfType<Sequencer>();
        if (logSource)
        {
            if (found != null)
                Debug.Log($"[UIManager][ChoiceSsOrMusic] ResolveSequencer: UIManager.sequencer was null; using FindObjectOfType → \"{found.name}\".");
            else
                Debug.LogWarning("[UIManager][ChoiceSsOrMusic] ResolveSequencer: no inspector reference and FindObjectOfType<Sequencer> returned null.");
        }
        return found;
    }

    /// <summary>True when the SS/Music menu should use the second-visit layout (copy + single allowed modality). Uses <see cref="Sequencer.dualstageStage"/>.</summary>
    private bool ResolveChoiceSsOrMusicSecondStageVariant()
    {
        var seq = ResolveSequencerForDualStageChoiceUi(logSource: true);
        if (seq == null)
        {
            Debug.Log("[UIManager][ChoiceSsOrMusic] ResolveChoiceSsOrMusicSecondStageVariant: no Sequencer → false (first-visit layout).");
            return false;
        }

        bool result = seq.dualstageStage >= 1;
        Debug.Log(
            $"[UIManager][ChoiceSsOrMusic] ResolveChoiceSsOrMusicSecondStageVariant: dualstageStage={seq.dualstageStage} " +
            $"(>=1) → secondStageVariant={result}");
        return result;
    }

    /// <summary>Stage-1 vs stage-2 header/description roots; on second visit, shows only the allowed modality via <see cref="GameObject.SetActive"/> on each choice <see cref="Button"/> root.</summary>
    private void ApplyChoiceSsOrMusicDualStagePresentation(bool secondStageVariant)
    {
        const string L = "[UIManager][ChoiceSsOrMusic]";
        Debug.Log(
            $"{L} ApplyChoiceSsOrMusicDualStagePresentation: secondStageVariant={secondStageVariant}; " +
            $"copyRoots stage1H={choiceSsOrMusicStage1Header != null} stage1D={choiceSsOrMusicStage1Description != null} " +
            $"stage2H={choiceSsOrMusicStage2Header != null} stage2D={choiceSsOrMusicStage2Description != null}");

        if (choiceSsOrMusicStage1Header != null)
            choiceSsOrMusicStage1Header.SetActive(!secondStageVariant);
        if (choiceSsOrMusicStage1Description != null)
            choiceSsOrMusicStage1Description.SetActive(!secondStageVariant);
        if (choiceSsOrMusicStage2Header != null)
            choiceSsOrMusicStage2Header.SetActive(secondStageVariant);
        if (choiceSsOrMusicStage2Description != null)
            choiceSsOrMusicStage2Description.SetActive(secondStageVariant);

        if (!secondStageVariant)
        {
            Debug.Log($"{L} First-visit mode: both choice buttons active (visible).");
            SetChoiceSsOrMusicChoiceButtonActive(choiceSsOrMusicPlaySoundSelfButton, true, "PlaySoundSelf");
            SetChoiceSsOrMusicChoiceButtonActive(choiceSsOrMusicPlayAlbumButton, true, "PlayAlbum");
            return;
        }

        var seq = ResolveSequencerForDualStageChoiceUi(logSource: true);
        if (seq == null)
        {
            Debug.LogWarning($"{L} secondStageVariant but no Sequencer — showing both choice buttons. Assign UIManager.sequencer or add a Sequencer to the scene.");
            SetChoiceSsOrMusicChoiceButtonActive(choiceSsOrMusicPlaySoundSelfButton, true, "PlaySoundSelf");
            SetChoiceSsOrMusicChoiceButtonActive(choiceSsOrMusicPlayAlbumButton, true, "PlayAlbum");
            return;
        }

        bool music = seq.dualstageSecondStageIsMusic;
        bool soundSelf = seq.dualstageSecondStageIsSoundSelf;
        Debug.Log($"{L} Sequencer dual-stage flags: dualstageStage={seq.dualstageStage} dualstageSecondStageIsMusic={music} dualstageSecondStageIsSoundSelf={soundSelf}");

        if (music == soundSelf)
        {
            Debug.LogWarning(
                $"{L} Ambiguous flags (music==soundSelf=={music}); cannot pick a single allowed modality — showing both buttons. " +
                "Check first-choice button order (SetDualstage* vs IncrementDualstageStage) and onlyAllowOnStage1 wiring.");
            SetChoiceSsOrMusicChoiceButtonActive(choiceSsOrMusicPlaySoundSelfButton, true, "PlaySoundSelf");
            SetChoiceSsOrMusicChoiceButtonActive(choiceSsOrMusicPlayAlbumButton, true, "PlayAlbum");
            return;
        }

        Debug.Log($"{L} Gating visibility: PlaySoundSelf active={soundSelf}, PlayAlbum active={music} (allowed modality for this visit).");
        SetChoiceSsOrMusicChoiceButtonActive(choiceSsOrMusicPlaySoundSelfButton, soundSelf, "PlaySoundSelf");
        SetChoiceSsOrMusicChoiceButtonActive(choiceSsOrMusicPlayAlbumButton, music, "PlayAlbum");
    }

    /// <summary>Shows or hides a choice button root (<see cref="GameObject.SetActive"/>). Assign full button objects in the inspector.</summary>
    private void SetChoiceSsOrMusicChoiceButtonActive(Button button, bool active, string choiceDebugRole)
    {
        const string L = "[UIManager][ChoiceSsOrMusic]";
        if (button == null)
        {
            Debug.Log($"{L} '{choiceDebugRole}': Button reference is null — cannot SetActive({active}).");
            return;
        }

        button.gameObject.SetActive(active);
        if (active)
            button.interactable = true;
        Debug.Log($"{L} '{choiceDebugRole}' on \"{button.gameObject.name}\": SetActive({active})");
    }

    private void ShowChoiceSonofloreMusicLengthScreenCore()
    {
        if (choiceSonofloreMusicLengthScreen == null)
        {
            Debug.LogError("UIManager.ShowChoiceSonofloreMusicLengthScreenCore: choiceSonofloreMusicLengthScreen is not assigned.");
            return;
        }
        UnsetAllScreens(() =>
        {
            choiceSonofloreMusicLengthScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }

    private void ShowChoiceAlbumScreenCore()
    {
        if (choiceAlbumScreen == null)
        {
            Debug.LogError("UIManager.ShowChoiceAlbumScreenCore: choiceAlbumScreen is not assigned.");
            return;
        }
        UnsetAllScreens(() =>
        {
            choiceAlbumScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }

    private void ShowChoiceMenuScreenCore(ChoiceMenuScreen screen)
    {
        switch (screen)
        {
            case ChoiceMenuScreen.ChoiceSsOrMusic:
                ShowChoiceSsOrMusicScreenCore(ResolveChoiceSsOrMusicSecondStageVariant());
                break;
            case ChoiceMenuScreen.ChoiceSonofloreMusicLength:
                ShowChoiceSonofloreMusicLengthScreenCore();
                break;
            case ChoiceMenuScreen.ChoiceAlbum:
                ShowChoiceAlbumScreenCore();
                break;
            default:
                Debug.LogWarning("UIManager.ShowChoiceMenuScreenCore: unhandled " + screen + "; opening Choice SS or Music.");
                ShowChoiceSsOrMusicScreenCore(ResolveChoiceSsOrMusicSecondStageVariant());
                break;
        }
    }

    /// <summary>Show or hide loading + label on bindings for Next Step wait (see <see cref="CalibrationCueWaitBinding"/>).</summary>
    public void SetCalibrationStepNextCuePendingVisual(CalibrationUI step, bool pending)
    {
        var root = GetCalibrationScreenRoot(step);
        if (root == null)
            return;
        foreach (var b in root.GetComponentsInChildren<CalibrationCueWaitBinding>(true))
        {
            if (b != null && b.DriveNextStepCueWaitVisual)
                b.SetPendingCueWaitActive(pending);
        }
    }

    /// <summary>Same as Next Step wait, for Conclusion confirm bindings only.</summary>
    public void SetCalibrationConclusionConfirmCuePendingVisual(bool pending)
    {
        if (conclusionScreen == null)
            return;
        foreach (var b in conclusionScreen.GetComponentsInChildren<CalibrationCueWaitBinding>(true))
        {
            if (b != null && b.DriveConclusionCueWaitVisual)
                b.SetPendingCueWaitActive(pending);
        }
    }

    /// <summary>
    /// Sets the calibration progress indicator: shows the first <paramref name="totalSteps"/> dots, hides the rest,
    /// then swaps the dot at <paramref name="activeStepIndex"/> for its matching line. Slots are indexed by step
    /// position in the active variant (so e.g. slot 2 is "the third step", whichever <see cref="CalibrationUI"/>
    /// the variant places there — see <see cref="SoundSelf.Sequence.CalibrationStageHandler"/>).
    /// </summary>
    public void SetCalibrationProgress(int totalSteps, int activeStepIndex)
    {
        int dotCount = calibrationProgressDots != null ? calibrationProgressDots.Length : 0;
        int lineCount = calibrationProgressLines != null ? calibrationProgressLines.Length : 0;
        int slotCount = Mathf.Max(dotCount, lineCount);
        for (int i = 0; i < slotCount; i++)
        {
            bool stepInUse = i < totalSteps;
            bool isActive = stepInUse && i == activeStepIndex;
            GameObject dot = i < dotCount ? calibrationProgressDots[i] : null;
            GameObject line = i < lineCount ? calibrationProgressLines[i] : null;
            bool wantDotActive = stepInUse && !isActive;
            bool wantLineActive = isActive;
            if (dot != null && dot.activeSelf != wantDotActive)
                dot.SetActive(wantDotActive);
            if (line != null && line.activeSelf != wantLineActive)
                line.SetActive(wantLineActive);
        }
    }

    /// <summary>Hide every calibration progress dot and line. Called from <c>CalibrationStageHandler.MarkComplete</c> and defensively from <c>LocalCleanup</c>.</summary>
    public void ClearCalibrationProgress()
    {
        if (calibrationProgressDots != null)
        {
            for (int i = 0; i < calibrationProgressDots.Length; i++)
            {
                var dot = calibrationProgressDots[i];
                if (dot != null && dot.activeSelf)
                    dot.SetActive(false);
            }
        }
        if (calibrationProgressLines != null)
        {
            for (int i = 0; i < calibrationProgressLines.Length; i++)
            {
                var line = calibrationProgressLines[i];
                if (line != null && line.activeSelf)
                    line.SetActive(false);
            }
        }
    }

    /// <summary>Clears cue-wait visuals (e.g. on calibration exit).</summary>
    public void ClearAllCalibrationCueWaitVisuals()
    {
        SetCalibrationStepNextCuePendingVisual(CalibrationUI.Start, false);
        SetCalibrationStepNextCuePendingVisual(CalibrationUI.Headphone, false);
        SetCalibrationStepNextCuePendingVisual(CalibrationUI.Microphone, false);
        SetCalibrationStepNextCuePendingVisual(CalibrationUI.VibroAcoustic, false);
        SetCalibrationStepNextCuePendingVisual(CalibrationUI.LightGlasses, false);
        SetCalibrationConclusionConfirmCuePendingVisual(false);
    }

    private GameObject GetCalibrationScreenRoot(CalibrationUI screen)
    {
        switch (screen)
        {
            case CalibrationUI.Start:
                return startScreen;
            case CalibrationUI.Headphone:
                return headphoneScreen;
            case CalibrationUI.Microphone:
                return microphoneScreen;
            case CalibrationUI.VibroAcoustic:
                return vibroAcousticScreen;
            case CalibrationUI.LightGlasses:
                return lightGlassesScreen;
            case CalibrationUI.Conclusion:
                return conclusionScreen;
            default:
                return null;
        }
    }

    private const float BatteryPollIntervalSeconds = 1f;
    private float _nextBatteryPollTime;
    private float _nextLocalTimeTextUpdateTime;

    /// <summary>Until this time (<see cref="Time.time"/>), inspector button callbacks ignore presses (debounce + post-screen-show grace).</summary>
    private float _nextButtonInteractionAllowedTime;

    private CanvasGroup _calibrationHeadGroup;
    private Coroutine _calibrationHeadFadeCoroutine;

    private bool TryAcceptButtonPress()
    {
        return Time.time >= _nextButtonInteractionAllowedTime;
    }

    private void ArmButtonInteractionCooldown()
    {
        _nextButtonInteractionAllowedTime = Time.time + UIManagerTiming.ButtonInteractionCooldownSeconds;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        if (calibrationHeadText != null)
        {
            _calibrationHeadGroup = calibrationHeadText.GetComponent<CanvasGroup>();
            if (_calibrationHeadGroup == null)
                _calibrationHeadGroup = calibrationHeadText.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        versionText.text = "Live Sequence Version " + Application.version;
        ArmButtonInteractionCooldown();
        if (calibrationHeadText != null)
        {
            _calibrationHeadGroup.alpha = 0f;
            calibrationHeadText.SetActive(false);
        }
        ClearCalibrationProgress();
        RefreshLocalTimeTextFromSystem();
        _nextLocalTimeTextUpdateTime = Time.time + UIManagerTiming.LocalTimeTextUpdateIntervalSeconds;
    }

    private void Update()
    {
        RefreshCalibrationVoiceTestUiIfNeeded();

        if (Time.time >= _nextLocalTimeTextUpdateTime)
        {
            _nextLocalTimeTextUpdateTime = Time.time + UIManagerTiming.LocalTimeTextUpdateIntervalSeconds;
            RefreshLocalTimeTextFromSystem();
        }

        if (Time.time >= _nextBatteryPollTime)
        {
            _nextBatteryPollTime = Time.time + BatteryPollIntervalSeconds;
            RefreshBatteryUiFromSystem();
        }
    }

    /// <summary>Sets <see cref="localTimeText"/> from the device clock, e.g. <c>11:11 am, CST</c>.</summary>
    private void RefreshLocalTimeTextFromSystem()
    {
        if (localTimeText == null)
            return;

        DateTime now = DateTime.Now;
        string timePart = now.ToString("h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant();
        localTimeText.text = timePart + ", " + GetLocalTimeZoneAbbreviation(now);
    }

    private static string GetLocalTimeZoneAbbreviation(DateTime localTime)
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.Local;
        }
        catch
        {
            return "UTC";
        }

        string zoneName = tz.IsDaylightSavingTime(localTime) ? tz.DaylightName : tz.StandardName;
        if (!string.IsNullOrEmpty(zoneName))
        {
            string[] parts = zoneName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
                return string.Concat(char.ToUpperInvariant(parts[0][0]), char.ToUpperInvariant(parts[1][0]), char.ToUpperInvariant(parts[2][0]));
            if (parts.Length == 1 && parts[0].Length <= 5)
                return parts[0].ToUpperInvariant();
        }

        TimeSpan offset = tz.GetUtcOffset(localTime);
        int hours = (int)offset.TotalHours;
        if (hours == 0)
            return "UTC";
        return "UTC" + (hours > 0 ? "+" : "") + hours.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Reads <see cref="SystemInfo.batteryLevel"/> and updates the battery bar (0–1).</summary>
    private void RefreshBatteryUiFromSystem()
    {
        if (battery == null)
            return;

        float level = SystemInfo.batteryLevel;
        // Unity returns -1 when level is unknown or no battery API (common on desktop/editor).
        if (level < 0f)
            level = 1f;

        SetBatteryHealth(Mathf.Clamp01(level));

        // Many laptops report NotCharging or Full on AC (battery full / trickle), not Charging.
        // Only Discharging reliably means "on battery"; Unknown is common in Editor / some desktops.
        var status = SystemInfo.batteryStatus;
        bool onExternalPowerOrCharging =
            status == BatteryStatus.Charging
            || status == BatteryStatus.Full
            || status == BatteryStatus.NotCharging;
        SetBatteryCharging(onExternalPowerOrCharging);
    }

    //Set battery health from 0-1
    public void SetBatteryHealth(float health)
    {
        battery.SetHealth(health);
    }
    public void SetBatteryCharging(bool isCharging)
    {
        battery.SetCharging(isCharging);
    }
    /// <summary>Manual override for <see cref="micLevelText"/> (normally driven from <see cref="ImitoneVoiceIntepreter.GetRawMicrophoneInputLevelPercent"/> during the microphone test).</summary>
    public void SetMicrophoneStatus(int inputLevel)
    {
        ApplyMicLevelText(inputLevel);
    }

    private static Color ColorForMicLevelPercent(int percent)
    {
        float t = Mathf.InverseLerp(
            CalibrationMicLevelColorMinPercent,
            CalibrationMicLevelColorMaxPercent,
            percent);
        return Color.Lerp(CalibrationMicLevelLowColor, CalibrationMicLevelHighColor, t);
    }

    private void ApplyMicLevelText(int inputLevel)
    {
        if (micLevelText == null)
            return;
        int clamped = Mathf.Clamp(inputLevel, 0, 99);
        micLevelText.text = clamped.ToString("D2") + "%";
        micLevelText.color = ColorForMicLevelPercent(clamped);
    }

    private bool ShouldShowCalibrationMicLevel()
    {
        if (_activeCalibrationUi != CalibrationUI.Microphone)
            return false;
        if (microphoneScreen == null || !microphoneScreen.activeSelf)
            return false;
        if (calibrationMicrophoneTestCard != null && !calibrationMicrophoneTestCard.activeInHierarchy)
            return false;
        return true;
    }

    private bool ShouldShowCalibrationToneDetectedText()
    {
        if (_activeCalibrationUi != CalibrationUI.Microphone && _activeCalibrationUi != CalibrationUI.VibroAcoustic)
            return false;

        GameObject screen = _activeCalibrationUi == CalibrationUI.Microphone ? microphoneScreen : vibroAcousticScreen;
        if (screen == null || !screen.activeSelf)
            return false;

        if (_activeCalibrationUi == CalibrationUI.Microphone
            && calibrationMicrophoneTestCard != null
            && !calibrationMicrophoneTestCard.activeInHierarchy)
            return false;

        return true;
    }

    private ImitoneVoiceIntepreter ResolveImitoneForMicLevel()
    {
        if (sequencer != null && sequencer.imitoneVoiceInterpreter != null)
            return sequencer.imitoneVoiceInterpreter;
        return FindObjectOfType<ImitoneVoiceIntepreter>();
    }

    private void RefreshCalibrationVoiceTestUiIfNeeded()
    {
        if (micLevelText == null && micToneOnText == null)
            return;

        bool showMicLevel = ShouldShowCalibrationMicLevel();
        bool showToneDetected = ShouldShowCalibrationToneDetectedText();
        if (!showMicLevel && !showToneDetected)
        {
            if (_calibrationVoiceTestUiWasActive)
            {
                if (micLevelText != null)
                    ApplyMicLevelText(0);
                SetMicToneOnText(false);
                _calibrationVoiceTestUiWasActive = false;
                _nextCalibrationVoiceTestUiUpdateTime = 0f;
                _micToneOnTextLastToneOn = null;
            }
            return;
        }

        bool firstUpdateThisVisit = !_calibrationVoiceTestUiWasActive;
        _calibrationVoiceTestUiWasActive = true;
        if (!firstUpdateThisVisit && Time.time < _nextCalibrationVoiceTestUiUpdateTime)
            return;
        _nextCalibrationVoiceTestUiUpdateTime = Time.time + (1f / UIManagerTiming.CalibrationMicLevelTextUpdatesPerSecond);

        var interpreter = ResolveImitoneForMicLevel();
        if (showMicLevel && micLevelText != null)
        {
            int level = interpreter != null ? interpreter.GetRawMicrophoneInputLevelPercent() : 0;
            ApplyMicLevelText(level);
        }

        if (showToneDetected && micToneOnText != null)
            SetMicToneOnText(interpreter != null && interpreter.toneActive);
    }

    private void SetMicToneOnText(bool toneOn)
    {
        if (micToneOnText == null)
            return;
        if (_micToneOnTextLastToneOn.HasValue && _micToneOnTextLastToneOn.Value == toneOn)
            return;

        _micToneOnTextLastToneOn = toneOn;
        if (toneOn)
        {
            micToneOnText.text = "YES";
            micToneOnText.color = CalibrationMicToneYesColor;
        }
        else
        {
            micToneOnText.text = "NO";
            micToneOnText.color = CalibrationMicToneNoColor;
        }
    }

    public void SetHeadphoneStatus(int outputLevel)
    {
        if (headphoneStatusText == null)
            return;
        headphoneStatusText.text = outputLevel.ToString() + "%";
    }

    public void SetProgressBar(float progress) //progress bar from 0 to 1.
    {
        sessionStartTimer.SetProgressBar(progress);

    }
    public void SetTimeText(string time) // time text as string.
    {
        sessionStartTimer.SetTimeText(time);
    }


    // =====================================
    // CALIBRATION HEAD TEXT FADE
    // =====================================

    private void FadeInCalibrationHead()
    {
        if (_calibrationHeadGroup == null) return;
        if (_calibrationHeadFadeCoroutine != null) StopCoroutine(_calibrationHeadFadeCoroutine);
        _calibrationHeadFadeCoroutine = StartCoroutine(FadeInCalibrationHeadCoroutine());
    }

    private void FadeOutCalibrationHead()
    {
        if (_calibrationHeadGroup == null) return;
        if (_calibrationHeadFadeCoroutine != null) StopCoroutine(_calibrationHeadFadeCoroutine);
        _calibrationHeadFadeCoroutine = StartCoroutine(FadeOutCalibrationHeadCoroutine());
    }

    private System.Collections.IEnumerator FadeInCalibrationHeadCoroutine()
    {
        _calibrationHeadGroup.alpha = 0f;
        calibrationHeadText.SetActive(true);
        float elapsed = 0f;
        float duration = 1f; // matches ScreenFadeEffect FadeIn duration
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _calibrationHeadGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        _calibrationHeadGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeOutCalibrationHeadCoroutine()
    {
        float elapsed = 0f;
        float duration = 0.7f; // matches ScreenFadeEffect FadeOut duration
        yield return new WaitForEndOfFrame();
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _calibrationHeadGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        _calibrationHeadGroup.alpha = 0f;
        calibrationHeadText.SetActive(false);
    }

    // =====================================
    // BUTTON SUBSCRIBERS
    // =====================================

    // Events for UI buttons
    //
    // --- UI ↔ sequence (handler subscription) pattern ---
    // UIManager exposes C# events (Action). Inspector buttons call methods like
    // StartSoundSelfButtonPress(), which only Invoke() the event — no sequencing logic here.
    //
    // A sequence stage handler (e.g. SetMenuStageHandler) subscribes while its stage is active:
    //   Enter(variant):  if (UIManager.Instance != null) Instance.OnStartSoundSelfPress += Handler;
    //   Exit():          if (UIManager.Instance != null) Instance.OnStartSoundSelfPress -= Handler;
    // Use Enter/Exit (not BeginTransitionOut) so subscription lifetime matches the stage, including
    // when SequenceRunner.StartSequence force-exits handlers (Exit still runs).
    //
    // "End This Sequence Stage" is wired on Sequencer: it subscribes to OnEndThisSequenceStagePress and
    // calls HandleSequenceCommand(SequenceCommand.EndThisSequenceStage) so handlers use WatchesSequenceCommand / ExecuteSequenceCommand.
    //
    // Calibration Next/Back/Conclusion use OnCalibrationNextStepPress, OnCalibrationBackPress, OnCalibrationConclusionConfirmPress
    // — subscribed only by CalibrationStageHandler while the Calibration stage is active.
    //
    // Choice-menu Back (SS / Sonoflore length / Album chain) uses ChoiceMenuBackButtonPress() and _choiceMenuBackStack — not BackStepButtonPress.
    //
    // In the handler, gate behavior on StageVariant (or other state) so the same button means
    // different things per menu kind. MarkComplete() / advance sequencing from the handler callback,
    // not from UIManager. If the callback calls StartProtocolStacksInteractiveSequence (or any
    // StartSequence path), defer one frame (yield return null on the Sequencer) so you are not
    // inside the event callback while the runner tears down this handler.
    //
    // Example handler sketch:
    //   void Handler() {
    //     if (_variant != StageVariant.Menu_Ps_InteractiveOrMusic) return;
    //     MarkComplete();
    //     _sequencer.StartCoroutine(StartInteractiveNextFrame());
    //   }

    // Inspector buttons call these methods; each applies <see cref="UIManagerTiming.ButtonInteractionCooldownSeconds"/> debounce
    // and re-arms after an accepted press. Screen transitions call <see cref="ArmButtonInteractionCooldown"/> so fresh UI is idle briefly.

    public void QuitButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnQuit?.Invoke();
        ArmButtonInteractionCooldown();
        StartCoroutine(QuitApplicationAfterNextFrame());
    }

    private System.Collections.IEnumerator QuitApplicationAfterNextFrame()
    {
        yield return null;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void EndThisSequenceStageButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnEndThisSequenceStagePress?.Invoke();
        ArmButtonInteractionCooldown();
    }

    public void StartSoundSelfButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnStartSoundSelfPress?.Invoke();
        ArmButtonInteractionCooldown();

        // choiceSSOrMusicScreenSetActive(false);
        // headphoneScreen.SetActive(true);
    }

    public void PlayMusicButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnPlayMusicPress?.Invoke();
        ArmButtonInteractionCooldown();

        // choiceSSOrMusicScreenSetActive(false);
        // microphoneScreen.SetActive(true);
    }

    /// <summary>Wire all calibration section primary Next buttons (Start through LightGlasses) to this; handler advances by variant step list.</summary>
    public void NextStepButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnCalibrationNextStepPress?.Invoke();
        ArmButtonInteractionCooldown();
    }

    /// <summary>Wire calibration Back buttons to this (omit or disable on Start).</summary>
    public void BackStepButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnCalibrationBackPress?.Invoke();
        ArmButtonInteractionCooldown();
    }

    /// <summary>Wire Choice-menu Back buttons (SS / Sonoflore length, etc.) to this — not <see cref="BackStepButtonPress"/> (calibration).</summary>
    public void ChoiceMenuBackButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        if (_choiceMenuBackStack.Count == 0)
        {
            Debug.Log("UIManager.ChoiceMenuBackButtonPress: choice stack is empty (already at root of choice flow, or stack was cleared).");
            ArmButtonInteractionCooldown();
            return;
        }
        var previous = _choiceMenuBackStack.Pop();
        ShowChoiceMenuScreenCore(previous);
    }

    /// <summary>Wire the Conclusion screen primary control to this — not <see cref="EndThisSequenceStageButtonPress"/>.</summary>
    public void CalibrationConclusionConfirmButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnCalibrationConclusionConfirmPress?.Invoke();
        ArmButtonInteractionCooldown();
    }

    [Obsolete("Use NextStepButtonPress — CalibrationStageHandler listens on OnCalibrationNextStepPress.")]
    public void MicrophoneNextStepButtonPress() => NextStepButtonPress();

    [Obsolete("Use NextStepButtonPress — CalibrationStageHandler listens on OnCalibrationNextStepPress.")]
    public void HeadphoneNextStepButtonPress() => NextStepButtonPress();

    [Obsolete("Use NextStepButtonPress — CalibrationStageHandler listens on OnCalibrationNextStepPress.")]
    public void VibroAcousticNextStepButtonPress() => NextStepButtonPress();

    [Obsolete("Use NextStepButtonPress — CalibrationStageHandler listens on OnCalibrationNextStepPress.")]
    public void LightGlassesNextStepButtonPress() => NextStepButtonPress();

    public void HeadphoneTroubleshootingButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnHeadphoneTroubleshootingPress?.Invoke();
        ArmButtonInteractionCooldown();
    }

    /// <summary>Wire the session skip control (meditation HUD). Root should use <see cref="ScreenFadeEffect"/> like other session rows. <paramref name="labelWhenEnabling"/> applies only when <paramref name="enabled"/> is true.</summary>
    public void EnableSkipButton(bool enabled, string labelWhenEnabling)
    {
        if (skipSessionButton == null)
        {
            if (enabled)
                Debug.LogWarning("UIManager.EnableSkipButton: skipSessionButton is not assigned.");
            return;
        }

        if (enabled)
        {
            if (!_skipButtonSuppressTextChanges && skipSessionButtonText != null)
                skipSessionButtonText.text = labelWhenEnabling ?? string.Empty;
            if (!skipSessionButton.activeSelf)
                skipSessionButton.SetActive(true);
        }
        else
        {
            if (!skipSessionButton.activeSelf)
                return;
            var fade = skipSessionButton.GetComponent<ScreenFadeEffect>();
            if (fade != null)
                fade.FadeOut(() => skipSessionButton.SetActive(false));
            else
                skipSessionButton.SetActive(false);
        }
    }

    public void SkipButtonPress()
    {
        if (_skipSessionButtonFadeOutPending)
            return;
        if (!TryAcceptButtonPress())
            return;

        if (skipSessionButton == null || !skipSessionButton.activeSelf)
        {
            OnSkipSessionButtonPress?.Invoke();
            ArmButtonInteractionCooldown();
            return;
        }

        var fade = skipSessionButton.GetComponent<ScreenFadeEffect>();
        _skipSessionButtonFadeOutPending = true;
        _skipButtonSuppressTextChanges = true;
        if (fade != null)
        {
            fade.FadeOut(() =>
            {
                _skipSessionButtonFadeOutPending = false;
                _skipButtonSuppressTextChanges = false;
                skipSessionButton.SetActive(false);
                OnSkipSessionButtonPress?.Invoke();
                ArmButtonInteractionCooldown();
            });
        }
        else
        {
            _skipSessionButtonFadeOutPending = false;
            _skipButtonSuppressTextChanges = false;
            skipSessionButton.SetActive(false);
            OnSkipSessionButtonPress?.Invoke();
            ArmButtonInteractionCooldown();
        }
    }

    public void MeditationQuitButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnMeditationQuitPress?.Invoke();
        ArmButtonInteractionCooldown();

        endMeditationScreen.SetActive(false);
    }


}
