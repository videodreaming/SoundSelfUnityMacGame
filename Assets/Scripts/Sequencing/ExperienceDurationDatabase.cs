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

        [Tooltip("Ordered chapter names for this mode.")]
        public List<string> chapters = new List<string>();
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

    public List<string> GetChaptersForMode(string modeName)
    {
        var entry = GetEntry(modeName);
        if (entry != null) return entry.chapters;

        Debug.LogWarning("Mode not found: " + modeName);
        return null; // or return new List<string>();
    }

        public bool TryGetMode(string modeName, out float duration, out List<string> chapters)
    {
        var entry = GetEntry(modeName);
        if (entry != null)
        {
            duration = entry.durationInSeconds;
            chapters = entry.chapters;
            return true;
        }

        duration = 0f;
        chapters = null;
        return false;
    }

    private ModeDuration GetEntry(string modeName)
    {
        // Safer compare than == (handles accidental whitespace / case if you want)
        foreach (var entry in modeDurations)
        {
            if (entry == null) continue;
            if (entry.modeName == modeName) return entry;
        }
        return null;
    }
}
