using System;
using System.Collections;
using UnityEngine;

public class ScreenFadeEffect : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private void Awake()
    {

        canvasGroup = GetComponent<CanvasGroup>();

    }

    public bool IsTransitioning { get; private set; }

    void OnEnable()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        StartCoroutine(FadeIn());
    }
    IEnumerator FadeIn()
    {
        IsTransitioning = true;
        float elapsedTime = 0f;
        float fadeDuration = 1f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        IsTransitioning = false;
    }
    void OnDisable()
    {
        IsTransitioning = false;
        StopAllCoroutines();
    }
    public void FadeOut(Action onComplete)
    {
        if (IsTransitioning) return;
        StartCoroutine(FadeOutCoroutine(onComplete));
    }
    IEnumerator FadeOutCoroutine(Action onComplete)
    {
        IsTransitioning = true;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        float elapsedTime = 0f;
        float duration = 0.7f;
        yield return new WaitForEndOfFrame();
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        yield return new WaitForSeconds(0.1f);
        IsTransitioning = false;
        onComplete?.Invoke();
    }
}
