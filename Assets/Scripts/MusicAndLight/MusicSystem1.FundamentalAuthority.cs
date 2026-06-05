using System.Collections.Generic;
using UnityEngine;
using ConversionUtilities;

/// <summary>
/// The thing that owns the master fundamental at a given moment. Exactly one is active at a time
/// (the active-source model that replaces the old priority lock stack).
/// </summary>
public enum FundamentalSource
{
    /// <summary>The sung-pitch voice-tracking loop (<c>FundamentalUpdate</c>), only live in tracking modes.</summary>
    InputDriven,
    /// <summary>The music loop bed (<c>Cue_Key_*</c> from MusicLoops). Wired in 4g.</summary>
    MusicBed,
    /// <summary>The sequencer / stage pins the note (replaces the old mode lock AND savasana's content lock).</summary>
    Sequence,
}

// Part of MusicSystem1 (see MusicSystem1.cs). Split out via `partial` to keep that file's size down (Block 7).
// Holds the active-source fundamental authority: which source is active, each source's preferred note, and the
// public API. All other state (fundamentalNoteName, the charge dict, ApplyMasterFundamental, ResetFundamentalTimers,
// currentMusicMode) is declared in MusicSystem1.cs. The legacy priority lock stack + debug override it replaced
// were removed in Stage 4e; the active source is now the sole authority over the master fundamental.
public partial class MusicSystem1
{
    // Active-source authority state (Block 7 / 4d). Startup is owned by the sequencer; 4e zone 1 declares this
    // explicitly via SetFundamentalSource(StartupSource, ...) in Start (the field initializer is just the pre-Start default).
    private FundamentalSource activeFundamentalSource = FundamentalSourcePolicy.StartupSource;

    // Each source's own preferred fundamental. NoteName.None = "not set yet" (seeded in Start to the startup fundamental).
    private readonly Dictionary<FundamentalSource, NoteName> preferredFundamentalBySource = new Dictionary<FundamentalSource, NoteName>
    {
        { FundamentalSource.InputDriven, NoteName.None },
        { FundamentalSource.MusicBed, NoteName.None },
        { FundamentalSource.Sequence, NoteName.None },
    };

    /// <summary>
    /// Switch the active source. <paramref name="firstFundamental"/> == None adopts that source's existing
    /// preferred; a real note sets it (and for InputDriven also wipes per-note charge memory — "clean slate").
    /// Writes the master if the source has a real preferred.
    /// </summary>
    public void SetFundamentalSource(FundamentalSource source, NoteName firstFundamental = NoteName.None)
    {
        // InputDriven only "lives" where DynamicMusicSystem() runs (tracking modes). Honor the switch anywhere, but warn (B457).
        // (Preparatory soundscape pre-sets don't reach here — they use SetSoundscapeWithoutChangingFundamentalSource.)
        if (source == FundamentalSource.InputDriven && !FundamentalSourcePolicy.IsTrackingMode(currentMusicMode))
        {
            if (debugAllowWarnings || debugAllowFundamentalLockLogs)
            {
                Debug.LogWarning($"B457 MUSIC FUNDAMENTAL-SOURCE: SetFundamentalSource(InputDriven) while not in a tracking mode (mode={currentMusicMode}) — honoring, but the master won't track sung pitch until a tracking mode resumes.");
            }
        }

        activeFundamentalSource = source;

        if (firstFundamental != NoteName.None)
        {
            preferredFundamentalBySource[source] = firstFundamental;
            if (source == FundamentalSource.InputDriven)
            {
                ResetFundamentalTimers(); // clean slate: wipe per-note charge memory
            }
        }

        if (debugAllowFundamentalLockLogs)
        {
            Debug.Log($"MUSIC FUNDAMENTAL-SOURCE: Active source → {source}, preferred={preferredFundamentalBySource[source]}");
        }

        NoteName preferred = preferredFundamentalBySource[source];
        if (preferred != NoteName.None)
        {
            ApplyMasterFundamental(preferred);
        }
    }

    /// <summary>
    /// Resume the soundscape-driven source after a tutorial/correction pin (Block 7 / 4e zone 4/5).
    /// This is the "exceptional space" resume — deliberately NOT the same as the playground/Freeplay entry resume:
    /// <list type="bullet">
    /// <item>SoundWorld → InputDriven seeded with the CURRENT master fundamental, so voice tracking resumes from where
    /// the pin left it (clean slate), NOT from InputDriven's stale shadow-tracked preferred.</item>
    /// <item>MusicLoop → MusicBed adopting the bed's own preferred (the correction must not overwrite the bed key).</item>
    /// </list>
    /// (Playground entry instead adopts the source's existing preferred — see SetMusicModeTo Freeplay zone 3.)
    /// </summary>
    public void ResumeFundamentalAfterCorrectionPin()
    {
        if (FundamentalSourcePolicy.SourceForInteractionType(currentInteractionType) == FundamentalSource.MusicBed)
        {
            SetFundamentalSource(FundamentalSource.MusicBed);
        }
        else
        {
            SetFundamentalSource(FundamentalSource.InputDriven, fundamentalNoteName);
        }
    }

    /// <summary>
    /// Update a source's preferred fundamental. Writes the master only if that source is currently active.
    /// InputDriven → clean-slate reset of the charge memory.
    /// </summary>
    public void SetFundamentalForSource(FundamentalSource source, NoteName note)
    {
        if (note == NoteName.None)
        {
            if (debugAllowWarnings || debugAllowFundamentalLockLogs)
            {
                Debug.LogWarning($"MUSIC FUNDAMENTAL-SOURCE: SetFundamentalForSource({source}, None) ignored — None is not a valid fundamental.");
            }
            return;
        }

        preferredFundamentalBySource[source] = note;
        if (source == FundamentalSource.InputDriven)
        {
            ResetFundamentalTimers(); // clean slate
        }

        if (FundamentalSourcePolicy.ShouldWriteMaster(source, activeFundamentalSource))
        {
            ApplyMasterFundamental(note);
        }
        else if (debugAllowFundamentalLockLogs)
        {
            Debug.Log($"MUSIC FUNDAMENTAL-SOURCE: preferred[{source}]={note} stored, but not written (active={activeFundamentalSource}).");
        }
    }

#if UNITY_EDITOR
    // ===== TEMPORARY — Stage 9 playtest scaffolding. REMOVE at the Stage 9 final commit. =====
    // Lets the guided playtest (MusicDebugGuidedPlaytest) exercise the Director ↔ fundamental path WITHOUT singing,
    // so each behavior is deterministic + measurable. It drives the EXACT same AnnounceFundamental path a real sung
    // change uses — it is NOT a bypass — so every downstream effect is identical to a real voice change: the master
    // move becomes a COUNTED Director audio event (pairs a visual flourish), the 5s anti-clutter gate applies, and
    // while director.disable it routes raw (no flourish). The InputDriven write gate (CanInputDrivenWriteMaster) still
    // applies, so this also behaves correctly under source switches once 9c/4g land.
    //
    // NOTE: this deliberately re-introduces a "force the master" surface (the kind 4e deleted), which is why it is
    // editor-only and tracked for deletion in Docs/BLOCKS_4_5_7_BUILD_CHECKLIST.md "Temporary playtest scaffolding".
    //
    // semitoneOffset: how far to move the master from its CURRENT note (kept non-zero so the master actually moves —
    //   a redundant target would correctly produce no flourish, but that is not what the goblin steps test).
    // immediate:      true = long/longish band (enqueue + activate now); false = short band (enqueue only — the caller
    //   then simulates the external beat via director.ActivateQueue()).
    public void DebugSimulateSungFundamentalChange(int semitoneOffset, bool immediate)
    {
        NoteName target = NoteUtils.AddInterval(fundamentalNoteName, semitoneOffset);
        if (target == NoteName.None || target == fundamentalNoteName)
        {
            Debug.LogWarning("[B457 DEBUG-SIM] no-op: offset " + semitoneOffset + " from "
                + NoteUtils.NoteToWwiseString(fundamentalNoteName) + " is invalid or unchanged.");
            return;
        }

        Debug.Log("[B457 DEBUG-SIM] simulate sung change " + NoteUtils.NoteToWwiseString(fundamentalNoteName)
            + "→" + NoteUtils.NoteToWwiseString(target) + " (offset " + semitoneOffset
            + ", band=" + (immediate ? "immediate/long" : "deferred/short")
            + ", activeSource=" + activeFundamentalSource + ", directorDisabled=" + (director != null && director.disable) + ")");

        AnnounceFundamental(target, immediate);
    }
#endif
}
