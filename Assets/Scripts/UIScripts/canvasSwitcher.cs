using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class canvasSwitcher : MonoBehaviour
{
    public DevelopmentMode developmentMode;
    public Canvas canvas1;
    public Canvas canvas2;
    public Canvas canvas3;
    public Canvas mainCanvas;

    public Canvas calibrationCanvas;
    public Canvas buttonCanvas;

    public bool UIDevMode = false;
    // Start is called before the first frame update

    void Start()
    {
        canvas1 = GameObject.Find("Canvas1").GetComponent<Canvas>();
        canvas2 = GameObject.Find("Canvas2").GetComponent<Canvas>();
        canvas3 = GameObject.Find("Canvas3").GetComponent<Canvas>();
        canvas1.enabled = false;
        canvas2.enabled = false;
        canvas3.enabled = false;
        calibrationCanvas.enabled = false;
        mainCanvas.enabled = false;
        buttonCanvas.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (UIDevMode)
        {
            if (developmentMode.configureMode)
            {
                canvas1.enabled = false;
                canvas2.enabled = false;
                canvas3.enabled = false;
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    canvas1.enabled = true;
                    canvas2.enabled = false;
                    canvas3.enabled = false;
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    canvas1.enabled = false;
                    canvas2.enabled = true;
                    canvas3.enabled = false;
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    canvas1.enabled = false;
                    canvas2.enabled = false;
                    canvas3.enabled = true;
                }
                if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    canvas1.enabled = false;
                    canvas2.enabled = false;
                    canvas3.enabled = false;
                }
            }
        }
    }

    public void SwitchToMainCanvas()
    {
        canvas1.enabled = false;
        canvas2.enabled = false;
        canvas3.enabled = false;
        mainCanvas.enabled = true;
        calibrationCanvas.enabled = false;
    }

    public void SwitchToCalibrationCanvas()
    {
        canvas1.enabled = false;
        canvas2.enabled = false;
        canvas3.enabled = false;
        mainCanvas.enabled = false;
        calibrationCanvas.enabled = true;
    }
}
