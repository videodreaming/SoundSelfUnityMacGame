using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

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

    // Start is called before the first frame update
    void Start()
    {
        /*// We first enumerate all Devices from the System shareset to have all available devices on Windows.
        uint sharesetIdSystem = AkSoundEngine.GetIDFromString("System");
        uint deviceCount = 0;
        // Change the name for the name of the Device as written in the Authoring Audio Preference list.
        uint deviceId = AkSoundEngine.GetDeviceIDFromName("VS24A (NVIDIA High Definition Audio)");

        // We create the Second Audio Device Listener GameObject and find the System_01 ShareSetID.
        AkSoundEngine.RegisterGameObj(gameObject, "System2Listener");
        uint sharesetIdSystem2 = AkSoundEngine.GetIDFromString("System_01");
        // Creation of the Output Settings for the second Audio Device. Which will be another device on the machine different from the main Default Device (e.g a Focusrite).
        AkOutputSettings outputSettings2 = new AkOutputSettings();
        outputSettings2.audioDeviceShareset = sharesetIdSystem2;
        // In my case, on my machine, I output to my monitor speaker in HDMI which is different from the default audio device (Realtek audio).
        if (devices.Count() == 0)
        {
            print("No Devices found");
            return;
        }
        outputSettings2.idDevice = deviceId;
        // We call the AddOutput with the newly created OutputSetting2 for the System_01 and for the system2Listener.
        ulong outDeviceId = 0;
        ulong[] ListenerIds = { AkSoundEngine.GetAkGameObjectID(gameObject) };
        AkSoundEngine.AddOutput(outputSettings2, out outDeviceId, ListenerIds, 1);
        // We Set the listener of Game_Object_System2 to be listened by system2Listener. Set will clear all Emitter-Listener already there, 
        // so the default listener will not be associated anymore.
        AkSoundEngine.RegisterGameObj(gameObject, "System2Go");
        AkSoundEngine.SetListeners(AkSoundEngine.GetAkGameObjectID(gameObject), ListenerIds, 1);

        AkSoundEngine.PostEvent("Play_Sound2", gameObject);*/
    


        musicSystem1.fundamentalNote = 9;
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

    public void userToningToChangeFundamental()
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup_12Pitches_FundamentalOnly", ConvertIntToNote(musicSystem1.fundamentalNote),gameObject);
        //AkSoundEngine.PostEvent("Play_Toning3_Fundamentalonly", gameObject);
        Debug.Log("Fundamental Note: " + ConvertIntToNote(musicSystem1.fundamentalNote));
    }
    public void changeHarmony()
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", ConvertIntToNote(musicSystem1.harmonyNote), gameObject);
        //AkSoundEngine.PostEvent("Play_Toning3_HarmonyOnly", gameObject);
        Debug.Log("Harmony Note: " + ConvertIntToNote(musicSystem1.harmonyNote));
    }

    // Update is called once per frame
    void Update()
    {
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
