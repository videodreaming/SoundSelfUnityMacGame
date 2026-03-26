using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTrackerScript : MonoBehaviour
{
    public static TimeTrackerScript instance { get; private set; }

    [Header("Elapsed Time")]
    public float TotalElapsedTime;
    public string DisplayTime;

    [Header("Time Left (source of truth)")]
    [SerializeField] private float _timeLeftSeconds;

    [SerializeField] private CalibrationMenu calibrationMenu;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

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

        if (calibrationMenu != null && calibrationMenu.startedExperience)
        {
            TickTimeLeft();
        }
    }

    void UpdateDisplayTime()
    {
        int minutes = Mathf.FloorToInt(TotalElapsedTime / 60);
        int seconds = Mathf.FloorToInt(TotalElapsedTime % 60);
        DisplayTime = $"{minutes} minutes {seconds} seconds";
    }

    private void TickTimeLeft()
    {
        if (_timeLeftSeconds <= 0f) return;
        _timeLeftSeconds -= Time.deltaTime;
        if (_timeLeftSeconds < 0f) _timeLeftSeconds = 0f;
    }

    public void SetTimeLeftSeconds(float seconds)
    {
        _timeLeftSeconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(_timeLeftSeconds / 60);
        int secs = Mathf.FloorToInt(_timeLeftSeconds % 60);
        Debug.Log("TimeTrackerScript: Setting time left to " + minutes + " minutes " + secs + " seconds");
    }

    public float GetTimeLeftSeconds() => _timeLeftSeconds;

    public string GetTimeLeftFormattedToMinutesAndSeconds()
    {
        int minutes = Mathf.FloorToInt(_timeLeftSeconds / 60);
        int seconds = Mathf.FloorToInt(_timeLeftSeconds % 60);
        return $"{minutes} minutes {seconds} seconds";
    }
}
