using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class UIAbsorption : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI noteText;
    public void Update(){
        //replace line below with the value you want to display.
        noteText.text = RespirationTracker.instance != null ? RespirationTracker.instance._absorption.ToString() : "-";
    }
}
