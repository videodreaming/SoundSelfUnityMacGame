using System.Runtime.InteropServices;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace imitone
{
    public class ImitoneVoice 
    {
        /*
            This function must be called with the DLL's license text before creating an ImitoneVoice.
        */
        public static void ActivateLicense(string licenseText)
        {
            ulong result = imi_ActivateLicense(licenseText);
            if (result == 0)
                throw new System.ArgumentException("Invalid imitone license activation text: #" + result);
        }


        /*
            Create an imitone voice with 'new ImitoneVoice( ... )' and keep it as a variable.
            
            sampleRate     ... samplerate in hertz, a property of the microphone input.  Cannot be changed.
            initial_config ... optional, same as the argument to SetConfig.
        */
        public ImitoneVoice(int sampleRate, string initial_config = "")
        {
            if (sampleRate <= 0 || sampleRate > 500000)
                throw new System.ArgumentException("ImitoneVoice received nonsensical samplerate!");

            voice = imi_Create(
                String.Format("{{ \"sampleRate\" : {0} }}", sampleRate),
                initial_config);

            if (voice == IntPtr.Zero)
                throw new System.ArgumentException(String.Format("Could not create voice: {0}", imi_GetError(voice)));

            config = imi_GetConfig(voice);

            // Feed buffer supports 1 second of audio input
            feed_buffer = new NativeArray<float>(new float[sampleRate], Unity.Collections.Allocator.Persistent);
        }
        
        ~ImitoneVoice()
        {
            if (voice != IntPtr.Zero) imi_Destroy(voice);
            feed_buffer.Dispose();
        }


        /*
            Sets a partial or full imitone configuration using a JSON string.
                Changes will take effect with the next call to InputAudio.
        */
        public void SetConfig(string config_changes, bool log = true)
        {
            uint result = 200;
            if (config_changes.Length > 0)
            {
                imi_SetConfig(voice, config_changes);
                if (log)
                {
                    Debug.Log("Config Changes: " + config_changes);
                }
            }
                
            if (result > 299)
                throw new System.ArgumentException(String.Format("Could not configure voice: {0}", imi_GetError(voice)));

            config = imi_GetConfig(voice);
        }

        public string GetConfig()
        {
            return config;
        }


        /*
            Call this function to input some mono audio to imitone.
                Unlike other functions, this can be called from a different thread.
            
            Continuous, un-processed audio is usually best.
            If audio is not continuous, feed imitone about 1/8 second worth of silence (ie, an array of zeroes whose size is sampleRate/8)
        */
        public void InputAudio(float[] audio)
        {
            uint length = (uint) audio.Length;
            total_samples += length;

            // Copy audio into NativeArray.
            for (int i = 0; i < audio.Length; ++i) feed_buffer[i] = audio[i];

            // Pass audio to imitone DLL.
            uint result = 200;
            unsafe {
                result = imi_AnalyzeF32(voice, new IntPtr(NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(feed_buffer)), length);
            }

            if (result > 299)
                throw new System.ArgumentException(String.Format("Could not analyze audio: {0}", imi_GetError(voice)));
        }


        /* 
            Get the latest readings from imitone.
                This returns a string containing a large JSON structure, documented separately.
                Calling this every frame, or after every call to InputAudio, is fine.
        */
        public string GetState()
        {
            string newState = imi_GetState(voice);

            if (newState.Length == 0)
                throw new System.ArgumentException(String.Format("Could not get voice state: {0}", imi_GetError(voice)));
                
            state = newState;
            return state;
        }




        /*
            (internal fields)
        */
        private IntPtr voice;
        private string config;
        private string state;
        private uint   sampleRate;
        private ulong  total_samples = 0;
        NativeArray<float> feed_buffer;




        /*
            Native interface with the imitone DLL.
            Do not change this code or the extension may stop working.
        */
        #if UNITY_IOS
            const string dll = "__Internal";
        #else
            const string dll = "imitone";
        #endif

        [DllImport(dll)] private static extern ulong  imi_ActivateLicense(string licenseText);

        [DllImport(dll)] private static extern IntPtr imi_Create   (string setup_parameters, string initial_config);
        [DllImport(dll)] private static extern uint   imi_Destroy  (IntPtr voice);

        [DllImport(dll)] private static extern string imi_GetConfig(IntPtr voice);
        [DllImport(dll)] private static extern uint   imi_SetConfig(IntPtr voice, string config);
        [DllImport(dll)] private static extern uint   imi_Reset    (IntPtr voice);

        [DllImport(dll)] private static extern string imi_GetState  (IntPtr voice);

        [DllImport(dll)] private static extern uint   imi_AnalyzeF32(IntPtr voice, IntPtr samples_f32, uint sampleCount);

        [DllImport(dll)] private static extern string imi_GetError(IntPtr voice);
    }
}

//Sample Copy of config.imitone.json. 
/*
{
    "$comment": [
        " This file was automatically generated by imitone.            ",
        " You should close imitone before editing it.                  ",
        " Changes here may be erased or reformatted when imitone runs. "
    ],
    "$edition": "standard",
    "$tips": {
        "About MIDI Mapping": [
            " MIDI 'map' options are:                            ",
            " cc-[0-119]     -- Control Change                   ",
            " cc_fine-[0-31] -- 14-bit Control Change            ",
            " pressure       -- Channel Pressure (Aftertouch)    ",
            " pressure_poly  -- Polyphonic Pressure (Aftertouch) "
        ],
        "About Units of Pitch": [
            " pitchBendRange -- in 12-EDO semitones (100-cent units) ",
            " transpose ------- pitch shift in 12-EDO semitones.     ",
            "           this can be used for non-A440 base pitch.    ",
            " range ----------- lowest and highest pitch, expressed  ",
            "           as MIDI Note Numbers in 12-EDO, A440 tuning. ",
            "         https://imitone.com/discover-midi/#Note_Number " (*) USEFUL FOR SOUNDSELF
        ]
    },
    "$version": "0.11",
    "audio_devices": {
        "mic1": {
            "device": {
                "mode": "Core Audio (pro)",
                "name": "MacBook Pro Microphone"
            }
        }
    },
    "midi_devices": {
        "midi1": {
            "device": {
                "mode": "",
                "name": "imitone app-to-app (app-to-app MIDI)"
            }
        }
    },
    "voices": [
        {
            "audio_in": {
                "channel": 0,
                "id": "mic1"
            },
            "midi_out": {
                "channel": 0,
                "id": "midi1"
            },
            "tonal": {
                "guide": "on", //— pitch-correction for singing consistently sharp or flat of scale by up to 50-100 cents
                "midi": { //— these are probably not useful for SoundSelf
                    "pitchBendRange": 2.0,
                    "power": {
                        "map": "cc-11"
                    },
                    "vibrato": {
                        "map": "cc-1",
                        "range": 1.0
                    }
                },
                "mute": false, //— suspends pitch tracking; will ignore audio passed in while set (*) USEFUL FOR SOUNDSELF
                "power": "no-send", //— maps tone loudness/power to note properties
                "quickBend": false,
                "range": { //— pitch range has a significant effect on CPU usage!!
                    "max": 84.0, //— highest pitch; 84 covers most high sopranos but excludes whistling (*) USEFUL FOR SOUNDSELF
                    "min": 34.0 //— lowest pitch expressed on MIDI frequency scale; 34 is low bass (*) USEFUL FOR SOUNDSELF
                },
                "scale": { //— pitch correction will affect note data, but not tone data
                    "id": "blues",
                    "key": "D",
                    "keyTranspose": 0, //— shift the piano scale by this many degrees to set a key
                    "mask": "101001011100" //— piano scale expressed as a mask, starting at C when keyTranspose is 0
                },
                "slide": "legato", //— governs how notes are strung together.  "exact" disables key/scale correction.
                "tracking": "fast", //— tracking preset; may be set to "reliable" for slightly better tracking at cost of latency (*) USEFUL FOR SOUNDSELF
                "transpose": 0.0, //— transpose tone pitch by this many 100-cent units before interpreting into notes
                "trigger": "sustain", //— "sustain" = steady sounds; "attack" = requires transient to start a note. (*) USEFUL FOR SOUNDSELF
                "vibrato": "send", //— maps tone vibrato to note properties
                "volume": {
                    "autoAdjust": true, //— does not function in the Unity plugin
                    "defaultValue": -6.0, //— determines note loudness properties when not 'sending' power
                    "maximum": -29.0, //— governs calculation of note loudness properties; no effect on tone measurements
                    "threshold": -59.0 //— minimum tone power in decibels to trigger/sustain a note. (*) USEFUL FOR SOUNDSELF
                }
            }
        }
    ]
}
*/