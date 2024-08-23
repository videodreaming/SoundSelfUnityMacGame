using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using AK.Wwise;
using System;

//TODO
//Set "Q" from blue to dark, before commiting changes

public class WwiseAVSMusicManagerForPlayGround : MonoBehaviour
{
    [SerializeField] AkDeviceDescriptionArray m_devices;
    float overrideValue = 100.0f;
    bool AVSColorSelected = false;
    bool AVSColorSelectedLastFrame = false;
    bool AVSColorChangeFrame = false;
    public string AVSColorCommand  = "";
    public string AVSStrobeCommand = "";
    private string cycleRecent = "dark";
    public string preferredColor = "Red";
    private float  _brightness = 0.8f;
    private int cycleRed = 0;
    private int cycleBlue = 0;
    private int cycleWhite = 0;
    public float _strobePWM    = 0.0f;
    public float _strobe1Smoothing = 0.0f;
    public float _gammaBurstMode = 0.0f;
    private bool toneVisualizationFlag = false;
    private bool chargeVisualizationFlag = false;
    private bool breathVisualizationFlag = false;
    private float _debugValue1    = 0.0f;
    private float _debugValue2    = 0.0f; //currently unused
    private float _debugValue3    = 0.0f;
    private float _debugValue4    = 0.0f;
    private int Qcount              = 0;

    public uint rtpcID;
    public float frequencyWave1Value;
    void Start()
    {
        rtpcID = AkSoundEngine.GetIDFromString("AVS_Modulation_Frequency_Wave1");
        AkSoundEngine.SetRTPCValue(rtpcID, 10.0f, gameObject);
        float initialValue;
        int type = 1;
        AkSoundEngine.GetRTPCValue(rtpcID, gameObject, 0, out initialValue, ref type);
        Debug.Log("RTPC Wave1 Frequency after initialization: " + initialValue);
        // We first enumerate all Devices from the System shareset to have all available devices on Windows.
       uint sharesetIdSystem = AkSoundEngine.GetIDFromString("System");
        uint deviceCount = AkSoundEngine.GetNumOutputDevices(sharesetIdSystem);
        AkDeviceDescriptionArray devices = new AkDeviceDescriptionArray((int)deviceCount);
        AkSoundEngine.GetDeviceList(sharesetIdSystem, out deviceCount, devices);

        // Return the device with the specified name on the system. This is where you will either put you logic to enumarate all the Device and let the user decide, or force a specified device directly.
        string wantedDevice;

        // We set the wantedDevice to the name of the device we want to use. This is the name of the device as it appears in the Wwise Audio Device Manager.
        #if UNITY_STANDALONE_OSX
            wantedDevice = "Kasina MMS Audio";
        #elif UNITY_STANDALONE_WIN
            wantedDevice = "Speakers (Kasina MMS Audio)";
        #else
            Debug.LogError("Unsupported platform");
            return;
        #endif


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
        AkSoundEngine.PostEvent("Play_AVS_Wave3", gameObject);
        
        //Initialize default RTPC values
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave1", 1.0f, gameObject); //1 enables modulation effects
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave2", 1.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave3", 0.0f, gameObject);
        
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave3", 0.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", 100.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave2", 100.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave1", 55.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave2", 55.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Waveform_Wave1", 2.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Waveform_Wave2", 2.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave1", 10.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave2", 40.0f, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Smoothing_Wave2", 0.0f, gameObject);

        //Randomize initial colors for red/blue/white
        cycleRed = UnityEngine.Random.Range(0, 3);
        cycleBlue = UnityEngine.Random.Range(0, 3);
        cycleWhite = UnityEngine.Random.Range(0, 3);

        //=================================================

        SetColorWorldByName("White3", 0.0f);
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
    //   Name        Strobe Color                Wave Color (these will be modified by _brightness in SetColorWorld)
    private static readonly Dictionary<string, ((float, float, float) strobeColor, (float, float, float) waveColor)> colorPresets = new Dictionary<string, ((float, float, float), (float, float, float))>
    {
        { "Dark", ((0.0f, 0.0f, 0.0f),          (0.0f, 0.0f, 0.0f)) },
        { "Red1", ((100.0f, 0.0f, 0.0f),        (72.0f, 100.0f, 100.0f)) },
        { "Red2", ((100.0f, 0.0f, 100.0f),      (68.0f, 100.0f, 0.0f)) },
        { "Red3", ((100.0f, 100.0f, 100.0f),    (68.0f, 100.0f, 0.0f)) },
        { "Blue1", ((0.0f, 0.0f, 100.0f),       (0.0f, 100.0f, 0.0f)) },
        { "Blue2", ((0.0f, 58.0f, 42.0f),       (0.0f, 66.0f, 40.0f)) },
        { "Blue3", ((0.0f, 54.0f, 100.0f),      (46.0f, 49.0f, 0.0f)) },
        { "White1", ((56.0f, 67.0f, 81.0f),     (40.0f, 65.0f, 66.0f)) },
        { "White2", ((56.0f, 100.0f, 80.0f),    (71.0f, 0.0f, 100.0f)) },
        { "White3", ((68.0f, 50.0f, 50.0f),     (71.0f, 0.0f, 40.0f)) }
    };


    public void NextColorWorld(float transitionTimeSec = 2.0f, float _exponent = 1.0f)
    {
        SetColorWorldByType(preferredColor, transitionTimeSec, _exponent);
    }

    public void SetColorWorldByType(string colorType, float transitionTimeSec = 2.0f, float _exponent = 1.0f)
    {
        switch (colorType)
        {
            case "Red":
                CycleColor(ref cycleRed, "Red", colorType, transitionTimeSec, _exponent);
                break;
            case "Blue":
                CycleColor(ref cycleBlue, "Blue", colorType, transitionTimeSec, _exponent);
                break;
            case "White":
                CycleColor(ref cycleWhite, "White", colorType, transitionTimeSec, _exponent);
                break;
            case "Dark":
                SetColorWorldByName("Dark", transitionTimeSec);
                break;
        }

        if (colorType != "Red" && colorType != "Blue" && colorType != "White" && colorType != "Dark")
        {
            Debug.LogError("Color type not recognized");
        }
    }
    private void CycleColor(ref int cycleCount, string colorBaseName, string colorType, float transitionTimeSec = 2.0f, float _exponent = 1.0f)
    {
        if (cycleRecent == colorType)
        {
            cycleCount++;
            switch (cycleCount % 3)
            {
                case 1:
                    SetColorWorldByName(colorBaseName + "1", transitionTimeSec, _exponent);
                    break;
                case 2:
                    SetColorWorldByName(colorBaseName + "2", transitionTimeSec, _exponent);
                    break;
                default:
                    SetColorWorldByName(colorBaseName + "3", transitionTimeSec, _exponent);
                    break;
            }
        }
        else
        {
            cycleRecent = colorType;
            switch (cycleCount % 3)
            {
                case 1:
                    SetColorWorldByName(colorBaseName + "3", transitionTimeSec, _exponent);
                    break;
                case 2:
                    SetColorWorldByName(colorBaseName + "1", transitionTimeSec, _exponent);
                    break;
                default:
                    SetColorWorldByName(colorBaseName + "2", transitionTimeSec, _exponent);
                    break;
            }
        }
    }

    void SetColorWorldByName(string colorName, float transitionTimeSec = 2.0f, float _exponent = 1.0f)
    {
        if (colorPresets.TryGetValue(colorName, out var colors))
        {
            SetColorWorld(colorName, colors.strobeColor, colors.waveColor, transitionTimeSec, _exponent);
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

    void SetColorWorld(string colorName, (float, float, float) strobeColor, (float, float, float) waveColor, float transitionTimeSec = 2.0f, float _exponent = 1.0f)
    {
        int transitionTimeMS = (int)(transitionTimeSec * 1000);
        float _v = _brightness;
        
        SetWaveColor(1, strobeColor.Item1*_v, strobeColor.Item2*_v, strobeColor.Item3*_v, transitionTimeMS, _exponent);
        SetWaveColor(2, strobeColor.Item1*_v, strobeColor.Item2*_v, strobeColor.Item3*_v, transitionTimeMS, _exponent);
        SetWaveColor(3, waveColor.Item1*_v, waveColor.Item2*_v, waveColor.Item3*_v, transitionTimeMS, _exponent);
        AVSColorCommand = $"Transition to {colorName} over {transitionTimeSec} s with exponent {_exponent}";
        Debug.Log($"Transition to {colorName} over {transitionTimeSec} s");
    }

    void SetWaveColor(int wave, float _red, float _green, float _blue, int transitionTimeMS, float _exponent = 1.0f)
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

        if(true)//(_exponent == 1.0f)
        {
            AkSoundEngine.SetRTPCValue(stringRed, _red, gameObject, transitionTimeMS);
            AkSoundEngine.SetRTPCValue(stringGreen, _green, gameObject, transitionTimeMS);
            AkSoundEngine.SetRTPCValue(stringBlue, _blue, gameObject, transitionTimeMS);
        }
        if (_exponent != 1.0f)
        {
            StartCoroutine(ExponentialRideColorRTPC(wave, stringRed, stringGreen, stringBlue, _red, _green, _blue, transitionTimeMS, _exponent));
        }
        
        if(_red >0 || _green > 0 || _blue > 0)
        {
            AVSColorSelected = true;
            AVSColorChangeFrame = true;
        }
    }
    IEnumerator ExponentialRideColorRTPC(int wave, string stringRed, string stringGreen, string stringBlue, float _red, float _green, float _blue, int transitionTimeMS = 2000, float _exponent = 1.0f)
    {
        uint rtpcID_red = AkSoundEngine.GetIDFromString(stringRed);
        uint rtpcID_green = AkSoundEngine.GetIDFromString(stringGreen);
        uint rtpcID_blue = AkSoundEngine.GetIDFromString(stringBlue);
        float initialValueRed = -1.0f;
        float initialValueGreen = -1.0f;
        float initialValueBlue = -1.0f;
        int type = 1;
        Debug.Log("[THIS ISN'T WORKING YET] starting exponential ride color rtpc coroutine with exponent " + _exponent);
        //AkSoundEngine.GetRTPCValue(rtpcID_red, gameObject, 0, out initialValueRed, ref type);
        //AkSoundEngine.GetRTPCValue(rtpcID_green, gameObject, 0, out initialValueGreen, ref type);
        //AkSoundEngine.GetRTPCValue(rtpcID_blue, gameObject, 0, out initialValueBlue, ref type);
        yield return null;
        //Debug.Log(stringRed + " Red is " + initialValueRed + " Green is " + initialValueGreen + " Blue is " + initialValueBlue);
    }
    // DYNAMIC AVS CONTROL SYSTEMS
    public void Wwise_Strobe_ToneDisplay (float _input)
    {
        float _m2 = Mathf.Max(Mathf.Min(_gammaBurstMode, 1.0f), 0.0f);
        float _m1 = 1.0f - _m2;
        float _i = Mathf.Max(Mathf.Min(_input, 1.0f), 0.0f);
        float _strobe1Depth       = (44.0f + 56.0f * _i);
        float _strobe1            = (45.0f + (((_m1*55.0f)-(_m2*45.0f)) * _i)); //gamma bursts make this go down, otherwise up 
        float _strobe2            = _m2 * ((70.0f * _i) + (30.0f * Mathf.Min((_i * 2), 1)));//there's a little boost at the bottom end because that works best with the glasses.

        if (toneVisualizationFlag)
        {
            Debug.Log("Warning: AVS Tone Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
        }
        toneVisualizationFlag    = true;
    
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", _strobe1Depth, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave1", _strobe1, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave2", _strobe2, gameObject);
    }

    public void Wwise_Strobe_ChargeDisplay (float _input) //RENAME THIS TO JUST BE WWISE_CHARGE
    {
        float _i = Mathf.Max(Mathf.Min(_input, 1.0f), 0.0f);
        float _strobe1Smoothing = 100.0f - _i*100.0f;
        float _strobePWM = 25.0f + 50.0f * _i;

        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave1", _strobePWM, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave2", _strobePWM, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Smoothing_Wave1", _strobe1Smoothing, gameObject);
        AkSoundEngine.SetRTPCValue("Unity_Charge", _input*100.0f, gameObject);

        if (chargeVisualizationFlag)
        {
            Debug.Log("Warning: AVS Charge Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
        }
        chargeVisualizationFlag = true;
    }

    public void Wwise_BreathDisplay (float _input)
    {
        float _i = Mathf.Max(Mathf.Min(_input, 1.0f), 0.0f);
        float _waveValue = 0.0f + 100.0f * _i;

        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave3", _waveValue, gameObject);
        AkSoundEngine.SetRTPCValue("Unity_Inhale", _input*100.0f, gameObject);

        if (_i != 0.0f)
        //Debug.Log("Breath Wave Value: " + _waveValue);

        if(breathVisualizationFlag)
        {
            Debug.Log("Warning: AVS Breath Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
        }
        breathVisualizationFlag = true;

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
        if (Input.GetKeyDown(KeyCode.K))
        {
            PopulateDevicesList();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            printDevicesList();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            NextColorWorld(5f);
        }
        else if (Input.GetKeyUp(KeyCode.C))
        {
            NextColorWorld(10f, 0.5f);
        }
        /*if(Input.GetKeyDown(KeyCode.C))
        {
            Qcount++;
            if(Qcount%10 == 0)
            {
                SetColorWorldByName("Dark", 2.0f);
            }
            else if(Qcount%10 == 1)
            {
                SetColorWorldByName("Red1", 2.0f);
            }
            else if(Qcount%10 == 2)
            {
                SetColorWorldByName("Red2", 2.0f);
            }
            else if(Qcount%10 == 3)
            {
                SetColorWorldByName("Red3", 2.0f);
            }
            else if(Qcount%10 == 4)
            {
                SetColorWorldByName("Blue1", 2.0f);
            }
            else if(Qcount%10 == 5)
            {
                SetColorWorldByName("Blue2", 2.0f);
            }
            else if(Qcount%10 == 6)
            {
                SetColorWorldByName("Blue3", 2.0f);
            }
            else if(Qcount%10 == 7)
            {
                SetColorWorldByName("White1", 2.0f);
            }
            else if(Qcount%10 == 8)
            {
                SetColorWorldByName("White2", 2.0f);
            }
            else if(Qcount%10 == 9)
            {
                SetColorWorldByName("White3", 2.0f);
            }
        }
        */

        if(Input.GetKeyDown(KeyCode.W))
        {
            SetStrobeRate(15f, 5f);
        }
        if(Input.GetKeyUp(KeyCode.W))
        {
            SetStrobeRate(5f, 0f);
        }
        if(Input.GetKey(KeyCode.R))
        {
            _gammaBurstMode = Mathf.Min(_debugValue2 + Time.deltaTime * 0.5f, 1.0f);
        }
        else
        {
            _gammaBurstMode = Mathf.Max(_debugValue2 - Time.deltaTime * 0.5f, 0.0f);
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
        toneVisualizationFlag = false;
        chargeVisualizationFlag = false;
        breathVisualizationFlag = false;
    }
}
