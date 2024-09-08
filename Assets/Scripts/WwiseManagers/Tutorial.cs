using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;


public class Tutorial : MonoBehaviour
{
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;
    public bool active = false;
    private bool primaryFlag = false;
    private bool correcting = false;
    float testThreshold = 5.0f;
    float failThreshold = 8.0f;
    string testVocalizationType = "";
    
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
            //THINGS TO DO:

            //Use a cue from WWise to change testVocalizationType from "hum" to "ahh" to "ohh" to "advanced", at the very beginning of the line being spoken.

            //MORE WWISE THINGS TO CHANGES
            //- Whenever he is talking, the "mic off" cue should happen at the start of his vo
            //- We should then trigger the "mic on" cue near the end (but not AT) the end, when he says "breathe in" or whatever.
            //- When the game is close to over (the 1 minute left logic in sequencer), we need to kill all behaviors here... 
            //- Set up a cue at the beginning of the last VO that triggers breaking all tests, cos we're done!       
    }
    
    private IEnumerator TestCoroutine()
    {
        float _failTimer = 0.0f;
        while(imitoneVoiceInterpreter._tThisTone < testThreshold)
        {
            if(!imitoneVoiceInterpreter.toneActive)
            {
                _failTimer += time.deltaTime;
                if(_failTimer > failThreshold)
                {
                    StartCoroutine(ProvideCorrection());                   
                    break;
                }
            }
            yield return null;
        }
        //PSUEDOCODE
        
        //AkSoundEngine.PostEvent("Play_Next_Test", gameObject); //(Play the next line of dialogue)
    }

    private IEnumerator ProvideCorrection()
    {
        testPrimary = false;
        testCorrection = true;
        if(testVocalizationType == "Hum")
        {
            AkSoundEngine.PostEvent("Play_VO_testRepairHum");
        } else if (testVocalizationType == "Ahh")
        {
            AkSoundEngine.PostEvent("Play_VO_testRepairAhh");
        } else if (testVocalizationType == "Ohh")
        {
            AkSoundEngine.PostEvent("Play_VO_testRepair");
        } else if (testVocalizationType == "Advanced")
        {
            AkSoundEngine.PostEvent("Play_VO_testRepair_Extended");
        }
        //Interrupt the primary playlist to offer a corrective. Then require a successful test to resume the primary playlist
    }
}