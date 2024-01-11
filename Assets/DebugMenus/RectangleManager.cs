using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectangleManager : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public GameObject rectanglePrefab;
    public GameObject SelectedRectangle;
    public Canvas canvas;  
    public bool voiceOn;

    // Start is called before the first frame update
    void Start()
    {
        GameObject rectangle = Instantiate(rectanglePrefab, new Vector3(0, 0, 0), Quaternion.identity, canvas.transform);
        SelectedRectangle = rectangle;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.R))
        {
            if (SelectedRectangle != null)
            {
                Debug.Log(SelectedRectangle.transform.localScale.x);
                SelectedRectangle.transform.position = new Vector3(transform.position.x + 0.05f, transform.position.y, transform.position.z);
                SelectedRectangle.transform.localScale = new Vector3(transform.localScale.x + 0.001f, transform.localScale.y, transform.localScale.z);
            }
            else
            {
                // Instantiate the rectangle if it doesn't exist
                //GameObject rectangle = Instantiate(rectanglePrefab, new Vector3(0, 0, 0), Quaternion.identity, canvas.transform);
                //SelectedRectangle = rectangle;
            }
        }
    }
}
