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
    public float pitch_hz = 0f;
    private const double A4 = 440.0; //Reference Frequency
    public float note_st = 0f;
    public float _dbThreshold = -59.0f;

    //coped variables from old Voice Intepreter
    // Are we using this action? Robin doesn't understand how an action works.
    public Action<float> OnNewTone;
    
    [Tooltip("imitoneActive when toning.")]
    public bool imitoneActive { get; private set; }
    

    [Tooltip("Toning With False Positive Logic")]
    public bool toneActive { get; private set; }
    
    [Tooltip("Confident Toning")]
    public bool toneActiveConfident { get; private set; }

    [SerializeField] private float positiveActiveThreshold1 = 0.05f;
    [SerializeField] private float positiveActiveThreshold2 = 0.45f;
    [SerializeField] private float negativeActiveThreshold = 0.1f;
    private float imitoneActiveTimer = 0f;
    private float imitoneInactiveTimer = 0f;
    private float imitoneConfidentActiveTimer = 0.0f;
    private float imitoneConfidentInactiveTimer = 0.0f;

    //TODO: using these vars
    public float ssVolume { get; private set; }    
    public float _tThisTone;
    public float _tThisRest;
    private float _durLastTone;    

    //BREATH
    [SerializeField] private float _breathHoldTimeBeforeInhale;
    //public float _inhaleDuration;
    //Both Above is the duration of breath 
    public float _tNextInhaleDuration = 0.0f;
    public float _breathVolume;
    private bool isResettingTone = false;
    public int breathStage = 0;
    
    //TODO: probably MostRecentSemitone should not be an int, but a float. There can be like a "semitoneRounded" too.
    public int MostRecentSemitone => _semitone;
    public string MostRecentSemitoneNote => _semitoneNote;
    private int _semitone;
    private string _semitoneNote;
    private int[] _mostRecentSemitone = new []{-1,-1};
    private int[] _previousSemitone = new []{-1,-1};
    
    [Header("Noise")]
    [SerializeField] private float _thresholdLerpValue;
    
    [Header("DampingValues")]
    public float _harmonicity = 0.0f;    
    private float _timeToneActiveTrue = 0.0f;
    private float _timeToneActiveFalse = 0.0f;
   // private float _elapsedTimeWithoutTone = 0.0f;
    //CHANTLERP
    
    public int chantLerpTarget = 0;
    private float _negativeThresholdForChantLerp    = 0.33f;
    private float _positiveThresholdForChantLerp    = 0.2f;

    private float _rmsValue;
    public float _dbValue = -80.0f;
    public float _timbre = 0.0f;
    public float _level; 
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
    private bool expectNoiseFloor = false;
    private bool manualMode = false;

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
        
        try
        {
            ImitoneVoice.ActivateLicense("imitone technology used under license to New Entheogen Ltd, March 2023.");

           // Original Settings:      (sampleRate, "{\"guide\":\"off\",\"slide\":\"bend\",\"range\":{\"min\":34.0,\"max\":101.0}}");
            imitone = new ImitoneVoice(sampleRate, "{\"guide\":\"on\",\"slide\":\"bend\",\"range\":{\"min\":34.0,\"max\":88.0},\"volume\":{\"threshold\":-40.0}}");
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
        if (frameCount == 2)
        {
            SetThreshold(-30.0f);
        }

        GetRawVoiceData();
        CheckToning();
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
                foreach (float sample in capturedInput)
                    if (Math.Abs(sample) > peakAmplitude)
                        peakAmplitude = Math.Abs(sample);
                //Debug.Log(String.Format("Analyzing mic samples x {0}, peak amplitude {1}", capturedInput.Length, peakAmplitude));
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
                                    }
                                    else if (forceNoTone == true)
                                    {
                                        _dbValue = -75.0f;
                                        imitoneActive = false;
                                    } 
                                    else
                                    {
                                        _dbValue = (float)(10.0 * Math.Log10(power));
                                        imitoneActive = true;
                                        //Debug.Log("Power = " + power + "   dbValue = " + _dbValue + "   threshold = " + GetVolumeThresholdFromJson());
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
                        _dbValue = -999f;
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
            }
        }
    }

    private void SetThreshold(float db = -52.5f){
        //Logic that sets the threshold for imitone's dbValue using SetConfig() to the value of dbThreshold
       
        imitone.SetConfig("{\"volume\" : {\"threshold\" : " + db + "} }");   
        imitoneConfig = imitone.GetConfig();
        //Debug.Log("imitone configuration: " + imitoneConfig);  
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
            if (imitoneActiveTimer >= positiveActiveThreshold1 && !toneActive)
            {
                //if imitoneActiveTimer is bigger than positiveActiveThreshold1 (0.05f) and toneActive is false, set toneActive to true and _thisTone is retuned to true
                toneActive = true;
                //_inhaleDuration = 0.0f;
                //_tThisTone = 0.0f;
            }
            if(imitoneActiveTimer >= positiveActiveThreshold2) 
            {
                //if activetimer is bigger than positiveActiveThreshold2 then toneActive is confident (0.45f)
                toneActiveConfident = true;
            }
            if(imitoneActiveTimer >= _positiveThresholdForChantLerp)
            {
                //if imitoneActiveTimer is bigger than positive Threshold For Chant Lerp (0.2f) then chantLerpTarget is set to 1.
                chantLerpTarget = 1;
            }
        
            int flooredSemitone = FrequencyToFlooredSemitone(pitch_hz);
            // _mostRecentSemitone = SemitoneUtility.GetSemitoneFromFrequency(pitch_hz);
            // _semitone = SemitoneUtility.GetNoteFromSemitone(_mostRecentSemitone[0], _mostRecentSemitone[1]);
            // _semitoneNote = SemitoneUtility.ToString(_mostRecentSemitone);
            // Debug.Log("Semitone = " + _semitone + "       SemitoneNote = " + _semitoneNote);
            // if (!(_mostRecentSemitone[0] < 0) && (_previousSemitone[0] != _mostRecentSemitone[0] ||
            //                                       _previousSemitone[1] != _mostRecentSemitone[1]))
            // {
            //     _previousSemitone = _mostRecentSemitone;
            //     OnNewTone?.Invoke(_semitone);
            // }
            
        }
        else
        {
            //Logic that runs everytime !imitoneActive
            {
                //Logic that increments timers
                //_tThisTone = 0.0f; 
                imitoneInactiveTimer += Time.deltaTime;
                imitoneConfidentInactiveTimer += Time.deltaTime;
                imitoneActiveTimer = 0f;
                imitoneConfidentActiveTimer = 0f;
                if(imitoneInactiveTimer >= negativeActiveThreshold)
                {
                    toneActiveConfident = false;
                    toneActive = false;
                }
                if(imitoneInactiveTimer >= 0.33)
                {
                    chantLerpTarget = 0;
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
            //Debug.Log(_tThisTone);
            isResettingTone = false;
            //_inhaleDuration = _tThisTone * 0.41f;
        }
        else
        {
            handleBreathStage();
            _tThisRest += Time.deltaTime;
            _tThisTone  = 0.0f;
            if(imitoneActive) //if, for some reason, toneActive is false but imitoneActive is true, don't trigger inhale yet
            {
                _tNextInhaleDuration += (Time.deltaTime * 0.41f); //magic number only used here and immedidately above
            }
            else if (!isResettingTone) //TRIGGER INHALE aka BREATHVOLUME
            {
                isResettingTone = true;
                float currentInhaleDuration = Math.Clamp(_tNextInhaleDuration, 1.76f, 7.0f);
                StartCoroutine(BreathVolumeCoroutine(currentInhaleDuration));
                Debug.Log("InhaleCoroutineStarted");
                _tNextInhaleDuration = 0.0f;
            }
        }


        // Handling _chantLerpSlow_lerp1 based on toneActive duration
        if (toneActive) {
            // When toning is active, increase the timer and reset the timer for inactive state
            _timeToneActiveTrue += Time.deltaTime;
            _timeToneActiveFalse = 0f;

            if (_timeToneActiveTrue > positiveActiveThreshold1) {
                //chantLerpTarget = 1;
            }
        } else {
            // When toning is not active, increase the timer for inactive state and reset the timer for active state
            _timeToneActiveFalse += Time.deltaTime;
            _timeToneActiveTrue = 0f;

            if (_timeToneActiveFalse > negativeActiveThreshold) {
                //chantLerpTarget = 0;
            }
        }
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
        Debug.Log(semitone);
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
