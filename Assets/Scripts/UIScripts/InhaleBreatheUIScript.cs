using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class InhaleBreatheVolUIScript : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    [SerializeField] private TextMeshProUGUI inhaleTotalText;
    public void Update(){
        inhaleTotalText.text = ImitoneVoiceIntepreter._tNextInhaleDuration.ToString();
    }
}
