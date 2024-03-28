using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpUtilities 
{

    public static float Damp2(float currentValue, float targetValue,  float velocity, float velocity2, float damp, float damp2, float linear)
    {
        float target = Mathf.SmoothDamp(currentValue, targetValue, ref velocity, damp);
        float target2 = Mathf.SmoothDamp(currentValue, target, ref velocity2, damp2);
        return Mathf.Lerp(currentValue, target2, linear);
    }
    public static float LerpAndInverse(float input, float inputa, float inputb, float outputa, float outputb)
    {
        return Mathf.Lerp(outputa, outputb, Mathf.InverseLerp(inputa, inputb, input));
    }
}
