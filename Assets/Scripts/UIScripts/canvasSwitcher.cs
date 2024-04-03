using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class canvasSwitcher : MonoBehaviour
{
    public Canvas canvas1;
    public Canvas canvas2;
    // Start is called before the first frame update
    void Start()
    {
        canvas1 = GameObject.Find("Canvas1").GetComponent<Canvas>();
        canvas2 = GameObject.Find("Canvas2").GetComponent<Canvas>();
        canvas2.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            canvas1.enabled = true;
            canvas2.enabled = false;
        } 
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            canvas1.enabled = false;
            canvas2.enabled = true;
        }
    }
}
