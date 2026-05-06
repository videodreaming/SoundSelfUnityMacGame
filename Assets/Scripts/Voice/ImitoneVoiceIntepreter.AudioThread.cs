using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

/// <summary>
/// Audio-thread parallel capture path: <see cref="OnAudioFilterRead"/> feeds a dedicated ring AND, as of Step 3a,
/// is the sole source of <c>imitone.InputAudio</c>. Legacy main-thread mic ingest still ring-writes its own copy and
/// computes <c>_dbMicrophone</c> on main thread (DSP / dB move to audio thread in Step 3b; legacy ingest deleted in 5b).
/// runs on: audio thread — <see cref="OnAudioFilterRead"/> only.
/// </summary>
public partial class ImitoneVoiceIntepreter
{
    [Header("Audio-thread capture (Step 1 — parallel path)")]
    [SerializeField] private int audioCallbackPrimingFramesToSkip = 8;
    [SerializeField] private float audioCallbackGcSuspectMsThreshold = 3f;

    // Step 3a F1 fix (DEPRECATED — superseded by Step 3a hybrid pivot; see Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md).
    // No longer read at runtime. Field kept for one pass so Inspector values aren't lost; will be deleted in pass 3.
    [Tooltip("DEPRECATED — replaced by audioThreadFeedLatencyMs in the Step 3a hybrid pivot (Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md). Kept for one pass so existing Inspector values aren't silently dropped; not read at runtime. Removed in pass 3.")]
    [SerializeField, Range(1, 6)] private int audioCapturePlayAlignmentBufferCount = 3;

    // Step 3a hybrid pivot — see Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md.
    // Imitone-feed latency: the audio-thread read cursor is primed this far behind the live rawRingBuffer
    // write head and stays there. Higher = more headroom for main-thread write-jitter bursts; lower =
    // faster pitch response. Floor 32 ms (≈ 1.5 callback periods at 1024 sa / 48 kHz; below that the
    // cursor underruns at the slightest writer hesitation). Default 64 ms gives ~3 callback periods of
    // headroom. Takes effect on the next capture session — does NOT live-retune mid-session.
    [Tooltip("Latency budget for the audio-thread imitone feed. The cursor is primed this far behind the live mic write head and advances at audio-thread cadence. Higher = more robust against main-thread write bursts; lower = faster pitch response. Floor 32 ms (≈ 1.5 callback periods at 1024 sa / 48 kHz). Default 64 ms is the sweet spot for sustained voice. Takes effect on next capture session.")]
    [SerializeField, Range(32f, 250f)] private float audioThreadFeedLatencyMs = 64f;

    // Step 3a: imitone is fed from OnAudioFilterRead; reusable buffer sized to current callback's `frames`.
    // Allocations are amortized — frames is constant within a session (= dspBufferSize), so realloc only on
    // audio config change. imitone.InputAudio uses audio.Length as sample count, so we MUST pass an array
    // of exactly `frames` length, not a larger scratch.
    private float[] imitoneFeedBuffer = Array.Empty<float>();

    // Step 3a hybrid pivot: ring-read cursor used by OnAudioFilterRead to pull the imitone feed from
    // rawRingBuffer (main-thread writer, audio-thread reader, TryEnter-safe via the existing 4-arg
    // ReadRawSamples). Primed once per capture session in WaitMicPositionThenPlayCapture; advances at
    // audio-thread cadence. -1 sentinel = not yet primed; ReadRawSamples auto-snaps to the live write head
    // in that case (zero latency, recovered on the next prime).
    private int audioThreadFeedReadPosition = -1;
    private long audioThreadFeedReadTotalSamples;

    // Step 3a hybrid pivot: counts samples dropped by the ring-read overflow guard inside ReadRawSamples
    // (the consumer fell more than ~250 ms behind the producer; the helper skips ahead and reports the
    // drop). Should stay 0 in normal play; sustained increments mean the audio thread starved or the
    // main-thread writer burst-paused for an unusually long stretch.
    private long audioFeedOverflowDroppedTotal;

    // Step 3a hybrid pivot: silent in-memory clip the captureSource plays just to keep OnAudioFilterRead
    // firing at audio-thread cadence. stream:false is critical — a streaming clip would re-engage the
    // FMOD ~100-DSP-buffer scheduling lookahead policy that this pivot is designed to escape. The clip's
    // contents are zeros; the final Array.Clear(data, 0, data.Length) at the end of OnAudioFilterRead
    // keeps speakers silent regardless.
    private AudioClip imitoneFeedDummyClip;

    // Step 3a: deferred-log channel for imitone.InputAudio exceptions. The audio thread MUST NOT call
    // Debug.Log* directly — string-interpolation alloc would trip audioCallbackGCAllocSuspectTotal and
    // latch FAIL_AUDIO_GC_ALLOC_DETECTED as a false positive. Instead, the audio thread stores the first
    // exception reference (no formatting) and the main thread drains + logs once per session.
    // imitoneInputAudioMainThreadLogged is the latch: once main thread logs, audio thread stops capturing.
    private Exception imitoneInputAudioPendingException;
    private volatile bool imitoneInputAudioMainThreadLogged;

    // Step 3a: counts successful imitone.InputAudio calls from the audio thread.
    private long imitoneInputAudioCallTotal;
    // Step 3a: counts ring writes skipped because they would overrun the consumer position.
    // Distinct from audioCallbackLockMissTotal (lock contention). Never increments in 3a (no consumer of
    // the audio-thread ring yet); placeholder for Step 5b when the legacy mic-ingest path is retired.
    private long micRingOverflowSkipTotal;

    // Step 3a debug (see Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md): peak absolute amplitude of the
    // mono buffer just before it's handed to imitone.InputAudio. Tells us whether the audio thread is
    // receiving voice or silence — independent of imitone's own analysis. Volatile single-writer (audio
    // thread) / single-reader (main thread); torn-read isn't worth defending against for a diagnostic
    // value the user reads with their eyes off an Inspector.
    private volatile float audioCallbackFeedPeakAbsVolatile;

    // Step 3a debug: AudioSource state surfaced for the H1a / H1b / H1c discrimination test. See
    // Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md (decision tree). Snapshotted at last bootstrap; main
    // thread also reads captureSource.isPlaying live each LateUpdate.
    private int audioSourceCountOnGameObjectAtBootstrap;
    public int AudioSourceCountOnGameObjectAtBootstrap => audioSourceCountOnGameObjectAtBootstrap;
    public bool CaptureSourceIsPlaying => captureSource != null && captureSource.isPlaying;
    public bool CaptureSourceHasClip => captureSource != null && captureSource.clip != null;

    // Step 3a debug F1 (imitone-feed latency). The AudioSource read position vs the mic write
    // position gap is the end-to-end imitone-feed latency: samples between "now at the mic"
    // and "now reaching imitone." Both are clip-position offsets modulo clip length. Read on
    // main thread (LateUpdate); both Unity APIs are main-thread-only.
    public int CaptureSourceTimeSamples => captureSource != null ? captureSource.timeSamples : -1;
    public int MicrophoneWritePositionSamples =>
        string.IsNullOrEmpty(microphoneDeviceName) ? -1 : Microphone.GetPosition(microphoneDeviceName);
    /// <summary>
    /// Imitone-feed latency in samples: (mic write position) - (AudioSource read position),
    /// modulo clip length. Returns -1 if either input is invalid. Multiplied by 1000 / sampleRate
    /// gives milliseconds. See <see cref="CaptureToMicGapMs"/>.
    /// </summary>
    public int CaptureToMicGapSamples
    {
        get
        {
            if (captureSource == null || string.IsNullOrEmpty(microphoneDeviceName)) return -1;
            AudioClip clip = captureSource.clip;
            if (clip == null || clip.samples <= 0) return -1;
            int read = captureSource.timeSamples;
            int write = Microphone.GetPosition(microphoneDeviceName);
            if (read < 0 || write < 0) return -1;
            int gap = write - read;
            int clipLen = clip.samples;
            if (gap < 0) gap += clipLen;          // wrap into [0, clipLen)
            if (gap >= clipLen) gap -= clipLen;
            return gap;
        }
    }
    public float CaptureToMicGapMs
    {
        get
        {
            int gap = CaptureToMicGapSamples;
            if (gap < 0 || audioConfigOutputSampleRate <= 0) return -1f;
            return gap * 1000f / audioConfigOutputSampleRate;
        }
    }

    // Step 3a prep: track which capture epoch the audio-thread path was bootstrapped against, so a mic
    // recovery (which ticks captureEpoch via StartMicrophoneCapture) triggers a rebootstrap.
    private int audioThreadLastBootstrappedCaptureEpoch = -1;

    private AudioSource captureSource;
    private Coroutine audioCaptureStartCoroutine;

    private int audioConfigOutputSampleRate;
    private int audioConfigDspBufferSize;
    private AudioSpeakerMode audioConfigSpeakerMode;

    private float[] monoScratch = Array.Empty<float>();
    private float[] audioThreadRing = Array.Empty<float>();
    private readonly object audioRingWriteLock = new object();
    private int audioRingWritePosition;
    private long audioRingWriteTotalSamples;

    private int audioCallbackPrimingFramesRemaining;

    private long audioCallbackTotal;
    private long audioCallbackSamplesProcessedTotal;
    private volatile int audioCallbackLastSamplesPerCallback;
    private volatile int aggMixerChannelsVolatile;
    private long audioCallbackLockMissTotal;
    private long audioCallbackGCAllocSuspectTotal;

    private volatile int audioRingWriteLastClipReadStart;
    private volatile int audioRingWriteLastClipReadCount;

    private int aggMicClipChannelsCached;

    private volatile float audioCallbackHzRollingVolatile;
    private volatile float audioCallbackMaxGapMsLastSecondVolatile;

    // Audio-thread-only: previous callback's Stopwatch ticks. Audio thread is the sole writer/reader.
    private long audioCallbackLastTicks;
    // Cross-thread max-gap accumulator (ticks). Audio thread updates via CAS-max; main thread reads-and-resets via Interlocked.Exchange every ~1s.
    private long audioCallbackMaxGapTicksWindow;

    // Main-thread-only: 1-second sliding window for Hz computation.
    private float _hzWindowStartTimeUnscaled = -1f;
    private long _hzWindowStartTotal;

    public int AudioConfigOutputSampleRate => audioConfigOutputSampleRate;
    public int AudioConfigDspBufferSize => audioConfigDspBufferSize;
    public AudioSpeakerMode AudioConfigSpeakerMode => audioConfigSpeakerMode;
    public int AggMicClipChannelsCached => aggMicClipChannelsCached;

    public AudioThreadHealthSnapshot GetAudioThreadHealthSnapshot()
    {
        return new AudioThreadHealthSnapshot
        {
            audioCallbackTotal = Interlocked.Read(ref audioCallbackTotal),
            audioCallbackSamplesProcessedTotal = Interlocked.Read(ref audioCallbackSamplesProcessedTotal),
            audioCallbackLastSamplesPerCallback = audioCallbackLastSamplesPerCallback,
            audioCallbackHzRolling = audioCallbackHzRollingVolatile,
            audioCallbackMaxGapMsLastSecond = audioCallbackMaxGapMsLastSecondVolatile,
            audioCallbackLockMissTotal = Interlocked.Read(ref audioCallbackLockMissTotal),
            audioCallbackGCAllocSuspectTotal = Interlocked.Read(ref audioCallbackGCAllocSuspectTotal),
            audioRingWriteTotalSamples = Interlocked.Read(ref audioRingWriteTotalSamples),
            audioRingWriteLastClipReadStart = audioRingWriteLastClipReadStart,
            audioRingWriteLastClipReadCount = audioRingWriteLastClipReadCount,
            aggMicClipChannels = aggMicClipChannelsCached,
            aggMixerChannels = aggMixerChannelsVolatile,
            audioConfigOutputSampleRate = audioConfigOutputSampleRate,
            audioConfigDspBufferSize = audioConfigDspBufferSize,
            imitoneInputAudioCallTotal = Interlocked.Read(ref imitoneInputAudioCallTotal),
            micRingOverflowSkipTotal = Interlocked.Read(ref micRingOverflowSkipTotal),
            audioCallbackFeedPeakAbsLastCallback = audioCallbackFeedPeakAbsVolatile,
            audioThreadFeedReadTotalSamples = Interlocked.Read(ref audioThreadFeedReadTotalSamples),
            audioFeedOverflowDroppedTotal = Interlocked.Read(ref audioFeedOverflowDroppedTotal),
        };
    }

    private void BootstrapAudioThreadCapturePath()
    {
        if (!MicIngestIsReady || microphoneBuffer == null)
        {
            return;
        }

        AudioConfiguration cfg = AudioSettings.GetConfiguration();
        audioConfigOutputSampleRate = cfg.sampleRate;
        audioConfigDspBufferSize = Mathf.Max(64, cfg.dspBufferSize);
        audioConfigSpeakerMode = cfg.speakerMode;

        int scratchFrames = Mathf.Max(audioConfigDspBufferSize * 2, 8192);
        monoScratch = new float[scratchFrames];

        int ringLen = Mathf.Max(audioConfigOutputSampleRate * 2, micCaptureSampleRate * 2, 8192);
        audioThreadRing = new float[ringLen];
        audioRingWritePosition = 0;
        Interlocked.Exchange(ref audioRingWriteTotalSamples, 0);

        audioCallbackLastTicks = 0;
        Interlocked.Exchange(ref audioCallbackMaxGapTicksWindow, 0);
        _hzWindowStartTimeUnscaled = -1f;
        _hzWindowStartTotal = 0;
        audioCallbackHzRollingVolatile = 0f;
        audioCallbackMaxGapMsLastSecondVolatile = 0f;

        aggMicClipChannelsCached = Mathf.Max(1, microphoneBuffer.channels);

        EnsureCaptureAudioSourceConfigured();

        audioCallbackPrimingFramesRemaining = Mathf.Max(0, audioCallbackPrimingFramesToSkip);

        // Note: imitoneInputAudioMainThreadLogged is intentionally NOT reset on rebootstrap. If imitone
        // started throwing in the previous capture session, the user has already been told once; spamming
        // them again on each recovery would be noise. The counters (aggImitoneInputAudioCallTotal /
        // FAIL_IMITONE_NOT_FED) keep reporting the runtime state regardless.

        if (audioCaptureStartCoroutine != null)
        {
            StopCoroutine(audioCaptureStartCoroutine);
        }

        audioCaptureStartCoroutine = StartCoroutine(WaitMicPositionThenPlayCapture());

        // Step 3a prep: remember which capture epoch this bootstrap matches so mic recovery can trigger a rebootstrap.
        audioThreadLastBootstrappedCaptureEpoch = captureEpoch;
    }

    /// <summary>
    /// Step 3a prep: detect mic recovery and rebootstrap the audio-thread capture path.
    /// Called every frame from <see cref="MicIngestMainThreadTick"/>; no-op when nothing changed.
    /// Without this, after a mic recovery the AudioSource still points at a destroyed AudioClip and
    /// <see cref="OnAudioFilterRead"/> stops firing — silently killing the audio-thread imitone feed.
    /// </summary>
    private void TryRebootstrapAudioThreadCaptureIfMicRecovered()
    {
        if (imitone == null || !MicIngestIsReady || microphoneBuffer == null)
        {
            return;
        }

        if (captureEpoch == audioThreadLastBootstrappedCaptureEpoch)
        {
            return;
        }

        StopAudioThreadCapture();
        BootstrapAudioThreadCapturePath();
    }

    /// <summary>
    /// Step 3a: drain the deferred imitone.InputAudio exception (if any) and log it from the main thread.
    /// Once-per-session — after the first log, <see cref="imitoneInputAudioMainThreadLogged"/> latches and
    /// the audio-thread catch stops capturing. Called from <see cref="MicIngestMainThreadTick"/>.
    /// </summary>
    private void DrainImitoneInputAudioPendingException()
    {
        if (imitoneInputAudioMainThreadLogged)
        {
            return;
        }

        Exception pending = Interlocked.Exchange(ref imitoneInputAudioPendingException, null);
        if (pending == null)
        {
            return;
        }

        imitoneInputAudioMainThreadLogged = true;
        UnityEngine.Debug.LogWarning(
            $"Step 3a: imitone.InputAudio threw on audio thread (logged once for this session): {pending.GetType().Name}: {pending.Message}");
    }

    private void EnsureCaptureAudioSourceConfigured()
    {
        AudioSource[] existing = GetComponents<AudioSource>();
        if (existing.Length > 1)
        {
            UnityEngine.Debug.LogWarning($"Imitone: Expected at most one AudioSource on {name} before capture setup; found {existing.Length}. Step 1 assumes a single capture source.");
        }

        captureSource = GetComponent<AudioSource>();
        if (captureSource == null)
        {
            captureSource = gameObject.AddComponent<AudioSource>();
        }

        captureSource.loop = true;
        // Step 3a bug — see Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md for the full investigation.
        //
        // Volume = 1f (was 0f originally). The 0f setting was a "make the speakers silent" reflex,
        // but it didn't matter for OnAudioFilterRead — and per the third-test findings the volume
        // value alone doesn't fix the silent-data problem. Speaker silencing is now done by the
        // Array.Clear(data, 0, data.Length) at the end of OnAudioFilterRead, AFTER our filter has
        // captured the real audio.
        captureSource.volume = 1f;
        captureSource.mute = false;
        // bypassEffects MUST be false. A user MonoBehaviour with OnAudioFilterRead is registered as
        // a filter component (Unity inserts it into the source's DSP chain). With bypassEffects=true,
        // the AudioSource's clip data routes AROUND our filter directly to output — OnAudioFilterRead
        // still gets called for bookkeeping, but `data[]` arrives zero-filled because the real audio
        // has been diverted past us. Set false so audio flows THROUGH our filter; we then Array.Clear
        // the buffer at the end to silence the speaker output. (Pre-3a this didn't matter — main-thread
        // ingest fed imitone via Microphone.GetData; the AudioSource was unused. Step 1 added the
        // AudioSource but only counted callbacks, not signal level — bypassEffects being true was
        // dormant/silent until Step 3a actually started reading data[] for imitone.)
        captureSource.bypassEffects = false;
        // bypassListenerEffects governs listener-side effects (AudioListener-attached filters). Safe
        // to keep true — those run after our filter regardless and don't affect what OnAudioFilterRead
        // sees, but bypassing them keeps the captured audio uncolored should one ever be added.
        captureSource.bypassListenerEffects = true;
        captureSource.bypassReverbZones = true;
        captureSource.spatialBlend = 0f;
        captureSource.playOnAwake = false;

        // Step 3a hybrid pivot: captureSource no longer plays the streaming mic clip. It plays a silent
        // in-memory dummy clip just to keep OnAudioFilterRead firing at audio-thread cadence. Imitone is
        // fed from rawRingBuffer inside OnAudioFilterRead via the existing 4-arg ReadRawSamples (TryEnter-
        // safe). Regenerate the dummy clip when audio config changes (sample rate especially), otherwise
        // reuse it across rebootstraps.
        if (audioConfigOutputSampleRate > 0)
        {
            int dummyLength = Mathf.Max(audioConfigDspBufferSize, 1024);
            if (imitoneFeedDummyClip == null
                || imitoneFeedDummyClip.frequency != audioConfigOutputSampleRate
                || imitoneFeedDummyClip.samples != dummyLength)
            {
                if (imitoneFeedDummyClip != null)
                {
                    Destroy(imitoneFeedDummyClip);
                }
                imitoneFeedDummyClip = AudioClip.Create(
                    "ImitoneFeedDummy",
                    dummyLength,
                    1,
                    audioConfigOutputSampleRate,
                    stream: false);
            }
            captureSource.clip = imitoneFeedDummyClip;
        }

        // Snapshot AFTER potential AddComponent so the user-facing diagnostic shows the post-bootstrap
        // count (always ≥ 1 in a healthy Step 3a state). The pre-bootstrap count was ambiguous: 0 is
        // valid (we'll create one) but indistinguishable from "AudioSource was deleted between sessions."
        audioSourceCountOnGameObjectAtBootstrap = GetComponents<AudioSource>().Length;
    }

    private IEnumerator WaitMicPositionThenPlayCapture()
    {
        // Step 3a hybrid pivot — see Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md.
        // Old behavior (pre-pivot): waited for Microphone.GetPosition > 0, assigned the streaming mic clip
        // to captureSource, called Play(), then tried to align captureSource.timeSamples behind the write
        // head. The audio engine snapped that gap back to ~100 DSP buffers within one frame regardless.
        //
        // New behavior: captureSource.clip is the silent dummy clip (set in EnsureCaptureAudioSourceConfigured).
        // We wait until rawRingBuffer holds at least audioThreadFeedLatencyMs + one DSP buffer of samples,
        // prime the audio-thread read cursor that far behind the write head, and Play immediately.
        // No yield between prime and Play — keeps the first OnAudioFilterRead from running with an
        // uninitialized cursor.
        if (audioConfigOutputSampleRate <= 0 || captureSource == null)
        {
            audioCaptureStartCoroutine = null;
            yield break;
        }

        int requiredFill = Mathf.RoundToInt(audioConfigOutputSampleRate * (audioThreadFeedLatencyMs / 1000f))
                           + Mathf.Max(0, audioConfigDspBufferSize);

        // rawWriteTotalSamples is updated by main-thread UpdateMicReadFrame under rawBufferLock; this
        // coroutine runs on the main thread too, so the read is uncontended (writer and reader are the
        // same thread, just different call sites within the same Update tick).
        while (enabled && MicIngestIsReady && microphoneBuffer != null && rawWriteTotalSamples < requiredFill)
        {
            yield return null;
        }

        if (!enabled || !MicIngestIsReady || microphoneBuffer == null || captureSource == null)
        {
            audioCaptureStartCoroutine = null;
            yield break;
        }

        // Prime the cursor under rawBufferLock; the helper computes (rawWritePosition - samplesBehind)
        // mod ringLength against the LIVE write head. Then Play immediately — same coroutine turn, no
        // yield in between.
        bool primed = TryCreateRawReadCursorBehindMs(
            audioThreadFeedLatencyMs,
            out audioThreadFeedReadPosition,
            out audioThreadFeedReadTotalSamples);

        captureSource.Play();

        if (!primed)
        {
            UnityEngine.Debug.LogWarning("[Step3a-pivot] cursor prime failed (rawRingBuffer not allocated). Audio-thread feed will start at the live write head once ring data arrives.");
        }
        else
        {
            long writeTotal = rawWriteTotalSamples;
            long gapSamples = writeTotal - audioThreadFeedReadTotalSamples;
            float gapMs = audioConfigOutputSampleRate > 0
                ? gapSamples * 1000f / audioConfigOutputSampleRate
                : -1f;
            UnityEngine.Debug.Log($"[Step3a-pivot] cursor primed: latencyTarget={audioThreadFeedLatencyMs:F1}ms, readPos={audioThreadFeedReadPosition}, readTotal={audioThreadFeedReadTotalSamples}, writeTotal={writeTotal}, gap={gapSamples}sa (~{gapMs:F1}ms), ringFill={writeTotal}sa");
        }

        audioCaptureStartCoroutine = null;
    }

    public float GetExpectedAudioCallbackHz()
    {
        if (audioConfigDspBufferSize <= 0 || audioConfigOutputSampleRate <= 0)
        {
            return 0f;
        }

        return audioConfigOutputSampleRate / (float)audioConfigDspBufferSize;
    }

    private void UpdateAudioThreadHealthOnMainThread()
    {
        // 1-second sliding window. Hz comes from total-count delta over wall-clock delta.
        // Max-gap comes from the audio-thread CAS-max accumulator, drained here once per window.
        float now = Time.unscaledTime;
        long currentTotal = Interlocked.Read(ref audioCallbackTotal);

        if (_hzWindowStartTimeUnscaled < 0f)
        {
            _hzWindowStartTimeUnscaled = now;
            _hzWindowStartTotal = currentTotal;
            Interlocked.Exchange(ref audioCallbackMaxGapTicksWindow, 0);
            audioCallbackHzRollingVolatile = 0f;
            audioCallbackMaxGapMsLastSecondVolatile = 0f;
            return;
        }

        float dt = now - _hzWindowStartTimeUnscaled;
        if (dt >= 1f)
        {
            audioCallbackHzRollingVolatile = (currentTotal - _hzWindowStartTotal) / dt;

            long maxGapTicks = Interlocked.Exchange(ref audioCallbackMaxGapTicksWindow, 0);
            if (maxGapTicks > 0 && Stopwatch.Frequency > 0)
            {
                audioCallbackMaxGapMsLastSecondVolatile =
                    (float)(maxGapTicks * 1000.0 / Stopwatch.Frequency);
            }
            else
            {
                audioCallbackMaxGapMsLastSecondVolatile = 0f;
            }

            _hzWindowStartTimeUnscaled = now;
            _hzWindowStartTotal = currentTotal;
        }
    }

    private void StopAudioThreadCapture()
    {
        if (audioCaptureStartCoroutine != null)
        {
            StopCoroutine(audioCaptureStartCoroutine);
            audioCaptureStartCoroutine = null;
        }

        if (captureSource != null)
        {
            captureSource.Stop();
            captureSource.clip = null;
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (channels <= 0 || data == null || data.Length == 0)
        {
            return;
        }

        int frames = data.Length / channels;
        if (frames <= 0)
        {
            return;
        }

        Interlocked.Increment(ref audioCallbackTotal);
        Interlocked.Add(ref audioCallbackSamplesProcessedTotal, frames);

        long t0 = Stopwatch.GetTimestamp();

        // Per-callback gap tracking: delta from previous callback in Stopwatch ticks.
        // CAS-max into the window accumulator so the main thread can drain via Interlocked.Exchange.
        long lastTicks = audioCallbackLastTicks;
        audioCallbackLastTicks = t0;
        if (lastTicks != 0L)
        {
            long deltaTicks = t0 - lastTicks;
            if (deltaTicks > 0L)
            {
                long current;
                do
                {
                    current = Interlocked.Read(ref audioCallbackMaxGapTicksWindow);
                    if (deltaTicks <= current)
                    {
                        break;
                    }
                }
                while (Interlocked.CompareExchange(
                           ref audioCallbackMaxGapTicksWindow, deltaTicks, current) != current);
            }
        }

        if (frames > monoScratch.Length)
        {
            Interlocked.Increment(ref audioCallbackGCAllocSuspectTotal);
            Array.Clear(data, 0, data.Length);
            return;
        }

        if (channels == 1)
        {
            Array.Copy(data, 0, monoScratch, 0, frames);
        }
        else
        {
            for (int i = 0; i < frames; i++)
            {
                float sum = 0f;
                int baseIdx = i * channels;
                for (int c = 0; c < channels; c++)
                {
                    sum += data[baseIdx + c];
                }

                monoScratch[i] = sum / channels;
            }
        }

        aggMixerChannelsVolatile = channels;
        audioCallbackLastSamplesPerCallback = frames;

        // Capture priming state BEFORE decrement so the imitone feed gate skips this callback if it was
        // still priming on entry (per plan: "do not call imitone.InputAudio while ...PrimingFramesRemaining > 0").
        // Without the snapshot, the very last priming callback (remaining: 1 -> 0) would feed imitone.
        bool stillPrimingThisCallback = audioCallbackPrimingFramesRemaining > 0;
        if (stillPrimingThisCallback)
        {
            audioCallbackPrimingFramesRemaining--;
        }

        bool lockTaken = false;
        try
        {
            lockTaken = Monitor.TryEnter(audioRingWriteLock, 0);
            if (!lockTaken)
            {
                Interlocked.Increment(ref audioCallbackLockMissTotal);
            }
            else
            {
                int ringLen = audioThreadRing.Length;
                if (ringLen > 0)
                {
                    long logicalStart = Interlocked.Read(ref audioRingWriteTotalSamples);
                    audioRingWriteLastClipReadStart = (int)(logicalStart % ringLen);
                    audioRingWriteLastClipReadCount = frames;

                    for (int i = 0; i < frames; i++)
                    {
                        audioThreadRing[audioRingWritePosition] = monoScratch[i];
                        audioRingWritePosition = (audioRingWritePosition + 1) % ringLen;
                    }

                    Interlocked.Add(ref audioRingWriteTotalSamples, frames);
                }
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(audioRingWriteLock);
            }
        }

        // Step 3a hybrid pivot — see Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md.
        // Imitone feed is now pulled from rawRingBuffer (main-thread writer, audio-thread reader, TryEnter-
        // safe) instead of from the data[] streaming-clip path. The captureSource is just driving this
        // callback's cadence with the silent dummy clip; data[] / monoScratch above are dormant in 3a but
        // kept through pass 2 so the parallel-path telemetry stays comparable.
        //
        // Cursor advance policy: read every callback (so the cursor stays at the configured latency offset
        // behind the write head; not reading would let the gap grow as the write head advances). On
        // priming callbacks we still read+advance, but skip imitone.InputAudio so FMOD startup transients
        // don't reach the analyzer. On lock miss / empty ring (copied == 0) we also skip — feeding a full
        // callback of zeros would be a sharper discontinuity than missing one analyzer tick (per imitone.cs:80).
        if (imitone != null && frames > 0)
        {
            if (imitoneFeedBuffer.Length != frames)
            {
                // Realloc only on dsp buffer size change (rare — typically once at bootstrap / audio config change).
                // Subsequent callbacks reuse the same array — no per-callback alloc on the audio thread.
                imitoneFeedBuffer = new float[frames];
            }

            int copied = ReadRawSamples(
                imitoneFeedBuffer,
                ref audioThreadFeedReadPosition,
                ref audioThreadFeedReadTotalSamples,
                out int overflowDropped);

            if (overflowDropped > 0)
            {
                Interlocked.Add(ref audioFeedOverflowDroppedTotal, overflowDropped);
            }

            // Peak-abs telemetry — measured on what we'd feed to imitone, regardless of priming. If this
            // stays ~0 while _dbMicrophone moves on voice, the ring isn't being filled (mic-ingest issue);
            // if it matches voice amplitude (~0.05–0.5) and imitone STILL reports power=0, the bug is
            // imitone-side, not feed-side.
            float peakAbs = 0f;
            for (int i = 0; i < frames; i++)
            {
                float a = imitoneFeedBuffer[i];
                if (a < 0f) a = -a;
                if (a > peakAbs) peakAbs = a;
            }
            audioCallbackFeedPeakAbsVolatile = peakAbs;

            if (!stillPrimingThisCallback && copied > 0)
            {
                try
                {
                    imitone.InputAudio(imitoneFeedBuffer);
                    Interlocked.Increment(ref imitoneInputAudioCallTotal);
                }
                catch (Exception ex)
                {
                    // Swallow + defer. Audio thread CAS-publishes the first exception reference (no alloc — the
                    // exception object was already allocated by the throw). Main thread drains via
                    // DrainImitoneInputAudioPendingException(). Once main thread has logged,
                    // imitoneInputAudioMainThreadLogged latches true and we stop capturing further exceptions
                    // (no point — main thread won't log again).
                    if (!imitoneInputAudioMainThreadLogged)
                    {
                        Interlocked.CompareExchange(ref imitoneInputAudioPendingException, ex, null);
                    }
                }
            }
        }

        long t1 = Stopwatch.GetTimestamp();
        double elapsedMs = (t1 - t0) * 1000.0 / Stopwatch.Frequency;
        if (elapsedMs > audioCallbackGcSuspectMsThreshold)
        {
            Interlocked.Increment(ref audioCallbackGCAllocSuspectTotal);
        }

        Array.Clear(data, 0, data.Length);
    }
}
