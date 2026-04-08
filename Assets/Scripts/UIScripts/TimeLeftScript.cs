using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class TimeLeftScript : MonoBehaviour
{
    public TextMeshProUGUI timeLeftText;
    public TimeTrackerScript timeTracker;
    void Update()
    {
        if (timeTracker == null)
            timeTracker = TimeTrackerScript.instance;
        if (timeTracker == null || timeLeftText == null) return;

        timeLeftText.text = timeTracker.FormatCountdownFullMinutesAndSeconds();
    }
}