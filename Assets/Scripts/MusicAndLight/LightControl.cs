using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using AK.Wwise;
using System;
using TMPro;

/// <summary>Logical color world for <see cref="LightControl.SetPreferredColor"/> / cycling (not numbered presets like Red1).</summary>
public enum PreferredColorWorld
{
    Red,
    Blue,
    White,
    Calibration,
    Dark,
    BreathOnly,
    Test
}

public class LightControl : MonoBehaviour
{
    public static LightControl instance { get; private set; }

    // Wwise "System" shareset device names for the auxiliary lights / vibroacoustic outputs.
    // These are the exact deviceName strings as enumerated by AkSoundEngine.GetDeviceList on each
    // platform — referenced both by LightControl.Start (to AddOutput the auxiliary listener) and by
    // DeviceDisconnectMonitor (to detect mid-session unplugs). Keep the values byte-identical to
    // whatever Wwise reports, including any trailing spaces — those are part of the OS-reported name.
#if UNITY_STANDALONE_OSX
    public const string KasinaWwiseDeviceName = "Kasina MMS Audio";
    public const string LiminaWwiseDeviceName = "MPL Audio       ";
#elif UNITY_STANDALONE_WIN
    public const string KasinaWwiseDeviceName = "Speakers (Kasina MMS Audio)";
    public const string LiminaWwiseDeviceName = "Speakers (MPL Audio       )";
#else
    public const string KasinaWwiseDeviceName = "";
    public const string LiminaWwiseDeviceName = "";
#endif

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
    [FormerlySerializedAs("currentColorType")]
    public PreferredColorWorld currentColorWorld = PreferredColorWorld.Dark;
    [FormerlySerializedAs("preferredColor")]
    public PreferredColorWorld preferredColor = PreferredColorWorld.Dark;

    private bool _calibrationColorWorldStageActive;
    private static bool _warnedCalibrationColorOutsideStage;

    [Header("Current color world (Play Mode)")]
    [ColorUsage(false, false)]
    [Tooltip("Strobe (waves 1–2): 0–1 per channel after _brightness. Runtime mirror — SetWaveColor converts to Wwise 0–100.")]
    [FormerlySerializedAs("toneWaveColor")]
    public UnityEngine.Color currentStrobeColor = UnityEngine.Color.black;
    [ColorUsage(false, false)]
    [Tooltip("Wave 3: 0–1 per channel after _brightness. Runtime mirror — SetWaveColor converts to Wwise 0–100.")]
    [FormerlySerializedAs("breathWaveColor")]
    public UnityEngine.Color currentWaveColor = UnityEngine.Color.black;

    [SerializeField]
    [Tooltip("Global multiplier on colorPresets RGB when applying a color world. Shown read-only in the inspector for now.")]
    private float _brightness = 1.0f;
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
        // Names are defined as platform-conditional constants on this class so DeviceDisconnectMonitor
        // can reference the same exact deviceName strings without duplication.
        string wantedDevice1 = KasinaWwiseDeviceName;
        string wantedDevice2 = LiminaWwiseDeviceName;

#if !UNITY_STANDALONE_OSX && !UNITY_STANDALONE_WIN
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
            // Hard launch-time precondition: at least one of Kasina / Limina must be plugged in.
            // Without it, the AVS auxiliary listener (lights + vibroacoustic output path) cannot be
            // initialized this session — escalated from Debug.Log to LogError so missing hardware is
            // obvious in the console at startup. DeviceDisconnectMonitor handles mid-session unplugs;
            // this log is specifically the "nothing was ever plugged in at launch" case.
            Debug.LogError(
                $"LightControl: Neither Kasina ('{KasinaWwiseDeviceName}') nor Limina ('{LiminaWwiseDeviceName}') " +
                "was found among active Wwise System devices at launch. The AVS auxiliary output path " +
                "(lights / vibroacoustic) will NOT be initialized this session — plug in a Kasina or Limina " +
                "before launch to enable it.");
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
            SetPreferredColor(PreferredColorWorld.Red, 5.0f);
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
    // colorPresets: per-channel 0–1 (not Wwise RTPC units). SetColorWorldByNumbers applies _brightness; SetWaveColor → RTPC 0–100.
    private const float AvsVolumeRtpcMax = 100f;

    private static float AvsVolumeToRtpc(float normalized01) =>
        Mathf.Clamp01(normalized01) * AvsVolumeRtpcMax;

    //   Name        Strobe Color (0–1)          Wave Color (0–1)
    private static readonly Dictionary<string, ((float, float, float) strobeColor, (float, float, float) waveColor)> colorPresets = new Dictionary<string, ((float, float, float), (float, float, float))>
    {
        { "Dark", ((0.0f, 0.0f, 0.0f),          (0.0f, 0.0f, 0.0f)) },
        { "BreathOnly", ((0.0f, 0.0f, 0.0f),    (1.0f, 0.0f, 0.0f)) },
        { "Red1", ((1.0f, 0.0f, 0.0f),          (0.72f, 1.0f, 1.0f)) },
        { "Red2", ((1.0f, 0.0f, 1.0f),          (0.68f, 1.0f, 0.0f)) },
        { "Red3", ((1.0f, 1.0f, 1.0f),          (0.68f, 1.0f, 0.0f)) },
        { "Blue1", ((0.0f, 0.0f, 1.0f),         (0.0f, 1.0f, 0.0f)) },
        { "Blue2", ((0.0f, 0.58f, 0.42f),      (0.0f, 0.44f, 0.30f)) },
        { "Blue3", ((0.0f, 0.54f, 1.0f),       (0.46f, 0.49f, 0.0f)) },
        // White1: +25% RGB vs (0.56,0.67,0.81)/(0.4,0.65,0.66) pre-boost baseline
        { "White1", ((0.70f, 0.8375f, 1.0f),   (0.50f, 0.8125f, 0.825f)) },
        { "White2", ((0.56f, 1.0f, 0.80f),     (0.71f, 0.0f, 1.0f)) },
        // Calibration: matches White2; calibration stage only (see SetCalibrationColorWorldStageActive).
        { "Calibration", ((0.56f, 1.0f, 0.80f), (0.71f, 0.0f, 1.0f)) },
        // White3: +25% RGB vs (0.68,0.5,0.5)/(0.71,0,0.4) pre-boost baseline
        { "White3", ((0.85f, 0.625f, 0.625f),   (0.8875f, 0.0f, 0.50f)) },
        { "Test1", ((1.0f, 0.0f, 0.0f),        (0.0f, 0.50f, 0.50f)) },
        { "Test2", ((0.0f, 1.0f, 0.0f),        (0.50f, 0.0f, 0.50f)) },
        { "Test3", ((0.0f, 0.0f, 1.0f),        (0.50f, 0.50f, 0.0f)) }

    };

    // Frozen AVS RTPCs while currentColorWorld is Calibration (_input = 0, _gammaBurstMode = 0). See PLAYTEST_NOTES Block 2.
    private const float CalibrationFrozenStrobeDepthW1 = 44f;
    private const float CalibrationFrozenStrobeMasterW1 = 45f;
    private const float CalibrationFrozenStrobeMasterW2 = 0f;
    private const float CalibrationFrozenChargePwm = 25f;
    private const float CalibrationFrozenChargeSmoothingW1 = 100f;
    private const float CalibrationFrozenBreathMasterW3 = 0f;

    private bool IsCalibrationColorWorld => currentColorWorld == PreferredColorWorld.Calibration;

    /// <summary>FXWave add-on for breath lights; zero during Calibration color world.</summary>
    public float GetBreathFxWaveAddOn() => IsCalibrationColorWorld ? 0f : _fxWave;

    /// <summary>True while calibration stage owns the Calibration color world (lights glasses step).</summary>
    public void SetCalibrationColorWorldStageActive(bool stageActive)
    {
        _calibrationColorWorldStageActive = stageActive;
    }

    private void WarnIfCalibrationColorOutsideStage()
    {
        if (_calibrationColorWorldStageActive || _warnedCalibrationColorOutsideStage)
            return;

        _warnedCalibrationColorOutsideStage = true;
        Debug.LogWarning(
            "LightControl: Calibration color world was selected outside the calibration stage. " +
            "Only CalibrationStageHandler should use SetPreferredColor(PreferredColorWorld.Calibration).");
    }

    public void SetPreferredColor(PreferredColorWorld color, float transitionTimeSec = 2.0f, bool exponentialCurve = true)
    {
        if (color == PreferredColorWorld.Calibration)
            WarnIfCalibrationColorOutsideStage();

        preferredColor = color;
        Debug.Log("Preferred color set to: " + color);
        NextPreferredColorWorld(transitionTimeSec, exponentialCurve);
    }

    public Action Action_SetPreferredColorWorld(PreferredColorWorld color, float transitionTimeSec = 2.0f, bool exponentialCurve = true)
    {
        return () => SetPreferredColor(color, transitionTimeSec, exponentialCurve);
    }

    public void NextPreferredColorWorld(float transitionTimeSec = 2.0f, bool exponentialCurve = true)
    {
        SetColorWorldByType(preferredColor, transitionTimeSec, exponentialCurve);
    }

    public void SetColorWorldByType(PreferredColorWorld colorType, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        switch (colorType)
        {
            case PreferredColorWorld.Red:
                CycleColor(ref cycleRed, colorType, transitionTimeSec, exponentialCurve);
                worldShuffler.SetCurrentColorWorld(PreferredColorWorld.Red);
                break;
            case PreferredColorWorld.Blue:
                CycleColor(ref cycleBlue, colorType, transitionTimeSec, exponentialCurve);
                worldShuffler.SetCurrentColorWorld(PreferredColorWorld.Blue);
                break;
            case PreferredColorWorld.White:
                CycleColor(ref cycleWhite, colorType, transitionTimeSec, exponentialCurve);
                worldShuffler.SetCurrentColorWorld(PreferredColorWorld.White);
                break;
            case PreferredColorWorld.Test:
                CycleColor(ref cycleTest, colorType, transitionTimeSec, exponentialCurve);
                worldShuffler.ClearCurrentColorWorld();
                break;
            case PreferredColorWorld.BreathOnly:
                SetColorWorldByName(PreferredColorWorld.BreathOnly, transitionTimeSec);
                worldShuffler.ClearCurrentColorWorld();
                break;
            case PreferredColorWorld.Dark:
                SetColorWorldByName(PreferredColorWorld.Dark, transitionTimeSec);
                if(worldShuffler == null)
                {
                    Debug.LogError("worldShuffler is null! Cannot clear current color world.");
                }
                else
                {
                    worldShuffler.ClearCurrentColorWorld();
                }
                break;
            case PreferredColorWorld.Calibration:
                SetColorWorldByName(PreferredColorWorld.Calibration, transitionTimeSec, exponentialCurve);
                if (worldShuffler != null)
                    worldShuffler.ClearCurrentColorWorld();
                else
                    Debug.LogError("worldShuffler is null! Cannot clear current color world.");
                break;
        }
    }

    private static string PresetKey(PreferredColorWorld world) => world.ToString();

    private static PreferredColorWorld PreferredColorWorldFromPresetName(string colorName)
    {
        string baseName = colorName;
        if (colorName.Length > 0 && char.IsDigit(colorName[colorName.Length - 1]))
            baseName = colorName.Substring(0, colorName.Length - 1);

        if (Enum.TryParse(baseName, out PreferredColorWorld world))
            return world;

        Debug.LogWarning("LightControl: preset name '" + colorName + "' did not map to a PreferredColorWorld; defaulting to Dark.");
        return PreferredColorWorld.Dark;
    }

    private void CycleColor(ref int cycleCount, PreferredColorWorld colorType, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        string colorBaseName = PresetKey(colorType);
        if (currentColorWorld == colorType)
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

    void SetColorWorldByName(PreferredColorWorld colorWorld, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        SetColorWorldByName(PresetKey(colorWorld), transitionTimeSec, exponentialCurve);
    }

    void SetColorWorldByName(string colorName, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        if (colorName == PresetKey(PreferredColorWorld.Calibration))
            WarnIfCalibrationColorOutsideStage();

        if (colorPresets.TryGetValue(colorName, out var colors))
        {
            SetColorWorldByNumbers(colorName, colors.strobeColor, colors.waveColor, transitionTimeSec, exponentialCurve);
            if (colorName == PresetKey(PreferredColorWorld.Dark))
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
        currentColorWorld = PreferredColorWorldFromPresetName(colorName);
        
        SetWaveColor(1, strobeColor.Item1*_v, strobeColor.Item2*_v, strobeColor.Item3*_v, transitionTimeMS, exponentialCurve);
        SetWaveColor(2, strobeColor.Item1*_v, strobeColor.Item2*_v, strobeColor.Item3*_v, transitionTimeMS, exponentialCurve);
        SetWaveColor(3, waveColor.Item1*_v, waveColor.Item2*_v, waveColor.Item3*_v, transitionTimeMS, exponentialCurve);

        AVSColorCommand = $"Transition to {colorName} over {transitionTimeSec} s";
        Debug.Log($"Transition to {colorName} over {transitionTimeSec} s with new currentColorWorld of {currentColorWorld}");
    }

    void SetWaveColor(int wave, float normalizedRed, float normalizedGreen, float normalizedBlue, int transitionTimeMS, bool exponentialCurve = false)
    {
        normalizedRed = Mathf.Clamp01(normalizedRed);
        normalizedGreen = Mathf.Clamp01(normalizedGreen);
        normalizedBlue = Mathf.Clamp01(normalizedBlue);

        UnityEngine.Color startColor;
        if (wave == 1 || wave == 2)
        {
            startColor = currentStrobeColor;
            currentStrobeColor = new UnityEngine.Color(normalizedRed, normalizedGreen, normalizedBlue);
        }
        else
        {
            startColor = currentWaveColor;
            currentWaveColor = new UnityEngine.Color(normalizedRed, normalizedGreen, normalizedBlue);
        }

        if (wave < 1 || wave > 3)
        {
            Debug.LogError("AVS wave must be between 1 and 3");
            return;
        }

        float rtpcRed = AvsVolumeToRtpc(normalizedRed);
        float rtpcGreen = AvsVolumeToRtpc(normalizedGreen);
        float rtpcBlue = AvsVolumeToRtpc(normalizedBlue);
        float rtpcStartR = AvsVolumeToRtpc(startColor.r);
        float rtpcStartG = AvsVolumeToRtpc(startColor.g);
        float rtpcStartB = AvsVolumeToRtpc(startColor.b);

        string stringRed = "AVS_Red_Volume_Wave" + wave;
        string stringGreen = "AVS_Green_Volume_Wave" + wave;
        string stringBlue = "AVS_Blue_Volume_Wave" + wave;

        if(!exponentialCurve)
        {
            AkSoundEngine.SetRTPCValue(stringRed, rtpcRed, gameObjectSystem2Listener, transitionTimeMS);
            AkSoundEngine.SetRTPCValue(stringGreen, rtpcGreen, gameObjectSystem2Listener, transitionTimeMS);
            AkSoundEngine.SetRTPCValue(stringBlue, rtpcBlue, gameObjectSystem2Listener, transitionTimeMS);
        }
        else
        {
            AkCurveInterpolation curveDown = AkCurveInterpolation.AkCurveInterpolation_Exp3;
            AkCurveInterpolation curveUp = AkCurveInterpolation.AkCurveInterpolation_Log3;

            AkCurveInterpolation curveR = rtpcStartR < rtpcRed ? curveUp : curveDown;
            AkCurveInterpolation curveG = rtpcStartG < rtpcGreen ? curveUp : curveDown;
            AkCurveInterpolation curveB = rtpcStartB < rtpcBlue ? curveUp : curveDown;

            AkSoundEngine.SetRTPCValue(stringRed, rtpcRed, gameObjectSystem2Listener, transitionTimeMS, curveR);
            AkSoundEngine.SetRTPCValue(stringGreen, rtpcGreen, gameObjectSystem2Listener, transitionTimeMS, curveG);
            AkSoundEngine.SetRTPCValue(stringBlue, rtpcBlue, gameObjectSystem2Listener, transitionTimeMS, curveB);
        }
        
        if(normalizedRed > 0f || normalizedGreen > 0f || normalizedBlue > 0f)
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
        if (IsCalibrationColorWorld)
        {
            if (toneVisualizationFlag)
            {
                Debug.LogWarning("Warning: AVS Tone Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
            }
            toneVisualizationFlag = true;
            AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", CalibrationFrozenStrobeDepthW1, gameObjectSystem2Listener);
            AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave1", CalibrationFrozenStrobeMasterW1, gameObjectSystem2Listener);
            AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave2", CalibrationFrozenStrobeMasterW2, gameObjectSystem2Listener);
            return;
        }

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
        if (IsCalibrationColorWorld)
        {
            AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave1", CalibrationFrozenChargePwm, gameObjectSystem2Listener);
            AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave2", CalibrationFrozenChargePwm, gameObjectSystem2Listener);
            AkSoundEngine.SetRTPCValue("AVS_Modulation_Smoothing_Wave1", CalibrationFrozenChargeSmoothingW1, gameObjectSystem2Listener);
            if (chargeVisualizationFlag)
            {
                Debug.LogWarning("Warning: AVS Charge Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
            }
            chargeVisualizationFlag = true;
            return;
        }

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
        if (IsCalibrationColorWorld)
        {
            AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave3", CalibrationFrozenBreathMasterW3, gameObjectSystem2Listener);
            breathVisualizationFlag = true;
            return;
        }

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
        if (IsCalibrationColorWorld)
            return;

        if(currentColorWorld == PreferredColorWorld.Dark && !allowDuringDark)
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

        if(currentColorWorld == PreferredColorWorld.Dark)
        {
            SetColorWorldByName(PreferredColorWorld.BreathOnly, 0.1f);
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

        if(setDarkAtEnd && currentColorWorld == PreferredColorWorld.BreathOnly)
        {
            SetColorWorldByName(PreferredColorWorld.Dark, 0.1f);
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
        SetPreferredColor(PreferredColorWorld.Dark, transitionTimeSec);
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
