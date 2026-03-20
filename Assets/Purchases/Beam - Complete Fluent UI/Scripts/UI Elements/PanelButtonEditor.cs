#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PanelButton))]
    public class PanelButtonEditor : Editor
    {
        private PanelButton buttonTarget;
        private GUISkin customSkin;
        private int currentTab;

        private void OnEnable()
        {
            buttonTarget = (PanelButton)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            BeamUIEditorHandler.DrawComponentHeader(customSkin, "TopHeader_Button");

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

            var buttonIcon = serializedObject.FindProperty("buttonIcon");
            var selectedIcon = serializedObject.FindProperty("selectedIcon");
            var buttonText = serializedObject.FindProperty("buttonText");

            var disabledCG = serializedObject.FindProperty("disabledCG");
            var normalCG = serializedObject.FindProperty("normalCG");
            var highlightCG = serializedObject.FindProperty("highlightCG");
            var selectCG = serializedObject.FindProperty("selectCG");
            var disabledTextObj = serializedObject.FindProperty("disabledTextObj");
            var normalTextObj = serializedObject.FindProperty("normalTextObj");
            var highlightTextObj = serializedObject.FindProperty("highlightTextObj");
            var selectTextObj = serializedObject.FindProperty("selectTextObj");
            var disabledImageObj = serializedObject.FindProperty("disabledImageObj");
            var normalImageObj = serializedObject.FindProperty("normalImageObj");
            var highlightImageObj = serializedObject.FindProperty("highlightImageObj");
            var selectedImageObj = serializedObject.FindProperty("selectedImageObj");
            var seperator = serializedObject.FindProperty("seperator");

            var isInteractable = serializedObject.FindProperty("isInteractable");
            var isSelected = serializedObject.FindProperty("isSelected");
            var useLocalization = serializedObject.FindProperty("useLocalization");
            var useCustomText = serializedObject.FindProperty("useCustomText");
            var useSeperator = serializedObject.FindProperty("useSeperator");
            var useSounds = serializedObject.FindProperty("useSounds");
            var useUINavigation = serializedObject.FindProperty("useUINavigation");
            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");

            var onClick = serializedObject.FindProperty("onClick");
            var onHover = serializedObject.FindProperty("onHover");
            var onLeave = serializedObject.FindProperty("onLeave");
            var onSelect = serializedObject.FindProperty("onSelect");

            switch (currentTab)
            {
                case 0:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    BeamUIEditorHandler.DrawPropertyCW(buttonIcon, customSkin, "Button Icon", 86);
                    if (buttonTarget.buttonIcon != null) { BeamUIEditorHandler.DrawPropertyCW(selectedIcon, customSkin, "Selected Icon", 86); }
                    if (useCustomText.boolValue == false) { BeamUIEditorHandler.DrawPropertyCW(buttonText, customSkin, "Button Text", 86); }
                    if (buttonTarget.buttonIcon != null || useCustomText.boolValue == false) { buttonTarget.UpdateUI(); }

                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onClick, new GUIContent("On Click"), true);
                    EditorGUILayout.PropertyField(onHover, new GUIContent("On Hover"), true);
                    EditorGUILayout.PropertyField(onLeave, new GUIContent("On Leave"), true);
                    EditorGUILayout.PropertyField(onSelect, new GUIContent("On Select"), true);
                    break;

                case 1:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    BeamUIEditorHandler.DrawProperty(disabledCG, customSkin, "Disabled CG");
                    BeamUIEditorHandler.DrawProperty(normalCG, customSkin, "Normal CG");
                    BeamUIEditorHandler.DrawProperty(highlightCG, customSkin, "Highlight CG");
                    BeamUIEditorHandler.DrawProperty(selectCG, customSkin, "Select CG");
                    BeamUIEditorHandler.DrawProperty(disabledTextObj, customSkin, "Disabled Text");
                    BeamUIEditorHandler.DrawProperty(normalTextObj, customSkin, "Normal Text");
                    BeamUIEditorHandler.DrawProperty(highlightTextObj, customSkin, "Highlight Text");
                    BeamUIEditorHandler.DrawProperty(selectTextObj, customSkin, "Select Text");
                    BeamUIEditorHandler.DrawProperty(disabledImageObj, customSkin, "Disabled Image");
                    BeamUIEditorHandler.DrawProperty(normalImageObj, customSkin, "Normal Image");
                    BeamUIEditorHandler.DrawProperty(highlightImageObj, customSkin, "Highlight Image");
                    BeamUIEditorHandler.DrawProperty(selectedImageObj, customSkin, "Select Image");
                    BeamUIEditorHandler.DrawProperty(seperator, customSkin, "Seperator");
                    break;

                case 2:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    isInteractable.boolValue = BeamUIEditorHandler.DrawToggle(isInteractable.boolValue, customSkin, "Is Interactable");
                    isSelected.boolValue = BeamUIEditorHandler.DrawToggle(isSelected.boolValue, customSkin, "Is Selected");
                    useLocalization.boolValue = BeamUIEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Bypasses localization functions when disabled.");
                    useCustomText.boolValue = BeamUIEditorHandler.DrawToggle(useCustomText.boolValue, customSkin, "Use Custom Text", "Bypasses inspector values and allows manual editing.");
                    useSeperator.boolValue = BeamUIEditorHandler.DrawToggle(useSeperator.boolValue, customSkin, "Use Seperator");
                    useUINavigation.boolValue = BeamUIEditorHandler.DrawToggle(useUINavigation.boolValue, customSkin, "Use UI Navigation", "Enables controller navigation.");
                    useSounds.boolValue = BeamUIEditorHandler.DrawToggle(useSounds.boolValue, customSkin, "Use Button Sounds");
                    BeamUIEditorHandler.DrawProperty(fadingMultiplier, customSkin, "Fading Multiplier", "Set the animation fade multiplier.");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif