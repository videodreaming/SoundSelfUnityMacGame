using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using AK.Wwise;
using System;
using TMPro;

public class LightControl : MonoBehaviour
{
    public static LightControl instance { get; private set; }

    public WorldShuffler worldShuffler;
    [SerializeField] AkDeviceDescriptionArray m_devices;
    public GameObject gameObjectSystem2Listener;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter; // Reference to an object that interprets voice to musical notes
    private bool playReference = false;
    private bool playReferenceLastFrame = false;
    private bool playReferenceFrame = false;
    public bool bilateral {get; private set;} = false; 
    public string AVSColorCommand  = "";
    public string AVSStrobeCommand = "";
    public string currentColorType = "Dark";
    public string preferredColor = "Dark";

    [Header("Current color world (Play Mode)")]
    [ColorUsage(true, true)]
    [Tooltip("Strobe (AVS waves 1 and 2): RGB after _brightness scaling. Matches colorPresets strobeColor for the active world.")]
    [FormerlySerializedAs("toneWaveColor")]
    public Color currentStrobeColor = new Color(0.0f, 0.0f, 0.0f);
    [ColorUsage(true, true)]
    [Tooltip("Wave / breath (AVS wave 3): RGB after _brightness scaling. Matches colorPresets waveColor for the active world.")]
    [FormerlySerializedAs("breathWaveColor")]
    public Color currentWaveColor = new Color(0.0f, 0.0f, 0.0f);

    private float  _brightness = 0.6f;
    private int cycleRed = 0;
    private int cycleBlue = 0;
    private int cycleWhite = 0;
    private int cycleTest = 0;
    public float _fxWave = 0f;
    [Header("Strobe (runtime)")]
    [SerializeField]
    [Tooltip("Current strobe frequency in Hz. Updated by SetStrobeRate / AVS; visible in Play Mode.")]
    private float _currentStrobeRateInspectorDisplay;
    public float _strobeRate
    {
        get => _currentStrobeRateInspectorDisplay;
        private set => _currentStrobeRateInspectorDisplay = value;
    }
    public float _strobePWM    = 0.0f;
    public float _strobe1Smoothing = 0.0f;
    public float _gammaBurstMode = 0.0f;
    private bool toneVisualizationFlag = false;
    private bool chargeVisualizationFlag = false;
    private bool breathVisualizationFlag = false;
    //private float _debugValue1    = 0.0f;
    //private float _debugValue2    = 0.0f; //currently unused
    //private float _debugValue3    = 0.0f;
    //private float _debugValue4    = 0.0f;
    public float _rate;
    uint wave1ID;
    public uint rtpcID;
    private Coroutine sawStrobeCoroutine;
    private Coroutine gammaCoroutine;
    private Coroutine reportStrobeRateCoroutine;
    public int fxWaveKey = 0;
    public Dictionary <int, float> _fxWaveDict = new Dictionary<int, float>();
    private bool developmentModeWarningFlag = false;

    private bool lightsInitialized = false;

    [Header("Debug logs (StartLights)")]
    [SerializeField] private bool debugAllowLogsLightControl = true;
    [Tooltip("When true, warnings still log even if LightControl info logs are off.")]
    [SerializeField] private bool debugAllowLogsWarnings = true;

    private void DbgLogLightControl(string message, bool isWarning = false)
    {
        if (isWarning)
        {
            if (debugAllowLogsWarnings)
                Debug.LogWarning(message);
        }
        else if (debugAllowLogsLightControl)
        {
            Debug.Log(message);
        }
    }

    public TMP_Dropdown ColorWorldDropdownChange;
    

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("LightControl: Multiple LightControl instances; destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (ColorWorldDropdownChange != null)
            ColorWorldDropdownChange.onValueChanged.AddListener(OnColorWorldDropdownChanged);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;

        Debug.LogWarning("LightControl: OnDestroy called - LightControl (or its GameObject) is being destroyed.");

        if (ColorWorldDropdownChange != null)
            ColorWorldDropdownChange.onValueChanged.RemoveListener(OnColorWorldDropdownChanged);
    }
    void Start()
    {
        rtpcID = AkSoundEngine.GetIDFromString("AVS_Modulation_Frequency_Wave1");
        AkSoundEngine.SetRTPCValue(rtpcID, 10.0f, gameObjectSystem2Listener);
        float initialValue;
        int type = 1;
        AkSoundEngine.GetRTPCValue(rtpcID, gameObjectSystem2Listener, 0, out initialValue, ref type);
        Debug.Log("RTPC Wave1 Frequency after initialization: " + initialValue);
        // We first enumerate all Devices from the System shareset to have all available devices on Windows.
       uint sharesetIdSystem = AkSoundEngine.GetIDFromString("System");
        uint deviceCount = AkSoundEngine.GetNumOutputDevices(sharesetIdSystem);
        AkDeviceDescriptionArray devices = new AkDeviceDescriptionArray((int)deviceCount);
        AkSoundEngine.GetDeviceList(sharesetIdSystem, out deviceCount, devices);

        // Return the device with the specified name on the system. This is where you will either put you logic to enumarate all the Device and let the user decide, or force a specified device directly.
        string wantedDevice1;
        string wantedDevice2;

        // We set the wantedDevice to the name of the device we want to use. This is the name of the device as it appears in the Wwise Audio Device Manager.
        #if UNITY_STANDALONE_OSX
            wantedDevice1 = "Kasina MMS Audio";
            wantedDevice2 = "MPL Audio       ";
        #elif UNITY_STANDALONE_WIN
            wantedDevice1 = "Speakers (Kasina MMS Audio)";
            wantedDevice2 = "Speakers (MPL Audio       )";
            //wantedDevice1 = "Kasina MMS Audio";
            //wantedDevice2 = "MPL Audio       ";
        #else
            Debug.LogError("Unsupported platform");
            return;
        #endif


        uint deviceId = 0;
        for (int i = 0; i < devices.Capacity; i++)
        {
            if (devices[i].deviceStateMask == AkAudioDeviceState.AkDeviceState_Active)
            {
                if (devices[i].deviceName == wantedDevice1)
                {
                    deviceId = devices[i].idDevice;
                    print("Device found: " + devices[i].deviceName + " With ID: " + devices[i].idDevice);
                    break;
                }
                else if(devices[i].deviceName == wantedDevice2)
                {
                    deviceId = devices[i].idDevice;
                    print("Device found: " + devices[i].deviceName + " With ID: " + devices[i].idDevice);
                    break;
                }
                else
                {
                    print("Devices not found");
                }
            }
        }
        if(deviceId == 0)
        {
            Debug.Log("Devices not found");
            return;
        }

        // We create the Second Audio Device Listener GameObject and find the System_01 ShareSetID. With a separate GameObject.
        AkSoundEngine.RegisterGameObj(gameObjectSystem2Listener, "System2Listener");
        uint sharesetIdSystem2 = AkSoundEngine.GetIDFromString("System_01");

        // Creation of the Output Settings for the second Audio Device. Which will be another device on the machine different from the main Default Device.
        AkOutputSettings outputSettings2 = new AkOutputSettings();
        outputSettings2.audioDeviceShareset = sharesetIdSystem2;
        outputSettings2.idDevice = deviceId;
        print("OutputSettings2 ID Device " + outputSettings2.idDevice);
        
        // We call the AddOutput with the newly create OutputSetting2 for the System_01 and for the system2Listener.
        ulong outDeviceId = 0;
        ulong[] ListenerIds = { AkSoundEngine.GetAkGameObjectID(gameObjectSystem2Listener) };
        AkSoundEngine.AddOutput(outputSettings2, out outDeviceId, ListenerIds, 1);
        
        // We Set the listener of Game_Object_System2 to be listened by system2Listener. Set will clear all Emitter-Listener already there, 
        // so the default listener will not be associated anymore.
        AkSoundEngine.RegisterGameObj(gameObjectSystem2Listener, "System2Go");
        AkSoundEngine.SetListeners(AkSoundEngine.GetAkGameObjectID(gameObjectSystem2Listener), ListenerIds, 1);
        print("GameObjectID : " + AkSoundEngine.GetAkGameObjectID(gameObjectSystem2Listener));


        //Play all appropriate AVS waves
        wave1ID = AkSoundEngine.PostEvent("Play_AVS_Wave1", gameObjectSystem2Listener);
        AkSoundEngine.PostEvent("Play_AVS_Wave2", gameObjectSystem2Listener);
        AkSoundEngine.PostEvent("Play_AVS_Wave3", gameObjectSystem2Listener);

        //Initialize default RTPC values
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave1", 1.0f, gameObjectSystem2Listener); //1 enables modulation effects
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave2", 1.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_BypassEffect_Wave3", 0.0f, gameObjectSystem2Listener);

        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave3", 0.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", 100.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave2", 100.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave1", 55.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave2", 55.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Waveform_Wave1", 2.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Waveform_Wave2", 2.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave1", 10.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave2", 40.0f, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Smoothing_Wave2", 0.0f, gameObjectSystem2Listener);

        //Randomize initial colors for red/blue/white
        cycleRed = UnityEngine.Random.Range(0, 3);
        cycleBlue = UnityEngine.Random.Range(0, 3);
        cycleWhite = UnityEngine.Random.Range(0, 3);

    }

    //====================================================================================================
    // Session start lights (first-time Red transition; idempotent)
    //====================================================================================================

    public void StartLights()
    {
        if (!lightsInitialized)
        {
            DbgLogLightControl("LightControl: StartLights");
            SetPreferredColor("Red", 5.0f);
            lightsInitialized = true;
        }
        else
        {
            DbgLogLightControl("LightControl: StartLights already initialized, skipping");
        }
    }

    public void StartLightsWithDelay()
    {
        DbgLogLightControl("LightControl: Starting Lights with Delay");
        StartCoroutine(StartLightsCoroutine());
    }

    private IEnumerator StartLightsCoroutine()
    {
        DbgLogLightControl("LightControl: Waiting for 1 second before starting lights");
        yield return new WaitForSeconds(1f);
        StartLights();
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;

        public Color(float red, float green, float blue)
        {
            r = red;
            g = green;
            b = blue;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleFXWave();
        
        // Start and Stop the Reference Signal, depending on the AVS color world (dark turns off)
        if (playReference && !playReferenceLastFrame)
        {
            AkSoundEngine.PostEvent("Play_AVS_SineGenerators_REFERENCE", gameObjectSystem2Listener);
            Debug.Log("AVS: Starting Reference Signal");
        }
        else if (!playReference && playReferenceLastFrame)
        {
            AkSoundEngine.PostEvent("Stop_AVS_SineGenerators_REFERENCE", gameObjectSystem2Listener);
            Debug.Log("AVS: Stopping Reference Signal");
        }
        playReferenceLastFrame = playReference;
    }

    void HandleFXWave()
    {
       //add all the float values of the dictionary together and output it to _fxWave
        _fxWave = _fxWaveDict.Values.Sum();
    }

    void LateUpdate()
    {
        playReferenceFrame = false;
        AVSColorCommand = "";
        AVSStrobeCommand = "";
        toneVisualizationFlag = false;
        chargeVisualizationFlag = false;
        breathVisualizationFlag = false;
    }

    void PopulateDevicesList() 
    {
        uint sharesetIdSystem = AkSoundEngine.GetIDFromString("System");
        uint deviceCount = AkSoundEngine.GetNumOutputDevices(sharesetIdSystem);
        print("Device count is: " + deviceCount);
        m_devices = new AkDeviceDescriptionArray((int)deviceCount);
        AkSoundEngine.GetDeviceList(sharesetIdSystem, out deviceCount, m_devices);
    }

    public void SetSawStrobe(float _start, float _end, float cycleLength)
    {
        if (sawStrobeCoroutine != null)
        {
            Debug.Log("Saw Strobe already running, stopping and restarting with new parameters");
            StopCoroutine(sawStrobeCoroutine);
        }
        AVSStrobeCommand = "Saw Strobe: " + _start + " to " + _end + " at " + cycleLength + "s wavelength";
        sawStrobeCoroutine = StartCoroutine(SawStrobeCoroutine(_start, _end, cycleLength));
    }

    //NOTING THAT THIS CODE IS UGLY AS FUQ
    private IEnumerator SawStrobeCoroutine(float _start, float _end, float cycleLength)
    {
        yield return null; //wait one frame to ensure that AVSStrobeCommand can update.
        float _timeRemaining = cycleLength;
        
        SetStrobeRate(_end, cycleLength/2f, true);
        MusicBinauralBeats.instance.NewBinauralBeatRate(_end, cycleLength/2f);

        while(_timeRemaining > (cycleLength/2f))
        {
            _timeRemaining -= Time.deltaTime;
            yield return null;
        }

        SetStrobeRate(_start, cycleLength/2f, true);
        MusicBinauralBeats.instance.NewBinauralBeatRate(_start, cycleLength/2f);

        while(_timeRemaining > 0)
        {
            _timeRemaining -= Time.deltaTime;
            yield return null;
        }

        sawStrobeCoroutine = StartCoroutine(SawStrobeCoroutine(_start, _end, cycleLength));
    }

    public void SetStrobeRate(float _rate, float transitionTimeSec = 0.0f, bool partOfCoroutine = false)
    {
        if(!partOfCoroutine && sawStrobeCoroutine != null)
        {
            StopCoroutine(sawStrobeCoroutine);
            Debug.Log("Saw Strobe Coroutine stopped");
        }

        if (!partOfCoroutine)
        {
            if(MusicBinauralBeats.instance == null)
            {
                Debug.LogError("MusicBinauralBeats.instance is null! Skipping NewBinauralBeatRate call.");
            }
            else
            {
                // Check if the MonoBehaviour is enabled and GameObject is active
                MonoBehaviour mb = MusicBinauralBeats.instance as MonoBehaviour;
                if(mb == null)
                {
                    Debug.LogError("MusicBinauralBeats.instance is not a MonoBehaviour! Skipping NewBinauralBeatRate call.");
                }
                else if(!mb.enabled)
                {
                    Debug.LogWarning("MusicBinauralBeats MonoBehaviour is disabled! Skipping NewBinauralBeatRate call to prevent crash.");
                }
                else if(!mb.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("MusicBinauralBeats GameObject is not active in hierarchy! Skipping NewBinauralBeatRate call to prevent crash.");
                }
                else
                {
                    try
                    {
                        MusicBinauralBeats.instance.NewBinauralBeatRate(_rate);
                    }
                    catch(System.Exception ex)
                    {
                        Debug.LogError("Exception calling NewBinauralBeatRate: " + ex.Message + "\n" + ex.StackTrace);
                    }
                }
            }
        }

        if (AVSStrobeCommand != "")
        {
            Debug.LogWarning("Warning: Strobe Command already made this frame, proceeding with new rate of " + _rate + " Hz, but this should really only happen once per frame");
        }

        int transitionTimeMS = (int)(transitionTimeSec * 1000);
        if(gameObjectSystem2Listener == null)
        {
            Debug.LogError("gameObjectSystem2Listener is null! Cannot set RTPC value.");
        }
        else
        {
            AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave1", _rate, gameObjectSystem2Listener, transitionTimeMS);
        }
        //Get RPTC Value of the StrobeRate and then use that as the strobe rate
        //Start a coroutineSetStrobeRate that always reports what _rate is, and stores that into another variable from other scripts
        //_currentStrobeRate
        if(transitionTimeMS == 0)
        {
            Debug.Log("Strobe Rate set to: " + _rate + " Hz immediately");
            if(!partOfCoroutine)
            AVSStrobeCommand = "Strobe Rate: " + _rate + " Hz immediately";
            //StopCoroutine(reportStrobeRateCoroutine);
            _strobeRate = _rate;
        }
        else
        {
            Debug.Log("Strobe Rate set to: " + _rate + " Hz over " + transitionTimeMS + " ms");
            if(!partOfCoroutine)
            AVSStrobeCommand = "Strobe Rate: " + _rate + " Hz over " + transitionTimeMS + " ms";
            //StartCoroutine(ReportStrobeTargetMet(_rate, transitionTimeSec));
            //StopCoroutine(reportStrobeRateCoroutine);
            reportStrobeRateCoroutine = StartCoroutine(ReportStrobeRate(_rate, transitionTimeSec));
        }        
    }

    private IEnumerator ReportStrobeRate(float _targetRate, float _transitionTimeSec)
    {
        float _t = 0f;
        float _initialRate = _strobeRate;

        while (_t < _transitionTimeSec)
        {
            _t += Mathf.Max(Time.deltaTime, 0f);
            _strobeRate = Mathf.Lerp(_initialRate, _targetRate, _t / _transitionTimeSec);
            yield return null;
        }
        _strobeRate = _targetRate;
    }
    
    //COLOR WORLD FUNCTIONS
    //   Name        Strobe Color                Wave Color (these will be modified by _brightness in SetColorWorldByNumbers)
    private static readonly Dictionary<string, ((float, float, float) strobeColor, (float, float, float) waveColor)> colorPresets = new Dictionary<string, ((float, float, float), (float, float, float))>
    {
        { "Dark", ((0.0f, 0.0f, 0.0f),          (0.0f, 0.0f, 0.0f)) },
        { "BreathOnly", ((0.0f, 0.0f, 0.0f),    (100.0f, 0.0f, 0.0f)) },
        { "Red1", ((100.0f, 0.0f, 0.0f),        (72.0f, 100.0f, 100.0f)) },
        { "Red2", ((100.0f, 0.0f, 100.0f),      (68.0f, 100.0f, 0.0f)) },
        { "Red3", ((100.0f, 100.0f, 100.0f),    (68.0f, 100.0f, 0.0f)) },
        { "Blue1", ((0.0f, 0.0f, 100.0f),       (0.0f, 100.0f, 0.0f)) },
        { "Blue2", ((0.0f, 58.0f, 42.0f),       (0.0f, 66.0f, 40.0f)) },
        { "Blue3", ((0.0f, 54.0f, 100.0f),      (46.0f, 49.0f, 0.0f)) },
        { "White1", ((56.0f, 67.0f, 81.0f),     (40.0f, 65.0f, 66.0f)) },
        { "White2", ((56.0f, 100.0f, 80.0f),    (71.0f, 0.0f, 100.0f)) },
        { "White3", ((68.0f, 50.0f, 50.0f),     (71.0f, 0.0f, 40.0f)) },
        { "Test1", ((100.0f, 0.0f, 0.0f),       (0.0f, 50.0f, 50.0f)) },
        { "Test2", ((0.0f, 100.0f, 0.0f),       (50.0f, 0.0f, 50.0f)) },
        { "Test3", ((0.0f, 0.0f, 100.0f),       (50.0f, 50.0f, 0.0f)) }

    };

    public void SetPreferredColor(string color, float transitionTimeSec = 2.0f, bool exponentialCurve = true)
    {
        if (color != "Red" && color != "Blue" && color != "White" && color != "Dark" && color != "BreathOnly" && color != "Test")
        {
            Debug.LogError("Color type " + color + " not recognized, please use Red, Blue, White, Dark, BreathOnly or Test");
            return;
        }
        else
        {
            preferredColor = color;
            Debug.Log("Preferred color set to: " + color);
            NextPreferredColorWorld(transitionTimeSec, exponentialCurve);
        }
    }
    public Action Action_SetPreferredColorWorld(string color, float transitionTimeSec = 2.0f, bool exponentialCurve = true)
    {
        return () => SetPreferredColor(color, transitionTimeSec, exponentialCurve);
    }

    public void NextPreferredColorWorld(float transitionTimeSec = 2.0f, bool exponentialCurve = true)
    {
        SetColorWorldByType(preferredColor, transitionTimeSec, exponentialCurve);
    }

    public void SetColorWorldByType(string colorType, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        switch (colorType)
        {
            case "Red":
                CycleColor(ref cycleRed, "Red", colorType, transitionTimeSec, exponentialCurve);
                worldShuffler.SetCurrentColorWorld("Red");
                break;
            case "Blue":
                CycleColor(ref cycleBlue, "Blue", colorType, transitionTimeSec, exponentialCurve);
                worldShuffler.SetCurrentColorWorld("Blue");
                break;
            case "White":
                CycleColor(ref cycleWhite, "White", colorType, transitionTimeSec, exponentialCurve);
                worldShuffler.SetCurrentColorWorld("White");
                break;
            case "Test":
                CycleColor(ref cycleTest, "Test", colorType, transitionTimeSec, exponentialCurve);
                worldShuffler.ClearCurrentColorWorld();
                break;
            case "BreathOnly":
                SetColorWorldByName("BreathOnly", transitionTimeSec);
                worldShuffler.ClearCurrentColorWorld();
                break;
            case "Dark":
                SetColorWorldByName("Dark", transitionTimeSec);
                if(worldShuffler == null)
                {
                    Debug.LogError("worldShuffler is null! Cannot clear current color world.");
                }
                else
                {
                    worldShuffler.ClearCurrentColorWorld();
                }
                break;
        }

        if (colorType != "Red" && colorType != "Blue" && colorType != "White" && colorType != "Dark" && colorType != "BreathOnly" && colorType != "Test")
        {
            Debug.LogError("Color type not recognized");
        }
    }
    private void CycleColor(ref int cycleCount, string colorBaseName, string colorType, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        if (currentColorType == colorType)
        {
            cycleCount++;
            switch (cycleCount % 3)
            {
                case 1:
                    SetColorWorldByName(colorBaseName + "1", transitionTimeSec, exponentialCurve);
                    break;
                case 2:
                    SetColorWorldByName(colorBaseName + "2", transitionTimeSec, exponentialCurve);
                    break;
                default:
                    SetColorWorldByName(colorBaseName + "3", transitionTimeSec, exponentialCurve);
                    break;
            }
        }
        else
        {
            switch (cycleCount % 3)
            {
                case 1:
                    SetColorWorldByName(colorBaseName + "3", transitionTimeSec, exponentialCurve);
                    break;
                case 2:
                    SetColorWorldByName(colorBaseName + "1", transitionTimeSec, exponentialCurve);
                    break;
                default:
                    SetColorWorldByName(colorBaseName + "2", transitionTimeSec, exponentialCurve);
                    break;
            }
        }
    }

    void SetColorWorldByName(string colorName, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        if (colorPresets.TryGetValue(colorName, out var colors))
        {
            SetColorWorldByNumbers(colorName, colors.strobeColor, colors.waveColor, transitionTimeSec, exponentialCurve);
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
    //Red1, Red2, Red3, Blue1, Blue2, Blue3, White1, White2, White3, Dark

    private void SetColorWorldByNumbers(string colorName, (float, float, float) strobeColor, (float, float, float) waveColor, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        int transitionTimeMS = (int)(transitionTimeSec * 1000);
        float _v = _brightness;

     
        //a variable that is the color name without the number
        if (colorName.Length > 0 && char.IsDigit(colorName[colorName.Length - 1]))
        {
            currentColorType = colorName.Substring(0, colorName.Length - 1);
        }
        else
        {
            currentColorType = colorName;
        }
        
        SetWaveColor(1, strobeColor.Item1*_v, strobeColor.Item2*_v, strobeColor.Item3*_v, transitionTimeMS, exponentialCurve);
        SetWaveColor(2, strobeColor.Item1*_v, strobeColor.Item2*_v, strobeColor.Item3*_v, transitionTimeMS, exponentialCurve);
        SetWaveColor(3, waveColor.Item1*_v, waveColor.Item2*_v, waveColor.Item3*_v, transitionTimeMS, exponentialCurve);

        AVSColorCommand = $"Transition to {colorName} over {transitionTimeSec} s";
        Debug.Log($"Transition to {colorName} over {transitionTimeSec} s with new currentColorType of {currentColorType}");
    }

    void SetWaveColor(int wave, float _red, float _green, float _blue, int transitionTimeMS, bool exponentialCurve = false)
    {
        
        //store the current color for the wave
        Color startColor;
        if (wave == 1 || wave == 2)
        {
            startColor = currentStrobeColor;
            currentStrobeColor = new Color(_red, _green, _blue);
        }
        else
        {
            startColor = currentWaveColor;
            currentWaveColor = new Color(_red, _green, _blue);
        }

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

        if(!exponentialCurve)
        {
            AkSoundEngine.SetRTPCValue(stringRed, _red, gameObjectSystem2Listener, transitionTimeMS);
            AkSoundEngine.SetRTPCValue(stringGreen, _green, gameObjectSystem2Listener, transitionTimeMS);
            AkSoundEngine.SetRTPCValue(stringBlue, _blue, gameObjectSystem2Listener, transitionTimeMS);
        }
        else
        {   
            // Determine the curve type for each color based on the start and end values
            
            AkCurveInterpolation curveDown = AkCurveInterpolation.AkCurveInterpolation_Exp3;
            AkCurveInterpolation curveUp = AkCurveInterpolation.AkCurveInterpolation_Log3;

            AkCurveInterpolation curveR = startColor.r < _red ? curveUp : curveDown;
            AkCurveInterpolation curveG = startColor.g < _green ? curveUp : curveDown;
            AkCurveInterpolation curveB = startColor.b < _blue ? curveUp : curveDown;

            //then set the values
            AkSoundEngine.SetRTPCValue(stringRed, _red, gameObjectSystem2Listener, transitionTimeMS, curveR);
            AkSoundEngine.SetRTPCValue(stringGreen, _green, gameObjectSystem2Listener, transitionTimeMS, curveG);
            AkSoundEngine.SetRTPCValue(stringBlue, _blue, gameObjectSystem2Listener, transitionTimeMS, curveB);
        }
        
        if(_red >0 || _green > 0 || _blue > 0)
        {
            playReference = true;
            playReferenceFrame = true;
        }
    }
    
    IEnumerator GoDark(int milliseconds = 1000)
    {
        float timeRemaining = (milliseconds) / 1000f;
        if(playReferenceFrame)
        {
            yield break;
        }
        Debug.Log("Going Dark - " + timeRemaining);
        yield return null;
        while(timeRemaining > 0)
        {
            if(playReferenceFrame)
            {
                yield break;
            }
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
        if(playReferenceFrame)
        {
            yield break;
        }
        else
        {
            playReference = false;
        }
    }
    // DYNAMIC AVS CONTROL SYSTEMS
    /// <param name="doBilateral"><see langword="true"/> = mono (bilateral), <see langword="false"/> = stereo; <see langword="null"/> = toggle from current.</param>
    public void Strobe_MonoStereo(bool? doBilateral = null)
    {
        if (doBilateral == null)
        {
            Strobe_MonoStereo(!bilateral);
            return;
        }

        bool doBil = doBilateral.Value;
        //AkSoundEngine.PostEvent("Stop_AVS_Wave1", gameObject);
        if (doBil == bilateral)
        {
            Debug.Log("AVS: Bilateral switch command changed to " + doBil + ", but no change in state. Ignoring.");
        }
        else
        {
            AkSoundEngine.StopPlayingID(wave1ID);
            //yield return new WaitForSeconds(1f);
            if (doBil)
            {
                AkSoundEngine.SetRTPCValue("AVS_Modulation_MonoStereo_Wave1", 1.0f, gameObjectSystem2Listener);
                Debug.Log("AVS: Switching to Stereo");
                bilateral = true;
            }
            else
            {
                AkSoundEngine.SetRTPCValue("AVS_Modulation_MonoStereo_Wave1", 0.0f, gameObjectSystem2Listener);
                Debug.Log("AVS: Switching to Mono");
                bilateral = false;
            }
            wave1ID = AkSoundEngine.PostEvent("Play_AVS_Wave1", gameObjectSystem2Listener);
        }
    }
    public void Wwise_Strobe_ToneDisplay (float _input)
    {
        float _input2 = _input;
        float _m2 = Mathf.Max(Mathf.Min(_gammaBurstMode, 1.0f), 0.0f);
        float _m1 = 1.0f - _m2;
        float _i = Mathf.Max(Mathf.Min(_input2, 1.0f), 0.0f);
        float _strobe1Depth       = (44.0f + 56.0f * _i);
        float _strobe1            = (45.0f + (((_m1*55.0f)-(_m2*45.0f)) * _i)); //gamma bursts make this go down, otherwise up 
        float _strobe2            = _m2 * ((70.0f * _i) + (30.0f * Mathf.Min((_i * 2), 1)));//there's a little boost at the bottom end because that works best with the glasses.

        if (toneVisualizationFlag)
        {
            Debug.LogWarning("Warning: AVS Tone Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
        }
        toneVisualizationFlag    = true;

        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", _strobe1Depth, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave1", _strobe1, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave2", _strobe2, gameObjectSystem2Listener);

    }

    public void Wwise_Strobe_ChargeDisplay (float _input) //RENAME THIS TO JUST BE WWISE_CHARGE
    {
        float _input2 = _input;
       
        float _i = Mathf.Max(Mathf.Min(_input2, 1.0f), 0.0f);
        float _strobe1Smoothing = 100.0f - _i*100.0f;
        float _strobePWM = 25.0f + 50.0f * _i;

        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave1", _strobePWM, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave2", _strobePWM, gameObjectSystem2Listener);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Smoothing_Wave1", _strobe1Smoothing, gameObjectSystem2Listener);


        if (chargeVisualizationFlag)
        {
            Debug.LogWarning("Warning: AVS Charge Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
        }
        chargeVisualizationFlag = true;
    }

    public void Wwise_BreathDisplay (float _waveValue)
    {
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave3", _waveValue, gameObjectSystem2Listener);

        if (_waveValue != 0.0f)

            if (breathVisualizationFlag)
            {
                Debug.LogWarning("Warning: AVS Breath Response (LIGHT) already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
            }
        breathVisualizationFlag = true;
    }

    public void FXWave(float _amplitude, float _dur, float _split, bool rampShape = false, bool allowDuringDark = false)
    {
        if(currentColorType == "Dark" && !allowDuringDark)
        {
            Debug.LogWarning("FXWave command ignored because the current color world is Dark");
            return;
        }
        else
        StartCoroutine(FXWaveCoroutine(_amplitude, _dur, _split, rampShape));
    }

    private IEnumerator FXWaveCoroutine(float _amplitude = 0.3f, float _dur = 4.0f, float _split = 0.25f, bool rampShape = false)
    {
        float _t = 0f;
        float _fx = 0.0f;
        int key = fxWaveKey++;
        float exponent = rampShape ? 0.5f : 1.0f;
        bool setDarkAtEnd = false;
        float _delay = 0.0f;

        _fxWaveDict.Add(key, 0.0f);

        if(currentColorType == "Dark")
        {
            SetColorWorldByName("BreathOnly", 0.1f);
            setDarkAtEnd = true;
            _delay = 0.25f;
        }

        Debug.Log("AVS FXWave started with key: " + key);

        while (_delay > 0)
        {
            //If we started dark, we will need a moment to turn on the lights first...
            _delay -= Time.deltaTime;
            yield return null;
        }

        while (_t <= 0.5f) //ramp _fx up to 1.0f in _split time
        {
            _t += (Time.deltaTime * 0.5f / (_dur * Mathf.Clamp(_split, 0.0f, 1.0f)));

            _fx = Mathf.Pow(_t * 2.0f, exponent);
            _fxWaveDict[key] = _fx * Mathf.Clamp(_amplitude, 0.0f, 1.0f);
            yield return null;
        }
        while(_t <= 1.0f) //ramp down to 0.0f in the remaining time
        {
            _t += (Time.deltaTime * 0.5f / (_dur * Mathf.Clamp(1 - _split, 0.0f, 1.0f)));
            _fx = Mathf.Pow(Mathf.Clamp(2.0f - _t * 2.0f, 0f, 1f), exponent);
            _fxWaveDict[key] = _fx * Mathf.Clamp(_amplitude, 0.0f, 1.0f);
            yield return null;
        }

        if(setDarkAtEnd && currentColorType == "BreathOnly")
        {
            SetColorWorldByName("Dark", 0.1f);
        }

        _fxWaveDict.Remove(key);
    }

    public void Gamma(bool gammaOn)
    {
        if(gammaOn)
        {
            if(gammaCoroutine != null)
            {
                StopCoroutine(gammaCoroutine);
            }
            gammaCoroutine = StartCoroutine(GammaCoroutine(1.0f, 2.0f));
        }
        else
        {
            if(gammaCoroutine != null)
            {
                StopCoroutine(gammaCoroutine);
            }
            gammaCoroutine = StartCoroutine(GammaCoroutine(0.0f, 2.0f));
        }
    }

    private IEnumerator GammaCoroutine(float _target, float _duration = 2.0f)
    {
        float _difference = _target - _gammaBurstMode;
        bool _up = _difference > 0;
        float _rate = _difference / _duration;

        //move to the target in the given time
        while(_gammaBurstMode != _target)
        {
            _gammaBurstMode += _rate * Time.deltaTime;
            if((_up && _gammaBurstMode > _target) || (!_up && _gammaBurstMode < _target))
            {
                _gammaBurstMode = _target;
            }
            yield return null;   
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

    public void LightSettingsInitialization(float transitionTimeSec = 0.0f)
    {
        SetPreferredColor("Dark", transitionTimeSec);
        SetStrobeRate(0f, transitionTimeSec);
    }

    private void OnColorWorldDropdownChanged(int index)
    {
        if(ColorWorldDropdownChange != null)
        {
            switch (index)
            {
                case 0: SetColorWorldByName("Dark"); break;
                case 1: SetColorWorldByName("Red1");  break;
                case 2: SetColorWorldByName("Red2");   break;
                case 3: SetColorWorldByName("Red3"); break;
                case 4: SetColorWorldByName("Blue1");  break;
                case 5: SetColorWorldByName("Blue2");  break;
                case 6: SetColorWorldByName("Blue3"); break;
                case 7: SetColorWorldByName("White1"); break;
                case 8: SetColorWorldByName("White2");  break;
                case 9: SetColorWorldByName("White3");  break;
                default: SetColorWorldByName("Dark");  break;
            }
        }
    }



}
