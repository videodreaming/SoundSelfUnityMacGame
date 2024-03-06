using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HarmonyManager : MonoBehaviour
{
    //TODO: testing
    public HarmonyBehavior Harmony { get; private set; }
    public Action OnSeriesEnd;
    
    [SerializeField] private GameObject _notePrefab;
    [SerializeField] private float _harmonyProgressionTime = 10f;
    private Coroutine _harmonySeriesRoutine;
    
    private FundamentalManager _fundamentalManager;
 
    private void Awake()
    {
        _fundamentalManager = GetComponent<FundamentalManager>();
        // OnNewNoteSpawn is invoked when a new fundamental is created, via player toning.
        _fundamentalManager.OnNewFundamentalSpawn += EndHarmony;
    }
    
    private void OnDestroy()
    {
        _fundamentalManager.OnNewFundamentalSpawn -= EndHarmony;
    }

    //TODO: determine how progression of harmonies is chosen
    /// <summary>
    /// ruleSet.progression: Starts a new harmony progression with the fundamental, and a series of harmony notes that sound good with it. The series passed in should
    /// not include the fundamental note itself (unless two of the same note is wanted).
    /// </summary>
    /// <param name="fundamental">The currently toning fundamental.</param>
    /// <param name="harmonies">Arbitrary number of parameters, each in the form of an array of floats. These are the possible
    /// harmony notes for the currently toning fundamental, not including the fundamental note.</param>
    public void StartNewHarmonySeries(Note fundamental, params float[][] harmonies)
    {
        if (_harmonySeriesRoutine != null) StopCoroutine(_harmonySeriesRoutine);
        _harmonySeriesRoutine = StartCoroutine(ProgressThroughSeries(fundamental, harmonies));
    }

    /// <summary>
    /// ruleSet.harmonize: create chord out of fundamental, the user's voice (as a Note), and an arbitrary number of other notes.
    /// </summary>
    /// <param name="fundamental">The current playing fundamental.</param>
    /// <param name="voice">The player's voice to playback, as a Note.</param>
    /// <param name="harmonies">Arbitrary number of notes to include in the harmony</param>
    public void StartVoiceHarmony(Note fundamental, Note voice, params Note[] harmonies)
    {
        //TODO: when there is voice playback
    }
    
    /// <summary>
    /// ruleSet.follow: follow the tones made by user.
    /// </summary>
    /// <param name="fundamental">The current playing fundamental.</param>
    /// <param name="harmonies">Arbitrary number of other notes to include.</param>
    public void StartRandomHarmony(Note fundamental, params Note[] harmonies)
    {
        //TODO: where to get tones..?
    }

    private void EndHarmonySeries()
    {
        if (_harmonySeriesRoutine != null) StopCoroutine(_harmonySeriesRoutine);
        _harmonySeriesRoutine = null;
    }

    /// <summary>
    /// Create a new harmony, stopping the old one.
    /// </summary>
    private void CreateNewHarmony(Note[] newHarmony)
    {
        Harmony?.End(newHarmony);
        Harmony = new HarmonyBehavior(newHarmony);
    }
    
    /// <summary>
    /// Goes through the series of harmonies, each time creating a new Harmony with the fundamental included. At the end of
    /// the series, invokes the onSeriesEnd event.
    /// </summary>
    private IEnumerator ProgressThroughSeries(Note fundamental, float[][] harmonies)
    {
        foreach (float[] harmony in harmonies)
        {
            //create the full harmony with the fundamental included.
            List<Note> notes = harmony.Select(n =>
            {
                Note note = Instantiate(_notePrefab).GetComponent<Note>();
                note.Initialize(n, 0, 0);
                return note;
            }).ToList();
            notes.Add(fundamental);
            
            CreateNewHarmony(notes.ToArray());
            yield return new WaitForSeconds(_harmonyProgressionTime);
        }
        
        OnSeriesEnd?.Invoke();
    }
    
    /// <summary>
    /// End the current harmony when a new fundamental is spawned, via player toning.
    /// </summary>
    private void EndHarmony(Note newFundamental)
    {
        EndHarmonySeries();
        Harmony?.End();
    }
}