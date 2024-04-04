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



}
