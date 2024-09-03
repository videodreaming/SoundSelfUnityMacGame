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
}
