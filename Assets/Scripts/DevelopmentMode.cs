using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevelopmentMode : MonoBehaviour
{
    public bool developmentPlayground = false;
    public bool developmentMode = false;
    public bool configureMode = false; //not actually a development mode, used for configuring light and sound.

    void Awake()
    {
        if(developmentMode)
        {
            Debug.Log("AWAKE IN DEVELOPMENT MODE");
            if(developmentPlayground)
            {
                Debug.Log("AWAKE IN PLAYGROUND MODE");
            }
        }
        else
        {
            Debug.Log("AWAKE IN PRODUCTION MODE");
            developmentPlayground = false;
        }
    }

    public void LogChangeBool(string text, bool input){
        bool oldBoolInput = false;
        if(input != oldBoolInput){
            //Debug.Log(text + input);
            oldBoolInput = input;
        }
    }

    public void LogChangeFloat(string text, float input){
        float oldFloatInput = 0.0f;
        if(input != oldFloatInput){
            //Debug.Log(text + input);
            oldFloatInput = input;
        }
    }
}
