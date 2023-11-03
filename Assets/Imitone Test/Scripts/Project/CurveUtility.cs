using UnityEngine;

public class CurveUtility
{
    public static float Damp(float currentValue, float targetValue, ref float velocity, float damp)
    {
        return Mathf.SmoothDamp(currentValue, targetValue, ref velocity, damp);
    }

    public static float Damp2(float currentValue, float targetValue, ref float velocity, float velocity2, float damp, float damp2)
    {
        float target = Mathf.SmoothDamp(currentValue, targetValue, ref velocity, damp);
        return Mathf.SmoothDamp(currentValue, target, ref velocity2, damp2);
    }

    public static float Linear(float currentValue, float targetValue, float linear)
    {
        return Mathf.Lerp(currentValue, targetValue,linear);
    }
}
