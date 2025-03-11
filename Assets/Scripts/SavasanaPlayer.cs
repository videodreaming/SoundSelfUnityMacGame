using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavasanaPlayer : MonoBehaviour
{
    public WwiseVOManager wwiseVOManager;
    public bool playedThematicSavasana {get; private set;} = false;

    public void PlayThematicSavasana()
    {
        if (playedThematicSavasana)
        {
            Debug.LogWarning("Savasana: Thematic Savasana has already been played.");
            return;
        }
        //AkSoundEngine.PostEvent("Play_VO_ThematicSavasana", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, wwiseVOManager.ClosingCallBackFunction, null);  
        AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, wwiseVOManager.ClosingCallBackFunction, null);
        playedThematicSavasana = true;
    }
}
