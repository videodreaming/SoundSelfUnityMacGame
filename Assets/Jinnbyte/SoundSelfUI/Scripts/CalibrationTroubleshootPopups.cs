using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrationTroubleshootPopups : MonoBehaviour
{
    [SerializeField] private GameObject[] troubleshootPopups;
    // Start is called before the first frame update
    void OnEnable()
    {
        DisableAll();
        ShowTroubleshootPopup(0);
    }

    // Update is called once per frame
    void DisableAll()
    {
        foreach (GameObject popup in troubleshootPopups)
        {
            popup.SetActive(false);
        }
    }
    public void ShowTroubleshootPopup(int index)
    {
        DisableAll();
        if (index >= 0 && index < troubleshootPopups.Length)
        {
            troubleshootPopups[index].SetActive(true);
        }
    }


}
