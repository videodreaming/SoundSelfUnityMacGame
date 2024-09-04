using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class breatheStageUI : MonoBehaviour
{
    //WE DO NOT USE BREATHSTAGE ANY MORE.
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    [SerializeField] private TextMeshProUGUI breathStage;
    public void Update(){
        breathStage.text = "breath stage not used any more - remove this debug element";
    }
}
