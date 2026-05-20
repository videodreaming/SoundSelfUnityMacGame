using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeEffect : MonoBehaviour
{
    [SerializeField] private bool enableScreenMovement = true;
    private float slideInDuration = 0.7f;
    private float slideOutDuration = 0.7f;

    private float fadeInDuration = 1f;
    private float fadeOutDuration = 0.7f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 centerAnchoredPosition;
    private float slideDistance;

    // When true, the next transition (FadeIn from OnEnable, or FadeOut without explicit param)
    // slides in the opposite direction: fade-in starts above and moves down to center,
    // fade-out slides down off-screen. Set by callers (e.g. UIManager on Back press) before
    // SetActive(true) / FadeOut; consumed once per transition.
    [HideInInspector] public bool reverseNextTransition;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        centerAnchoredPosition = rectTransform.anchoredPosition;
        slideDistance = rectTransform.rect.height > 0 ? rectTransform.rect.height : Screen.height;
        slideDistance /= 5; // Add some extra distance to ensure it fully slides off-screen
    }

    public bool IsTransitioning { get; private set; }

    void OnEnable()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        bool reverse = reverseNextTransition;
        reverseNextTransition = false;
        Vector2 enterDir = reverse ? Vector2.up : Vector2.down;
        if (enableScreenMovement && rectTransform != null)
            rectTransform.anchoredPosition = centerAnchoredPosition + enterDir * slideDistance;
        StartCoroutine(FadeIn(enterDir));
    }
    IEnumerator FadeIn(Vector2 enterDir)
    {
        IsTransitioning = true;
        float elapsedTime = 0f;
        float fadeDuration = fadeInDuration;
        Vector2 startPos = centerAnchoredPosition + enterDir * slideDistance;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            if (enableScreenMovement && rectTransform != null)
            {
                float slideT = slideInDuration > 0f ? Mathf.Clamp01(elapsedTime / slideInDuration) : 1f;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, centerAnchoredPosition, slideT);
            }
            yield return null;
        }
        canvasGroup.alpha = 1f;
        if (enableScreenMovement && rectTransform != null)
            rectTransform.anchoredPosition = centerAnchoredPosition;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        IsTransitioning = false;
    }
    void OnDisable()
    {
        IsTransitioning = false;
        StopAllCoroutines();
    }
    /// <summary>
    /// Fades out and invokes <paramref name="onComplete"/> when done.
    /// If a fade-in (from <see cref="OnEnable"/>) or another transition is still running, it is cancelled first so
    /// <paramref name="onComplete"/> always runs — callers such as <see cref="UIManager.UnsetAllScreens"/> rely on that.
    /// </summary>
    public void FadeOut(Action onComplete) => FadeOut(onComplete, reverse: false);
    public void FadeOut(Action onComplete, bool reverse)
    {
        StopAllCoroutines();
        IsTransitioning = false;
        StartCoroutine(FadeOutCoroutine(onComplete, reverse));
    }
    IEnumerator FadeOutCoroutine(Action onComplete, bool reverse)
    {
        IsTransitioning = true;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : centerAnchoredPosition;
        Vector2 exitDir = reverse ? Vector2.down : Vector2.up;
        Vector2 endPos = centerAnchoredPosition + exitDir * slideDistance;
        float elapsedTime = 0f;
        float duration = fadeOutDuration;
        yield return new WaitForEndOfFrame();
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            if (enableScreenMovement && rectTransform != null)
            {
                float slideT = slideOutDuration > 0f ? Mathf.Clamp01(elapsedTime / slideOutDuration) : 1f;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, slideT);
            }
            yield return null;
        }
        canvasGroup.alpha = 0f;
        if (enableScreenMovement && rectTransform != null)
            rectTransform.anchoredPosition = endPos;
        yield return new WaitForSeconds(0.1f);
        if (enableScreenMovement && rectTransform != null)
            rectTransform.anchoredPosition = centerAnchoredPosition;
        IsTransitioning = false;
        onComplete?.Invoke();
    }
}
