using UnityEngine;

[System.Serializable]
public class Config
{
    public string EncryptionKey;
}

public static class ConfigLoader
{
    public static Config LoadConfig()
    {
        TextAsset configText = Resources.Load<TextAsset>("config");
        if (configText == null)
        {
            Debug.LogError("Config file not found!");
            return null;
        }
        Config config = JsonUtility.FromJson<Config>(configText.text);
        return config;
    }
}