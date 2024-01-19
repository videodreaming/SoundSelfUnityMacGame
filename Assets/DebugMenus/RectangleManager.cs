using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RectangleManager : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public GameObject rectanglePrefab;
    public GameObject SelectedRectangle;
    public GameObject InhaleRectangle;
    public Canvas canvas;  
    public bool voiceOn;
    private Vector3 startingPoint;
    private float amountToScale;
    private float amountToMove;

    void Start()
    {
        voiceOn = false;
        amountToScale = 1f;
        amountToMove = 1f;
        startingPoint = new Vector3(0, transform.position.y, transform.position.z);
    }

    void FixedUpdate()
    {
    if(Input.GetKey(KeyCode.R)){
        if(SelectedRectangle != null){
            RectTransform rectTransform = SelectedRectangle.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + amountToScale, rectTransform.sizeDelta.y);
            }
           if(InhaleRectangle != null){
            InhaleRectangle.tag = "oldrect";
            }
            InhaleRectangle = null;
        } else {
            GameObject rectangle = Instantiate(rectanglePrefab, startingPoint - new Vector3(0.0f, 400.0f, 0.0f), Quaternion.identity, canvas.transform);
            SelectedRectangle = rectangle;
            Image imageComponent = SelectedRectangle.GetComponent<Image>(); // Get the Image component
            if (imageComponent != null)
            {
                imageComponent.color = Color.blue;
            }
        }
    } else {
        if(InhaleRectangle != null){
            RectTransform rectTransform = InhaleRectangle.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + amountToScale, rectTransform.sizeDelta.y);
        }
        if(SelectedRectangle != null){
            SelectedRectangle.tag = "oldrect";
        }
        
        SelectedRectangle = null;
        } else {
            GameObject rectangle = Instantiate(rectanglePrefab, startingPoint - new Vector3(0.0f, 400.0f, 0.0f), Quaternion.identity, canvas.transform);
            InhaleRectangle = rectangle;
            Image imageComponent = InhaleRectangle.GetComponent<Image>(); // Get the Image component
            if (imageComponent != null)
            {
                imageComponent.color = Color.red;
            }
        }
    }
        GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag("oldrect");
        // Loop through each GameObject and apply the position change
        foreach (GameObject obj in objectsWithTag)
        {
            obj.transform.position = new Vector3(obj.transform.position.x + amountToMove, obj.transform.position.y, obj.transform.position.z);
        }
    }
}
