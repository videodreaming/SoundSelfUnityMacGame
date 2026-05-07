using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum CalibrationUI
{
    Introduction,
    Headphone,
    Microphone,
    VibroAcoustic,
    LightGlass,
    Conclusion
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
    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject headphoneScreen; //calibration screens
    [SerializeField] private GameObject microphoneScreen; //calibration screens
    [SerializeField] private GameObject vibroAcousticScreen; //calibration screens
    [SerializeField] private GameObject lightGlassScreen; //calibration screens
    [SerializeField] private GameObject startMeditationScreen;
    [SerializeField] private GameObject endMeditationScreen;




    public Action OnQuit;
    public Action OnStartSoundSelfPress;
    public Action OnPlayMusicPress;
    public Action OnMicrophoneScreenNextPress;
    public Action OnHeadphoneScreenNextPress;
    public Action OnVibroacousticScreenNextPress;
    public Action OnLightglassScreenNextPress;
    public Action OnHeadphoneTroubleshootingPress;
    public Action OnSkipSessionButtonPress;
    public Action OnMeditationQuitPress;
    public Action OnSessionTimeEnds;

    // Screen Set Functions
    public void UnsetAllScreens()
    {
        mainScreen.SetActive(false);
        headphoneScreen.SetActive(false);
        microphoneScreen.SetActive(false);
        vibroAcousticScreen.SetActive(false);
        lightGlassScreen.SetActive(false);
        startMeditationScreen.SetActive(false);
        endMeditationScreen.SetActive(false);
    }
    public void SetCalibrationScreen(CalibrationUI screen)
    {
        UnsetAllScreens();
        switch (screen)
        {
            case CalibrationUI.Headphone:
                headphoneScreen.SetActive(true);
                break;
            case CalibrationUI.Microphone:
                microphoneScreen.SetActive(true);
                break;
            case CalibrationUI.VibroAcoustic:
                vibroAcousticScreen.SetActive(true);
                break;
            case CalibrationUI.LightGlass:
                lightGlassScreen.SetActive(true);
                break;
        }
    }

    public void SetMeditationScreen()
    {
        UnsetAllScreens();
        startMeditationScreen.SetActive(true);
    }
    public void SetEndMeditationScreen()
    {
        UnsetAllScreens();
        endMeditationScreen.SetActive(true);
    }
    public void SetChoiceScreen()
    {
        UnsetAllScreens();
        mainScreen.SetActive(true);
    }
    // Set Time 
    // Set Progress Bar

    private bool isSessionActive = false;
    private float sessionDuration = 1f; // Example session duration in seconds
    private float sessionElapsedTime = 0f;

    private const float BatteryPollIntervalSeconds = 1f;
    private float _nextBatteryPollTime;

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

    public void QuitButtonPress()
    {
        OnQuit?.Invoke();
    }
    public void StartSoundSelfButtonPress()
    {
        OnStartSoundSelfPress?.Invoke();


        // mainScreen.SetActive(false);
        // headphoneScreen.SetActive(true);
    }
    public void PlayMusicButtonPress()
    {
        OnPlayMusicPress?.Invoke();

        // mainScreen.SetActive(false);
        // microphoneScreen.SetActive(true);
    }

    public void MicrophoneNextScreenButtonPress()
    {
        OnMicrophoneScreenNextPress?.Invoke();

        // microphoneScreen.SetActive(false);
        // vibroAcousticScreen.SetActive(true);
    }

    public void HeadphoneNextScreenButtonPress()
    {
        OnHeadphoneScreenNextPress?.Invoke();

        // headphoneScreen.SetActive(false);
        // microphoneScreen.SetActive(true);
    }

    public void VibroAcousticNextScreenButtonPress()
    {
        OnVibroacousticScreenNextPress?.Invoke();

        // vibroAcousticScreen.SetActive(false);
        // lightGlassScreen.SetActive(true);
    }

    public void LightGlassNextScreenButtonPress()
    {
        OnLightglassScreenNextPress?.Invoke();

        // lightGlassScreen.SetActive(false);
        // startMeditationScreen.SetActive(true);
    }
    public void HeadphoneTroubleshootingButtonPress()
    {
        OnHeadphoneTroubleshootingPress?.Invoke();
    }
    public void SkipMeditationSessionButtonPress()
    {
        OnSkipSessionButtonPress?.Invoke();

        // startMeditationScreen.SetActive(false);
        // endMeditationScreen.SetActive(true);
    }

    public void MeditationQuitButtonPress()
    {
        OnMeditationQuitPress?.Invoke();

        endMeditationScreen.SetActive(false);
    }


}
