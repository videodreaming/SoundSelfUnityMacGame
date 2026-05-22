using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Watches a small fixed set of audio devices for mid-session unplugs and routes the first loss of
/// each device into <see cref="UIManager.NotifyDeviceDisconnected"/>. Designed to be dropped onto a
/// single GameObject in the scene — usually wherever <see cref="LightControl"/> lives, since this
/// monitor reuses LightControl's Kasina / Limina deviceName constants.
///
/// <para><b>Monitored set</b> (each opt-in via inspector toggles):</para>
/// <list type="bullet">
///   <item>Kasina — Wwise "System" shareset deviceName <see cref="LightControl.KasinaWwiseDeviceName"/>.</item>
///   <item>Limina (MPL) — Wwise "System" shareset deviceName <see cref="LightControl.LiminaWwiseDeviceName"/>.</item>
///   <item>Default audio output — whichever Wwise System device reports <c>isDefaultDevice == true</c> at baseline capture.</item>
///   <item>Default audio input — <see cref="ImitoneVoiceIntepreter.MicrophoneDeviceName"/> when assigned, else <c>Microphone.devices[0]</c>.</item>
/// </list>
///
/// <para><b>Lifecycle</b>: After <see cref="baselineDelaySeconds"/>, the monitor snapshots which of
/// the above devices are currently present + Active. Only devices that pass the baseline check are
/// monitored — a Kasina that was never plugged in won't ever raise a warning. Once baselined, the
/// monitor polls every <see cref="pollIntervalSeconds"/> and, when a monitored device disappears
/// from the live device list, notifies <see cref="UIManager"/> once and stops monitoring that
/// specific name (so we don't re-notify every poll). Reconnections are intentionally ignored —
/// the on-screen warning is one-way for the session, matching the product spec.</para>
///
/// <para><b>Friendly names</b>: Kasina / Limina map to short labels for the on-screen list; any
/// other device shows its raw name (trimmed). Override via <see cref="friendlyNameOverrides"/> if
/// you want different display strings.</para>
/// </summary>
[DisallowMultipleComponent]
public class DeviceDisconnectMonitor : MonoBehaviour
{
    public static DeviceDisconnectMonitor Instance { get; private set; }

    [Header("Dependencies (optional — auto-resolved at runtime)")]
    [Tooltip("Used only for clarity / explicit wiring; the monitor does not read mutable state from LightControl. Device name strings come from LightControl.KasinaWwiseDeviceName / LiminaWwiseDeviceName (compile-time constants), so this reference is informational.")]
    [SerializeField] private LightControl lightControl;
    [Tooltip("Source of the active microphone deviceName when monitoring default audio input. Falls back to Microphone.devices[0] if null or not yet started.")]
    [SerializeField] private ImitoneVoiceIntepreter imitoneVoiceInterpreter;

    [Header("Monitoring categories")]
    [Tooltip("Watch Kasina (LightControl.KasinaWwiseDeviceName) — only flagged if present at baseline.")]
    [SerializeField] private bool monitorKasina = true;
    [Tooltip("Watch Limina / MPL (LightControl.LiminaWwiseDeviceName) — only flagged if present at baseline.")]
    [SerializeField] private bool monitorLimina = true;
    [Tooltip("Watch the OS default audio output as seen by Wwise at baseline time (Wwise AkDeviceDescription.isDefaultDevice on the System shareset).")]
    [SerializeField] private bool monitorDefaultOutput = true;
    [Tooltip("Watch the active microphone — preferring ImitoneVoiceIntepreter.MicrophoneDeviceName, falling back to Microphone.devices[0].")]
    [SerializeField] private bool monitorDefaultInput = true;

    [Header("Timing")]
    [Tooltip("Delay between Start() and baseline capture. Needs to be long enough for Wwise + Microphone subsystems to enumerate devices reliably. 3 s is conservative on Win/Mac.")]
    [SerializeField] private float baselineDelaySeconds = 3f;
    [Tooltip("Interval between device-list polls once baseline is captured. 1.5 s gives a near-instant on-screen warning without spamming Wwise device enumeration.")]
    [SerializeField] private float pollIntervalSeconds = 1.5f;

    [Header("Display overrides (raw deviceName → friendly label shown in the warning UI)")]
    [Tooltip("Optional per-entry overrides. Defaults already map Kasina / Limina deviceName strings to 'Kasina' / 'Limina'. Add overrides here to relabel any other device.")]
    [SerializeField] private List<FriendlyNameOverride> friendlyNameOverrides = new List<FriendlyNameOverride>();

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool debugLogPolls = false;

    [Serializable]
    public struct FriendlyNameOverride
    {
        public string rawDeviceName;
        public string displayName;
    }

    /// <summary>
    /// One entry per device we baselined as present. <see cref="rawDeviceName"/> is the exact string
    /// returned by Wwise / Unity; <see cref="displayName"/> is what we pass to UIManager.
    /// </summary>
    private struct MonitoredDevice
    {
        public string rawDeviceName;
        public string displayName;
        public DeviceKind kind;
    }

    private enum DeviceKind
    {
        WwiseOutput, // enumerated via AkSoundEngine.GetDeviceList on the System shareset
        MicrophoneInput, // enumerated via UnityEngine.Microphone.devices
    }

    private readonly List<MonitoredDevice> _monitored = new List<MonitoredDevice>();
    private readonly HashSet<string> _alreadyNotified = new HashSet<string>(StringComparer.Ordinal);
    private bool _baselineCaptured;
    private float _nextPollTime;
    private uint _cachedSystemSharesetId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("DeviceDisconnectMonitor: duplicate instance — destroying this one.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (lightControl == null)
            lightControl = LightControl.instance != null ? LightControl.instance : FindObjectOfType<LightControl>();
        if (imitoneVoiceInterpreter == null)
            imitoneVoiceInterpreter = FindObjectOfType<ImitoneVoiceIntepreter>();

        _cachedSystemSharesetId = AkSoundEngine.GetIDFromString("System");
        StartCoroutine(CaptureBaselineAfterDelay());
    }

    private IEnumerator CaptureBaselineAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, baselineDelaySeconds));
        CaptureBaseline();
        _baselineCaptured = true;
        _nextPollTime = Time.unscaledTime + Mathf.Max(0.1f, pollIntervalSeconds);
    }

    private void Update()
    {
        if (!_baselineCaptured)
            return;
        if (Time.unscaledTime < _nextPollTime)
            return;
        _nextPollTime = Time.unscaledTime + Mathf.Max(0.1f, pollIntervalSeconds);
        Poll();
    }

    // =====================================
    // BASELINE
    // =====================================

    private void CaptureBaseline()
    {
        // Snapshot Wwise system devices once (single enumeration) — cheaper than calling per category
        // and gives us a consistent view (active set + default flag) at a single point in time.
        var wwiseDevices = EnumerateActiveWwiseSystemDevices();

        if (monitorKasina && !string.IsNullOrEmpty(LightControl.KasinaWwiseDeviceName))
            TryAddWwiseBaseline(LightControl.KasinaWwiseDeviceName, "Kasina", wwiseDevices);
        if (monitorLimina && !string.IsNullOrEmpty(LightControl.LiminaWwiseDeviceName))
            TryAddWwiseBaseline(LightControl.LiminaWwiseDeviceName, "Limina", wwiseDevices);

        if (monitorDefaultOutput)
        {
            string defaultOutputName = FindDefaultWwiseOutputName(wwiseDevices);
            if (!string.IsNullOrEmpty(defaultOutputName))
            {
                if (IsAlreadyMonitored(defaultOutputName))
                {
                    if (debugLogs)
                        Debug.Log($"DeviceDisconnectMonitor: default Wwise output '{defaultOutputName}' is also one of the explicit auxiliary devices — already monitored, skipping.");
                }
                else
                {
                    AddMonitored(defaultOutputName, FriendlyNameFor(defaultOutputName, fallbackKind: "Default output"), DeviceKind.WwiseOutput);
                }
            }
            else if (debugLogs)
            {
                Debug.LogWarning("DeviceDisconnectMonitor: could not identify a default Wwise output device (no entry with isDefaultDevice=true). Skipping default-output monitoring.");
            }
        }

        if (monitorDefaultInput)
        {
            string micName = ResolveActiveMicrophoneName();
            if (!string.IsNullOrEmpty(micName))
            {
                AddMonitored(micName, FriendlyNameFor(micName, fallbackKind: "Default input"), DeviceKind.MicrophoneInput);
            }
            else if (debugLogs)
            {
                Debug.LogWarning("DeviceDisconnectMonitor: no active microphone deviceName at baseline (ImitoneVoiceIntepreter not yet running and Microphone.devices is empty). Skipping default-input monitoring.");
            }
        }

        if (debugLogs)
        {
            if (_monitored.Count == 0)
                Debug.Log("DeviceDisconnectMonitor: baseline complete — 0 devices monitored (nothing matching was present at startup).");
            else
            {
                var names = new List<string>(_monitored.Count);
                foreach (var m in _monitored) names.Add($"'{m.rawDeviceName}' [{m.kind}] → '{m.displayName}'");
                Debug.Log($"DeviceDisconnectMonitor: baseline complete — monitoring {_monitored.Count} device(s): {string.Join(", ", names)}.");
            }
        }
    }

    private void TryAddWwiseBaseline(string rawName, string defaultFriendly, List<AkDeviceDescription> wwiseDevices)
    {
        bool foundActive = false;
        for (int i = 0; i < wwiseDevices.Count; i++)
        {
            if (string.Equals(wwiseDevices[i].deviceName, rawName, StringComparison.Ordinal))
            {
                foundActive = true;
                break;
            }
        }

        if (!foundActive)
        {
            if (debugLogs)
                Debug.Log($"DeviceDisconnectMonitor: '{rawName}' not present at baseline — skipping (won't warn unless it appears and then disappears in a later session).");
            return;
        }

        string friendly = FriendlyNameFor(rawName, fallbackKind: defaultFriendly);
        AddMonitored(rawName, friendly, DeviceKind.WwiseOutput);
    }

    private bool IsAlreadyMonitored(string rawName)
    {
        for (int i = 0; i < _monitored.Count; i++)
            if (string.Equals(_monitored[i].rawDeviceName, rawName, StringComparison.Ordinal))
                return true;
        return false;
    }

    private void AddMonitored(string rawName, string displayName, DeviceKind kind)
    {
        _monitored.Add(new MonitoredDevice { rawDeviceName = rawName, displayName = displayName, kind = kind });
    }

    // =====================================
    // POLLING
    // =====================================

    private void Poll()
    {
        if (_monitored.Count == _alreadyNotified.Count)
            return; // every monitored device has already been flagged

        // Cache live device lists once per poll for cheap per-device checks below.
        List<AkDeviceDescription> wwiseDevices = null;
        string[] micDevices = null;

        for (int i = 0; i < _monitored.Count; i++)
        {
            var m = _monitored[i];
            if (_alreadyNotified.Contains(m.rawDeviceName))
                continue;

            bool stillPresent;
            switch (m.kind)
            {
                case DeviceKind.WwiseOutput:
                    if (wwiseDevices == null)
                        wwiseDevices = EnumerateActiveWwiseSystemDevices();
                    stillPresent = WwiseDeviceListContains(wwiseDevices, m.rawDeviceName);
                    break;
                case DeviceKind.MicrophoneInput:
                    if (micDevices == null)
                        micDevices = Microphone.devices ?? Array.Empty<string>();
                    stillPresent = MicrophoneDeviceListContains(micDevices, m.rawDeviceName);
                    break;
                default:
                    stillPresent = true;
                    break;
            }

            if (debugLogPolls)
                Debug.Log($"DeviceDisconnectMonitor.Poll: '{m.rawDeviceName}' [{m.kind}] stillPresent={stillPresent}.");

            if (!stillPresent)
            {
                ReportDisconnect(m);
            }
        }
    }

    private void ReportDisconnect(MonitoredDevice m)
    {
        if (!_alreadyNotified.Add(m.rawDeviceName))
            return;

        Debug.LogWarning($"DeviceDisconnectMonitor: device disconnected — raw='{m.rawDeviceName}' display='{m.displayName}' kind={m.kind}.");

        if (UIManager.Instance != null)
            UIManager.Instance.NotifyDeviceDisconnected(m.displayName);
        else
            Debug.LogWarning("DeviceDisconnectMonitor: UIManager.Instance is null — cannot show warning screen. Disconnect was logged.");
    }

    // =====================================
    // DEVICE ENUMERATION HELPERS
    // =====================================

    private List<AkDeviceDescription> EnumerateActiveWwiseSystemDevices()
    {
        var list = new List<AkDeviceDescription>();
        uint sharesetId = _cachedSystemSharesetId != 0u
            ? _cachedSystemSharesetId
            : AkSoundEngine.GetIDFromString("System");
        if (sharesetId == 0u)
        {
            if (debugLogPolls || debugLogs)
                Debug.LogWarning("DeviceDisconnectMonitor: Wwise 'System' shareset ID is 0 — Wwise probably not initialized yet.");
            return list;
        }

        uint deviceCount = AkSoundEngine.GetNumOutputDevices(sharesetId);
        if (deviceCount == 0u)
            return list;

        var devices = new AkDeviceDescriptionArray((int)deviceCount);
        AkSoundEngine.GetDeviceList(sharesetId, out deviceCount, devices);

        for (int i = 0; i < devices.Capacity && i < (int)deviceCount; i++)
        {
            var desc = devices[i];
            if (desc == null) continue;
            // Treat anything other than Active as effectively-disconnected for warning purposes.
            if (desc.deviceStateMask == AkAudioDeviceState.AkDeviceState_Active)
                list.Add(desc);
        }
        return list;
    }

    private static bool WwiseDeviceListContains(List<AkDeviceDescription> devices, string rawName)
    {
        if (devices == null) return false;
        for (int i = 0; i < devices.Count; i++)
        {
            if (string.Equals(devices[i].deviceName, rawName, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static bool MicrophoneDeviceListContains(string[] devices, string rawName)
    {
        if (devices == null) return false;
        for (int i = 0; i < devices.Length; i++)
        {
            if (string.Equals(devices[i], rawName, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private string FindDefaultWwiseOutputName(List<AkDeviceDescription> wwiseDevices)
    {
        for (int i = 0; i < wwiseDevices.Count; i++)
        {
            if (wwiseDevices[i].isDefaultDevice)
                return wwiseDevices[i].deviceName;
        }
        return null;
    }

    private string ResolveActiveMicrophoneName()
    {
        if (imitoneVoiceInterpreter != null && !string.IsNullOrEmpty(imitoneVoiceInterpreter.MicrophoneDeviceName))
            return imitoneVoiceInterpreter.MicrophoneDeviceName;

        var devices = Microphone.devices;
        if (devices != null && devices.Length > 0 && !string.IsNullOrEmpty(devices[0]))
            return devices[0];

        return null;
    }

    // =====================================
    // FRIENDLY-NAME MAPPING
    // =====================================

    private string FriendlyNameFor(string rawName, string fallbackKind)
    {
        if (friendlyNameOverrides != null)
        {
            for (int i = 0; i < friendlyNameOverrides.Count; i++)
            {
                if (string.Equals(friendlyNameOverrides[i].rawDeviceName, rawName, StringComparison.Ordinal)
                    && !string.IsNullOrWhiteSpace(friendlyNameOverrides[i].displayName))
                    return friendlyNameOverrides[i].displayName;
            }
        }

        // Built-in mappings for the Wwise lights/vibro devices on either platform.
        if (string.Equals(rawName, LightControl.KasinaWwiseDeviceName, StringComparison.Ordinal))
            return "Kasina";
        if (string.Equals(rawName, LightControl.LiminaWwiseDeviceName, StringComparison.Ordinal))
            return "Limina";

        string trimmed = rawName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return fallbackKind ?? "Unknown device";
        if (!string.IsNullOrEmpty(fallbackKind))
            return $"{fallbackKind}: {trimmed}";
        return trimmed;
    }

    // =====================================
    // DEBUG / QA
    // =====================================

    /// <summary>Editor-only QA helper — simulates the first still-connected monitored device disconnecting, so you can verify the splash + text path without unplugging hardware.</summary>
    [ContextMenu("DEBUG — Simulate disconnect of first still-connected monitored device")]
    private void DebugSimulateFirstDisconnect()
    {
        if (!_baselineCaptured)
        {
            Debug.LogWarning("DeviceDisconnectMonitor.DebugSimulate: baseline not yet captured — wait for baselineDelaySeconds after entering Play mode.");
            return;
        }
        for (int i = 0; i < _monitored.Count; i++)
        {
            if (_alreadyNotified.Contains(_monitored[i].rawDeviceName))
                continue;
            Debug.LogWarning($"DeviceDisconnectMonitor.DebugSimulate: forcing disconnect of '{_monitored[i].rawDeviceName}'.");
            ReportDisconnect(_monitored[i]);
            return;
        }
        Debug.LogWarning("DeviceDisconnectMonitor.DebugSimulate: no monitored devices are currently still-connected (either nothing was baselined, or all have already been flagged).");
    }

    /// <summary>Editor-only QA helper — re-runs the baseline + clears already-notified set. Useful after toggling devices in OS settings.</summary>
    [ContextMenu("DEBUG — Reset baseline (re-capture monitored device set)")]
    private void DebugResetBaseline()
    {
        _monitored.Clear();
        _alreadyNotified.Clear();
        _baselineCaptured = false;
        StartCoroutine(CaptureBaselineAfterDelay());
        Debug.LogWarning($"DeviceDisconnectMonitor.DebugReset: cleared monitored set; re-baselining in {Mathf.Max(0f, baselineDelaySeconds)}s.");
    }
}
