using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using System;

public class WwiseAVSMusicManager : MonoBehaviour
{
    [SerializeField] AkDeviceDescriptionArray m_devices;
    float overrideValue = 100.0f;
    bool AVSColorSelected = false;
    bool AVSColorSelectedLastFrame = false;
    bool AVSColorChangeFrame = false;
    public string AVSColorCommand  = "";
    void Start()
    {
        // We first enumerate all Devices from the System shareset to have all available devices on Windows.
       uint sharesetIdSystem = AkSoundEngine.GetIDFromString("System");
        uint deviceCount = AkSoundEngine.GetNumOutputDevices(sharesetIdSystem);
        AkDeviceDescriptionArray devices = new AkDeviceDescriptionArray((int)deviceCount);
        AkSoundEngine.GetDeviceList(sharesetIdSystem, out deviceCount, devices);

        // Return the device with the specified name on the system. This is where you will either put you logic to enumarate all the Device and let the user decide, or force a specified device directly.
        string wantedDevice;

        if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            wantedDevice = "Speakers (Kasina MMS Audio)";
        }
        else if (System.Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            wantedDevice = "Kasina MMS Audio";
        }
        else
        {
            Debug.LogError("Unsupported operating system");
            return;
        }
        uint deviceId = 0;
        for (int i = 0; i < devices.Capacity; i++)
        {
            if (devices[i].deviceStateMask == AkAudioDeviceState.AkDeviceState_Active)
            {
                if (devices[i].deviceName == wantedDevice)
                {
                    deviceId = devices[i].idDevice;
                    print("Device found: " + devices[i].deviceName);
                    break;
                }
            }
        }
        if(deviceId == 0)
        {
            Debug.LogError("Device not found");
            return;
        }

        // We create the Second Audio Device Listener GameObject and find the System_01 ShareSetID.
        AkSoundEngine.RegisterGameObj(gameObject, "System2Listener");
        uint sharesetIdSystem2 = AkSoundEngine.GetIDFromString("System_01");
        // Creation of the Output Settings for the second Audio Device. Which will be another device on the machine different from the main Default Device (e.g a Focusrite).
        AkOutputSettings outputSettings2 = new AkOutputSettings();
        outputSettings2.audioDeviceShareset = sharesetIdSystem2;
        outputSettings2.idDevice = deviceId;
        // We call the AddOutput with the newly create OutputSetting2 for the System_01 and for the system2Listener.
        ulong outDeviceId = 0;
        ulong[] ListenerIds = { AkSoundEngine.GetAkGameObjectID(gameObject) };
        AkSoundEngine.AddOutput(outputSettings2, out outDeviceId, ListenerIds, 1);
        // We Set the listener of Game_Object_System2 to be listened by system2Listener. Set will clear all Emitter-Listener already there, 
        // so the default listener will not be associated anymore.
        AkSoundEngine.RegisterGameObj(gameObject, "System2Go");
        AkSoundEngine.SetListeners(AkSoundEngine.GetAkGameObjectID(gameObject), ListenerIds, 1);


        //Play all appropriate AVS waves
        AkSoundEngine.PostEvent("Play_AVS_Wave1", gameObject);
        //AkSoundEngine.PostEvent("Play_AVS_Wave2", gameObject);
        //AkSoundEngine.PostEvent("Play_AVS_Wave3", gameObject);
        AkSoundEngine.PostEvent("Play_AVS_SineGenerators_RGB",gameObject);
    }

    void PopulateDevicesList() 
    {
        uint sharesetIdSystem = AkSoundEngine.GetIDFromString("System");
        uint deviceCount = AkSoundEngine.GetNumOutputDevices(sharesetIdSystem);
        print("Device count is: " + deviceCount);
        m_devices = new AkDeviceDescriptionArray((int)deviceCount);
        AkSoundEngine.GetDeviceList(sharesetIdSystem, out deviceCount, m_devices);
    }
    void SetWaveColor(int wave, float _red, float _green, float _blue, int transitionTimeMS)
    {
        //produce error if "wave" is not between 1 and 3
        if (wave < 1 || wave > 3)
        {
            Debug.LogError("AVS wave must be between 1 and 3");
            return;
        }

        //Change a string, between AVS_Red_Volume_Wave1 and AVS_Red_Volume_Wave2 (etc.) depending on the int value of wave:
        string stringRed = "AVS_Red_Volume_Wave" + wave;
        string stringGreen = "AVS_Green_Volume_Wave" + wave;
        string stringBlue = "AVS_Blue_Volume_Wave" + wave;

        AkSoundEngine.SetRTPCValue(stringRed, _red, gameObject, transitionTimeMS);
        AkSoundEngine.SetRTPCValue(stringGreen, _green, gameObject, transitionTimeMS);
        AkSoundEngine.SetRTPCValue(stringBlue, _blue, gameObject, transitionTimeMS);

        if(_red >0 || _green > 0 || _blue > 0)
        {
            AVSColorSelected = true;
            AVSColorChangeFrame = true;
        }
    }
    void SetStrobeColor(float _red, float _green, float _blue, int transitionTimeMS)
    {
        SetWaveColor(1,     _red,   _green,   _blue,   transitionTimeMS);
        SetWaveColor(2,     _red,   _green,   _blue,   transitionTimeMS);
    }
    //COLOR WORLD FUNCTIONS
    //                  Wave  Red   Green   Blue    Transition Time in MS
    void SetColorWorld_Dark(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   0.0f,   0.0f,   0.0f,   transitionTimeMS);      //strobe = toning
        SetWaveColor    (3, 0.0f,   0.0f,   0.0f,   transitionTimeMS);      //wave = inhalation
        StartCoroutine(GoDark(transitionTimeMS));
        AVSColorCommand = "Dark - " + transitionTimeMS;
    }
    void SetColorWorld_Red1(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   100.0f, 0.0f,   0.0f,   transitionTimeMS);
        SetWaveColor    (3, 72.0f,  100.0f, 100.0f, transitionTimeMS);
        AVSColorCommand = "Red1 - " + transitionTimeMS;
    }
    void SetColorWorld_Red2(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   100.0f, 100.0f, 100.0f, transitionTimeMS);
        SetWaveColor    (3, 68.0f,  100.0f, 0.0f,   transitionTimeMS);
        AVSColorCommand = "Red2 - " + transitionTimeMS;
    }
    void SetColorWorld_Red3(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   100.0f, 0.0f,   100.0f, transitionTimeMS);
        SetWaveColor    (3, 68.0f,  100.0f, 0.0f,   transitionTimeMS);
        AVSColorCommand = "Red3 - " + transitionTimeMS;
    }
    void SetColorWorld_Blue1(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   0.0f,   0.0f,   100.0f, transitionTimeMS);
        SetWaveColor    (3, 0.0f,   100.0f, 0.0f,   transitionTimeMS);
        AVSColorCommand = "Blue1 - " + transitionTimeMS;
    }
    void SetColorWorld_Blue2(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   0.0f,   58.0f,  42.0f,  transitionTimeMS);
        SetWaveColor    (3, 0.0f,   66.0f,  40.0f,  transitionTimeMS);
        AVSColorCommand = "Blue2 - " + transitionTimeMS;
    }
    void SetColorWorld_Blue3(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   0.0f,   54.0f,  100.0f, transitionTimeMS);
        SetWaveColor    (3, 46.0f,  49.0f,  0.0f,   transitionTimeMS);
        AVSColorCommand = "Blue3 - " + transitionTimeMS;
    }
    void SetColorWorld_White1(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   56.0f,  67.0f,  81.0f,  transitionTimeMS);
        SetWaveColor    (3, 40.0f,  65.0f,  66.0f,  transitionTimeMS);
        AVSColorCommand = "White1 - " + transitionTimeMS;
    }
    void SetColorWorld_White2(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   56.0f,  100.0f, 80.0f,  transitionTimeMS);
        SetWaveColor    (3, 71.0f,  0.0f,  100.0f,  transitionTimeMS);
        AVSColorCommand = "White2 - " + transitionTimeMS;
    }
    void SetColorWorld_White3(int transitionTimeMS = 2000)
    {
        SetStrobeColor  (   68.0f,  50.0f, 50.0f,   transitionTimeMS);
        SetWaveColor    (3, 71.0f,  0.0f,  40.0f,   transitionTimeMS);
        AVSColorCommand = "White3 - " + transitionTimeMS;
    }

    void printDevicesList() 
    {
        if(m_devices == null)
        {
            print("Device list not populated");
            return;
        }

        for (int i = 0; i < m_devices.Capacity; i++)
        {
            if (m_devices[i].deviceStateMask == AkAudioDeviceState.AkDeviceState_Active)
            {
                print("Device found: " + m_devices[i].deviceName);
            }
        }
    }

    IEnumerator GoDark(int milliseconds = 1000)
    {
        float timeRemaining = (milliseconds) / 1000f;
        if(AVSColorChangeFrame)
        {
            yield break;
        }
        Debug.Log("Going dark - " + timeRemaining);
        yield return null;
        while(timeRemaining > 0)
        {
            if(AVSColorChangeFrame)
            {
                yield break;
            }
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
        if(AVSColorChangeFrame)
        {
            yield break;
        }
        else
        {
            AVSColorSelected = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            PopulateDevicesList();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            printDevicesList();
        }

        if(Input.GetKeyUp(KeyCode.Q))
        {
            SetColorWorld_Dark(2000);
        }
        if(Input.GetKeyDown(KeyCode.Q))
        {
            SetColorWorld_Red1(2000);
        }
        
        // Start and Stop the Reference Signal, depending on the AVS color world (dark turns off)
        if (AVSColorSelected && !AVSColorSelectedLastFrame)
        {
            AkSoundEngine.PostEvent("Play_AVS_SineGenerators_REFERENCE", gameObject);
            Debug.Log("AVS: Starting Reference Signal");
        }
        else if (!AVSColorSelected && AVSColorSelectedLastFrame)
        {
            AkSoundEngine.PostEvent("Stop_AVS_SineGenerators_REFERENCE", gameObject);
            Debug.Log("AVS: Stopping Reference Signal");
        }
        AVSColorSelectedLastFrame = AVSColorSelected;
    }

    void LateUpdate()
    {
        AVSColorChangeFrame = false;
        AVSColorCommand = "";
    }
}
