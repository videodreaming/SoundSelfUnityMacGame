using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class SliderControllers : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI masterSliderText = null;

    // Update is called once per frame
    public void SliderChange(float value)
    {
        float localValue = value;
        masterSliderText.text = localValue.ToString("0");
    }
}
