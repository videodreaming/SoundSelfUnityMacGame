using System;
using System.Collections;
using UnityEngine;

public enum HorizontalSlideDirection
{
    Left,
    Right
}

/// <summary>
/// Fade + horizontal slide effect for UI elements (Unity UI legacy Text, Image, etc.).
/// Drop on any RectTransform; a CanvasGroup is auto-required.
///
/// <para><b>Fade-in:</b> triggered automatically by <see cref="OnEnable"/> — element starts at
/// <c>fadeInFromDirection</c> offset and slides to its inspector-layout position over <c>fadeInDuration</c>.</para>
///
/// <para><b>Fade-out:</b> caller invokes <see cref="FadeOut(Action)"/>; element slides toward
/// <c>fadeOutToDirection</c> over <c>fadeOutDuration</c>, then <paramref name="onComplete"/> fires.</para>
///
/// Mirrors the API of <see cref="ScreenFadeEffect"/> so <see cref="UIManager"/> can swap session-header
/// fade-outs in/out without touching the rest of the flow.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HorizontalSlideFadeEffect : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.7f;

    [Header("Movement")]
    [SerializeField] private bool enableEntery = true;
        [SerializeField] private bool enableExit = true;


    [SerializeField] private bool enableMovement = true;
    [SerializeField] private float slideDistance = 200f;
    [Tooltip("Side the element enters FROM on fade-in (animates to its layout position).")]
    [SerializeField] private HorizontalSlideDirection fadeInFromDirection = HorizontalSlideDirection.Right;
    [Tooltip("Side the element exits TO on fade-out.")]
    [SerializeField] private HorizontalSlideDirection fadeOutToDirection = HorizontalSlideDirection.Left;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 centerAnchoredPosition;

    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            centerAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (canvasGroup == null || !enableEntery) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        if (enableMovement && rectTransform != null)
            rectTransform.anchoredPosition = centerAnchoredPosition + GetOffset(fadeInFromDirection);
        StartCoroutine(FadeInCoroutine());
    }

    private void OnDisable()
    {
        IsTransitioning = false;
        StopAllCoroutines();
    }

    private IEnumerator FadeInCoroutine()
    {
        IsTransitioning = true;
        Vector2 startPos = centerAnchoredPosition + GetOffset(fadeInFromDirection);
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeInDuration > 0f ? Mathf.Clamp01(elapsed / fadeInDuration) : 1f;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            if (enableMovement && rectTransform != null)
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, centerAnchoredPosition, t);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        if (enableMovement && rectTransform != null)
            rectTransform.anchoredPosition = centerAnchoredPosition;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        IsTransitioning = false;
    }

    /// <summary>
    /// Fades out and slides toward <c>fadeOutToDirection</c>, invoking <paramref name="onComplete"/> when done.
    /// Cancels any in-flight transition so <paramref name="onComplete"/> always runs.
    /// </summary>
    public void FadeOut(Action onComplete)
    {
        StopAllCoroutines();
        IsTransitioning = false;
        if(enableExit)
        StartCoroutine(FadeOutCoroutine(onComplete));
        else
        onComplete?.Invoke();
    }

    private IEnumerator FadeOutCoroutine(Action onComplete)
    {
        IsTransitioning = true;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : centerAnchoredPosition;
        Vector2 endPos = centerAnchoredPosition + GetOffset(fadeOutToDirection);
        float elapsed = 0f;
        yield return new WaitForEndOfFrame();
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeOutDuration > 0f ? Mathf.Clamp01(elapsed / fadeOutDuration) : 1f;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            if (enableMovement && rectTransform != null)
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        IsTransitioning = false;
        onComplete?.Invoke();
    }

    private Vector2 GetOffset(HorizontalSlideDirection dir)
    {
        return dir == HorizontalSlideDirection.Right
            ? new Vector2(slideDistance, 0f)
            : new Vector2(-slideDistance, 0f);
    }
}
