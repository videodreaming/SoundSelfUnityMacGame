using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WwiseBGManager : MonoBehaviour
{
  void Awake()
    {
        DontDestroyOnLoad(gameObject);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }

    void Update()
    {
        AkSoundEngine.RenderAudio();
    }
}
