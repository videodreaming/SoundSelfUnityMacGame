using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.MathHelpers;
//using System.Text.Json;
//using System.Text.Json.Nodes;
using Defective.JSON;

using imitone;
//use this to translate the voice intepreter stuff into imitone
//copy functions from voiceinterpreter to here.

[Serializable]
public class Threshold
{
    public float Upper { 
        get => _upper;
        set => _upper = value;
    }
    public float Lower { 
        get => _lower;
        set => _lower = value; }

    [SerializeField] private float _upper;
    [SerializeField] private float _lower;
    [SerializeField] private float _interval;


    public Threshold(float lower, float interval)
    {
        _upper = _lower;
        _upper = _lower + interval;
        _interval = interval;
    }

    public void MoveThreshold()
    {
        _upper = _lower + _interval;
    }
}




public class ImitoneVoiceIntepreter: MonoBehaviour
{
    //base variables pitch and midiNote
    public float pitch_hz = 0f;
    public float note_st = 0f;

    //coped variables from old Voice Intepreter
    public Action<float> OnNewTone;
    public Action ChantEvent;
    public Action BreathEvent;
    
    [Tooltip("Active when toning.")]
    public bool Active { get; private set; }
    
    //TODO: using these vars
    public float ssVolume { get; private set; }
    public float cChantCharge => _cChantCharge;
    
    public float Cadence => _lengthOfLastBreath == 0 ? 0 : (_lengthOfTonesSinceBreath / _lengthOfLastBreath);
    [SerializeField] private float _cadence;
    private float _lengthOfTonesSinceBreath;
    private float _lengthOfLastBreath;
    private float _lengthOfTones;
    private float _lengthOfBreath;
    
    public int MostRecentSemitone => _semitone;
    public string MostRecentSemitoneNote => _semitoneNote;
    private int _semitone;
    private string _semitoneNote;
    private int[] _mostRecentSemitone = new []{-1,-1};
    private int[] _previousSemitone = new []{-1,-1};
    
    [Header("Noise")]
    [SerializeField] private float _thresholdLerpValue;
    [SerializeField] private Threshold _noiseLevel;
    public Threshold NoiseLevel => _noiseLevel;
    
    private int _midiNote;
    private bool _chanting;
    private float _cChantCharge;
     private float _rmsValue;
    private float _dbValue;
    private const int SAMPLE_SIZE = 1024;
    private AudioSource _audioSource;
     private string _selectedDevice; 
     private int _sampleRate;
    private readonly float _referenceAmplitude = 20.0f * Mathf.Pow(10.0f, -6.0f);
    [SerializeField] private AudioClip _audioClip;
    /*[Header("audio analysis")]
    
    [SerializeField] private bool _useMicrophone = true;
   
    */
    [SerializeField] private float _pitchDifference = 3;
    



    [TextAreaAttribute(8,8)] public string imitoneState;


    int sampleRate;
    ImitoneVoice imitone;

    string             microphoneName;
    AudioClip          inputBuffer;
    int                micPosRead = 0;
    float[]            capturedInput;

    // Start is called before the first frame update
    void Start()
    {
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
            Debug.Log("PitchTracker failed to Start recording from Microphone!");
            return;
        }
        
        try
        {
            ImitoneVoice.ActivateLicense("imitone technology used under license to New Entheogen Ltd, March 2023.");

            // Create an imitone voice whose notes are in "exact pitch" mode.
            // Also specify 'range' large enough to permit whistling.
            imitone = new ImitoneVoice(sampleRate, "{\"guide\":\"off\",\"slide\":\"bend\",\"range\":{\"min\":34.0,\"max\":101.0}}");
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
    void FixedUpdate()
    {
        _cadence = _lengthOfLastBreath == 0 ? 0 : (_lengthOfTonesSinceBreath / _lengthOfLastBreath);
        CheckToning();
        if (!inputBuffer) return;
        
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
                        if (!tone.isObject) throw new ArgumentException("imitone tone is not an object");

                        if (tone["frequency_hz"] == null) throw new ArgumentException("imitone tone does not have frequency_hz");
                        pitch_hz = tone["frequency_hz"].floatValue;
                    }
                    else
                    {
                        pitch_hz = 0f;
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
        }
    }


     private void CheckToning(){
        //if(_dbValue > _noiseLevel.Upper)
        if(pitch_hz > 0)
        {
            if (!Active)
            {
                Active = true;
                OnActiveInactive();
            }
            _noiseLevel.Lower = Mathf.Lerp(_noiseLevel.Lower,_dbValue-10f,_thresholdLerpValue);
            _noiseLevel.MoveThreshold();
            
            //TODO: determine when to start note
            _mostRecentSemitone = SemitoneUtility.GetSemitoneFromFrequency(pitch_hz);
            _semitone = SemitoneUtility.GetNoteFromSemitone(_mostRecentSemitone[0], _mostRecentSemitone[1]);
            _semitoneNote = SemitoneUtility.ToString(_mostRecentSemitone);
            if (!(_mostRecentSemitone[0] < 0) && (_previousSemitone[0] != _mostRecentSemitone[0] ||
                                                  _previousSemitone[1] != _mostRecentSemitone[1]))
            {
                _previousSemitone = _mostRecentSemitone;
                OnNewTone?.Invoke(_semitone);
            }
        }
        else if (pitch_hz == 0) 
        //(_dbValue < _noiseLevel.Lower)
        {
            if (Active)
            {
                Active = false;
                OnActiveInactive();
            }
            _noiseLevel.Lower = Mathf.Lerp(_noiseLevel.Lower,_dbValue,_thresholdLerpValue/5);
            _noiseLevel.MoveThreshold();
        }
    }

        private void OnActiveInactive()
    {
        if(Active)
        {
            ChantEvent?.Invoke();
            _cChantCharge += 0.01f;
            _lengthOfLastBreath = _lengthOfBreath;
            _lengthOfBreath = 0;
            _lengthOfTones += Time.deltaTime;
        } 
        else
        {
            BreathEvent?.Invoke();
            _cChantCharge = 0;
            _lengthOfTonesSinceBreath = _lengthOfTones;
            _lengthOfTones = 0;
            _lengthOfBreath += Time.deltaTime;
        }
    }
}   
