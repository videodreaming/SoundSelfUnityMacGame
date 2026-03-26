using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.MathHelpers;
using System.Text.RegularExpressions;
using Defective.JSON;

using imitone;
//use this to translate the voice intepreter stuff into imitone
//copy functions from voiceinterpreter to here.

//TODO
//Why is flooredsemitone floored and not rounded?

public class ImitoneVoiceIntepreter : MonoBehaviour
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
    [SerializeField] public float _dbMicrophone = -999.0f; //this seems to be the db of the raw mic
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
    [SerializeField] private float _thresholdAboveNoiseFloor = 8f;

    public float _imitoneVolumeThreshold { get; private set; } = 0f;

    private Coroutine currentNoiseFloorCoroutine;

    //private bool manualMode = false;

    private float UpperThreshold = -20.0f;
    private float LowerThreshold = -35.0f;

    //Volume Tracking
    private List<(float, float)> volumes1s = new List<(float, float)>();
    private List<(float, float)> anomalyBaselineVolumes = new List<(float, float)>();
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
    ImitoneVoice imitone;

    string microphoneName;
    AudioClip inputBuffer;
    int micPosRead = 0;
    float[] capturedInput;
    // Reusable buffer for chunking audio to imitone. Imitone's internal feed_buffer holds max 1 second (sampleRate samples).
    private float[] _imitoneChunkBuffer;

    [Header("High Pass Filter")]
    [Tooltip("Removes low-frequency rumble (e.g. AC hum, wind) before pitch analysis. 80 Hz is typical for voice.")]
    [SerializeField] private bool _highPassFilterEnabled = true;
    [SerializeField] private float _highPassCutoffHz = 80f;
    private float _hpPrevInput;
    private float _hpPrevOutput;

    [Header("Low Pass Filter")]
    [Tooltip("Removes high-frequency hiss and overtones above voice range. 520 Hz keeps tenor fundamentals.")]
    [SerializeField] private bool _lowPassFilterEnabled = true;
    [SerializeField] private float _lowPassCutoffHz = 520f;
    private float _lpPrevOutput;

    // Public accessors for shared microphone usage
    public AudioClip MicrophoneBuffer => inputBuffer;
    public string MicrophoneDeviceName => microphoneName;
    public int MicrophoneSampleRate => sampleRate;

    // Debug log category flags
    private bool debugAllowInitializationLogs = true;
   // private bool debugAllowToneActiveLogs = true;
    private bool debugAllowToneActiveVariationsLogs = false;
    private bool debugAllowSFXLogs = false;
    private bool debugAllowVolumeTrackingLogs = false;
    private bool debugAllowMonitoringLogs = false;
    private bool debugAllowWarnings = false; // Warnings show if this OR the category flag is true

    void Start()
    {
        _volumeAnomalyThresholdDb = _volumeAnomalyThresholdDb_init;

        //Checking for all devices in the list of devices 
        foreach (var device in Microphone.devices)
        { microphoneName = device; break; }
        if (microphoneName.Length == 0)
        {
            if(debugAllowInitializationLogs || debugAllowWarnings)
            {
                Debug.LogError("Imitone: No microphone was available for pitch tracking.");
            }
            return;
        }
        if(debugAllowInitializationLogs)
        {
            Debug.Log("Imitone: Chose microphone: " + microphoneName);
        }
        // NOTE: Unity doesn't give us a way to query native samplerate.
        //  Converting to 48khz may degrade audio quality slightly.
        sampleRate = 48000;

        // NOTE: this requires permission on mobile.

        inputBuffer = Microphone.Start(
                deviceName: microphoneName,
                loop: true,
                lengthSec: 6,
                frequency: sampleRate
                );

        if (inputBuffer == null)
        {
            //If mircophone fails to start
            if(debugAllowInitializationLogs || debugAllowWarnings)
            {
                Debug.LogError("Imitone: PitchTracker failed to Start recording from Microphone!");
            }
            return;
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
    }

    void Update()
    {
        SetNoiseFloorThreshold();
        GetRawVoiceData();
        CheckToning();
        TrackMicVolume();
        //Wwise_BreathSound(_breathVolume, lightControl._fxWave);

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
        }

        if (toneActiveConfident)
        {
            //LOG 1S AVERAGE
            _vol1Sec = volumes1s.Average(x => x.Item2);
            _volFlagA = false;

            //RECORD ANOMALY BASELINE DATA
            _timerForAnomalyBaselines += Time.deltaTime;
            if (_timerForAnomalyBaselines >= 0.1f)
            {
                anomalyBaselineVolumes.Add((Time.time, _vol1Sec));
                _timerForAnomalyBaselines = 0.0f;
            }

        }
        else if (!_volFlagA || !toneActive)
        {
            //CLEAR DATA FROM 1S
            volumes1s.Clear();
            _vol1Sec = -1000.0f;
            _volFlagA = true;
        }

        //CALCULATE ANOMALY BASELINE FROM 1M AVERAGE
        if (anomalyBaselineVolumes.Count > 0)
        {
            _anomalyBaseline = anomalyBaselineVolumes.Average(x => x.Item2);
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
            float highestVolume = volumes1s.Max(x => x.Item2);
            //add in the equivalent of 7.5 seconds of data to the anomaly baseline, at a value equal to the highest value in volumes1s
            for (int i = 0; i < 75; i++)
            {
                anomalyBaselineVolumes.Add((Time.time, highestVolume));
            }
        }

        //CLEAR DATA THAT IS TOO OLD
        for (int i = volumes1s.Count - 1; i >= 0; i--)
        {
            if (volumes1s[i].Item1 < Time.time - 1.0f)
            {
                volumes1s.RemoveAt(i);
            }
        }
        for (int i = anomalyBaselineVolumes.Count - 1; i >= 0; i--)
        {
            if (anomalyBaselineVolumes[i].Item1 < Time.time - _anomalyBaselineMeasurementTime) // 60 seconds...
            {
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


    private void SetNoiseFloorThreshold() //WE NEED RAW VALUES FOR THIS. CURRENT RAW DEPENDENCIES: _dbMicrophone
    {
        rawMic.Add(uniqueKey++, (Time.time, _dbMicrophone));
        //Remove any entries more than noiseDropMeasurmentWindow frames old.
        List<int> keysToRemove = new List<int>();
        foreach (var entry in rawMic)
        {
            if (Time.time - entry.Value.Item1 > _volumeChangeMeasurementWindow)
                keysToRemove.Add(entry.Key);
        }
        foreach (var key in keysToRemove)
        {
            rawMic.Remove(key);
        }

        //when _dbMicrophone > (rawMic.Values.Min() + _volumeJumpTriggerThresholdDB), begin the MeasureNoiseFloor coroutine. If there is already an instance of the coroutine running, stop it and start a new one.
        if (_dbMicrophone > (rawMic.Values.Min(x => x.Item2) + _volumeJumpTriggerThresholdDB))
        {
            if (currentNoiseFloorCoroutine != null)
                StopCoroutine(currentNoiseFloorCoroutine);

            currentNoiseFloorCoroutine = StartCoroutine(MeasureNoiseFloorCoroutine());
        }

    }


    private IEnumerator MeasureNoiseFloorCoroutine()
    {
        float _noiseFloorMeasurementSum = 0f;
        float _noiseFloorMeasurementCount = 0f;
        //Debug.Log("Preparing to Measure Noise Floor...");

        //First wait for the levels to drop an appropriate amount
        while (_dbMicrophone >= (rawMic.Values.Max(x => x.Item2) - _volumeDropTriggerThresholdDB))
        {
            yield return null;
        }
        //Then, wait a little longer before starting to measure the noise floor.
        float _measuredPeak = rawMic.Values.Max(x => x.Item2);
        yield return new WaitForSeconds(_afterDropWaitTime);

        //Now we can measure the noise floor
        float _measuredTime = 0f;
        while (_measuredTime < _noiseFloorMeasurementTime)
        {
            _noiseFloorMeasurementSum += _dbMicrophone;
            _noiseFloorMeasurementCount++;
            _measuredTime += Time.deltaTime;
            yield return null;
        }

        //Once the noise floor has been measured, add the average to the noiseMeasurements dictionary, using the time as the key.
        float _noiseFloorMeasurementAverage = _noiseFloorMeasurementSum / _noiseFloorMeasurementCount;

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

        //Calculate the median noise floor from the noiseMeasurements dictionary:
        List<float> values = noiseMeasurements.Values.Select(x => x.Item2).OrderBy(x => x).ToList();
        float _medianNoiseFloor = (values.Count % 2 != 0) ?
        values[values.Count / 2] :
        (values[(values.Count - 1) / 2] + values[values.Count / 2]) / 2.0f;
        _imitoneVolumeThreshold = _medianNoiseFloor + _thresholdAboveNoiseFloor;
        SetThreshold(_imitoneVolumeThreshold);
        //Debug.Log("Noise Floor Measured: " + _noiseFloorMeasurementAverage + " (from peak: " + _measuredPeak + ") New Threshold: " + _imitoneVolumeThreshold + " from " + noiseMeasurements.Count + " measurements.");
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



    /// <summary>First-order high-pass filter in-place. Removes rumble below cutoff. State persists across calls.</summary>
    private void ApplyHighPassFilter(float[] samples)
    {
        if (samples == null || samples.Length == 0 || sampleRate <= 0) return;
        float rc = 1f / (2f * Mathf.PI * _highPassCutoffHz);
        float dt = 1f / sampleRate;
        float alpha = rc / (rc + dt);
        for (int i = 0; i < samples.Length; i++)
        {
            float x = samples[i];
            float y = alpha * (_hpPrevOutput + x - _hpPrevInput);
            _hpPrevInput = x;
            _hpPrevOutput = y;
            samples[i] = y;
        }
    }

    /// <summary>First-order low-pass filter in-place. Attenuates frequencies above cutoff. State persists across calls.</summary>
    private void ApplyLowPassFilter(float[] samples)
    {
        if (samples == null || samples.Length == 0 || sampleRate <= 0) return;
        float rc = 1f / (2f * Mathf.PI * _lowPassCutoffHz);
        float dt = 1f / sampleRate;
        float alpha = dt / (rc + dt);
        for (int i = 0; i < samples.Length; i++)
        {
            float x = samples[i];
            float y = alpha * x + (1f - alpha) * _lpPrevOutput;
            _lpPrevOutput = y;
            samples[i] = y;
        }
    }

    private void GetRawVoiceData()
    { //WE NEED RAW VALUES FOR THIS
        if (!inputBuffer)
        {
            if(debugAllowInitializationLogs || debugAllowWarnings)
            {
                Debug.LogError("Imitone: No Input Buffer");
            }
            return;
        }

        // The microphone's write position in the clip can wrap back around to the beginning.
        int micPosWrite = Microphone.GetPosition(microphoneName);
        Array.Resize(ref capturedInput, (inputBuffer.samples + micPosWrite - micPosRead) % inputBuffer.samples);
        if (capturedInput.Length > 0)
        {
            // Read the latest audio data, beginning from where we left off and wrapping around as needed.
            inputBuffer.GetData(capturedInput, micPosRead);
            micPosRead = (micPosRead + capturedInput.Length) % inputBuffer.samples;

            if (_highPassFilterEnabled && _highPassCutoffHz > 0f)
                ApplyHighPassFilter(capturedInput);
            if (_lowPassFilterEnabled && _lowPassCutoffHz > 0f)
                ApplyLowPassFilter(capturedInput);

            // Analyze the captured audio with imitone.
            if (imitone != null)
            {
                float peakAmplitude = 0f;
                float meanAmplitude = 0f;
                foreach (float sample in capturedInput)
                {
                    if (Math.Abs(sample) > peakAmplitude)
                    {
                        peakAmplitude = Math.Abs(sample);
                    }
                    meanAmplitude += Math.Abs(sample);
                }
                meanAmplitude /= capturedInput.Length;

                //Debug.Log(String.Format("Analyzing mic samples x {0}, peak amplitude {1}", capturedInput.Length, peakAmplitude));
                _dbMicrophone = (float)(10.0 * Math.Log10(meanAmplitude * meanAmplitude));

                // CHUNKING: imitone's feed_buffer holds max 1 second (sampleRate samples). When inputBuffer was 1 second,
                // capturedInput never exceeded that. After increasing inputBuffer to 6 seconds, we can read up to ~6 sec
                // in one frame (e.g. after startup lag or frame spike), causing IndexOutOfRangeException in imitone.
                // We process in chunks of sampleRate, oldest-first, so imitone receives all audio in order.
                // TO REVERT: remove the chunking block below and restore: imitone.InputAudio(capturedInput);
                if (_imitoneChunkBuffer == null || _imitoneChunkBuffer.Length != sampleRate)
                    _imitoneChunkBuffer = new float[sampleRate];
                for (int offset = 0; offset < capturedInput.Length; offset += sampleRate)
                {
                    int chunkSize = Math.Min(sampleRate, capturedInput.Length - offset);
                    float[] chunkToPass;
                    if (chunkSize == sampleRate)
                    {
                        Array.Copy(capturedInput, offset, _imitoneChunkBuffer, 0, chunkSize);
                        chunkToPass = _imitoneChunkBuffer;
                    }
                    else
                    {
                        chunkToPass = new float[chunkSize];
                        Array.Copy(capturedInput, offset, chunkToPass, 0, chunkSize);
                    }
                    imitone.InputAudio(chunkToPass);
                }
                //imitone.InputAudio(capturedInput); //Old Behavior
                //END CHUNKING

                imitoneState = imitone.GetState();
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

                                if (!forceImitoneActive && !forceImitoneInactive)
                                {
                                    _dbValue = (float)(10.0 * Math.Log10(power));
                                    imitoneActiveRaw = true;
                                    imitoneActive = gameOn ? true : false;
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
                    }
                    else
                    {
                        pitch_hz = 0f;
                        //_dbValue = (float)(10.0 * Math.Log10(soundObject.GetField("power").floatValue));
                        //_dbValue = -999f;
                        imitoneActiveRaw = false;
                        imitoneActive = false;
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

            }
            else
            {
                //Debug.Log("No imitone voice to analyze audio.");
                //FORCE TONE LOGIC WAS HERE (look in Code Snippets in Notion)
            }
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
            AkSoundEngine.SetSwitch("ToneActive", "Toning", gameObject);

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
            AkSoundEngine.SetSwitch("ToneActive", "Resting", gameObject);

            //TODO: Next time we refactor, move the breath stuff below into its own method, or even its own .cs\
            if (imitoneActive) //if, for some reason, toneActive is false but imitoneActive is true, don't trigger inhale yet
            {

                _tNextInhaleDuration += (Time.deltaTime * 0.5f); //magic number only used here and immedidately above
            }
            else if (!resetToneFrame) //TRIGGER INHALE aka BREATHVOLUME
            {
                resetToneFrame = true;
                float newInhaleEffectTargetDuration = Mathf.Clamp(_tNextInhaleDuration, 0f, 7.0f);
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
        float delayDuration = newInhaleEffectTargetDuration / 10f;
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
        StartCoroutine(BreathVolumeCoroutine(Mathf.Max(1.76f, newInhaleEffectTargetDuration)));
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

        AkSoundEngine.SetRTPCValue("Unity_Inhale", _waveValue, gameObject); //TODO: Make sure this is the correct game object.
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
