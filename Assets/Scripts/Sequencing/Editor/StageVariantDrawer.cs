using System;
using System.Collections.Generic;
using SoundSelf.Sequence;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws <see cref="StageVariant"/> as a named dropdown (not a raw int). Options are sorted alphabetically by enum name.
/// </summary>
[CustomPropertyDrawer(typeof(StageVariant))]
public class StageVariantDrawer : PropertyDrawer
{
    private static string[] _sortedLabels;
    private static int[] _sortedValues;

    private static void EnsureSortedCache()
    {
        if (_sortedLabels != null)
            return;

        var values = (StageVariant[])Enum.GetValues(typeof(StageVariant));
        var pairs = new List<(string name, int val)>(values.Length);
        foreach (StageVariant v in values)
            pairs.Add((v.ToString(), (int)v));

        pairs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        _sortedLabels = new string[pairs.Count];
        _sortedValues = new int[pairs.Count];
        for (int i = 0; i < pairs.Count; i++)
        {
            _sortedLabels[i] = pairs[i].name;
            _sortedValues[i] = pairs[i].val;
        }
    }

    private static int IndexOfValue(int intValue)
    {
        EnsureSortedCache();
        for (int i = 0; i < _sortedValues.Length; i++)
        {
            if (_sortedValues[i] == intValue)
                return i;
        }
        // Unknown serialized int — fall back to None in the sorted list
        int none = (int)StageVariant.None;
        for (int i = 0; i < _sortedValues.Length; i++)
        {
            if (_sortedValues[i] == none)
                return i;
        }
        return 0;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

        EnsureSortedCache();
        int currentInt = property.intValue;
        int selectedIndex = IndexOfValue(currentInt);

        EditorGUI.BeginChangeCheck();
        Rect fieldRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        int newIndex = EditorGUI.Popup(fieldRect, selectedIndex, _sortedLabels);
        if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < _sortedValues.Length)
            property.intValue = _sortedValues[newIndex];

        EditorGUI.showMixedValue = false;
        EditorGUI.EndProperty();
    }
}
