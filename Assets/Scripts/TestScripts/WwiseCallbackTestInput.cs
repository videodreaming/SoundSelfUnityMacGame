using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WwiseCallbackTestInput : MonoBehaviour
{
    public WwiseCallbackTest wwiseCallbackTest;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            wwiseCallbackTest.handleTutorialMeditation();
        }
    }
}
