using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO; // Include this for file reading
using TMPro;

public class CSVReaderSub : MonoBehaviour
{
    private TMP_Text tmpText; // TMP component

    [System.Serializable]
    public class GameManager
    {
        public string GameMode;
        public string SubGameMode;
    }

    public GameManager GameSettings;

    // Start is called before the first frame update
    void Start()
    {
        tmpText = GetComponent<TMP_Text>(); // Get the TMP component
        ReadCSV();
    }

    void ReadCSV()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "resources", "hardware_config.csv");
        if (File.Exists(filePath))
        {
            string[] data = File.ReadAllText(filePath).Split(new string[] { ",", "\n" }, StringSplitOptions.None);
            int tableSize = data.Length / 2 - 1;

            // Initialize GameSettings
            GameSettings = new GameManager();
            Debug.Log("data Length" + data.Length);
            // Make sure there is enough data
            if (data.Length >= 4)
            {
                GameSettings.GameMode = data[3];
                Debug.Log("GameMode: " + GameSettings.GameMode);
                GameSettings.SubGameMode = data[4];
                Debug.Log("SubGameMode: " + GameSettings.SubGameMode);
            }
        }
        else
        {
            Debug.LogError("CSV file not found at: " + filePath);
            // Handle the case where the file is not found
            // e.g., Load default values, show an error message, etc.
        }
    }
}