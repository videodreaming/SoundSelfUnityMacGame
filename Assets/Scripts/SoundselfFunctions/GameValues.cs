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

//TODO - revisit "responsiveness"

public class GameValues: MonoBehaviour
{
    public AudioManager AudioManager;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    public RespirationTracker respirationTracker;

    
    [Header("DampingValues")]
    private float responsiveness = 1.0f; //revisit when we have absorption

    //VOLUME
    public float _dbValidValue {get; private set;} = -1000.0f;
    public float _dbLowestLastSecond {get; private set;}
    public float _dbHighestLastSecond {get; private set;}
    private Dictionary<int, (float, float)> _dbSecond = new Dictionary<int, (float, float)>();
    int volumeKey = 0;
    int volumeKey2 = 0;
    private float _volumeTimeGuardTimer = 0.0f;
    public float _loudnessRelative {get; private set;} = 0.0f;
    private float _dbLowest;
    private float _dbHighest;
    private Dictionary <int, (float, float, float)> _db5Minutes = new Dictionary<int, (float, float, float)>(); //time, high-second, low-second

    //CHANTLERP
    [SerializeField] public float _meanToneLengthLerp {get; private set;} = 0.0f;
    [SerializeField] private float _chantLerpSlowDamp1;
    [SerializeField]  private float _chantLerpSlowDamp2;
    private float _chantLerpSlowDepletion;
    [SerializeField] private float _chantLerpFastDamp1;
    [SerializeField] private float _chantLerpFastDamp2;
    private float _chantLerpFastDepletion;
    private const float _chantLerpLinear = 0.0001f;
    float _lerpTargetSlow = 0.0f;
    float _lerpTargetFast   = 0.0f;
    public float _chantLerpFast {get; private set;} = 0.0f;
    public float _chantLerpSlow {get; private set;} = 0.0f;
    public float _tChantLerp {get; private set;} = 0.0f;
    public float _tRestLerp {get; private set;} = 0.0f;

    //CHANTCHARGE
    private float RecentToneMeanDuration = 5.0f; //resvisit when we have cadence/respirationrate.
    private float _chantChargeDamp1 = 0.01f;
    private float _chantChargeDamp2 = 0.005f;
    private float _chantChargeLinear = 0.00005f;
    private bool chantChargeToneGuard   = false;
    public float _chantCharge {get; private set;} = 0f;   
    
    private Dictionary<int, float> _chantChargeContributions = new Dictionary<int, float>(); 
    private int chantChargeCoroutineCounter = 0;

   
    void Start()
    {
    }

    // Fixed Update is called once per frame, but it is called on a fixed time step.
    void FixedUpdate()
    {
        handlecChanting();
        handleVolume();

        if (_chantLerpSlow > 0.0f)
        _tChantLerp += Time.fixedDeltaTime * _chantLerpSlow;
        if (_chantLerpSlow < 1.0f)
        _tRestLerp += Time.fixedDeltaTime * (1.0f - _chantLerpSlow);

        //HANDLE "CHANT CHARGE"
        //if we are in a guided vocalization state, set the full value to 5, otherwise it sets dynamically.
        float _forceDurationToFill = 0f; // 0 does not force
        if (imitoneVoiceInterpreter._tThisToneBiasTrue == 0)
        {
            chantChargeToneGuard = false;
        }
        else if (!chantChargeToneGuard)
        {
            chantChargeToneGuard = true;
            if(AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationAdvanced
            ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationAhh
            ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationOhh
            ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationHum)
            {
                _forceDurationToFill = 5.0f;
            }
            StartCoroutine(ChantChargeMemberCoroutine(_forceDurationToFill));
        }
        _chantCharge = Mathf.Clamp(_chantChargeContributions.Values.Sum(), 0,1);
    }

    private void handleVolume(){
        
        //every frame, store the current _dbValue in the dictionary. The dictionary should only ever have 1 second of data. It will clear when toneActive is false. When there is at least 1 second of data, we will calculate _dbValidValue as the highest value in the dictionary

        if (imitoneVoiceInterpreter.toneActive)
        { 
            if(imitoneVoiceInterpreter.toneActiveConfident)
            {
                volumeKey ++;
                // Adding a dictionary entry with two values
                _dbSecond.Add(volumeKey, (Time.time, imitoneVoiceInterpreter._dbValue));
                //if any entries are older than 1 second, remove them:
                _dbSecond = _dbSecond.Where(x => Time.time - x.Value.Item1 < 1).ToDictionary(x => x.Key, x => x.Value);
                //send the maximum value of the dictionary to _dbValidValue, and the minimum to _dbLowestLastSecond (only after 1 sec toning)
                _dbValidValue = _dbSecond.Values.Max(x => x.Item2);
                if (imitoneVoiceInterpreter._tThisTone >= 1.0f)
                {
                    _dbLowestLastSecond = _dbSecond.Values.Min(x => x.Item2);
                    _dbHighestLastSecond = _dbSecond.Values.Max(x => x.Item2);
                }

            }
        }
        else
        {
            _dbSecond.Clear();
            volumeKey = 0;
        }

        //every one second, store the current _dbValidValue in the +db5Minutes dictionary. The dictionary should only ever have 5 minutes of data. We use volume1secondGuard to ensure that we only do this once per second.
        if (imitoneVoiceInterpreter.toneActive && _volumeTimeGuardTimer >= 1.0f)
        {
            _volumeTimeGuardTimer = 0.0f;
            volumeKey2 ++;

            if(_dbHighestLastSecond!=null && _dbLowestLastSecond!=null)
            {
                _db5Minutes.Add(volumeKey2, (Time.time, _dbHighestLastSecond, _dbLowestLastSecond));
                //if any entries are older than 5 minutes, remove them:
                _db5Minutes = _db5Minutes.Where(x => Time.time - x.Value.Item1 < 300).ToDictionary(x => x.Key, x => x.Value);

                //send the lowest value recorded for "_dbValidValue" to _dbLowest, and the highest value recorded for "_dbLowestLastSecond" to _dbHighest

                //if there are at least 10 entries in the dictionary, set _dbLowest and _dbHighest
                if (_db5Minutes.Count >= 10)
                {
                    _dbHighest = _db5Minutes.Values.Max(x => x.Item3);
                    _dbLowest = Mathf.Min(_db5Minutes.Values.Min(x => x.Item2), _dbHighest - 6);
                }
            }
            
            Debug.Log("Set DB Highest: " + _dbHighest + " DB Lowest: " + _dbLowest);
        }

        if (imitoneVoiceInterpreter.toneActive)
       _volumeTimeGuardTimer += Time.fixedDeltaTime;
        
        //calculate _loudnessRelative as the _dbValidValue mapped to the range of _dbLowest to _dbHighest

        if (_dbLowest!=null && _dbHighest!=null && _dbValidValue!=null)
        _loudnessRelative = Mathf.Clamp(Mathf.InverseLerp(_dbLowest, _dbHighest, _dbValidValue), 0, 1);

        Debug.Log("Time = " + Time.time + ", DB Valid: " + _dbValidValue + ", Loudness Relative: " + _loudnessRelative + ", DB  Range: " + _dbLowest + ", " + _dbHighest);
        //Debug.Log("Time.FrameCount = " + Time.frameCount + ", Time = " + Time.time);
    }

    private void handlecChanting(){

        //Set interpoloation speeds
        _meanToneLengthLerp = LerpUtilities.DampTool("meanToneLengthLerp", _meanToneLengthLerp, respirationTracker._meanToneLength, 0.036f, 0.018f, 0.0001f);

        // the quicker values were painstakingly set to match the original soundself defaults
        _chantLerpSlowDamp1 = LerpUtilities.LerpAndInverse(_meanToneLengthLerp, 4f, 12f, 0.036f, 0.025f, true);
        _chantLerpSlowDamp2 = LerpUtilities.LerpAndInverse(_meanToneLengthLerp, 4f, 12f, 0.018f, 0.009f, true);
        _chantLerpSlowDepletion = LerpUtilities.LerpAndInverse(_meanToneLengthLerp, 4f, 12f, 0.036f, 0.025f, true);
        _chantLerpFastDamp1 = LerpUtilities.LerpAndInverse(_meanToneLengthLerp, 4f, 12f, 0.09f, 0.045f, true);
        _chantLerpFastDamp2 = LerpUtilities.LerpAndInverse(_meanToneLengthLerp, 4f, 12f, 0.045f, 0.0225f, true);
        _chantLerpFastDepletion = LerpUtilities.LerpAndInverse(_meanToneLengthLerp, 4f, 12f, 0.09f, 0.045f, true);


        // Update cChanting to move towards the target
        if(imitoneVoiceInterpreter.toneActive)
        {
            _lerpTargetSlow = Mathf.Lerp(_lerpTargetSlow, 1.0f, _chantLerpSlowDamp1);
            _chantLerpSlow = Mathf.Lerp(_chantLerpSlow, _lerpTargetSlow, _chantLerpSlowDamp2);

            _lerpTargetFast = Mathf.Lerp(_lerpTargetFast, 1.0f, _chantLerpFastDamp1);
            _chantLerpFast = Mathf.Lerp(_chantLerpFast, _lerpTargetFast, _chantLerpFastDamp2);
            
            //Debug.Log("1: SlowTarget: " + _lerpTargetSlow + " Slow: " + _chantLerpSlow + " FastTarget: " + _lerpTargetFast + " Fast: " + _chantLerpFast);
        }
        else
        {
            _lerpTargetSlow = Mathf.Lerp(_lerpTargetSlow, 0.0f, _chantLerpSlowDamp1);
            _lerpTargetSlow = Mathf.Clamp(_lerpTargetSlow - _chantLerpSlowDepletion, 0, 1);
            _chantLerpSlow = Mathf.Lerp(_chantLerpSlow, _lerpTargetSlow, _chantLerpSlowDamp2);

            _lerpTargetFast = Mathf.Lerp(_lerpTargetFast, 0.0f, _chantLerpFastDamp1);
            _lerpTargetFast = Mathf.Clamp(_lerpTargetFast - _chantLerpFastDepletion, 0, 1);
            _chantLerpFast = Mathf.Lerp(_chantLerpFast, _lerpTargetFast, _chantLerpFastDamp2);
            
            //Debug.Log("0: SlowTarget: " + _lerpTargetSlow + " Slow: " + _chantLerpSlow + " FastTarget: " + _lerpTargetFast + " Fast: " + _chantLerpFast);
        }

        if (_chantLerpSlow < _lerpTargetSlow)
        _chantLerpSlow = Mathf.Clamp(_chantLerpSlow + _chantLerpLinear, 0, 1);
        if (_chantLerpSlow > _lerpTargetSlow)
        _chantLerpSlow = Mathf.Clamp(_chantLerpSlow - _chantLerpLinear, 0, 1);
        if (_chantLerpFast < _lerpTargetFast)
        _chantLerpFast = Mathf.Clamp(_chantLerpFast + _chantLerpLinear, 0, 1);
        if (_chantLerpFast > _lerpTargetFast)
        _chantLerpFast = Mathf.Clamp(_chantLerpFast - _chantLerpLinear, 0, 1);
    }

    private IEnumerator ChantChargeMemberCoroutine(float _forceDurationToFill= 0f) {
        int chantChargeCoroutineID = chantChargeCoroutineCounter++;
        float _meanToneAtStart = respirationTracker._meanToneLength;
        float _normalizedToneTime = 0f;
        float _lerpTarget1        = 0f;
        float _lerpTarget2      = 0f;
        float _forceDurationToFillAtStart = _forceDurationToFill;
        _chantChargeContributions[chantChargeCoroutineID] = 0f;
        string localID = $"{nameof(_chantChargeContributions)}.{chantChargeCoroutineID}";

        Debug.Log("ChantCharge " + chantChargeCoroutineID + " Begin, mean tone is: " + _meanToneAtStart);

        //lerp up to 1 over the course of the tone
        while (imitoneVoiceInterpreter._tThisToneBiasTrue > 0f) 
        {
            if(_forceDurationToFillAtStart != 0f)
            _normalizedToneTime = Mathf.Clamp(imitoneVoiceInterpreter._tThisToneBiasTrue / _forceDurationToFill, 0f, 1f);
            else
            _normalizedToneTime = Mathf.Clamp(imitoneVoiceInterpreter._tThisToneBiasTrue / Mathf.Clamp((_meanToneAtStart * 0.8f), 5f, 20f), 0f, 1f);

            _chantChargeContributions[chantChargeCoroutineID] = LerpUtilities.DampTool(localID, _chantChargeContributions[chantChargeCoroutineID], _normalizedToneTime, _chantChargeDamp1, _chantChargeDamp2, _chantChargeLinear);

            yield return null;
        }

        //Debug.Log("ChantCharge " + chantChargeCoroutineCounter + " Fade Out from " + _chantChargeContributions[chantChargeCoroutineID] + " _lerpTarget1: " + _lerpTarget1 + " _lerpTarget2: " + _lerpTarget2);

        //lerp down to 0 after the tone ends
        while (_chantChargeContributions[chantChargeCoroutineID] > 0f) 
        {
            _chantChargeContributions[chantChargeCoroutineID] = LerpUtilities.DampTool(localID, _chantChargeContributions[chantChargeCoroutineID], 0f, _chantChargeDamp1, _chantChargeDamp2, _chantChargeLinear);

            yield return null;
        }
        _chantChargeContributions.Remove(chantChargeCoroutineID);
        LerpUtilities.CleanUpDampTool(localID);
        
        //Debug.Log("ChantCharge " + chantChargeCoroutineCounter + " End");
    }

}
