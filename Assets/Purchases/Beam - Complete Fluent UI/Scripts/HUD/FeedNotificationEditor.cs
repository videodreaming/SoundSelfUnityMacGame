#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FeedNotification))]
    public class FeedNotificationEditor : Editor
    {
        private FeedNotification fnTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            fnTarget = (FeedNotification)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var icon = serializedObject.FindProperty("icon");
            var notificationText = serializedObject.FindProperty("notificationText");
            var localizationKey = serializedObject.FindProperty("localizationKey");

            var itemAnimator = serializedObject.FindProperty("itemAnimator");
            var iconObj = serializedObject.FindProperty("iconObj");
            var textObj = serializedObject.FindProperty("textObj");

            var useLocalization = serializedObject.FindProperty("useLocalization");
            var updateOnAnimate = serializedObject.FindProperty("updateOnAnimate");
            var minimizeAfter = serializedObject.FindProperty("minimizeAfter");
            var defaultState = serializedObject.FindProperty("defaultState");
            var afterMinimize = serializedObject.FindProperty("afterMinimize");

            var onDestroy = serializedObject.FindProperty("onDestroy");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
            BeamUIEditorHandler.DrawProperty(icon, customSkin, "Icon");
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent("Notification Text"), customSkin.FindStyle("Text"), GUILayout.Width(-3));
            EditorGUILayout.PropertyField(notificationText, new GUIContent(""), GUILayout.Height(70));
            GUILayout.EndHorizontal();
            BeamUIEditorHandler.DrawProperty(localizationKey, customSkin, "Localization Key");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 10);
            BeamUIEditorHandler.DrawProperty(itemAnimator, customSkin, "Animator");
            BeamUIEditorHandler.DrawProperty(iconObj, customSkin, "Icon Object");
            BeamUIEditorHandler.DrawProperty(textObj, customSkin, "Text Object");

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