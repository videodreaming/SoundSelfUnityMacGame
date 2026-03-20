#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AchievementManager))]
    public class AchievementManagerEditor : Editor
    {
        private AchievementManager amTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            amTarget = (AchievementManager)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var UIManagerAsset = serializedObject.FindProperty("UIManagerAsset");
            var allParent = serializedObject.FindProperty("allParent");
            var commonParent = serializedObject.FindProperty("commonParent");
            var rareParent = serializedObject.FindProperty("rareParent");
            var legendaryParent = serializedObject.FindProperty("legendaryParent");
            var achievementPreset = serializedObject.FindProperty("achievementPreset");
            var totalUnlockedObj = serializedObject.FindProperty("totalUnlockedObj");
            var totalValueObj = serializedObject.FindProperty("totalValueObj");
            var commonUnlockedObj = serializedObject.FindProperty("commonUnlockedObj");
            var commonlTotalObj = serializedObject.FindProperty("commonlTotalObj");
            var rareUnlockedObj = serializedObject.FindProperty("rareUnlockedObj");
            var rareTotalObj = serializedObject.FindProperty("rareTotalObj");
            var legendaryUnlockedObj = serializedObject.FindProperty("legendaryUnlockedObj");
            var legendaryTotalObj = serializedObject.FindProperty("legendaryTotalObj");

            var useLocalization = serializedObject.FindProperty("useLocalization");
            var useAlphabeticalOrder = serializedObject.FindProperty("useAlphabeticalOrder");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
            BeamUIEditorHandler.DrawProperty(UIManagerAsset, customSkin, "UI Manager");

            if (amTarget.UIManagerAsset != null)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(new GUIContent("Library Preset"), customSkin.FindStyle("Text"), GUILayout.Width(120));
                GUI.enabled = false;
                amTarget.UIManagerAsset.achievementLibrary = EditorGUILayout.ObjectField(amTarget.UIManagerAsset.achievementLibrary, typeof(AchievementLibrary), true) as AchievementLibrary;
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 10);
            BeamUIEditorHandler.DrawProperty(achievementPreset, customSkin, "Achievement Preset");
            BeamUIEditorHandler.DrawProperty(allParent, customSkin, "All Parent");
            BeamUIEditorHandler.DrawProperty(commonParent, customSkin, "Common Parent");
            BeamUIEditorHandler.DrawProperty(rareParent, customSkin, "Rare Parent");
            BeamUIEditorHandler.DrawProperty(legendaryParent, customSkin, "Legendary Parent");
            BeamUIEditorHandler.DrawProperty(totalUnlockedObj, customSkin, "Total Unlocked");
            BeamUIEditorHandler.DrawProperty(totalValueObj, customSkin, "Total Value");
            BeamUIEditorHandler.DrawProperty(commonUnlockedObj, customSkin, "Common Unlocked");
            BeamUIEditorHandler.DrawProperty(commonlTotalObj, customSkin, "Commonl Total");
            BeamUIEditorHandler.DrawProperty(rareUnlockedObj, customSkin, "Rare Unlocked");
            BeamUIEditorHandler.DrawProperty(rareTotalObj, customSkin, "Rare Total");
            BeamUIEditorHandler.DrawProperty(legendaryUnlockedObj, customSkin, "Legendary Unlocked");
            BeamUIEditorHandler.DrawProperty(legendaryTotalObj, customSkin, "Legendary Total");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            useLocalization.boolValue = BeamUIEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Bypasses localization functions when disabled.");
            useAlphabeticalOrder.boolValue = BeamUIEditorHandler.DrawToggle(useAlphabeticalOrder.boolValue, customSkin, "Use Alphabetical Order");

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif