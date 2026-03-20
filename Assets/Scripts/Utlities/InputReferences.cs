using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConversionUtilities;
using SoundSelf.Sequence;

public class InputReferences : MonoBehaviour
{
    
    public static InputReferences instance;

    public bool setStateInteractiveMusicMode = false;
    
    // MusicLoops_Switch values to cycle through
    private readonly string[] musicLoopsSwitchValues = new string[]
    {
        "AmbientLoop",
        "AweAtmosphere",
        "BreathworkHandpan",
        "CelestialDreamscape",
        "CosmicAir",
        "Environment",
        "MysticalVision",
        "PinkNoiseAtmosphere",
        "ShiftingEarth",
        "Silence",
        "SingingBowls",
        "SitarAmbience"
    };
    
    private int currentMusicLoopsSwitchIndex = 0;

    [Header("Debug: Protocol Stacks Sequence Advance (F key)")]
    [SerializeField] private Sequencer sequencer;
    [SerializeField] private Director director;
    private Coroutine _sequenceAdvanceCountdownCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (sequencer == null) sequencer = FindObjectOfType<Sequencer>();
        if (director == null) director = FindObjectOfType<Director>();
    }

    private IEnumerator SequenceAdvanceAndCountdownCoroutine()
    {
        if (sequencer == null || director == null)
        {
            Debug.LogWarning("[Input] F key: Sequencer or Director not found. Assign in Inspector or ensure they exist in scene.");
            yield break;
        }
        sequencer.ForceSequenceAdvance();
        Debug.Log("[Input] F key: Sequence advanced. Countdown 10 seconds to director queue activation...");
        for (int i = 10; i >= 1; i--)
        {
            Debug.Log("[Input] F key: Director queue in " + i + " seconds...");
            yield return new WaitForSeconds(1f);
        }
        director.ActivateQueue(15f);
        Debug.Log("[Input] F key: Director queue activated.");
        _sequenceAdvanceCountdownCoroutine = null;
    }

    //================================
    // EXAMPLE TEST COROUTINE (Reference format)
    //================================
    // Each test coroutine should:
    // 1. Start with a comment: LOOK FOR / SUCCESS / FAILURE
    // 2. Log instructions for the tester
    // 3. Use WaitUntil(Input.GetKeyDown(KeyCode.Space)) to step through
    /*
    private IEnumerator SampleTestCoroutine()
    {
        Debug.Log("[SampleTestCoroutine] Stage 1: Started.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        Debug.Log("[SampleTestCoroutine] Test complete!");
    }
    */

    //================================
    // SEQUENCE REFACTOR TEST PLAN
    //================================
    // To run a test: Uncomment ONE trigger in Update() below. Coroutines stay uncommented.
    // Keys: 7=Test1, 8=Test2, 9=Test3, 0=Test4, Minus=Test5, Equals=Test6
    //================================

    // TEST 1: SequenceRunner and CSVLoader wiring
    // LOOK FOR: SequenceRunner and SequenceDefinition are reachable; StageCount correct.
    // SUCCESS: StageCount = 3 (Protocol Stacks Ascending), CurrentStageIndex = -1 before start, no null refs.
    // FAILURE: Null refs, StageCount 0, or CSVLoader/Sequencer not in scene.
    private IEnumerator Test1_SequenceRunnerAndCSVLoaderWiring()
    {
        Debug.Log("[Test1] === SEQUENCE REFACTOR TEST 1: SequenceRunner and CSVLoader wiring ===");
        Debug.Log("[Test1] INSTRUCTIONS: Verify Console output. Press Space to proceed through steps.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        var runner = sequencer != null ? sequencer.GetComponent<SequenceRunner>() : null;
        if (runner == null)
        {
            Debug.LogError("[Test1] FAIL: SequenceRunner not found. Ensure Sequencer has SequenceRunner on same GameObject.");
            yield break;
        }
        Debug.Log("[Test1] OK: SequenceRunner found.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        var csvLoader = CSVLoader.instance;
        if (csvLoader == null)
        {
            Debug.LogError("[Test1] FAIL: CSVLoader.instance is null. Ensure CSVLoader is in scene.");
            yield break;
        }
        Debug.Log("[Test1] OK: CSVLoader found. gameMode=" + csvLoader.gameMode + ", subGameMode=" + csvLoader.subGameMode);
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        var def = csvLoader.GetSequenceDefinitionForProtocolStacks();
        if (def == null)
        {
            Debug.LogWarning("[Test1] WARN: GetSequenceDefinitionForProtocolStacks() returned null. Is gameMode 'Protocol Stacks'?");
        }
        else
        {
            Debug.Log("[Test1] OK: SequenceDefinition found. displayName=" + def.displayName + ", StageCount=" + def.StagesOrEmpty.Length);
        }
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        Debug.Log("[Test1] runner.StageCount=" + runner.StageCount + ", CurrentStageIndex=" + runner.CurrentStageIndex + ", IsSequenceComplete=" + runner.IsSequenceComplete);
        Debug.Log("[Test1] === TEST 1 COMPLETE. Press Space to finish. ===");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
    }

    // TEST 2: StartSequence
    // LOOK FOR: StartSequence advances to stage 0 (Opening); Console shows "Sequence: Entered stage 0 (Opening)".
    // SUCCESS: CurrentStageIndex = 0, CurrentStage = Opening, opening VO/AVS starts.
    // FAILURE: CurrentStageIndex stays -1, no stage log, or null ref.
    private IEnumerator Test2_StartSequence()
    {
        Debug.Log("[Test2] === SEQUENCE REFACTOR TEST 2: StartSequence ===");
        Debug.Log("[Test2] INSTRUCTIONS: Ensure gameMode is Protocol Stacks. Press Space to call StartSequence.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        var runner = sequencer != null ? sequencer.GetComponent<SequenceRunner>() : null;
        var def = CSVLoader.instance?.GetSequenceDefinitionForProtocolStacks();
        if (runner == null || def == null) { Debug.LogError("[Test2] FAIL: runner or def null."); yield break; }

        runner.StartSequence(def);
        Debug.Log("[Test2] StartSequence called. Check Console for 'Sequence: Entered stage 0 (Opening)'. CurrentStageIndex=" + runner.CurrentStageIndex);
        Debug.Log("[Test2] SUCCESS = stage 0, Opening plays. FAILURE = stuck at -1. Press Space to finish.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
    }

    // TEST 3: Cue handling (HandleCue StartInteractive)
    // LOOK FOR: When in Opening, HandleCue(StartInteractive) advances to Playground.
    // SUCCESS: Console shows "StartInteractive: Handled by current stage (Opening)", then "Sequence: Entered stage 1 (Playground)".
    // FAILURE: "not being watched" warning, or no advance.
    private IEnumerator Test3_CueHandling()
    {
        Debug.Log("[Test3] === SEQUENCE REFACTOR TEST 3: Cue handling ===");
        Debug.Log("[Test3] INSTRUCTIONS: Start sequence first (Test 2 or normal flow). When in Opening, press Space to simulate Cue_StartInteractive.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        var runner = sequencer != null ? sequencer.GetComponent<SequenceRunner>() : null;
        if (runner == null || runner.CurrentStageIndex != 0) { Debug.LogWarning("[Test3] SKIP: Must be in Opening (stage 0). CurrentStageIndex=" + (runner?.CurrentStageIndex ?? -999)); yield break; }

        bool handled = sequencer.HandleCue(CueType.StartInteractive);
        Debug.Log("[Test3] HandleCue(StartInteractive) returned " + handled + ". Next frame should advance to Playground. Check Console.");
        Debug.Log("[Test3] Press Space to finish.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
    }

    // TEST 4: ForceSequenceAdvance in Playground
    // LOOK FOR: F key (or ForceSequenceAdvance) skips Playground countdown steps.
    // SUCCESS: Playground advances through steps faster; eventually reaches Savasana.
    // FAILURE: Nothing happens, or sequence breaks.
    private IEnumerator Test4_ForceSequenceAdvance()
    {
        Debug.Log("[Test4] === SEQUENCE REFACTOR TEST 4: ForceSequenceAdvance ===");
        Debug.Log("[Test4] INSTRUCTIONS: Be in Playground stage. Press Space to call ForceSequenceAdvance().");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        sequencer.ForceSequenceAdvance();
        Debug.Log("[Test4] ForceSequenceAdvance() called. Playground should skip current wait. Press F repeatedly to skip more, or wait for countdown.");
        Debug.Log("[Test4] Press Space to finish.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
    }

    // TEST 5: Full sequence to completion
    // LOOK FOR: Sequence runs Opening -> Playground -> Savasana -> complete; OnSequenceComplete fires.
    // SUCCESS: Console shows "SequenceRunner: Sequence complete.", IsSequenceComplete = true.
    // FAILURE: Stuck in a stage, no completion log, or crash.
    private IEnumerator Test5_FullSequenceToCompletion()
    {
        Debug.Log("[Test5] === SEQUENCE REFACTOR TEST 5: Full sequence to completion ===");
        Debug.Log("[Test5] INSTRUCTIONS: Start from Calibration. Click Start to begin. Use F key to ForceSequenceAdvance through Playground.");
        Debug.Log("[Test5] Watch for: Opening -> Cue_StartInteractive -> Playground -> countdown 0 -> Savasana -> Sequence complete.");
        Debug.Log("[Test5] Press Space to acknowledge and finish.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
    }

    // TEST 6: SequenceProgressUI
    // LOOK FOR: If SequenceProgressUI is in scene, it logs "SequenceProgressUI: Stage X" and "Sequence complete." when sequence runs.
    // SUCCESS: SequenceProgressUI logs appear alongside SequenceRunner logs.
    // FAILURE: No SequenceProgressUI logs (component missing or sequenceRunner not assigned).
    private IEnumerator Test6_SequenceProgressUI()
    {
        Debug.Log("[Test6] === SEQUENCE REFACTOR TEST 6: SequenceProgressUI ===");
        Debug.Log("[Test6] INSTRUCTIONS: Add SequenceProgressUI to a GameObject, assign sequenceRunner. Run sequence. Check for 'SequenceProgressUI:' logs.");
        Debug.Log("[Test6] SUCCESS = duplicate logs (SequenceRunner + SequenceProgressUI). FAILURE = only SequenceRunner logs.");
        Debug.Log("[Test6] Press Space to finish.");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
    }

    //================================
    //UPDATE METHOD
    //================================

    // Update is called once per frame
    void Update()
    {
        // ============================================
        // SEQUENCE REFACTOR TESTS (7, 8, 9, 0, Minus, Equals)
        // Uncomment ONE test trigger at a time.
        // ============================================
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            StartCoroutine(Test1_SequenceRunnerAndCSVLoaderWiring());
        }
        /*
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            StartCoroutine(Test2_StartSequence());
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            StartCoroutine(Test3_CueHandling());
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            StartCoroutine(Test4_ForceSequenceAdvance());
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            StartCoroutine(Test5_FullSequenceToCompletion());
        }
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            StartCoroutine(Test6_SequenceProgressUI());
        }
        */

        // F key: Force sequence advance, then 10s countdown, then activate director queue
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[Input] F key pressed.");
            if (_sequenceAdvanceCountdownCoroutine == null)
            {
                Debug.Log("[Input] Starting SequenceAdvanceAndCountdownCoroutine logic for F key...");
                _sequenceAdvanceCountdownCoroutine = StartCoroutine(SequenceAdvanceAndCountdownCoroutine());
            }
            else
            {
                Debug.Log("[Input] F key: Countdown already in progress.");
            }
        }

        // "D" key: Activate director queue immediately (no countdown)
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("[Input] D key pressed.");
            if (director != null)
            {
                Debug.Log("[Input] Running logic for D key: Activating director queue immediately...");
                director.ActivateQueue(10f); // 10 seconds as seen in other contexts; adjust duration as needed
                Debug.Log("[Input] D key: Director queue activated immediately.");
            }
            else
            {
                Debug.LogWarning("[Input] D key: Director reference is null!");
            }
        }

        // List All Recorded Files
        /*
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                RecordedAudioPlaybackTest.Instance.ListAllRecordedFiles();
            }
        }
        */

        //keyboard inputs
        /*
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (DebugMenuController.instance != null)
            {
                DebugMenuController.instance.SwitchDebugMenu();
            }
        }

        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (canvasSwitcher.instance != null)
            {
                canvasSwitcher.instance.SwitchCanvas(1);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (canvasSwitcher.instance != null)
            {
                canvasSwitcher.instance.SwitchCanvas(2);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (canvasSwitcher.instance != null)
            {
                canvasSwitcher.instance.SwitchCanvas(3);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (canvasSwitcher.instance != null)
            {
                canvasSwitcher.instance.SwitchCanvas(4);
            }
        }

        /*
        if(Input.GetKeyDown(KeyCode.U))
        {
            MusicBinauralBeats.instance.PlayBinauralBeats();
        }
        if(Input.GetKeyDown(KeyCode.I))
        {
            MusicBinauralBeats.instance.StopBinauralBeats();
        }
        if(Input.GetKeyDown(KeyCode.O))
        {
            float newRate = Random.Range(4f, 8f);
            MusicBinauralBeats.instance.NewBinauralBeatRate(newRate, 4.0f);
        }
        if(Input.GetKeyDown(KeyCode.P))
        {
            if(MusicBinauralBeats.instance._volume < 50f)
            {
                MusicBinauralBeats.instance.SetVolume(100f, 2.0f);
            }
            else
            {
                MusicBinauralBeats.instance.SetVolume(0f, 2.0f);
            }
        }
        */
        
        // ===================================================================
        //TESTING KEYBOARD COMMANDS FOR MUSIC SYSTEM
        // ===================================================================
        // Keyboard shortcuts for changing fundamental (for quick MusicSystem1 functional testing)
        // Note: Check for Shift+C first to avoid conflict with Play_MusicLoops command
        /*
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[TEST] Keyboard command C pressed");
            if (MusicSystem1.instance != null)
            {
                Debug.Log("[TEST] MusicSystem1.instance is not null");
                // Change fundamental to C (ordinal 0)
                MusicSystem1.instance.SetFundamentalDirect(NoteName.C);
                Debug.Log("[TEST] Music fundamental changed to C via keyboard (C)");
            }
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (MusicSystem1.instance != null)
            {
                // Change fundamental to G (ordinal 7)
                MusicSystem1.instance.SetFundamentalDirect(NoteName.G);
                Debug.Log("[TEST] Music fundamental changed to G via keyboard (G)");
            }
        }
        */
        // ===================================================================
        // KEYBOARD COMMANDS FOR FUNDAMENTAL LOCKS (MusicSystem1)
        // I/O: Debug Lock   |   K/L: Content Lock   |   N/M: Mode Lock
        // Press key to SET the lock, adjacent key to CLEAR the lock
        // ===================================================================
        // DEBUG LOCK (I = Set Debug Lock to C, O = Clear Debug Lock)
        /*
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (MusicSystem1.instance != null)
            {
                MusicSystem1.instance.SetFundamentalDebugLock(NoteName.As);
                Debug.Log("[LOCK] Debug fundamental lock set to As via I");
            }
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (MusicSystem1.instance != null)
            {
                MusicSystem1.instance.SetFundamentalDebugLock(null); // Pass null to clear
                Debug.Log("[LOCK] Debug fundamental lock cleared via O");
            }
        }

        // CONTENT LOCK (K = Set Content Lock to E, L = Clear Content Lock)
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (MusicSystem1.instance != null)
            {
                MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);
                Debug.Log("[LOCK] Content fundamental lock set to C via K");
            }
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (MusicSystem1.instance != null)
            {
                MusicSystem1.instance.SetFundamentalContentLock(null); // Pass null to clear, not NoteName.None
                Debug.Log("[LOCK] Content fundamental lock cleared via L");
            }
        }

        // MODE LOCK (N = Set Mode Lock to G, M = Clear Mode Lock)
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (MusicSystem1.instance != null)
            {
                MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.G);
                Debug.Log("[LOCK] Mode fundamental lock set to G via N");
            }
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (MusicSystem1.instance != null)
            {
                MusicSystem1.instance.SetFundamentalModeLock(false); // Note parameter is ignored when doLock is false
                Debug.Log("[LOCK] Mode fundamental lock cleared via M");
            }
        }
        */
        // ===================================================================
        // TESTING KEYBOARD COMMANDS FOR InputReferences.cs
        // ===================================================================
        // Add these commands to the Update() method in InputReferences.cs
        // Place them after the existing keyboard input checks
        // ===================================================================


        // Recording Controls
        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     if (RecordedAudioPlaybackTest.Instance != null)
        //     {
        //         bool newMode = !RecordedAudioPlaybackTest.Instance.recordMode;
        //         RecordedAudioPlaybackTest.Instance.SetRecordMode(newMode);
        //         Debug.Log($"[TEST] Record mode toggled: {newMode}");
        //     }
        // }

        // if (Input.GetKeyDown(KeyCode.P))
        // {
        //     if (RecordedAudioPlaybackTest.Instance != null)
        //     {
        //         bool newMode = !RecordedAudioPlaybackTest.Instance.playMode;
        //         RecordedAudioPlaybackTest.Instance.SetPlaybackMode(newMode);
        //         Debug.Log($"[TEST] Playback mode toggled: {newMode}");
        //     }
        // }

        // // Delete All Recordings
        // if (Input.GetKeyDown(KeyCode.S))
        // {
        //     if (RecordedAudioPlaybackTest.Instance != null)
        //     {
        //         RecordedAudioPlaybackTest.Instance.DeleteAllRecordings();
        //         Debug.Log("[TEST] DeleteAllRecordings called");
        //     }
        // }

        // // Print Slot Status (for debugging)
        // if (Input.GetKeyDown(KeyCode.A))
        // {
        //     if (RecordedAudioPlaybackTest.Instance != null)
        //     {
        //         RecordedAudioPlaybackTest.Instance.PrintSlotStatus();
        //     }
        // }



        // // Manual Test Recording (5 seconds)
        // if (Input.GetKeyDown(KeyCode.M))
        // {
        //     if (RecordedAudioPlaybackTest.Instance != null)
        //     {
        //         RecordedAudioPlaybackTest.Instance.ManualTestRecording();
        //     }
        // }

        // // Get Recording Counts
        // if (Input.GetKeyDown(KeyCode.C))
        // {
        //     if (RecordedAudioPlaybackTest.Instance != null)
        //     {
        //         RecordedAudioPlaybackTest.Instance.GetRecordingCounts();
        //     }
        // }
        // ===================================================================
        // KEYBOARD SHORTCUT REFERENCE:
        // ===================================================================
        // R - Toggle Record Mode
        // P - Toggle Playback Mode
        // S - Delete All Recordings
        // A - Print Slot Status
        // L - List All Recorded Files
        // M - Manual Test Recording (5 seconds)
        // C - Get Recording Counts
        // N - Stop Music Loops
        // SHIFT + V - Cycle through MusicLoops_Switch values
        // B - Play Silent Loops
        // ===================================================================
        
        // Wwise Music Controls
        /*
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (MusicSystem1.instance != null)
            {
                MusicSystem1.instance.SetInteractiveMusicModeToMusicLoops(80f);
            }
            else
            {
                Debug.LogWarning("[Input] MusicSystem1.instance is null, cannot set Wwise state");
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (MusicSystem1.instance != null && MusicSystem1.instance.gameObject != null)
            {
                AkSoundEngine.SetSwitch("MusicLoops_Switch", "SitarAmbience", MusicSystem1.instance.gameObject);
            }
            else
            {
                Debug.LogWarning("[Input] MusicSystem1.instance or gameObject is null, cannot set Wwise switch");
            }
        }

        // Play_MusicLoops - using Shift+C to avoid conflict with GetRecordingCounts (C)
        if (Input.GetKeyDown(KeyCode.C) && Input.GetKey(KeyCode.LeftShift))
        {
            if (MusicSystem1.instance != null && MusicSystem1.instance.gameObject != null)
            {
                uint eventId = AkSoundEngine.GetIDFromString("Play_MusicLoops");
                if (eventId != 0)
                {
                    AkSoundEngine.PostEvent("Play_MusicLoops", MusicSystem1.instance.gameObject);
                    Debug.Log("[Input] Posted Wwise event: Play_MusicLoops");
                    Debug.Log("Event ID: " + eventId);
                }
                else
                {
                    Debug.LogError("[Input] Wwise event 'Play_MusicLoops' not found. Check Wwise project - event may not exist or Wwise banks not loaded.");
                }
            }
            else
            {
                Debug.LogWarning("[Input] MusicSystem1.instance or gameObject is null, cannot post Wwise event");
            }
        }
        
        
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (MusicSystem1.instance != null && MusicSystem1.instance.gameObject != null)
            {
                uint eventId = AkSoundEngine.GetIDFromString("Stop_MusicLoops");
                if (eventId != 0)
                {
                    AkSoundEngine.PostEvent("Stop_MusicLoops", MusicSystem1.instance.gameObject);
                    Debug.Log("[Input] Posted Wwise event: Stop_MusicLoops");
                }
                else
                {
                    Debug.LogError("[Input] Wwise event 'Stop_MusicLoops' not found. Check Wwise project - event may not exist or Wwise banks not loaded.");
                }
            }
            else
            {
                Debug.LogWarning("[Input] MusicSystem1.instance or gameObject is null, cannot post Wwise event");
            }
        }
        

        // Cycle through MusicLoops_Switch values with Shift+V
        if (Input.GetKeyDown(KeyCode.V) && Input.GetKey(KeyCode.LeftShift))
        {
            if (MusicSystem1.instance != null && MusicSystem1.instance.gameObject != null)
            {
                // Increment index and wrap around
                currentMusicLoopsSwitchIndex = (currentMusicLoopsSwitchIndex + 1) % musicLoopsSwitchValues.Length;
                string switchValue = musicLoopsSwitchValues[currentMusicLoopsSwitchIndex];
                
                AkSoundEngine.SetSwitch("MusicLoops_Switch", switchValue, MusicSystem1.instance.gameObject);
                Debug.Log($"[Input] MusicLoops_Switch cycled to: {switchValue} ({currentMusicLoopsSwitchIndex + 1}/{musicLoopsSwitchValues.Length})");
            }
            else
            {
                Debug.LogWarning("[Input] MusicSystem1.instance or gameObject is null, cannot set Wwise switch");
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            if (MusicSystem1.instance != null && MusicSystem1.instance.gameObject != null)
            {
                uint eventId = AkSoundEngine.GetIDFromString("Play_SilentLoops");
                if (eventId != 0)
                {
                    AkSoundEngine.PostEvent("Play_SilentLoops", MusicSystem1.instance.gameObject);
                    Debug.Log("[Input] Posted Wwise event: Play_SilentLoops");
                }
                else
                {
                    Debug.LogError("[Input] Wwise event 'Play_SilentLoops' not found. Check Wwise project - event may not exist or Wwise banks not loaded.");
                }
            }
            else
            {
                Debug.LogWarning("[Input] MusicSystem1.instance or gameObject is null, cannot post Wwise event");
            }
        }
        */
    }
}
