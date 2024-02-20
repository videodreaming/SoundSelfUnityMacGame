using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugMenuController : MonoBehaviour
{
    public GameObject menu1;
    //public GameObject menu2;
    //public GameObject menu3;

    private int currentMenu = 1;
    void Start(){
        menu1.SetActive(false);
      //  menu2.SetActive(false);
        //menu3.SetActive(false);
    }
    void Update()
    {
        // Check for Tab key press
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchDebugMenu();
        }
    }
    void SwitchDebugMenu()
    {
        // Disable current menu
        switch (currentMenu)
        {
            case 1:
                menu1.SetActive(false);
                break;
          //  case 2:
            //    menu2.SetActive(false);
              //  break;
            //case 3:
              //  menu3.SetActive(false);
                //break;
            case 4:
                menu1.SetActive(false);
            //    menu2.SetActive(false);
              //  menu3.SetActive(false);
                break;
        }

        // Increment current menu index
        currentMenu++;

        // Loop back to the first menu if it exceeds the total number of menus
        if (currentMenu > 4)
        {
            currentMenu = 1;
        }
        // Enable the new current menu
        switch (currentMenu)
        {
            case 1:
                menu1.SetActive(true);
                break;
            /*case 2:
                menu2.SetActive(true);
                break;
            case 3:
                menu3.SetActive(true);
                break;*/
            case 4:
            break;
        }
    }
}
