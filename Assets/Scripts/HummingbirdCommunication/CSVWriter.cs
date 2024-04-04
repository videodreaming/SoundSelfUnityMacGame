using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class CSVWriter : MonoBehaviour
{
    string sessionsPath = "";
    string hardware_configPath = "";
    string session_paramsPath = "";
    string session_resultsPath = "";
    string session_statusPath = "";
    string wavFilesPath = "";
     int currentSessionNumber = 0;
    public UserOutput playerOutput;

    [System.Serializable]
    public class PlayerData
    {
        public int respirationRate;
        public int averageVolume;
        public int averagePitch;
    }

    // Start is called before the first frame update
    void Start()
    {
        string baseSessionsFolderPath = Path.Combine(Application.streamingAssetsPath, "Resources");
        Directory.CreateDirectory(baseSessionsFolderPath); // Ensure base path exists

        string[] existingSessionDirectories = Directory.GetDirectories(baseSessionsFolderPath)
            .Select(Path.GetFileName)
            .Where(dir => dir.StartsWith("session"))
            .ToArray();

        List<int> sessionNumbers = new List<int>();
        foreach (string sessionDir in existingSessionDirectories)
        {
            // Extract the part of the directory name that should be a number
            string numberPart = sessionDir.Replace("session", "");
            if (int.TryParse(numberPart, out int sessionNumber))
            {
                sessionNumbers.Add(sessionNumber);
            }
        }

        if (sessionNumbers.Count > 0)
        {
            currentSessionNumber = sessionNumbers.Max() + 1;
        }

        string sessionsFolder = Path.Combine(baseSessionsFolderPath, $"session{currentSessionNumber}");
        Directory.CreateDirectory(sessionsFolder);

        sessionsPath = Path.Combine(baseSessionsFolderPath, "sessions.csv"); // Assuming this is intended to be at base level
        hardware_configPath = Path.Combine(baseSessionsFolderPath, "hardware_config.csv"); // Assuming base level too
        session_paramsPath = Path.Combine(sessionsFolder, "session_params.csv");
        session_resultsPath = Path.Combine(sessionsFolder, "session_results.csv");
        session_statusPath = Path.Combine(sessionsFolder, "session_status.csv");
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            writeCSV();
            Debug.Log(Application.streamingAssetsPath);
        }
    }

    public void writeCSV()
    {
        TextWriter tw = new StreamWriter(sessionsPath, false);
        tw.Close();

        if (!File.Exists(hardware_configPath))
        {
            TextWriter tw1 = new StreamWriter(hardware_configPath, false);
            tw1.Close();
        }



        TextWriter tw2 = new StreamWriter(session_paramsPath, false);
        tw2.Close();

        TextWriter tw3 = new StreamWriter(session_resultsPath, false);
        tw3.Close();

        TextWriter tw4 = new StreamWriter(session_statusPath, false);
        tw4.Close();

        tw = new StreamWriter(session_resultsPath, true);
        tw.WriteLine(playerOutput.respirationRate + "," + playerOutput.averageVolume + "," + playerOutput.averagePitch);
        tw.Close();
    }
}
