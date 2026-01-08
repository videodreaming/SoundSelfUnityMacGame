using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputReferences : MonoBehaviour
{
    
    public static InputReferences instance;
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
        if(Input.GetKeyDown(KeyCode.B))
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
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (RecordedAudioPlaybackTest.Instance != null)
            {
                RecordedAudioPlaybackTest.Instance.DeleteAllRecordings();
                Debug.Log("[TEST] DeleteAllRecordings called");
            }
        }

        // Print Slot Status (for debugging)
        if (Input.GetKeyDown(KeyCode.S))
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
        // B - Toggle Playback Mode (B for "Back" or "Broadcast")
        // X - Delete All Recordings
        // S - Print Slot Status
        // L - List All Recorded Files
        // M - Manual Test Recording (5 seconds)
        // C - Get Recording Counts
        // ===================================================================
    }
}
