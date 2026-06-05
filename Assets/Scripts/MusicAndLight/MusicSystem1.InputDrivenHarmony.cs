using System.Collections.Generic;
using UnityEngine;
using ConversionUtilities;

// Part of MusicSystem1 (see MusicSystem1.cs). Split out via `partial` purely to reduce file size (Block 7 / 4c).
// Owns the input-driven harmony tracking (HarmonyUpdate) and the Wwise harmony-switch wrapper (changeHarmony).
// All state (sequences, currentSequenceIndex, currentHarmonyIndex, random, harmonyNote) is declared in MusicSystem1.cs;
// these methods are members of the same class. No behavior change — bodies were moved verbatim from MusicSystem1.cs.
public partial class MusicSystem1
{
    private void HarmonyUpdate()
    {
        if(imitoneVoiceInterpreter.toneActiveBiasTrueFrame)
        {
            //Choose a tone based on a sequence
            List<int> currentSequence = sequences[currentSequenceIndex];
            int harmonization = currentSequence[currentHarmonyIndex];

            // Move to the next note in the sequence
            currentHarmonyIndex++;

            // If we've reached the end of the sequence, select a new sequence
            if (currentHarmonyIndex >= currentSequence.Count)
            {
                currentSequenceIndex = random.Next(sequences.Count);
                currentHarmonyIndex = 0;
                if(debugAllowHarmonyLogicLogs)
                {
                    Debug.Log("MUSIC: New Harmony Sequence Selected:" + currentSequenceIndex);
                }
            }

            //Now play the tone
                            
            // Conversion point: NoteName arithmetic - AddInterval() handles NoteName enum internally
            // Converts to int for modulo arithmetic, then back to NoteName enum
            // Calculate harmony note by adding interval to fundamental note
            harmonyNote = NoteUtils.AddInterval(fundamentalNoteName, harmonization);
            if (harmonyNote == NoteName.None)
            {
                if(debugAllowWarnings || debugAllowHarmonyChangeLogs)
                {
                    Debug.LogWarning($"MUSIC: AddInterval() returned NoteName.None for fundamentalNoteName={fundamentalNoteName}, harmonization={harmonization} - harmony will not play correctly");
                }
            }
            changeHarmony(harmonyNote); 
            if (debugAllowHarmonyChangeLogs)
            {
                Debug.Log("MUSIC: Harmony Played: " + NoteUtils.NoteToWwiseString(harmonyNote) + " ~ (fundamentalNoteName + " + harmonization + ")");
            }
        }
    }

    private void changeHarmony(NoteName harmonyNote)
    {
        // Wwise 12-pitch groups have no "none" switch; SetSwitch("…", "none") leaves the group invalid and triggers errors when silent loops resolve.
        if (harmonyNote == NoteName.None)
        {
            if(debugAllowWarnings || debugAllowHarmonyChangeLogs)
            {
                Debug.LogWarning("MUSIC: changeHarmony skipped — harmony is None (would be invalid Wwise switch 'none'); keeping previous harmony switch.");
            }
            return;
        }

        SetSwitchRestoreToningV3("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", NoteUtils.NoteToWwiseString(harmonyNote));
        if(debugAllowHarmonyChangeLogs)
        {
            Debug.Log("MUSIC: Harmony Note Set To: " + NoteUtils.NoteToWwiseString(harmonyNote));
        }
    }
}
