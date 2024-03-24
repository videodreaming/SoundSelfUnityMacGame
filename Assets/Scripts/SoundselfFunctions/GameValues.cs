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
    private float lerpedMemory1 = 0.0f;
    private float lerpedMemory2 = 0.0f;
    private float mean = 0.0f;
    private float fullValue1 = 0.0f;
    private float fullValue2 = 0.0f;
    private float chantChargeCurve1 = 0.0f;
    private float chantChargeCurve2 = 0.0f;
    public float _cChantCharge;   

   
    void Start()
    {
    }

    // Fixed Update is called once per frame, but it is called on a fixed time step.
    void FixedUpdate()
    {
        handlecChanting();
        //cChantingModifications();
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

        getChantCharge();
    }
    
    private void getChantCharge()
    {
        lerpedMemory1 = Mathf.Lerp(lerpedMemory1, RecentToneMeanDuration,0.05f);
        lerpedMemory2 = Mathf.Lerp(lerpedMemory2, lerpedMemory1, 0.01f + 0.0125f * imitoneVoiceInterpreter._breathVolume);
        if(AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationAdvanced
        ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationAhh
        ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationOhh
        ||AudioManager.currentState == AudioManager.AudioManagerState.GuidedVocalizationHum){
            fullValue2 = 5;
        } else {
            mean = (lerpedMemory2 + 10.0f) / 2.0f;
            fullValue1 = Mathf.Lerp(fullValue1, mean, 0.05f);
            fullValue2 = Mathf.Lerp(fullValue2, fullValue1, 0.01f + 0.0125f * imitoneVoiceInterpreter._breathVolume);
        }

        if(_chantLerpSlow == 0.0f){
            chantChargeCurve1 = 0.0f;
            chantChargeCurve2 = 0.0f;
        }
        else{
            chantChargeCurve1 = Mathf.Lerp(chantChargeCurve1, Math.Min(imitoneVoiceInterpreter._tThisTone/Math.Max(fullValue2,1.0f),1.0f),0.05f);
            chantChargeCurve2 = Mathf.Lerp(chantChargeCurve2, chantChargeCurve1, 0.01f + 0.0125f * imitoneVoiceInterpreter._breathVolume);
        }

        _cChantCharge = chantChargeCurve2 * _chantLerpSlow;
        _cChantCharge = Mathf.Clamp(_cChantCharge, 0.0f, 1.0f);
        //Debug.Log("Target = " + imitoneVoiceInterpreter.chantLerpTarget + "       _tThisTone =" + _tThisTone + "       _chantLerpSlow =" + _chantLerpSlow + "        _chantLerpFast = " + _chantLerpFast + "         ChantChargeCurve = " + chantChargeCurve2 + "        _chantChargeFINAL = " + _cChantCharge);
    }
}
