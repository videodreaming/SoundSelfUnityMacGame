#if UNITY_EDITOR
using System.Collections;
using SoundSelf.Sequence;
using UnityEngine;

/// <summary>
/// Editor-only guided subjective playtest. Runs three parts in one coroutine (G key) while parked in
/// <see cref="StageVariant.Playground_Debug"/>:
/// <list type="number">
/// <item>Part A — sound-world cycle audit (all 4 worlds distinct; SOUNDWORLD_SWITCH_NOT_AUDIBLE follow-up).</item>
/// <item>Part B — MusicLoops → Silence hygiene (no loop bleed under a world).</item>
/// <item>Part C — Stage 1 (Director queue) + Stage 2 (binaural gating).</item>
/// </list>
/// </summary>
public class MusicDebugGuidedPlaytest : MonoBehaviour
{
    const string Prefix = "[B457 MusicDebugHarness] GUIDED";
    const string AdvanceHintKeyboard = "PRESS SPACE OR RETURN WHEN READY.";
    const string StepNavHint = "SPACE/RETURN/→ = NEXT · ←/BACKSPACE = PREVIOUS.";
    const float AdvanceTimeoutSeconds = 600f;
    const float ToneReleaseSeconds = 0.25f;
    const float ToneHoldRequiredSeconds = 0.4f;
    const float DirectorTimerSeconds = 8f;
    const string ShadowSoundWorldTarget = "Shadow";
    /// <summary>Used when auto-shuffle already landed on Shadow so the director step is a real world→world change.</summary>
    const string ShadowTestBaselineWorld = "SonoFlore";

    // ===== TEMPORARY — Stage 9 playtest scaffolding (drives simulated sung changes, no microphone). =====
    // Tuning for the goblin steps that call MusicSystem1.DebugSimulateSungFundamentalChange. REMOVE with that
    // method at the Stage 9 final commit (see BLOCKS_4_5_7_BUILD_CHECKLIST.md "Temporary playtest scaffolding").
    /// <summary>Semitones each simulated sung change moves the master (non-zero ⇒ always a real, audible move).</summary>
    const int SimSemitoneStep = 4;
    /// <summary>Step 2 anti-clutter burst: how many simulated changes…</summary>
    const int RapidChangeBurstCount = 5;
    /// <summary>…and how far apart. 5 changes × 1s spans ~4s &lt; the 5s gate, so after the window is cleared exactly ONE flourish (the first) should fire and the other four are suppressed.</summary>
    const float RapidChangeSpacingSeconds = 1.0f;
    /// <summary>Timer-baked wait (just over the 5s flourish gate) before the Step 2 burst so the first burst change is guaranteed to flourish — makes the "1 flourish, rest suppressed" result deterministic regardless of how long Step 1 took.</summary>
    const float FlourishWindowClearSeconds = 5.5f;

    /// <summary>Part A: every sound world, set in turn, listening for a distinct + clean switch.</summary>
    static readonly string[] WorldAuditCycle = { "SonoFlore", "Shadow", "Gentle", "Shruti" };
    /// <summary>Part B: a music loop set first, then a world, to confirm the loop bed is silenced (no bleed).</summary>
    const string MusicLoopForHygiene = "ShiftingEarth";
    const string HygieneWorldTarget = "Shadow";

    /// <summary>
    /// Stage 3b.0: every soundscape (4 worlds + 3 loops) in alternating world↔loop order so each step is an A/B
    /// transition for per-soundscape SoundscapeMonitoring tuning (see BLOCKS_4_5_7_PLAN §3b.0).
    /// </summary>
    static readonly (string soundscape, bool isLoop)[] MicMixerTuneOrder =
    {
        ("SonoFlore", false),
        ("ShiftingEarth", true),
        ("Shadow", false),
        ("SitarAmbience", true),
        ("Gentle", false),
        ("PinkNoiseAtmosphere", true),
        ("Shruti", false),
    };

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

        _run = StartCoroutine(RunGuidedPlaytest());
    }

    /// <summary>Stage 9a goblin playtest only (F key): fundamental change ↔ visual flourish pairing, anti-clutter, disabled-bypass.</summary>
    public void ToggleRunGoblin()
    {
        if (_run != null)
        {
            StopCoroutine(_run);
            _run = null;
            if (director != null)
                director.Enable(); // re-enable in case we aborted mid disabled-bypass step
            Debug.Log(Prefix + " GOBLIN STOPPED by user (F again).");
            return;
        }

        _run = StartCoroutine(RunGoblinPlaytest());
    }

    IEnumerator RunGoblinPlaytest()
    {
        try
        {
            LogCaps("STAGE 9A GOBLIN PLAYTEST START — FUNDAMENTAL CHANGE ↔ FLOURISH. CONSOLE FILTER: B457. F AGAIN = ABORT.");
            yield return RunGoblinCore();
        }
        finally
        {
            if (director != null)
                director.Enable();
            _run = null;
            LogCaps("GOBLIN PLAYTEST FINISHED — paste Console logs (filter: B457) and your subjective notes (did the key shift pair a light flourish?).");
        }
    }

    // === Stage 9a: long-test fundamental change pairs a visual flourish; anti-clutter; disabled-bypass ===
    IEnumerator RunGoblinCore()
    {
        if (sequencer == null || director == null || harness == null)
        {
            Debug.LogError(Prefix + " Missing harness / sequencer / director.");
            yield break;
        }

        var ms = MusicSystem1.instance;
        if (ms == null)
        {
            Debug.LogError(Prefix + " MusicSystem1.instance null — cannot run goblin playtest.");
            yield break;
        }

        var imitone = sequencer.imitoneVoiceInterpreter;
        var shuffler = sequencer.worldShuffler;

        LogPartBegin(
            "9A",
            "FUNDAMENTAL CHANGE ↔ VISUAL FLOURISH (STAGE 9A GOBLIN)",
            "An InputDriven fundamental change routes through the Director so it is a COUNTED audio event — so it pairs exactly one VISUAL flourish (color-world shift + FX wave). A dedicated 5s window prevents flourish spam. While the Director is disabled the change still applies directly (no flourish). NO SINGING NEEDED — this test DRIVES simulated sung changes (MusicSystem1.DebugSimulateSungFundamentalChange) down the exact same path a real voice change uses, so the key shifts are deterministic + measurable; you just watch the lights and the Console.",
            "By ear+eye: (1) a simulated key shift is accompanied by a light flourish; (2) a rapid burst of changes does NOT flash a flourish every time (≈5s gate); (3) with the Director disabled the key still shifts, with NO flourish. Console (B457): DEBUG-SIM → FUND-SLOT set=… (immediate) → FUND-SLOT cleared → applying → FUND-COMMIT → DIRECTOR-FLOURISH add=visual; suppressed lines during the burst; FUND-SLOT … director disabled → raw apply while disabled.");

        // Stop auto-shuffle so soundscape changes don't muddy the fundamental↔flourish read; put us on a voice-tracked world.
        if (shuffler != null && shuffler.shuffling)
            shuffler.StopShuffle();
        ms.SetSoundWorld("SonoFlore");
        director.Enable();
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        LogCaps("PUT ON HEADPHONES. SONOFLORE SET (INPUTDRIVEN ACTIVE), DIRECTOR ENABLED. NO NEED TO SING — STEPS DRIVE SIMULATED KEY CHANGES; JUST WATCH + ADVANCE. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        // ============================ STEP 1 of 3 — key shift pairs a flourish ============================
        LogCaps("──────── STEP 1 OF 3 — A KEY SHIFT SHOULD PAIR WITH A VISUAL FLOURISH ────────");
        LogCaps("WHAT'S ABOUT TO HAPPEN: I WILL SIMULATE *ONE* IMMEDIATE (LONG-TEST) SUNG CHANGE (+" + SimSemitoneStep + " SEMITONES). IT SHOULD MOVE THE KEY *AND* TRIGGER ONE VISUAL FLOURISH (COLOR-WORLD SHIFT + FX WAVE).");
        LogCaps("GET READY TO WATCH THE LIGHTS. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogCaps("▶ FIRING ONE IMMEDIATE CHANGE NOW — WATCH THE LIGHTS.");
        ms.DebugSimulateSungFundamentalChange(SimSemitoneStep, immediate: true);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        LogCaps("DID IT HAPPEN? EXPECT: KEY SHIFTED + EXACTLY ONE FLOURISH. CONSOLE: DEBUG-SIM → FUND-SLOT set=… (IMMEDIATE → ACTIVATING) → FUND-SLOT CLEARED → APPLYING → FUND-COMMIT → DIRECTOR-FLOURISH add=visual.");
        LogCaps("WHEN YOU'VE NOTED WHAT YOU SAW/HEARD, " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        // ============================ STEP 2 of 3 — no flourish spam (timer-driven) ============================
        LogCaps("──────── STEP 2 OF 3 — RAPID CHANGES MUST NOT SPAM FLOURISHES (5s GATE) ────────");
        LogCaps("WHAT'S ABOUT TO HAPPEN: FIRST I WAIT ~" + FlourishWindowClearSeconds + "s (BY TIMER) TO CLEAR THE 5s FLOURISH WINDOW, THEN AUTO-FIRE " + RapidChangeBurstCount + " IMMEDIATE CHANGES ONE EVERY " + RapidChangeSpacingSeconds + "s (ALSO BY TIMER — NO KEY PRESSES DURING THE BURST).");
        LogCaps("EXPECTED RESULT: THE KEY CHANGES ALL " + RapidChangeBurstCount + " TIMES, BUT ONLY THE *FIRST* PAIRS A FLOURISH; THE OTHER " + (RapidChangeBurstCount - 1) + " ARE SUPPRESSED (WITHIN 5s).");
        LogCaps("GET READY TO WATCH THE LIGHTS THROUGH THE WHOLE BURST. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        yield return WaitWithCountdown(FlourishWindowClearSeconds, "CLEARING 5s FLOURISH WINDOW BEFORE BURST");
        for (int i = 0; i < RapidChangeBurstCount; i++)
        {
            LogCaps("▶ BURST CHANGE " + (i + 1) + " OF " + RapidChangeBurstCount + (i == 0 ? " (THIS ONE SHOULD FLOURISH)" : " (THIS ONE SHOULD BE SUPPRESSED)"));
            ms.DebugSimulateSungFundamentalChange(SimSemitoneStep, immediate: true);
            if (i < RapidChangeBurstCount - 1)
                yield return WaitWithCountdown(RapidChangeSpacingSeconds, "NEXT BURST CHANGE IN");
        }
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        LogCaps("DID IT HAPPEN? EXPECT: " + RapidChangeBurstCount + "× FUND-COMMIT, BUT ONLY 1× DIRECTOR-FLOURISH add=visual AND " + (RapidChangeBurstCount - 1) + "× DIRECTOR-FLOURISH suppressed. (KEY MOVED EVERY TIME; LIGHTS FLOURISHED ONCE.)");
        LogCaps("WHEN YOU'VE NOTED WHAT YOU SAW/HEARD, " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        // ============================ STEP 3 of 3 — disabled-bypass (no flourish) ============================
        LogCaps("──────── STEP 3 OF 3 — DISABLED DIRECTOR STILL SHIFTS THE KEY, WITH NO FLOURISH ────────");
        LogCaps("WHAT'S ABOUT TO HAPPEN: I DISABLE THE DIRECTOR (AS DURING SAVASANA / PLAYGROUND-OFF), THEN SIMULATE ONE IMMEDIATE CHANGE. THE KEY SHOULD STILL SHIFT (APPLIED DIRECTLY) WITH **NO** FLOURISH.");
        LogCaps("GET READY TO WATCH THE LIGHTS (EXPECT NO FLOURISH). " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        director.Disable();
        LogCaps("▶ DIRECTOR DISABLED — FIRING ONE IMMEDIATE CHANGE NOW. WATCH THE LIGHTS (EXPECT NONE).");
        ms.DebugSimulateSungFundamentalChange(SimSemitoneStep, immediate: true);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        LogCaps("DID IT HAPPEN? EXPECT: KEY SHIFTED, NO FLOURISH. CONSOLE: FUND-SLOT set=… (IMMEDIATE, DIRECTOR DISABLED → RAW APPLY) → FUND-COMMIT, AND *NO* DIRECTOR-FLOURISH LINE.");
        LogCaps("WHEN YOU'VE NOTED WHAT YOU SAW/HEARD, " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);
        director.Enable();
        Debug.Log(Prefix + " Director re-enabled after disabled-bypass step.");

        LogPartComplete("9A", "Report: (1) did one change pair one flourish? (2) did the burst give " + RapidChangeBurstCount + " key moves but only 1 flourish? (3) did the disabled change shift the key with no flourish? Paste the full B457 console too.");
    }

    IEnumerator RunGuidedPlaytest()
    {
        try
        {
            LogCaps("GUIDED PLAYTEST START — A → B → 3b.1 ADSR → 3b.0 MIC A/B → C. CONSOLE FILTER: B457. G AGAIN = ABORT.");
            yield return RunWorldAndLoopAuditCore();
            yield return RunMonitoringAdsrTuneCore();
            yield return RunMicMixerHeadroomTuneCore();
            yield return RunStage1And2PlaytestCore();
        }
        finally
        {
            _run = null;
            LogCaps("GUIDED PLAYTEST FINISHED — paste Console logs (single filter: B457) and your subjective notes.");
        }
    }

    // === Part A (world cycle) + Part B (MusicLoops → Silence hygiene) ===
    IEnumerator RunWorldAndLoopAuditCore()
    {
        if (harness == null || sequencer == null)
        {
            Debug.LogError(Prefix + " Missing harness / sequencer.");
            yield break;
        }

        var ms = MusicSystem1.instance;
        if (ms == null)
        {
            Debug.LogWarning(Prefix + " MusicSystem1.instance null — skipping world/loop audit (Parts A/B).");
            yield break;
        }

        var imitone = sequencer.imitoneVoiceInterpreter;

        LogPartBegin(
            "A",
            "SOUND WORLD CYCLE (BLOCK 4 / SET SOUND WORLD)",
            "Each of the four sound worlds (SonoFlore, Shadow, Gentle, Shruti) is set in turn via SetSoundWorld — you hear one bed at a time, switching cleanly in Wwise.",
            "By ear: all four sound clearly different; no stacked worlds, no old world lingering after a switch.");
        LogCaps("PUT ON HEADPHONES. TONE WHILE LISTENING IS FINE — ONLY SPACE/RETURN ADVANCES. WHEN READY FOR THE FIRST WORLD, " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        foreach (var world in WorldAuditCycle)
        {
            ms.SetSoundWorld(world);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
            LogCaps("NOW LISTENING: " + world.ToUpperInvariant()
                + " IS ACTIVE. TONE WHILE YOU LISTEN — ONLY SPACE/RETURN ADVANCES WHEN DONE (DISTINCT + CLEAN). " + AdvanceHintKeyboard);
            yield return WaitForAdvance(imitone);
        }

        LogPartComplete("A", "Note any world that sounded the same, muddy, or layered.");
        yield return WaitForAdvance(imitone);

        yield return BeginNextPart(
            imitone,
            "B",
            "MUSICLOOPS → SILENCE HYGIENE (BLOCK 4)",
            "A music loop bed is started, then a sound world is set — the loop must be silenced (MusicLoops_Switch → Silence) so only the world is audible, not the loop underneath.",
            "By ear: loop bed audible on step 1; after world switch the loop is fully gone — no bleed under the world.");

        ms.SetMusicLoop(MusicLoopForHygiene);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        LogCaps("LOOP SET → " + MusicLoopForHygiene.ToUpperInvariant()
            + ". YOU SHOULD HEAR THE LOOP BED. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        ms.SetSoundWorld(HygieneWorldTarget);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        LogCaps("WORLD SET → " + HygieneWorldTarget.ToUpperInvariant()
            + ". CRITICAL: LOOP BED MUST BE SILENT — ONLY THE WORLD, NO LOOP UNDERNEATH. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogPartComplete("B", "Report any loop bleed you still heard under the world.");
        yield return WaitForAdvance(imitone);
    }

    IEnumerator RunMicMixerHeadroomTuneCore()
    {
        if (harness == null || sequencer == null)
        {
            Debug.LogError(Prefix + " Missing harness / sequencer.");
            yield break;
        }

        var ms = MusicSystem1.instance;
        if (ms == null)
        {
            Debug.LogWarning(Prefix + " MusicSystem1.instance null — skipping 3b.0 MicMixer A/B.");
            yield break;
        }

        var imitone = sequencer.imitoneVoiceInterpreter;
        var dvm = UnityEngine.Object.FindObjectOfType<DirectVoiceMonitoring>();

        yield return BeginNextPart(
            imitone,
            "3b.0",
            "MIC MIXER A/B (BLOCK 8 — PER-SOUNDSCAPE MONITORING)",
            "Walk every soundscape (4 worlds + 3 loops) in alternating world↔loop order so you can set each by ear. Each soundscape lerps its own SoundscapeMonitoring dB on the MicMixer bus (loops seeded +8, worlds 0, over 10s).",
            "By ear: each soundscape's mic monitoring feels usable; loops ≈ worlds (baked: worlds 0 dB, loops +3 dB). State line: micMixerSumDb + SoundscapeMonitoringDb. Wait ~10s after each change before judging.");

        LogCaps("PUT ON HEADPHONES. TONE WHILE LISTENING. THIS WALKS EVERY SOUNDSCAPE IN ORDER, ALTERNATING WORLD↔LOOP. STEP " + StepNavHint + " — GO BACK AND FORTH TO A/B EACH PAIR. ALLOW ~10s AFTER EACH STEP FOR THE LERP. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        var nav = new StepNav[1];
        int idx = 0;
        while (idx < MicMixerTuneOrder.Length)
        {
            var (soundscape, isLoop) = MicMixerTuneOrder[idx];
            ms.SetSoundscape(soundscape);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
            if (dvm != null)
            {
                Debug.Log(Prefix + " 3b.0 step " + (idx + 1) + "/" + MicMixerTuneOrder.Length + " "
                    + (isLoop ? "loop=" : "world=") + soundscape
                    + " soundscapeDb=" + dvm.GetSoundscapeMonitoringDb(soundscape).ToString("F1")
                    + " sum=" + dvm.GetMicMixerVolumeSumDb().ToString("F1") + " dB (expect SoundscapeMonitoring ≈ that after 10s lerp).");
            }
            LogCaps((isLoop ? "LOOP → " : "WORLD → ") + soundscape.ToUpperInvariant()
                + " [STEP " + (idx + 1) + "/" + MicMixerTuneOrder.Length + "]. LISTEN — BAKED PER-SOUNDSCAPE dB. WAIT ~10s. "
                + StepNavHint);
            yield return WaitForStepNav(imitone, nav);
            idx += nav[0] == StepNav.Back ? -1 : 1;
            if (idx < 0) idx = 0;
        }

        LogPartComplete("3b.0", "All soundscapes visited. Step back/forward (←/→) to re-check any pair; paste B457 state lines.");
        yield return WaitForAdvance(imitone);
    }

    // === Stage 3b.1: stacked monitoring ADSR tune (see BLOCKS_4_5_7_PLAN §3b.1) ===
    IEnumerator RunMonitoringAdsrTuneCore()
    {
        if (harness == null || sequencer == null)
        {
            Debug.LogError(Prefix + " Missing harness / sequencer.");
            yield break;
        }

        var ms = MusicSystem1.instance;
        if (ms == null)
        {
            Debug.LogError(Prefix + " MusicSystem1.instance null — cannot run 3b.1.");
            yield break;
        }

        var imitone = sequencer.imitoneVoiceInterpreter;
        var dvm = UnityEngine.Object.FindObjectOfType<DirectVoiceMonitoring>();

        yield return BeginNextPart(
            imitone,
            "3b.1",
            "MONITORING ADSR (BLOCK 8 — VOICE ENVELOPE)",
            "Headphone mic monitoring now follows a stacked ADSR (attack on confident tone → decay to sustain over the charge runway → release when you take the tone off). Replaces the old sluggish chantPresence. Tune by ear in the ONE box on DirectVoiceMonitoring.",
            "Attack not sluggish; decay settles to sustain (not endless creep); release on finger-off (both gates off) not harsh; stacking sums but never clips. Baked: sustain 0.4, BoardFader low -18 dB. Press P anytime → adsrSum / adsrVoices; Inspector shows ADSR phase telemetry.");

        // Interactive world so toning drives the envelope.
        ms.SetSoundscape("SonoFlore");
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        LogCaps("PUT ON HEADPHONES. SONOFLORE SET. PRESS P ANYTIME FOR adsrSum / adsrVoices. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogCaps("STEP 1 — PLAYFUL RISE: TONE SHORT NOTES (modeMeditativeLerp ≈ 0). ATTACK SHOULD FEEL SNAPPY (chantLerpFast). " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogCaps("STEP 2 — MEDITATIVE RISE (OPTIONAL): IF ABSORPTION > 0.25, TONE AGAIN — RISE SHOULD FEEL SOFTER/ROUNDER OVER ~60s (blend of fast+slow). SKIP IF STILL PLAYFUL. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogCaps("STEP 3 — DECAY→SUSTAIN: HOLD A LONG TONE. LEVEL RISES THEN SETTLES TO SUSTAIN (baked 0.4). WATCH debugMonitoringAdsrCurrentBurstPhase. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogCaps("STEP 4 — RELEASE: STOP TONING. MONITORING SHOULD EASE DOWN (not snap). BoardFader floor baked -18 dB. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogCaps("STEP 5 — STACKING: TONE, RELEASE THE KEY (let confident drop), THEN TONE AGAIN BEFORE IT FULLY FADES. NEW ATTACK STACKS ON THE TAIL — adsrVoices > 1, adsrSum CLAMPS AT 1. NO HARSH JUMP. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogPartComplete("3b.1", "Attack/decay/release/stacking feel right; paste B457 state lines (adsrSum / adsrVoices / burst phase).");
        yield return WaitForAdvance(imitone);
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

        yield return BeginNextPart(
            imitone,
            "C",
            "DIRECTOR QUEUE + BINAURAL STAGE GATING (STAGES 1 + 2)",
            "Stage 1: Director items expire and fire on tone (whole-queue activation, Shadow soundscape, transition flourish, shuffle) — no empty-queue bug. Stage 2: binaural fades in on Playground, optional attenuation, fades out after E → Linear_Nature.",
            "Logs: 'Activating entire queue with tone' and repro/shuffle execute — NOT 'queue is empty'. By ear: binaural present on Playground (~70–100), gone on Linear (~0); optional MusicLoopSilent → binauralAtt=on, binauralOut≈70.");
        ClearPlaygroundShuffleQueue(director);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);

        // --- Part C / Stage 2: binaural audible on Playground ---
        LogCaps("--- PART C / STAGE 2: BINAURAL ON PLAYGROUND ---");
        LogCaps("LISTEN: BINAURAL BEATS SHOULD FADE IN OVER ~30 SECONDS (TARGET binauralBase≈70–100 binauralOut≈70–100).");
        LogCaps("OPTIONAL: PRESS P ANYTIME FOR A STATE LINE. WHEN FADE-IN SOUNDS RIGHT, " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);

        // --- Part C / Stage 1: Director repro (ActivateEntireQueueOnNextTone self-retention) ---
        LogCaps("--- PART C / STAGE 1: DIRECTOR — REPRO (ActivateEntireQueueOnNextTone) ---");
        LogCaps("RELEASE ANY TONE NOW — REPRO MUST NOT FIRE UNTIL THE 'NOW TONE' STEP BELOW.");
        yield return WaitForToneRelease(imitone, 120f);

        director.ClearQueueOfType("MusicDebugHarness_Repro");
        bool reproFired = false;
        int reproId = director.AddActionToQueue(
            () =>
            {
                reproFired = true;
                Debug.Log(Prefix + " Director repro action executed (ActivateEntireQueueOnNextTone).");
            },
            "MusicDebugHarness_Repro",
            true,
            false,
            0.05f,
            DirectorActivationBehavior.ActivateEntireQueueOnNextTone,
            DirectorExclusivityBehavior.None);
        Debug.Log(Prefix + " Queued repro id=" + reproId + " " + director.FormatQueueContents());
        yield return WaitForDirectorTimerElapsed(0.12f);
        LogCaps("REPRO TIMER EXPIRED (0.05s). STAY SILENT — DO NOT TONE. WHEN READY FOR THE TONE STEP, " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        LogCaps("NOW DO A CLEAR, SUSTAINED TONE (2–3 SECONDS) TO FIRE THE REPRO — RELEASE, THEN SPACE/RETURN.");
        LogCaps("EXPECT: 'Director repro action executed' + 'Activating entire queue with tone' — NOT 'queue is empty'.");
        if (reproFired)
            LogCaps("NOTE: REPRO ALREADY FIRED (YOU MAY HAVE TONED EARLY) — STILL OK IF LOGS MATCH EXPECTATION.");

        yield return WaitForSustainedTone(imitone);
        yield return WaitForAdvance(imitone);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
        Debug.Log(Prefix + " Queue after repro tone: " + director.FormatQueueContents());
        LogCaps("REPRO STEP DONE. " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);

        // --- Stage 1: whole-queue soundscape change (Shadow) ---
        if (ms != null)
        {
            LogCaps("--- PART C / STAGE 1: DIRECTOR — BASELINE BEFORE SHADOW ---");
            EnsureBaselineBeforeSoundWorldTest(shuffler, ms, ShadowSoundWorldTarget, ShadowTestBaselineWorld);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
            LogCaps("LISTEN — BASELINE SHOULD BE " + ShadowTestBaselineWorld.ToUpperInvariant()
                + " (OR ANOTHER NON-SHADOW WORLD), NOT SHADOW YET. " + AdvanceHintKeyboard);
            yield return WaitForAdvance(imitone);

            LogCaps("--- PART C / STAGE 1: DIRECTOR — SOUNDSCAPE → SHADOW ON TONE ---");
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
            LogCaps("SHADOW QUEUED (" + DirectorTimerSeconds + "s TIMER). WHEN TIMER EXPIRED, " + AdvanceHintKeyboard);

            yield return WaitForAdvance(imitone);

            LogCaps("NOW TONE — SOUND BED SHOULD SHIFT TO SHADOW. RELEASE, THEN " + AdvanceHintKeyboard);
            yield return WaitForSustainedTone(imitone);
            yield return WaitForAdvance(imitone);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
            LogCaps("SHADOW DIRECTOR STEP DONE. " + AdvanceHintKeyboard);
            yield return WaitForAdvance(imitone);
        }

        // --- Stage 1: ActivateThisActionOnNextTone (transition flourish) ---
        LogCaps("--- PART C / STAGE 1: DIRECTOR — TRANSITION SOUND (ActivateThisActionOnNextTone) ---");
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
        LogCaps("TRANSITION QUEUED (" + DirectorTimerSeconds + "s TIMER). WHEN EXPIRED, " + AdvanceHintKeyboard);

        yield return WaitForAdvance(imitone);

        LogCaps("NOW TONE — EXPECT A SHORT TRANSITION / WHOOSH. RELEASE, THEN " + AdvanceHintKeyboard);
        yield return WaitForSustainedTone(imitone);
        yield return WaitForAdvance(imitone);

        // --- Stage 1: whole-queue shuffle (SoundscapeShuffle + ColorWorldShuffle pattern) ---
        if (shuffler != null)
        {
            LogCaps("--- PART C / STAGE 1: DIRECTOR — SHUFFLE ON TONE ---");
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
            LogCaps("SHUFFLE QUEUED (" + DirectorTimerSeconds + "s TIMER). WHEN EXPIRED, " + AdvanceHintKeyboard);

            yield return WaitForAdvance(imitone);

            LogCaps("NOW TONE — SOUND WORLD + LIGHT COLOR SHOULD SHUFFLE TOGETHER. RELEASE, THEN " + AdvanceHintKeyboard);
            yield return WaitForSustainedTone(imitone);
            yield return WaitForAdvance(imitone);
            harness.ExecuteAction(MusicDebugHarnessAction.DumpState);
            LogCaps("SHUFFLE STEP DONE. " + AdvanceHintKeyboard);
            yield return WaitForAdvance(imitone);
        }

        // --- Stage 2: optional attenuation on playground (MusicLoopSilent) ---
        if (ms != null && harness != null)
        {
            LogCaps("--- PART C / STAGE 2: BINAURAL ATTENUATION ON PLAYGROUND (OPTIONAL) — SPACE TO SKIP ---");
            harness.ApplyMusicLoopSilentMode();
            LogCaps("MUSICLOOPSILENT MODE SET. LISTEN ~30s: binauralAtt SHOULD BECOME on; binauralOut ≈ 70 (100×0.7) IF base=100. " + AdvanceHintKeyboard);
            yield return WaitForAdvance(imitone);
            harness.ApplyFreeplayMode();
            LogCaps("FREEPLAY RESTORED. binauralAtt SHOULD TURN off; binauralOut RECOVER TOWARD ~100. " + AdvanceHintKeyboard);
            yield return WaitForAdvance(imitone);
            LogCaps("ATTENUATION OPTIONAL DONE. " + AdvanceHintKeyboard);
            yield return WaitForAdvance(imitone);
        }

        // --- Stage 2: leave Playground → Linear_Nature (mute) ---
        LogCaps("--- PART C / STAGE 2: LEAVE PLAYGROUND (BINAURAL FADE OUT) ---");
        LogCaps("PRESS E NOW (EndThisSequenceStage) → Linear_Nature. gameOn MAY TURN OFF — EXPECTED.");
        yield return WaitForAdvance(imitone, AdvanceTimeoutSeconds, KeyCode.E);

        LogCaps("LISTEN: BINAURAL SHOULD FADE OUT OVER ~30s (binauralBase=0 binauralOut=0). PRESS P FOR STATE. WHEN FADE-OUT JUDGED, " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);
        harness.ExecuteAction(MusicDebugHarnessAction.DumpState);

        LogCaps("OPTIONAL: PRESS E AGAIN FOR NEXT STAGE — BINURAL SHOULD STAY MUTED ON NON-PLAYGROUND STAGES. OR " + AdvanceHintKeyboard + " TO FINISH.");
        yield return WaitForAdvance(imitone, AdvanceTimeoutSeconds, KeyCode.E);
        LogPartComplete("C", "Report what you heard vs B457 logs for director + binaural.");
        LogCaps("=== ALL PARTS A + B + C FINISHED ===");
    }

    static void LogCaps(string message)
    {
        Debug.Log(Prefix + " >>> " + message.ToUpperInvariant() + " <<<");
    }

    static void LogPartBegin(string part, string shortTitle, string testingFor, string passIf)
    {
        LogCaps("════════════════════════════════════════");
        LogCaps("ENTERING PART " + part + " — " + shortTitle);
        LogCaps("WHAT WE ARE TESTING: " + testingFor);
        LogCaps("PASS IF: " + passIf);
        LogCaps("════════════════════════════════════════");
    }

    static void LogPartComplete(string part, string wrapUpNote)
    {
        LogCaps("────────────────────────────────────────");
        LogCaps("PART " + part + " COMPLETE — " + wrapUpNote);
        LogCaps("────────────────────────────────────────");
    }

    static IEnumerator BeginNextPart(
        ImitoneVoiceIntepreter imitone,
        string part,
        string shortTitle,
        string testingFor,
        string passIf)
    {
        LogCaps("▶▶▶ MOVING ON TO PART " + part + " ◀◀◀");
        LogPartBegin(part, shortTitle, testingFor, passIf);
        LogCaps("WHEN YOU HAVE READ THE PART " + part + " GOALS ABOVE, " + AdvanceHintKeyboard);
        yield return WaitForAdvance(imitone);
    }

    /// <summary>
    /// Auto-shuffle during the repro step can land on Shadow before we queue Shadow via Director.
    /// Force a known non-Shadow baseline so the next step is a real world change (Step 0 in SOUNDWORLD_SWITCH_NOT_AUDIBLE.md).
    /// </summary>
    static void ClearPlaygroundShuffleQueue(Director director)
    {
        if (director == null)
            return;

        director.ClearQueueOfType("SoundscapeShuffle");
        director.ClearQueueOfType("ColorWorldShuffle");
        Debug.Log(Prefix + " Cleared SoundscapeShuffle + ColorWorldShuffle from director queue (playground auto-queue noise).");
    }

    static IEnumerator WaitForDirectorTimerElapsed(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ===== TEMPORARY — Stage 9 playtest scaffolding. REMOVE with DebugSimulateSungFundamentalChange. =====
    /// <summary>
    /// Timer-driven wait (NO key press) with a once-per-second countdown log, for the goblin steps that must
    /// progress by timer (clearing the 5s flourish window, spacing the anti-clutter burst) rather than by Space.
    /// </summary>
    static IEnumerator WaitWithCountdown(float seconds, string label)
    {
        float remaining = seconds;
        float tick = 0f;
        Debug.Log(Prefix + " ⏱ " + label + " — " + Mathf.CeilToInt(remaining) + "s (timer, no key needed)…");
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            tick += Time.deltaTime;
            if (tick >= 1f && remaining > 0f)
            {
                Debug.Log(Prefix + " ⏱ " + label + " — " + Mathf.CeilToInt(remaining) + "s left");
                tick = 0f;
            }
            yield return null;
        }
    }

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

    /// <summary>
    /// Pacing gate: Space/Return or optional harness keys only. Toning does not advance (prevents one held note skipping steps).
    /// Waits for any in-progress tone to release before listening for input.
    /// </summary>
    static IEnumerator WaitForAdvance(
        ImitoneVoiceIntepreter imitone,
        float timeoutSeconds = AdvanceTimeoutSeconds,
        params KeyCode[] extraAdvanceKeys)
    {
        yield return WaitForToneRelease(imitone);

        float elapsed = 0f;
        while (elapsed < timeoutSeconds)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log(Prefix + " Advance: Space/Return.");
                yield break;
            }

            for (int i = 0; i < extraAdvanceKeys.Length; i++)
            {
                if (Input.GetKeyDown(extraAdvanceKeys[i]))
                {
                    Debug.Log(Prefix + " Advance: " + extraAdvanceKeys[i] + ".");
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning(Prefix + " Advance wait timed out after " + timeoutSeconds + "s — continuing.");
    }

    /// <summary>Direction chosen at a navigable step (forward/back through a linear list).</summary>
    enum StepNav { Next, Back }

    /// <summary>
    /// Pacing gate for linear lists where Robin can step forward AND backward.
    /// Space/Return/Right = Next, Left/Backspace = Back. Toning does not advance. Result written to result[0].
    /// </summary>
    static IEnumerator WaitForStepNav(ImitoneVoiceIntepreter imitone, StepNav[] result)
    {
        yield return WaitForToneRelease(imitone);

        float elapsed = 0f;
        while (elapsed < AdvanceTimeoutSeconds)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                result[0] = StepNav.Next;
                Debug.Log(Prefix + " Step: NEXT.");
                yield break;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Backspace))
            {
                result[0] = StepNav.Back;
                Debug.Log(Prefix + " Step: BACK.");
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        result[0] = StepNav.Next;
        Debug.LogWarning(Prefix + " Step wait timed out after " + AdvanceTimeoutSeconds + "s — advancing.");
    }

    /// <summary>Director on-tone steps only — does not advance on Space (tone must fire game logic).</summary>
    static IEnumerator WaitForSustainedTone(ImitoneVoiceIntepreter imitone, float timeoutSeconds = 90f)
    {
        yield return WaitForToneRelease(imitone);

        if (imitone == null)
        {
            Debug.LogWarning(Prefix + " No imitone — cannot detect tone; " + AdvanceHintKeyboard);
            yield return WaitForAdvance(null);
            yield break;
        }

        float elapsed = 0f;
        float toneHeld = 0f;
        while (elapsed < timeoutSeconds)
        {
            if (imitone.toneActiveConfident)
            {
                toneHeld += Time.deltaTime;
                if (toneHeld >= ToneHoldRequiredSeconds)
                {
                    Debug.Log(Prefix + " Director step: sustained tone detected.");
                    yield return WaitForToneRelease(imitone);
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

        Debug.LogWarning(Prefix + " Tone wait timed out after " + timeoutSeconds + "s — continue with Space when ready.");
    }

    /// <summary>After a step ends, require brief silence so the next step does not instantly consume the same held tone.</summary>
    static IEnumerator WaitForToneRelease(ImitoneVoiceIntepreter imitone, float timeoutSeconds = 30f)
    {
        if (imitone == null)
            yield break;

        float elapsed = 0f;
        float releasedFor = 0f;
        while (elapsed < timeoutSeconds)
        {
            if (!imitone.toneActiveConfident)
            {
                releasedFor += Time.deltaTime;
                if (releasedFor >= ToneReleaseSeconds)
                    yield break;
            }
            else
            {
                releasedFor = 0f;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
#endif
