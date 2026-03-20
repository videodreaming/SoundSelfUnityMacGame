#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SettingsElement))]
    public class SettingsElementEditor : Editor
    {
        private SettingsElement seTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            seTarget = (SettingsElement)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var highlightCG = serializedObject.FindProperty("highlightCG");

            var isInteractable = serializedObject.FindProperty("isInteractable");
            var useSounds = serializedObject.FindProperty("useSounds");
            var useUINavigation = serializedObject.FindProperty("useUINavigation");
            var navigationMode = serializedObject.FindProperty("navigationMode");
            var selectOnUp = serializedObject.FindProperty("selectOnUp");
            var selectOnDown = serializedObject.FindProperty("selectOnDown");
            var selectOnLeft = serializedObject.FindProperty("selectOnLeft");
            var selectOnRight = serializedObject.FindProperty("selectOnRight");
            var wrapAround = serializedObject.FindProperty("wrapAround");
            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");

            var onClick = serializedObject.FindProperty("onClick");
            var onHover = serializedObject.FindProperty("onHover");
            var onLeave = serializedObject.FindProperty("onLeave");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            BeamUIEditorHandler.DrawProperty(highlightCG, customSkin, "Highlight CG");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            isInteractable.boolValue = BeamUIEditorHandler.DrawToggle(isInteractable.boolValue, customSkin, "Is Interactable");
            useSounds.boolValue = BeamUIEditorHandler.DrawToggle(useSounds.boolValue, customSkin, "Use Sounds");

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(-3);

            useUINavigation.boolValue = BeamUIEditorHandler.DrawTogglePlain(useUINavigation.boolValue, customSkin, "Use UI Navigation", "Enables controller navigation.");

            GUILayout.Space(4);

            if (useUINavigation.boolValue == true)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                BeamUIEditorHandler.DrawPropertyPlain(navigationMode, customSkin, "Navigation Mode");

                if (seTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Horizontal)
                {
                    EditorGUI.indentLevel = 1;
                    wrapAround.boolValue = BeamUIEditorHandler.DrawToggle(wrapAround.boolValue, customSkin, "Wrap Around");
                    EditorGUI.indentLevel = 0;
                }

                else if (seTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Vertical)
                {
                    wrapAround.boolValue = BeamUIEditorHandler.DrawTogglePlain(wrapAround.boolValue, customSkin, "Wrap Around");
                }

                else if (seTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Explicit)
                {
                    EditorGUI.indentLevel = 1;
                    BeamUIEditorHandler.DrawPropertyPlain(selectOnUp, customSkin, "Select On Up");
                    BeamUIEditorHandler.DrawPropertyPlain(selectOnDown, customSkin, "Select On Down");
                    BeamUIEditorHandler.DrawPropertyPlain(selectOnLeft, customSkin, "Select On Left");
                    BeamUIEditorHandler.DrawPropertyPlain(selectOnRight, customSkin, "Select On Right");
                    EditorGUI.indentLevel = 0;
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
            BeamUIEditorHandler.DrawProperty(fadingMultiplier, customSkin, "Fading Multiplier", "Set the animation fade multiplier.");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
            EditorGUILayout.PropertyField(onClick, new GUIContent("On Click"), true);
            EditorGUILayout.PropertyField(onHover, new GUIContent("On Hover"), true);
            EditorGUILayout.PropertyField(onLeave, new GUIContent("On Leave"), true);

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif