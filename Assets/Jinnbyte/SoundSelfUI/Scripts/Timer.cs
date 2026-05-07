using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [SerializeField] private Image meditationProgressBar;
    [SerializeField] private RectTransform mediationBarHandler;
    private Text textComponent;
    private float duration;
    private bool isSessionActive = false;
    private float elapsedTime = 0f;

    private void Awake()
    {
        textComponent = GetComponent<Text>();
    }
    void SetTimer(float time)
    {
        duration = time * 60;
        elapsedTime = 0f;
        isSessionActive = true;

    }

    private void Update()
    {
        if (!isSessionActive) return;

        elapsedTime += Time.deltaTime;
        meditationProgressBar.fillAmount = elapsedTime / duration;
        mediationBarHandler.rotation = Quaternion.Euler(0f, 0f, -360f * (elapsedTime / duration));
        float remainingTime = Mathf.Max(0f, duration - elapsedTime);
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        textComponent.text = $"{minutes:00}:{seconds:00}";

        if (elapsedTime >= duration)
        {
            isSessionActive = false;
            textComponent.text = "00:00";
            UIManager.Instance.OnSessionTimeEnds?.Invoke();
        }
    }
    public void SetProgressBar(float progress)
    {
        meditationProgressBar.fillAmount = progress;
        mediationBarHandler.rotation = Quaternion.Euler(0f, 0f, -360f * progress);
    }
    public void SetTimeText(string time)
    {
        textComponent.text = time;
    }
}
