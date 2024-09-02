using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//THIS SCRIPT NO LONGER IN USE

public class PitchMusicSystem : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public MusicSystem1 musicSystem1;
    //public WwiseGlobalManager wwiseGlobalManager;
    //public DebugKeyBoardIlluminator debugKeyBoardIlluminator;
    //public string currentNote = "";

    // Define a list of tuples representing note ranges and corresponding note names
    private List<(float minFreq, float maxFreq, string noteName)> noteRanges = new List<(float, float, string)>()
    {
        // (105f, 115f, "A2"),
        // //(116.5f, 136.5f, "A#2/Bb2"),
        // (118.47f, 128.47f, "B2"),
        // // Add other notes as necessary, with +-10 Hz buffer
        // (125.81f,135.81f, "C3"),
        // //(138.6f, 158.6f, "C#3/Db3"),
        // (141.83f, 151.83f, "D3"),
        // //(155.6f, 175.6f, "D#3/Eb3"),
        // (159.81f, 169.81f, "E3"),
        // (169.61f, 179.61f, "F3"),
        // //(185f, 205f, "F#3/Gb3"),
        // (191f, 201f, "G3"),
        // //(207.7f, 227.7f, "G#3/Ab3"),
        // (215f, 225f, "A3"),
        // //(233.1f, 253.1f, "A#3/Bb3"),
        // (241.9f, 251.9f, "B3"),
        // (256.6f, 266.6f, "C4"),
        // //(277.2f, 297.2f, "C#4/Db4"),
        // (288.7f, 298.7f, "D4"),
        // //(311.1f, 331.1f, "D#4/Eb4"),
        // (324.6f, 334.6f, "E4"),
        // (344.2f, 354.2f, "F4"),
        // //(370f, 390f, "F#4/Gb4"),
        // (387f, 497f, "G4"), 
        // //(415.3f, 435.3f, "G#4/Ab4"),
        // (435f, 445f, "A4"),
        // //(466.2f, 486.2f, "A#4/Bb4"),
        // (488.9f, 498.9f, "B4"),
        // (518.3f, 528.3f, "C5"),

        (107.5f, 112.5f, "A2"),
        (116.5f, 136.5f, "A#2/Bb2"),
        (120.97f, 125.97f, "B2"),
        // Add other notes as necessary, with +-10 Hz buffer
        (128.31f,133.31f, "C3"),
        (138.6f, 158.6f, "C#3/Db3"),
        (144.33f, 149.333f, "D3"),
        (155.6f, 175.6f, "D#3/Eb3"),
        (162.31f,167.31f, "E3"),
        (172.11f,177.11f, "F3"),
        (185f, 205f, "F#3/Gb3"),
        (193.5f,198.5f, "G3"),
        (207.7f, 227.7f, "G#3/Ab3"),
        (217.5f, 222.5f, "A3"),
        (233.1f, 253.1f, "A#3/Bb3"),
        (244.44f,249.44f, "B3"),
        (259.13f,264.13f, "C4"),
        (277.2f, 297.2f, "C#4/Db4"),
        (291.16f, 296.16f, "D4"),
        (311.1f, 331.1f, "D#4/Eb4"),
        (327.13f,332.13f, "E4"),
        (346.73f, 351.73f, "F4"),
        (370f, 390f, "F#4/Gb4"),
        (387.5f, 394.5f, "G4"), 
        (415.3f, 435.3f, "G#4/Ab4"),
        (437.5f, 442.5f, "A4"),
        (466.2f, 486.2f, "A#4/Bb4"),
        (491.38f, 496.38f, "B4"),
        (520.75f, 525.75f, "C5"),
        
    };

    void Update()
    {
        // THE BELOW WAS MOVED INTO MUSICSYSTEM1.CS
        // if (imitoneVoiceIntepreter != null && imitoneVoiceIntepreter.pitch_hz > 0)
        // {
        //     // Get the current pitch from the interpreter
        //     float currentPitch = imitoneVoiceIntepreter.pitch_hz;

        //     // Check which range the current pitch falls into
        //     foreach (var range in noteRanges)
        //     {
        //         if (currentPitch >= range.minFreq && currentPitch <= range.maxFreq)
        //         {
        //             //currentNote = range.noteName;
        //            // Debug.Log("noteName" + range.noteName + "   frequency" + currentPitch);
        //             if(range.noteName == "A2" || range.noteName == "A3" || range.noteName == "A4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "A";
        //                 musicSystem1.ChangeSwitchState();
        //             }
        //             else if(range.noteName == "A#2/Bb2" || range.noteName == "A#3/Bb3" || range.noteName == "A#4/Bb4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "As";
        //                 musicSystem1.ChangeSwitchState();
        //             }
        //             else if(range.noteName == "B2" || range.noteName == "B3" || range.noteName == "B4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "B";
        //                 musicSystem1.ChangeSwitchState();
        //             } else if(range.noteName == "C3" || range.noteName == "C4" || range.noteName == "C5" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "C";
        //                 musicSystem1.ChangeSwitchState();
        //             } 
        //             else if(range.noteName == "C#3/Db3" || range.noteName == "C#4/Db4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "Cs";
        //                 musicSystem1.ChangeSwitchState();
        //             }
        //             else if(range.noteName == "D3" || range.noteName == "D4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "D";
        //                 musicSystem1.ChangeSwitchState();
        //             } 
        //             else if(range.noteName == "D#3/Eb3" || range.noteName == "D#4/Eb4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "Ds";
        //                 musicSystem1.ChangeSwitchState();
        //             }
        //             else if(range.noteName == "E3" || range.noteName == "E4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "E";
        //                 musicSystem1.ChangeSwitchState();
        //             } else if(range.noteName == "F3" || range.noteName == "F4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "F";
        //                 musicSystem1.ChangeSwitchState();
        //             } 
        //             else if(range.noteName == "F#3/Gb3" || range.noteName == "F#4/Gb4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "Fs";
        //                 musicSystem1.ChangeSwitchState();
        //             }
        //             else if(range.noteName == "G3" || range.noteName == "G4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "G";
        //                 musicSystem1.ChangeSwitchState();
        //             }
        //             else if(range.noteName == "G#3/Ab3" || range.noteName == "G#4/Ab4" ) 
        //             {
        //                 musicSystem1.currentSwitchState = "Gs";
        //                 musicSystem1.ChangeSwitchState();
        //             }
        //             break; // Stop checking once the correct range is found
        //         }
        //     }
        // }
    }
}