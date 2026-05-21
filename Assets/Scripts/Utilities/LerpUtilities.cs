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

    /// <summary>
    /// Two-stage damp toward <paramref name="target"/> with optional linear creep.
    ///
    /// <para>Frame-rate-independent: <paramref name="damp1"/>, <paramref name="damp2"/>, and
    /// <paramref name="linear"/> are interpreted as values tuned for ~60 FPS (one call per frame).
    /// The function scales them by <c>Time.unscaledDeltaTime * 60</c> so behavior at the legacy
    /// design framerate is mathematically identical, while lower (or higher) framerates produce
    /// the same wall-clock response. Without this scaling, capping <c>Application.targetFrameRate</c>
    /// below 60 visibly doubles half-lives in chant feel code.</para>
    ///
    /// <para>Assumes once-per-frame invocation per <paramref name="key"/> (see editor warning).
    /// Safe to call on any thread that owns the Unity main-thread context.</para>
    /// </summary>
    public static float DampTool(string key, float currentValue, float target, float damp1 = 1f, float damp2 = 1f, float linear = 0f, float initialValue = 0f)
    {
#if UNITY_EDITOR
        DampToolEditorWarnIfDuplicateKeySameFrame(key);
#endif
        if (currentValue == target)
            return currentValue;

        if (!dampedTargets.ContainsKey(key))
            dampedTargets[key] = initialValue;

        // Treat damp1/damp2/linear as per-frame values tuned at 60 FPS.
        // multiplier == 1 at 60 FPS (no change). At 30 FPS multiplier == 2, i.e. one call is
        // equivalent to running the legacy lerp twice; for fractional multipliers, Pow gives
        // the correct interpolated decay.
        float multiplier = Time.unscaledDeltaTime * 60f;
        // Guard against zero/negative dt (editor pause, first frame) — no damping that tick.
        if (multiplier <= 0f)
            return currentValue;

        float effDamp1 = damp1 != 0f ? 1f - Mathf.Pow(1f - Mathf.Clamp01(damp1), multiplier) : 0f;
        float effDamp2 = damp2 != 0f ? 1f - Mathf.Pow(1f - Mathf.Clamp01(damp2), multiplier) : 0f;
        float effLinear = linear * multiplier;

        if (effDamp1 != 0f)
            dampedTargets[key] = Mathf.Lerp(dampedTargets[key], target, effDamp1);

        float _lerpTarget2 = currentValue;
        if (effDamp2 != 0f)
            _lerpTarget2 = Mathf.Lerp(currentValue, dampedTargets[key], effDamp2);

        if (currentValue > target)
            return Mathf.Max(_lerpTarget2 - effLinear, target);
        else
            return Mathf.Min(_lerpTarget2 + effLinear, target);
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
