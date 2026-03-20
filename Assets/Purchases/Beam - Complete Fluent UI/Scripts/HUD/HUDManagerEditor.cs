#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HUDManager))]
    public class HUDManagerEditor : Editor
    {
        private HUDManager hmTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            hmTarget = (HUDManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var HUDPanel = serializedObject.FindProperty("HUDPanel");

            var fadeSpeed = serializedObject.FindProperty("fadeSpeed");
            var defaultBehaviour = serializedObject.FindProperty("defaultBehaviour");

            var onSetVisible = serializedObject.FindProperty("onSetVisible");
            var onSetInvisible = serializedObject.FindProperty("onSetInvisible");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            BeamUIEditorHandler.DrawProperty(HUDPanel, customSkin, "HUD Panel");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            BeamUIEditorHandler.DrawProperty(fadeSpeed, customSkin, "Fade Speed", "Sets the fade animation speed.");
            BeamUIEditorHandler.DrawProperty(defaultBehaviour, customSkin, "Default Behaviour");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
            EditorGUILayout.PropertyField(onSetVisible, new GUIContent("On Set Visible"), true);
            EditorGUILayout.PropertyField(onSetInvisible, new GUIContent("On Set Invisible"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif