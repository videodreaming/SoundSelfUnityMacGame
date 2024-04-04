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

    public void SilentFundamentalSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", localValue);
        silentLoopsFundamentalText.text = localValue.ToString("0");
    }

    public void SilentHarmonySlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", localValue);
        silentLoopsHarmonyText.text = localValue.ToString("0");
    }

     public void VoBusSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("VO_Bus_Volume", localValue);
        VOBusText.text = localValue.ToString("0");
    }

     public void InteractiveBusSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("InteractiveMusic_Bus_Volume", localValue);
        silentInteractiveBusText.text = localValue.ToString("0");
    }

    public void MasterVolumeSlider(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("Master_Volume", localValue);
        masterSliderText.text = localValue.ToString("0");
    }


}
