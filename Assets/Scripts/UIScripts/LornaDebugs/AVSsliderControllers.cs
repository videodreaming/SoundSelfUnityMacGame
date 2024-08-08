using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AVSsliderControllers : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI AVS_Red_Volume_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Blue_Volume_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Green_Volume_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_MasterVolume_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Modulation_BypassEffect_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Modulation_Depth_Wave_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Modulation_Frequency_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Modulation_MonoStereo_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Modulation_PWM_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Modulation_Smoothing_1_Text = null;
    [SerializeField] private TextMeshProUGUI AVS_Modulation_Waveform_1_Text = null;
    
    //AVS Wave 1

    /*
    void Start()
    {
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave1",100.0f,gameObject);
    }
    public void RedSliderChange(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_RED_Volume_Wave_1", localValue, gameObject);
        AVS_Red_Volume_1_Text.text = localValue.ToString("0");
    }

    public void BlueSliderChange(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_BLUE_Volume_Wave_1", localValue, gameObject);
        AVS_Blue_Volume_1_Text.text = localValue.ToString("0");
    }
    public void GreenSliderChange(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_GREEN_Volume_Wave_1", localValue, gameObject);
        AVS_Green_Volume_1_Text.text = localValue.ToString("0");
    }

    public void AVS_Modulation_BypassEffect_1_Change(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave1", localValue, gameObject);
        AVS_Green_Volume_1_Text.text = localValue.ToString("0");
    }
    public void AVS_Modulation_Depth_Wave_1_Change(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", localValue, gameObject);
        AVS_Green_Volume_1_Text.text = localValue.ToString("0");
    }
    public void AVS_Modulation_Frequency_1_Change(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave1", localValue, gameObject);
        AVS_Green_Volume_1_Text.text = localValue.ToString("0");
    }
    public void AVS_Modulation_MonoStereo_1_Change(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_Modulation_MonoStereo_Wave1", localValue, gameObject);
        AVS_Green_Volume_1_Text.text = localValue.ToString("0");
    }
    public void AVS_Modulation_PWMs_1_Change(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave1", localValue, gameObject);
        AVS_Green_Volume_1_Text.text = localValue.ToString("0");
    }
    public void AVS_Modulation_Smoothing_1_Change(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Smoothing_Wave1", localValue, gameObject);
        AVS_Green_Volume_1_Text.text = localValue.ToString("0");
    }
    
    public void AVS_Modulation_Waveform_1_Change(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Waveform_Wave1", localValue, gameObject);
        AVS_Green_Volume_1_Text.text = localValue.ToString("0");
    }

    public void MasterSineSliderChange(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave1", localValue, gameObject);
        AVS_MasterVolume_1_Text.text = localValue.ToString("0");
    }
    */
}
