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
        WaitForButton,      // WaitForInput → user presses button to play music
        MusicTrackEnding,
        ThematicSavasana_End  // Savasana → closing teaching phase done (optional; Savasana may complete immediately)
    }

    public enum StageType
    {
        Calibration,   // Pre-sequence; user hasn't started
        Opening,
        Tutorial,     // Optional; Protocol Stacks skips
        Playground,
        Savasana,
        WaitForInput,
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
}
