using UnityEngine;

public class DebugTimbre : MonoBehaviour
{
    // Assuming these values are constant, if not, they can be made public and set from the editor
    private float inputMin = 0.0f;
    private float inputMax = 3.0f;
    private float outputMin = -761.0f;
    private float outputMax = -406.0f;

    // Update this value from another script or inspector
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;

    void Update()
    {
        // Map the inputValue to the new range
        float mappedValue = MapValue(ImitoneVoiceIntepreter._timbre, inputMin, inputMax, outputMin, outputMax);
        float clampedXPosition = Mathf.Clamp(mappedValue, outputMin, outputMax);
        // Set the x position of this GameObject
        transform.position = new Vector3(clampedXPosition + 960.0f, transform.position.y, transform.position.z);
    }

    float MapValue(float value, float fromMin, float fromMax, float toMin, float toMax) 
    {
        // Ensure the value is clamped to avoid unexpected results
        value = Mathf.Clamp(value, fromMin, fromMax);

        // Map the value from one range to another
        return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
    }
}

