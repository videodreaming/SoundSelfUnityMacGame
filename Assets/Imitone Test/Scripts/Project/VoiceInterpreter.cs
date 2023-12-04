// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using B83.MathHelpers;
// using UnityEngine;

// /*[Serializable]
// public class Threshold
// {
//     public float Upper { 
//         get => _upper;
//         set => _upper = value;
//     }
//     public float Lower { 
//         get => _lower;
//         set => _lower = value; }

//     [SerializeField] private float _upper;
//     [SerializeField] private float _lower;
//     [SerializeField] private float _interval;


//     public Threshold(float lower, float interval)
//     {
//         _upper = _lower;
//         _upper = _lower + interval;
//         _interval = interval;
//     }

//     public void MoveThreshold()
//     {
//         _upper = _lower + _interval;
//     }
// }*/

// public class VoiceInterpreter : MonoBehaviour
// {
//     //TODO: cChanting & related
    
//     //TODO: ssVolume
    
//     public Action<float> OnNewTone;
    
//     //TODO: using chant event/breath event ?
//     //TODO: breath event (increase cosine length along with the range of time that the person has been toning.
//     //^ This should work with active, active will iterate a time variable that will tell breatheEvent how long it will take)
//     public Action ChantEvent;
//     public Action BreathEvent;
    
//     [Tooltip("Active when toning.")]
//     public bool Active { get; private set; }
    
//     //TODO: using these vars
//     public float ssVolume { get; private set; }
//     public float cChantCharge => _cChantCharge;
    
//     public float Cadence => _lengthOfLastBreath == 0 ? 0 : (_lengthOfTonesSinceBreath / _lengthOfLastBreath);
//     [SerializeField] private float _cadence;
//     private float _lengthOfTonesSinceBreath;
//     private float _lengthOfLastBreath;
//     private float _lengthOfTones;
//     private float _lengthOfBreath;
    
//     public int MostRecentSemitone => _semitone;
//     public string MostRecentSemitoneNote => _semitoneNote;
//     private int _semitone;
//     private string _semitoneNote;
//     private int[] _mostRecentSemitone = new []{-1,-1};
//     private int[] _previousSemitone = new []{-1,-1};
    
//     [Header("Noise")]
//     [SerializeField] private float _thresholdLerpValue;
//     //[SerializeField] private Threshold _noiseLevel;
//     //public Threshold NoiseLevel => _noiseLevel;
    
//     private int _midiNote;
    

//     private bool _chanting;
//     private float _cChantCharge;

//     [Header("audio analysis")]
//     private readonly float _referenceAmplitude = 20.0f * Mathf.Pow(10.0f, -6.0f);
//     private const int SAMPLE_SIZE = 1024;
//     [SerializeField] private bool _useMicrophone = true;
//     [SerializeField] private AudioClip _audioClip;
//     private AudioSource _audioSource;
//     private int _sampleRate;
//     private string _selectedDevice;
//     private float _rmsValue;
//     private float _dbValue;
//     public float _pitchValue;
//     [SerializeField] private float _pitchDifference = 3;
    

//     private void Awake()
//     {
//         _audioSource = GetComponent<AudioSource>();
//         _sampleRate = AudioSettings.outputSampleRate;
        
//         if(_useMicrophone)
//         {
//             MicrophoneToAudioClip();
//         }
//         else
//         {
//             Debug.Log("not using microphone");
//         }
        
//         _audioSource.clip = _audioClip;
//     }

//     private void Update()
//     {
//         _cadence = _lengthOfLastBreath == 0 ? 0 : (_lengthOfTonesSinceBreath / _lengthOfLastBreath);
//         AnalyzeSound();
//         CheckToning();
//     }

//     private void MicrophoneToAudioClip()
//     {
//         if(Microphone.devices.Length > 0)
//         {
//             _selectedDevice = Microphone.devices[0];
//             _audioClip = Microphone.Start(_selectedDevice, true, 10, _sampleRate);
//         }
//         else{
//             _useMicrophone = false;
//         }
//     }
    
//     private float[] GetSampleData(int size)
//     {
//         float [] samples = new float[size];
//         int startPosition = Microphone.GetPosition(_selectedDevice) - size;
//         if (startPosition < 0) startPosition = 0;
//         _audioSource.clip.GetData(samples, startPosition);
        
//         return samples;
//     }

//     private float GetRMSValue(float [] samples)
//     {
//         float amplitude = samples.Sum(t => (t * t));
//         return Mathf.Sqrt(amplitude/samples.Length);
//     }

//     private void GetDBValue()
//     {
//         _rmsValue = GetRMSValue(GetSampleData(SAMPLE_SIZE));

//         //Get DB Value
//         _dbValue = 20f * Mathf.Log10(_rmsValue/_referenceAmplitude);
//         if (_dbValue < 0) _dbValue = 0;
//     }

    
//    private float[] GetSpectrumData()
//     {
//         float[] samples = GetSampleData(SAMPLE_SIZE*2);
        
//         Complex[] fft = FFT.Float2Complex(samples);
//         Complex[] result = FFT.CalculateFFT(fft, false);
//         return FFT.Complex2Float(result, false);
//     }
     
//     private void GetPitch()
//     {
//         // Get sound Spectrum
//         float [] spectrumData = GetSpectrumData();

//         //FindPitch
//         float maxM = 0;
//         var maxI = 0;
//         for(int i=0; i < SAMPLE_SIZE; i++){
//             float magnitude = spectrumData[i];
//             if (magnitude > maxM)
//             {
//                 maxM = magnitude;
//                 maxI = i;
//             }
//         }
        
//         //TODO: the pitch/frequency is a bit off
//         _pitchValue = (float)maxI * (_sampleRate*0.5f) / SAMPLE_SIZE - _pitchDifference;
//     }
    
//     private void AnalyzeSound()
//     {
//         GetDBValue();
//         GetPitch();
        
//         // Debug.Log($"Pitch: {_pitchValue} Hz, Decibels: {_dbValue} dB");
//     }
    
//     private void CheckToning(){
//         if(_dbValue > -35.0f)
//         {
//             if (!Active)
//             {
//                 Active = true;
//                 OnActiveInactive();
//             }
//             _noiseLevel.Lower = Mathf.Lerp(_noiseLevel.Lower,_dbValue-10f,_thresholdLerpValue);
//             _noiseLevel.MoveThreshold();
            
//             //TODO: determine when to start note
//             _mostRecentSemitone = SemitoneUtility.GetSemitoneFromFrequency(_pitchValue);
//             _semitone = SemitoneUtility.GetNoteFromSemitone(_mostRecentSemitone[0], _mostRecentSemitone[1]);
//             _semitoneNote = SemitoneUtility.ToString(_mostRecentSemitone);
//             if (!(_mostRecentSemitone[0] < 0) && (_previousSemitone[0] != _mostRecentSemitone[0] ||
//                                                   _previousSemitone[1] != _mostRecentSemitone[1]))
//             {
//                 _previousSemitone = _mostRecentSemitone;
//                 OnNewTone?.Invoke(_semitone);
//             }
//         }
//         else if (_dbValue < -35.0f)
//         {
//             if (Active)
//             {
//                 Active = false;
//                 OnActiveInactive();
//             }
//             _noiseLevel.Lower = Mathf.Lerp(_noiseLevel.Lower,_dbValue,_thresholdLerpValue/5);
//             _noiseLevel.MoveThreshold();
//         }
//     }
    
//     private void OnActiveInactive()
//     {
//         if(Active)
//         {
//             ChantEvent?.Invoke();
//             _cChantCharge += 0.01f;
//             _lengthOfLastBreath = _lengthOfBreath;
//             _lengthOfBreath = 0;
//             _lengthOfTones += Time.deltaTime;
//         } 
//         else
//         {
//             BreathEvent?.Invoke();
//             _cChantCharge = 0;
//             _lengthOfTonesSinceBreath = _lengthOfTones;
//             _lengthOfTones = 0;
//             _lengthOfBreath += Time.deltaTime;
//         }
//     }
// }
