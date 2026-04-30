using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class GameManagement : MonoBehaviour
{
    public CSVWriter CSVWriter;

    public void EndGame()
    {
        RecordedAudioPlayback.Instance.DeleteAllRecordings();
        CSVWriter.writeCSV();
        Debug.Log("Ending game...");
        Application.Quit();
    }
}
