#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CustomEditor(typeof(CreditsManager))]
    public class CreditsManagerEditor : Editor
    {
        private CreditsManager cmTarget;
        private GUISkin customSkin;
        private int latestTabIndex;

        private void OnEnable()
        {
            cmTarget = (CreditsManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            BeamUIEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Credits");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            latestTabIndex = BeamUIEditorHandler.DrawTabs(latestTabIndex, toolbarTabs, customSkin);

            if (GUILayout.Button(new GUIContent("Content", "Content"), customSkin.FindStyle("Tab_Content")))
                latestTabIndex = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                latestTabIndex = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                latestTabIndex = 2;

            GUILayout.EndHorizontal();

            var creditsPreset = serializedObject.FindProperty("creditsPreset");

            var canvasGroup = serializedObject.FindProperty("canvasGroup");
            var backgroundImage = serializedObject.FindProperty("backgroundImage");
            var creditsListParent = serializedObject.FindProperty("creditsListParent");
            var scrollHelper = serializedObject.FindProperty("scrollHelper");
            var creditsSectionPreset = serializedObject.FindProperty("creditsSectionPreset");
            var creditsMentionPreset = serializedObject.FindProperty("creditsMentionPreset");

            var closeAutomatically = serializedObject.FindProperty("closeAutomatically");
            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");
            var scrollDelay = serializedObject.FindProperty("scrollDelay");
            var scrollSpeed = serializedObject.FindProperty("scrollSpeed");
            var boostValue = serializedObject.FindProperty("boostValue");
            var boostHotkey = serializedObject.FindProperty("boostHotkey");

            var onOpen = serializedObject.FindProperty("onOpen");
            var onClose = serializedObject.FindProperty("onClose");
            var onCreditsEnd = serializedObject.FindProperty("onCreditsEnd");

            switch (latestTabIndex)
            {
                case 0:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    BeamUIEditorHandler.DrawProperty(creditsPreset, customSkin, "Credits Preset");

                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onOpen, new GUIContent("On Open"), true);
                    EditorGUILayout.PropertyField(onClose, new GUIContent("On Close"), true);
                    EditorGUILayout.PropertyField(onCreditsEnd, new GUIContent("On Credits End"), true);
                    break;

                case 1:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    BeamUIEditorHandler.DrawProperty(canvasGroup, customSkin, "Canvas Group");
                    BeamUIEditorHandler.DrawProperty(backgroundImage, customSkin, "BG Image");
                    BeamUIEditorHandler.DrawProperty(creditsListParent, customSkin, "List Parent");
                    BeamUIEditorHandler.DrawProperty(scrollHelper, customSkin, "Scroll Helper");
                    BeamUIEditorHandler.DrawProperty(creditsSectionPreset, customSkin, "Section Preset");
                    BeamUIEditorHandler.DrawProperty(creditsMentionPreset, customSkin, "Mention Preset");
                    break;

                case 2:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    closeAutomatically.boolValue = BeamUIEditorHandler.DrawToggle(closeAutomatically.boolValue, customSkin, "Close Automatically");
                    BeamUIEditorHandler.DrawProperty(fadingMultiplier, customSkin, "Fading Multiplier", "Set the animation fade multiplier.");
                    BeamUIEditorHandler.DrawProperty(scrollDelay, customSkin, "Scroll Delay");
                    BeamUIEditorHandler.DrawProperty(scrollSpeed, customSkin, "Scroll Speed");
                    BeamUIEditorHandler.DrawProperty(boostValue, customSkin, "Boost Value");
                    EditorGUILayout.PropertyField(boostHotkey, new GUIContent("Boost Hotkey"), true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif