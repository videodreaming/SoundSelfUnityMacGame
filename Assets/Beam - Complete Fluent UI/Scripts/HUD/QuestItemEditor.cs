#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(QuestItem))]
    public class QuestItemEditor : Editor
    {
        private QuestItem qiTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            qiTarget = (QuestItem)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var questText = serializedObject.FindProperty("questText");
            var localizationKey = serializedObject.FindProperty("localizationKey");

            var questAnimator = serializedObject.FindProperty("questAnimator");
            var questTextObj = serializedObject.FindProperty("questTextObj");

            var useLocalization = serializedObject.FindProperty("useLocalization");
            var updateOnAnimate = serializedObject.FindProperty("updateOnAnimate");
            var minimizeAfter = serializedObject.FindProperty("minimizeAfter");
            var defaultState = serializedObject.FindProperty("defaultState");
            var afterMinimize = serializedObject.FindProperty("afterMinimize");

            var onDestroy = serializedObject.FindProperty("onDestroy");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent("Quest Text"), customSkin.FindStyle("Text"), GUILayout.Width(-3));
            EditorGUILayout.PropertyField(questText, new GUIContent(""), GUILayout.Height(70));
            GUILayout.EndHorizontal();
            BeamUIEditorHandler.DrawProperty(localizationKey, customSkin, "Localization Key");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 10);
            BeamUIEditorHandler.DrawProperty(questAnimator, customSkin, "Quest Animator");
            BeamUIEditorHandler.DrawProperty(questTextObj, customSkin, "Quest Text Object");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            useLocalization.boolValue = BeamUIEditorHandler.DrawToggle(useLocalization.boolValue, customSkin, "Use Localization", "Bypasses localization functions when disabled.");
            updateOnAnimate.boolValue = BeamUIEditorHandler.DrawToggle(updateOnAnimate.boolValue, customSkin, "Update On Animate");
            BeamUIEditorHandler.DrawProperty(minimizeAfter, customSkin, "Minimize After");
            BeamUIEditorHandler.DrawProperty(defaultState, customSkin, "Default State");
            BeamUIEditorHandler.DrawProperty(afterMinimize, customSkin, "After Minimize");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
            EditorGUILayout.PropertyField(onDestroy, new GUIContent("On Destroy"), true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif