using System.Collections.Generic;
using UnityEngine;

public static class VariableChangeTracker
{
    private static Dictionary<string, object> previousValues = new Dictionary<string, object>();

    public static bool CheckVariableChange<T>(string variableName, T currentValue)
    {
        // Key to uniquely identify variable locations, combining the variable name with the caller's information
        var stackTrace = new System.Diagnostics.StackTrace();
        var frame = stackTrace.GetFrame(1); // 1 for immediate caller
        var method = frame.GetMethod();
        var uniqueKey = $"{method.ReflectedType.FullName}.{variableName}";

        // If the variable name is not in the dictionary, add it with the current value.
        if (!previousValues.ContainsKey(uniqueKey))
        {
            previousValues[uniqueKey] = currentValue;
            return false; // Assume no change if it's the first check.
        }

        // Compare the current value with the previous value.
        if (!Equals(previousValues[uniqueKey], currentValue))
        {
            // If different, update the previous value and return true.
            previousValues[uniqueKey] = currentValue;
            return true;
        }

        return false; // No change detected.
    }

    
}
