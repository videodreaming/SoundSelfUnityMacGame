using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CurrentSession : MonoBehaviour
{
    public static UI_CurrentSession Instance {get; private set;}
    public string currentSession = "Introduction";
    public TextMeshProUGUI currentSessionText;

    // Update is called once per frame
    void Update()
    {
        currentSessionText.text = currentSession;
    }
}
