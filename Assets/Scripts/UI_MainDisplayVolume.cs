using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_MainDisplayVolume : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public Image fillImage;
    public float minValue;
    public float maxValue;
    public float OrginalValue = -15.0f;

    void Start(){
        minValue = -50.0f;
        maxValue = -10.0f;
        fillImage = GetComponent<Image>();
        fillImage.fillAmount = 0.5f;
    }
    void Update()
    {
        UpdateFill();
    }
    void UpdateFill()
    {
        float normalizedValue = Mathf.Clamp01((ImitoneVoiceIntepreter._dbValue - minValue) / (maxValue - minValue));
        fillImage.fillAmount = normalizedValue;
    }
}
