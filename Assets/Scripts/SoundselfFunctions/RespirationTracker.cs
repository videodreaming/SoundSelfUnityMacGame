using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//This class is used to track the respiration rate of the user
public class RespirationTracker : MonoBehaviour
{
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public float _respirationRate = 1.0f;    
    public float _respirationRateRaw    = 1.0f;
    public bool toneActiveForRespirationRate = false;
    private bool frameGuardTone = false;
    private float _positiveActiveThreshold = 0.75f;
    private float _negativeActiveThreshold = 0.75f;
    private float _respirationMeasurementWindow1 = 60.0f;
    private float _respirationMeasurementWindow2 = 120.0f;
    private float _respirationMeasured   = 0.0f;
    private bool insufficientData = true;
    private int idCounter = 0;
    private bool invalidFlag = false;
    private Vector3 startingPoint;
    private float amountToScale;
    private float amountToMove;
    public Canvas canvas;  
    public GameObject rectanglePrefab;
    public GameObject SelectedRectangle;
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
        public GameObject inhalerectangle;
        public GameObject exhalerectangle;
    }
    void Start()
    {
        amountToScale = 0.05f;
        amountToMove = 0.05f;
        startingPoint = new Vector3(0, transform.position.y, transform.position.z);
    }

    //TO DO: 
    // 1. make the count measurements fade in, based on median tone lengths and breath lengths
    // 2. add a dictionary for the longer measurement window.
    // 3. fade respiration rate as used by the game between the two, depending on if one is invalid (i.e. toning for a really long time or resting for a really long time should switch the respiration rate to the longer window)

    // Update is called once per frame

    void Update()
    {
        if(Input.GetKey(KeyCode.R))
        //if (ImitoneVoiceIntepreter._tThisTone > _positiveActiveThreshold)
        {
           //return the total of all the cycle counts in the dictionary:
            toneActiveForRespirationRate = true;
            if (!frameGuardTone)
            {
                // Start the coroutine to measure the duration of one tone/rest cycle, but do it just once per tone:
                Debug.Log("Start Respiration Cycle Coroutine");
                StartCoroutine(RespirationCycleCoroutine(_respirationMeasurementWindow1, true));
                frameGuardTone = true;
            }
        }
        //else if (ImitoneVoiceIntepreter._tThisRest > _negativeActiveThreshold)
        if(!Input.GetKey(KeyCode.R))
        {
            toneActiveForRespirationRate = false;
            frameGuardTone = false;
        }

        UpdateRespirationRate(BreathCycleDictionary);

        //Debug Visualizations
        GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag("oldrect");
        // Loop through each GameObject and apply the position change
        foreach (GameObject obj in objectsWithTag)
        {
            obj.transform.position = new Vector3(obj.transform.position.x + amountToMove, obj.transform.position.y, obj.transform.position.z);
        }
    }

    private IEnumerator RespirationCycleCoroutine (float _measurementWindow = 60.0f, bool visualize = true){
        // This coroutine measures the duration of one tone/rest cycle.
        // It starts when toneActiveForRespirationRate is true.
        // It measures the duration of the tone, the next rest (when toneActiveForRespirationRate is false), and the full cycle (which ends the next time toneActiveForRespirationRate is true).
        // The measurements are stored in a dictionary, and cleared after the cycle exits the measurement window entirely.
        float _age = 0.0f;
        float _tAfterCycle = 0.0f;
        float _toneLength = _positiveActiveThreshold;
        float _restLength = 0.0f;
        float _cycleLength = 0.0f;

        idCounter++;
        int id = idCounter;

        //create a new dictionary entry for this coroutine cycle:
        BreathCycleData thisBreathCycleData = new BreathCycleData();
        thisBreathCycleData.window = 1;
        thisBreathCycleData._weight = 1f;
        thisBreathCycleData._cycleCount = 0.5f;

        if(visualize)
        {
            thisBreathCycleData.inhalerectangle = Instantiate(rectanglePrefab, startingPoint - new Vector3(0.0f, 400.0f, 0.0f), Quaternion.identity, canvas.transform);
            thisBreathCycleData.exhalerectangle = Instantiate(rectanglePrefab, startingPoint - new Vector3(0.0f, 400.0f, 0.0f), Quaternion.identity, canvas.transform);
        }
        
        //add the new dictionary entry to the dictionary:
        BreathCycleDictionary.Add(id, thisBreathCycleData);
        Debug.Log("RespirationCycleCoroutine started <" + id + ">");
        //UpdateRespirationRate();
    
        //Step 1: Measure the Tone
        while (toneActiveForRespirationRate == true)
        {
            _toneLength += Time.deltaTime;
            _cycleLength += Time.deltaTime;
            _age += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            thisBreathCycleData = BreathCycleDictionary[id];
            // Update the object
            thisBreathCycleData._toneLength = _toneLength;
            thisBreathCycleData._cycleLength = _cycleLength;
            thisBreathCycleData.invalid = _toneLength > 45.0f;

            if(visualize)
            {
                SelectedRectangle = thisBreathCycleData.inhalerectangle;
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

        thisBreathCycleData.inhalerectangle.tag = "oldrect";
        thisBreathCycleData = BreathCycleDictionary[id];
        thisBreathCycleData._cycleCount = 1.0f;
        BreathCycleDictionary[id] = thisBreathCycleData;
        Debug.Log("RespirationCycleCoroutine moving to Step 2 <" + id + ">");
        //UpdateRespirationRate();

        //Step 2: Measure the Rest
        _restLength = _negativeActiveThreshold;
        while (toneActiveForRespirationRate == false)
        {
            _restLength += Time.deltaTime;
            _cycleLength += Time.deltaTime;
            _age += Time.deltaTime;
            // Get the BreathCycleData object from the dictionary
            thisBreathCycleData = BreathCycleDictionary[id];

            // Update the object
            thisBreathCycleData._restLength = _restLength;
            thisBreathCycleData._cycleLength = _cycleLength;
            thisBreathCycleData.invalid = (_restLength > 13.0f);

            // Do debug visualizations
            if (visualize)
            {
                SelectedRectangle = thisBreathCycleData.exhalerectangle;
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
            
            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = thisBreathCycleData;
            
            //Debug.Log("id: " + id + " resting " + "restLength: " + _restLength + " _respirationRate: " + _respirationRate);

            yield return null;
        }

        Debug.Log("RespirationCycleCoroutine moving to Step 3 <" + id + ">");
        thisBreathCycleData.exhalerectangle.tag = "oldrect";

        //Step 3: Measure the time since the cycle ended, and fade it out of memory
        while (thisBreathCycleData._cycleCount > 0.0f)
        {
            
            _tAfterCycle += Time.deltaTime;
            _age += Time.deltaTime;

            // Get the BreathCycleData object from the dictionary
            thisBreathCycleData = BreathCycleDictionary[id];

            // Update the object and fade it from memory
            thisBreathCycleData._cycleCount = Mathf.Clamp((_measurementWindow - _age) / Mathf.Max(_cycleLength, 1.0f), 0.0f, 1.0f);

            // Do debug visualizations
            if(visualize)
            {
                RectTransform rectTransforminhale = thisBreathCycleData.inhalerectangle.GetComponent<RectTransform>();
                RectTransform rectTransformexhale = thisBreathCycleData.exhalerectangle.GetComponent<RectTransform>();
                if (rectTransforminhale != null)
                {
                    rectTransforminhale.sizeDelta = new Vector2(rectTransforminhale.sizeDelta.x, thisBreathCycleData._cycleCount*100.0f);
                }
                if (rectTransformexhale != null)
                {
                    rectTransformexhale.sizeDelta = new Vector2(rectTransformexhale.sizeDelta.x, thisBreathCycleData._cycleCount*100.0f);
                }
            }

            // Put the modified object back into the dictionary
            BreathCycleDictionary[id] = thisBreathCycleData;

            //Debug.Log("id: " + id + " waiting " + " cycleLength: " + _cycleLength + " _respirationRate: " + _respirationRate);

            yield return null;
        }

        //Check if this dictionary entry is invalid
        thisBreathCycleData = BreathCycleDictionary[id];
        bool invalid = false;
        invalid = thisBreathCycleData.invalid;

        //If the dictionary entry is invalid, update the respiration rate
        if (invalid)
        {
            Debug.Log("Invalid breath cycle ending. Updating respiration rate. <" + id + ">");
            //UpdateRespirationRate();
        }

        //Remove the dictionary entry:
        Debug.Log("RespirationCycleCoroutine ended <" + id + ">");
        BreathCycleDictionary.Remove(id);

    }

    private void UpdateRespirationRate(Dictionary<int, BreathCycleData> breathCycleDictionary)
    {
        // Set _respirationRate to the total of all the cycle counts in the dictionary:
        _respirationRate = 0.0f;
        _respirationRateRaw = 0.0f;
        _respirationMeasured = 0.0f;

        bool invalid = false;
        foreach (KeyValuePair<int, BreathCycleData> entry in breathCycleDictionary)
        {
            _respirationMeasured += entry.Value._cycleLength;
            if (entry.Value.invalid)
            {
                invalid = true;
                break;
            }
        }
        if (_respirationMeasured < _respirationMeasurementWindow1)
        {
            invalid = true;
        }
        if (invalid)
        {
            _respirationRate = -1.0f;

            foreach (KeyValuePair<int, BreathCycleData> entry in breathCycleDictionary)
            {
                _respirationRateRaw += entry.Value._cycleCount;
            }

            if (!invalidFlag)
            {
                invalidFlag = true;
                Debug.Log("Invalid breath cycle detected. Respiration rate set to -1.0");
            }

        }
        else
        {
            invalidFlag = false;
            foreach (KeyValuePair<int, BreathCycleData> entry in breathCycleDictionary)
            {
                _respirationRate += entry.Value._cycleCount;
                _respirationRateRaw += entry.Value._cycleCount;
            }
        }
    }

/*    private void UpdateRespirationRate()
    {
        // Set _respirationRate to the total of all the cycle counts in the dictionary:
        _respirationRate = 0.0f;
        _respirationRateRaw = 0.0f;
        _respirationMeasured = 0.0f;

        bool invalid = false;
        foreach (KeyValuePair<int, BreathCycleData> entry in BreathCycleDictionary)
        {
            _respirationMeasured += entry.Value._cycleLength;
            if (entry.Value.invalid)
            {
                invalid = true;
                break;
            }
        }
        if (_respirationMeasured < _respirationMeasurementWindow1)
        {
            invalid = true;
        }
        if (invalid)
        {
            _respirationRate = -1.0f;

            foreach (KeyValuePair<int, BreathCycleData> entry in BreathCycleDictionary)
            {
                _respirationRateRaw += entry.Value._cycleCount;
            }

            if(!invalidFlag)
            {
                invalidFlag = true;
                Debug.Log("Invalid breath cycle detected. Respiration rate set to -1.0");
            }

        }
        else
        {
            invalidFlag = false;
            foreach (KeyValuePair<int, BreathCycleData> entry in BreathCycleDictionary)
            {
                _respirationRate += entry.Value._cycleCount;
                _respirationRateRaw += entry.Value._cycleCount;
            }
        }
        Debug.Log("Updated respiration rate. Raw: " + _respirationRateRaw + " Standard: " + _respirationRate);
    }*/
}