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

    [System.Serializable]
    public struct SequenceStage
    {
        public StageType type;
        public string variant;  // e.g. "PS_Ascending", "Preparation_Long", null = default
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
