using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oscillates a UI Image color between white and mid-gray (127, 127, 127).
/// Runs only while this GameObject (and component) is active.
/// </summary>
[RequireComponent(typeof(Image))]
public class UIImageColorPulse : MonoBehaviour
{
    [SerializeField] private float periodSeconds = 1.5f;

    private static readonly Color White = Color.white;
    private static readonly Color Gray = new Color(127f / 255f, 127f / 255f, 127f / 255f, 1f);

    private Image _image;
    private float _alpha = 1f;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _alpha = _image.color.a;
    }

    private void Update()
    {
        float t = (Mathf.Sin((Time.time * Mathf.PI * 2f) / periodSeconds) + 1f) * 0.5f;
        Color c = Color.Lerp(Gray, White, t);
        c.a = _alpha;
        _image.color = c;
    }
}
