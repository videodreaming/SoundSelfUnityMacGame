#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CustomEditor(typeof(UIManagerText))]
    public class UIManagerTextEditor : Editor
    {
        private UIManagerText uimtTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            uimtTarget = (UIManagerText)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var UIManagerAsset = serializedObject.FindProperty("UIManagerAsset");
            var fontType = serializedObject.FindProperty("fontType");
            var colorType = serializedObject.FindProperty("colorType");
            var useCustomColor = serializedObject.FindProperty("useCustomColor");
            var useCustomAlpha = serializedObject.FindProperty("useCustomAlpha");
            var useCustomFont = serializedObject.FindProperty("useCustomFont");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            BeamUIEditorHandler.DrawProperty(UIManagerAsset, customSkin, "UI Manager");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);

            if (uimtTarget.UIManagerAsset != null)
            {
                if (useCustomFont.boolValue == true) { GUI.enabled = false; }
                BeamUIEditorHandler.DrawProperty(fontType, customSkin, "Font Type");
                GUI.enabled = true;
                BeamUIEditorHandler.DrawProperty(colorType, customSkin, "Color Type");
                useCustomColor.boolValue = BeamUIEditorHandler.DrawToggle(useCustomColor.boolValue, customSkin, "Use Custom Color");
                if (useCustomColor.boolValue == true) { GUI.enabled = false; }
                useCustomAlpha.boolValue = BeamUIEditorHandler.DrawToggle(useCustomAlpha.boolValue, customSkin, "Use Custom Alpha");
                GUI.enabled = true;
                useCustomFont.boolValue = BeamUIEditorHandler.DrawToggle(useCustomFont.boolValue, customSkin, "Use Custom Font");
            }

            else { EditorGUILayout.HelpBox("UI Manager should be assigned.", MessageType.Error); }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif