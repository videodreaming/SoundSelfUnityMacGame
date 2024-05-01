using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Include this to access UI components like Image

public class DebugKeyBoardIlluminator : MonoBehaviour
{
    public PitchMusicSystem pitchMusicSystem;

    // Update is called once per frame
    void Update()
    {
        // Fetch all game objects tagged as "PianoKey"
        GameObject[] pianoKeys = GameObject.FindGameObjectsWithTag("Piano");

        // Iterate through each piano key
        foreach (GameObject key in pianoKeys)
        {
            Image keyImage = key.GetComponent<Image>(); // Get the Image component
            if (keyImage != null) // Check if the Image component is found
            {
                // If the key's name matches the current note, set it to green
                if (key.name == pitchMusicSystem.currentNote)
                {
                    keyImage.color = Color.green;
                }
                else
                {
                    // Otherwise, set the key to white
                    keyImage.color = Color.white;
                }
            }
        }
    }
}
