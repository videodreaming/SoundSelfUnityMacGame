using UnityEngine;

/// <summary>
/// Session timing: authoritative clocks. <see cref="TotalElapsedTime"/> = session wall clock;
/// <see cref="CountdownThisSection"/> = time left in the current main segment (e.g. playground / interactive; historically “countdown to savasana”);
/// <see cref="CountdownFull"/> = time left for the full session including post-unguided closing, so it tracks Section + closing when both tick together.
/// Both decrement while <see cref="IsCountdownRunning"/> after <see cref="BeginCountdownPair"/>.
/// <see cref="DisplayTime"/> mirrors <see cref="CountdownFull"/> as clock text for inspector/debug; it refreshes when the whole displayed second changes, not every frame.
/// </summary>
public class TimeTrackerScript : MonoBehaviour
{
    public static TimeTrackerScript instance { get; private set; }

    [Header("Elapsed time (session open)")]
    [Tooltip("Authoritative elapsed seconds since session start. Always increases during Update.")]
    public float TotalElapsedTime;

    [Tooltip("Formatted CountdownFull (m:ss) for inspector/debug; see CircleCountdownTimerUI. Refreshes when the clock’s whole second changes, not every frame.")]
    public string DisplayTime;

    private int _displayTimeLastFlooredFullSeconds = int.MinValue;

    [Header("Countdown — this section (main) vs full session")]
    [Tooltip("Time left in the main segment (e.g. until end of playground logic). Ticks down; clamps at 0 while Full may still run.")]
    [SerializeField] private float _countdownThisSection = 1_000_000f;
    [Tooltip("Time left for full session including post-unguided closing. When this hits 0, countdown running stops.")]
    [SerializeField] private float _countdownFull = 1_000_000f;
    [SerializeField] private bool _countdownRunning;
    [SerializeField] private float _configuredThisSectionAtLastConfigure;
    [SerializeField] private float _configuredFullAtLastConfigure;

    [Header("Tutorial reference")]
    [Tooltip("Wired in inspector for session wiring; not used to drive playground elapsed time.")]
    [SerializeField] private Tutorial tutorial;

    [Header("Time since playground start")]
    [SerializeField] private float _timeSincePlaygroundStart;
    [SerializeField] private bool _timeSincePlaygroundStartTimerStarted;

    [Header("CSV-derived inputs (Phase 1 mirror; CSV still owns field until Phase 4)")]
    [Tooltip("Closing / post-unguided content duration from CSV.")]
    [SerializeField] private float _totalTimeOfPostUnguidedVocalizationContent;

    [Tooltip("Set by CSVLoader when TimeLeftInitializations() completes with recognized game + sub game mode (tracker inputs hydrated). StartCountdown logs if false; post-unguided duration may still be 0 intentionally.")]
    [SerializeField] private bool _sessionTimingInitializedFromCsv;

    [Header("Latches")]
    [SerializeField] private bool _countdownCompleteLached;

    [Header("Debug")]
    [Tooltip("Periodic logs of TotalElapsedTime and countdown pair. Cadence follows session time (Time.deltaTime / timeScale); when timeScale is 0, ticks pause.")]
    [SerializeField] public bool debugAllowTimingLogs = true;
    [Tooltip("Seconds of session time between timing logs (same clock as TotalElapsedTime; affected by Time.timeScale).")]
    [SerializeField] private float _debugTimingLogIntervalSeconds = 1f;
    private float _lastTimingLogTotalElapsed;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        TotalElapsedTime = 0f;
        DisplayTime = "0:00";
        _lastTimingLogTotalElapsed = 0f;
    }

    void Start()
    {
        if (tutorial == null)
        {
            Debug.LogError("TimeTrackerScript: Tutorial is not set. Assign in inspector for session wiring.");
        }
        Debug.Log($"TimeTrackerScript: Start called. Time.timeScale = {Time.timeScale}");
    }

    void Update()
    {
        TotalElapsedTime += Time.deltaTime;
        TickDebugTimingLogs();
        UpdateDisplayTime();
        TickTimeSincePlaygroundStartInternal(Time.deltaTime);

        if (_countdownRunning)
        {
            if (_countdownThisSection > 0f)
            {
                _countdownThisSection -= Time.deltaTime;
                if (_countdownThisSection < 0f)
                    _countdownThisSection = 0f;
            }

            if (_countdownFull > 0f)
            {
                _countdownFull -= Time.deltaTime;
                if (_countdownFull <= 0f)
                {
                    _countdownFull = 0f;
                    _countdownThisSection = 0f;
                    _countdownRunning = false;
                    _countdownCompleteLached = true;
                }
            }
        }
    }

    private void UpdateDisplayTime()
    {
        float t = Mathf.Max(0f, _countdownFull);
        int floored = Mathf.FloorToInt(t);
        if (floored == _displayTimeLastFlooredFullSeconds)
            return;

        _displayTimeLastFlooredFullSeconds = floored;
        int minutes = floored / 60;
        int seconds = floored % 60;
        DisplayTime = $"{minutes}:{seconds:D2}";
    }

    private void TickDebugTimingLogs()
    {
        if (!debugAllowTimingLogs)
            return;
        float interval = Mathf.Max(0.1f, _debugTimingLogIntervalSeconds);
        if (TotalElapsedTime - _lastTimingLogTotalElapsed < interval)
            return;
        _lastTimingLogTotalElapsed = TotalElapsedTime;
        int totalSeconds = Mathf.RoundToInt(TotalElapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        int sectionMinutes = Mathf.FloorToInt(_countdownThisSection / 60f);
        int sectionSeconds = Mathf.FloorToInt(_countdownThisSection % 60f);
        int fullMinutes = Mathf.FloorToInt(_countdownFull / 60f);
        int fullSeconds = Mathf.FloorToInt(_countdownFull % 60f);

        Debug.Log(
            $"TimeTrackerScript: [tick] {minutes}:{seconds:D2}    " +
            $"[CountdownThisSection] {sectionMinutes}:{sectionSeconds:D2}    " + 
            $"[CountdownFull] {fullMinutes}:{fullSeconds:D2}"
        );
    }

    // -------------------------------------------------------------------------
    // Countdown API
    // -------------------------------------------------------------------------

    /// <summary>Time left in the main segment (protocol milestones, LastMinute until section end).</summary>
    public float CountdownThisSection => _countdownThisSection;

    /// <summary>Time left for the full session including post-unguided closing.</summary>
    public float CountdownFull => _countdownFull;

    public bool IsCountdownRunning => _countdownRunning;

    /// <summary>Last configured full-session baseline (from <see cref="ConfigureCountdownPair"/> / <see cref="BeginCountdownPair"/>). May differ from live <see cref="CountdownFull"/> after flows that restore baseline only (e.g. ClosingDuration).</summary>
    public float ConfiguredFullAtLastConfigure => _configuredFullAtLastConfigure;

    /// <summary>Sets only the stored full-session baseline; does <b>not</b> change live <see cref="CountdownFull"/> / <see cref="CountdownThisSection"/>.</summary>
    public void SetConfiguredFullBaselineOnly(float seconds)
    {
        _configuredFullAtLastConfigure = Mathf.Max(0f, seconds);
    }

    /// <summary>Configure both start values without starting the clock. When already running, updates pending baselines for the next Begin.</summary>
    public void ConfigureCountdownPair(float thisSectionSeconds, float fullSeconds)
    {
        WarnIfPairLooksWrong(thisSectionSeconds, fullSeconds);

        float sec = Mathf.Max(0f, thisSectionSeconds);
        float ful = Mathf.Max(0f, fullSeconds);
        ApplyConfiguredThisSection(sec, syncLiveWhenIdle: !_countdownRunning);
        ApplyConfiguredFull(ful, syncLiveWhenIdle: !_countdownRunning);
    }

    /// <summary>Updates the stored main-segment baseline (and live value when idle). Pair logic uses <see cref="ConfigureCountdownPair"/>.</summary>
    public void ConfigureCountdownThisSectionOnly(float thisSectionSeconds)
    {
        float sec = Mathf.Max(0f, thisSectionSeconds);
        ApplyConfiguredThisSection(sec, syncLiveWhenIdle: !_countdownRunning);
    }

    /// <summary>Updates the stored full-session baseline (and live value when idle). Pair logic uses <see cref="ConfigureCountdownPair"/>.</summary>
    public void ConfigureCountdownFullOnly(float fullSeconds)
    {
        float ful = Mathf.Max(0f, fullSeconds);
        if (ful <= 0f)
            Debug.LogError("TimeTrackerScript: ConfigureCountdownFullOnly — [CountdownFull] is " + ful + " (should be > 0). Expect strange behavior.");
        ApplyConfiguredFull(ful, syncLiveWhenIdle: !_countdownRunning);
    }

    /// <summary>Starts both countdowns from last configured pair, or optional overrides (see <see cref="ConfigureCountdownPair"/>).</summary>
    public void BeginCountdownPair(float? thisSectionSeconds = null, float? fullSeconds = null)
    {
        if (thisSectionSeconds != null || fullSeconds != null)
        {
            float sec = thisSectionSeconds ?? _configuredThisSectionAtLastConfigure;
            float ful = fullSeconds ?? _configuredFullAtLastConfigure;
            ConfigureCountdownPair(sec, ful);
        }

        float newSec = _configuredThisSectionAtLastConfigure;
        float newFul = _configuredFullAtLastConfigure;

        if (_countdownRunning)
            Debug.LogWarning("TimeTrackerScript: BeginCountdownPair re-entry — [CountdownThisSection] was " + _countdownThisSection + ", [CountdownFull] was " + _countdownFull + "; new starts " + newSec + " / " + newFul + ".");

        if (newFul <= 0f)
            Debug.LogError("TimeTrackerScript: BeginCountdownPair — [CountdownFull] new start " + newFul + " (should be > 0).");
        else
        {
            // ForceSetBothCountdownsAndStop (e.g. Countdown_StopCountdowns) sets CountdownCompleteLatched true; a new positive arm must clear it
            // so UI/progress helpers do not treat the session as still "completed" while live timers restart.
            SetCountdownCompleteLatched(false);
        }

        _countdownThisSection = newSec;
        _countdownFull = newFul;
        StartCountdownTickingFromLiveValues();
    }

    private void ApplyConfiguredThisSection(float sec, bool syncLiveWhenIdle)
    {
        _configuredThisSectionAtLastConfigure = sec;
        if (syncLiveWhenIdle)
            _countdownThisSection = sec;
    }

    private void ApplyConfiguredFull(float ful, bool syncLiveWhenIdle)
    {
        _configuredFullAtLastConfigure = ful;
        if (syncLiveWhenIdle)
            _countdownFull = ful;
    }

    private static void WarnIfPairLooksWrong(float thisSectionSeconds, float fullSeconds)
    {
        if (thisSectionSeconds <= 0f && fullSeconds > 0f)
            Debug.LogWarning("TimeTrackerScript: ConfigureCountdownPair — [CountdownThisSection] is " + thisSectionSeconds + " while [CountdownFull] is " + fullSeconds + ".");
        if (fullSeconds <= 0f)
            Debug.LogError("TimeTrackerScript: ConfigureCountdownPair — [CountdownFull] is " + fullSeconds + " (should be > 0). Expect strange behavior.");

        float sec = Mathf.Max(0f, thisSectionSeconds);
        float ful = Mathf.Max(0f, fullSeconds);
        if (ful + 0.01f < sec)
            Debug.LogWarning("TimeTrackerScript: ConfigureCountdownPair — [CountdownFull]=" + ful + " < [CountdownThisSection]=" + sec + "; check StartCountdown variant math.");
    }

    private void StartCountdownTickingFromLiveValues()
    {
        _countdownRunning = true;
    }

    /// <summary>At end of playground / LastMinute: resync <see cref="CountdownFull"/> to CSV closing duration and log drift if &gt; 10s from previous <see cref="CountdownFull"/>.</summary>
    public void SnapCountdownFullToClosingContentDurationAndLogDrift(float closingContentDurationSeconds, string contextLabel)
    {
        float closing = Mathf.Max(0f, closingContentDurationSeconds);
        float fullBefore = _countdownFull;
        _countdownFull = closing;
        _configuredFullAtLastConfigure = closing;

        float drift = Mathf.Abs(fullBefore - closing);
        if (drift > 10f)
        {
            Debug.LogWarning("TimeTrackerScript: [" + contextLabel + "] [CountdownFull] Drift " + drift + " s before snap — was " + fullBefore + " s, expected closing " + closing + " s. Snapped [CountdownFull] to closing.");
        }
        else
        {
            Debug.Log("TimeTrackerScript: [" + contextLabel + "] [CountdownFull] Closing-phase check: before=" + fullBefore + " s, snapped to " + closing + " s (drift " + drift + " s).");
        }
    }

    public bool CountdownCompleteLatched => _countdownCompleteLached;

    public void SetCountdownCompleteLatched(bool latched)
    {
        _countdownCompleteLached = latched;
    }

    /// <summary>Force both values and stop running (e.g. Savasana / Playground complete — typically <c>0, 0</c>).</summary>
    public void ForceSetBothCountdownsAndStop(float thisSectionValue, float fullValue)
    {
        _countdownRunning = false;
        _countdownThisSection = thisSectionValue;
        _countdownFull = fullValue;
        _configuredThisSectionAtLastConfigure = thisSectionValue;
        _configuredFullAtLastConfigure = fullValue;
        SetCountdownCompleteLatched(true);
    }

    // -------------------------------------------------------------------------
    // Time since playground start
    // -------------------------------------------------------------------------

    public float TimeSincePlaygroundStart => _timeSincePlaygroundStart;

    private void TickTimeSincePlaygroundStartInternal(float deltaTime)
    {
        if (!_timeSincePlaygroundStartTimerStarted)
            return;
        _timeSincePlaygroundStart += deltaTime;
    }

    /// <summary>Called from <see cref="SoundSelf.Sequence.PlaygroundStageHandler.Enter"/> — resets to zero and begins accumulation.</summary>
    public void OnPlaygroundStageEntered()
    {
        TimeTrackerScript.instance?.ResetTimeSincePlaygroundStart();
        _timeSincePlaygroundStartTimerStarted = true;
    }

    /// <summary>Clears elapsed time to zero and stops accumulation until <see cref="OnPlaygroundStageEntered"/>.</summary>
    public void ResetTimeSincePlaygroundStart()
    {
        _timeSincePlaygroundStart = 0f;
        _timeSincePlaygroundStartTimerStarted = false;
    }

    /// <summary>Dev / cheat paths (e.g. <see cref="Sequencer.StartPlayground"/>) — does not arm the timer if it was never started.</summary>
    public void SetTimeSincePlaygroundStart(float seconds)
    {
        _timeSincePlaygroundStart = seconds;
    }

    // -------------------------------------------------------------------------
    // CSV inputs
    // -------------------------------------------------------------------------

    public float TotalTimeOfPostUnguidedVocalizationContent => _totalTimeOfPostUnguidedVocalizationContent;

    /// <summary>True after <see cref="CSVLoader"/> completes <c>TimeLeftInitializations</c> with a recognized <c>gameMode</c> and <c>contentPack</c> (where that product uses content packs). Post-unguided duration may still be zero by design.</summary>
    public bool SessionTimingInitializedFromCsv => _sessionTimingInitializedFromCsv;

    /// <summary>Called from <see cref="CSVLoader"/> at end of <c>TimeLeftInitializations</c> after hydrating tracker inputs (post-unguided duration, etc.).</summary>
    public void MarkSessionTimingInitializedFromCsv()
    {
        _sessionTimingInitializedFromCsv = true;
    }

    public void SetTotalTimeOfPostUnguidedVocalizationContent(float seconds)
    {
        _totalTimeOfPostUnguidedVocalizationContent = Mathf.Max(0f, seconds);
    }

    /// <summary>UI helper: <see cref="CountdownFull"/> as “M minutes S seconds”; non-positive shows as zero.</summary>
    public string FormatCountdownFullMinutesAndSeconds()
    {
        float t = Mathf.Max(0f, CountdownFull);
        int minutes = Mathf.FloorToInt(t / 60);
        int seconds = Mathf.FloorToInt(t % 60);
        return $"{minutes} minutes {seconds} seconds";
    }
}
