using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugHarmonicity : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public Image fillImage;
    public float minValue;
    public float maxValue;
    public float OrginalValue = -15.0f;

    void Start(){
        minValue = 0.0f;
        maxValue = 1.0f;
        fillImage = GetComponent<Image>();
        fillImage.fillAmount = 0.5f;
    }
    void Update()
    {
        UpdateHarmFill();
    }
    void UpdateHarmFill()
    {
        //Debug.Log("OrginalValue: " + OrginalValue);
        //Debug.Log("minValue: " + minValue);
        //Debug.Log("maxValue: " + maxValue);
        float normalizedValue = Mathf.Clamp01((ImitoneVoiceIntepreter._harmonicity - minValue) / (maxValue - minValue));
        //Debug.Log("Normalized Value: " + normalizedValue);
        //Debug.Log("DBValue " + ImitoneVoiceIntepreter._dbValue);
        fillImage.fillAmount = normalizedValue;

        if(ImitoneVoiceIntepreter.imitoneActive == true){
            fillImage.color = Color.blue;
        } else if(ImitoneVoiceIntepreter.imitoneActive == false) {
            //Debug.Log("ImitoneVoiceIntepreter.imitoneActive == false");
            fillImage.color = new Color(176.0f / 255.0f, 113.0f / 255.0f, 167.0f / 255.0f, 1.0f);
        }
    }
}
