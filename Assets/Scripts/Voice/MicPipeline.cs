using System;
using UnityEngine;

public class MicPipeline : MonoBehaviour
{
    private enum MicChannelMode
    {
        Mono = 1
    }

    [Header("Microphone Source")]
    [SerializeField] private string preferredDeviceName = "";
    [SerializeField] private int sampleRate = 48000;
    [SerializeField] private int loopLengthSeconds = 6;
    [Tooltip("Chunk size used for microphone GetData reads to reduce allocation churn.")]
    [SerializeField] private int micReadChunkSize = 2048;
    [Tooltip("Retry interval used when microphone device is unavailable or capture fails.")]
    [SerializeField] private float recoveryRetryIntervalSeconds = 1f;
    [Tooltip("How many consecutive frames with no write-head movement trigger mic recovery.")]
    [SerializeField] private int stalledWriteHeadFrameThreshold = 120;

    [Header("Channel Contract")]
    [Tooltip("Pipeline output contract for consumers. This pipeline publishes mono samples.")]
    [SerializeField] private MicChannelMode channelMode = MicChannelMode.Mono;
    [SerializeField] private ImitoneVoiceIntepreter imitoneVoiceIntepreter;

    [Header("Normalization")]
    [Tooltip("Master toggle for normalized output stream. Raw output is always unaffected.")]
    [SerializeField] private bool normalizationEnabled = false;
    [Tooltip("Gain in dB applied to normalized output stream.")]
    [SerializeField] private float normalizationGainDb = 0f;
    [Tooltip("Clamp normalized samples to +/- clamp value after gain.")]
    [SerializeField] private bool normalizationHardClampEnabled = true;
    [Tooltip("Absolute clamp value used when hard clamp is enabled.")]
    [SerializeField] [Range(0.01f, 1f)] private float normalizationClampAbs = 0.98f;

    [Header("Normalization Gain Riding")]
    [Tooltip("Automatically rides normalization gain while imitone toneActive is true.")]
    [SerializeField] private bool gainRidingEnabled = false;
    [SerializeField] private float gainRidingTargetDb = -8f;
    [Tooltip("If mic dB is below target by more than this, raise gain.")]
    [SerializeField] [Range(0f, 24f)] private float gainRidingRaiseThresholdDb = 4f;
    [Tooltip("If mic dB is above target by more than this, lower gain at normal rate.")]
    [SerializeField] [Range(0f, 24f)] private float gainRidingLowerThresholdDb = 2f;
    [Tooltip("If mic dB is above target by more than this, lower gain at rapid rate.")]
    [SerializeField] [Range(0f, 24f)] private float gainRidingRapidLowerThresholdDb = 6f;
    [SerializeField] [Range(0f, 24f)] private float gainRidingRaiseRateDbPerSecond = 0.5f;
    [SerializeField] [Range(0f, 48f)] private float gainRidingLowerRateDbPerSecond = 4f;
    [SerializeField] [Range(0f, 96f)] private float gainRidingRapidLowerRateDbPerSecond = 16f;
    [Tooltip("Clamp for ridden normalization gain.")]
    [SerializeField] private Vector2 gainRidingGainDbClamp = new Vector2(-24f, 24f);

    [Header("Normalization Runtime Telemetry (Inspector)")]
    [Tooltip("Current effective normalization gain in dB that consumers use.")]
    [SerializeField] private float normalizationGainDbRuntime = 0f;
    [Tooltip("Current effective normalization gain in linear scale.")]
    [SerializeField] private float normalizationGainLinearRuntime = 1f;
    [Tooltip("Last mic loudness dB sampled by gain riding.")]
    [SerializeField] private float gainRidingLastMicDb = float.NaN;
    [Tooltip("Estimated normalized loudness dB used by gain-riding control.")]
    [SerializeField] private float gainRidingLastEstimatedNormalizedDb = float.NaN;
    [Tooltip("Last loudness delta (mic dB - target dB).")]
    [SerializeField] private float gainRidingLastDbDelta = 0f;
    [Tooltip("Current riding rate in dB/sec (positive raises gain, negative lowers gain).")]
    [SerializeField] private float gainRidingCurrentRateDbPerSecond = 0f;
    [Tooltip("Most recent gain change step applied this frame in dB.")]
    [SerializeField] private float gainRidingLastAppliedStepDb = 0f;
    [Tooltip("Accumulated gain-riding adjustment since play entered.")]
    [SerializeField] private float gainRidingAccumulatedAdjustmentDb = 0f;
    [Tooltip("True when gain riding feature toggle is enabled.")]
    [SerializeField] private bool gainRidingGateEnabled = false;
    [Tooltip("True when mic pipeline is initialized and ready.")]
    [SerializeField] private bool gainRidingGatePipelineReady = false;
    [Tooltip("True when ImitoneVoiceIntepreter reference is valid.")]
    [SerializeField] private bool gainRidingGateInterpreterBound = false;
    [Tooltip("True when toneActive is true.")]
    [SerializeField] private bool gainRidingGateToneActive = false;
    [Tooltip("True when sampled mic dB value is finite and usable.")]
    [SerializeField] private bool gainRidingGateMicDbValid = false;

    [Header("Telemetry (Inspector)")]
    [Tooltip("Current-frame absolute peak after normalization (0..1).")]
    [SerializeField] [Range(0f, 1f)] private float normalizedPeakCurrentFrame = 0f;
    [Tooltip("Smoothed peak meter for easier visual monitoring in Inspector.")]
    [SerializeField] [Range(0f, 1f)] private float normalizedPeakMeter = 0f;
    [SerializeField] [Range(0f, 10f)] private float normalizedPeakMeterDecayPerSecond = 2.5f;
    [Tooltip("Current ring-buffer size in samples.")]
    [SerializeField] private int normalizedRingBufferCapacitySamples = 0;
    [Tooltip("Current raw ring-buffer size in samples.")]
    [SerializeField] private int rawRingBufferCapacitySamples = 0;

    private string microphoneDeviceName;
    private AudioClip microphoneBuffer;
    private int micPosRead;
    private int captureEpoch;
    private int micInputChannels = 1;
    private bool micInputWasDownmixedToMono;
    private int lastMicWritePosition = -1;
    private int stalledWriteHeadFrameCount;
    private float nextRecoveryAttemptTime;
    private bool recoveryWarningLogged;
    private float[] latestRawFrame = Array.Empty<float>();
    private int latestRawSampleCount;
    private float[] rawRingBuffer = Array.Empty<float>();
    private int rawWritePosition;
    private readonly object rawBufferLock = new object();
    private float[] latestNormalizedFrame = Array.Empty<float>();
    private int latestNormalizedSampleCount;
    private float[] normalizedRingBuffer = Array.Empty<float>();
    private int normalizedWritePosition;
    private readonly object normalizedBufferLock = new object();
    private int lastFrameUpdated = -1;
    private float[] micReadChunkBuffer = Array.Empty<float>(); // interleaved input buffer
    private float[] micReadTailBuffer = Array.Empty<float>();  // interleaved input tail buffer

    public bool IsReady => microphoneBuffer != null && !string.IsNullOrEmpty(microphoneDeviceName);
    public string MicrophoneDeviceName => microphoneDeviceName;
    public AudioClip MicrophoneBuffer => microphoneBuffer;
    public int SampleRate => sampleRate;
    public int Channels => (int)channelMode;
    public int CaptureEpoch => captureEpoch;
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
        if (imitoneVoiceIntepreter == null)
        {
            imitoneVoiceIntepreter = GetComponent<ImitoneVoiceIntepreter>();
        }
        InitializeMicrophone();
    }

    private void OnEnable()
    {
        if (!IsReady)
        {
            InitializeMicrophone();
        }
    }

    private void Update()
    {
        if (!IsReady && Time.unscaledTime >= nextRecoveryAttemptTime)
        {
            InitializeMicrophone();
        }

        // Main-thread producer for raw/normalized frames and ring-buffer writes.
        EnsureFrameUpdated();
        UpdateNormalizationGainRiding();
        UpdateNormalizationTelemetry();

        // Decay visual telemetry smoothly when no new peak is present.
        normalizedPeakMeter = Mathf.Max(0f, normalizedPeakMeter - normalizedPeakMeterDecayPerSecond * Time.deltaTime);
    }

    public void InitializeMicrophone()
    {
        if (IsReady)
        {
            return;
        }

        if (!TryResolveMicrophoneDevice(out string resolvedDeviceName))
        {
            ScheduleRecoveryAttempt("No microphone devices available.");
            return;
        }

        if (!StartMicrophoneCapture(resolvedDeviceName))
        {
            ScheduleRecoveryAttempt($"Failed to start microphone capture on '{resolvedDeviceName}'.");
            return;
        }

        recoveryWarningLogged = false;
        nextRecoveryAttemptTime = 0f;
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

    public int ReadRawSamples(float[] destination, ref int readPosition)
    {
        if (destination == null || destination.Length == 0)
        {
            return 0;
        }

        lock (rawBufferLock)
        {
            if (rawRingBuffer == null || rawRingBuffer.Length == 0)
            {
                Array.Clear(destination, 0, destination.Length);
                return 0;
            }

            if (readPosition < 0 || readPosition >= rawRingBuffer.Length)
            {
                readPosition = rawWritePosition;
            }

            int available = (rawRingBuffer.Length + rawWritePosition - readPosition) % rawRingBuffer.Length;
            int toCopy = Mathf.Min(destination.Length, available);

            for (int i = 0; i < toCopy; i++)
            {
                destination[i] = rawRingBuffer[(readPosition + i) % rawRingBuffer.Length];
            }

            if (toCopy < destination.Length)
            {
                Array.Clear(destination, toCopy, destination.Length - toCopy);
            }

            readPosition = (readPosition + toCopy) % rawRingBuffer.Length;
            return toCopy;
        }
    }

    public int CreateRawReadPositionBehindMs(float delayMs)
    {
        lock (rawBufferLock)
        {
            if (rawRingBuffer == null || rawRingBuffer.Length == 0)
            {
                return -1;
            }

            float clampedMs = Mathf.Max(0f, delayMs);
            int samplesBehind = Mathf.RoundToInt((clampedMs / 1000f) * sampleRate);
            if (samplesBehind >= rawRingBuffer.Length)
            {
                samplesBehind = rawRingBuffer.Length - 1;
            }

            int readPos = (rawWritePosition - samplesBehind + rawRingBuffer.Length) % rawRingBuffer.Length;
            return readPos;
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

        if (!IsCurrentDeviceStillAvailable())
        {
            ScheduleRecoveryAttempt($"Microphone device '{microphoneDeviceName}' is no longer available.");
            StopMicrophoneCapture();
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            return;
        }

        int micPosWrite = Microphone.GetPosition(microphoneDeviceName);
        if (micPosWrite < 0 || micPosWrite >= microphoneBuffer.samples || microphoneBuffer.samples <= 0)
        {
            ScheduleRecoveryAttempt($"Invalid Microphone.GetPosition() value '{micPosWrite}' for device '{microphoneDeviceName}'.");
            StopMicrophoneCapture();
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            return;
        }

        if (lastMicWritePosition == micPosWrite)
        {
            stalledWriteHeadFrameCount++;
        }
        else
        {
            stalledWriteHeadFrameCount = 0;
        }
        lastMicWritePosition = micPosWrite;

        if (stalledWriteHeadFrameCount >= Mathf.Max(5, stalledWriteHeadFrameThreshold))
        {
            ScheduleRecoveryAttempt($"Detected stalled microphone write-head for device '{microphoneDeviceName}'.");
            StopMicrophoneCapture();
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            return;
        }

        int frameCount = (microphoneBuffer.samples + micPosWrite - micPosRead) % microphoneBuffer.samples;
        if (frameCount <= 0)
        {
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            return;
        }

        if (latestRawFrame.Length < frameCount)
        {
            latestRawFrame = new float[frameCount];
        }

        ReadMicFramesIntoLatestRawMono(frameCount);
        latestRawSampleCount = frameCount;
        WriteRawFrameToRingBuffer(frameCount);

        if (latestNormalizedFrame.Length < frameCount)
        {
            latestNormalizedFrame = new float[frameCount];
        }

        if (!normalizationEnabled)
        {
            Array.Copy(latestRawFrame, latestNormalizedFrame, frameCount);
        }
        else
        {
            float gainLinear = GetNormalizationGainLinear();
            for (int i = 0; i < frameCount; i++)
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
        for (int i = 0; i < frameCount; i++)
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

        WriteNormalizedFrameToRingBuffer(frameCount);
        latestNormalizedSampleCount = frameCount;
    }

    private void OnNormalizationConfigChanged()
    {
        // Force recompute on next read so consumers get fresh settings immediately.
        lastFrameUpdated = -1;
        NormalizationStateChanged?.Invoke(GetNormalizationState());
    }

    private void WriteRawFrameToRingBuffer(int sampleCount)
    {
        if (sampleCount <= 0 || rawRingBuffer == null || rawRingBuffer.Length == 0)
        {
            return;
        }

        lock (rawBufferLock)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                rawRingBuffer[rawWritePosition] = latestRawFrame[i];
                rawWritePosition = (rawWritePosition + 1) % rawRingBuffer.Length;
            }
        }
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

    private void ReadMicFramesIntoLatestRawMono(int frameCount)
    {
        int remainingFrames = frameCount;
        int writeOffset = 0;
        int readPos = micPosRead;

        while (remainingFrames > 0)
        {
            int chunkFrames = Mathf.Min(remainingFrames, Mathf.Max(1, micReadChunkSize));
            int chunkInterleavedLength = chunkFrames * micInputChannels;
            float[] sourceBuffer;

            if (micReadChunkBuffer.Length >= chunkInterleavedLength)
            {
                sourceBuffer = micReadChunkBuffer;
            }
            else
            {
                if (micReadTailBuffer.Length < chunkInterleavedLength)
                {
                    micReadTailBuffer = new float[chunkInterleavedLength];
                }
                sourceBuffer = micReadTailBuffer;
            }

            microphoneBuffer.GetData(sourceBuffer, readPos);

            if (micInputChannels == 1)
            {
                Array.Copy(sourceBuffer, 0, latestRawFrame, writeOffset, chunkFrames);
            }
            else
            {
                for (int frame = 0; frame < chunkFrames; frame++)
                {
                    float mono = 0f;
                    int interleavedStart = frame * micInputChannels;
                    for (int channel = 0; channel < micInputChannels; channel++)
                    {
                        mono += sourceBuffer[interleavedStart + channel];
                    }
                    latestRawFrame[writeOffset + frame] = mono / micInputChannels;
                }
            }

            readPos = (readPos + chunkFrames) % microphoneBuffer.samples;
            writeOffset += chunkFrames;
            remainingFrames -= chunkFrames;
        }

        micPosRead = readPos;
    }

    private bool TryResolveMicrophoneDevice(out string deviceName)
    {
        deviceName = null;
        var devices = Microphone.devices;
        if (devices == null || devices.Length == 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
        {
            foreach (var device in devices)
            {
                if (string.Equals(device, preferredDeviceName, StringComparison.Ordinal))
                {
                    deviceName = device;
                    return true;
                }
            }
        }

        deviceName = devices[0];
        return true;
    }

    private bool StartMicrophoneCapture(string deviceName)
    {
        StopMicrophoneCapture();

        microphoneDeviceName = deviceName;
        microphoneBuffer = Microphone.Start(microphoneDeviceName, true, loopLengthSeconds, sampleRate);
        if (microphoneBuffer == null)
        {
            microphoneDeviceName = null;
            return false;
        }

        micPosRead = 0;
        lastMicWritePosition = -1;
        stalledWriteHeadFrameCount = 0;
        captureEpoch++;
        micInputChannels = Mathf.Max(1, microphoneBuffer.channels);
        micInputWasDownmixedToMono = micInputChannels > (int)channelMode;
        if (micInputWasDownmixedToMono)
        {
            Debug.LogWarning($"MicPipeline: Input clip has {micInputChannels} channels. Downmixing to mono to honor pipeline channel contract.");
        }

        int ringSize = Mathf.Max(sampleRate * Mathf.Max(1, loopLengthSeconds), 1024);
        rawRingBuffer = new float[ringSize];
        rawWritePosition = 0;
        rawRingBufferCapacitySamples = ringSize;
        normalizedRingBuffer = new float[ringSize];
        normalizedWritePosition = 0;
        normalizedRingBufferCapacitySamples = ringSize;

        int chunkFrames = Mathf.Max(256, micReadChunkSize);
        int interleavedLength = chunkFrames * micInputChannels;
        micReadChunkBuffer = new float[interleavedLength];
        micReadTailBuffer = new float[interleavedLength];
        return true;
    }

    private bool IsCurrentDeviceStillAvailable()
    {
        if (string.IsNullOrEmpty(microphoneDeviceName))
        {
            return false;
        }

        var devices = Microphone.devices;
        if (devices == null || devices.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (string.Equals(devices[i], microphoneDeviceName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void ScheduleRecoveryAttempt(string reason)
    {
        nextRecoveryAttemptTime = Time.unscaledTime + Mathf.Max(0.1f, recoveryRetryIntervalSeconds);
        if (!recoveryWarningLogged)
        {
            recoveryWarningLogged = true;
            Debug.LogWarning($"MicPipeline: {reason} Scheduling recovery retry.");
        }
    }

    private void UpdateNormalizationGainRiding()
    {
        gainRidingCurrentRateDbPerSecond = 0f;
        gainRidingLastAppliedStepDb = 0f;

        if (!gainRidingEnabled || !IsReady || imitoneVoiceIntepreter == null)
        {
            return;
        }

        bool canRaiseGain = imitoneVoiceIntepreter.toneActiveConfident;
        bool canLowerGain = imitoneVoiceIntepreter.toneActiveBiasTrue;
        if (!canRaiseGain && !canLowerGain)
        {
            return;
        }

        float currentMicDb = imitoneVoiceIntepreter._dbMicrophone;
        if (float.IsNaN(currentMicDb) || float.IsInfinity(currentMicDb))
        {
            return;
        }

        // Control against estimated normalized loudness so feedback closes as gain moves.
        float estimatedNormalizedDb = currentMicDb + normalizationGainDb;
        float dbDelta = estimatedNormalizedDb - gainRidingTargetDb; // positive => too loud, negative => too quiet
        float rateDbPerSecond = 0f;
        gainRidingLastMicDb = currentMicDb;
        gainRidingLastEstimatedNormalizedDb = estimatedNormalizedDb;
        gainRidingLastDbDelta = dbDelta;

        if (canLowerGain && dbDelta > gainRidingRapidLowerThresholdDb)
        {
            rateDbPerSecond = -gainRidingRapidLowerRateDbPerSecond;
        }
        else if (canLowerGain && dbDelta > gainRidingLowerThresholdDb)
        {
            rateDbPerSecond = -gainRidingLowerRateDbPerSecond;
        }
        else if (canRaiseGain && dbDelta < -gainRidingRaiseThresholdDb)
        {
            rateDbPerSecond = gainRidingRaiseRateDbPerSecond;
        }
        gainRidingCurrentRateDbPerSecond = rateDbPerSecond;

        if (Mathf.Approximately(rateDbPerSecond, 0f))
        {
            return;
        }

        float minGainDb = Mathf.Min(gainRidingGainDbClamp.x, gainRidingGainDbClamp.y);
        float maxGainDb = Mathf.Max(gainRidingGainDbClamp.x, gainRidingGainDbClamp.y);
        float nextGainDb = Mathf.Clamp(normalizationGainDb + (rateDbPerSecond * Time.deltaTime), minGainDb, maxGainDb);
        if (!Mathf.Approximately(nextGainDb, normalizationGainDb))
        {
            float appliedStepDb = nextGainDb - normalizationGainDb;
            gainRidingLastAppliedStepDb = appliedStepDb;
            gainRidingAccumulatedAdjustmentDb += appliedStepDb;
            normalizationGainDb = nextGainDb;
            OnNormalizationConfigChanged();
        }
    }

    private void UpdateNormalizationTelemetry()
    {
        normalizationGainDbRuntime = normalizationGainDb;
        normalizationGainLinearRuntime = GetNormalizationGainLinear();

        gainRidingGateEnabled = gainRidingEnabled;
        gainRidingGatePipelineReady = IsReady;
        gainRidingGateInterpreterBound = imitoneVoiceIntepreter != null;
        gainRidingGateToneActive = gainRidingGateInterpreterBound &&
                                   (imitoneVoiceIntepreter.toneActiveConfident || imitoneVoiceIntepreter.toneActiveBiasTrue);

        if (!gainRidingGateInterpreterBound)
        {
            gainRidingGateMicDbValid = false;
            gainRidingLastMicDb = float.NaN;
            gainRidingLastDbDelta = 0f;
            return;
        }

        float currentMicDb = imitoneVoiceIntepreter._dbMicrophone;
        gainRidingGateMicDbValid = !float.IsNaN(currentMicDb) && !float.IsInfinity(currentMicDb);
        gainRidingLastMicDb = currentMicDb;
        if (!gainRidingGateMicDbValid)
        {
            gainRidingLastEstimatedNormalizedDb = float.NaN;
            gainRidingLastDbDelta = 0f;
            return;
        }

        gainRidingLastEstimatedNormalizedDb = currentMicDb + normalizationGainDb;
        gainRidingLastDbDelta = gainRidingLastEstimatedNormalizedDb - gainRidingTargetDb;
    }

    private void StopMicrophoneCapture()
    {
        if (!string.IsNullOrEmpty(microphoneDeviceName))
        {
            Microphone.End(microphoneDeviceName);
        }

        microphoneBuffer = null;
        microphoneDeviceName = null;
        micPosRead = 0;
        lastMicWritePosition = -1;
        stalledWriteHeadFrameCount = 0;
    }

    private void OnDisable()
    {
        StopMicrophoneCapture();
    }
}
