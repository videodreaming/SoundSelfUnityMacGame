using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class noteUIScript : MonoBehaviour
{
    public ImitoneVoiceIntepreter mainImitone;
    [SerializeField] private TextMeshProUGUI noteText;
    public void Update(){
        noteText.text = mainImitone.note_st.ToString();
    }
}
