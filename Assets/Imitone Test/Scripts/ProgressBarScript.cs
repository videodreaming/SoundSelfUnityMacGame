using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [SerializeField]
    private float totalTime = 2400f;  // 40 minutes in seconds

    private float currentTime = 0f;
    private Image progressBar;

    private void Start()
    {
        progressBar = GetComponent<Image>();
    }

    private void Update()
    {
        if (currentTime < totalTime)
        {
            currentTime += Time.deltaTime;

            // Calculate the fill amount based on the current time
            float fillAmount = currentTime / totalTime;

            // Update the UI Image fill amount
            progressBar.fillAmount = fillAmount;
        }
    }
}
