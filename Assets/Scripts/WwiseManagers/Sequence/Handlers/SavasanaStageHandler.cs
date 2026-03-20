using UnityEngine;
using ConversionUtilities;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Savasana stage: locks fundamental, activates director queue, plays Ascending Closing VO. Completes immediately (savasana plays to end).</summary>
    public class SavasanaStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _hasEntered;

        public StageType StageType => StageType.Savasana;

        public bool IsComplete => true;

        public SavasanaStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        /// <param name="variant">Reserved for future use. Will select savasana type when we add back Preparation, Integration, and PS_Descending modes.</param>
        public void Enter(string variant)
        {
            if (_hasEntered)
            {
                Debug.LogError("SavasanaStageHandler: ENTER() CALLED AGAIN BEFORE EXIT(). THE SEQUENCE IS LIKELY BROKEN. STOP THE SEQUENCE BEFORE STARTING IT AGAIN.");
                return;
            }
            _hasEntered = true;

            if (_sequencer == null)
            {
                Debug.LogError("SavasanaStageHandler: Sequencer is null. _countdownToSavasana not set (Sequencer unavailable).");
                _hasEntered = false;
                return;
            }
            if (_sequencer.director == null)
            {
                Debug.LogError("SavasanaStageHandler: director is null. _countdownToSavasana not set. Current value: " + _sequencer._countdownToSavasana);
                _hasEntered = false;
                return;
            }
            if (_sequencer.wwiseVOManager == null)
            {
                Debug.LogError("SavasanaStageHandler: wwiseVOManager is null. _countdownToSavasana not set. Current value: " + _sequencer._countdownToSavasana);
                _hasEntered = false;
                return;
            }
            if (MusicSystem1.instance == null)
            {
                Debug.LogError("SavasanaStageHandler: MusicSystem1.instance is null. _countdownToSavasana not set. Current value: " + _sequencer._countdownToSavasana);
                _hasEntered = false;
                return;
            }

            Debug.Log("SavasanaStageHandler: Enter - running Savasana (Ascending Closing)");

            MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);
            _sequencer.director.ActivateQueue(15f);
            _sequencer.director.Disable();
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.MusicLoopSilent);
            _sequencer.wwiseVOManager.PlayAscendingClosing();
            _sequencer._countdownToSavasana = -1.0f;
        }

        public void Exit()
        {
            _hasEntered = false;  // Allow re-enter on sequence restart
        }
    }
}
