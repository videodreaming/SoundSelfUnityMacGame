using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecodedAudioPlaybackTest : MonoBehaviour
{
    public AudioManager audioManager; // Assign your AudioManager in the Inspector
    public DevelopmentMode developmentMode; // Assign your DevelopmentMode in the Inspector
    void Update()
    {
        // Check if AudioManager and recordedAudioClip are assigned
        if (audioManager != null && audioManager.recordedAudioClip != null)
        {
            // Create an AudioSource on this GameObject if it doesn't already have one
            AudioSource audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Assign the recordedAudioClip to the AudioSource
            audioSource.clip = audioManager.recordedAudioClip;

            if(developmentMode.developmentMode && Input.GetKeyDown(KeyCode.H)){
                audioSource.Play();
            }
            // Play the AudioClip

        }
    }

}
