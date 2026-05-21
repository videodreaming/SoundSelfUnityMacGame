using UnityEngine;

/// <summary>
/// Caps <see cref="Application.targetFrameRate"/> based on whether the system is running on
/// external power or on battery. Designed to live as a sibling component of <c>WwiseBGManager</c>
/// (which already calls <see cref="Object.DontDestroyOnLoad"/>) so this component inherits the
/// same single-global-instance lifecycle across scene loads — no extra plumbing required.
///
/// <para>Battery semantics: only <see cref="BatteryStatus.Discharging"/> counts as "on battery".
/// <see cref="BatteryStatus.Charging"/>, <see cref="BatteryStatus.Full"/>,
/// <see cref="BatteryStatus.NotCharging"/>, and <see cref="BatteryStatus.Unknown"/> all map to the
/// plugged-in target. This matches <c>UIManager.RefreshBatteryUiFromSystem</c> so the battery
/// icon UI and the FPS cap are always in agreement — single source of truth, single interpretation.
/// In the Unity Editor (where <see cref="SystemInfo.batteryStatus"/> returns Unknown) and on
/// desktops with no battery, the plugged-in target is always used.</para>
///
/// <para>Replaces the previous hardcoded <c>QualitySettings.vSyncCount = 0;
/// Application.targetFrameRate = 30;</c> in <c>WwiseBGManager.Awake</c>. That pairing is intentionally
/// preserved here: vSync must be off for <see cref="Application.targetFrameRate"/> to actually clamp.</para>
/// </summary>
public class PowerAwareFrameRate : MonoBehaviour
{
    [Tooltip("Target frame rate when on external power (or when battery status is Unknown).")]
    [SerializeField] [Range(15, 120)] private int targetFrameRatePlugged = 30;

    [Tooltip("Target frame rate when running on battery (SystemInfo.batteryStatus == Discharging).")]
    [SerializeField] [Range(15, 120)] private int targetFrameRateBattery = 20;

    [Tooltip("How often to re-check SystemInfo.batteryStatus (seconds). Cheap call, slow cadence is fine.")]
    [SerializeField] [Range(1f, 30f)] private float pollIntervalSeconds = 5f;

    [Tooltip("If true, also forces QualitySettings.vSyncCount = 0 in Awake so the targetFrameRate cap actually applies. " +
             "Quality presets can re-enable vSync — leave this on unless you're handling vSync elsewhere.")]
    [SerializeField] private bool disableVSync = true;

    private bool _initialized;
    private bool _lastAppliedOnBattery;

    private void Awake()
    {
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }
        ApplyForCurrentBatteryStatus();
    }

    private void OnEnable() => InvokeRepeating(nameof(ApplyForCurrentBatteryStatus), pollIntervalSeconds, pollIntervalSeconds);
    private void OnDisable() => CancelInvoke(nameof(ApplyForCurrentBatteryStatus));

    private void ApplyForCurrentBatteryStatus()
    {
        // Same semantics as UIManager.RefreshBatteryUiFromSystem: only Discharging counts as "on battery."
        bool onBattery = SystemInfo.batteryStatus == BatteryStatus.Discharging;
        int target = onBattery ? targetFrameRateBattery : targetFrameRatePlugged;

        if (!_initialized || onBattery != _lastAppliedOnBattery || Application.targetFrameRate != target)
        {
            Application.targetFrameRate = target;
            _lastAppliedOnBattery = onBattery;
            _initialized = true;
        }
    }
}
