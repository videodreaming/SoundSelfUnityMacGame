using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemController : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneIntepreter;
    public GameValues gameValues;
    private ParticleSystem ps;

    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        // Change the start color of the particles to red
        var main = ps.main;
        main.startColor = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        if (imitoneIntepreter != null)
        {
            var emission = ps.emission;
            // Set the emission rate based on the pitch_hz variable multiplied by ChantLerpSlow
            emission.rateOverTime = imitoneIntepreter.pitch_hz * gameValues._chantLerpFast;

            // Set the start speed of the particles based on _chantCharge
            var main = ps.main;
            main.startSpeed = 5f * Mathf.Pow(2f, gameValues._cChantCharge * 2f);
            main.startSize = 2f * Mathf.Pow(2f, gameValues._chantLerpSlow * 2f);
        }
    }
}