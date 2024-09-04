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

public class GameValues : MonoBehaviour
{
    public DevelopmentMode developmentMode;
    public AudioManager AudioManager;
    public Director director;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    public RespirationTracker respirationTracker;
    public LightControl lightControl;
    private bool debugAllowChangeVerboseLogs = false;
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
    public float _loudnessRelative {get; private set;} = 0.5f; //THIS IS A GOOD METRIC, NOT USED YET.
    private float _dbLowest;
    private float _dbHighest;
    private Dictionary <int, (float, float, float)> _db5Minutes = new Dictionary<int, (float, float, float)>(); //time, high-second, low-second
    private bool guard1 = false;
    private bool guard2 = false;

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
    public float _tChantLerp {get; private set;} = 0.0f; //not currently referenced, but might be useful for WWise
    public float _tRestLerp {get; private set;} = 0.0f; //not currently referenced, but might be useful for WWise

    //CHANTCHARGE
    private float _chantChargeDamp1 = 0.01f;
    private float _chantChargeDamp2 = 0.005f;
    private float _chantChargeLinear = 0.00005f;
    private bool chantChargeToneGuard   = false;
    public float _chantCharge {get; private set;} = 0f;   
    
    float _chantChargeDuration = 0f; // 0 does not force
    private Dictionary<int, float> _chantChargeContributions = new Dictionary<int, float>(); 
    private int chantChargeCoroutineCounter = 0;

    //CHANGE DETECTION
    private Dictionary<int, float> _toneDurations = new Dictionary<int, float>();
    private Dictionary<int, float> _restDurations = new Dictionary<int, float>();
    public bool changeDetectedToneLength = false;
    public bool changeDetectedRestLength = false;
    private int toneRestCounterMax = 3; //not including the current one...
    private int toneDurationCounter = 0;
    private int restDurationCounter = 0;
    private bool toneDurationGuard = false;
    private bool restDurationGuard = false;    
    private float _toneAnchorHigh;
    private float _toneAnchorLow;
    private bool toneAnchored = false;
    private float _restAnchorHigh;
    private float _restAnchorLow;
    private bool restAnchored = false;
    private float _toneWindlassSpread;
    private float _restWindlassSpread;
    //The below 4 values require tweaking from gameplay observations. Notes from changes in comments below.
    private float _toneWindlassSpreadInitialize = 0.15f;
    private float _restWindlassSpreadInitialize = 0.075f; //Needs to be lower than toneWindlassSpreadInitialize
    private float _windlassSpreadGrowthPerMinute_Init = 0.025f; // * mean duration
    private float _windlassSpreadGrowthPerMinute = 0.025f; // * mean duration
    private float _anchorSpreadShrinkPerMinute_Init = 0.025f; // * mean duration
    private float _anchorSpreadShrinkPerMinute = 0.025f; // * mean duration
    private float _anchorSetMult = 2.0f;

    void Start()
    {
        _toneWindlassSpread = _toneWindlassSpreadInitialize;
        _restWindlassSpread = _restWindlassSpreadInitialize;
    }

    void FixedUpdate()
    {
            
        // Fixed Update is called once per frame, but it is called on a fixed time step.
        // THIS IS A PROBLEM TO CONSIDER FIXING IN THE FUTURE, BY CAREFULLY MOVING EVERYTHING TO UPDATE()
        // Because this happens on a different time step to the rest of the game, we can't trust it
        // to use any kind of "one-frame" logic.
        
        handlecChanting();
        handleVolume();
        handleChantCharge();
    }

    void Update()
    {
        //probably handleVolume() could go in here too, with a little tweaking. cChanting and chantCharge should stay in FixedUpdate unlesss you want to fiddle deeper with the lerp tools.
        changeDetection();
        if(changeDetectedToneLength)
        {
            Debug.Log("Change Detection: Tone Length Change");
        }
        if(changeDetectedRestLength)
        {
            Debug.Log("Change Detection: Breath Length Change");
        }
        
        lightControl.Wwise_Strobe_ChargeDisplay(_chantCharge);
        lightControl.Wwise_Strobe_ToneDisplay(_chantLerpFast);
    }

    private void handleVolume()
    {
        
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
                    guard1 = true;
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

            if(guard1)
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
                    guard2 = true;
                }
            }
            
            //Debug.Log("Set DB Highest: " + _dbHighest + " DB Lowest: " + _dbLowest);
        }

        if (imitoneVoiceInterpreter.toneActive)
       _volumeTimeGuardTimer += Time.fixedDeltaTime;
        
        //calculate _loudnessRelative as the _dbValidValue mapped to the range of _dbLowest to _dbHighest

        if (guard1 && guard2)
        {
            _loudnessRelative = Mathf.Clamp(Mathf.InverseLerp(_dbLowest, _dbHighest, _dbValidValue), 0, 1);
        }
        //Debug.Log("Time = " + Time.time + ", DB Valid: " + _dbValidValue + ", Loudness Relative: " + _loudnessRelative + ", DB  Range: " + _dbLowest + ", " + _dbHighest);
        //Debug.Log("Time.FrameCount = " + Time.frameCount + ", Time = " + Time.time);
    }

    private void handlecChanting()
    {

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

        
        if (_chantLerpSlow > 0.0f)
        _tChantLerp += Time.fixedDeltaTime * _chantLerpSlow;
        if (_chantLerpSlow < 1.0f)
        _tRestLerp += Time.fixedDeltaTime * (1.0f - _chantLerpSlow);

        AkSoundEngine.SetRTPCValue("Unity_ChantLerpFast", _chantLerpFast * 100.0f, gameObject);
        AkSoundEngine.SetRTPCValue("Unity_ChantLerpSlow", _chantLerpSlow * 100.0f, gameObject);
    }

    private void handleChantCharge()
    {
        //HANDLE "CHANT CHARGE"
        //if we are in a guided vocalization state, set the full value to 5, otherwise it sets dynamically.
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
                _chantChargeDuration = 5.0f;
            }
            else
            {
                _chantChargeDuration = Mathf.Clamp((respirationTracker._meanToneLength * 0.8f), 5f, 20f);
            }
            StartCoroutine(ChantChargeMemberCoroutine(_chantChargeDuration));
        }
        _chantCharge = Mathf.Clamp(_chantChargeContributions.Values.Sum(), 0,1);
        
        AkSoundEngine.SetRTPCValue("Unity_Charge", _chantCharge*100.0f, gameObject);
    }

    private IEnumerator ChantChargeMemberCoroutine(float _chantChargeDuration= 0f) 
    {
        int chantChargeCoroutineID = chantChargeCoroutineCounter++;
        float _normalizedToneTime = 0f;
        float _chantChargeDurationAtStart = _chantChargeDuration;

        _chantChargeContributions[chantChargeCoroutineID] = 0f;
        
        string localID = $"{nameof(_chantChargeContributions)}.{chantChargeCoroutineID}";

        //Debug.Log("ChantCharge " + chantChargeCoroutineID + " Begin, mean tone is: " + _meanToneAtStart);

        //lerp up to 1 over the course of the tone
        while (imitoneVoiceInterpreter._tThisToneBiasTrue > 0f) 
        {
            _normalizedToneTime = Mathf.Clamp(imitoneVoiceInterpreter._tThisToneBiasTrue / _chantChargeDurationAtStart, 0f, 1f);
            

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

    private void changeDetection()
    {
        _windlassSpreadGrowthPerMinute = _windlassSpreadGrowthPerMinute_Init * Mathf.Pow(2, (1-Mathf.Clamp(respirationTracker._absorption, 0, 1)));
        _anchorSpreadShrinkPerMinute = _anchorSpreadShrinkPerMinute_Init * Mathf.Pow(2,(1-Mathf.Clamp(respirationTracker._absorption, 0, 1)));

        // Track the three most recent tone and breath durations from imitoneVoiceInterpreter using a library of the last 3 values
        if (imitoneVoiceInterpreter.toneActiveConfident)
        {
            if (toneDurationGuard == false)
            {
                StartCoroutine(DurationCoroutine(true));
                toneDurationGuard = true;
            }
        }
        else
        {
            if (restDurationGuard == false)
            {
                
                StartCoroutine(DurationCoroutine(false));
                restDurationGuard = true;
            }
        }

        if(changeDetectedToneLength || changeDetectedRestLength)
        {
            float flourishTime = 0.0f;
            if(changeDetectedToneLength)
            {
                flourishTime = imitoneVoiceInterpreter.toneActiveConfident ? 3.0f : 7.0f;
                //ChangeColor(imitoneVoiceInterpreter.toneActiveConfident ? 3.0f : 7.0f);
            }
            else if(changeDetectedRestLength)
            {
                flourishTime = 5.0f;
                //ChangeColor(5.0f);
            }
            
            director.ActivateQueue(flourishTime); //whenever there is a change detected, process any queued a/v actions

        }
    }

    // A coroutine that will be used to measure the duration of most recent tones or breaths
    private IEnumerator DurationCoroutine(bool isTone)
    {
        int counter = isTone ? toneDurationCounter++ : restDurationCounter++;
        int id = isTone ? toneDurationCounter : restDurationCounter;
        float _duration = 0.0f;
        bool triggeredChange = false;
        float _windlassSpread = isTone ? _toneWindlassSpread : _restWindlassSpread;
        float _anchorLow = isTone ? _toneAnchorLow : _restAnchorLow;
        float _anchorHigh = isTone ? _toneAnchorHigh : _restAnchorHigh;
        bool anchored = isTone ? toneAnchored : restAnchored;
        Dictionary<int, float> _durations = isTone ? _toneDurations : _restDurations;
        float _durationsMean = (_durations.Count > 0) ? _durations.Values.Average() : 0.0f;
        float _durationsRelativeRange = (_durations.Count > 0) ? ((_durations.Values.Max() - _durations.Values.Min()) / _durationsMean) : 0.0f;
        
        if (debugAllowChangeVerboseLogs) //(isTone)
        {
            if (anchored)
            {
                Debug.Log("Change Detection " + (isTone ? "TONE " : "REST ") + id + ": COROUTINE START, ANCHORED (" + _anchorLow + ")|(" + _anchorHigh + ")");
            }
            else
            {
                Debug.Log("Change Detection " + (isTone ? "TONE " : "REST ") + id + ": COROUTINE START unanchored with range threshold (" + _windlassSpread + ")");
            }
        }


        while (isTone == imitoneVoiceInterpreter.toneActiveConfident)
        {
            _duration += Time.deltaTime;//Time.fixedDeltaTime;

            //EARLY CHANGE DETECTION FOR EXCEEDING THRESHOLD DURING TONING ONLY
            if(anchored && isTone && !triggeredChange && (_duration > _anchorHigh))
            {
                triggeredChange = true;
                StartCoroutine(ToneChangeEvent());
                if(debugAllowChangeVerboseLogs)
                {
                    Debug.Log("Change Detection TONE: [CHANGE] " + id + " by exceeding upper threshold " + _anchorHigh);
                }
            }

            yield return null;
        }

        //at end of measurement, add to the dictionary, remove excess values, and do a test for change detection
        _durations.Add(id, _duration);
        if (_durations.Count > toneRestCounterMax)
        {
            _durations = _durations.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            _durations.Remove(_durations.Keys.First());
            _durationsMean = _durations.Values.Average();
            _durationsRelativeRange = (_durations.Values.Max() - _durations.Values.Min()) / _durations.Values.Average();

            if (anchored) //ANCHORED, MIGHT UNANCHOR
            {
                if(isTone && !triggeredChange && (_duration < _anchorLow))
                {
                    triggeredChange = true;
                    StartCoroutine(ToneChangeEvent());
                    if(debugAllowChangeVerboseLogs)
                        Debug.Log("Change Detection TONE: [CHANGE] " + id + " by falling below lower threshold " + _anchorLow);
                }
                else if (!isTone && !triggeredChange && anchored && ((_duration < _anchorLow) || (_duration > _anchorHigh)))
                {
                    triggeredChange = true;
                    StartCoroutine(RestChangeEvent());
                    if(debugAllowChangeVerboseLogs)
                        Debug.Log("Change Detection REST: [CHANGE] " + id + " by ranging out of threshold (" + _anchorLow + ")(" + _anchorHigh + ")");
                }
               

                if(triggeredChange) //IF A CHANGE WAS DETECTED, AND WE NOW UNANCHOR...
                {
                    anchored = false;
                    _windlassSpread = isTone ? _toneWindlassSpreadInitialize : _restWindlassSpreadInitialize;
                    if(isTone)
                    {
                        toneAnchored = false;
                        _toneWindlassSpread = _windlassSpread;
                    }
                    else
                    {
                        restAnchored = false;
                        _restWindlassSpread = _windlassSpread;
                    }
                    
                    Debug.Log("Change Detection" + (isTone ? " TONE: " : " REST: ") + "unanchored with range threshold(" + _windlassSpread + ")");
                }
                else //IF NO CHANGE WAS DETECTED, SHRINK THE ANCHORED RANGE
                {
                    _anchorHigh -= _anchorSpreadShrinkPerMinute * 0.5f * _durationsMean * _duration / 60f;
                    _anchorLow += _anchorSpreadShrinkPerMinute * 0.5f * _durationsMean * _duration / 60f;
                    if(isTone)
                    {
                        _toneAnchorHigh = _anchorHigh;
                        _toneAnchorLow = _anchorLow;   
                    }
                    else
                    {
                        _restAnchorHigh = _anchorHigh;
                        _restAnchorLow = _anchorLow;
                    }
                    
                    if(debugAllowChangeVerboseLogs)
                        Debug.Log("Change Detection" + (isTone ? " TONE: " : " REST: ") + "anchored range set to (" + _anchorLow + ")|(" + _anchorHigh + ")");
                }
            }
            else //NOT ANCHORED, MIGHT SET ANCHOR
            {
                if(_durationsRelativeRange < _windlassSpread) //if we are setting an anchor...
                {
                        _anchorHigh = _durationsMean + _windlassSpread * 0.5f *_anchorSetMult;
                        _anchorLow  = _durationsMean - _windlassSpread * 0.5f *_anchorSetMult;
                        anchored = true;
                        
                        if(isTone)
                        {
                            _toneAnchorHigh = _anchorHigh;
                            _toneAnchorLow = _anchorLow;
                            toneAnchored = true;
                            Debug.Log("Change Detection TONE: [ANCHOR SET] (" + _toneAnchorLow + ")|(" + _toneAnchorHigh + ")");
                        }
                        else
                        {
                            _restAnchorHigh = _anchorHigh;
                            _restAnchorLow = _anchorLow;
                            restAnchored = true;
                            Debug.Log("Change Detection REST: [ANCHOR SET] (" + _restAnchorLow + ")|(" + _restAnchorHigh + ")");
                        }
                }
                else//if we are not setting an anchor...
                {
                    if(isTone)
                    {
                        _toneWindlassSpread += _windlassSpreadGrowthPerMinute * _durationsMean * _duration / 60f;
                    }
                    else
                    {
                        _restWindlassSpread += _windlassSpreadGrowthPerMinute * _durationsMean * _duration / 60f;
                    }
                    
                    if(debugAllowChangeVerboseLogs)
                    Debug.Log("Change Detection" + (isTone ? " TONE (no anchor): " : " REST (no anchor): ") + "memoryMin(" + _durations.Values.Min() + ") memoryMax(" + _durations.Values.Max() + ") range(" + _durationsRelativeRange + ") next-threshold set to ("  + _windlassSpread + ")");
                }
                
            }

        }

        
        if(!anchored && debugAllowChangeVerboseLogs)
        {
            Debug.Log("Change Detection " + (isTone ? "TONE (no anchor):" : "REST (no anchor):") + " Duration Coroutine " + id + " END with duration(" + _duration + ") count(" + _durations.Count + ") range(" + _durationsRelativeRange + ")  nextRangeThreshold(" + _windlassSpread + ")");
        }
        else if (anchored && debugAllowChangeVerboseLogs)
        {
            Debug.Log("Change Detection " + (isTone ? "TONE (anchored):" : "REST (anchored):") + " Duration Coroutine " + id + " END with duration(" + _duration + ") count(" + _durations.Count + ") anchors(" + _anchorLow + ")|(" + _anchorHigh + ")");
        }
        
        if (isTone)
        {
            _toneDurations = _durations;
            toneDurationGuard = false;

        }
        else
        {
            _restDurations = _durations;
            restDurationGuard = false;
        }

    }

    private IEnumerator ToneChangeEvent() //used to set value for one frame, given coroutine timing
    {
        changeDetectedToneLength = true;
        yield return null;
        changeDetectedToneLength = false;
    }

    private IEnumerator RestChangeEvent() //used to set value for one frame, given coroutine timing
    {
        changeDetectedRestLength = true;
        yield return null;
        changeDetectedRestLength = false;
    }
}
