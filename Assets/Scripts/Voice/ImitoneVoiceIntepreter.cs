using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;
using B83.MathHelpers;
using System.Text.RegularExpressions;
using Defective.JSON;

using imitone;
//use this to translate the voice intepreter stuff into imitone
//copy functions from voiceinterpreter to here.

//TODO
//Why is flooredsemitone floored and not rounded?

// Threading note (5b-iii): every method declared in THIS partial file (ImitoneVoiceIntepreter.cs)
// runs on: main thread. Unity lifecycle (Start / Update / LateUpdate), tone-active telemetry,
// noise-floor coroutines, breath-volume coroutines, Wwise calls — all main-thread. Audio-thread
// code lives in ImitoneVoiceIntepreter.AudioThread.cs (OnAudioFilterRead + filter helpers).
// Mic-ingest producer + ring-buffer accessors live in ImitoneVoiceIntepreter.MicIngest.cs (mostly
// main-thread; the 4-arg ReadRawSamples / ReadNormalizedSamples are the explicit "both threads"
// boundary). Cross-thread shared state is documented field-by-field via the
// aggCrossThreadFieldsUsingVolatile / aggCrossThreadFieldsUsingInterlocked label strings on
// MicVoiceIngestDebugAggregate.
// 5b-v: [DefaultExecutionOrder(50)] removed — overridden by Project Settings → Script Execution Order
// at -104 (engineer-verified 2026-05-08), so the attribute was misleading dead metadata. The Project
// Settings entry is the binding source.
public partial class ImitoneVoiceIntepreter : MonoBehaviour
{
    //base variables pitch and midiNote
    public LightControl lightControl;
    public Director director;
    public float pitch_hz = 0f;
    private const double A4 = 440.0; //Reference Frequency
    public float note_st = 0f;
    public float _dbThreshold;
    // Are we using this action? Robin doesn't understand how an action works.
    public Action<float> OnNewTone;
    public bool gameOn = false;
    private bool gameOnLastFrame = true;

    [Tooltip("imitoneActive when toning.")]
    public bool imitoneActive { get; private set; } = false;
    public bool imitoneActiveRaw { get; private set; } = false;

    [Tooltip("Toning With False Positive Logic")]
    private bool toneActiveBiasTrueLastFrame = false;
    private bool toneActiveConfidentLastFrame = false;
    public bool toneActive { get; private set; } = false;
    public int toneActiveCounter { get; private set; } = 0;
    public bool toneActiveRaw { get; private set; } = false;
    public bool toneActiveFrame { get; private set; } = false;
    public bool toneActiveConfident { get; private set; } = false;
    public bool toneActiveConfidentFrame { get; private set; } = false;
    public int toneActiveConfidentCounter { get; private set; } = 0;
    public bool toneActiveBiasTrue { get; private set; } = false;   //combines toneActive & toneActiveConfident
    public float toneActiveBiasTrueTimer = 0f;
    public bool toneActiveBiasTrueFrame { get; private set; } = false;
    private bool toneActiveBiasTrueFrameFlag = false;
    public bool toneActiveVeryConfident { get; private set; } = false;
    public bool toneActiveVeryConfidentRaw { get; private set; } = false;
    [Header("Tone-Active Telemetry (Inspector)")]
    [SerializeField] private bool telemetryToneActive = false;
    [SerializeField] private bool telemetryToneActiveRaw = false;
    [SerializeField] private bool telemetryToneActiveConfident = false;
    [SerializeField] private bool telemetryToneActiveVeryConfident = false;
    [SerializeField] private bool telemetryToneActiveBiasTrue = false;
    public float positiveActiveThreshold1 { get; private set; } = 0.05f; //for toneActive 
    public float positiveActiveThreshold2 { get; private set; } = 0.2f; //for toneActiveConfident
    public float negativeActiveThreshold1 { get; private set; } = 0.2f; //for toneActive
    public float negativeActiveThreshold2 { get; private set; } = 0.4f; //for toneActiveConfident
    public float _activeThreshold3 { get; private set; } = 0.75f; //positive and negative are the same... used for respiration rate (toneActiveVeryConfident)
    //public float _activeThreshold4 { get; private set; } = 7.0f; //positive and negative are the same... used for respiration rate (toneActiveVeryConfident)
    public bool exceptionFlag = false;

    //TODO: using these vars
    public float ssVolume { get; private set; }     //WORK ON THIS ONE IN GAMEVALUES
    private float _imitoneActiveRawTimer = 0f;
    public float _imitoneInactiveRawTimer = 0f;
    private float _imitoneActiveTimer;
    private float _imitoneInactiveTimer;
    public float _tThisTone;

    public float _tSessionToneActive { get; private set; } = 0f;
    public float _tThisToneRaw;

    [SerializeField]
    public float _tThisToneConfident;

    public float _tThisToneBiasTrue;
    public float _tThisRest;
    public float _tThisRestRaw;

    [SerializeField]
    public float _tThisRestConfident;

    //BREATH
    [SerializeField] private float _breathHoldTimeBeforeInhale;
    //public float _inhaleDuration;
    //Both Above is the duration of breath 
    public float _tNextInhaleDuration = 0.0f;
    public float _breathVolume;
    private bool resetToneFrame = false; //the first frame that !toneActive && !imitoneActive, before toneActive is true again.
    private bool endBreathVolumesRequested = false; //THIS SYSTEM CAN DEFINITELY BE CLEANED UP QUITE EASILY...
    private bool breathSoundFlag = false;

    //public int MostRecentSemitone => _semitone;
    //public string MostRecentSemitoneNote => _semitoneNote;
    //private int _semitone;
    //private string _semitoneNote;
    //private int[] _mostRecentSemitone = new []{-1,-1};
    //private int[] _previousSemitone = new []{-1,-1};

   

    [Header("DampingValues")]
    public float _harmonicity = 0.0f;
    private float _rmsValue;
    [SerializeField] public float _dbValue = -80.0f; //this seems to be the db of the mic while toning
    // Step 3b (Docs/MIC_VOICE_INGEST_FIX_PLAN.md § Step 3b / V7): _dbMicrophone is written from the
    // audio thread (OnAudioFilterRead, post-filter) and read from main thread / Inspector. `volatile`
    // is the V7 default — guarantees the main-thread read sees the latest committed value and prevents
    // compiler reordering. Tear detection (aggDbMicrophoneTearDetectedTotal) sanity-checks the value
    // on every LateUpdate; if it ever fires, escalate to Interlocked.Exchange via SingleToInt32Bits.
    [SerializeField] public volatile float _dbMicrophone = -999.0f;
    [SerializeField] public float _timbre = 0.0f;
    [SerializeField] public float _level;
    private const int SAMPLE_SIZE = 1024;

    private string _selectedDevice;
    private int _sampleRate;
    private readonly float _referenceAmplitude = 20.0f * Mathf.Pow(10.0f, -6.0f);
    [SerializeField] private float _pitchDifference = 3;

    private Dictionary<int, float> _breathVolumeContributions = new Dictionary<int, float>();
    private int _coroutineCounter = 0; // To generate unique keys

    [TextAreaAttribute(8, 8)] public string imitoneState;

    [Header("dbController")]

    //NoiseFloor
    [Header("Noise Floor")]
    //A dictionary that stores a float for the dbValue each frame, and the time that frame was recorded.
    private Dictionary<int, (float, float)> rawMic = new Dictionary<int, (float, float)>();
    private Dictionary<int, (float, float)> noiseMeasurements = new Dictionary<int, (float, float)>();
    private readonly List<int> _rawMicKeysToRemove = new List<int>();
    private bool expectNoiseFloor = false; //NOT YET IMPLEMENTED, use this when the system is programmatically expecting noise.
    private bool noiseFloorFlag = false;
    [SerializeField] private float _volumeChangeMeasurementWindow = 0.3f;
    [SerializeField] private float _volumeDropTriggerThresholdDB = 7f;
    [SerializeField] private float _volumeJumpTriggerThresholdDB = 12f;
    [SerializeField] private float _afterDropWaitTime = 0.5f;
    private float _afterDropWaitTimer = 0f;
    [SerializeField] private float _noiseFloorMeasurementTime = 1.5f;
    [SerializeField] private int noiseFloorMeasurementMaxAge = 120;
    private int uniqueKey = 0;
    [SerializeField] private float _thresholdAboveNoiseFloor = 3f;
    [SerializeField] public float _noiseFloorThreshold = -52.0f;
    [Header("Noise Floor Runtime Telemetry")]
    [SerializeField] public bool micIsNearNoiseFloor = false;
    [SerializeField] private bool telemetryNoiseFloorImitoneActiveRaw = false;
    [SerializeField] [Range(-80f, -12f)] private float telemetryJumpTriggerDb = -80f;
    [SerializeField] [Range(-80f, -12f)] private float telemetryDropExitDb = -80f;
    [SerializeField] private bool telemetryJumpTriggerMet = false;
    [SerializeField] private bool telemetryNoiseFloorCoroutineRunning = false;
    [SerializeField] private string telemetryNoiseFloorPhase = "idle";
    [SerializeField] [Range(-80f, -12f)] private float telemetryNoiseFloorMeasuredPeakDb = -80f;
    [SerializeField] private float telemetryNoiseFloorMeasurementElapsed = 0f;
    [SerializeField] [Range(-80f, -12f)] private float telemetryNoiseFloorMeasurementAverageDb = -80f;
    [SerializeField] private int telemetryNoiseMeasurementsCount = 0;
    [SerializeField] [Range(-80f, -12f)] private float telemetryMedianNoiseFloorDb = -80f;
    [SerializeField] [Range(-80f, -12f)] private float telemetryAppliedThresholdDb = -52f;
    [SerializeField] [Range(-80f, -12f)] private float telemetryMicDb = -80f;
    [SerializeField] [Range(-80f, -12f)] private float telemetryImitoneDb = -80f;
    [Tooltip("Raw _dbMicrophone (not clamped to -80..-12). Use to see rail vs real movement.")]
    [SerializeField] private float telemetryMicDbUnclamped = -999f;
    [Tooltip("Raw _dbValue (not clamped to -80..-12). Use to see rail vs real movement.")]
    [SerializeField] private float telemetryImitoneDbUnclamped = -999f;
    // Step 3b: legacy raw-voice-path diagnostics removed. The main-thread `if (tryCopyOk &&
    // rawSampleCount > 0) { filter; dB; ... }` block was the only writer to these fields and is
    // gone (filter + dB are now audio-thread-only; imitone is fed exclusively from
    // OnAudioFilterRead). Mic-readiness liveness moved into the aggregate's direct read of
    // `IsMicReady`. The legacy main-thread mic-ingest block itself is retired in Step 5b.
    [Header("Tone Gate Runtime Telemetry")]
    [SerializeField] private float telemetryImitoneActiveTimer = 0f;
    [SerializeField] private float telemetryImitoneInactiveTimer = 0f;
    [SerializeField] private float telemetryImitoneActiveRawTimer = 0f;
    [SerializeField] private float telemetryImitoneInactiveRawTimer = 0f;

    [Serializable]
    public struct MicIngestDebugSnapshot
    {
        public string lastExitReason;
        public int lastUnreadComputed;
        public int lastLatestRawSampleCount;
        public int lastMicPosWrite;
        public int lastMicPosRead;
        public int lastStalledWriteHeadFrameCount;
        public int lastClipSamples;
        public int lastUnityFrame;
        public long rawRingWriteTotalSamples;
        public long normalizedRingWriteTotalSamples;
    }

    // Step 3b: snapshot trimmed to the two unclamped-dB telemetry fields (mic dB + imitone dB).
    // The five legacy raw-voice-path booleans were removed along with the main-thread `tryCopyOk`
    // block they tracked; mic-readiness is now read by the aggregate via the public IsMicReady
    // property directly, eliminating the round trip. Renaming this struct is left for Step 5b
    // (along with the legacy main-thread ingest cleanup).
    [Serializable]
    public struct RawVoicePathDebugSnapshot
    {
        public float telemetryMicDbUnclamped;
        public float telemetryImitoneDbUnclamped;
    }

    // runs on: main thread (called from MicVoiceIngestDebugAggregate.LateUpdate). Reads two plain
    // floats; both are written only from main-thread UpdateToneActiveTelemetryInspector.
    public RawVoicePathDebugSnapshot GetRawVoicePathDebugSnapshot()
    {
        return new RawVoicePathDebugSnapshot
        {
            telemetryMicDbUnclamped = telemetryMicDbUnclamped,
            telemetryImitoneDbUnclamped = telemetryImitoneDbUnclamped,
        };
    }

    [Serializable]
    public struct AudioThreadHealthSnapshot
    {
        public long audioCallbackTotal;
        public long audioCallbackSamplesProcessedTotal;
        public int audioCallbackLastSamplesPerCallback;
        public float audioCallbackHzRolling;
        public float audioCallbackMaxGapMsLastSecond;
        public long audioCallbackGCAllocSuspectTotal;
        public int aggMicClipChannels;
        public int aggMixerChannels;
        public int audioConfigOutputSampleRate;
        public int audioConfigDspBufferSize;
        // Step 3a: imitone-feed observability.
        public long imitoneInputAudioCallTotal;
        // 5b-iv: micRingOverflowSkipTotal field retired from this snapshot — see AudioThread.cs note;
        // counter never incremented (F1 hybrid moved overflow protection to the read side as
        // audioFeedOverflowDroppedTotal, which is still here below).
        // Step 3a debug (see Docs/STEP_3A_BUG_IMITONE_NON_RESPONSIVE.md): peak abs of the mono buffer
        // OnAudioFilterRead is about to feed imitone. Used to discriminate "feed is silent" from
        // "feed is voice but imitone isn't pitching."
        public float audioCallbackFeedPeakAbsLastCallback;
        // Step 3a hybrid pivot (see Docs/STEP_3A_F1_HYBRID_RING_FEED_PLAN.md): logical read cursor into
        // rawRingBuffer on the audio-thread imitone-feed path; cumulative overflow drops from ReadRawSamples.
        public long audioThreadFeedReadTotalSamples;
        public long audioFeedOverflowDroppedTotal;
        // Step 3a Pass 2: live gap (samples) between rawRingBuffer write head and audio-thread feed cursor.
        // Snapshotted under rawBufferLock alongside the cursor read for a coherent pair (gap = writeTotal -
        // readTotal); aggregate converts to ms via audioConfigOutputSampleRate. Persistent target ~40-90 ms.
        public long audioThreadFeedToWriteHeadGapSamples;
        // Step 3a Pass 2: TryEnter(0) misses on rawBufferLock from audio-thread readers (imitone feed +
        // DirectVoiceMonitoring). Drives FAIL_AUDIO_LOCK_CONTENTION (pass 3 repointed it from the now-deleted
        // audioCallbackLockMissTotal on the audioRingWriteLock).
        public long rawRingReadLockMissTotal;
    }

    private Coroutine currentNoiseFloorCoroutine;
    private float latestRawMicWindowMaxDb = -999f;

    //private bool manualMode = false;

    private float UpperThreshold = -20.0f;
    private float LowerThreshold = -35.0f;

    //Volume Tracking
    private List<(float, float)> volumes1s = new List<(float, float)>();
    private List<(float, float)> anomalyBaselineVolumes = new List<(float, float)>();
    private float _volumes1sRollingSum = 0f;
    private float _anomalyBaselineRollingSum = 0f;
    private float _vol1Sec = 0.0f;
    private float _anomalyBaseline = 0.0f;
    private float _timerForAnomalyBaselines = 0.0f;
    private bool _volFlagA = false;
    private float _anomalyBaselineMeasurementTime = 60.0f;
    private float _volumeAnomalyThresholdDb_init = 6.0f;
    private float _volumeAnomalyThresholdDbDecreaseRate = 1.0f;//per minute
    private float _volumeAnomalyThresholdDb;
    //DevMode
    public string imitoneConfig;

    private bool forceImitoneActive = false;
    private bool forceImitoneInactive = false;
    private bool developmentModeWarningFlag = false;

    int sampleRate;
    // Step 3b review-pass follow-up (V7 audit): the audio thread reads `imitone` in
    // OnAudioFilterRead (ImitoneVoiceIntepreter.AudioThread.cs) to call imitone.InputAudio. The
    // single-write-in-Start happens-before-Play synchronization makes a stale read effectively
    // impossible in practice, but `volatile` is the V7 default for any audio-written-or-audio-read
    // managed-reference field and closes the audit item the 3a developer note flagged for 3b.
    // Cost: a memory barrier per dereference (negligible). Field is assigned once in Start(), never
    // reassigned by mic recovery (StopAudioThreadCapture / BootstrapAudioThreadCapturePath leave it
    // alone) — so volatility costs essentially nothing here while making the cross-thread contract
    // explicit + matching the aggCrossThreadFieldsUsingVolatile label string.
    volatile ImitoneVoice imitone;

    // Step 3b: capturedInput retired. The legacy main-thread copy of mic samples (sized to
    // microphoneBuffer.samples * channels) was only used by the main-thread filter+dB+imitone
    // block, which is gone — imitone is fed exclusively from rawRingBuffer on the audio thread,
    // and _dbMicrophone is computed there.

    // Step 3a: imitone is now fed from OnAudioFilterRead (see ImitoneVoiceIntepreter.AudioThread.cs).
    // The chunking workaround for imitone's 1-second feed_buffer is gone — audio-thread callbacks deliver
    // ~21 ms (1024 samples at 48 kHz) per call, well under the 1 s limit, so chunking is unnecessary.
    // Counters below let the aggregate observe imitone-feed health from main thread.
    private long imitoneGetStateCallTotal;
    private int mainThreadFramesSinceLastImitoneStateChange;
    private float _lastImitoneStatePower = float.NaN;
    private float _lastImitoneStatePitchHz = float.NaN;
    private string _lastImitoneStateRaw = null;

    public long ImitoneGetStateCallTotal => imitoneGetStateCallTotal;
    public int MainThreadFramesSinceLastImitoneStateChange => mainThreadFramesSinceLastImitoneStateChange;

    private static bool FloatsEqualOrBothNaN(float a, float b)
    {
        bool aNan = float.IsNaN(a);
        bool bNan = float.IsNaN(b);
        if (aNan && bNan) return true;
        if (aNan != bNan) return false;
        return Mathf.Approximately(a, b);
    }

    // Step 3b: filter SerializeFields are read by the audio thread (HPF + LPF live in
    // OnAudioFilterRead now). `volatile` ensures Inspector edits are picked up by the next
    // audio callback rather than getting cached on the audio thread (it's runtime-tunability
    // insurance — without volatile, the audio thread might cache the value indefinitely).
    // Filter STATE (previous-input / previous-output) lives on the audio thread only — see
    // ImitoneVoiceIntepreter.AudioThread.cs (`audioThreadHpPrevInput`, `audioThreadHpPrevOutput`,
    // `audioThreadLpPrevOutput`).
    [Header("High Pass Filter")]
    [Tooltip("Removes low-frequency rumble (e.g. AC hum, wind) before pitch analysis. 80 Hz is typical for voice. Step 3b: applied on the audio thread, just before imitone.InputAudio.")]
    [SerializeField] private volatile bool _highPassFilterEnabled = true;
    [SerializeField] private volatile float _highPassCutoffHz = 80f;

    [Header("Low Pass Filter")]
    [Tooltip("Removes high-frequency hiss and overtones above voice range. 520 Hz keeps tenor fundamentals. Step 3b: applied on the audio thread, just before imitone.InputAudio.")]
    [SerializeField] private volatile bool _lowPassFilterEnabled = true;
    [SerializeField] private volatile float _lowPassCutoffHz = 520f;

    // Debug log category flags
    private bool debugAllowInitializationLogs = true;
   // private bool debugAllowToneActiveLogs = true;
    private bool debugAllowToneActiveVariationsLogs = false;
    private bool debugAllowSFXLogs = false;
    private bool debugAllowVolumeTrackingLogs = false;
    private bool debugAllowMonitoringLogs = false;
    private bool debugAllowWarnings = false; // Warnings show if this OR the category flag is true
    private bool? _lastToneActiveSwitchState = null;
    private float _lastInhaleRTPCValue = float.NaN;

    // runs on: main thread (Unity lifecycle). Sets `imitone` (the volatile ImitoneVoice reference
    // OnAudioFilterRead reads) once, before BootstrapAudioThreadCapturePath kicks the audio thread.
    void Start()
    {
        _volumeAnomalyThresholdDb = _volumeAnomalyThresholdDb_init;

        InitializeMicrophone();
        if (!MicIngestIsReady)
        {
            if (debugAllowInitializationLogs || debugAllowWarnings)
            {
                Debug.LogError("Imitone: Microphone capture failed to initialize.");
            }
            return;
        }

        sampleRate = micCaptureSampleRate;
        if (debugAllowInitializationLogs)
        {
            Debug.Log("Imitone: Chose microphone: " + MicrophoneDeviceName);
        }

        try
        {
            ImitoneVoice.ActivateLicense("imitone technology used under license to New Entheogen Ltd, March 2023.");
            // Original Settings:      (sampleRate, "{\"guide\":\"off\",\"slide\":\"bend\",\"range\":{\"min\":34.0,\"max\":101.0}}");
            imitone = new ImitoneVoice(sampleRate, "{\"guide\":\"on\",\"slide\":\"bend\",\"range\":{\"min\":34.0,\"max\":88.0},\"volume\":{\"threshold\":-52.0}}"); //threshold of -52 is ideal for Corsair HS80
        }
        catch (System.Exception e)
        {
            if(debugAllowWarnings || debugAllowInitializationLogs)
            {
                Debug.Log(e);
            }
            throw;
        }

        if (imitone == null)
        {
            if(debugAllowInitializationLogs || debugAllowWarnings)
            {
                Debug.LogError("Imitone: imitone was null after creation.");
            }
        }

        BootstrapAudioThreadCapturePath();
    }

    // runs on: main thread (Unity lifecycle). Per-frame voice-path orchestrator; pumps mic ingest,
    // audio-thread health rollup, noise floor, imitone GetState (`GetRawVoiceData`), tone tracking,
    // and Wwise breath SFX.
    void Update()
    {
        MicIngestMainThreadTick();
        UpdateAudioThreadHealthOnMainThread();
        SetNoiseFloorThreshold();
        GetRawVoiceData();
        CheckToning();
        UpdateToneActiveTelemetryInspector();
        TrackMicVolume();
        Wwise_BreathSound(_breathVolume, lightControl._fxWave);

        if (gameOn != gameOnLastFrame)
        {
            if (gameOn)
            {
                if(debugAllowMonitoringLogs)
                {
                    Debug.Log("Imitone: Game On");
                }
            }
            else
            {
                if(debugAllowMonitoringLogs)
                {
                    Debug.Log("Imitone: Game Off");
                }
            }
            gameOnLastFrame = gameOn;
        }
    }

    private void UpdateToneActiveTelemetryInspector()
    {
        telemetryToneActive = toneActive;
        telemetryToneActiveRaw = toneActiveRaw;
        telemetryToneActiveConfident = toneActiveConfident;
        telemetryToneActiveVeryConfident = toneActiveVeryConfident;
        telemetryToneActiveBiasTrue = toneActiveBiasTrue;
        telemetryNoiseFloorImitoneActiveRaw = imitoneActiveRaw;
        telemetryImitoneActiveTimer = _imitoneActiveTimer;
        telemetryImitoneInactiveTimer = _imitoneInactiveTimer;
        telemetryImitoneActiveRawTimer = _imitoneActiveRawTimer;
        telemetryImitoneInactiveRawTimer = _imitoneInactiveRawTimer;
        telemetryMicDb = Mathf.Clamp(_dbMicrophone, -80f, -12f);
        telemetryImitoneDb = Mathf.Clamp(_dbValue, -80f, -12f);
        telemetryMicDbUnclamped = _dbMicrophone;
        telemetryImitoneDbUnclamped = _dbValue;
        telemetryAppliedThresholdDb = Mathf.Clamp(_dbThreshold, -80f, -12f);
        micIsNearNoiseFloor = _dbMicrophone <= _noiseFloorThreshold;
    }

    // runs on: main thread (Unity lifecycle). Runs after the aggregate's LateUpdate (the aggregate
    // sits at the default execution order; this script runs at +50, so its LateUpdate is later in
    // the frame than the aggregate's — but both are main-thread regardless).
    void LateUpdate()
    {
        breathSoundFlag = false;
    }
    private void TrackMicVolume()
    {

        //We are trying to detect when the volume rises up, for the director.
        //This means that right now, the most recent one second has been the loudest of the last 30 seconds, AND the loudest of the last 2 minutes.
        //It will trigger the Director Queue to process its contents.
        //It will also not be able to trigger again for another 30 seconds.
        //first, get the average volume of the last one second

        if (toneActive)
        {
            //RECORD 1S DATA
            volumes1s.Add((Time.time, _dbMicrophone));
            _volumes1sRollingSum += _dbMicrophone;
        }

        if (toneActiveConfident)
        {
            //LOG 1S AVERAGE
            _vol1Sec = volumes1s.Count > 0 ? _volumes1sRollingSum / volumes1s.Count : -1000f;
            _volFlagA = false;

            //RECORD ANOMALY BASELINE DATA
            _timerForAnomalyBaselines += Time.deltaTime;
            if (_timerForAnomalyBaselines >= 0.1f)
            {
                anomalyBaselineVolumes.Add((Time.time, _vol1Sec));
                _anomalyBaselineRollingSum += _vol1Sec;
                _timerForAnomalyBaselines = 0.0f;
            }

        }
        else if (!_volFlagA || !toneActive)
        {
            //CLEAR DATA FROM 1S
            volumes1s.Clear();
            _volumes1sRollingSum = 0f;
            _vol1Sec = -1000.0f;
            _volFlagA = true;
        }

        //CALCULATE ANOMALY BASELINE FROM 1M AVERAGE
        if (anomalyBaselineVolumes.Count > 0)
        {
            _anomalyBaseline = _anomalyBaselineRollingSum / anomalyBaselineVolumes.Count;
        }
        else
        {
            _anomalyBaseline = 0.0f;
        }
        float _secsCapturedBaseline = anomalyBaselineVolumes.Count * 0.1f;

        //Debug.Log("Volume 1s: " + _vol1Sec + " Volume 1m: " + _anomalyBaseline + " Seconds Captured 1m: " + _secsCapturedBaseline + " Anomaly Threshold: " + _volumeAnomalyThresholdDb);


        //DETECT VOLUME ANOMALY
        _volumeAnomalyThresholdDb -= _volumeAnomalyThresholdDbDecreaseRate * Time.deltaTime / 60.0f;
        _volumeAnomalyThresholdDb = Mathf.Max(_volumeAnomalyThresholdDb, 2.0f);
        bool captureThreshold = _secsCapturedBaseline > 15f;
        if (captureThreshold && (_vol1Sec > (_anomalyBaseline + _volumeAnomalyThresholdDb)))
        {
            if(debugAllowVolumeTrackingLogs)
            {
                Debug.Log("Imitone: Director Volume Anomaly Detected: " + _vol1Sec + " > " + _anomalyBaseline + " + " + _volumeAnomalyThresholdDb);
            }
            _volumeAnomalyThresholdDb = _volumeAnomalyThresholdDb_init;

            director.ActivateQueue(1.75f);

            //Clear the anomaly baseline data
            anomalyBaselineVolumes.Clear();
            _anomalyBaselineRollingSum = 0f;
            float highestVolume = volumes1s.Max(x => x.Item2);
            //add in the equivalent of 7.5 seconds of data to the anomaly baseline, at a value equal to the highest value in volumes1s
            for (int i = 0; i < 75; i++)
            {
                anomalyBaselineVolumes.Add((Time.time, highestVolume));
                _anomalyBaselineRollingSum += highestVolume;
            }
        }

        //CLEAR DATA THAT IS TOO OLD
        for (int i = volumes1s.Count - 1; i >= 0; i--)
        {
            if (volumes1s[i].Item1 < Time.time - 1.0f)
            {
                _volumes1sRollingSum -= volumes1s[i].Item2;
                volumes1s.RemoveAt(i);
            }
        }
        for (int i = anomalyBaselineVolumes.Count - 1; i >= 0; i--)
        {
            if (anomalyBaselineVolumes[i].Item1 < Time.time - _anomalyBaselineMeasurementTime) // 60 seconds...
            {
                _anomalyBaselineRollingSum -= anomalyBaselineVolumes[i].Item2;
                anomalyBaselineVolumes.RemoveAt(i);
            }
        }

        if (MusicSystem1.instance != null &&_vol1Sec > -1000.0f)
        {
            MusicSystem1.instance.SetMusicToningLayerVolume(80f + NormalizeVolume(_vol1Sec * 20f), 0f);
        }

        
        //Change the music volume based on the microphone input level

    }

    public float NormalizeVolume(float volume)
    {
        return Mathf.Clamp(Mathf.InverseLerp(-55.0f, -29.0f, volume), 0.0f, 1.0f);
    }

    public float GetNormalizedVolume()
    {
        return NormalizeVolume(_vol1Sec * 40f);
    }


    /// <summary>
    /// Trigger stage of adaptive thresholding:
    /// 1) Maintain a short rolling window of raw mic dB.
    /// 2) Detect a "jump" above the recent local floor.
    /// 3) If jump is detected, start/restart noise-floor measurement coroutine.
    ///
    /// This method DOES NOT directly pick the new threshold; it only decides when
    /// to launch MeasureNoiseFloorCoroutine(), which then computes median noise floor
    /// and applies _noiseFloorThreshold via SetThreshold(...).
    /// </summary>
    private void SetNoiseFloorThreshold() //WE NEED RAW VALUES FOR THIS. CURRENT RAW DEPENDENCIES: _dbMicrophone
    {
        // Keep a short history of recent raw mic dB values for local min/max comparison.
        rawMic.Add(uniqueKey++, (Time.time, _dbMicrophone));
        // Remove entries older than the configured rolling-window duration.
        _rawMicKeysToRemove.Clear();
        foreach (var entry in rawMic)
        {
            if (Time.time - entry.Value.Item1 > _volumeChangeMeasurementWindow)
                _rawMicKeysToRemove.Add(entry.Key);
        }
        foreach (var key in _rawMicKeysToRemove)
        {
            rawMic.Remove(key);
        }

        if (rawMic.Count <= 0)
        {
            float fallbackDb = Mathf.Clamp(_dbMicrophone, -80f, -12f);
            telemetryJumpTriggerDb = Mathf.Clamp(_dbMicrophone + _volumeJumpTriggerThresholdDB, -80f, -12f);
            telemetryDropExitDb = Mathf.Clamp(_dbMicrophone - _volumeDropTriggerThresholdDB, -80f, -12f);
            telemetryJumpTriggerMet = false;
            latestRawMicWindowMaxDb = fallbackDb;
            return;
        }

        // Local window floor/ceiling define two trigger levels:
        // - jumpTriggerDb: "sound rose enough to consider new vocal event"
        // - dropExitDb: level we must fall below before measuring ambient floor
        float windowMinDb = rawMic.Values.Min(x => x.Item2);
        float windowMaxDb = rawMic.Values.Max(x => x.Item2);
        float jumpTriggerDb = windowMinDb + _volumeJumpTriggerThresholdDB;
        float dropExitDb = windowMaxDb - _volumeDropTriggerThresholdDB;
        latestRawMicWindowMaxDb = windowMaxDb;
        telemetryJumpTriggerDb = Mathf.Clamp(jumpTriggerDb, -80f, -12f);
        telemetryDropExitDb = Mathf.Clamp(dropExitDb, -80f, -12f);
        telemetryJumpTriggerMet = _dbMicrophone > jumpTriggerDb;

        // Jump detection gate:
        // if current mic dB jumps enough above local floor, restart noise-floor measurement.
        // Restarting keeps us aligned with the latest event rather than stale pre-event context.
        if (telemetryJumpTriggerMet)
        {
            if (currentNoiseFloorCoroutine != null)
                StopCoroutine(currentNoiseFloorCoroutine);

            currentNoiseFloorCoroutine = StartCoroutine(MeasureNoiseFloorCoroutine());
        }

    }


    private IEnumerator MeasureNoiseFloorCoroutine()
    {
        telemetryNoiseFloorCoroutineRunning = true;
        telemetryNoiseFloorPhase = "waiting_for_drop";
        telemetryNoiseFloorMeasurementElapsed = 0f;
        telemetryNoiseFloorMeasurementAverageDb = -80f;
        float _noiseFloorMeasurementSum = 0f;
        float _noiseFloorMeasurementCount = 0f;
        //Debug.Log("Preparing to Measure Noise Floor...");

        // Phase 1: wait for post-event level to settle back down near floor.
        while (_dbMicrophone >= telemetryDropExitDb)
        {
            yield return null;
        }
        // Phase 2: add an extra hold to avoid capturing the event tail.
        telemetryNoiseFloorPhase = "after_drop_wait";
        float _measuredPeak = latestRawMicWindowMaxDb;
        telemetryNoiseFloorMeasuredPeakDb = Mathf.Clamp(_measuredPeak, -80f, -12f);
        yield return new WaitForSeconds(_afterDropWaitTime);

        // Phase 3: sample candidate ambient floor over a fixed interval.
        telemetryNoiseFloorPhase = "measuring";
        float _measuredTime = 0f;
        while (_measuredTime < _noiseFloorMeasurementTime)
        {
            _noiseFloorMeasurementSum += _dbMicrophone;
            _noiseFloorMeasurementCount++;
            _measuredTime += Time.deltaTime;
            telemetryNoiseFloorMeasurementElapsed = _measuredTime;
            yield return null;
        }

        // Phase 4: store this measurement and compute a robust threshold from history.
        float _noiseFloorMeasurementAverage = _noiseFloorMeasurementSum / _noiseFloorMeasurementCount;
        telemetryNoiseFloorMeasurementAverageDb = Mathf.Clamp(_noiseFloorMeasurementAverage, -80f, -12f);

        noiseMeasurements.Add(uniqueKey++, (Time.time, _noiseFloorMeasurementAverage));
        //Then, if there are entries that are older than noiseFloorMeasurementMaxAge, remove them
        List<int> keysToRemove = new List<int>();
        foreach (var entry in noiseMeasurements)
        {
            if ((Time.time - entry.Value.Item1) > noiseFloorMeasurementMaxAge)
            {
                keysToRemove.Add(entry.Key);
                //Debug.Log("Removing Noise Key " + entry.Key + " with value " + entry.Value.Item2 + " from time " + entry.Value.Item1 + " because it is older than " + noiseFloorMeasurementMaxAge + " seconds.");
            }
        }

        foreach (var key in keysToRemove)
        {
            noiseMeasurements.Remove(key);
        }

        // Median across historical measurements resists one-off spikes better than mean.
        List<float> values = noiseMeasurements.Values.Select(x => x.Item2).OrderBy(x => x).ToList();
        float _medianNoiseFloor = (values.Count % 2 != 0) ?
        values[values.Count / 2] :
        (values[(values.Count - 1) / 2] + values[values.Count / 2]) / 2.0f;
        telemetryMedianNoiseFloorDb = Mathf.Clamp(_medianNoiseFloor, -80f, -12f);
        telemetryNoiseMeasurementsCount = noiseMeasurements.Count;
        _noiseFloorThreshold = _medianNoiseFloor + _thresholdAboveNoiseFloor;
        telemetryAppliedThresholdDb = Mathf.Clamp(_noiseFloorThreshold, -80f, -12f);
        SetThreshold(_noiseFloorThreshold);
        micIsNearNoiseFloor = _dbMicrophone <= _noiseFloorThreshold;
        telemetryNoiseFloorPhase = "applied_threshold";
        //Debug.Log("Noise Floor Measured: " + _noiseFloorMeasurementAverage + " (from peak: " + _measuredPeak + ") New Threshold: " + _noiseFloorThreshold + " from " + noiseMeasurements.Count + " measurements.");
        telemetryNoiseFloorCoroutineRunning = false;
        telemetryNoiseFloorPhase = "idle";
        yield return null;
    }

    private void SetThreshold(float db = -52.5f)
    {
        //Logic that sets the threshold for imitone's dbValue using SetConfig() to the value of dbThreshold
        //False has been removed as the second arguement for imitone.SetConfig to prevent the game from breaking after March 18th 2025 when the dylib is updated for apple silicon arch.
        imitone.SetConfig("{\"volume\" : {\"threshold\" : " + db + "} }");
        imitoneConfig = imitone.GetConfig();
        _dbThreshold = db;
        //Debug.Log("imitone configuration: " + imitoneConfig);  
    }



    // Step 3b: ApplyHighPassFilter / ApplyLowPassFilter retired from the main thread.
    // Audio-thread variants live in ImitoneVoiceIntepreter.AudioThread.cs
    // (ApplyHighPassFilterOnAudioThread / ApplyLowPassFilterOnAudioThread). Filter state
    // (`audioThreadHpPrevInput` / `audioThreadHpPrevOutput` / `audioThreadLpPrevOutput`) lives
    // there too — touched only from OnAudioFilterRead, never from the main thread.

    // runs on: main thread (called from Update). NOTE: this is the only main-thread call into
    // `imitone.*` — the audio thread is the sole caller of `imitone.InputAudio` (in
    // ImitoneVoiceIntepreter.AudioThread.cs's OnAudioFilterRead). The cross-thread contract is
    // imitone-internal: imitone.GetState reads the analysis state the audio-thread feed produced,
    // and is documented to be safe to call concurrently with InputAudio.
    private void GetRawVoiceData()
    {
        // Step 3b: this method no longer copies samples or computes _dbMicrophone (those moved to
        // OnAudioFilterRead on the audio thread). Its sole responsibility is to poll imitone's
        // analysis state — produced from the samples the audio thread already fed it via
        // imitone.InputAudio earlier — and update the main-thread tone-gate variables that the
        // game logic (CheckToning, Update) consumes. Returning early on !MicIngestIsReady prevents
        // imitone state polling before the mic is online; legacy main-thread mic-ingest still owns
        // microphone bootstrapping and the IsMicReady contract until Step 5b retires that block.
        if (!MicIngestIsReady)
        {
            if(debugAllowInitializationLogs || debugAllowWarnings)
            {
                Debug.LogError("Imitone: Microphone capture is not ready.");
            }
            return;
        }

        if (imitone != null)
        {
            // Step 3b: imitone is fed exclusively from OnAudioFilterRead (audio thread); the
            // main-thread feed (and its 1 s feed_buffer chunking workaround) is retired. Main
            // thread only polls state. _dbMicrophone is also written from the audio thread,
            // immediately after the same buffer is filtered, so the value we read here is
            // already the post-filter dB reading from the most recent audio callback.
            string rawState = imitone.GetState();
            imitoneGetStateCallTotal++;

            if (rawState == _lastImitoneStateRaw)
            {
                mainThreadFramesSinceLastImitoneStateChange++;
                return;
            }
            _lastImitoneStateRaw = rawState;
            imitoneState = rawState;

            // Step 3a: track imitone state staleness via raw observed power + pitch_hz (not _dbValue, which
            // is overridden in force-mode). NaN means "not seen this frame" (parse exception or absent field).
            float thisFrameObservedPower = float.NaN;
            float thisFrameObservedPitchHz = float.NaN;

            try
            {
                var data = new JSONObject(imitoneState);
                JSONObject tones = data["tones"];
                JSONObject notes = data["notes"];
                if (!tones || !tones.isArray) throw new ArgumentException("imitone output did not include tones array.");
                if (!notes || !notes.isArray) throw new ArgumentException("imitone output did not include notes array.");
                if (tones.list != null && tones.list.Count > 0)
                {
                    var tone = tones[0];
                    if (tone.HasField("sound"))
                    {
                        var soundObject = tone.GetField("sound");
                        if (soundObject.HasField("power"))
                        {
                            float power = soundObject.GetField("power").floatValue;
                            thisFrameObservedPower = power;

                            if (!forceImitoneActive && !forceImitoneInactive)
                            {
                                _dbValue = AudioLevelUtilities.PowerToDb(power);
                                imitoneActiveRaw = true;
                                // Game-facing only: do not clear imitoneActiveRaw; gate imitoneActive so imitone
                                // analysis and thresholds are unchanged while near estimated noise floor.
                                micIsNearNoiseFloor = _dbMicrophone <= _noiseFloorThreshold;
                                imitoneActive = gameOn && !micIsNearNoiseFloor;
                                //Debug.Log("Power = " + power + "   dbValue = " + _dbValue + "   threshold = " + GetVolumeThresholdFromJson());
                            }

                            _level = (float)Math.Pow(10, _dbValue) * 0.05f;
                        }
                        if (soundObject.HasField("brightness"))
                        {
                            float brightness = soundObject.GetField("brightness").floatValue;
                            _timbre = brightness;
                        }
                    }
                    if (tone.HasField("sahir"))
                    {
                        var SahirObject = tone.GetField("sahir");
                        if (SahirObject.HasField("conv"))
                        {
                            _harmonicity = SahirObject.GetField("conv").floatValue;
                        }
                    }
                    if (!tone.isObject) throw new ArgumentException("imitone tone is not an object");
                    if (tone["frequency_hz"] == null) throw new ArgumentException("imitone tone does not have frequency_hz");
                    pitch_hz = tone["frequency_hz"].floatValue;
                    thisFrameObservedPitchHz = pitch_hz;
                }
                else
                {
                    pitch_hz = 0f;
                    thisFrameObservedPitchHz = 0f;
                    imitoneActiveRaw = false;
                    imitoneActive = false;
                    micIsNearNoiseFloor = _dbMicrophone <= _noiseFloorThreshold;
                    // No tone reported: refresh imitone power dB so telemetry does not hold last toning value.
                    _dbValue = AudioLevelUtilities.PowerToDb(0f);
                    _level = 0f;
                }
                if (notes.list != null && notes.list.Count > 0)
                {
                    var note = notes[0];
                    if (!note.isObject) throw new ArgumentException("imitone note is not an object");
                    if (note["pitch"] == null) throw new ArgumentException("imitone note does not have frequency_hz");
                    // Convert from imitone's wacky pitch value to MIDI frequency format
                    note_st = note["pitch"].floatValue / 100f - 36.3763165623f;
                }
                else
                {
                    note_st = 0f;
                }
            }
            catch (Exception e)
            {
                if(debugAllowWarnings || debugAllowInitializationLogs)
                {
                    Debug.Log(e);
                }
                pitch_hz = -1f;
                note_st = -1f;
            }

            // Step 3a: state-change detection. Climbs persistently only when imitone is hung (audio-thread
            // feed dead, GetState returning identical bits frame after frame). In normal operation the
            // counter stays near 0 because real input induces tiny power fluctuations every callback.
            bool imitoneStateChanged =
                !FloatsEqualOrBothNaN(thisFrameObservedPower, _lastImitoneStatePower)
                || !FloatsEqualOrBothNaN(thisFrameObservedPitchHz, _lastImitoneStatePitchHz);
            if (imitoneStateChanged)
            {
                mainThreadFramesSinceLastImitoneStateChange = 0;
                _lastImitoneStatePower = thisFrameObservedPower;
                _lastImitoneStatePitchHz = thisFrameObservedPitchHz;
            }
            else
            {
                mainThreadFramesSinceLastImitoneStateChange++;
            }
        }
        else
        {
            Debug.LogError("ImitoneVoiceIntepreter.GetRawVoiceData: imitone is null; pitch / dB cannot be polled. Init order issue?");
        }
    }

    private void CheckToning()
    {
        if (imitoneActiveRaw)
        {
            //Logic that runs everytime imitoneActive is true. Increments timers
            _imitoneActiveRawTimer += Time.deltaTime;
            _imitoneInactiveRawTimer = 0f;
        }
        else
        {
            //Logic that increments timers
            _imitoneInactiveRawTimer += Time.deltaTime;
            _imitoneActiveRawTimer = 0f;
        }

        if (imitoneActive)
        {
            _imitoneActiveTimer += Time.deltaTime;
            _imitoneInactiveTimer = 0f;

            if (_imitoneActiveTimer >= positiveActiveThreshold1 && !toneActive)
            {
                toneActiveRaw = true;
                toneActive = gameOn ? true : false;
                toneActiveBiasTrue = gameOn ? true : false;
                toneActiveBiasTrueTimer += Time.deltaTime;
            }
            if (_imitoneActiveTimer >= positiveActiveThreshold2)
            {
                toneActiveConfident = gameOn ? true : false;
            }
            int flooredSemitone = FrequencyToFlooredSemitone(pitch_hz);
        }
        else
        {
            _imitoneInactiveTimer += Time.deltaTime;
            _imitoneActiveTimer = 0f;

            if (_imitoneInactiveTimer >= negativeActiveThreshold1)
            {
                toneActiveRaw = false;
                toneActive = false;
            }
            if (_imitoneInactiveTimer >= negativeActiveThreshold2)
            {
                toneActiveConfident = false;
                toneActiveBiasTrue = false;
                toneActiveBiasTrueTimer = 0f;
            }

        }

        //Logic that switches between toneActive and !toneActive, including setting _tThisTone and _tThisRest

        if (toneActive)
        {
            //BE MINDFUL THAT ANY CHANGES HERE ARE APPROPRIATELY DUPLICATED IN THE TONEACTIVERAW BLOCK BELOW.
            _tThisTone += Time.deltaTime;
            _tSessionToneActive += Time.deltaTime;
            _tNextInhaleDuration += (Time.deltaTime * 0.5f); //magic number only used here and immedidately below
            _tThisRest = 0.0f;
            resetToneFrame = false;
            if (_lastToneActiveSwitchState != true)
            {
                AkSoundEngine.SetSwitch("ToneActive", "Toning", gameObject);
                _lastToneActiveSwitchState = true;
            }

            if (!toneActiveFrame)
            {
                toneActiveFrame = true;
                toneActiveCounter++;
                AkSoundEngine.PostEvent("Stop_Inhales", gameObject);
            }

            if (_tThisTone > _activeThreshold3)
            {
                toneActiveVeryConfident = true;
            }

        }
        else
        {
            _tThisRest += Time.deltaTime;
            _tThisTone = 0.0f;
            toneActiveFrame = false;
            if (_lastToneActiveSwitchState != false)
            {
                AkSoundEngine.SetSwitch("ToneActive", "Resting", gameObject);
                _lastToneActiveSwitchState = false;
            }

            //TODO: Next time we refactor, move the breath stuff below into its own method, or even its own .cs\
            if (imitoneActive) //if, for some reason, toneActive is false but imitoneActive is true, don't trigger inhale yet
            {

                _tNextInhaleDuration += (Time.deltaTime * 0.75f); //magic number only used here and immedidately above
            }
            else if (!resetToneFrame) //TRIGGER INHALE aka BREATHVOLUME
            {
                resetToneFrame = true;
                float newInhaleEffectTargetDuration = Mathf.Clamp(_tNextInhaleDuration, 0f, 5.0f);
                if (newInhaleEffectTargetDuration >= 1.0f)
                {
                    //BREATHE-IN LIGHT AND SOUND CONTROL
                    endBreathVolumesRequested = false;
                    //Debug.Log("BreathVolumeCoroutine Started, _tNextInhaleDuration = " + _tNextInhaleDuration + " and newInhaleEffectTargetDuration = " + newInhaleEffectTargetDuration);
                    StartCoroutine(EndBreathVolumesOnNextTone()); //no issue having multiple of these.
                    
                    StartCoroutine(StartInhaleEffectWithDelay(newInhaleEffectTargetDuration));
                }
            }

            if (_tThisRest > _activeThreshold3)
            {
                toneActiveVeryConfident = false;
            }
        }

        if (toneActiveRaw) //RAW VARIATION, FOR TONEACTIVEVERYCONFIDENTRAW FOR RESPIRATIONRATE
        {
            _tThisToneRaw += Time.deltaTime;
            _tThisRestRaw = 0.0f;

            if (_tThisToneRaw > _activeThreshold3)
            {
                toneActiveVeryConfidentRaw = true;
            }
        }
        else
        {
            _tThisRestRaw += Time.deltaTime;
            _tThisToneRaw = 0.0f;
            if (_tThisRestRaw > _activeThreshold3)
            {
                toneActiveVeryConfidentRaw = false;
            }
        }

        if (toneActiveBiasTrue)
        {
            if (!toneActiveBiasTrueFrameFlag)
            {
                toneActiveBiasTrueFrame = true;
                toneActiveBiasTrueFrameFlag = true;
            }
            else
            {
                toneActiveBiasTrueFrame = false;
            }
        }
        else
        {
            toneActiveBiasTrueFrame = false;
            toneActiveBiasTrueFrameFlag = false;
        }

        if (toneActiveConfident)
        {
            _tThisToneConfident += Time.deltaTime;
            _tThisRestConfident = 0.0f;
            if (!toneActiveConfidentFrame)
            {
                toneActiveConfidentFrame = true;
                toneActiveConfidentCounter++;
            }
        }
        else
        {
            _tThisToneConfident = 0.0f;
            _tThisRestConfident += Time.deltaTime;
        }

        if (toneActiveBiasTrue)
            _tThisToneBiasTrue += Time.deltaTime;
        else
            _tThisToneBiasTrue = 0.0f;
        // Custom debug for biasTrue and Confident ON/OFF states
        if (toneActiveBiasTrue != toneActiveBiasTrueLastFrame)
        {
            if (debugAllowToneActiveVariationsLogs)
            {
                if (toneActiveBiasTrue)
                {
                    Debug.Log("Imitone: toneActiveBiasTrue --ON--");
                }
                else
                {
                    Debug.Log("Imitone: toneActiveBiasTrue --off--");
                }
            }
        }
        if (toneActiveConfident != toneActiveConfidentLastFrame)
        {
            if (debugAllowToneActiveVariationsLogs)
            {
                if (toneActiveConfident)
                {
                    Debug.Log("Imitone: toneActiveConfident --ON--");
                }
                else
                {
                    Debug.Log("Imitone: toneActiveConfident --off--");
                }
            }
        }
        // Track state changes for debugging (at end of CheckToning())
        if (toneActiveBiasTrue != toneActiveBiasTrueLastFrame)
        {
            if(debugAllowToneActiveVariationsLogs)
            {
                Debug.Log("Imitone: toneActiveBiasTrue changed to " + toneActiveBiasTrue);
            }
        }
        if (toneActiveConfident != toneActiveConfidentLastFrame)
        {
            if(debugAllowToneActiveVariationsLogs)
            {
                Debug.Log("Imitone: toneActiveConfident changed to " + toneActiveConfident);
            }
        }
        
        // Update previous frame state at the end of CheckToning()
        toneActiveBiasTrueLastFrame = toneActiveBiasTrue;
        toneActiveConfidentLastFrame = toneActiveConfident;
    }


    // private void StoppedToning()
    // {
    //     if (_inhaleDuration < 1.76f)
    //     {
    //         _inhaleDuration = 1.76f;
    //     }
    //     else if (_inhaleDuration > 7.0f)
    //     {
    //         _inhaleDuration = 7.0f;
    //     }
    // }   

    //Breathe Volume Coroutine
    private IEnumerator EndBreathVolumesOnNextTone()
    {
        while (toneActiveConfident) //so if we are toneActiveConfident at the start... it will wait until we are not
        {
            yield return null;
        }
        //Debug.Log("EndBreathVolumesOnNextTone: waiting...");
        while (!toneActiveConfident)
        {
            yield return null;
        }
        //Debug.Log("EndBreathVolumesOnNextTone: ending breath volumes");

        _tNextInhaleDuration = 0.0f; //DECOUPLING THIS FROM TONEACTIVE COULD BE AWKWARD, but I think it will get best results. If this is awkward, put it in the (!resetToneFrame) if statement above.
        endBreathVolumesRequested = true;
        AkSoundEngine.PostEvent("Stop_Inhales", gameObject);
    }

    private IEnumerator StartInhaleEffectWithDelay(float newInhaleEffectTargetDuration)
    {
        float delayDuration = newInhaleEffectTargetDuration / 13f;
        float elapsedTime = 0f;
        
        // Wait for delay, checking for abort request each frame
        while (elapsedTime < delayDuration)
        {
            if (endBreathVolumesRequested)
            {
                yield break; // Abort if cancellation requested
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Check one more time after delay completes (handles race condition on final frame)
        if (endBreathVolumesRequested)
        {
            yield break; // Abort if cancellation requested
        }
        
        // If we reach here, delay completed without abort - execute the effect
        StartCoroutine(BreathVolumeCoroutine(Mathf.Max(2.5f, newInhaleEffectTargetDuration)));
        if (newInhaleEffectTargetDuration > 5.0f)
        {
            AkSoundEngine.PostEvent("Play_Inhale_Long", gameObject);
            if(debugAllowSFXLogs)
            {
                Debug.Log("Imitone: SFX: Play_Inhale_Long (" + newInhaleEffectTargetDuration + ")");
            }
        }
        else if (newInhaleEffectTargetDuration > 3.0f)
        {
            AkSoundEngine.PostEvent("Play_Inhale_Medium", gameObject);
            if(debugAllowSFXLogs)
            {
                Debug.Log("Imitone: SFX: Play_Inhale_Medium(" + newInhaleEffectTargetDuration + ")");
            }
        }
        else if (newInhaleEffectTargetDuration >= 1.0f)
        {
            AkSoundEngine.PostEvent("Play_Inhale_Short", gameObject);
            if(debugAllowSFXLogs)
            {
                Debug.Log("Imitone: SFX: Play_Inhale_Short (" + newInhaleEffectTargetDuration + ")");
            }
        }
    }

    private IEnumerator BreathVolumeCoroutine(float inhaleDuration)
    {
        int coroutineID = _coroutineCounter++;
        _breathVolumeContributions[coroutineID] = 0f;

        float normalizedTime = 0f;
        float currentBreathValue = 0f;
        float pi2 = 2 * Mathf.PI;
        float _v = 0f;

        if (inhaleDuration > 3.0f)
            _v = 0.25f; // how much time do we spend in stage 1 (up), vs. stage 2 (down)
        else
            _v = 0.33f; // how much time do we spend in stage 1 (up), vs. stage 2 (down)


        //Debug.Log("BreathVolumeCoroutine: Starting with Inhale Duration of " + inhaleDuration + " and _v of " + _v);
        while (normalizedTime <= _v && !endBreathVolumesRequested)
        { //Stage 1 - rapid increase, can be interrupted by tone
            normalizedTime += Time.deltaTime / inhaleDuration;
            float _progress = Mathf.Min(normalizedTime / _v / 2.0f, 0.5f); // will get half way through when normalizedTime = _v
            //currentBreathValue = (1f - Mathf.Cos(_progress * pi2)) * 0.5f; //cosine calculation
            //currentBreathValue = _progress * 2.0f; //linear calculation
            currentBreathValue = Mathf.Clamp(MathF.Sqrt(_progress * 2.0f), 0f, 1f); //square root calculation
            _breathVolumeContributions[coroutineID] = currentBreathValue;
            //Debug.Log("BreathVolumeCoroutine: Stage 1 Rising at nT("+normalizedTime+") p(" + _progress + ") vol(" + currentBreathValue + ")");
            UpdateBreathVolumeTotal();

            yield return null;
        }

        normalizedTime = _v;
        float _volumeAtBreak = currentBreathValue;

        while (normalizedTime < 1.0f)
        { //Stage 2 - slow decrease
            normalizedTime += Time.deltaTime / inhaleDuration;
            float _progress = Mathf.Min(0.5f + (normalizedTime - _v) / (1f - _v) * 0.5f, 1.0f);
            currentBreathValue = (1f - Mathf.Cos(_progress * pi2)) * 0.5f * _volumeAtBreak;
            _breathVolumeContributions[coroutineID] = currentBreathValue;
            UpdateBreathVolumeTotal();

            yield return null;
        }

        //Debug.Log("BreathVolumeCoroutine: ending");
        _breathVolumeContributions.Remove(coroutineID);
        UpdateBreathVolumeTotal();
    }


    //Updating Breath Volume Total
    private void UpdateBreathVolumeTotal()
    {
        _breathVolume = 0f;
        foreach (var contribution in _breathVolumeContributions.Values)
        {
            _breathVolume += contribution;
        }
        _breathVolume = Mathf.Clamp(_breathVolume, 0.0f, 1.0f);
    }

    // private void handleBreathStage(){
    //     if(breathStage == 0){
    //         breathStage = 1;
    //     } else if(breathStage == 1){
    //         if(_breathVolume > 0 && 0.5f > _breathVolume){
    //             breathStage = 2;
    //         }
    //     } else if (breathStage == 2){
    //         if(_breathVolume > 0.5f){
    //             breathStage = 3;
    //         }
    //     } else if (breathStage == 3){
    //         if(_breathVolume < 0.5f && _breathVolume > 0){
    //             breathStage = 4;
    //         }
    //     } else if (breathStage == 4 || breathStage == 3){
    //         if(_breathVolume <= 0){
    //             breathStage = 5;
    //         }
    //     }
    // }

    public static int FrequencyToFlooredSemitone(double frequency)
    {
        double semitone = 12 * Math.Log(frequency / A4, 2);
        return (int)Math.Floor(semitone);
        //Debug.Log(semitone);
    }

    public float GetVolumeThresholdFromJson()
    {
        var match = Regex.Match(imitoneConfig, @"""volume"":{.*""threshold"":([^,}]*)");

        if (match.Success)
        {
            exceptionFlag = false;
            return float.Parse(match.Groups[1].Value);
        }
        else
        {
            return -999f;
            //if (!exceptionFlag)
            //{
            //    exceptionFlag = true;
            //    throw new Exception("Could not find 'volume:threshold' in JSON string");
            //}
        }
    }

    public void SetGameOn(bool monitorOn)
    {
        if (monitorOn)
        {
            if(debugAllowMonitoringLogs)
            {
                Debug.Log("Imitone: Monitoring start");
            }
            gameOn = true;
        }
        else
        {
            if(debugAllowMonitoringLogs)
            {
                Debug.Log("Imitone: Monitoring stop");
            }
            gameOn = false;
        }
    }

    private void Wwise_BreathSound (float _input, float _addition = 0.0f) //THIS LOOKS PRETTY BROKEN TO ME
    {
        float _input2 = _input;
        float _addition2 = _addition;
      
        float _i = Mathf.Max(Mathf.Min(_input2 + _addition2, 1.0f), 0.0f);
        float _waveValue = 0.0f + 100.0f * _i;

        if (!Mathf.Approximately(_waveValue, _lastInhaleRTPCValue))
        {
            AkSoundEngine.SetRTPCValue("Unity_Inhale", _waveValue, gameObject); //TODO: Make sure this is the correct game object.
            _lastInhaleRTPCValue = _waveValue;
        }
        lightControl.Wwise_BreathDisplay(_waveValue);
        
        if (_i != 0.0f)
            //Debug.Log("Breath Wave Value: " + _waveValue);

            if (breathSoundFlag)
            {
                if(debugAllowWarnings || debugAllowSFXLogs)
                {
                    Debug.LogWarning("Warning: AVS Breath Response (SOUND) already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
                }
            }
        breathSoundFlag = true;
    }
}
