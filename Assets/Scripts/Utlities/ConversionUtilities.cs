using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ConversionUtilities
{
    public enum NoteName
    {
        None = -1,
        C = 0,
        Cs = 1,
        D = 2,
        Ds = 3,
        E = 4,
        F = 5,
        Fs = 6,
        G = 7,
        Gs = 8,
        A = 9,
        As = 10,
        B = 11
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

        note = NoteName.None; // Explicitly set to None to indicate failure
        return false;
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

    /// <summary>
    /// Converts NoteName enum directly to int (0-11). Much more efficient than string conversion.
    /// </summary>
    public static int NoteToInt(NoteName note)
    {
        // Direct cast - no string conversion needed!
        if (note == NoteName.None)
            return -1;
        return (int)note;
    }

    /// <summary>
    /// Converts NoteName enum to frequency in Hz (A440 standard).
    /// </summary>
    public static float NoteToFrequencyA440(NoteName note)
    {
        if (note == NoteName.None)
        {
            Debug.LogWarning("MUSIC: Cannot convert NoteName.None to frequency");
            return -1f;
        }
        // note is within 0..11
        int semitoneDifferenceFromA = ((int)note) - 9;
        return 440f * Mathf.Pow(2f, semitoneDifferenceFromA / 12f);
    }

    /// <summary>
    /// Converts int (0-11) directly to frequency in Hz (A440 standard). 
    /// Most efficient path for int -> frequency conversion.
    /// </summary>
    public static float NoteToFrequencyA440(int noteNumber)
    {
        if (noteNumber < 0 || noteNumber > 11)
        {
            Debug.LogWarning($"MUSIC: Invalid note number {noteNumber} for frequency conversion");
            return -1f;
        }
        int semitoneDifferenceFromA = noteNumber - 9;
        return 440f * Mathf.Pow(2f, semitoneDifferenceFromA / 12f);
    }

    /// <summary>
    /// Converts string note name to frequency in Hz (A440 standard).
    /// For direct int conversion, use NoteToFrequencyA440(int) instead.
    /// </summary>
    public static float NoteToFrequencyA440(string noteName)
    {
        if (!TryParseNote(noteName, out var note))
        {
            Debug.LogWarning($"MUSIC: Cannot convert invalid note name to frequency: {noteName}");
            return -1f;
        }

        return NoteToFrequencyA440(note);
    }

    /// <summary>
    /// Adds semitones to a note, wrapping around octave (modulo 12).
    /// </summary>
    public static NoteName AddInterval(NoteName note, int semitones)
    {
        if (note == NoteName.None) return NoteName.None;
        int noteInt = NoteToInt(note);
        if (noteInt < 0 || noteInt > 11) return NoteName.None; // Safety check - NoteToInt returns -1 for None
        int result = (noteInt + semitones) % 12;
        if (result < 0) result += 12; // Handle negative intervals
        if (!TryIntToNote(result, out NoteName resultNote))
            return NoteName.None;
        return resultNote;
    }

    /// <summary>
    /// Calculates the wrapped distance between two notes (0-6 semitones).
    /// Returns -1 if either note is None.
    /// </summary>
    public static int GetWrappedDistance(NoteName a, NoteName b)
    {
        if (a == NoteName.None || b == NoteName.None) return -1;
        int aInt = NoteToInt(a);
        int bInt = NoteToInt(b);
        if (aInt < 0 || aInt > 11 || bInt < 0 || bInt > 11) return -1; // Safety check
        int diff = Mathf.Abs(aInt - bInt);
        return Mathf.Min(diff, 12 - diff);
    }

    /// <summary>
    /// Converts NoteName to string for Wwise (uses enum's ToString()).
    /// </summary>
    public static string NoteToWwiseString(NoteName note)
    {
        if (note == NoteName.None) return "none";
        return note.ToString();
    }

    /// <summary>
    /// Converts float note value from Imitone to NoteName (modulo 12).
    /// Rounds to nearest integer before conversion.
    /// Returns None for invalid/error values (negative or very large).
    /// </summary>
    public static NoteName FloatToNoteName(float noteValue)
    {
        //NOTE FROM ROBIN: I'm not fully confident how this works. Be sure to check it's implementation carefully.
        
        // Handle error cases: -1f indicates error from Imitone, negative values are invalid
        if (noteValue < 0f)
            return NoteName.None;
        
        // Round to nearest integer and apply modulo 12
        int noteInt = Mathf.RoundToInt(noteValue) % 12;
        if (noteInt < 0) noteInt += 12; // Handle negative modulo results (shouldn't happen after check above, but safety)
        
        // Validate the result is in valid range (0-11)
        if (!TryIntToNote(noteInt, out NoteName note))
            return NoteName.None;
        return note;
    }
}

}
