using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class is used to track the respiration rate of the user
public class RespirationTracker : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public float _respirationRate = 1.0f;    
    private bool toneActiveForRespirationRate = false;
    private float _positiveActiveThreshold = 1.2f;
    private float _negativeActiveThreshold = 0.75f;
    private float _respirationMeasurementWindow1 = 60.0f;
    private float _respirationMeasurementWindow2 = 120.0f;
    private int idCounter1 = 0;
    private int idCounter2 = 0;
    private Dictionary<int, BreathCycleData> BreathCycleDictionary1 = new Dictionary<int, BreathCycleData>();

    private struct BreathCycleData
    {
        private int id;
        private int invalid;
        private float _weight;
        private float _cycleCount;
        private float _toneLength;
        private float _restLength;
        private float _cycleLength;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Set toneActiveForRespirationRate
        if (ImitoneVoiceIntepreter._tThisTone > _positiveActiveThreshold)
        {
            toneActiveForRespirationRate = true;
        }
        else if (ImitoneVoiceIntepreter._tThisTone < _negativeActiveThreshold)
        {
            toneActiveForRespirationRate = false;
        }
    }

    //Set up a coroutine using the BreathCycleDictionary, that will trigger a new instance of the coroutine every time toneActiveForRespirationRate is set to true.
    //Each instance of the coroutine is designed to measure information about one cycle of tone/rest. It will measure the duration in seconds of the tone(toneActiveForRespirationRate == true), of the rest following the tone(toneActiveForRespirationRate == false), and of the whole cycle (the sum of both durations), and will send these floats to the dictionary.
    //The instance will end when the the rest it was measuring ended more than _respirationMeasurementWindow1 ago (because we are using this to measure the respiration rate per second)
    //the other values to send to the dictionary work like this:
    //- id should be a unique identifier for the cycle. We can use the number of cycles that have been measured so far.
    //- the key should be 1
    //- _weight is 1
    //- invalid is set to 1 if there is something wrong with the measurement: if the rest length is greater than 13 seconds, or if the tone length is greater than 45 seconds. This is to prevent the measurement of a cycle that is not a respiration cycle.
    //- _cycleCount is used to count the number of cycles in the measurement window. It is set to 0.5 when the coroutine is started, then it is set to 1 when the tone ends and we begin measuring the rest. We then diminish the value as the cycle exits the measurement window. So if the cycle is 60 seconds long, and the cycle is 38 seconds long, and the rest ended 55 seconds ago, then we will set to the _cycleCount to equal the portion of the measurement that is in the measurement window: in this case, (60 - 55) / 38.
    //here is the code:
    //NOTE TO SELF, ASK FOR LESS FOR THE FIRST VERION, THEN ADD PIECES TO IT!
}