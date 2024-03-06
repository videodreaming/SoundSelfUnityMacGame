using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RectangleManager : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public RespirationTracker respirationTracker;
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
        startingPoint = new Vector3(0, transform.position.y +500.0f, transform.position.z);
    }

         //@REEF: (before undoing all my changes) I made the rectangle work as a function I can access from the respiration tracker
         // (maybe you can just have it access the dictionary directly instead of the dumb way I did it here), because I need to be able to visualize information about each cycle which may change dynamically. Other comments on how it needs to work:
        // 1. I need to be able to change the color of the rectangle if the state of the cycle becomes "invalid" (see code in RespirationTracker.cs). I need to see which particular rectangle (i.e. inhale or exhale) triggered the invalidation
        // IF the rectangle becomes invalid, it needs to change color to dark red if it was an inhale rectangle. if it becomes invalid and is a exhale, make it a dark blue 
        // 2. The rectangle needs to shorten again as it leaves the "measurement window" (see code in RespirationTracker.cs)
        // The correspending rectangle's Y scale should be tied to respirationTracker.thisbreathecycledata._cycleCount (This rectangle is purely tied to it's respirationtrack dictionary entry counterpart)
        // 3. When I changed this to a public void, I borked it, and it no longer moves to the right. That needs to be fixed. FIXED
        // 4. I need some text right above or on top of the rectangles that just tells me both respirationtracker._respirationRate and respirationtracker._respirationRateRaw.
        // print these floats onto the screen as a text object.

        //change colours to be green and blue instead of red and blue
   
    void FixedUpdate()
    {
    /*if(Input.GetKey(KeyCode.R)){
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
    }*/
}
}
