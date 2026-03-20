#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SocialsWidget))]
    public class SocialsWidgetEditor : Editor
    {
        private SocialsWidget swTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            swTarget = (SocialsWidget)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            BeamUIEditorHandler.DrawComponentHeader(customSkin, "TopHeader_SocialsWidget");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = BeamUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var socials = serializedObject.FindProperty("socials");

            var itemPreset = serializedObject.FindProperty("itemPreset");
            var itemParent = serializedObject.FindProperty("itemParent");
            var buttonPreset = serializedObject.FindProperty("buttonPreset");
            var buttonParent = serializedObject.FindProperty("buttonParent");
            var background = serializedObject.FindProperty("background");

            var allowTransition = serializedObject.FindProperty("allowTransition");
            var useLocalization = serializedObject.FindProperty("useLocalization");
            var timer = serializedObject.FindProperty("timer");
            var tintSpeed = serializedObject.FindProperty("tintSpeed");
            var tintCurve = serializedObject.FindProperty("tintCurve");
            var updateMode = serializedObject.FindProperty("updateMode");

            switch (currentTab)
            {
                case 0:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(socials, new GUIContent("Socials"), true);
                    EditorGUI.indentLevel = 0;
                    break;

                case 1:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    BeamUIEditorHandler.DrawProperty(itemPreset, customSkin, "Item Preset");
                    BeamUIEditorHandler.DrawProperty(itemParent, customSkin, "Item Parent");
                    BeamUIEditorHandler.DrawProperty(buttonPreset, customSkin, "Button Preset");
                    BeamUIEditorHandler.DrawProperty(buttonParent, customSkin, "Button Parent");
                    BeamUIEditorHandler.DrawProperty(background, customSkin, "Background");
                    break;

                case 2:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    allowTransition.boolValue = BeamUIEditorHandler.DrawToggle(allowTransition.boolValue, customSkin, "Allow Transition", "Pause or unpause the transition.");
                    useLocalization.boolValue = BeamUIEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Bypasses localization functions when disabled.");
                    BeamUIEditorHandler.DrawProperty(timer, customSkin, "Timer");
                    BeamUIEditorHandler.DrawProperty(tintSpeed, customSkin, "Tint Speed");
                    BeamUIEditorHandler.DrawProperty(tintCurve, customSkin, "Tint Curve");
                    BeamUIEditorHandler.DrawProperty(updateMode, customSkin, "Update Mode");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif