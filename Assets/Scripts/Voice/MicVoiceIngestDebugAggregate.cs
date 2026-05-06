using UnityEngine;

/// <summary>
/// Copies mic-ingest debug (from ImitoneVoiceIntepreter) + Imitone raw-path debug + tone/imitone gate flags
/// (+ optional DirectVoiceMonitoring transport totals)
/// into one Inspector block after upstream Update() (LateUpdate).
/// Mic-ingest snapshot type is <see cref="ImitoneVoiceIntepreter.MicIngestDebugSnapshot"/>; values are copied from <see cref="ImitoneVoiceIntepreter.GetMicIngestDebugSnapshot"/>.
/// During Step 3a F1 hybrid pivot work, the <b>CURRENT TEST</b> section lists only what the
/// <b>active</b> play test needs — tight enough for one Inspector screengrab. Deeper metrics stay in the
/// headed sections below. See <c>Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md</c> § Inspector / CURRENT TEST protocol.
/// </summary>
public class MicVoiceIngestDebugAggregate : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // CURRENT TEST — values mirrored to the top of the Inspector for whichever
    // diagnostic step is active. Updated each test pass (this section is
    // disposable; do NOT add permanent fields here — they belong in their
    // own headed sections below). Convention documented in
    // Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md (Debugging Process section).
    //
    // PERSISTENT RULE (Step 3a F1 hybrid pivot): CURRENT TEST holds only fields needed for the
    // **active** diagnostic (one screengrab). Do not accumulate retired fields; remove when the test
    // changes. See Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md § Inspector / CURRENT TEST protocol.
    // ----------------------------------------------------------------------

    [Header("CURRENT TEST — Pass 2: telemetry rename + cursor-vs-write-head gap")]
    [Tooltip("Screengrab this block (toning + silent), report two grabs ~60s apart to confirm gap drift < 20ms.\n\nBAR: hybridGapMs in 40–180ms band while toning (absolute value is hardware-dependent — mic ADC vs engine clock skew sets the floor) | DRIFT < 20ms across 60s (this is the actual test) | overflowDrops not climbing | rawRingReadLockMisses near 0 | rollingHz ≈ sampleRate/dspSize | input/callback ratio ~1 | feedPeak >0.05 on voice | pitch/db move | exitReason=copied_samples | failure=false (ignoring known GC false-positive).\n\nFor callback totals, lock-write misses, raw ring totals, config, monitoring — scroll to sections below.")]
    [SerializeField] private string currentTestDescription = "Pass 2: two screengrabs ~60s apart (tone + silent). Verify gap drift + lock-miss counter.";

    [Tooltip("Time.time — report with each grab.")]
    [SerializeField] private float currentTestSessionTimeSeconds;
    [Tooltip("Any FAIL_* below.")]
    [SerializeField] private bool currentTestFailure;

    [Tooltip("Cursor-to-write-head imitone-feed latency (ms): rawWriteTotal minus audio-thread feed read cursor. Stable 40–180ms band while toning; absolute value is hardware-dependent (mic ADC vs engine clock skew). Drift < 20ms / 60s is the real test. Self-limits at 250ms via ReadRawSamples overflow guard.")]
    [SerializeField] private float currentTestHybridFeedGapMs;
    [Tooltip("Rolling OAFR rate (Hz). Expect ~ output sample rate / DSP buffer size (e.g. ~46.9 @ 48k/1024).")]
    [SerializeField] private float currentTestAudioCallbackHzRolling;
    [Tooltip("imitone.InputAudio calls / callbacks (cumulative). ~1.0 after priming.")]
    [SerializeField] private float currentTestImitoneInputToCallbackRatio;
    [Tooltip("ReadRawSamples overflow drops on imitone path. Should NOT climb between two grabs taken ~60s apart.")]
    [SerializeField] private long currentTestAudioFeedOverflowDroppedTotal;
    [Tooltip("Pass 2: rawBufferLock TryEnter(0) misses from audio-thread readers (imitone feed + DirectVoiceMonitoring). Should stay near 0; sustained climb means main-thread writer hold-time is colliding with audio reads.")]
    [SerializeField] private long currentTestRawRingReadLockMissTotal;

    [Tooltip("Mic ingest branch (expect copied_samples when healthy).")]
    [SerializeField] private string currentTestMicExitReason = "";

    [Tooltip("Peak |sample| going to imitone. Voice ~0.05–0.5.")]
    [SerializeField] private float currentTestFeedPeakAbs;
    [SerializeField] private float currentTestDbValue;
    [SerializeField] private float currentTestPitchHz;

    [Tooltip("Sticky GC false positive (imitone CPU time). Separate cleanup.")]
    [SerializeField] private bool currentTestKnownFalsePositive_GcAlloc;

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

    [Header("FAIL OBSERVATION — Phase 3 (imitone feed)")]
    [Tooltip("True when aggImitoneInputAudioCallTotal has not advanced for failImitoneNotFedSeconds while the audio callback IS still advancing (audio thread alive but feed broken). Distinguishes feed-side failures from a frozen audio thread.")]
    [SerializeField] private bool FAIL_IMITONE_NOT_FED;
    [Tooltip("True when aggImitoneInputAudioCallTotal / aggAudioCallbackTotal stays below failImitoneFeedRatioMin over the last 1-second window. Catches partial feed (e.g. priming gate stuck on, exception loop dropping calls).")]
    [SerializeField] private bool FAIL_IMITONE_FEED_RATIO_LOW;
    [Tooltip("True when aggMicRingOverflowSkipTotal increased within the last failRingOverflowWindowSeconds. (3a: counter not yet incremented from any code path — placeholder for Step 5b when the audio-thread ring gets a real consumer.)")]
    [SerializeField] private bool FAIL_RING_OVERFLOW_GROWING;

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
    [SerializeField] private float failImitoneNotFedSeconds = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float failImitoneFeedRatioMin = 0.95f;
    [SerializeField] private float failRingOverflowWindowSeconds = 2f;

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

    [Header("Imitone feed observability (Step 3a)")]
    [Tooltip("Successful imitone.InputAudio calls from OnAudioFilterRead. Should track aggAudioCallbackTotal 1:1 (modulo priming).")]
    [SerializeField] private long aggImitoneInputAudioCallTotal;
    [Tooltip("imitone.GetState() calls from main thread (one per Update where the imitone block runs).")]
    [SerializeField] private long aggImitoneGetStateCallTotal;
    [Tooltip("aggImitoneInputAudioCallTotal / aggAudioCallbackTotal ratio over the last 1-second window. Should sit at ~1.0 in steady state.")]
    [SerializeField] [Range(0f, 1.5f)] private float aggImitoneInputToCallbackRatio;
    [Tooltip("Frames since imitone GetState returned a different (power, pitch_hz) tuple. Stays near 0 in normal operation; climbs only if imitone is hung.")]
    [SerializeField] private int aggMainThreadFramesSinceLastImitoneStateChange;
    [Tooltip("Audio-thread ring writes skipped due to consumer-overrun. (3a: never increments — placeholder for Step 5b when a consumer exists. Distinct from aggAudioCallbackLockMissTotal, which is lock contention.)")]
    [SerializeField] private long aggMicRingOverflowSkipTotal;
    [Tooltip("DEBUG: peak |sample| of the mono buffer the audio thread just handed to imitone.InputAudio. While toning, expect 0.05–0.5. If ~0 while _dbMicrophone moves on voice, the audio thread is being fed silence and imitone is innocent.")]
    [SerializeField] private float aggAudioThreadFeedPeakAbsLastCallback;
    [Tooltip("Step 3a hybrid pivot: logical read cursor total on the imitone raw-ring feed path (samples since session start, monotonic).")]
    [SerializeField] private long aggAudioThreadFeedReadTotalSamples;
    [Tooltip("Step 3a hybrid pivot: cumulative samples dropped by ReadRawSamples overflow guard on imitone feed.")]
    [SerializeField] private long aggAudioFeedOverflowDroppedTotal;
    [Tooltip("Step 3a Pass 2: feed-cursor-to-write-head gap in samples (monotonic write total minus audio-thread feed read total). Target ~40-90 ms equivalent while toning.")]
    [SerializeField] private long aggAudioThreadFeedToWriteHeadGapSamples;
    [Tooltip("Step 3a Pass 2: same gap converted to ms via aggAudioConfigOutputSampleRate. Stable 40-180 ms band; absolute value is mic-ADC-vs-engine clock-skew dependent. Drift < 20 ms across a 60s session is the real test. Self-limits at 250ms via ReadRawSamples overflow guard.")]
    [SerializeField] private float aggAudioThreadFeedToWriteHeadGapMs;
    [Tooltip("Step 3a Pass 2: TryEnter(0) misses on rawBufferLock from audio-thread readers (imitone feed + DirectVoiceMonitoring). Distinct from aggAudioCallbackLockMissTotal (audioRingWriteLock; pass-3-doomed). Should stay near 0.")]
    [SerializeField] private long aggRawRingReadLockMissTotal;

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

    // Step 3a: trackers for Phase 3 FAIL_* triggers.
    private long _prevAggImitoneInputAudioCallTotalForNotFed = -1;
    private float _lastImitoneInputAudioAdvanceRealtime;
    private bool _imitoneFeedRatioWindowInitialized;
    private float _imitoneFeedRatioWindowTimer;
    private long _imitoneFeedRatioWindowStartCallbackTotal;
    private long _imitoneFeedRatioWindowStartInputTotal;
    private long _prevAggMicRingOverflowSkipTotal = -1;
    private float _lastMicRingOverflowIncreaseRealtime = -1f;
    private int _prevAggCaptureEpoch = -1;

    private void Awake()
    {
        _lastAudioCallbackTotalAdvanceRealtime = Time.realtimeSinceStartup;
        _lastImitoneInputAudioAdvanceRealtime = Time.realtimeSinceStartup;
        _gcSuspectBaselineAtClear = 0;
        _audioGCBaselineInitialized = false;
        _audioLockBaselineInitialized = false;
        _imitoneFeedRatioWindowInitialized = false;
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

            // Step 3a: imitone-feed observability — audio-thread side.
            aggImitoneInputAudioCallTotal = a.imitoneInputAudioCallTotal;
            aggMicRingOverflowSkipTotal = a.micRingOverflowSkipTotal;
            aggAudioThreadFeedPeakAbsLastCallback = a.audioCallbackFeedPeakAbsLastCallback;
            aggAudioThreadFeedReadTotalSamples = a.audioThreadFeedReadTotalSamples;
            aggAudioFeedOverflowDroppedTotal = a.audioFeedOverflowDroppedTotal;
            // Step 3a Pass 2: gap (samples + ms) and rawBufferLock TryEnter miss counter.
            aggAudioThreadFeedToWriteHeadGapSamples = a.audioThreadFeedToWriteHeadGapSamples;
            aggAudioThreadFeedToWriteHeadGapMs = aggAudioConfigOutputSampleRate > 0
                ? aggAudioThreadFeedToWriteHeadGapSamples * 1000f / aggAudioConfigOutputSampleRate
                : -1f;
            aggRawRingReadLockMissTotal = a.rawRingReadLockMissTotal;

            // CURRENT TEST — only fields needed for the active pass (one screengrab). See plan doc § Inspector.
            currentTestSessionTimeSeconds = Time.time;
            currentTestAudioCallbackHzRolling = aggAudioCallbackHzRolling;
            currentTestAudioFeedOverflowDroppedTotal = aggAudioFeedOverflowDroppedTotal;
            currentTestRawRingReadLockMissTotal = aggRawRingReadLockMissTotal;
            currentTestMicExitReason = aggMicExitReason ?? "";
            currentTestFeedPeakAbs = aggAudioThreadFeedPeakAbsLastCallback;
            currentTestDbValue = interpreter._dbValue;
            currentTestPitchHz = interpreter.pitch_hz;

            // Pass 2: source from snapshot's coherent (writeTotal, readTotal) pair instead of recomputing
            // from cross-snapshot fields (rawRingWrite came from a different GetMicIngestDebugSnapshot read).
            currentTestHybridFeedGapMs = aggAudioThreadFeedToWriteHeadGapMs;

            // Step 3a: imitone-feed observability — main-thread side.
            aggImitoneGetStateCallTotal = interpreter.ImitoneGetStateCallTotal;
            aggMainThreadFramesSinceLastImitoneStateChange = interpreter.MainThreadFramesSinceLastImitoneStateChange;

            // Cumulative ratio (snapshot value; the rolling-window check below is what FAIL_IMITONE_FEED_RATIO_LOW uses).
            aggImitoneInputToCallbackRatio = aggAudioCallbackTotal > 0
                ? (float)aggImitoneInputAudioCallTotal / aggAudioCallbackTotal
                : 0f;
            currentTestImitoneInputToCallbackRatio = aggImitoneInputToCallbackRatio;

            // Step 3a: detect mic recovery (captureEpoch tick) and absorb the priming-window transient on the
            // imitone-feed side ONLY. Without this, FAIL_IMITONE_NOT_FED briefly fires after every recovery
            // because feedStaleSeconds includes the gap from "last feed before mic died" to "first feed after
            // priming." We deliberately do NOT reset the audio-callback advance tracker — if callbacks fail
            // to resume after recovery, FAIL_AUDIO_CALLBACK_FROZEN must still fire as the primary signal.
            int currentCaptureEpoch = interpreter.MicCaptureEpoch;
            if (_prevAggCaptureEpoch >= 0 && _prevAggCaptureEpoch != currentCaptureEpoch)
            {
                _lastImitoneInputAudioAdvanceRealtime = Time.realtimeSinceStartup;
                _imitoneFeedRatioWindowInitialized = false;
            }
            _prevAggCaptureEpoch = currentCaptureEpoch;
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

            if (aggAudioCallbackGCAllocSuspectTotal > _gcSuspectBaselineAtClear)
            {
                _gcAllocStickyLatched = true;
            }

            FAIL_AUDIO_GC_ALLOC_DETECTED = _gcAllocStickyLatched;
            currentTestKnownFalsePositive_GcAlloc = FAIL_AUDIO_GC_ALLOC_DETECTED;

            // --- Step 3a: Phase 3 (imitone feed) FAIL_* triggers ---

            // FAIL_IMITONE_NOT_FED — feed counter not advancing while audio thread IS still firing callbacks.
            // (If callbacks are also frozen, FAIL_AUDIO_CALLBACK_FROZEN reports it; this flag is specifically
            // "the audio thread is alive but the imitone feed died" — e.g. priming gate stuck on, persistent
            // feed-side exception bypassing the increment, or the feed code path was somehow short-circuited.)
            if (_prevAggImitoneInputAudioCallTotalForNotFed < 0
                || aggImitoneInputAudioCallTotal != _prevAggImitoneInputAudioCallTotalForNotFed)
            {
                _lastImitoneInputAudioAdvanceRealtime = Time.realtimeSinceStartup;
                _prevAggImitoneInputAudioCallTotalForNotFed = aggImitoneInputAudioCallTotal;
            }

            float feedStaleSeconds = Time.realtimeSinceStartup - _lastImitoneInputAudioAdvanceRealtime;
            float callbackStaleSeconds = Time.realtimeSinceStartup - _lastAudioCallbackTotalAdvanceRealtime;
            FAIL_IMITONE_NOT_FED =
                aggAudioCallbackTotal > 32
                && feedStaleSeconds >= failImitoneNotFedSeconds
                && callbackStaleSeconds < failImitoneNotFedSeconds;

            // FAIL_IMITONE_FEED_RATIO_LOW — rolling 1-second window. Catches partial feed (some callbacks miss
            // the increment) that wouldn't be caught by FAIL_IMITONE_NOT_FED's "not advancing at all" trigger.
            if (!_imitoneFeedRatioWindowInitialized)
            {
                _imitoneFeedRatioWindowStartCallbackTotal = aggAudioCallbackTotal;
                _imitoneFeedRatioWindowStartInputTotal = aggImitoneInputAudioCallTotal;
                _imitoneFeedRatioWindowTimer = 0f;
                _imitoneFeedRatioWindowInitialized = true;
                FAIL_IMITONE_FEED_RATIO_LOW = false;
            }
            else
            {
                _imitoneFeedRatioWindowTimer += Time.deltaTime;
                if (_imitoneFeedRatioWindowTimer >= 1f)
                {
                    long callbackDelta = aggAudioCallbackTotal - _imitoneFeedRatioWindowStartCallbackTotal;
                    long inputDelta = aggImitoneInputAudioCallTotal - _imitoneFeedRatioWindowStartInputTotal;
                    if (callbackDelta > 0)
                    {
                        float windowRatio = (float)inputDelta / callbackDelta;
                        FAIL_IMITONE_FEED_RATIO_LOW = windowRatio < failImitoneFeedRatioMin;
                    }
                    else
                    {
                        // No callbacks in the window — falls under FAIL_AUDIO_CALLBACK_FROZEN, not a feed-ratio failure.
                        FAIL_IMITONE_FEED_RATIO_LOW = false;
                    }
                    _imitoneFeedRatioWindowStartCallbackTotal = aggAudioCallbackTotal;
                    _imitoneFeedRatioWindowStartInputTotal = aggImitoneInputAudioCallTotal;
                    _imitoneFeedRatioWindowTimer = 0f;
                }
                // Between window rolls, FAIL_IMITONE_FEED_RATIO_LOW retains its last evaluated value.
            }

            // FAIL_RING_OVERFLOW_GROWING — 3a: counter never increments (placeholder for Step 5b). Wiring lands
            // here so the trigger logic exists when Step 5b's consumer-position tracking starts feeding it.
            if (_prevAggMicRingOverflowSkipTotal < 0)
            {
                _prevAggMicRingOverflowSkipTotal = aggMicRingOverflowSkipTotal;
            }
            else if (aggMicRingOverflowSkipTotal > _prevAggMicRingOverflowSkipTotal)
            {
                _lastMicRingOverflowIncreaseRealtime = Time.realtimeSinceStartup;
                _prevAggMicRingOverflowSkipTotal = aggMicRingOverflowSkipTotal;
            }

            FAIL_RING_OVERFLOW_GROWING = _lastMicRingOverflowIncreaseRealtime >= 0f
                && Time.realtimeSinceStartup - _lastMicRingOverflowIncreaseRealtime <= failRingOverflowWindowSeconds;
        }
        else
        {
            FAIL_AUDIO_CALLBACK_FROZEN = false;
            FAIL_AUDIO_CALLBACK_RATE_LOW = false;
            FAIL_AUDIO_CALLBACK_GAP_HIGH = false;
            FAIL_AUDIO_LOCK_CONTENTION = false;
            FAIL_AUDIO_GC_ALLOC_DETECTED = false;
            FAIL_IMITONE_NOT_FED = false;
            FAIL_IMITONE_FEED_RATIO_LOW = false;
            FAIL_RING_OVERFLOW_GROWING = false;
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
            || FAIL_IMITONE_NOT_FED
            || FAIL_IMITONE_FEED_RATIO_LOW
            || FAIL_RING_OVERFLOW_GROWING
            || FAIL_UNREAD_ZERO_SUSTAINED
            || FAIL_INGEST_RING_STALLED
            || FAIL_INTERPRETER_NOT_CONSUMING
            || FAIL_GENTLE_RECOVERY_FIRED
            || FAIL_MONITORING_STARVATION_GROWING
            || FAIL_MIC_NOT_READY;

        currentTestFailure = FAILURE;
    }
}
