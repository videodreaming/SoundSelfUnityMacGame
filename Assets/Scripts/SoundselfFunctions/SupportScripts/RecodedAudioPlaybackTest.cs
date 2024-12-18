using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecodedAudioPlaybackTest : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; // Assign your imitoneVoiceInterpreter in the Inspector
    public DevelopmentMode developmentMode; // Assign your DevelopmentMode in the Inspector
    public MusicSystem1 musicSystem1; // Assign your MusicSystem1 in the Inspector
    public AudioSource ImitoneaudioSource; // Assign your AudioSource in the Inspector 

    public List<AudioClip> audioClips = new List<AudioClip>(12); // Preallocate space for 12 notes    private List<AudioClip> audioClips = new List<AudioClip>(); // List to store audio clips
    public int maxClips = 12; 
    private Dictionary<string, int> noteToIndex;
    private string[] noteNames = { "C", "Cs", "D", "Ds", "E", "F", "Fs", "G", "Gs", "A", "As", "B" };

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            StartRecording();
        }
        if(Input.GetKeyDown(KeyCode.W))
        {
            StopRecording();
        }
    }
     private void Awake()
    {
        // Initialize the dictionary to map notes to indices
        noteToIndex = new Dictionary<string, int>
        {
            { "C", 0 }, { "Cs", 1 }, { "D", 2 }, { "Ds", 3 },
            { "E", 4 }, { "F", 5 }, { "Fs", 6 }, { "G", 7 },
            { "Gs", 8 }, { "A", 9 }, { "As", 10 }, { "B", 11 }
        };
        
        for (int i = 0; i < maxClips; i++)
        {
            audioClips.Add(null);
        }
    }
    //INFO NEEDED: WHEN DO WE REPLAY THESE AUDIO CLIPS?
    public void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            string deviceName = Microphone.devices[0]; // Use the first microphone device
            ImitoneaudioSource.clip = Microphone.Start(deviceName, false, 10, 44100); // Record for up to 10 seconds
            Debug.Log("Recording started...");
        }
        else
        {
            Debug.LogWarning("No microphone detected!");
        }
    }

    public void StopRecording()
    {
        Debug.Log(musicSystem1.fundamentalNote);
        if (Microphone.IsRecording(null))
        {
            // Stop the microphone recording
            Microphone.End(null);
            Debug.Log("Recording stopped.");

            // Save a reference to the current audioSource clip
            AudioClip recordedClip = ImitoneaudioSource.clip;

            if (recordedClip != null)
            {
                if (noteToIndex.TryGetValue(noteNames[musicSystem1.fundamentalNote], out int index))
                {
                    // Place the clip at the appropriate index
                    audioClips[index] = recordedClip;
                    Debug.Log($"Clip saved for note {musicSystem1.fundamentalNote} at index {index}.");
                }
                else
                {
                    Debug.LogWarning($"Invalid note '{musicSystem1.fundamentalNote}' specified.");
                }
            }
            else
            {
                Debug.LogWarning("No clip found in AudioSource.");
            }
        }
        else
        {
            Debug.LogWarning("No recording to stop.");
        }
    }
}

