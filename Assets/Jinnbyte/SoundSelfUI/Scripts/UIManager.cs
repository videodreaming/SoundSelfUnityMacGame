using System;
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

/// <summary>Cooldown after any accepted button press, and minimum idle time after a screen with buttons becomes visible.</summary>
public static class UIManagerTiming
{
    public const float ButtonInteractionCooldownSeconds = 0.333f;
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private Battery battery;
    [SerializeField] private Timer sessionStartTimer;
    [SerializeField] private Text microphoneStatusText;
    [SerializeField] private Text headphoneStatusText;
    [SerializeField] private Text versionText;


    //Screens
    [SerializeField] private GameObject choiceSSOrMusicScreen;
    [SerializeField] private GameObject welcomeScreen;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject conclusionScreen;
    [SerializeField] private GameObject headphoneScreen; //calibration screens
    [SerializeField] private GameObject microphoneScreen; //calibration screens
    [SerializeField] private GameObject vibroAcousticScreen; //calibration screens
    [SerializeField] private GameObject lightGlassesScreen; // Section Calibration Lightglasses root
    [SerializeField] private GameObject startMeditationScreen;
    [SerializeField] private GameObject endMeditationScreen;




    public Action OnQuit;
    public Action OnStartSoundSelfPress;
    public Action OnEndThisSequenceStagePress;
    public Action OnPlayMusicPress;
    /// <summary>Generic Next Step during calibration; <see cref="CalibrationStageHandler"/> advances by variant step list.</summary>
    public Action OnCalibrationNextStepPress;
    /// <summary>Back one calibration step; no-op on <see cref="CalibrationUI.Start"/>.</summary>
    public Action OnCalibrationBackPress;
    /// <summary>Conclusion screen only — user confirmed ready to finish calibration (paired with VO-done in Stage E before <c>MarkComplete</c>).</summary>
    public Action OnCalibrationConclusionConfirmPress;
    public Action OnHeadphoneTroubleshootingPress;
    public Action OnSkipSessionButtonPress;
    public Action OnMeditationQuitPress;
    public Action OnSessionTimeEnds;

    // Screen Set Functions
    public void UnsetAllScreens(Action onComplete)
    {

        FadeOutScreen(() =>
        {
            choiceSSOrMusicScreen.SetActive(false);
            welcomeScreen.SetActive(false);
            if (startScreen != null)
                startScreen.SetActive(false);
            if (conclusionScreen != null)
                conclusionScreen.SetActive(false);
            headphoneScreen.SetActive(false);
            microphoneScreen.SetActive(false);
            vibroAcousticScreen.SetActive(false);
            lightGlassesScreen.SetActive(false);
            startMeditationScreen.SetActive(false);
            endMeditationScreen.SetActive(false);
            onComplete?.Invoke();

        });


    }
    void FadeOutScreen(Action onComplete)
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
        else if (startScreen != null && startScreen.activeSelf)
        {
            startScreen.GetComponent<ScreenFadeEffect>().FadeOut(onComplete);
        }
        else if (conclusionScreen != null && conclusionScreen.activeSelf)
        {
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
    public void SetCalibrationScreen(CalibrationUI screen)
    {
        UnsetAllScreens(() =>
        {
            switch (screen)
            {
                case CalibrationUI.Start:
                    if (startScreen != null)
                        startScreen.SetActive(true);
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
        });

    }

    public void SetMeditationScreen()
    {
        UnsetAllScreens(() =>
        {
            startMeditationScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }
    public void SetEndMeditationScreen()
    {
        UnsetAllScreens(() =>
        {
            endMeditationScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }
    public void SetChoiceSSOrMusicScreen()
    {
        UnsetAllScreens(() =>
        {
            choiceSSOrMusicScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
    }
    public void SetWelcomeScreen()
    {
        UnsetAllScreens(() =>
        {
            welcomeScreen.SetActive(true);
            ArmButtonInteractionCooldown();
        });
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
    }

    void Start()
    {
        versionText.text = "Live Sequence Version " + Application.version;
        ArmButtonInteractionCooldown();
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
