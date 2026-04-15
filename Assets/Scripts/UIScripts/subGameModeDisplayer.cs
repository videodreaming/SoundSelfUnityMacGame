using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class subGameModeDisplayer : MonoBehaviour
{

    public CSVLoader csvLoader;
    public TextMeshProUGUI modeText;

    // Start is called before the first frame update
    void Start()
    {
        modeText = GetComponent<TextMeshProUGUI>();
        string pack = csvLoader.GetCurrentContentPack();
        modeText.text = "Content pack: " + pack;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
