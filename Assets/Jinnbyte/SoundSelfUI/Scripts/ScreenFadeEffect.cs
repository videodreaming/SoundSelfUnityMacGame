using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeEffect : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool enableScreenMovement = true;
    [Tooltip("Anchored-position offset (pixels) for enter/exit slide. Tune per screen.")]
    [SerializeField] private float slideOffsetDistance = 20f;

    [Header("Fade (slide rides along on the same duration)")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 centerAnchoredPosition;

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
        CacheCenterPosition();
    }

    private void CacheCenterPosition()
    {
        if (rectTransform != null)
            centerAnchoredPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Slide-in easing: fast start, decelerating to zero velocity at the end
    /// (so motion happens while transparent and settles as it becomes visible).
    /// Quadratic ease-out: 1 - (1 - t)^2.
    /// </summary>
    private static float EaseOutSlideIn(float t)
    {
        float c = Mathf.Clamp01(t);
        float inv = 1f - c;
        return 1f - inv * inv;
    }

    /// <summary>
    /// Slide-out easing: starts slow, accelerating to max velocity at the end
    /// (so motion picks up as the screen fades away).
    /// Quadratic ease-in: t^2.
    /// </summary>
    private static float EaseInSlideOut(float t)
    {
        float c = Mathf.Clamp01(t);
        return c * c;
    }

    private static float Linear01(float elapsed, float duration) =>
        duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

    public bool IsTransitioning { get; private set; }

    void OnEnable()
    {
        CacheCenterPosition();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        bool reverse = reverseNextTransition;
        reverseNextTransition = false;
        Vector2 enterDir = reverse ? Vector2.up : Vector2.down;
        if (enableScreenMovement && rectTransform != null)
            rectTransform.anchoredPosition = centerAnchoredPosition + enterDir * slideOffsetDistance;
        StartCoroutine(FadeIn(enterDir));
    }

    IEnumerator FadeIn(Vector2 enterDir)
    {
        IsTransitioning = true;
        Vector2 startPos = centerAnchoredPosition + enterDir * slideOffsetDistance;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float linearT = Linear01(elapsed, fadeInDuration);
            canvasGroup.alpha = linearT;
            if (enableScreenMovement && rectTransform != null)
            {
                float slideT = EaseOutSlideIn(linearT);
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
        CacheCenterPosition();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : centerAnchoredPosition;
        Vector2 exitDir = reverse ? Vector2.down : Vector2.up;
        Vector2 endPos = centerAnchoredPosition + exitDir * slideOffsetDistance;
        float elapsed = 0f;
        yield return new WaitForEndOfFrame();
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float linearT = Linear01(elapsed, fadeOutDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, linearT);
            if (enableScreenMovement && rectTransform != null)
            {
                float slideT = EaseInSlideOut(linearT);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, slideT);
            }
            yield return null;
        }
        canvasGroup.alpha = 0f;
        if (enableScreenMovement && rectTransform != null)
            rectTransform.anchoredPosition = centerAnchoredPosition;
        IsTransitioning = false;
        onComplete?.Invoke();
    }
}
