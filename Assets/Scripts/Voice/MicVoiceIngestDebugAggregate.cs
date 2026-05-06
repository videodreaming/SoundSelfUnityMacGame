using UnityEngine;

/// <summary>
/// Copies mic-ingest debug (from ImitoneVoiceIntepreter) + Imitone raw-path debug + tone/imitone gate flags
/// (+ optional DirectVoiceMonitoring transport totals) into one Inspector block after upstream Update()
/// (LateUpdate). Step 3b adds a cross-thread atomicity / tear-detection block.
/// Mic-ingest snapshot type is <see cref="ImitoneVoiceIntepreter.MicIngestDebugSnapshot"/>; values are copied
/// from <see cref="ImitoneVoiceIntepreter.GetMicIngestDebugSnapshot"/>.
/// The <b>CURRENT TEST</b> section at the top contains <i>only</i> what the active play test needs — tight
/// enough for one Inspector screengrab. The contract: this block is rewritten <i>before</i> each testing
/// round (header text, currentTestDescription, field set, LateUpdate mirror copies). Old fields from prior
/// rounds get removed in the same edit pass — no accumulation. See
/// <c>Docs/MIC_VOICE_INGEST_FIX_PLAN.md</c> § 9 (Active-bug debugging convention) for the full rules.
/// </summary>
public class MicVoiceIngestDebugAggregate : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // CURRENT TEST — values mirrored to the top of the Inspector for whichever
    // diagnostic step is active. Per § 9 of Docs/MIC_VOICE_INGEST_FIX_PLAN.md:
    //   * ONLY contains values needed for the current test (no accumulation).
    //   * Rewritten BEFORE each testing round, in the same edit pass as the
    //     code that round tests. Header, currentTestDescription, field set,
    //     and LateUpdate mirror copies all change together.
    // Permanent fields belong in their own headed sections below — never here.
    // ----------------------------------------------------------------------

    [Header("CURRENT TEST — Step 3b: filter + dB on audio thread, tear detector")]
    [Tooltip("Step 3b test bar — screengrab while toning + silent + during click testing protocol.\n\nWHAT 3b CHANGES: HPF / LPF + _dbMicrophone moved from main thread (GetRawVoiceData) to audio thread (OnAudioFilterRead). _dbMicrophone is now volatile float (audio writer, main reader). Tear detector watches for NaN / ±Infinity / out-of-dB-range values (would indicate the volatile guarantee is insufficient and we'd escalate to Interlocked).\n\nBAR — all of these must hold throughout the test:\n  • dbMicTearTotal stays at 0 (sticky; any non-zero is a tear, escalate to Interlocked).\n  • dbMicSnapshot moves with voice (expect roughly -50 quiet, > -30 toning) — confirms the audio-thread dB writer is alive and crossing back to main without corruption.\n  • dbValue + pitchHz stay alive on voice (no regression vs F1 closeout).\n  • feedPeakAbs ~0.01–0.1 on voice (post-filter; the F1-era 0.05–0.5 number was on unfiltered samples).\n  • rollingHz ≈ outputSampleRate/dspBufferSize (audio thread alive).\n  • input/callback ratio ~1 (steady state, post-priming).\n  • overflowDrops not climbing across the session.\n  • rawRingReadLockMisses near 0 (the new filter+dB work doesn't cost lock contention).\n  • failure = false. (Post-3b play-test follow-up retired FAIL_AUDIO_GC_ALLOC_DETECTED — the duration-heuristic flag was structurally redundant with FAIL_AUDIO_CALLBACK_RATE_LOW + FAIL_AUDIO_CALLBACK_FROZEN, and no longer false-positive-trips on imitone CPU time. If failure is true, it's a real signal now.)\n\nCLICK TESTING PROTOCOL: run all 5 scenarios from Docs/MIC_VOICE_INGEST_FIX_PLAN.md § Click prevention appendix (mic re-init mid-session, scene change, etc.). No audible click in any scenario.\n\nDeeper metrics (callback totals, ring totals, feed gap, monitoring transport, full FAIL flag set) live in the headed sections below — scroll for them.")]
    [SerializeField] private string currentTestDescription = "Step 3b: filter + _dbMicrophone on audio thread + tear detector + cross-thread labels. Tone normally + run click testing protocol; verify tear total = 0 and dbMicSnapshot tracks voice.";

    [Tooltip("Time.time — report with each grab.")]
    [SerializeField] private float currentTestSessionTimeSeconds;
    [Tooltip("Any FAIL_* below.")]
    [SerializeField] private bool currentTestFailure;

    [Tooltip("Step 3b: live _dbMicrophone snapshot, read once per LateUpdate via volatile float. The value the main thread is consuming this frame, computed on the audio thread from the post-filter buffer. Expect roughly -50 dB at quiet ambient, > -30 dB while toning. If this stays at -999 forever the audio-thread dB writer never ran (filter cutoffs zeroing the signal? audio thread frozen?). If it sits at a frozen value while feedPeakAbs moves, suspect a tear (see currentTestDbMicrophoneTearDetectedTotal).")]
    [SerializeField] private float currentTestDbMicrophoneSnapshot;
    [Tooltip("Step 3b: tear-detection counter. Increments when LateUpdate's read of _dbMicrophone returns NaN, ±Infinity, or a value outside the plausible dB band [-120, +24]. STICKY — do not auto-clear. BAR: must stay 0. Any non-zero value means the volatile guarantee is insufficient for cross-thread float visibility on this platform; escalate _dbMicrophone to Interlocked.Exchange via SingleToInt32Bits and rerun.")]
    [SerializeField] private long currentTestDbMicrophoneTearDetectedTotal;
    [Tooltip("Imitone-derived tone dB (post-analysis). Should still respond to voice — no regression vs F1.")]
    [SerializeField] private float currentTestDbValue;
    [Tooltip("Imitone-derived fundamental frequency (Hz). Should still track voice — no regression vs F1.")]
    [SerializeField] private float currentTestPitchHz;

    [Tooltip("Peak |sample| going to imitone (post-filter as of 3b). Voice ~0.01–0.1, silent < 0.005, voice/silent contrast > 10× while toning. (The F1-era ~0.05–0.5 reading was measured on UNFILTERED samples; the 80–520 Hz band-pass attenuates voice harmonics above 520 Hz and rumble below 80 Hz, so post-filter peak is naturally smaller. The contrast ratio is what matters.) If this stays at 0 while you tone but copied > 0, the filter chain is killing the signal (cutoffs misconfigured?). If peakAbs moves but dbMicSnapshot stays at floor, suspect a tear or stale dB writer.")]
    [SerializeField] private float currentTestFeedPeakAbs;
    [Tooltip("Rolling OAFR rate (Hz). Expect ~ output sample rate / DSP buffer size (e.g. ~46.9 @ 48k/1024).")]
    [SerializeField] private float currentTestAudioCallbackHzRolling;
    [Tooltip("imitone.InputAudio calls / callbacks (cumulative). ~1.0 after priming.")]
    [SerializeField] private float currentTestImitoneInputToCallbackRatio;
    [Tooltip("ReadRawSamples overflow drops on imitone path. Should NOT climb between grabs taken across the session.")]
    [SerializeField] private long currentTestAudioFeedOverflowDroppedTotal;
    [Tooltip("rawBufferLock TryEnter(0) misses from audio-thread readers. Should stay near 0; the new filter+dB work runs entirely after the lock is released so this counter should not move vs F1 closeout.")]
    [SerializeField] private long currentTestRawRingReadLockMissTotal;

    // Step 3b play-test follow-up: currentTestKnownFalsePositive_GcAlloc retired alongside
    // FAIL_AUDIO_GC_ALLOC_DETECTED. The duration heuristic is no longer a FAIL trigger, so the
    // CURRENT TEST block no longer needs to flag it as a known false positive.

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
    [Tooltip("True when rawBufferLock TryEnter(0) miss count (aggRawRingReadLockMissTotal) in the last 1s window exceeds failAudioLockMissPerSecondThreshold. Pass 3 repointed this from the deleted audioRingWriteLock counter; the FAIL flag name kept stable.")]
    [SerializeField] private bool FAIL_AUDIO_LOCK_CONTENTION;
    // Step 3b play-test follow-up (2026-05-06): FAIL_AUDIO_GC_ALLOC_DETECTED retired. The duration
    // heuristic was a Phase 2 proxy from when OnAudioFilterRead was near-empty; with imitone +
    // filter + dB now occupying the same callback (legitimate 5-15 ms CPU), the threshold cannot
    // reliably distinguish "imitone slow" from "GC pause" — at 3 ms it tripped on every callback,
    // at 15 ms it still tripped on imitone tail-latency spikes. The actual user-facing concerns
    // (engine starvation, callback freeze) are caught directly + reliably by FAIL_AUDIO_CALLBACK_RATE_LOW
    // and FAIL_AUDIO_CALLBACK_FROZEN. The underlying counter aggAudioCallbackGCAllocSuspectTotal
    // survives as diagnostic telemetry only — see its updated tooltip below.

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
    [Tooltip("Sticky: latched when aggMicGentleUnreadZeroRecoveryTotal increases above the baseline from last clear; clear by ticking clearFailObservationStickyFlags below.")]
    [SerializeField] private bool FAIL_GENTLE_RECOVERY_FIRED;
    [Tooltip("True when aggMonStarvationEvents increased within the last failMonitoringStarvationWindowSeconds wall-clock seconds.")]
    [SerializeField] private bool FAIL_MONITORING_STARVATION_GROWING;
    [Tooltip("True after startup grace when mic reports not ready (interpreter.IsMicReady = false). Step 3b simplified the trigger — the legacy aggInterpMicRefNull source field was always false, and the new direct read of IsMicReady is the canonical liveness signal.")]
    [SerializeField] private bool FAIL_MIC_NOT_READY;
    [Tooltip("Step 3b: STICKY — latches when aggDbMicrophoneTearDetectedTotal > 0 (volatile float read produced NaN / ±Infinity / out-of-range value, indicating a torn cross-thread read). Cure is to escalate _dbMicrophone from `volatile` to `Interlocked.Exchange` via `BitConverter.SingleToInt32Bits`. Cleared via clearFailObservationStickyFlags.")]
    [SerializeField] private bool FAIL_DB_TEAR_DETECTED;

    [Header("FAIL OBSERVATION — thresholds")]
    [SerializeField] private int failUnreadZeroSustainedFrameThreshold = 30;
    [SerializeField] private float failUnreadZeroSustainedSecondsThreshold = 0.5f;
    [SerializeField] private int failIngestRingStalledFrameThreshold = 30;
    // Step 3b: failInterpreterNotConsumingFrameThreshold retired with FAIL_INTERPRETER_NOT_CONSUMING —
    // its trigger (telemetryRawVoiceDataConsumedThisFrame) lived inside the deleted main-thread
    // tryCopyOk gate. Step 5b retires the legacy main-thread mic-ingest block entirely.
    [SerializeField] private float failMonitoringStarvationWindowSeconds = 2f;
    [SerializeField] private float failMicNotReadyGracePeriodSeconds = 2f;
    [SerializeField] private float failAudioCallbackStartupGraceSeconds = 2f;
    [SerializeField] private float failAudioCallbackFrozenSeconds = 0.2f;
    [SerializeField] private float failAudioCallbackRateLowFraction = 0.75f;
    [SerializeField] private float failAudioCallbackGapHighMultiplier = 2f;
    // Pass 3 note: now applies to rawBufferLock TryEnter(0) misses (was audioRingWriteLock pre-pass-3). Pass 2
    // play-test data showed ~0.04 misses/sec under healthy conditions; 50/sec is a "lock storm" alarm, not a
    // "mild contention" alarm. Tune lower if/when we want earlier warning. Left at 50 for Pass 3 to avoid
    // shifting two variables (counter source + threshold) in the same pass.
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
    [Tooltip("Diagnostic-only counter (no longer drives any FAIL flag as of Step 3b play-test follow-up). Counts callbacks whose duration exceeded audioCallbackGcSuspectMsThreshold. Was originally a Phase 2 GC-pause heuristic, but with imitone + filter + dB on the audio thread, legitimate steady-state callbacks routinely run 5-15 ms — the duration cannot reliably distinguish 'imitone slow' from 'GC pause.' Real audio-thread starvation / freezes are caught directly by FAIL_AUDIO_CALLBACK_RATE_LOW and FAIL_AUDIO_CALLBACK_FROZEN. Kept here as a 'how often did the audio thread spike above N ms' telemetry; for true GC-allocation verification, use the Profiler.")]
    [SerializeField] private long aggAudioCallbackGCAllocSuspectTotal;
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
    [Tooltip("Audio-thread ring writes skipped due to consumer-overrun. (3a: never increments — placeholder for Step 5b when a consumer exists. Distinct from aggRawRingReadLockMissTotal, which is rawBufferLock TryEnter contention.)")]
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
    [Tooltip("Step 3a Pass 2: TryEnter(0) misses on rawBufferLock from audio-thread readers (imitone feed + DirectVoiceMonitoring). The driver of FAIL_AUDIO_LOCK_CONTENTION (Pass 3 repointed it from the now-deleted audioRingWriteLock counter to this one). Should stay near 0.")]
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
    // Step 3b: aggRawConsumedThisFrame / aggInterpMicRefNull / aggInterpTryCopyTrue /
    // aggInterpTryCopySampleCount retired with the main-thread tryCopyOk gate they mirrored.
    // aggInterpMicReady survives — sourced directly from interpreter.IsMicReady now (the legacy
    // round-trip via debugInterpreterMicReady was removed). dB-unclamped fields keep their old
    // RawVoicePathDebugSnapshot path; that struct is trimmed but not retired (Step 5b owns the rename).
    [Tooltip("True when interpreter.IsMicReady = true (mic is initialized and capturing). Drives FAIL_MIC_NOT_READY post-Step-3b.")]
    [SerializeField] private bool aggInterpMicReady;
    [SerializeField] private float aggInterpMicDbUnclamped = -999f;
    [SerializeField] private float aggInterpImitoneDbUnclamped = -999f;

    [Header("Cross-thread atomicity (Step 3b)")]
    [Tooltip("Step 3b: live snapshot of interpreter._dbMicrophone (volatile float, audio writer / main reader). Read once per LateUpdate for tear-detection sanity (next field) and for the CURRENT TEST mirror. The value the main thread is consuming this frame.")]
    [SerializeField] private float aggDbMicrophoneSnapshot;
    [Tooltip("Step 3b: STICKY — counter of detected torn reads on _dbMicrophone. Detection method: each LateUpdate, read _dbMicrophone; if value is NaN, ±Infinity, or outside the plausible dB range [-120, +24], increment. Any non-zero count means the volatile guarantee is insufficient on this platform and _dbMicrophone needs to escalate to Interlocked.Exchange. Cleared via clearFailObservationStickyFlags below.")]
    [SerializeField] private long aggDbMicrophoneTearDetectedTotal;
    [Tooltip("Step 3b: read-only label listing audio-thread / main-thread shared fields currently behind C# `volatile` semantics. Set once at startup; reflects the actual code, not a wish list. Quick-glance reference for the cross-thread contract.")]
    [SerializeField] private string aggCrossThreadFieldsUsingVolatile = "";
    [Tooltip("Step 3b: read-only label listing audio-thread / main-thread shared fields currently behind `Interlocked.*` operations (counters, CAS, atomic exchanges). Set once at startup; reflects the actual code.")]
    [SerializeField] private string aggCrossThreadFieldsUsingInterlocked = "";

    private const float DbMicrophoneTearDetectMinDb = -120f;
    private const float DbMicrophoneTearDetectMaxDb = 24f;
    private const float DbMicrophonePreInitSentinel = -999f;

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

    // Step 3b: _consecutiveInterpreterNotConsumingFrames retired with FAIL_INTERPRETER_NOT_CONSUMING.

    private int _gentleRecoveryBaselineAtClear;
    private bool _gentleRecoveryStickyLatched;

    // Step 3b: sticky latch for _dbMicrophone tear detector. Latches when aggDbMicrophoneTearDetectedTotal
    // climbs above the cleared baseline; user must clear via clearFailObservationStickyFlags. The cure is
    // an atomicity escalation (volatile -> Interlocked) which is a code change, not a runtime recovery —
    // hence sticky.
    private long _dbMicTearBaselineAtClear;
    private bool _dbMicTearStickyLatched;

    private int _prevAggMonStarvationEvents = -1;
    private float _lastStarvationIncreaseRealtime = -1f;

    private long _prevAggAudioCallbackTotalForFrozen = -1;
    private float _lastAudioCallbackTotalAdvanceRealtime;
    private float _audioRateLowSustainedTimer;
    private float _audioLockMissWindowTimer;
    // Pass 3: was tracking audioRingWriteLock (now deleted). Repointed to aggRawRingReadLockMissTotal —
    // contention on rawBufferLock between the main-thread mic writer and audio-thread readers (imitone feed,
    // DirectVoiceMonitoring). Field name kept generic ("audio lock miss") since the FAIL flag is the same.
    private long _audioLockMissAtWindowStart;
    private bool _audioLockBaselineInitialized;
    // Step 3b play-test follow-up: _audioGCBaselineInitialized / _gcAllocStickyLatched /
    // _gcSuspectBaselineAtClear retired with FAIL_AUDIO_GC_ALLOC_DETECTED. The duration heuristic
    // is now diagnostic telemetry only; no sticky latch needed.

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
        _audioLockBaselineInitialized = false;
        _imitoneFeedRatioWindowInitialized = false;
        _dbMicTearBaselineAtClear = 0;
        _dbMicTearStickyLatched = false;
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

        // Step 3b: read-only labels listing the cross-thread contract for fields shared between
        // audio thread (writer) and main thread (reader), or vice versa. Set once here from the
        // actual code state — when adding a new shared field, update these strings AND the
        // matching field declaration in the same edit pass so the labels stay truthful.
        aggCrossThreadFieldsUsingVolatile =
            "ImitoneVoiceIntepreter._dbMicrophone (audio→main; primary V7 candidate); " +
            "ImitoneVoiceIntepreter.imitone (main→audio; review-pass V7 close-out); " +
            "ImitoneVoiceIntepreter._highPassFilterEnabled, _highPassCutoffHz, " +
            "_lowPassFilterEnabled, _lowPassCutoffHz (main→audio; runtime-tunability); " +
            "AudioThread.audioCallbackFeedPeakAbsVolatile, audioCallbackHzRollingVolatile, " +
            "audioCallbackMaxGapMsLastSecondVolatile, audioCallbackLastSamplesPerCallback, " +
            "aggMixerChannelsVolatile, imitoneInputAudioMainThreadLogged; " +
            "DirectVoiceMonitoring.effectiveMonitoringGain";
        aggCrossThreadFieldsUsingInterlocked =
            "AudioThread: audioCallbackTotal, audioCallbackSamplesProcessedTotal, " +
            "audioCallbackGCAllocSuspectTotal, audioCallbackMaxGapTicksWindow, " +
            "imitoneInputAudioCallTotal, imitoneInputAudioPendingException (CAS), " +
            "audioFeedOverflowDroppedTotal, audioThreadFeedReadTotalSamples, " +
            "rawWriteTotalSamples, rawRingReadLockMissTotal, micRingOverflowSkipTotal; " +
            "DirectVoiceMonitoring: bufferUnderflow*, bufferOverflow*, callbackStarvation*";
    }

    private void ApplyClearFailObservationStickyFlags()
    {
        clearFailObservationStickyFlags = false;
        _gentleRecoveryBaselineAtClear = aggMicGentleUnreadZeroRecoveryTotal;
        _gentleRecoveryStickyLatched = false;
        _rawRingStallPrevInitialized = false;
        _consecutiveIngestRingStallFrames = 0;
        _audioLockBaselineInitialized = false;
        _audioLockMissWindowTimer = 0f;
        // Step 3b: snapshot the current tear count as the new baseline. Subsequent tears (above
        // this baseline) re-latch FAIL_DB_TEAR_DETECTED. Clearing does NOT cure the underlying
        // atomicity issue — that's a code change (volatile -> Interlocked) — so the user clears
        // only after addressing it.
        _dbMicTearBaselineAtClear = aggDbMicrophoneTearDetectedTotal;
        _dbMicTearStickyLatched = false;
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
            // Step 3b: legacy tryCopyOk-gate booleans deleted. Mic-readiness sourced directly from
            // IsMicReady (the canonical liveness signal) instead of round-tripping through a debug
            // mirror field. RawVoicePathDebugSnapshot now carries only the two unclamped-dB fields
            // — Step 5b will rename it.
            aggInterpMicReady = interpreter.IsMicReady;
            ImitoneVoiceIntepreter.RawVoicePathDebugSnapshot v = interpreter.GetRawVoicePathDebugSnapshot();
            aggInterpMicDbUnclamped = v.telemetryMicDbUnclamped;
            aggInterpImitoneDbUnclamped = v.telemetryImitoneDbUnclamped;

            // Step 3b: cross-thread atomicity snapshot. Read _dbMicrophone ONCE per LateUpdate via
            // its volatile semantics; downstream FAIL trigger and CURRENT TEST mirror both consume
            // this single read. The tear detector sanity-checks the value bit-pattern: NaN /
            // ±Infinity / out-of-band-dB indicate a torn cross-thread read (the volatile guarantee
            // is insufficient on this platform), at which point _dbMicrophone needs to escalate to
            // Interlocked.Exchange via BitConverter.SingleToInt32Bits. The single-read pattern is
            // important — a re-read could mask a tear by getting a clean value next time.
            float dbMicSample = interpreter._dbMicrophone;
            aggDbMicrophoneSnapshot = dbMicSample;
            // -999f is the documented pre-init sentinel (set in the field initializer; the audio
            // thread will never write a value < -120 dB because AudioLevelUtilities.LinearToDb
            // clamps amplitude to 1e-6 → -120 dB floor). Skip tear-detect until the audio thread
            // has written at least once. Without this exclusion every LateUpdate before the first
            // audio callback would log a tear and FAIL_DB_TEAR_DETECTED would latch on startup.
            bool isPreInitSentinel = dbMicSample == DbMicrophonePreInitSentinel;
            bool tornRead =
                !isPreInitSentinel
                && (float.IsNaN(dbMicSample)
                    || float.IsInfinity(dbMicSample)
                    || dbMicSample < DbMicrophoneTearDetectMinDb
                    || dbMicSample > DbMicrophoneTearDetectMaxDb);
            if (tornRead)
            {
                aggDbMicrophoneTearDetectedTotal++;
            }

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
            aggAudioCallbackGCAllocSuspectTotal = a.audioCallbackGCAllocSuspectTotal;
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

            // CURRENT TEST — only fields needed for the active pass (Step 3b). See plan doc § 9.
            currentTestSessionTimeSeconds = Time.time;
            currentTestDbMicrophoneSnapshot = aggDbMicrophoneSnapshot;
            currentTestDbMicrophoneTearDetectedTotal = aggDbMicrophoneTearDetectedTotal;
            currentTestDbValue = interpreter._dbValue;
            currentTestPitchHz = interpreter.pitch_hz;
            currentTestFeedPeakAbs = aggAudioThreadFeedPeakAbsLastCallback;
            currentTestAudioCallbackHzRolling = aggAudioCallbackHzRolling;
            currentTestAudioFeedOverflowDroppedTotal = aggAudioFeedOverflowDroppedTotal;
            currentTestRawRingReadLockMissTotal = aggRawRingReadLockMissTotal;

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
                _audioLockMissAtWindowStart = aggRawRingReadLockMissTotal;
                _audioLockMissWindowTimer = 0f;
                _audioLockBaselineInitialized = true;
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
                long d = aggRawRingReadLockMissTotal - _audioLockMissAtWindowStart;
                FAIL_AUDIO_LOCK_CONTENTION = d > failAudioLockMissPerSecondThreshold;
                _audioLockMissAtWindowStart = aggRawRingReadLockMissTotal;
                _audioLockMissWindowTimer = 0f;
            }

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
        // Step 3b: FAIL_INTERPRETER_NOT_CONSUMING retired — its trigger lived inside the deleted
        // main-thread tryCopyOk gate. Imitone is now fed exclusively from the audio thread; if the
        // feed dies, FAIL_IMITONE_NOT_FED + FAIL_IMITONE_FEED_RATIO_LOW catch it directly without
        // going through a "main-thread did not consume" proxy.

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
            // Step 3b: simplified — interpreter.IsMicReady is the canonical liveness signal. The
            // legacy aggInterpMicRefNull source was always false (never assigned true anywhere) and
            // contributed no information to the trigger.
            FAIL_MIC_NOT_READY = !aggInterpMicReady;
        }
        else
        {
            FAIL_MIC_NOT_READY = false;
        }

        // Step 3b: tear-detection sticky latch. Increases above the cleared baseline mean a torn
        // cross-thread float read happened — the cure is to escalate _dbMicrophone from `volatile`
        // to `Interlocked.Exchange` (code change), so the latch survives until manually cleared.
        if (aggDbMicrophoneTearDetectedTotal > _dbMicTearBaselineAtClear)
        {
            _dbMicTearStickyLatched = true;
        }
        FAIL_DB_TEAR_DETECTED = _dbMicTearStickyLatched;

        FAILURE =
            FAIL_AUDIO_CALLBACK_FROZEN
            || FAIL_AUDIO_CALLBACK_RATE_LOW
            || FAIL_AUDIO_CALLBACK_GAP_HIGH
            || FAIL_AUDIO_LOCK_CONTENTION
            || FAIL_IMITONE_NOT_FED
            || FAIL_IMITONE_FEED_RATIO_LOW
            || FAIL_RING_OVERFLOW_GROWING
            || FAIL_UNREAD_ZERO_SUSTAINED
            || FAIL_INGEST_RING_STALLED
            || FAIL_GENTLE_RECOVERY_FIRED
            || FAIL_MONITORING_STARVATION_GROWING
            || FAIL_MIC_NOT_READY
            || FAIL_DB_TEAR_DETECTED;

        currentTestFailure = FAILURE;
    }
}
