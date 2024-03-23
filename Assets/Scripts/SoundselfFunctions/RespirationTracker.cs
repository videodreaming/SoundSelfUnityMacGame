using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//This class is used to track the respiration rate of the user

//TODO:
// mean tone length and mean rest length must disclude invalid cycles. They are currently behaving weirdly, see CSV.

public class RespirationTracker : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public float _respirationRate       = 1.0f;   
    public float _respirationRateRaw        = 1.0f; //uses either the 1min or 2min version, depending on validity, preferrring 1min
    public float _respirationRateRaw1min    = -1.0f;
    public float _respirationRateRaw2min    = -1.0f;
    public float _respirationRate1min       = -1.0f;
    public float _respirationRate2min       = -1.0f;
    public bool toneActiveForRespirationRate = false;
    public float _meanToneLength = 0.0f;
    public float _meanToneLength1min = 0.0f;
    public float _meanToneLength2min = 0.0f;
    public float _meanRestLength = 0.0f;
    public float _meanRestLength1min = 0.0f;
    public float _meanRestLength2min = 0.0f;
    private bool frameGuardTone = false;
    public bool pauseGuardTone  = false;
    private float _positiveActiveThreshold = 0.75f;
    private float _negativeActiveThreshold = 0.75f;
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
        //if(Input.GetKey(KeyCode.R))
        if (ImitoneVoiceIntepreter._tThisTone > _positiveActiveThreshold)
        {
           //return the total of all the cycle counts in the dictionary:
            toneActiveForRespirationRate = true;
            if (!frameGuardTone && !pauseGuardTone)
            {
                // Start the coroutine to measure the duration of one tone/rest cycle, but do it just once per tone:
                Debug.Log("Start Respiration Cycle Coroutine");
                StartCoroutine(RespirationCycleCoroutine(BreathCycleDictionary1,_respirationMeasurementWindow1, true, 0));
                StartCoroutine(RespirationCycleCoroutine(BreathCycleDictionary2,_respirationMeasurementWindow2, true, 1));
                frameGuardTone = true;
            }
        }
        else if (ImitoneVoiceIntepreter._tThisRest > _negativeActiveThreshold)
        //if(!Input.GetKey(KeyCode.R))
        {
            toneActiveForRespirationRate = false;
            frameGuardTone = false;
        }

        //Set the respiration rate values for the 1min and 2min windows
        _respirationRateRaw1min = GetRespirationRateRaw(BreathCycleDictionary1, _respirationMeasurementWindow1);
        _respirationRateRaw2min = GetRespirationRateRaw(BreathCycleDictionary2, _respirationMeasurementWindow2);

        _respirationRate1min = GetRespirationRate(BreathCycleDictionary1, _respirationMeasurementWindow1);
        _respirationRate2min = GetRespirationRate(BreathCycleDictionary2, _respirationMeasurementWindow2);

        //NOTE FOR ROBIN: GetMeanLength is definitely not working right
        _meanToneLength1min = GetMeanLength(BreathCycleDictionary1, true);
        _meanRestLength1min = GetMeanLength(BreathCycleDictionary1, false);
        _meanToneLength2min = GetMeanLength(BreathCycleDictionary2, true);
        _meanRestLength2min = GetMeanLength(BreathCycleDictionary2, false);

        //Update the game-chosen respiration rate values based on the validity of the 1min and 2min windows
        if ((_respirationRate1min == -1f) && (_respirationRate2min != -1f))
        {
            if (logGuard != 2) //this should happen once, on the first frame that the 2min window is valid
            {
                Debug.Log("Switching to respiration rate with a " + _respirationMeasurementWindow2 + " second window");
                logGuard = 2;
            }
            _respirationRate = _respirationRate2min;
            _respirationRateRaw = _respirationRateRaw2min;
            _meanToneLength = _meanToneLength2min;
            _meanRestLength = _meanRestLength2min;
        }
        else
        {
            if (logGuard != 1)//this should happen once, on the first frame that the 1min window is valid
            {
                Debug.Log("Switching to respiration rate with a " + _respirationMeasurementWindow1 + " second window");
                logGuard = 1;
            }
    
            _respirationRate = _respirationRate1min;
            _respirationRateRaw = _respirationRateRaw1min;
            _meanToneLength = _meanToneLength1min;
            _meanRestLength = _meanRestLength1min;
        }
        
        //Debug Visualizations
        GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag("oldrect");
        // Loop through each GameObject and apply the position change
        foreach (GameObject obj in objectsWithTag)
        {
            obj.transform.position = new Vector3(obj.transform.position.x + amountToMove, obj.transform.position.y, obj.transform.position.z);
        }
    }

    private IEnumerator RespirationCycleCoroutine (Dictionary<int,BreathCycleData> BreathCycleDictionary, float _measurementWindow = 60.0f, bool visualize = true, int visualizeY = 0){
        // This coroutine measures the duration of one tone/rest cycle.
        // It starts when toneActiveForRespirationRate is true.
        // It measures the duration of the tone, the next rest (when toneActiveForRespirationRate is false), and the full cycle (which ends the next time toneActiveForRespirationRate is true).
        // The measurements are stored in a dictionary, and cleared after the cycle exits the measurement window entirely.
        float _age = 0.0f;
        float _tAfterCycle = 0.0f;
        float _toneLength = _positiveActiveThreshold;
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

        Debug.Log("RespirationCycleCoroutine started <" + id + "> (fade in time = " + _initialMeanToneLength + " seconds");
    
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

            //Debug.Log("_cycleCount for id <" + id + "> is " + thisBreathCycleData._cycleCount + " and _initialMeanToneLength is " + _initialMeanToneLength);

            // Update the object
            thisBreathCycleData._toneLength = _toneLength;
            thisBreathCycleData._cycleLength = _cycleLength;
            if(!thisBreathCycleData.invalid)
            {
                thisBreathCycleData.invalid = pauseGuardTone || ((_toneLength > 45.0f) || (_cycleLength > (_measurementWindow / 2)));

                if (thisBreathCycleData.invalid)
                {
                    Debug.Log("id: " + id + " is invalid");
                }
            }
          
            if (thisBreathCycleData.invalid && isFirstInvalidFrame)
            {
                Debug.Log("id: " + id + " is invalid");
                isFirstInvalidFrame = false; // Set isFirstInvalidFrame to false after the first invalid frame
            }

            if(visualize)
            {
                SelectedRectangle = thisBreathCycleData.inhaleRectangle;
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
                    imageComponent.color = new Color(0f, 0.3f, 0f, 1f); // R, G, B, A
                }
            }

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = thisBreathCycleData; 

            //Debug.Log("id: " + id + " toning " + "toneLength: " + _toneLength + " _respirationRate: " + _respirationRate);

            yield return null;
        }

        thisBreathCycleData = BreathCycleDictionary[id];

        if(visualize) {thisBreathCycleData.inhaleRectangle.tag = "oldrect";}
        BreathCycleDictionary[id] = thisBreathCycleData;

        thisBreathCycleData._cycleCount = 0.5f;
        
        // Put the modified object back into the dictionary
        BreathCycleDictionary[id] = thisBreathCycleData; 

        Debug.Log("RespirationCycleCoroutine moving to Step 2 <" + id + ">");

        //Step 2: Measure the Rest
        float _initialMeanRestLength = _meanRestLength;
        _restLength = _negativeActiveThreshold;
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
            thisBreathCycleData.invalid = pauseGuardTone || ((_restLength > 13.0f) || (_cycleLength > (_measurementWindow / 2)));
            if (thisBreathCycleData.invalid && isFirstInvalidFrame)
            {
                Debug.Log("id: " + id + " is invalid");
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
                    imageComponent.color = Color.blue;
                }
                RectTransform rectTransform = SelectedRectangle.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + amountToScale, rectTransform.sizeDelta.y);
                }

                if(thisBreathCycleData.invalid)
                {
                // This is an example of a dark blue color. You might need to adjust the values to get the exact shade you want.
                    imageComponent.color = new Color(0f, 0f, 0.3f, 1f); // R, G, B, A
                }
            }
            
            //Debug.Log("id: " + id + " resting " + "restLength: " + _restLength + " _respirationRate: " + _respirationRate);

            yield return null;
        }

        // Get the BreathCycleData object from the dictionary
        thisBreathCycleData = BreathCycleDictionary[id];

        thisBreathCycleData._cycleCount = 1.0f;

        Debug.Log("RespirationCycleCoroutine moving to Step 3 <" + id + ">");
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
                Debug.Log("id: " + id + "_cycleCount: " + thisBreathCycleData._cycleCount);
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
        Debug.Log("RespirationCycleCoroutine <" + id + "> ended with cycle count: " + thisBreathCycleData._cycleCount + " and age: " + _age + " and cycle length: " + _cycleLength);

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
            bool invalid = false;
            float _respirationMeasured = 0.0f;

            foreach (KeyValuePair<int, BreathCycleData> entry in breathCycleDictionary)
            {
                _respirationMeasured += entry.Value._cycleLength;
                if (entry.Value.invalid)
                {
                    return -1.0f;
                    break;
                }
            }

            if (_respirationMeasured < _window)
            {
                return -1.0f;
            }
            else
            return GetRespirationRateRaw(breathCycleDictionary, _window);
    }

    private float GetMeanLength(Dictionary<int, BreathCycleData> breathCycleDictionary, bool isTone)
    {
        float sumLength = 0.0f;
        int countCompletedCycles = 0;

        foreach (KeyValuePair<int, BreathCycleData> entry in breathCycleDictionary)
        {
            if (isTone)
            {
                sumLength += entry.Value._toneLength;
                if (entry.Value._cycleCount >= 0.5f)
                {
                    countCompletedCycles++;
                }
            }
            else
            {
                sumLength += entry.Value._restLength;
                if (entry.Value._cycleCount >= 1.0f)
                {
                    countCompletedCycles++;
                }
            }
        }

        return sumLength / Mathf.Max(countCompletedCycles, 1);
    }
}