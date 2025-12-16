using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if((DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode))
        {
            if(Input.GetKeyDown(KeyCode.A))
            {
                respirationRate = respirationRate + 1.0f;
                averagePitch = averagePitch + 1.0f;
                averageVolume = averageVolume + 1.0f;
            }
        }
        else if(DevelopmentMode.instance == null && !developmentModeWarningFlag)
        {
            developmentModeWarningFlag = true;
            Debug.LogWarning("User Output: DevelopmentMode instance is null. Cannot check development mode status.");
        }
    }
}