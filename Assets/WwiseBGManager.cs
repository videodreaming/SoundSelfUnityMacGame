using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WwiseBGManager : MonoBehaviour
{
  void Awake()
    {
        DontDestroyOnLoad(gameObject); // optional, for persistence across scenes
    }

    void Update()
    {
        AkSoundEngine.RenderAudio();
    }
}
