using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WwiseConnection : MonoBehaviour
{

    private int fundamentalCount = -1;
    private int harmonyCount = -1;
    private int worldCount = -1;
    void Start()
    {
        AkSoundEngine.PostEvent("Play_SilentLoops_v3_FundamentalOnly", gameObject);
        AkSoundEngine.PostEvent("Play_SilentLoops_v3_HarmonyOnly", gameObject);
        AkSoundEngine.SetState("SoundWorldMode","SonoFlore");
        AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
        AkSoundEngine.PostEvent("Play_AMBIENT_ENVIRONMENT_LOOP",gameObject);
        
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", "A", gameObject);
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", "E", gameObject);

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            AkSoundEngine.PostEvent("Play_Toning_v3_FundamentalOnly", gameObject);
            AkSoundEngine.PostEvent("Play_Toning_v3_HarmonyOnly", gameObject);
        }
        if(Input.GetKeyUp(KeyCode.T))
        {
            AkSoundEngine.PostEvent("Stop_Toning_v3_FundamentalOnly", gameObject);
            AkSoundEngine.PostEvent("Stop_Toning_v3_HarmonyOnly", gameObject);
        }
        if(Input.GetKeyDown(KeyCode.F))
        {
            fundamentalCount++;
            int mod = fundamentalCount % 3;
            int note = 0;
            
            switch(mod)
            {
                case 0:
                    note = 0;
                    AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", ConvertIntToNote(note), gameObject);
                    break;
                case 1:
                    note = 5;
                    AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", ConvertIntToNote(note), gameObject);
                    break;
                case 2:
                    note = 8;
                    AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", ConvertIntToNote(note), gameObject);
                    break;
            }

            Debug.Log("Fundamental Note: " + ConvertIntToNote(note));
        }
        if(Input.GetKeyDown(KeyCode.H))
        {
            harmonyCount++;
            int mod = harmonyCount % 3;
            int note = 0;

            switch(mod)
            {
                case 0:
                    note = 1;
                    AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", ConvertIntToNote(note), gameObject);
                    break;
                case 1:
                    note = 6;
                    AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", ConvertIntToNote(note), gameObject);
                    break;
                case 2:
                    note = 10;
                    AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", ConvertIntToNote(note), gameObject);
                    break;
            }

            Debug.Log("Harmony Note: " + ConvertIntToNote(note));
        }
        if(Input.GetKeyDown(KeyCode.W))
        {
            // Switch Sound Worlds (Sonoflore, Gentle, Shadow, Shruti)
            worldCount++;
            int mod = worldCount % 4;

            switch(mod)
            {
                case 0:
                    AkSoundEngine.SetState("SoundWorldMode","SonoFlore");
                    break;
                case 1:
                    AkSoundEngine.SetState("SoundWorldMode","Gentle");
                    break;
                case 2:
                    AkSoundEngine.SetState("SoundWorldMode","Shadow");
                    break;
                case 3:
                    AkSoundEngine.SetState("SoundWorldMode","Shruti");
                    break;
            }
        }
    }

    public string ConvertIntToNote(int noteNumber)
    {
        if (noteNumber >= 0 && noteNumber <= 11)
        {
            return Enum.GetName(typeof(NoteName), noteNumber);
        }
        else
        {
            throw new ArgumentException("Invalid noteNumber value");
        }
    }


    public enum NoteName
    {
        C,
        CsharpDflat,
        D,
        DsharpEflat,
        E,
        F,
        FsharpGflat,
        G,
        GsharpAflat,
        A,
        AsharpBflat,
        B
    }

}
