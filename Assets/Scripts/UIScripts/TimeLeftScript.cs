using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class TimeLeftScript : MonoBehaviour
{
    public TimeTrackerScript timeTracker;
    public Sequencer sequencer;
    public TextMeshProUGUI timeLeftText;
    public float _timeLeft;
    int minutes;
    int seconds ;

    void Update()
    {
        _timeLeft -= Time.deltaTime;
        minutes = Mathf.FloorToInt(_timeLeft / 60);
        seconds = Mathf.FloorToInt(_timeLeft % 60);
        timeLeftText.text = $"{minutes} minutes {seconds} seconds";
    }

    public void SetTimeLeftSeconds(float timeLeft)
    {
        Debug.Log("TimeLeftScript: Setting time left to " + minutes + " minutes " + seconds + " seconds");
        _timeLeft = timeLeft;
    }
}
