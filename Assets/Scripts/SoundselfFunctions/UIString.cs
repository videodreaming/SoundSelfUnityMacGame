using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Reflection;

public class UIStringToScreen : MonoBehaviour
{
    public GameObject targetObject;
    public string targetVariableName;
    [SerializeField] private TextMeshProUGUI displayText;

    public void Update()
    {
        var targetComponent = targetObject.GetComponent(targetObject.GetType());
        var targetVariable = targetComponent.GetType().GetField(targetVariableName, BindingFlags.Public | BindingFlags.Instance);
        if (targetVariable != null)
        {
            displayText.text = targetVariable.GetValue(targetComponent).ToString();
        }
        else
        {
            displayText.text = "Variable not found";
        }
    }
}