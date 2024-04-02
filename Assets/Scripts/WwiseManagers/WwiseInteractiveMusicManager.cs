using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WwiseInteractiveMusicManager: MonoBehaviour
{
    public string currentSwitchState = "B";
    public float InteractiveMusicSilentLoopsRTPC = 0.0f;
    public float HarmonySilentVolumeRTPC = 0.0f;
    public float FundamentalSilentVolumeRTPC = 0.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", 30.0f, gameObject);
        AkSoundEngine.PostEvent("Play_InteractiveMusicSystem_SilentLoops", gameObject);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha7)) 
        {
            currentSwitchState = "C";
            ChangeSwitchState();
        }
        if(Input.GetKeyDown(KeyCode.Alpha8)) 
        {
            currentSwitchState = "G";
            ChangeSwitchState();
        }
        if(Input.GetKeyDown(KeyCode.Alpha9)) 
        {
            currentSwitchState = "B";
            ChangeSwitchState();
        }
        if(Input.GetKeyDown(KeyCode.Alpha0)) 
        {
            currentSwitchState = "A";
            ChangeSwitchState();
        }
    }



    
    public void ChangeSwitchState()
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup", currentSwitchState, gameObject);
    }

    public void setallRTPCValue(float newRTPCValue)
    {
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", newRTPCValue, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", newRTPCValue, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", newRTPCValue, gameObject);
    }

    public void setHarmonySilentVolumeRTPCValue(float newHarmonyVolumeRTPC)
    {
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", newHarmonyVolumeRTPC, gameObject);
    }
    public void setFundamentalSilentVolumeRTPCValue(float newFundamentalVolumeRTPC)
    {
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", newFundamentalVolumeRTPC, gameObject);
    }
    public void setInteractiveMusicSilentLoopsRTPCValue(float newInteractiveMusicSilentLoopRTPC)
    {
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", newInteractiveMusicSilentLoopRTPC, gameObject);
    }
}
