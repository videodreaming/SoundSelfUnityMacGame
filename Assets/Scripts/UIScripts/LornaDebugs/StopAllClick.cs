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
        Debug.Log("PlayToning");
    }

    public void SilentLoops()
    {
        Debug.Log("PlaySilentLoops");
    }
}
