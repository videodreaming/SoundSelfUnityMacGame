using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private float pressedScale = 0.92f;
    private float pressDuration = 0.12f;
    private float releaseDuration = 0.22f;

    private RectTransform _rect;
    private Coroutine _activeCoroutine;

    void Awake() => _rect = GetComponent<RectTransform>();

    public void OnPointerDown(PointerEventData e) => Animate(pressedScale, pressDuration, EaseOutQuad);
    public void OnPointerUp(PointerEventData e) => Animate(1f, releaseDuration, EaseOutBack);
    public void OnPointerExit(PointerEventData e) => Animate(1f, releaseDuration, EaseOutBack);

    void Animate(float targetScale, float duration, System.Func<float, float> easeFn)
    {
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(ScaleCoroutine(targetScale, duration, easeFn));
    }

    IEnumerator ScaleCoroutine(float target, float duration, System.Func<float, float> easeFn)
    {
        float start = _rect.localScale.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = easeFn(Mathf.Clamp01(elapsed / duration));
            float scale = Mathf.LerpUnclamped(start, target, t);
            _rect.localScale = Vector3.one * scale;
            yield return null;
        }

        _rect.localScale = Vector3.one * target;
    }

    // Ease functions
    float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}