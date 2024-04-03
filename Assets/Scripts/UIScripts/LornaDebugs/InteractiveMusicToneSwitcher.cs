using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractiveMusicToneSwitcher : MonoBehaviour
{
    public WwiseInteractiveMusicManager wwiseInteractiveMusicManager;
    // Start is called before the first frame update
    public void DropDown(int index)
    {
        switch (index)
        {
            case 0:
                wwiseInteractiveMusicManager.currentToningState = "None";
                Debug.Log("Switching to none");
                wwiseInteractiveMusicManager.ChangeToningState();
                break;
            case 1:
                wwiseInteractiveMusicManager.currentToningState = "Gentle";
                Debug.Log("Switching to gentle");
                wwiseInteractiveMusicManager.ChangeToningState();
                break;
            case 2:
                wwiseInteractiveMusicManager.currentToningState = "Shadow";
                Debug.Log("Switching to none");
                wwiseInteractiveMusicManager.ChangeToningState();
                break;
            case 3:
                wwiseInteractiveMusicManager.currentToningState = "Shruti";
                Debug.Log("Switching to none");
                wwiseInteractiveMusicManager.ChangeToningState();
                break;
            case 4:
                wwiseInteractiveMusicManager.currentToningState = "SoneFlore";
                Debug.Log("Switching to none");
                wwiseInteractiveMusicManager.ChangeToningState();
                break;
        }
    }
}
