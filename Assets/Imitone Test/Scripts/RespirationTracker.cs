using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class is used to track the respiration rate of the user
public class RespirationTracker : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public float _respirationRate = 1.0f;    
    private bool toneActiveForRespirationRate = false;
    private bool coroutineGuard = false;
    private float _positiveActiveThreshold = 1.2f;
    private float _negativeActiveThreshold = 0.75f;
    private float _respirationMeasurementWindow1 = 60.0f;
    private float _respirationMeasurementWindow2 = 120.0f;
    private int idCounter = 0;
    private Dictionary<int, BreathCycleData> BreathCycleDictionary = new Dictionary<int, BreathCycleData>();

    public struct BreathCycleData
    {
        public int window;
        public int invalid;
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
           
            toneActiveForRespirationRate = true;
            if (!coroutineGuard)
            {
                // Start the coroutine to measure the duration of one tone/rest cycle, but do it just once per tone:
                StartCoroutine(RespirationCycleCoroutine());
                coroutineGuard = true;
            }
        }
        else if (ImitoneVoiceIntepreter._tThisTone < _negativeActiveThreshold)
        {
            toneActiveForRespirationRate = false;
            coroutineGuard = false;
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

        //Step 1: Measure the Tone
        while (toneActiveForRespirationRate == true)
        {
            _toneLength += Time.deltaTime;
            _cycleLength += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            BreathCycleData data1 = BreathCycleDictionary[id];

            // Update the object
            data1._toneLength = _toneLength;
            data1._cycleLength = _cycleLength;

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = data1;

            Debug.Log("id: " + id + " toning " + "toneLength: " + _toneLength + " cycleLength: " + _cycleLength);

            yield return null;
        }

        BreathCycleData data2 = BreathCycleDictionary[id];
        data2._cycleCount = 1.0f;
        BreathCycleDictionary[id] = data2;

        //Step 2: Measure the Rest
        while (toneActiveForRespirationRate == false)
        {
            _restLength += Time.deltaTime;
            _cycleLength += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            BreathCycleData data = BreathCycleDictionary[id];

            // Update the object
            data._restLength = _restLength;
            data._cycleLength = _cycleLength;

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = data;

            Debug.Log("id: " + id + " resting " + "restLength: " + _restLength + " cycleLength: " + _cycleLength);

            yield return null;
        }

        //Step 3: Measure the time since the cycle ended, and fade it out of memory
        while (_tAfterCycle < _respirationMeasurementWindow1)
        {
            _tAfterCycle += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            BreathCycleData data = BreathCycleDictionary[id];

            // Update the object
            data._cycleCount = Mathf.Clamp((_respirationMeasurementWindow1 - _tAfterCycle) / Mathf.Max(_cycleLength, 1.0f), 0.0f, 1.0f);

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = data;

            Debug.Log("id: " + id + " waiting " + "cycleCount: " + BreathCycleDictionary[id]._cycleCount + " cycleLength: " + _cycleLength);

            yield return null;
        }

        //Remove the dictionary entry:
        Debug.Log("RespirationCycleCoroutine ended <" + id + ">");
        BreathCycleDictionary.Remove(id);
    }
}