using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Include this to access UI components like Image

public class DebugKeyBoardIlluminatorForPlayGround : MonoBehaviour
{
    public MusicSystem1ForPlayGround musicSystem1ForPlayGround;

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
                bool isBlue = (musicSystem1ForPlayGround.fundamentalNote == 0 && key.name == "C") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 1 && key.name == "C#") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 2 && key.name == "D") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 3 && key.name == "D#") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 4 && key.name == "E") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 5 && key.name == "F") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 6 && key.name == "F#") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 7 && key.name == "G") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 8 && key.name == "G#") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 9 && key.name == "A") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 10 && key.name == "A#") ||
                              (musicSystem1ForPlayGround.fundamentalNote == 11 && key.name == "B");

                // Check if the current key should be set to yellow
                bool isYellow = (musicSystem1ForPlayGround.musicNoteActivated == 0 && key.name == "C") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 1 && key.name == "C#") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 2 && key.name == "D") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 3 && key.name == "D#") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 4 && key.name == "E") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 5 && key.name == "F") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 6 && key.name == "F#") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 7 && key.name == "G") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 8 && key.name == "G#") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 9 && key.name == "A") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 10 && key.name == "A#") ||
                                (musicSystem1ForPlayGround.musicNoteActivated == 11 && key.name == "B");

                if (isYellow) // If isYellow is true, set the color to yellow
                {
                    keyImage.color = Color.yellow;
                }
                else if (isBlue) // If isBlue is true, set the color to blue
                {
                    keyImage.color = Color.blue;
                }
                //else if (isBlue && isYellow)
                //{
                //    keyImage.color = Color.green;
                //}
                else // Otherwise, set the key to white
                {
                    keyImage.color = Color.white;
                }
            }
        }
    }
}
