using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; // Include this to access UI components like Image

public class DebugKeyBoardIlluminator : MonoBehaviour
{

    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;

    [SerializeField]
    private int fundamentalNote;
    [SerializeField]
    private int musicNoteActivated;
    [SerializeField]
    private bool imitoneActive;
    [SerializeField]
    private bool toneActiveBiasTrue;

    

    // Update is called once per frame
    void Update()
    {
        fundamentalNote = MusicSystem1.instance.fundamentalNote;
        musicNoteActivated = MusicSystem1.instance.musicNoteActivated;
        imitoneActive = imitoneVoiceInterpreter.imitoneActive;
        toneActiveBiasTrue = imitoneVoiceInterpreter.toneActiveBiasTrue;

        // Fetch all game objects tagged as "Piano"
        GameObject[] pianoKeys = GameObject.FindGameObjectsWithTag("Piano");
        // Iterate through each piano key
        foreach (GameObject key in pianoKeys)
        {
            Image keyImage = key.GetComponent<Image>(); // Get the Image component
            if (keyImage != null) // Check if the Image component is found
            {
                // Check if the current key should be set to blue
                bool isBlue = (MusicSystem1.instance.fundamentalNote == 0 && key.name == "C") ||
                              (MusicSystem1.instance.fundamentalNote == 1 && key.name == "C#") ||
                              (MusicSystem1.instance.fundamentalNote == 2 && key.name == "D") ||
                              (MusicSystem1.instance.fundamentalNote == 3 && key.name == "D#") ||
                              (MusicSystem1.instance.fundamentalNote == 4 && key.name == "E") ||
                              (MusicSystem1.instance.fundamentalNote == 5 && key.name == "F") ||
                              (MusicSystem1.instance.fundamentalNote == 6 && key.name == "F#") ||
                              (MusicSystem1.instance.fundamentalNote == 7 && key.name == "G") ||
                              (MusicSystem1.instance.fundamentalNote == 8 && key.name == "G#") ||
                              (MusicSystem1.instance.fundamentalNote == 9 && key.name == "A") ||
                              (MusicSystem1.instance.fundamentalNote == 10 && key.name == "A#") ||
                              (MusicSystem1.instance.fundamentalNote == 11 && key.name == "B");

                // Check if the current key should be set to yellow
                bool isYellow = (MusicSystem1.instance.musicNoteActivated == 0 && key.name == "C") ||
                                (MusicSystem1.instance.musicNoteActivated == 1 && key.name == "C#") ||
                                (MusicSystem1.instance.musicNoteActivated == 2 && key.name == "D") ||
                                (MusicSystem1.instance.musicNoteActivated == 3 && key.name == "D#") ||
                                (MusicSystem1.instance.musicNoteActivated == 4 && key.name == "E") ||
                                (MusicSystem1.instance.musicNoteActivated == 5 && key.name == "F") ||
                                (MusicSystem1.instance.musicNoteActivated == 6 && key.name == "F#") ||
                                (MusicSystem1.instance.musicNoteActivated == 7 && key.name == "G") ||
                                (MusicSystem1.instance.musicNoteActivated == 8 && key.name == "G#") ||
                                (MusicSystem1.instance.musicNoteActivated == 9 && key.name == "A") ||
                                (MusicSystem1.instance.musicNoteActivated == 10 && key.name == "A#") ||
                                (MusicSystem1.instance.musicNoteActivated == 11 && key.name == "B");

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
