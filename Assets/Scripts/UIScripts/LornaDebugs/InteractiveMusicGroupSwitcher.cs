using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class InteractiveMusicGroupSwitcher : MonoBehaviour
{
    public WwiseInteractiveMusicManager wwiseInteractiveMusicManager;
    // Start is called before the first frame update
    public void DropDown(int index)
    {
        switch (index)
        {
            case 0:
                wwiseInteractiveMusicManager.currentSwitchState = "C";
                Debug.Log("Switching to C");
                wwiseInteractiveMusicManager.ChangeSwitchState();
                break;
            case 1:
                wwiseInteractiveMusicManager.currentSwitchState = "D";
                Debug.Log("Switching to D");
                wwiseInteractiveMusicManager.ChangeSwitchState();
                break;
            case 2:
                wwiseInteractiveMusicManager.currentSwitchState = "E";
                Debug.Log("Switching to E");
                wwiseInteractiveMusicManager.ChangeSwitchState();
                break;
            case 3:
                wwiseInteractiveMusicManager.currentSwitchState = "F";
                Debug.Log("Switching to F");
                wwiseInteractiveMusicManager.ChangeSwitchState();
                break;
            case 4:
                wwiseInteractiveMusicManager.currentSwitchState = "G";
                Debug.Log("Switching to F");
                wwiseInteractiveMusicManager.ChangeSwitchState();
                break;
            case 5:
                wwiseInteractiveMusicManager.currentSwitchState = "A";
                Debug.Log("Switching to A");
                wwiseInteractiveMusicManager.ChangeSwitchState();
                break;
            case 6:
                wwiseInteractiveMusicManager.currentSwitchState = "B";
                Debug.Log("Switching to B");
                wwiseInteractiveMusicManager.ChangeSwitchState();
                break;
        }
    }
}
