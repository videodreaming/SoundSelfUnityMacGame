using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class PitchUIScript : MonoBehaviour
{
    public ImitoneVoiceIntepreter mainImitone;
    [SerializeField] private TextMeshProUGUI pitchText;
    public void Update(){
        pitchText.text = mainImitone.pitch_hz.ToString();
    }
}

