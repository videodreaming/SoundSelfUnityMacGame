using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityPlayBack : MonoBehaviour
{
    private AudioSource audioSource;
    public float targetVolume;
    public DevelopmentMode developmentMode;

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
            audioSource.volume = 2.0f;
            //StartCoroutine(WaitForMicAndPlay(micName));
        }
        else
        {
            Debug.LogWarning("No microphone found!");
        }
    }

    void Update()
    {
        if(audioSource.volume != targetVolume)
        {
            audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime);
        }
        //REMOVE ON PRODUCTION BUILD
        if(developmentMode.developmentMode)
        {
            if(Input.GetKeyDown(KeyCode.Q))
            {
                targetVolume = 1.0f;
            } 

            if(Input.GetKeyDown(KeyCode.W))
            {
                targetVolume = 0f;
            }
        }

    }

}

