using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>
    /// One-shot stage: resolves session countdown from <paramref name="variant"/>, configures the tracker, calls
    /// <see cref="TimeTrackerScript.BeginCountdownPair"/> (ClosingDuration uses <see cref="TimeTrackerScript.ConfiguredFullAtLastConfigure"/> / <see cref="TimeTrackerScript.SetConfiguredFullBaselineOnly"/> to preserve session HUD baseline; both live timers set to post-unguided duration and the pair is restarted),
    /// or <see cref="TimeTrackerScript.ForceSetBothCountdownsAndStop"/> for <c>StopCountdowns</c>,
    /// then marks complete.
    /// </summary>
    public class StartCountdownStageHandler : IStageHandler
    {
        private readonly Sequencer _sequencer;
        private bool _hasEntered;

        public StartCountdownStageHandler(Sequencer sequencer)
        {
            _sequencer = sequencer;
        }

        public StageType StageType => StageType.StartCountdown;

        public bool IsComplete { get; private set; }

        public bool WatchesSequenceCommand(SequenceCommand sequenceCommand) =>
            sequenceCommand == SequenceCommand.EndThisSequenceStage;

        public void ExecuteSequenceCommand(SequenceCommand sequenceCommand)
        {
            if (sequenceCommand == SequenceCommand.EndThisSequenceStage)
                MarkComplete();
        }

        public void Enter(StageVariant variant)
        {
            if (_hasEntered)
            {
                Debug.LogWarning("StartCountdownStageHandler: Enter() called again before Exit(). Skipping.");
                return;
            }
            _hasEntered = true;
            IsComplete = false;

            // Stage D: tear down the linear ambient bed; StartCountdown belongs to the closing/final-cue phase. Idempotent.
            if (MusicSystemLinear.instance != null)
                MusicSystemLinear.instance.Stop();

            var tt = TimeTrackerScript.instance;
            if (tt == null)
            {
                Debug.LogError("StartCountdownStageHandler: TimeTrackerScript.instance is null. Cannot start countdown.");
                MarkComplete();
                return;
            }
            if (!tt.SessionTimingInitializedFromCsv)
            {
                Debug.LogError("StartCountdownStageHandler: Session timing was not initialized from CSV (CSVLoader.TimeLeftInitializations did not complete). " +
                               "Load sessions.csv / session_params and ensure TimeLeftInitializations runs before the StartCountdown stage. Proceeding anyway, but expect unexpected behavior.");
            }

            if (variant == StageVariant.None)
            {
                Debug.LogError("StartCountdownStageHandler: variant is None. Pick a Countdown_* variant.");
                MarkComplete();
                return;
            }

            float closing = tt.TotalTimeOfPostUnguidedVocalizationContent;

            if (variant == StageVariant.Countdown_ClosingDuration)
            {
                float closingSecs = Mathf.Max(0f, closing);
                if (closingSecs <= 0f)
                {
                    Debug.LogError("StartCountdownStageHandler: ClosingDuration requires TotalTimeOfPostUnguidedVocalizationContent > 0 from CSV / tracker. Countdown not started.");
                    MarkComplete();
                    return;
                }

                float preservedFullBaseline = tt.ConfiguredFullAtLastConfigure;
                float fullBefore = tt.CountdownFull;
                float sectionBefore = tt.CountdownThisSection;
                tt.ConfigureCountdownPair(closingSecs, closingSecs);
                tt.BeginCountdownPair();
                if (preservedFullBaseline > 0f)
                    tt.SetConfiguredFullBaselineOnly(preservedFullBaseline);
                Debug.Log("StartCountdownStageHandler: ClosingDuration — live [CountdownThisSection]/[CountdownFull] both " + closingSecs + " s (post-unguided). Before: ThisSection=" + sectionBefore + " Full=" + fullBefore + "." +
                          (preservedFullBaseline > 0f ? " Full baseline for HUD restored to " + preservedFullBaseline + " s." : ""));
                if (Mathf.Abs(tt.CountdownFull - closingSecs) > 10f || Mathf.Abs(tt.CountdownThisSection - closingSecs) > 10f)
                    Debug.LogWarning("StartCountdownStageHandler: ClosingDuration — applied pair differs from expected by more than 10s.");
                MarkComplete();
                return;
            }

            if (variant == StageVariant.Countdown_StopCountdowns)
            {
                tt.ForceSetBothCountdownsAndStop(0f, 0f);
                MarkComplete();
                return;
            }

            if (!TryGetCountdownPair(variant, closing, out float thisSection, out float full))
            {
                Debug.LogError("StartCountdownStageHandler: Could not resolve countdown pair for variant '" + variant + "'. Countdown not started.");
                MarkComplete();
                return;
            }

            tt.ConfigureCountdownPair(thisSection, full);
            tt.BeginCountdownPair();
            Debug.Log("StartCountdownStageHandler: BeginCountdownPair [CountdownThisSection]=" + thisSection + " s, [CountdownFull]=" + full + " s for variant '" + variant + "'.");
            MarkComplete();
        }

        public void Exit()
        {
            _hasEntered = false;
        }

        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
            Debug.Log("StartCountdownStageHandler: Stage complete.");
        }

        /// <summary>
        /// <b>Nm simple:</b> both timers N×60. <b>Nm with savasana:</b> main N×60, full N×60 + post-unguided when post-unguided &gt; 0.
        /// </summary>
        /// <returns><c>false</c> if <paramref name="variant"/> is not a duration countdown variant or the pair is invalid.</returns>
        private static bool TryGetCountdownPair(StageVariant variant, float postUnguidedSeconds, out float countdownThisSection, out float countdownFull)
        {
            countdownThisSection = 0f;
            countdownFull = 0f;

            switch (variant)
            {
                case StageVariant.Countdown_60m_Simple:
                    countdownThisSection = 3600f;
                    countdownFull = 3600f;
                    break;
                case StageVariant.Countdown_60m_WithSavasana:
                    SetPairWithSavasana(3600f, postUnguidedSeconds, variant, out countdownThisSection, out countdownFull);
                    break;
                case StageVariant.Countdown_40m_Simple:
                    countdownThisSection = 2400f;
                    countdownFull = 2400f;
                    break;
                case StageVariant.Countdown_40m_WithSavasana:
                    SetPairWithSavasana(2400f, postUnguidedSeconds, variant, out countdownThisSection, out countdownFull);
                    break;
                case StageVariant.Countdown_25m_Simple:
                    countdownThisSection = 1500f;
                    countdownFull = 1500f;
                    break;
                case StageVariant.Countdown_25m_WithSavasana:
                    SetPairWithSavasana(1500f, postUnguidedSeconds, variant, out countdownThisSection, out countdownFull);
                    break;
                default:
                    Debug.LogError("StartCountdownStageHandler: Unknown countdown variant '" + variant + "'. Use a Countdown_* duration variant, Countdown_ClosingDuration, or Countdown_StopCountdowns.");
                    return false;
            }

            return countdownFull > 0f && countdownThisSection >= 0f;
        }

        private static void SetPairWithSavasana(float baseSeconds, float closing, StageVariant variantLabel, out float countdownThisSection, out float countdownFull)
        {
            if (closing <= 0f)
            {
                Debug.LogWarning("StartCountdownStageHandler: '" + variantLabel + "' (with savasana) — post-unguided duration 0; [CountdownFull] equals [CountdownThisSection].");
                countdownThisSection = baseSeconds;
                countdownFull = baseSeconds;
            }
            else if (closing > baseSeconds)
            {
                Debug.LogWarning("StartCountdownStageHandler: '" + variantLabel + "' (with savasana) — post-unguided duration > baseSeconds; setting [CountdownFull] equals [CountdownThisSection].");
                countdownThisSection = baseSeconds;
                countdownFull = baseSeconds;
            }
            else
            {
                countdownThisSection = baseSeconds - closing;
                countdownFull = baseSeconds;
            }
        }
    }
}
