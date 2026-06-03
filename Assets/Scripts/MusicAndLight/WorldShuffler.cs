using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WorldShuffler : MonoBehaviour
{
    // =============================================================================
    // TODO (AI — read before refactoring WorldShuffler or Director color integration)
    // =============================================================================
    // When this class is next refactored together with the Director queue / flourish
    // system, also fold in the LightControl color-world simplification plan (discuss with
    // Robin first — do not implement unilaterally).
    //
    // Current inelegance (LightControl + Director + this class):
    // - LightControl keeps both preferredColor and currentColorWorld (PreferredColorWorld),
    //   but SetPreferredColor always applies immediately, so they rarely differ; nothing
    //   outside LightControl reads preferredColor. Variant presets (Red1/2/3) live in
    //   cycleRed/Blue/White instead. WorldShuffler uses a separate field also named
    //   currentColorWorld — shuffle bookkeeping only, easy to confuse with LightControl.
    // - Director "visual flourish" calls NextPreferredColorWorld() without SetPreferredColor,
    //   which is the main reason preferred vs current still matters.
    //
    // Proposed direction (needs design agreement):
    // - Single source of truth on LightControl (e.g. activeColorWorld); remove preferredColor.
    // - Rename APIs: SetColorWorld (apply now) vs CycleVariantWithinCurrentWorld (flourish).
    // - Rename WorldShuffler tracker (e.g. lastShuffledColorWorld) to avoid name collision.
    // - Optional: explicit activePreset string instead of opaque cycle counters.
    //
    // Soundscape shuffle exclusions (fold into same refactor; discuss with Robin):
    // - Replace global bool flags on availableSoundscapes (ExcludeSoundscape sets false with
    //   no record of who excluded what) with a list or dictionary of soundscapes NOT to
    //   shuffle, keyed/named by the caller/site that set the exclusion (e.g. "OpeningStage",
    //   "PlaygroundStep2") so that site can clear only its own exclusions when done.
    // - ShuffleSoundscape builds the eligible set from all soundscapes minus the union of
    //   active exclusions; avoid orphaned false flags after stage exit.
    // - If no soundscape remains eligible (all excluded or invalid), fall back to a
    //   pre-defined backup list (agree contents with Robin) rather than logging an error
    //   and skipping shuffle.
    //
    // See also: LightControl.SetPreferredColor / NextPreferredColorWorld / CycleColor,
    // Director.ProcessQueue visual flourish branch, PlaygroundStageHandler color queue actions,
    // OpeningStageHandler / PlaygroundStageHandler ExcludeSoundscape call sites.
    // =============================================================================

    public MusicSystem1 musicSystem1;
    public LightControl lightControl;
    public RespirationTracker respirationTracker;
    public Director director;
    private int debugWorldCount = 0;
    public bool shuffling {get; private set;} = false;
    private bool soundscapeQueueOpen = true; 
    private bool waiting = false; // for when a world change has been added to the director queue, but not triggered yet.
    private float _shuffleInterval = 120.0f; //will be effectively half this if absorption is 0. After this much time, world shuffle will be added to the director queue.
    private float _shuffleTimer = 0.0f;

    //options available for shuffling
    // Dictionaries to store available options and their availability status
    public Dictionary<string, bool> availableSoundscapes = new Dictionary<string, bool>
    {
        { "Gentle", true },
        { "Shadow", true },
        { "Shruti", true },
        { "SonoFlore", true }
    };

    public Dictionary<PreferredColorWorld, bool> availableColorWorlds = new Dictionary<PreferredColorWorld, bool>
    {
        { PreferredColorWorld.Red, true },
        { PreferredColorWorld.Blue, true },
        { PreferredColorWorld.White, true }
    };

    private string currentSoundscape;

#if UNITY_EDITOR
    /// <summary>Harness / editor diagnostics only.</summary>
    public string EditorCurrentSoundscape => currentSoundscape ?? "";
#endif
    private PreferredColorWorld? currentColorWorld;

    void Awake ()
    {
    }

    void Start ()
    {
    }

    void Update ()
    {
        //in playground mode, when I press the M button, cycle to the next soundscape (Gentle, Shadow, Shruti, SonoFlore)
        
        if(shuffling)
        {
            if(!waiting)
            {
                _shuffleTimer += (Time.deltaTime * Mathf.Pow(2, (1-Mathf.Clamp(RespirationTracker.instance._absorption, 0, 1))));
                if(_shuffleTimer >= _shuffleInterval)
                {
                    Debug.Log("WorldShuffler: Time to shuffle worlds.");
                    QueueWorldShuffle();
                }
            }
        }
    }

    
    //====================================================================================================
    //WORLD CONTROLS
    //====================================================================================================
    public void ShuffleWorldsNow()
    {
        ShuffleSoundscape();
        ShuffleColorWorld();
        StartTimer();
    }
    private void ShuffleSoundscape()
    {
        if (!shuffling)
        {
            Debug.LogWarning("WorldShuffler: Attempted to shuffle soundscape, but shuffling is disabled.");
            return;
        }
        //Shuffle the soundscape to a random available soundscape, excluding the current one
        List<string> availableWorlds = new List<string>();
        foreach(KeyValuePair<string, bool> entry in availableSoundscapes)
        {
            if(entry.Value && entry.Key != currentSoundscape)
            {
                availableWorlds.Add(entry.Key);
            }
        }
        if(availableWorlds.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableWorlds.Count);
            musicSystem1.SetSoundscape(availableWorlds[randomIndex]);
            director.PlayTransitionSound();
        }
        else
        {
            Debug.LogError("WorldShuffler: No available soundscapes to shuffle to.");
        }
        StartTimer();
    }

    private void ShuffleColorWorld(float transitionTimeSec = 2f)
    {
        
        if (!shuffling)
        {
            Debug.LogWarning("WorldShuffler: Attempted to shuffle color world, but shuffling is disabled.");
            return;
        }

        //Shuffle the color world to a random available color world, excluding the current one
        List<PreferredColorWorld> availableWorlds = new List<PreferredColorWorld>();
        foreach(KeyValuePair<PreferredColorWorld, bool> entry in availableColorWorlds)
        {
            if(entry.Value && (!currentColorWorld.HasValue || entry.Key != currentColorWorld.Value))
            {
                availableWorlds.Add(entry.Key);
            }
        }
        if(availableWorlds.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableWorlds.Count);
            lightControl.SetPreferredColor(availableWorlds[randomIndex], transitionTimeSec);
        }
        else
        {
            Debug.LogError("WorldShuffler: No available color worlds to shuffle to.");
        }
        StartTimer();
    }

    private void StartTimer()
    {
        Debug.Log("WorldShuffler: Starting shuffle timer.");
        _shuffleTimer = 0.0f;
        waiting = false;
    }

    private void WaitForNextShuffle()
    {
        Debug.Log("WorldShuffler: Waiting for next shuffle to begin timer.");
        _shuffleTimer = 0.0f;
        waiting = true;
    }

    //====================================================================================================
    //DIRECTOR QUEUE
    //====================================================================================================

    private void QueueWorldShuffle(float _seconds = 120.0f)
    {
        
        Debug.Log("WorldShuffler: Queuing World Shuffle");
        if(soundscapeQueueOpen)
        {
            if(!director.SearchQueueForType("Soundscape"))
            {
                director.AddActionToQueue(Action_ShuffleSoundscape(), "SoundscapeShuffle", true, false, _seconds, DirectorActivationBehavior.ActivateEntireQueueOnNextTone, DirectorExclusivityBehavior.PreferShorterTimeRemaining);
            }
            else
            {
                Debug.LogWarning("WorldShuffler: Attempted to queue SoundscapeShuffle, but director queue already has a specific SoundScape in it.");
            }
        }
        else
        {
            Debug.LogWarning("WorldShuffler: Attempted to queue sound world shuffle, but music queue is closed.");
        }
        director.AddActionToQueue(Action_ShuffleColorWorld(), "ColorWorldShuffle", false, true, _seconds, DirectorActivationBehavior.ActivateEntireQueueOnNextTone, DirectorExclusivityBehavior.PreferShorterTimeRemaining);
        
        WaitForNextShuffle();
    }

    private Action Action_ShuffleSoundscape()
    {
        return () => ShuffleSoundscape();
    }

    private Action Action_ShuffleColorWorld()
    {
        return () => ShuffleColorWorld();
    }


    //====================================================================================================
    //PUBLIC FUNCTIONS
    //====================================================================================================
    public void BeginShuffle(bool shuffleNow = true)
    {
        if(!shuffling)
        {
            shuffling = true;
            soundscapeQueueOpen = true;
            if(shuffleNow)
            {
                Debug.Log("WorldShuffler: Beginning shuffle immediately.");
                ShuffleWorldsNow();
            }
            else
            {
                Debug.Log("WorldShuffler: Beginning shuffle with director queue.");
                QueueWorldShuffle(120f);
            }
        }
        else
        {
            Debug.LogWarning("WorldShuffler: Attempted to begin shuffling, but shuffling is already active.");
        }
    }

    public void StopShuffle()
    {
        // Always clear queued shuffle actions from the director queue, regardless of shuffling state
        // This ensures cleanup even if shuffling was already stopped
        director.ClearQueueOfType("SoundscapeShuffle");
        director.ClearQueueOfType("ColorWorldShuffle");
        
        if(shuffling)
        {
            shuffling = false;
            soundscapeQueueOpen = false;
            _shuffleTimer = 0.0f;
            Debug.Log("WorldShuffler: Stopping shuffle.");
        }
        else
        {
            Debug.LogWarning("WorldShuffler: Attempted to stop shuffling, but shuffling is already inactive.");
        }
    }

    public void CloseSoundscapeQueue()
    {
        soundscapeQueueOpen = false;
    }


    public void ExcludeSoundscape(string world)
    {
        Debug.Log("WorldShuffler: Excluding Soundscape -" + world + "- from shuffle.");
        //Exclude a soundscape from the shuffle, turning it "false" in the dictionary. If the world is not correctly named, log an error
        if(availableSoundscapes.ContainsKey(world))
        {
            availableSoundscapes[world] = false;
        }
        else
        {
            Debug.LogError("WorldShuffler: Cannot exclude Soundscape -" + world + "- as that is not a valid soundscape name in the shuffler.");
        }
    }

    public void ExcludeColorWorld(PreferredColorWorld world)
    {
        Debug.Log("WorldShuffler: Excluding Color World -" + world + "- from shuffle.");
        if(availableColorWorlds.ContainsKey(world))
        {
            availableColorWorlds[world] = false;
        }
        else
        {
            Debug.LogError("WorldShuffler: Cannot exclude Color World -" + world + "- as that is not a valid color world name.");
        }
    }

    public void ResetSoundscapeExclusions()
    {
        Debug.Log("WorldShuffler: Resetting all soundscapes to be available for shuffling.");
        //Reset all soundscapes to be available for shuffling
        Dictionary<string, bool> updatedAvailableSoundscapes = new Dictionary<string, bool>(availableSoundscapes);
        foreach(KeyValuePair<string, bool> entry in availableSoundscapes)
        {
            updatedAvailableSoundscapes[entry.Key] = true;
        }
        availableSoundscapes = updatedAvailableSoundscapes;
    }

    public void ResetColorWorlds()
    {
        Debug.Log("WorldShuffler: Resetting all color worlds to be available for shuffling.");
        //Reset all color worlds to be available for shuffling
        Dictionary<PreferredColorWorld, bool> updatedColorWorlds = new Dictionary<PreferredColorWorld, bool>(availableColorWorlds);
        foreach(KeyValuePair<PreferredColorWorld, bool> entry in availableColorWorlds)
        { 
            updatedColorWorlds[entry.Key] = true;
        }
        availableColorWorlds = updatedColorWorlds;
    }

    
    public void SetCurrentSoundscape(string world) //this is to tell the shuffler where we are now, it does not change the music
    {
        currentSoundscape = world;
    }

    public void SetCurrentColorWorld(PreferredColorWorld world) //this is to tell the shuffler where we are now, it does not change the color.
    {
        currentColorWorld = world;
    }

    public void ClearCurrentSoundscape()
    {
        currentSoundscape = "";
    }
    public void ClearCurrentColorWorld()
    {
        currentColorWorld = null;
    }

}