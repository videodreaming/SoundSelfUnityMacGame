using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class BreatheVolUIScript : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    [SerializeField] private TextMeshProUGUI breatheTotalText;
    public void Update(){
        breatheTotalText.text = ImitoneVoiceIntepreter._breathVolumeTotal.ToString();
    }
}
