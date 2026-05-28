using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightControl))]
public class LightControlEditor : Editor
{
    const string StrobeColorProp = "currentStrobeColor";
    const string WaveColorProp = "currentWaveColor";
    const string BrightnessProp = "_brightness";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Current color world (Play Mode)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "0–1 per channel after preset brightness (Wwise AVS volume uses 0–100 at runtime). Strobe = waves 1–2; Wave = wave 3. " +
            "Runtime mirror only — do not edit.",
            MessageType.None);

        var brightness = serializedObject.FindProperty(BrightnessProp);
        var strobe = serializedObject.FindProperty(StrobeColorProp);
        var wave = serializedObject.FindProperty(WaveColorProp);

        EditorGUI.BeginDisabledGroup(true);
        if (brightness != null)
            EditorGUILayout.PropertyField(brightness, new GUIContent("Preset brightness"));
        else
            EditorGUILayout.HelpBox($"Serialized property \"{BrightnessProp}\" not found on LightControl.", MessageType.Error);
        if (strobe != null)
            EditorGUILayout.PropertyField(strobe, new GUIContent("Strobe (waves 1–2)"), true);
        else
            EditorGUILayout.HelpBox($"Serialized property \"{StrobeColorProp}\" not found on LightControl.", MessageType.Error);

        if (wave != null)
            EditorGUILayout.PropertyField(wave, new GUIContent("Wave (wave 3)"), true);
        else
            EditorGUILayout.HelpBox($"Serialized property \"{WaveColorProp}\" not found on LightControl.", MessageType.Error);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        // Draw the rest of the component but not these two colors (they are shown above).
        DrawPropertiesExcluding(serializedObject, StrobeColorProp, WaveColorProp, BrightnessProp);

        serializedObject.ApplyModifiedProperties();
    }
}
