using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AVSsliderControllers : MonoBehaviour
{
        [SerializeField] private TextMeshProUGUI redSineSliderText = null;
    [SerializeField] private TextMeshProUGUI blueSineSliderText = null;
        [SerializeField] private TextMeshProUGUI greenSineSliderText = null;
            [SerializeField] private TextMeshProUGUI masterSineSliderText = null;
    public void RedSliderChange(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("RedSineVolume", localValue, gameObject);
        redSineSliderText.text = localValue.ToString("0");
    }

        public void BlueSliderChange(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("BlueSineVolume", localValue, gameObject);
        blueSineSliderText.text = localValue.ToString("0");
    }
        public void GreenSliderChange(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("GreenSineVolume", localValue, gameObject);
        greenSineSliderText.text = localValue.ToString("0");
    }
        public void MasterSineSliderChange(float value)
    {
        float localValue = value;
        AkSoundEngine.SetRTPCValue("AVSMasterVolume", localValue, gameObject);
        masterSineSliderText.text = localValue.ToString("0");
    }
}
