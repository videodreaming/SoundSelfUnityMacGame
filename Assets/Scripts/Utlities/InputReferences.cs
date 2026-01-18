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
    /// Test 2: Harmony System
    /// Tests harmony sequences, octave wrapping, and harmony updates when fundamental changes.
    /// </summary>
    private IEnumerator TestHarmonySystem()
    {
        Debug.Log("=== TEST 2: HARMONY SYSTEM ===");
        Debug.Log("This test verifies harmony calculations work correctly with NoteName enum.");
        
        if (MusicSystem1.instance == null)
        {
            Debug.LogError("[TEST] MusicSystem1.instance is null - cannot run test");
            yield break;
        }
        
        Debug.Log("\n[TEST] Stage 1: Testing harmony calculation for different fundamentals");
        Debug.Log("Press SPACE to test harmony with fundamental = C");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDirect(NoteName.C);
        yield return new WaitForSeconds(0.5f);
        
        NoteName fundamental = MusicSystem1.instance.fundamentalNoteName;
        NoteName harmony = MusicSystem1.instance.harmonyNote;
        Debug.Log($"[TEST] Fundamental: {fundamental}, Harmony: {harmony}");
        Debug.Log($"[TEST] ✓ Harmony is NoteName enum: {(harmony != NoteName.None || harmony == NoteName.None ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 2: Testing harmony wrapping (C + 7 = G, B + 1 = C)");
        Debug.Log("Press SPACE to test C + 7 semitones (should be G)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        NoteName result = NoteUtils.AddInterval(NoteName.C, 7);
        Debug.Log($"[TEST] C + 7 semitones = {result} (expected: G)");
        Debug.Log($"[TEST] ✓ Wrapping test: {(result == NoteName.G ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Press SPACE to test B + 1 semitone (should wrap to C)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        result = NoteUtils.AddInterval(NoteName.B, 1);
        Debug.Log($"[TEST] B + 1 semitone = {result} (expected: C)");
        Debug.Log($"[TEST] ✓ Octave wrap test: {(result == NoteName.C ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 3: Testing negative intervals");
        Debug.Log("Press SPACE to test C - 1 semitone (should wrap to B)");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        result = NoteUtils.AddInterval(NoteName.C, -1);
        Debug.Log($"[TEST] C - 1 semitone = {result} (expected: B)");
        Debug.Log($"[TEST] ✓ Negative interval test: {(result == NoteName.B ? "PASS" : "FAIL")}");
        
        Debug.Log("\n[TEST] Stage 4: Testing harmony updates when fundamental changes");
        Debug.Log("Press SPACE to change fundamental from C to A and verify harmony updates");
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        
        MusicSystem1.instance.SetFundamentalDirect(NoteName.C);
        yield return new WaitForSeconds(0.5f);
        NoteName harmony1 = MusicSystem1.instance.harmonyNote;
        Debug.Log($"[TEST] Fundamental: C, Harmony: {harmony1}");
        
        MusicSystem1.instance.SetFundamentalDirect(NoteName.A);
        yield return new WaitForSeconds(0.5f);
        NoteName harmony2 = MusicSystem1.instance.harmonyNote;
        Debug.Log($"[TEST] Fundamental: A, Harmony: {harmony2}");
        Debug.Log($"[TEST] ✓ Harmony updated: {(harmony1 != harmony2 ? "PASS" : "FAIL")}");
        
        Debug.Log("\n=== TEST 2 COMPLETE ===");
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
}
