using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopAllClick : MonoBehaviour
{
    public void StopAll()
    {
        AkSoundEngine.StopAll();
    }

    public void Toning()
    {
        AkSoundEngine.PostEvent("Play_InteractiveMusicSystem_Toning", gameObject);
    }

    public void SilentLoops()
    {
        AkSoundEngine.PostEvent("Play_InteractiveMusicSystem_SilentLoops", gameObject);
    }
}
