using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WwiseInteractiveMusicManager : MonoBehaviour
{
    public DevelopmentMode developmentMode;
    public MusicSystem1 musicSystem1;
    public LightControl lightControl;
    public RespirationTracker respirationTracker;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public WwiseVOManager wwiseVOManager;
    public Director director;
    private int fundamentalCount = -1;
    private int harmonyCount = -1;

    public string currentSwitchState = "B";
    public string currentToningState = "None";
    public float InteractiveMusicSilentLoopsRTPC = 0.0f;
    public float HarmonySilentVolumeRTPC = 0.0f;
    public float FundamentalSilentVolumeRTPC = 0.0f;
    private float UserNotToningThreshold = 30.0f; //controls environment shift.
    public uint playingId;
    private bool toneActiveTriggered = false; // Flag to control the event triggering

    [SerializeField]
    private int currentStage = 0; // Tracks the current stage of the sound world
    public CSVWriter csvWriter;
    private bool thisTonesImpactPlayed = false;
    // AVS Controls
    private float _absorptionThreshold;
    float d = 1f; //debug timer mult

    //THESE THINGS ARE DEFINITELY PERTAINING TO THE  STORY PROGRESSION, AND SHOULD PROBABLY BE REFACTORED
    
    private bool musicProgressionFlag = false;

    private float interactiveMusicExperienceTotalTime;
    private float finalStagePreLogicTime;

    private float WakeUpCounter;
    private bool wakeUpEndSoonTriggered = false; // Flag to control the event triggering
    private float soundWorldChangeTime;
    private bool finalStagePreLogicExecuted = false; 
    public bool CFundamentalGHarmonyLock = false;
    private bool flagTriggerEnd = false;
    private bool flagThetaCoroutine = false;
    private List<int> coroutineCleanupList = new List<int>();
    private Coroutine CoroutineDynamicDropStart;
    private Coroutine CoroutineDynamicDropTheta;
    private Coroutine CoroutineDynamicDropEnd;


    void Awake()
    {
        AkSoundEngine.SetState("SoundWorldMode","SonoFlore");

        if(!developmentMode.developmentMode)
        {
            d = 1f;
        }
    }

    void Start()
    {
        WakeUpCounter = 2280.0f;
        interactiveMusicExperienceTotalTime = 1245.0f;
        soundWorldChangeTime = interactiveMusicExperienceTotalTime / 4;
        finalStagePreLogicTime = 15f; 
        _absorptionThreshold = UnityEngine.Random.Range(0.08f, 0.35f);
        
        CoroutineDynamicDropStart = StartCoroutine(AVS_Program_DynamicDrop_Start());
        //Uncomment when CSV Writer is implemented
        /*if(csvWriter.GameMode == "Preperation")
        {
            WakeupCounter = 2280.0f;
            if(csvWriter.SubGameMode == "Peace")
            {   
                interactiveMusicExpereicneTotalTime = 1245.0f;
                finalStagePreLogicTime = 15f; 
            } else if (csvWriter.SubGameMode == "Narrative")
            {
                interactiveMusicExpereicneTotalTime = 1378.0f;
            } else if (csvWriter.SubGameMode == "Surrender")
            {   
                interactiveMusicExpereicneTotalTime = 1254.0f;
            }
            soundWorldChangeTime = interactiveMusicExperienceTotalTime / 4;
        }*/
        

        AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
        //AkSoundEngine.SetState("SoundWorldMode","SonoFlore");
        //AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly","A",gameObject);
        //AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pithces_HarmonyOnly","E",gameObject);
        
        musicSystem1.fundamentalNote = 9;
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", 30.0f, gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Fundamentalonly", gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Harmonyonly", gameObject);
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Fundamentalonly", "AVS System");
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Harmonyonly", "Master Audio Bus");
    }
   
    // Update is called once per frame
    void Update()
    {

        if(developmentMode.developmentMode)
        {
            if(Input.GetKeyDown(KeyCode.L))
            {
                WakeUpCounter = 190f;
                Debug.Log("Wakeup Counter set to " + WakeUpCounter);
            }
        }

        if(imitoneVoiceIntepreter.toneActive == false)
        {
            thisTonesImpactPlayed = false;
        }
        if(WakeUpCounter > -1.0f)
        {
            WakeUpCounter -= Time.deltaTime;
        }
        if(WakeUpCounter <= 180f && !flagTriggerEnd)
        {
            CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End());
            flagTriggerEnd = true;
        }
        
        if( WakeUpCounter <= 0.0f && !wakeUpEndSoonTriggered)
        {
            AkSoundEngine.PostEvent("Play_WakeUpEndSoon_SEQUENCE", gameObject);
            WakeUpCounter = -1.0f;
            wakeUpEndSoonTriggered = true;
            lightControl.SetPreferredColor("Dark");
            lightControl.NextColorWorld(10f);
        }
        if (imitoneVoiceIntepreter.toneActiveConfident)
        {
            if (!toneActiveTriggered)
            {
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

        
        if(wwiseVOManager.interactive && !musicProgressionFlag)
        {
            musicProgressionFlag = true;
            StartCoroutine(StartMusicalProgression());
        }

        if(wwiseVOManager.interactive)
        {
            if(imitoneVoiceIntepreter.imitoneConfidentInactiveTimer > UserNotToningThreshold)
            {
                AkSoundEngine.SetState("InteractiveMusicMode", "Environment");
            }
            else
            {
                AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
            }

            if(imitoneVoiceIntepreter._tThisTone > imitoneVoiceIntepreter._activeThreshold4)
            {
                if(!thisTonesImpactPlayed)
                {
                    Debug.Log("this tone is now longer than 8s, play impact");

                    Debug.Log("impact");
                    AkSoundEngine.PostEvent("Play_sfx_Impact",gameObject);
                    thisTonesImpactPlayed = true;   
                }
            }
        }
    }

    public void PostTheToningEvents()
    {
        AkSoundEngine.PostEvent("Play_Toning_v3_FundamentalOnly",gameObject);
        AkSoundEngine.PostEvent("Play_Toning_v3_HarmonyOnly",gameObject);
    }
    
     public void userToningToChangeFundamental(string fundamentalNote)
     {
         AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", fundamentalNote, gameObject);
         Debug.Log("Fundamental Note Set To: " + fundamentalNote);
     }
     public void changeHarmony(string harmonyNote)
     {
         AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", harmonyNote, gameObject);
         Debug.Log("Harmony Note Set To: " + harmonyNote);
     }

    IEnumerator StartMusicalProgression()
    {
        float _t = WakeUpCounter;
        float _tQueueShadow = _t * 1f/4f - 60f;
        float _tQueueShruti = _t * 2f/4f - 60f;
        float _tQueueSonoflore = _t * 3f/4f - 60f;

        Debug.Log("Music Progression: Stage 0 SonoFlore");
        QueueNewWorld("Gentle", "Red");

        while(_t >= _tQueueShadow)
        {
            _t -= Time.deltaTime;
            yield return null;
        }
        Debug.Log("Music Progression: Stage 1 Gentle");
        QueueNewWorld("Shadow", "Blue");

        while(_t >= _tQueueShruti)
        {
            _t -= Time.deltaTime;
            yield return null;
        }
        Debug.Log("Music Progression: Stage 2 Shadow");
        QueueNewWorld("Shruti", "White");

        while(_t >= _tQueueSonoflore)
        {
            _t -= Time.deltaTime;
            yield return null;
        }
        Debug.Log("Music Progression: Stage 3 Shruti");
        QueueNewWorld("SonoFlore", "Red");

        //LET'S PUT THE GO DARK LOGIC HERE.

    }

    IEnumerator AVS_Program_DynamicDrop_Start()
    {
        bool stopProgression = false;
        Cleanup(coroutineCleanupList); //not necessary for the first one, but placing it here for convention.
        yield return null;
        //define a list of integers to hold the director queue index items that are created in this coroutine
       
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDropStart. Waiting for lights. Currently:" + lightControl.cycleRecent);

        if(developmentMode.developmentPlayground)
        {
            lightControl.SetColorWorldByType("Red", 0.0f);
        }

        while((lightControl.cycleRecent == "dark") || (lightControl.cycleRecent == "Dark"))
        {
            yield return null;
        }
        //ONCE THE LIGHTS TURN ON, START AT 45HZ
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDropStart. Lights detected, set strobe to 45hz.");
        lightControl.SetStrobeRate(45.0f, 0.0f);
        float _timer = 10f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //AFTER 10 SECOND HOLD IS FINISHED, DROP TO 11HZ OVER 30 SECONDS
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDropStart. Initializeing drop from gamma to high alpha.");
        _timer = 30f / d;
        lightControl.SetStrobeRate(11.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //NOW TAKE 120 SECONDS TO DROP TO 8.5HZ
        //FOLLOWING THIS POINT, IF THE ABSORPTION THRESHOLD IS MET, WE WILL SKIP TO THE NEXT PROGRAM
        _timer = 150f / d;
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDropStart. Begining drop from high alpha to 10hz.");
        lightControl.SetStrobeRate(8.5f, _timer);
        while(_timer > 0 || stopProgression)
        {
            if(AVS_Program_ManageThetaTransition(coroutineCleanupList))
            {
                stopProgression = true;
                break;
            }
            _timer -= Time.deltaTime;
            yield return null;
        }
        yield return null;
        //NOW START A SAW STROBE COROUTINE AROUND ALPHA
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDropStart. Starting Saw Strobe Coroutine.");
        float _wavelength = 360f / d;
        float _halfWavelength = _wavelength / 2;
        lightControl.SetSawStrobe(8.5f, 11.5f, _wavelength);
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDropStart. Waiting for absorption threshold to be met.");

        _timer = _halfWavelength;
        bool flag1 = false;
        bool flag2 = false;
        while(stopProgression)
        {
            if(AVS_Program_ManageThetaTransition(coroutineCleanupList))
            {
                stopProgression = true;
                break;
            }

            //ADD SOME MONO/STEREO BEHAVIOR. RHYTHMICALLY ADD MONO/STEREO COMMANDS TO DIRECTOR QUEUE
            //MONO ACTIVATES AT END, STEREO DOES NOT. COMMANDS ARE EXCLUSIVE.
            if (_timer > 0)
            {
                _timer -= Time.deltaTime;
            }
            else
            {
                _timer = _halfWavelength;
            }
            if(_timer > _halfWavelength*3/4)
            {
                flag2 = false;
                if(!flag1)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60.0f, false, 2));
                    flag1 = true;
                }
            }
            else if(_timer <= _halfWavelength*3/4)
            {
                flag1 = false;
                if(!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, true, 2));
                    flag2 = true;
                }
            }
            yield return null;
        }
        
    }

    private bool AVS_Program_ManageThetaTransition(List<int> coroutineCleanupList)
    {
        bool forceIt = developmentMode.developmentMode && Input.GetKeyDown(KeyCode.K);
        if((respirationTracker._absorption > _absorptionThreshold || forceIt) && !flagThetaCoroutine)
        {
            flagThetaCoroutine = true;
            CoroutineDynamicDropTheta = StartCoroutine(AVS_Program_DynamicDrop_Theta());
            return true;
        }
        return false;
    }

    IEnumerator AVS_Program_DynamicDrop_Theta()
    {
        if(CoroutineDynamicDropStart != null)
        {
            Debug.Log(WakeUpCounter + "| AVS Program: Stopping Coroutine from THETA Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
          
        yield return null;
        Cleanup(coroutineCleanupList);
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Starting Theta program.");

        //SET CORRECT MONO/STEREO
        director.ClearQueueOfType("monostereo");
        //add mono to queue, if we're in bilateral
        if(lightControl.bilateral)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, true, 2));
            Debug.Log(WakeUpCounter + " Director Queue: (AVS Program) DynamicDrop_Theta. Since starting in bilateral, adding " + (director.queueIndex - 1) + " monostereo=mono to director queue, and waiting.");
            director.LogQueue();
        }
        //and wait to enter mono...
        while(lightControl.bilateral)
        {
            yield return null;
        }
        //DROP TO 7HZ
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Dropping to 7hz.");
        float _timer = 120f / d;
        lightControl.SetStrobeRate(7.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //CYCLE THROUGH BILATERAL ONCE
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queueing Bilateral Strobe.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60f/d, true, 2));
        while(!lightControl.bilateral)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, true, 2));
        while(lightControl.bilateral)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queueing Mono Strobe.");
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. bilateral is: " + lightControl.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, true, 2));
        while(lightControl.bilateral)
        {
            yield return null;
        }
        //DROP TO 6HZ
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Dropping to 6hz.");
        _timer = 60f / d;
        lightControl.SetStrobeRate(6.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //GAMMA BURSTS, THEN HANG HERE. 
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queuing Gamma Burst.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f/d, true, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queuing Gamma Burst Stop.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 180f/d, true, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //DROP TO 5HZ
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Dropping to 5hz.");
        _timer = 60f / d;
        lightControl.SetStrobeRate(5.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //NOW CYCLE THROUGH GAMMA BETWEEN GAMMA AND NOT-GAMMA, AS WE WERE DOING WITH BILATERAL IN THE LAST PROGRAM
        _timer = 300f / d;
        float _halfWave = _timer / 2;
        bool flag1 = false;
        bool flag2 = false;
        while(true)
        {
            if(_timer > 0)
            {
                _timer -= Time.deltaTime;
            }
            else
            {
                _timer = 300f / d;
            }
            if(_timer > _halfWave)
            {
                flag2 = false;
                if(!flag1)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f/d, true, 2));
                    flag1 = true;
                    Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst.");
                }
            }
            else if(_timer <= _halfWave)
            {
                flag1 = false;
                if(!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 60f/d, true, 2));
                    flag2 = true;
                    Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst Stop.");
                }
            }
            yield return null;
        }
    }

    IEnumerator AVS_Program_DynamicDrop_End() //TEST THIS
    {
        if(CoroutineDynamicDropStart != null)
        {
            Debug.Log(WakeUpCounter + "| AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
        if(CoroutineDynamicDropTheta != null)
        {
            Debug.Log(WakeUpCounter + "| AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropTheta);
        }

        yield return null;
        Cleanup(coroutineCleanupList);
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_End. Starting End Program.");
        director.ClearQueueOfType("gamma");
        director.ClearQueueOfType("monostereo");
        if(!lightControl.bilateral)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 30.0f, true, 2));
        }
        if(lightControl._gammaBurstMode != 0.0f)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 30.0f, true, 2));
        }

        while(WakeUpCounter > 110f)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_End. Stabilizing before dramatic rise.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 10.0f, true, 2));

        while(WakeUpCounter > 90f)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_End. Starting dramatic rise to 40hz.");
        lightControl.SetStrobeRate(40.0f, 90f);

        while(WakeUpCounter > 0f)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_End. End of AVS Program. Goodnight!");
    }

    private void Cleanup(List<int> coroutineCleanupList)
    {
        Debug.Log("Performing cleanup.");
        foreach (int index in coroutineCleanupList)
        {
            Debug.Log(WakeUpCounter + " Director Queue (AVS Program): DynamicDrop (Transitioning). Removing " + index + " " + director.queue[index].Item2);
            director.queue.Remove(index);
        }
        director.LogQueue();
    }
    
    private Action Action_Gamma(bool gammaOn)
    {
        return () => lightControl.Gamma(gammaOn);
    }
    
    private void QueueNewWorld(string world, string color)
    {
        director.AddActionToQueue(Action_SetSoundWorld(world), "SoundWorld", true, false, 120.0f, true, 2);
        director.AddActionToQueue(Action_SetPreferredColor(color), "ColorPreference", false, true, 120.0f, true, 2);
        director.AddActionToQueue(Action_NextColorWorld(120.0f), "ColorCycle", false, true, 120.0f, true, 2);
        director.AddActionToQueue(Action_PlayTransitionSound(), "TransitionSound", true, false, 120.0f, true, 2);
    }

    private Action Action_SetSoundWorld(string soundWorld)
    {
        return () => SetSoundWorld(soundWorld);
    }

    private void SetSoundWorld(string soundWorld)
    {
        AkSoundEngine.SetState("SoundWorldMode", soundWorld);
        Debug.Log("Sound World Set To: " + soundWorld);
    }

    private Action Action_SetPreferredColor(string color)
    {
        return () => lightControl.SetPreferredColor(color);
    }

    private Action Action_NextColorWorld(float _seconds)
    {
        return () => lightControl.NextColorWorld(_seconds);
    }
    
    private Action Action_Strobe_MonoStereo(bool bilateral = false)
    {
        return () => lightControl.Strobe_MonoStereo(bilateral);
    }
    private Action Action_Strobe_Frequency(float frequency, float seconds)
    {
        return () => lightControl.SetStrobeRate(frequency, seconds);
    }
    private Action Action_PlayTransitionSound()
    {
        return () => director.PlayTransitionSound();
    }

    public void ChangeToningState()
    {
        AkSoundEngine.SetState("SoundWorldMode", currentToningState);
    }

    public void ChangeSwitchState()
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup", currentSwitchState, gameObject);
    }

    //THESE ARE NEVER USED - COMMENTING THEM OUT
    //public void setallRTPCValue(float newRTPCValue)
    //{
    //    AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", newRTPCValue, gameObject);
    //    AkSoundEngine.SetRTPCValue("HarmonySilentVolume", newRTPCValue, gameObject);
    //    AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", newRTPCValue, gameObject);
    //}

    // public void setHarmonySilentVolumeRTPCValue(float newHarmonyVolumeRTPC)
    // {
    //     AkSoundEngine.SetRTPCValue("HarmonySilentVolume", newHarmonyVolumeRTPC, gameObject);
    // }

    // public void setFundamentalSilentVolumeRTPCValue(float newFundamentalVolumeRTPC)
    // {
    //     AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", newFundamentalVolumeRTPC, gameObject);
    // }

    // public void setInteractiveMusicSilentLoopsRTPCValue(float newInteractiveMusicSilentLoopRTPC)
    // {
    //     AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", newInteractiveMusicSilentLoopRTPC, gameObject);
    // }



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
