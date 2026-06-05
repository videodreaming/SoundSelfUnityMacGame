using UnityEngine;
using System.Collections;

public class PingPongScale : MonoBehaviour
{
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float duration = 1f;

    private void OnEnable()
    {
        StartCoroutine(ScalePingPong());
    }
    void OnDisable()
    {
        StopAllCoroutines();
    }
    private IEnumerator ScalePingPong()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.time / duration, 1f);
            float scale = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
    }
}