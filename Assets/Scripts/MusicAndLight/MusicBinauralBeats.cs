using System.Collections;
using System.Collections.Generic;
using ConversionUtilities;
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

    // Bus output = _volume (stage-owned BASE, lerped by lerpVolume) × _attenuationFactor (mode-owned, lerped by
    // lerpAttenuation). Both feed the single writer ApplyBusVolume(); see BinauralAttenuationPolicy.
    /// <summary>Shared default fade for both base-volume (<see cref="SetVolume"/>) and attenuation
    /// (<see cref="SetBinauralAttenuated"/>) transitions. Single source of truth (also referenced by
    /// <see cref="BinauralStagePolicy.DefaultLerpDurationSeconds"/>).</summary>
    public const float DefaultLerpDurationSeconds = 30f;

    public float _volume = 0f;                                                     // base volume (0–100)
    private bool _attenuated = false;                                              // mode-driven attenuation state
    private float _attenuationFactor = BinauralAttenuationPolicy.UnattenuatedFactor; // live factor (1.0 → 0.7)
    private Coroutine _volumeLerp;                                                  // active base-volume lerp (stop by handle)
    private Coroutine _attenuationLerp;                                            // active attenuation lerp (stop by handle)
    private Coroutine _stopAfterMuteLerp;                                          // posts Stop after volume fade to 0
    private bool _generatorPlaying;                                                // Play_BinauralGenerator posted and not stopped

    /// <summary>True when <see cref="PlayBinauralBeats"/> has been posted and <see cref="StopBinauralBeats"/> has not.</summary>
    public bool IsGeneratorRunning => _generatorPlaying;

    /// <summary>Live binaural bus output actually pushed to Wwise (base × attenuation). For debug/inspection.</summary>
    public float EffectiveBusVolume => BinauralAttenuationPolicy.Apply(_volume, _attenuationFactor);

    /// <summary>True when mode-driven attenuation is currently engaged.</summary>
    public bool IsAttenuated => _attenuated;
    // Start is called before the first frame update

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        Debug.Log("Binaural Beats: Initializing Binaural Beats Manager");
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // Sending out information from this     
        // When the center frequency changes, we need to fade out the signal, then change the actual frequencies of the output, and then fade it back up.
        // Practically speaking, we can achieve this most easily by stopping and starting the generator, which has a 4 second fade time in WWise.
    }
       
    //FUNCTION CALLS
    /// <summary>Posts Play_BinauralGenerator. Prefer <see cref="EnsureGeneratorRunning"/> from stage policy when entering audible stages.</summary>
    public void PlayBinauralBeats()
    {
        SetBinauralFrequencies();
        AkSoundEngine.PostEvent("Play_BinauralGenerator", gameObject);
        _generatorPlaying = true;
        Debug.Log("Binaural Beats: Play_BinauralGenerator posted.");
    }

    public void StopBinauralBeats()
    {
        CancelPendingStopAfterMute();
        AkSoundEngine.PostEvent("Stop_BinauralGenerator", gameObject);
        _generatorPlaying = false;
        Debug.Log("Binaural Beats: Stop_BinauralGenerator posted.");
    }

    /// <summary>
    /// Stage entry point: audible target (&gt;0) starts the generator and lerps volume up; muted target (0) lerps volume
    /// down then posts Stop_BinauralGenerator and clears <see cref="_generatorPlaying"/>.
    /// </summary>
    public void ApplyStageTargetVolume(float targetVolume, float lerpDurationSeconds = DefaultLerpDurationSeconds)
    {
        CancelPendingStopAfterMute();

        if (targetVolume > 0f)
        {
            EnsureGeneratorRunning();
            SetVolume(targetVolume, lerpDurationSeconds);
            return;
        }

        SetVolume(0f, lerpDurationSeconds);
        if (_generatorPlaying)
            _stopAfterMuteLerp = StartCoroutine(StopGeneratorAfterVolumeLerp(lerpDurationSeconds));
    }

    void CancelPendingStopAfterMute()
    {
        if (_stopAfterMuteLerp == null)
            return;
        StopCoroutine(_stopAfterMuteLerp);
        _stopAfterMuteLerp = null;
    }

    IEnumerator StopGeneratorAfterVolumeLerp(float waitSeconds)
    {
        float elapsed = 0f;
        while (elapsed < waitSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        _stopAfterMuteLerp = null;
        if (!_generatorPlaying)
            yield break;
        StopBinauralBeats();
    }

    /// <summary>
    /// Idempotent start for audible stages: sync RTPC frequencies from the live fundamental (if available), then post
    /// Play_BinauralGenerator once. SetVolume alone does not start the generator — this was missing before Stage 2b.
    /// </summary>
    public void EnsureGeneratorRunning()
    {
        CancelPendingStopAfterMute();
        if (_generatorPlaying)
            return;

        SyncCenterFrequencyFromMusicSystem();
        SetBinauralFrequencies();
        AkSoundEngine.PostEvent("Play_BinauralGenerator", gameObject);
        _generatorPlaying = true;
        Debug.Log("Binaural Beats: EnsureGeneratorRunning — Play_BinauralGenerator posted (fundamental-synced).");
    }

    void SyncCenterFrequencyFromMusicSystem()
    {
        var ms = MusicSystem1.instance;
        if (ms == null || ms.fundamentalNoteName == NoteName.None)
            return;

        float hz = NoteUtils.NoteToFrequencyA440(ms.fundamentalNoteName);
        _centerFrequency = NormalizeCenterFrequency(hz);
    }

    /// <summary>Same octave fold as <see cref="ChangeCenterFrequency"/> — keeps RTPC in the viable band without stop/play.</summary>
    static float NormalizeCenterFrequency(float hz)
    {
        float f = hz;
        while (f < 185f) f *= 2f;
        while (f > 370f) f /= 2f;
        return f;
    }

    //A Function for changing the center frequency (half way between left and right)
    //We need to transform the inRate to be within the acceptable range, which should be one octave centered around 261hz

    /// <summary>Sets the BASE binaural bus volume (0–100). Owned by the stage layer via
    /// <see cref="BinauralStagePolicy.ApplyBinauralVolumeForStage"/>; the live output is this scaled by attenuation.</summary>
    public void SetVolume(float _inVolume, float _lerpDuration = DefaultLerpDurationSeconds)
    {
        // Stop by handle: StopCoroutine("lerpVolume") would be a no-op because the coroutine is started with an
        // IEnumerator, not a string — without this a rapid second SetVolume would stack a second lerp on _volume.
        if (_volumeLerp != null) StopCoroutine(_volumeLerp);
        _volumeLerp = StartCoroutine(lerpVolume(_volume, _inVolume, _lerpDuration));
    }

    private IEnumerator lerpVolume(float _startVolume, float _targetVolume, float _lerpDuration = 5.0f)
    {
        float _elapsedTime = 0f;
        Debug.Log("Binaural Beats: Lerping Volume from " + _startVolume + " to " + _targetVolume + "over " + _lerpDuration + " seconds");
        while (_elapsedTime < _lerpDuration)
        {
            _volume = Mathf.Lerp(_startVolume, _targetVolume, _elapsedTime / _lerpDuration);
            ApplyBusVolume();
            _elapsedTime += Time.deltaTime;
            yield return null;
        }
        //snap at end
        _volume = _targetVolume;
        Debug.Log("Binaural Beats: New Volume is " + _targetVolume);
        ApplyBusVolume();
    }

    /// <summary>
    /// Mode-driven attenuation toggle (called from <see cref="MusicSystem1.SetMusicModeFlags"/>). Leaves the stage
    /// base volume untouched and lerps the output down/up by <see cref="BinauralAttenuationPolicy.AttenuationFraction"/>.
    /// </summary>
    public void SetBinauralAttenuated(bool attenuated, float _lerpDuration = DefaultLerpDurationSeconds)
    {
        if (_attenuated == attenuated)
            return;
        _attenuated = attenuated;
        float targetFactor = BinauralAttenuationPolicy.GetFactor(attenuated);
        Debug.Log("Binaural Beats: Attenuation " + (attenuated ? "ON" : "OFF") + " (factor " + targetFactor + ")");
        // Stop by handle (see SetVolume) so a quick toggle doesn't stack a second lerp on _attenuationFactor.
        if (_attenuationLerp != null) StopCoroutine(_attenuationLerp);
        _attenuationLerp = StartCoroutine(lerpAttenuation(_attenuationFactor, targetFactor, _lerpDuration));
    }

    private IEnumerator lerpAttenuation(float _startFactor, float _targetFactor, float _lerpDuration)
    {
        float _elapsedTime = 0f;
        while (_elapsedTime < _lerpDuration)
        {
            _attenuationFactor = Mathf.Lerp(_startFactor, _targetFactor, _elapsedTime / _lerpDuration);
            ApplyBusVolume();
            _elapsedTime += Time.deltaTime;
            yield return null;
        }
        //snap at end
        _attenuationFactor = _targetFactor;
        ApplyBusVolume();
    }

    /// <summary>Single writer of the binaural bus volume RTPC: base volume scaled by the current attenuation factor.</summary>
    private void ApplyBusVolume()
    {
        AkSoundEngine.SetRTPCValue("BinauralGenerator_Bus_Volume",
            BinauralAttenuationPolicy.Apply(_volume, _attenuationFactor));
    }
   

    // ChangeCenterFrequency(float newCenterFrequency)
    public void ChangeCenterFrequency(float _inFrequency)
    {
        
        if (_inFrequency <= 0f || float.IsNaN(_inFrequency) || float.IsInfinity(_inFrequency))
        {
            Debug.LogError($"Invalid frequency for ChangeCenterFrequency: {_inFrequency} (input was {_inFrequency})");
            return;
        }
        
        float _newCenterFrequency = _inFrequency;
        
        //viable range centering on 261 is 185hz to 370hz
    
        while (_newCenterFrequency < 185f)
        {
            _newCenterFrequency *= 2f;
        }
        while (_newCenterFrequency > 370f)
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
        _generatorPlaying = false;
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
        _generatorPlaying = true;
        
    }


    //A function for changing the binaural beat rate (adds half to the left, subtracts half from the right) for new Binaural beat rate. 
    public void NewBinauralBeatRate(float _inRate, float duration = 8.0f, string waveType = "theta")
    {
        // Set min/max based on waveType
        float minRate;
        float maxRate;
        switch(waveType.ToLower())
        {
            case "alpha":
                minRate = 8f;
                maxRate = 16f;
                break;
            case "delta":
                minRate = 2f;
                maxRate = 4f;
                break;
            case "theta":
            default:
                minRate = 4f;
                maxRate = 8f;
                break;
        }
        
        //the binarual beat rate should always be within the specified range for the wave type
        float _newBinauralBeatRate = _inRate;
        
        // Handle zero or negative rates - set to minimum for the wave type
        if(_newBinauralBeatRate <= 0f)
        {
            Debug.LogWarning("MusicBinauralBeats: Rate is " + _newBinauralBeatRate + " (<= 0), setting to minimum " + minRate + "Hz for waveType " + waveType);
            _newBinauralBeatRate = minRate;
        }
        else
        {
            // Normalize rate to be within the wave type's range
            while (_newBinauralBeatRate < minRate)
            {
                _newBinauralBeatRate *= 2f;
                // Safety check to prevent infinite loop (shouldn't happen now, but good to have)
                if(_newBinauralBeatRate == 0f)
                {
                    Debug.LogWarning("MusicBinauralBeats: Rate normalization resulted in 0, setting to minimum " + minRate + "Hz");
                    _newBinauralBeatRate = minRate;
                    break;
                }
            }
            while (_newBinauralBeatRate > maxRate)
            {
                _newBinauralBeatRate /= 2f;
            }
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
                Debug.Log("Binaural Beats: New Rate is (instantly) " + _newBinauralBeatRate + " Hz for waveType " + waveType);
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


