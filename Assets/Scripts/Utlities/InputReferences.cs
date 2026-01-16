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
    void Start()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
         // List All Recorded Files
         if (Input.GetKeyDown(KeyCode.L))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
           {
                RecordedAudioPlaybackTest.Instance.ListAllRecordedFiles();
            }
         }
    

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
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[TEST] Keyboard command C pressed");
            if (MusicSystem1.instance != null)
            {
                Debug.Log("[TEST] MusicSystem1.instance is not null");
                // Change fundamental to C (ordinal 0)
                MusicSystem1.instance.SetFundamentalDirect(0);
                Debug.Log("[TEST] Music fundamental changed to C via keyboard (C)");
            }
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (MusicSystem1.instance != null)
            {
                // Change fundamental to G (ordinal 7)
                MusicSystem1.instance.SetFundamentalDirect(7);
                Debug.Log("[TEST] Music fundamental changed to G via keyboard (G)");
            }
        }

        // ===================================================================
        // KEYBOARD COMMANDS FOR FUNDAMENTAL LOCKS (MusicSystem1)
        // I/O: Debug Lock   |   K/L: Content Lock   |   N/M: Mode Lock
        // Press key to SET the lock, adjacent key to CLEAR the lock
        // ===================================================================
        // DEBUG LOCK (I = Set Debug Lock to C, O = Clear Debug Lock)
        
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
    }
}
