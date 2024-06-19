using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_ADJUNCT_LONG", gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
