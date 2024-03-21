using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserControlScript : MonoBehaviour
{
    public AudioManager audioManager;
    public RespirationTracker respirationTracker;
    public WwiseGlobalManager wwiseGlobalManager;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    // Start is called before the first frame update
    void Start()
    {
        if (imitoneVoiceIntepreter == null)
        {
            Debug.LogError("Exception: ImitoneVoiceIntepreter not found");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("Pause Placeholder Activated");
            imitoneVoiceIntepreter.SetMute(true);
            respirationTracker.ClearRespirationDictionaries();
        }
        else if(Input.GetKeyUp(KeyCode.Tab))
        {
            Debug.Log("Pause Placeholder Deactivated");
            imitoneVoiceIntepreter.SetMute(false);
        }
    }
}
