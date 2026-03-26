using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugVolume : MonoBehaviour
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
        if(ImitoneVoiceIntepreter.imitoneActive == true){
            fillImage.color = Color.blue;
        } else {
            fillImage.color = new Color(134.0f/255f, 255.0f/255f, 92.0f/255f, 255.0f/255f);
        }
    }
}
