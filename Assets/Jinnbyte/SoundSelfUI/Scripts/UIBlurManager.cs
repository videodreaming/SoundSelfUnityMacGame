using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Blurs the background UI Image texture and caches it into _UIBlurGrabTexture.
/// All UI materials using SimpleGrabPassBlur automatically sample this cached texture.
/// Does NOT require a Camera component — works with Screen Space Overlay canvases.
/// </summary>
public class UIBlurManager : MonoBehaviour
{
    public static UIBlurManager Instance { get; private set; }

    [Header("Blur Source")]
    [Tooltip("The full-screen background Image or RawImage to blur.")]
    [SerializeField] Graphic backgroundGraphic;

    [Header("Blur Quality")]
    [Tooltip("Blur radius in texels at downsampled resolution.")]
    [SerializeField, Range(1f, 20f)] float blurSize = 4f;

    [Tooltip("Number of horizontal+vertical pass pairs. More = softer blur.")]
    [SerializeField, Range(1, 4)] int blurIterations = 2;

    [Tooltip("Downsamples the source before blurring. 2 = half-res (best perf/quality).")]
    [SerializeField, Range(1, 4)] int downsampleFactor = 2;

    [Header("Cache Timing")]
    [Tooltip("Seconds between blur rebuilds. 0 = rebuild every frame.")]
    [SerializeField, Min(0f)] float updateInterval = 1f;

    [Header("References")]
    [SerializeField] Shader blurPassShader;

    Material blurMat;
    RenderTexture cachedRT;
    float timer;
    bool needsCapture = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (blurPassShader == null)
            blurPassShader = Shader.Find("Hidden/UIBlurPass");

        blurMat = new Material(blurPassShader) { hideFlags = HideFlags.HideAndDontSave };
    }

    void Start()
    {
        RebuildBlur();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ReleaseRT();
        if (blurMat != null) Destroy(blurMat);
    }

    void Update()
    {
        if (updateInterval <= 0f)
        {
            RebuildBlur();
            return;
        }

        timer += Time.deltaTime;
        if (needsCapture || timer >= updateInterval)
        {
            timer = 0f;
            needsCapture = false;
            RebuildBlur();
        }
    }

    /// <summary>Forces a blur rebuild on the next Update tick.</summary>
    public void ForceRefresh() => needsCapture = true;

    void RebuildBlur()
    {
        if (backgroundGraphic == null) return;

        Texture source = backgroundGraphic.mainTexture;
        if (source == null) return;

        int w = Mathf.Max(1, Screen.width  / downsampleFactor);
        int h = Mathf.Max(1, Screen.height / downsampleFactor);

        EnsureRT(w, h);

        blurMat.SetFloat("_BlurSize", blurSize);

        RenderTexture tmp = RenderTexture.GetTemporary(w, h, 0, cachedRT.format);
        tmp.filterMode = FilterMode.Bilinear;

        // First pass blits from the source texture (handles any resolution difference).
        Graphics.Blit(source, tmp,      blurMat, 0); // horizontal
        Graphics.Blit(tmp,    cachedRT, blurMat, 1); // vertical

        for (int i = 1; i < blurIterations; i++)
        {
            Graphics.Blit(cachedRT, tmp,      blurMat, 0);
            Graphics.Blit(tmp,      cachedRT, blurMat, 1);
        }

        RenderTexture.ReleaseTemporary(tmp);
        Shader.SetGlobalTexture("_UIBlurGrabTexture", cachedRT);
    }

    void EnsureRT(int w, int h)
    {
        if (cachedRT != null && cachedRT.width == w && cachedRT.height == h) return;
        ReleaseRT();
        cachedRT = new RenderTexture(w, h, 0, RenderTextureFormat.Default)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };
        cachedRT.Create();
    }

    void ReleaseRT()
    {
        if (cachedRT == null) return;
        cachedRT.Release();
        Destroy(cachedRT);
        cachedRT = null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) needsCapture = true;
    }
#endif
}
