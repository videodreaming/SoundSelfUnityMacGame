using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Opening stage: light init, Wwise opening sequence, and AVS program. Watches for Cue_StartInteractive; when it fires, marks complete and SequenceRunner advances on next poll.</summary>
    public class OpeningStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _hasEntered;

        public StageType StageType => StageType.Opening;

        public bool IsComplete { get; private set; }

        public OpeningStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public void Enter(string variant)
        {
            if (_hasEntered)
            {
                Debug.LogWarning("OpeningStageHandler: Enter() called again before Exit(). Skipping to prevent double-play.");
                return;
            }
            _hasEntered = true;
            IsComplete = false;

            if (_sequencer == null)
            {
                Debug.LogError("OpeningStageHandler: Sequencer is null.");
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            if (_sequencer.lightControl == null)
            {
                Debug.LogError("OpeningStageHandler: lightControl is null. Cannot initialize lights.");
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            if (_sequencer.wwiseVOManager == null)
            {
                Debug.LogError("OpeningStageHandler: wwiseVOManager is null. Cannot play opening sequence.");
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            try
            {
                _sequencer.lightControl.LightSettingsInitialization(5.0f);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("OpeningStageHandler: LightSettingsInitialization failed: " + ex.Message);
                Debug.LogError("Stack trace: " + ex.StackTrace);
                _hasEntered = false;
                IsComplete = true;
                return;
            }

            string openingVariant = string.IsNullOrEmpty(variant) ? "PS_Ascending" : variant;

            if (_sequencer.csvLoader != null && _sequencer.csvLoader.subGameMode == "Descending" && (openingVariant == "PS_Ascending" || openingVariant == "Ascending"))
                Debug.LogWarning("Ascending sequence is playing but subGameMode is Descending. Descending sequence not yet implemented.");
            
            _sequencer.wwiseVOManager.PlayOpeningSequence(openingVariant);
            Debug.Log("OpeningStageHandler: Playing opening sequence: " + openingVariant);

            _sequencer.StartOpeningAVSProgram();
        }

        public void Exit()
        {
            _hasEntered = false;  // Allow re-enter on sequence restart
            // AVS coroutine continues running in parallel; no cleanup needed when advancing to Playground
        }


        public bool WatchesCue(CueType cue) => cue == CueType.StartInteractive;

        public void NotifyCue(CueType cue)
        {
            if (cue == CueType.StartInteractive)
                MarkComplete();
        }

        /// <summary>Marks the stage complete; SequenceRunner advances on next poll. Idempotent.</summary>
        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
        }
    }
}
