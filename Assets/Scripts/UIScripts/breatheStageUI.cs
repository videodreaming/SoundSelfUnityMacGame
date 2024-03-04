using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class breatheStageUI : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    [SerializeField] private TextMeshProUGUI breathStage;
    public void Update(){
        breathStage.text = ImitoneVoiceIntepreter.breathStage.ToString();
    }
}
