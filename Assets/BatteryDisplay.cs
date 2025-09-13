using UnityEngine;
using TMPro;

public class BatteryDisplayTMP : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI batteryText;

    void Update()
    {
        float level = SystemInfo.batteryLevel;
        BatteryStatus status = SystemInfo.batteryStatus;

        if (level >= 0f)
        {
            int percent = Mathf.RoundToInt(level * 100f);
            batteryText.text = $"Battery: {percent}% ({status})";
        }
        else
        {
            batteryText.text = "Battery info not available";
        }
    }
}
