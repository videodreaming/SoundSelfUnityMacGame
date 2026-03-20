using System.Collections;
using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Handles the Playground stage: countdown-based timed steps. Completes when _countdownToSavasana reaches 0.</summary>
    public class PlaygroundStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _isComplete;
        private Coroutine _playgroundCoroutine;

        public StageType StageType => StageType.Playground;

        public bool IsComplete => _isComplete;

        public PlaygroundStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public void Enter(string variant)
        {
            _isComplete = false;
            if (_sequencer == null)
            {
                Debug.LogError("PlaygroundStageHandler: Sequencer is null.");
                _isComplete = true;
                return;
            }
            if (_playgroundCoroutine != null)
            {
                Debug.LogError("PlaygroundStageHandler: ENTER() CALLED WHILE COROUTINE IS ALREADY RUNNING. THE SEQUENCE IS LIKELY BROKEN. STOP THE SEQUENCE BEFORE STARTING IT AGAIN.");
                return;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _playgroundCoroutine = _sequencer.StartCoroutine(PlaygroundCoroutine());
        }

        public void Exit()
        {
            if (_playgroundCoroutine != null && _sequencer != null)
            {
                _sequencer.StopCoroutine(_playgroundCoroutine);
                _playgroundCoroutine = null;
            }
        }

        private IEnumerator PlaygroundCoroutine()
        {
            Debug.Log("PlaygroundStageHandler: STARTED - Current countdown: " + _sequencer._countdownToSavasana + " seconds (" + (_sequencer._countdownToSavasana / 60f) + " minutes)");

            if (_sequencer.calibrationMenu == null)
            {
                Debug.LogWarning("PlaygroundStageHandler: calibrationMenu is null - countdown will not decrement.");
            }
            else if (_sequencer.calibrationMenu.startedExperience != true)
            {
                Debug.LogWarning("PlaygroundStageHandler: Experience not started yet (calibrationMenu.startedExperience != true). The sequence will not progress.");
            }

            if (_sequencer.worldShuffler == null || _sequencer.director == null || _sequencer.lightControl == null)
            {
                Debug.LogError("PlaygroundStageHandler: worldShuffler, director, or lightControl is null. Cannot run Playground stage.");
                _playgroundCoroutine = null;
                _isComplete = true;
                yield break;
            }

            float step1Threshold = 20f * 60f; // 1200 seconds = 20 minutes

            Debug.Log("PlaygroundStageHandler: Waiting for countdown to reach " + step1Threshold + " seconds (20 minutes). Current: " + _sequencer._countdownToSavasana + " (or call ForceSequenceAdvance() to skip)");
            int frameCount = 0;
            while (_sequencer._countdownToSavasana > step1Threshold && !_sequencer.ForceSequenceAdvanceRequested)
            {
                frameCount++;
                if (frameCount % 600 == 0)
                {
                    Debug.Log("PlaygroundStageHandler: Still waiting. Countdown: " + _sequencer._countdownToSavasana + " seconds (" + (_sequencer._countdownToSavasana / 60f) + " minutes). Threshold: " + step1Threshold);
                }
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: Threshold reached! Countdown: " + _sequencer._countdownToSavasana + " seconds. Proceeding to Step 1.");
            Debug.Log("PlaygroundStageHandler: Step 1 - Starting interactive music (20 minutes or less remaining)");
            MusicSystem1.instance.StopBreathworkCycle();
            MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
            MusicSystem1.instance.SetSoundscape("ShiftingEarth");
            _sequencer.StartPlayground(false, false, true);

            _sequencer.worldShuffler.ExcludeSoundscape("Shadow");

            while (_sequencer._countdownToSavasana > (19f * 60f - 30f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: Step 2");
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("SitarAmbience"), "Soundscape", true, false, 180.0f, 2, 2);

            while (_sequencer._countdownToSavasana > (16f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("Shadow"), "Soundscape", true, false, 180.0f, 2, 2);
            _sequencer.director.AddActionToQueue(_sequencer.lightControl.Action_SetPreferredColorWorld("Blue", 8.0f), "ColorWorld", false, true, 180.0f, 1, 2);
            Debug.Log("PlaygroundStageHandler: Step 4");

            while (_sequencer._countdownToSavasana > (13f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("PinkNoiseAtmosphere"), "Soundscape", true, false, 180.0f, 2, 2);
            Debug.Log("PlaygroundStageHandler: Step 5");

            while (_sequencer._countdownToSavasana > (12f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            _sequencer.worldShuffler.BeginShuffle(false);

            while (_sequencer._countdownToSavasana > (10f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            Debug.Log("PlaygroundStageHandler: Step 6");
            _sequencer.worldShuffler.ExcludeSoundscape("SonoFlore");

            while (_sequencer._countdownToSavasana > (4f * 60f) && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;
            Debug.Log("PlaygroundStageHandler: Step 8");
            _sequencer.worldShuffler.StopShuffle();
            _sequencer.worldShuffler.CloseSoundscapeQueue();
            _sequencer.director.AddActionToQueue(MusicSystem1.instance.Action_SetSoundscape("SonoFlore"), "Soundscape", true, false, 180.0f, 2, 2);

            while (_sequencer._countdownToSavasana > 60f && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            while (_sequencer._countdownToSavasana > 0f && !_sequencer.ForceSequenceAdvanceRequested)
            {
                yield return null;
            }
            _sequencer.ForceSequenceAdvanceRequested = false;

            Debug.Log("PlaygroundStageHandler: Countdown reached 0. Playground complete.");
            _playgroundCoroutine = null;
            _isComplete = true;
        }
    }
}
