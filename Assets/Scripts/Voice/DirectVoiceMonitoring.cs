using System;
using System.Collections;
using System.Threading;
using UnityEngine;

/// <summary>
/// Provides real-time monitoring of microphone input using buffered pull transport from ImitoneVoiceIntepreter ring data.
/// Raw and normalized modes both consume pipeline-published mono streams with fixed-latency read cursors.
/// Note: ring-buffer reads use <see cref="ImitoneVoiceIntepreter"/> as the single mic-ingest owner (0.7c-ii collapsed the old split component).
/// </summary>
public class DirectVoiceMonitoring : MonoBehaviour
{
    private enum PlaybackHeadSeekReason
    {
        StartPrime = 0,
        StreamSwitchPrime = 1
    }

    public enum MonitoringStreamSource
    {
        Normalized = 0,
        Raw = 1
    }

    [Header("Core References")]
    [Tooltip("Single source for both tone state AND normalized/raw mic ring reads after the 0.7 merge.")]
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    
    [Header("Audio Output")]
    [Tooltip("AudioSource used for monitoring playback. If not assigned, will be created automatically.")]
    public AudioSource monitoringSource;
    
    [Header("Monitoring Controls")]
    [SerializeField] private bool monitoringEnabled = true;
    [SerializeField] [Range(0f, 1f)] private float monitoringVolume = 1f;
    [Tooltip("Select which mic ring stream (raw vs. normalized) to monitor for A/B testing and debugging.")]
    [SerializeField] private MonitoringStreamSource monitoringStreamSource = MonitoringStreamSource.Normalized;
    [Tooltip("When true, monitoring is attenuated (post dynamic scaling) by monitoringAttenuationMultiplier.")]
    [SerializeField] private bool monitoringAttenuated = false;
    [SerializeField] [Range(0f, 1f)] private float monitoringAttenuationMultiplier = 0.4f;
    [Tooltip("Smoothing time for attenuation transitions to reduce click risk when toggled.")]
    [SerializeField] [Range(0.005f, 0.25f)] private float attenuationSmoothingSeconds = 0.03f;

    [Header("Dynamic Monitoring Volume")]
    [Tooltip("When enabled, monitoring volume follows gameOn/toneActive/chant state (migrated from MusicSystem1).")]
    [SerializeField] private bool dynamicVolumeEnabled = true;
    [SerializeField] private float gameOnRiseSpeed = 2f;
    [SerializeField] private float gameOnFallSpeed = 0.5f;
    [SerializeField] private float chargeRiseSpeed = 1f;
    [SerializeField] private float chargeFallSpeed = 1f;
    [Header("Chant Presence Gain Shaping")]
    [Tooltip("Chant gain at the bottom of the dB ramp (e.g. -35 dB).")]
    [SerializeField] private float chantPresenceMinGainDb = -35f;
    [Tooltip("Chant gain at the top of the dB ramp (typically 0 dB).")]
    [SerializeField] private float chantPresenceMaxGainDb = 0f;
    [Tooltip("Initial chantLerpFast range that also gets an extra linear fade-to-zero multiplier.")]
    [SerializeField] [Range(0.01f, 0.5f)] private float chantPresenceLinearFloorRange = 0.125f;

    [Header("Buffered Pull Transport")]
    [Tooltip("Read cursor delay behind live mic write head for buffered transport.")]
    [SerializeField] [Range(20f, 500f)] private float bufferedReadLatencyMs = 125f;
    [SerializeField] private int bufferUnderflowWarningThresholdPerWindow = 8;
    [SerializeField] private int bufferOverflowWarningThresholdPerWindow = 4;
    [SerializeField] private int callbackStarvationWarningThresholdPerWindow = 8;

    [Header("Reliability Telemetry")]
    [SerializeField] private bool enableReliabilityLogs = true;
    [Tooltip("Automatically reset reliability counters shortly after monitoring starts to ignore startup transients.")]
    [SerializeField] private bool autoResetTelemetryAfterMonitoringStart = true;
    [SerializeField] [Range(0f, 10f)] private float autoResetTelemetryDelaySeconds = 1f;
    [SerializeField] [Range(1f, 60f)] private float healthSummaryLogIntervalSeconds = 10f;
    [SerializeField] [Range(1f, 300f)] private float warningWindowSeconds = 60f;
    [SerializeField] private int seekWarningThresholdPerWindow = 12;
    [SerializeField] private int rebindWarningThresholdPerWindow = 4;
    [SerializeField] private int cooldownSuppressionWarningThresholdPerWindow = 30;
    [SerializeField] private bool logWindowWarnings = true;
    [SerializeField] private bool logHealthSummary = true;
    [Header("Debug Log Controls")]
    [SerializeField] private bool debugAllowMonitoringLogs = true;
    [SerializeField] private bool debugAllowMonitoringWarnings = true;
    [Header("Transition Audit")]
    [SerializeField] private bool enableTransitionAuditWarnings = true;
    [SerializeField] [Range(0.01f, 1f)] private float hardVolumeStepThreshold = 0.2f;
    [SerializeField] [Range(0.5f, 30f)] private float rebindFailureWarningIntervalSeconds = 5f;
    [SerializeField] [Range(0.1f, 5f)] private float setupRetryIntervalSeconds = 0.5f;
    [SerializeField] [Range(0.5f, 30f)] private float clipBindFailureErrorIntervalSeconds = 5f;
    
    private bool isInitialized = false;
    private AudioClip sharedMicrophoneBuffer;
    private int lastSeenCaptureEpoch = -1;
    private float gameOnLerp = 0f;
    private float chargeLerp = 0f;
    private float smoothedAttenuationScale = 1f;
    private bool attenuationScaleInitialized;
    private string nextStartPrimeReason = "start_prime";
    private float lastHealthSummaryLogTime = -999f;
    private float lastWarningWindowResetTime = 0f;
    [SerializeField] private int seekCorrectionCountTotal = 0;
    [SerializeField] private int seekCorrectionCountDrift = 0;
    [SerializeField] private int seekCorrectionCountStartPrime = 0;
    [SerializeField] private int seekCorrectionCountStreamSwitchPrime = 0;
    [SerializeField] private int seekCooldownSuppressedCount = 0;
    [SerializeField] private int captureRebindCount = 0;
    [SerializeField] private int captureRebindFailureCount = 0;
    [SerializeField] private int seekCorrectionCountWindow = 0;
    [SerializeField] private int seekCorrectionCountDriftWindow = 0;
    [SerializeField] private int seekCooldownSuppressedCountWindow = 0;
    [SerializeField] private int captureRebindCountWindow = 0;
    [SerializeField] private int bufferUnderflowFillCount = 0;
    [SerializeField] private int bufferUnderflowFillCountWindow = 0;
    [SerializeField] private int bufferUnderflowFillSamples = 0;
    [SerializeField] private int bufferOverflowDropCount = 0;
    [SerializeField] private int bufferOverflowDropCountWindow = 0;
    [SerializeField] private int bufferOverflowDropSamples = 0;
    [SerializeField] private int callbackStarvationCount = 0;
    [SerializeField] private int callbackStarvationCountWindow = 0;
    [SerializeField] private int transitionStartCount = 0;
    [SerializeField] private int transitionStopCount = 0;
    [SerializeField] private int transitionAttenuationToggleCount = 0;
    [SerializeField] private int transitionStreamSwitchCount = 0;
    [SerializeField] private int hardVolumeStepCount = 0;
    [SerializeField] private float lastVolumeStepDelta = 0f;
    [SerializeField] private string lastVolumeStepContext = "";
    [Header("Runtime Dynamic Volume Debug")]
    [Tooltip("Final linear gain applied in OnAudioFilterRead (monitoringVolume × dynamicScale × attenuation × AudioSource.volume; mute forces 0).")]
    [SerializeField] [Range(0f, 1f)] private float debugEffectiveMonitoringGain = 0f;
    [SerializeField] [Range(0f, 1f)] private float debugAppliedAudioSourceVolume = 0f;
    [SerializeField] [Range(0f, 1f)] private float debugChantLerpSlow = 0f;
    [SerializeField] [Range(0f, 1f)] private float debugChantLerpFast = 0f;
    [SerializeField] [Range(0f, 1f)] private float debugChantCharge = 0f;
    [SerializeField] private bool debugToneActive = false;
    [SerializeField] private bool debugToneActiveRaw = false;
    [SerializeField] private bool debugToneActiveConfident = false;
    [SerializeField] private bool debugToneActiveVeryConfident = false;
    [SerializeField] private bool debugToneActiveBiasTrue = false;
    private float lastAppliedMonitoringVolume = -1f;
    private volatile float effectiveMonitoringGain = 0f;
    private float lastRebindFailureWarningTime = -999f;
    private float lastClipBindErrorTime = -999f;
    private float nextSetupRetryTime = 0f;
    private bool telemetryAutoResetPending;
    private float telemetryAutoResetAtTime;
    private int rawReadPosition = -1;
    private long rawReadTotalSamples = 0;
    private int normalizedReadPosition = -1;
    private long normalizedReadTotalSamples = 0;
    private float[] monitoringMonoReadBuffer = new float[0];

    /// <summary>
    /// Initializes the monitoring system and AudioSource.
    /// </summary>
    private void Awake()
    {
        float initialAttenuationScale = monitoringAttenuated ? Mathf.Clamp01(monitoringAttenuationMultiplier) : 1f;
        smoothedAttenuationScale = initialAttenuationScale;
        attenuationScaleInitialized = true;
        lastWarningWindowResetTime = Time.unscaledTime;

        // Create monitoring AudioSource if not assigned
        if (monitoringSource == null)
        {
            monitoringSource = gameObject.AddComponent<AudioSource>();
            monitoringSource.playOnAwake = false;
            monitoringSource.loop = true;
            monitoringSource.volume = 1f;
        }

        lastAppliedMonitoringVolume = -1f;
        effectiveMonitoringGain = 0f;
    }

    /// <summary>
    /// Sets up monitoring once ImitoneVoiceIntepreter has initialized its microphone.
    /// </summary>
    private void Start()
    {
        StartCoroutine(InitializeMonitoring());
    }

    /// <summary>
    /// Coroutine that waits for ImitoneVoiceIntepreter's mic to initialize, then sets up monitoring.
    /// </summary>
    private IEnumerator InitializeMonitoring()
    {
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;

        while (imitoneVoiceInterpreter == null && elapsed < timeout)
        {
            DbgWarn("DirectVoiceMonitoring: Waiting for ImitoneVoiceIntepreter reference...");
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (imitoneVoiceInterpreter == null)
        {
            Debug.LogError("DirectVoiceMonitoring: ImitoneVoiceIntepreter reference is missing.");
            yield break;
        }

        elapsed = 0f;
        while (!imitoneVoiceInterpreter.IsMicReady && elapsed < timeout)
        {
            DbgWarn("DirectVoiceMonitoring: Waiting for microphone initialization...");
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (!imitoneVoiceInterpreter.IsMicReady)
        {
            Debug.LogError("DirectVoiceMonitoring: Timeout waiting for microphone initialization.");
            yield break;
        }

        if (!SetupMonitoring())
        {
            nextSetupRetryTime = Time.unscaledTime + Mathf.Max(0.1f, setupRetryIntervalSeconds);
        }
    }

    /// <summary>
    /// Sets up monitoring AudioSource with a streaming clip fed by ImitoneVoiceIntepreter samples.
    /// </summary>
    private bool SetupMonitoring()
    {
        if (imitoneVoiceInterpreter == null || !imitoneVoiceInterpreter.IsMicReady)
        {
            Debug.LogError("DirectVoiceMonitoring: Microphone is not ready.");
            return false;
        }

        if (!ConfigureMonitoringSourceClip())
        {
            DbgWarn("DirectVoiceMonitoring: Setup aborted because monitoring clip bind failed.");
            isInitialized = false;
            return false;
        }
        monitoringSource.loop = true;
        monitoringSource.playOnAwake = false;

        // Mark as initialized BEFORE starting monitoring
        isInitialized = true;

        // Start playback if monitoring is enabled
        if (monitoringEnabled)
        {
            StartMonitoring();
        }

        WarnIfRawMonitoring("startup");
        DbgLog($"DirectVoiceMonitoring: Monitoring initialized from ImitoneVoiceIntepreter ({monitoringStreamSource} stream).");
        nextSetupRetryTime = 0f;
        return true;
    }

    /// <summary>
    /// Keeps monitoring volume in sync while active.
    /// </summary>
    private void Update()
    {
        if (monitoringSource == null)
            return;

        TryAutoResetTelemetryAfterStart();

        if (!isInitialized)
        {
            TryRecoverSetupIfNeeded();
            UpdateReliabilityTelemetry();
            UpdateRuntimeDynamicDebugState();
            return;
        }

        if (imitoneVoiceInterpreter != null && imitoneVoiceInterpreter.IsMicReady)
        {
            int currentCaptureEpoch = imitoneVoiceInterpreter.MicCaptureEpoch;
            if (currentCaptureEpoch != lastSeenCaptureEpoch)
            {
                if (ConfigureMonitoringSourceClip())
                {
                    captureRebindCount++;
                    captureRebindCountWindow++;
                    PrimeBufferedReadCursorForSource(monitoringStreamSource);
                    DbgLog("DirectVoiceMonitoring: Mic capture restart detected. Re-bound shared monitoring clip.");
                }
                else
                {
                    captureRebindFailureCount++;
                    float now = Time.unscaledTime;
                    if (now - lastRebindFailureWarningTime >= Mathf.Max(0.5f, rebindFailureWarningIntervalSeconds))
                    {
                        DbgWarn("DirectVoiceMonitoring: Mic capture restart detected, but monitoring clip rebind failed.");
                        lastRebindFailureWarningTime = now;
                    }
                }
            }
        }

        ApplyMonitoringVolume("update");
        UpdateReliabilityTelemetry();
        UpdateRuntimeDynamicDebugState();
    }

    private void UpdateRuntimeDynamicDebugState()
    {
        debugEffectiveMonitoringGain = Mathf.Clamp01(effectiveMonitoringGain);
        debugAppliedAudioSourceVolume = monitoringSource != null ? monitoringSource.volume : 0f;

        if (GameValues.instance != null)
        {
            debugChantLerpSlow = GameValues.instance._chantLerpSlow;
            debugChantLerpFast = GameValues.instance._chantLerpFast;
            debugChantCharge = GameValues.instance._chantCharge;
        }
        else
        {
            debugChantLerpSlow = 0f;
            debugChantLerpFast = 0f;
            debugChantCharge = 0f;
        }

        if (imitoneVoiceInterpreter != null)
        {
            debugToneActive = imitoneVoiceInterpreter.toneActive;
            debugToneActiveRaw = imitoneVoiceInterpreter.toneActiveRaw;
            debugToneActiveConfident = imitoneVoiceInterpreter.toneActiveConfident;
            debugToneActiveVeryConfident = imitoneVoiceInterpreter.toneActiveVeryConfident;
            debugToneActiveBiasTrue = imitoneVoiceInterpreter.toneActiveBiasTrue;
        }
        else
        {
            debugToneActive = false;
            debugToneActiveRaw = false;
            debugToneActiveConfident = false;
            debugToneActiveVeryConfident = false;
            debugToneActiveBiasTrue = false;
        }
    }

    private void TryAutoResetTelemetryAfterStart()
    {
        if (!telemetryAutoResetPending)
        {
            return;
        }

        if (Time.unscaledTime < telemetryAutoResetAtTime)
        {
            return;
        }

        telemetryAutoResetPending = false;
        ResetReliabilityTelemetryCounters();
        DbgLog("DirectVoiceMonitoring: Auto-reset reliability telemetry after startup delay.");
    }

    private void TryRecoverSetupIfNeeded()
    {
        float now = Time.unscaledTime;
        if (now < nextSetupRetryTime)
        {
            return;
        }

        nextSetupRetryTime = now + Mathf.Max(0.1f, setupRetryIntervalSeconds);

        if (imitoneVoiceInterpreter == null || !imitoneVoiceInterpreter.IsMicReady)
        {
            return;
        }

        if (SetupMonitoring())
        {
            DbgLog("DirectVoiceMonitoring: Recovered monitoring initialization after transient setup failure.");
        }
    }

    private void PrimeBufferedReadCursorForSource(MonitoringStreamSource source)
    {
        if (imitoneVoiceInterpreter == null)
        {
            return;
        }

        float delayMs = Mathf.Max(0f, bufferedReadLatencyMs);
        bool success = false;
        if (source == MonitoringStreamSource.Normalized)
        {
            success = imitoneVoiceInterpreter.TryCreateNormalizedReadCursorBehindMs(delayMs, out normalizedReadPosition, out normalizedReadTotalSamples);
        }
        else
        {
            success = imitoneVoiceInterpreter.TryCreateRawReadCursorBehindMs(delayMs, out rawReadPosition, out rawReadTotalSamples);
        }

        if (!success)
        {
            DbgWarn($"DirectVoiceMonitoring: Failed to prime buffered read cursor for {source} stream.");
        }
    }

    private void TrackStartPrimeReason()
    {
        PlaybackHeadSeekReason reason = nextStartPrimeReason == "stream_switch_prime"
            ? PlaybackHeadSeekReason.StreamSwitchPrime
            : PlaybackHeadSeekReason.StartPrime;
        seekCorrectionCountTotal++;
        seekCorrectionCountWindow++;

        switch (reason)
        {
            case PlaybackHeadSeekReason.StartPrime:
                seekCorrectionCountStartPrime++;
                break;
            case PlaybackHeadSeekReason.StreamSwitchPrime:
                seekCorrectionCountStreamSwitchPrime++;
                break;
        }

        nextStartPrimeReason = "start_prime";
    }

    /// <summary>
    /// Starts monitoring playback.
    /// </summary>
    public void StartMonitoring()
    {
        if (!isInitialized)
        {
            nextStartPrimeReason = "start_prime";
            DbgWarn("DirectVoiceMonitoring: Cannot start monitoring - not initialized yet.");
            return;
        }

        if (monitoringSource == null || monitoringSource.clip == null)
        {
            nextStartPrimeReason = "start_prime";
            Debug.LogError("DirectVoiceMonitoring: Cannot start monitoring - AudioSource or buffer is null.");
            return;
        }

        monitoringEnabled = true;
        ApplyMonitoringVolume("start_monitoring");

        if (!monitoringSource.isPlaying)
        {
            PrimeBufferedReadCursorForSource(monitoringStreamSource);
            TrackStartPrimeReason();
            monitoringSource.Play();
            transitionStartCount++;
            if (autoResetTelemetryAfterMonitoringStart)
            {
                telemetryAutoResetPending = true;
                telemetryAutoResetAtTime = Time.unscaledTime + Mathf.Max(0f, autoResetTelemetryDelaySeconds);
            }
            DbgLog("DirectVoiceMonitoring: Monitoring started.");
        }
    }

    /// <summary>
    /// Stops monitoring playback.
    /// </summary>
    public void StopMonitoring()
    {
        monitoringEnabled = false;
        telemetryAutoResetPending = false;
        if (monitoringSource != null && monitoringSource.isPlaying)
        {
            monitoringSource.Stop();
            transitionStopCount++;
            DbgLog("DirectVoiceMonitoring: Monitoring stopped");
        }
    }

    /// <summary>
    /// Sets the monitoring volume.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    public void SetMonitoringVolume(float volume)
    {
        monitoringVolume = Mathf.Clamp01(volume);
        if (monitoringSource != null)
        {
            ApplyMonitoringVolume("set_monitoring_volume");
        }
    }

    /// <summary>
    /// Toggles monitoring on/off.
    /// </summary>
    public void ToggleMonitoring()
    {
        if (monitoringEnabled)
        {
            StopMonitoring();
        }
        else
        {
            StartMonitoring();
        }
    }

    /// <summary>
    /// Applies or removes monitoring attenuation while preserving dynamic volume behavior.
    /// </summary>
    public void AttenuateMonitoring(bool attenuated)
    {
        if (monitoringAttenuated == attenuated)
        {
            return;
        }

        monitoringAttenuated = attenuated;
        transitionAttenuationToggleCount++;
        ApplyMonitoringVolume("attenuation_toggle");
    }

    /// <summary>
    /// Public external control to enable/disable direct voice monitoring.
    /// </summary>
    public void SetDirectVoiceMonitoringEnabled(bool enabled)
    {
        if (enabled)
        {
            StartMonitoring();
        }
        else
        {
            StopMonitoring();
        }
    }

    /// <summary>
    /// Sets monitoring stream source.
    /// Stream source changes are only supported while monitoring is not actively running.
    /// </summary>
    public void SetMonitoringStreamSource(MonitoringStreamSource source)
    {
        bool isMonitoringRunning = monitoringSource != null && monitoringSource.isPlaying && monitoringEnabled;
        if (isMonitoringRunning)
        {
            DbgWarn("DirectVoiceMonitoring: Ignoring monitoring stream source change while monitoring is running. Stop monitoring before switching sources.");
            return;
        }

        bool changed = monitoringStreamSource != source;
        monitoringStreamSource = source;
        if (changed)
        {
            transitionStreamSwitchCount++;
        }

        // Keep clip binding current for the next monitoring start.
        ConfigureMonitoringSourceClip();
        PrimeBufferedReadCursorForSource(monitoringStreamSource);

        if (changed && monitoringStreamSource == MonitoringStreamSource.Raw)
        {
            WarnIfRawMonitoring("pre-run switch");
        }
    }

    [ContextMenu("Reset Reliability Telemetry Counters")]
    public void ResetReliabilityTelemetryCounters()
    {
        seekCorrectionCountTotal = 0;
        seekCorrectionCountDrift = 0;
        seekCorrectionCountStartPrime = 0;
        seekCorrectionCountStreamSwitchPrime = 0;
        seekCooldownSuppressedCount = 0;
        captureRebindCount = 0;
        captureRebindFailureCount = 0;
        seekCorrectionCountWindow = 0;
        seekCorrectionCountDriftWindow = 0;
        seekCooldownSuppressedCountWindow = 0;
        captureRebindCountWindow = 0;
        Interlocked.Exchange(ref bufferUnderflowFillCount, 0);
        Interlocked.Exchange(ref bufferUnderflowFillCountWindow, 0);
        Interlocked.Exchange(ref bufferUnderflowFillSamples, 0);
        Interlocked.Exchange(ref bufferOverflowDropCount, 0);
        Interlocked.Exchange(ref bufferOverflowDropCountWindow, 0);
        Interlocked.Exchange(ref bufferOverflowDropSamples, 0);
        Interlocked.Exchange(ref callbackStarvationCount, 0);
        Interlocked.Exchange(ref callbackStarvationCountWindow, 0);
        lastWarningWindowResetTime = Time.unscaledTime;
        lastHealthSummaryLogTime = Time.unscaledTime;
        transitionStartCount = 0;
        transitionStopCount = 0;
        transitionAttenuationToggleCount = 0;
        transitionStreamSwitchCount = 0;
        hardVolumeStepCount = 0;
        lastVolumeStepDelta = 0f;
        lastVolumeStepContext = "";
        lastAppliedMonitoringVolume = monitoringSource != null ? Mathf.Clamp01(effectiveMonitoringGain) : -1f;
        lastRebindFailureWarningTime = Time.unscaledTime;
    }

    public MonitoringStreamSource GetMonitoringStreamSource()
    {
        return monitoringStreamSource;
    }

    public void ToggleMonitoringStreamSource()
    {
        SetMonitoringStreamSource(
            monitoringStreamSource == MonitoringStreamSource.Normalized
                ? MonitoringStreamSource.Raw
                : MonitoringStreamSource.Normalized
        );
    }

    /// <summary>
    /// Gets the current monitoring volume.
    /// </summary>
    public float GetMonitoringVolume()
    {
        return monitoringVolume;
    }

    /// <summary>
    /// Gets whether monitoring is currently enabled and playing.
    /// </summary>
    public bool IsMonitoring()
    {
        return monitoringEnabled && monitoringSource != null && monitoringSource.isPlaying;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (data == null || data.Length == 0)
        {
            return;
        }

        if (channels <= 0)
        {
            Array.Clear(data, 0, data.Length);
            return;
        }

        if (!monitoringEnabled || !isInitialized || imitoneVoiceInterpreter == null)
        {
            Array.Clear(data, 0, data.Length);
            return;
        }

        int frameCount = data.Length / channels;
        if (frameCount <= 0)
        {
            Array.Clear(data, 0, data.Length);
            return;
        }

        if (monitoringMonoReadBuffer == null || monitoringMonoReadBuffer.Length < frameCount)
        {
            monitoringMonoReadBuffer = new float[frameCount];
        }

        int copied = 0;
        int overflowDropped = 0;
        if (monitoringStreamSource == MonitoringStreamSource.Normalized)
        {
            copied = imitoneVoiceInterpreter.ReadNormalizedSamples(monitoringMonoReadBuffer, ref normalizedReadPosition, ref normalizedReadTotalSamples, out overflowDropped);
        }
        else
        {
            copied = imitoneVoiceInterpreter.ReadRawSamples(monitoringMonoReadBuffer, ref rawReadPosition, ref rawReadTotalSamples, out overflowDropped);
        }

        if (overflowDropped > 0)
        {
            Interlocked.Increment(ref bufferOverflowDropCount);
            Interlocked.Increment(ref bufferOverflowDropCountWindow);
            Interlocked.Add(ref bufferOverflowDropSamples, overflowDropped);
        }

        if (copied < frameCount)
        {
            int underflowFillSamples = frameCount - copied;
            Interlocked.Increment(ref bufferUnderflowFillCount);
            Interlocked.Increment(ref bufferUnderflowFillCountWindow);
            Interlocked.Add(ref bufferUnderflowFillSamples, underflowFillSamples);
            if (copied <= 0)
            {
                Interlocked.Increment(ref callbackStarvationCount);
                Interlocked.Increment(ref callbackStarvationCountWindow);
            }
        }

        int writeIndex = 0;
        float outputGain = Mathf.Clamp01(effectiveMonitoringGain);
        for (int frame = 0; frame < frameCount; frame++)
        {
            float sample = monitoringMonoReadBuffer[frame] * outputGain;
            for (int channel = 0; channel < channels; channel++)
            {
                data[writeIndex++] = sample;
            }
        }
    }

    private bool ConfigureMonitoringSourceClip()
    {
        if (monitoringSource == null || imitoneVoiceInterpreter == null)
        {
            return false;
        }
        sharedMicrophoneBuffer = imitoneVoiceInterpreter.MicrophoneBuffer;
        if (sharedMicrophoneBuffer == null)
        {
            float now = Time.unscaledTime;
            if (now - lastClipBindErrorTime >= Mathf.Max(0.5f, clipBindFailureErrorIntervalSeconds))
            {
                Debug.LogError("DirectVoiceMonitoring: Shared microphone buffer is null.");
                lastClipBindErrorTime = now;
            }
            return false;
        }

        monitoringSource.clip = sharedMicrophoneBuffer;
        lastSeenCaptureEpoch = imitoneVoiceInterpreter.MicCaptureEpoch;
        return true;
    }

    // Update volume when changed in inspector
    private void OnValidate()
    {
        if (monitoringSource != null && Application.isPlaying)
        {
            ApplyMonitoringVolume("on_validate");
        }
    }

    private void WarnIfRawMonitoring(string context)
    {
        if (monitoringStreamSource != MonitoringStreamSource.Raw)
        {
            return;
        }

        DbgWarn($"DirectVoiceMonitoring: Monitoring stream is set to RAW ({context}). Use Normalized for non-debug/shipping builds.");
    }

    private void ApplyMonitoringVolume(string context)
    {
        if (monitoringSource == null)
        {
            effectiveMonitoringGain = 0f;
            return;
        }

        float dynamicScale = 1f;
        float chantPresenceScale = 1f;
        float gameOnScale = 1f;
        float chargeDuckScale = 1f;
        if (dynamicVolumeEnabled && imitoneVoiceInterpreter != null && GameValues.instance != null)
        {
            UpdateDynamicVolumeLerps();
            chantPresenceScale = AudioLevelUtilities.BoardFader(
                GameValues.instance._chantLerpSlow,
                chantPresenceMinGainDb,
                chantPresenceMaxGainDb,
                chantPresenceLinearFloorRange);
            gameOnScale = gameOnLerp;
            chargeDuckScale = 1f - chargeLerp * 0.5f;
            dynamicScale = gameOnScale * chargeDuckScale * chantPresenceScale;
            //dynamicScale = GameValues.instance._chantLerpSlow;
        }

        float targetAttenuationScale = monitoringAttenuated ? Mathf.Clamp01(monitoringAttenuationMultiplier) : 1f;
        float attenuationScale = GetSmoothedAttenuationScale(targetAttenuationScale);
        float targetVolume = Mathf.Clamp01(monitoringVolume * Mathf.Clamp01(dynamicScale) * attenuationScale);
        float audioSourceVolumeScale = Mathf.Clamp01(monitoringSource.volume);
        float muteScale = monitoringSource.mute ? 0f : 1f;
        float appliedGain = targetVolume * audioSourceVolumeScale * muteScale;
        float volumeDelta = lastAppliedMonitoringVolume < 0f ? 0f : Mathf.Abs(appliedGain - lastAppliedMonitoringVolume);
        lastVolumeStepDelta = volumeDelta;
        if (lastAppliedMonitoringVolume >= 0f && volumeDelta > hardVolumeStepThreshold)
        {
            hardVolumeStepCount++;
            lastVolumeStepContext = context;
            if (enableTransitionAuditWarnings)
            {
                DbgWarn($"DirectVoiceMonitoring: Hard volume step detected (delta={volumeDelta:F3}, threshold={hardVolumeStepThreshold:F3}, context={context}).");
            }
        }
        effectiveMonitoringGain = appliedGain;
        lastAppliedMonitoringVolume = appliedGain;
    }

    private float GetSmoothedAttenuationScale(float targetScale)
    {
        if (!attenuationScaleInitialized)
        {
            smoothedAttenuationScale = targetScale;
            attenuationScaleInitialized = true;
            return smoothedAttenuationScale;
        }

        float tau = Mathf.Max(0.005f, attenuationSmoothingSeconds);
        float deltaTime = Mathf.Max(0f, Time.unscaledDeltaTime);
        if (deltaTime <= 0f)
        {
            smoothedAttenuationScale = targetScale;
            return smoothedAttenuationScale;
        }

        float alpha = 1f - Mathf.Exp(-deltaTime / tau);
        smoothedAttenuationScale = Mathf.Lerp(smoothedAttenuationScale, targetScale, alpha);
        return smoothedAttenuationScale;
    }

    private void UpdateReliabilityTelemetry()
    {
        float now = Time.unscaledTime;
        float windowDuration = Mathf.Max(1f, warningWindowSeconds);

        if (now - lastWarningWindowResetTime >= windowDuration)
        {
            int underflowWindow = Interlocked.Exchange(ref bufferUnderflowFillCountWindow, 0);
            int overflowWindow = Interlocked.Exchange(ref bufferOverflowDropCountWindow, 0);
            int starvationWindow = Interlocked.Exchange(ref callbackStarvationCountWindow, 0);

            if (enableReliabilityLogs && logWindowWarnings)
            {
                if (seekCorrectionCountDriftWindow > seekWarningThresholdPerWindow)
                {
                    DbgWarn($"DirectVoiceMonitoring: High drift-seek correction rate ({seekCorrectionCountDriftWindow}/{windowDuration:F0}s, threshold {seekWarningThresholdPerWindow}).");
                }
                if (captureRebindCountWindow > rebindWarningThresholdPerWindow)
                {
                    DbgWarn($"DirectVoiceMonitoring: High capture rebind rate ({captureRebindCountWindow}/{windowDuration:F0}s, threshold {rebindWarningThresholdPerWindow}).");
                }
                if (seekCooldownSuppressedCountWindow > cooldownSuppressionWarningThresholdPerWindow)
                {
                    DbgWarn($"DirectVoiceMonitoring: Frequent over-threshold drift suppressed by cooldown ({seekCooldownSuppressedCountWindow}/{windowDuration:F0}s, threshold {cooldownSuppressionWarningThresholdPerWindow}).");
                }
                if (underflowWindow > bufferUnderflowWarningThresholdPerWindow)
                {
                    DbgWarn($"DirectVoiceMonitoring: Buffered transport underflow fills detected ({underflowWindow}/{windowDuration:F0}s, threshold {bufferUnderflowWarningThresholdPerWindow}).");
                }
                if (overflowWindow > bufferOverflowWarningThresholdPerWindow)
                {
                    DbgWarn($"DirectVoiceMonitoring: Buffered transport overflow drops detected ({overflowWindow}/{windowDuration:F0}s, threshold {bufferOverflowWarningThresholdPerWindow}).");
                }
                if (starvationWindow > callbackStarvationWarningThresholdPerWindow)
                {
                    DbgWarn($"DirectVoiceMonitoring: Buffered transport callback starvation detected ({starvationWindow}/{windowDuration:F0}s, threshold {callbackStarvationWarningThresholdPerWindow}).");
                }
            }

            seekCorrectionCountWindow = 0;
            seekCorrectionCountDriftWindow = 0;
            captureRebindCountWindow = 0;
            seekCooldownSuppressedCountWindow = 0;
            lastWarningWindowResetTime = now;
        }

        if (enableReliabilityLogs && logHealthSummary && now - lastHealthSummaryLogTime >= Mathf.Max(1f, healthSummaryLogIntervalSeconds))
        {
            int underflowTotal = AtomicRead(ref bufferUnderflowFillCount);
            int underflowSamples = AtomicRead(ref bufferUnderflowFillSamples);
            int overflowTotal = AtomicRead(ref bufferOverflowDropCount);
            int overflowSamples = AtomicRead(ref bufferOverflowDropSamples);
            int starvationTotal = AtomicRead(ref callbackStarvationCount);
            DbgLog($"DirectVoiceMonitoring Health: seeks(total={seekCorrectionCountTotal}, drift={seekCorrectionCountDrift}, start={seekCorrectionCountStartPrime}, switch={seekCorrectionCountStreamSwitchPrime}) cooldownSuppressed={seekCooldownSuppressedCount} rebinds(success={captureRebindCount}, fail={captureRebindFailureCount}) buffered(underflow={underflowTotal}, underflowSamples={underflowSamples}, overflow={overflowTotal}, overflowSamples={overflowSamples}, starvation={starvationTotal}) transitions(start={transitionStartCount}, stop={transitionStopCount}, atten={transitionAttenuationToggleCount}, switch={transitionStreamSwitchCount}) hardSteps={hardVolumeStepCount} source={monitoringStreamSource} enabled={monitoringEnabled}");
            lastHealthSummaryLogTime = now;
        }
    }

    private static int AtomicRead(ref int value)
    {
        return Interlocked.CompareExchange(ref value, 0, 0);
    }

    /// <summary>
    /// Cumulative buffered-transport counters (thread-safe read). Use with Mic + Imitone ingest snapshots in the same frame.
    /// </summary>
    public void GetBufferedTransportTotals(
        out int underflowEvents,
        out int underflowSamples,
        out int overflowEvents,
        out int overflowSamples,
        out int starvationEvents)
    {
        underflowEvents = AtomicRead(ref bufferUnderflowFillCount);
        underflowSamples = AtomicRead(ref bufferUnderflowFillSamples);
        overflowEvents = AtomicRead(ref bufferOverflowDropCount);
        overflowSamples = AtomicRead(ref bufferOverflowDropSamples);
        starvationEvents = AtomicRead(ref callbackStarvationCount);
    }

    private void DbgLog(string message)
    {
        if (debugAllowMonitoringLogs)
        {
            Debug.Log(message);
        }
    }

    private void DbgWarn(string message)
    {
        if (debugAllowMonitoringWarnings)
        {
            Debug.LogWarning(message);
        }
    }

    private void UpdateDynamicVolumeLerps()
    {
        if (imitoneVoiceInterpreter.gameOn)
        {
            gameOnLerp += Time.deltaTime * Mathf.Max(0f, gameOnRiseSpeed);
        }
        else
        {
            gameOnLerp -= Time.deltaTime * Mathf.Max(0f, gameOnFallSpeed);
        }
        gameOnLerp = Mathf.Clamp01(gameOnLerp);

        if (imitoneVoiceInterpreter.toneActive)
        {
            float chantCharge = Mathf.Clamp01(GameValues.instance._chantCharge);
            if (chantCharge > chargeLerp)
            {
                chargeLerp += Time.deltaTime * Mathf.Max(0f, chargeRiseSpeed);
                chargeLerp = Mathf.Clamp(chargeLerp, 0f, chantCharge);
            }
            else if (chantCharge < chargeLerp)
            {
                chargeLerp -= Time.deltaTime * Mathf.Max(0f, chargeFallSpeed);
                chargeLerp = Mathf.Clamp(chargeLerp, chantCharge, 1f);
            }
            else
            {
                chargeLerp = chantCharge;
            }
        }
        else
        {
            chargeLerp -= Time.deltaTime * Mathf.Max(0f, chargeFallSpeed);
            chargeLerp = Mathf.Clamp01(chargeLerp);
        }
    }
}

