using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        //keyboard inputs
        
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            DebugMenuController.instance.SwitchDebugMenu();
        }

        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            canvasSwitcher.instance.SwitchCanvas(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            canvasSwitcher.instance.SwitchCanvas(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            canvasSwitcher.instance.SwitchCanvas(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            canvasSwitcher.instance.SwitchCanvas(4);
        }


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

        // ===================================================================
        // TESTING KEYBOARD COMMANDS FOR InputReferences.cs
        // ===================================================================
        // Add these commands to the Update() method in InputReferences.cs
        // Place them after the existing keyboard input checks
        // ===================================================================


        // Recording Controls
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                bool newMode = !RecordedAudioPlaybackTest.Instance.recordMode;
                RecordedAudioPlaybackTest.Instance.SetRecordMode(newMode);
                Debug.Log($"[TEST] Record mode toggled: {newMode}");
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                bool newMode = !RecordedAudioPlaybackTest.Instance.playMode;
                RecordedAudioPlaybackTest.Instance.SetPlaybackMode(newMode);
                Debug.Log($"[TEST] Playback mode toggled: {newMode}");
            }
        }

        // Delete All Recordings
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                RecordedAudioPlaybackTest.Instance.DeleteAllRecordings();
                Debug.Log("[TEST] DeleteAllRecordings called");
            }
        }

        // Print Slot Status (for debugging)
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                RecordedAudioPlaybackTest.Instance.PrintSlotStatus();
            }
        }

        // List All Recorded Files
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                RecordedAudioPlaybackTest.Instance.ListAllRecordedFiles();
            }
        }

        // Manual Test Recording (5 seconds)
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                RecordedAudioPlaybackTest.Instance.ManualTestRecording();
            }
        }

        // Get Recording Counts
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                RecordedAudioPlaybackTest.Instance.GetRecordingCounts();
            }
        }
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
        
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (MusicSystem1.instance != null && MusicSystem1.instance.gameObject != null)
            {
                uint eventId = AkSoundEngine.GetIDFromString("Stop_SilentLoops");
                if (eventId != 0)
                {
                    AkSoundEngine.PostEvent("Stop_SilentLoops", MusicSystem1.instance.gameObject);
                    Debug.Log("[Input] Posted Wwise event: Stop_SilentLoops");
                }
                else
                {
                    Debug.LogError("[Input] Wwise event 'Stop_SilentLoops' not found. Check Wwise project - event may not exist or Wwise banks not loaded.");
                }
            }
            else
            {
                Debug.LogWarning("[Input] MusicSystem1.instance or gameObject is null, cannot post Wwise event");
            }
        }
    }

}
