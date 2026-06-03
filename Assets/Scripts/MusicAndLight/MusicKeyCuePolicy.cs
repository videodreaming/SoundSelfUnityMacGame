using System.Collections.Generic;
using ConversionUtilities;
using UnityEngine;

/// <summary>
/// Maps Lorna <c>Cue_Key_*</c> user cues to <see cref="NoteName"/> and applies via <see cref="MusicSystem1.SetFundamentalDirect"/>.
/// Shared handler for Wwise callbacks lands in Stage 5; harness uses this policy directly.
/// </summary>
public static class MusicKeyCuePolicy
{
    static readonly Dictionary<string, NoteName> CueToNote = new Dictionary<string, NoteName>
    {
        { "Cue_Key_C", NoteName.C },
        { "Cue_Key_D", NoteName.D },
        { "Cue_Key_E", NoteName.E },
        { "Cue_Key_F", NoteName.F },
        { "Cue_Key_G", NoteName.G },
        { "Cue_Key_A", NoteName.A },
        { "Cue_Key_B", NoteName.B },
        { "Cue_Key_Gsharp", NoteName.Gs },
        { "Cue_Key_Bflat", NoteName.As },
        { "Cue_Key_Aflat", NoteName.Gs },
        { "Cue_Key_Eflat", NoteName.Ds },
    };

    /// <summary>Lorna example timeline (one bed) — harness steps through with K.</summary>
    public static readonly string[] LornaExampleTimeline =
    {
        "Cue_Key_C", "Cue_Key_B", "Cue_Key_G", "Cue_Key_F", "Cue_Key_A", "Cue_Key_E",
        "Cue_Key_Gsharp", "Cue_Key_Bflat", "Cue_Key_C", "Cue_Key_Aflat", "Cue_Key_C",
        "Cue_Key_C", "Cue_Key_D", "Cue_Key_C", "Cue_Key_C", "Cue_Key_Eflat",
        "Cue_Key_C", "Cue_Key_C", "Cue_Key_A",
    };

    public static bool TryGetNoteForCue(string cue, out NoteName note)
    {
        note = NoteName.None;
        if (string.IsNullOrEmpty(cue))
            return false;
        if (!CueToNote.TryGetValue(cue, out NoteName mapped))
            return false;
        note = mapped;
        return true;
    }

    public static bool TryApplyCue(string cue, MusicSystem1 musicSystem)
    {
        if (musicSystem == null || string.IsNullOrEmpty(cue))
            return false;

        if (!TryGetNoteForCue(cue, out NoteName note))
        {
            if (cue.StartsWith("Cue_Key_"))
                Debug.LogWarning("MusicKeyCuePolicy: Unknown music-key cue: " + cue);
            return false;
        }

        musicSystem.SetFundamentalDirect(note);
        Debug.Log("MusicKeyCuePolicy: Applied " + cue + " → " + note);
        return true;
    }
}
