#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NewsSlider))]
    public class NewsSliderEditor : Editor
    {
        private NewsSlider nsTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            nsTarget = (NewsSlider)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            BeamUIEditorHandler.DrawComponentHeader(customSkin, "TopHeader_NewsSlider");

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

            var items = serializedObject.FindProperty("items");

            var itemPreset = serializedObject.FindProperty("itemPreset");
            var itemParent = serializedObject.FindProperty("itemParent");
            var timerPreset = serializedObject.FindProperty("timerPreset");
            var timerParent = serializedObject.FindProperty("timerParent");

            var allowUpdate = serializedObject.FindProperty("allowUpdate");
            var useLocalization = serializedObject.FindProperty("useLocalization");
            var sliderTimer = serializedObject.FindProperty("sliderTimer");
            var updateMode = serializedObject.FindProperty("updateMode");

            switch (currentTab)
            {
                case 0:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.PropertyField(items, new GUIContent("Slider Items"), true);
                    EditorGUI.indentLevel = 0;
                    break;

                case 1:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    BeamUIEditorHandler.DrawProperty(itemPreset, customSkin, "Item Preset");
                    BeamUIEditorHandler.DrawProperty(itemParent, customSkin, "Item Parent");
                    BeamUIEditorHandler.DrawProperty(timerPreset, customSkin, "Timer Preset");
                    BeamUIEditorHandler.DrawProperty(timerParent, customSkin, "Timer Parent");
                    break;

                case 2:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    allowUpdate.boolValue = BeamUIEditorHandler.DrawToggle(allowUpdate.boolValue, customSkin, "Allow Update", "Pause or unpause the slider.");
                    useLocalization.boolValue = BeamUIEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Bypasses localization functions when disabled.");
                    BeamUIEditorHandler.DrawProperty(sliderTimer, customSkin, "Slider Timer");
                    BeamUIEditorHandler.DrawProperty(updateMode, customSkin, "Update Mode");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif