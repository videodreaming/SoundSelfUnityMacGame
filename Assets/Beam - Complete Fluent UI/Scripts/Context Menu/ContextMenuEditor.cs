#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ContextMenu))]
    public class ContextMenuEditor : Editor
    {
        private GUISkin customSkin;
        private ContextMenu cmTarget;

        private void OnEnable()
        {
            cmTarget = (ContextMenu)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var menuItems = serializedObject.FindProperty("menuItems");

            var contextRect = serializedObject.FindProperty("contextRect");
            var contextPopup = serializedObject.FindProperty("contextPopup");
            var buttonPreset = serializedObject.FindProperty("buttonPreset");
            var separatorPreset = serializedObject.FindProperty("separatorPreset");
            var itemParent = serializedObject.FindProperty("itemParent");
            var boundTrigger = serializedObject.FindProperty("boundTrigger");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
            GUILayout.BeginVertical();
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(menuItems, new GUIContent("Menu Items"), true);
            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 10);
            BeamUIEditorHandler.DrawProperty(contextRect, customSkin, "Context Rect");
            BeamUIEditorHandler.DrawProperty(contextPopup, customSkin, "Context Popup");
            BeamUIEditorHandler.DrawProperty(buttonPreset, customSkin, "Button Preset");
            BeamUIEditorHandler.DrawProperty(separatorPreset, customSkin, "Seperator Preset");
            BeamUIEditorHandler.DrawProperty(itemParent, customSkin, "Item Parent");
            BeamUIEditorHandler.DrawProperty(boundTrigger, customSkin, "Bound Trigger");

            if (Application.isPlaying == false) { this.Repaint(); }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif