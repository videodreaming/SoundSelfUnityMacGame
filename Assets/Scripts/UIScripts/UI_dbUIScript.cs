using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class dbUIScript : MonoBehaviour
{
    public ImitoneVoiceIntepreter mainImitone;
    [SerializeField] private TextMeshProUGUI noteText;
    public void Update(){
        noteText.text = mainImitone._dbValue.ToString() + " ~ " + mainImitone._dbMicrophone.ToString();
    }
}
