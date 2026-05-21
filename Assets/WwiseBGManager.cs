using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WwiseBGManager : MonoBehaviour
{
  void Awake()
    {
        DontDestroyOnLoad(gameObject);
        // Framerate / vSync now owned by PowerAwareFrameRate (sibling component on this GameObject).
        // That component reads SystemInfo.batteryStatus on a slow poll and applies
        // Application.targetFrameRate accordingly (plugged vs Discharging) so the cap stays in
        // sync with the battery UI in UIManager.RefreshBatteryUiFromSystem.
    }

    void Update()
    {
        AkSoundEngine.RenderAudio();
    }
}
