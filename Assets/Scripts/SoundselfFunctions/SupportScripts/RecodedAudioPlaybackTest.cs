using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecodedAudioPlaybackTest : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; // Assign your imitoneVoiceInterpreter in the Inspector
    public DevelopmentMode developmentMode; // Assign your DevelopmentMode in the Inspector
    public MusicSystem1 musicSystem1; // Assign your MusicSystem1 in the Inspector
    public AudioSource ThisObjectAudioSource; // Assign your AudioSource in the Inspector 

    public List<AudioClip> audioClips = new List<AudioClip>(12); // Preallocate space for 12 notes    private List<AudioClip> audioClips = new List<AudioClip>(); // List to store audio clips
    public int maxClips = 12; 
    private bool recordMode = false;
    private Dictionary<string, int> noteToIndex;
    private string[] noteNames = { "C", "Cs", "D", "Ds", "E", "F", "Fs", "G", "Gs", "A", "As", "B" };

    // Change audioClips to a list of lists
    private List<List<AudioClip>> audioClips = new List<List<AudioClip>>(); 
    private int maxClipsPerNote = 3; // Maximum clips per note
    
    private void Awake()
    {
        // Initialize the dictionary to map notes to indices
        noteToIndex = new Dictionary<string, int>
        {
            { "C", 0 }, { "Cs", 1 }, { "D", 2 }, { "Ds", 3 },
            { "E", 4 }, { "F", 5 }, { "Fs", 6 }, { "G", 7 },
            { "Gs", 8 }, { "A", 9 }, { "As", 10 }, { "B", 11 }
        };
        
        // Initialize the audioClips list for each note
        for (int i = 0; i < noteNames.Length; i++)
        {
            List<AudioClip> clipsForNote = new List<AudioClip>(maxClipsPerNote);
            for (int j = 0; j < maxClipsPerNote; j++)
            {
                clipsForNote.Add(null); // Add placeholders for the audio clips
            }
            audioClips.Add(clipsForNote); // Add the list to the main list
        }
    }
    public void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            string deviceName = Microphone.devices[0]; // Use the first microphone device
            ThisObjectAudioSource.clip = Microphone.Start(deviceName, false, 3600, 44100); // Record for up to 10 seconds OR this should be a dynamic number based on the length of the breath.
            TimeEndingOfRecordingCoroutine(10);

            Debug.Log("Recording: started...");
        }
        else
        {
            Debug.LogWarning("Recording: No microphone detected!");
        }
    }

    //INFO NEEDED ON HOW TO DELETE OTHER RECORDED CLIPS
    public void SaveRecording(AudioClip recordedClip, string noteName)
    {
        if (noteToIndex.TryGetValue(noteName, out int index))
        {
            List<AudioClip> clipsForNote = audioClips[index];
            bool saved = false;
            for(int i = 0;i<maxClipsPerNote;i++)
            {
                if(clipsForNote[i] == null)
                {
                    clipsForNote[i] = recordedClip;
                    saved = true;
                    Debug.Log($"Recording saved for note {noteName} in slot {i}.");
                    break;
                }
            }
            if (!saved)
            {
                clipsForNote[0] = recordedClip; // Overwrite the first slot (you can implement more complex rotation logic if needed)
                Debug.Log($"No free slot for note {noteName}. Overwriting the oldest clip.");
            }
        }
    }
    
    public void StopRecording()
    {
        Debug.Log(musicSystem1.fundamentalNote);
        if (Microphone.IsRecording(null))
        {
            // Stop the microphone recording
            Microphone.End(null);
            Debug.Log("Recording: stopped.");

            // Save a reference to the current audioSource clip
            AudioClip recordedClip = ThisObjectAudioSource.clip;
        }
    }

    //A coroutine that is used to control audio recording - it records for a set amount of time and then stops recording
    private IEnumerator TimeEndingOfRecordingCoroutine(float duration)
    {
        //wait for the "duration" amount of seconds
        yield return new WaitForSeconds(duration);
        float _t = 0.0f;
        while(imitoneVoiceInterpreter._tThisRest < 0.5f || _t < 20f)
        {
            if(!recordMode)
            {
                break;
            }

            _t += Time.deltaTime;
            yield return null;
        }
        StopRecording();
        
    }


    public void SetRecordReplayMode(bool localRecordMode)
    {
        if (localRecordMode)
        {
            recordMode = true;
            if(!Microphone.IsRecording(null))
            {
                StartRecording();
            }
        }
        else
        {
            StopRecording();
            recordMode = false;
        }
    }
}

