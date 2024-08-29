using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PauseStateUI : MonoBehaviour
{
    public CSVWriter csvWriter;
    private TMP_Text tmpText; // TMP component
    // Start is called before the first frame update
    void Start()
    {
        tmpText = GetComponent<TMP_Text>();
    }
    // Update is called once per frame
    void Update()
    {
        if(csvWriter.decryptedstatus == "paused")
        {
            tmpText.text = "Paused";
        }
        else
        {
            tmpText.text = "Resumed";
        }
    }
}
