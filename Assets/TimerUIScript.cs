using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerUIScript : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    // Start is called before the first frame update
    void Start()
    {
        if (timerText == null)
            timerText = GetComponent<TextMeshProUGUI>();
        if (timerText != null)
            timerText.fontSize = 24;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (timerText == null) return;
        timerText.text = TimeTrackerScript.instance != null
            ? "Time Left: " + TimeTrackerScript.instance.FormatCountdownFullMinutesAndSeconds()
            : "Time Left: --:--";
    }
}
