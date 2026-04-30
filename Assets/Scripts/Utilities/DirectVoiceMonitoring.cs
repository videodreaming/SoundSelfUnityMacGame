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
    
    private bool isInitialized = false;
    private AudioClip monitoringClip;
    private int monitoringReadPosition = -1;
    private int monitoringClipChannels = 1;
    private const int MonitoringClipLengthSec = 2;
    private int underrunCount;
    private int lastLoggedUnderrunCount;
    private float underrunLogCooldownTimer = 0f;
    [SerializeField] private float underrunLogIntervalSeconds = 1f;

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
    /// Sets up monitoring AudioSource with a streaming clip fed by MicPipeline normalized samples.
    /// </summary>
    private void SetupMonitoring()
    {
        if (micPipeline == null || !micPipeline.IsReady)
        {
            Debug.LogError("DirectVoiceMonitoring: MicPipeline is not ready.");
            return;
        }

        monitoringClipChannels = 1;
        monitoringReadPosition = micPipeline.CreateNormalizedReadPositionBehindMs(monitoringSafetyBufferMs);
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

        Debug.Log($"DirectVoiceMonitoring: Monitoring initialized from MicPipeline. SampleRate: {clipFrequency}Hz");
    }

    /// <summary>
    /// Keeps monitoring volume in sync while active.
    /// </summary>
    private void Update()
    {
        if (!isInitialized || monitoringSource == null)
            return;
        monitoringSource.volume = monitoringVolume;

        underrunLogCooldownTimer += Time.deltaTime;

        int totalUnderruns = underrunCount;
        if (totalUnderruns > lastLoggedUnderrunCount && underrunLogCooldownTimer >= Mathf.Max(0.1f, underrunLogIntervalSeconds))
        {
            int newUnderruns = totalUnderruns - lastLoggedUnderrunCount;
            lastLoggedUnderrunCount = totalUnderruns;
            underrunLogCooldownTimer = 0f;
            Debug.LogError($"DirectVoiceMonitoring: Normalized stream underrun detected ({newUnderruns} new, {totalUnderruns} total). Consider increasing monitoring safety buffer or investigating frame drops.");
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
        monitoringSource.volume = monitoringVolume;
        
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
            monitoringSource.volume = monitoringVolume;
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

        int copied = micPipeline.ReadNormalizedSamples(data, ref monitoringReadPosition);
        if (copied < data.Length)
        {
            Interlocked.Increment(ref underrunCount);
        }
    }

    private void OnMonitoringAudioSetPosition(int position)
    {
        // Re-prime reader with safety delay after seeks/loops.
        if (micPipeline != null)
        {
            monitoringReadPosition = micPipeline.CreateNormalizedReadPositionBehindMs(monitoringSafetyBufferMs);
        }
        else
        {
            monitoringReadPosition = -1;
        }
    }

    // Update volume when changed in inspector
    private void OnValidate()
    {
        if (monitoringSource != null && Application.isPlaying)
        {
            monitoringSource.volume = monitoringVolume;
        }
    }

    private static void ArrayClear(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0f;
        }
    }
}

