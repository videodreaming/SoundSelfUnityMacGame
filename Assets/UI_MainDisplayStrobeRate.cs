using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_MainDisplayStrobeRate : MonoBehaviour
{
    public LightControl lightControl;

    public TextMeshProUGUI strobeRateText;
    // Start is called before the first frame update
    void Start()
    {
        strobeRateText = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        strobeRateText.text = "Strobe Rate: " + lightControl._rate.ToString("F2") + " Hz";
    }
}
