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

    [Header("Debug: Adjunctive Sequence Advance (F key) / editor cheats")]
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
#if UNITY_EDITOR
        if (GetComponent<MusicDebugHarness>() == null)
            gameObject.AddComponent<MusicDebugHarness>();
        if (GetComponent<MusicDebugGuidedPlaytest>() == null)
            gameObject.AddComponent<MusicDebugGuidedPlaytest>();
#endif
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
    //UPDATE METHOD
    //================================

    // Update is called once per frame
    void Update()
    {

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

#if UNITY_EDITOR
        // Shift+E (editor only): end current sequence stage. (Avoid Shift+Q — Unity Editor often steals Q for the Hand tool when Scene view is focused.)
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.E))
        {
            if (sequencer == null)
                sequencer = FindObjectOfType<Sequencer>();
            if (sequencer == null)
                Debug.LogWarning("[Input] Shift+E (editor): No Sequencer in scene.");
            else
            {
                bool handled = sequencer.HandleSequenceCommand(SequenceCommand.EndThisSequenceStage);
                Debug.Log(handled
                    ? "[Input] Shift+E (editor): EndThisSequenceStage handled — current stage should complete / advance."
                    : "[Input] Shift+E (editor): EndThisSequenceStage not handled (no watcher for this stage, or no active stage).");
            }
        }
#endif

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

        // K key: Initialize lights to Dark immediately
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (LightControl.instance != null)
            {
                LightControl.instance.LightSettingsInitialization(0.0f);
                Debug.Log("[Input] K key: LightControl.instance.LightSettingsInitialization(0.0f)");
            }
            else
            {
                Debug.LogWarning("[Input] K key: LightControl.instance is null!");
            }
        }

        // L key: Trigger LightControl StartLights
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (LightControl.instance != null)
            {
                LightControl.instance.StartLights();
                Debug.Log("[Input] L key: LightControl.instance.StartLights()");
            }
            else
            {
                Debug.LogWarning("[Input] L key: LightControl.instance is null!");
            }
        }

        // List All Recorded Files
        /*
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (RecordedAudioPlayback.Instance != null)
            {
                RecordedAudioPlayback.Instance.ListAllRecordedFiles();
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
        // TESTING KEYBOARD COMMANDS FOR InputReferences.cs
        // ===================================================================
        // Add these commands to the Update() method in InputReferences.cs
        // Place them after the existing keyboard input checks
        // ===================================================================


        // Recording Controls
        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     if (RecordedAudioPlayback.Instance != null)
        //     {
        //         bool newMode = !RecordedAudioPlayback.Instance.recordMode;
        //         RecordedAudioPlayback.Instance.SetRecordMode(newMode);
        //         Debug.Log($"[TEST] Record mode toggled: {newMode}");
        //     }
        // }

        // if (Input.GetKeyDown(KeyCode.P))
        // {
        //     if (RecordedAudioPlayback.Instance != null)
        //     {
        //         bool newMode = !RecordedAudioPlayback.Instance.playMode;
        //         RecordedAudioPlayback.Instance.SetPlaybackMode(newMode);
        //         Debug.Log($"[TEST] Playback mode toggled: {newMode}");
        //     }
        // }

        // // Delete All Recordings
        // if (Input.GetKeyDown(KeyCode.S))
        // {
        //     if (RecordedAudioPlayback.Instance != null)
        //     {
        //         RecordedAudioPlayback.Instance.DeleteAllRecordings();
        //         Debug.Log("[TEST] DeleteAllRecordings called");
        //     }
        // }

        // // Print Slot Status (for debugging)
        // if (Input.GetKeyDown(KeyCode.A))
        // {
        //     if (RecordedAudioPlayback.Instance != null)
        //     {
        //         RecordedAudioPlayback.Instance.PrintSlotStatus();
        //     }
        // }



        // // Manual Test Recording (5 seconds)
        // if (Input.GetKeyDown(KeyCode.M))
        // {
        //     if (RecordedAudioPlayback.Instance != null)
        //     {
        //         RecordedAudioPlayback.Instance.ManualTestRecording();
        //     }
        // }

        // // Get Recording Counts
        // if (Input.GetKeyDown(KeyCode.C))
        // {
        //     if (RecordedAudioPlayback.Instance != null)
        //     {
        //         RecordedAudioPlayback.Instance.GetRecordingCounts();
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
