using UnityEngine;

/// <summary>
/// Copies mic-ingest debug (from ImitoneVoiceIntepreter) + Imitone raw-path debug + tone/imitone gate flags
/// (+ optional DirectVoiceMonitoring transport totals)
/// into one Inspector block after upstream Update() (LateUpdate).
/// Mic-ingest snapshot type is <see cref="ImitoneVoiceIntepreter.MicIngestDebugSnapshot"/>; values are copied from <see cref="ImitoneVoiceIntepreter.GetMicIngestDebugSnapshot"/>.
/// </summary>
public class MicVoiceIngestDebugAggregate : MonoBehaviour
{
    [Header("FAIL OBSERVATION (glance here first)")]
    [Tooltip("True if any subsidiary FAIL_* flag is true this frame (pure OR).")]
    [SerializeField] private bool FAILURE;

    [Header("FAIL OBSERVATION — Phase 2 (audio thread)")]
    [Tooltip("True when the audio callback counter has not advanced for failAudioCallbackFrozenSeconds (after grace).")]
    [SerializeField] private bool FAIL_AUDIO_CALLBACK_FROZEN;
    [Tooltip("True when rolling callback rate stays below the low fraction of expected for 1+ second.")]
    [SerializeField] private bool FAIL_AUDIO_CALLBACK_RATE_LOW;
    [Tooltip("True when max inter-callback gap in the last second exceeds the high multiplier of nominal.")]
    [SerializeField] private bool FAIL_AUDIO_CALLBACK_GAP_HIGH;
    [Tooltip("True when lock-miss count in the last 1s window exceeds the per-second threshold.")]
    [SerializeField] private bool FAIL_AUDIO_LOCK_CONTENTION;
    [Tooltip("Sticky: latched when GC-alloc-suspect counter increases; clear with clearFailObservationStickyFlags.")]
    [SerializeField] private bool FAIL_AUDIO_GC_ALLOC_DETECTED;

    [Header("FAIL OBSERVATION — Phase 1 (ingest)")]
    [Tooltip("True when the ingest exit reason stayed unread_zero for too many consecutive frames or seconds (see thresholds).")]
    [SerializeField] private bool FAIL_UNREAD_ZERO_SUSTAINED;
    [Tooltip("True when aggMicRawRingWriteTotalSamples has not increased for failIngestRingStalledFrameThreshold consecutive LateUpdate calls.")]
    [SerializeField] private bool FAIL_INGEST_RING_STALLED;
    [Tooltip("True when raw voice data was not consumed for failInterpreterNotConsumingFrameThreshold consecutive frames while mic is ready and startup grace has elapsed.")]
    [SerializeField] private bool FAIL_INTERPRETER_NOT_CONSUMING;
    [Tooltip("Sticky: latched when aggMicGentleUnreadZeroRecoveryTotal increases above the baseline from last clear; clear by ticking clearFailObservationStickyFlags below.")]
    [SerializeField] private bool FAIL_GENTLE_RECOVERY_FIRED;
    [Tooltip("True when aggMonStarvationEvents increased within the last failMonitoringStarvationWindowSeconds wall-clock seconds.")]
    [SerializeField] private bool FAIL_MONITORING_STARVATION_GROWING;
    [Tooltip("True after startup grace when mic ref is null or mic reports not ready.")]
    [SerializeField] private bool FAIL_MIC_NOT_READY;

    [Header("FAIL OBSERVATION — thresholds")]
    [SerializeField] private int failUnreadZeroSustainedFrameThreshold = 30;
    [SerializeField] private float failUnreadZeroSustainedSecondsThreshold = 0.5f;
    [SerializeField] private int failIngestRingStalledFrameThreshold = 30;
    [SerializeField] private int failInterpreterNotConsumingFrameThreshold = 30;
    [SerializeField] private float failMonitoringStarvationWindowSeconds = 2f;
    [SerializeField] private float failMicNotReadyGracePeriodSeconds = 2f;
    [SerializeField] private float failAudioCallbackStartupGraceSeconds = 2f;
    [SerializeField] private float failAudioCallbackFrozenSeconds = 0.2f;
    [SerializeField] private float failAudioCallbackRateLowFraction = 0.75f;
    [SerializeField] private float failAudioCallbackGapHighMultiplier = 2f;
    [SerializeField] private long failAudioLockMissPerSecondThreshold = 50;

    [Header("FAIL OBSERVATION — actions")]
    [Tooltip("Tick once in Play mode to clear sticky gentle-recovery latch and reset ring-stall baseline; unticks automatically.")]
    [SerializeField] private bool clearFailObservationStickyFlags;

    [Header("References (auto-filled from this GameObject if empty)")]
    [Tooltip("Single source for both tone/imitone state and mic-ingest snapshots. Snapshots are routed via this reference during 0.7b; underlying state migrates fully into the interpreter in 0.7c.")]
    [SerializeField] private ImitoneVoiceIntepreter interpreter;
    [Tooltip("Optional: cumulative underflow/overflow/starvation from monitoring ring pull.")]
    [SerializeField] private DirectVoiceMonitoring voiceMonitoring;

    [Header("Audio thread health (Step 1 — parallel path)")]
    [SerializeField] private long aggAudioCallbackTotal;
    [SerializeField] private long aggAudioCallbackSamplesProcessedTotal;
    [SerializeField] private int aggAudioCallbackLastSamplesPerCallback;
    [SerializeField] private float aggAudioCallbackHzRolling;
    [SerializeField] private float aggAudioCallbackMaxGapMsLastSecond;
    [SerializeField] private long aggAudioCallbackLockMissTotal;
    [SerializeField] private long aggAudioCallbackGCAllocSuspectTotal;
    [SerializeField] private long aggAudioRingWriteTotalSamples;
    [SerializeField] private int aggAudioRingWriteLastClipReadStart;
    [SerializeField] private int aggAudioRingWriteLastClipReadCount;
    [SerializeField] private int aggMicClipChannels;
    [SerializeField] private int aggMixerChannels;
    [SerializeField] private int aggAudioConfigOutputSampleRate;
    [SerializeField] private int aggAudioConfigDspBufferSize;

    [Header("Step 2 stress test (temporary — remove after verification)")]
    [SerializeField] private long aggStressAudioThreadInputAudioCallTotal;
    [SerializeField] private long aggStressAudioThreadInputAudioFailureTotal;
    [SerializeField] private long aggStressMainThreadGetStateCallTotal;
    [SerializeField] private long aggStressMainThreadGetStateFailureTotal;

    [Header("Tone / imitone gate (ImitoneVoiceIntepreter — public runtime flags)")]
    [SerializeField] private bool aggImitoneActive;
    [SerializeField] private bool aggImitoneActiveRaw;
    [SerializeField] private bool aggToneActive;
    [SerializeField] private bool aggToneActiveRaw;
    [SerializeField] private bool aggToneActiveConfident;
    [SerializeField] private bool aggToneActiveVeryConfident;
    [SerializeField] private bool aggToneActiveVeryConfidentRaw;
    [SerializeField] private bool aggToneActiveBiasTrue;
    [SerializeField] private bool aggToneActiveFrame;
    [SerializeField] private bool aggToneActiveConfidentFrame;
    [SerializeField] private bool aggToneActiveBiasTrueFrame;
    [SerializeField] private int aggToneActiveCounter;
    [SerializeField] private int aggToneActiveConfidentCounter;

    [Header("Mic ingest (ImitoneVoiceIntepreter snapshot — same frame as interpreter section below)")]
    [SerializeField] private string aggMicExitReason = "";
    [SerializeField] private int aggMicUnreadComputed = -1;
    [SerializeField] private int aggMicLatestRawSampleCount = -1;
    [SerializeField] private int aggMicPosWrite = -1;
    [SerializeField] private int aggMicPosRead = -1;
    [SerializeField] private int aggMicStalledWriteHeadFrames;
    [SerializeField] private int aggMicClipSamples;
    [SerializeField] private int aggMicUnityFrame;
    [SerializeField] private int aggMicGentleUnreadZeroConsecutiveFrames;
    [SerializeField] private int aggMicGentleUnreadZeroRecoveryTotal;
    [SerializeField] private bool aggMicGentleRecoveryEnabled;
    [SerializeField] private long aggMicRawRingWriteTotalSamples;
    [SerializeField] private long aggMicNormRingWriteTotalSamples;

    [Header("Interpreter raw path (ImitoneVoiceIntepreter)")]
    [SerializeField] private bool aggRawConsumedThisFrame;
    [SerializeField] private bool aggInterpMicRefNull;
    [SerializeField] private bool aggInterpMicReady;
    [SerializeField] private bool aggInterpTryCopyTrue;
    [SerializeField] private int aggInterpTryCopySampleCount = -1;
    [SerializeField] private float aggInterpMicDbUnclamped = -999f;
    [SerializeField] private float aggInterpImitoneDbUnclamped = -999f;

    [Header("Monitoring transport (DirectVoiceMonitoring — cumulative)")]
    [SerializeField] private bool aggMonitoringAssigned;
    [SerializeField] private int aggMonUnderflowEvents;
    [SerializeField] private int aggMonUnderflowSamples;
    [SerializeField] private int aggMonOverflowEvents;
    [SerializeField] private int aggMonOverflowSamples;
    [SerializeField] private int aggMonStarvationEvents;

    private const string UnreadZeroExitReason = "unread_zero";
    private const string UnreadZeroGentleRestartExitReason = "unread_zero_gentle_restart";

    private int _consecutiveUnreadZeroFrames;
    private float _unreadZeroSegmentStartRealtime = -1f;

    private bool _rawRingStallPrevInitialized;
    private long _prevAggMicRawRingWriteTotalSamples;
    private int _consecutiveIngestRingStallFrames;

    private int _consecutiveInterpreterNotConsumingFrames;

    private int _gentleRecoveryBaselineAtClear;
    private bool _gentleRecoveryStickyLatched;

    private int _prevAggMonStarvationEvents = -1;
    private float _lastStarvationIncreaseRealtime = -1f;

    private long _prevAggAudioCallbackTotalForFrozen = -1;
    private float _lastAudioCallbackTotalAdvanceRealtime;
    private float _audioRateLowSustainedTimer;
    private float _audioLockMissWindowTimer;
    private long _audioLockMissAtWindowStart;
    private bool _audioLockBaselineInitialized;
    private bool _audioGCBaselineInitialized;
    private bool _gcAllocStickyLatched;
    private long _gcSuspectBaselineAtClear;

    private void Awake()
    {
        _lastAudioCallbackTotalAdvanceRealtime = Time.realtimeSinceStartup;
        _gcSuspectBaselineAtClear = 0;
        _audioGCBaselineInitialized = false;
        _audioLockBaselineInitialized = false;
        if (interpreter == null)
        {
            interpreter = GetComponent<ImitoneVoiceIntepreter>();
        }

        if (voiceMonitoring == null)
        {
            voiceMonitoring = GetComponent<DirectVoiceMonitoring>();
            if (voiceMonitoring == null)
            {
                voiceMonitoring = GetComponentInChildren<DirectVoiceMonitoring>(true);
            }
        }
    }

    private void ApplyClearFailObservationStickyFlags()
    {
        clearFailObservationStickyFlags = false;
        _gentleRecoveryBaselineAtClear = aggMicGentleUnreadZeroRecoveryTotal;
        _gentleRecoveryStickyLatched = false;
        _rawRingStallPrevInitialized = false;
        _consecutiveIngestRingStallFrames = 0;
        _gcSuspectBaselineAtClear = aggAudioCallbackGCAllocSuspectTotal;
        _gcAllocStickyLatched = false;
        _audioGCBaselineInitialized = true;
        _audioLockBaselineInitialized = false;
        _audioLockMissWindowTimer = 0f;
    }

    private void LateUpdate()
    {
        if (interpreter != null)
        {
            ImitoneVoiceIntepreter.MicIngestDebugSnapshot m = interpreter.GetMicIngestDebugSnapshot();
            // Facade null-path can still yield default(snapshot); normalize exit reason for Inspector string compares.
            aggMicExitReason = m.lastExitReason;
            aggMicUnreadComputed = m.lastUnreadComputed;
            aggMicLatestRawSampleCount = m.lastLatestRawSampleCount;
            aggMicPosWrite = m.lastMicPosWrite;
            aggMicPosRead = m.lastMicPosRead;
            aggMicStalledWriteHeadFrames = m.lastStalledWriteHeadFrameCount;
            aggMicClipSamples = m.lastClipSamples;
            aggMicUnityFrame = m.lastUnityFrame;
            aggMicGentleUnreadZeroConsecutiveFrames = m.gentleUnreadZeroConsecutiveFrames;
            aggMicGentleUnreadZeroRecoveryTotal = m.gentleUnreadZeroRecoveryTotal;
            aggMicGentleRecoveryEnabled = m.gentleUnreadZeroRecoveryEnabled;
            aggMicRawRingWriteTotalSamples = m.rawRingWriteTotalSamples;
            aggMicNormRingWriteTotalSamples = m.normalizedRingWriteTotalSamples;
        }

        if (interpreter != null)
        {
            ImitoneVoiceIntepreter.RawVoicePathDebugSnapshot v = interpreter.GetRawVoicePathDebugSnapshot();
            aggRawConsumedThisFrame = v.rawVoiceDataConsumedThisFrame;
            aggInterpMicRefNull = v.interpreterMicRefNull;
            aggInterpMicReady = v.interpreterMicReady;
            aggInterpTryCopyTrue = v.interpreterTryCopyReturnedTrue;
            aggInterpTryCopySampleCount = v.interpreterTryCopyOutSampleCount;
            aggInterpMicDbUnclamped = v.telemetryMicDbUnclamped;
            aggInterpImitoneDbUnclamped = v.telemetryImitoneDbUnclamped;

            aggImitoneActive = interpreter.imitoneActive;
            aggImitoneActiveRaw = interpreter.imitoneActiveRaw;
            aggToneActive = interpreter.toneActive;
            aggToneActiveRaw = interpreter.toneActiveRaw;
            aggToneActiveConfident = interpreter.toneActiveConfident;
            aggToneActiveVeryConfident = interpreter.toneActiveVeryConfident;
            aggToneActiveVeryConfidentRaw = interpreter.toneActiveVeryConfidentRaw;
            aggToneActiveBiasTrue = interpreter.toneActiveBiasTrue;
            aggToneActiveFrame = interpreter.toneActiveFrame;
            aggToneActiveConfidentFrame = interpreter.toneActiveConfidentFrame;
            aggToneActiveBiasTrueFrame = interpreter.toneActiveBiasTrueFrame;
            aggToneActiveCounter = interpreter.toneActiveCounter;
            aggToneActiveConfidentCounter = interpreter.toneActiveConfidentCounter;

            ImitoneVoiceIntepreter.AudioThreadHealthSnapshot a = interpreter.GetAudioThreadHealthSnapshot();
            aggAudioCallbackTotal = a.audioCallbackTotal;
            aggAudioCallbackSamplesProcessedTotal = a.audioCallbackSamplesProcessedTotal;
            aggAudioCallbackLastSamplesPerCallback = a.audioCallbackLastSamplesPerCallback;
            aggAudioCallbackHzRolling = a.audioCallbackHzRolling;
            aggAudioCallbackMaxGapMsLastSecond = a.audioCallbackMaxGapMsLastSecond;
            aggAudioCallbackLockMissTotal = a.audioCallbackLockMissTotal;
            aggAudioCallbackGCAllocSuspectTotal = a.audioCallbackGCAllocSuspectTotal;
            aggAudioRingWriteTotalSamples = a.audioRingWriteTotalSamples;
            aggAudioRingWriteLastClipReadStart = a.audioRingWriteLastClipReadStart;
            aggAudioRingWriteLastClipReadCount = a.audioRingWriteLastClipReadCount;
            aggMicClipChannels = a.aggMicClipChannels;
            aggMixerChannels = a.aggMixerChannels;
            aggAudioConfigOutputSampleRate = a.audioConfigOutputSampleRate;
            aggAudioConfigDspBufferSize = a.audioConfigDspBufferSize;

            aggStressAudioThreadInputAudioCallTotal = interpreter.StressAudioThreadInputAudioCallTotal;
            aggStressAudioThreadInputAudioFailureTotal = interpreter.StressAudioThreadInputAudioFailureTotal;
            aggStressMainThreadGetStateCallTotal = interpreter.StressMainThreadGetStateCallTotal;
            aggStressMainThreadGetStateFailureTotal = interpreter.StressMainThreadGetStateFailureTotal;
        }

        aggMonitoringAssigned = voiceMonitoring != null;
        if (voiceMonitoring != null)
        {
            voiceMonitoring.GetBufferedTransportTotals(
                out aggMonUnderflowEvents,
                out aggMonUnderflowSamples,
                out aggMonOverflowEvents,
                out aggMonOverflowSamples,
                out aggMonStarvationEvents);
        }

        if (clearFailObservationStickyFlags)
        {
            ApplyClearFailObservationStickyFlags();
        }

        bool pastAudioGrace = Time.timeSinceLevelLoad >= failAudioCallbackStartupGraceSeconds;
        float expectedAudioHz = 0f;
        if (aggAudioConfigDspBufferSize > 0 && aggAudioConfigOutputSampleRate > 0)
        {
            expectedAudioHz = aggAudioConfigOutputSampleRate / (float)aggAudioConfigDspBufferSize;
        }

        float nominalGapMs = expectedAudioHz > 1e-3f ? 1000f / expectedAudioHz : 0f;

        if (interpreter != null && pastAudioGrace)
        {
            if (!_audioLockBaselineInitialized)
            {
                _audioLockMissAtWindowStart = aggAudioCallbackLockMissTotal;
                _audioLockMissWindowTimer = 0f;
                _audioLockBaselineInitialized = true;
            }

            // Seed the GC-suspect baseline once the audio thread is past startup grace.
            // Cold-start callbacks can spike >3ms during JIT/warm-up; without this seed the
            // sticky flag would latch on every fresh play.
            if (!_audioGCBaselineInitialized)
            {
                _gcSuspectBaselineAtClear = aggAudioCallbackGCAllocSuspectTotal;
                _audioGCBaselineInitialized = true;
            }

            if (aggAudioCallbackTotal != _prevAggAudioCallbackTotalForFrozen)
            {
                _lastAudioCallbackTotalAdvanceRealtime = Time.realtimeSinceStartup;
                _prevAggAudioCallbackTotalForFrozen = aggAudioCallbackTotal;
            }

            FAIL_AUDIO_CALLBACK_FROZEN =
                Time.realtimeSinceStartup - _lastAudioCallbackTotalAdvanceRealtime >= failAudioCallbackFrozenSeconds;

            if (expectedAudioHz > 1e-3f)
            {
                if (aggAudioCallbackHzRolling < failAudioCallbackRateLowFraction * expectedAudioHz)
                {
                    _audioRateLowSustainedTimer += Time.deltaTime;
                }
                else
                {
                    _audioRateLowSustainedTimer = 0f;
                }
            }
            else
            {
                _audioRateLowSustainedTimer = 0f;
            }

            FAIL_AUDIO_CALLBACK_RATE_LOW = _audioRateLowSustainedTimer >= 1f
                && aggAudioCallbackTotal > 32;

            FAIL_AUDIO_CALLBACK_GAP_HIGH =
                aggAudioCallbackTotal > 32
                && nominalGapMs > 1e-3f
                && aggAudioCallbackMaxGapMsLastSecond > failAudioCallbackGapHighMultiplier * nominalGapMs;

            _audioLockMissWindowTimer += Time.deltaTime;
            if (_audioLockMissWindowTimer >= 1f)
            {
                long d = aggAudioCallbackLockMissTotal - _audioLockMissAtWindowStart;
                FAIL_AUDIO_LOCK_CONTENTION = d > failAudioLockMissPerSecondThreshold;
                _audioLockMissAtWindowStart = aggAudioCallbackLockMissTotal;
                _audioLockMissWindowTimer = 0f;
            }

            if (interpreter.Step2StressTestSessionActive)
            {
                FAIL_AUDIO_GC_ALLOC_DETECTED = false;
            }
            else
            {
                if (aggAudioCallbackGCAllocSuspectTotal > _gcSuspectBaselineAtClear)
                {
                    _gcAllocStickyLatched = true;
                }

                FAIL_AUDIO_GC_ALLOC_DETECTED = _gcAllocStickyLatched;
            }
        }
        else
        {
            FAIL_AUDIO_CALLBACK_FROZEN = false;
            FAIL_AUDIO_CALLBACK_RATE_LOW = false;
            FAIL_AUDIO_CALLBACK_GAP_HIGH = false;
            FAIL_AUDIO_LOCK_CONTENTION = false;
            FAIL_AUDIO_GC_ALLOC_DETECTED = false;
            _audioRateLowSustainedTimer = 0f;
        }

        // --- FAIL OBSERVATION Phase 1 (after aggregate copies and optional clear) ---
        // R1: gentle restart is itself a symptom of being stuck in unread_zero,
        // so treat unread_zero_gentle_restart as "still in trouble" — segment only
        // resets when ingest reports anything else (e.g. copied_samples).
        bool inUnreadZeroSegment =
            aggMicExitReason == UnreadZeroExitReason
            || aggMicExitReason == UnreadZeroGentleRestartExitReason;
        if (inUnreadZeroSegment)
        {
            _consecutiveUnreadZeroFrames++;
            if (_unreadZeroSegmentStartRealtime < 0f)
            {
                _unreadZeroSegmentStartRealtime = Time.realtimeSinceStartup;
            }
        }
        else
        {
            _consecutiveUnreadZeroFrames = 0;
            _unreadZeroSegmentStartRealtime = -1f;
        }

        float unreadZeroSegmentSeconds = 0f;
        if (_unreadZeroSegmentStartRealtime >= 0f)
        {
            unreadZeroSegmentSeconds = Time.realtimeSinceStartup - _unreadZeroSegmentStartRealtime;
        }

        FAIL_UNREAD_ZERO_SUSTAINED =
            _consecutiveUnreadZeroFrames >= failUnreadZeroSustainedFrameThreshold
            || (_unreadZeroSegmentStartRealtime >= 0f && unreadZeroSegmentSeconds >= failUnreadZeroSustainedSecondsThreshold);

        if (interpreter != null)
        {
            if (!_rawRingStallPrevInitialized)
            {
                _prevAggMicRawRingWriteTotalSamples = aggMicRawRingWriteTotalSamples;
                _rawRingStallPrevInitialized = true;
                _consecutiveIngestRingStallFrames = 0;
            }
            else if (aggMicRawRingWriteTotalSamples == _prevAggMicRawRingWriteTotalSamples)
            {
                _consecutiveIngestRingStallFrames++;
            }
            else
            {
                _prevAggMicRawRingWriteTotalSamples = aggMicRawRingWriteTotalSamples;
                _consecutiveIngestRingStallFrames = 0;
            }

            FAIL_INGEST_RING_STALLED = _consecutiveIngestRingStallFrames >= failIngestRingStalledFrameThreshold;
        }
        else
        {
            FAIL_INGEST_RING_STALLED = false;
        }

        // M2: Time.timeSinceLevelLoad resets on scene reload, which is intentional —
        // a fresh scene load gets its own grace period. Single-main-scene flows are unaffected.
        bool pastMicGrace = Time.timeSinceLevelLoad >= failMicNotReadyGracePeriodSeconds;
        if (interpreter != null && pastMicGrace)
        {
            if (aggInterpMicReady)
            {
                if (!aggRawConsumedThisFrame)
                {
                    _consecutiveInterpreterNotConsumingFrames++;
                }
                else
                {
                    _consecutiveInterpreterNotConsumingFrames = 0;
                }
            }
            else
            {
                _consecutiveInterpreterNotConsumingFrames = 0;
            }

            FAIL_INTERPRETER_NOT_CONSUMING =
                _consecutiveInterpreterNotConsumingFrames >= failInterpreterNotConsumingFrameThreshold;
        }
        else
        {
            _consecutiveInterpreterNotConsumingFrames = 0;
            FAIL_INTERPRETER_NOT_CONSUMING = false;
        }

        if (aggMicGentleUnreadZeroRecoveryTotal > _gentleRecoveryBaselineAtClear)
        {
            _gentleRecoveryStickyLatched = true;
        }

        FAIL_GENTLE_RECOVERY_FIRED = _gentleRecoveryStickyLatched;

        if (_prevAggMonStarvationEvents < 0)
        {
            _prevAggMonStarvationEvents = aggMonStarvationEvents;
        }
        else if (aggMonStarvationEvents > _prevAggMonStarvationEvents)
        {
            _lastStarvationIncreaseRealtime = Time.realtimeSinceStartup;
            _prevAggMonStarvationEvents = aggMonStarvationEvents;
        }

        if (_lastStarvationIncreaseRealtime >= 0f)
        {
            FAIL_MONITORING_STARVATION_GROWING =
                Time.realtimeSinceStartup - _lastStarvationIncreaseRealtime <= failMonitoringStarvationWindowSeconds;
        }
        else
        {
            FAIL_MONITORING_STARVATION_GROWING = false;
        }

        if (interpreter != null && pastMicGrace)
        {
            FAIL_MIC_NOT_READY = aggInterpMicRefNull || !aggInterpMicReady;
        }
        else
        {
            FAIL_MIC_NOT_READY = false;
        }

        FAILURE =
            FAIL_AUDIO_CALLBACK_FROZEN
            || FAIL_AUDIO_CALLBACK_RATE_LOW
            || FAIL_AUDIO_CALLBACK_GAP_HIGH
            || FAIL_AUDIO_LOCK_CONTENTION
            || FAIL_AUDIO_GC_ALLOC_DETECTED
            || FAIL_UNREAD_ZERO_SUSTAINED
            || FAIL_INGEST_RING_STALLED
            || FAIL_INTERPRETER_NOT_CONSUMING
            || FAIL_GENTLE_RECOVERY_FIRED
            || FAIL_MONITORING_STARVATION_GROWING
            || FAIL_MIC_NOT_READY;
    }
}
