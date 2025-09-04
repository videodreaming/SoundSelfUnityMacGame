using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerUIScript : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public TimeLeftScript timeLeftScript;

    // Start is called before the first frame update
    void Start()
    {
        timerText = GetComponent<TextMeshProUGUI>();
        timerText.fontSize = 24;
    }
    

    // Update is called once per frame
    void Update()
    {
        timerText.text = "Time Left: " + timeLeftScript.GetTimeLeftFormattedToMinutesAndSeconds();
    }
}
