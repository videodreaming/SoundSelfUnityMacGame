using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WwiseInteractiveMusicManager : MonoBehaviour
{
    public string currentSwitchState = "B";
    public string currentToningState = "None";
    public float InteractiveMusicSilentLoopsRTPC = 0.0f;
    public float HarmonySilentVolumeRTPC = 0.0f;
    public float FundamentalSilentVolumeRTPC = 0.0f;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public uint playingId;
    private bool toneActiveTriggered = false; // Flag to control the event triggering

    // Start is called before the first frame update
    void Start()
    {
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", 30.0f, gameObject);
        AkSoundEngine.PostEvent("Play_InteractiveMusicSystem_SilentLoops", gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (imitoneVoiceIntepreter.toneActiveConfident)
        {
            if (!toneActiveTriggered)
            {
                toning();
                toneActiveTriggered = true; // Set the flag to true after the event is triggered
            }
        }
        else
        {
            if (toneActiveTriggered)
            {
                AkSoundEngine.StopPlayingID(playingId);
                toneActiveTriggered = false; // Reset the flag when toneActiveConfident becomes false
            }
        }
    }

    void toning()
    {
        playingId = AkSoundEngine.PostEvent("Play_InteractiveMusicSystem_Toning", gameObject);
    }

    public void ChangeToningState()
    {
        AkSoundEngine.SetState("SoundWorldMode", currentToningState);
    }

    public void ChangeSwitchState()
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup", currentSwitchState, gameObject);
    }

    public void setallRTPCValue(float newRTPCValue)
    {
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", newRTPCValue, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", newRTPCValue, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", newRTPCValue, gameObject);
    }

    public void setHarmonySilentVolumeRTPCValue(float newHarmonyVolumeRTPC)
    {
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", newHarmonyVolumeRTPC, gameObject);
    }

    public void setFundamentalSilentVolumeRTPCValue(float newFundamentalVolumeRTPC)
    {
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", newFundamentalVolumeRTPC, gameObject);
    }

    public void setInteractiveMusicSilentLoopsRTPCValue(float newInteractiveMusicSilentLoopRTPC)
    {
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", newInteractiveMusicSilentLoopRTPC, gameObject);
    }
}
