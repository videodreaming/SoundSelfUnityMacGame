using System;
using System.Collections;
using System.Collections.Generic;
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
    private volatile float aggMixerChannelsVolatile;
    private long audioCallbackLockMissTotal;
    private long audioCallbackGCAllocSuspectTotal;

    private volatile int audioRingWriteLastClipReadStart;
    private volatile int audioRingWriteLastClipReadCount;

    private int aggMicClipChannelsCached;

    private volatile float audioCallbackHzRollingVolatile;
    private volatile float audioCallbackMaxGapMsLastSecondVolatile;

    private readonly List<(float timeUnscaled, long total)> _hzHistory = new List<(float, long)>(128);

    private long _lastSeenAudioCallbackTotalHz = -1;

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
            aggMixerChannels = Mathf.RoundToInt(aggMixerChannelsVolatile),
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

        _hzHistory.Clear();
        _lastSeenAudioCallbackTotalHz = -1;

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
            Debug.LogWarning($"Imitone: Expected at most one AudioSource on {name} before capture setup; found {existing.Length}. Step 1 assumes a single capture source.");
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
        float now = Time.unscaledTime;
        long tot = Interlocked.Read(ref audioCallbackTotal);
        if (tot != _lastSeenAudioCallbackTotalHz)
        {
            _hzHistory.Add((now, tot));
            _lastSeenAudioCallbackTotalHz = tot;
        }

        _hzHistory.RemoveAll(e => e.timeUnscaled < now - 1f);
        if (_hzHistory.Count >= 2)
        {
            var a = _hzHistory[0];
            var b = _hzHistory[_hzHistory.Count - 1];
            float dt = b.timeUnscaled - a.timeUnscaled;
            if (dt > 1e-4f)
            {
                audioCallbackHzRollingVolatile = (b.total - a.total) / dt;
            }
        }

        float maxGap = 0f;
        for (int i = 1; i < _hzHistory.Count; i++)
        {
            float gapMs = (_hzHistory[i].timeUnscaled - _hzHistory[i - 1].timeUnscaled) * 1000f;
            if (gapMs > maxGap)
            {
                maxGap = gapMs;
            }
        }

        audioCallbackMaxGapMsLastSecondVolatile = maxGap;
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
}
