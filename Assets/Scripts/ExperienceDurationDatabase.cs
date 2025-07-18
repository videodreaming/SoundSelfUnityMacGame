using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExperienceDurationDatabase", menuName = "ScriptableObjects/ExperienceDurationDatabase", order = 1)]
public class ExperienceDurationDatabase : ScriptableObject
{

    [System.Serializable]
    public class ModeDuration
    {
        public string modeName;
        public float durationInSeconds;
    }

    public ModeDuration[] modeDurations;

    public float GetDurationForMode(string modeName)
    {
        foreach (var entry in modeDurations)
        {
            if (entry.modeName == modeName) return entry.durationInSeconds;
        }
        Debug.LogWarning("Mode not found: " + modeName);
        return 0f; // Default value if mode not found
    }
}
