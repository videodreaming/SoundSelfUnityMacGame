using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractiveMusicToneSwitcher : MonoBehaviour
{
    public MusicSystem1 musicSystem1;
    // Start is called before the first frame update
    public void DropDown(int index)
    {
        switch (index)
        {
            case 0:
                musicSystem1.currentToningState = "None";
                Debug.Log("Switching to none");
                musicSystem1.ChangeToningState();
                break;
            case 1:
                musicSystem1.currentToningState = "Gentle";
                Debug.Log("Switching to gentle");
                musicSystem1.ChangeToningState();
                break;
            case 2:
                musicSystem1.currentToningState = "Shadow";
                Debug.Log("Switching to none");
                musicSystem1.ChangeToningState();
                break;
            case 3:
                musicSystem1.currentToningState = "Shruti";
                Debug.Log("Switching to none");
                musicSystem1.ChangeToningState();
                break;
            case 4:
                musicSystem1.currentToningState = "SoneFlore";
                Debug.Log("Switching to none");
                musicSystem1.ChangeToningState();
                break;
        }
    }
}
