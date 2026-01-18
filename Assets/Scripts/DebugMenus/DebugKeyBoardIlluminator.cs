using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI; // Include this to access UI components like Image
using ConversionUtilities;

public class DebugKeyBoardIlluminator : MonoBehaviour
{

    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;

    [SerializeField]
    private int fundamentalNote; // TODO: Will be updated to use fundamentalNoteName in Phase 3
    [SerializeField]
    private int musicNoteActivated; // Stored as int for serialization, converted from NoteName
    [SerializeField]
    private bool imitoneActive;
    [SerializeField]
    private bool toneActiveBiasTrue;

    

    // Update is called once per frame
    void Update()
    {
        // TODO Phase 3: Update to use fundamentalNoteName
        fundamentalNote = NoteUtils.NoteToInt(MusicSystem1.instance.fundamentalNoteName);
        musicNoteActivated = NoteUtils.NoteToInt(MusicSystem1.instance.musicNoteActivated);
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
                // TODO Phase 3: Update to use fundamentalNoteName directly
                int fundamentalNoteInt = NoteUtils.NoteToInt(MusicSystem1.instance.fundamentalNoteName);
                bool isBlue = (fundamentalNoteInt == 0 && key.name == "C") ||
                              (fundamentalNoteInt == 1 && key.name == "C#") ||
                              (fundamentalNoteInt == 2 && key.name == "D") ||
                              (fundamentalNoteInt == 3 && key.name == "D#") ||
                              (fundamentalNoteInt == 4 && key.name == "E") ||
                              (fundamentalNoteInt == 5 && key.name == "F") ||
                              (fundamentalNoteInt == 6 && key.name == "F#") ||
                              (fundamentalNoteInt == 7 && key.name == "G") ||
                              (fundamentalNoteInt == 8 && key.name == "G#") ||
                              (fundamentalNoteInt == 9 && key.name == "A") ||
                              (fundamentalNoteInt == 10 && key.name == "A#") ||
                              (fundamentalNoteInt == 11 && key.name == "B");

                // Check if the current key should be set to yellow
                int musicNoteActivatedInt = NoteUtils.NoteToInt(MusicSystem1.instance.musicNoteActivated);
                bool isYellow = (musicNoteActivatedInt == 0 && key.name == "C") ||
                                (musicNoteActivatedInt == 1 && key.name == "C#") ||
                                (musicNoteActivatedInt == 2 && key.name == "D") ||
                                (musicNoteActivatedInt == 3 && key.name == "D#") ||
                                (musicNoteActivatedInt == 4 && key.name == "E") ||
                                (musicNoteActivatedInt == 5 && key.name == "F") ||
                                (musicNoteActivatedInt == 6 && key.name == "F#") ||
                                (musicNoteActivatedInt == 7 && key.name == "G") ||
                                (musicNoteActivatedInt == 8 && key.name == "G#") ||
                                (musicNoteActivatedInt == 9 && key.name == "A") ||
                                (musicNoteActivatedInt == 10 && key.name == "A#") ||
                                (musicNoteActivatedInt == 11 && key.name == "B");

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
