using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityPlayBack : MonoBehaviour
{
    private AudioSource audioSource;
    //public float targetVolume;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.mute = false;

        string micName = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;

        if (micName != null)
        {
            Debug.Log("Using microphone For Playback: " + micName);
            audioSource.clip = Microphone.Start(micName, true, 10, 44100);
            audioSource.volume = 0.0f;
            //StartCoroutine(WaitForMicAndPlay(micName));
        }
        else
        {
            Debug.LogWarning("No microphone found!");
        }
    }

}

