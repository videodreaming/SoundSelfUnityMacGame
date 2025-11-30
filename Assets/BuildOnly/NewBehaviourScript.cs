#if !UNITY_EDITOR && UNITY_STANDALONE_OSX
using System.IO;
using UnityEngine;

public class PluginPathProbe : MonoBehaviour
{
    void Start()
    {
        // In a macOS .app, the executable is at .../Contents/MacOS/<exe>
        // Plugins go in .../Contents/Plugins/
        var exeDir = System.IO.Path.GetDirectoryName(Application.dataPath); // .../Contents
        var pluginPath = Path.Combine(exeDir, "Plugins/imitone.dylib");
        Debug.Log($"[Probe] Looking for {pluginPath}  Exists? {File.Exists(pluginPath)}");

        // (Optional) list all files in Plugins
        if (Directory.Exists(Path.Combine(exeDir, "Plugins")))
        {
            foreach (var f in Directory.GetFiles(Path.Combine(exeDir, "Plugins")))
                Debug.Log("[Probe] Plugins file: " + f);
        }
    }
}
#endif
