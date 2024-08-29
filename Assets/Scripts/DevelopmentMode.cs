using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevelopmentMode : MonoBehaviour
{
    public bool developmentPlayground = false;
    public bool developmentMode = false;

    void Awake()
    {
        if(developmentMode)
        {
            Debug.Log("AWAKE IN DEVELOPMENT MODE");
        }
        else
        {
            Debug.Log("AWAKE IN PRODUCTION MODE");
            developmentPlayground = false;
        }
    }
}
