using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WwiseInteractiveMusicManagerForPlayGround : MonoBehaviour
{
    private int fundamentalCount = -1;
    private int harmonyCount = -1;

    public DevelopmentMode developmentMode;
    public MusicSystem1ForPlayGround musicSystem1ForPlayGround;
    public WwiseAVSMusicManagerForPlayGround wwiseAVSMusicManager;
    public RespirationTrackerForPlayground respirationTracker;
    public GameValuesForPlayGround gameValues;
    public string currentSwitchState = "B";
    public string currentToningState = "None";
    public float InteractiveMusicSilentLoopsRTPC = 0.0f;
    public float HarmonySilentVolumeRTPC = 0.0f;
    public float FundamentalSilentVolumeRTPC = 0.0f;
    private float UserNotToningThreshold = 60.0f;
    public ImitoneVoiceIntepreterForPlayground imitoneVoiceIntepreter;
    public WwiseVOManagerForPlayGround wwiseVOManagerForPlayGround;
    public uint playingId;
    private bool toneActiveTriggered = false; // Flag to control the event triggering

    [SerializeField]
    private int currentStage = 0; // Tracks the current stage of the sound world
    private bool musicProgressionFlag = false;

    private float interactiveMusicExperienceTotalTime;
    private float finalStagePreLogicTime;

    private float WakeUpCounter;
    private bool wakeUpEndSoonTriggered = false; // Flag to control the event triggering
    private float soundWorldChangeTime;
    private bool finalStagePreLogicExecuted = false; 
    public bool CFundamentalGHarmonyLock = false;
    public CSVWriter csvWriter;
    private bool thisTonesImpactPlayed = false;
    // AVS Controls
    private float _absorptionThreshold;
    float d = 10f; //debug timer mult
    private Coroutine AVSControlCoroutine;

    //Director Queue... 
    //REEF, the Director Queue and its Update function should, in my opinion, be in a separate script. I didn't want to make new scripts while in the playground editing. It is referenced in GameValuesForPlayground.cs
    //The way this works is:
    //1. You add an action to the director queue with a time limit.
    //2. The DirectorQueueUpdate function will update the time limit and execute the action when the time limit is reached.
    //3. The DirectorQueueProcessAll function will execute all actions in the queue. This is typically done when a change is detected in the GameValues script (those parts of that script should probably also be moved into a new director script with the rest of this, so it's all in one neat and tidy place)

    //THIS DICTIONARY IS INTERESTING
    //It collects actions.
    //One of three things can happen:
    //1. If "DirectorQueueProcessAll()" is called, all the actions execute. We do this (mostly) when the system detects a change in player behavior
    //2. If the time limit is reached, actions with "activateAtEnd" will activate on the next tone. Otherwise, they will expire. (See "DirectorQueueUpdate()")
    //3. The queue can also be cleared.
    
    public int audioTweakCounter = 0;
    public float imminentTransitionTime = 5.0f;
    public Dictionary<int, (Action action, string type, bool isAudioAction, bool isVisualAction, float timeLeft, bool activateAtEnd)> directorQueue = new Dictionary<int, (Action action, string type, bool isAudioAction, bool isVisualAction, float timeLeft, bool activateAtEnd)>();
    public int directorQueueIndex = 0;

    void Awake()
    {
        AkSoundEngine.SetState("SoundWorldMode","SonoFlore");
    }

    void Start()
    {
        WakeUpCounter = 2280.0f;
        interactiveMusicExperienceTotalTime = 1245.0f;
        soundWorldChangeTime = interactiveMusicExperienceTotalTime / 4;
        finalStagePreLogicTime = 15f; 
        _absorptionThreshold = UnityEngine.Random.Range(0.08f, 0.35f);
        
        AVSControlCoroutine = StartCoroutine(AVS_Program_DynamicDrop());
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
        
        musicSystem1ForPlayGround.fundamentalNote = 9;
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", 30.0f, gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Fundamentalonly", gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Harmonyonly", gameObject);
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Fundamentalonly", "AVS System");
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Harmonyonly", "Master Audio Bus");
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

   
    // Update is called once per frame
    void Update()
    {
        // if(Input.GetKeyDown(KeyCode.F))
        // {
        //     fundamentalCount++;
        //     int mod = fundamentalCount % 3;
        //     int note = 0;
            
        //     switch(mod)
        //     {
        //         case 0:
        //             note = 0;
        //             AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", ConvertIntToNote(note), gameObject);
        //             break;
        //         case 1:
        //             note = 5;
        //             AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", ConvertIntToNote(note), gameObject);
        //             break;
        //         case 2:
        //             note = 8;
        //             AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", ConvertIntToNote(note), gameObject);
        //             break;
        //     }

        //     Debug.Log("Fundamental Note: " + ConvertIntToNote(note));
        // }
        // if(Input.GetKeyDown(KeyCode.H))
        // {
        //     harmonyCount++;
        //     int mod = harmonyCount % 3;
        //     int note = 0;

        //     switch(mod)
        //     {
        //         case 0:
        //             note = 1;
        //             AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", ConvertIntToNote(note), gameObject);
        //             break;
        //         case 1:
        //             note = 6;
        //             AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", ConvertIntToNote(note), gameObject);
        //             break;
        //         case 2:
        //             note = 10;
        //             AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", ConvertIntToNote(note), gameObject);
        //             break;
        //     }

        //     Debug.Log("Harmony Note: " + ConvertIntToNote(note));
        // }
        DirectorQueueUpdate(); //TODO: put this in a separate script with director stuff

        if(imitoneVoiceIntepreter.toneActive == false)
        {
            thisTonesImpactPlayed = false;
        }
        if(WakeUpCounter > -1.0f)
        {
            WakeUpCounter -= Time.deltaTime;
        } 
        
        if( WakeUpCounter <= 0.0f && !wakeUpEndSoonTriggered)
        {
            AkSoundEngine.PostEvent("Play_WakeUpEndSoon_SEQUENCE", gameObject);
            WakeUpCounter = -1.0f;
            wakeUpEndSoonTriggered = true;
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

        
        if(wwiseVOManagerForPlayGround.interactive && !musicProgressionFlag)
        {
            musicProgressionFlag = true;
            StartCoroutine(StartMusicalProgression());
        }

        if(wwiseVOManagerForPlayGround.interactive)
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

    //public void AVS_ProgramControl()
    //{
        //Let's do this:

        //When the lights first come on, fade relatively quickly from gamma to high alpha.
        //Then, fade relatively slowly from high alpha to 10hz.
        //Then, once absorption falls above a certain level, begin drop to theta.
        //Once in theta, do either gamma-bursts or bilateral, depending on absorption.
        //In last three minutes, bilateral strobing
        //In last 60 seconds, quickly rise back to Beta while in bilateral.

        //If we get a "change" at one of our holding places, switch to bilateral until next change.

    //}

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

    }

    IEnumerator AVS_Program_DynamicDrop()
    {
        yield return null;
        //define a list of integers to hold the director queue index items that are created in this coroutine
        List<int> directorQueueIndexList = new List<int>();
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Waiting for lights. Currently:" + wwiseAVSMusicManager.cycleRecent);

        if(developmentMode.developmentPlayground)
        {
            wwiseAVSMusicManager.SetColorWorldByType("Red", 0.0f);
        }

        while((wwiseAVSMusicManager.cycleRecent == "dark") || (wwiseAVSMusicManager.cycleRecent == "Dark"))
        {
            yield return null;
        }
        //ONCE THE LIGHTS TURN ON, START AT 45HZ
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Lights detected, set strobe to 45hz.");
        wwiseAVSMusicManager.SetStrobeRate(45.0f, 0.0f);
        float _timer = 10f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //AFTER 10 SECOND HOLD IS FINISHED, DROP TO 11HZ OVER 30 SECONDS
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Initializeing drop from gamma to high alpha.");
        _timer = 30f / d;
        wwiseAVSMusicManager.SetStrobeRate(11.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //NOW TAKE 120 SECONDS TO DROP TO 8.5HZ
        //FOLLOWING THIS POINT, IF THE ABSORPTION THRESHOLD IS MET, WE WILL SKIP TO THE NEXT PROGRAM
        _timer = 150f / d;
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Begining drop from high alpha to 10hz.");
        wwiseAVSMusicManager.SetStrobeRate(8.5f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            if(AVS_Program_ManageThetaTransition(directorQueueIndexList))
            {
                break;
            }
            yield return null;
        }
        //NOW START A SAW STROBE COROUTINE AROUND ALPHA
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Starting Saw Strobe Coroutine.");
        float _wavelength = 360f / d;
        float _halfWavelength = _wavelength / 2;
        wwiseAVSMusicManager.SetSawStrobe(8.5f, 11.5f, _wavelength);
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Waiting for absorption threshold to be met.");

        _timer = _halfWavelength;
        bool flag1 = false;
        bool flag2 = false;
        while(true)
        {
            if(AVS_Program_ManageThetaTransition(directorQueueIndexList))
            {
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
                    directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60.0f, false, 2));
                    flag1 = true;
                }
            }
            else if(_timer <= _halfWavelength*3/4)
            {
                flag1 = false;
                if(!flag2)
                {
                    directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, true, 2));
                    flag2 = true;
                }
            }
            yield return null;
        }
    }

    private bool AVS_Program_ManageThetaTransition(List<int> directorQueueIndexList)
    {
        if(respirationTracker._absorption > _absorptionThreshold || Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Absorption threshold met, starting Theta coroutine");
            
            // Remove all items from the director queue that were created in this coroutine
            foreach (int index in directorQueueIndexList)
            {
                Debug.Log(WakeUpCounter + " Director Queue (AVS Program): DynamicDrop (Transitioning). Removing " + index + " " + directorQueue[index].Item2);
                directorQueue.Remove(index);
            }
            LogDirectorQueue();

            // Stop the current coroutine, if there is one
            if(AVSControlCoroutine != null)
            {
                StopCoroutine(AVSControlCoroutine);
            }

            AVSControlCoroutine = StartCoroutine(AVS_Program_DynamicDrop_Theta());
            return true;
        }
        return false;
    }

    IEnumerator AVS_Program_DynamicDrop_Theta()
    {
        List<int> directorQueueIndexList = new List<int>();
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Starting Theta program.");

        //SET CORRECT MONO/STEREO
        ClearActionsOfTypeFromDirectorQueue("monostereo");
        //add mono to queue, if we're in bilateral
        if(wwiseAVSMusicManager.bilateral)
        {
            directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, true, 2));
            Debug.Log(WakeUpCounter + " Director Queue: (AVS Program) DynamicDrop_Theta. Since starting in bilateral, adding " + (directorQueueIndex - 1) + " monostereo=mono to director queue, and waiting.");
            LogDirectorQueue();
        }
        //and wait to enter mono...
        while(wwiseAVSMusicManager.bilateral)
        {
            yield return null;
        }
        //DROP TO 7HZ
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Dropping to 7hz.");
        float _timer = 120f / d;
        wwiseAVSMusicManager.SetStrobeRate(7.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //CYCLE THROUGH BILATERAL ONCE
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. bilateral is: " + wwiseAVSMusicManager.bilateral);
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queueing Bilateral Strobe.");
        directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60f/d, true, 2));
        while(!wwiseAVSMusicManager.bilateral)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. bilateral is: " + wwiseAVSMusicManager.bilateral);
        directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, true, 2));
        while(wwiseAVSMusicManager.bilateral)
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queueing Mono Strobe.");
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. bilateral is: " + wwiseAVSMusicManager.bilateral);
        directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f/d, true, 2));
        while(wwiseAVSMusicManager.bilateral)
        {
            yield return null;
        }
        //DROP TO 6HZ
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Dropping to 6hz.");
        _timer = 60f / d;
        wwiseAVSMusicManager.SetStrobeRate(6.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //GAMMA BURSTS, THEN HANG HERE. 
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queuing Gamma Burst.");
        directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Gamma(true), "gamma", false, false, 60f/d, true, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queuing Gamma Burst Stop.");
        directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Gamma(false), "gamma", false, false, 180f/d, true, 2));
        _timer = 180f / d;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //DROP TO 5HZ
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Dropping to 5hz.");
        _timer = 60f / d;
        wwiseAVSMusicManager.SetStrobeRate(5.0f, _timer);
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
                    directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Gamma(true), "gamma", false, false, 60f/d, true, 2));
                    flag1 = true;
                    Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst.");
                }
            }
            else if(_timer <= _halfWave)
            {
                flag1 = false;
                if(!flag2)
                {
                    directorQueueIndexList.Add(AddActionToDirectorQueue(Action_Gamma(false), "gamma", false, false, 60f/d, true, 2));
                    flag2 = true;
                    Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst Stop.");
                }
            }
            yield return null;
        }
        
        //NEAR END (NEXT METHOD), START IN BILATERAL AND NO GAMMA, THEN COME ALL THE WAY UP TO 40HZ
    }
    
    private Action Action_Gamma(bool gammaOn)
    {
        return () => wwiseAVSMusicManager.Gamma(gammaOn);
    }
    //TODO, TEST THIS
    public int AddActionToDirectorQueue(Action action, string type, bool isAudioAction, bool isVisualAction, float timeLimit, bool activateAtEnd, int exclusivityBehavior = 1)
    {
        //exclusivity behavior works like this:
        //0: NONE - No exclusivity, just add the action to the queue
        //1: PREFER LOWEST TIME LEFT - Check if there are any actions of the same type in the queue. If there are, only add (replace) the action if the new action has less time left than the existing action.
        // Code below:        
        //2: ALWAYS REPLACE - Clear all actions of the same type from the queue, then add the action
       
        if(exclusivityBehavior == 1)
        {
            if(SearchDirectorQueueForType(type))
            {
                foreach (var item in directorQueue)
                {
                    if(item.Value.type == type)
                    {
                        if(item.Value.timeLeft <= timeLimit)
                        {
                            Debug.Log("Director Queue: Action " + type + " already exists in director queue with shorter timeLeft, not adding new one per exclusivity rules.");
                            LogDirectorQueue();
                            return -1; //return -1 to indicate that the action was not added
                        }
                    }
                }
                ClearActionsOfTypeFromDirectorQueue(type);
            }
        }
        else if(exclusivityBehavior == 2)
        {
            ClearActionsOfTypeFromDirectorQueue(type);
        }
        directorQueue.Add(directorQueueIndex++, (action, type, isAudioAction, isVisualAction, timeLimit, activateAtEnd));

        Debug.Log("Director Queue: Added " + (directorQueueIndex - 1) + " " + type + " to director queue.");
        LogDirectorQueue();

        return directorQueueIndex - 1;
    }

    public bool SearchDirectorQueueForType(string type)
    {
        foreach (var item in directorQueue)
        {
            if(item.Value.Item2 == type)
            {
                return true;
            }
        }
        return false;
    }

    private void QueueNewWorld(string world, string color)
    {
        AddActionToDirectorQueue(Action_SetSoundWorld(world), "SoundWorld", true, false, 120.0f, true, 2);
        AddActionToDirectorQueue(Action_SetPreferredColor(color), "ColorPreference", false, true, 120.0f, true, 2);
        AddActionToDirectorQueue(Action_NextColorWorld(120.0f), "ColorCycle", false, true, 120.0f, true, 2);
        AddActionToDirectorQueue(Action_PlayTransitionSound(), "TransitionSound", true, false, 120.0f, true, 2);
    }

    private Action Action_DirectorTest(string print)
    {
        return () => DirectorTest(print);
    }

    private void DirectorTest(string print)
    {
        Debug.Log("Director Test: " + print);
    }

    public void ClearActionsOfTypeFromDirectorQueue(string type)
    {
        List<int> keysToRemove = new List<int>();
        foreach (var item in directorQueue)
        {
            if(item.Value.Item2 == type)
            {
                keysToRemove.Add(item.Key);
            }
        }
        foreach (int key in keysToRemove)
        {
            directorQueue.Remove(key);
        }
        LogDirectorQueue();
        Debug.Log("Director Queue: Removed all " + type + " items from director queue.");
        LogDirectorQueue();
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
        return () => wwiseAVSMusicManager.SetPreferredColor(color);
    }

    private Action Action_NextColorWorld(float _seconds)
    {
        return () => wwiseAVSMusicManager.NextColorWorld(_seconds);
    }
    
    private Action Action_Strobe_MonoStereo(bool bilateral = false)
    {
        return () => wwiseAVSMusicManager.Strobe_MonoStereo(bilateral);
    }
    private Action Action_Strobe_Frequency(float frequency, float seconds)
    {
        return () => wwiseAVSMusicManager.SetStrobeRate(frequency, seconds);
    }
    private Action Action_TweakAudio(float _seconds)
    {
        return () => TweakAudio(_seconds);
    }
    private Action Action_ChangeColor(float _seconds)
    {
        return () => wwiseAVSMusicManager.NextColorWorld(_seconds);;
    }

    //REEF, these two functions should go in another script with the director queue
    //a function to go through the director queue, update the times, and execute the actions that have run out of time
    //TODO, update the director queue to use the ActivateOnTone
    public void DirectorQueueUpdate()
    {
        List<int> keysToRemove = new List<int>();
        var keys = new List<int>(directorQueue.Keys); // Create a copy of the keys to avoid modifying the collection during iteration
        LogDirectorQueue();
        foreach (var key in keys)
        {
            var value = directorQueue[key]; // Create a temporary variable
            if (value.timeLeft > 0)
            {
                value.timeLeft -= Time.deltaTime;
                directorQueue[key] = value; // Update the dictionary with the modified value
            }
            else
            {
                //only execute the action if its "expires" bool is false
                if(value.activateAtEnd)
                {
                    Debug.Log("Director Queue: Action " + key + " " + value.type + " will execute on next tone...");
                    StartCoroutine(ActivateOnTone(value.action, key, value.type));
                }
                else
                {
                    Debug.Log("Director Queue: Action " + key + " " + value.type + " expired without executing");
                }
                keysToRemove.Add(key);
            }
        }
        foreach (int key in keysToRemove)
        {
            directorQueue.Remove(key);
            LogDirectorQueue();
        }
    }

    //a function that takes an action as a parameter, and waits for the next time toneActiveConfident becomes true to execute it
    private IEnumerator ActivateOnTone(Action action, int id = -1, string type = "(unknown type)")
    {
        Debug.Log("Director Queue: Action " + id + " " + type + " will activate when next tone begins");
        //first, if we are toning, wait for this tone to finish...
        while (imitoneVoiceIntepreter.toneActiveConfident)
        {
            yield return null;
        }
        //then, wait for the next tone to start
        while(!imitoneVoiceIntepreter.toneActiveConfident)
        {
            yield return null;
        }
        //then run the action
        action();
    }

    public void DirectorQueueProcessAll()
    {
        int countAudioEvents = 0;
        int countVisualEvents = 0;

        
        LogDirectorQueue();
      
        foreach (var item in directorQueue)
        {
            item.Value.action();
            
            if(item.Value.isAudioAction)
            {
                countAudioEvents++;
            }
            if(item.Value.isVisualAction)
            {
                countVisualEvents++;
            }

            Debug.Log("Director Queue: Action " + item.Key + " " + item.Value.type + " executed from process-all");
        }

        if (countAudioEvents == 0)
        {
            Debug.Log("Director Queue: No Audio Actions Queued, Triggering one to complete syncresis");
            TweakAudio(imminentTransitionTime);
            PlayTransitionSound();
        }
        if (countVisualEvents == 0)
        {
            Debug.Log("Director Queue: No Visual Actions Queued, Triggering one to complete syncresis");
            wwiseAVSMusicManager.NextColorWorld(imminentTransitionTime);
        }
        directorQueue.Clear();
        
        LogDirectorQueue();
    }

    public void LogDirectorQueue()
    {
        //outputs a single log line, with the following format: "Director Queue: <index> <type>, <index> <type>, <index> <type>..."
        string logString = "Director Queue Contents: ";
        foreach (var item in directorQueue)
        {
            logString += "<" + item.Key + " " + item.Value.type + ", " + item.Value.timeLeft + "s> ";
        }
        
    }

    private Action Action_PlayTransitionSound()
    {
        return () => PlayTransitionSound();
    }
    private void PlayTransitionSound()
    {
        AkSoundEngine.PostEvent("Unity_TransitionSFX", gameObject);
        Debug.Log("Transition Sound Played");
    }

    private void TweakAudio(float _seconds)
    {
        audioTweakCounter++;

        float _rtpcTarget = audioTweakCounter % 2 == 0 ? 100.0f : 0.0f;
        int ms = (int)(_seconds * 1000.0f);
        
        AkSoundEngine.SetRTPCValue("Unity_SoundTweak", _rtpcTarget, gameObject, ms);

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
