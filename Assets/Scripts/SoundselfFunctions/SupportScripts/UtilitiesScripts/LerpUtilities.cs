using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpUtilities 
{
    
    private static Dictionary<string, float> dampedTargets = new Dictionary<string, float>();

    public static float Damp2(float currentValue, float targetValue,  float velocity, float velocity2, float damp, float damp2, float linear)
    {
        float target = Mathf.SmoothDamp(currentValue, targetValue, ref velocity, damp);
        float target2 = Mathf.SmoothDamp(currentValue, target, ref velocity2, damp2);
        return Mathf.Lerp(currentValue, target2, linear);
    }
    public static float LerpAndInverse(float input, float inputa, float inputb, float outputa, float outputb, bool clamp = false)
    {
        float output = Mathf.Lerp(outputa, outputb, Mathf.InverseLerp(inputa, inputb, input));
        if (clamp)
        {
            if(outputa < outputb)
            output = Mathf.Clamp(output, outputa, outputb);
            else
            output = Mathf.Clamp(output, outputb, outputa);
        }
        return output;

    }

    public static float DampTool(string key, float currentValue, float target, float damp1 = 1f, float damp2 = 1f, float linear = 0f, float initialValue = 0f)
    {
        //if the dictionary does not contain the key, add it with the initialValue
        if(currentValue == target)
        return currentValue;
        else
        {
            //create a string that conbines key with the caller's information
            //var stackTrace = new System.Diagnostics.StackTrace();
            //var frame = stackTrace.GetFrame(1); // 1 for immediate caller
            //var method = frame.GetMethod();
            //var uniqueKey = $"{method.ReflectedType.FullName}.{key}";

            if (!dampedTargets.ContainsKey(key))
            {
                dampedTargets[key] = initialValue;
                //Debug.Log($"Added new key {key} to dampedTargets dictionary");
            }

            if (damp1 != 0f)
            dampedTargets[key] = Mathf.Lerp(dampedTargets[key], target, damp1);
            
            float _lerpTarget2 = currentValue;
            if (damp2 != 0f)
            _lerpTarget2 = Mathf.Lerp(currentValue, dampedTargets[key], damp2);

            if (currentValue > target)
            return Mathf.Max(_lerpTarget2 - linear, target);
            else
            return Mathf.Min(_lerpTarget2 + linear, target);
        }
    }
    public static void CleanUpDampTool(string key)
    {
        //var stackTrace = new System.Diagnostics.StackTrace();
        //var frame = stackTrace.GetFrame(1); // 1 for immediate caller
        //var method = frame.GetMethod();
        //var uniqueKey = $"{method.ReflectedType.FullName}.{key}";
        dampedTargets.Remove(key);
    }
}
