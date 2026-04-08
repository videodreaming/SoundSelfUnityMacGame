using UnityEngine;
using UnityEngine.UI;


public class ProgressBar : MonoBehaviour
{
    private float totalTime;  // 40 minutes in seconds

    private float currentTime = 0f;
    public Slider progressBar;

    private void Start()
    {
        totalTime = TimeTrackerScript.instance != null ? TimeTrackerScript.instance.CountdownFull : 0f; // full-session countdown at Start (seconds remaining)
        if (progressBar == null)
        {
            progressBar = GetComponent<Slider>();
        }
    }

    private void Update()
    {
        if (currentTime < totalTime)
        {
            currentTime += Time.deltaTime;

            // Calculate the fill amount based on the current time
            float fillAmount = currentTime / totalTime;

            // Update the UI Image fill amount
            progressBar.value = fillAmount;
        }
    }
}
