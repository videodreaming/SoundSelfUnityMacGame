namespace SoundSelf.Sequence
{
    /// <summary>Wwise cues that stages may watch for completion. Handlers declare WatchesCue; Sequencer dispatches via HandleCue.</summary>
    public enum CueType
    {
        StartInteractive,   // Opening, Tutorial → transition to Playground
        Break_Tests,        // Tutorial → ends naturally (end of "Keep going" instruction)
        WaitForButton,      // WaitForInput → user presses button to play music
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
        LinearAudio   // Multi-purpose linear audio stage. Stub until implementation.
    }

    [System.Serializable]
    public struct SequenceStage
    {
        public StageType type;
        public string variant;  // e.g. "PS_Ascending", "Preparation_Long", null = default
    }
}
