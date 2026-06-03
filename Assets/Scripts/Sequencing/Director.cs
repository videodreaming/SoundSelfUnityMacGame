using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;

/// <summary>When a queued action's time limit reaches zero (see <c>Director.QueueUpdate</c>).</summary>
public enum DirectorActivationBehavior
{
    /// <summary>Remove the item without running the action.</summary>
    ExpireWithoutExecuting = 0,
    /// <summary>Run this action on the next confident tone.</summary>
    ActivateThisActionOnNextTone = 1,
    /// <summary>Activate the entire queue on the next confident tone.</summary>
    ActivateEntireQueueOnNextTone = 2,
}

/// <summary>How a new item coexists with existing queue items of the same <c>type</c> (see <see cref="Director.AddActionToQueue"/>).</summary>
public enum DirectorExclusivityBehavior
{
    /// <summary>Allow multiple items of the same type.</summary>
    None = 0,
    /// <summary>If same type exists with time remaining ≤ new time limit, skip add; else clear that type and add.</summary>
    PreferShorterTimeRemaining = 1,
    /// <summary>Clear all items of that type, then add.</summary>
    ReplaceAllOfType = 2,
}

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
    //2. If the time limit is reached, see <see cref="DirectorActivationBehavior"/> (QueueUpdate).
    //3. The queue can also be cleared.

    public DevelopmentMode developmentMode;
    public LightControl lightControl;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    
    // Debug log category flags
    private bool debugAllowLogs = true;
    private bool debugAllowWarnings = true; // Warnings show if this OR the category flag is true
    
    public Dictionary<int, DirectorQueueItem> queue = new Dictionary<int, DirectorQueueItem>();
    /// <summary>Stored in <see cref="queue"/> when <see cref="AddActionToQueue"/> receives <c>null</c> for time limit — timer never expires (see <c>QueueUpdate</c>).</summary>
    public const float UnboundedQueueTimeSeconds = float.MaxValue;
    public int queueIndex = 0;
    private int audioTweakCounter = 0;
    public bool disable = false;
    private bool disableLast = false;
    
    // Transition sound cooldown
    private bool canPlayTransitionSound = true;
    private Coroutine transitionSoundCooldownCoroutine = null;
    
    // Track if ActivateQueueOnTone coroutine is already running to prevent multiple simultaneous activations
    private bool activateQueueOnToneRunning = false;
    private Coroutine _activateQueueOnToneCoroutine = null;
    private float timeSinceLastActivation = 0.0f;
    private float activateWhenEmptyThreshold = 25.0f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        timeSinceLastActivation += Time.deltaTime;
        QueueUpdate(); 

        if(disable != disableLast)
        {
            if(disable)
            {
                if(debugAllowLogs)
                {
                    Debug.Log("Director Queue: Director Disabled");
                }
            }
            else
            {
                if(debugAllowLogs)
                {
                    Debug.Log("Director Queue: Director Enabled");
                }
            }
            disableLast = disable;
        }
    }

    public void Disable()
    {
        disable = true;
        CancelPendingWholeQueueActivation();
    }

    public void Enable()
    {
        disable = false;
    }

    private void QueueUpdate()
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
                // Unbounded (null timeLimit → UnboundedQueueTimeSeconds): do not decrement; never expires by timer.
                if (value.timeLeft < UnboundedQueueTimeSeconds - 1e30f)
                    value.timeLeft -= Time.deltaTime;
                queue[key] = value; // Update the dictionary with the modified value
                continue;
            }

            // Timer expired — disposition is decided by DirectorQueuePolicy (see Stage 1 fix).
            switch (DirectorQueuePolicy.GetExpiryDisposition(value.activationBehavior))
            {
                case DirectorQueueExpiryDisposition.CaptureActionThenRemove:
                    if(debugAllowLogs)
                    {
                        Debug.Log("Director Queue: Action " + key + " " + value.type + " expired, will activate on next tone...");
                    }
                    StartCoroutine(ActivateOnTone(value.action, key, value.type));
                    keysToRemove.Add(key);
                    break;

                case DirectorQueueExpiryDisposition.RetainForWholeQueueActivation:
                    // Fix: do NOT remove this item. It must remain in the queue so the whole-queue
                    // activation (on the next tone) includes it. Mark pending so we don't re-log /
                    // re-arm every frame while its timer sits at <= 0.
                    if (!value.pendingActivation)
                    {
                        value.pendingActivation = true;
                        queue[key] = value;
                        if(debugAllowLogs)
                        {
                            Debug.Log("Director Queue: Action " + key + " " + value.type + " expired, will activate entire queue on next tone (retained)...");
                        }
                    }
                    if(!activateQueueOnToneRunning)
                    {
                        activateQueueOnToneRunning = true;
                        _activateQueueOnToneCoroutine = StartCoroutine(ActivateQueueOnTone());
                    }
                    break;

                case DirectorQueueExpiryDisposition.RemoveWithoutExecuting:
                default:
                    if(value.activationBehavior == DirectorActivationBehavior.ExpireWithoutExecuting)
                    {
                        if(debugAllowLogs)
                        {
                            Debug.Log("Director Queue: Action " + key + " " + value.type + " expired without executing");
                        }
                    }
                    else if(debugAllowWarnings || debugAllowLogs)
                    {
                        Debug.LogWarning("Director Queue: Action " + key + " " + value.type + " has invalid activationBehavior value: " + value.activationBehavior + ". Treating as ExpireWithoutExecuting.");
                    }
                    keysToRemove.Add(key);
                    break;
            }
        }
        foreach (int key in keysToRemove)
        {
            queue.Remove(key);
            // LogQueue();
        }
    }
    
    private IEnumerator ActivateOnTone(Action action, int id = -1, string type = "(unknown type)")
    {
        if(debugAllowLogs)
        {
            Debug.Log("Director Queue: Action " + id + " " + type + " will activate when next tone begins");
        }
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
        if(debugAllowLogs)
        {
            Debug.Log("Director Queue: Action " + id + " " + type + " activating with tone");
        }

        try
        {
            if(action != null)
            {
                action();
            }
            else
            {
                if(debugAllowWarnings || debugAllowLogs)
                {
                    Debug.LogWarning("Director Queue: Action " + id + " " + type + " is null, cannot execute");
                }
            }
        }
        catch(System.Exception ex)
        {
            if(debugAllowWarnings || debugAllowLogs)
            {
                Debug.LogError("Director Queue: Exception executing action " + id + " " + type + ": " + ex.Message);
            }
        }
    }
    
    private IEnumerator ActivateQueueOnTone()
    {
        if(debugAllowLogs)
        {
            Debug.Log("Director Queue: Entire queue will activate when next tone begins");
        }
        try
        {
            // Check for null reference
            if(imitoneVoiceInterpreter == null)
            {
                if(debugAllowWarnings || debugAllowLogs)
                {
                    Debug.LogError("Director Queue: imitoneVoiceInterpreter is null, cannot wait for tone");
                }
                yield break;
            }
            
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
            //then activate the entire queue (only if queue is not empty)
            if(queue.Count > 0)
            {
                if(debugAllowLogs)
                {
                    Debug.Log("Director Queue: Activating entire queue with tone");
                }
                ActivateQueue();
            }
            else
            {
                if(debugAllowLogs)
                {
                    Debug.Log("Director Queue: Queue activation requested but queue is empty (may have been cleared)");
                }
            }
        }
        finally
        {
            // Always reset flag, even if coroutine is stopped or exception occurs
            activateQueueOnToneRunning = false;
            _activateQueueOnToneCoroutine = null;
        }
    }

    /// <param name="timeLimit">Seconds until timer-driven behavior runs; <c>null</c> means no expiry (stored as <see cref="UnboundedQueueTimeSeconds"/>).</param>
    public int AddActionToQueue(Action action, string type, bool isAudioAction, bool isVisualAction, float? timeLimit, DirectorActivationBehavior activationBehavior, DirectorExclusivityBehavior exclusivityBehavior = DirectorExclusivityBehavior.PreferShorterTimeRemaining)
    {
        float storedTimeLeft = timeLimit ?? UnboundedQueueTimeSeconds;

        // Validate parameters
        if(action == null)
        {
            if(debugAllowWarnings || debugAllowLogs)
            {
                Debug.LogWarning("Director Queue: Cannot add null action to queue");
            }
            return -1;
        }
        
        if(!Enum.IsDefined(typeof(DirectorActivationBehavior), activationBehavior))
        {
            if(debugAllowWarnings || debugAllowLogs)
            {
                Debug.LogWarning("Director Queue: Invalid activationBehavior " + activationBehavior + ". Using ExpireWithoutExecuting.");
            }
            activationBehavior = DirectorActivationBehavior.ExpireWithoutExecuting;
        }
        
        if(disable)
        {
            if(debugAllowWarnings || debugAllowLogs)
            {
                Debug.LogWarning("Director Queue: Director is disabled, not adding action to queue.");
            }
            return -1;
        }
        if(exclusivityBehavior == DirectorExclusivityBehavior.PreferShorterTimeRemaining)
        {
            if(SearchQueueForType(type))
            {
                if(timeLimit.HasValue)
                {
                    foreach (var item in queue)
                    {
                        if(item.Value.type == type)
                        {
                            if(item.Value.timeLeft <= timeLimit.Value)
                            {
                                if(debugAllowLogs)
                                {
                                    Debug.Log("Director Queue: Action " + type + " already exists in director queue with shorter timeLeft, not adding new one per exclusivity rules.");
                                }
                                // LogQueue();
                                return -1; //return -1 to indicate that the action was not added
                            }
                        }
                    }
                }
                ClearQueueOfType(type);
            }
        }
        else if(exclusivityBehavior == DirectorExclusivityBehavior.ReplaceAllOfType)
        {
            ClearQueueOfType(type);
        }
        queue.Add(queueIndex++, new DirectorQueueItem(action, type, isAudioAction, isVisualAction, storedTimeLeft, activationBehavior));

        if(debugAllowLogs)
        {
            Debug.Log("Director Queue: Added " + (queueIndex - 1) + " " + type + " to director queue.");
        }
        // LogQueue();

        return queueIndex - 1;
    }

    /// <summary>Unbounded time limit — same as passing <c>null</c> for <see cref="AddActionToQueue"/> time limit.</summary>
    public int AddActionToQueue(Action action, string type, bool isAudioAction, bool isVisualAction, DirectorActivationBehavior activationBehavior, DirectorExclusivityBehavior exclusivityBehavior = DirectorExclusivityBehavior.PreferShorterTimeRemaining)
    {
        return AddActionToQueue(action, type, isAudioAction, isVisualAction, null, activationBehavior, exclusivityBehavior);
    }

    
    public void ActivateQueue(float transitionTimeForFlourishes = 5.0f, bool tryActivateWhenEmpty = false)
    {
        int countAudioEvents = 0;
        int countVisualEvents = 0;

        if (disable)
        {
            if(debugAllowWarnings || debugAllowLogs)
            {
                Debug.LogWarning("Director Queue: Director is disabled, not activating queue.");
            }
            return;
        }

        // Early return if queue is empty
        bool localActivateWhenEmpty = tryActivateWhenEmpty && timeSinceLastActivation > activateWhenEmptyThreshold;
        if(queue.Count == 0 && !localActivateWhenEmpty)
        {
            if(debugAllowLogs)
            {
                Debug.Log("Director Queue: ActivateQueue called but queue is empty");
            }
            return;
        }

        // LogQueue();

        // Copy out the queue's items first
        var queuedItems = new List<DirectorQueueItem>(queue.Values);

        // The order of execution matters. 
        // By default, the queue is executed in the order it was added.
        // However, we prioritize fundamentalChange, SoundscapeShuffle, and ColorWorldShuffle to the front of the queue first.

        // Separate and reorder the queue: fundamentalChange, then SoundscapeShuffle, then ColorWorldShuffle, then all others
        var fundamentalChangeItems = queuedItems.Where(item => item.type == "fundamentalChange").ToList(); //prioritized so that this comes ahead of any instrument change
        var soundscapeShuffleItems = queuedItems.Where(item => item.type == "SoundscapeShuffle").ToList(); //prioritized so this comes ahead of any specific soundscape change
        var colorWorldShuffleItems = queuedItems.Where(item => item.type == "ColorWorldShuffle").ToList();
        var otherItems = queuedItems.Where(item => item.type != "fundamentalChange" && item.type != "SoundscapeShuffle" && item.type != "ColorWorldShuffle").ToList();
        queuedItems = new List<DirectorQueueItem>();
        queuedItems.AddRange(fundamentalChangeItems);
        queuedItems.AddRange(soundscapeShuffleItems);
        queuedItems.AddRange(colorWorldShuffleItems);
        queuedItems.AddRange(otherItems);

        // Now iterate over the COPY
        foreach (var item in queuedItems)
        {
            // Execute the action
            item.action();

            if (item.isAudioAction)
                countAudioEvents++;

            if (item.isVisualAction)
                countVisualEvents++;

            if(debugAllowLogs)
            {
                Debug.Log($"Director Queue: Action {item.type} executed from process-all");
            }
        }

        // Once done, we can safely clear the queue 
        // (which no longer breaks the iteration because we’re not iterating over the original dictionary)
        if (queuedItems.Count > 0 || localActivateWhenEmpty)
        {
            // If no audio events, do an audio flourish
            if (countAudioEvents == 0 && countVisualEvents != 0)
            {
                if(debugAllowLogs)
                {
                    Debug.Log("Director Queue: No Audio Actions Queued, triggering one to complete syncresis");
                }
                TweakAudio(transitionTimeForFlourishes);
                PlayTransitionSound();
            }

            // If no visual events, do a visual flourish
            if (countVisualEvents == 0 && countAudioEvents != 0)
            {
                if(debugAllowLogs)
                {
                    Debug.Log("Director Queue: No Visual Actions Queued, Triggering one to complete syncresis");
                }
                lightControl.NextPreferredColorWorld(transitionTimeForFlourishes);
                lightControl.FXWave(0.75f, 15.0f, 0.1f, true);
            }
        }
        timeSinceLastActivation = 0.0f;

        // Clear the dictionary at the end
        queue.Clear();
        // LogQueue();
    }

    public void LogQueue()
    {
        if(debugAllowLogs)
            Debug.Log(EditorFormatQueueContents());
    }

#if UNITY_EDITOR
    /// <summary>Single-line queue snapshot for <see cref="MusicDebugHarness"/> and playtests.</summary>
    public string EditorFormatQueueContents()
    {
        if (queue.Count == 0)
            return "Director Queue Contents: (empty)";
        var logString = "Director Queue Contents: ";
        foreach (var item in queue)
            logString += "<" + item.Key + " " + item.Value.type + ", " + item.Value.timeLeft.ToString("F2") + "s, " + item.Value.activationBehavior + "> ";
        return logString;
    }
#endif

    public bool SearchQueueForType(string type)
    {
        foreach (var item in queue)
        {
            if(item.Value.type == type)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>True if any queued item has expired and is awaiting whole-queue activation on the next tone.</summary>
    private bool AnyPendingActivation()
    {
        foreach (var item in queue)
        {
            if (item.Value.pendingActivation)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Cancels a pending whole-queue activation: stops the waiting coroutine, resets its guard, and clears
    /// the <see cref="DirectorQueueItem.pendingActivation"/> flags. Called on <see cref="Disable"/> and when a
    /// clear removes the last pending item. Retained items left in the queue re-arm on their next expiry.
    /// </summary>
    public void CancelPendingWholeQueueActivation()
    {
        bool hadPending = activateQueueOnToneRunning;

        var keys = new List<int>(queue.Keys);
        foreach (int key in keys)
        {
            var value = queue[key];
            if (value.pendingActivation)
            {
                value.pendingActivation = false;
                queue[key] = value;
            }
        }

        if (_activateQueueOnToneCoroutine != null)
        {
            StopCoroutine(_activateQueueOnToneCoroutine);
            _activateQueueOnToneCoroutine = null;
        }
        activateQueueOnToneRunning = false;

        if (hadPending && debugAllowLogs)
        {
            Debug.Log("Director Queue: Pending whole-queue activation cancelled.");
        }
    }

    public float ClearQueueOfType(string type) //returns the shortest time left of the cleared items, or -1 if nothing was cleared
    {
        float shortestTimeLeft = float.MaxValue;
        List<int> keysToRemove = new List<int>();
        foreach (var item in queue)
        {
            if(item.Value.type == type)
            {
                // Track shortest time
                if (item.Value.timeLeft < shortestTimeLeft)
                {
                    shortestTimeLeft = item.Value.timeLeft;
                }
                keysToRemove.Add(item.Key);
            }
        }
        foreach (int key in keysToRemove)
        {
            queue.Remove(key);
        }
        // LogQueue();
        if(debugAllowLogs)
        {
            Debug.Log("Director Queue: Removed all " + type + " items from director queue.");
        }
        // LogQueue();

        // If clearing removed the last item awaiting whole-queue activation, cancel the now-pointless coroutine.
        if (activateQueueOnToneRunning && !AnyPendingActivation())
            CancelPendingWholeQueueActivation();

        // Return shortest time (or -1 if nothing was cleared)
        return shortestTimeLeft == float.MaxValue ? -1f : shortestTimeLeft;
    }

    public int ReplaceActionInQueue(Action action, string newType, string oldType, bool isAudioAction, bool isVisualAction, float newMaximumTimeLimit, DirectorActivationBehavior activationBehavior)
    {
        // ReplaceActionInQueue clears actions of both oldType and newType, then adds the new action
        // The expiration time is set to the minimum of:
        // - shortestTimeLeft of cleared oldType actions
        // - shortestTimeLeft of cleared newType actions  
        // - newMaximumTimeLimit
        
        // Validate parameters
        if(action == null)
        {
            if(debugAllowWarnings || debugAllowLogs)
            {
                Debug.LogWarning("Director Queue: Cannot replace with null action");
            }
            return -1;
        }
        
        if(disable)
        {
            if(debugAllowWarnings || debugAllowLogs)
            {
                Debug.LogWarning("Director Queue: Director is disabled, not replacing action in queue.");
            }
            return -1;
        }
        
        // Clear old type and get shortest time left
        float shortestTimeOld = ClearQueueOfType(oldType);
        if(shortestTimeOld == -1f)
        {
            shortestTimeOld = float.MaxValue; // If nothing was cleared, use MaxValue so it doesn't affect the min calculation
        }
        
        // Clear new type and get shortest time left
        float shortestTimeNew = ClearQueueOfType(newType);
        if(shortestTimeNew == -1f)
        {
            shortestTimeNew = float.MaxValue; // If nothing was cleared, use MaxValue so it doesn't affect the min calculation
        }
        
        // Calculate the minimum expiration time
        float calculatedTimeLimit = Mathf.Min(shortestTimeOld, shortestTimeNew, newMaximumTimeLimit);
        
        // Add the new action with the calculated time limit
        int result = AddActionToQueue(action, newType, isAudioAction, isVisualAction, calculatedTimeLimit, activationBehavior, DirectorExclusivityBehavior.None);
        
        if(debugAllowLogs)
        {
            Debug.Log($"Director Queue: ReplaceActionInQueue cleared '{oldType}' and '{newType}', added '{newType}' with timeLimit={calculatedTimeLimit}s (min of old={shortestTimeOld}, new={shortestTimeNew}, max={newMaximumTimeLimit})");
        }
        
        return result;
    }

    //====================================================================================================
    //ACTIONS
    //====================================================================================================
    
    //PUBLIC
    public void PlayTransitionSound()
    {
        if (!canPlayTransitionSound)
        {
            if(debugAllowLogs)
            {
                Debug.Log("Director: Transition Sound requested but still on cooldown. Ignoring request.");
            }
            return;
        }
        
        AkSoundEngine.PostEvent("Unity_TransitionSFX", gameObject);
        if(debugAllowLogs)
        {
            Debug.Log("Director: Transition Sound Played");
        }
        
        // Start cooldown
        canPlayTransitionSound = false;
        
        // Stop existing cooldown coroutine if one is running
        if (transitionSoundCooldownCoroutine != null)
        {
            StopCoroutine(transitionSoundCooldownCoroutine);
        }
        
        // Start new cooldown coroutine
        transitionSoundCooldownCoroutine = StartCoroutine(TransitionSoundCooldown(5.0f));
    }
    
    private IEnumerator TransitionSoundCooldown(float _cooldownSeconds = 5.0f)
    {
        yield return new WaitForSeconds(_cooldownSeconds);
        canPlayTransitionSound = true;
        transitionSoundCooldownCoroutine = null;
        if(debugAllowLogs)
        {
            Debug.Log("Director: Transition Sound cooldown expired - can play again.");
        }
    }

    public Action Action_PlayTransitionSound()
    {
        return () => PlayTransitionSound();
    }

    //PRIVATE
    private Action Action_DirectorTest(string print)
    {
        return () => DirectorTest(print);
    }

    private void DirectorTest(string print)
    {
        if(debugAllowLogs)
        {
            Debug.Log("Director Test: " + print);
        }
    }

    private void TweakAudio(float _seconds)
    {
        audioTweakCounter++;

        float _rtpcTarget = audioTweakCounter % 2 == 0 ? 100.0f : 0.0f;
        int ms = (int)(_seconds * 1000.0f);
        
        AkSoundEngine.SetRTPCValue("Unity_SoundTweak", _rtpcTarget, gameObject, ms);
        if(debugAllowLogs)
        {
            Debug.Log("Director: Audio Tweak to " + _rtpcTarget + " in " + ms + "ms (this isn't in wwise yet, I think)");
        }
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
