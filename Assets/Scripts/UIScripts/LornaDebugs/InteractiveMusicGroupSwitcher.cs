using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class InteractiveMusicGroupSwitcher : MonoBehaviour
{
    public Sequencer sequencer; //THESE FUNCTIONALITIES SHOULD BE REFACTORED INTO MUSICSYSTEM1
    // Start is called before the first frame update
    public void DropDown(int index)
    {
        switch (index)
        {
            case 0:
                sequencer.currentSwitchState = "C";
                Debug.Log("Switching to C");
                sequencer.ChangeSwitchState();
                break;
            case 1:
                sequencer.currentSwitchState = "D";
                Debug.Log("Switching to D");
                sequencer.ChangeSwitchState();
                break;
            case 2:
                sequencer.currentSwitchState = "E";
                Debug.Log("Switching to E");
                sequencer.ChangeSwitchState();
                break;
            case 3:
                sequencer.currentSwitchState = "F";
                Debug.Log("Switching to F");
                sequencer.ChangeSwitchState();
                break;
            case 4:
                sequencer.currentSwitchState = "G";
                Debug.Log("Switching to F");
                sequencer.ChangeSwitchState();
                break;
            case 5:
                sequencer.currentSwitchState = "A";
                Debug.Log("Switching to A");
                sequencer.ChangeSwitchState();
                break;
            case 6:
                sequencer.currentSwitchState = "B";
                Debug.Log("Switching to B");
                sequencer.ChangeSwitchState();
                break;
        }
    }
}
