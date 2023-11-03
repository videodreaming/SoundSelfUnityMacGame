using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ADSR2
{
    public float Attack, Decay, Sustain, Release;
    public ADSR2()
    {
        Attack  = 0.1f;
        Decay   = 0.1f;
        Sustain = 0.5f;
        Release = 0.3f;
    }
}

public struct ActiveNote2
{
    public double  startPlayTime;
    public double  releaseTime;
    public bool    isBeingPlayed;
    public float   fundementalFrequency;
    
}

public class Note : MonoBehaviour
{

    //references
    private Action<Note> _onEndCallback;

    public float NoteValue { get; private set; }
    
    [SerializeField] [Range(0f,1f)]
    private float _pitch;
    [SerializeField] [Range(0f,1f)]
    private float _volume;
    
    private bool _despawning;
    
    /*procgen references
    public KeyIndeciesToFrequencies fundementalToneFrequencies;
    public  float gain;
    public KeyCode[] notesKeyCodes;
    public int beginingKeyIndex = 50;
    public ADSR2 keysADSR;

    public  float[] harmonicStrengths = new float[12];

    private float   samplingFrequency;     // this is the number of samples we use per second,to construct the sound waveforms.
                                           // default is 48,000 samples. This means if your frame rate is 60 fps, in each frame you need to provide 48k/60 samples. 

    private AudioSource ad_source;
    
    private float[] phase = new float[12];
    private float fundementalToneFrequency;

    private float scaleTimer = 0;
    private ActiveNote[] currentlyBeingPlayed = new ActiveNote[12];*/

    public void Initialize(float note, float pitch, float volume, Action<Note> onEndCallback = null)
    {
        NoteValue = note;
        _pitch = pitch;
        _volume = volume;
        
        _onEndCallback = onEndCallback;

        StartCoroutine(Spawn());
    }

    public void End()
    {
        Debug.Log($"despawning {name}");
        StartDespawn();
    }

    public bool CheckDissonantTo(Harmony harmonyToCompare)
    {
        //TODO: determine when note is dissonant with other notes, note that this fundamental may be in the harmony
        return false;
    }

    public void Update(){

    }

    private IEnumerator Spawn()
    {
        while(_volume < 1f){
            _volume += 0.001f;
            yield return null;
        }
    }
    
    private void StartDespawn()
    {
        if (_despawning) return;
        _despawning = true;
        StartCoroutine(Despawn());
    }

    private IEnumerator Despawn()
    {
        while(_volume > 0f){
            _volume -= 0.005f;
            yield return null;
        }
        _onEndCallback?.Invoke(this);
    }
}

