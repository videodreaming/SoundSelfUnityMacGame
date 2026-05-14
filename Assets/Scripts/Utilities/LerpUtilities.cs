using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpUtilities 
{
    
    private static Dictionary<string, float> dampedTargets = new Dictionary<string, float>();

#if UNITY_EDITOR
    private static int _dampToolEditorFrameStamp = -1;
    private static readonly HashSet<string> _dampToolEditorKeysThisFrame = new HashSet<string>();

    private static void DampToolEditorWarnIfDuplicateKeySameFrame(string key)
    {
        if (Time.frameCount != _dampToolEditorFrameStamp)
        {
            _dampToolEditorFrameStamp = Time.frameCount;
            _dampToolEditorKeysThisFrame.Clear();
        }

        if (!_dampToolEditorKeysThisFrame.Add(key))
            Debug.LogWarning(
                $"LerpUtilities.DampTool: key \"{key}\" was invoked twice in the same frame (frame {Time.frameCount}). " +
                "Shared static dictionary state may be inconsistent between callers.");
    }
#endif

    /// <summary>Optional default for <see cref="DampTool"/> <c>linear</c> when matching chant-style tiny steps (same magnitude as <c>_chantLerpLinear</c> in <see cref="GameValues"/>).</summary>
    public const float ChantLinearCreep = 0.0001f;

    // LerpAndInverse interpolates between two output values (outputa, outputb) based on where the input lies between inputa and inputb.
    // It first remaps 'input' from the input range [inputa, inputb] to a normalized [0, 1] value using Mathf.InverseLerp,
    // then linearly interpolates between outputa and outputb with Mathf.Lerp.
    // If 'clamp' is true, the result is clamped to always stay within [outputa, outputb] regardless of overshoot due to input range.
    // This is useful for mapping an input range to a different output range with optional clamping.
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
#if UNITY_EDITOR
        DampToolEditorWarnIfDuplicateKeySameFrame(key);
#endif
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
