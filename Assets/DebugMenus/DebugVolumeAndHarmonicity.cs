using UnityEngine;
using UnityEngine.UI;

public class BarControl : MonoBehaviour
{
    public RectTransform bar1;
    public RectTransform bar2;
    public RectTransform thresholdLine;
    public RectTransform VolumeThresholdLine;
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;

    void Update()
    {
        // Update bars height based on _dbValue
        float newHeight = CalculateBarHeight(ImitoneVoiceIntepreter._dbValue);
        bar1.sizeDelta = new Vector2(bar1.sizeDelta.x, newHeight);
        bar2.sizeDelta = new Vector2(bar2.sizeDelta.x, newHeight);

        // Update threshold line position based on _dbThreshold
        float thresholdPosition = CalculateThresholdPosition(ImitoneVoiceIntepreter._dbThreshold);
        float VolumeThresholdPosition = CalculateVolumeThresholdPosition(ImitoneVoiceIntepreter._dbThreshold);
        thresholdLine.anchoredPosition = new Vector2(thresholdLine.anchoredPosition.x, thresholdPosition);
    }
    float CalculateBarHeight(float dbValue)
    {
        // Clamp dbValue between -50 and -10
        dbValue = Mathf.Clamp(dbValue, -50, -10);

        // Map dbValue from the range [-50, -10] to [0, 1]
        float normalizedValue = Mathf.InverseLerp(-50, -10, dbValue);

        // Assuming maxBarHeight is the maximum height you want for your bars
        float maxBarHeight = 100; // You can adjust this value based on your UI

        // Calculate the actual height of the bar
        return normalizedValue * maxBarHeight;
    }
    

    float CalculateThresholdPosition(float dbThreshold)
    {
        // Clamp dbThreshold between -50 and -10
        dbThreshold = Mathf.Clamp(dbThreshold, -50, -10);

        // Map dbThreshold from the range [-50, -10] to [-192, 203]
        float yPos = Mathf.Lerp(-192, 203, Mathf.InverseLerp(-50, -10, dbThreshold));

        return yPos;
    }
    float CalculateVolumeThresholdPosition(float dbThreshold)
    {
        // Clamp dbThreshold between -50 and -10
        dbThreshold = Mathf.Clamp(dbThreshold, -50, -10);

        // Map dbThreshold from the range [-50, -10] to [-192, 203]
        float yPos2 = Mathf.Lerp(-192, 203, Mathf.InverseLerp(-50, -10, dbThreshold));

        return yPos2;
    }
}

