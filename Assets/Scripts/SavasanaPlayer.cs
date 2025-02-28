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
        AkSoundEngine.PostEvent("Play_VO_ThematicSavasana", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, wwiseVOManager.ClosingCallBackFunction, null);  
    }
    

}
