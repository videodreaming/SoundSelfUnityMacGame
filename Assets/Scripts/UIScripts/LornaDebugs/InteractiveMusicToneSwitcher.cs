using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractiveMusicToneSwitcher : MonoBehaviour
{
    public Sequencer sequencer;
    // Start is called before the first frame update
    public void DropDown(int index)
    {
        switch (index)
        {
            case 0:
                sequencer.currentToningState = "None";
                Debug.Log("Switching to none");
                sequencer.ChangeToningState();
                break;
            case 1:
                sequencer.currentToningState = "Gentle";
                Debug.Log("Switching to gentle");
                sequencer.ChangeToningState();
                break;
            case 2:
                sequencer.currentToningState = "Shadow";
                Debug.Log("Switching to none");
                sequencer.ChangeToningState();
                break;
            case 3:
                sequencer.currentToningState = "Shruti";
                Debug.Log("Switching to none");
                sequencer.ChangeToningState();
                break;
            case 4:
                sequencer.currentToningState = "SoneFlore";
                Debug.Log("Switching to none");
                sequencer.ChangeToningState();
                break;
        }
    }
}
