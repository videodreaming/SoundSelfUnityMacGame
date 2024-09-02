using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class InteractiveMusicGroupSwitcher : MonoBehaviour
{
    public MusicSystem1 musicSystem1;
    public void DropDown(int index)
    {
        switch (index)
        {
            case 0:
                musicSystem1.currentSwitchState = "C";
                Debug.Log("Switching to C");
                musicSystem1.ChangeSwitchState();
                break;
            case 1:
                musicSystem1.currentSwitchState = "D";
                Debug.Log("Switching to D");
                musicSystem1.ChangeSwitchState();
                break;
            case 2:
                musicSystem1.currentSwitchState = "E";
                Debug.Log("Switching to E");
                musicSystem1.ChangeSwitchState();
                break;
            case 3:
                musicSystem1.currentSwitchState = "F";
                Debug.Log("Switching to F");
                musicSystem1.ChangeSwitchState();
                break;
            case 4:
                musicSystem1.currentSwitchState = "G";
                Debug.Log("Switching to F");
                musicSystem1.ChangeSwitchState();
                break;
            case 5:
                musicSystem1.currentSwitchState = "A";
                Debug.Log("Switching to A");
                musicSystem1.ChangeSwitchState();
                break;
            case 6:
                musicSystem1.currentSwitchState = "B";
                Debug.Log("Switching to B");
                musicSystem1.ChangeSwitchState();
                break;
        }
    }
}
