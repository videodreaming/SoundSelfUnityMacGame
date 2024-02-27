using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WwiseGlobalManager : MonoBehaviour
{
    public InteractiveMusicManager InteractiveMusicManager;
    public LinearMusicManager LinearMusicManager;
    public VOManager VOManager;

    // Start is called before the first frame update
    void Start()
    {
        VOManager.playOpening("openingPassive");

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha0)) 
        {
            InteractiveMusicManager.setallRTPCValue(0.0f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha1)) 
        {
            InteractiveMusicManager.setallRTPCValue(10.0f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            InteractiveMusicManager.setallRTPCValue(20.0f);
        }
         if(Input.GetKeyDown(KeyCode.Alpha3)) 
        {
            InteractiveMusicManager.setallRTPCValue(30.0f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha4)) 
        {
            InteractiveMusicManager.setallRTPCValue(40.0f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha5)) 
        {
            InteractiveMusicManager.setallRTPCValue(50.0f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha6)) 
        {
            InteractiveMusicManager.setallRTPCValue(60.0f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha7)) 
        {
            InteractiveMusicManager.setallRTPCValue(70.0f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha8)) 
        {
            InteractiveMusicManager.setallRTPCValue(80.0f);
        }
        if(Input.GetKeyDown(KeyCode.Alpha9)) 
        {
            InteractiveMusicManager.setallRTPCValue(90.0f);
        }
    }

    public void NewAudioState(string newStateName)
    {
        
    }
}
