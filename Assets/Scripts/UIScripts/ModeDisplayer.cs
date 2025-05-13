using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ModeDisplayer : MonoBehaviour
{
    public CSVLoader csvLoader;
    public TextMeshProUGUI modeText;

    // Start is called before the first frame update
    void Start()
    {
        modeText = GetComponent<TextMeshProUGUI>();
        string currentMode = csvLoader.GetCurrentMode();    
        modeText.text = "Mode: " + currentMode;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
