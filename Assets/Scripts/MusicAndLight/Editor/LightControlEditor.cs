using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightControl))]
public class LightControlEditor : Editor
{
    const string StrobeColorProp = "currentStrobeColor";
    const string WaveColorProp = "currentWaveColor";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Current color world (Play Mode)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "RGB sent to AVS after _brightness scaling. Strobe = waves 1–2; Wave = wave 3. Updates in Play Mode when the color world changes.",
            MessageType.None);

        var strobe = serializedObject.FindProperty(StrobeColorProp);
        var wave = serializedObject.FindProperty(WaveColorProp);
        if (strobe != null)
            EditorGUILayout.PropertyField(strobe, new GUIContent("Strobe (waves 1–2)"), true);
        else
            EditorGUILayout.HelpBox($"Serialized property \"{StrobeColorProp}\" not found on LightControl.", MessageType.Error);

        if (wave != null)
            EditorGUILayout.PropertyField(wave, new GUIContent("Wave (wave 3)"), true);
        else
            EditorGUILayout.HelpBox($"Serialized property \"{WaveColorProp}\" not found on LightControl.", MessageType.Error);

        EditorGUILayout.Space(10);

        // Draw the rest of the component but not these two colors (they are shown above).
        DrawPropertiesExcluding(serializedObject, StrobeColorProp, WaveColorProp);

        serializedObject.ApplyModifiedProperties();
    }
}
