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
    public DevModeSettings DevModeSettings;
    public WwiseAVSMusicManager wwiseAVSMusicManager;
    public float pitch_hz = 0f;
    private const double A4 = 440.0; //Reference Frequency
    public float note_st = 0f;
    public float _dbThreshold = -59.0f;

    //coped variables from old Voice Intepreter
    // Are we using this action? Robin doesn't understand how an action works.
    public Action<float> OnNewTone;
    
    [Tooltip("imitoneActive when toning.")]
    public bool imitoneActive { get; private set; } = false;

    [Tooltip("Toning With False Positive Logic")]
    public bool toneActive { get; private set; } = false;   
    
    [Tooltip("Confident Toning, used for... nothing yet")]
    public bool toneActiveConfident { get; private set; } = false;
    public bool toneActiveBiasTrue { get; private set; } = false;   //combines toneActive & toneActiveConfident
    public float toneActiveBiasTrueTimer { get; private set; } = 0f;
    public bool toneActiveBiasFalse { get; private set; } = false;  //combines toneActive & toneActiveVeryConfident
    [Tooltip("Very Confident Toning, used for respiration")]
    public bool toneActiveVeryConfident { get; private set; } = false;
    public float positiveActiveThreshold1 {get; private set;} = 0.05f; //for toneActive 
    public float positiveActiveThreshold2 {get; private set;}  = 0.2f; //for toneActiveConfident
    public float negativeActiveThreshold1 {get; private set;}  = 0.1f; //for toneActive
    public float negativeActiveThreshold2 {get; private set;}  = 0.33f; //for toneActiveConfident
    public float _activeThreshold3 {get; private set;}  = 0.75f; //positive and negative are the same... used for respiration rate (toneActiveVeryConfident)
    private float imitoneActiveTimer = 0f;
    public float imitoneInactiveTimer = 0f;
    private float imitoneConfidentActiveTimer = 0.0f;
    public float imitoneConfidentInactiveTimer = 0.0f;

    //TODO: using these vars
    public float ssVolume { get; private set; }     //WORK ON THIS ONE IN GAMEVALUES
    public float _tThisTone;
    public float _tThisToneConfident;
    public float _tThisToneBiasTrue;
    public float _tThisRest;
    public float _tThisRestConfident;
    public float _tThisRestBiasTrue;
    private float _durLastTone;    

    //BREATH
    [SerializeField] private float _breathHoldTimeBeforeInhale;
    //public float _inhaleDuration;
    //Both Above is the duration of breath 
    public float _tNextInhaleDuration = 0.0f;
    public float _breathVolume;
    private bool isResettingTone = false;
    public int breathStage = 0;
    
    //public int MostRecentSemitone => _semitone;
    //public string MostRecentSemitoneNote => _semitoneNote;
    //private int _semitone;
    //private string _semitoneNote;
    //private int[] _mostRecentSemitone = new []{-1,-1};
    //private int[] _previousSemitone = new []{-1,-1};
    
    [Header("DampingValues")]
    public float _harmonicity = 0.0f;    
    private float _rmsValue;
    [SerializeField] public float _dbValue = -80.0f;
    [SerializeField] public float _dbMicrophone = -999.0f;
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

    //DevMode
    public string imitoneConfig;

    private bool forceToneActive = false;
    private bool forceNoTone = false;

    int sampleRate;
    ImitoneVoice imitone;

    string             microphoneName;
    AudioClip          inputBuffer;
    int                micPosRead = 0;
    float[]            capturedInput;
   
    void Start()
    {
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

    // Update is called once per frame
    private int frameCount = 0;

    //NOTE FOR REEF, I've moved a bunch of logic that isn't interpolating from FixedUpdate() into Update().
    void Update()
    {
        //FOR TESTING, I am having this only happen once, on the 2nd frame. 
        frameCount++;
        //if (frameCount == 2)
        //{
        //    SetThreshold(-30.0f);
        //}
        SetNoiseFloorThreshold();
        GetRawVoiceData();
        CheckToning();
        //wwiseAVSMusicManager.Wwise_BreathDisplay(_breathVolume);
    }

    private void SetNoiseFloorThreshold()
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

        
    private IEnumerator MeasureNoiseFloorCoroutine(){
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

    private void GetRawVoiceData(){
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
                                    //override imitone if using forceToneActive
                                    if(forceToneActive == true)
                                    {
                                        _dbValue = -25.0f;
                                        imitoneActive = true;
                                        Debug.Log("Force Tone Active");
                                    }
                                    else if (forceNoTone == true)
                                    {
                                        _dbValue = -75.0f;
                                        imitoneActive = false;
                                        Debug.Log("Force No Tone Active");
                                    } 
                                    else
                                    {
                                        _dbValue = (float)(10.0 * Math.Log10(power));
                                        imitoneActive = true;
                                        Debug.Log("Power = " + power + "   dbValue = " + _dbValue + "   threshold = " + GetVolumeThresholdFromJson());
                                    }
                                    _level = (float)Math.Pow(10,_dbValue) * 0.05f;

                                    DevModeSettings.LogChangeBool("ForceTone = ", DevModeSettings.forceToneActive);
                                    DevModeSettings.LogChangeFloat("dbValue = ", _dbValue);
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
            if(DevModeSettings.devMode == true){
                //T = FORCE TONEACTIVE 
                if(Input.GetKey(KeyCode.T)){
                    if (forceToneActive == false)
                    {
                        Debug.Log("Force Tone");
                        forceToneActive = true;
                        forceNoTone = false;
                    }
                }
                else if (!Input.GetKey(KeyCode.T) && forceToneActive){
                    Debug.Log("Force No-Tone");
                    forceToneActive = false;
                    forceNoTone = true;
                }
                if (forceToneActive)
                {
                    imitoneActive = true;
                }
                if (forceNoTone)
                {
                    imitoneActive = false;
                }
            }
        }
    }

    private void SetThreshold(float db = -52.5f){
        //Logic that sets the threshold for imitone's dbValue using SetConfig() to the value of dbThreshold
       
        imitone.SetConfig("{\"volume\" : {\"threshold\" : " + db + "} }");   
        imitoneConfig = imitone.GetConfig();
        //Debug.Log("imitone configuration: " + imitoneConfig);  
    }

    private void CaptureNoiseFloorData(float imitoneVolume)
    {
    
    }
    
    public void SetMute(bool mute = false){
      
      Debug.Log("imitone mute should (but does not yet) become = " + mute);
       //BELOW IS THE ORIGINAL CODE FOR SETTING MUTE, COMMENTED BECAUSE IT CAUSES A CRASH 
        //imitone.SetConfig("{\"mute\": " + mute + "}");   
        //imitoneConfig = imitone.GetConfig();
        //Debug.Log("imitone configuration: " + imitoneConfig);  
    }

    private void CheckToning(){
        if(imitoneActive)
        {        
            //Logic that runs everytime imitoneActive is true. Increments timers
            imitoneConfidentActiveTimer += Time.deltaTime;
            imitoneActiveTimer += Time.deltaTime;
            imitoneInactiveTimer = 0f;
            imitoneConfidentInactiveTimer = 0f;
            if (imitoneActiveTimer >= positiveActiveThreshold1 && !toneActive)
            {
                toneActive = true;
                toneActiveBiasTrue = true;
                toneActiveBiasTrueTimer += Time.deltaTime;
            }
            if(imitoneActiveTimer >= positiveActiveThreshold2) 
            {
                toneActiveConfident = true;
            }
            int flooredSemitone = FrequencyToFlooredSemitone(pitch_hz);
        }
        else
        {
            //Logic that runs everytime !imitoneActive
            {
                //Logic that increments timers
                imitoneInactiveTimer += Time.deltaTime;
                imitoneConfidentInactiveTimer += Time.deltaTime;
                imitoneActiveTimer = 0f;
                imitoneConfidentActiveTimer = 0f;
                if(imitoneInactiveTimer >= negativeActiveThreshold1)
                {
                    toneActive = false;
                }
                if(imitoneInactiveTimer >= negativeActiveThreshold2)
                {
                    toneActiveConfident = false;
                    toneActiveBiasTrue = false;
                    toneActiveBiasTrueTimer = 0f;
                }
            }
        }

        //Logic that switches between toneActive and !toneActive, including setting _tThisTone and _tThisRest
        if(toneActive)
        {
            breathStage = 0;
            _tThisTone += Time.deltaTime;
            _tNextInhaleDuration += (Time.deltaTime * 0.41f); //magic number only used here and immedidately below
            _tThisRest = 0.0f;
            isResettingTone = false;

            if (_tThisTone > _activeThreshold3)
            toneActiveVeryConfident = true;
            toneActiveBiasFalse = true;
        }
        else
        {
            handleBreathStage();
            _tThisRest += Time.deltaTime;
            _tThisTone  = 0.0f;
            if(imitoneActive) //if, for some reason, toneActive is false but imitoneActive is true, don't trigger inhale yet
            {
                
                toneActiveBiasFalse = false;
                _tNextInhaleDuration += (Time.deltaTime * 0.41f); //magic number only used here and immedidately above
            }
            else if (!isResettingTone) //TRIGGER INHALE aka BREATHVOLUME
            {
                isResettingTone = true;
                float currentInhaleDuration = Math.Clamp(_tNextInhaleDuration, 1.76f, 7.0f);
                StartCoroutine(BreathVolumeCoroutine(currentInhaleDuration));
                //Debug.Log("InhaleCoroutineStarted");
                _tNextInhaleDuration = 0.0f;
            }

            if (_tThisRest > _activeThreshold3)
            toneActiveVeryConfident = false;
        }

        if(toneActiveConfident)
        {
            _tThisToneConfident += Time.deltaTime;
            _tThisRestConfident = 0.0f;
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

        if(toneActiveBiasFalse)
        _tThisRestBiasTrue = 0.0f;
        else
        _tThisRestBiasTrue += Time.deltaTime;

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
private IEnumerator BreathVolumeCoroutine(float inhaleDuration) {
    int coroutineID = _coroutineCounter++;
    _breathVolumeContributions[coroutineID] = 0f;

    float elapsedTime = 0f;
    float normalizedTime = 0f;
    
    while (elapsedTime < inhaleDuration) {
        normalizedTime = elapsedTime / inhaleDuration;
        float currentBreathValue = (1 - Mathf.Cos(normalizedTime * 2 * Mathf.PI)) * 0.5f;

        _breathVolumeContributions[coroutineID] = currentBreathValue;
        UpdateBreathVolumeTotal();

        elapsedTime += Time.deltaTime;
        yield return null;
    }
    
    //Debug.Log ("inhaleDuration = " + inhaleDuration + "   normalizedTime = " + normalizedTime + "    /_breathVolume = " + _breathVolume);
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

private void handleBreathStage(){
    if(breathStage == 0){
        breathStage = 1;
    } else if(breathStage == 1){
        if(_breathVolume > 0 && 0.5f > _breathVolume){
            breathStage = 2;
        }
    } else if (breathStage == 2){
        if(_breathVolume > 0.5f){
            breathStage = 3;
        }
    } else if (breathStage == 3){
        if(_breathVolume < 0.5f && _breathVolume > 0){
            breathStage = 4;
        }
    } else if (breathStage == 4 || breathStage == 3){
        if(_breathVolume <= 0){
            breathStage = 5;
        }
    }
}

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
        return float.Parse(match.Groups[1].Value);

    }
    else
    {
        throw new Exception("Could not find 'volume:threshold' in JSON string");
    }
}
}
