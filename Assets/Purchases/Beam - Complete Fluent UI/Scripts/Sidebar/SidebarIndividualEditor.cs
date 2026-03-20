#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SidebarIndividual))]
    public class SidebarIndividualEditor : Editor
    {
        private SidebarIndividual siTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            siTarget = (SidebarIndividual)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var contextMenu = serializedObject.FindProperty("contextMenu");
            var profileImage = serializedObject.FindProperty("profileImage");
            var profileText = serializedObject.FindProperty("profileText");
            var contentGroup = serializedObject.FindProperty("contentGroup");
            var highlightCG = serializedObject.FindProperty("highlightCG");
            var onlineIndicators = serializedObject.FindProperty("onlineIndicators");
            var awayIndicators = serializedObject.FindProperty("awayIndicators");
            var offlineIndicators = serializedObject.FindProperty("offlineIndicators");
            var customIndicators = serializedObject.FindProperty("customIndicators");

            var setStateOnEnable = serializedObject.FindProperty("setStateOnEnable");
            var isInteractable = serializedObject.FindProperty("isInteractable");
            var useSounds = serializedObject.FindProperty("useSounds");
            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");
            var individualState = serializedObject.FindProperty("individualState");

            var onClick = serializedObject.FindProperty("onClick");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            BeamUIEditorHandler.DrawProperty(contextMenu, customSkin, "Context Menu");
            BeamUIEditorHandler.DrawProperty(profileImage, customSkin, "Profile Image");
            BeamUIEditorHandler.DrawProperty(profileText, customSkin, "Profile Text");
            BeamUIEditorHandler.DrawProperty(contentGroup, customSkin, "Content Group");
            BeamUIEditorHandler.DrawProperty(highlightCG, customSkin, "Highlight CG");
            GUILayout.BeginVertical();
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(onlineIndicators, new GUIContent("Online Indicators"), true);
            EditorGUILayout.PropertyField(awayIndicators, new GUIContent("Away Indicators"), true);
            EditorGUILayout.PropertyField(offlineIndicators, new GUIContent("Offline Indicators"), true);
            EditorGUILayout.PropertyField(customIndicators, new GUIContent("Custom Indicators"), true);
            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            setStateOnEnable.boolValue = BeamUIEditorHandler.DrawToggle(setStateOnEnable.boolValue, customSkin, "Set State On Enable");
            isInteractable.boolValue = BeamUIEditorHandler.DrawToggle(isInteractable.boolValue, customSkin, "Is Interactable");
            useSounds.boolValue = BeamUIEditorHandler.DrawToggle(useSounds.boolValue, customSkin, "Use Sounds");
            BeamUIEditorHandler.DrawProperty(fadingMultiplier, customSkin, "Fading Multiplier", "Set the animation fade multiplier.");
            BeamUIEditorHandler.DrawProperty(individualState, customSkin, "Individual State");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
            EditorGUILayout.PropertyField(onClick, new GUIContent("On Click"), true);

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif