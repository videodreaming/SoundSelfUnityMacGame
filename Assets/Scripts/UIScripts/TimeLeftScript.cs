using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class TimeLeftScript : MonoBehaviour
{
    public static TimeLeftScript instance { get; private set; }
    public Sequencer sequencer;
    public TextMeshProUGUI timeLeftText;
    public float _timeLeft;
    int minutes;
    int seconds;
    public StartButtonScript startButtonScript;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Optional: only if you want it to persist across scenes
        DontDestroyOnLoad(gameObject);
    }
    void Update()
    {
        if(startButtonScript != null)
        {
        if(startButtonScript.startedExperience)
        {
            UpdateTimeLeft();
        }
        }
    }

    private void UpdateTimeLeft()
    {
        _timeLeft -= Time.deltaTime;
        minutes = Mathf.FloorToInt(_timeLeft / 60);
        seconds = Mathf.FloorToInt(_timeLeft % 60);
        if(timeLeftText != null)
        {
            timeLeftText.text = $"{minutes} minutes {seconds} seconds";
        }
    }

    public void SetTimeLeftSeconds(float timeLeft)
    {
        Debug.Log("TimeLeftScript: Setting time left to " + minutes + " minutes " + seconds + " seconds");
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