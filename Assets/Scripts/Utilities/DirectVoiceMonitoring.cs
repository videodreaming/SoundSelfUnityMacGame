using System.Collections;
using System.Threading;
using UnityEngine;

/// <summary>
/// Provides real-time monitoring of normalized microphone input by streaming samples from
/// MicPipeline into a generated AudioClip. This allows independent monitoring latency/buffering
/// while raw microphone samples remain available for low-latency imitone analysis.
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
    [SerializeField] [Range(0f, 500f)] private float monitoringSafetyBufferMs = 150f;
    [Tooltip("Select which MicPipeline stream to monitor for A/B testing and debugging.")]
    [SerializeField] private MonitoringStreamSource monitoringStreamSource = MonitoringStreamSource.Normalized;

    [Header("Dynamic Monitoring Volume")]
    [Tooltip("When enabled, monitoring volume follows gameOn/toneActive/chant state (migrated from MusicSystem1).")]
    [SerializeField] private bool dynamicVolumeEnabled = true;
    [SerializeField] private float gameOnRiseSpeed = 2f;
    [SerializeField] private float gameOnFallSpeed = 0.5f;
    [SerializeField] private float chargeRiseSpeed = 1f;
    [SerializeField] private float chargeFallSpeed = 1f;
    
    private bool isInitialized = false;
    private AudioClip monitoringClip;
    private int monitoringReadPosition = -1;
    private int lastSeenCaptureEpoch = -1;
    private int monitoringClipChannels = 1;
    private const int MonitoringClipLengthSec = 2;
    private int underrunCount;
    private int lastLoggedUnderrunCount;
    private float underrunLogCooldownTimer = 0f;
    [SerializeField] private float underrunLogIntervalSeconds = 1f;
    private float gameOnLerp = 0f;
    private float chargeLerp = 0f;

    /// <summary>
    /// Initializes the monitoring system and AudioSource.
    /// </summary>
    private void Awake()
    {
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

        monitoringClipChannels = Mathf.Max(1, micPipeline.Channels);
        PrimeMonitoringReadPosition();
        underrunCount = 0;
        lastLoggedUnderrunCount = 0;
        int clipFrequency = Mathf.Max(8000, micPipeline.SampleRate);
        int clipSamples = clipFrequency * MonitoringClipLengthSec;

        monitoringClip = AudioClip.Create(
            name: "MicPipelineNormalizedMonitoring",
            lengthSamples: clipSamples,
            channels: monitoringClipChannels,
            frequency: clipFrequency,
            stream: true,
            pcmreadercallback: OnMonitoringAudioRead,
            pcmsetpositioncallback: OnMonitoringAudioSetPosition
        );

        if (monitoringClip == null)
        {
            Debug.LogError("DirectVoiceMonitoring: Failed to create monitoring stream clip.");
            return;
        }

        monitoringSource.clip = monitoringClip;
        monitoringSource.loop = true;
        monitoringSource.volume = monitoringVolume;
        monitoringSource.playOnAwake = false;

        // Mark as initialized BEFORE starting monitoring
        isInitialized = true;

        // Start playback if monitoring is enabled
        if (monitoringEnabled)
        {
            StartMonitoring();
        }

        WarnIfRawMonitoring("startup");
        Debug.Log($"DirectVoiceMonitoring: Monitoring initialized from MicPipeline ({monitoringStreamSource} stream). SampleRate: {clipFrequency}Hz");
    }

    /// <summary>
    /// Keeps monitoring volume in sync while active.
    /// </summary>
    private void Update()
    {
        if (!isInitialized || monitoringSource == null)
            return;

        if (micPipeline != null && micPipeline.IsReady)
        {
            int currentCaptureEpoch = micPipeline.CaptureEpoch;
            if (currentCaptureEpoch != lastSeenCaptureEpoch)
            {
                PrimeMonitoringReadPosition();
                lastSeenCaptureEpoch = currentCaptureEpoch;
                Debug.Log("DirectVoiceMonitoring: MicPipeline capture restart detected. Re-primed monitoring read position.");
            }
        }

        ApplyMonitoringVolume();

        underrunLogCooldownTimer += Time.deltaTime;

        int totalUnderruns = underrunCount;
        if (totalUnderruns > lastLoggedUnderrunCount && underrunLogCooldownTimer >= Mathf.Max(0.1f, underrunLogIntervalSeconds))
        {
            int newUnderruns = totalUnderruns - lastLoggedUnderrunCount;
            lastLoggedUnderrunCount = totalUnderruns;
            underrunLogCooldownTimer = 0f;
            Debug.LogError($"DirectVoiceMonitoring: {monitoringStreamSource} stream underrun detected ({newUnderruns} new, {totalUnderruns} total). Consider increasing monitoring safety buffer or investigating frame drops.");
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

        if (monitoringSource == null || monitoringClip == null)
        {
            Debug.LogError("DirectVoiceMonitoring: Cannot start monitoring - AudioSource or buffer is null.");
            return;
        }

        monitoringEnabled = true;
        ApplyMonitoringVolume();
        
        if (!monitoringSource.isPlaying)
        {
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
        PrimeMonitoringReadPosition();

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

    private void OnMonitoringAudioRead(float[] data)
    {
        if (data == null || data.Length == 0)
        {
            return;
        }

        if (!monitoringEnabled || !isInitialized || micPipeline == null || !micPipeline.IsReady)
        {
            ArrayClear(data);
            return;
        }

        int copied;
        if (monitoringStreamSource == MonitoringStreamSource.Raw)
        {
            copied = micPipeline.ReadRawSamples(data, ref monitoringReadPosition);
        }
        else
        {
            copied = micPipeline.ReadNormalizedSamples(data, ref monitoringReadPosition);
        }

        if (copied < data.Length)
        {
            Interlocked.Increment(ref underrunCount);
        }
    }

    private void OnMonitoringAudioSetPosition(int position)
    {
        // Re-prime reader with safety delay after seeks/loops.
        PrimeMonitoringReadPosition();
    }

    // Update volume when changed in inspector
    private void OnValidate()
    {
        if (monitoringSource != null && Application.isPlaying)
        {
            ApplyMonitoringVolume();
        }
    }

    private static void ArrayClear(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0f;
        }
    }

    private void PrimeMonitoringReadPosition()
    {
        if (micPipeline == null)
        {
            monitoringReadPosition = -1;
            return;
        }

        if (monitoringStreamSource == MonitoringStreamSource.Raw)
        {
            monitoringReadPosition = micPipeline.CreateRawReadPositionBehindMs(monitoringSafetyBufferMs);
        }
        else
        {
            monitoringReadPosition = micPipeline.CreateNormalizedReadPositionBehindMs(monitoringSafetyBufferMs);
        }
        lastSeenCaptureEpoch = micPipeline.CaptureEpoch;
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
            dynamicScale = gameOnLerp * (1f - chargeLerp * 0.5f) * GameValues.instance._chantLerpFast;
        }

        float targetVolume = Mathf.Clamp01(monitoringVolume * Mathf.Clamp01(dynamicScale));
        monitoringSource.volume = targetVolume;
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

