using System;
using System.Text;

namespace SoundSelf.Sequence
{
    /// <summary>Commands dispatched from Wwise (and similar) into the sequence. Handlers declare WatchesSequenceCommand; Sequencer dispatches via HandleSequenceCommand. UI "End This Sequence Stage" uses <see cref="EndThisSequenceStage"/>.</summary>
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
        CueSilentMeditationStart, // Wwise Cue_SilentMeditation_Start — Savasana PS Ascending only (Jaya VO)
        /// <summary>Wwise <c>Cue_ClosingGoodbye_End</c> — Savasana stage completes (goodbye VO segment end).</summary>
        CueClosingGoodbyeEnd,
        /// <summary>UI "End This Sequence Stage" — <see cref="Sequencer.HandleSequenceCommand"/>; active stage handler should <c>MarkComplete()</c>.</summary>
        EndThisSequenceStage,
        /// <summary>Wwise <c>Cue_Microphone_ON</c> inside <c>Play_Calibration_Sequence</c> — calibration handler enables Imitone game-on.</summary>
        CalibrationMicrophoneOn,
        /// <summary>Wwise <c>Cue_Microphone_OFF</c> inside <c>Play_Calibration_Sequence</c> — calibration handler disables Imitone game-on.</summary>
        CalibrationMicrophoneOff,
        /// <summary>Wwise <c>Cue_AVS_Calibration_Start</c> — calibration handler drives AVS color/strobe on for the lights step.</summary>
        CalibrationAvsStart,
        /// <summary>Wwise <c>Cue_AVS_Calibration_End</c> — calibration handler restores light settings.</summary>
        CalibrationAvsEnd,
        /// <summary>Wwise <c>Cue_Calibration_Instruction_ON</c> — instruction line started; may unlock pending Next on non-Start steps when that ON is the destination portion’s first line.</summary>
        CalibrationInstructionVoStarted,
        /// <summary>Wwise <c>Cue_Calibration_Instruction_OFF</c> — instruction line ended; may unlock pending Next.</summary>
        CalibrationInstructionVoEnded,
        /// <summary>Wwise <c>Cue_Calibration_Next</c> — still forwarded for logging; pending Next unlock does <b>not</b> use this (see <c>CalibrationStageHandler</c>).</summary>
        CalibrationPoliteNext,
        /// <summary>Wwise <c>Cue_Calibration_Intro_End</c> — fired near the end of the Intro (Start) segment; auto-advances the Start step regardless of whether the user pressed Next.</summary>
        CalibrationIntroEnded
    }

    public enum StageType
    {
        Calibration,   // Pre-sequence; user hasn't started
        Opening,
        Tutorial,     // Optional; Adjunctive skips
        Playground,
        Savasana,
        SetMenu,
        MusicPlaylist,
        Inquiry,      // Asks player how they are feeling; records answer. Stub until implementation.
        End,          // End stage; happens at the end of a sequence.
        LinearAudio,  // Multi-purpose linear audio stage. Stub until implementation.
        StartCountdown, // Computes session countdown pair from variant, then BeginCountdownPair() on TimeTrackerScript.
        /// <summary>Scripted branch / glue (e.g. dual-stage section boundaries). Appended to avoid renumbering prior <see cref="StageType"/> values in serialized assets.</summary>
        Code
    }

    /// <summary>
    /// Single enum for all stage variants. Prefix matches <see cref="StageType"/> (Set Menu → Menu, Music Playlist → Playlist,
    /// Start Countdown → Countdown, Linear Audio → Linear, Code → Code).
    /// Integer values are explicit and stable for Unity serialization — add new variants with unused ids (e.g. 100+) or append at the end without renumbering existing.
    /// </summary>
    public enum StageVariant
    {
        None = 0,
        Calibration_Default = 1,
        Opening_PS_Ascending = 2,
        Opening_Preparation = 3,
        Opening_Sonoflore = 4,
        Opening_Activation = 5,
        Tutorial_Long = 6,
        Tutorial_Short = 7,
        Playground_Standard = 8,
        Playground_SkipAscending = 9,
        Playground_Ascending = 10,
        Playground_SkipStandard = 30,
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
        Menu_Ps_InteractiveOrMusic = 27,
        Playlist_40m = 28,
        Playlist_60m = 29,
        Menu_Welcome_PreCalibration = 31,
        /// <summary>Stub: alternate calibration content (e.g. album-specific copy); same <see cref="StageType.Calibration"/> stage, different handler behavior when implemented.</summary>
        Calibration_Album = 32,
        /// <summary>Stub: calibration without vibro step; same <see cref="StageType.Calibration"/> stage.</summary>
        Calibration_NoVibro = 33,
        /// <summary>
        /// <see cref="StageType.Code"/>: dual-stage section boundary — routes to trailing
        /// <see cref="StageVariant.Menu_Ps_InteractiveOrMusic"/> when <c>dualstageStage==1</c> or
        /// <see cref="StageVariant.End_Default"/> when <c>dualstageStage==2</c> (see <see cref="CodeStageHandler"/>).
        /// </summary>
        Code_Dualstage_SectionEnd = 34,
        /// <summary><see cref="StageType.SetMenu"/>: Choice Album screen (<see cref="UIManager.SetChoiceScreen"/>(<see cref="ChoiceScreen.Album"/>)).</summary>
        Menu_AlbumChoice = 35,
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
