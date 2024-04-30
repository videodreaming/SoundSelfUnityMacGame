using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class CSVWriter : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
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

        sessionsPath = Path.Combine(baseSessionsFolderPath, "sessions.csv");
        hardware_configPath = Path.Combine(baseSessionsFolderPath, "hardware_config.csv");
        session_paramsPath = Path.Combine(sessionsFolder, "session_params.csv");
        session_resultsPath = Path.Combine(sessionsFolder, "session_results.csv");
        session_statusPath = Path.Combine(sessionsFolder, "session_status.csv");

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
    }

    // Update is called once per frame
    void Update()
    {
        writeCSV();
    }

    public void writeCSV()
    {
        // Ensure the session results file is created if it does not exist
        if (!File.Exists(session_resultsPath))
        {
            using (TextWriter tw = new StreamWriter(session_resultsPath, false))
            {
                tw.Close();
            }
        }

        // Append new data to the session results file
        using (TextWriter tw = new StreamWriter(session_resultsPath, true))
        {
            tw.WriteLine(imitoneVoiceIntepreter.pitch_hz + "," + imitoneVoiceIntepreter._dbValue);
        }
    }
}
