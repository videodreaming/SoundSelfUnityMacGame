using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//TODO Look Into Deleting This Script
public class UserOutput : MonoBehaviour
{
    public float respirationRate;
    public float averageVolume;
    public float averagePitch;
    private bool developmentModeWarningFlag = false;
    void Start()
    {
        respirationRate = 10.0f;
        averageVolume = 50.0f;
        averagePitch = 30.0f;
    }

    // Update is called once per frame
    void Update()
    {


    }
}