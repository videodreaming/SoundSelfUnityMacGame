using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;

public class WwiseCallbackTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void handleTutorialMeditation()
    {
        AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationHum", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, TutorialCallBackFunction, null);
    }

    void TutorialCallBackFunction(object in_cookie, AkCallbackType in_type, AkCallbackInfo in_info)
    {
        Debug.Log("Callback triggered: " + in_type);
        AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
        if (musicSyncInfo.userCueName == "Cue_GuidedVocalization_Start")
        {
            Debug.Log("Cue_GuidedVocalization_Start");
        } else 
        {
            Debug.Log("Not the right cue");
        }
    }

}
