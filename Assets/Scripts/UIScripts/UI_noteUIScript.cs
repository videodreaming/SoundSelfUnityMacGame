using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ConversionUtilities;

public class noteUIScript : MonoBehaviour
{
    public ImitoneVoiceIntepreter mainImitone;
    [SerializeField] private TextMeshProUGUI noteText;
    public void Update(){
        NoteName currentNote = NoteUtils.FloatToNoteName(mainImitone.note_st);
        if (currentNote == NoteName.None)
        {
            noteText.text = "None";
        }
        else
        {
            noteText.text = currentNote.ToString();
        }
    }
}
