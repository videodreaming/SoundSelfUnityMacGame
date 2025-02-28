using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class ImageChanger : MonoBehaviour
{
    public LightControl lightControl;
    public TimeLeftScript timeLeftScript;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    private bool inGame = true;

    // Reference to the Image component on the UI
    public Image frequencyUIImage;
    public Image toggleUIImage;
    public Image experienceUIImage;

    // Array or list of sprites for different numbers
    public Sprite[] frequencySprites;
    public Sprite[] toggleUISprites;
    public Sprite[] experienceSprites;
 

    void Update()
    {
        if (lightControl._rate < 30.0f)
        {
            frequencyUIImage.sprite = frequencySprites[1];
        } else if (lightControl._rate > 12.0f && lightControl._rate <= 30.0f)
        {
            frequencyUIImage.sprite = frequencySprites[2];
        } else if (lightControl._rate > 8.0f && lightControl._rate <= 12.0f)
        {
            frequencyUIImage.sprite = frequencySprites[3];
        } else if (lightControl._rate > 4.0f && lightControl._rate <= 8.0f)
        {
            frequencyUIImage.sprite = frequencySprites[4];
        } else if (lightControl._rate > 0.5f && lightControl._rate <= 4.0f)
        {
            frequencyUIImage.sprite = frequencySprites[5];
        } else 
        {
            frequencyUIImage.sprite = frequencySprites[0];
        }

        if(timeLeftScript._timeLeft <= 0)
        {
            inGame = false;
        } 
        if(inGame)
        {
            if(imitoneVoiceIntepreter.toneActive)
            {
                experienceUIImage.sprite = experienceSprites[2];
            } else if (!imitoneVoiceIntepreter.toneActive)
            {
                experienceUIImage.sprite = experienceSprites [0];
            } 
        } else 
        {
            experienceUIImage.sprite = experienceSprites[4];
        }

    }   
}