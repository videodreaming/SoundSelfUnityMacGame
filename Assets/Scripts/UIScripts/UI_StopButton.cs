using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_StopButton : MonoBehaviour
{
    public Button stopButton;

    void Start()
    {
        Button btn = stopButton.GetComponent<Button>();
        btn.onClick.AddListener(StopButton);
    }

    void StopButton()
    {
        Debug.Log("Stop Button Clicked");
        GameObject.Find("GameManager").GetComponent<GameManagement>().EndGame();

    }
}
