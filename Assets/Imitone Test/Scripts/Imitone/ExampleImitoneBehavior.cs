using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using System.Text.Json;
//using System.Text.Json.Nodes;
using Defective.JSON;

using imitone;


public class ExampleImitoneBehavior: MonoBehaviour
{
    public float pitch_hz = 0f;
    public float note_st = 0f;

    [TextAreaAttribute(8,8)] public string imitoneState;


    int sampleRate;
    ImitoneVoice imitone;

    string             microphoneName;
    AudioClip          inputBuffer;
    int                micPosRead = 0;
    float[]            capturedInput;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var device in Microphone.devices)
            {microphoneName = device; break;}
        
        if (microphoneName.Length == 0)
        {
            Debug.Log("No microphone was available for pitch tracking.");
            return;
        }
        Debug.Log("Chose microphone: " + microphoneName);
        
        // NOTE: Unity doesn't give us a way to query native samplerate.
        //  Converting to 48khz may degrade audio quality slightly.
        sampleRate = 48000;

        // NOTE: this requires permission on mobile.
        
        inputBuffer = Microphone.Start(
                deviceName: microphoneName,
                loop:       true,
                lengthSec:  1,
                frequency:  sampleRate
                );

        if (inputBuffer == null)
        {
            Debug.Log("PitchTracker failed to Start recording from Microphone!");
            return;
        }
        
        try
        {
            ImitoneVoice.ActivateLicense("imitone technology used under license to New Entheogen Ltd, March 2023.");

            // Create an imitone voice whose notes are in "exact pitch" mode.
            // Also specify 'range' large enough to permit whistling.
            imitone = new ImitoneVoice(sampleRate, "{\"guide\":\"off\",\"slide\":\"bend\",\"range\":{\"min\":34.0,\"max\":101.0}}");
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
            throw;
        }

        if (imitone == null)
        {
            Debug.Log("imitone was null after creation.");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!inputBuffer) return;
        
        // The microphone's write position in the clip can wrap back around to the beginning.
        int micPosWrite = Microphone.GetPosition(microphoneName);
        Array.Resize(ref capturedInput, (inputBuffer.samples  +  micPosWrite - micPosRead) % inputBuffer.samples);
        if (capturedInput.Length > 0)
        {
            
       
            // Read the latest audio data, beginning from where we left off and wrapping around as needed.
            inputBuffer.GetData(capturedInput, micPosRead);
            micPosRead = (micPosRead + capturedInput.Length) % inputBuffer.samples;

            // Analyze the captured audio with imitone.
            if (imitone != null)
            {
                float peakAmplitude = 0f;
                foreach (float sample in capturedInput)
                    if (Math.Abs(sample) > peakAmplitude)
                        peakAmplitude = Math.Abs(sample);

                //Debug.Log(String.Format("Analyzing mic samples x {0}, peak amplitude {1}", capturedInput.Length, peakAmplitude));


                imitone.InputAudio(capturedInput);

                imitoneState = imitone.GetState();

                try
                {
                    var data = new JSONObject(imitoneState);
                    
                    JSONObject tones = data["tones"];
                    JSONObject notes = data["notes"];
                    if (!tones || !tones.isArray) throw new ArgumentException("imitone output did not include tones array.");
                    if (!notes || !notes.isArray) throw new ArgumentException("imitone output did not include notes array.");

                    if (tones.list != null && tones.list.Count > 0)
                    {
                        var tone = tones[0];
                        if (!tone.isObject) throw new ArgumentException("imitone tone is not an object");

                        if (tone["frequency_hz"] == null) throw new ArgumentException("imitone tone does not have frequency_hz");
                        pitch_hz = tone["frequency_hz"].floatValue;
                    }
                    else
                    {
                        pitch_hz = 0f;
                    }

                    if (notes.list != null && notes.list.Count > 0)
                    {
                        var note = notes[0];
                        if (!note.isObject) throw new ArgumentException("imitone note is not an object");

                        if (note["pitch"] == null) throw new ArgumentException("imitone note does not have frequency_hz");

                        // Convert from imitone's wacky pitch value to MIDI frequency format
                        note_st = note["pitch"].floatValue / 100f - 36.3763165623f;
                    }
                    else
                    {
                        note_st = 0f;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    pitch_hz = -1f;
                    note_st = -1f;
                }
                
            }
            else
            {
                //Debug.Log("No imitone voice to analyze audio.");
            }
        }
    }
}   
