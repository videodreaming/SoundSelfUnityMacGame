using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserOutput : MonoBehaviour
{
    public DevelopmentMode developmentMode;
    public float respirationRate;
    public float averageVolume;
    public float averagePitch;
    void Start()
    {
        respirationRate = 10.0f;
        averageVolume = 50.0f;
        averagePitch = 30.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if(developmentMode.developmentMode)
        {
            if(Input.GetKeyDown(KeyCode.A))
            {
                respirationRate = respirationRate + 1.0f;
                averagePitch = averagePitch + 1.0f;
                averageVolume = averageVolume + 1.0f;
            }

            if(Input.GetKeyDown(KeyCode.B))
            {
                Application.Quit();
            }
        }
    }
}