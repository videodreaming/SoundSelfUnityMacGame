using System;
using UnityEngine;

public class MicPipeline : MonoBehaviour
{
    [Header("Microphone Source")]
    [SerializeField] private string preferredDeviceName = "";
    [SerializeField] private int sampleRate = 48000;
    [SerializeField] private int loopLengthSeconds = 6;
    [Tooltip("Chunk size used for microphone GetData reads to reduce allocation churn.")]
    [SerializeField] private int micReadChunkSize = 2048;

    [Header("Normalization")]
    [Tooltip("Master toggle for normalized output stream. Raw output is always unaffected.")]
    [SerializeField] private bool normalizationEnabled = false;
    [Tooltip("Gain in dB applied to normalized output stream.")]
    [SerializeField] private float normalizationGainDb = 0f;
    [Tooltip("Clamp normalized samples to +/- clamp value after gain.")]
    [SerializeField] private bool normalizationHardClampEnabled = true;
    [Tooltip("Absolute clamp value used when hard clamp is enabled.")]
    [SerializeField] [Range(0.01f, 1f)] private float normalizationClampAbs = 0.98f;

    [Header("Telemetry (Inspector)")]
    [Tooltip("Current-frame absolute peak after normalization (0..1).")]
    [SerializeField] [Range(0f, 1f)] private float normalizedPeakCurrentFrame = 0f;
    [Tooltip("Smoothed peak meter for easier visual monitoring in Inspector.")]
    [SerializeField] [Range(0f, 1f)] private float normalizedPeakMeter = 0f;
    [SerializeField] [Range(0f, 10f)] private float normalizedPeakMeterDecayPerSecond = 2.5f;
    [Tooltip("Current ring-buffer size in samples.")]
    [SerializeField] private int normalizedRingBufferCapacitySamples = 0;

    private string microphoneDeviceName;
    private AudioClip microphoneBuffer;
    private int micPosRead;
    private float[] latestRawFrame = Array.Empty<float>();
    private int latestRawSampleCount;
    private float[] latestNormalizedFrame = Array.Empty<float>();
    private int latestNormalizedSampleCount;
    private float[] normalizedRingBuffer = Array.Empty<float>();
    private int normalizedWritePosition;
    private readonly object normalizedBufferLock = new object();
    private int lastFrameUpdated = -1;
    private float[] micReadChunkBuffer = Array.Empty<float>();
    private float[] micReadTailBuffer = Array.Empty<float>();

    public bool IsReady => microphoneBuffer != null && !string.IsNullOrEmpty(microphoneDeviceName);
    public string MicrophoneDeviceName => microphoneDeviceName;
    public AudioClip MicrophoneBuffer => microphoneBuffer;
    public int SampleRate => sampleRate;
    public int Channels => 1;
    public event Action<MicNormalizationState> NormalizationStateChanged;

    [Serializable]
    public struct MicNormalizationState
    {
        public bool enabled;
        public float gainDb;
        public bool hardClampEnabled;
        public float clampAbs;
    }

    private void Awake()
    {
        InitializeMicrophone();
    }

    private void Update()
    {
        // Main-thread producer for raw/normalized frames and ring-buffer writes.
        EnsureFrameUpdated();

        // Decay visual telemetry smoothly when no new peak is present.
        normalizedPeakMeter = Mathf.Max(0f, normalizedPeakMeter - normalizedPeakMeterDecayPerSecond * Time.deltaTime);
    }

    public void InitializeMicrophone()
    {
        if (IsReady)
        {
            return;
        }

        var devices = Microphone.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("MicPipeline: No microphone devices available.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
        {
            foreach (var device in devices)
            {
                if (string.Equals(device, preferredDeviceName, StringComparison.Ordinal))
                {
                    microphoneDeviceName = device;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(microphoneDeviceName))
        {
            microphoneDeviceName = devices[0];
        }

        microphoneBuffer = Microphone.Start(microphoneDeviceName, true, loopLengthSeconds, sampleRate);
        micPosRead = 0;

        if (microphoneBuffer == null)
        {
            Debug.LogError("MicPipeline: Failed to start microphone capture.");
            return;
        }

        int ringSize = Mathf.Max(sampleRate * Mathf.Max(1, loopLengthSeconds), 1024);
        normalizedRingBuffer = new float[ringSize];
        normalizedWritePosition = 0;
        normalizedRingBufferCapacitySamples = ringSize;

        int chunkSize = Mathf.Max(256, micReadChunkSize);
        micReadChunkBuffer = new float[chunkSize];
        micReadTailBuffer = new float[chunkSize];
    }

    public bool TryCopyLatestRawFrame(ref float[] destination, out int sampleCount)
    {
        EnsureFrameUpdated();
        sampleCount = latestRawSampleCount;
        if (sampleCount <= 0)
        {
            return false;
        }

        if (destination == null || destination.Length < sampleCount)
        {
            destination = new float[sampleCount];
        }

        Array.Copy(latestRawFrame, destination, sampleCount);
        return true;
    }

    public bool TryCopyLatestNormalizedFrame(ref float[] destination, out int sampleCount)
    {
        EnsureFrameUpdated();
        sampleCount = latestNormalizedSampleCount;
        if (sampleCount <= 0)
        {
            return false;
        }

        if (destination == null || destination.Length < sampleCount)
        {
            destination = new float[sampleCount];
        }

        Array.Copy(latestNormalizedFrame, destination, sampleCount);
        return true;
    }

    public MicNormalizationState GetNormalizationState()
    {
        return new MicNormalizationState
        {
            enabled = normalizationEnabled,
            gainDb = normalizationGainDb,
            hardClampEnabled = normalizationHardClampEnabled,
            clampAbs = normalizationClampAbs
        };
    }

    public void SetNormalizationState(MicNormalizationState state)
    {
        normalizationEnabled = state.enabled;
        normalizationGainDb = state.gainDb;
        normalizationHardClampEnabled = state.hardClampEnabled;
        normalizationClampAbs = Mathf.Clamp(state.clampAbs, 0.01f, 1f);
        OnNormalizationConfigChanged();
    }

    public void SetNormalizationEnabled(bool enabled)
    {
        normalizationEnabled = enabled;
        OnNormalizationConfigChanged();
    }

    public void SetNormalizationGainDb(float gainDb)
    {
        normalizationGainDb = gainDb;
        OnNormalizationConfigChanged();
    }

    public void SetNormalizationHardClampEnabled(bool enabled)
    {
        normalizationHardClampEnabled = enabled;
        OnNormalizationConfigChanged();
    }

    public void SetNormalizationClampAbs(float clampAbs)
    {
        normalizationClampAbs = Mathf.Clamp(clampAbs, 0.01f, 1f);
        OnNormalizationConfigChanged();
    }

    public float GetNormalizationGainLinear()
    {
        return Mathf.Pow(10f, normalizationGainDb / 20f);
    }

    public int ReadNormalizedSamples(float[] destination, ref int readPosition)
    {
        if (destination == null || destination.Length == 0)
        {
            return 0;
        }

        lock (normalizedBufferLock)
        {
            if (normalizedRingBuffer == null || normalizedRingBuffer.Length == 0)
            {
                Array.Clear(destination, 0, destination.Length);
                return 0;
            }

            if (readPosition < 0 || readPosition >= normalizedRingBuffer.Length)
            {
                readPosition = normalizedWritePosition;
            }

            int available = (normalizedRingBuffer.Length + normalizedWritePosition - readPosition) % normalizedRingBuffer.Length;
            int toCopy = Mathf.Min(destination.Length, available);

            for (int i = 0; i < toCopy; i++)
            {
                destination[i] = normalizedRingBuffer[(readPosition + i) % normalizedRingBuffer.Length];
            }

            if (toCopy < destination.Length)
            {
                Array.Clear(destination, toCopy, destination.Length - toCopy);
            }

            readPosition = (readPosition + toCopy) % normalizedRingBuffer.Length;
            return toCopy;
        }
    }

    public int CreateNormalizedReadPositionBehindMs(float delayMs)
    {
        lock (normalizedBufferLock)
        {
            if (normalizedRingBuffer == null || normalizedRingBuffer.Length == 0)
            {
                return -1;
            }

            float clampedMs = Mathf.Max(0f, delayMs);
            int samplesBehind = Mathf.RoundToInt((clampedMs / 1000f) * sampleRate);
            if (samplesBehind >= normalizedRingBuffer.Length)
            {
                samplesBehind = normalizedRingBuffer.Length - 1;
            }

            int readPos = (normalizedWritePosition - samplesBehind + normalizedRingBuffer.Length) % normalizedRingBuffer.Length;
            return readPos;
        }
    }

    private void EnsureFrameUpdated()
    {
        if (lastFrameUpdated == Time.frameCount)
        {
            return;
        }

        UpdateMicReadFrame();
        lastFrameUpdated = Time.frameCount;
    }

    private void UpdateMicReadFrame()
    {
        if (!IsReady)
        {
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            return;
        }

        int micPosWrite = Microphone.GetPosition(microphoneDeviceName);
        if (micPosWrite < 0 || microphoneBuffer.samples <= 0)
        {
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            return;
        }

        int sampleCount = (microphoneBuffer.samples + micPosWrite - micPosRead) % microphoneBuffer.samples;
        if (sampleCount <= 0)
        {
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            return;
        }

        if (latestRawFrame.Length < sampleCount)
        {
            latestRawFrame = new float[sampleCount];
        }

        ReadMicSamplesIntoLatestRaw(sampleCount);
        latestRawSampleCount = sampleCount;

        if (latestNormalizedFrame.Length < sampleCount)
        {
            latestNormalizedFrame = new float[sampleCount];
        }

        if (!normalizationEnabled)
        {
            Array.Copy(latestRawFrame, latestNormalizedFrame, sampleCount);
        }
        else
        {
            float gainLinear = GetNormalizationGainLinear();
            for (int i = 0; i < sampleCount; i++)
            {
                float normalizedSample = latestRawFrame[i] * gainLinear;
                if (normalizationHardClampEnabled)
                {
                    normalizedSample = Mathf.Clamp(normalizedSample, -normalizationClampAbs, normalizationClampAbs);
                }
                latestNormalizedFrame[i] = normalizedSample;
            }
        }

        float framePeak = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float abs = Mathf.Abs(latestNormalizedFrame[i]);
            if (abs > framePeak)
            {
                framePeak = abs;
            }
        }
        normalizedPeakCurrentFrame = framePeak;
        if (framePeak > normalizedPeakMeter)
        {
            normalizedPeakMeter = framePeak;
        }

        WriteNormalizedFrameToRingBuffer(sampleCount);
        latestNormalizedSampleCount = sampleCount;
    }

    private void OnNormalizationConfigChanged()
    {
        // Force recompute on next read so consumers get fresh settings immediately.
        lastFrameUpdated = -1;
        NormalizationStateChanged?.Invoke(GetNormalizationState());
    }

    private void WriteNormalizedFrameToRingBuffer(int sampleCount)
    {
        if (sampleCount <= 0 || normalizedRingBuffer == null || normalizedRingBuffer.Length == 0)
        {
            return;
        }

        lock (normalizedBufferLock)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                normalizedRingBuffer[normalizedWritePosition] = latestNormalizedFrame[i];
                normalizedWritePosition = (normalizedWritePosition + 1) % normalizedRingBuffer.Length;
            }
        }
    }

    private void ReadMicSamplesIntoLatestRaw(int sampleCount)
    {
        int remaining = sampleCount;
        int writeOffset = 0;
        int readPos = micPosRead;

        while (remaining > 0)
        {
            int chunkSize = Mathf.Min(remaining, micReadChunkBuffer.Length);
            float[] sourceBuffer;

            if (chunkSize == micReadChunkBuffer.Length)
            {
                sourceBuffer = micReadChunkBuffer;
            }
            else
            {
                if (micReadTailBuffer.Length < chunkSize)
                {
                    micReadTailBuffer = new float[chunkSize];
                }
                sourceBuffer = micReadTailBuffer;
            }

            microphoneBuffer.GetData(sourceBuffer, readPos);
            Array.Copy(sourceBuffer, 0, latestRawFrame, writeOffset, chunkSize);

            readPos = (readPos + chunkSize) % microphoneBuffer.samples;
            writeOffset += chunkSize;
            remaining -= chunkSize;
        }

        micPosRead = readPos;
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(microphoneDeviceName))
        {
            Microphone.End(microphoneDeviceName);
        }
        microphoneBuffer = null;
        micPosRead = 0;
    }
}
