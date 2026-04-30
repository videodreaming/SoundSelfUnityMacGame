using System.Collections;
using UnityEngine;

/// <summary>
/// Provides real-time monitoring of microphone input using the shared microphone clip transport.
/// Raw mode is pass-through. Normalized mode applies gain/clamp at the output callback edge
/// to preserve low-latency behavior while keeping raw and normalized monitor options separate.
/// </summary>
public class DirectVoiceMonitoring : MonoBehaviour
{
    public enum MonitoringStreamSource
    {
        Normalized = 0,
        Raw = 1
    }

    [Header("Core References")]
    [Tooltip("Reference to ImitoneVoiceIntepreter. Used to auto-resolve MicPipeline if not assigned.")]
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    [Tooltip("Reference to MicPipeline that provides normalized monitoring stream.")]
    public MicPipeline micPipeline;
    
    [Header("Audio Output")]
    [Tooltip("AudioSource used for monitoring playback. If not assigned, will be created automatically.")]
    public AudioSource monitoringSource;
    
    [Header("Monitoring Controls")]
    [SerializeField] private bool monitoringEnabled = true;
    [SerializeField] [Range(0f, 1f)] private float monitoringVolume = 1f;
    [SerializeField] [Range(0f, 500f)] private float monitoringSafetyBufferMs = 100f;
    [Tooltip("Select which MicPipeline stream to monitor for A/B testing and debugging.")]
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

    [Header("Playback Sync Tuning")]
    [Tooltip("Minimum drift in milliseconds before forcing a playback seek to mic write-head offset.")]
    [SerializeField] [Range(5f, 250f)] private float syncSeekThresholdMs = 30f;
    [Tooltip("Minimum time between forced playback seeks. Higher values reduce click risk from frequent seeks.")]
    [SerializeField] [Range(0f, 1f)] private float syncSeekCooldownSeconds = 0.08f;
    
    private bool isInitialized = false;
    private AudioClip sharedMicrophoneBuffer;
    private string microphoneDeviceName;
    private int lastSeenCaptureEpoch = -1;
    [Header("Normalized Callback Smoothing")]
    [SerializeField] [Range(0.001f, 0.05f)] private float normalizationGainSmoothingSeconds = 0.01f;

    private float normalizedSmoothedGainLinear = 1f;
    private bool normalizedGainInitialized;
    private bool normalizedMonitorEnabledCached;
    private float normalizedTargetGainLinearCached = 1f;
    private bool normalizedHardClampEnabledCached = true;
    private float normalizedClampAbsCached = 0.98f;
    private float gameOnLerp = 0f;
    private float chargeLerp = 0f;
    private float lastSyncSeekTime = -10f;
    private float cachedOutputSampleRate = 48000f;
    private bool normalizationStateSubscribed;
    private float smoothedAttenuationScale = 1f;
    private bool attenuationScaleInitialized;

    /// <summary>
    /// Initializes the monitoring system and AudioSource.
    /// </summary>
    private void Awake()
    {
        cachedOutputSampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000f;
        float initialAttenuationScale = monitoringAttenuated ? Mathf.Clamp01(monitoringAttenuationMultiplier) : 1f;
        smoothedAttenuationScale = initialAttenuationScale;
        attenuationScaleInitialized = true;

        // Create monitoring AudioSource if not assigned
        if (monitoringSource == null)
        {
            monitoringSource = gameObject.AddComponent<AudioSource>();
            monitoringSource.playOnAwake = false;
            monitoringSource.loop = true;
            monitoringSource.volume = monitoringVolume;
        }
    }

    /// <summary>
    /// Sets up monitoring once MicPipeline has initialized its microphone.
    /// </summary>
    private void Start()
    {
        StartCoroutine(InitializeMonitoring());
    }

    /// <summary>
    /// Coroutine that waits for MicPipeline to initialize, then sets up monitoring.
    /// </summary>
    private IEnumerator InitializeMonitoring()
    {
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;

        // Wait for ImitoneVoiceIntepreter if needed for auto-resolve.
        while (micPipeline == null && imitoneVoiceInterpreter == null && elapsed < timeout)
        {
            Debug.LogWarning("DirectVoiceMonitoring: Waiting for MicPipeline or ImitoneVoiceIntepreter reference...");
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (micPipeline == null && imitoneVoiceInterpreter != null)
        {
            micPipeline = imitoneVoiceInterpreter.GetComponent<MicPipeline>();
        }

        if (micPipeline == null)
        {
            Debug.LogError("DirectVoiceMonitoring: MicPipeline reference is missing.");
            yield break;
        }

        elapsed = 0f;
        while (!micPipeline.IsReady && elapsed < timeout)
        {
            Debug.LogWarning("DirectVoiceMonitoring: Waiting for MicPipeline microphone initialization...");
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (!micPipeline.IsReady)
        {
            Debug.LogError("DirectVoiceMonitoring: Timeout waiting for MicPipeline microphone initialization.");
            yield break;
        }

        SetupMonitoring();
    }

    /// <summary>
    /// Sets up monitoring AudioSource with a streaming clip fed by MicPipeline samples.
    /// </summary>
    private void SetupMonitoring()
    {
        if (micPipeline == null || !micPipeline.IsReady)
        {
            Debug.LogError("DirectVoiceMonitoring: MicPipeline is not ready.");
            return;
        }

        ConfigureMonitoringSourceClip();
        monitoringSource.loop = true;
        monitoringSource.volume = monitoringVolume;
        monitoringSource.playOnAwake = false;
        BindNormalizationStateSubscription();
        RefreshNormalizedMonitorConfigCache();
        normalizedGainInitialized = false;

        // Mark as initialized BEFORE starting monitoring
        isInitialized = true;

        // Start playback if monitoring is enabled
        if (monitoringEnabled)
        {
            StartMonitoring();
        }

        WarnIfRawMonitoring("startup");
        Debug.Log($"DirectVoiceMonitoring: Monitoring initialized from MicPipeline ({monitoringStreamSource} stream).");
    }

    /// <summary>
    /// Keeps monitoring volume in sync while active.
    /// </summary>
    private void Update()
    {
        if (!isInitialized || monitoringSource == null)
            return;

        cachedOutputSampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000f;

        if (monitoringStreamSource == MonitoringStreamSource.Raw)
        {
            SyncLegacyMonitoringPlaybackPosition();
        }
        else if (micPipeline != null && micPipeline.IsReady)
        {
            int currentCaptureEpoch = micPipeline.CaptureEpoch;
            if (currentCaptureEpoch != lastSeenCaptureEpoch)
            {
                ConfigureMonitoringSourceClip();
                lastSeenCaptureEpoch = currentCaptureEpoch;
                Debug.Log("DirectVoiceMonitoring: MicPipeline capture restart detected. Re-bound shared monitoring clip.");
            }
        }

        ApplyMonitoringVolume();
        SyncLegacyMonitoringPlaybackPosition();
    }

    private void OnDestroy()
    {
        UnbindNormalizationStateSubscription();
    }

    private void SyncLegacyMonitoringPlaybackPosition()
    {
        if (!monitoringEnabled || sharedMicrophoneBuffer == null || string.IsNullOrEmpty(microphoneDeviceName))
        {
            return;
        }

        if (!monitoringSource.isPlaying)
        {
            return;
        }

        int micWritePos = Microphone.GetPosition(microphoneDeviceName);
        if (micWritePos < 0 || sharedMicrophoneBuffer.samples <= 0)
        {
            return;
        }

        int sourcePlayPos = monitoringSource.timeSamples;
        int samplesBehind = Mathf.RoundToInt(sharedMicrophoneBuffer.frequency * (monitoringSafetyBufferMs / 1000f));
        int targetReadPos = (micWritePos - samplesBehind + sharedMicrophoneBuffer.samples) % sharedMicrophoneBuffer.samples;

        int wrappedDifference = Mathf.Abs(targetReadPos - sourcePlayPos);
        int shortestDifference = Mathf.Min(
            wrappedDifference,
            sharedMicrophoneBuffer.samples - wrappedDifference
        );

        float thresholdSamples = Mathf.Max(1f, sharedMicrophoneBuffer.frequency * (syncSeekThresholdMs / 1000f));
        bool overThreshold = shortestDifference > thresholdSamples;
        bool cooldownElapsed = (Time.unscaledTime - lastSyncSeekTime) >= Mathf.Max(0f, syncSeekCooldownSeconds);

        if (overThreshold && cooldownElapsed)
        {
            monitoringSource.timeSamples = targetReadPos;
            lastSyncSeekTime = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Starts monitoring playback.
    /// </summary>
    public void StartMonitoring()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("DirectVoiceMonitoring: Cannot start monitoring - not initialized yet.");
            return;
        }

        if (monitoringSource == null || monitoringSource.clip == null)
        {
            Debug.LogError("DirectVoiceMonitoring: Cannot start monitoring - AudioSource or buffer is null.");
            return;
        }

        monitoringEnabled = true;
        ApplyMonitoringVolume();
        
        if (!monitoringSource.isPlaying)
        {
            if (sharedMicrophoneBuffer != null && !string.IsNullOrEmpty(microphoneDeviceName))
            {
                int micWritePos = Microphone.GetPosition(microphoneDeviceName);
                if (micWritePos >= 0 && sharedMicrophoneBuffer.samples > 0)
                {
                    int samplesBehind = Mathf.RoundToInt(sharedMicrophoneBuffer.frequency * (monitoringSafetyBufferMs / 1000f));
                    int startPos = (micWritePos - samplesBehind + sharedMicrophoneBuffer.samples) % sharedMicrophoneBuffer.samples;
                    monitoringSource.timeSamples = startPos;
                }
            }
            monitoringSource.Play();
            Debug.Log("DirectVoiceMonitoring: Monitoring started.");
        }
    }

    /// <summary>
    /// Stops monitoring playback.
    /// </summary>
    public void StopMonitoring()
    {
        monitoringEnabled = false;
        if (monitoringSource != null && monitoringSource.isPlaying)
        {
            monitoringSource.Stop();
            Debug.Log("DirectVoiceMonitoring: Monitoring stopped");
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
            ApplyMonitoringVolume();
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
        ApplyMonitoringVolume();
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
    /// Sets monitoring stream source and re-primes read position for low-glitch switching.
    /// </summary>
    public void SetMonitoringStreamSource(MonitoringStreamSource source)
    {
        bool changed = monitoringStreamSource != source;
        monitoringStreamSource = source;
        if (isInitialized)
        {
            bool wasPlaying = monitoringSource != null && monitoringSource.isPlaying;
            if (wasPlaying)
            {
                monitoringSource.Stop();
            }

            ConfigureMonitoringSourceClip();

            if (wasPlaying && monitoringEnabled)
            {
                StartMonitoring();
            }
        }
        else
        {
            ConfigureMonitoringSourceClip();
        }

        if (changed && monitoringStreamSource == MonitoringStreamSource.Raw)
        {
            WarnIfRawMonitoring("runtime switch");
        }
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

        if (!monitoringEnabled || !isInitialized || monitoringStreamSource != MonitoringStreamSource.Normalized)
        {
            normalizedGainInitialized = false;
            return;
        }

        bool normalizationEnabled = normalizedMonitorEnabledCached;
        float targetGain = normalizationEnabled ? Mathf.Max(0f, normalizedTargetGainLinearCached) : 1f;
        bool hardClampEnabled = normalizationEnabled && normalizedHardClampEnabledCached;
        float clampAbs = Mathf.Clamp(normalizedClampAbsCached, 0.01f, 1f);

        if (!normalizedGainInitialized)
        {
            normalizedSmoothedGainLinear = targetGain;
            normalizedGainInitialized = true;
        }

        float sampleRate = cachedOutputSampleRate > 0f ? cachedOutputSampleRate : 48000f;
        float tau = Mathf.Max(0.001f, normalizationGainSmoothingSeconds);
        float alpha = 1f - Mathf.Exp(-1f / (tau * sampleRate));

        for (int i = 0; i < data.Length; i++)
        {
            normalizedSmoothedGainLinear = Mathf.Lerp(normalizedSmoothedGainLinear, targetGain, alpha);
            float sample = data[i] * normalizedSmoothedGainLinear;
            if (hardClampEnabled)
            {
                sample = Mathf.Clamp(sample, -clampAbs, clampAbs);
            }
            data[i] = sample;
        }
    }

    private void ConfigureMonitoringSourceClip()
    {
        if (monitoringSource == null || micPipeline == null)
        {
            return;
        }
        sharedMicrophoneBuffer = micPipeline.MicrophoneBuffer;
        microphoneDeviceName = micPipeline.MicrophoneDeviceName;
        if (sharedMicrophoneBuffer == null)
        {
            Debug.LogError("DirectVoiceMonitoring: Shared microphone buffer is null.");
            return;
        }

        monitoringSource.clip = sharedMicrophoneBuffer;
        lastSeenCaptureEpoch = micPipeline.CaptureEpoch;
    }

    private void BindNormalizationStateSubscription()
    {
        UnbindNormalizationStateSubscription();
        if (micPipeline == null)
        {
            return;
        }

        micPipeline.NormalizationStateChanged += OnPipelineNormalizationStateChanged;
        normalizationStateSubscribed = true;
    }

    private void UnbindNormalizationStateSubscription()
    {
        if (!normalizationStateSubscribed || micPipeline == null)
        {
            return;
        }

        micPipeline.NormalizationStateChanged -= OnPipelineNormalizationStateChanged;
        normalizationStateSubscribed = false;
    }

    private void OnPipelineNormalizationStateChanged(MicPipeline.MicNormalizationState state)
    {
        normalizedMonitorEnabledCached = state.enabled;
        normalizedTargetGainLinearCached = Mathf.Pow(10f, state.gainDb / 20f);
        normalizedHardClampEnabledCached = state.hardClampEnabled;
        normalizedClampAbsCached = state.clampAbs;
    }

    private void RefreshNormalizedMonitorConfigCache()
    {
        if (micPipeline == null)
        {
            return;
        }

        OnPipelineNormalizationStateChanged(micPipeline.GetNormalizationState());
    }

    // Update volume when changed in inspector
    private void OnValidate()
    {
        if (monitoringSource != null && Application.isPlaying)
        {
            ApplyMonitoringVolume();
        }
    }

    private void WarnIfRawMonitoring(string context)
    {
        if (monitoringStreamSource != MonitoringStreamSource.Raw)
        {
            return;
        }

        Debug.LogWarning($"DirectVoiceMonitoring: Monitoring stream is set to RAW ({context}). Use Normalized for non-debug/shipping builds.");
    }

    private void ApplyMonitoringVolume()
    {
        if (monitoringSource == null)
        {
            return;
        }

        float dynamicScale = 1f;
        if (dynamicVolumeEnabled && imitoneVoiceInterpreter != null && GameValues.instance != null)
        {
            UpdateDynamicVolumeLerps();
            dynamicScale = gameOnLerp * (1f - chargeLerp * 0.5f) * Mathf.Clamp01(GameValues.instance._chantLerpFast);
        }

        float targetAttenuationScale = monitoringAttenuated ? Mathf.Clamp01(monitoringAttenuationMultiplier) : 1f;
        float attenuationScale = GetSmoothedAttenuationScale(targetAttenuationScale);
        float targetVolume = Mathf.Clamp01(monitoringVolume * Mathf.Clamp01(dynamicScale) * attenuationScale);
        monitoringSource.volume = targetVolume;
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

