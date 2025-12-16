using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WorldShuffler : MonoBehaviour
{
    public MusicSystem1 musicSystem1;
    public LightControl lightControl;
    public RespirationTracker respirationTracker;
    public Director director;
    private int debugWorldCount = 0;
    public bool shuffling {get; private set;} = false;
    public bool musicQueueOpen = true;
    private bool waiting = false; // for when a world change has been added to the director queue, but not triggered yet.
    private float _shuffleInterval = 120.0f; //will be effectively half this if absorption is 0. After this much time, world shuffle will be added to the director queue.
    private float _shuffleTimer = 0.0f;

    //options available for shuffling
    // Dictionaries to store available options and their availability status
    public Dictionary<string, bool> availableMusicWorlds = new Dictionary<string, bool>
    {
        { "Gentle", true },
        { "Shadow", true },
        { "Shruti", true },
        { "SonoFlore", true }
    };

    public Dictionary<string, bool> availableColorWorlds = new Dictionary<string, bool>
    {
        { "Red", true },
        { "Blue", true },
        { "White", true }
    };

    private string currentMusicWorld;
    private string currentColorWorld;

    void Awake ()
    {}

    void Start ()
    {
    }

    void Update ()
    {
        //in playground mode, when I press the M button, cycle to the next music world (Gentle, Shadow, Shruti, Sonoflore)
        
        if(shuffling)
        {
            if(!waiting)
            {
                _shuffleTimer += (Time.deltaTime * Mathf.Pow(2, (1-Mathf.Clamp(respirationTracker._absorption, 0, 1))));
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
        ShuffleMusicWorld();
        ShuffleColorWorld();
        StartTimer();
    }
    private void ShuffleMusicWorld()
    {
        if (!shuffling)
        {
            Debug.LogWarning("WorldShuffler: Attempted to shuffle music world, but shuffling is disabled.");
            return;
        }
        //Shuffle the music world to a random available music world, excluding the current one
        List<string> availableWorlds = new List<string>();
        foreach(KeyValuePair<string, bool> entry in availableMusicWorlds)
        {
            if(entry.Value && entry.Key != currentMusicWorld)
            {
                availableWorlds.Add(entry.Key);
            }
        }
        if(availableWorlds.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableWorlds.Count);
            musicSystem1.SetSoundWorld(availableWorlds[randomIndex]);
            director.PlayTransitionSound();
        }
        else
        {
            Debug.LogError("WorldShuffler: No available music worlds to shuffle to.");
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
        List<string> availableWorlds = new List<string>();
        foreach(KeyValuePair<string, bool> entry in availableColorWorlds)
        {
            if(entry.Value && entry.Key != currentColorWorld)
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
        if(musicQueueOpen)
        {
            director.AddActionToQueue(Action_ShuffleSoundWorld(), "SoundWorld", true, false, _seconds, true, 1);
        }
        else
        {
            Debug.LogWarning("WorldShuffler: Attempted to queue sound world shuffle, but music queue is closed.");
        }
        director.AddActionToQueue(Action_ShuffleColorWorld(), "ColorPreference", false, true, _seconds, true, 1);
        
        WaitForNextShuffle();
    }

    private Action Action_ShuffleSoundWorld()
    {
        return () => ShuffleMusicWorld();
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
            musicQueueOpen = true;
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
        if(shuffling)
        {
            shuffling = false;
            musicQueueOpen = false;
            _shuffleTimer = 0.0f;
            Debug.Log("WorldShuffler: Stopping shuffle.");
        }
        else
        {
            Debug.LogWarning("WorldShuffler: Attempted to stop shuffling, but shuffling is already inactive.");
        }
    }

    public void CloseMusicQueue()
    {
        musicQueueOpen = false;
    }


    public void ExcludeMusicWorld(string world)
    {
        Debug.Log("WorldShuffler: Excluding Music World -" + world + "- from shuffle.");
        //Exclude a music world from the shuffle, turning it "false" in the dictionary. If the world is not correctly named, log an error
        if(availableMusicWorlds.ContainsKey(world))
        {
            availableMusicWorlds[world] = false;
        }
        else
        {
            Debug.LogError("WorldShuffler: Cannot exclude Music World -" + world + "- as that is not a valid music world name.");
        }
    }

    public void ExcludeColorWorld(string world)
    {
        Debug.Log("WorldShuffler: Excluding Color World -" + world + "- from shuffle.");
        //Exclude a color world from the shuffle, turning it "false" in the dictionary. If the world is not correctly named, log an error
        if(availableColorWorlds.ContainsKey(world))
        {
            availableColorWorlds[world] = false;
        }
        else
        {
            Debug.LogError("WorldShuffler: Cannot exclude Color World -" + world + "- as that is not a valid color world name.");
        }
    }

    public void ResetMusicWorlds()
    {
        Debug.Log("WorldShuffler: Resetting all music worlds to be available for shuffling.");
        //Reset all music worlds to be available for shuffling
        Dictionary<string, bool> updatedMusicWorlds = new Dictionary<string, bool>(availableMusicWorlds);
        foreach(KeyValuePair<string, bool> entry in availableMusicWorlds)
        {
            updatedMusicWorlds[entry.Key] = true;
        }
        availableMusicWorlds = updatedMusicWorlds;
    }

    public void ResetColorWorlds()
    {
        Debug.Log("WorldShuffler: Resetting all color worlds to be available for shuffling.");
        //Reset all color worlds to be available for shuffling
        Dictionary<string, bool> updatedColorWorlds = new Dictionary<string, bool>(availableColorWorlds);
        foreach(KeyValuePair<string, bool> entry in availableColorWorlds)
        {
            updatedColorWorlds[entry.Key] = true;
        }
        availableColorWorlds = updatedColorWorlds;
    }

    
    public void SetCurrentMusicWorld(string world)
    {
        currentMusicWorld = world;
    }

    public void SetCurrentColorWorld(string world)
    {
        currentColorWorld = world;
    }

    public void ClearCurrentMusicWorld()
    {
        currentMusicWorld = "";
    }
    public void ClearCurrentColorWorld()
    {
        currentColorWorld = "";
    }

}