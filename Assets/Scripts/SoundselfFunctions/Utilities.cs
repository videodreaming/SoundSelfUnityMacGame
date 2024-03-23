using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    //A boolean function that outputs "true" on the frame that the input value (which should be any type of input) changes
    // This method checks if the input value has changed since the last frame
    // It uses a dictionary to store the previous values for different types of inputs
    // The key for each type is the full name of the type
   
   /*
    public static bool OnChange<T>(T value, bool returnTrueOnFirstFrame = true)
    {
        // Static dictionary to store previous values
        static Dictionary<string, T> previousValues = new Dictionary<string, T>();

        // Get the key for the current type
        string key = typeof(T).FullName;

        // If the dictionary does not contain the key, store the current value and return false
        if (!previousValues.ContainsKey(key))
        {
            previousValues[key] = value;
            return returnTrueOnFirstFrame;
        }

        // Get the previous value for the current type
        T previousValue = previousValues[key];

        // If the current value is different from the previous value, update the dictionary and return true
        if (!value.Equals(previousValue))
        {
            previousValues[key] = value;
            return true;
        }

        // If the current value is the same as the previous value, return false
        return false;
    }
    */
}
