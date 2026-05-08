using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

public partial class ImitoneVoiceIntepreter
{
    private enum MicChannelMode
    {
        Mono = 1
    }

    [Header("Microphone Source")]
    [SerializeField] private string preferredDeviceName = "";
    [SerializeField] [FormerlySerializedAs("sampleRate")] private int micCaptureSampleRate = 48000;
    [SerializeField] private int loopLengthSeconds = 6;
    [Tooltip("Chunk size used for microphone GetData reads to reduce allocation churn.")]
    [SerializeField] private int micReadChunkSize = 2048;
    [Tooltip("Retry interval used when microphone device is unavailable or capture fails.")]
    [SerializeField] private float recoveryRetryIntervalSeconds = 1f;
    [Tooltip("How many consecutive OnAudioFilterRead callbacks with unchanged Microphone.GetPosition trigger recovery (Step 5b: audio-thread stall detection).")]
    [SerializeField] [FormerlySerializedAs("stalledWriteHeadFrameThreshold")] private int audioThreadMicStallRecoveryThreshold = 120;
    [Tooltip("Second Microphone.GetPosition() when first is in-range; use if different (some platforms report a stale head on the first poll). Applies to audio-thread capture only.")]
    [SerializeField] [FormerlySerializedAs("micWriteHeadDoublePoll")] private bool audioThreadMicPositionDoublePoll = true;

    [Header("Channel Contract")]
    [Tooltip("Pipeline output contract for consumers. This pipeline publishes mono samples.")]
    [SerializeField] private MicChannelMode channelMode = MicChannelMode.Mono;

    [Header("Normalization")]
    [Tooltip("Master toggle for normalized output stream. Raw output is always unaffected.")]
    [SerializeField] private bool normalizationEnabled = false;
    [Tooltip("Gain in dB applied to normalized output stream.")]
    [SerializeField] private float normalizationGainDb = 16f;
    [Tooltip("Clamp normalized samples to +/- clamp value after gain.")]
    [SerializeField] private bool normalizationHardClampEnabled = true;
    [Tooltip("Absolute clamp value used when hard clamp is enabled.")]
    [SerializeField] [Range(0.01f, 1f)] private float normalizationClampAbs = 0.98f;

    [Header("Imitone Tone-Active Telemetry (Inspector)")]
    [SerializeField] private bool imitoneInterpreterBound = false;
    [SerializeField] private bool imitoneToneActive = false;
    [SerializeField] private bool imitoneToneActiveRaw = false;
    [SerializeField] private bool imitoneToneActiveConfident = false;
    [SerializeField] private bool imitoneToneActiveVeryConfident = false;
    [SerializeField] private bool imitoneToneActiveBiasTrue = false;

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
    [Tooltip("Gain riding can raise normalization only while toneActiveConfident duration is below this window.")]
    [SerializeField] [Range(0f, 30f)] private float gainRidingRaiseMaxToneActiveConfidentSeconds = 6f;
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
    [Tooltip("True when confident-tone duration is within the raise window.")]
    [SerializeField] private bool gainRidingGateRaiseWindowOpen = false;
    [Tooltip("True when mic level is not at/below the interpreter noise-floor threshold (raises blocked when false).")]
    [SerializeField] private bool gainRidingGateRaiseNoiseFloorClear = false;

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
    // Step 5b: mic clip read position — owned exclusively by the audio thread (OnAudioFilterRead capture block).
    private int micPosRead;
    private int captureEpoch;
    private int micInputChannels = 1;
    private bool micInputWasDownmixedToMono;
    private float nextRecoveryAttemptTime;
    private bool recoveryWarningLogged;
    private float[] rawRingBuffer = Array.Empty<float>();
    private int rawWritePosition;
    private long rawWriteTotalSamples;
    private readonly object rawBufferLock = new object();
    // Step 5b: rawBufferLock — audio-thread writer (capture → ring) contends with audio-thread readers on this
    // same Behaviour (imitone feed ReadRawSamples inside OnAudioFilterRead) plus DirectVoiceMonitoring's
    // OnAudioFilterRead (separate script order). TryEnter(0) misses increment rawRingReadLockMissTotal.
    private long rawRingReadLockMissTotal;
    private float[] normalizedRingBuffer = Array.Empty<float>();
    private int normalizedWritePosition;
    private long normalizedWriteTotalSamples;
    private readonly object normalizedBufferLock = new object();
    private float[] micReadChunkBuffer = Array.Empty<float>(); // interleaved input buffer (audio-thread-only use)
    private float[] micReadTailBuffer = Array.Empty<float>();  // interleaved input tail buffer (audio-thread-only use)
    // Step 5b: capture scratch — audio-thread-only.
    private float[] audioThreadCaptureMono = Array.Empty<float>();
    private float[] audioThreadNormalizedScratch = Array.Empty<float>();
    private int audioThreadLastMicWritePosition = -1;
    private int audioThreadStalledCallbackCount;
    private int _audioThreadMicRecoveryRequest; // Interlocked: 0=none, 1=invalid position, 2=stalled head

    private bool MicIngestIsReady => microphoneBuffer != null && !string.IsNullOrEmpty(microphoneDeviceName);
    public AudioClip MicrophoneBuffer => microphoneBuffer;
    public string MicrophoneDeviceName => microphoneDeviceName;
    public int MicrophoneSampleRate => micCaptureSampleRate;
    public bool IsMicReady => MicIngestIsReady;
    public int MicChannels => (int)channelMode;
    public int MicCaptureEpoch => captureEpoch;
    public event Action<MicNormalizationState> NormalizationStateChanged;

    [Serializable]
    public struct MicNormalizationState
    {
        public bool enabled;
        public float gainDb;
        public bool hardClampEnabled;
        public float clampAbs;
    }

    public MicIngestDebugSnapshot GetMicIngestDebugSnapshot()
    {
        long rawTotal = 0;
        long normTotal = 0;
        lock (rawBufferLock)
        {
            rawTotal = rawWriteTotalSamples;
        }

        lock (normalizedBufferLock)
        {
            normTotal = normalizedWriteTotalSamples;
        }

        return new MicIngestDebugSnapshot
        {
            ingestPathLabel = "audio_thread_ring_writes",
            rawRingWriteTotalSamples = rawTotal,
            normalizedRingWriteTotalSamples = normTotal,
        };
    }

    private void ProcessAudioThreadMicRecoveryRequests()
    {
        int r = Interlocked.Exchange(ref _audioThreadMicRecoveryRequest, 0);
        if (r == 1)
        {
            ScheduleRecoveryAttempt("Invalid Microphone.GetPosition from audio-thread capture.");
            StopMicrophoneCapture();
        }
        else if (r == 2)
        {
            ScheduleRecoveryAttempt("Stalled microphone write-head (audio-thread detection).");
            StopMicrophoneCapture();
        }
    }

    private void Awake()
    {
        InitializeMicrophone();
    }

    private void OnEnable()
    {
        if (!MicIngestIsReady)
        {
            InitializeMicrophone();
        }
    }

    /// <summary>
    /// Step 5b: main-thread housekeeping only — no mic clip reads. Device-loss + deferred audio-thread
    /// recovery requests, normalization gain riding / telemetry, peak-meter decay, audio-thread capture
    /// rebootstrap, imitone exception drain.
    /// </summary>
    private void MicIngestMainThreadTick()
    {
        ProcessAudioThreadMicRecoveryRequests();

        if (MicIngestIsReady && !IsCurrentDeviceStillAvailable())
        {
            ScheduleRecoveryAttempt($"Microphone device '{microphoneDeviceName}' is no longer available.");
            StopMicrophoneCapture();
        }

        if (!MicIngestIsReady && Time.unscaledTime >= nextRecoveryAttemptTime)
        {
            InitializeMicrophone();
        }

        UpdateNormalizationGainRiding();
        UpdateNormalizationTelemetry();

        normalizedPeakMeter = Mathf.Max(0f, normalizedPeakMeter - normalizedPeakMeterDecayPerSecond * Time.deltaTime);

        TryRebootstrapAudioThreadCaptureIfMicRecovered();
        DrainImitoneInputAudioPendingException();
    }

    public void InitializeMicrophone()
    {
        if (MicIngestIsReady)
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
        return AudioLevelUtilities.DbToLinear(normalizationGainDb);
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

    public int ReadNormalizedSamples(float[] destination, ref int readPosition, ref long readTotalSamples, out int overflowDroppedSamples)
    {
        overflowDroppedSamples = 0;
        if (destination == null || destination.Length == 0)
        {
            return 0;
        }

        bool lockTaken = false;
        try
        {
            lockTaken = Monitor.TryEnter(normalizedBufferLock, 0);
            if (!lockTaken)
            {
                // Audio-thread safety: never block waiting on producer lock.
                Array.Clear(destination, 0, destination.Length);
                int ringLength = normalizedRingBuffer != null ? normalizedRingBuffer.Length : 0;
                if (ringLength > 0)
                {
                    if (readPosition < 0 || readPosition >= ringLength)
                    {
                        readPosition = 0;
                    }
                    readPosition = (readPosition + destination.Length) % ringLength;
                }
                readTotalSamples += destination.Length;
                return 0;
            }

            if (normalizedRingBuffer == null || normalizedRingBuffer.Length == 0)
            {
                Array.Clear(destination, 0, destination.Length);
                return 0;
            }

            if (readPosition < 0 || readPosition >= normalizedRingBuffer.Length)
            {
                readPosition = normalizedWritePosition;
                readTotalSamples = normalizedWriteTotalSamples;
            }

            long writeTotalSamples = normalizedWriteTotalSamples;
            if (readTotalSamples > writeTotalSamples)
            {
                readTotalSamples = writeTotalSamples;
                readPosition = normalizedWritePosition;
            }

            long available = writeTotalSamples - readTotalSamples;
            int maxRealtimeLagSamples = Mathf.Max(destination.Length * 2, Mathf.RoundToInt(micCaptureSampleRate * 0.25f));
            if (available > maxRealtimeLagSamples)
            {
                overflowDroppedSamples = (int)Math.Min(int.MaxValue, available - maxRealtimeLagSamples);
                int skipSamples = overflowDroppedSamples % normalizedRingBuffer.Length;
                readPosition = (readPosition + skipSamples) % normalizedRingBuffer.Length;
                readTotalSamples += overflowDroppedSamples;
                available = writeTotalSamples - readTotalSamples;
            }
            if (available > normalizedRingBuffer.Length)
            {
                int hardDropSamples = (int)Math.Min(int.MaxValue, available - normalizedRingBuffer.Length);
                int skipSamples = hardDropSamples % normalizedRingBuffer.Length;
                readPosition = (readPosition + skipSamples) % normalizedRingBuffer.Length;
                readTotalSamples += hardDropSamples;
                overflowDroppedSamples += hardDropSamples;
                available = writeTotalSamples - readTotalSamples;
            }

            int toCopy = Mathf.Min(destination.Length, Mathf.Max(0, (int)available));
            for (int i = 0; i < toCopy; i++)
            {
                destination[i] = normalizedRingBuffer[(readPosition + i) % normalizedRingBuffer.Length];
            }

            int requested = destination.Length;
            if (toCopy < requested)
            {
                Array.Clear(destination, toCopy, requested - toCopy);
            }

            // Real-time consumer pacing: even when we underrun and fill silence,
            // advance cursor by requested samples so monitoring does not accumulate lag.
            readPosition = (readPosition + requested) % normalizedRingBuffer.Length;
            readTotalSamples += requested;
            return toCopy;
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(normalizedBufferLock);
            }
        }
    }

    public int ReadRawSamples(float[] destination, ref int readPosition, ref long readTotalSamples, out int overflowDroppedSamples)
    {
        overflowDroppedSamples = 0;
        if (destination == null || destination.Length == 0)
        {
            return 0;
        }

        bool lockTaken = false;
        try
        {
            lockTaken = Monitor.TryEnter(rawBufferLock, 0);
            if (!lockTaken)
            {
                Interlocked.Increment(ref rawRingReadLockMissTotal);
                // Audio-thread safety: never block waiting on producer lock.
                Array.Clear(destination, 0, destination.Length);
                int ringLength = rawRingBuffer != null ? rawRingBuffer.Length : 0;
                if (ringLength > 0)
                {
                    if (readPosition < 0 || readPosition >= ringLength)
                    {
                        readPosition = 0;
                    }
                    readPosition = (readPosition + destination.Length) % ringLength;
                }
                readTotalSamples += destination.Length;
                return 0;
            }

            if (rawRingBuffer == null || rawRingBuffer.Length == 0)
            {
                Array.Clear(destination, 0, destination.Length);
                return 0;
            }

            if (readPosition < 0 || readPosition >= rawRingBuffer.Length)
            {
                readPosition = rawWritePosition;
                readTotalSamples = rawWriteTotalSamples;
            }

            long writeTotalSamples = rawWriteTotalSamples;
            if (readTotalSamples > writeTotalSamples)
            {
                readTotalSamples = writeTotalSamples;
                readPosition = rawWritePosition;
            }

            long available = writeTotalSamples - readTotalSamples;
            int maxRealtimeLagSamples = Mathf.Max(destination.Length * 2, Mathf.RoundToInt(micCaptureSampleRate * 0.25f));
            if (available > maxRealtimeLagSamples)
            {
                overflowDroppedSamples = (int)Math.Min(int.MaxValue, available - maxRealtimeLagSamples);
                int skipSamples = overflowDroppedSamples % rawRingBuffer.Length;
                readPosition = (readPosition + skipSamples) % rawRingBuffer.Length;
                readTotalSamples += overflowDroppedSamples;
                available = writeTotalSamples - readTotalSamples;
            }
            if (available > rawRingBuffer.Length)
            {
                int hardDropSamples = (int)Math.Min(int.MaxValue, available - rawRingBuffer.Length);
                int skipSamples = hardDropSamples % rawRingBuffer.Length;
                readPosition = (readPosition + skipSamples) % rawRingBuffer.Length;
                readTotalSamples += hardDropSamples;
                overflowDroppedSamples += hardDropSamples;
                available = writeTotalSamples - readTotalSamples;
            }

            int toCopy = Mathf.Min(destination.Length, Mathf.Max(0, (int)available));
            for (int i = 0; i < toCopy; i++)
            {
                destination[i] = rawRingBuffer[(readPosition + i) % rawRingBuffer.Length];
            }

            int requested = destination.Length;
            if (toCopy < requested)
            {
                Array.Clear(destination, toCopy, requested - toCopy);
            }

            // Real-time consumer pacing: even when we underrun and fill silence,
            // advance cursor by requested samples so monitoring does not accumulate lag.
            readPosition = (readPosition + requested) % rawRingBuffer.Length;
            readTotalSamples += requested;
            return toCopy;
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(rawBufferLock);
            }
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
            int samplesBehind = Mathf.RoundToInt((clampedMs / 1000f) * micCaptureSampleRate);
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
            int samplesBehind = Mathf.RoundToInt((clampedMs / 1000f) * micCaptureSampleRate);
            if (samplesBehind >= normalizedRingBuffer.Length)
            {
                samplesBehind = normalizedRingBuffer.Length - 1;
            }

            int readPos = (normalizedWritePosition - samplesBehind + normalizedRingBuffer.Length) % normalizedRingBuffer.Length;
            return readPos;
        }
    }

    public bool TryCreateRawReadCursorBehindMs(float delayMs, out int readPosition, out long readTotalSamples)
    {
        readPosition = -1;
        readTotalSamples = 0;
        lock (rawBufferLock)
        {
            if (rawRingBuffer == null || rawRingBuffer.Length == 0)
            {
                return false;
            }

            float clampedMs = Mathf.Max(0f, delayMs);
            int samplesBehind = Mathf.RoundToInt((clampedMs / 1000f) * micCaptureSampleRate);
            if (samplesBehind >= rawRingBuffer.Length)
            {
                samplesBehind = rawRingBuffer.Length - 1;
            }

            readPosition = (rawWritePosition - samplesBehind + rawRingBuffer.Length) % rawRingBuffer.Length;
            readTotalSamples = Math.Max(0L, rawWriteTotalSamples - (long)samplesBehind);
            return true;
        }
    }

    public bool TryCreateNormalizedReadCursorBehindMs(float delayMs, out int readPosition, out long readTotalSamples)
    {
        readPosition = -1;
        readTotalSamples = 0;
        lock (normalizedBufferLock)
        {
            if (normalizedRingBuffer == null || normalizedRingBuffer.Length == 0)
            {
                return false;
            }

            float clampedMs = Mathf.Max(0f, delayMs);
            int samplesBehind = Mathf.RoundToInt((clampedMs / 1000f) * micCaptureSampleRate);
            if (samplesBehind >= normalizedRingBuffer.Length)
            {
                samplesBehind = normalizedRingBuffer.Length - 1;
            }

            readPosition = (normalizedWritePosition - samplesBehind + normalizedRingBuffer.Length) % normalizedRingBuffer.Length;
            readTotalSamples = Math.Max(0L, normalizedWriteTotalSamples - (long)samplesBehind);
            return true;
        }
    }

    /// <summary>
    /// runs on: audio thread — <see cref="OnAudioFilterRead"/> only, before the imitone feed read.
    /// Pulls mono samples from the streaming mic clip and appends raw + normalized rings. Stall / invalid
    /// position requests recovery on the main thread via <see cref="_audioThreadMicRecoveryRequest"/>.
    /// </summary>
    private void AudioThreadCaptureMicAndWriteRings(int maxFramesPerCallback)
    {
        if (!MicIngestIsReady || microphoneBuffer == null || string.IsNullOrEmpty(microphoneDeviceName))
        {
            return;
        }

        int clipSamples = microphoneBuffer.samples;
        if (clipSamples <= 0)
        {
            return;
        }

        int micPosWrite = Microphone.GetPosition(microphoneDeviceName);
        if (audioThreadMicPositionDoublePoll &&
            micPosWrite >= 0 &&
            micPosWrite < clipSamples)
        {
            int w2 = Microphone.GetPosition(microphoneDeviceName);
            if (w2 >= 0 && w2 < clipSamples && w2 != micPosWrite)
            {
                micPosWrite = w2;
            }
        }

        if (micPosWrite < 0 || micPosWrite >= clipSamples)
        {
            Interlocked.Exchange(ref _audioThreadMicRecoveryRequest, 1);
            return;
        }

        if (audioThreadLastMicWritePosition == micPosWrite)
        {
            audioThreadStalledCallbackCount++;
        }
        else
        {
            audioThreadStalledCallbackCount = 0;
        }

        audioThreadLastMicWritePosition = micPosWrite;

        if (audioThreadStalledCallbackCount >= Mathf.Max(5, audioThreadMicStallRecoveryThreshold))
        {
            Interlocked.Exchange(ref _audioThreadMicRecoveryRequest, 2);
            return;
        }

        int available = (clipSamples + micPosWrite - micPosRead) % clipSamples;
        if (available <= 0)
        {
            return;
        }

        int frameCount = Mathf.Min(available, maxFramesPerCallback);
        if (frameCount <= 0)
        {
            return;
        }

        if (audioThreadCaptureMono.Length < frameCount)
        {
            audioThreadCaptureMono = new float[frameCount];
        }

        if (audioThreadNormalizedScratch.Length < frameCount)
        {
            audioThreadNormalizedScratch = new float[frameCount];
        }

        ReadMicClipToMonoBuffer(frameCount, ref micPosRead, audioThreadCaptureMono);
        AppendMonoSamplesToRawRing(audioThreadCaptureMono, frameCount);
        BuildNormalizedScratchFromRaw(audioThreadCaptureMono, audioThreadNormalizedScratch, frameCount);
        AppendMonoSamplesToNormalizedRing(audioThreadNormalizedScratch, frameCount);

        float framePeak = 0f;
        for (int i = 0; i < frameCount; i++)
        {
            float abs = Mathf.Abs(audioThreadNormalizedScratch[i]);
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
    }

    private void OnNormalizationConfigChanged()
    {
        NormalizationStateChanged?.Invoke(GetNormalizationState());
    }

    private void AppendMonoSamplesToRawRing(float[] src, int sampleCount)
    {
        if (sampleCount <= 0 || rawRingBuffer == null || rawRingBuffer.Length == 0)
        {
            return;
        }

        lock (rawBufferLock)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                rawRingBuffer[rawWritePosition] = src[i];
                rawWritePosition = (rawWritePosition + 1) % rawRingBuffer.Length;
                rawWriteTotalSamples++;
            }
        }
    }

    private void AppendMonoSamplesToNormalizedRing(float[] src, int sampleCount)
    {
        if (sampleCount <= 0 || normalizedRingBuffer == null || normalizedRingBuffer.Length == 0)
        {
            return;
        }

        lock (normalizedBufferLock)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                normalizedRingBuffer[normalizedWritePosition] = src[i];
                normalizedWritePosition = (normalizedWritePosition + 1) % normalizedRingBuffer.Length;
                normalizedWriteTotalSamples++;
            }
        }
    }

    private void BuildNormalizedScratchFromRaw(float[] raw, float[] destNormalized, int frameCount)
    {
        if (!normalizationEnabled)
        {
            Array.Copy(raw, 0, destNormalized, 0, frameCount);
            return;
        }

        float gainLinear = GetNormalizationGainLinear();
        for (int i = 0; i < frameCount; i++)
        {
            float normalizedSample = raw[i] * gainLinear;
            if (normalizationHardClampEnabled)
            {
                normalizedSample = Mathf.Clamp(normalizedSample, -normalizationClampAbs, normalizationClampAbs);
            }

            destNormalized[i] = normalizedSample;
        }
    }

    private void ReadMicClipToMonoBuffer(int frameCount, ref int readPos, float[] destMono)
    {
        int remainingFrames = frameCount;
        int writeOffset = 0;

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
                Array.Copy(sourceBuffer, 0, destMono, writeOffset, chunkFrames);
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

                    destMono[writeOffset + frame] = mono / micInputChannels;
                }
            }

            readPos = (readPos + chunkFrames) % microphoneBuffer.samples;
            writeOffset += chunkFrames;
            remainingFrames -= chunkFrames;
        }
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
        microphoneBuffer = Microphone.Start(microphoneDeviceName, true, loopLengthSeconds, micCaptureSampleRate);
        if (microphoneBuffer == null)
        {
            microphoneDeviceName = null;
            return false;
        }

        micPosRead = 0;
        Interlocked.Exchange(ref _audioThreadMicRecoveryRequest, 0);
        audioThreadLastMicWritePosition = -1;
        audioThreadStalledCallbackCount = 0;
        captureEpoch++;
        micInputChannels = Mathf.Max(1, microphoneBuffer.channels);
        micInputWasDownmixedToMono = micInputChannels > (int)channelMode;
        if (micInputWasDownmixedToMono)
        {
            Debug.LogWarning($"Mic ingest: Input clip has {micInputChannels} channels. Downmixing to mono to honor pipeline channel contract.");
        }

        int ringSize = Mathf.Max(micCaptureSampleRate * Mathf.Max(1, loopLengthSeconds), 1024);
        rawRingBuffer = new float[ringSize];
        rawWritePosition = 0;
        rawWriteTotalSamples = 0;
        Interlocked.Exchange(ref rawRingReadLockMissTotal, 0L);
        rawRingBufferCapacitySamples = ringSize;
        normalizedRingBuffer = new float[ringSize];
        normalizedWritePosition = 0;
        normalizedWriteTotalSamples = 0;
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
            Debug.LogWarning($"Mic ingest: {reason} Scheduling recovery retry.");
        }
    }

    private void UpdateNormalizationGainRiding()
    {
        gainRidingCurrentRateDbPerSecond = 0f;
        gainRidingLastAppliedStepDb = 0f;

        if (!gainRidingEnabled || !MicIngestIsReady)
        {
            return;
        }

        float confidentToneDuration = Mathf.Max(0f, _tThisToneConfident);
        bool raiseWindowOpen = confidentToneDuration < Mathf.Max(0f, gainRidingRaiseMaxToneActiveConfidentSeconds);
        bool raiseNoiseFloorClear = !micIsNearNoiseFloor;
        bool canRaiseGain = toneActiveConfident && raiseWindowOpen && raiseNoiseFloorClear;
        bool canLowerGain = toneActiveBiasTrue;
        if (!canRaiseGain && !canLowerGain)
        {
            return;
        }

        float currentMicDb = _dbMicrophone;
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

        imitoneInterpreterBound = true;
        imitoneToneActive = toneActive;
        imitoneToneActiveRaw = toneActiveRaw;
        imitoneToneActiveConfident = toneActiveConfident;
        imitoneToneActiveVeryConfident = toneActiveVeryConfident;
        imitoneToneActiveBiasTrue = toneActiveBiasTrue;

        gainRidingGateEnabled = gainRidingEnabled;
        gainRidingGatePipelineReady = MicIngestIsReady;
        gainRidingGateInterpreterBound = true;
        gainRidingGateToneActive = toneActiveConfident || toneActiveBiasTrue;
        gainRidingGateRaiseWindowOpen = _tThisToneConfident < Mathf.Max(0f, gainRidingRaiseMaxToneActiveConfidentSeconds);
        gainRidingGateRaiseNoiseFloorClear = !micIsNearNoiseFloor;

        float currentMicDb = _dbMicrophone;
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
    }

    private void OnDisable()
    {
        StopAudioThreadCapture();
        StopMicrophoneCapture();
    }
}
