using System;
using System.Collections.Generic;
using SoundSelf.Sequence;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws <see cref="StageVariant"/> as a named dropdown (not a raw int).
/// When the field is on a <see cref="SequenceStage"/> (<c>*.variant</c>), options are filtered by sibling <c>type</c> prefix (e.g. SetMenu → Menu_* only).
/// </summary>
[CustomPropertyDrawer(typeof(StageVariant))]
public class StageVariantDrawer : PropertyDrawer
{
    private static string[] _sortedLabels;
    private static int[] _sortedValues;
    private static int _cachedEnumCount = -1;

    [InitializeOnLoadMethod]
    private static void ClearCacheOnReload() => InvalidateCache();

    private static void InvalidateCache()
    {
        _sortedLabels = null;
        _sortedValues = null;
        _cachedEnumCount = -1;
    }

    private static void EnsureSortedCache()
    {
        var values = (StageVariant[])Enum.GetValues(typeof(StageVariant));
        if (_sortedLabels != null && _cachedEnumCount == values.Length)
            return;

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

        _cachedEnumCount = values.Length;
    }

    private static string GetVariantPrefixForStageType(StageType type) =>
        type switch
        {
            StageType.Calibration => "Calibration_",
            StageType.Opening => "Opening_",
            StageType.Tutorial => "Tutorial_",
            StageType.Playground => "Playground_",
            StageType.Savasana => "Savasana_",
            StageType.SetMenu => "Menu_",
            StageType.MusicPlaylist => "Playlist_",
            StageType.Inquiry => "Inquiry_",
            StageType.End => "End_",
            StageType.LinearAudio => "Linear_",
            StageType.StartCountdown => "Countdown_",
            StageType.Code => "Code_",
            _ => null,
        };

    private static bool TryGetParentStageType(SerializedProperty variantProperty, out StageType stageType)
    {
        stageType = default;
        if (variantProperty == null || !variantProperty.propertyPath.EndsWith(".variant", StringComparison.Ordinal))
            return false;

        string typePath = variantProperty.propertyPath.Substring(0, variantProperty.propertyPath.Length - ".variant".Length) + ".type";
        SerializedProperty typeProp = variantProperty.serializedObject.FindProperty(typePath);
        if (typeProp == null || typeProp.propertyType != SerializedPropertyType.Enum)
            return false;

        stageType = (StageType)typeProp.intValue;
        return true;
    }

    private static void BuildOptionsForStageType(StageType stageType, out string[] labels, out int[] values)
    {
        EnsureSortedCache();
        string prefix = GetVariantPrefixForStageType(stageType);
        if (string.IsNullOrEmpty(prefix))
        {
            labels = _sortedLabels;
            values = _sortedValues;
            return;
        }

        var filtered = new List<(string name, int val)>();
        for (int i = 0; i < _sortedValues.Length; i++)
        {
            var v = (StageVariant)_sortedValues[i];
            if (v == StageVariant.None || v.ToString().StartsWith(prefix, StringComparison.Ordinal))
                filtered.Add((_sortedLabels[i], _sortedValues[i]));
        }

        labels = new string[filtered.Count];
        values = new int[filtered.Count];
        for (int i = 0; i < filtered.Count; i++)
        {
            labels[i] = filtered[i].name;
            values[i] = filtered[i].val;
        }
    }

    private static int IndexOfValue(int intValue, int[] valueList)
    {
        for (int i = 0; i < valueList.Length; i++)
        {
            if (valueList[i] == intValue)
                return i;
        }

        int none = (int)StageVariant.None;
        for (int i = 0; i < valueList.Length; i++)
        {
            if (valueList[i] == none)
                return i;
        }

        return 0;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

        string[] labels;
        int[] values;
        if (TryGetParentStageType(property, out StageType stageType))
            BuildOptionsForStageType(stageType, out labels, out values);
        else
        {
            EnsureSortedCache();
            labels = _sortedLabels;
            values = _sortedValues;
        }

        int currentInt = property.intValue;
        int selectedIndex = IndexOfValue(currentInt, values);

        EditorGUI.BeginChangeCheck();
        Rect fieldRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        int newIndex = EditorGUI.Popup(fieldRect, selectedIndex, labels);
        if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < values.Length)
            property.intValue = values[newIndex];

        EditorGUI.showMixedValue = false;
        EditorGUI.EndProperty();
    }
}
