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
    int seconds;
    public StartButtonScript startButtonScript;

    void Update()
    {
        if(startButtonScript.startedExperience)
        {
            UpdateTimeLeft();
        }

    }

    private void UpdateTimeLeft()
    {
        _timeLeft -= Time.deltaTime;
        minutes = Mathf.FloorToInt(_timeLeft / 60);
        seconds = Mathf.FloorToInt(_timeLeft % 60);
        timeLeftText.text = $"{minutes} minutes {seconds} seconds";
    }

    public void SetTimeLeftSeconds(float timeLeft)
    {
        Debug.Log("TimeLeft: SetTimeLeftSeconds() called. Previous time: " + _timeLeft + ". Setting _timeLeft to " + timeLeft);
        _timeLeft = timeLeft;
    }

    public float GetTimeLeft()
    {
        return _timeLeft;
    }

    public string GetTimeLeftFormattedToMinutesAndSeconds()
    {
        int minutes = Mathf.FloorToInt(_timeLeft / 60);
        int seconds = Mathf.FloorToInt(_timeLeft % 60);
        return $"{minutes} minutes {seconds} seconds";
    }
}