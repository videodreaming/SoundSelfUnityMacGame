using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AK.Wwise;

public class testScript : MonoBehaviour
{
    public AK.Wwise.Event wwiseEvent;
    private uint playingID = AkSoundEngine.AK_INVALID_PLAYING_ID;
    private bool isPlaying = false;

    void Start()
    {
        // Post the Wwise event and register a callback for the end of the event
        playingID = wwiseEvent.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, OnEventEnd);
        isPlaying = true;
    }

    void Update()
    {
        // Check if the event is playing and log to the console
        if (isPlaying)
        {
            Debug.Log("The Wwise event is currently playing.");
        }
    }

    private void OnEventEnd(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        // Update the playing state when the event ends
        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            isPlaying = false;
        }
    }
}
