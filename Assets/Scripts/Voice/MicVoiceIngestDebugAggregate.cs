using UnityEngine;

/// <summary>
/// Copies mic-ingest debug (from ImitoneVoiceIntepreter) + Imitone raw-path debug + tone/imitone gate flags
/// (+ optional DirectVoiceMonitoring transport totals) into one Inspector block after upstream Update()
/// (LateUpdate). Step 3b adds a cross-thread atomicity / tear-detection block. <b>Step 5a rewrites the
/// CURRENT TEST header/field set for click protocol verification</b> (M1/M2/M5/M6) — surfaces
/// DirectVoiceMonitoring underflow / overflow / starvation / hard-volume-step counters in the aggregate
/// (previously visible only in DirectVoiceMonitoring's own Inspector). Optional <c>Debug.LogError</c> on
/// FAIL rising edge / sustained (exponential backoff: 0.25 s → 16 s) / falling edge with duration +
/// "flags seen during window" summary, when <see cref="logFailObservationErrorsToConsole"/> is enabled —
/// for soak and user builds where Inspector FAIL flags are not visible.
/// Mic-ingest snapshot type is <see cref="ImitoneVoiceIntepreter.MicIngestDebugSnapshot"/>; values are copied
/// from <see cref="ImitoneVoiceIntepreter.GetMicIngestDebugSnapshot"/>.
/// The <b>CURRENT TEST</b> section at the top contains <i>only</i> what the active play test needs — tight
/// enough for one Inspector screengrab. The contract: this block is rewritten <i>before</i> each testing
/// round (header text, currentTestDescription, field set, LateUpdate mirror copies). Old fields from prior
/// rounds get removed in the same edit pass — no accumulation. See
/// <c>Docs/MIC_VOICE_INGEST_FIX_PLAN.md</c> § 9 (Active-bug debugging convention) for the full rules.
/// Step 6 adds <see cref="inspectorInterpretationGuideReference"/> (healthy-vs-broken reference text; canonical tables stay in the plan doc).
/// </summary>
// Threading note (5b-iii): every method in this file runs on: main thread. The aggregate is a pure
// consumer — it reads cross-thread state via the snapshot accessors on the producers (each of which
// uses Interlocked.Read / volatile reads internally to make a torn-read-safe copy onto the main
// thread). NO direct cross-thread synchronization happens here; the aggregate trusts its sources.
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

    [Header("CURRENT TEST — Step 5b-vi: click protocol + F1 hybrid regression bar")]
    [Tooltip("Step 5b-vi — full 5-scenario click protocol (same five scenarios as Step 5a) after the 5b cleanup passes, with F1 hybrid health rows kept visible. PRIMARY PASS/FAIL: (1) no audible clicks in any scenario — trust ears first; (2) failure = false (composite FAIL_* OR).\n\nF1 HYBRID / 5b CLEANUP: micExitReason flickers \"unread_zero\" often — Unity Microphone.GetPosition() advances in chunks, so many LateUpdates see zero unread samples; any \"copied_samples\" tick resets the unread-zero streak. Unrelated to voicing vs silence. Compare aggUnreadZeroConsecutiveFrames (Mic ingest section) to failUnreadZeroSustainedFrameThreshold (default 30): bursty drivers keep the streak below 30 while micExitReason still shows unread_zero most frames. micRawRingWriteTotalSamples must climb at sample rate; imitoneInputAudioCallTotal tracks callbacks. Sticky unhealthy reasons: stalled_capture_stopped, device_unavailable, invalid_mic_position.\n\nCLICK MITIGATION (5a): monUnderflowEvents / monOverflowEvents / monStarvationEvents / monHardVolumeStepCount — same story as Step 5a (ticks under load OK; steady-state climb on underflow/overflow/starvation is concerning; hardVolumeStepCount is M6 input — pass/fail is ears on onset/offset).\n\nDeeper metrics (feed gap, raw lock misses, full FAIL set) live in headed sections below. Per §9, rewrite CURRENT TEST before the next testing round.")]
    [SerializeField] private string currentTestDescription = "Step 5b-vi — full click protocol + F1 hybrid bar. Run all 5 scenarios (quiet baseline, sustained tone, onset/offset, heavy load, long session); capture Inspector after each. Trust ears first. Rewrite before next round (§9).";

    [Tooltip("Time.time — report with each grab.")]
    [SerializeField] private float currentTestSessionTimeSeconds;
    [Tooltip("Any FAIL_* below — composite OR. Universal canary, must stay false.")]
    [SerializeField] private bool currentTestFailure;

    [Tooltip("F1 hybrid producer snapshot exit reason from UpdateMicReadFrame. \"copied_samples\" = mic frame copied this LateUpdate. \"unread_zero\" = computed unread sample count <= 0 (Microphone.GetPosition has not advanced past our read cursor since last poll — normal when the driver reports the write head in chunks). Any copied_samples tick resets the unread-zero streak (see aggUnreadZeroConsecutiveFrames). Unhealthy when sticky: stalled_capture_stopped, device_unavailable, invalid_mic_position.")]
    [SerializeField] private string currentTestMicExitReason = "";
    [Tooltip("Step 5b regression watch — main-thread producer total samples written to the raw ring. MUST climb at ~aggAudioConfigOutputSampleRate per second while the mic is alive. Flat = main-thread producer broken (UpdateMicReadFrame stopped writing); F1 hybrid is dead.")]
    [SerializeField] private long currentTestMicRawRingWriteTotalSamples;
    [Tooltip("Step 5b regression watch — audio-thread consumer total imitone.InputAudio calls. MUST climb ~1:1 with audioCallbackHzRolling × time. Flat while audioCallbackHzRolling is alive = audio-thread consumer feeding imitone is broken (drain + InputAudio path); F1 hybrid consumer is dead.")]
    [SerializeField] private long currentTestImitoneInputAudioCallTotal;

    [Tooltip("Step 3b regression watch (sticky): tear-detection counter on _dbMicrophone volatile float read. Must stay 0 — any increment means we saw NaN/±Inf/out-of-range; escalate _dbMicrophone to Interlocked.")]
    [SerializeField] private long currentTestDbMicrophoneTearDetectedTotal;
    [Tooltip("Rolling OnAudioFilterRead rate (Hz). Audio thread alive precondition. ~ outputSampleRate/dspBufferSize (e.g. ~46.9 Hz at 1024/48 k).")]
    [SerializeField] private float currentTestAudioCallbackHzRolling;
    [Tooltip("Voice-alive signal — imitone-derived tone dB. Confirms downstream voice path still produces audio.")]
    [SerializeField] private float currentTestDbValue;
    [Tooltip("Voice-alive signal — imitone-derived pitch (Hz).")]
    [SerializeField] private float currentTestPitchHz;

    [Tooltip("5a click-mitigation regression watch: cumulative DirectVoiceMonitoring underflow fills (ReadRawSamples / ReadNormalizedSamples returned copied < frameCount). May tick under load; steady-state climb = the cleanup loosened a buffer-sizing or read-latency invariant.")]
    [SerializeField] private int currentTestMonUnderflowEvents;
    [Tooltip("5a click-mitigation regression watch: cumulative DirectVoiceMonitoring overflow drops (helper jumped readPosition). May tick under load; steady-state climb = ring undersized or main-thread stalling reads.")]
    [SerializeField] private int currentTestMonOverflowEvents;
    [Tooltip("5b-vi click protocol: cumulative DirectVoiceMonitoring callback starvations (copied == 0). Startup transients normal (compare 5a gold-standard notes); steady-state climb = capture not feeding ring fast enough.")]
    [SerializeField] private int currentTestMonStarvationEvents;
    [Tooltip("5b-vi click protocol: cumulative main-thread per-frame gain deltas > DirectVoiceMonitoring.hardVolumeStepThreshold (M6 input signal). Diagnostic — may climb on toneActive flips; pass/fail is ears on scenario 3, not this counter.")]
    [SerializeField] private int currentTestMonHardVolumeStepCount;

    [Header("Interpretation guide (Step 6 — reference)")]
    [Tooltip("Healthy-vs-broken cheat sheet for this Inspector. Canonical tables + retired-flag notes: Docs/MIC_VOICE_INGEST_FIX_PLAN.md Step 6 (reconciled). Safe to edit locally; default documents F1 hybrid producer/consumer split.")]
    [SerializeField] [TextArea(22, 120)] private string inspectorInterpretationGuideReference =
        "F1 HYBRID — Main thread is ring PRODUCER (UpdateMicReadFrame polls Microphone / GetData). Audio thread is CONSUMER (OnAudioFilterRead feeds imitone from rawRingBuffer; DirectVoiceMonitoring also reads rings).\r\n\r\n"
        + "UNIVERSAL PASS — FAILURE stays false (composite OR of every FAIL_*).\r\n\r\n"
        + "FAIL PHASE 2 (audio thread) — FAIL_AUDIO_CALLBACK_FROZEN / RATE_LOW / GAP_HIGH. FAIL_AUDIO_LOCK_CONTENTION uses aggRawRingReadLockMissTotal (rawBufferLock TryEnter misses), not legacy ring-write lock.\r\n\r\n"
        + "FAIL PHASE 3 (imitone feed) — FAIL_IMITONE_NOT_FED; FAIL_IMITONE_FEED_RATIO_LOW.\r\n\r\n"
        + "FAIL PHASE 1 — FAIL_UNREAD_ZERO_SUSTAINED (compare aggUnreadZeroConsecutiveFrames to thresholds; exit reason may flicker unread_zero on chunk drivers). FAIL_INGEST_RING_STALLED. FAIL_MONITORING_STARVATION_GROWING. FAIL_MIC_NOT_READY. FAIL_DB_TEAR_DETECTED (sticky).\r\n\r\n"
        + "MIC PRODUCER — aggMicRawRingWriteTotalSamples climbs at sample rate while capturing. Sticky unhealthy exit reasons: stalled_capture_stopped, device_unavailable, invalid_mic_position.\r\n\r\n"
        + "IMITONE FEED — aggImitoneInputAudioCallTotal tracks aggAudioCallbackTotal. aggAudioFeedOverflowDroppedTotal = read-side overflow drops (prefer 0). Gap ms/samples — stable band while toning.\r\n\r\n"
        + "MONITORING — aggMonUnderflowEvents / aggMonOverflowEvents / aggMonStarvationEvents (steady-state climb bad).\r\n\r\n"
        + "RETIRED (do not hunt in Inspector) — FAIL_AUDIO_GC_ALLOC_DETECTED; FAIL_RING_OVERFLOW_GROWING + phantom skip counter (5b-iv); FAIL_GENTLE_RECOVERY_FIRED (5b-ii).";

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
    // 5b-iv: FAIL_RING_OVERFLOW_GROWING retired alongside its source counter aggMicRingOverflowSkipTotal
    // (see ImitoneVoiceIntepreter.AudioThread.cs note). Counter never incremented from any code path —
    // placeholder for an architecture (audio-thread ring producer) the F1 hybrid pivot abandoned.
    // The active overflow watch on the F1 hybrid lives on the read side as aggAudioFeedOverflowDroppedTotal.

    [Header("FAIL OBSERVATION — Phase 1 (ingest)")]
    [Tooltip("True when the ingest exit reason stayed unread_zero for too many consecutive frames or seconds (see thresholds).")]
    [SerializeField] private bool FAIL_UNREAD_ZERO_SUSTAINED;
    [Tooltip("True when aggMicRawRingWriteTotalSamples has not increased for failIngestRingStalledFrameThreshold consecutive LateUpdate calls.")]
    [SerializeField] private bool FAIL_INGEST_RING_STALLED;
    // 5b-ii: FAIL_GENTLE_RECOVERY_FIRED retired — its source counter (the gentle unread_zero capture restart)
    // was deleted from the mic-ingest producer in 5b. The remaining ingest-health failures (FAIL_UNREAD_ZERO_SUSTAINED,
    // FAIL_INGEST_RING_STALLED) continue to cover the same failure modes.
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
    [Header("FAIL OBSERVATION — actions")]
    [Tooltip("Tick once in Play mode to clear sticky FAIL_DB_TEAR_DETECTED latch and reset ring-stall baseline; unticks automatically. (5b-ii: previously also cleared the gentle-recovery sticky latch, which has been retired.)")]
    [SerializeField] private bool clearFailObservationStickyFlags;
    [Tooltip("When enabled: Debug.LogError on the rising edge AND while sustained AND on the falling edge of FAIL_OBSERVATION (composite FAILURE) — for soak sessions / user builds where Inspector FAIL flags are not visible. Also logs once per contiguous streak when the _dbMicrophone tear detector fires. Disable if another pipeline ingests Unity console logs and you need less noise. See failObservationLogIntervalInitialSeconds and failObservationLogIntervalCapSeconds for re-log cadence while a failure is sustained.")]
    [SerializeField] private bool logFailObservationErrorsToConsole = true;
    [Tooltip("First periodic re-log delay AFTER the rising-edge log, while FAILURE is still TRUE. Each subsequent re-log doubles this interval until failObservationLogIntervalCapSeconds is hit (exponential backoff). Default 0.25 s preserves resolution for short blips; backoff prevents log spam on long sustained failures. Set to 0 to disable periodic re-logging (rising + falling-edge only).")]
    [SerializeField] [Range(0f, 5f)] private float failObservationLogIntervalInitialSeconds = 0.25f;
    [Tooltip("Maximum interval between periodic re-logs while FAILURE is sustained (exponential backoff cap). After hitting this cap, re-logs continue at this rate until the failure clears. Default 16 s = a 60 s sustained failure produces ~9 logs total instead of ~240 at fixed 0.25 s.")]
    [SerializeField] [Range(1f, 120f)] private float failObservationLogIntervalCapSeconds = 16f;

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
    [Tooltip("DEBUG: peak |sample| of the mono buffer the audio thread just handed to imitone.InputAudio. Post-3b (filtered): voice ~0.01–0.1, silent < 0.005, contrast > 10×. Pre-3b unfiltered bar was ~0.05–0.5. If ~0 while _dbMicrophone moves on voice, the audio thread is being fed silence and imitone is innocent.")]
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
    [SerializeField] private long aggMicRawRingWriteTotalSamples;
    [SerializeField] private long aggMicNormRingWriteTotalSamples;
    [Tooltip("5b-vi: consecutive LateUpdate frames where aggMicExitReason stayed \"unread_zero\" without a \"copied_samples\" tick interrupting the streak. Resets to 0 on any copied_samples frame. FAIL_UNREAD_ZERO_SUSTAINED uses this vs failUnreadZeroSustainedFrameThreshold (default 30) and wall-clock failUnreadZeroSustainedSecondsThreshold. Bursty drivers: Inspector often shows unread_zero while this stays well below 30.")]
    [SerializeField] private int aggUnreadZeroConsecutiveFrames;

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
    [Tooltip("Step 5a M6 diagnostic: cumulative count of main-thread per-frame gain deltas > DirectVoiceMonitoring.hardVolumeStepThreshold (default 0.2). NOT a pass/fail counter post-5a — these are the *input* signal to a click vector that M6's per-sample audio-thread interpolation smooths inaudibly. Climbing on toneActive flips / attenuation toggles is expected; pass/fail is \"no audible clicks\", not \"counter at 0\".")]
    [SerializeField] private int aggMonHardVolumeStepCount;

    private const string UnreadZeroExitReason = "unread_zero";

    private int _consecutiveUnreadZeroFrames;
    private float _unreadZeroSegmentStartRealtime = -1f;

    private bool _rawRingStallPrevInitialized;
    private long _prevAggMicRawRingWriteTotalSamples;
    private int _consecutiveIngestRingStallFrames;

    // Step 3b: _consecutiveInterpreterNotConsumingFrames retired with FAIL_INTERPRETER_NOT_CONSUMING.
    // 5b-ii: _gentleRecoveryBaselineAtClear / _gentleRecoveryStickyLatched retired with FAIL_GENTLE_RECOVERY_FIRED.

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
    private int _prevAggCaptureEpoch = -1;

    // Production/soak logging: rising / sustained / falling edges of composite FAILURE + first frame
    // of each dB-tear streak. The rising-edge log captures "when did this start"; the periodic re-log
    // (exponential backoff from failObservationLogIntervalInitialSeconds → failObservationLogIntervalCapSeconds)
    // tells field/soak readers "is it still happening RIGHT NOW after N seconds"; the falling-edge log
    // captures "how long did it last + what flags were ever true during the window."
    private bool _prevFailureObserved;
    private bool _dbMicTearReadErrorStreakActive;
    private float _failObservationRisingEdgeRealtime;
    private float _failObservationLastLogRealtime;
    private float _failObservationNextLogIntervalSeconds;
    // Per-flag "ever-true during this failure window" trackers for the falling-edge "flags seen during" summary.
    // Reset on rising edge; OR'd-in every frame while sustained; emitted in BuildFailObservationAccumulatedFlagsSummary.
    private bool _failSeenAudioCallbackFrozen;
    private bool _failSeenAudioCallbackRateLow;
    private bool _failSeenAudioCallbackGapHigh;
    private bool _failSeenAudioLockContention;
    private bool _failSeenImitoneNotFed;
    private bool _failSeenImitoneFeedRatioLow;
    private bool _failSeenUnreadZeroSustained;
    private bool _failSeenIngestRingStalled;
    private bool _failSeenMonitoringStarvationGrowing;
    private bool _failSeenMicNotReady;
    private bool _failSeenDbTearDetected;

    // runs on: main thread (Unity lifecycle).
    private void Awake()
    {
        _lastAudioCallbackTotalAdvanceRealtime = Time.realtimeSinceStartup;
        _lastImitoneInputAudioAdvanceRealtime = Time.realtimeSinceStartup;
        _audioLockBaselineInitialized = false;
        _imitoneFeedRatioWindowInitialized = false;
        _dbMicTearBaselineAtClear = 0;
        _dbMicTearStickyLatched = false;
        _prevFailureObserved = false;
        _dbMicTearReadErrorStreakActive = false;
        _failObservationRisingEdgeRealtime = 0f;
        _failObservationLastLogRealtime = 0f;
        _failObservationNextLogIntervalSeconds = 0f;
        ResetFailObservationAccumulatedFlags();
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
            "rawWriteTotalSamples, rawRingReadLockMissTotal; " +
            "DirectVoiceMonitoring: bufferUnderflow*, bufferOverflow*, callbackStarvation*";
    }

    // runs on: main thread (called from LateUpdate when the Inspector toggle is ticked).
    private void ApplyClearFailObservationStickyFlags()
    {
        clearFailObservationStickyFlags = false;
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

    // runs on: main thread (Unity lifecycle). The aggregate's sole entry point per frame: pulls
    // snapshots from the three producers (interpreter mic-ingest, audio-thread health, monitoring
    // counters) and rolls them into the FAIL OBSERVATION evaluation + soak-log edge detection.
    // Runs after every other voice-path script's Update because LateUpdate is later in the frame.
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
                if (logFailObservationErrorsToConsole && !_dbMicTearReadErrorStreakActive)
                {
                    _dbMicTearReadErrorStreakActive = true;
                    UnityEngine.Debug.LogError(
                        $"[MicVoiceIngest] _dbMicrophone tear-detector: impossible cross-thread float read "
                        + $"(sample={dbMicSample}, frame={Time.frameCount}, time={Time.realtimeSinceStartup:F2}s). "
                        + "See FAIL_DB_TEAR_DETECTED / aggDbMicrophoneTearDetectedTotal — escalate to Interlocked if this persists.");
                }
            }
            else
            {
                _dbMicTearReadErrorStreakActive = false;
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
            aggAudioThreadFeedPeakAbsLastCallback = a.audioCallbackFeedPeakAbsLastCallback;
            aggAudioThreadFeedReadTotalSamples = a.audioThreadFeedReadTotalSamples;
            aggAudioFeedOverflowDroppedTotal = a.audioFeedOverflowDroppedTotal;
            // Step 3a Pass 2: gap (samples + ms) and rawBufferLock TryEnter miss counter.
            aggAudioThreadFeedToWriteHeadGapSamples = a.audioThreadFeedToWriteHeadGapSamples;
            aggAudioThreadFeedToWriteHeadGapMs = aggAudioConfigOutputSampleRate > 0
                ? aggAudioThreadFeedToWriteHeadGapSamples * 1000f / aggAudioConfigOutputSampleRate
                : -1f;
            aggRawRingReadLockMissTotal = a.rawRingReadLockMissTotal;

            // Step 3a: imitone-feed observability — main-thread side (before CURRENT TEST — ratio + staleness need these first).
            aggImitoneGetStateCallTotal = interpreter.ImitoneGetStateCallTotal;
            aggMainThreadFramesSinceLastImitoneStateChange = interpreter.MainThreadFramesSinceLastImitoneStateChange;

            // Cumulative ratio (snapshot value; the rolling-window check below is what FAIL_IMITONE_FEED_RATIO_LOW uses).
            aggImitoneInputToCallbackRatio = aggAudioCallbackTotal > 0
                ? (float)aggImitoneInputAudioCallTotal / aggAudioCallbackTotal
                : 0f;

            // CURRENT TEST — Step 5b-vi: click protocol + F1 hybrid regression bar. See plan doc § 9.
            // Monitoring transport mirrors (currentTestMon*) populated below after voiceMonitoring read.
            currentTestSessionTimeSeconds = Time.time;
            currentTestMicExitReason = aggMicExitReason;
            currentTestMicRawRingWriteTotalSamples = aggMicRawRingWriteTotalSamples;
            currentTestImitoneInputAudioCallTotal = aggImitoneInputAudioCallTotal;
            currentTestDbMicrophoneTearDetectedTotal = aggDbMicrophoneTearDetectedTotal;
            currentTestAudioCallbackHzRolling = aggAudioCallbackHzRolling;
            currentTestDbValue = interpreter._dbValue;
            currentTestPitchHz = interpreter.pitch_hz;

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
            aggMonHardVolumeStepCount = voiceMonitoring.HardVolumeStepCount;
        }

        // CURRENT TEST — Step 5b-vi monitoring-transport mirrors (click protocol + 5a M-vectors). See § 9.
        currentTestMonUnderflowEvents = aggMonUnderflowEvents;
        currentTestMonOverflowEvents = aggMonOverflowEvents;
        currentTestMonStarvationEvents = aggMonStarvationEvents;
        currentTestMonHardVolumeStepCount = aggMonHardVolumeStepCount;

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

            // 5b-iv: FAIL_RING_OVERFLOW_GROWING trigger block retired — counter never incremented (F1
            // hybrid pivot relocated overflow protection to the read side as aggAudioFeedOverflowDroppedTotal,
            // so a write-side overflow guard on the audio thread became architecturally unnecessary).
        }
        else
        {
            FAIL_AUDIO_CALLBACK_FROZEN = false;
            FAIL_AUDIO_CALLBACK_RATE_LOW = false;
            FAIL_AUDIO_CALLBACK_GAP_HIGH = false;
            FAIL_AUDIO_LOCK_CONTENTION = false;
            FAIL_IMITONE_NOT_FED = false;
            FAIL_IMITONE_FEED_RATIO_LOW = false;
            _audioRateLowSustainedTimer = 0f;
        }

        // --- FAIL OBSERVATION Phase 1 (after aggregate copies and optional clear) ---
        // 5b-ii: gentle-restart exit reason was retired alongside the gentle-recovery family;
        // the only "still in trouble" exit reason left is bare unread_zero.
        bool inUnreadZeroSegment = aggMicExitReason == UnreadZeroExitReason;
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

        aggUnreadZeroConsecutiveFrames = _consecutiveUnreadZeroFrames;

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
        // 5b-ii: FAIL_GENTLE_RECOVERY_FIRED retired — its source counter (the gentle unread_zero
        // capture restart) was deleted from the mic-ingest producer.

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
            || FAIL_UNREAD_ZERO_SUSTAINED
            || FAIL_INGEST_RING_STALLED
            || FAIL_MONITORING_STARVATION_GROWING
            || FAIL_MIC_NOT_READY
            || FAIL_DB_TEAR_DETECTED;

        currentTestFailure = FAILURE;

        if (logFailObservationErrorsToConsole)
        {
            bool isRising = FAILURE && !_prevFailureObserved;
            bool isSustained = FAILURE && _prevFailureObserved;
            bool isFalling = !FAILURE && _prevFailureObserved;
            float now = Time.realtimeSinceStartup;

            if (isRising)
            {
                _failObservationRisingEdgeRealtime = now;
                _failObservationLastLogRealtime = now;
                _failObservationNextLogIntervalSeconds = Mathf.Max(0f, failObservationLogIntervalInitialSeconds);
                ResetFailObservationAccumulatedFlags();
                AccumulateFailObservationFlags();
                UnityEngine.Debug.LogError("[MicVoiceIngest] FAIL_OBSERVATION composite is now TRUE — " + BuildFailObservationLogDetail());
            }
            else if (isSustained)
            {
                AccumulateFailObservationFlags();
                // Periodic re-log with exponential backoff. interval == 0 => disabled (rising + falling only).
                if (_failObservationNextLogIntervalSeconds > 0f
                    && now - _failObservationLastLogRealtime >= _failObservationNextLogIntervalSeconds)
                {
                    float elapsed = now - _failObservationRisingEdgeRealtime;
                    UnityEngine.Debug.LogError(
                        $"[MicVoiceIngest] FAIL_OBSERVATION still TRUE after {elapsed:F2}s — "
                        + BuildFailObservationLogDetail());
                    _failObservationLastLogRealtime = now;
                    _failObservationNextLogIntervalSeconds = Mathf.Min(
                        _failObservationNextLogIntervalSeconds * 2f,
                        Mathf.Max(failObservationLogIntervalInitialSeconds, failObservationLogIntervalCapSeconds));
                }
            }
            else if (isFalling)
            {
                float duration = now - _failObservationRisingEdgeRealtime;
                UnityEngine.Debug.LogError(
                    $"[MicVoiceIngest] FAIL_OBSERVATION cleared after {duration:F2}s — flags seen during window: "
                    + BuildFailObservationAccumulatedFlagsSummary());
                ResetFailObservationAccumulatedFlags();
                _failObservationNextLogIntervalSeconds = 0f;
            }
        }

        _prevFailureObserved = FAILURE;
    }

    /// <summary>
    /// OR every currently-true FAIL_* flag into the per-window "ever seen" trackers. Called on
    /// rising edge (captures the initial set) and every sustained-failure frame (captures any flags
    /// that joined the window after the initial trip). Read at falling edge by
    /// <see cref="BuildFailObservationAccumulatedFlagsSummary"/>.
    /// </summary>
    // runs on: main thread (called from LateUpdate during a sustained FAILURE window).
    private void AccumulateFailObservationFlags()
    {
        if (FAIL_AUDIO_CALLBACK_FROZEN) _failSeenAudioCallbackFrozen = true;
        if (FAIL_AUDIO_CALLBACK_RATE_LOW) _failSeenAudioCallbackRateLow = true;
        if (FAIL_AUDIO_CALLBACK_GAP_HIGH) _failSeenAudioCallbackGapHigh = true;
        if (FAIL_AUDIO_LOCK_CONTENTION) _failSeenAudioLockContention = true;
        if (FAIL_IMITONE_NOT_FED) _failSeenImitoneNotFed = true;
        if (FAIL_IMITONE_FEED_RATIO_LOW) _failSeenImitoneFeedRatioLow = true;
        if (FAIL_UNREAD_ZERO_SUSTAINED) _failSeenUnreadZeroSustained = true;
        if (FAIL_INGEST_RING_STALLED) _failSeenIngestRingStalled = true;
        if (FAIL_MONITORING_STARVATION_GROWING) _failSeenMonitoringStarvationGrowing = true;
        if (FAIL_MIC_NOT_READY) _failSeenMicNotReady = true;
        if (FAIL_DB_TEAR_DETECTED) _failSeenDbTearDetected = true;
    }

    // runs on: main thread (called from LateUpdate on the FAILURE rising edge).
    private void ResetFailObservationAccumulatedFlags()
    {
        _failSeenAudioCallbackFrozen = false;
        _failSeenAudioCallbackRateLow = false;
        _failSeenAudioCallbackGapHigh = false;
        _failSeenAudioLockContention = false;
        _failSeenImitoneNotFed = false;
        _failSeenImitoneFeedRatioLow = false;
        _failSeenUnreadZeroSustained = false;
        _failSeenIngestRingStalled = false;
        _failSeenMonitoringStarvationGrowing = false;
        _failSeenMicNotReady = false;
        _failSeenDbTearDetected = false;
    }

    /// <summary>
    /// Falling-edge "flags seen during window" summary — every FAIL_* flag that was ever true at any
    /// point during this rising-to-falling window, OR'd in by <see cref="AccumulateFailObservationFlags"/>.
    /// Distinct from <see cref="BuildFailObservationLogDetail"/>'s current-frame snapshot used by rising
    /// + sustained logs.
    /// </summary>
    // runs on: main thread (called from LateUpdate's FAILURE falling-edge log path). Allocates a
    // string — fine on main thread; this code path runs once per FAILURE window edge.
    private string BuildFailObservationAccumulatedFlagsSummary()
    {
        return
            (_failSeenAudioCallbackFrozen ? "FAIL_AUDIO_CALLBACK_FROZEN " : "")
            + (_failSeenAudioCallbackRateLow ? "FAIL_AUDIO_CALLBACK_RATE_LOW " : "")
            + (_failSeenAudioCallbackGapHigh ? "FAIL_AUDIO_CALLBACK_GAP_HIGH " : "")
            + (_failSeenAudioLockContention ? "FAIL_AUDIO_LOCK_CONTENTION " : "")
            + (_failSeenImitoneNotFed ? "FAIL_IMITONE_NOT_FED " : "")
            + (_failSeenImitoneFeedRatioLow ? "FAIL_IMITONE_FEED_RATIO_LOW " : "")
            + (_failSeenUnreadZeroSustained ? "FAIL_UNREAD_ZERO_SUSTAINED " : "")
            + (_failSeenIngestRingStalled ? "FAIL_INGEST_RING_STALLED " : "")
            + (_failSeenMonitoringStarvationGrowing ? "FAIL_MONITORING_STARVATION_GROWING " : "")
            + (_failSeenMicNotReady ? "FAIL_MIC_NOT_READY " : "")
            + (_failSeenDbTearDetected ? "FAIL_DB_TEAR_DETECTED " : "");
    }

    /// <summary>
    /// One-line current-frame snapshot for soak / player logs while <see cref="FAILURE"/> is TRUE.
    /// Called on the rising edge AND on every periodic re-log while sustained — so the message always
    /// reflects what's happening RIGHT NOW (which flags are firing, current rolling-Hz / max-gap-ms /
    /// ratio / etc.). The falling-edge log uses <see cref="BuildFailObservationAccumulatedFlagsSummary"/>
    /// instead so the soak reader sees every flag that ever fired during the window, not just whatever
    /// happened to be true on the last sustained frame.
    /// </summary>
    // runs on: main thread (called from LateUpdate's FAILURE rising-edge + sustained-re-log paths).
    // Allocates a string — fine on main thread; cadence is rising-edge + exponentially-backed-off
    // re-logs while sustained.
    private string BuildFailObservationLogDetail()
    {
        // Deliberately flat string — runs rarely (FAIL edges + exponentially-backed-off re-log); readability matters more than zero-GC here.
        return
            $"flags: "
            + (FAIL_AUDIO_CALLBACK_FROZEN ? "FAIL_AUDIO_CALLBACK_FROZEN " : "")
            + (FAIL_AUDIO_CALLBACK_RATE_LOW ? "FAIL_AUDIO_CALLBACK_RATE_LOW " : "")
            + (FAIL_AUDIO_CALLBACK_GAP_HIGH ? "FAIL_AUDIO_CALLBACK_GAP_HIGH " : "")
            + (FAIL_AUDIO_LOCK_CONTENTION ? "FAIL_AUDIO_LOCK_CONTENTION " : "")
            + (FAIL_IMITONE_NOT_FED ? "FAIL_IMITONE_NOT_FED " : "")
            + (FAIL_IMITONE_FEED_RATIO_LOW ? "FAIL_IMITONE_FEED_RATIO_LOW " : "")
            + (FAIL_UNREAD_ZERO_SUSTAINED ? "FAIL_UNREAD_ZERO_SUSTAINED " : "")
            + (FAIL_INGEST_RING_STALLED ? "FAIL_INGEST_RING_STALLED " : "")
            + (FAIL_MONITORING_STARVATION_GROWING ? "FAIL_MONITORING_STARVATION_GROWING " : "")
            + (FAIL_MIC_NOT_READY ? "FAIL_MIC_NOT_READY " : "")
            + (FAIL_DB_TEAR_DETECTED ? "FAIL_DB_TEAR_DETECTED " : "")
            + "| micExitReason=" + aggMicExitReason
            + $" hzRolling={aggAudioCallbackHzRolling:F1} maxGapMs={aggAudioCallbackMaxGapMsLastSecond:F2}"
            + $" imitone/callback={aggImitoneInputToCallbackRatio:F3} rawLockMisses={aggRawRingReadLockMissTotal}"
            + $" overflowDrops={aggAudioFeedOverflowDroppedTotal} dbMicSnap={aggDbMicrophoneSnapshot:F2}"
            + $" imitoneStateStallFrames={aggMainThreadFramesSinceLastImitoneStateChange}";
    }
}
