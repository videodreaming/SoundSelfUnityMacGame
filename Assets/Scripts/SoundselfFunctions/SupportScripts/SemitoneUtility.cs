using System;
using System.Collections.Generic;
using UnityEngine;

public static class SemitoneUtility
{
    public static readonly  string[] semitones = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    
    public static string GetSemitoneFromMIDI(int midi)
    {
        int l = midi % 12;
        if (l < 0 || l >= semitones.Length) return "None";
        return semitones[midi % 12]+(Mathf.FloorToInt(midi/12.0f)-1);
    }
    
    public static int GetNoteFromSemitone(int semitone, int octave)
    {
        if (semitone < 0 || octave < 0) return -1;
        return ((octave + 1) * 12) + semitone;
    }

    //TODO: probably need a more accurate semitone calculation in future
    public static int[] GetSemitoneFromFrequency(float pitch)
    {
        int s = Mathf.RoundToInt(12 * Mathf.Log(pitch/440, 2));
        int l = (9 + s)%12;
        int n = (9 + s)/12;
        if (l < 0 || l >= semitones.Length) return new int[2]{-1,-1};
        return new int[2]{l, 4+n};
    }

    public static string ToString(int [] s)
    {
        return s[0] < 0 ? "None" : semitones[s[0]] + s[1];
    }
}