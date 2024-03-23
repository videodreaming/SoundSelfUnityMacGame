using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class UIRespirationMeans : MonoBehaviour
{
    public RespirationTracker RespirationTracker;
    [SerializeField] private TextMeshProUGUI noteText;
    public void Update()
    {
        // Replace line below with the value you want to display.
        noteText.text = $"{RespirationTracker._meanToneLength:F2} / {RespirationTracker._meanRestLength:F2} / {RespirationTracker._meanCycleLength:F2}";
    }
}
