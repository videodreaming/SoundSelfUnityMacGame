using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneManager : MonoBehaviour
{
    private AudioSource audioSource;
    private bool isRecording = false;
    private AudioClip recordedClip;
    private string microphone;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (Microphone.devices.Length > 0)
        {
            microphone = Microphone.devices[0];
            audioSource.loop = true;
            audioSource.mute = false; // Initially mute the audio source
            audioSource.clip = Microphone.Start(microphone, true, 10, 44100);
            while (!(Microphone.GetPosition(microphone) > 0)) { 
                audioSource.Play();
            }
            
        }
        else
        {
            Debug.LogError("No microphone found to record audio.");
        }
    }

    void Update()
    {
    if (Microphone.IsRecording(microphone))
    {
    }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
                SaveRecording();
            }
        }
    }

    private void StartRecording()
    {
        recordedClip = Microphone.Start(microphone, true, 300, 44100);
        isRecording = true;
        Debug.Log("Recording Started");
    }

    private void StopRecording()
    {
        Microphone.End(microphone);
        isRecording = false;
        Debug.Log("Recording Stopped");
    }

    private void SaveRecording()
    {
        if (recordedClip == null)
        {
            Debug.LogError("Audio clip is null");
            return;
        }

        var filename = "RecordedAudio.wav";
        var filepath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        SavWav.Save(filepath, recordedClip); // Using SavWav utility
        Debug.Log("Recording saved: " + filepath);
    }
}
