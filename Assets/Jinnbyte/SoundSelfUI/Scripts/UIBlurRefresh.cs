using UnityEngine;

/// <summary>
/// Add to any panel that uses the blur material. Forces the blur cache to rebuild
/// when the panel becomes visible so it reflects the current scene content.
/// </summary>
public class UIBlurRefresh : MonoBehaviour
{
    void OnEnable()
    {
        if (UIBlurManager.Instance != null)
            UIBlurManager.Instance.ForceRefresh();
    }
}
