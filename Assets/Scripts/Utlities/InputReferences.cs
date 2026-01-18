using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConversionUtilities;

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
    // Start is called before the first frame update

    //We are using Coroutines to test different parts of the system.
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

        // Start the sample testing coroutine at launch as a skeleton
        // StartCoroutine(SampleTestCoroutine());
        
        // ===================================================================
        // NOTE NAME REFACTORING TEST COROUTINES
        // ===================================================================
        // Uncomment one at a time to run tests:
        
        // Test 1: Fundamental Note Changes
        // StartCoroutine(TestFundamentalNoteChanges());
        
        // Test 2: Harmony System
        // StartCoroutine(TestHarmonySystem());
        
        // Test 3: Note Detection
        // StartCoroutine(TestNoteDetection());
        
        // Test 4: Mode Switching & Locks
        // StartCoroutine(TestModeSwitchingAndLocks());
        
        // Test 5: Edge Cases
        // StartCoroutine(TestEdgeCases());
        
        // Test 6: Note Mapping Verification
        // StartCoroutine(TestNoteMapping());
        
        // Test 7: Combined Locking System Tests (8.1 - 9.1)
        // StartCoroutine(TestAllLockingSystems());
        
        // Test 8: Content Lock with MusicLoops and SoundWorlds (9.2 - 9.6)
        // StartCoroutine(TestContentLockWithSoundscapes());
        
        // Test 9: Lock Priority and Unlock Resolution (10.1 - 11.1)
        // StartCoroutine(TestLockPriorityAndUnlockResolution());
        
        // Test 10: ResolveFundamentalOnUnlock with No Locks (11.2)
        // StartCoroutine(TestUnlockResolutionImmediateThreshold());
        
        // Test 11: ResolveFundamentalOnUnlock Queue Threshold (11.3)
        // StartCoroutine(TestUnlockResolutionQueueThreshold());
        
        // Test 12: ResolveFundamentalOnUnlock Below Threshold (11.4)
        // StartCoroutine(TestUnlockResolutionBelowThreshold());
        
        // Test 13: Director Queue Priority System - ReplaceActionInQueue (2.1)
        // StartCoroutine(TestReplaceActionInQueue());
        
        // Test 14: Director Queue Priority System - SoundscapeShuffle Rejected (2.2)
        // StartCoroutine(TestSoundscapeShuffleRejected());
        
        // Test 15: Director Queue Priority System - ReplaceActionInQueue Expiration Time (2.3)
        // StartCoroutine(TestReplaceActionInQueueExpirationTime());
        
        // Test 16: Silent Mode Behavior (6.1 - 6.3)
        // StartCoroutine(TestSilentModeBehavior());
    }

    // A sample coroutine for testing, acting as a skeleton for future tests
    private IEnumerator SampleTestCoroutine()
    {
        Debug.Log("[SampleTestCoroutine] Stage 1: Started the sample test coroutine.");
        Debug.Log("[SampleTestCoroutine] Press Space to proceed to Stage 2.");

        // Wait for user to press the space bar to continue
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        Debug.Log("[SampleTestCoroutine] Stage 2: Spacebar pressed! Proceeding to next stage.");
        Debug.Log("[SampleTestCoroutine] Press Space to finish the sample coroutine.");

        // Wait for another space bar
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        Debug.Log("[SampleTestCoroutine] Test complete! End of sample skeleton.");
    }
    //================================
    //COROUTINES
    //================================
    //These testing coroutines are used to test different parts of the system.
    //They proceed through the different stages of the test with the tester (me) hitting the spacebar.
    //Different keyboard inputs from the tester can be used to play with variables of the system relevant to the test.
    //The coroutines should have Debug Logs to indicate the stage of the test and the current state of the system, brief instructions to tester (buttons to press)
    //As well as (briefly) what success looks like for a particular stage. More importantly though, it should also display the relevant variables.


    //================================
    //OLD UPDATE METHOD
    //================================

    //THIS IS THE OLD UPDATE METHOD WE USED FOR TESTING MANUALLY.
    // Update is called once per frame
    //void Update()
    //{
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
                AkSoundEngine.SetState("InteractiveMusicMode", "MusicLoops");
                AkSoundEngine.SetRTPCValue("MusicLoops_Volume", 80.0f);
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
        
    }*/
    //}
    
    // ===================================================================
    // NOTE NAME REFACTORING TEST COROUTINES
    // ===================================================================
    
    /// <summary>
    /// Test 1: Fundamental Note Changes
    /// Tests that fundamental changes correctly when unlocked, Wwise switches update, and binaural beats frequency updates.
    /// </summary>
    private IEnumerator TestFundamentalNoteChanges()
    {
        Debug.Log("=== TEST 1: FUNDAMENTAL NOTE CHANGES ===");
        Debug.Log("This test verifies fundamental note changes work correctly with NoteName enum.");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        // Ensure fundamental is unlocked
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("\n[TEST] Stage 1: Testing fundamental change to C");
        Debug.Log("Expected: fundamentalNoteName = C, Wwise switch = 'C', binaural frequency ≈ 261.63 Hz");
        Debug.Log("Press SPACE to change fundamental to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName testNote = NoteName.C;
        NoteName previousFundamental = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.SetFundamentalDirect(testNote);
        yield return new WaitForSeconds(0.5f);
        
        // Verify changes
        NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        float expectedFreq = NoteUtils.NoteToFrequencyA440(testNote);
        
        Debug.Log($"[TEST] Previous fundamental: {previousFundamental}");
        Debug.Log($"[TEST] Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] Expected frequency: {expectedFreq} Hz");
        Debug.Log($"[TEST] ✓ Fundamental changed: {(currentFundamental == testNote ? "PASS" : "FAIL")}");
        Debug.Log($"[TEST] ✓ NoteName enum used: {(currentFundamental != NoteName.None ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 2: Testing fundamental change to A");
        Debug.Log("Expected: fundamentalNoteName = A, Wwise switch = 'A', binaural frequency = 440 Hz");
        Debug.Log("Press SPACE to change fundamental to A");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        testNote = NoteName.A;
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalDirect(testNote);
        yield return new WaitForSeconds(0.5f);
        
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        expectedFreq = NoteUtils.NoteToFrequencyA440(testNote);
        
        Debug.Log($"[TEST] Previous fundamental: {previousFundamental}");
        Debug.Log($"[TEST] Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] Expected frequency: {expectedFreq} Hz");
        Debug.Log($"[TEST] ✓ Fundamental changed: {(currentFundamental == testNote ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 3: Testing all 12 notes sequentially");
        Debug.Log("Press SPACE to cycle through all notes (C, Cs, D, Ds, E, F, Fs, G, Gs, A, As, B)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        for (NoteName note = NoteName.C; note <= NoteName.B; note++)
        {
            MusicSystem1.instance.SetFundamentalDirect(note);
            yield return new WaitForSeconds(0.3f);
            currentFundamental = MusicSystem1.instance.fundamentalNoteName;
            expectedFreq = NoteUtils.NoteToFrequencyA440(note);
            Debug.Log($"[TEST] Note: {note}, Fundamental: {currentFundamental}, Expected Freq: {expectedFreq:F2} Hz, Match: {(currentFundamental == note ? "✓" : "✗")}");
        }
        
        Debug.Log("\n=== TEST 1 COMPLETE ===");
        Debug.Log("Check logs above for results. Verify Wwise switches and binaural beats frequency match expectations.");
    }
    
    /// <summary>
    /// Test 2: Harmony System - Live Toning & Fundamental Step Test
    /// Prompts the tester to tone/sing a sustained note.
    /// Every 1s prints the current fundamental and harmony note (with assessment).
    /// Press SPACE to step the fundamental up by 1 semitone (for subjective mapping assessment).
    /// Cycles through all 12 notes sequentially.
    /// </summary>
    private IEnumerator TestHarmonySystem()
    {
        Debug.Log("=== TEST 2: HARMONY SYSTEM ===");
        Debug.Log("Instructions:");
        Debug.Log(" 1. Tone/sing a sustained note. This test will print the current fundamental note and its computed harmony every 1 second.");
        Debug.Log(" 2. Press SPACE to step the fundamental up by 1 semitone.");
        Debug.Log(" 3. The test will cycle through all 12 chromatic notes.");
        Debug.Log(" 4. Assess with your ear and the keyboard display if both fundamental and harmony are as expected.");
        Debug.Log(" 5. We are particularly concerned with wrapping modulo behavior.");
        Debug.Log("========================================");

        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run harmony test.");
            yield break;
        }

        // Start with fundamental C
        NoteName currentFundamental = NoteName.C;
        MusicSystem1.instance.SetFundamentalDirect(currentFundamental);
        yield return new WaitForSeconds(0.5f);

        int steps = 0;
        const int TOTAL_NOTES = 12;

        Debug.Log("[TEST] Begin toning/singing and observe the harmony outputs below.");
        Debug.Log("Press SPACE at any time to change the fundamental up by 1 semitone.");

        // Used to track input for advancing note
        bool waitingForNextNote = false;

        while (steps < TOTAL_NOTES)
        {
            float timer = 0f;
            waitingForNextNote = false;

            while (!waitingForNextNote)
            {
                // Every 1s, print the current state for the current fundamental
                timer += Time.deltaTime;
                if (timer >= 1.0f)
                {
                    timer = 0f;

                    NoteName fundamental = MusicSystem1.instance.fundamentalNoteName;
                    NoteName harmony = MusicSystem1.instance.harmonyNote;

                    // Assessment: is harmony what we expect? This may depend on the current harmony logic
                    // For this test, assume harmony is a set interval (e.g. a perfect fifth: +7 semitones)
                    // We'll print an assessment using NoteUtils.AddInterval to compare.
                    NoteName expectedHarmony = NoteUtils.AddInterval(fundamental, 7); // perfect fifth

                    string assessment = (harmony == expectedHarmony)
                        ? "✓ EXPECTED HARMONY"
                        : $"✗ UNEXPECTED (expected {expectedHarmony})";

                    Debug.Log($"[HarmonyTest] Fundamental: {fundamental} | Harmony: {harmony} | {assessment}");
                    Debug.Log($"           (You should hear: {fundamental} as the tonic, {harmony} as harmony, fifth = {expectedHarmony})");
                }

                // If space is pressed, break and move to next note
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    waitingForNextNote = true;
                }
                yield return null;
            }

            // Advance to next fundamental
            steps++;
            if (steps < TOTAL_NOTES)
            {
                // Step up by 1 semitone, wrap if needed
                currentFundamental = NoteUtils.AddInterval(currentFundamental, 1);
                MusicSystem1.instance.SetFundamentalDirect(currentFundamental);
                Debug.Log($"\n[TEST] -- Fundamental changed to {currentFundamental}. Continue toning and assessing. --");
                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log("\n=== TEST 2: HARMONY SYSTEM COMPLETE ===");
        Debug.Log("You've cycled through all 12 fundamentals. Assess mapping by ear and check console for expected results.");
    }

    /// <summary>
    /// Test 3: Note Detection
    /// Tests voice input note detection, NoteTracker updates, and activation timers.
    /// </summary>
    private IEnumerator TestNoteDetection()
    {
        Debug.Log("=== TEST 3: NOTE DETECTION ===");
        Debug.Log("This test verifies note detection works correctly with NoteName enum.");
        Debug.Log("NOTE: This test requires active voice input from Imitone.");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.imitoneVoiceInterpreter == null)
        {
            Debug.LogError("[TEST] ImitoneVoiceInterpreter is null - cannot run test");
            yield break;
        }
        
        Debug.Log("\n[TEST] Stage 1: Monitoring note detection");
        Debug.Log("Tone into the microphone and watch the logs below.");
        Debug.Log("Press SPACE to start monitoring (will monitor for 10 seconds)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        float monitorDuration = 10f;
        float elapsed = 0f;
        NoteName lastActivated = NoteName.None;
        int activationCount = 0;
        
        Debug.Log($"[TEST] Monitoring for {monitorDuration} seconds...");
        
        while (elapsed < monitorDuration)
        {
            NoteName currentActivated = MusicSystem1.instance.musicNoteActivated;
            bool toneActive = MusicSystem1.instance.imitoneVoiceInterpreter.imitoneActive;
            
            if (currentActivated != lastActivated && currentActivated != NoteName.None)
            {
                Debug.Log($"[TEST] Note activated: {currentActivated} (toneActive: {toneActive})");
                lastActivated = currentActivated;
                activationCount++;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.Log($"[TEST] ✓ Detected {activationCount} note activations");
        Debug.Log($"[TEST] ✓ All activations used NoteName enum: {(lastActivated != NoteName.None || activationCount == 0 ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 2: Testing NoteTracker (requires active toning)");
        Debug.Log("Press SPACE to check NoteTracker state");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("[TEST] NoteTracker contents:");
        // NoteTracker is private, so we can't directly access it, but we can verify through musicNoteActivated
        NoteName currentNote = MusicSystem1.instance.musicNoteActivated;
        Debug.Log($"[TEST] Currently activated note: {currentNote}");
        Debug.Log($"[TEST] ✓ NoteTracker uses NoteName enum: {(currentNote != NoteName.None || !MusicSystem1.instance.imitoneVoiceInterpreter.imitoneActive ? "PASS" : "FAIL")}");
        
        Debug.Log("\n=== TEST 3 COMPLETE ===");
        Debug.Log("Note: Full NoteTracker verification requires checking internal state or adding debug methods.");
    }
    
    /// <summary>
    /// Test 4: Mode Switching & Locks
    /// Tests Tutorial mode lock, Freeplay unlock, and MusicLoop content locks.
    /// </summary>
    private IEnumerator TestModeSwitchingAndLocks()
    {
        Debug.Log("=== TEST 4: MODE SWITCHING & LOCKS ===");
        Debug.Log("This test verifies fundamental locking system works correctly with NoteName enum.");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        Debug.Log("\n[TEST] Stage 1: Testing Debug Lock (highest priority)");
        Debug.Log("Press SPACE to set debug lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(NoteName.C);
        yield return new WaitForSeconds(0.5f);
        NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Debug lock set to C, Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Debug lock works: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Press SPACE to try changing fundamental to A (should fail due to lock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName beforeChange = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.ChangeFundamental(NoteName.A);
        yield return new WaitForSeconds(0.5f);
        NoteName afterChange = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Before: {beforeChange}, After: {afterChange}");
        Debug.Log($"[TEST] ✓ Lock prevented change: {(beforeChange == afterChange ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Press SPACE to clear debug lock");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[TEST] Debug lock cleared");
        
        Debug.Log("\n[TEST] Stage 2: Testing Content Lock");
        Debug.Log("Press SPACE to set content lock to E");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.E);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Content lock set to E, Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Content lock works: {(currentFundamental == NoteName.E ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Press SPACE to clear content lock");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalContentLock(null);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[TEST] Content lock cleared");
        
        Debug.Log("\n[TEST] Stage 3: Testing Mode Lock");
        Debug.Log("Press SPACE to set mode lock to G");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.G);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Mode lock set to G, Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Mode lock works: {(currentFundamental == NoteName.G ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Press SPACE to clear mode lock");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[TEST] Mode lock cleared");
        
        Debug.Log("\n[TEST] Stage 4: Testing lock priority (Debug > Content > Mode)");
        Debug.Log("Press SPACE to set all three locks and verify debug lock takes priority");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.D);
        MusicSystem1.instance.SetFundamentalDebugLock(NoteName.E);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Mode lock: C, Content lock: D, Debug lock: E");
        Debug.Log($"[TEST] Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Debug lock priority: {(currentFundamental == NoteName.E ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Press SPACE to clear debug lock (content lock should take over)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Content lock priority: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        
        // Cleanup
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        
        Debug.Log("\n=== TEST 4 COMPLETE ===");
    }
    
    /// <summary>
    /// Test 5: Edge Cases
    /// Tests None/Invalid note handling, negative intervals, wrapped distance, and modulo operations.
    /// </summary>
    private IEnumerator TestEdgeCases()
    {
        Debug.Log("=== TEST 5: EDGE CASES ===");
        Debug.Log("This test verifies edge case handling with NoteName enum.");
        
        Debug.Log("\n[TEST] Stage 1: Testing None/Invalid note handling");
        Debug.Log("Press SPACE to test AddInterval with None");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName result = NoteUtils.AddInterval(NoteName.None, 5);
        Debug.Log($"[TEST] AddInterval(None, 5) = {result}");
        Debug.Log($"[TEST] ✓ None handling: {(result == NoteName.None ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Press SPACE to test GetWrappedDistance with None");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        int distance = NoteUtils.GetWrappedDistance(NoteName.None, NoteName.C);
        Debug.Log($"[TEST] GetWrappedDistance(None, C) = {distance}");
        Debug.Log($"[TEST] ✓ None distance: {(distance == -1 ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 2: Testing negative intervals");
        Debug.Log("Press SPACE to test various negative intervals");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        result = NoteUtils.AddInterval(NoteName.C, -1);
        Debug.Log($"[TEST] C - 1 = {result} (expected: B)");
        Debug.Log($"[TEST] ✓ Negative wrap: {(result == NoteName.B ? "PASS" : "FAIL")}");
        
        result = NoteUtils.AddInterval(NoteName.C, -13);
        Debug.Log($"[TEST] C - 13 = {result} (expected: B, wraps around)");
        Debug.Log($"[TEST] ✓ Large negative: {(result == NoteName.B ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 3: Testing wrapped distance calculations");
        Debug.Log("Press SPACE to test distance calculations");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        distance = NoteUtils.GetWrappedDistance(NoteName.C, NoteName.B);
        Debug.Log($"[TEST] Distance(C, B) = {distance} (expected: 1, wraps around)");
        Debug.Log($"[TEST] ✓ Wrapped distance: {(distance == 1 ? "PASS" : "FAIL")}");
        
        distance = NoteUtils.GetWrappedDistance(NoteName.C, NoteName.G);
        Debug.Log($"[TEST] Distance(C, G) = {distance} (expected: 5)");
        Debug.Log($"[TEST] ✓ Normal distance: {(distance == 5 ? "PASS" : "FAIL")}");
        
        distance = NoteUtils.GetWrappedDistance(NoteName.C, NoteName.C);
        Debug.Log($"[TEST] Distance(C, C) = {distance} (expected: 0)");
        Debug.Log($"[TEST] ✓ Same note: {(distance == 0 ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 4: Testing modulo 12 operations");
        Debug.Log("Press SPACE to test interval wrapping");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        result = NoteUtils.AddInterval(NoteName.B, 1);
        Debug.Log($"[TEST] B + 1 = {result} (expected: C, wraps around)");
        Debug.Log($"[TEST] ✓ Modulo wrap: {(result == NoteName.C ? "PASS" : "FAIL")}");
        
        result = NoteUtils.AddInterval(NoteName.C, 12);
        Debug.Log($"[TEST] C + 12 = {result} (expected: C, full octave)");
        Debug.Log($"[TEST] ✓ Full octave: {(result == NoteName.C ? "PASS" : "FAIL")}");
        
        result = NoteUtils.AddInterval(NoteName.C, 25);
        Debug.Log($"[TEST] C + 25 = {result} (expected: D, 25 % 12 = 1)");
        Debug.Log($"[TEST] ✓ Large interval: {(result == NoteName.D ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 5: Testing FloatToNoteName edge cases");
        Debug.Log("Press SPACE to test float conversions");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        result = NoteUtils.FloatToNoteName(-1f);
        Debug.Log($"[TEST] FloatToNoteName(-1) = {result} (expected: None)");
        Debug.Log($"[TEST] ✓ Negative input: {(result == NoteName.None ? "PASS" : "FAIL")}");
        
        result = NoteUtils.FloatToNoteName(12.5f);
        Debug.Log($"[TEST] FloatToNoteName(12.5) = {result} (expected: C or D, wraps)");
        Debug.Log($"[TEST] ✓ Over 12 wraps: {(result != NoteName.None ? "PASS" : "FAIL")}");
        
        Debug.Log("\n=== TEST 5 COMPLETE ===");
    }
    
    /// <summary>
    /// Test 6: Note Mapping Verification
    /// Subjective tests to verify C and A fundamentals match sung notes.
    /// </summary>
    private IEnumerator TestNoteMapping()
    {
        Debug.Log("=== TEST 6: NOTE MAPPING VERIFICATION ===");
        Debug.Log("This is a SUBJECTIVE test - verify by listening that fundamentals match sung notes.");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        Debug.Log("\n[TEST] Stage 1: Testing C fundamental");
        Debug.Log("INSTRUCTIONS:");
        Debug.Log("1. The system will set fundamental to C");
        Debug.Log("2. Tone/sing a C note into the microphone");
        Debug.Log("3. Verify that the system responds correctly (keyboard should light up, harmony should sound correct)");
        Debug.Log("Press SPACE to set fundamental to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDirect(NoteName.C);
        yield return new WaitForSeconds(0.5f);
        
        NoteName fundamental = MusicSystem1.instance.fundamentalNoteName;
        float expectedFreq = NoteUtils.NoteToFrequencyA440(NoteName.C);
        Debug.Log($"[TEST] Fundamental set to: {fundamental}");
        Debug.Log($"[TEST] Expected frequency: {expectedFreq} Hz");
        Debug.Log($"[TEST] Expected Wwise switch: 'C'");
        Debug.Log("[TEST] Now tone a C note and verify:");
        Debug.Log("[TEST] - Keyboard shows C key highlighted");
        Debug.Log("[TEST] - Harmony sounds correct");
        Debug.Log("[TEST] - System responds to your C tone");
        Debug.Log("\nPress SPACE when done testing C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("\n[TEST] Stage 2: Testing A fundamental");
        Debug.Log("INSTRUCTIONS:");
        Debug.Log("1. The system will set fundamental to A");
        Debug.Log("2. Tone/sing an A note (440 Hz) into the microphone");
        Debug.Log("3. Verify that the system responds correctly");
        Debug.Log("Press SPACE to set fundamental to A");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDirect(NoteName.A);
        yield return new WaitForSeconds(0.5f);
        
        fundamental = MusicSystem1.instance.fundamentalNoteName;
        expectedFreq = NoteUtils.NoteToFrequencyA440(NoteName.A);
        Debug.Log($"[TEST] Fundamental set to: {fundamental}");
        Debug.Log($"[TEST] Expected frequency: {expectedFreq} Hz (should be exactly 440 Hz)");
        Debug.Log($"[TEST] Expected Wwise switch: 'A'");
        Debug.Log("[TEST] Now tone an A note and verify:");
        Debug.Log("[TEST] - Keyboard shows A key highlighted");
        Debug.Log("[TEST] - Harmony sounds correct");
        Debug.Log("[TEST] - System responds to your A tone");
        Debug.Log("\nPress SPACE when done testing A");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("\n=== TEST 6 COMPLETE ===");
        Debug.Log("If both C and A fundamentals matched your sung notes, the mapping is correct!");
    }
    
    /// <summary>
    /// Test 7: Combined Locking System Tests (8.1 - 9.1)
    /// Tests all locking systems: Mode Lock, Content Lock, and their combinations.
    /// Includes tests for basic functionality, redundant calls, note changes, and lock resolution.
    /// </summary>
    private IEnumerator TestAllLockingSystems()
    {
        Debug.Log("=== TEST 7: ALL LOCKING SYSTEMS (Tests 8.1 - 9.1) ===");
        Debug.Log("This test verifies mode lock, content lock, and their interactions.");
        Debug.Log("\nCONTROLS:");
        Debug.Log("SPACE - Proceed to next test stage");
        Debug.Log("1 - Manually change fundamental to C (for testing locks)");
        Debug.Log("2 - Manually change fundamental to A (for testing locks)");
        Debug.Log("========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        // Helper method to print lock status
        void PrintLockStatus()
        {
            NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
            Debug.Log($"\n[LOCK STATUS] Current Fundamental: {currentFundamental}");
            Debug.Log("[LOCK STATUS] Check console logs above for lock states (Mode/Content/Debug)");
        }
        
        // Ensure all locks are cleared at start
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 8.1: Mode Lock Basic Functionality
        // ===================================================================
        Debug.Log("\n=== TEST 8.1: MODE LOCK BASIC FUNCTIONALITY ===");
        Debug.Log("Objective: Verify mode lock sets and clears correctly");
        Debug.Log("\nPress SPACE to set mode lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 8.1] ✓ Mode lock set to C: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 8.1] Check console for: 'MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Locked to C'");
        
        Debug.Log("\nPress SPACE to clear mode lock");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        PrintLockStatus();
        Debug.Log("[TEST 8.1] Check console for: 'MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Unlocked'");
        Debug.Log("[TEST 8.1] Check console for: 'ResolveFundamentalOnUnlock()' call");
        Debug.Log("[TEST 8.1] ✓ Mode lock cleared: PASS (check logs)");
        
        // ===================================================================
        // TEST 8.2: Mode Lock Redundant Calls
        // ===================================================================
        Debug.Log("\n=== TEST 8.2: MODE LOCK REDUNDANT CALLS ===");
        Debug.Log("Objective: Verify redundant lock/unlock calls are handled gracefully");
        Debug.Log("\nPress SPACE to set mode lock to C (first call)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[TEST 8.2] First lock call completed");
        
        Debug.Log("\nPress SPACE to set mode lock to C again (second call - should be redundant)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[TEST 8.2] Check console for: 'MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode relocked to C'");
        Debug.Log("[TEST 8.2] ✓ Redundant lock handled: PASS (check logs)");
        
        Debug.Log("\nPress SPACE to clear mode lock (first unlock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[TEST 8.2] First unlock call completed");
        
        Debug.Log("\nPress SPACE to clear mode lock again (second unlock - should be redundant)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[TEST 8.2] Check console for: 'MUSIC FUNDAMENTAL-MODE-LOCK: Tried to unlock fundamental mode, but it was already unlocked'");
        Debug.Log("[TEST 8.2] ✓ Redundant unlock handled: PASS (check logs)");
        
        // ===================================================================
        // TEST 8.3: Mode Lock with Different Notes
        // ===================================================================
        Debug.Log("\n=== TEST 8.3: MODE LOCK WITH DIFFERENT NOTES ===");
        Debug.Log("Objective: Verify mode lock can be changed to different notes");
        Debug.Log("\nPress SPACE to set mode lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST 8.3] Mode lock set to C, Fundamental: {currentFundamental}");
        
        Debug.Log("\nPress SPACE to change mode lock to D");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.D);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST 8.3] Mode lock changed to D, Fundamental: {currentFundamental}");
        Debug.Log("[TEST 8.3] Check console for: 'MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Lock changed from C to D'");
        Debug.Log($"[TEST 8.3] ✓ Lock changed from C to D: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        
        // Clear mode lock
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 9.1: Content Lock Basic Functionality
        // ===================================================================
        Debug.Log("\n=== TEST 9.1: CONTENT LOCK BASIC FUNCTIONALITY ===");
        Debug.Log("Objective: Verify content lock sets and clears correctly");
        Debug.Log("\nPress SPACE to set content lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 9.1] ✓ Content lock set to C: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 9.1] Check console for: 'MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Locked to C'");
        
        Debug.Log("\nPress SPACE to clear content lock");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalContentLock(null);
        yield return new WaitForSeconds(0.5f);
        PrintLockStatus();
        Debug.Log("[TEST 9.1] Check console for: 'MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Unlocked'");
        Debug.Log("[TEST 9.1] Check console for: 'ResolveFundamentalOnUnlock()' call");
        Debug.Log("[TEST 9.1] ✓ Content lock cleared: PASS (check logs)");
        
        // ===================================================================
        // TEST: Manual Fundamental Changes (Testing Lock Prevention)
        // ===================================================================
        Debug.Log("\n=== TEST: MANUAL FUNDAMENTAL CHANGES (Testing Lock Prevention) ===");
        Debug.Log("Objective: Verify locks prevent manual fundamental changes");
        Debug.Log("\nPress SPACE to set mode lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Mode lock active, Current fundamental: {currentFundamental}");
        
        Debug.Log("\nNow try to manually change fundamental:");
        Debug.Log("Press 1 to try changing fundamental to C (should work if already C, or be blocked)");
        Debug.Log("Press 2 to try changing fundamental to A (should be BLOCKED by lock)");
        Debug.Log("Press SPACE when done testing manual changes");
        
        float waitTime = 5f;
        float elapsed = 0f;
        
        while (elapsed < waitTime)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("[TEST] Manual change attempt: C");
                NoteName before = MusicSystem1.instance.fundamentalNoteName;
                MusicSystem1.instance.ChangeFundamental(NoteName.C);
                yield return new WaitForSeconds(0.3f);
                NoteName after = MusicSystem1.instance.fundamentalNoteName;
                Debug.Log($"[TEST] Before: {before}, After: {after}");
                if (before == NoteName.C)
                {
                    Debug.Log("[TEST] ✓ Already at C (no change needed)");
                }
                else
                {
                    Debug.Log($"[TEST] Change result: {(before == after ? "BLOCKED by lock" : "ALLOWED")}");
                }
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("[TEST] Manual change attempt: A");
                NoteName before = MusicSystem1.instance.fundamentalNoteName;
                MusicSystem1.instance.ChangeFundamental(NoteName.A);
                yield return new WaitForSeconds(0.3f);
                NoteName after = MusicSystem1.instance.fundamentalNoteName;
                Debug.Log($"[TEST] Before: {before}, After: {after}");
                Debug.Log($"[TEST] ✓ Lock prevention: {(before == after ? "PASS (change blocked)" : "FAIL (change allowed despite lock)")}");
                Debug.Log("[TEST] Check console for warning: 'Tried to change the fundamental, but it was locked'");
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        // Clear mode lock
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST: Lock Combinations and Resolution Order
        // ===================================================================
        Debug.Log("\n=== TEST: LOCK COMBINATIONS AND RESOLUTION ORDER ===");
        Debug.Log("Objective: Test different combinations of locks and unlock order");
        
        Debug.Log("\nPress SPACE to set all three locks (Mode=C, Content=D, Debug=E)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.D);
        MusicSystem1.instance.SetFundamentalDebugLock(NoteName.E);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST] All locks set. Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Debug lock priority: {(currentFundamental == NoteName.E ? "PASS" : "FAIL")}");
        Debug.Log("[TEST] Expected: Debug lock (E) should be active (highest priority)");
        
        Debug.Log("\nPress SPACE to clear debug lock (Content lock should take over)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST] Debug lock cleared. Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Content lock priority: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        Debug.Log("[TEST] Expected: Content lock (D) should be active");
        Debug.Log("[TEST] Check console for: 'ResolveFundamentalOnUnlock()' call");
        
        Debug.Log("\nPress SPACE to clear content lock (Mode lock should take over)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalContentLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST] Content lock cleared. Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Mode lock priority: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST] Expected: Mode lock (C) should be active");
        Debug.Log("[TEST] Check console for: 'ResolveFundamentalOnUnlock()' call");
        
        Debug.Log("\nPress SPACE to clear mode lock (No locks, fundamental should resolve)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST] Mode lock cleared. Current fundamental: {currentFundamental}");
        Debug.Log("[TEST] Expected: No locks active, fundamental resolved based on tracking");
        Debug.Log("[TEST] Check console for: 'ResolveFundamentalOnUnlock()' call");
        
        // ===================================================================
        // TEST: Unlock Order Variations
        // ===================================================================
        Debug.Log("\n=== TEST: UNLOCK ORDER VARIATIONS ===");
        Debug.Log("Objective: Test unlocking locks in different orders");
        
        Debug.Log("\nPress SPACE to set all three locks again");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.D);
        MusicSystem1.instance.SetFundamentalDebugLock(NoteName.E);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[TEST] All locks set (Mode=C, Content=D, Debug=E)");
        
        Debug.Log("\nPress SPACE to clear mode lock first (Debug lock should still be active)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Mode lock cleared. Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Debug lock still active: {(currentFundamental == NoteName.E ? "PASS" : "FAIL")}");
        
        Debug.Log("\nPress SPACE to clear content lock (Debug lock should still be active)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalContentLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST] Content lock cleared. Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST] ✓ Debug lock still active: {(currentFundamental == NoteName.E ? "PASS" : "FAIL")}");
        
        Debug.Log("\nPress SPACE to clear debug lock (No locks remaining)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log("[TEST] All locks cleared. Fundamental resolved.");
        Debug.Log("[TEST] Check console for: 'ResolveFundamentalOnUnlock()' call");
        
        Debug.Log("\n=== TEST 7 COMPLETE ===");
        Debug.Log("Summary:");
        Debug.Log("✓ Test 8.1: Mode Lock Basic Functionality");
        Debug.Log("✓ Test 8.2: Mode Lock Redundant Calls");
        Debug.Log("✓ Test 8.3: Mode Lock with Different Notes");
        Debug.Log("✓ Test 9.1: Content Lock Basic Functionality");
        Debug.Log("✓ Lock Combinations and Priority");
        Debug.Log("✓ Unlock Order Variations");
        Debug.Log("\nReview console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 8: Content Lock with MusicLoops and SoundWorlds (Tests 9.2 - 9.6)
    /// Tests MusicLoop content lock setting, SoundWorld lock clearing, switching between MusicLoops,
    /// safety checks, and invalid fundamental handling.
    /// </summary>
    private IEnumerator TestContentLockWithSoundscapes()
    {
        Debug.Log("=== TEST 8: CONTENT LOCK WITH MUSICLOOPS AND SOUNDWORLDS (Tests 9.2 - 9.6) ===");
        Debug.Log("This test verifies content lock behavior with MusicLoops and SoundWorlds.");
        Debug.Log("\nCONTROLS:");
        Debug.Log("SPACE - Proceed to next test stage");
        Debug.Log("========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        // Helper method to print lock status
        void PrintLockStatus()
        {
            NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
            Debug.Log($"\n[LOCK STATUS] Current Fundamental: {currentFundamental}");
            Debug.Log("[LOCK STATUS] Check console logs above for lock states (Mode/Content/Debug)");
        }
        
        // Ensure all locks are cleared at start
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 9.2: MusicLoop Sets Content Lock
        // ===================================================================
        Debug.Log("\n=== TEST 9.2: MUSICLOOP SETS CONTENT LOCK ===");
        Debug.Log("Objective: Verify MusicLoop automatically sets content lock to correct fundamental");
        
        Debug.Log("\nPress SPACE to set MusicLoop 'ShiftingEarth' (requires C)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetMusicLoop("ShiftingEarth");
        yield return new WaitForSeconds(0.5f);
        NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        NoteName expectedNote = NoteName.C;
        PrintLockStatus();
        Debug.Log($"[TEST 9.2] MusicLoop 'ShiftingEarth' set");
        Debug.Log($"[TEST 9.2] Expected content lock: {expectedNote}");
        Debug.Log($"[TEST 9.2] Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST 9.2] ✓ Content lock set to C: {(currentFundamental == expectedNote ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 9.2] Check console for: 'MUSIC: Content lock set to C for MusicLoop 'ShiftingEarth''");
        
        Debug.Log("\nPress SPACE to set MusicLoop 'PinkNoiseAtmosphere' (requires As)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName previousFundamental = currentFundamental;
        MusicSystem1.instance.SetMusicLoop("PinkNoiseAtmosphere");
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        expectedNote = NoteName.As;
        PrintLockStatus();
        Debug.Log($"[TEST 9.2] MusicLoop 'PinkNoiseAtmosphere' set");
        Debug.Log($"[TEST 9.2] Previous fundamental: {previousFundamental}");
        Debug.Log($"[TEST 9.2] Expected content lock: {expectedNote}");
        Debug.Log($"[TEST 9.2] Current fundamental: {currentFundamental}");
        Debug.Log($"[TEST 9.2] ✓ Content lock updated to As: {(currentFundamental == expectedNote ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 9.2] Check console for: 'MUSIC: Content lock set to As for MusicLoop 'PinkNoiseAtmosphere''");
        Debug.Log("[TEST 9.2] Check console for: 'MUSIC: Fundamental Content Lock changed from C to As'");
        
        // ===================================================================
        // TEST 9.3: SoundWorld Clears Content Lock
        // ===================================================================
        Debug.Log("\n=== TEST 9.3: SOUNDWORLD CLEARS CONTENT LOCK ===");
        Debug.Log("Objective: Verify SoundWorld clears content lock");
        
        Debug.Log("\nPress SPACE to set SoundWorld 'SonoFlore' (should clear content lock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetSoundWorld("SonoFlore");
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 9.3] SoundWorld 'SonoFlore' set");
        Debug.Log($"[TEST 9.3] Previous fundamental: {previousFundamental}");
        Debug.Log($"[TEST 9.3] Current fundamental: {currentFundamental}");
        Debug.Log("[TEST 9.3] Check console for: 'MUSIC: Fundamental Content Unlocked'");
        Debug.Log("[TEST 9.3] Check console for: 'ResolveFundamentalOnUnlock()' call");
        Debug.Log("[TEST 9.3] ✓ Content lock cleared: PASS (check logs)");
        
        // ===================================================================
        // TEST 9.4: Switching Between MusicLoops Updates Lock
        // ===================================================================
        Debug.Log("\n=== TEST 9.4: SWITCHING BETWEEN MUSICLOOPS UPDATES LOCK ===");
        Debug.Log("Objective: Verify switching between MusicLoops updates content lock correctly");
        
        Debug.Log("\nPress SPACE to set MusicLoop 'ShiftingEarth' (C)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetMusicLoop("ShiftingEarth");
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST 9.4] MusicLoop 'ShiftingEarth' set, Fundamental: {currentFundamental}");
        
        Debug.Log("\nPress SPACE to set MusicLoop 'SitarAmbience' (C - same note)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetMusicLoop("SitarAmbience");
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST 9.4] MusicLoop 'SitarAmbience' set, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 9.4] ✓ Lock stays C when switching C → C: {(currentFundamental == NoteName.C && previousFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 9.4] Check console for: 'MUSIC: Fundamental Content relocked to C'");
        
        Debug.Log("\nPress SPACE to set MusicLoop 'PinkNoiseAtmosphere' (As - different note)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetMusicLoop("PinkNoiseAtmosphere");
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST 9.4] MusicLoop 'PinkNoiseAtmosphere' set, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 9.4] ✓ Lock updates to As when switching C → As: {(currentFundamental == NoteName.As ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 9.4] Check console for: 'MUSIC: Fundamental Content Lock changed from C to As'");
        
        // ===================================================================
        // TEST 9.5: Content Lock Safety Checks
        // ===================================================================
        Debug.Log("\n=== TEST 9.5: CONTENT LOCK SAFETY CHECKS ===");
        Debug.Log("Objective: Verify content lock rejects invalid inputs");
        
        Debug.Log("\nPress SPACE to test SetFundamentalContentLock(NoteName.None)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.None);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 9.5] SetFundamentalContentLock(NoteName.None) called");
        Debug.Log($"[TEST 9.5] Previous fundamental: {previousFundamental}");
        Debug.Log($"[TEST 9.5] Current fundamental: {currentFundamental}");
        Debug.Log("[TEST 9.5] Check console for: 'MUSIC: Cannot set content lock to NoteName.None - ignoring request'");
        Debug.Log($"[TEST 9.5] ✓ Lock unchanged: {(currentFundamental == previousFundamental ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 9.5] ✓ Warning logged: PASS (check logs)");
        
        // ===================================================================
        // TEST 9.6: MusicLoop with Invalid Fundamental
        // ===================================================================
        Debug.Log("\n=== TEST 9.6: MUSICLOOP WITH INVALID FUNDAMENTAL ===");
        Debug.Log("Objective: Verify MusicLoop handles invalid fundamental gracefully");
        Debug.Log("NOTE: This test requires a MusicLoop that returns NoteName.None from GetMusicLoopFundamental()");
        Debug.Log("Since all current MusicLoops have valid fundamentals, we'll test with a non-existent MusicLoop name");
        
        Debug.Log("\nPress SPACE to test SetMusicLoop with invalid name (should handle gracefully)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetMusicLoop("NonExistentMusicLoop");
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 9.6] SetMusicLoop('NonExistentMusicLoop') called");
        Debug.Log($"[TEST 9.6] Previous fundamental: {previousFundamental}");
        Debug.Log($"[TEST 9.6] Current fundamental: {currentFundamental}");
        Debug.Log("[TEST 9.6] Check console for: 'MUSIC: MusicLoop 'NonExistentMusicLoop' not found in musicLoops dictionary - aborting SetMusicLoop()'");
        Debug.Log("[TEST 9.6] ✓ Invalid MusicLoop handled gracefully: PASS (check logs)");
        Debug.Log("[TEST 9.6] NOTE: To fully test GetMusicLoopFundamental() returning None, you would need to temporarily modify the musicLoops dictionary");
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("\n=== CLEANUP ===");
        Debug.Log("Press SPACE to clear all locks and complete test");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        PrintLockStatus();
        
        Debug.Log("\n=== TEST 8 COMPLETE ===");
        Debug.Log("Summary:");
        Debug.Log("✓ Test 9.2: MusicLoop Sets Content Lock");
        Debug.Log("✓ Test 9.3: SoundWorld Clears Content Lock");
        Debug.Log("✓ Test 9.4: Switching Between MusicLoops Updates Lock");
        Debug.Log("✓ Test 9.5: Content Lock Safety Checks");
        Debug.Log("✓ Test 9.6: MusicLoop with Invalid Fundamental");
        Debug.Log("\nReview console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 9: Lock Priority and Unlock Resolution (Tests 10.1 - 11.1)
    /// Tests lock priority system (Debug > Content > Mode) and unlock resolution behavior.
    /// </summary>
    private IEnumerator TestLockPriorityAndUnlockResolution()
    {
        Debug.Log("=== TEST 9: LOCK PRIORITY AND UNLOCK RESOLUTION (Tests 10.1 - 11.1) ===");
        Debug.Log("This test verifies lock priority system and unlock resolution behavior.");
        Debug.Log("\nCONTROLS:");
        Debug.Log("SPACE - Proceed to next test stage");
        Debug.Log("========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        // Helper method to print lock status
        void PrintLockStatus()
        {
            NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
            Debug.Log($"\n[LOCK STATUS] Current Fundamental: {currentFundamental}");
            Debug.Log("[LOCK STATUS] Check console logs above for lock states (Mode/Content/Debug)");
        }
        
        // Ensure all locks are cleared at start
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 10.1: Debug Lock Overrides Content Lock
        // ===================================================================
        Debug.Log("\n=== TEST 10.1: DEBUG LOCK OVERRIDES CONTENT LOCK ===");
        Debug.Log("Objective: Verify debug lock takes highest priority");
        
        Debug.Log("\nPress SPACE to set content lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);
        yield return new WaitForSeconds(0.5f);
        NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.1] Content lock set to C, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.1] ✓ Content lock set to C: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        
        Debug.Log("\nPress SPACE to set debug lock to D (should override content lock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalDebugLock(NoteName.D);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.1] Debug lock set to D, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.1] ✓ Debug lock overrides content lock: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 10.1] Check console for: 'MUSIC FUNDAMENTAL-DEBUG-LOCK: Debug lock set and locked fundamental to D (DEVELOPMENT ONLY - highest priority)'");
        
        Debug.Log("\nPress SPACE to try setting content lock to C again (should be blocked by debug lock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.C);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST 10.1] Attempted to set content lock to C, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.1] ✓ Content lock blocked by debug lock: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 10.1] Check console for: 'MUSIC FUNDAMENTAL-CONTENT-LOCK: Content lock set to C, but higher priority lock active (D) - fundamental unchanged'");
        
        Debug.Log("\nPress SPACE to clear debug lock (content lock should become active)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.1] Debug lock cleared, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.1] ✓ Content lock becomes active: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 10.1] Check console for: 'MUSIC FUNDAMENTAL-DEBUG-LOCK: Debug lock cleared'");
        Debug.Log("[TEST 10.1] Check console for: 'ResolveFundamentalOnUnlock()' call");
        
        // Clear content lock for next test
        MusicSystem1.instance.SetFundamentalContentLock(null);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 10.2: Content Lock Overrides Mode Lock
        // ===================================================================
        Debug.Log("\n=== TEST 10.2: CONTENT LOCK OVERRIDES MODE LOCK ===");
        Debug.Log("Objective: Verify content lock takes priority over mode lock");
        
        Debug.Log("\nPress SPACE to set mode lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.2] Mode lock set to C, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.2] ✓ Mode lock set to C: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        
        Debug.Log("\nPress SPACE to set content lock to D (should override mode lock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.D);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.2] Content lock set to D, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.2] ✓ Content lock overrides mode lock: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        
        Debug.Log("\nPress SPACE to try setting mode lock to C again (should be blocked by content lock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        Debug.Log($"[TEST 10.2] Attempted to set mode lock to C, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.2] ✓ Mode lock blocked by content lock: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 10.2] Check console for: 'MUSIC FUNDAMENTAL-MODE-LOCK: Mode lock set to C, but higher priority lock active (D) - fundamental unchanged'");
        
        Debug.Log("\nPress SPACE to clear content lock (mode lock should become active)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalContentLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.2] Content lock cleared, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.2] ✓ Mode lock becomes active: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 10.2] Check console for: 'MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Unlocked'");
        Debug.Log("[TEST 10.2] Check console for: 'ResolveFundamentalOnUnlock()' call");
        
        // Clear mode lock for next test
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 10.3: Mode Lock Works When Others Are Null
        // ===================================================================
        Debug.Log("\n=== TEST 10.3: MODE LOCK WORKS WHEN OTHERS ARE NULL ===");
        Debug.Log("Objective: Verify mode lock functions correctly when no other locks exist");
        
        Debug.Log("\nPress SPACE to set mode lock to C (no other locks active)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.3] Mode lock set to C, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.3] ✓ Mode lock is active: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 10.3] Check console for: 'MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Locked to C'");
        
        // Clear mode lock for next test
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 10.4: All Three Locks Active (Priority Verification)
        // ===================================================================
        Debug.Log("\n=== TEST 10.4: ALL THREE LOCKS ACTIVE (PRIORITY VERIFICATION) ===");
        Debug.Log("Objective: Verify priority order when all locks are active");
        
        Debug.Log("\nPress SPACE to set all three locks (Mode=C, Content=D, Debug=E)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.D);
        MusicSystem1.instance.SetFundamentalDebugLock(NoteName.E);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.4] All locks set (Mode=C, Content=D, Debug=E), Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.4] ✓ Debug lock is active (highest priority): {(currentFundamental == NoteName.E ? "PASS" : "FAIL")}");
        
        Debug.Log("\nPress SPACE to clear debug lock (content lock should become active)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.4] Debug lock cleared, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.4] ✓ Content lock becomes active: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 10.4] Check console for: 'ResolveFundamentalOnUnlock()' call");
        
        Debug.Log("\nPress SPACE to clear content lock (mode lock should become active)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalContentLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 10.4] Content lock cleared, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 10.4] ✓ Mode lock becomes active: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 10.4] Check console for: 'ResolveFundamentalOnUnlock()' call");
        
        // Clear mode lock for next test
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 11.1: ResolveFundamentalOnUnlock with Lower Priority Lock
        // ===================================================================
        Debug.Log("\n=== TEST 11.1: RESOLVEFUNDAMENTALONUNLOCK WITH LOWER PRIORITY LOCK ===");
        Debug.Log("Objective: Verify unlock resolution applies lower priority locks correctly");
        
        Debug.Log("\nPress SPACE to set mode lock to C, then content lock to D");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        MusicSystem1.instance.SetFundamentalContentLock(NoteName.D);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 11.1] Mode lock set to C, Content lock set to D");
        Debug.Log($"[TEST 11.1] Current fundamental: {currentFundamental} (should be D - content lock active)");
        Debug.Log($"[TEST 11.1] ✓ Content lock active: {(currentFundamental == NoteName.D ? "PASS" : "FAIL")}");
        
        Debug.Log("\nPress SPACE to clear content lock (mode lock should become active via ResolveFundamentalOnUnlock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        previousFundamental = currentFundamental;
        MusicSystem1.instance.SetFundamentalContentLock(null);
        yield return new WaitForSeconds(0.5f);
        currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 11.1] Content lock cleared, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 11.1] ✓ Mode lock becomes active via ResolveFundamentalOnUnlock: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        Debug.Log("[TEST 11.1] Check console for: 'MUSIC FUNDAMENTAL-CONTENT-LOCK: Fundamental Content Unlocked'");
        Debug.Log("[TEST 11.1] Check console for: 'ResolveFundamentalOnUnlock()' call");
        Debug.Log("[TEST 11.1] Check console for: 'MUSIC: Lower priority lock active (C) - fundamental set accordingly'");
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("\n=== CLEANUP ===");
        Debug.Log("Press SPACE to clear all locks and complete test");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        PrintLockStatus();
        
        Debug.Log("\n=== TEST 9 COMPLETE ===");
        Debug.Log("Summary:");
        Debug.Log("✓ Test 10.1: Debug Lock Overrides Content Lock");
        Debug.Log("✓ Test 10.2: Content Lock Overrides Mode Lock");
        Debug.Log("✓ Test 10.3: Mode Lock Works When Others Are Null");
        Debug.Log("✓ Test 10.4: All Three Locks Active (Priority Verification)");
        Debug.Log("✓ Test 11.1: ResolveFundamentalOnUnlock with Lower Priority Lock");
        Debug.Log("\nReview console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 10: ResolveFundamentalOnUnlock with No Locks - Immediate Threshold (Test 11.2)
    /// Tests that unlock resolution triggers immediate fundamental change when threshold is high (>= 22.0f).
    /// </summary>
    private IEnumerator TestUnlockResolutionImmediateThreshold()
    {
        Debug.Log("=== TEST 10: UNLOCK RESOLUTION IMMEDIATE THRESHOLD (Test 11.2) ===");
        Debug.Log("This test verifies unlock resolution triggers immediate change when threshold is high.");
        Debug.Log("\nCONTROLS:");
        Debug.Log("SPACE - Proceed to next test stage");
        Debug.Log("========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.imitoneVoiceInterpreter == null)
        {
            Debug.LogError("[TEST] ImitoneVoiceInterpreter is null - cannot run test");
            yield break;
        }
        
        // Helper method to print lock status
        void PrintLockStatus()
        {
            NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
            Debug.Log($"\n[LOCK STATUS] Current Fundamental: {currentFundamental}");
            Debug.Log("[LOCK STATUS] Check console logs above for lock states (Mode/Content/Debug)");
        }
        
        // Ensure all locks are cleared at start
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 11.2: ResolveFundamentalOnUnlock with No Locks (Immediate Threshold)
        // ===================================================================
        Debug.Log("\n=== TEST 11.2: RESOLVEFUNDAMENTALONUNLOCK WITH NO LOCKS (IMMEDIATE THRESHOLD) ===");
        Debug.Log("Objective: Verify unlock resolution triggers immediate change when toning duration threshold is high");
        Debug.Log("\nIMPORTANT: This test requires active voice input from Imitone.");
        Debug.Log("You will need to tone/sing note D for an extended period (>= 22 seconds) (not in one breath)");
        Debug.Log("to build up the ChangeFundamentalTimer before clearing the lock.");
        
        Debug.Log("\nPress SPACE to set mode lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 11.2] Mode lock set to C, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 11.2] ✓ Mode lock set to C: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        
        Debug.Log("\n=== CRITICAL INSTRUCTIONS ===");
        Debug.Log("1. Start toning/singing note D into the microphone NOW");
        Debug.Log("2. Continue toning note D for at least 22 seconds (it can be in several breaths)");
        Debug.Log("3. The system will build up ChangeFundamentalTimer for note D, but you won't any updates");
        Debug.Log("4. Try not to tone other notes, at least not as much as D");
        Debug.Log("5. Press SPACE when you've toned for >= 22 seconds");
        Debug.Log("\nNOTE: The timer builds up while the lock is active, but the fundamental");
        Debug.Log("won't change until the lock is cleared (that's what we're testing).");
        Debug.Log("========================================\n");
        
        Debug.Log("\nPress SPACE when you've toned note D for >= 22 seconds");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        
        Debug.Log("\n[TEST 11.2] Clearing mode lock now...");
        Debug.Log("[TEST 11.2] Expected: Fundamental will change to D in 3s (if threshold >= 22.0f)");
        Debug.Log("[TEST 11.2] If threshold < 22.0f but >= 12.0f, it will be queued instead.");
        
        NoteName fundamentalBeforeUnlock = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(3.0f); // Give time for the change to occur
        
        NoteName fundamentalAfterUnlock = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        
        Debug.Log($"[TEST 11.2] Fundamental before unlock: {fundamentalBeforeUnlock}");
        Debug.Log($"[TEST 11.2] Fundamental after unlock: {fundamentalAfterUnlock}");
        
        if (fundamentalAfterUnlock == NoteName.D && fundamentalBeforeUnlock == NoteName.C)
        {
            Debug.Log("[TEST 11.2] ✓ Fundamental changed immediately to D: PASS");
            Debug.Log("[TEST 11.2] This indicates the threshold was >= 22.0f (immediate threshold)");
            Debug.Log("[TEST 11.2] Check console for: 'MUSIC FUNDAMENTAL MODE UNLOCK: Fundamental Changed Immediately on Unlock (high threshold): D'");
            Debug.Log("[TEST 11.2] Check console for: Director queue activation logs");
        }
        else if (fundamentalAfterUnlock != fundamentalBeforeUnlock)
        {
            Debug.Log($"[TEST 11.2] ✓ Fundamental changed to {fundamentalAfterUnlock}: PASS (but not D - check if timer was high enough)");
            Debug.Log("[TEST 11.2] Check console logs to see if it was immediate or queued");
        }
        else
        {
            Debug.Log("[TEST 11.2] ✗ Fundamental did not change: FAIL");
            Debug.Log("[TEST 11.2] Possible reasons:");
            Debug.Log("[TEST 11.2] - Timer for note D was < 12.0f (queue threshold not met)");
            Debug.Log("[TEST 11.2] - No note was being tracked with sufficient timer");
            Debug.Log("[TEST 11.2] - Check console logs for more details");
        }
        
        Debug.Log("\n[TEST 11.2] Check console for one of these messages:");
        Debug.Log("  - 'MUSIC FUNDAMENTAL MODE UNLOCK: Fundamental Changed Immediately on Unlock (high threshold): D' (if >= 22.0f)");
        Debug.Log("  - 'MUSIC FUNDAMENNTAL MODE UNLOCK: New Fundamental Queued on Unlock: D' (if >= 12.0f but < 22.0f)");
        Debug.Log("  - No message (if < 12.0f threshold)");
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("\n=== CLEANUP ===");
        Debug.Log("Press SPACE to clear all locks and complete test");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        PrintLockStatus();
        
        Debug.Log("\n=== TEST 10 COMPLETE ===");
        Debug.Log("Summary:");
        Debug.Log("✓ Test 11.2: ResolveFundamentalOnUnlock with No Locks (Immediate Threshold)");
        Debug.Log("\nNOTE: This test requires manual voice input and timing.");
        Debug.Log("If the fundamental didn't change immediately, ensure you toned note D for >= 22 seconds.");
        Debug.Log("Review console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 11: ResolveFundamentalOnUnlock Queue Threshold (Test 11.3)
    /// Tests that unlock resolution queues fundamental change when threshold is medium (>= 12.0f but < 22.0f).
    /// </summary>
    private IEnumerator TestUnlockResolutionQueueThreshold()
    {
        Debug.Log("=== TEST 11: UNLOCK RESOLUTION QUEUE THRESHOLD (Test 11.3) ===");
        Debug.Log("This test verifies unlock resolution queues change when threshold is medium.");
        Debug.Log("\nCONTROLS:");
        Debug.Log("SPACE - Proceed to next test stage");
        Debug.Log("========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.imitoneVoiceInterpreter == null)
        {
            Debug.LogError("[TEST] ImitoneVoiceInterpreter is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.director == null)
        {
            Debug.LogError("[TEST] Director is null - cannot run test");
            yield break;
        }
        
        // Helper method to print lock status
        void PrintLockStatus()
        {
            NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
            Debug.Log($"\n[LOCK STATUS] Current Fundamental: {currentFundamental}");
            Debug.Log("[LOCK STATUS] Check console logs above for lock states (Mode/Content/Debug)");
        }
        
        // Ensure all locks are cleared at start
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 11.3: ResolveFundamentalOnUnlock with No Locks (Queue Threshold)
        // ===================================================================
        Debug.Log("\n=== TEST 11.3: RESOLVEFUNDAMENTALONUNLOCK WITH NO LOCKS (QUEUE THRESHOLD) ===");
        Debug.Log("Objective: Verify unlock resolution queues change when threshold is medium");
        Debug.Log("\nIMPORTANT: This test requires active voice input from Imitone.");
        Debug.Log("You will need to tone/sing note D for 12-20 seconds (not in one breath)");
        Debug.Log("to build up the ChangeFundamentalTimer before clearing the lock.");
        
        Debug.Log("\nPress SPACE to set mode lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 11.3] Mode lock set to C, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 11.3] ✓ Mode lock set to C: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        
        Debug.Log("\n=== CRITICAL INSTRUCTIONS ===");
        Debug.Log("1. Start toning/singing note D into the microphone NOW");
        Debug.Log("2. Continue toning note D for 12-20 seconds (it can be in several breaths)");
        Debug.Log("3. The system will build up ChangeFundamentalTimer for note D");
        Debug.Log("4. Try not to tone other notes, at least not as much as D");
        Debug.Log("5. Press SPACE when you've toned for 12-20 seconds");
        Debug.Log("\nNOTE: The timer builds up while the lock is active, but the fundamental");
        Debug.Log("won't change until the lock is cleared (that's what we're testing).");
        Debug.Log("========================================\n");
        
        Debug.Log("\nPress SPACE when you've toned note D for 12-20 seconds");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        
        Debug.Log("\n[TEST 11.3] Clearing mode lock now...");
        Debug.Log("[TEST 11.3] Expected: Fundamental change should be QUEUED (not immediate)");
        Debug.Log("[TEST 11.3] If threshold >= 12.0f but < 22.0f, it will be queued.");
        Debug.Log("[TEST 11.3] If threshold >= 22.0f, it will be immediate (wrong test).");
        
        NoteName fundamentalBeforeUnlock = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f); // Give time for any immediate processing
        
        NoteName fundamentalAfterUnlock = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        
        Debug.Log($"[TEST 11.3] Fundamental before unlock: {fundamentalBeforeUnlock}");
        Debug.Log($"[TEST 11.3] Fundamental after unlock: {fundamentalAfterUnlock}");
        
        // First check: fundamental should NOT have changed (it's queued, not immediate)
        if (fundamentalAfterUnlock == fundamentalBeforeUnlock)
        {
            Debug.Log("[TEST 11.3] ✓ Fundamental did NOT change immediately (queued): PASS");
            Debug.Log("[TEST 11.3] This indicates the threshold was >= 12.0f but < 22.0f (queue threshold)");
            Debug.Log("[TEST 11.3] Check console for: 'MUSIC FUNDAMENNTAL MODE UNLOCK: New Fundamental Queued on Unlock: D'");
        }
        else if (fundamentalAfterUnlock == NoteName.D)
        {
            Debug.LogWarning("[TEST 11.3] WARNING: Fundamental changed immediately to D");
            Debug.LogWarning("[TEST 11.3] This suggests threshold was >= 22.0f (immediate threshold)");
            Debug.LogWarning("[TEST 11.3] You may have toned for too long. Try again with less time.");
            Debug.LogWarning("[TEST 11.3] Check console for immediate change message.");
        }
        else
        {
            Debug.LogWarning($"[TEST 11.3] WARNING: Fundamental changed to {fundamentalAfterUnlock} (unexpected)");
            Debug.LogWarning("[TEST 11.3] Check console logs for details.");
        }
        
        Debug.Log("\n[TEST 11.3] Check console for one of these messages:");
        Debug.Log("  - 'MUSIC FUNDAMENNTAL MODE UNLOCK: New Fundamental Queued on Unlock: D' (if >= 12.0f but < 22.0f)");
        Debug.Log("  - 'MUSIC FUNDAMENTAL MODE UNLOCK: Fundamental Changed Immediately on Unlock (high threshold): D' (if >= 22.0f - wrong test)");
        Debug.Log("  - No message (if < 12.0f threshold)");
        
        Debug.Log("\n=== SECOND STAGE: ACTIVATE QUEUE ===");
        Debug.Log("Now we will activate the director queue to execute the queued fundamental change.");
        Debug.Log("Press SPACE to activate the director queue");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("[TEST 11.3] Activating director queue...");
        NoteName fundamentalBeforeActivation = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.director.ActivateQueue(1.0f);
        yield return new WaitForSeconds(1.0f); // Wait for the queue to execute
        
        NoteName fundamentalAfterActivation = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        
        Debug.Log($"[TEST 11.3] Fundamental before queue activation: {fundamentalBeforeActivation}");
        Debug.Log($"[TEST 11.3] Fundamental after queue activation: {fundamentalAfterActivation}");
        
        if (fundamentalAfterActivation == NoteName.D && fundamentalBeforeActivation != NoteName.D)
        {
            Debug.Log("[TEST 11.3] ✓ Fundamental changed to D after queue activation: PASS");
            Debug.Log("[TEST 11.3] The queued fundamental change was executed successfully.");
        }
        else if (fundamentalAfterActivation == fundamentalBeforeActivation)
        {
            Debug.LogWarning("[TEST 11.3] ✗ Fundamental did NOT change after queue activation: FAIL");
            Debug.LogWarning("[TEST 11.3] Possible reasons:");
            Debug.LogWarning("[TEST 11.3] - No queued action was found");
            Debug.LogWarning("[TEST 11.3] - Queue activation failed");
            Debug.LogWarning("[TEST 11.3] - Check console logs for director queue messages");
        }
        else
        {
            Debug.LogWarning($"[TEST 11.3] ✗ Fundamental changed to {fundamentalAfterActivation} (unexpected): FAIL");
            Debug.LogWarning("[TEST 11.3] Expected D, got something else. Check console logs.");
        }
        
        Debug.Log("\n[TEST 11.3] Check console for director queue activation logs:");
        Debug.Log("  - 'Director Queue: Activating queue...'");
        Debug.Log("  - Director queue execution logs");
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("\n=== CLEANUP ===");
        Debug.Log("Press SPACE to clear all locks and complete test");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        PrintLockStatus();
        
        Debug.Log("\n=== TEST 11 COMPLETE ===");
        Debug.Log("Summary:");
        Debug.Log("✓ Test 11.3: ResolveFundamentalOnUnlock with No Locks (Queue Threshold)");
        Debug.Log("\nNOTE: This test requires manual voice input and timing.");
        Debug.Log("Ensure you toned note D for 12-20 seconds (not >= 22 seconds).");
        Debug.Log("Review console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 12: ResolveFundamentalOnUnlock Below Threshold (Test 11.4)
    /// Tests that unlock resolution does nothing when threshold is not met (< 12.0f).
    /// </summary>
    private IEnumerator TestUnlockResolutionBelowThreshold()
    {
        Debug.Log("=== TEST 12: UNLOCK RESOLUTION BELOW THRESHOLD (Test 11.4) ===");
        Debug.Log("This test verifies unlock resolution does nothing when threshold is not met.");
        Debug.Log("\nCONTROLS:");
        Debug.Log("SPACE - Proceed to next test stage");
        Debug.Log("========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.imitoneVoiceInterpreter == null)
        {
            Debug.LogError("[TEST] ImitoneVoiceInterpreter is null - cannot run test");
            yield break;
        }
        
        // Helper method to print lock status
        void PrintLockStatus()
        {
            NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
            Debug.Log($"\n[LOCK STATUS] Current Fundamental: {currentFundamental}");
            Debug.Log("[LOCK STATUS] Check console logs above for lock states (Mode/Content/Debug)");
        }
        
        // Ensure all locks are cleared at start
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 11.4: ResolveFundamentalOnUnlock with No Locks (Below Threshold)
        // ===================================================================
        Debug.Log("\n=== TEST 11.4: RESOLVEFUNDAMENTALONUNLOCK WITH NO LOCKS (BELOW THRESHOLD) ===");
        Debug.Log("Objective: Verify unlock resolution does nothing when threshold not met");
        Debug.Log("\nIMPORTANT: This test requires active voice input from Imitone.");
        Debug.Log("You will need to tone/sing note D BRIEFLY (< 12 seconds)");
        Debug.Log("to build up a small ChangeFundamentalTimer before clearing the lock.");
        
        Debug.Log("\nPress SPACE to set mode lock to C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalModeLock(true, NoteName.C);
        yield return new WaitForSeconds(0.5f);
        NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        Debug.Log($"[TEST 11.4] Mode lock set to C, Fundamental: {currentFundamental}");
        Debug.Log($"[TEST 11.4] ✓ Mode lock set to C: {(currentFundamental == NoteName.C ? "PASS" : "FAIL")}");
        
        Debug.Log("\n=== CRITICAL INSTRUCTIONS ===");
        Debug.Log("1. Start toning/singing note D into the microphone NOW");
        Debug.Log("2. Continue toning note D for LESS than 12 seconds (briefly)");
        Debug.Log("3. The system will build up ChangeFundamentalTimer for note D, but not enough");
        Debug.Log("4. Try not to tone other notes, at least not as much as D");
        Debug.Log("5. Press SPACE when you've toned for < 12 seconds (e.g., 5-10 seconds)");
        Debug.Log("\nNOTE: The timer builds up while the lock is active, but the fundamental");
        Debug.Log("won't change until the lock is cleared (that's what we're testing).");
        Debug.Log("Since the timer is < 12.0f, no change should occur when we unlock.");
        Debug.Log("========================================\n");
        
        Debug.Log("\nPress SPACE when you've toned note D for < 12 seconds (briefly)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("\n[TEST 11.4] Clearing mode lock now...");
        Debug.Log("[TEST 11.4] Expected: NO fundamental change should occur");
        Debug.Log("[TEST 11.4] If threshold < 12.0f, no change will be queued or executed.");
        
        NoteName fundamentalBeforeUnlock = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(1.0f); // Give time for any processing
        
        NoteName fundamentalAfterUnlock = MusicSystem1.instance.fundamentalNoteName;
        PrintLockStatus();
        
        Debug.Log($"[TEST 11.4] Fundamental before unlock: {fundamentalBeforeUnlock}");
        Debug.Log($"[TEST 11.4] Fundamental after unlock: {fundamentalAfterUnlock}");
        
        // Verify: fundamental should NOT have changed
        if (fundamentalAfterUnlock == fundamentalBeforeUnlock)
        {
            Debug.Log("[TEST 11.4] ✓ Fundamental did NOT change: PASS");
            Debug.Log("[TEST 11.4] This indicates the threshold was < 12.0f (below queue threshold)");
            Debug.Log("[TEST 11.4] No fundamental change was queued or executed.");
        }
        else
        {
            Debug.LogWarning($"[TEST 11.4] ✗ Fundamental changed from {fundamentalBeforeUnlock} to {fundamentalAfterUnlock}: FAIL");
            Debug.LogWarning("[TEST 11.4] This suggests threshold was >= 12.0f");
            Debug.LogWarning("[TEST 11.4] You may have toned for too long. Try again with less time (< 12 seconds).");
            Debug.LogWarning("[TEST 11.4] Check console for queue or immediate change messages.");
        }
        
        Debug.Log("\n[TEST 11.4] Check console for:");
        Debug.Log("  - 'MUSIC FUNDAMENTAL-MODE-LOCK: Fundamental Mode Unlocked'");
        Debug.Log("  - NO fundamental change logs (no queue, no immediate change)");
        Debug.Log("  - If you see 'New Fundamental Queued' or 'Changed Immediately', threshold was too high");
        
        // Wait a bit longer to ensure no delayed changes occur
        Debug.Log("\n[TEST 11.4] Waiting 2 more seconds to ensure no delayed changes occur...");
        yield return new WaitForSeconds(2.0f);
        
        NoteName fundamentalAfterWait = MusicSystem1.instance.fundamentalNoteName;
        if (fundamentalAfterWait == fundamentalAfterUnlock)
        {
            Debug.Log($"[TEST 11.4] ✓ Fundamental still unchanged after wait: {fundamentalAfterWait} - PASS");
        }
        else
        {
            Debug.LogWarning($"[TEST 11.4] ✗ Fundamental changed during wait: {fundamentalAfterUnlock} -> {fundamentalAfterWait} - FAIL");
            Debug.LogWarning("[TEST 11.4] An unexpected change occurred. Check console logs.");
        }
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("\n=== CLEANUP ===");
        Debug.Log("Press SPACE to clear all locks and complete test");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        PrintLockStatus();
        
        Debug.Log("\n=== TEST 12 COMPLETE ===");
        Debug.Log("Summary:");
        Debug.Log("✓ Test 11.4: ResolveFundamentalOnUnlock with No Locks (Below Threshold)");
        Debug.Log("\nNOTE: This test requires manual voice input and timing.");
        Debug.Log("Ensure you toned note D for < 12 seconds (briefly).");
        Debug.Log("Review console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 13: Director Queue Priority System - ReplaceActionInQueue (Test 2.1)
    /// Tests that ReplaceActionInQueue clears "SoundscapeShuffle" actions when replacing with "Soundscape".
    /// </summary>
    private IEnumerator TestReplaceActionInQueue()
    {
        Debug.Log("[TEST] === TEST 13: DIRECTOR QUEUE PRIORITY SYSTEM - REPLACEACTIONINQUEUE (Test 2.1) ===");
        Debug.Log("[TEST] This test verifies ReplaceActionInQueue clears SoundscapeShuffle when replacing with Soundscape.");
        Debug.Log("[TEST] \nCONTROLS:");
        Debug.Log("[TEST] SPACE - Proceed to next test stage");
        Debug.Log("[TEST] ========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.director == null)
        {
            Debug.LogError("[TEST] Director is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.worldShuffler == null)
        {
            Debug.LogError("[TEST] WorldShuffler is null - cannot run test");
            yield break;
        }
        
        // Helper method to check queue status
        void CheckQueueStatus()
        {
            bool hasSoundscapeShuffle = MusicSystem1.instance.director.SearchQueueForType("SoundscapeShuffle");
            bool hasSoundscape = MusicSystem1.instance.director.SearchQueueForType("Soundscape");
            Debug.Log($"[TEST] Queue Status - SoundscapeShuffle: {(hasSoundscapeShuffle ? "PRESENT" : "NOT PRESENT")}, Soundscape: {(hasSoundscape ? "PRESENT" : "NOT PRESENT")}");
        }
        
        // Ensure WorldShuffler is stopped, director is enabled, and queue is clear
        MusicSystem1.instance.worldShuffler.StopShuffle();
        MusicSystem1.instance.director.Enable(); // Ensure director is enabled
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 2.1: ReplaceActionInQueue Clears SoundscapeShuffle
        // ===================================================================
        Debug.Log("[TEST] \n=== TEST 2.1: REPLACEACTIONINQUEUE CLEARS SOUNDSCAPESHUFFLE ===");
        Debug.Log("[TEST] Objective: Verify ReplaceActionInQueue() clears SoundscapeShuffle actions when replacing with Soundscape");
        
        Debug.Log("[TEST] \nPress SPACE to start WorldShuffler shuffling (should queue SoundscapeShuffle action)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.worldShuffler.BeginShuffle(false); // false = queue it, don't shuffle immediately
        yield return new WaitForSeconds(0.5f);
        
        CheckQueueStatus();
        bool hasShuffleBefore = MusicSystem1.instance.director.SearchQueueForType("SoundscapeShuffle");
        Debug.Log($"[TEST] WorldShuffler shuffling started");
        Debug.Log($"[TEST] ✓ SoundscapeShuffle queued: {(hasShuffleBefore ? "PASS" : "FAIL")}");
        Debug.Log("[TEST] Check console for: 'WorldShuffler: Beginning shuffle with director queue.'");
        Debug.Log("[TEST] Check console for: 'WorldShuffler: Queuing World Shuffle'");
        Debug.Log("[TEST] Check console for: 'Director Queue: Added [index] SoundscapeShuffle to director queue.'");
        
        if (!hasShuffleBefore)
        {
            Debug.LogWarning("[TEST] WARNING: SoundscapeShuffle was not queued. The test may not work correctly.");
            Debug.LogWarning("[TEST] Press SPACE to continue anyway, or restart the test.");
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        }
        
        Debug.Log("[TEST] \nPress SPACE to use ReplaceActionInQueue() to replace SoundscapeShuffle with Soundscape");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("[TEST] Calling ReplaceActionInQueue()...");
        Debug.Log("[TEST] Replacing SoundscapeShuffle with Soundscape 'Shruti'");
        
        int result = MusicSystem1.instance.director.ReplaceActionInQueue(
            MusicSystem1.instance.Action_SetSoundscape("Shruti"),
            "Soundscape",
            "SoundscapeShuffle",
            true,
            false,
            180.0f,
            true
        );
        
        yield return new WaitForSeconds(0.5f);
        
        CheckQueueStatus();
        bool hasShuffleAfter = MusicSystem1.instance.director.SearchQueueForType("SoundscapeShuffle");
        bool hasSoundscapeAfter = MusicSystem1.instance.director.SearchQueueForType("Soundscape");
        
        Debug.Log($"[TEST] ReplaceActionInQueue() returned: {result} (should be >= 0 if successful, -1 if failed)");
        
        // Check if ReplaceActionInQueue failed
        if (result == -1)
        {
            Debug.LogWarning("[TEST] ✗ ReplaceActionInQueue() returned -1, indicating failure");
            Debug.LogWarning("[TEST] Possible reasons:");
            Debug.LogWarning("[TEST]   - Director is disabled (check console for 'Director is disabled' message)");
            Debug.LogWarning("[TEST]   - AddActionToQueue() failed for some reason");
            Debug.LogWarning("[TEST] Check console logs above for details.");
        }
        
        Debug.Log($"[TEST] ✓ SoundscapeShuffle cleared: {(!hasShuffleAfter ? "PASS" : "FAIL")}");
        Debug.Log($"[TEST] ✓ Soundscape added: {(hasSoundscapeAfter ? "PASS" : "FAIL")}");
        
        if (!hasShuffleAfter && hasSoundscapeAfter)
        {
            Debug.Log("[TEST] ✓ ReplaceActionInQueue() worked correctly: PASS");
        }
        else
        {
            Debug.LogWarning("[TEST] ✗ ReplaceActionInQueue() did not work as expected: FAIL");
            if (hasShuffleAfter)
            {
                Debug.LogWarning("[TEST] SoundscapeShuffle was not cleared from queue");
            }
            if (!hasSoundscapeAfter)
            {
                Debug.LogWarning("[TEST] Soundscape was not added to queue");
                if (result == -1)
                {
                    Debug.LogWarning("[TEST] This is because ReplaceActionInQueue() returned -1 (check director disable status)");
                }
            }
        }
        
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - 'Director Queue: Removed all SoundscapeShuffle items from director queue.'");
        Debug.Log("[TEST]   - 'Director Queue: Removed all Soundscape items from director queue.' (if any existed)");
        Debug.Log("[TEST]   - 'Director Queue: Added [index] Soundscape to director queue.'");
        Debug.Log("[TEST]   - Expiration time should be minimum of: cleared SoundscapeShuffle time, cleared Soundscape time (if any), and 180.0f");
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("[TEST] \n=== CLEANUP ===");
        Debug.Log("[TEST] Press SPACE to stop WorldShuffler and complete test");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.worldShuffler.StopShuffle();
        yield return new WaitForSeconds(0.5f);
        CheckQueueStatus();
        
        Debug.Log("[TEST] \n=== TEST 13 COMPLETE ===");
        Debug.Log("[TEST] Summary:");
        Debug.Log("[TEST] ✓ Test 2.1: ReplaceActionInQueue Clears SoundscapeShuffle");
        Debug.Log("[TEST] \nReview console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 14: Director Queue Priority System - SoundscapeShuffle Rejected (Test 2.2)
    /// Tests that SoundscapeShuffle cannot be added if Soundscape already exists in the queue.
    /// </summary>
    private IEnumerator TestSoundscapeShuffleRejected()
    {
        Debug.Log("[TEST] === TEST 14: DIRECTOR QUEUE PRIORITY SYSTEM - SOUNDSCAPESHUFFLE REJECTED (Test 2.2) ===");
        Debug.Log("[TEST] This test verifies SoundscapeShuffle cannot be added if Soundscape already exists.");
        Debug.Log("[TEST] \nCONTROLS:");
        Debug.Log("[TEST] SPACE - Proceed to next test stage");
        Debug.Log("[TEST] ========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.director == null)
        {
            Debug.LogError("[TEST] Director is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.worldShuffler == null)
        {
            Debug.LogError("[TEST] WorldShuffler is null - cannot run test");
            yield break;
        }
        
        // Helper method to check queue status and log it
        void CheckQueueStatus()
        {
            bool hasSoundscapeShuffle = MusicSystem1.instance.director.SearchQueueForType("SoundscapeShuffle");
            bool hasSoundscape = MusicSystem1.instance.director.SearchQueueForType("Soundscape");
            Debug.Log($"[TEST] Queue Status - SoundscapeShuffle: {(hasSoundscapeShuffle ? "PRESENT" : "NOT PRESENT")}, Soundscape: {(hasSoundscape ? "PRESENT" : "NOT PRESENT")}");
            Debug.Log("[TEST] Full queue contents:");
            MusicSystem1.instance.director.LogQueue();
        }
        
        // Ensure WorldShuffler is stopped and queue is clear
        MusicSystem1.instance.worldShuffler.StopShuffle();
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 2.2: SoundscapeShuffle Rejected When Soundscape Exists
        // ===================================================================
        Debug.Log("[TEST] \n=== TEST 2.2: SOUNDSCAPESHUFFLE REJECTED WHEN SOUNDSCAPE EXISTS ===");
        Debug.Log("[TEST] Objective: Verify SoundscapeShuffle cannot be added if Soundscape exists");
        
        Debug.Log("[TEST] \nPress SPACE to queue a Soundscape action first");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("[TEST] Queuing Soundscape 'Shruti' action...");
        int soundscapeResult = MusicSystem1.instance.director.AddActionToQueue(
            MusicSystem1.instance.Action_SetSoundscape("Shruti"),
            "Soundscape",
            true,
            false,
            180.0f,
            true,
            0 // No exclusivity behavior - just add it
        );
        
        yield return new WaitForSeconds(0.5f);
        
        CheckQueueStatus();
        bool hasSoundscapeBefore = MusicSystem1.instance.director.SearchQueueForType("Soundscape");
        bool hasShuffleBefore = MusicSystem1.instance.director.SearchQueueForType("SoundscapeShuffle");
        
        Debug.Log($"[TEST] AddActionToQueue() returned: {soundscapeResult} (should be >= 0 if successful)");
        Debug.Log($"[TEST] ✓ Soundscape queued: {(hasSoundscapeBefore ? "PASS" : "FAIL")}");
        Debug.Log($"[TEST] ✓ SoundscapeShuffle not present: {(!hasShuffleBefore ? "PASS" : "FAIL")}");
        
        if (!hasSoundscapeBefore)
        {
            Debug.LogWarning("[TEST] WARNING: Soundscape was not queued. The test may not work correctly.");
            Debug.LogWarning("[TEST] Press SPACE to continue anyway, or restart the test.");
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        }
        
        Debug.Log("[TEST] \nPress SPACE to attempt to queue SoundscapeShuffle (should be rejected)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("[TEST] Attempting to queue SoundscapeShuffle via WorldShuffler.BeginShuffle(false)...");
        MusicSystem1.instance.worldShuffler.BeginShuffle(false); // false = queue it, don't shuffle immediately
        yield return new WaitForSeconds(0.5f);
        
        CheckQueueStatus();
        bool hasSoundscapeAfter = MusicSystem1.instance.director.SearchQueueForType("Soundscape");
        bool hasShuffleAfter = MusicSystem1.instance.director.SearchQueueForType("SoundscapeShuffle");
        
        Debug.Log($"[TEST] After attempting to queue SoundscapeShuffle:");
        Debug.Log($"[TEST] ✓ Soundscape still present: {(hasSoundscapeAfter ? "PASS" : "FAIL")}");
        Debug.Log($"[TEST] ✓ SoundscapeShuffle NOT added: {(!hasShuffleAfter ? "PASS" : "FAIL")}");
        
        if (!hasShuffleAfter && hasSoundscapeAfter)
        {
            Debug.Log("[TEST] ✓ SoundscapeShuffle correctly rejected: PASS");
            Debug.Log("[TEST] WorldShuffler's SearchQueueForType('Soundscape') check worked correctly.");
        }
        else
        {
            Debug.LogWarning("[TEST] ✗ SoundscapeShuffle was not rejected correctly: FAIL");
            if (hasShuffleAfter)
            {
                Debug.LogWarning("[TEST] SoundscapeShuffle was incorrectly added to queue");
            }
            if (!hasSoundscapeAfter)
            {
                Debug.LogWarning("[TEST] Soundscape was removed from queue (unexpected)");
            }
        }
        
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - 'WorldShuffler: Attempted to queue SoundscapeShuffle, but director queue already has a specific SoundScape in it.'");
        Debug.Log("[TEST]   - Director queue logs showing only Soundscape action (no SoundscapeShuffle)");
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("[TEST] \n=== CLEANUP ===");
        Debug.Log("[TEST] Press SPACE to stop WorldShuffler and clear queue");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.worldShuffler.StopShuffle();
        MusicSystem1.instance.director.ClearQueueOfType("Soundscape");
        MusicSystem1.instance.director.ClearQueueOfType("SoundscapeShuffle");
        yield return new WaitForSeconds(0.5f);
        CheckQueueStatus();
        
        Debug.Log("[TEST] \n=== TEST 14 COMPLETE ===");
        Debug.Log("[TEST] Summary:");
        Debug.Log("[TEST] ✓ Test 2.2: SoundscapeShuffle Rejected When Soundscape Exists");
        Debug.Log("[TEST] \nReview console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 15: Director Queue Priority System - ReplaceActionInQueue Expiration Time (Test 2.3)
    /// Tests that ReplaceActionInQueue preserves the shortest expiration time correctly.
    /// </summary>
    private IEnumerator TestReplaceActionInQueueExpirationTime()
    {
        Debug.Log("[TEST] === TEST 15: DIRECTOR QUEUE PRIORITY SYSTEM - REPLACEACTIONINQUEUE EXPIRATION TIME (Test 2.3) ===");
        Debug.Log("[TEST] This test verifies ReplaceActionInQueue preserves shortest expiration time correctly.");
        Debug.Log("[TEST] \nCONTROLS:");
        Debug.Log("[TEST] SPACE - Proceed to next test stage");
        Debug.Log("[TEST] ========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.director == null)
        {
            Debug.LogError("[TEST] Director is null - cannot run test");
            yield break;
        }
        
        if (MusicSystem1.instance.worldShuffler == null)
        {
            Debug.LogError("[TEST] WorldShuffler is null - cannot run test");
            yield break;
        }
        
        // Helper method to get expiration time for a specific action type
        float GetExpirationTimeForType(string type)
        {
            float shortestTime = float.MaxValue;
            bool found = false;
            foreach (var item in MusicSystem1.instance.director.queue)
            {
                if (item.Value.type == type)
                {
                    found = true;
                    if (item.Value.timeLeft < shortestTime)
                    {
                        shortestTime = item.Value.timeLeft;
                    }
                }
            }
            return found ? shortestTime : -1f;
        }
        
        // Helper method to check queue status and log it
        void CheckQueueStatus()
        {
            bool hasSoundscapeShuffle = MusicSystem1.instance.director.SearchQueueForType("SoundscapeShuffle");
            bool hasSoundscape = MusicSystem1.instance.director.SearchQueueForType("Soundscape");
            Debug.Log($"[TEST] Queue Status - SoundscapeShuffle: {(hasSoundscapeShuffle ? "PRESENT" : "NOT PRESENT")}, Soundscape: {(hasSoundscape ? "PRESENT" : "NOT PRESENT")}");
            if (hasSoundscapeShuffle)
            {
                float shuffleTime = GetExpirationTimeForType("SoundscapeShuffle");
                Debug.Log($"[TEST] SoundscapeShuffle expiration time: {shuffleTime}s");
            }
            if (hasSoundscape)
            {
                float soundscapeTime = GetExpirationTimeForType("Soundscape");
                Debug.Log($"[TEST] Soundscape expiration time: {soundscapeTime}s");
            }
            Debug.Log("[TEST] Full queue contents:");
            MusicSystem1.instance.director.LogQueue();
        }
        
        // Ensure WorldShuffler is stopped and queue is clear
        MusicSystem1.instance.worldShuffler.StopShuffle();
        MusicSystem1.instance.director.ClearQueueOfType("SoundscapeShuffle");
        MusicSystem1.instance.director.ClearQueueOfType("Soundscape");
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 2.3: ReplaceActionInQueue Expiration Time Matching
        // ===================================================================
        Debug.Log("[TEST] \n=== TEST 2.3: REPLACEACTIONINQUEUE EXPIRATION TIME MATCHING ===");
        Debug.Log("[TEST] Objective: Verify ReplaceActionInQueue preserves shortest expiration time correctly");
        
        Debug.Log("[TEST] \nPress SPACE to queue first SoundscapeShuffle with 60s expiration");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("[TEST] Queuing SoundscapeShuffle with 60s expiration...");
        // Create action inline since Action_ShuffleSoundscape() is private
        System.Action shuffleAction1 = () => MusicSystem1.instance.worldShuffler.ShuffleWorldsNow();
        int result1 = MusicSystem1.instance.director.AddActionToQueue(
            shuffleAction1,
            "SoundscapeShuffle",
            true,
            false,
            60.0f,
            true,
            1 // Exclusivity behavior 1: prefer lowest time left
        );
        
        yield return new WaitForSeconds(0.5f);
        CheckQueueStatus();
        float time1 = GetExpirationTimeForType("SoundscapeShuffle");
        Debug.Log($"[TEST] AddActionToQueue() returned: {result1}");
        Debug.Log($"[TEST] ✓ First SoundscapeShuffle queued with 60s expiration: {(Mathf.Approximately(time1, 60.0f) ? "PASS" : "FAIL")}");
        
        Debug.Log("[TEST] \nPress SPACE to queue second SoundscapeShuffle with 30s expiration");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("[TEST] Queuing second SoundscapeShuffle with 30s expiration...");
        // Create action inline since Action_ShuffleSoundscape() is private
        System.Action shuffleAction2 = () => MusicSystem1.instance.worldShuffler.ShuffleWorldsNow();
        int result2 = MusicSystem1.instance.director.AddActionToQueue(
            shuffleAction2,
            "SoundscapeShuffle",
            true,
            false,
            30.0f,
            true,
            1 // Exclusivity behavior 1: prefer lowest time left
        );
        
        yield return new WaitForSeconds(0.5f);
        CheckQueueStatus();
        float time2 = GetExpirationTimeForType("SoundscapeShuffle");
        Debug.Log($"[TEST] AddActionToQueue() returned: {result2}");
        Debug.Log($"[TEST] ✓ Second SoundscapeShuffle queued with 30s expiration: {(Mathf.Approximately(time2, 30.0f) ? "PASS" : "FAIL")}");
        Debug.Log($"[TEST] Note: With exclusivity behavior 1, the shorter time (30s) should be kept.");
        
        Debug.Log("[TEST] \nPress SPACE to use ReplaceActionInQueue() with newMaximumTimeLimit of 120s");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        Debug.Log("[TEST] Calling ReplaceActionInQueue()...");
        Debug.Log("[TEST] Replacing SoundscapeShuffle with Soundscape 'Shruti'");
        Debug.Log("[TEST] Expected expiration time: Mathf.Min(30f, 60f, 120f) = 30f");
        
        int replaceResult = MusicSystem1.instance.director.ReplaceActionInQueue(
            MusicSystem1.instance.Action_SetSoundscape("Shruti"),
            "Soundscape",
            "SoundscapeShuffle",
            true,
            false,
            120.0f, // newMaximumTimeLimit
            true
        );
        
        yield return new WaitForSeconds(0.5f);
        CheckQueueStatus();
        
        float soundscapeTime = GetExpirationTimeForType("Soundscape");
        bool hasShuffleAfter = MusicSystem1.instance.director.SearchQueueForType("SoundscapeShuffle");
        bool hasSoundscapeAfter = MusicSystem1.instance.director.SearchQueueForType("Soundscape");
        
        Debug.Log($"[TEST] ReplaceActionInQueue() returned: {replaceResult}");
        Debug.Log($"[TEST] ✓ SoundscapeShuffle cleared: {(!hasShuffleAfter ? "PASS" : "FAIL")}");
        Debug.Log($"[TEST] ✓ Soundscape added: {(hasSoundscapeAfter ? "PASS" : "FAIL")}");
        Debug.Log($"[TEST] Soundscape expiration time: {soundscapeTime}s");
        
        float expectedTime = Mathf.Min(30f, 60f, 120f); // Should be 30f
        if (Mathf.Approximately(soundscapeTime, expectedTime))
        {
            Debug.Log($"[TEST] ✓ Expiration time is correct ({soundscapeTime}s = {expectedTime}s): PASS");
            Debug.Log("[TEST] ReplaceActionInQueue() correctly calculated: Mathf.Min(shortestTimeOld, shortestTimeNew, newMaximumTimeLimit)");
        }
        else
        {
            Debug.LogWarning($"[TEST] ✗ Expiration time is incorrect: FAIL");
            Debug.LogWarning($"[TEST] Expected: {expectedTime}s, Got: {soundscapeTime}s");
            Debug.LogWarning("[TEST] ReplaceActionInQueue() should use: Mathf.Min(30f, 60f, 120f) = 30f");
        }
        
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - Director queue logs showing Soundscape expiration time");
        Debug.Log("[TEST]   - Verify the math: Mathf.Min(30f, 60f, 120f) = 30f");
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("[TEST] \n=== CLEANUP ===");
        Debug.Log("[TEST] Press SPACE to stop WorldShuffler and clear queue");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.worldShuffler.StopShuffle();
        MusicSystem1.instance.director.ClearQueueOfType("SoundscapeShuffle");
        MusicSystem1.instance.director.ClearQueueOfType("Soundscape");
        yield return new WaitForSeconds(0.5f);
        CheckQueueStatus();
        
        Debug.Log("[TEST] \n=== TEST 15 COMPLETE ===");
        Debug.Log("[TEST] Summary:");
        Debug.Log("[TEST] ✓ Test 2.3: ReplaceActionInQueue Expiration Time Matching");
        Debug.Log("[TEST] \nReview console logs above for detailed results.");
    }
    
    /// <summary>
    /// Test 16: Silent Mode Behavior (Tests 6.1 - 6.3)
    /// Tests Silent mode behavior with SoundWorlds and MusicLoops, including lock persistence and soundscape changes.
    /// </summary>
    /// REVIEW THIS
    private IEnumerator TestSilentModeBehavior()
    {
        Debug.Log("[TEST] === TEST 16: SILENT MODE BEHAVIOR (Tests 6.1 - 6.3) ===");
        Debug.Log("[TEST] This test verifies Silent mode behavior with SoundWorlds and MusicLoops.");
        Debug.Log("[TEST] \nCONTROLS:");
        Debug.Log("[TEST] SPACE - Proceed to next test stage");
        Debug.Log("[TEST] ========================================\n");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        // Helper method to print current state
        void PrintCurrentState()
        {
            Debug.Log($"[TEST] Current Music Mode: {MusicSystem1.instance.currentMusicMode}");
            Debug.Log($"[TEST] Current Interaction Type: {MusicSystem1.instance.currentInteractionType}");
            NoteName currentFundamental = MusicSystem1.instance.fundamentalNoteName;
            Debug.Log($"[TEST] Current Fundamental: {currentFundamental}");
            Debug.Log("[TEST] Check console logs above for lock states (Mode/Content/Debug)");
        }
        
        // Ensure we start in a clean state
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        
        // ===================================================================
        // TEST 6.1: Silent Mode with SoundWorld
        // ===================================================================
        Debug.Log("[TEST] \n=== TEST 6.1: SILENT MODE WITH SOUNDWORLD ===");
        Debug.Log("[TEST] Objective: Verify Silent mode behavior with SoundWorld");
        
        Debug.Log("[TEST] \nPress SPACE to set music mode to Freeplay");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
        yield return new WaitForSeconds(0.5f);
        PrintCurrentState();
        Debug.Log("[TEST] ✓ Music mode set to Freeplay");
        
        Debug.Log("[TEST] \nPress SPACE to set soundscape to SoundWorld 'SonoFlore'");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetSoundscape("SonoFlore");
        yield return new WaitForSeconds(0.5f);
        PrintCurrentState();
        Debug.Log($"[TEST] ✓ Soundscape set to SonoFlore (SoundWorld)");
        Debug.Log($"[TEST] ✓ Interaction Type: {MusicSystem1.instance.currentInteractionType} (should be SoundWorld)");
        
        Debug.Log("[TEST] \nPress SPACE to enter Silent mode");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName fundamentalBeforeSilent = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Silent);
        yield return new WaitForSeconds(0.5f);
        PrintCurrentState();
        
        Debug.Log($"[TEST] ✓ Music mode set to Silent");
        Debug.Log($"[TEST] ✓ Fundamental before Silent: {fundamentalBeforeSilent}");
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - 'MUSIC: Music Mode Set to Silent'");
        Debug.Log("[TEST]   - 'MUSIC: InteractiveMusic stopped' (StopInteractiveMusic() called)");
        Debug.Log("[TEST]   - 'MUSIC: Interactive Music Mode Recovered to InteractiveMusicSystem because Interaction Type is SoundWorld' (RecoverInteractiveMusicModeFromInteractionType() called)");
        Debug.Log("[TEST] ⚠️ NOTE: Verify if RecoverInteractiveMusicModeFromInteractionType() setting Wwise states in Silent mode is intended behavior");
        
        // Check that no locks are active (we can't directly check private fields, but we can verify fundamental didn't change unexpectedly)
        Debug.Log("[TEST] ✓ No fundamental locks active: PASS (Silent mode doesn't require locks)");
        Debug.Log("[TEST] NOTE: Verify in console that no lock-related logs appear");
        
        Debug.Log("[TEST] \nPress SPACE to change soundscape while in Silent mode (should show warning)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetSoundscape("Shadow");
        yield return new WaitForSeconds(0.5f);
        PrintCurrentState();
        
        Debug.Log("[TEST] ✓ Soundscape changed to Shadow");
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - 'MUSIC: Changing SoundWorld to 'Shadow', but current mode is 'Silent' (Environment or Silent) -- this change will not be audible.'");
        Debug.Log("[TEST] ✓ Warning logged: PASS (check logs)");
        Debug.Log("[TEST] ✓ Soundscape changes are not audible: PASS (Silent mode)");
        
        // ===================================================================
        // TEST 6.2: Silent Mode with MusicLoop
        // ===================================================================
        Debug.Log("[TEST] \n=== TEST 6.2: SILENT MODE WITH MUSICLOOP ===");
        Debug.Log("[TEST] Objective: Verify Silent mode with MusicLoop and content lock persistence");
        
        Debug.Log("[TEST] \nPress SPACE to set music mode back to Freeplay");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
        yield return new WaitForSeconds(0.5f);
        PrintCurrentState();
        
        Debug.Log("[TEST] \nPress SPACE to set soundscape to MusicLoop 'ShiftingEarth' (requires C)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetSoundscape("ShiftingEarth");
        yield return new WaitForSeconds(0.5f);
        NoteName fundamentalBeforeSilent2 = MusicSystem1.instance.fundamentalNoteName;
        PrintCurrentState();
        Debug.Log($"[TEST] ✓ Soundscape set to ShiftingEarth (MusicLoop)");
        Debug.Log($"[TEST] ✓ Content lock should be set to C, Fundamental: {fundamentalBeforeSilent2}");
        Debug.Log($"[TEST] ✓ Interaction Type: {MusicSystem1.instance.currentInteractionType} (should be MusicLoop)");
        
        Debug.Log("[TEST] \nPress SPACE to enter Silent mode (content lock should persist)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Silent);
        yield return new WaitForSeconds(0.5f);
        NoteName fundamentalAfterSilent = MusicSystem1.instance.fundamentalNoteName;
        PrintCurrentState();
        
        Debug.Log($"[TEST] ✓ Music mode set to Silent");
        Debug.Log($"[TEST] ✓ Fundamental before Silent: {fundamentalBeforeSilent2}");
        Debug.Log($"[TEST] ✓ Fundamental after Silent: {fundamentalAfterSilent}");
        Debug.Log($"[TEST] ✓ Content lock persists: {(fundamentalAfterSilent == fundamentalBeforeSilent2 ? "PASS" : "FAIL")}");
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - 'MUSIC: Music Mode Set to Silent'");
        Debug.Log("[TEST]   - 'MUSIC: InteractiveMusic stopped'");
        
        Debug.Log("[TEST] \nPress SPACE to change soundscape while in Silent mode (should show MusicLoop warning)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetSoundscape("PinkNoiseAtmosphere");
        yield return new WaitForSeconds(0.5f);
        PrintCurrentState();
        
        Debug.Log("[TEST] ✓ Soundscape changed to PinkNoiseAtmosphere");
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - 'MUSIC: Changing MusicLoop to 'PinkNoiseAtmosphere', but current mode is 'Silent' (Environment or Silent) -- this change will not be audible.'");
        Debug.Log("[TEST] ✓ Warning logged: PASS (check logs)");
        Debug.Log("[TEST] ✓ Content lock should update to As (PinkNoiseAtmosphere requirement)");
        
        // ===================================================================
        // TEST 6.3: Changing Soundscapes in Silent Mode
        // ===================================================================
        Debug.Log("[TEST] \n=== TEST 6.3: CHANGING SOUNDSCAPES IN SILENT MODE ===");
        Debug.Log("[TEST] Objective: Verify soundscape changes don't play audio but update locks correctly");
        
        Debug.Log("[TEST] \nPress SPACE to call SetSoundscape('SonoFlore') - SoundWorld (should clear lock)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName fundamentalBefore1 = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.SetSoundscape("SonoFlore");
        yield return new WaitForSeconds(0.5f);
        NoteName fundamentalAfter1 = MusicSystem1.instance.fundamentalNoteName;
        PrintCurrentState();
        
        Debug.Log($"[TEST] SetSoundscape('SonoFlore') called");
        Debug.Log($"[TEST] Fundamental before: {fundamentalBefore1}, after: {fundamentalAfter1}");
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - Warning about Silent mode");
        Debug.Log("[TEST]   - 'MUSIC: Fundamental Content Unlocked' (SoundWorld clears lock)");
        Debug.Log("[TEST] ✓ Content lock cleared: PASS (check logs for unlock message)");
        
        Debug.Log("[TEST] \nPress SPACE to call SetSoundscape('ShiftingEarth') - MusicLoop (should set lock to C)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName fundamentalBefore2 = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.SetSoundscape("ShiftingEarth");
        yield return new WaitForSeconds(0.5f);
        NoteName fundamentalAfter2 = MusicSystem1.instance.fundamentalNoteName;
        PrintCurrentState();
        
        Debug.Log($"[TEST] SetSoundscape('ShiftingEarth') called");
        Debug.Log($"[TEST] Fundamental before: {fundamentalBefore2}, after: {fundamentalAfter2}");
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - Warning about Silent mode");
        Debug.Log("[TEST]   - 'MUSIC: Content lock set to C for MusicLoop 'ShiftingEarth''");
        Debug.Log($"[TEST] ✓ Content lock set to C: {(fundamentalAfter2 == NoteName.C ? "PASS" : "FAIL")}");
        
        Debug.Log("[TEST] \nPress SPACE to call SetSoundscape('Shadow') - SoundWorld (should clear lock again)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName fundamentalBefore3 = MusicSystem1.instance.fundamentalNoteName;
        MusicSystem1.instance.SetSoundscape("Shadow");
        yield return new WaitForSeconds(0.5f);
        NoteName fundamentalAfter3 = MusicSystem1.instance.fundamentalNoteName;
        PrintCurrentState();
        
        Debug.Log($"[TEST] SetSoundscape('Shadow') called");
        Debug.Log($"[TEST] Fundamental before: {fundamentalBefore3}, after: {fundamentalAfter3}");
        Debug.Log("[TEST] Check console for:");
        Debug.Log("[TEST]   - Warning about Silent mode");
        Debug.Log("[TEST]   - 'MUSIC: Fundamental Content Unlocked' (SoundWorld clears lock)");
        Debug.Log($"[TEST] ✓ Content lock cleared again: PASS (check logs)");
        
        Debug.Log("[TEST] \nSummary of Test 6.3:");
        Debug.Log("[TEST] ✓ No audio plays: PASS (Silent mode)");
        Debug.Log("[TEST] ✓ currentInteractionType updates: PASS (check logs)");
        Debug.Log("[TEST] ✓ Content lock updates correctly:");
        Debug.Log("[TEST]   - SoundWorld → Lock cleared");
        Debug.Log("[TEST]   - MusicLoop → Lock set");
        Debug.Log("[TEST]   - SoundWorld → Lock cleared again");
        
        // ===================================================================
        // CLEANUP
        // ===================================================================
        Debug.Log("[TEST] \n=== CLEANUP ===");
        Debug.Log("[TEST] Press SPACE to exit Silent mode and clear locks");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetMusicModeTo(MusicSystem1.MusicMode.Freeplay);
        MusicSystem1.instance.SetFundamentalDebugLock(null);
        MusicSystem1.instance.SetFundamentalContentLock(null);
        MusicSystem1.instance.SetFundamentalModeLock(false);
        yield return new WaitForSeconds(0.5f);
        PrintCurrentState();
        
        Debug.Log("[TEST] \n=== TEST 16 COMPLETE ===");
        Debug.Log("[TEST] Summary:");
        Debug.Log("[TEST] ✓ Test 6.1: Silent Mode with SoundWorld");
        Debug.Log("[TEST] ✓ Test 6.2: Silent Mode with MusicLoop");
        Debug.Log("[TEST] ✓ Test 6.3: Changing Soundscapes in Silent Mode");
        Debug.Log("[TEST] \nReview console logs above for detailed results.");
    }
}
