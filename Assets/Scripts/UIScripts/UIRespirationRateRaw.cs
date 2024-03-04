using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class UIRespirationRateRaw : MonoBehaviour
{
    public RespirationTracker RespirationTracker;
    [SerializeField] private TextMeshProUGUI noteText;
    public void Update(){
        //replace line below with the value you want to display.
        noteText.text = RespirationTracker._respirationRateRaw.ToString();
    }
}
