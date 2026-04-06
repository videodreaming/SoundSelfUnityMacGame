using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>
    /// One-shot stage: resolves session countdown seconds from <paramref name="variant"/>, configures the tracker, calls
    /// <see cref="TimeTrackerScript.BeginCountdown"/>, then marks complete. See Phase 2 table in TimeAndCountdownRefactorPlan.
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

        public void Enter(string variant)
        {
            if (_hasEntered)
            {
                Debug.LogWarning("StartCountdownStageHandler: Enter() called again before Exit(). Skipping.");
                return;
            }
            _hasEntered = true;
            IsComplete = false;

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

            string key = NormalizeVariant(variant);
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("StartCountdownStageHandler: variant is null or empty. Expected e.g. 60m, 40m minus closing, ClosingDuration.");
                MarkComplete();
                return;
            }

            float closing = tt.TotalTimeOfPostUnguidedVocalizationContent;
            float seconds;

            if (key == "closingduration")
            {
                float before = tt.CountdownSeconds;
                float closingSecs = Mathf.Max(0f, closing);
                if (closingSecs <= 0f)
                {
                    Debug.LogError("StartCountdownStageHandler: ClosingDuration requires TotalTimeOfPostUnguidedVocalizationContent > 0 from CSV / tracker. Countdown not started.");
                    MarkComplete();
                    return;
                }

                tt.BeginCountdown(closingSecs);
                float after = tt.CountdownSeconds;
                Debug.Log("StartCountdownStageHandler: ClosingDuration countdown before=" + before + " after=" + after + " (post-unguided content=" + closing + ").");
                if (Mathf.Abs(after - closingSecs) > 10f)
                    Debug.LogWarning("StartCountdownStageHandler: ClosingDuration — applied countdown differs from post-unguided content by more than 10s (check ConfigureCountdown / BeginCountdown).");
                MarkComplete();
                return;
            }

            seconds = ResolveSecondsForVariant(key, closing);
            if (seconds <= 0f)
            {
                Debug.LogError("StartCountdownStageHandler: Resolved invalid seconds for variant '" + variant + "'. Countdown not started.");
                MarkComplete();
                return;
            }

            tt.BeginCountdown(seconds);
            Debug.Log("StartCountdownStageHandler: Started countdown at " + seconds + " s for variant '" + variant + "' (normalized '" + key + "').");
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

        private static string NormalizeVariant(string variant)
        {
            if (string.IsNullOrWhiteSpace(variant))
                return string.Empty;
            string s = variant.Trim().ToLowerInvariant();
            string[] parts = s.Split(new[] { ' ', '\t', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        /// <summary>Variant key is normalized: e.g. <c>60m minus closing</c>, <c>closingduration</c>.</summary>
        private static float ResolveSecondsForVariant(string key, float totalTimeOfPostUnguidedVocalizationContent)
        {
            switch (key)
            {
                case "60m":
                    return 3600f;
                case "60m minus closing":
                    return SubtractClosing(3600f, totalTimeOfPostUnguidedVocalizationContent, key);
                case "40m":
                    return 2400f;
                case "40m minus closing":
                    return SubtractClosing(2400f, totalTimeOfPostUnguidedVocalizationContent, key);
                case "25m":
                    return 1500f;
                case "25m minus closing":
                    return SubtractClosing(1500f, totalTimeOfPostUnguidedVocalizationContent, key);
                default:
                    Debug.LogError("StartCountdownStageHandler: Unknown variant key '" + key + "'. Supported: 60m, 60m minus closing, 40m, 40m minus closing, 25m, 25m minus closing, ClosingDuration.");
                    return -1f;
            }
        }

        private static float SubtractClosing(float baseSeconds, float closing, string variantLabel)
        {
            if (closing <= 0f)
            {
                Debug.LogError("StartCountdownStageHandler: '" + variantLabel + "' requires TotalTimeOfPostUnguidedVocalizationContent > 0 (got " + closing + ").");
                return -1f;
            }
            float v = baseSeconds - closing;
            if (v <= 0f)
            {
                Debug.LogError("StartCountdownStageHandler: '" + variantLabel + "' computed " + v + " s (base " + baseSeconds + " - closing " + closing + ").");
                return -1f;
            }
            return v;
        }
    }
}
