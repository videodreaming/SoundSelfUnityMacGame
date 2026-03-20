#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Michsky.UI.Beam
{
    public class ToolsMenu : Editor
    {
        static string objectPath;

        static void GetObjectPath()
        {
            objectPath = AssetDatabase.GetAssetPath(Resources.Load("Beam UI Manager"));
            objectPath = objectPath.Replace("Resources/Beam UI Manager.asset", "").Trim();
            objectPath = objectPath + "Prefabs/";
        }

        static void MakeSceneDirty(GameObject source, string sourceName)
        {
            if (Application.isPlaying == false)
            {
                Undo.RegisterCreatedObjectUndo(source, sourceName);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        static void ShowErrorDialog()
        {
            EditorUtility.DisplayDialog("Beam UI", "Cannot create the object due to missing manager file. " +
                    "Make sure you have 'Beam UI Manager' file in Beam UI > Resources folder.", "Dismiss");
        }

        static void UpdateCustomEditorPath()
        {
            string darkPath = AssetDatabase.GetAssetPath(Resources.Load("BeamUIEditor-Dark"));
            string lightPath = AssetDatabase.GetAssetPath(Resources.Load("BeamUIEditor-Light"));

            EditorPrefs.SetString("BeamUI.CustomEditorDark", darkPath);
            EditorPrefs.SetString("BeamUI.CustomEditorLight", lightPath);
        }

        [MenuItem("Tools/Beam UI/Show UI Manager %#M")]
        static void ShowManager()
        {
            Selection.activeObject = Resources.Load("Beam UI Manager");

            if (Selection.activeObject == null)
                Debug.Log("<b>[Beam UI]</b>Can't find an asset called 'Beam UI Manager'. Make sure you have 'Beam UI Manager' in: Beam UI > Editor > Resources");
        }

        static void CreateObject(string resourcePath)
        {
            try
            {
                GetObjectPath();
                UpdateCustomEditorPath();
                GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + resourcePath + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;

                try
                {
                    if (Selection.activeGameObject == null)
                    {

#if UNITY_2023_2_OR_NEWER
                        var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
#else
                        var canvas = (Canvas)FindObjectsOfType(typeof(Canvas))[0];
#endif
                        clone.transform.SetParent(canvas.transform, false);
                    }

                    else { clone.transform.SetParent(Selection.activeGameObject.transform, false); }

                    clone.name = clone.name.Replace("(Clone)", "").Trim();
                    MakeSceneDirty(clone, clone.name);
                }

                catch
                {
                    CreateCanvas();
#if UNITY_2023_2_OR_NEWER
                    var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)[0];
#else
                    var canvas = (Canvas)FindObjectsOfType(typeof(Canvas))[0];
#endif
                    clone.transform.SetParent(canvas.transform, false);
                    clone.name = clone.name.Replace("(Clone)", "").Trim();
                    MakeSceneDirty(clone, clone.name);
                }

                Selection.activeObject = clone;
            }

            catch { ShowErrorDialog(); }
        }

        [MenuItem("GameObject/Beam UI/Canvas", false, 8)]
        static void CreateCanvas()
        {
            try
            {
                GetObjectPath();
                UpdateCustomEditorPath();
                GameObject clone = Instantiate(AssetDatabase.LoadAssetAtPath(objectPath + "UI Elements/Canvas/Canvas" + ".prefab", typeof(GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
                clone.name = clone.name.Replace("(Clone)", "").Trim();
                Selection.activeObject = clone;
                MakeSceneDirty(clone, clone.name);
            }

            catch { ShowErrorDialog(); }
        }

        [MenuItem("GameObject/Beam UI/Button/Button", false, 8)]
        static void CreateButtonMain() { CreateObject("UI Elements/Button/Button"); }

        [MenuItem("GameObject/Beam UI/Button/Button (Icon Only)", false, 8)]
        static void CreateButtonIconOnly() { CreateObject("UI Elements/Button/Button (Icon Only)"); }

        [MenuItem("GameObject/Beam UI/Button/Button (Panel)", false, 8)]
        static void CreateButtonPanel() { CreateObject("UI Elements/Button/Button (Panel)"); }

        [MenuItem("GameObject/Beam UI/Button/Button (Panel Alt)", false, 8)]
        static void CreateButtonPanelAlt() { CreateObject("UI Elements/Button/Button (Panel Alt)"); }

        [MenuItem("GameObject/Beam UI/Button/Button (Shop)", false, 8)]
        static void CreateButtonShop() { CreateObject("UI Elements/Button/Button (Shop)"); }

        [MenuItem("GameObject/Beam UI/Button/Button (Spot)", false, 8)]
        static void CreateButtonSpot() { CreateObject("UI Elements/Button/Button (Spot)"); }

        [MenuItem("GameObject/Beam UI/Dropdown/Standard", false, 8)]
        static void CreateDropdown() { CreateObject("UI Elements/Dropdown/Dropdown"); }

        [MenuItem("GameObject/Beam UI/HUD/Feed Notification", false, 8)]
        static void CreateHudFN() { CreateObject("HUD/Feed Notification"); }

        [MenuItem("GameObject/Beam UI/HUD/Health Bar", false, 8)]
        static void CreateHudHealthBar() { CreateObject("HUD/Health Bar"); }

        [MenuItem("GameObject/Beam UI/HUD/Minimap", false, 8)]
        static void CreateHudMinimap() { CreateObject("HUD/Minimap"); }

        [MenuItem("GameObject/Beam UI/HUD/Quest Item", false, 8)]
        static void CreateHudQuestItem() { CreateObject("HUD/Quest Item"); }

        [MenuItem("GameObject/Beam UI/Input/Hotkey Indicator", false, 8)]
        static void CreateHotkeyIndicator() { CreateObject("UI Elements/Input/Hotkey Indicator"); }

        [MenuItem("GameObject/Beam UI/Input Field/Standard", false, 8)]
        static void CreateInputField() { CreateObject("UI Elements/Input Field/Input Field"); }

        [MenuItem("GameObject/Beam UI/Misc/Server Browser Item", false, 8)]
        static void CreateServerBrowserItem() { CreateObject("UI Elements/Misc/Server Browser Item"); }

        [MenuItem("GameObject/Beam UI/Misc/Server Browser Filter (Switch)", false, 8)]
        static void CreateServerBrowserFilterSwitch() { CreateObject("UI Elements/Misc/Server Browser Filter (Switch)"); }

        [MenuItem("GameObject/Beam UI/Modal Window/Standard", false, 8)]
        static void CreateModalWindow() { CreateObject("UI Elements/Modal Window/Modal Window"); }

        [MenuItem("GameObject/Beam UI/Modal Window/Custom Content", false, 8)]
        static void CreateModalWindowCC() { CreateObject("UI Elements/Modal Window/Modal Window (Custom Content)"); }

        [MenuItem("GameObject/Beam UI/Notification/Standard Notification", false, 8)]
        static void CreateNotification() { CreateObject("UI Elements/Notification/Notification"); }

        [MenuItem("GameObject/Beam UI/Notification/Feed Notification", false, 8)]
        static void CreateFeedNotification() { CreateObject("HUD/Feed Notification"); }

        [MenuItem("GameObject/Beam UI/Panels/Credits", false, 8)]
        static void CreateCredits() { CreateObject("Panels/Credits"); }

        [MenuItem("GameObject/Beam UI/Panels/Panel Manager", false, 8)]
        static void CreatePanelManager() { CreateObject("Panels/Panel Manager"); }

        [MenuItem("GameObject/Beam UI/Progress Bar/Standard", false, 8)]
        static void CreateProgressBar() { CreateObject("UI Elements/Progress Bar/Progress Bar"); }

        [MenuItem("GameObject/Beam UI/Scrollbar/Horizontal", false, 8)]
        static void CreateScrollbarHorizontal() { CreateObject("UI Elements/Scrollbar/Scrollbar Horizontal"); }

        [MenuItem("GameObject/Beam UI/Scrollbar/Vertical", false, 8)]
        static void CreateScrollbarVertical() { CreateObject("UI Elements/Scrollbar/Scrollbar Vertical"); }

        [MenuItem("GameObject/Beam UI/Selectors/Horizontal Selector", false, 8)]
        static void CreateHorizontalSelector() { CreateObject("UI Elements/Selectors/Horizontal Selector"); }

        [MenuItem("GameObject/Beam UI/Selectors/Mode Selector", false, 8)]
        static void CreateModeSelector() { CreateObject("UI Elements/Selectors/Mode Selector"); }

        [MenuItem("GameObject/Beam UI/Settings/Settings Element (Dropdown)", false, 8)]
        static void CreateSettingsDropdownt() { CreateObject("UI Elements/Settings/Settings Element (Dropdown Alt)"); }

        [MenuItem("GameObject/Beam UI/Settings/Settings Element (Horizontal Selector)", false, 8)]
        static void CreateSettingsHS() { CreateObject("UI Elements/Settings/Settings Element (Horizontal Selector)"); }

        [MenuItem("GameObject/Beam UI/Settings/Settings Element (Slider)", false, 8)]
        static void CreateSettingsSlider() { CreateObject("UI Elements/Settings/Settings Element (Slider)"); }

        [MenuItem("GameObject/Beam UI/Settings/Settings Element (Switch)", false, 8)]
        static void CreateSettingsSwitch() { CreateObject("UI Elements/Settings/Settings Element (Switch)"); }

        [MenuItem("GameObject/Beam UI/Sidebar/Animated Sidebar", false, 8)]
        static void CreateAnimatedSidebar() { CreateObject("UI Elements/Sidebar/Animated Sidebar"); }

        [MenuItem("GameObject/Beam UI/Sidebar/Sidebar Header", false, 8)]
        static void CreateSidebarHeader() { CreateObject("UI Elements/Sidebar/Sidebar Header"); }

        [MenuItem("GameObject/Beam UI/Sidebar/Sidebar Individual", false, 8)]
        static void CreateSidebarIndividual() { CreateObject("UI Elements/Sidebar/Sidebar Individual"); }

        [MenuItem("GameObject/Beam UI/Slider/Standard", false, 8)]
        static void CreateSlider() { CreateObject("UI Elements/Slider/Slider"); }

        [MenuItem("GameObject/Beam UI/Spinners/Basic Spinner", false, 8)]
        static void CreateSpinner() { CreateObject("UI Elements/Spinners/Basic Spinner"); }

        [MenuItem("GameObject/Beam UI/Spinners/Fluid Line", false, 8)]
        static void CreateFluidLine() { CreateObject("UI Elements/Spinners/Fluid Line"); }

        [MenuItem("GameObject/Beam UI/Switch/Standard", false, 8)]
        static void CreateSwitch() { CreateObject("UI Elements/Switch/Switch"); }

        [MenuItem("GameObject/Beam UI/Text/Text (TMP)", false, 8)]
        static void CreateText() { CreateObject("UI Elements/Text/Text (TMP)"); }

        [MenuItem("GameObject/Beam UI/Timer/Timer Bar", false, 8)]
        static void CreateTimerBar() { CreateObject("UI Elements/Timer/Timer Bar"); }

        [MenuItem("GameObject/Beam UI/Widgets/News Slider", false, 8)]
        static void CreateNewsSlider() { CreateObject("UI Elements/Widgets/News Slider/News Slider"); }

        [MenuItem("GameObject/Beam UI/Widgets/Socials", false, 8)]
        static void CreateSocialsWidget() { CreateObject("UI Elements/Widgets/Socials/Socials Widget"); }
    }
}
#endif