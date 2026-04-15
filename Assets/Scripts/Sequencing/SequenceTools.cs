using System;
using System.Text;

namespace SoundSelf.Sequence
{
    /// <summary>Commands dispatched from Wwise (and similar) into the sequence. Handlers declare WatchesSequenceCommand; Sequencer dispatches via HandleSequenceCommand.</summary>
    public enum SequenceCommand
    {
        StartTutorial,
        FirstVocalizationStart,
        StartInteractive,   // Opening, Tutorial → transition to Playground
        Break_Tests,        // Tutorial → ends naturally (end of "Keep going" instruction)
        TutorialPassed,     // Tutorial → success path or explicit stop; advances off Tutorial stage
        WaitForButton,      // SetMenu stage → user presses button to play music
        MusicTrackEnding,
        ThematicSavasana_End,  // Savasana → closing teaching phase done (optional; Savasana may complete immediately)
        CueStopInteractive,     // Wwise Cue_Stop_Interactive — Playground/Savasana Standard → FrozenFreeplay
        CueStopInteractive3m,   // Wwise Cue_Stop_Interactive_3m — Savasana PS Ascending only
        CueSilentMeditationStart // Wwise Cue_SilentMeditation_Start — Savasana PS Ascending only (Jaya VO)
    }

    public enum StageType
    {
        Calibration,   // Pre-sequence; user hasn't started
        Opening,
        Tutorial,     // Optional; Protocol Stacks skips
        Playground,
        Savasana,
        SetMenu,
        MusicPlaylist,
        Inquiry,      // Asks player how they are feeling; records answer. Stub until implementation.
        End,          // End stage; happens at the end of a sequence.
        LinearAudio,  // Multi-purpose linear audio stage. Stub until implementation.
        StartCountdown // Computes session countdown pair from variant, then BeginCountdownPair() on TimeTrackerScript.
    }

    /// <summary>
    /// Single enum for all stage variants. Prefix matches <see cref="StageType"/> (Set Menu → Menu, Music Playlist → Playlist,
    /// Start Countdown → Countdown, Linear Audio → Linear).
    /// Integer values are explicit and stable for Unity serialization — add new variants with unused ids (e.g. 100+) or append at the end without renumbering existing.
    /// </summary>
    public enum StageVariant
    {
        None = 0,
        Calibration_Default = 1,
        Opening_PS_Ascending = 2,
        Opening_Preparation = 3,
        Opening_SkillsTraining = 4,
        Opening_Integration = 5,
        Tutorial_Long = 6,
        Tutorial_Short = 7,
        Playground_Standard = 8,
        Playground_SkipAscending = 9,
        Playground_Ascending = 10,
        Savasana_Standard = 11,
        Savasana_PsAscending = 12,
        Menu_Default = 13,
        Playlist_Default = 14,
        Inquiry_Default = 15,
        End_Default = 16,
        Linear_Default = 17,
        Linear_Nature = 18,
        Countdown_25m_Simple = 19,
        Countdown_25m_WithSavasana = 20,
        Countdown_40m_Simple = 21,
        Countdown_40m_WithSavasana = 22,
        Countdown_60m_Simple = 23,
        Countdown_60m_WithSavasana = 24,
        Countdown_ClosingDuration = 25,
        Countdown_StopCountdowns = 26,
    }

    [System.Serializable]
    public struct SequenceStage
    {
        public StageType type;
        public StageVariant variant;
    }

    /// <summary>Shared helpers for <see cref="IStageHandler"/> implementations (e.g. variant string normalization).</summary>
    public static class StageHandlerHelpers
    {
        /// <summary>Trims, lowercases, then removes whitespace, underscores, and hyphens so variant keys match across assets and code (e.g. <c>Closing Duration</c> and <c>ClosingDuration</c>).</summary>
        public static string NormalizeVariant(string variant)
        {
            if (string.IsNullOrWhiteSpace(variant))
                return string.Empty;
            var sb = new StringBuilder(variant.Length);
            foreach (char c in variant.Trim().ToLowerInvariant())
            {
                if (char.IsWhiteSpace(c) || c == '_' || c == '-')
                    continue;
                sb.Append(c);
            }
            return sb.Length == 0 ? string.Empty : sb.ToString();
        }
    }
}
