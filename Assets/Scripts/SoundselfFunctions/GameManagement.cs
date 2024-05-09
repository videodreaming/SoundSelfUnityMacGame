using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class GameManagement : MonoBehaviour
{
    public string controlStatus = "resumed";
    public CSVWriter CSVWriter;
    void Update()
    {
        // Quit the game if the player presses the Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CSVWriter.writeCSV();
            EndGame();
        }
    }
    public void EndGame()
    {
        // End the game
        Application.Quit();
    }
}
