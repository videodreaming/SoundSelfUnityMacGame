using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DirectVoiceMonitoring))]
public class DirectVoiceMonitoringEditor : Editor
{
    static readonly string[] ReadOnlyPropertyNames =
    {
        "monitoringVolume",
        "monitoringAttenuated",
        "monitoringAttenuationDb",
        "micMixerInitializationVolumeDb",
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Gray fields are read-only (see Docs/INSPECTOR_CLEANUP_IMITONE_AND_DVR2.md). " +
            "Object references stay editable.",
            MessageType.None);

        InspectorFieldDrawUtility.DrawPropertiesInOrder(serializedObject, ReadOnlyPropertyNames);

        serializedObject.ApplyModifiedProperties();
    }
}
