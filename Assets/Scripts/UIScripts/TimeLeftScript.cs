using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class TimeLeftScript : MonoBehaviour
{
    public TimeTrackerScript timeTracker;
    public Sequencer sequencer;
    public TextMeshProUGUI timeLeftText;

    void Update()
    {
        float timeLeft = timeTracker.TotalElapsedTime - sequencer.totalTimeOfExperience;
        int minutes = Mathf.FloorToInt(timeLeft / 60);
        int seconds = Mathf.FloorToInt(timeLeft % 60);
        timeLeftText.text = $"{minutes} minutes {seconds} seconds";
    }
}
