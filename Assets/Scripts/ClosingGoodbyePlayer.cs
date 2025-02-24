using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosingGoodbyePlayer : MonoBehaviour
{

    public WwiseVOManager wwiseVOManager;
    public bool playedClosingGoodbye = false;
    public TimeTrackerScript timeTrackerScript;
    private bool playedClosingGoodbyeOnce = false;
    private float timeToPlayClosingGoodbye;
    // Start is called before the first frame update
    

    void Update()
    {
       if(timeTrackerScript.TotalElapsedTime >= timeToPlayClosingGoodbye && !playedClosingGoodbyeOnce)
       {
            playedClosingGoodbyeOnce = true;
            wwiseVOManager.PlayClosingGoodbye();
       }
    }


}
