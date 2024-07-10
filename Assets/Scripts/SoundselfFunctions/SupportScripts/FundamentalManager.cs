using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FundamentalManager : MonoBehaviour
{
    [SerializeField] private GameObject _fundamentalPrefab;
    public Action<Note> OnNewFundamentalSpawn;

    public ImitoneVoiceIntepreter _voiceInterpreter;
    //public ExampleImitoneBehavior _voiceInterpreter;
    private HarmonyManager _harmonyManager;

    private List<Note> _fundamentals = new List<Note>();
    
    [Header("Filling Note")]
    private Coroutine _fundamentalFillRoutine;
    [Tooltip("The size of the meter the user has to fill for a new fundamental to spawn. " +
             "\nIf fill rate is set to 1, then the meter's size is in Time.deltaTime.")]
    [SerializeField] private float _fundamentalMeterToFill = 5f;
    [Tooltip("The rate at which the meter fills per second." +
             "\nAt 2, the meter fills twice as fast as Time.deltaTime.")]
    [SerializeField] private float _fillRate = 1f;
    
    [Header("Offering")]
    [SerializeField] private float _offerTime = 5f;
    private Coroutine _offerRoutine;
    private Note _offeredFundamental;
    private float normalized_frequency;
    private void Awake()
    {
        _voiceInterpreter = GetComponent<ImitoneVoiceIntepreter>();
        //_voiceInterpreter = GetComponent<ExampleImitoneBehavior>();
        _harmonyManager = GetComponent<HarmonyManager>();
        _voiceInterpreter.OnNewTone += HandleNewToning;
        _harmonyManager.OnSeriesEnd += MakeOffer;
    }

    private void OnDestroy()
    {
        _voiceInterpreter.OnNewTone -= HandleNewToning;
        _harmonyManager.OnSeriesEnd -= MakeOffer;
    }

    /// <summary>
    /// When a new tone is started, begin filling the meter.
    /// </summary>
    private void HandleNewToning(float note)
    {
        ProcessFundamental(note);
    }
    
    private void ProcessFundamental(float pitch)
    {
        if (_fundamentalFillRoutine != null) StopCoroutine(_fundamentalFillRoutine);
            _fundamentalFillRoutine = StartCoroutine(FillNote(_fundamentalMeterToFill, pitch));
    }
    
    /// <summary>
    /// Fills the note while the user is toning, and decreases by half the rate when they're not. This routine gets stopped
    /// when the user begins toning a new note.
    /// </summary>
    private IEnumerator FillNote(float timeToFill, float note)
    {
        float fillTime = 0;
        while (fillTime < timeToFill)
        {
            if (_voiceInterpreter.imitoneActive)
            {
                fillTime += (_fillRate) * Time.deltaTime;
            }
            else
            {
                fillTime = Math.Max(0, fillTime - (_fillRate * 0.5f) * Time.deltaTime);
            }
            yield return null;
        }

        if (_offeredFundamental)
        {
            HandleNewNoteDuringOffer(note);
        }
        else
        {
            SpawnNewFundamental(note);
        }
        _fundamentalFillRoutine = null;
    }
    
    private void SpawnNewFundamental(float note)
    {
        //normalized_frequency = (_voiceInterpreter.pitch_hz - 220) / (880 - 220);
        Note newNote = Instantiate(_fundamentalPrefab).GetComponent<Note>();
        //TODO: what is starting pitch/volume
        Debug.Log(normalized_frequency);
        newNote.Initialize(note: _voiceInterpreter.note_st, pitch: _voiceInterpreter.pitch_hz, volume: 0 ,RemoveFromFundamentalList);
        OnNewFundamentalSpawn?.Invoke(newNote);
        HandleNewFundamental(newNote);
    }
    
    
    //TODO: testing offering notes
    public void MakeOffer()
    {
        float note = ChooseOffer();
        if (_offerRoutine != null) StopCoroutine(_offerRoutine);
        _offerRoutine = StartCoroutine(OfferFundamental(_offerTime, note));
    }

    private float ChooseOffer()
    {
        //TODO: how to choose the note to offer?
        return 0;
    }
    
    private IEnumerator OfferFundamental(float offerTime, float note)
    {
        _offeredFundamental = Instantiate(_fundamentalPrefab).GetComponent<Note>();
        //TODO: what is starting pitch/volume
        _offeredFundamental.Initialize(note: note, pitch: 0.5f, volume: 0, RemoveFromFundamentalList);
        
        //continue offer until reaching offerTime.
        float fillTime = 0;
        while (fillTime < offerTime)
        {
            fillTime += Time.deltaTime;
            yield return null;
        }
        _offeredFundamental.End();
        _fundamentalFillRoutine = null;
    }

    private void HandleNewNoteDuringOffer(float newNote)
    {
        if (_offerRoutine != null)
        {
            StopCoroutine(_offerRoutine);
            _offerRoutine = null;
        }
        //if offer is taken, replace fundamental
        if (Math.Abs(newNote - _offeredFundamental.NoteValue) < 0.001)
        {
            OnNewFundamentalSpawn?.Invoke(_offeredFundamental);
            HandleNewFundamental(_offeredFundamental);
            _offeredFundamental = null;
            
        }
        //otherwise stop offer
        else
        {
            _offeredFundamental.End();
        }
    }

    private void HandleNewFundamental(Note newFundamental)
    {
        for (int i = _fundamentals.Count-1; i >= 0; i--)
        {
            _fundamentals[i].End();
        }
        _fundamentals.Add(newFundamental);
    }

    private void RemoveFromFundamentalList(Note fundamental)
    {
        _fundamentals.Remove(fundamental);
        Destroy(fundamental.gameObject);
    }
}
