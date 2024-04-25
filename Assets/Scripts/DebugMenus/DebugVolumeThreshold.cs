using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugVolumeThreshold : MonoBehaviour
{
    public RectTransform blueBar;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter; // Reference to the other script

    private float minYPosition = -50f;
    private float maxYPosition = 50f;
    private float minThreshold = -50f;
    private float maxThreshold = -10f;

    void Update()
    {
        // Obtain the _dbThreshold from the other script
        float dbThreshold = imitoneVoiceIntepreter._dbThreshold;

        // Map the _dbThreshold to the bar's y position
        float newPositionY = Map(dbThreshold, minThreshold, maxThreshold, minYPosition, maxYPosition);

        // Set the new position of the blue bar
        blueBar.anchoredPosition = new Vector2(blueBar.anchoredPosition.x, newPositionY);
    }

    // Map a value from one range to another
    private float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget)
    {
        return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
    }
}
