using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTrackerScript : MonoBehaviour
{
    public float TotalElapsedTime;
    public string DisplayTime;

    // Start is called before the first frame update
    void Start()
    {
        TotalElapsedTime = 0f;
        DisplayTime = "0 minutes 0 seconds";
    }

    // Update is called once per frame
    void Update()
    {
        TotalElapsedTime += Time.deltaTime;
        UpdateDisplayTime();
    }

    void UpdateDisplayTime()
    {
        int minutes = Mathf.FloorToInt(TotalElapsedTime / 60);
        int seconds = Mathf.FloorToInt(TotalElapsedTime % 60);
        DisplayTime = $"{minutes} minutes {seconds} seconds";
    }
}