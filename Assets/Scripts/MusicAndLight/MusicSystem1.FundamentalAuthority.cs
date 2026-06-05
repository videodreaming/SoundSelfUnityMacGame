using System.Collections.Generic;
using UnityEngine;
using ConversionUtilities;

/// <summary>
/// The thing that owns the master fundamental at a given moment. Exactly one is active at a time
/// (the active-source model that replaces the old priority lock stack); a debug override sits on top.
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
// Holds the active-source fundamental authority introduced in Stage 4d: which source is active, each source's
// preferred note, the debug override, and the public API. All other state (fundamentalNoteName, the charge dict,
// the legacy lock fields, ApplyMasterFundamental, ResetFundamentalTimers, currentMusicMode) is declared in MusicSystem1.cs.
//
// 4d is behavior-preserving: the legacy lock setters keep their exact logic and route their inner master-write
// through this API (recording the active source + preferred), while the production write-gate remains the legacy
// IsFundamentalLocked(). The active-source state becomes authoritative as call sites migrate in 4e.
public partial class MusicSystem1
{
    // Active-source authority state (Block 7 / 4d). Default Sequence — startup is owned by the sequencer (4e pins it on Awake).
    private FundamentalSource activeFundamentalSource = FundamentalSource.Sequence;

    // Each source's own preferred fundamental. NoteName.None = "not set yet" (seeded in Start to the startup fundamental).
    private readonly Dictionary<FundamentalSource, NoteName> preferredFundamentalBySource = new Dictionary<FundamentalSource, NoteName>
    {
        { FundamentalSource.InputDriven, NoteName.None },
        { FundamentalSource.MusicBed, NoteName.None },
        { FundamentalSource.Sequence, NoteName.None },
    };

    // Debug override sits on top of the active source; while held, nothing else writes the master.
    private NoteName? debugFundamentalOverride = null;

    /// <summary>
    /// Switch the active source. <paramref name="firstFundamental"/> == None adopts that source's existing
    /// preferred; a real note sets it (and for InputDriven also wipes per-note charge memory — "clean slate").
    /// Writes the master only if no debug override is held and the source has a real preferred.
    /// </summary>
    public void SetFundamentalSource(FundamentalSource source, NoteName firstFundamental = NoteName.None)
    {
        // InputDriven only "lives" where DynamicMusicSystem() runs (tracking modes). Honor the switch anywhere, but warn (B457).
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
            Debug.Log($"MUSIC FUNDAMENTAL-SOURCE: Active source → {source}, preferred={preferredFundamentalBySource[source]}, debugOverride={(debugFundamentalOverride.HasValue ? debugFundamentalOverride.Value.ToString() : "none")}");
        }

        NoteName preferred = preferredFundamentalBySource[source];
        if (!debugFundamentalOverride.HasValue && preferred != NoteName.None)
        {
            ApplyMasterFundamental(preferred);
        }
    }

    /// <summary>
    /// Update a source's preferred fundamental. Writes the master only if that source is currently active
    /// and no debug override is held. InputDriven → clean-slate reset of the charge memory.
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

        if (FundamentalSourcePolicy.ShouldWriteMaster(source, activeFundamentalSource, debugFundamentalOverride.HasValue))
        {
            ApplyMasterFundamental(note);
        }
        else if (debugAllowFundamentalLockLogs)
        {
            Debug.Log($"MUSIC FUNDAMENTAL-SOURCE: preferred[{source}]={note} stored, but not written (active={activeFundamentalSource}, debugOverride={(debugFundamentalOverride.HasValue ? debugFundamentalOverride.Value.ToString() : "none")}).");
        }
    }

    /// <summary>
    /// Set or clear the debug fundamental override (sits on top of the active source). Pass null to clear,
    /// which restores the active source's preferred. None is rejected.
    /// </summary>
    public void SetDebugFundamentalOverride(NoteName? note)
    {
        if (note.HasValue)
        {
            if (note.Value == NoteName.None)
            {
                if (debugAllowWarnings || debugAllowFundamentalLockLogs)
                {
                    Debug.LogWarning("MUSIC FUNDAMENTAL-SOURCE: SetDebugFundamentalOverride(None) ignored — None is not a valid fundamental.");
                }
                return;
            }

            debugFundamentalOverride = note.Value;
            ApplyMasterFundamental(note.Value);
            if (debugAllowFundamentalLockLogs)
            {
                Debug.Log($"MUSIC FUNDAMENTAL-SOURCE: Debug override set to {note.Value} (overrides active source {activeFundamentalSource}).");
            }
        }
        else
        {
            if (!debugFundamentalOverride.HasValue)
            {
                return;
            }

            debugFundamentalOverride = null;
            NoteName preferred = preferredFundamentalBySource[activeFundamentalSource];
            if (preferred != NoteName.None)
            {
                ApplyMasterFundamental(preferred);
            }
            if (debugAllowFundamentalLockLogs)
            {
                Debug.Log($"MUSIC FUNDAMENTAL-SOURCE: Debug override cleared — restored active source {activeFundamentalSource} preferred={preferred}.");
            }
        }
    }
}
