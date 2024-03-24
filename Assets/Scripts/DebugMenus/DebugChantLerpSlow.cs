using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugChantLerpSlow : MonoBehaviour
{
    // Start is called before the first frame update
    public ImitoneVoiceIntepreter ImitoneVoiceIntepreter;
    public GameValues gameValues;
    public Image scaleImage;
    public Vector3 minScale = new Vector3(0f, 0f, 0f);
    public Vector3 maxScale = new Vector3(1f, 1f, 1f);
    public float normalizedValue;

    void Start()
    {
        normalizedValue = 0.5f;
        scaleImage = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        ScaleObjectNormalized();
    }

    void ScaleObjectNormalized()
    {
        // Interpolate between minScale and maxScale using normalizedValue
        Vector3 newScale = Vector3.Lerp(minScale, maxScale, gameValues._chantLerpSlow);
        // Apply the new scale to the object
        transform.localScale = newScale;
    }
}