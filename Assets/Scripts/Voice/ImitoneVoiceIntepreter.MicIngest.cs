using System;
using System.Threading;
using UnityEngine;

public partial class ImitoneVoiceIntepreter
{
    private enum MicChannelMode
    {
        Mono = 1
    }

    private string preferredDeviceName = "";
    private int micCaptureSampleRate = 48000;
    private int loopLengthSeconds = 6;
    [Tooltip("Chunk size used for microphone GetData reads to reduce allocation churn.")]
    private int micReadChunkSize = 2048;
    [Tooltip("Retry interval used when microphone device is unavailable or capture fails.")]
    private float recoveryRetryIntervalSeconds = 1f;
    [Tooltip("How long (seconds) with no microphone write-head movement before mic recovery is scheduled. " +
             "Replaces the previous frame-count threshold so behavior is FPS-independent — default 2 s " +
             "preserves the original 60 FPS / 120-frame timing across the 30 / 20 FPS power-aware caps. " +
             "Uses Time.unscaledDeltaTime so recovery still fires on wall-clock time even if Time.timeScale " +
             "is ever changed.")]
    private float stalledWriteHeadTimeoutSeconds = 2f;
    [Tooltip("Second Microphone.GetPosition() when first is in-range; use if different (some platforms report a stale head on the first poll). F1-hybrid producer defense — keep on.")]
    private bool micWriteHeadDoublePoll = true;

    [Tooltip("Pipeline output contract for consumers. This pipeline publishes mono samples.")]
    private MicChannelMode channelMode = MicChannelMode.Mono;

    [Header("Normalization")]
    [Tooltip("Master toggle for normalized output stream. Raw output is always unaffected.")]
    [SerializeField] private bool normalizationEnabled = true;
    [Tooltip("Gain in dB applied to normalized output stream.")]
    [SerializeField] private float normalizationGainDb = 16f;
    [Tooltip("Clamp normalized samples to +/- clamp value after gain.")]
    private bool normalizationHardClampEnabled = true;
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
    [SerializeField] private bool gainRidingEnabled = true;
    [SerializeField] private float gainRidingTargetDb = -22f;
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
    [SerializeField] private Vector2 gainRidingGainDbClamp = new Vector2(-36f, 36f);

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
    [Tooltip("True when stage policy blocks gain-riding raises (opening/savasana); lowers still allowed.")]
    [SerializeField] private bool gainRidingGateRaiseFrozen = false;

    /// <summary>
    /// When true, normalization gain riding may lower gain but not raise it.
    /// Used during opening/savasana where toning is not expected (avoids creep on false positives).
    /// </summary>
    private bool normalizationGainRidingRaiseFrozen;

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

    [Header("Debug (copy when mic ingest stuck)")]
    [Tooltip("Last branch taken in UpdateMicReadFrame: not_ready | device_unavailable | invalid_mic_position | stalled_capture_stopped | unread_zero | copied_samples")]
    [SerializeField] private string debugMicLastExitReason = "";
    [SerializeField] private int debugMicLastUnreadComputed = -1;
    [SerializeField] private int debugMicLastLatestRawSampleCount = -1;
    [SerializeField] private int debugMicLastMicPosWrite = -1;
    [SerializeField] private int debugMicLastMicPosRead = -1;
    [SerializeField] private int debugMicLastStalledWriteHeadFrameCount;
    [SerializeField] private int debugMicLastClipSamples;
    [SerializeField] private int debugMicLastUnityFrame;

    private string microphoneDeviceName;
    private AudioClip microphoneBuffer;
    private int micPosRead;
    private int captureEpoch;
    private int micInputChannels = 1;
    private bool micInputWasDownmixedToMono;
    private int lastMicWritePosition = -1;
    // stalledWriteHeadFrameCount is retained purely for telemetry (debugMicLastStalledWriteHeadFrameCount,
    // MicVoiceIngestDebugAggregate.aggMicStalledWriteHeadFrames). The actual recovery trigger now uses
    // stalledWriteHeadStallSeconds so behavior is FPS-independent.
    private int stalledWriteHeadFrameCount;
    private float stalledWriteHeadStallSeconds;
    private float nextRecoveryAttemptTime;
    private bool recoveryWarningLogged;
    private float[] latestRawFrame = Array.Empty<float>();
    private int latestRawSampleCount;
    private float[] rawRingBuffer = Array.Empty<float>();
    private int rawWritePosition;
    private long rawWriteTotalSamples;
    private readonly object rawBufferLock = new object();
    // Step 3a Pass 2 (Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md): counts TryEnter(0) misses on rawBufferLock
    // from audio-thread readers (the imitone feed in OnAudioFilterRead, DirectVoiceMonitoring). Drives
    // FAIL_AUDIO_LOCK_CONTENTION as of pass 3 (the previous source — audioCallbackLockMissTotal on the
    // now-deleted audioRingWriteLock — was retired). Should stay near 0 in steady play; sustained increments
    // mean the main-thread writer is holding rawBufferLock long enough to clash with one or both audio-thread reads.
    private long rawRingReadLockMissTotal;
    private float[] latestNormalizedFrame = Array.Empty<float>();
    private int latestNormalizedSampleCount;
    private float[] normalizedRingBuffer = Array.Empty<float>();
    private int normalizedWritePosition;
    private long normalizedWriteTotalSamples;
    private readonly object normalizedBufferLock = new object();
    private int lastFrameUpdated = -1;
    private float[] micReadChunkBuffer = Array.Empty<float>(); // interleaved input buffer
    private float[] micReadTailBuffer = Array.Empty<float>();  // interleaved input tail buffer

    private bool MicIngestIsReady => microphoneBuffer != null && !string.IsNullOrEmpty(microphoneDeviceName);
    public AudioClip MicrophoneBuffer => microphoneBuffer;
    public string MicrophoneDeviceName => microphoneDeviceName;
    public int MicrophoneSampleRate => micCaptureSampleRate;
    public bool IsMicReady => MicIngestIsReady;
    public int MicChannels => (int)channelMode;
    public int MicCaptureEpoch => captureEpoch;
    public event Action<MicNormalizationState> NormalizationStateChanged;

    /// <summary>Current rise-rate (dB/sec) used by normalization gain-riding inside the confident-tone window. Used by stage handlers that want to capture-and-restore around a temporary override (e.g. calibration boosts to 6×).</summary>
    public float GetNormalizationGainRidingRaiseRateDbPerSecond() => gainRidingRaiseRateDbPerSecond;

    /// <summary>Sets the rise-rate (dB/sec) used by normalization gain-riding inside the confident-tone window. Clamped to the inspector range. Callers (e.g. <c>CalibrationStageHandler</c>) should cache <see cref="GetNormalizationGainRidingRaiseRateDbPerSecond"/> before overriding so they can restore on exit.</summary>
    public void SetNormalizationGainRidingRaiseRateDbPerSecond(float dbPerSecond)
    {
        gainRidingRaiseRateDbPerSecond = Mathf.Clamp(dbPerSecond, 0f, 24f);
    }

    /// <summary>When <paramref name="frozen"/> is true, gain riding may lower normalization gain but not raise it.</summary>
    public void SetNormalizationGainRidingRaiseFrozen(bool frozen)
    {
        if (normalizationGainRidingRaiseFrozen == frozen)
            return;
        normalizationGainRidingRaiseFrozen = frozen;
    }

    public bool NormalizationGainRidingRaiseFrozen => normalizationGainRidingRaiseFrozen;

    [Serializable]
    public struct MicNormalizationState
    {
        public bool enabled;
        public float gainDb;
        public bool hardClampEnabled;
        public float clampAbs;
    }

    // runs on: main thread (called from MicVoiceIngestDebugAggregate.LateUpdate). Briefly takes
    // rawBufferLock + normalizedBufferLock with blocking `lock` — fine on main thread.
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
            lastExitReason = debugMicLastExitReason ?? "",
            lastUnreadComputed = debugMicLastUnreadComputed,
            lastLatestRawSampleCount = debugMicLastLatestRawSampleCount,
            lastMicPosWrite = debugMicLastMicPosWrite,
            lastMicPosRead = debugMicLastMicPosRead,
            lastStalledWriteHeadFrameCount = debugMicLastStalledWriteHeadFrameCount,
            lastClipSamples = debugMicLastClipSamples,
            lastUnityFrame = debugMicLastUnityFrame,
            rawRingWriteTotalSamples = rawTotal,
            normalizedRingWriteTotalSamples = normTotal,
        };
    }

    // runs on: main thread (Unity lifecycle).
    private void Awake()
    {
        gameOn = true;
        gameOnLastFrame = true;

        // Runtime accumulators — defensively zeroed each session so a stray serialized value never leaks in.
        _imitoneInactiveRawTimer = 0f;
        _tThisTone = 0f;
        _tThisToneRaw = 0f;
        _tThisToneConfident = 0f;
        _tThisToneBiasTrue = 0f;
        _tThisRest = 0f;
        _tThisRestRaw = 0f;
        _tThisRestConfident = 0f;
        _breathHoldTimeBeforeInhale = 0f;
        _tNextInhaleDuration = 0f;
        _breathVolume = 0f;

        InitializeMicrophone();
    }

    // runs on: main thread (Unity lifecycle).
    private void OnEnable()
    {
        if (!MicIngestIsReady)
        {
            InitializeMicrophone();
        }
    }

    /// <summary>
    /// Run early each frame so microphone frames are produced before <see cref="GetRawVoiceData"/>.
    /// </summary>
    // runs on: main thread (the F1-hybrid producer's per-frame entry point — see Step 3a). Pumps
    // UpdateMicReadFrame (Microphone.GetPosition + GetData → rawRingBuffer write), normalization,
    // audio-thread rebootstrap, and the deferred-imitone-exception drain. All forbidden-on-audio-thread
    // operations live downstream of here.
    private void MicIngestMainThreadTick()
    {
        if (!MicIngestIsReady && Time.unscaledTime >= nextRecoveryAttemptTime)
        {
            InitializeMicrophone();
        }

        EnsureFrameUpdated();
        UpdateNormalizationGainRiding();
        UpdateNormalizationTelemetry();

        normalizedPeakMeter = Mathf.Max(0f, normalizedPeakMeter - normalizedPeakMeterDecayPerSecond * Time.deltaTime);

        // Step 3a prep: catches scheduled recovery (InitializeMicrophone above) — that path ticks captureEpoch
        // on success but does not rebootstrap the audio-thread capture path. Without this, the AudioSource keeps
        // pointing at a destroyed AudioClip after recovery and OnAudioFilterRead silently dies.
        // (5b-ii: the gentle-restart trigger that previously also flowed through here was deleted.)
        TryRebootstrapAudioThreadCaptureIfMicRecovered();

        // Step 3a: drain any deferred imitone.InputAudio exception captured by OnAudioFilterRead (Debug.Log* is
        // unsafe / GC-heavy from the audio thread; we log once per session from main thread).
        DrainImitoneInputAudioPendingException();
    }

    // runs on: main thread (Microphone.Start lives here — V3 firm rule forbids it on the audio thread).
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

    // runs on: main thread (calls EnsureFrameUpdated which gates by Time.frameCount; reads
    // latestRawSampleCount / latestRawFrame which are updated only by UpdateMicReadFrame on the
    // main thread). May allocate a destination array — never call from the audio thread.
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

    // runs on: main thread (same constraints as TryCopyLatestRawFrame).
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

    // runs on: main thread (Inspector / scripted reads of plain Inspector fields; no synchronization
    // — these fields are written only from the main thread).
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

    // runs on: main thread (writes Inspector fields + fires OnNormalizationConfigChanged event;
    // event subscribers are also main-thread).
    public void SetNormalizationState(MicNormalizationState state)
    {
        normalizationEnabled = state.enabled;
        normalizationGainDb = state.gainDb;
        normalizationHardClampEnabled = state.hardClampEnabled;
        normalizationClampAbs = Mathf.Clamp(state.clampAbs, 0.01f, 1f);
        OnNormalizationConfigChanged();
    }

    // runs on: main thread.
    public void SetNormalizationEnabled(bool enabled)
    {
        normalizationEnabled = enabled;
        OnNormalizationConfigChanged();
    }

    // runs on: main thread.
    public void SetNormalizationGainDb(float gainDb)
    {
        normalizationGainDb = gainDb;
        OnNormalizationConfigChanged();
    }

    // runs on: main thread.
    public void SetNormalizationHardClampEnabled(bool enabled)
    {
        normalizationHardClampEnabled = enabled;
        OnNormalizationConfigChanged();
    }

    // runs on: main thread.
    public void SetNormalizationClampAbs(float clampAbs)
    {
        normalizationClampAbs = Mathf.Clamp(clampAbs, 0.01f, 1f);
        OnNormalizationConfigChanged();
    }

    // runs on: main thread (reads non-atomic float `normalizationGainDb`, written only on main thread).
    public float GetNormalizationGainLinear()
    {
        return AudioLevelUtilities.DbToLinear(normalizationGainDb);
    }

    // runs on: main thread ONLY (uses blocking `lock(normalizedBufferLock)`). The 4-arg overload
    // below is the audio-thread-safe variant. Kept for legacy callers; if a caller is unclear which
    // overload to use, prefer the 4-arg version unconditionally.
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

    // runs on: main thread ONLY (uses blocking `lock(rawBufferLock)`). Audio-thread callers must use
    // the 4-arg overload below.
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

    // runs on: BOTH — main thread (Update / LateUpdate consumers) and audio thread (DirectVoiceMonitoring's
    // OnAudioFilterRead). Uses Monitor.TryEnter(0) to never block the audio thread; on a lock miss the
    // destination is zero-filled and the cursor advances by `destination.Length` so the consumer's
    // pacing doesn't drift. Allocation-free in steady state.
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

    // runs on: BOTH — main thread (DirectVoiceMonitoring main-thread setup paths) and audio thread
    // (OnAudioFilterRead's imitone feed in the AudioThread partial). Uses Monitor.TryEnter(0); on a
    // lock miss increments `rawRingReadLockMissTotal` (drives FAIL_AUDIO_LOCK_CONTENTION) and
    // returns silence with cursor advanced for pacing. Allocation-free in steady state.
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

    // runs on: main thread ONLY (blocking `lock(rawBufferLock)`). Used during consumer setup to seed
    // a read cursor at a target latency behind the live write head.
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

    // runs on: main thread ONLY (blocking `lock(normalizedBufferLock)`).
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

    // runs on: main thread ONLY (blocking `lock(rawBufferLock)`). Called from
    // WaitMicPositionThenPlayCapture (main-thread coroutine) to seed the audio-thread read cursor
    // before captureSource.Play. Returns the (position, totalSamples) pair atomically under the lock.
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

    // runs on: main thread ONLY (blocking `lock(normalizedBufferLock)`). Used by main-thread
    // monitoring setup to seed a normalized-stream read cursor.
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

    // runs on: main thread ONLY (reads `Time.frameCount` — main-thread Unity API). Idempotent within
    // a frame: gates UpdateMicReadFrame by frameCount so multiple consumers don't double-pump the mic.
    private void EnsureFrameUpdated()
    {
        if (lastFrameUpdated == Time.frameCount)
        {
            return;
        }

        UpdateMicReadFrame();

        lastFrameUpdated = Time.frameCount;
    }

    // runs on: main thread ONLY. The F1-hybrid producer's heart: polls Microphone.GetPosition
    // (V3 firm rule — forbidden on audio thread), reads samples via microphoneBuffer.GetData
    // (also forbidden on audio thread), pushes them into rawRingBuffer / normalizedRingBuffer
    // under their respective locks. Audio-thread consumers read those rings via the 4-arg
    // ReadRawSamples / ReadNormalizedSamples (TryEnter-safe).
    private void UpdateMicReadFrame()
    {
        debugMicLastUnityFrame = Time.frameCount;

        if (!MicIngestIsReady)
        {
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            debugMicLastExitReason = "not_ready";
            debugMicLastUnreadComputed = -1;
            debugMicLastLatestRawSampleCount = 0;
            debugMicLastMicPosWrite = -1;
            debugMicLastMicPosRead = micPosRead;
            debugMicLastStalledWriteHeadFrameCount = stalledWriteHeadFrameCount;
            debugMicLastClipSamples = microphoneBuffer != null ? microphoneBuffer.samples : 0;
            return;
        }

        if (!IsCurrentDeviceStillAvailable())
        {
            ScheduleRecoveryAttempt($"Microphone device '{microphoneDeviceName}' is no longer available.");
            int readPosForDebug = micPosRead;
            int clipSamplesForDebug = microphoneBuffer != null ? microphoneBuffer.samples : 0;
            int stalledFramesForDebug = stalledWriteHeadFrameCount;
            StopMicrophoneCapture();
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            debugMicLastExitReason = "device_unavailable";
            debugMicLastUnreadComputed = -1;
            debugMicLastLatestRawSampleCount = 0;
            debugMicLastMicPosWrite = -1;
            debugMicLastMicPosRead = readPosForDebug;
            debugMicLastStalledWriteHeadFrameCount = stalledFramesForDebug;
            debugMicLastClipSamples = clipSamplesForDebug;
            return;
        }

        int micPosWrite = Microphone.GetPosition(microphoneDeviceName);
        if (micWriteHeadDoublePoll &&
            microphoneBuffer.samples > 0 &&
            micPosWrite >= 0 &&
            micPosWrite < microphoneBuffer.samples)
        {
            int w2 = Microphone.GetPosition(microphoneDeviceName);
            if (w2 >= 0 && w2 < microphoneBuffer.samples && w2 != micPosWrite)
            {
                micPosWrite = w2;
            }
        }

        if (micPosWrite < 0 || micPosWrite >= microphoneBuffer.samples || microphoneBuffer.samples <= 0)
        {
            ScheduleRecoveryAttempt($"Invalid Microphone.GetPosition() value '{micPosWrite}' for device '{microphoneDeviceName}'.");
            int clipSamplesForDebug = microphoneBuffer.samples;
            int readHeadForDebug = micPosRead;
            int stalledFramesForDebug = stalledWriteHeadFrameCount;
            StopMicrophoneCapture();
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            debugMicLastExitReason = "invalid_mic_position";
            debugMicLastUnreadComputed = -1;
            debugMicLastLatestRawSampleCount = 0;
            debugMicLastMicPosWrite = micPosWrite;
            debugMicLastMicPosRead = readHeadForDebug;
            debugMicLastStalledWriteHeadFrameCount = stalledFramesForDebug;
            debugMicLastClipSamples = clipSamplesForDebug;
            return;
        }

        if (lastMicWritePosition == micPosWrite)
        {
            stalledWriteHeadFrameCount++;
            stalledWriteHeadStallSeconds += Time.unscaledDeltaTime;
        }
        else
        {
            stalledWriteHeadFrameCount = 0;
            stalledWriteHeadStallSeconds = 0f;
        }
        lastMicWritePosition = micPosWrite;

        if (stalledWriteHeadStallSeconds >= Mathf.Max(0.1f, stalledWriteHeadTimeoutSeconds))
        {
            ScheduleRecoveryAttempt($"Detected stalled microphone write-head for device '{microphoneDeviceName}'.");
            int clipSamplesForDebug = microphoneBuffer.samples;
            int writePosForDebug = micPosWrite;
            int readHeadForDebug = micPosRead;
            int unreadForDebug = (clipSamplesForDebug + writePosForDebug - readHeadForDebug) % clipSamplesForDebug;
            int stalledFramesForDebug = stalledWriteHeadFrameCount;
            StopMicrophoneCapture();
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            debugMicLastExitReason = "stalled_capture_stopped";
            debugMicLastUnreadComputed = unreadForDebug;
            debugMicLastLatestRawSampleCount = 0;
            debugMicLastMicPosWrite = writePosForDebug;
            debugMicLastMicPosRead = readHeadForDebug;
            debugMicLastStalledWriteHeadFrameCount = stalledFramesForDebug;
            debugMicLastClipSamples = clipSamplesForDebug;
            return;
        }

        int frameCount = (microphoneBuffer.samples + micPosWrite - micPosRead) % microphoneBuffer.samples;
        if (frameCount <= 0)
        {
            latestRawSampleCount = 0;
            latestNormalizedSampleCount = 0;
            debugMicLastExitReason = "unread_zero";
            debugMicLastUnreadComputed = frameCount;
            debugMicLastLatestRawSampleCount = 0;
            debugMicLastMicPosWrite = micPosWrite;
            debugMicLastMicPosRead = micPosRead;
            debugMicLastStalledWriteHeadFrameCount = stalledWriteHeadFrameCount;
            debugMicLastClipSamples = microphoneBuffer.samples;
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

        debugMicLastExitReason = "copied_samples";
        debugMicLastUnreadComputed = frameCount;
        debugMicLastLatestRawSampleCount = latestRawSampleCount;
        debugMicLastMicPosWrite = micPosWrite;
        debugMicLastMicPosRead = micPosRead;
        debugMicLastStalledWriteHeadFrameCount = stalledWriteHeadFrameCount;
        debugMicLastClipSamples = microphoneBuffer.samples;
    }

    // runs on: main thread (called from the SetNormalization* setters and `OnEnable`-equivalent paths).
    // Fires the public NormalizationStateChanged event; subscribers run synchronously on main thread.
    private void OnNormalizationConfigChanged()
    {
        // Force recompute on next read so consumers get fresh settings immediately.
        lastFrameUpdated = -1;
        NormalizationStateChanged?.Invoke(GetNormalizationState());
    }

    // runs on: main thread (called from UpdateMicReadFrame). Briefly takes rawBufferLock with blocking
    // `lock` — fine on main thread; the audio-thread reader (4-arg ReadRawSamples) uses TryEnter(0)
    // and skips on miss.
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
                rawWriteTotalSamples++;
            }
        }
    }

    // runs on: main thread (called from UpdateMicReadFrame). Same lock contract as WriteRawFrameToRingBuffer
    // but on normalizedBufferLock.
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
                normalizedWriteTotalSamples++;
            }
        }
    }

    // runs on: main thread ONLY (calls microphoneBuffer.GetData — V3 firm rule forbids it on the audio thread).
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

    // runs on: main thread ONLY (Microphone.devices — V3 firm rule).
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

    // runs on: main thread ONLY (Microphone.Start — V3 firm rule). Allocates ring buffers + chunk
    // buffers; bumps `captureEpoch` to drive TryRebootstrapAudioThreadCaptureIfMicRecovered on the
    // next MicIngestMainThreadTick.
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
        lastMicWritePosition = -1;
        stalledWriteHeadFrameCount = 0;
        stalledWriteHeadStallSeconds = 0f;
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

    // runs on: main thread ONLY (Microphone.devices — V3 firm rule).
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

    // runs on: main thread (reads Time.unscaledTime + Debug.LogWarning).
    private void ScheduleRecoveryAttempt(string reason)
    {
        nextRecoveryAttemptTime = Time.unscaledTime + Mathf.Max(0.1f, recoveryRetryIntervalSeconds);
        if (!recoveryWarningLogged)
        {
            recoveryWarningLogged = true;
            Debug.LogWarning($"Mic ingest: {reason} Scheduling recovery retry.");
        }
    }

    // runs on: main thread (called from MicIngestMainThreadTick). Reads `_dbMicrophone` (volatile float
    // written from the audio thread) — single-read pattern is fine; the value is a smoothed dB whose
    // single-frame variance is below the gain-riding control's resolution.
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
        bool canRaiseGain = MicNormalizationStagePolicy.AllowsGainRidingRaise(
            normalizationGainRidingRaiseFrozen,
            toneActiveConfident,
            raiseWindowOpen,
            raiseNoiseFloorClear);
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

    // runs on: main thread (called from MicIngestMainThreadTick). Reads `_dbMicrophone` (audio-thread
    // writer / main-thread reader, volatile float).
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
        gainRidingGateRaiseFrozen = normalizationGainRidingRaiseFrozen;

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

    // runs on: main thread ONLY (Microphone.End — V3 firm rule). Note: does NOT take rawBufferLock /
    // normalizedBufferLock; the audio-thread reader's null-check on `rawRingBuffer` (still set to a
    // valid array here) plus its TryEnter(0) miss path keep it safe across this teardown.
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
        stalledWriteHeadStallSeconds = 0f;
    }

    // runs on: main thread (Unity lifecycle). Stops both capture paths in the right order — audio
    // thread first (so OnAudioFilterRead stops trying to read the soon-to-be-torn-down ring), then
    // microphone.
    private void OnDisable()
    {
        StopAudioThreadCapture();
        StopMicrophoneCapture();
    }
}
