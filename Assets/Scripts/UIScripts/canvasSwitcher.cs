using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class canvasSwitcher : MonoBehaviour
{
    public Canvas canvas1;
    public Canvas canvas2;
    public Canvas canvas3;
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
            canvas3.enabled = false;
        } 
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            canvas1.enabled = false;
            canvas2.enabled = true;
            canvas3.enabled = false;
        }
        if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            canvas1.enabled = false;
            canvas2.enabled = false;
            canvas3.enabled = true;
        }
        if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            canvas1.enabled = false;
            canvas2.enabled = false;
            canvas3.enabled = false;
        }
    }
}
