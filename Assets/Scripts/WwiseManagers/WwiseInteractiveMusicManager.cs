using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using AK.Wwise;

public class WwiseInteractiveMusicManager : MonoBehaviour
{
    public MusicSystem1 musicSystem1;
    public string currentSwitchState = "B";
    public string currentToningState = "None";
    public float InteractiveMusicSilentLoopsRTPC = 0.0f;
    public float HarmonySilentVolumeRTPC = 0.0f;
    public float FundamentalSilentVolumeRTPC = 0.0f;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public uint playingId;
    private bool toneActiveTriggered = false; // Flag to control the event triggering
    public bool interactive = false;
    
    private bool silentPlaying = false;
    private bool previousToneActiveConfident = false;

    public float fadeDuration = 54.0f;
    public float targetValue = 80.0f;
    public RTPC silentrtpcvolume;
    public RTPC toningrtpcvolume;
    public RTPC silentFundamentalrtpcvolume;
    public RTPC toningFundamentalrtpcvolume;
    public RTPC silentHarmonyrtpcvolume;
    public RTPC toningHarmonyrtpcvolume;

    // Start is called before the first frame update
    void Start()
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", "E", gameObject);
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup_12Pitches_FundamentalOnly", "A", gameObject);
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", 30.0f, gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Fundamentalonly", gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Harmonyonly", gameObject);
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Fundamentalonly", "AVS System");
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Harmonyonly", "Master Audio Bus");
    }

    void PlaySoundOnSpecificBus(string eventName, string busName)
    {
        uint eventID = AkSoundEngine.PostEvent(eventName, gameObject);

    // Get the bus ID using the bus name
        uint busID = AkSoundEngine.GetIDFromString(busName);
    // Ensure the bus ID is correctly retrieved and used
    if (busID != AkSoundEngine.AK_INVALID_UNIQUE_ID)
    {
        Debug.Log("Bus ID retrieved for bus name: " + busName);
    }
    else
    {
        Debug.LogError("Invalid bus ID retrieved for bus name: " + busName);
    }
    // Set the output bus for the playing event
        AkSoundEngine.SetBusDevice(eventID, busID);
    }

    public void userToningToChangeFundamental(string FundamentalnoteReceived)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", FundamentalnoteReceived, gameObject);
    }
    public void changeHarmony(string HarmonynoteRecieved)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", HarmonynoteRecieved, gameObject);
    }

    // Update is called once per frame

        public IEnumerator InteractiveMusicSystemFade()
    {
        float initialValue = 0.0f;
        float startTime = Time.time;

        while(Time.time - startTime <fadeDuration)
        {
            float elapsed = (Time.time - startTime)/fadeDuration;
            float currentValue = Mathf.Lerp(initialValue, targetValue, elapsed);
            silentrtpcvolume.SetGlobalValue(currentValue);
            toningrtpcvolume.SetGlobalValue(currentValue);
            yield return null;
        }
        silentrtpcvolume.SetGlobalValue(targetValue);
        toningrtpcvolume.SetGlobalValue(targetValue);
        yield break;
    }
    
    private float GetRTPCValue(RTPC rtpc)
    {
        uint rtpcID = AkSoundEngine.GetIDFromString(rtpc.Name);
        int valueType = 1; // AkRTPCValue_type type, 0 for game object, 1 for global RTPC
        float value;
        AkSoundEngine.GetRTPCValue(rtpcID, gameObject, 0, out value, ref valueType);
        return value;
    }
    void Update()
    {
         if(interactive)
        {
            if(silentPlaying == false)
            {
                silentrtpcvolume.SetGlobalValue(targetValue);
                toningrtpcvolume.SetGlobalValue(targetValue);
                //StartCoroutine (InteractiveMusicSystemFade());
                AkSoundEngine.PostEvent("Play_SilentLoops_v3_FundamentalOnly",gameObject);
                AkSoundEngine.PostEvent("Play_SilentLoops_v3_HarmonyOnly",gameObject);
                silentPlaying = true;
            }
            
            bool currentToneActiveConfident = imitoneVoiceIntepreter.toneActiveConfident;
            if(currentToneActiveConfident && !previousToneActiveConfident)
            {
                AkSoundEngine.PostEvent("Play_Toning_v3_FundamentalOnly",gameObject);
                AkSoundEngine.PostEvent("Play_Toning_v3_HarmonyOnly",gameObject);
            } else if (!currentToneActiveConfident && previousToneActiveConfident)
            {
                AkSoundEngine.PostEvent("Stop_Toning",gameObject);
            }
            previousToneActiveConfident = currentToneActiveConfident;
        }


        if(Input.GetKeyDown(KeyCode.F))
        {
            musicSystem1.fundamentalNote = 6;
        }
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
                //AkSoundEngine.StopPlayingID(playingId);
                toneActiveTriggered = false; // Reset the flag when toneActiveConfident becomes false
            }
        }
    }

    void toning()
    {
        //playingId = AkSoundEngine.PostEvent("Play_InteractiveMusicSystem_Toning", gameObject);
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



        public enum NoteName
    {
        C,
        CsharpDflat,
        D,
        DsharpEflat,
        E,
        F,
        FsharpGflat,
        G,
        GsharpAflat,
        A,
        AsharpBflat,
        B
    }

    public string ConvertIntToNote(int noteNumber)
    {
        if (noteNumber >= 0 && noteNumber <= 11)
        {
            return Enum.GetName(typeof(NoteName), noteNumber);
        }
        else
        {
            throw new ArgumentException("Invalid noteNumber value");
        }
    }
}
