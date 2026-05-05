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

    // Step 3a: imitone is fed from OnAudioFilterRead; reusable buffer sized to current callback's `frames`.
    // Allocations are amortized — frames is constant within a session (= dspBufferSize), so realloc only on
    // audio config change. imitone.InputAudio uses audio.Length as sample count, so we MUST pass an array
    // of exactly `frames` length, not a larger scratch.
    private float[] imitoneFeedBuffer = Array.Empty<float>();

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

        // Snapshot AFTER potential AddComponent so the user-facing diagnostic shows the post-bootstrap
        // count (always ≥ 1 in a healthy Step 3a state). The pre-bootstrap count was ambiguous: 0 is
        // valid (we'll create one) but indistinguishable from "AudioSource was deleted between sessions."
        audioSourceCountOnGameObjectAtBootstrap = GetComponents<AudioSource>().Length;
    }

    private IEnumerator WaitMicPositionThenPlayCapture()
    {
        while (enabled && MicIngestIsReady && !string.IsNullOrEmpty(microphoneDeviceName))
        {
            int pos = Microphone.GetPosition(microphoneDeviceName);
            if (pos > 0)
            {
                break;
            }

            yield return null;
        }

        if (!enabled || !MicIngestIsReady || microphoneBuffer == null || captureSource == null)
        {
            audioCaptureStartCoroutine = null;
            yield break;
        }

        captureSource.clip = microphoneBuffer;
        captureSource.Play();
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

        // Step 3a: feed imitone from the audio thread, after the ring write and only past the priming window.
        // FMOD's record buffer typically delivers silence/garbage in the first few callbacks — feeding that to
        // imitone teaches the analyzer to lock onto silence and pollutes noise-floor calibration.
        // imitone is fed UNFILTERED mono in 3a (DSP / dB metering still run on main thread); 3b moves filtering
        // ahead of this call. 3a's test bar is "tone tracking responsive," not "tone tracking pre-3a-identical."
        // imitone.InputAudio uses audio.Length as sample count (imitone.cs), so we copy `frames` from monoScratch
        // (which is sized to scratchFrames >= frames) into a feed buffer of EXACTLY `frames` length.
        if (!stillPrimingThisCallback && imitone != null && frames > 0)
        {
            if (imitoneFeedBuffer.Length != frames)
            {
                // Realloc only on dsp buffer size change (rare — typically once at bootstrap / audio config change).
                // Subsequent callbacks reuse the same array — no per-callback alloc on the audio thread.
                imitoneFeedBuffer = new float[frames];
            }

            Array.Copy(monoScratch, 0, imitoneFeedBuffer, 0, frames);

            // Step 3a debug telemetry — peak abs of the buffer about to be fed to imitone. See
            // Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md. If this stays ~0 while _dbMicrophone moves
            // on voice, the audio thread is receiving silence (AudioSource path is broken). If this
            // matches voice amplitude (~0.05–0.5) and imitone STILL reports power=0, the bug is
            // imitone-side, not feed-side. Decision tree in the doc.
            float peakAbs = 0f;
            for (int i = 0; i < frames; i++)
            {
                float a = imitoneFeedBuffer[i];
                if (a < 0f) a = -a;
                if (a > peakAbs) peakAbs = a;
            }
            audioCallbackFeedPeakAbsVolatile = peakAbs;

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

        long t1 = Stopwatch.GetTimestamp();
        double elapsedMs = (t1 - t0) * 1000.0 / Stopwatch.Frequency;
        if (elapsedMs > audioCallbackGcSuspectMsThreshold)
        {
            Interlocked.Increment(ref audioCallbackGCAllocSuspectTotal);
        }

        Array.Clear(data, 0, data.Length);
    }
}
