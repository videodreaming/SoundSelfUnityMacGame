using System.Collections;
using UnityEngine;

/// <summary>
/// Provides real-time monitoring of microphone input by playing back the shared microphone buffer
/// from ImitoneVoiceIntepreter. Allows the player to hear their own voice input through a
/// dedicated AudioSource while the microphone is concurrently used for analysis and recording.
/// </summary>
public class DirectVoiceMonitoring : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("Reference to ImitoneVoiceIntepreter that manages the shared microphone buffer")]
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    
    [Header("Audio Output")]
    [Tooltip("AudioSource used for monitoring playback. If not assigned, will be created automatically.")]
    public AudioSource monitoringSource;
    
    [Header("Monitoring Controls")]
    [SerializeField] private bool monitoringEnabled = true;
    [SerializeField] [Range(0f, 1f)] private float monitoringVolume = 1f;
    
    private AudioClip sharedMicrophoneBuffer;
    private string microphoneDeviceName;
    private bool isInitialized = false;

    /// <summary>
    /// Initializes the monitoring system by getting references to the shared microphone buffer.
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
    /// Sets up monitoring once ImitoneVoiceIntepreter has initialized its microphone.
    /// </summary>
    private void Start()
    {
        StartCoroutine(InitializeMonitoring());
    }

    /// <summary>
    /// Coroutine that waits for ImitoneVoiceIntepreter to initialize, then sets up monitoring.
    /// </summary>
    private IEnumerator InitializeMonitoring()
    {
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;

        // Wait for ImitoneVoiceIntepreter to be assigned and initialize
        while (imitoneVoiceInterpreter == null && elapsed < timeout)
        {
            Debug.LogWarning("DirectVoiceMonitoring: Waiting for ImitoneVoiceIntepreter reference...");
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (imitoneVoiceInterpreter == null)
        {
            Debug.LogError("DirectVoiceMonitoring: Timeout waiting for ImitoneVoiceIntepreter reference!");
            yield break;
        }

        elapsed = 0f;
        // Wait for microphone buffer to be available
        while (imitoneVoiceInterpreter.MicrophoneBuffer == null && elapsed < timeout)
        {
            Debug.LogWarning("DirectVoiceMonitoring: Waiting for microphone buffer to initialize...");
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (imitoneVoiceInterpreter.MicrophoneBuffer == null)
        {
            Debug.LogError("DirectVoiceMonitoring: Timeout waiting for microphone buffer to initialize!");
            yield break;
        }

        // Wait for microphone to start recording (position > 0)
        microphoneDeviceName = imitoneVoiceInterpreter.MicrophoneDeviceName;
        elapsed = 0f;
        while (Microphone.GetPosition(microphoneDeviceName) <= 0 && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (Microphone.GetPosition(microphoneDeviceName) <= 0)
        {
            Debug.LogError("DirectVoiceMonitoring: Timeout waiting for microphone to start recording!");
            yield break;
        }

        SetupMonitoring();
    }

    /// <summary>
    /// Sets up the monitoring AudioSource with the shared microphone buffer.
    /// </summary>
    private void SetupMonitoring()
    {
        sharedMicrophoneBuffer = imitoneVoiceInterpreter.MicrophoneBuffer;
        microphoneDeviceName = imitoneVoiceInterpreter.MicrophoneDeviceName;

        if (sharedMicrophoneBuffer == null)
        {
            Debug.LogError("DirectVoiceMonitoring: Shared microphone buffer is null!");
            return;
        }

        // Configure monitoring AudioSource
        monitoringSource.clip = sharedMicrophoneBuffer;
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

        Debug.Log($"DirectVoiceMonitoring: Monitoring initialized. Device: {microphoneDeviceName}, Buffer samples: {sharedMicrophoneBuffer.samples}, Frequency: {sharedMicrophoneBuffer.frequency}Hz");
    }

    /// <summary>
    /// Updates monitoring playback position to stay synchronized with microphone write position for minimal latency.
    /// </summary>
    private void Update()
    {
        if (!isInitialized || !monitoringEnabled || sharedMicrophoneBuffer == null)
            return;

        // Keep monitoring source synchronized with microphone write position
        // This minimizes latency by reading from the most recent audio data
        if (monitoringSource.isPlaying)
        {
            int micWritePos = Microphone.GetPosition(microphoneDeviceName);
            int sourcePlayPos = monitoringSource.timeSamples;

            // Calculate the read position (slightly behind write position for stability)
            // Reading from ~100ms behind write position prevents reading from the write head
            int samplesBehind = Mathf.RoundToInt(sharedMicrophoneBuffer.frequency * 0.1f); // 100ms delay
            int targetReadPos = (micWritePos - samplesBehind + sharedMicrophoneBuffer.samples) % sharedMicrophoneBuffer.samples;

            // Only update if there's a significant difference to avoid constant seeking
            int difference = Mathf.Abs(targetReadPos - sourcePlayPos);
            if (difference > sharedMicrophoneBuffer.samples / 10) // Update if more than 10% off
            {
                monitoringSource.timeSamples = targetReadPos;
            }
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

        if (monitoringSource == null || sharedMicrophoneBuffer == null)
        {
            Debug.LogError("DirectVoiceMonitoring: Cannot start monitoring - AudioSource or buffer is null.");
            return;
        }

        monitoringEnabled = true;
        monitoringSource.volume = monitoringVolume;
        
        if (!monitoringSource.isPlaying)
        {
            // Start playback from current microphone read position
            int micWritePos = Microphone.GetPosition(microphoneDeviceName);
            int samplesBehind = Mathf.RoundToInt(sharedMicrophoneBuffer.frequency * 0.1f); // 100ms delay
            int startPos = (micWritePos - samplesBehind + sharedMicrophoneBuffer.samples) % sharedMicrophoneBuffer.samples;
            
            monitoringSource.timeSamples = startPos;
            monitoringSource.Play();
            
            Debug.Log($"DirectVoiceMonitoring: Monitoring started at position {startPos}");
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

    // Update volume when changed in inspector
    private void OnValidate()
    {
        if (monitoringSource != null && Application.isPlaying)
        {
            monitoringSource.volume = monitoringVolume;
        }
    }
}

