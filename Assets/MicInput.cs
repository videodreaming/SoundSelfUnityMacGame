using UnityEngine;

public class MicInput : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.mute = false; 
        
        string micName = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (micName != null)
        {
            audioSource.clip = Microphone.Start(micName, true, 10, 44100);
            while (!(Microphone.GetPosition(micName) > 0)) { } // Wait for the mic to start
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("No microphone found!");
        }
    }
}
