using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AK.Wwise;


/*
//Wwise Calls for Binaural Beats
AkSoundEngine.SetRTPCValue("BinauralGenerator_LEFT_Frequency", 258f);
AkSoundEngine.SetRTPCValue("BinauralGenerator_RIGHT_Frequency", 263f);
Below are the Events & RTPCs available:
Play_BinauralGenerator
Stop_BinauralGenerator
RTPCs:
BinauralGenerator_Bus_Volume (Range 0-100)
BinauralGenerator_LEFT_Frequency (Range 100 Hz - 1000 Hz),
BinauralGenerator_RIGHT_Frequency (edited) 

Lorna says:  just pushed to GitHub with an added Binaural Generator. I set a range of 100-1,000 Hz. Any higher and it's very annoying, any lower and we can't hear it. I would recommend the center being something like a low C (261 Hz), so with Theta (4-8 Hz difference) that would be something like this in Unity:
*/

public class MusicBinauralBeats : MonoBehaviour
{

    public static MusicBinauralBeats instance {get; private set;}

    //VARIABLES
    private float _centerFrequency = 261f; // Default center frequency (C4)
    private float _beatRate = 4f; // Last frame's binaural beat rate
    private bool instantUpdate = false;
    private float _volume = 0f;
    // Start is called before the first frame update

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("Binaural Beats: Initializing Binaural Beats Manager");
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("Binaural Beats: Starting Binaural Beats");
            AkSoundEngine.PostEvent("Play_BinauralGenerator", gameObject);
        }
        if(Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("Binaural Beats: Stopping Binaural Beats");
       }
        if(Input.GetKeyDown(KeyCode.O))
        {
            float newRate = Random.Range(4f, 8f);
            Debug.Log("Binaural Beats: Changing Binaural Beat Rate to " + newRate + " Hz");
            NewBinauralBeatRate(newRate, 4.0f);
        }
        if(Input.GetKeyDown(KeyCode.P))
        {
            if(_volume < 50f)
            {
                Debug.Log("Binaural Beats: Fading In Binaural Beats");
                SetVolume(100f, 2.0f);
            }
            else
            {
                Debug.Log("Binaural Beats: Fading Out Binaural Beats");
                SetVolume(0f, 2.0f);
            }
        }
        // Sending out information from this     
        // When the center frequency changes, we need to fade out the signal, then change the actual frequencies of the output, and then fade it back up.
        // Practically speaking, we can achieve this most easily by stopping and starting the generator, which has a 4 second fade time in WWise.
    }
       
    //FUNCTION CALLS
    //A Function for changing the center frequency (half way between left and right)
    //We need to transform the inRate to be within the acceptable range, which should be one octave centered around 261hz

    public void SetVolume(float _inVolume, float _lerpDuration = 5.0f)
    {

        StopCoroutine("lerpVolume");
        StartCoroutine(lerpVolume(_volume, _inVolume, _lerpDuration));
    }

    private IEnumerator lerpVolume(float _startVolume, float _targetVolume, float _lerpDuration = 5.0f)
    {
        float _elapsedTime = 0f;
        Debug.Log("Binaural Beats: Lerping Volume from " + _startVolume + " to " + _targetVolume + "over " + _lerpDuration + " seconds");
        while (_elapsedTime < _lerpDuration)
        {
            _volume = Mathf.Lerp(_startVolume, _targetVolume, _elapsedTime / _lerpDuration);
            
            AkSoundEngine.SetRTPCValue("BinauralGenerator_Bus_Volume", _volume);
            _elapsedTime += Time.deltaTime;
            yield return null;
        }
        //snap at end
        _volume = _targetVolume;
        Debug.Log("Binaural Beats: New Volume is " + _targetVolume);
        AkSoundEngine.SetRTPCValue("BinauralGenerator_Bus_Volume", _volume);   
    }
   

    // ChangeCenterFrequency(float newCenterFrequency)
    public void ChangeCenterFrequency(float _inFrequency)
    {
        float _newCenterFrequency = _inFrequency;
        
        //viable range centering on 261 is 185hz to 370hz
    
        while (_inFrequency < 185f)
        {
            _newCenterFrequency *= 2f;
        }
        while (_inFrequency > 370f)
        {
            _newCenterFrequency /= 2f;
        }

        if(_newCenterFrequency != _centerFrequency)
        {
            StopCoroutine("changeFrequencyCoroutine");
            StartCoroutine(changeFrequencyCoroutine(_newCenterFrequency));
        }
    }
    // needs to stop the generator, wait 4 seconds, change frequencies, then start again
    private IEnumerator changeFrequencyCoroutine(float _newCenterFrequency)
    {
        //stop generator
        Debug.Log("Binaural Beats: Fading Out Binaural Beats with Stop event for frequency change to " + _newCenterFrequency + " Hz");
        AkSoundEngine.PostEvent("Stop_BinauralGenerator", gameObject);
        //wait 4 seconds
        yield return new WaitForSeconds(4.0f);
        instantUpdate = true;
        yield return null;
        instantUpdate = false;
        //change frequencies
        _centerFrequency = _newCenterFrequency;
        Debug.Log("Binaural Beats: Changing Center Frequency to " + _centerFrequency + " Hz, and fading in again with Play event");
        SetBinauralFrequencies();
        //start generator
        AkSoundEngine.PostEvent("Play_BinauralGenerator", gameObject);
        
    }


    //A function for changing the binaural beat rate (adds half to the left, subtracts half from the right) for new Binaural beat rate. 
    public void NewBinauralBeatRate(float _inRate, float duration = 8.0f)
    {
        //the binarual beat rate should always be between 4hz (min) and 8 hz(maximum). 
        float _newBinauralBeatRate = _inRate;
        while (_newBinauralBeatRate < 4f)
        {
            _newBinauralBeatRate *= 2f;
        }
        while (_newBinauralBeatRate > 8f)
        {
            _newBinauralBeatRate /= 2f;
        }

        StopCoroutine("lerpNewBinauralBeatRate");

        if(_beatRate != _newBinauralBeatRate)
        {
            if(duration > 0f)
            {
                StartCoroutine(lerpNewBinauralBeatRate(_beatRate, _newBinauralBeatRate, duration));
            }
            else
            {
                _beatRate = _newBinauralBeatRate;
                SetBinauralFrequencies();
                Debug.Log("Binaural Beats: New Rate is (instantly) " + _newBinauralBeatRate);
            }
        }
    } 
    

   private IEnumerator lerpNewBinauralBeatRate(float _startRate, float _targetRate, float _lerpDuration = 8.0f)
    {
        Debug.Log("Binaural Beats: Lerping Binaural Beat Rate from " + _startRate + " to " + _targetRate);
        float _elapsedTime = 0f;
        while (_elapsedTime < _lerpDuration)
        {
            if (instantUpdate)
            {
                _beatRate = _targetRate;
            }
            else
            {
                _beatRate = Mathf.Lerp(_startRate, _targetRate, _elapsedTime / _lerpDuration);
            }
            SetBinauralFrequencies();
            _elapsedTime += Time.deltaTime;
            yield return null;
        }
        //snap at end
        _beatRate = _targetRate;
        Debug.Log("Binaural Beats: New Rate is " + _targetRate);
        SetBinauralFrequencies();
    }

    private void SetBinauralFrequencies()
    {
        AkSoundEngine.SetRTPCValue("BinauralGenerator_LEFT_Frequency", _centerFrequency + (_beatRate / 2f));
        AkSoundEngine.SetRTPCValue("BinauralGenerator_RIGHT_Frequency", _centerFrequency - (_beatRate / 2f));
    }
    
}


