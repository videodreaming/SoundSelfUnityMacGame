using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VOManager : MonoBehaviour
{
 
    // Start is called before the first frame update
    void Start()
    {

    }

    public void playOpening(string length)
    {
        if(length=="openingLong")
        {
            AkSoundEngine.SetSwitch("VO_Opening", length, gameObject);
        } else if(length=="openingShort") {
            AkSoundEngine.SetSwitch("VO_Opening", length, gameObject);
        } else if(length=="openingPassive"){
            AkSoundEngine.SetSwitch("VO_Opening", length, gameObject);
        }
        AkSoundEngine.PostEvent("Play_VO_Opening", gameObject);
    }

    public void playNewVOclip()
    {

    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            AkSoundEngine.PostEvent("Play_LinearMusic_Opening", gameObject);
        }
    }
}

