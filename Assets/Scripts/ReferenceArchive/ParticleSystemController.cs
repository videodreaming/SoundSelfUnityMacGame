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
            float chantLerpFast = GameValues.instance != null ? GameValues.instance._chantLerpFast : 0f;
            float chantCharge = GameValues.instance != null ? GameValues.instance._chantCharge : 0f;
            float chantLerpSlow = GameValues.instance != null ? GameValues.instance._chantLerpSlow : 0f;
            emission.rateOverTime = imitoneIntepreter.pitch_hz * chantLerpFast;

            // Set the start speed of the particles based on _chantCharge
            var main = ps.main;
            main.startSpeed = 5f * Mathf.Pow(2f, chantCharge * 2f);
            main.startSize = 2f * Mathf.Pow(2f, chantLerpSlow * 2f);
        }
    }
}