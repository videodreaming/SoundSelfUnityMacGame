#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UIManagerAudio))]
    public class UIManagerAudioEditor : Editor
    {
        private UIManagerAudio uimaTarget;
        private GUISkin customSkin;

        private void OnEnable()
        {
            uimaTarget = (UIManagerAudio)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            var UIManagerAsset = serializedObject.FindProperty("UIManagerAsset");
            var audioMixer = serializedObject.FindProperty("audioMixer");
            var audioSource = serializedObject.FindProperty("audioSource");
            var masterSlider = serializedObject.FindProperty("masterSlider");
            var musicSlider = serializedObject.FindProperty("musicSlider");
            var SFXSlider = serializedObject.FindProperty("SFXSlider");
            var UISlider = serializedObject.FindProperty("UISlider");

            BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
            BeamUIEditorHandler.DrawProperty(UIManagerAsset, customSkin, "UI Manager");
            BeamUIEditorHandler.DrawProperty(audioMixer, customSkin, "Audio Mixer");
            BeamUIEditorHandler.DrawProperty(audioSource, customSkin, "Audio Source");
            BeamUIEditorHandler.DrawProperty(masterSlider, customSkin, "Master Slider");
            BeamUIEditorHandler.DrawProperty(musicSlider, customSkin, "Music Slider");
            BeamUIEditorHandler.DrawProperty(SFXSlider, customSkin, "SFX Slider");
            BeamUIEditorHandler.DrawProperty(UISlider, customSkin, "UI Slider");

            if (Application.isPlaying == true)
                return;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif