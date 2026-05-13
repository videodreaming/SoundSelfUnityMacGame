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

    void OnEnable()
    {
        canvasGroup.alpha = 0; // Start fully opaque
        StartCoroutine(FadeIn());
    }
    IEnumerator FadeIn()
    {
        //yield return new WaitForSeconds(.5f);
        float elapsedTime = 0f;
        float fadeDuration = 2f; // Duration of the fade effect in seconds
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f; // Ensure it's fully transparent at the end
    }
    void OnDisable()
    {
        StopAllCoroutines(); // Stop any ongoing fade-in or fade-out coroutines
    }
    public void FadeOut(Action onComplete)
    {
        StartCoroutine(FadeOutCoroutine(onComplete));
    }
    IEnumerator FadeOutCoroutine(Action onComplete)
    {
        float elapsedTime = 0f;
        float duration = 0.7f;
        yield return new WaitForEndOfFrame(); // Optional delay before starting the fade-out
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        yield return new WaitForSeconds(0.1f);// Ensure it's fully opaque at the end
        onComplete?.Invoke();
    }
}
