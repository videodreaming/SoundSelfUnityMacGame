using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevelopmentMode : MonoBehaviour
{
    public bool developmentPlayground = true;
    public bool developmentMode = true;

    void Start()
    {
        if(developmentMode)
        {
            Debug.Log("STARTED IN DEVELOPMENT MODE");
        }
        else
        {
            Debug.Log("STARTED IN PRODUCTION MODE");
            developmentPlayground = false;
        }
    }
}
