using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVLogger : MonoBehaviour
{
    private StreamWriter writer;
    private string filePath;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    private float _dbFromImitone;
    private float _harmonicityFromImitone;
    private float _timeSinceLaunch;

    void Start()
    {
        if (imitoneVoiceIntepreter == null)
        {
            Debug.LogError("CSVLogger: Assign imitoneVoiceIntepreter in the Inspector (or disable this component). Logging disabled.");
            return;
        }

        // StreamingAssets is often read-only or memory-mapped (Win32 1224 ERROR_USER_MAPPED_FILE); use a writable folder.
        filePath = Path.Combine(Application.persistentDataPath, "VolumeAndHarmonicity.csv");
        try
        {
            writer = new StreamWriter(filePath, false);
            writer.WriteLine("Time, Volume, Harmonicity");
            Debug.Log("CSVLogger: Writing to " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("CSVLogger: Could not open " + filePath + " — " + e.Message);
            writer = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (writer == null || imitoneVoiceIntepreter == null)
            return;

        _dbFromImitone = imitoneVoiceIntepreter._dbValue;
        _harmonicityFromImitone = imitoneVoiceIntepreter._harmonicity;
        _timeSinceLaunch = Time.time;
        writer.WriteLine($"{_timeSinceLaunch}, {_dbFromImitone}, {_harmonicityFromImitone}");
    }

    void OnDisable()
    {
        if(writer != null){
            writer.Close();
        }
    }
}
