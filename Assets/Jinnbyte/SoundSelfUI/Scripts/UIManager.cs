using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

/// <summary>Roots managed by <see cref="UIManager"/> choice-menu navigation stack (<see cref="UIManager.NavigateChoiceMenuToSonofloreMusicLengthFromSsOrMusic"/>, <see cref="UIManager.ChoiceMenuBackButtonPress"/>). Calibration flows use <see cref="CalibrationUI"/> and <see cref="UIManager.SetCalibrationScreen"/> instead — see <see cref="SoundSelf.Sequence.CalibrationStageHandler"/>.</summary>
public enum ChoiceMenuScreen
{
    None = 0,
    ChoiceSsOrMusic = 1,
    ChoiceSonofloreMusicLength = 2,
}

/// <summary>Cooldown after any accepted button press, and minimum idle time after a screen with buttons becomes visible.</summary>
public static class UIManagerTiming
{
    public const float ButtonInteractionCooldownSeconds = 0.333f;
}

/// <summary>
/// Session UI: battery/timer, screen roots with <see cref="ScreenFadeEffect"/>, and button debounce.
/// <para><b>Choice menus</b> (<see cref="ChoiceMenuScreen"/>): forward navigation can push a return target onto <c>_choiceMenuBackStack</c>; Back uses <see cref="ChoiceMenuBackButtonPress"/>. <see cref="SetChoiceSSOrMusicScreen"/> arms a one-shot back anchor so <see cref="SetChoiceSonofloreMusicLengthScreen"/> still records SS/Music even when that screen is not yet <c>activeSelf</c> during fades; <see cref="NavigateChoiceMenuToSonofloreMusicLengthFromSsOrMusic"/> pushes explicitly.</para>
/// <para><b>Calibration</b> (<see cref="CalibrationUI"/>): separate flow — <see cref="SetCalibrationScreen"/>, <see cref="OnCalibrationNextStepPress"/> / <see cref="OnCalibrationBackPress"/> wired to <see cref="SoundSelf.Sequence.CalibrationStageHandler"/>; not driven by the choice stack.</para>
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private Battery battery;
    [SerializeField] private Timer sessionStartTimer;
    [SerializeField] private Text microphoneStatusText;
    [SerializeField] private Text headphoneStatusText;
    [SerializeField] private Text versionText;
    [SerializeField] private GameObject calibrationHeadText; // Set by CalibrationStageHandler per step; not all steps have instructions, so optional assignment.


    //Screens
    [SerializeField] private GameObject choiceSSOrMusicScreen;
    [SerializeField] private GameObject choiceSonofloreMusicLengthScreen;
    [SerializeField] private GameObject welcomeScreen;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject conclusionScreen;
    [SerializeField] private GameObject headphoneScreen; //calibration screens
    [SerializeField] private GameObject microphoneScreen; //calibration screens
    [SerializeField] private GameObject vibroAcousticScreen; //calibration screens
    [SerializeField] private GameObject lightGlassesScreen; // Section Calibration Lightglasses root
    [SerializeField] private GameObject startMeditationScreen;
    [SerializeField] private GameObject endMeditationScreen;

    /// <summary>When moving forward between <see cref="ChoiceMenuScreen"/> roots, we push the parent so <see cref="ChoiceMenuBackButtonPress"/> can restore it. Not used for calibration — see <see cref="OnCalibrationBackPress"/> / <see cref="SoundSelf.Sequence.CalibrationStageHandler"/>.</summary>
    private readonly Stack<ChoiceMenuScreen> _choiceMenuBackStack = new Stack<ChoiceMenuScreen>();

    /// <summary>Set when SS/Music choice UI is shown (<see cref="ShowChoiceSsOrMusicScreenCore"/>); cleared by <see cref="ClearChoiceMenuNavigationStack"/>. Lets <see cref="SetChoiceSonofloreMusicLengthScreen"/> push SS/Music as Back target even if fades mean SS is not yet <see cref="GameObject.activeSelf"/>.</summary>
    private bool _pendingSonofloreLengthBackToSsOrMusic;




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
    /// <summary>Wwise <c>Cue_Calibration_Instruction_OFF</c> while calibration UI is still on <see cref="CalibrationUI.Start"/> — e.g. swap Start Next button copy from “please wait” to primary label.</summary>
    public Action OnCalibrationStartInstructionVoLineEnded;
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
        ClearChoiceMenuNavigationStack();
        if (endMeditationScreen != null && endMeditationScreen.activeSelf)
            return;
        UnsetAllScreens(() =>
        {
            endMeditationScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }
    /// <summary>Shows Choice SS or Music from sequence (e.g. SetMenu); clears choice back-stack (pending Back anchor is set when SS UI becomes active — see <see cref="ShowChoiceSsOrMusicScreenCore"/>).</summary>
    public void SetChoiceSSOrMusicScreen()
    {
        ClearChoiceMenuNavigationStack();
        ShowChoiceSsOrMusicScreenCore();
    }

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
    /// Shows Choice Sonoflore Music Length. Clears the choice stack, then if we are coming from SS/Music (screen active or <see cref="SetChoiceSSOrMusicScreen"/> just ran), pushes SS/Music so <see cref="ChoiceMenuBackButtonPress"/> works.
    /// Prefer <see cref="NavigateChoiceMenuToSonofloreMusicLengthFromSsOrMusic"/> when wiring forward navigation explicitly.
    /// </summary>
    public void SetChoiceSonofloreMusicLengthScreen()
    {
        if (choiceSonofloreMusicLengthScreen != null && choiceSonofloreMusicLengthScreen.activeSelf)
            return;

        bool recordBackToSsOrMusic = (choiceSSOrMusicScreen != null && choiceSSOrMusicScreen.activeSelf)
            || _pendingSonofloreLengthBackToSsOrMusic;

        ClearChoiceMenuNavigationStack();
        if (recordBackToSsOrMusic)
            _choiceMenuBackStack.Push(ChoiceMenuScreen.ChoiceSsOrMusic);

        ShowChoiceSonofloreMusicLengthScreenCore();
    }

    /// <summary>
    /// Forward navigation: from Choice SS or Music to Choice Sonoflore Music Length, recording SS as the Back target (clears the one-shot pending flag from <see cref="SetChoiceSSOrMusicScreen"/>).
    /// Equivalent to <see cref="SetChoiceSonofloreMusicLengthScreen"/> when coming from that choice screen; use either from Inspector.
    /// </summary>
    public void NavigateChoiceMenuToSonofloreMusicLengthFromSsOrMusic()
    {
        _pendingSonofloreLengthBackToSsOrMusic = false;
        _choiceMenuBackStack.Push(ChoiceMenuScreen.ChoiceSsOrMusic);
        ShowChoiceSonofloreMusicLengthScreenCore();
    }

    private void ClearChoiceMenuNavigationStack()
    {
        _choiceMenuBackStack.Clear();
        _pendingSonofloreLengthBackToSsOrMusic = false;
    }

    private void ShowChoiceSsOrMusicScreenCore()
    {
        UnsetAllScreens(() =>
        {
            choiceSSOrMusicScreen.SetActive(true);
            _pendingSonofloreLengthBackToSsOrMusic = true;
            ArmButtonInteractionCooldown();
        });
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

    private void ShowChoiceMenuScreenCore(ChoiceMenuScreen screen)
    {
        switch (screen)
        {
            case ChoiceMenuScreen.ChoiceSsOrMusic:
                ShowChoiceSsOrMusicScreenCore();
                break;
            case ChoiceMenuScreen.ChoiceSonofloreMusicLength:
                ShowChoiceSonofloreMusicLengthScreenCore();
                break;
            default:
                Debug.LogWarning("UIManager.ShowChoiceMenuScreenCore: unhandled " + screen + "; opening Choice SS or Music.");
                ShowChoiceSsOrMusicScreenCore();
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

    /// <summary>Called from <see cref="SoundSelf.Sequence.CalibrationStageHandler"/> when <c>Cue_Calibration_Instruction_OFF</c> fires while still on the Start calibration step.</summary>
    public void NotifyCalibrationStartInstructionVoLineEnded() => OnCalibrationStartInstructionVoLineEnded?.Invoke();

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

    // Set Time 
    // Set Progress Bar

    private bool isSessionActive = false;
    private float sessionDuration = 1f; // Example session duration in seconds
    private float sessionElapsedTime = 0f;

    private const float BatteryPollIntervalSeconds = 1f;
    private float _nextBatteryPollTime;

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
    }

    private void Update()
    {
        if (Time.time < _nextBatteryPollTime)
            return;
        _nextBatteryPollTime = Time.time + BatteryPollIntervalSeconds;
        RefreshBatteryUiFromSystem();
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
    public void SetMicrophoneStatus(int inputLevel)
    {
        microphoneStatusText.text = inputLevel.ToString() + "%";
    }

    public void SetHeadphoneStatus(int outputLevel)
    {
        headphoneStatusText.text = outputLevel.ToString() + "%";
    }

    // public void SetSessionStartTimer(float time)
    // {
    //     sessionStartTimer.SetTimer(time);
    // }

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
    // Choice-menu Back (SS / Sonoflore length chain) uses ChoiceMenuBackButtonPress() and _choiceMenuBackStack — not BackStepButtonPress.
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

    public void SkipMeditationSessionButtonPress()
    {
        if (!TryAcceptButtonPress())
            return;
        OnSkipSessionButtonPress?.Invoke();
        ArmButtonInteractionCooldown();

        // startMeditationScreen.SetActive(false);
        // endMeditationScreen.SetActive(true);
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
