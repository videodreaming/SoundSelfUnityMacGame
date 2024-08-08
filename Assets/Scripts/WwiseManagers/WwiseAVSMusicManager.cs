using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using System;

//TODO
//Set "Q" from blue to dark, before commiting changes

public class WwiseAVSMusicManager : MonoBehaviour
{
    [SerializeField] AkDeviceDescriptionArray m_devices;
    float overrideValue = 100.0f;
    bool AVSColorSelected = false;
    bool AVSColorSelectedLastFrame = false;
    bool AVSColorChangeFrame = false;
    public string AVSColorCommand  = "";
    public string AVSStrobeCommand = "";
    public float _strobePWM    = 0.0f;
    public float _strobe1Smoothing = 0.0f;
    private bool toneResponseFlag = false;
    private bool toneResponsePrintFlag = false;
    private float _debugValue1    = 0.0f;
    private float _debugValue2    = 0.0f;
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
        AkSoundEngine.PostEvent("Play_AVS_Wave2", gameObject);
        //AkSoundEngine.PostEvent("Play_AVS_Wave3", gameObject);
        AkSoundEngine.PostEvent("Play_AVS_SineGenerators_RGB",gameObject);
        
        //Initialize default RTPC values
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave1", 1.0f, gameObject); //1 enables modulation effects
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave2", 1.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave3", 0.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", 100.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave2", 100.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave1", 55.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave2", 55.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Waveform_Wave1", 2.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Waveform_Wave2", 2.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave1", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave2", 40.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Smoothing_Wave2", 0.0f, gameObject);

        //===== REEF, COULD YOU HAVE A LOOK AT THIS? =====
        //IF THE CONNECTION BETWEEN UNITY AND WWISE IS CORRECT
        //THEN THESE LINES SHOULD DISABLE ALL THE LIGHTS
        //These first ones won't do anything unless Strobe_ToneResponse has its functionality commented out.
         AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave1", 0.0f, gameObject);
         AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave2", 0.0f, gameObject);
        //These ones, however, should be fatal to any AVS activity other than the reference tone.
         AkSoundEngine.PostEvent("Stop_AVS_Wave1", gameObject);
         AkSoundEngine.PostEvent("Stop_AVS_Wave2", gameObject);
         AkSoundEngine.PostEvent("Stop_AVS_Wave3", gameObject);
         //On the other hand, confusingly to me, "Play_AVS_SineGenerators_RGB" seems to be
         //required for the lights to work, but I thought (perhaps incorrectly?) that that was retired?
        //=================================================

        SetColorWorldByName("Dark", 0.0f);
    }

    void PopulateDevicesList() 
    {
        uint sharesetIdSystem = AkSoundEngine.GetIDFromString("System");
        uint deviceCount = AkSoundEngine.GetNumOutputDevices(sharesetIdSystem);
        print("Device count is: " + deviceCount);
        m_devices = new AkDeviceDescriptionArray((int)deviceCount);
        AkSoundEngine.GetDeviceList(sharesetIdSystem, out deviceCount, m_devices);
    }
    void SetStrobeRate(float _rate, float transitionTimeSec = 0.0f)
    {
        if (AVSStrobeCommand != "")
        {
            Debug.Log("Warning: Strobe Command already made this frame, ignoring new command for " + _rate + " Hz");
            return;
        }
        int transitionTimeMS = (int)(transitionTimeSec * 1000);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave1", _rate, gameObject, transitionTimeMS);
        if(transitionTimeMS == 0)
        {
            Debug.Log("Strobe Rate set to: " + _rate + " Hz immediately");
            AVSStrobeCommand = "Strobe Rate: " + _rate + " Hz immediately";
        }
        else
        {
            Debug.Log("Strobe Rate set to: " + _rate + " Hz over " + transitionTimeMS + " ms");
            AVSStrobeCommand = "Strobe Rate: " + _rate + " Hz over " + transitionTimeMS + " ms";
            //StartCoroutine(ReportStrobeTargetMet(_rate, transitionTimeSec));
        }
        
    }
    
    //COLOR WORLD FUNCTIONS
    //   Name        Strobe Color                Wave Color
    private static readonly Dictionary<string, ((float, float, float) strobeColor, (float, float, float) waveColor)> colorPresets = new Dictionary<string, ((float, float, float), (float, float, float))>
    {
        { "Dark", ((0.0f, 0.0f, 0.0f),          (0.0f, 0.0f, 0.0f)) },
        { "Red1", ((100.0f, 0.0f, 0.0f),        (72.0f, 100.0f, 100.0f)) },
        { "Red2", ((100.0f, 100.0f, 100.0f),    (68.0f, 100.0f, 0.0f)) },
        { "Red3", ((100.0f, 0.0f, 100.0f),      (68.0f, 100.0f, 0.0f)) },
        { "Blue1", ((0.0f, 0.0f, 100.0f),       (0.0f, 100.0f, 0.0f)) },
        { "Blue2", ((0.0f, 58.0f, 42.0f),       (0.0f, 66.0f, 40.0f)) },
        { "Blue3", ((0.0f, 54.0f, 100.0f),      (46.0f, 49.0f, 0.0f)) },
        { "White1", ((56.0f, 67.0f, 81.0f),     (40.0f, 65.0f, 66.0f)) },
        { "White2", ((56.0f, 100.0f, 80.0f),    (71.0f, 0.0f, 100.0f)) },
        { "White3", ((68.0f, 50.0f, 50.0f),     (71.0f, 0.0f, 40.0f)) }
    };

    void SetColorWorld(string colorName, (float, float, float) strobeColor, (float, float, float) waveColor, float transitionTimeSec = 2.0f)
    {
        int transitionTimeMS = (int)(transitionTimeSec * 1000);
        
        SetWaveColor(1, strobeColor.Item1, strobeColor.Item2, strobeColor.Item3, transitionTimeMS);
        SetWaveColor(2, strobeColor.Item1, strobeColor.Item2, strobeColor.Item3, transitionTimeMS);
        SetWaveColor(3, waveColor.Item1, waveColor.Item2, waveColor.Item3, transitionTimeMS);
        AVSColorCommand = $"Transition to {colorName} over {transitionTimeSec} s";
    }

    void SetColorWorldByName(string colorName, float transitionTimeSec = 2.0f)
    {
        if (colorPresets.TryGetValue(colorName, out var colors))
        {
            SetColorWorld(colorName, colors.strobeColor, colors.waveColor, transitionTimeSec);
            if (colorName == "Dark")
            {
                StartCoroutine(GoDark((int)(transitionTimeSec * 1000)));
            }
        }
        else
        {
            throw new ArgumentException($"Color name '{colorName}' not found in presets.");
        }
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
    // DYNAMIC AVS CONTROL SYSTEMS
    public void Strobe_ToneResponse (float _input, float _gammaBurstMode = 0.0f)
    {
        float _m2 = Mathf.Max(Mathf.Min(_gammaBurstMode, 1.0f), 0.0f);
        float _m1 = 1.0f - _m2;
        float _i = Mathf.Max(Mathf.Min(_input, 1.0f), 0.0f);
        float _strobe1Depth       = (44.0f + 56.0f * _i);
        float _strobe1            = (50.0f + ((_m1 - _m2) * 50.0f * _i)); //gamma bursts make this go from 50 to 0, otherwise 50 to 100 
        float _strobe2            = (_m2 * 100.0f * _i); 

        if (toneResponseFlag)
        {
            Debug.Log("Warning: Tone Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
        }
        toneResponseFlag    = true;
    
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", _strobe1Depth, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave1", strobe1, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave2", strobe2, gameObject);

        //Below is only required for debugging
        if (!toneResponseFlag  && (_i == 1.0f || _i == 0.0f))
        {
            Debug.Log("Strobe Tone-Control Input(" + _i + ") Strobe1 Depth(" + _strobe1Depth + ") Strobe1(" + _strobe1 + ") Strobe2(" + _strobe2 + ")");
            toneResponsePrintFlag = true;
        }
        else if (_i != 1.0f && _i != 0.0f)
        {
            toneResponsePrintFlag = false;
        }   
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

    IEnumerator ReportStrobeTargetMet(float targetRate, float timeToWait)
    {
        float timeRemaining = timeToWait;
        yield return null;
        while(timeRemaining > 0)
        {
            if(AVSStrobeCommand != "")
            {
                yield break;
            }
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
        if(AVSStrobeCommand == "")
        {
            AVSStrobeCommand = "(Strobe Rate Settled: " + targetRate + " Hz)";
        }
        Debug.Log("Strobe Rate Settled: " + targetRate + " Hz");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug Controls
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
            SetColorWorldByName("Red2", 2.0f);
        }
        if(Input.GetKeyDown(KeyCode.Q))
        {
            SetColorWorldByName("Dark", 2.0f);
        }

        if(Input.GetKeyDown(KeyCode.W))
        {
            SetStrobeRate(15f, 5f);
        }
        if(Input.GetKeyUp(KeyCode.W))
        {
            SetStrobeRate(5f, 0f);
        }
        if(Input.GetKey(KeyCode.E))
        {
            _debugValue1 = Mathf.Min(_debugValue1 + Time.deltaTime * 0.5f, 1.0f);
            //Strobe_ToneResponse(_debugValue1, _debugValue2);
        }
        else
        {
            _debugValue1 = Mathf.Max(_debugValue1 - Time.deltaTime * 0.5f, 0.0f);
            //Strobe_ToneResponse(_debugValue1, _debugValue2);
        }
        if(Input.GetKey(KeyCode.R))
        {
            _debugValue2 = Mathf.Min(_debugValue2 + Time.deltaTime * 0.5f, 1.0f);
        }
        else
        {
            _debugValue2 = Mathf.Max(_debugValue2 - Time.deltaTime * 0.5f);
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
        AVSStrobeCommand = "";
        toneResponseFlag = false;
    }
}
