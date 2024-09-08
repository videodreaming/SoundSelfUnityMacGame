using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class Director : MonoBehaviour
{
    
    //Director Queue... 
    //The way this works is:
    //1. You add an action to the director queue with a time limit.
    //2. The QueueUpdate function will update the time limit and execute the action when the time limit is reached.
    //3. The ActivateQueue function will execute all actions in the queue. This is typically done when a change is detected in the GameValues script (those parts of that script should probably also be moved into a new director script with the rest of this, so it's all in one neat and tidy place)

    //THIS DICTIONARY IS INTERESTING
    //It collects actions.
    //One of three things can happen:
    //1. If "ActivateQueue()" is called, all the actions execute. We do this (mostly) when the system detects a change in player behavior
    //2. If the time limit is reached, actions with "activateAtEnd" will activate on the next tone. Otherwise, they will expire. (See "QueueUpdate()")
    //3. The queue can also be cleared.

    public DevelopmentMode developmentMode;
    public LightControl lightControl;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    private bool debugAllowLogs = false;
    
    public Dictionary<int, (Action action, string type, bool isAudioAction, bool isVisualAction, float timeLeft, bool activateAtEnd)> queue = new Dictionary<int, (Action action, string type, bool isAudioAction, bool isVisualAction, float timeLeft, bool activateAtEnd)>();
    public int queueIndex = 0;
    private int audioTweakCounter = 0;
    public bool disable = false;
    private bool disableLast = false;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        QueueUpdate(); //TODO: put this in a separate script with director stuff

        if(disable != disableLast)
        {
            if(disable)
            {
                Debug.Log("Director Queue: Director Disabled");
            }
            else
            {
                Debug.Log("Director Queue: Director Enabled");
            }
            disableLast = disable;
        }
    }

    public void QueueUpdate()
    {
        if(disable)
        {
            return;
        }
        List<int> keysToRemove = new List<int>();
        var keys = new List<int>(queue.Keys); // Create a copy of the keys to avoid modifying the collection during iteration
        foreach (var key in keys)
        {
            var value = queue[key]; // Create a temporary variable
            if (value.timeLeft > 0)
            {
                value.timeLeft -= Time.deltaTime;
                queue[key] = value; // Update the dictionary with the modified value
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
            queue.Remove(key);
            LogQueue();
        }
    }
    
    private IEnumerator ActivateOnTone(Action action, int id = -1, string type = "(unknown type)")
    {
        Debug.Log("Director Queue: Action " + id + " " + type + " will activate when next tone begins");
        //first, if we are toning, wait for this tone to finish...
        while (imitoneVoiceInterpreter.toneActiveConfident)
        {
            yield return null;
        }
        //then, wait for the next tone to start
        while(!imitoneVoiceInterpreter.toneActiveConfident)
        {
            yield return null;
        }
        //then run the action
        action();
    }

    public int AddActionToQueue(Action action, string type, bool isAudioAction, bool isVisualAction, float timeLimit, bool activateAtEnd, int exclusivityBehavior = 1)
    {
        //exclusivity behavior works like this:
        //0: NONE - No exclusivity, just add the action to the queue
        //1: PREFER LOWEST TIME LEFT - Check if there are any actions of the same type in the queue. If there are, only add (replace) the action if the new action has less time left than the existing action.
        // Code below:        
        //2: ALWAYS REPLACE - Clear all actions of the same type from the queue, then add the action
        if(disable)
        {
            Debug.LogWarning("Director Queue: Director is disabled, not adding action to queue.");
            return -1;
        }
        if(exclusivityBehavior == 1)
        {
            if(SearchQueueForType(type))
            {
                foreach (var item in queue)
                {
                    if(item.Value.type == type)
                    {
                        if(item.Value.timeLeft <= timeLimit)
                        {
                            Debug.Log("Director Queue: Action " + type + " already exists in director queue with shorter timeLeft, not adding new one per exclusivity rules.");
                            LogQueue();
                            return -1; //return -1 to indicate that the action was not added
                        }
                    }
                }
                ClearQueueOfType(type);
            }
        }
        else if(exclusivityBehavior == 2)
        {
            ClearQueueOfType(type);
        }
        queue.Add(queueIndex++, (action, type, isAudioAction, isVisualAction, timeLimit, activateAtEnd));

        Debug.Log("Director Queue: Added " + (queueIndex - 1) + " " + type + " to director queue.");
        LogQueue();

        return queueIndex - 1;
    }

    
    public void ActivateQueue(float transitionTimeForFlourishes = 5.0f)
    {
        int countAudioEvents = 0;
        int countVisualEvents = 0;

        if(disable)
        {
            Debug.LogWarning("Director Queue: Director is disabled, not activating queue.");
            return;
        }

        LogQueue();
      
        foreach (var item in queue)
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

        //FLOURISHES
        //if an audio or visual action is missing from the queue,
        //we need to trigger one of each to complete syncresis

        if (countAudioEvents == 0)
        {
            Debug.Log("Director Queue: No Audio Actions Queued, Triggering one to complete syncresis");
            TweakAudio(transitionTimeForFlourishes);
            PlayTransitionSound();
        }
        if (countVisualEvents == 0)
        {
            Debug.Log("Director Queue: No Visual Actions Queued, Triggering one to complete syncresis");
            lightControl.NextPreferredColorWorld(transitionTimeForFlourishes);
            lightControl.FXWave(0.75f, 15.0f, 0.1f, true);
        }
        queue.Clear();
        
        LogQueue();
    }

    public void LogQueue()
    {
        //outputs a single log line, with the following format: "Director Queue: <index> <type>, <index> <type>, <index> <type>..."
        string logString = "Director Queue Contents: ";
        foreach (var item in queue)
        {
            logString += "<" + item.Key + " " + item.Value.type + ", " + item.Value.timeLeft + "s> ";
        }
        Debug.Log(logString);
    }

    public bool SearchQueueForType(string type)
    {
        foreach (var item in queue)
        {
            if(item.Value.Item2 == type)
            {
                return true;
            }
        }
        return false;
    }

    public void ClearQueueOfType(string type)
    {
        List<int> keysToRemove = new List<int>();
        foreach (var item in queue)
        {
            if(item.Value.Item2 == type)
            {
                keysToRemove.Add(item.Key);
            }
        }
        foreach (int key in keysToRemove)
        {
            queue.Remove(key);
        }
        LogQueue();
        Debug.Log("Director Queue: Removed all " + type + " items from director queue.");
        LogQueue();
    }

    //====================================================================================================
    //ACTIONS
    //====================================================================================================
    
    //PUBLIC
    public void PlayTransitionSound()
    {
        AkSoundEngine.PostEvent("Unity_TransitionSFX", gameObject);
        Debug.Log("Director: Transition Sound Played");
    }

    //PRIVATE
    private Action Action_DirectorTest(string print)
    {
        return () => DirectorTest(print);
    }

    private void DirectorTest(string print)
    {
        Debug.Log("Director Test: " + print);
    }

    private void TweakAudio(float _seconds)
    {
        audioTweakCounter++;

        float _rtpcTarget = audioTweakCounter % 2 == 0 ? 100.0f : 0.0f;
        int ms = (int)(_seconds * 1000.0f);
        
        AkSoundEngine.SetRTPCValue("Unity_SoundTweak", _rtpcTarget, gameObject, ms);
        Debug.Log("Director: Audio Tweak to " + _rtpcTarget + " in " + ms + "ms (this isn't in wwise yet, I think)");
    }
    
    // private Action Action_TweakAudio(float _seconds)
    // {
    //     return () => director.TweakAudio(_seconds);
    // }
    // private Action Action_ChangeColor(float _seconds)
    // {
    //     return () => lightControl.NextPreferredColorWorld(_seconds);;
    // }

}
