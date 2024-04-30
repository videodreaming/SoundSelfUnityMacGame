using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SubGameModeValue : MonoBehaviour
{
    public CSVReader csvReader;
    private TMP_Text tmpText; // TMP component
    // Start is called before the first frame update
    void Start()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
         tmpText.text = csvReader.GameSettings.SubGameMode;
    }
}
