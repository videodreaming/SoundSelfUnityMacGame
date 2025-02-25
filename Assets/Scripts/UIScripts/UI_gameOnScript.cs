using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_gameOnScript : MonoBehaviour
{
    public ImitoneVoiceIntepreter  imitoneVoiceInterpreter; 
    public TextMeshProUGUI gameOnText;

    // Update is called once per frame
    void Update()
    {
        gameOnText.text = imitoneVoiceInterpreter.gameOn.ToString();
    }
}
