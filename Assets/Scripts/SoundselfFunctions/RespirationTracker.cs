using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//This class is used to track the respiration rate of the user

//TODO:
// mean tone length and mean rest length must disclude invalid cycles. They are currently behaving weirdly, see CSV.
// Then add the Absorption rate.

public class RespirationTracker : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    private bool debugAllowLogs = false;
    public float _respirationRate       {get; private set;} = 1.0f;   
    public float _respirationRateRaw        {get; private set;} = 1.0f; //uses either the 1min or 2min version, depending on validity, preferrring 1min
    public float _respirationRateMostRecentValid {get; private set;} = 1.0f; //this only updates if it is valid
    public float _respirationRateRaw1min    {get; private set;} = -1.0f;
    public float _respirationRateRaw2min    {get; private set;} = -1.0f;
    public float _respirationRate1min       {get; private set;} = -1.0f;
    public float _respirationRate2min       {get; private set;} = -1.0f;
    public bool toneActiveForRespirationRate {get; private set;} = false;
    public float _meanToneLength {get; private set;} = 0.0f;    //this only updates if it is valid
    public float _meanToneLength1min {get; private set;} = 0.0f;
    public float _meanToneLength2min {get; private set;} = 0.0f;
    public float _meanRestLength {get; private set;} = 0.0f;    //this only updates if it is valid
    public float _meanRestLength1min {get; private set;} = 0.0f;
    public float _meanRestLength2min {get; private set;} = 0.0f;
    public float _meanCycleLength {get; private set;} = 0.0f;       //this only updates if it is valid
    public float _meanCycleLength1min {get; private set;} = 0.0f;
    public float _meanCycleLength2min {get; private set;} = 0.0f;
    public float _standardDeviationTone1min {get; private set;} = 0.0f;
    public float _standardDeviationTone2min {get; private set;} = 0.0f;
    public float _standardDeviationRest1min {get; private set;} = 0.0f;
    public float _standardDeviationRest2min {get; private set;} = 0.0f;
    public float _absorptionRespirationRateMultiplier1min {get; private set;} = 0.0f;
    public float _absorptionRespirationRateMultiplier2min {get; private set;} = 0.0f;
    public float _absorptionToneLengthMultiplier1min {get; private set;} = 0.0f;
    public float _absorptionToneLengthMultiplier2min {get; private set;} = 0.0f;
    public float _absorption {get; private set;} = 0.0f;
    public float _absorptionRaw {get; private set;} = 0.0f;
    public float _absorptionMostRecentValid {get; private set;} = 0.0f; //this only updates if it is valid
    private bool frameGuardTone = false;
    public bool modePlayful     = true;
    public bool modeMeditative  = false;
    private float _respirationMeasurementWindow1 = 60.0f;
    private float _respirationMeasurementWindow2 = 120.0f;
    private int idCounter = 0;
    private int logGuard = 0;
    private Vector3 startingPoint;
    private float amountToScale;
    private float amountToMove;
    private float debugStackY = -100f;
    public Canvas canvas;  
    public GameObject rectanglePrefab;
    public GameObject SelectedRectangle;
    private Dictionary<int, BreathCycleData> BreathCycleDictionary1 = new Dictionary<int, BreathCycleData>();
    private Dictionary<int, BreathCycleData> BreathCycleDictionary2 = new Dictionary<int, BreathCycleData>();


    public struct BreathCycleData
    {
        public int window;
        public bool invalid;
        public float _weight;
        public float _cycleCount;
        public float _toneLength;
        public float _restLength;
        public float _tAfterCycle;
        public float _cycleLength;
        public GameObject inhaleRectangle;
        public GameObject exhaleRectangle;
    }
    void Start()
    {
        amountToScale = 0.03f;
        amountToMove = 0.03f;
        startingPoint = new Vector3(0, transform.position.y, transform.position.z);
    }


    // Update is called once per frame

    void Update()
    {
        //if(Input.GetKeyDown(KeyCode.K))
        //{
        //    debugAllowLogs = !debugAllowLogs;
        //}
        if (imitoneVoiceInterpreter.toneActiveVeryConfidentRaw)
        {
           //return the total of all the cycle counts in the dictionary:
            toneActiveForRespirationRate = true;
            if (!frameGuardTone)
            {
                // Start the coroutine to measure the duration of one tone/rest cycle, but do it just once per tone:
                //Debug.Log("Start Respiration Cycle Coroutine");
                StartCoroutine(RespirationCycleCoroutine(BreathCycleDictionary1,_respirationMeasurementWindow1, true, 0));
                StartCoroutine(RespirationCycleCoroutine(BreathCycleDictionary2,_respirationMeasurementWindow2, true, 1));
                frameGuardTone = true;
            }
        }
        else
        {
            toneActiveForRespirationRate = false;
            frameGuardTone = false;
        }

        //Set the various respiration rate values for the 1min and 2min windows
        _respirationRateRaw1min = GetRespirationRateRaw(BreathCycleDictionary1, _respirationMeasurementWindow1);
        _respirationRateRaw2min = GetRespirationRateRaw(BreathCycleDictionary2, _respirationMeasurementWindow2);
        _respirationRate1min = GetRespirationRate(BreathCycleDictionary1, _respirationMeasurementWindow1);
        _respirationRate2min = GetRespirationRate(BreathCycleDictionary2, _respirationMeasurementWindow2);
        _meanToneLength1min = GetMeanLength(BreathCycleDictionary1, true);
        _meanRestLength1min = GetMeanLength(BreathCycleDictionary1, false);
        _meanToneLength2min = GetMeanLength(BreathCycleDictionary2, true);
        _meanRestLength2min = GetMeanLength(BreathCycleDictionary2, false);
        _meanCycleLength1min = _meanToneLength1min + _meanRestLength1min;
        _meanCycleLength2min = _meanToneLength2min + _meanRestLength2min;
        _standardDeviationTone1min = GetStandardDeviation(BreathCycleDictionary1, true);
        _standardDeviationRest1min = GetStandardDeviation(BreathCycleDictionary1, false);
        _standardDeviationTone2min = GetStandardDeviation(BreathCycleDictionary2, true);
        _standardDeviationRest2min = GetStandardDeviation(BreathCycleDictionary2, false);
        _absorptionRespirationRateMultiplier1min = Mathf.Lerp(1f, 0.75f, AbsorptionRespirationRateComponent(_respirationRate1min));
        _absorptionRespirationRateMultiplier2min = Mathf.Lerp(1f, 0.75f, AbsorptionRespirationRateComponent(_respirationRate2min));
        _absorptionToneLengthMultiplier1min = Mathf.Lerp(0.5f, 1f, AbsorptionToneLengthComponent(_meanToneLength1min));
        _absorptionToneLengthMultiplier2min = Mathf.Lerp(0.5f, 1f, AbsorptionToneLengthComponent(_meanToneLength2min));

        //Update the game-chosen respiration values based on the validity of the 1min and 2min windows
        if ((_respirationRate1min == -1f) && (_respirationRate2min != -1f))
        {
            //THIS WILL ONLY HAPPEN IF AT LEAST ONE OF THE WINDOWS IS VALID
            if (logGuard != 2) //this should happen once, on the first frame that the 2min window is valid
            {
                if(debugAllowLogs)
                {
                    Debug.Log("Switching to respiration rate with a " + _respirationMeasurementWindow2 + " second window");
                }
                logGuard = 2;
            }
            _respirationRate = _respirationRate2min;
            _respirationRateRaw = _respirationRateRaw2min;
            _meanToneLength = _meanToneLength2min;
            _meanRestLength = _meanRestLength2min;
            _meanCycleLength = _meanCycleLength2min;
            _absorptionRaw = Absorption(_respirationRateRaw2min, _standardDeviationTone2min, _standardDeviationRest2min, _meanToneLength2min, _meanRestLength2min, _absorptionRespirationRateMultiplier2min, _absorptionToneLengthMultiplier2min);
            _absorption = _absorptionRaw; //absorptionRaw will, in this if statement, be the same as absorption            
        }
        else
        {
            if (logGuard != 1)//this should happen once, on the first frame that the 1min window is valid
            {
                if(debugAllowLogs)
                {
                    Debug.Log("Switching to respiration rate with a " + _respirationMeasurementWindow1 + " second window");
                }
                logGuard = 1;
            }
    
            _respirationRate = _respirationRate1min;
            _respirationRateRaw = _respirationRateRaw1min;
            _absorptionRaw = Absorption(_respirationRateRaw1min, _standardDeviationTone1min, _standardDeviationRest1min, _meanToneLength1min, _meanRestLength1min, _absorptionRespirationRateMultiplier1min, _absorptionToneLengthMultiplier1min);
            
            //absorption should be -1 if not valid.
            if (_respirationRate == -1f)
            _absorption = -1f;
            else
            {
                _absorption = _absorptionRaw;

                //variables that should only change if they are valid
                _meanToneLength = _meanToneLength1min;
                _meanRestLength = _meanRestLength1min;
                _meanCycleLength = _meanCycleLength1min;
            }
        }

        if (_respirationRate != -1f)            
        _respirationRateMostRecentValid = _respirationRate;
        if (_absorption != -1f)
        _absorptionMostRecentValid = _absorption;
        
        
        //Debug Visualizations
        GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag("oldrect");
        // Loop through each GameObject and apply the position change
        foreach (GameObject obj in objectsWithTag)
        {
            obj.transform.position = new Vector3(obj.transform.position.x + amountToMove, obj.transform.position.y, obj.transform.position.z);
        }

        //Playfulness Switch
        if (_absorption > 0.25f && modePlayful)
        {
            modePlayful = false;
            modeMeditative = true;
            AkSoundEngine.SetState("AbsorptionMode", "Meditative");
            Debug.Log("Switching to Meditative Mode");

        }
        else if (_absorption < 0.1f && modeMeditative)
        {
            modePlayful = true;
            modeMeditative = false;
            AkSoundEngine.SetState("AbsorptionMode", "Playful");
            Debug.Log("Switching to Playful Mode");
        }
        AkSoundEngine.SetRTPCValue("Unity_Absorption", Mathf.Clamp(_absorption, 0f, 1f) * 100f , gameObject);

    }

    private IEnumerator RespirationCycleCoroutine (Dictionary<int,BreathCycleData> BreathCycleDictionary, float _measurementWindow = 60.0f, bool visualize = true, int visualizeY = 0){
        // This coroutine measures the duration of one tone/rest cycle.
        // It starts when toneActiveForRespirationRate is true.
        // It measures the duration of the tone, the next rest (when toneActiveForRespirationRate is false), and the full cycle (which ends the next time toneActiveForRespirationRate is true).
        // The measurements are stored in a dictionary, and cleared after the cycle exits the measurement window entirely.
        float _age = 0.0f;
        float _tAfterCycle = 0.0f;
        float _toneLength = imitoneVoiceInterpreter._activeThreshold3;
        float _restLength = 0.0f;
        float _cycleLength = 0.0f;
        bool isFirstInvalidFrame = true; // turns false after the first invalid frame.

        idCounter++;
        int id = idCounter;

        //create a new dictionary entry for this coroutine cycle:
        BreathCycleData thisBreathCycleData = new BreathCycleData();
        thisBreathCycleData.window = 1;
        thisBreathCycleData._weight = 1f;

        if(visualize)
        {
            thisBreathCycleData.inhaleRectangle = Instantiate(rectanglePrefab, startingPoint - new Vector3(0.0f, 400.0f + debugStackY*visualizeY, 0.0f), Quaternion.identity, canvas.transform);
            thisBreathCycleData.exhaleRectangle = Instantiate(rectanglePrefab, startingPoint - new Vector3(0.0f, 400.0f+debugStackY*visualizeY, 0.0f), Quaternion.identity, canvas.transform);
        }
        
        //add the new dictionary entry to the dictionary:
        BreathCycleDictionary.Add(id, thisBreathCycleData);
        //UpdateRespirationRate();

        float _initialMeanToneLength = _meanToneLength;

        if(debugAllowLogs)
        {
            Debug.Log("RespirationCycleCoroutine started <"+ visualizeY + ":" + id + "> (fade in time = " + _initialMeanToneLength + " seconds");
        }

    
        //Step 1: Measure the Tone
        while (toneActiveForRespirationRate == true)
        {
            _toneLength += Time.deltaTime;
            _cycleLength += Time.deltaTime;
            _age += Time.deltaTime;
            
            // Get the BreathCycleData object from the dictionary
            thisBreathCycleData = BreathCycleDictionary[id];
            // set thisBreathCycleData._cycleCount to gradually lerp from 0 to 0.5 over _initialMeanToneLength seconds
            if (_initialMeanToneLength > 0.0f)
            thisBreathCycleData._cycleCount = Mathf.Clamp(_toneLength / _initialMeanToneLength, 0.0f, 1.0f) * 0.5f;
            else
            thisBreathCycleData._cycleCount = 0.5f;

            if(debugAllowLogs)
            {
                Debug.Log("_cycleCount for id <" + id + "> is " + thisBreathCycleData._cycleCount + " and _initialMeanToneLength is " + _initialMeanToneLength);
            }


            // Update the object
            thisBreathCycleData._toneLength = _toneLength;
            thisBreathCycleData._cycleLength = _cycleLength;
            if(!thisBreathCycleData.invalid)
            {
                thisBreathCycleData.invalid = !imitoneVoiceInterpreter.gameOn || ((_toneLength > 45.0f) || (_cycleLength > (_measurementWindow / 2)));

                if (thisBreathCycleData.invalid)
                {
                    if(debugAllowLogs)
                    {
                        Debug.Log("id: " + id + " is invalid");
                    }
                }
            }
          
            if (thisBreathCycleData.invalid && isFirstInvalidFrame)
            {
                if(debugAllowLogs)
                {
                    Debug.Log("id: " + id + " is invalid");
                }
                isFirstInvalidFrame = false; // Set isFirstInvalidFrame to false after the first invalid frame
            }

            if(visualize)
            {
                SelectedRectangle = thisBreathCycleData.inhaleRectangle;
                Image imageComponent = SelectedRectangle.GetComponent<Image>(); // Get the Image component
                if(imageComponent != null)
                {
                    imageComponent.color = Color.blue;
                }
                RectTransform rectTransform = SelectedRectangle.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + amountToScale, rectTransform.sizeDelta.y);
                }
                if(thisBreathCycleData.invalid)
                {
                    imageComponent.color = new Color(0f, 0f, 0.3f, 1f); // R, G, B, A
                }
            }

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = thisBreathCycleData; 

            if(debugAllowLogs)
            {
                Debug.Log("id: " + id + " toning " + "toneLength: " + _toneLength + " _respirationRate: " + _respirationRate);
            }


            yield return null;
        }

        thisBreathCycleData = BreathCycleDictionary[id];

        if(visualize) {thisBreathCycleData.inhaleRectangle.tag = "oldrect";}
        BreathCycleDictionary[id] = thisBreathCycleData;

        thisBreathCycleData._cycleCount = 0.5f;
        
        // Put the modified object back into the dictionary
        BreathCycleDictionary[id] = thisBreathCycleData; 

        if(debugAllowLogs)
        {
            Debug.Log("RespirationCycleCoroutine moving to Step 2 <" + id + ">");
        }

        //Step 2: Measure the Rest
        float _initialMeanRestLength = _meanRestLength;
        _restLength = imitoneVoiceInterpreter._activeThreshold3;

        while (toneActiveForRespirationRate == false)
        {
            _restLength += Time.deltaTime;
            _cycleLength += Time.deltaTime;
            _age += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            thisBreathCycleData = BreathCycleDictionary[id];

            // set thisBreathCycleData._cycleCount to gradually lerp from 0.5 to 1 over _meanRestLength seconds
            if (_initialMeanRestLength > 0.0f)
            thisBreathCycleData._cycleCount = 0.5f + Mathf.Clamp(_restLength / _initialMeanRestLength, 0.0f, 1.0f) * 0.5f;
            else
            thisBreathCycleData._cycleCount = 1.0f;


            // Update the object
            thisBreathCycleData._restLength = _restLength;
            thisBreathCycleData._cycleLength = _cycleLength;
            if(!thisBreathCycleData.invalid)
            thisBreathCycleData.invalid = !imitoneVoiceInterpreter.gameOn || ((_restLength > 13.0f) || (_cycleLength > (_measurementWindow / 2)));
            if (thisBreathCycleData.invalid && isFirstInvalidFrame)
            {
                if(debugAllowLogs)
                {
                    Debug.Log("id: " + id + " is invalid");
                }
                isFirstInvalidFrame = false; // Set isFirstInvalidFrame to false after the first invalid frame
            }

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = thisBreathCycleData; 

            // Do debug visualizations
            if (visualize)
            {
                SelectedRectangle = thisBreathCycleData.exhaleRectangle;
                Image imageComponent = SelectedRectangle.GetComponent<Image>(); // Get the Image component
                if(imageComponent != null)
                {
                    imageComponent.color = Color.green;
                }
                RectTransform rectTransform = SelectedRectangle.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + amountToScale, rectTransform.sizeDelta.y);
                }

                if(thisBreathCycleData.invalid)
                {
                // This is an example of a dark blue color. You might need to adjust the values to get the exact shade you want.
                    imageComponent.color = new Color(0f, 0.3f, 0f, 1f); // R, G, B, A
                }
            }
            
            if(debugAllowLogs)
            {
                Debug.Log("id: " + id + " resting " + "restLength: " + _restLength + " _respirationRate: " + _respirationRate);
            }
            yield return null;
        }

        // Get the BreathCycleData object from the dictionary
        thisBreathCycleData = BreathCycleDictionary[id];

        thisBreathCycleData._cycleCount = 1.0f;

        if(debugAllowLogs)
        {
            Debug.Log("RespirationCycleCoroutine moving to Step 3 <" + id + ">");
        }

        if(visualize){thisBreathCycleData.exhaleRectangle.tag = "oldrect";}

        // Put the modified object back into the dictionary
        BreathCycleDictionary[id] = thisBreathCycleData; 

        float previousCycleCount = 1.0f;

        //Step 3: Measure the time since the cycle ended, and fade it out of memory
        while (thisBreathCycleData._cycleCount > 0.0f)
        {
            
            _tAfterCycle += Time.deltaTime;
            _age += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            thisBreathCycleData = BreathCycleDictionary[id];

            // Update the object and fade it from memory
            thisBreathCycleData._cycleCount = Mathf.Clamp((_measurementWindow - _tAfterCycle) / Mathf.Max(_cycleLength, 1.0f), 0.0f, 1.0f);
            thisBreathCycleData._tAfterCycle = _tAfterCycle;
            
            // Do debug visualizations
            if(visualize) //@REEF, for another glimpse at how this bug works, switch this if statement with the one below, and see how the rectangles behave
            //if(false)
            {
                RectTransform rectTransforminhale = thisBreathCycleData.inhaleRectangle.GetComponent<RectTransform>();
                RectTransform rectTransformexhale = thisBreathCycleData.exhaleRectangle.GetComponent<RectTransform>();
                if (rectTransforminhale != null)
                {
                    rectTransforminhale.sizeDelta = new Vector2(rectTransforminhale.sizeDelta.x, thisBreathCycleData._cycleCount*100.0f);
                }
                if (rectTransformexhale != null)
                {
                    rectTransformexhale.sizeDelta = new Vector2(rectTransformexhale.sizeDelta.x, thisBreathCycleData._cycleCount*100.0f);
                }
            }

            if (Mathf.Floor(thisBreathCycleData._cycleCount * 10) != Mathf.Floor(previousCycleCount * 10))
            {
                if(debugAllowLogs)
                {
                    Debug.Log("id: " + id + "_cycleCount: " + thisBreathCycleData._cycleCount);
                }
            }

            // Update the previous cycle count
            previousCycleCount = thisBreathCycleData._cycleCount;
            
            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = thisBreathCycleData; 
            
            yield return null;
        }

        //if we are visualizing, then destroy the created game objects
        if(visualize)
        {
            Destroy(thisBreathCycleData.inhaleRectangle);
            Destroy(thisBreathCycleData.exhaleRectangle);
        }

        //Remove the dictionary entry:
        if(debugAllowLogs)
        {
            Debug.Log("RespirationCycleCoroutine <" + id + "> ended with cycle count: " + thisBreathCycleData._cycleCount + " and age: " + _age + " and cycle length: " + _cycleLength);
        }
       

        BreathCycleDictionary.Remove(id);
    }

    private float GetRespirationRateRaw(Dictionary<int, BreathCycleData> BreathCycleDictionary, float _window)
    {
        // Set _respirationRate to the total of all the cycle counts in the dictionary:
        float _respirationCount = 0.0f;
        //_respirationRateRaw = 0.0f;


       
        foreach (KeyValuePair<int, BreathCycleData> entry in BreathCycleDictionary)
        {
            _respirationCount += entry.Value._cycleCount;
        }

        return _respirationCount / _window * 60.0f;
    }

        
    private float GetRespirationRate(Dictionary<int, BreathCycleData> breathCycleDictionary, float _window)
    {
            //first check for invalid breath cycles
            //bool invalid = false;
            float _respirationMeasured = 0.0f;

            foreach (KeyValuePair<int, BreathCycleData> entry in breathCycleDictionary)
            {
                _respirationMeasured += entry.Value._cycleLength;
                if (entry.Value.invalid)
                {
                    return -1.0f;
                }
            }

            if (_respirationMeasured < _window)
            {
                return -1.0f;
            }
            else
            return GetRespirationRateRaw(breathCycleDictionary, _window);
    }

    private float GetMeanLength(Dictionary<int, BreathCycleData> breathCycleDictionary, bool getTone)
    {
        float sumLength = 0.0f;
        int countCompletedCycles = 0;

        foreach (KeyValuePair<int, BreathCycleData> entry in breathCycleDictionary)
        {
            if (getTone)
            {
                if (!entry.Value.invalid && (entry.Value._restLength > 0f))
                {
                    sumLength += entry.Value._toneLength;
                    countCompletedCycles++;
                }
            }
            else
            {
                if (!entry.Value.invalid && (entry.Value._tAfterCycle > 0f))
                {
                    sumLength += entry.Value._restLength;
                    countCompletedCycles++;
                }
            }
        }

        return sumLength / Mathf.Max(countCompletedCycles, 1);
    }

    private float GetStandardDeviation(Dictionary<int, BreathCycleData> breathCycleDictionary, bool getTone)
    {
        float meanLength = GetMeanLength(breathCycleDictionary, getTone);
        float sumSquaredDeviations = 0.0f;
        int countCompletedCycles = 0;

        foreach (KeyValuePair<int, BreathCycleData> entry in breathCycleDictionary)
        {
            if (!entry.Value.invalid && ((getTone && entry.Value._restLength > 0f) || (!getTone && entry.Value._tAfterCycle > 0f)))
            {
                float deviation = (getTone ? entry.Value._toneLength : entry.Value._restLength) - meanLength;
                sumSquaredDeviations += deviation * deviation;
                countCompletedCycles++;
            }
        }

        float variance = sumSquaredDeviations / Mathf.Max(countCompletedCycles, 1);
        float standardDeviation = Mathf.Sqrt(variance);

        return standardDeviation;
    }

    private float Absorption(float respirationRate, float toneDeviation, float restDeviation, float toneMean, float restMean, float respirationRateMultiplier, float toneLengthMultiplier)
    {
        //first work with standard deviations
        float value1 = toneDeviation / toneMean * 10f;
        float value2 = restDeviation / restMean * 9f; //original mult was 10f
        float value3 = Mathf.Log(Mathf.Max(value1, value2, 0.1f), 2f);
        float value4 = Mathf.Clamp(Mathf.InverseLerp(3f, -2.237f, value3), 0f, 1f); //original value was 3f, -2.737

        //then work with respiration rate, to reduce the absorption rate if the respiration rate is too high or the tones too short
        float value7 = Mathf.Min(respirationRateMultiplier, toneLengthMultiplier);

        //return the multiple of the two values
        float absorption = value4 * value7;
        return absorption;
    }

    private float AbsorptionRespirationRateComponent(float respirationRate)
    {
        //as respirationRate increases, absorption will decrease. Output 0-1 value, to be applied negatively to the absorption value.
        return Mathf.Clamp(Mathf.InverseLerp(3f, 7f, respirationRate), 0f, 1f);
    }

    private float AbsorptionToneLengthComponent(float toneMean)
    {
        //as tone length increases, absorption increases. Output 0-1 value.
        return Mathf.Clamp(Mathf.InverseLerp(4f, 8f, toneMean), 0f, 1f);
    }
}