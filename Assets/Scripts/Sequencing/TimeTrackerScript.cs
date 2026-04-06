using UnityEngine;

/// <summary>
/// Session timing (Phase 1+): authoritative clocks. <see cref="TotalElapsedTime"/> = session wall clock;
/// <see cref="CountdownSeconds"/> = main countdown (ticks only when <see cref="IsCountdownRunning"/> after <see cref="BeginCountdown"/>);
/// <see cref="DisplayTime"/> = formatted <see cref="TotalElapsedTime"/>; <see cref="TimeSinceTutorial"/> ticks in <c>Update</c> when tutorial is complete or <see cref="StartTimeSinceTutorialTimer"/> was called.
/// Countdown writes: <see cref="ConfigureCountdownSeconds"/>, <see cref="MirrorLegacyCountdown"/> (while not running), and
/// <see cref="BeginCountdown"/> (starts the tick). <see cref="MirrorLegacyCountdown"/> does not imply a separate Sequencer clock anymore.
/// </summary>
public class TimeTrackerScript : MonoBehaviour
{
    public static TimeTrackerScript instance { get; private set; }

    [Header("Elapsed time (session open)")]
    [Tooltip("Authoritative elapsed seconds since session start. Always increases during Update.")]
    public float TotalElapsedTime;

    [Tooltip("Formatted from TotalElapsedTime only.")]
    public string DisplayTime;

    [Header("Countdown (main session \"time left\")")]
    [SerializeField] private float _countdownSeconds = 1_000_000f;
    [SerializeField] private bool _countdownRunning;
    [SerializeField] private float _configuredCountdownAtLastConfigure;

    [Header("Time since tutorial timer")]
    [Tooltip("If unset, resolved once in Start via FindObjectOfType.")]
    [SerializeField] private Tutorial tutorial;
    [SerializeField] private float _timeSinceTutorial;
    [SerializeField] private bool _timeSinceTutorialTimerStarted;

    [Header("CSV-derived inputs (Phase 1 mirror; CSV still owns field until Phase 4)")]
    [Tooltip("Closing / post-unguided content duration from CSV.")]
    [SerializeField] private float _totalTimeOfPostUnguidedVocalizationContent;

    [Tooltip("Set by CSVLoader when TimeLeftInitializations() completes (timeLeft valid and countdown mirrored). StartCountdown requires this; post-unguided duration may still be 0 intentionally.")]
    [SerializeField] private bool _sessionTimingInitializedFromCsv;

    [Header("Latches")]
    [SerializeField] private bool _countdownCompleteLached;

    [Header("Debug")]
    [SerializeField] public bool debugAllowTimingLogs;
    private float _lastTimingLogTime;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        TotalElapsedTime = 0f;
        DisplayTime = "0 minutes 0 seconds";
        _lastTimingLogTime = 0f;
        if(tutorial == null)
        {
            Debug.LogError("TimeTrackerScript: Tutorial is not set. This is required for tutorial elapsed tracking.");
        }
    }

    void Update()
    {
        TotalElapsedTime += Time.deltaTime;
        TickDebugTimingLogs();
        UpdateDisplayTime();
        TickTimeSinceTutorialTimerInternal(Time.deltaTime);

        if (_countdownRunning && _countdownSeconds > 0f)
        {
            _countdownSeconds -= Time.deltaTime;
            if (_countdownSeconds <= 0f)
            {
                _countdownSeconds = -1f;
                _countdownRunning = false;
                _countdownCompleteLached = true;
            }
        }
    }

    private void UpdateDisplayTime()
    {
        int minutes = Mathf.FloorToInt(TotalElapsedTime / 60);
        int seconds = Mathf.FloorToInt(TotalElapsedTime % 60);
        DisplayTime = $"{minutes} minutes {seconds} seconds";
    }

    private void TickDebugTimingLogs()
    {
        if (!debugAllowTimingLogs)
            return;
        if (TotalElapsedTime - _lastTimingLogTime < 1.0f)
            return;
        int totalSeconds = Mathf.RoundToInt(TotalElapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        Debug.Log($"TimeTrackerScript: [tick] {minutes}:{seconds:D2}");
        _lastTimingLogTime = TotalElapsedTime;
    }

    // -------------------------------------------------------------------------
    // Countdown API
    // -------------------------------------------------------------------------

    /// <summary>Current session countdown in seconds. When not running, still reflects last configured or mirrored value.</summary>
    public float CountdownSeconds => _countdownSeconds;

    public bool IsCountdownRunning => _countdownRunning;

    /// <summary>Sets the countdown value without starting the clock. When already running, only updates the pending configure value for logging on next BeginCountdown.</summary>
    public void ConfigureCountdownSeconds(float seconds)
    {
        if(seconds <= 0f)
        {
            Debug.LogError("TimeTrackerScript: ConfigureCountdownSeconds() - seconds is " + seconds + " (should be > 0). Expect strange behavior.");
        }

        float v = Mathf.Max(0f, seconds);
        _configuredCountdownAtLastConfigure = v;
        if (!_countdownRunning)
        {
            _countdownSeconds = v;
        }
    }

    /// <summary>
    /// Starts (or restarts) countdown decrement using the last <see cref="ConfigureCountdownSeconds"/> value
    /// or an optional new value. If an explicit <paramref name="seconds"/> is passed, it updates the countdown value before starting.
    /// If already running, logs previous and new start for debug visibility (double stage entry detection).
    /// </summary>
    public void BeginCountdown(float? seconds = null)
    {
        if(seconds != null)
        {
            ConfigureCountdownSeconds(seconds.Value);
        }
        float newStart = _configuredCountdownAtLastConfigure;
        if (_countdownRunning)
        {
            Debug.LogWarning($"TimeTrackerScript: BeginCountdown re-entry — previous CountdownSeconds was {_countdownSeconds}, new start value {newStart}.");
        }
        if (newStart <= 0f)
        {
            Debug.LogError("TimeTrackerScript: BeginCountdown() - newStart is " + newStart + " (should be > 0). Expect strange behavior.");
        }
        _countdownSeconds = newStart;
        _countdownRunning = true;
    }

    /// <summary>
    /// While <see cref="IsCountdownRunning"/> is false, copies <paramref name="valueFromSequencer"/> into stored countdown and the
    /// configure baseline (e.g. <see cref="Sequencer.SetCountdownToSavasana"/> after CSV). No-op while the tracker is actively ticking.
    /// </summary>
    public void MirrorLegacyCountdown(float valueFromSequencer)
    {
        if (_countdownRunning)
            return;
        _countdownSeconds = valueFromSequencer;
        _configuredCountdownAtLastConfigure = _countdownSeconds;
    }

    public bool CountdownCompleteLatched => _countdownCompleteLached;

    public void SetCountdownCompleteLatched(bool latched)
    {
        _countdownCompleteLached = latched;
    }

    /// <summary>Force countdown to a value and stop running (e.g. Savasana -1 semantics — Phase 7 may refine).</summary>
    public void ForceSetCountdownSecondsAndStop(float seconds)
    {
        _countdownRunning = false;
        _countdownSeconds = seconds;
        _configuredCountdownAtLastConfigure = seconds;
        if(seconds == -1)
        {
            SetCountdownCompleteLatched(true);
        }
    }

    // -------------------------------------------------------------------------
    // Time since tutorial timer
    // -------------------------------------------------------------------------

    public float TimeSinceTutorial => _timeSinceTutorial;

    private void TickTimeSinceTutorialTimerInternal(float deltaTime)
    {
        if (!_timeSinceTutorialTimerStarted)
            return;
        _timeSinceTutorial += deltaTime;
    }

    /// <summary>Idempotent: subsequent calls do not reset <see cref="TimeSinceTutorial"/>.</summary>
    public void StartTimeSinceTutorialTimer()
    {
        _timeSinceTutorialTimerStarted = true;
    }

    /// <summary>Resets <see cref="TimeSinceTutorial"/> to zero and pauses accumulation until <see cref="StartTimeSinceTutorialTimer"/> or tutorial-complete path.</summary>
    public void ResetTimeSinceTutorialTimer()
    {
        _timeSinceTutorial = 0f;
        _timeSinceTutorialTimerStarted = false;
    }

    public void SetTimeSinceTutorial(float seconds)
    {
        _timeSinceTutorial = seconds;
    }

    // -------------------------------------------------------------------------
    // CSV inputs
    // -------------------------------------------------------------------------

    public float TotalTimeOfPostUnguidedVocalizationContent => _totalTimeOfPostUnguidedVocalizationContent;

    /// <summary>True after <see cref="CSVLoader"/> completes <c>TimeLeftInitializations</c> for this session (mirrored countdown + tracker inputs). Post-unguided duration may still be zero by design.</summary>
    public bool SessionTimingInitializedFromCsv => _sessionTimingInitializedFromCsv;

    /// <summary>Called from <see cref="CSVLoader"/> after <c>SetTotalTimeOfPostUnguidedVocalizationContent</c> and <c>SetCountdownToSavasana</c> on the success path (<c>timeLeft &gt; 0</c>).</summary>
    public void MarkSessionTimingInitializedFromCsv()
    {
        _sessionTimingInitializedFromCsv = true;
    }

    public void SetTotalTimeOfPostUnguidedVocalizationContent(float seconds)
    {
        _totalTimeOfPostUnguidedVocalizationContent = Mathf.Max(0f, seconds);
    }

    // -------------------------------------------------------------------------
    // Legacy compatibility (merge countdown with former time-left helpers)
    // -------------------------------------------------------------------------

    /// <summary>Alias for <see cref="ConfigureCountdownSeconds"/> during migration.</summary>
    public void SetTimeLeftSeconds(float seconds)
    {
        ConfigureCountdownSeconds(seconds);
        int minutes = Mathf.FloorToInt(CountdownSeconds / 60);
        int secs = Mathf.FloorToInt(CountdownSeconds % 60);
        Debug.Log("TimeTrackerScript: SetTimeLeftSeconds → ConfigureCountdownSeconds " + minutes + " min " + secs + " s");
    }

    public float GetTimeLeftSeconds() => CountdownSeconds;

    public string GetTimeLeftFormattedToMinutesAndSeconds()
    {
        int minutes = Mathf.FloorToInt(CountdownSeconds / 60);
        int seconds = Mathf.FloorToInt(CountdownSeconds % 60);
        return $"{minutes} minutes {seconds} seconds";
    }
}
