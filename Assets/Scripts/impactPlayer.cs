using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class impactPlayer : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Q key was pressed.");
            AkSoundEngine.PostEvent("Play_sfx_Impact_AVS_Only", gameObject);
        }

    }
}
