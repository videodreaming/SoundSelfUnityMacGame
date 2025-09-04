using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class VerticalLayoutGroupController : MonoBehaviour
{
    public GameObject verticalLayoutGroupGameObject;
    public VerticalLayoutGroup verticalLayoutGroup;

    float originalfontSize = 36f; // Default font size
    float targetfontSize = 71f;
    
    // Start is called before the first frame update
    void Start()
    {
        verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
        List<TextMeshProUGUI> children = new List<TextMeshProUGUI>();
        foreach (Transform child in verticalLayoutGroupGameObject.transform)
        {
            TextMeshProUGUI textMeshPro = child.GetComponent<TextMeshProUGUI>();
            if (textMeshPro != null)
            {
                children.Add(textMeshPro);
            }
        }
    }
    public void highLightText(GameObject textObject)
    {
        TextMeshProUGUI textMeshPro = textObject.GetComponent<TextMeshProUGUI>();
        if (textMeshPro != null)
        {
            textMeshPro.color = new Color (104f/255f,117f/255f,255f/255f,1f); // Change color to custom blue
        }
    }

    public void unhighLightText(GameObject textObject)
    {
        TextMeshProUGUI textMeshPro = textObject.GetComponent<TextMeshProUGUI>();
        if (textMeshPro != null)
        {
            textMeshPro.color = Color.white; // Change color to white
        }
    }




    public IEnumerator scaleText(GameObject textObject, float duration)
    {
        TextMeshProUGUI textMeshPro = textObject.GetComponent<TextMeshProUGUI>();
        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();

        if (textMeshPro == null)
        {
            Debug.LogWarning("TextMeshProUGUI component not found.");
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            textMeshPro.fontSize = Mathf.SmoothStep(originalfontSize, targetfontSize, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textMeshPro.fontSize = targetfontSize; // Ensure it ends at the target size
    }

    public IEnumerator unScaleText(GameObject textObject, float duration)
    {
        TextMeshProUGUI textMeshPro = textObject.GetComponent<TextMeshProUGUI>();
        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();

        if (textMeshPro == null)
        {
            Debug.LogWarning("TextMeshProUGUI component not found.");
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            layoutElement.minHeight = Mathf.SmoothStep(layoutElement.minHeight, 0f, elapsedTime / duration);
            textMeshPro.fontSize = Mathf.SmoothStep(targetfontSize, originalfontSize, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textMeshPro.fontSize = originalfontSize; // Ensure it ends at the original size
    }
}
