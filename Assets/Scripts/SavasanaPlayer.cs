using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavasanaPlayer : MonoBehaviour
{
    public WwiseVOManager wwiseVOManager;
    public bool playedThematicSavasana = false;

    public void PlayThematicSavasana()
    {
        playedThematicSavasana = true;
        AkSoundEngine.PostEvent("Play_VO_ThematicSavasana", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, ClosingCallBackFunction, null);  
        if(wwiseVOManager.firstTimeUser)
        {
            AkSoundEngine.SetSwitch("VO_ClosingGoodbye","Long",gameObject);
        } else if(wwiseVOManager.firstTimeUser == false)
        {
            AkSoundEngine.SetSwitch("VO_ClosingGoodbye","Short",gameObject);
        }
        
        Debug.Log("ThematicSavasana_ShouldBePlaying");
    }
    
    public void ClosingCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            Debug.Log("WWise_VO: Callback triggered: " + in_type);
            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
            if (musicSyncInfo.userCueName == "Cue_ThematicSavasana_Start")
            {
                Debug.Log("WWise_VO: Cue_ThematicSavasana_Start");
            } else if (musicSyncInfo.userCueName == "Cue_ThematicSavansana_End")
            {
                Debug.Log("Wwise_VO: Cue_ThematicSavasana_End");
            }
        }
    }
}
