using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_MainDisplayVolume : MonoBehaviour
{
    public ImitoneVoiceIntepreter voiceIntepreter;
    public Slider volumeSlider;
    // Start is called before the first frame update
    void Start()
    {
        volumeSlider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        volumeSlider.value = voiceIntepreter.GetNormalizedVolume();
    }
}
