using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

/// <summary>
/// Audio-thread parallel capture path (Step 1): <see cref="OnAudioFilterRead"/> feeds a dedicated ring.
/// Legacy main-thread mic ingest continues to drive imitone until Step 3.
/// runs on: audio thread — <see cref="OnAudioFilterRead"/> only.
/// </summary>
public partial class ImitoneVoiceIntepreter
{
    // STRESS TEST (Step 2) — REMOVE IN SAME COMMIT (as removal pass)
    private long stressAudioThreadInputAudioCallTotal;
    private long stressAudioThreadInputAudioFailureTotal;
    private long stressMainThreadGetStateCallTotal;
    private long stressMainThreadGetStateFailureTotal;
    private bool stressInputAudioExceptionLogged;
    private bool stressGetStateExceptionLogged;

    [Header("Audio-thread capture (Step 1 — parallel path)")]
    [SerializeField] private int audioCallbackPrimingFramesToSkip = 8;
    [SerializeField] private float audioCallbackGcSuspectMsThreshold = 3f;

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

        if (audioCaptureStartCoroutine != null)
        {
            StopCoroutine(audioCaptureStartCoroutine);
        }

        audioCaptureStartCoroutine = StartCoroutine(WaitMicPositionThenPlayCapture());
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
        captureSource.volume = 0f;
        captureSource.mute = false;
        captureSource.bypassEffects = true;
        captureSource.bypassListenerEffects = true;
        captureSource.bypassReverbZones = true;
        captureSource.spatialBlend = 0f;
        captureSource.playOnAwake = false;
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

        if (audioCallbackPrimingFramesRemaining > 0)
        {
            audioCallbackPrimingFramesRemaining--;
        }

        // STRESS TEST (Step 2) — REMOVE IN SAME COMMIT (as removal pass)
        // Double-feed imitone from audio thread while main thread still calls InputAudio + GetState (see plan).
        if (enableAudioThreadImitoneFeedStressTest && imitone != null && frames > 0)
        {
            // imitone.InputAudio uses audio.Length as sample count (imitone.cs) — pass exactly `frames` samples.
            // Per-callback GC alloc here is intentional for this temporary manual test only; Step 3+ forbids allocation on this thread.
            float[] stressPass = new float[frames];
            Array.Copy(monoScratch, 0, stressPass, 0, frames);
            try
            {
                imitone.InputAudio(stressPass);
                Interlocked.Increment(ref stressAudioThreadInputAudioCallTotal);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref stressAudioThreadInputAudioFailureTotal);
                if (!stressInputAudioExceptionLogged)
                {
                    stressInputAudioExceptionLogged = true;
                    UnityEngine.Debug.LogWarning($"STRESS TEST (Step 2): imitone.InputAudio threw (logged once): {ex.Message}");
                }
            }
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

        long t1 = Stopwatch.GetTimestamp();
        double elapsedMs = (t1 - t0) * 1000.0 / Stopwatch.Frequency;
        if (elapsedMs > audioCallbackGcSuspectMsThreshold)
        {
            Interlocked.Increment(ref audioCallbackGCAllocSuspectTotal);
        }

        Array.Clear(data, 0, data.Length);
    }

    // STRESS TEST (Step 2) — REMOVE IN SAME COMMIT (as removal pass)
    public long StressAudioThreadInputAudioCallTotal => Interlocked.Read(ref stressAudioThreadInputAudioCallTotal);
    public long StressAudioThreadInputAudioFailureTotal => Interlocked.Read(ref stressAudioThreadInputAudioFailureTotal);
    public long StressMainThreadGetStateCallTotal => Interlocked.Read(ref stressMainThreadGetStateCallTotal);
    public long StressMainThreadGetStateFailureTotal => Interlocked.Read(ref stressMainThreadGetStateFailureTotal);
}
