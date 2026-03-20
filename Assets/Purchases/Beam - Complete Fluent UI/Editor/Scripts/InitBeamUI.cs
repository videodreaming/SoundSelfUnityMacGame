#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.UI.Beam
{
    public class InitBeamUI
    {
        [InitializeOnLoad]
        public class InitOnLoad
        {
            static InitOnLoad()
            {
                if (!EditorPrefs.HasKey("BeamUI.HasCustomEditorData"))
                {
                    string darkPath = AssetDatabase.GetAssetPath(Resources.Load("BeamUIEditor-Dark"));
                    string lightPath = AssetDatabase.GetAssetPath(Resources.Load("BeamUIEditor-Light"));

                    EditorPrefs.SetString("BeamUI.CustomEditorDark", darkPath);
                    EditorPrefs.SetString("BeamUI.CustomEditorLight", lightPath);
                    EditorPrefs.SetInt("BeamUI.HasCustomEditorData", 1);
                }
            }
        }
    }
}
#endif