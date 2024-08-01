using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTrackerScript : MonoBehaviour
{
    public float TotalElapsedTime;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        TotalElapsedTime += Time.deltaTime;
    }
}
