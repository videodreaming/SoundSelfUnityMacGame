using System.Collections;
using UnityEngine;

public class ScreenFadeEffect : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1.5f; // Duration of the fade effect in seconds
    private void Awake()
    {

        canvasGroup = GetComponent<CanvasGroup>();

    }

    void OnEnable()
    {
        canvasGroup.alpha = 0; // Start fully opaque
        StartCoroutine(FadeOut());
    }
    IEnumerator FadeOut()
    {
        //yield return new WaitForSeconds(.5f);
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f; // Ensure it's fully transparent at the end
    }
}
