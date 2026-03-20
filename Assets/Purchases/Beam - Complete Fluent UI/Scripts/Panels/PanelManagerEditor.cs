#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CustomEditor(typeof(PanelManager))]
    public class PanelManagerEditor : Editor
    {
        private GUISkin customSkin;
        private PanelManager pmTarget;
        private int currentTab;

        private void OnEnable()
        {
            pmTarget = (PanelManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            BeamUIEditorHandler.DrawComponentHeader(customSkin, "TopHeader_PanelManager");

            GUIContent[] toolbarTabs = new GUIContent[3];
            toolbarTabs[0] = new GUIContent("Content");
            toolbarTabs[1] = new GUIContent("Resources");
            toolbarTabs[2] = new GUIContent("Settings");

            currentTab = BeamUIEditorHandler.DrawTabs(currentTab, toolbarTabs, customSkin);


            if (GUILayout.Button(new GUIContent("Chat List", "Chat List"), customSkin.FindStyle("Tab_Content")))
                currentTab = 0;
            if (GUILayout.Button(new GUIContent("Resources", "Resources"), customSkin.FindStyle("Tab_Resources")))
                currentTab = 1;
            if (GUILayout.Button(new GUIContent("Settings", "Settings"), customSkin.FindStyle("Tab_Settings")))
                currentTab = 2;

            GUILayout.EndHorizontal();

            var panels = serializedObject.FindProperty("panels");
            var currentPanelIndex = serializedObject.FindProperty("currentPanelIndex");

            var indicator = serializedObject.FindProperty("indicator");
            var header = serializedObject.FindProperty("header");

            var cullPanels = serializedObject.FindProperty("cullPanels");
            var onPanelChanged = serializedObject.FindProperty("onPanelChanged");
            var initializeButtons = serializedObject.FindProperty("initializeButtons");
            var checkForPanelOrder = serializedObject.FindProperty("checkForPanelOrder");
            var useCooldownForHotkeys = serializedObject.FindProperty("useCooldownForHotkeys");
            var bypassAnimationOnEnable = serializedObject.FindProperty("bypassAnimationOnEnable");
            var updateMode = serializedObject.FindProperty("updateMode");
            var panelMode = serializedObject.FindProperty("panelMode");
            var animationSpeed = serializedObject.FindProperty("animationSpeed");
            var indicatorDuration = serializedObject.FindProperty("indicatorDuration");
            var indicatorCurve = serializedObject.FindProperty("indicatorCurve");
            var indicatorMode = serializedObject.FindProperty("indicatorMode");

            switch (currentTab)
            {
                case 0:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);

                    if (pmTarget.currentPanelIndex > pmTarget.panels.Count - 1) { pmTarget.currentPanelIndex = 0; }
                    if (pmTarget.panels.Count != 0)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();

                        GUI.enabled = false;
                        EditorGUILayout.LabelField(new GUIContent("Current Panel:"), customSkin.FindStyle("Text"), GUILayout.Width(82));
                        GUI.enabled = true;
                        EditorGUILayout.LabelField(new GUIContent(pmTarget.panels[currentPanelIndex.intValue].panelName), customSkin.FindStyle("Text"));

                        GUILayout.EndHorizontal();
                        GUILayout.Space(2);

                        if (Application.isPlaying == true) { GUI.enabled = false; }

                        currentPanelIndex.intValue = EditorGUILayout.IntSlider(currentPanelIndex.intValue, 0, pmTarget.panels.Count - 1);

                        if (Application.isPlaying == false && pmTarget.panels[currentPanelIndex.intValue].panelObject != null)
                        {
                            for (int i = 0; i < pmTarget.panels.Count; i++)
                            {
                                if (i == currentPanelIndex.intValue)
                                {
                                    var tempCG = pmTarget.panels[currentPanelIndex.intValue].panelObject.GetComponent<CanvasGroup>();
                                    if (tempCG != null) { tempCG.alpha = 1; }
                                }

                                else if (pmTarget.panels[i].panelObject != null)
                                {
                                    var tempCG = pmTarget.panels[i].panelObject.GetComponent<CanvasGroup>();
                                    if (tempCG != null) { tempCG.alpha = 0; }
                                }
                            }
                        }

                        if (pmTarget.panels[pmTarget.currentPanelIndex].panelObject != null && GUILayout.Button("Select Current Panel", customSkin.button)) { Selection.activeObject = pmTarget.panels[pmTarget.currentPanelIndex].panelObject; }
                        GUI.enabled = true;
                        GUILayout.EndVertical();

                    }

                    else { EditorGUILayout.HelpBox("Panel List is empty. Create a new item to see more options.", MessageType.Info); }

                    GUILayout.BeginVertical();
                    EditorGUI.indentLevel = 1;

                    EditorGUILayout.PropertyField(panels, new GUIContent("Panel Items"), true);

                    EditorGUI.indentLevel = 0;
                    GUILayout.EndVertical();

                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onPanelChanged, new GUIContent("On Panel Changed"), true);
                    break;

                case 1:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    BeamUIEditorHandler.DrawProperty(indicator, customSkin, "Indicator");
                    BeamUIEditorHandler.DrawProperty(header, customSkin, "Header");
                    break;

                case 2:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    cullPanels.boolValue = BeamUIEditorHandler.DrawToggle(cullPanels.boolValue, customSkin, "Cull Panels", "Disables unused panels.");
                    initializeButtons.boolValue = BeamUIEditorHandler.DrawToggle(initializeButtons.boolValue, customSkin, "Initialize Buttons", "Automatically adds necessary events to buttons.");
                    checkForPanelOrder.boolValue = BeamUIEditorHandler.DrawToggle(checkForPanelOrder.boolValue, customSkin, "Check For Panel Order", "Adjusts animation direction based on panel list order.");
                    useCooldownForHotkeys.boolValue = BeamUIEditorHandler.DrawToggle(useCooldownForHotkeys.boolValue, customSkin, "Use Cooldown For Hotkeys", "Fixes input issues when switching panels via hotkeys.");
                    bypassAnimationOnEnable.boolValue = BeamUIEditorHandler.DrawToggle(bypassAnimationOnEnable.boolValue, customSkin, "Bypass Animation On Enable");
                    BeamUIEditorHandler.DrawProperty(updateMode, customSkin, "Update Mode");
                    BeamUIEditorHandler.DrawProperty(panelMode, customSkin, "Panel Mode");
                    BeamUIEditorHandler.DrawProperty(animationSpeed, customSkin, "Animation Speed");
                    BeamUIEditorHandler.DrawProperty(indicatorDuration, customSkin, "Indicator Duration");
                    BeamUIEditorHandler.DrawProperty(indicatorCurve, customSkin, "Indicator Curve");
                    BeamUIEditorHandler.DrawProperty(indicatorMode, customSkin, "Indicator Mode");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif