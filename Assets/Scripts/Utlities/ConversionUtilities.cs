using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConversionUtilities
{
    public enum NoteName
{
    C, Cs, D, Ds, E, F, Fs, G, Gs, A, As, B
}

public static class NoteUtils
{
    public static float GetFrequency(NoteName note) => note switch
    {
        NoteName.C  => 261.63f,
        NoteName.Cs => 277.18f,
        NoteName.D  => 293.66f,
        NoteName.Ds => 311.13f,
        NoteName.E  => 329.63f,
        NoteName.F  => 349.23f,
        NoteName.Fs => 369.99f,
        NoteName.G  => 392.00f,
        NoteName.Gs => 415.30f,
        NoteName.A  => 440.00f,
        NoteName.As => 466.16f,
        NoteName.B  => 493.88f,
        _ => 440f
    };
}

}
