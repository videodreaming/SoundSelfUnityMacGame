using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class AudioStateUI : MonoBehaviour
{
    public AudioManager AudioManager;
    [SerializeField] private TextMeshProUGUI audioStateText;
    public void Update(){
        audioStateText.text = AudioManager.currentState.ToString();
    }
}