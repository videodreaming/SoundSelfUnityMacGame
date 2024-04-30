using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class PlayerOutput : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    private TMP_Text tmpText; // TMP component
    // Start is called before the first frame update
    void Start()
    {
        tmpText = GetComponent<TMP_Text>(); // Get the TMP component
    }

    // Update is called once per frame
    void Update()
    {
        ReadOutput();
    }

    void ReadOutput()
    {
        tmpText.text = "pitch: " + imitoneVoiceIntepreter.pitch_hz  + "         Volume: " + imitoneVoiceIntepreter._dbValue;
    }
}
