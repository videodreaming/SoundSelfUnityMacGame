using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using AK.Wwise;
using System;

public class LightControl : MonoBehaviour
{
     public DevelopmentMode developmentMode;
    [SerializeField] AkDeviceDescriptionArray m_devices;
    private bool playReference = false;
    private bool playReferenceLastFrame = false;
    private bool playReferenceFrame = false;
    public bool bilateral {get; private set;} = false; 
    public string AVSColorCommand  = "";
    public string AVSStrobeCommand = "";
    public string currentColorType = "Dark";
    public string preferredColor = "Red";
    private float  _brightness = 0.6f;
    private int cycleRed = 0;
    private int cycleBlue = 0;
    private int cycleWhite = 0;
    private int cycleTest = 0;
    public float _fxWave = 0f;
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
    uint wave1ID;
    public uint rtpcID;
    public float frequencyWave1Value;
    private Coroutine sawStrobeCoroutine;
    private Coroutine gammaCoroutine;
    public Color toneWaveColor = new Color(0.0f, 0.0f, 0.0f);
    public Color breathWaveColor = new Color(0.0f, 0.0f, 0.0f);
    public int fxWaveKey = 0;
    public Dictionary <int, float> _fxWaveDict = new Dictionary<int, float>();
    

    void Awake()
    {
        //INITIALIZE COLORS
        if(developmentMode.configureMode)
        {
            SetColorWorldByType("White", 0.0f);
            SetStrobeRate(8f, 0.0f);
        }
        else if(developmentMode.startAtStart)
        {
            SetColorWorldByType("Dark", 0.0f);
        }
        //ulong gameObjectID = AkSoundEngine.GetAkGameObjectID(gameObject);
        // Log the Game Object ID for tracking purposes
        //Debug.Log($"Game Object '{gameObject.name}' registered with Wwise ID: {gameObjectID}");
    }
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
                    print("Device found: " + devices[i].deviceName + " With ID: " + devices[i].idDevice);
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
        print("OutputSettings2 ID Device " + outputSettings2.idDevice);
        
        // We call the AddOutput with the newly create OutputSetting2 for the System_01 and for the system2Listener.
        ulong outDeviceId = 0;
        ulong[] ListenerIds = { AkSoundEngine.GetAkGameObjectID(gameObject) };
        AkSoundEngine.AddOutput(outputSettings2, out outDeviceId, ListenerIds, 1);
        
        // We Set the listener of Game_Object_System2 to be listened by system2Listener. Set will clear all Emitter-Listener already there, 
        // so the default listener will not be associated anymore.
        AkSoundEngine.RegisterGameObj(gameObject, "System2Go");
        AkSoundEngine.SetListeners(AkSoundEngine.GetAkGameObjectID(gameObject), ListenerIds, 1);
        print("GameObjectID : " + AkSoundEngine.GetAkGameObjectID(gameObject));

        //Play all appropriate AVS waves
        wave1ID = AkSoundEngine.PostEvent("Play_AVS_Wave1", gameObject);
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
        if(developmentMode.developmentMode)
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
                preferredColor = "Test";
                NextPreferredColorWorld(3f, true);
            }
            else if (Input.GetKeyUp(KeyCode.C))
            {
                NextPreferredColorWorld(7f, true);
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                Strobe_MonoStereo(true);
            }
            else if (Input.GetKeyUp(KeyCode.V))
            {
                Strobe_MonoStereo(false);
            }

            if(Input.GetKeyDown(KeyCode.W))
            {
                SetStrobeRate(15f, 5f);
            }
            if(Input.GetKeyUp(KeyCode.W))
            {
                SetStrobeRate(5f, 0f);
            }
            if(Input.GetKeyDown(KeyCode.R))
            {
                Gamma(true);
            }
            if(Input.GetKeyUp(KeyCode.R))
            {
                Gamma(false);
            }
        }
        
        // Start and Stop the Reference Signal, depending on the AVS color world (dark turns off)
        if (playReference && !playReferenceLastFrame)
        {
            AkSoundEngine.PostEvent("Play_AVS_SineGenerators_REFERENCE", gameObject);
            Debug.Log("AVS: Starting Reference Signal");
        }
        else if (!playReference && playReferenceLastFrame)
        {
            AkSoundEngine.PostEvent("Stop_AVS_SineGenerators_REFERENCE", gameObject);
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

    private IEnumerator SawStrobeCoroutine(float _start, float _end, float cycleLength)
    {
        yield return null; //wait one frame to ensure that AVSStrobeCommand can update.
        float _timeRemaining = cycleLength;
        
        SetStrobeRate(_end, cycleLength/2f, true);

        while(_timeRemaining > (cycleLength/2f))
        {
            _timeRemaining -= Time.deltaTime;
            yield return null;
        }

        SetStrobeRate(_start, cycleLength/2f, true);

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

        if (AVSStrobeCommand != "")
        {
            Debug.LogWarning("Warning: Strobe Command already made this frame, proceeding with new rate of " + _rate + " Hz, but this should really only happen once per frame");
        }

        int transitionTimeMS = (int)(transitionTimeSec * 1000);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Frequency_Wave1", _rate, gameObject, transitionTimeMS);
        if(transitionTimeMS == 0)
        {
            Debug.Log("Strobe Rate set to: " + _rate + " Hz immediately");
            if(!partOfCoroutine)
            AVSStrobeCommand = "Strobe Rate: " + _rate + " Hz immediately";
        }
        else
        {
            Debug.Log("Strobe Rate set to: " + _rate + " Hz over " + transitionTimeMS + " ms");
            if(!partOfCoroutine)
            AVSStrobeCommand = "Strobe Rate: " + _rate + " Hz over " + transitionTimeMS + " ms";
            //StartCoroutine(ReportStrobeTargetMet(_rate, transitionTimeSec));
        }
        
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

    public void SetPreferredColor(string color)
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
        }
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
                break;
            case "Blue":
                CycleColor(ref cycleBlue, "Blue", colorType, transitionTimeSec, exponentialCurve);
                break;
            case "White":
                CycleColor(ref cycleWhite, "White", colorType, transitionTimeSec, exponentialCurve);
                break;
            case "Test":
                CycleColor(ref cycleTest, "Test", colorType, transitionTimeSec, exponentialCurve);
                break;
            case "BreathOnly":
                SetColorWorldByName("BreathOnly", transitionTimeSec);
                break;
            case "Dark":
                SetColorWorldByName("Dark", transitionTimeSec);
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

    private void SetColorWorldByNumbers(string colorName, (float, float, float) strobeColor, (float, float, float) waveColor, float transitionTimeSec = 2.0f, bool exponentialCurve = false)
    {
        int transitionTimeMS = (int)(transitionTimeSec * 1000);
        float _v = _brightness;

        //colorName is a color ("Red" "Blue" or "White") followed by a number (1, 2, or 3)
        //or is "Dark" or "BreathOnly"
        //I need a variable that is the color name without the number

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
            startColor = toneWaveColor;
            toneWaveColor = new Color(_red, _green, _blue);
        }
        else
        {
            startColor = breathWaveColor;
            breathWaveColor = new Color(_red, _green, _blue);
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
            AkSoundEngine.SetRTPCValue(stringRed, _red, gameObject, transitionTimeMS);
            AkSoundEngine.SetRTPCValue(stringGreen, _green, gameObject, transitionTimeMS);
            AkSoundEngine.SetRTPCValue(stringBlue, _blue, gameObject, transitionTimeMS);
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
            AkSoundEngine.SetRTPCValue(stringRed, _red, gameObject, transitionTimeMS, curveR);
            AkSoundEngine.SetRTPCValue(stringGreen, _green, gameObject, transitionTimeMS, curveG);
            AkSoundEngine.SetRTPCValue(stringBlue, _blue, gameObject, transitionTimeMS, curveB);
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
    public void Strobe_MonoStereo (bool doBilateral = false)
    {
        //AkSoundEngine.PostEvent("Stop_AVS_Wave1", gameObject);
        if(doBilateral == bilateral)
        {
            Debug.Log("AVS: Bilateral switch command changed to " + doBilateral + ", but no change in state. Ignoring.");
        }
        else
        {
            AkSoundEngine.StopPlayingID(wave1ID);
            //yield return new WaitForSeconds(1f);
            if(doBilateral)
            {
                AkSoundEngine.SetRTPCValue("AVS_Modulation_MonoStereo_Wave1", 1.0f, gameObject);
                Debug.Log("AVS: Switching to Mono");
                bilateral = true;
            }
            else
            {
                AkSoundEngine.SetRTPCValue("AVS_Modulation_MonoStereo_Wave1", 0.0f, gameObject);
                Debug.Log("AVS: Switching to Stereo");
                bilateral = false;
            }
            wave1ID = AkSoundEngine.PostEvent("Play_AVS_Wave1", gameObject);
        }
    }
    public void Wwise_Strobe_ToneDisplay (float _input)
    {
        float _input2 = _input;
        if(developmentMode.configureMode)
        {
            _input2 = 1.0f;
        }
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
    
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Depth_Wave1", _strobe1Depth, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave1", _strobe1, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave2", _strobe2, gameObject);
        
    }

    public void Wwise_Strobe_ChargeDisplay (float _input) //RENAME THIS TO JUST BE WWISE_CHARGE
    {
        float _input2 = _input;
        if(developmentMode.configureMode)
        {
            _input2 = 0.75f;
        }
        float _i = Mathf.Max(Mathf.Min(_input2, 1.0f), 0.0f);
        float _strobe1Smoothing = 100.0f - _i*100.0f;
        float _strobePWM = 25.0f + 50.0f * _i;

        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave1", _strobePWM, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_PWM_Wave2", _strobePWM, gameObject);
        AkSoundEngine.SetRTPCValue("AVS_Modulation_Smoothing_Wave1", _strobe1Smoothing, gameObject);
        

        if (chargeVisualizationFlag)
        {
            Debug.LogWarning("Warning: AVS Charge Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
        }
        chargeVisualizationFlag = true;
    }

    public void Wwise_BreathDisplay (float _input, float _addition = 0.0f)
    {
        float _input2 = _input;
        float _addition2 = _addition;
        if(developmentMode.configureMode)
        {
            _input2 = 0.0f;
            _addition2 = 0.0f;
        }
        float _i = Mathf.Max(Mathf.Min(_input2 + _addition2, 1.0f), 0.0f);
        float _waveValue = 0.0f + 100.0f * _i;

        AkSoundEngine.SetRTPCValue("AVS_MasterVolume_Wave3", _waveValue, gameObject);
        AkSoundEngine.SetRTPCValue("Unity_Inhale", _waveValue, gameObject);

        if (_i != 0.0f)
        //Debug.Log("Breath Wave Value: " + _waveValue);

        if(breathVisualizationFlag)
        {
            Debug.LogWarning("Warning: AVS Breath Response already set this frame. Proceeding with new configuration. But this is really only meant to happen once per frame.");
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
        StartCoroutine(FXWaveCoroutine(_amplitude, _dur, _split, true));
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



}
