using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Include this to access UI components like Image

public class DebugKeyBoardIlluminator : MonoBehaviour
{
    public MusicSystem1 musicSystem1;

    // Update is called once per frame
    void Update()
    {
        // Fetch all game objects tagged as "Piano"
        GameObject[] pianoKeys = GameObject.FindGameObjectsWithTag("Piano");
        // Iterate through each piano key
        foreach (GameObject key in pianoKeys)
        {
            Image keyImage = key.GetComponent<Image>(); // Get the Image component
            if (keyImage != null) // Check if the Image component is found
            {
                // Check if the current key should be set to blue
                bool isBlue = (musicSystem1.fundamentalNote == 0 && key.name == "C") ||
                              (musicSystem1.fundamentalNote == 1 && key.name == "C#") ||
                              (musicSystem1.fundamentalNote == 2 && key.name == "D") ||
                              (musicSystem1.fundamentalNote == 3 && key.name == "D#") ||
                              (musicSystem1.fundamentalNote == 4 && key.name == "E") ||
                              (musicSystem1.fundamentalNote == 5 && key.name == "F") ||
                              (musicSystem1.fundamentalNote == 6 && key.name == "F#") ||
                              (musicSystem1.fundamentalNote == 7 && key.name == "G") ||
                              (musicSystem1.fundamentalNote == 8 && key.name == "G#") ||
                              (musicSystem1.fundamentalNote == 9 && key.name == "A") ||
                              (musicSystem1.fundamentalNote == 10 && key.name == "A#") ||
                              (musicSystem1.fundamentalNote == 11 && key.name == "B");

                // Check if the current key should be set to yellow
                bool isYellow = (musicSystem1.musicNoteActivated == 0 && key.name == "C") ||
                                (musicSystem1.musicNoteActivated == 1 && key.name == "C#") ||
                                (musicSystem1.musicNoteActivated == 2 && key.name == "D") ||
                                (musicSystem1.musicNoteActivated == 3 && key.name == "D#") ||
                                (musicSystem1.musicNoteActivated == 4 && key.name == "E") ||
                                (musicSystem1.musicNoteActivated == 5 && key.name == "F") ||
                                (musicSystem1.musicNoteActivated == 6 && key.name == "F#") ||
                                (musicSystem1.musicNoteActivated == 7 && key.name == "G") ||
                                (musicSystem1.musicNoteActivated == 8 && key.name == "G#") ||
                                (musicSystem1.musicNoteActivated == 9 && key.name == "A") ||
                                (musicSystem1.musicNoteActivated == 10 && key.name == "A#") ||
                                (musicSystem1.musicNoteActivated == 11 && key.name == "B");

                if (isBlue) // If isBlue is true, set the color to blue
                {
                    keyImage.color = Color.blue;
                }
                else if (isYellow) // If isYellow is true, set the color to yellow
                {
                    keyImage.color = Color.yellow;
                }
                else // Otherwise, set the key to white
                {
                    keyImage.color = Color.white;
                }
            }
        }
    }
}
