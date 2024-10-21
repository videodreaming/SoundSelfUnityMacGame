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

public class ImitoneVoiceIntepreter: MonoBehaviour
{
    //base variables pitch and midiNote
    public DevelopmentMode developmentMode;
    public LightControl lightControl;
    public Director director;
    public float pitch_hz = 0f;
    private const double A4 = 440.0; //Reference Frequency
    public float note_st = 0f;
    public float _dbThreshold;

    // Are we using this action? Robin doesn't understand how an action works.
    public Action<float> OnNewTone;
    public bool gameOn = true;
    private bool gameOnLastFrame = true;
    
    [Tooltip("imitoneActive when toning.")]
    public bool imitoneActive { get; private set; } = false;
    public bool imitoneActiveRaw { get; private set; } = false;

    [Tooltip("Toning With False Positive Logic")]
    public bool toneActive { get; private set; } = false;   
    public int toneActiveCounter { get; private set; } = 0;
    public bool toneActiveRaw { get; private set; } = false;
    public bool toneActiveFrame { get; private set; } = false;
    public bool toneActiveConfident { get; private set; } = false;
    public bool toneActiveConfidentFrame { get; private set; } = false;
    public int toneActiveConfidentCounter { get; private set; } = 0;
    public bool toneActiveBiasTrue { get; private set; } = false;   //combines toneActive & toneActiveConfident
    public float toneActiveBiasTrueTimer { get; private set; } = 0f;
    public bool toneActiveBiasTrueFrame { get; private set; } = false;
    private bool toneActiveBiasTrueFrameFlag = false;
    public bool toneActiveVeryConfident { get; private set; } = false;
    public bool toneActiveVeryConfidentRaw { get; private set; } = false;
    public float positiveActiveThreshold1 {get; private set;} = 0.05f; //for toneActive 
    public float positiveActiveThreshold2 {get; private set;}  = 0.2f; //for toneActiveConfident
    public float negativeActiveThreshold1 {get; private set;}  = 0.1f; //for toneActive
    public float negativeActiveThreshold2 {get; private set;}  = 0.33f; //for toneActiveConfident
    public float _activeThreshold3 {get; private set;}  = 0.75f; //positive and negative are the same... used for respiration rate (toneActiveVeryConfident)
    public float _activeThreshold4 {get; private set;}  = 7.0f; //positive and negative are the same... used for respiration rate (toneActiveVeryConfident)
    public bool exceptionFlag = false;

    //TODO: using these vars
    public float ssVolume { get; private set; }     //WORK ON THIS ONE IN GAMEVALUES
    private float _imitoneActiveRawTimer = 0f;
    public float _imitoneInactiveRawTimer = 0f;
    private float _imitoneActiveTimer;
    private float _imitoneInactiveTimer;
    public float _tThisTone;
    public float _tThisToneRaw;
    public float _tThisToneConfident;
    public float _tThisToneBiasTrue;
    public float _tThisRest;
    public float _tThisRestRaw;
    public float _tThisRestConfident;
    private float _durLastTone;    

    //BREATH
    [SerializeField] private float _breathHoldTimeBeforeInhale;
    //public float _inhaleDuration;
    //Both Above is the duration of breath 
    public float _tNextInhaleDuration = 0.0f;
    public float _breathVolume;
    private bool resetToneFrame = false; //the first frame that !toneActive && !imitoneActive, before toneActive is true again.
    private bool endBreathVolumes   = false; //THIS SYSTEM CAN DEFINITELY BE CLEANED UP QUITE EASILY...
    
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
    private AudioSource _audioSource;
    private string _selectedDevice; 
    private int _sampleRate;
    private readonly float _referenceAmplitude = 20.0f * Mathf.Pow(10.0f, -6.0f);
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private float _pitchDifference = 3;
    
    private Dictionary<int, float> _breathVolumeContributions = new Dictionary<int, float>();
    private int _coroutineCounter = 0; // To generate unique keys

    [TextAreaAttribute(8,8)] public string imitoneState;

    [Header("dbController")]

    //NoiseFloor
    [Header("Noise Floor")]
    //A dictionary that stores a float for the dbValue each frame, and the time that frame was recorded.
    private Dictionary<int, (float, float)> rawMic = new Dictionary<int, (float, float)>();
    private Dictionary<int, (float, float)> noiseMeasurements = new Dictionary<int, (float, float)>();
    private bool expectNoiseFloor = false; //NOT YET IMPLEMENTED, use this when the system is programmatically expecting noise.
    private bool noiseFloorFlag = false;
    [SerializeField] private float _volumeChangeMeasurementWindow   = 0.3f;
    [SerializeField] private float _volumeDropTriggerThresholdDB = 7f;
    [SerializeField] private float _volumeJumpTriggerThresholdDB = 12f;
    [SerializeField] private float _afterDropWaitTime           = 0.5f;
    private float _afterDropWaitTimer                           = 0f;
    [SerializeField] private float _noiseFloorMeasurementTime    = 1.5f;
    [SerializeField] private int noiseFloorMeasurementMaxAge    = 120;
    private int uniqueKey                                       = 0;
    [SerializeField] private float _thresholdAboveNoiseFloor    = 8f;

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
    private float _volumeAnomalyThresholdDbDecreaseRate = 0.5f;//per minute
    private float _volumeAnomalyThresholdDb;
    //DevMode
    public string imitoneConfig;

    private bool forceImitoneActive = false;
    private bool forceImitoneInactive = false;

    int sampleRate;
    ImitoneVoice imitone;

    string             microphoneName;
    AudioClip          inputBuffer;
    int                micPosRead = 0;
    float[]            capturedInput;
   
    void Start()
    {
        _volumeAnomalyThresholdDb = _volumeAnomalyThresholdDb_init;
        _audioSource = GetComponent<AudioSource>();
        //Checking for all devices in the list of devices 
        foreach (var device in Microphone.devices)
            {microphoneName = device; break;}
        if (microphoneName.Length == 0)
        {
            Debug.Log("No microphone was available for pitch tracking.");
            return;
        }
        Debug.Log("Chose microphone: " + microphoneName);
        // NOTE: Unity doesn't give us a way to query native samplerate.
        //  Converting to 48khz may degrade audio quality slightly.
        sampleRate = 48000;

        // NOTE: this requires permission on mobile.
        
        inputBuffer = Microphone.Start(
                deviceName: microphoneName,
                loop:       true,
                lengthSec:  1,
                frequency:  sampleRate
                );

        if (inputBuffer == null)
        {
            //If mircophone fails to start
            Debug.Log("PitchTracker failed to Start recording from Microphone!");
            return;
        }
        _audioSource.clip = inputBuffer;
        _audioSource.loop = true;
        while(!(Microphone.GetPosition(microphoneName) > 0)){
            _audioSource.Play();
        } 
        
        try
        {
            ImitoneVoice.ActivateLicense("imitone technology used under license to New Entheogen Ltd, March 2023.");
           // Original Settings:      (sampleRate, "{\"guide\":\"off\",\"slide\":\"bend\",\"range\":{\"min\":34.0,\"max\":101.0}}");
            imitone = new ImitoneVoice(sampleRate, "{\"guide\":\"on\",\"slide\":\"bend\",\"range\":{\"min\":34.0,\"max\":88.0},\"volume\":{\"threshold\":-52.0}}"); //threshold of -52 is ideal for Corsair HS80
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
            throw;
        }

        if (imitone == null)
        {
            Debug.Log("imitone was null after creation.");
        }
    }

    void Update()
    {
        SetNoiseFloorThreshold();
        GetRawVoiceData();
        CheckToning();
        TrackMicVolume();
        lightControl.Wwise_BreathDisplay(_breathVolume, lightControl._fxWave);

        if (gameOn != gameOnLastFrame)
        {
            if (gameOn)
            {
                Debug.Log("Imitone: Game On");
            }
            else
            {
                Debug.Log("Imitone: Game Off");
            }
            gameOnLastFrame = gameOn;
        }
    }

    private void TrackMicVolume()
    {

        //We are trying to detect when the volume rises up, for the director.
        //This means that right now, the most recent one second has been the loudest of the last 30 seconds, AND the loudest of the last 2 minutes.
        //It will trigger the Director Queue to process its contents.
        //It will also not be able to trigger again for another 30 seconds.
        //first, get the average volume of the last one second
        
        if(toneActive)
        {
            //RECORD 1S DATA
            volumes1s.Add((Time.time, _dbMicrophone));       
        }
        
        if(toneActiveConfident)
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
        else if(!_volFlagA || !toneActive)
        {
            //CLEAR DATA FROM 1S
            volumes1s.Clear();
            _vol1Sec = -1000.0f;
            _volFlagA = true;
        }

        //CALCULATE ANOMALY BASELINE FROM 1M AVERAGE
        if(anomalyBaselineVolumes.Count > 0)
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
            Debug.Log("Director Volume Anomaly Detected: " + _vol1Sec + " > " + _anomalyBaseline + " + " + _volumeAnomalyThresholdDb);
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

        if(_vol1Sec > -1000.0f)
        AkSoundEngine.SetRTPCValue("TONING_Volume", 60f + NormalizeVolume(_vol1Sec * 40f), gameObject);
    }

    private float NormalizeVolume(float volume)
    {
        return Mathf.Clamp(Mathf.InverseLerp(-55.0f, -29.0f, volume), 0.0f, 1.0f);
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
        float _noiseFloorMeasurementSum                     = 0f;
        float _noiseFloorMeasurementCount                   = 0f;
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
        float _measuredTime                                 = 0f;
        while (_measuredTime < _noiseFloorMeasurementTime)
        {
            _noiseFloorMeasurementSum += _dbMicrophone;
            _noiseFloorMeasurementCount++;
            _measuredTime += Time.deltaTime;
            yield return null;
        }
        
        //Once the noise floor has been measured, add the average to the noiseMeasurements dictionary, using the time as the key.
        float _noiseFloorMeasurementAverage                = _noiseFloorMeasurementSum / _noiseFloorMeasurementCount;

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
    
    private void SetThreshold(float db = -52.5f){
        //Logic that sets the threshold for imitone's dbValue using SetConfig() to the value of dbThreshold
       
        imitone.SetConfig("{\"volume\" : {\"threshold\" : " + db + "} }", false);   
        imitoneConfig = imitone.GetConfig();
        _dbThreshold = db;
        //Debug.Log("imitone configuration: " + imitoneConfig);  
    }


    private void GetRawVoiceData(){ //WE NEED RAW VALUES FOR THIS
        if (!inputBuffer) 
        {
            Debug.Log("No Input Buffer");
            return;
        }

        // The microphone's write position in the clip can wrap back around to the beginning.
        int micPosWrite = Microphone.GetPosition(microphoneName);
        Array.Resize(ref capturedInput, (inputBuffer.samples  +  micPosWrite - micPosRead) % inputBuffer.samples);
        if (capturedInput.Length > 0)
        {
            // Read the latest audio data, beginning from where we left off and wrapping around as needed.
            inputBuffer.GetData(capturedInput, micPosRead);
            micPosRead = (micPosRead + capturedInput.Length) % inputBuffer.samples;
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
                _dbMicrophone = (float)(10.0 * Math.Log10(meanAmplitude*meanAmplitude));

                imitone.InputAudio(capturedInput);
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
                            if(tone.HasField("sound")){
                                var soundObject = tone.GetField("sound");
                                if(soundObject.HasField("power"))
                                {
                                    float power = soundObject.GetField("power").floatValue;
                                    
                                    if(!forceImitoneActive && !forceImitoneInactive)
                                    {
                                        _dbValue = (float)(10.0 * Math.Log10(power));
                                        imitoneActiveRaw = true;
                                        imitoneActive = gameOn ? true : false;
                                        //Debug.Log("Power = " + power + "   dbValue = " + _dbValue + "   threshold = " + GetVolumeThresholdFromJson());
                                    }

                                    _level = (float)Math.Pow(10,_dbValue) * 0.05f;
                                }
                                if(soundObject.HasField("brightness"))
                                {
                                    float brightness = soundObject.GetField("brightness").floatValue;
                                    _timbre = brightness;
                                }
                            }
                            if(tone.HasField("sahir")){
                                var SahirObject = tone.GetField("sahir");
                                if(SahirObject.HasField("conv"))
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
                    Debug.Log(e);
                    pitch_hz = -1f;
                    note_st = -1f;
                }
                
            }
            else
            {
                //Debug.Log("No imitone voice to analyze audio.");
            }
            //Debug Beheviors
            if(developmentMode.developmentMode == true)
            {
                //T = FORCE TONEACTIVE 
                if(Input.GetKey(KeyCode.T)){
                    if (gameOn && forceImitoneActive == false)
                    {
                        Debug.Log("Force Tone");
                        forceImitoneActive = true;
                        forceImitoneInactive = false;
                    }
                }
                else if (gameOn  && !Input.GetKey(KeyCode.T) && forceImitoneActive){
                    Debug.Log("Force No-Tone");
                    forceImitoneActive = false;
                    forceImitoneInactive = true;
                }
                if (forceImitoneActive)
                {
                    _dbValue = -25.0f;
                    imitoneActive = gameOn ? true : false;
                }
                if (forceImitoneInactive)
                {
                    _dbValue = -75.0f;
                    imitoneActive = false;
                }
            }
        }
    }

    private void CheckToning(){
        if(imitoneActiveRaw)
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

        if(imitoneActive)
        {
            _imitoneActiveTimer += Time.deltaTime;
            _imitoneInactiveTimer = 0f;

            if (_imitoneActiveTimer >= positiveActiveThreshold1 && !toneActive)
            {
                toneActive = gameOn ? true : false;
                toneActiveBiasTrue = gameOn ? true : false;
                toneActiveBiasTrueTimer += Time.deltaTime;
            }
            if(_imitoneActiveTimer >= positiveActiveThreshold2) 
            {
                toneActiveConfident = gameOn ? true : false;
            }
            int flooredSemitone = FrequencyToFlooredSemitone(pitch_hz);
        }
        else
        {
            _imitoneInactiveTimer += Time.deltaTime;
            _imitoneActiveTimer = 0f;

            if(_imitoneInactiveTimer >= negativeActiveThreshold1)
            {
                toneActive = false;
            }
            if(_imitoneInactiveTimer >= negativeActiveThreshold2)
            {
                toneActiveConfident = false;
                toneActiveBiasTrue = false;
                toneActiveBiasTrueTimer = 0f;
            }

        }

        //Logic that switches between toneActive and !toneActive, including setting _tThisTone and _tThisRest

        if(toneActive)
        {
            //BE MINDFUL THAT ANY CHANGES HERE ARE APPROPRIATELY DUPLICATED IN THE TONEACTIVERAW BLOCK BELOW.
            _tThisTone += Time.deltaTime;
            _tNextInhaleDuration += (Time.deltaTime * 0.5f); //magic number only used here and immedidately below
            _tThisRest = 0.0f;
            resetToneFrame = false;
            AkSoundEngine.SetSwitch("ToneActive","Toning",gameObject);
            
            if(!toneActiveFrame)
            {
                toneActiveFrame = true;
                toneActiveCounter++;
                AkSoundEngine.PostEvent("Stop_Inhales", gameObject);
                Debug.Log("SFX: Stop_Inhales");
            }

            if (_tThisTone > _activeThreshold3)
            {
                toneActiveVeryConfident = true;
            }

        }
        else
        {
            _tThisRest += Time.deltaTime;
            _tThisTone  = 0.0f;
            toneActiveFrame = false;
            AkSoundEngine.SetSwitch("ToneActive","Resting",gameObject);
            
            if(imitoneActive) //if, for some reason, toneActive is false but imitoneActive is true, don't trigger inhale yet
            {
                
                _tNextInhaleDuration += (Time.deltaTime * 0.5f); //magic number only used here and immedidately above
            }
            else if (!resetToneFrame) //TRIGGER INHALE aka BREATHVOLUME
            {
                resetToneFrame = true;
                float currentInhaleDuration = Mathf.Clamp(_tNextInhaleDuration, 0f, 7.0f);
                if(currentInhaleDuration >= 1.0f)
                {
                    endBreathVolumes = false;
                    //Debug.Log("BreathVolumeCoroutine Started, _tNextInhaleDuration = " + _tNextInhaleDuration + " and currentInhaleDuration = " + currentInhaleDuration);
                    StartCoroutine(BreathVolumeCoroutine(Mathf.Max(1.76f,currentInhaleDuration)));
                    StartCoroutine(EndBreathVolumesOnNextTone()); //no issue having multiple of these.
                    if(currentInhaleDuration > 5.0f)
                    {
                        AkSoundEngine.PostEvent("Play_Inhale_Long", gameObject);
                        Debug.Log("SFX: Play_Inhale_Long (" + currentInhaleDuration + ")");
                    }
                    
                    else if(currentInhaleDuration > 3.0f)
                    {
                        AkSoundEngine.PostEvent("Play_Inhale_Medium", gameObject);
                        Debug.Log("SFX: Play_Inhale_Medium(" + currentInhaleDuration + ")");
                    }
                    else if(currentInhaleDuration >= 1.0f)
                    {
                        AkSoundEngine.PostEvent("Play_Inhale_Short", gameObject);
                        Debug.Log("SFX: Play_Inhale_Short (" + currentInhaleDuration + ")");
                    }
                }
            }

            if (_tThisRest > _activeThreshold3)
            {
                toneActiveVeryConfident = false;
                EndBreathVolumesOnNextTone();
            }
        }
        
        if(toneActiveRaw) //RAW VARIATION, FOR TONEACTIVEVERYCONFIDENTRAW FOR RESPIRATIONRATE
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
            if(_tThisRestRaw > _activeThreshold3)
            {
                toneActiveVeryConfidentRaw = false;
            }
        }

        if (toneActiveBiasTrue)
        {
            if(!toneActiveBiasTrueFrameFlag)
            {
                Debug.Log("Tone Active Bias True Frame");
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

        if(toneActiveConfident)
        {
            _tThisToneConfident += Time.deltaTime;
            _tThisRestConfident = 0.0f;
            if(!toneActiveConfidentFrame)
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

        if(toneActiveBiasTrue)
        _tThisToneBiasTrue += Time.deltaTime;
        else
        _tThisToneBiasTrue = 0.0f;
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
        endBreathVolumes = true;
        AkSoundEngine.PostEvent("Stop_Inhales", gameObject);
    }
    private IEnumerator BreathVolumeCoroutine(float inhaleDuration) {
        int coroutineID = _coroutineCounter++;
        _breathVolumeContributions[coroutineID] = 0f;

        float normalizedTime = 0f;
        float currentBreathValue = 0f;
        float pi2           = 2 * Mathf.PI;
        float _v            = 0f;

        if (inhaleDuration > 3.0f)
        _v         = 0.25f; // how much time do we spend in stage 1 (up), vs. stage 2 (down)
        else
        _v         = 0.33f; // how much time do we spend in stage 1 (up), vs. stage 2 (down)
        
        
        //Debug.Log("BreathVolumeCoroutine: Starting with Inhale Duration of " + inhaleDuration + " and _v of " + _v);
        while (normalizedTime <= _v && !endBreathVolumes) { //Stage 1 - rapid increase, can be interrupted by tone
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

        while (normalizedTime < 1.0f) { //Stage 2 - slow decrease
            normalizedTime += Time.deltaTime / inhaleDuration;
            float _progress = Mathf.Min(0.5f + (normalizedTime - _v)/(1f - _v) * 0.5f, 1.0f);
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
}
