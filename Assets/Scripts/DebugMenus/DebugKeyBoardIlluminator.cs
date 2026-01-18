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
    private NoteName fundamentalNote; // Current fundamental note for debug display
    [SerializeField]
    private NoteName musicNoteActivated; // Current activated note for debug display
    [SerializeField]
    private bool imitoneActive;
    [SerializeField]
    private bool toneActiveBiasTrue;

    

    // Update is called once per frame
    void Update()
    {
        fundamentalNote = MusicSystem1.instance.fundamentalNoteName;
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
                // Convert key name to NoteName for comparison
                NoteName keyNote = GetNoteNameFromKeyName(key.name);
                
                // Check if the current key should be set to blue (fundamental note)
                bool isBlue = fundamentalNote != NoteName.None && fundamentalNote == keyNote;

                // Check if the current key should be set to yellow (activated note)
                bool isYellow = musicNoteActivated != NoteName.None && musicNoteActivated == keyNote;

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

    /// <summary>
    /// Converts a piano key GameObject name to NoteName enum.
    /// Handles both "C#" and "Cs" style naming conventions.
    /// </summary>
    private NoteName GetNoteNameFromKeyName(string keyName)
    {
        if (NoteUtils.TryParseNote(keyName, out NoteName note))
        {
            return note;
        }
        return NoteName.None;
    }
}
