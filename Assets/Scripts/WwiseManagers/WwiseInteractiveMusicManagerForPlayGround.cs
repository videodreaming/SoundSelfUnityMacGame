using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WwiseInteractiveMusicManagerForPlayGround : MonoBehaviour
{
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
    private float elapsedTime = 0f; // Tracks the elapsed time since the start
    private int currentStage = 0; // Tracks the current stage of the sound world

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
    // Start is called before the first frame update
    void Start()
    {
        WakeUpCounter = 2280.0f;
        interactiveMusicExperienceTotalTime = 1245.0f;
        soundWorldChangeTime = interactiveMusicExperienceTotalTime / 4;
        finalStagePreLogicTime = 15f; 
        _absorptionThreshold = UnityEngine.Random.Range(0.08f, 0.35f);
        
        StartCoroutine(AVS_Program_DynamicDrop());
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
        AkSoundEngine.SetState("SoundWorldMode","SonoFlore");
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly","A",gameObject);
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pithces_HarmonyOnly","E",gameObject);
        
        musicSystem1ForPlayGround.fundamentalNote = 9;
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", 30.0f, gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Fundamentalonly", gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Harmonyonly", gameObject);
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Fundamentalonly", "AVS System");
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Harmonyonly", "Master Audio Bus");
    }


    public void userToningToChangeFundamental(string fundamentalNote)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup_12Pitches_FundamentalOnly", fundamentalNote,gameObject);
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
        if(wwiseVOManagerForPlayGround.interactive == true)
        {
            //TODO: 
            //The original logic for timing was made before the introduction of the director.
            //As it is, there is more variability around the actual times that the transition occurs
            //And the last stage will get as much as 2 minutes less play time.
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= soundWorldChangeTime)
            {
                if (currentStage == 0)
                {
                    Debug.Log("Stage 0 SonoFlore");
                }
                elapsedTime = 0f; // Reset elapsed time
                currentStage++; // Move to the next stage
                
                switch (currentStage)
                {
                    case 1:
                        Debug.Log("Stage 1 Gentle Queued");
                        QueueNewWorld("Gentle", "Red");
                        break;
                    case 2:
                        Debug.Log("Stage 2 Shadow Queued");
                        QueueNewWorld("Shadow", "Blue");
                        break;
                    case 3:
                        Debug.Log("Stage 3 Shruti Queued");
                        QueueNewWorld("Shruti", "White");
                        break;
                    case 4:
                        Debug.Log("Stage 0 SonoFlore Queued");
                        QueueNewWorld("SonoFlore", "Red");
                        currentStage = 0;
                        break;
                }   
            }

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

    IEnumerator AVS_Program_DynamicDrop()
    {
        yield return null;
        //define a list of integers to hold the director queue index items that are created in this coroutine
        List<int> directorQueueIndexList = new List<int>();
        float d = 10f; //debug timer mult
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Waiting for lights. Currently:" + wwiseAVSMusicManager.cycleRecent);

        if(gameValues.developmentPlayground)
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
        //AFTER 10 SECOND HOLD IS FINISHED, DROP TO 12HZ OVER 30 SECONDS
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Initializeing drop from gamma to high alpha.");
        _timer = 30f / d;
        wwiseAVSMusicManager.SetStrobeRate(12.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        //NOW TAKE 120 SECONDS TO DROP TO 10HZ
        //FOLLOWING THIS POINT, IF THE ABSORPTION THRESHOLD IS MET, WE WILL SKIP TO THE NEXT PROGRAM
        _timer = 120f / d;
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Begining drop from high alpha to 10hz.");
        wwiseAVSMusicManager.SetStrobeRate(10.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            if(AVS_Program_ManageThetaTransition(directorQueueIndexList))
            {
                break;
            }
            yield return null;
        }
        //NOW ENTER A LOOP, SWITCHING BETWEEN BILATERAL AND MONO, BUT JUST STAY AT 10HZ
        //TODO, - RIGHT NOW THIS WILL JUST HAPPEN FOREVER, WHICH IS LAME,
        //THIS AND THE OTHER ONE HAVE TO BE INTERRUPTED BY THE CLOSING PROGRAM
        //WHICH WILL HAPPEN CLOSE TO THE END OF THE SESSION.
        float _timer2 = 0f;
        bool flag1 = false;
        bool flag2 = false;
        float _newAlpha = UnityEngine.Random.Range(8.0f, 12.0f);

        //TODO - THIS LOOP IS NOT WORKING, REPLACE IT WITH SOME OTHER MORE INTENTIONAL BEHAVIOR. IT AIN'T WORKING ANYWAY.
        /*
        while(true)
        {
            if(AVS_Program_ManageThetaTransition(directorQueueIndexList))
            {
                break;
            }
           if(_timer2 < (60f / d))
           {
               if(!flag1)
               {
                    foreach (int index in directorQueueIndexList)
                    {
                        Debug.Log(WakeUpCounter + " Director Queue (AVS Program: DynamicDrop.) Removing " + directorQueue[index].Item2 + "item from director queue with index: " + index);
                        directorQueue.Remove(index);
                    }

                   directorQueue.Add(directorQueueIndex++, (Action_Strobe_MonoStereo(true), "monostereo", false, true, (120.0f / d), false));
                   directorQueueIndexList.Add(directorQueueIndex - 1);
                   Debug.Log(WakeUpCounter + " Director Queue [EDIT THIS] AVS Program: DynamicDrop. Added monostereo=stereo to director queue.");
                  
                   flag1 = true;
                   flag2 = false;
               }
               Debug.Log("FIRST :" + _timer2);               
           }
           else if (_timer2 < (300f / d))
           {
               if(!flag2)
               {    //with more time, trigger both mono and a new alpha rate
                    foreach (int index in directorQueueIndexList)
                    {
                        Debug.Log(WakeUpCounter + " Director Queue (AVS Program: DynamicDrop): Removing " + index + " " directorQueue[index].Item2);
                        directorQueue.Remove(index);
                    }

                    _newAlpha = UnityEngine.Random.Range(8.0f, 12.0f);
                   directorQueue.Add(directorQueueIndex++, (Action_Strobe_Frequency(_newAlpha, 120.0f / d), "frequency", false, false, 60.0f / d, true));
                   directorQueueIndexList.Add(directorQueueIndex - 1);

                   directorQueue.Add(directorQueueIndex++, (Action_Strobe_MonoStereo(false), "monostereo", false, true, (60.0f / d), true));
                    directorQueueIndexList.Add(directorQueueIndex - 1);

                   Debug.Log(WakeUpCounter + " Director Queue (AVS Program: DynamicDrop). Added " + (directorQueueIndex - 1) + " and " + (directorQuqueIndex -2) + " monostereo=mono and target new alpha of " + _newAlpha + " to director queue.");

                   flag2 = true;
                   flag1 = false;
               }
               Debug.Log("SECOND :" + _timer2);
           }
           else
           {
                _timer2 = 0f;
           }
           _timer2 += Time.deltaTime;
           Debug.Log("TIMER :" + _timer2 + " flag1: " + flag1 + " flag2: " + flag2);
           yield return null;
        }
        */
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Exiting DynamicDrop coroutine.");
        yield return null;
    }

    private bool AVS_Program_ManageThetaTransition(List<int> directorQueueIndexList)
    {
        if(respirationTracker._absorption > _absorptionThreshold)
        {
            Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Absorption threshold met, starting Theta coroutine");
            
            // Remove all items from the director queue that were created in this coroutine
            foreach (int index in directorQueueIndexList)
            {
                Debug.Log(WakeUpCounter + " Director Queue (AVS Program): DynamicDrop (Transitioning). Removing " + index + " " + directorQueue[index].Item2);
                directorQueue.Remove(index);
            }
            LogDirectorQueue();

            StartCoroutine(AVS_Program_DynamicDrop_Theta());
            return true;
        }
        return false;
    }

    IEnumerator AVS_Program_DynamicDrop_Theta()
    {
        List<int> directorQueueIndexList = new List<int>();
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Starting Theta program.");
        //clear any items from the director queue that are "monostereo" type
        foreach (var item in directorQueue)
        {
            if(item.Value.Item2 == "monostereo")
            {
                directorQueue.Remove(item.Key);
                Debug.Log(WakeUpCounter + " Director Queue (AVS Program): DynamicDrop_Theta. Removing " + item.Key + " " + item.Value.Item2 + " item from director queue.");
                LogDirectorQueue();
            }
        }
        yield return null;

        if(wwiseAVSMusicManager.bilateral) //DISABLE BILATERAL
        {
            directorQueue.Add(directorQueueIndex++, (Action_Strobe_MonoStereo(), "monostereo", false, true, 60.0f, true));
            directorQueueIndexList.Add(directorQueueIndex - 1);
            Debug.Log(WakeUpCounter + " Director Queue: (AVS Program) DynamicDrop_Theta. Added " + (directorQueueIndex - 1) + " monostereo=mono to director queue.");
            LogDirectorQueue();
        }
        //TODO - ADD BEHAVIOR FOR DROPPING DOWN INTO THETA AND DOING GAMMA-BURSTS. STOP FOR SOME BILATERAL STROBING IF THERE'S TIME.
    }

    //TODO, TEST THIS
    public void AddActionToDirectorQueue(Action action, string type, bool isAudioAction, bool isVisualAction, float timeLimit, bool activateAtEnd, int exclusivityBehavior = 1)
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
                            return;
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
