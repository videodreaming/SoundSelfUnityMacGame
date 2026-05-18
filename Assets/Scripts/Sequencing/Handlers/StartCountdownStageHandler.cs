using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>
    /// One-shot stage: resolves session countdown from <paramref name="variant"/>, configures the tracker, calls
    /// <see cref="TimeTrackerScript.BeginCountdownPair"/> (ClosingDuration uses <see cref="TimeTrackerScript.ConfiguredFullAtLastConfigure"/> / <see cref="TimeTrackerScript.SetConfiguredFullBaselineOnly"/> to preserve session HUD baseline; both live timers set to post-unguided duration and the pair is restarted),
    /// or <see cref="TimeTrackerScript.ForceSetBothCountdownsAndStop"/> for <c>StopCountdowns</c>,
    /// then marks complete. Each <see cref="Enter"/> applies the variant (handler is shared across sequences; do not skip re-entry when <see cref="IStageHandler.Exit"/> was not invoked).
    /// </summary>
    public class StartCountdownStageHandler : IStageHandler
    {
        private const string LogPrefix = "[StartCountdown]";

        private readonly Sequencer _sequencer;

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
            // Do not gate Enter on a sticky flag: the same handler instance is reused for every StartCountdown stage
            // in every sequence for the session. SequenceRunner often advances away from this stage without calling
            // Exit() on it (e.g. linear AdvanceToNextStage), and StartSequence() only ForceExits the *current* handler
            // when swapping to a nested playlist — so Exit() may never run before a later Enter. Skipping Enter left
            // TimeTracker on an old session countdown (e.g. 40m) while the new definition expected 60m.
            IsComplete = false;

            Debug.Log($"{LogPrefix} Enter variant={variant}.");

            if (UIManager.Instance != null)
                UIManager.Instance.EnableSkipButton(false, null);

            var tt = TimeTrackerScript.instance;
            if (tt == null)
            {
                Debug.LogError($"{LogPrefix} TimeTrackerScript.instance is null. Cannot start countdown.");
                MarkComplete();
                return;
            }

            LogTrackerCountdowns($"{LogPrefix} TimeTracker state before this stage", tt);

            if (!tt.SessionTimingInitializedFromCsv)
            {
                Debug.LogError($"{LogPrefix} Session timing was not initialized from CSV (CSVLoader.TimeLeftInitializations did not complete). " +
                               "Load sessions.csv / session_params and ensure TimeLeftInitializations runs before the StartCountdown stage. Proceeding anyway, but expect unexpected behavior.");
            }

            if (variant == StageVariant.None)
            {
                Debug.LogError($"{LogPrefix} variant is None. Pick a Countdown_* variant.");
                MarkComplete();
                return;
            }

            float closing = tt.TotalTimeOfPostUnguidedVocalizationContent;

            if (variant == StageVariant.Countdown_ClosingDuration)
            {
                float closingSecs = Mathf.Max(0f, closing);
                if (closingSecs <= 0f)
                {
                    Debug.LogError($"{LogPrefix} Countdown_ClosingDuration: TotalTimeOfPostUnguidedVocalizationContent is {closing} s — must be > 0. Countdown not started.");
                    MarkComplete();
                    return;
                }

                float preservedFullBaseline = tt.ConfiguredFullAtLastConfigure;
                float fullBefore = tt.CountdownFull;
                float sectionBefore = tt.CountdownThisSection;
                Debug.Log(
                    $"{LogPrefix} Countdown_ClosingDuration: applying post-unguided pair both = {FormatMmSs(closingSecs)} " +
                    $"(raw closing from CSV/tracker {closing:F1} s). Preserved full baseline for HUD (if any) = {preservedFullBaseline:F1} s. " +
                    $"Before: ThisSection={FormatMmSs(sectionBefore)} Full={FormatMmSs(fullBefore)}.");
                tt.ConfigureCountdownPair(closingSecs, closingSecs);
                tt.BeginCountdownPair();
                if (preservedFullBaseline > 0f)
                    tt.SetConfiguredFullBaselineOnly(preservedFullBaseline);
                LogTrackerCountdowns($"{LogPrefix} Countdown_ClosingDuration: after ConfigureCountdownPair + BeginCountdownPair (+ baseline restore)", tt);
                if (Mathf.Abs(tt.CountdownFull - closingSecs) > 10f || Mathf.Abs(tt.CountdownThisSection - closingSecs) > 10f)
                    Debug.LogWarning($"{LogPrefix} Countdown_ClosingDuration: live values differ from expected closing by >10s (check TimeTracker warnings).");
                MarkComplete();
                return;
            }

            if (variant == StageVariant.Countdown_StopCountdowns)
            {
                Debug.Log($"{LogPrefix} Countdown_StopCountdowns: forcing both countdowns to 0 and stopping ticking.");
                tt.ForceSetBothCountdownsAndStop(0f, 0f);
                LogTrackerCountdowns($"{LogPrefix} Countdown_StopCountdowns: after ForceSetBothCountdownsAndStop", tt);
                MarkComplete();
                return;
            }

            Debug.Log($"{LogPrefix} Duration variant path: TotalTimeOfPostUnguidedVocalizationContent (for WithSavasana math) = {closing:F1} s {FormatMmSs(closing)}.");

            if (!TryGetCountdownPair(variant, closing, out float thisSection, out float full))
            {
                Debug.LogError($"{LogPrefix} Could not resolve countdown pair for variant '{variant}'. Countdown not started.");
                MarkComplete();
                return;
            }

            Debug.Log(
                $"{LogPrefix} Resolved configured pair for '{variant}': [CountdownThisSection] target = {FormatMmSs(thisSection)} " +
                $"| [CountdownFull] target = {FormatMmSs(full)}.");
            tt.ConfigureCountdownPair(thisSection, full);
            tt.BeginCountdownPair();
            LogTrackerCountdowns($"{LogPrefix} After ConfigureCountdownPair + BeginCountdownPair (live values)", tt);
            MarkComplete();
        }

        public void Exit()
        {
        }

        private void MarkComplete()
        {
            if (IsComplete) return;
            IsComplete = true;
            Debug.Log($"{LogPrefix} Stage complete (IsComplete=true).");
        }

        private static string FormatMmSs(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m}:{s:D2} ({seconds:F0}s)";
        }

        private static void LogTrackerCountdowns(string label, TimeTrackerScript tt)
        {
            if (tt == null)
            {
                Debug.Log(label + " (TimeTracker is null)");
                return;
            }

            Debug.Log(
                $"{label}: [CountdownThisSection]={FormatMmSs(tt.CountdownThisSection)} " +
                $"[CountdownFull]={FormatMmSs(tt.CountdownFull)} " +
                $"IsCountdownRunning={tt.IsCountdownRunning} " +
                $"ConfiguredFullAtLastConfigure={tt.ConfiguredFullAtLastConfigure:F0}s");
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
                    Debug.LogError($"{LogPrefix} Unknown countdown variant '{variant}'. Use a Countdown_* duration variant, Countdown_ClosingDuration, or Countdown_StopCountdowns.");
                    return false;
            }

            return countdownFull > 0f && countdownThisSection >= 0f;
        }

        private static void SetPairWithSavasana(float baseSeconds, float closing, StageVariant variantLabel, out float countdownThisSection, out float countdownFull)
        {
            if (closing <= 0f)
            {
                Debug.LogWarning($"{LogPrefix} '{variantLabel}' (with savasana) — post-unguided duration 0; [CountdownFull] equals [CountdownThisSection] ({FormatMmSs(baseSeconds)}).");
                countdownThisSection = baseSeconds;
                countdownFull = baseSeconds;
            }
            else if (closing > baseSeconds)
            {
                Debug.LogWarning($"{LogPrefix} '{variantLabel}' (with savasana) — post-unguided {FormatMmSs(closing)} > base {FormatMmSs(baseSeconds)}; [CountdownFull] equals [CountdownThisSection].");
                countdownThisSection = baseSeconds;
                countdownFull = baseSeconds;
            }
            else
            {
                countdownThisSection = baseSeconds - closing;
                countdownFull = baseSeconds;
                Debug.Log(
                    $"{LogPrefix} '{variantLabel}' WithSavasana: base={FormatMmSs(baseSeconds)} postUnguided={FormatMmSs(closing)} " +
                    $"→ [CountdownThisSection]={FormatMmSs(countdownThisSection)} [CountdownFull]={FormatMmSs(countdownFull)}.");
            }
        }
    }
}
