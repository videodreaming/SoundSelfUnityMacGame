using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugImitoneActive : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public Image fillImage;
    void Start(){
        fillImage = GetComponent<Image>();
    }
    void Update()
    {
        UpdateFill();
    }
    void UpdateFill()
    {
        if(ImitoneVoiceIntepreter.toneActive == true){
            fillImage.color = Color.blue;
        } else {
            fillImage.color = new Color(67.0f/255.0f, 147.0f/255.0f, 92.0f/255.0f, 255.0f/255.0f);
        }
    }
}
