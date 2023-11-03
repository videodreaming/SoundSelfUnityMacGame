using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemController : MonoBehaviour
{
     public ImitoneVoiceIntepreter imitoneIntepreter;
     private ParticleSystem ps;
    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (imitoneIntepreter != null)
        {
            var emission = ps.emission;
            // Set the emission rate based on the pitch_hz variable from the other script
            emission.rateOverTime = imitoneIntepreter.pitch_hz;
        }
    }
}
