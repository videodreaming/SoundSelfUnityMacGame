using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_StrobeRate : MonoBehaviour
{
    public LightControl lightControl;
    public TextMeshProUGUI strobeRateText;

    // Update is called once per frame
    void Update()
    {
        strobeRateText.text = lightControl._strobeRate.ToString();
    }
}
