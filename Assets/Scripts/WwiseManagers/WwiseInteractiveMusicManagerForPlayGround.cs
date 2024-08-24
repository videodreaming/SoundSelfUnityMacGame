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

    public Dictionary<int, (Action action, string type, bool isAudioAction, bool isVisualAction, float timeLeft, bool expires)> directorQueue = new Dictionary<int, (Action, string, bool, bool, float, bool)>();
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
        Debug.Log("Fundamental Note: " + fundamentalNote);
    }
    public void changeHarmony(string harmonyNote)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", harmonyNote, gameObject);
        Debug.Log("Harmony Note: " + harmonyNote);
    }

   
    // Update is called once per frame
    void Update()
    {
        DirectorQueueUpdate(); //REEF, this should be in a separate script with the director queue

        /*
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            directorQueue.Add(directorQueueIndex++, (Action_Apples(), "apples", false, false, 10.0f));
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            directorQueue.Add(directorQueueIndex++, (Action_Bananas(), "bananas", false, false, 20.0f));
        }
        if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            DirectorQueueProcessAll();
        }
        */

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
                        Debug.Log("Stage 1 Gentle");
                        AkSoundEngine.SetState("SoundWorldMode", "Gentle");
                        wwiseAVSMusicManager.preferredColor = "Red";
                        break;
                    case 2:
                        Debug.Log("Stage 2 Shadow");
                        AkSoundEngine.SetState("SoundWorldMode", "Shadow");
                        wwiseAVSMusicManager.preferredColor = "Blue";
                        break;
                    case 3:
                        Debug.Log("Stage 3 Shruti");
                        AkSoundEngine.SetState("SoundWorldMode", "Shruti");
                        wwiseAVSMusicManager.preferredColor = "White";
                        break;
                    case 4:
                        Debug.Log("Stage 0 SonoFlore");
                        AkSoundEngine.SetState("SoundWorldMode", "SonoFlore");
                        wwiseAVSMusicManager.preferredColor = "Red";
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
                        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Removing " + directorQueue[index].Item2 + "item from director queue with index: " + index);
                        directorQueue.Remove(index);
                    }

                   directorQueue.Add(directorQueueIndex++, (Action_Strobe_MonoStereo(true), "monostereo", false, true, (120.0f / d), true));
                   directorQueueIndexList.Add(directorQueueIndex - 1);
                   Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Added monostereo=stereo to director queue.");
                  
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
                        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Removing " + directorQueue[index].Item2 + "item from director queue with index: " + index);
                        directorQueue.Remove(index);
                    }

                    _newAlpha = UnityEngine.Random.Range(8.0f, 12.0f);
                   directorQueue.Add(directorQueueIndex++, (Action_Strobe_Frequency(_newAlpha, 120.0f / d), "frequency", false, false, 60.0f / d, false));
                   directorQueueIndexList.Add(directorQueueIndex - 1);

                   directorQueue.Add(directorQueueIndex++, (Action_Strobe_MonoStereo(false), "monostereo", false, true, (60.0f / d), false));
                    directorQueueIndexList.Add(directorQueueIndex - 1);

                   Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Added monostereo=mono and target new alpha of " + _newAlpha + " to director queue.");

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
                Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Removing " + directorQueue[index].Item2 + "item from director queue with index: " + index);
                directorQueue.Remove(index);
            }

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
            }
        }
        yield return null;

        if(wwiseAVSMusicManager.bilateral) //DISABLE BILATERAL
        {
            directorQueue.Add(directorQueueIndex++, (Action_Strobe_MonoStereo(), "monostereo", false, true, 60.0f, false));
            directorQueueIndexList.Add(directorQueueIndex - 1);
            Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop_Theta. Added monostereo=mono to director queue.");
        }
        //TODO - ADD BEHAVIOR FOR DROPPING DOWN INTO THETA AND DOING GAMMA-BURSTS. STOP FOR SOME BILATERAL STROBING IF THERE'S TIME.
    }
    
    private Action Action_Strobe_MonoStereo(bool bilateral = false)
    {
        return () => wwiseAVSMusicManager.Strobe_MonoStereo(bilateral);
    }
    private Action Action_Strobe_Frequency(float frequency, float seconds)
    {
        return () => wwiseAVSMusicManager.SetStrobeRate(frequency, seconds);
    }

    //REEF, these two functions should go in another script with the director queue
    //a function to go through the director queue, update the times, and execute the actions that have run out of time
    public void DirectorQueueUpdate()
    {
        List<int> keysToRemove = new List<int>();
        var keys = new List<int>(directorQueue.Keys); // Create a copy of the keys to avoid modifying the collection during iteration
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
                if(!value.expires)
                {
                    value.action(); // Perform the action when it runs out of time.
                    Debug.Log("Director Queue: Action " + value.type + " executed from running out of time");
                }
                else
                {
                    Debug.Log("Director Queue: Action " + value.type + " expired without executing");
                }
                keysToRemove.Add(key);
            }
        }
        foreach (int key in keysToRemove)
        {
            directorQueue.Remove(key);
        }
    }

    public void DirectorQueueProcessAll()
    {
        foreach (var item in directorQueue)
        {
            item.Value.action();
            Debug.Log("Director Queue: Action " + item.Value.type + " executed from process-all");
        }
        directorQueue.Clear();
    }
    
    /*
    public void TestDirectorQueue_Apples()
    {
        Debug.Log("TestDirectorQueue_Apples");
    }
    public void TestDirectorQueue_Bananas()
    {
        Debug.Log("TestDirectorQueue_Bananas");
    }
    public Action Action_Apples()
    {
        return () => TestDirectorQueue_Apples();
    }
    public Action Action_Bananas()
    {
        return () => TestDirectorQueue_Bananas();
    }
   */

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
