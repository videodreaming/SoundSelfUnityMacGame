#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using static UnityEngine.GraphicsBuffer;
using UnityEditor.SceneManagement;

namespace Michsky.UI.Beam
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ServerBrowserItem))]
    public class ServerBrowserItemEditor : Editor
    {
        private ServerBrowserItem sbiTarget;
        private GUISkin customSkin;
        private int currentTab = 0;

        private void OnEnable()
        {
            sbiTarget = (ServerBrowserItem)target;

            if (EditorGUIUtility.isProSkin == true) { customSkin = BeamUIEditorHandler.GetDarkEditor(customSkin); }
            else { customSkin = BeamUIEditorHandler.GetLightEditor(customSkin); }
        }

        public override void OnInspectorGUI()
        {
            BeamUIEditorHandler.DrawComponentHeader(customSkin, "TopHeader_ServerItem");

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

            var serverName = serializedObject.FindProperty("serverName");
            var serverPing = serializedObject.FindProperty("serverPing");
            var badPingThreshold = serializedObject.FindProperty("badPingThreshold");
            var normalPingThreshold = serializedObject.FindProperty("normalPingThreshold");
            var goodPingThreshold = serializedObject.FindProperty("goodPingThreshold");
            var currentPlayers = serializedObject.FindProperty("currentPlayers");
            var maxPlayers = serializedObject.FindProperty("maxPlayers");
            var isFavorite = serializedObject.FindProperty("isFavorite");
            var isLocked = serializedObject.FindProperty("isLocked");

            var highlightCG = serializedObject.FindProperty("highlightCG");
            var objectRect = serializedObject.FindProperty("objectRect");
            var detailsParent = serializedObject.FindProperty("detailsParent");
            var connectButton = serializedObject.FindProperty("connectButton");
            var serverNameObj = serializedObject.FindProperty("serverNameObj");
            var playersObj = serializedObject.FindProperty("playersObj");
            var favoriteObj = serializedObject.FindProperty("favoriteObj");
            var notFavoriteObj = serializedObject.FindProperty("notFavoriteObj");
            var lockedObj = serializedObject.FindProperty("lockedObj");
            var notLockedObj = serializedObject.FindProperty("notLockedObj");
            var pingTextObj = serializedObject.FindProperty("pingTextObj");
            var pingIconObj = serializedObject.FindProperty("pingIconObj");
            var badPingIcon = serializedObject.FindProperty("badPingIcon");
            var normalPingIcon = serializedObject.FindProperty("normalPingIcon");
            var goodPingIcon = serializedObject.FindProperty("goodPingIcon");

            var isInteractable = serializedObject.FindProperty("isInteractable");
            var bypassConnectLimitations = serializedObject.FindProperty("bypassConnectLimitations");
            var useSounds = serializedObject.FindProperty("useSounds");
            var useUINavigation = serializedObject.FindProperty("useUINavigation");
            var navigationMode = serializedObject.FindProperty("navigationMode");
            var selectOnUp = serializedObject.FindProperty("selectOnUp");
            var selectOnDown = serializedObject.FindProperty("selectOnDown");
            var selectOnLeft = serializedObject.FindProperty("selectOnLeft");
            var selectOnRight = serializedObject.FindProperty("selectOnRight");
            var wrapAround = serializedObject.FindProperty("wrapAround");
            var fadingMultiplier = serializedObject.FindProperty("fadingMultiplier");

            var animationCurve = serializedObject.FindProperty("animationCurve");
            var curveSpeed = serializedObject.FindProperty("curveSpeed");
            var normalHeight = serializedObject.FindProperty("normalHeight");
            var expandedHeight = serializedObject.FindProperty("expandedHeight");

            var onClick = serializedObject.FindProperty("onClick");
            var onConnect = serializedObject.FindProperty("onConnect");

            switch (currentTab)
            {
                case 0:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Content", 6);
                    BeamUIEditorHandler.DrawPropertyCW(serverName, customSkin, "Server Name", 100);
                    BeamUIEditorHandler.DrawPropertyCW(serverPing, customSkin, "Server Ping", 100);
                    BeamUIEditorHandler.DrawPropertyCW(currentPlayers, customSkin, "Current Players", 100);
                    BeamUIEditorHandler.DrawPropertyCW(maxPlayers, customSkin, "Max Players", 100);
                    isFavorite.boolValue = BeamUIEditorHandler.DrawToggle(isFavorite.boolValue, customSkin, "Is Favorite");
                    isLocked.boolValue = BeamUIEditorHandler.DrawToggle(isLocked.boolValue, customSkin, "Is Locked");

                    if (Application.isPlaying == false && sbiTarget.objectRect != null)
                    {
                        if (sbiTarget.objectRect.sizeDelta.y != expandedHeight.floatValue && GUILayout.Button("Expand", customSkin.button))
                        {
                            sbiTarget.objectRect.sizeDelta = new Vector2(sbiTarget.objectRect.sizeDelta.x, expandedHeight.floatValue);
                        }

                        else if (sbiTarget.objectRect.sizeDelta.y != normalHeight.floatValue && GUILayout.Button("Minimize", customSkin.button))
                        {
                            sbiTarget.objectRect.sizeDelta = new Vector2(sbiTarget.objectRect.sizeDelta.x, normalHeight.floatValue);
                        }
                    }

                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Events", 10);
                    EditorGUILayout.PropertyField(onClick, new GUIContent("On Click"), true);
                    EditorGUILayout.PropertyField(onConnect, new GUIContent("On Connect"), true);
                    break;

                case 1:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Resources", 6);
                    BeamUIEditorHandler.DrawProperty(highlightCG, customSkin, "Highlight CG");
                    BeamUIEditorHandler.DrawProperty(objectRect, customSkin, "Object Rect");
                    BeamUIEditorHandler.DrawProperty(detailsParent, customSkin, "Details Parent");
                    BeamUIEditorHandler.DrawProperty(connectButton, customSkin, "Connect Button");
                    BeamUIEditorHandler.DrawProperty(serverNameObj, customSkin, "Server Name Text");
                    BeamUIEditorHandler.DrawProperty(playersObj, customSkin, "Players Text");
                    BeamUIEditorHandler.DrawProperty(favoriteObj, customSkin, "Favorite Obj");
                    BeamUIEditorHandler.DrawProperty(notFavoriteObj, customSkin, "Not Favorite Obj");
                    BeamUIEditorHandler.DrawProperty(lockedObj, customSkin, "Locked Obj");
                    BeamUIEditorHandler.DrawProperty(notLockedObj, customSkin, "Not Locked Obj");
                    BeamUIEditorHandler.DrawProperty(pingTextObj, customSkin, "Ping Text");
                    BeamUIEditorHandler.DrawProperty(pingIconObj, customSkin, "Ping Image");
                    break;

                case 2:
                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Settings", 6);
                    isInteractable.boolValue = BeamUIEditorHandler.DrawToggle(isInteractable.boolValue, customSkin, "Is Interactable");
                    bypassConnectLimitations.boolValue = BeamUIEditorHandler.DrawToggle(bypassConnectLimitations.boolValue, customSkin, "Bypass Connect Limitations");
                    useSounds.boolValue = BeamUIEditorHandler.DrawToggle(useSounds.boolValue, customSkin, "Use Sounds");

                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Space(-3);

                    useUINavigation.boolValue = BeamUIEditorHandler.DrawTogglePlain(useUINavigation.boolValue, customSkin, "Use UI Navigation", "Enables controller navigation.");

                    GUILayout.Space(4);

                    if (useUINavigation.boolValue == true)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        BeamUIEditorHandler.DrawPropertyPlain(navigationMode, customSkin, "Navigation Mode");

                        if (sbiTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Horizontal)
                        {
                            EditorGUI.indentLevel = 1;
                            wrapAround.boolValue = BeamUIEditorHandler.DrawToggle(wrapAround.boolValue, customSkin, "Wrap Around");
                            EditorGUI.indentLevel = 0;
                        }

                        else if (sbiTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Vertical)
                        {
                            wrapAround.boolValue = BeamUIEditorHandler.DrawTogglePlain(wrapAround.boolValue, customSkin, "Wrap Around");
                        }

                        else if (sbiTarget.navigationMode == UnityEngine.UI.Navigation.Mode.Explicit)
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
                    BeamUIEditorHandler.DrawProperty(badPingThreshold, customSkin, "Bad Ping Thrsh.");
                    BeamUIEditorHandler.DrawProperty(normalPingThreshold, customSkin, "Normal Ping Thrsh.");
                    BeamUIEditorHandler.DrawProperty(goodPingThreshold, customSkin, "Good Ping Thrsh.");
                    BeamUIEditorHandler.DrawProperty(badPingIcon, customSkin, "Bad Ping Icon");
                    BeamUIEditorHandler.DrawProperty(normalPingIcon, customSkin, "Normal Ping Icon");
                    BeamUIEditorHandler.DrawProperty(goodPingIcon, customSkin, "Good Ping Icon");

                    BeamUIEditorHandler.DrawHeader(customSkin, "Header_Animation", 10);
                    BeamUIEditorHandler.DrawProperty(animationCurve, customSkin, "Animation Curve");
                    BeamUIEditorHandler.DrawProperty(curveSpeed, customSkin, "Curve Speed");
                    BeamUIEditorHandler.DrawProperty(normalHeight, customSkin, "Normal Height");
                    BeamUIEditorHandler.DrawProperty(expandedHeight, customSkin, "Expanded Height");
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            if (Application.isPlaying == false) { Repaint(); }
        }
    }
}
#endif