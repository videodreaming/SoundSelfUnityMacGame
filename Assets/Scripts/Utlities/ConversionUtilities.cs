using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ConversionUtilities
{
    public enum NoteName
    {
        None, // -1
        C,  //0
        Cs, //1
        D,  //2
        Ds, //3
        E,  //4
        F,  //5
        Fs, //6
        G,  //7
        Gs, //8
        A,  //9
        As, //10
        B   //11
    }

public static class NoteUtils
{
    // Optional: string aliases if you ever want "C#" instead of "Cs"
    private static readonly Dictionary<string, NoteName> ParseMap =
        new Dictionary<string, NoteName>(StringComparer.OrdinalIgnoreCase)
        {
            { "C", NoteName.C },
            { "Cs", NoteName.Cs }, { "C#", NoteName.Cs },
            { "D", NoteName.D },
            { "Ds", NoteName.Ds }, { "D#", NoteName.Ds },
            { "E", NoteName.E },
            { "F", NoteName.F },
            { "Fs", NoteName.Fs }, { "F#", NoteName.Fs },
            { "G", NoteName.G },
            { "Gs", NoteName.Gs }, { "G#", NoteName.Gs },
            { "A", NoteName.A },
            { "As", NoteName.As }, { "A#", NoteName.As },
            { "B", NoteName.B },
            { "none", (NoteName)(-1) } // handled specially below
        };

    public static bool TryIntToNote(int noteNumber, out NoteName note)
    {
        if (noteNumber >= 0 && noteNumber <= 11)
        {
            note = (NoteName)noteNumber;
            return true;
        }

        note = default;
        return false;
    }

    public static string IntToNoteString(int noteNumber)
    {
        if (!TryIntToNote(noteNumber, out var note))
        {
            Debug.LogWarning($"MUSIC: Invalid note number {noteNumber}");
            return "none";
        }

        return note.ToString();
    }

    public static bool TryParseNote(string noteName, out NoteName note)
    {
        if (string.IsNullOrWhiteSpace(noteName) || noteName.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            note = default;
            return false;
        }

        return ParseMap.TryGetValue(noteName.Trim(), out note) && (int)note >= 0;
    }

    public static int NoteToInt(string noteName)
    {
        if (!TryParseNote(noteName, out var note))
            return -1; // your convention for empty/none/invalid

        return (int)note;
    }

    public static int NoteToInt(NoteName note)
        {
            if (!TryParseNote(note.ToString(), out var parsedNote))
                return -1;
            return (int)parsedNote;
        }

    public static float NoteToFrequencyA440(NoteName note)
    {
        // note is within 0..11
        int semitoneDifferenceFromA = ((int)note) - 9;
        return 440f * Mathf.Pow(2f, semitoneDifferenceFromA / 12f);
    }

    public static float NoteToFrequencyA440(string noteName)
    {
        if (!TryParseNote(noteName, out var note))
        {
            Debug.LogWarning($"MUSIC: Cannot convert invalid note name to frequency: {noteName}");
            return -1f;
        }

        return NoteToFrequencyA440(note);
    }
}

}
