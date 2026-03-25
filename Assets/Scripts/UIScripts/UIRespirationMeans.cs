using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class UIRespirationMeans : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI noteText;
    public void Update()
    {
        // Replace line below with the value you want to display.
        noteText.text = RespirationTracker.instance != null
            ? $"{RespirationTracker.instance._meanToneLength:F2} / {RespirationTracker.instance._meanRestLength:F2} / {RespirationTracker.instance._meanCycleLength:F2}"
            : "- / - / -";
    }
}
