using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class is used to track the respiration rate of the user
public class RespirationTracker : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public float _respirationRate = 1.0f;    
    public float _respirationRateRaw    = 1.0f;
    private bool toneActiveForRespirationRate = false;
    private bool frameGuardTone = false;
    private float _positiveActiveThreshold = 1.2f;
    private float _negativeActiveThreshold = 0.75f;
    private float _respirationMeasurementWindow1 = 60.0f;
    private float _respirationMeasurementWindow2 = 120.0f;
    private int idCounter = 0;
    private Dictionary<int, BreathCycleData> BreathCycleDictionary = new Dictionary<int, BreathCycleData>();

    public struct BreathCycleData
    {
        public int window;
        public bool invalid;
        public float _weight;
        public float _cycleCount;
        public float _toneLength;
        public float _restLength;
        public float _cycleLength;
    }
    void Start()
    {
        
    }

    // Update is called once per frame

    void Update()
    {
        // Set toneActiveForRespirationRate
        if (ImitoneVoiceIntepreter._tThisTone > _positiveActiveThreshold)
        {
           //return the total of all the cycle counts in the dictionary:

            toneActiveForRespirationRate = true;
            if (!frameGuardTone)
            {
                // Start the coroutine to measure the duration of one tone/rest cycle, but do it just once per tone:
                Debug.Log("Start Respiration Cycle Coroutine");
                StartCoroutine(RespirationCycleCoroutine());
                frameGuardTone = true;
            }
        }
        else if (ImitoneVoiceIntepreter._tThisTone < _negativeActiveThreshold)
        {
            toneActiveForRespirationRate = false;
            frameGuardTone = false;
        }
    }

    private IEnumerator RespirationCycleCoroutine (){
        // This coroutine measures the duration of one tone/rest cycle.
        // It starts when toneActiveForRespirationRate is true.
        // It measures the duration of the tone, the next rest (when toneActiveForRespirationRate is false), and the full cycle (which ends the next time toneActiveForRespirationRate is true).
        // The measurements are stored in a dictionary, and cleared after the cycle exits the measurement window entirely.
        float _tAfterCycle = 0.0f;
        float _toneLength = 0.0f;
        float _restLength = 0.0f;
        float _cycleLength = 0.0f;

        idCounter++;
        int id = idCounter;

        //create a new dictionary entry for this coroutine cycle:
        BreathCycleData newBreathCycleData = new BreathCycleData();
        newBreathCycleData.window = 1;
        newBreathCycleData._weight = 1f;
        newBreathCycleData._cycleCount = 0.5f;
        
        //add the new dictionary entry to the dictionary:
        BreathCycleDictionary.Add(id, newBreathCycleData);
        Debug.Log("RespirationCycleCoroutine started <" + id + ">");
        UpdateRespirationRate();

        //Step 1: Measure the Tone
        while (toneActiveForRespirationRate == true)
        {
            _toneLength += Time.deltaTime;
            _cycleLength += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            newBreathCycleData = BreathCycleDictionary[id];

            // Update the object
            newBreathCycleData._toneLength = _toneLength;
            newBreathCycleData._cycleLength = _cycleLength;
            newBreathCycleData.invalid = _toneLength > 45.0f;

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = newBreathCycleData;

            //Debug.Log("id: " + id + " toning " + "toneLength: " + _toneLength + " _respirationRate: " + _respirationRate);

            yield return null;
        }

        newBreathCycleData = BreathCycleDictionary[id];
        newBreathCycleData._cycleCount = 1.0f;
        BreathCycleDictionary[id] = newBreathCycleData;
        Debug.Log("RespirationCycleCoroutine moving to Step 2 <" + id + ">");
        UpdateRespirationRate();

        //Step 2: Measure the Rest
        while (toneActiveForRespirationRate == false)
        {
            _restLength += Time.deltaTime;
            _cycleLength += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            newBreathCycleData = BreathCycleDictionary[id];

            // Update the object
            newBreathCycleData._restLength = _restLength;
            newBreathCycleData._cycleLength = _cycleLength;
            newBreathCycleData.invalid = (_restLength > 13.0f) || (_toneLength > 45.0f);

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = newBreathCycleData;

            //Debug.Log("id: " + id + " resting " + "restLength: " + _restLength + " _respirationRate: " + _respirationRate);

            yield return null;
        }

        Debug.Log("RespirationCycleCoroutine moving to Step 3 <" + id + ">");


        //Step 3: Measure the time since the cycle ended, and fade it out of memory
        while (_tAfterCycle < _respirationMeasurementWindow1)
        {
            _tAfterCycle += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            newBreathCycleData = BreathCycleDictionary[id];

            // Update the object
            newBreathCycleData._cycleCount = Mathf.Clamp((_respirationMeasurementWindow1 - _tAfterCycle) / Mathf.Max(_cycleLength, 1.0f), 0.0f, 1.0f);

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = newBreathCycleData;

            //Debug.Log("id: " + id + " waiting " + " cycleLength: " + _cycleLength + " _respirationRate: " + _respirationRate);

            yield return null;
        }

        //Check if this dictionary entry is invalid
        newBreathCycleData = BreathCycleDictionary[id];
        bool invalid = false;
        invalid = newBreathCycleData.invalid;

        //Remove the dictionary entry:
        Debug.Log("RespirationCycleCoroutine ended <" + id + ">");
        BreathCycleDictionary.Remove(id);

        //If the dictionary entry is invalid, update the respiration rate
        if (invalid)
        {
            Debug.Log("Invalid breaty cycle ending. Updating respiration rate. <" + id + ">");
            UpdateRespirationRate();
        }
    }

    private void UpdateRespirationRate()
    {
        // Set _respirationRate to the total of all the cycle counts in the dictionary:
        _respirationRate = 0.0f;

        //return -1 if there are any invalid entries in the dictionary
        bool invalid = false;
        foreach (KeyValuePair<int, BreathCycleData> entry in BreathCycleDictionary)
        {
            if (entry.Value.invalid)
            {
                invalid = true;
                break;
            }
        }
        if (invalid)
        {
            _respirationRate = -1.0f;

            foreach (KeyValuePair<int, BreathCycleData> entry in BreathCycleDictionary)
            {
                _respirationRateRaw += entry.Value._cycleCount;
            }
        }
        else
        {
            foreach (KeyValuePair<int, BreathCycleData> entry in BreathCycleDictionary)
            {
                _respirationRate += entry.Value._cycleCount;
                _respirationRateRaw += entry.Value._cycleCount;
            }
        }
        Debug.Log("Updated respiration rate. Raw: " + _respirationRateRaw + " Standard: " + _respirationRate);
    }
}