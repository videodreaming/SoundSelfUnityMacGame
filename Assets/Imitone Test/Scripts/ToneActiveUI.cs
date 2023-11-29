using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class ToneActiveUI : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    [SerializeField] private TextMeshProUGUI ToneActiveUIText;
    public void Update(){
        ToneActiveUIText.text = ImitoneVoiceIntepreter.toneActive.ToString();
    }
}
