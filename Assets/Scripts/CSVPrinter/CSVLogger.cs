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
        filePath = Path.Combine(Application.streamingAssetsPath, "VolumeAndHarmonicity.csv");
        writer = new StreamWriter(filePath, false);

        writer.WriteLine("Time, Volume, Harmonicity");
    }

    // Update is called once per frame
    void Update()
    {
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
