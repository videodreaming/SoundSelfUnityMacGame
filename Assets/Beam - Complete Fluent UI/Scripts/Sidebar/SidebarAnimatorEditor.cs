#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SidebarAnimator))]
    public class SidebarAnimatorEditor : Editor
    {
        private SidebarAnimator saTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            saTarget = (SidebarAnimator)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var targetRect = serializedObject.FindProperty("targetRect");
            var background = serializedObject.FindProperty("background");
            var boundTrigger = serializedObject.FindProperty("boundTrigger");
            var headers = serializedObject.FindProperty("headers");
            var individuals = serializedObject.FindProperty("individuals");

            var targetWidth = serializedObject.FindProperty("targetWidth");
            var curveSpeed = serializedObject.FindProperty("curveSpeed");
            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");
            var animationCurve = serializedObject.FindProperty("animationCurve");
            var startState = serializedObject.FindProperty("startState");
            var interactType = serializedObject.FindProperty("interactType");

            var onOpen = serializedObject.FindProperty("onOpen");
            var onClose = serializedObject.FindProperty("onClose");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            BeamUIEditorHandler.DrawProperty(targetRect, customSkin, "Target Rect");
            BeamUIEditorHandler.DrawProperty(background, customSkin, "Background");
            BeamUIEditorHandler.DrawProperty(boundTrigger, customSkin, "Bound Trigger");
            GUILayout.BeginVertical();
            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(headers, new GUIContent("Headers"), true);
            EditorGUILayout.PropertyField(individuals, new GUIContent("Individuals"), true);
            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 10);
            BeamUIEditorHandler.DrawProperty(targetWidth, customSkin, "Target Width");
            BeamUIEditorHandler.DrawProperty(curveSpeed, customSkin, "Curve Speed");
            BeamUIEditorHandler.DrawProperty(fadingMultiplier, customSkin, "Fading Multiplier");
            BeamUIEditorHandler.DrawProperty(animationCurve, customSkin, "Animation Curve");
            BeamUIEditorHandler.DrawProperty(startState, customSkin, "Start State");
            BeamUIEditorHandler.DrawProperty(interactType, customSkin, "Interact Type");

            if (Application.isPlaying == false && saTarget.targetRect != null)
            {
                if (saTarget.targetRect.sizeDelta.x != saTarget.targetWidth && GUILayout.Button("Open Sidebar", customSkin.button))
                {
                    saTarget.OpenInstant(false);
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }

                else if (saTarget.targetRect.sizeDelta.x != saTarget.defaultWidth && GUILayout.Button("Close Sidebar", customSkin.button))
                {
                    saTarget.CloseInstant(false);
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
            EditorGUILayout.PropertyField(onOpen, new GUIContent("On Open"), true);
            EditorGUILayout.PropertyField(onClose, new GUIContent("On Close"), true);

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif