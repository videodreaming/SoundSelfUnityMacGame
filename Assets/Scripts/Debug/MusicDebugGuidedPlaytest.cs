#if UNITY_EDITOR
using System.Collections;
using SoundSelf.Sequence;
using UnityEngine;

/// <summary>
/// Editor-only guided subjective playtest for Stage 1 (Director queue) + Stage 2 (binaural gating).
/// Started from <see cref="MusicDebugHarness"/> (G key) while parked in <see cref="StageVariant.Playground_Debug"/>.
/// </summary>
public class MusicDebugGuidedPlaytest : MonoBehaviour
{
    const string Prefix = "[MusicDebugHarness] GUIDED";
    const float DirectorTimerSeconds = 8f;
    const float BinauralLerpWaitSeconds = 32f;
    const string ShadowSoundWorldTarget = "Shadow";
    /// <summary>Used when auto-shuffle already landed on Shadow so the director step is a real world→world change.</summary>
    const string ShadowTestBaselineWorld = "SonoFlore";

    [SerializeField] private MusicDebugHarness harness;
    [SerializeField] private Sequencer sequencer;
    [SerializeField] private Director director;

    Coroutine _run;

    void Awake()
    {
        if (harness == null) harness = GetComponent<MusicDebugHarness>();
        if (sequencer == null) sequencer = FindObjectOfType<Sequencer>();
        if (director == null) director = FindObjectOfType<Director>();
    }

    public bool IsRunning => _run != null;

    public void ToggleRun()
    {
        if (_run != null)
        {
            StopCoroutine(_run);
            _run = null;
            Debug.Log(Prefix + " STOPPED by user (G again). Resume manually or restart from Playground_Debug.");
            return;
        }

        _run = StartCoroutine(RunStage1And2Playtest());
    }

    IEnumerator RunStage1And2Playtest()
    {
        try
        {
            yield return RunStage1And2PlaytestCore();
        }
        finally
        {
            _run = null;
            LogCaps("GUIDED PLAYTEST FINISHED — paste Console logs (filter: MusicDebugHarness | Director Queue | Binaural) and your subjective notes.");
        }
    }

    IEnumerator RunStage1And2PlaytestCore()
    {
        if (sequencer == null || director == null || harness == null)
        {
            Debug.LogError(Prefix + " Missing harness / sequencer / director.");
            yield break;
        }

        var imitone = sequencer.imitoneVoiceInterpreter;
        var ms = MusicSystem1.instance;
        var shuffler = sequencer.worldShuffler;

        LogCaps("=== STAGE 1 + 2 SUBJECTIVE PLAYTEST START ===");
        LogCaps("PUT ON HEADPHONES NOW. Console filter: MusicDebugHarness | Director Queue | Binaural");
        LogCaps("YOU MUST BE PARKED IN Playground_Debug (DebugSequence). G again = abort.");
        LogCaps("MIC / gameOn SHOULD BE ON — hum or tone when instructed.");

        yield return WaitSeconds(3f);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);

        // --- Stage 2: binaural audible on Playground ---
        LogCaps("--- STAGE 2 (BINAURAL) — PLAYGROUND ---");
        LogCaps("LISTEN: BINURAL BEATS SHOULD FADE IN OVER ~30 SECONDS (TARGET binauralBase=100 binauralOut=100).");
        LogCaps("YOU MAY HEAR A GENTLE PULSE / HUM UNDER THE BED — NOT LOUD, BUT PRESENT.");

        for (int i = 0; i < 6; i++)
        {
            yield return WaitSeconds(5f);
            if (i == 2 || i == 5)
                harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        }

        LogCaps("NOW PRESS P — CONFIRM binauralBase≈100 binauralAtt=off binauralOut≈100 (may still be lerping).");
        yield return WaitSeconds(8f);

        // --- Stage 1: Director repro (ActivateEntireQueueOnNextTone self-retention) ---
        LogCaps("--- STAGE 1 (DIRECTOR) — REPRO: ActivateEntireQueueOnNextTone ---");
        LogCaps("QUEUING HARNESS REPRO (0.05s TIMER). WAIT ~10 SECONDS — DO NOT TONE YET.");

        director.ClearQueueOfType("MusicDebugHarness_Repro");
        harness.ExecuteAction(MusicDebugHarnessAction.DirectorQueueRepro);
        Debug.Log(Prefix + " Queue: " + director.FormatQueueContents());

        yield return WaitSeconds(10f);

        LogCaps("NOW DO A CLEAR, SUSTAINED TONE (2–3 SECONDS).");
        LogCaps("WATCH CONSOLE: YOU SHOULD SEE 'Director repro action executed' AND 'Activating entire queue with tone'.");
        LogCaps("YOU SHOULD NOT SEE 'Queue activation requested but queue is empty' (THAT WAS THE OLD BUG).");

        yield return WaitForTone(imitone, 60f);
        yield return WaitSeconds(2f);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        Debug.Log(Prefix + " Queue after repro tone: " + director.FormatQueueContents());

        yield return WaitSeconds(3f);

        // --- Stage 1: whole-queue soundscape change (Shadow) ---
        if (ms != null)
        {
            LogCaps("--- STAGE 1 (DIRECTOR) — BASELINE BEFORE SHADOW (KILL SAME-VALUE NO-OP) ---");
            EnsureBaselineBeforeSoundWorldTest(shuffler, ms, ShadowSoundWorldTarget, ShadowTestBaselineWorld);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
            LogCaps("LISTEN BRIEFLY — YOU SHOULD BE ON " + ShadowTestBaselineWorld.ToUpperInvariant()
                + " (OR ANOTHER NON-SHADOW WORLD), NOT SHADOW YET.");
            yield return WaitSeconds(5f);

            LogCaps("--- STAGE 1 (DIRECTOR) — SOUNDSCAPE CHANGE ON TONE (Shadow) ---");
            LogCaps("QUEUING Soundscape→Shadow (" + DirectorTimerSeconds + "s, ActivateEntireQueueOnNextTone). WAIT FOR TIMER…");
            LogCaps("THIS IS A REAL CHANGE FROM THE BASELINE ABOVE — NOT A NO-OP IF SWITCH WORKS.");

            director.ClearQueueOfType("Soundscape");
            int shadowId = director.AddActionToQueue(
                ms.Action_SetSoundscape(ShadowSoundWorldTarget),
                "Soundscape",
                true,
                false,
                DirectorTimerSeconds,
                DirectorActivationBehavior.ActivateEntireQueueOnNextTone,
                DirectorExclusivityBehavior.ReplaceAllOfType);
            Debug.Log(Prefix + " Queued Shadow id=" + shadowId + " " + director.FormatQueueContents());

            yield return WaitSeconds(DirectorTimerSeconds + 2f);

            LogCaps("NOW TONE AGAIN — YOU SHOULD NOTICE THE SOUND BED SHIFT TO SHADOW (DARKER / DIFFERENT TIMBRE).");
            LogCaps("CONSOLE SHOULD LOG Soundscape SET AND Director queue activation.");

            yield return WaitForTone(imitone, 60f);
            yield return WaitSeconds(2f);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        }

        yield return WaitSeconds(3f);

        // --- Stage 1: ActivateThisActionOnNextTone (transition flourish) ---
        LogCaps("--- STAGE 1 (DIRECTOR) — ActivateThisActionOnNextTone (transition sound) ---");
        LogCaps("QUEUING PlayTransitionSound (" + DirectorTimerSeconds + "s). WAIT FOR TIMER…");

        director.ClearQueueOfType("TransitionSound");
        int transId = director.AddActionToQueue(
            director.Action_PlayTransitionSound(),
            "TransitionSound",
            true,
            false,
            DirectorTimerSeconds,
            DirectorActivationBehavior.ActivateThisActionOnNextTone,
            DirectorExclusivityBehavior.ReplaceAllOfType);
        Debug.Log(Prefix + " Queued TransitionSound id=" + transId);

        yield return WaitSeconds(DirectorTimerSeconds + 2f);

        LogCaps("NOW TONE — YOU SHOULD HEAR A SHORT TRANSITION / WHOOSH ON TONE START (AUDIO FLOURISH).");

        yield return WaitForTone(imitone, 60f);
        yield return WaitSeconds(2f);

        // --- Stage 1: whole-queue shuffle (SoundscapeShuffle + ColorWorldShuffle pattern) ---
        if (shuffler != null)
        {
            LogCaps("--- STAGE 1 (DIRECTOR) — SHUFFLE ON TONE (SoundscapeShuffle) ---");
            LogCaps("QUEUING ShuffleWorldsNow via ActivateEntireQueueOnNextTone (" + DirectorTimerSeconds + "s). WAIT FOR TIMER…");

            director.ClearQueueOfType("SoundscapeShuffle");
            director.ClearQueueOfType("ColorWorldShuffle");
            int shuffleId = director.AddActionToQueue(
                () =>
                {
                    Debug.Log(Prefix + " ShuffleWorldsNow executing from director queue.");
                    shuffler.ShuffleWorldsNow();
                },
                "SoundscapeShuffle",
                true,
                false,
                DirectorTimerSeconds,
                DirectorActivationBehavior.ActivateEntireQueueOnNextTone,
                DirectorExclusivityBehavior.PreferShorterTimeRemaining);
            Debug.Log(Prefix + " Queued SoundscapeShuffle id=" + shuffleId + " " + director.FormatQueueContents());

            yield return WaitSeconds(DirectorTimerSeconds + 2f);

            LogCaps("NOW TONE — YOU SHOULD NOTICE SOUND WORLD + LIGHT COLOR CHANGE TOGETHER (SHUFFLE).");
            LogCaps("CONSOLE: 'ShuffleWorldsNow executing' AND queue activation logs — NOT empty queue.");

            yield return WaitForTone(imitone, 60f);
            yield return WaitSeconds(2f);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        }

        yield return WaitSeconds(3f);

        // --- Stage 2: optional attenuation on playground (MusicLoopSilent) ---
        if (ms != null)
        {
            LogCaps("--- STAGE 2 (BINAURAL) — ATTENUATION ON PLAYGROUND (OPTIONAL) ---");
            LogCaps("PRESS ] NOW (MusicLoopSilent) — OVER ~30s binauralOut SHOULD DIP FROM ~100 TOWARD ~70 (100×0.7).");
            LogCaps("LISTEN: BEATS GET SLIGHTLY QUIETER; binauralAtt SHOULD BECOME on. SKIP IF YOU WANT TO RUSH TO STAGE EXIT.");

            yield return WaitSeconds(15f);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
            yield return WaitSeconds(20f);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);

            LogCaps("PRESS [ TWICE TO RETURN TO A SOUND WORLD (Freeplay) — binauralOut SHOULD CREEP BACK TOWARD ~100.");
            yield return WaitSeconds(25f);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        }

        // --- Stage 2: leave Playground → Linear_Nature (mute) ---
        LogCaps("--- STAGE 2 (BINAURAL) — LEAVE PLAYGROUND ---");
        LogCaps("PRESS E NOW (EndThisSequenceStage) — ADVANCES TO Linear_Nature IN DebugSequence.");
        LogCaps("LISTEN: BINURAL BEATS SHOULD FADE OUT OVER ~30 SECONDS (TARGET binauralBase=0 binauralOut=0).");
        LogCaps("gameOn MAY TURN OFF ON Linear_Nature — THAT IS EXPECTED.");

        yield return WaitSeconds(20f);

        LogCaps("WAITING " + BinauralLerpWaitSeconds + "s FOR BINURAL FADE-OUT — LISTEN FOR FADE…");

        for (int i = 0; i < 6; i++)
        {
            yield return WaitSeconds(5f);
            if (i == 2 || i == 5)
                harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        }

        LogCaps("PRESS P — CONFIRM binauralBase=0 binauralAtt=on binauralOut=0 (OR VERY CLOSE).");
        LogCaps("SUBJECTIVE: ARE THE BINURAL BEATS GONE / INAUDIBLE NOW?");

        yield return WaitSeconds(10f);

        LogCaps("OPTIONAL: PRESS E AGAIN TO ADVANCE (MusicPlaylist) — BINURAL SHOULD STAY MUTED (binauralOut=0).");
        LogCaps("=== END OF SCRIPT — REPORT WHAT YOU HEARD VS WHAT LOGS SAY ===");
    }

    static void LogCaps(string message)
    {
        Debug.Log(Prefix + " >>> " + message.ToUpperInvariant() + " <<<");
    }

    /// <summary>
    /// Auto-shuffle during the repro step can land on Shadow before we queue Shadow via Director.
    /// Force a known non-Shadow baseline so the next step is a real world change (Step 0 in SOUNDWORLD_SWITCH_NOT_AUDIBLE.md).
    /// </summary>
    static void EnsureBaselineBeforeSoundWorldTest(
        WorldShuffler shuffler,
        MusicSystem1 ms,
        string targetWorld,
        string baselineWorld)
    {
        if (ms == null)
            return;

        string current = shuffler != null ? shuffler.EditorCurrentSoundscape : "";
        if (string.IsNullOrEmpty(current))
            current = "(none)";

        if (string.Equals(current, targetWorld, System.StringComparison.Ordinal))
        {
            LogCaps("WAS ALREADY ON " + targetWorld.ToUpperInvariant()
                + " (LIKELY FROM AUTO-SHUFFLE) — SETTING " + baselineWorld.ToUpperInvariant()
                + " FIRST SO SHADOW IS A REAL CHANGE.");
            ms.SetSoundWorld(baselineWorld);
            Debug.Log(Prefix + " Forced baseline " + baselineWorld + " (was " + targetWorld + ") before Shadow director test.");
            return;
        }

        Debug.Log(Prefix + " Baseline soundscape '" + current + "' — director Shadow step should change to " + targetWorld + ".");
        LogCaps("CURRENT BASELINE: " + current.ToUpperInvariant() + " — NEXT STEP QUEUES SHADOW.");
    }

    static IEnumerator WaitSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>Waits until the player sustains a confident tone, or times out.</summary>
    static IEnumerator WaitForTone(ImitoneVoiceIntepreter imitone, float timeoutSeconds)
    {
        if (imitone == null)
        {
            Debug.LogWarning(Prefix + " No imitoneVoiceInterpreter — cannot detect tone; waiting " + timeoutSeconds + "s.");
            yield return WaitSeconds(timeoutSeconds);
            yield break;
        }

        float elapsed = 0f;
        float toneHeld = 0f;
        const float holdRequired = 0.4f;

        while (elapsed < timeoutSeconds)
        {
            if (imitone.toneActiveConfident)
            {
                toneHeld += Time.deltaTime;
                if (toneHeld >= holdRequired)
                {
                    Debug.Log(Prefix + " Tone detected (held " + holdRequired + "s).");
                    yield break;
                }
            }
            else
            {
                toneHeld = 0f;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning(Prefix + " Tone wait timed out after " + timeoutSeconds + "s — continue anyway; director may still fire on a late tone.");
    }
}
#endif
