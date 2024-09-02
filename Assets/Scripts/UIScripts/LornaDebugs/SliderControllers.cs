using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class SliderControllers : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI masterSliderText = null;
    [SerializeField] private TextMeshProUGUI toningFundamentalText = null;
    [SerializeField] private TextMeshProUGUI toningHarmonyText = null;
    [SerializeField] private TextMeshProUGUI silentLoopsFundamentalText = null;
    [SerializeField] private TextMeshProUGUI silentLoopsHarmonyText = null;
    [SerializeField] private TextMeshProUGUI VOBusText = null;
    [SerializeField] private TextMeshProUGUI silentInteractiveBusText = null;

    [SerializeField] private TextMeshProUGUI env1WaveformText = null;
    [SerializeField] private TextMeshProUGUI env1RateText = null;
    [SerializeField] private TextMeshProUGUI env1PWMDutyCycleText = null;
    [SerializeField] private TextMeshProUGUI env1SmoothingText = null;
    [SerializeField] private TextMeshProUGUI env1DepthText = null;

     [SerializeField] private TextMeshProUGUI env2WaveformText = null;
    [SerializeField] private TextMeshProUGUI env2RateText = null;
    [SerializeField] private TextMeshProUGUI env2PWMDutyCycleText = null;
    [SerializeField] private TextMeshProUGUI env2SmoothingText = null;
    [SerializeField] private TextMeshProUGUI env2DepthText = null;

    // Update is called once per frame
    public void SliderChange(float value)
    {
        float localValue = value;
        masterSliderText.text = localValue.ToString("0");
    }

    public void ToningFundamentalSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("FundamentalToningVolume", localValue);
        toningFundamentalText.text = localValue.ToString("0");
    }

    public void ToningHarmonySlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("HarmonyToningVolume", localValue);
        toningHarmonyText.text = localValue.ToString("0");
    }

    // public void SilentFundamentalSlider(float value)
    // {
    //     float localValue = value;
    //     AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", localValue);
    //     silentLoopsFundamentalText.text = localValue.ToString("0");
    // }

    // public void SilentHarmonySlider(float value)
    // {
    //     float localValue = value;
    //     AkSoundEngine.SetRTPCValue("HarmonySilentVolume", localValue);
    //     silentLoopsHarmonyText.text = localValue.ToString("0");
    // }

     public void VoBusSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("VO_Bus_Volume", localValue);
        VOBusText.text = localValue.ToString("0");
    }

    //  public void InteractiveBusSlider(float value)
    // {
    //     float localValue = value;
    //     AkSoundEngine.SetRTPCValue("InteractiveMusic_Bus_Volume", localValue);
    //     silentInteractiveBusText.text = localValue.ToString("0");
    // }

    public void MasterVolumeSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Master_Volume", localValue);
        masterSliderText.text = localValue.ToString("0");
    }

    public void env1WaveformSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env1_Waveform", localValue);
        env1WaveformText.text = localValue.ToString("0");
    }
    public void env1RateSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env1_Rate", localValue);
        env1RateText.text = localValue.ToString("0");
    }

    public void env1PWNDutyCycleSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env1_PWM_DutyCycle", localValue);
        env1PWMDutyCycleText.text = localValue.ToString("0");
    }

        public void env1SmoothingSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env1_Smoothing", localValue);
        env1SmoothingText.text = localValue.ToString("0");
    }

    public void env1DepthSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env1_Depth", localValue);
        env1DepthText.text = localValue.ToString("0");
    }

    public void env2WaveformSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env2_Waveform", localValue);
        env2WaveformText.text = localValue.ToString("0");
    }

    public void env2RateSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env2_Rate", localValue);
        env2RateText.text = localValue.ToString("0");
    }

    public void env2PWMDutyCycleSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env2_PWM_DutyCycle", localValue);
        env2PWMDutyCycleText.text = localValue.ToString("0");
    }
        public void env2SmoothingSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env2_Smoothing", localValue);
        env2SmoothingText.text = localValue.ToString("0");
    }
    public void env2DepthSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Env2_Depth", localValue);
        env2DepthText.text = localValue.ToString("0");
    }



}
