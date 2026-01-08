using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class canvasSwitcher : MonoBehaviour
{
    public static canvasSwitcher instance {get; private set;}
    public Canvas canvas1;
    public Canvas canvas2;
    public Canvas canvas3;
    public Canvas mainCanvas;

    public Canvas calibrationCanvas;
    public Canvas buttonCanvas;


    // Start is called before the first frame update
    void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

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
    }

    public void SwitchCanvas(int canvas)
    {
        switch (canvas)
        {
            case 1:
                canvas1.enabled = true;
                canvas2.enabled = false;
                canvas3.enabled = false;
                break;
            case 2:
                canvas1.enabled = false;
                canvas2.enabled = true;
                canvas3.enabled = false;
                break;
            case 3:
                canvas1.enabled = false;
                canvas2.enabled = false;
                canvas3.enabled = true;
                break;
            case 4:
                canvas1.enabled = false;
                canvas2.enabled = false;
                canvas3.enabled = false;
                break;
            default:
                // Optionally do nothing or disable all canvases
                break;
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
