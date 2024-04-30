using System.IO;
using UnityEngine;

public class CSVLoader : MonoBehaviour
{
    private string filePath;

    void Start()
    {
        // Set the file path to the persistent data directory
        filePath = Path.Combine(Application.persistentDataPath, "readCSV.csv");
        // Check if the file exists in persistent data path
        if (!File.Exists(filePath))
        {
            // If not, load the default file from Resources and copy it
            TextAsset defaultCSV = Resources.Load<TextAsset>("ReadCSV");
            File.WriteAllText(filePath, defaultCSV.text);
        }

        // Load and process the CSV file
        LoadCSV();
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.D))
        {
            ReloadCSV();
        }
    }


    void LoadCSV()
    {
        if (File.Exists(filePath))
        {
            string csvData = File.ReadAllText(filePath);
            // Process the CSV data as needed
        }
        else
        {
            Debug.LogError("Cannot find readCSV.csv at: " + filePath);
        }
    }

    // Optionally, create a method to reload the CSV data if it changes while the application is running
    public void ReloadCSV()
    {
        LoadCSV();
    }
}