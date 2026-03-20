#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SidebarReveal))]
    public class SidebarRevealEditor : Editor
    {
        private SidebarReveal srTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            srTarget = (SidebarReveal)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var animator = serializedObject.FindProperty("animator");
            var canvasGroup = serializedObject.FindProperty("canvasGroup");

            var updateMode = serializedObject.FindProperty("updateMode");
            var barDirection = serializedObject.FindProperty("barDirection");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            BeamUIEditorHandler.DrawProperty(animator, customSkin, "Animator");
            BeamUIEditorHandler.DrawProperty(canvasGroup, customSkin, "Canvas Group");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            BeamUIEditorHandler.DrawProperty(updateMode, customSkin, "Update Mode");
            BeamUIEditorHandler.DrawProperty(barDirection, customSkin, "Bar Direction");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif