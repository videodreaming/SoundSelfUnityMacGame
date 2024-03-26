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

    //Are we using these actions?
    public Action ChantEvent;
    public Action BreathEvent;

    
    [Header("DampingValues")]
    private float responsiveness = 0.25f; //revisit when we have absorption
    //CHANTLERP
    private float damp1 = 0.08f;
    private float damp2 = 0.0f;
    public float _chantLerpFast = 0.0f;
    public float _chantLerpSlow = 0.0f;
    private float _chantLerpSlow_lerp1 = 0.0f;
    private float _chantLerpFast_lerp1 = 0.0f;

    //CHANTCHARGE
    private float RecentToneMeanDuration = 5.0f; //resvisit when we have cadence/respirationrate.
    private float _chantChargeDamp1 = 0.2f;
    private float _chantChargeDamp2 = 0.1f;
    private float _chantChargeLinear = 0.001f;
    private bool chantChargeToneGuard   = false;
    public float _chantCharge;   
    
    private Dictionary<int, float> _chantChargeContributions = new Dictionary<int, float>();
    private int chantChargeCoroutineCounter = 0;

   
    void Start()
    {
    }

    // Fixed Update is called once per frame, but it is called on a fixed time step.
    void FixedUpdate()
    {
        handlecChanting();

        //HANDLE "CHANT CHARGE"
        //if we are in a guided vocalization state, set the full value to 5, otherwise it sets dynamically.
        float _durationToFill = 0f;
        if (imitoneVoiceInterpreter._tThisTone > 0 && !chantChargeToneGuard)
        {
            chantChargeToneGuard = true;
            if(AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationAdvanced
            ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationAhh
            ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationOhh
            ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationHum)
            {
                _durationToFill = 5.0f;
            }
            StartCoroutine(ChantChargeMemberCoroutine(_durationToFill));
        }
        _chantCharge = _chantChargeContributions.Values.Sum();
    }

    private void handlecChanting(){

        // Update cChanting to move towards the target
        _chantLerpSlow_lerp1 = Mathf.Lerp(_chantLerpSlow_lerp1, imitoneVoiceInterpreter.chantLerpTarget, damp1);
        _chantLerpFast_lerp1 = Mathf.Lerp(_chantLerpFast_lerp1, imitoneVoiceInterpreter.chantLerpTarget, damp1*2.0f);

        if(imitoneVoiceInterpreter.chantLerpTarget == 0)
        {
            _chantLerpSlow_lerp1 -= damp1;  
            _chantLerpFast_lerp1 -= damp1*2.0f;
        }

        damp2 = 0.04f * (float)Math.Pow(2.0f, responsiveness) / Math.Min(Math.Max(RecentToneMeanDuration, 0.25f), 10.0f);

        _chantLerpSlow = Mathf.Lerp(_chantLerpSlow, _chantLerpSlow_lerp1, damp2); 
        _chantLerpFast = Mathf.Lerp(_chantLerpFast, _chantLerpFast_lerp1, damp2*2); 

        _chantLerpSlow = Mathf.Clamp(_chantLerpSlow, 0f, 1f);
        _chantLerpFast = Mathf.Clamp(_chantLerpFast, 0f, 1f);
    }

    private IEnumerator ChantChargeMemberCoroutine(float _forceDurationToFill= 0f) {
        int chantChargeCoroutineID = chantChargeCoroutineCounter++;
        float _meanToneAtStart = respirationTracker._meanToneLength;
        float _normalizedToneTime = 0f;
        float _lerpTarget1        = 0f;
        float _lerpTarget2      = 0f;

        _chantChargeContributions[chantChargeCoroutineID] = 0f;

        //lerp up to 1 over the course of the tone
        while (imitoneVoiceInterpreter._tThisTone > 0f) 
        {
            if(_forceDurationToFill != 0f)
            _normalizedToneTime = Mathf.Clamp(imitoneVoiceInterpreter._tThisTone / _forceDurationToFill, 0f, 1f);
            else
            _normalizedToneTime = Mathf.Clamp(imitoneVoiceInterpreter._tThisTone / Mathf.Clamp((_meanToneAtStart * 0.8f), 5f, 20f), 0f, 1f);

            //damp, then damp, then linear
            _lerpTarget1 = Mathf.Lerp(_lerpTarget1, _normalizedToneTime, _chantChargeDamp1);
            _lerpTarget2 = Mathf.Lerp(_lerpTarget2, _lerpTarget1, _chantChargeDamp2);
            _chantChargeContributions[chantChargeCoroutineID] = Mathf.Min(_lerpTarget2 + _chantChargeLinear, _normalizedToneTime);

            yield return null;
        }

        //lerp down to 0 after the tone ends
        while (_chantChargeContributions[chantChargeCoroutineID] > 0f) 
        {
            _lerpTarget1 = Mathf.Lerp(_lerpTarget1, 0f, _chantChargeDamp1);
            _lerpTarget2 = Mathf.Lerp(_lerpTarget2, _lerpTarget1, _chantChargeDamp2);
            _chantChargeContributions[chantChargeCoroutineID] = Mathf.Max(0f, _lerpTarget2 - _chantChargeLinear);
            yield return null;
        }
        _chantChargeContributions.Remove(chantChargeCoroutineID);
    }

}
