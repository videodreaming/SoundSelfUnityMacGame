using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ImitoneVoiceIntepreter))]
public class ImitoneVoiceIntepreterEditor : Editor
{
    static readonly string[] ReadOnlyPropertyNames =
    {
        "gameOn",
        "_dbThreshold",
        "_volumeChangeMeasurementWindow",
        "_volumeDropTriggerThresholdDB",
        "_noiseFloorMeasurementTime",
        "_thresholdAboveNoiseFloor",
        "_highPassCutoffHz",
        "normalizationEnabled",
        "gainRidingEnabled",
        "gainRidingTargetDb",
        "gainRidingGainDbClamp",
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Locked settings (read-only)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tuned values aligned with MainGame / code defaults. gameOn and _dbThreshold are runtime-driven. " +
            "Mic normalization, gain riding, noise-floor jump tuning, and HPF cutoff are fixed here.",
            MessageType.None);

        EditorGUI.BeginDisabledGroup(true);
        foreach (var propName in ReadOnlyPropertyNames)
        {
            var prop = serializedObject.FindProperty(propName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, true);
            else
                EditorGUILayout.HelpBox($"Property \"{propName}\" not found.", MessageType.Warning);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Editable", EditorStyles.boldLabel);
        DrawPropertiesExcluding(serializedObject, ReadOnlyPropertyNames);

        serializedObject.ApplyModifiedProperties();
    }
}
