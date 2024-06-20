using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;



public class WwiseVOManager : MonoBehaviour
{
    public AudioManager audioManager;
    private bool voOpeningEventPlayed = false;
    private bool voSighElicitation1Played = false;
    private bool voSighElicitationfail1Played = false;
    private bool QueryElicitation1Played = false;
    private bool QueryElicitationFail1Played = false;
    private bool QueryElicitationPassThankYou1Played = false;
    private bool ThematicContentPlayed = false;
    private bool PosturePlayed = false;
    private bool OrientationPlayed = false;
    private bool SomaticPlayed = false;
    private bool GuidededVocalizationHumPlayed = false;
    private bool GuidededVocalizationAhhPlayed = false;
    private bool GuidededVocalizationOhhPlayed = false;
    private bool GuidededVocalizationAdvancedPlayed = false;
    private bool UnGuidedVocalizationPlayed = false;
    private bool ThematicSavasanaPlayed= false;
    private bool SilentMeditationPlayed = false;
    private bool WakeUpPlayed = false;
    private bool EndingSoonPlayed = false;
    private bool SighElicitation2Played = false;
    private bool SighElicitationFail2Played = false;
    private bool QueryElicitationPass2Played = false;
    private bool ClosingGoodbyePlayed = false;
    private bool pause = true;

    public string ThematicContent = null;
    public string ThematicSavasana = null;
    public string VO_ClosingGoodbye = null;
    public bool firstTimeUser = true;
    public bool layingDown = true;
    public string currentlyPlaying;


    public CSVWriter csvWriter;

    // Start is called before the first frame update
    void Start()
    {
        assignVOs();
        playOpening();
        currentlyPlaying = "VO_OPENING_SEQUENCE";
        AkSoundEngine.PostEvent("Play_Somatic_Sequence", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
        Debug.Log("playing");
    }
    
    void assignVOs()
    {
        //Set VO_Posture
        if(layingDown)
        {
            AkSoundEngine.SetSwitch("VO_Posture","LieDown",gameObject);
        } else 
        {
            AkSoundEngine.SetSwitch("VO_Posture","Relax",gameObject);
        }


        if(firstTimeUser == true)
        {
            AkSoundEngine.SetSwitch("VO_Opening", "openingLong", gameObject);
            //AkSoundEngine.SetSwitch("VO_Somatic", "long", gameObject);
        } else if (firstTimeUser == false)
        {
            AkSoundEngine.SetSwitch("VO_Opening", "openingShort", gameObject);
           // AkSoundEngine.SetSwitch("VO_Somatic", "short", gameObject);
        } else 
        {
            AkSoundEngine.SetSwitch("VO_Opening", "openingPassive", gameObject);
            //AkSoundEngine.SetSwitch("VO_Somatic", "short", gameObject);
        }

        if(csvWriter.SubGameMode == "DieWell")
        {
            AkSoundEngine.SetSwitch("VO_ThematicContent", "DieWell", gameObject);
        } else if (csvWriter.SubGameMode == "Narrative")
        {
           AkSoundEngine.SetSwitch("VO_ThematicContent", "Narrative", gameObject);
        } else if (csvWriter.SubGameMode == "Peace")
        {
            AkSoundEngine.SetSwitch("VO_ThematicContent", "Peace", gameObject);
        } else if (csvWriter.SubGameMode == "Surrender")
        {
            AkSoundEngine.SetSwitch("VO_ThematicContent", "Surrender", gameObject);
        }
    
        if(csvWriter.GameMode == "Preperation")
        {

        } 
        if(csvWriter.GameMode == "Integration")
        {

        }
        if(csvWriter.GameMode == "Adjunctive")
        {

        }
    }

    public void playOpening()
    {
        if(firstTimeUser == true)
        {
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_ADJUNCT_LONG", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
        } else 
        {
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_ADJUNCT_SHORT", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
        }
    }

    // Update is called once per frame
    void Update()
    {
 
    }

    void PlayNext()
    {
        if(csvWriter.GameMode == "Adjunctive")
        {
            if(currentlyPlaying == "VO_OPENING_SEQUENCE")
            {
                AkSoundEngine.PostEvent("Play_SOMATIC_SEQUENCE", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                currentlyPlaying ="Play_SOMATIC_SEQUENCE";
            }
        }

    }

        void MyCallbackFunction(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                // Call OnAudioFinished only if the AudioManager state is not SighElicitation1
                if (audioManager.currentState == AudioManager.AudioManagerState.SighElicitation1 || audioManager.currentState == AudioManager.AudioManagerState.SighElicitationFail1)
                {
                    pause = true;
                    StartCoroutine(StartSighElicitationTimer());
                    resetFlags();
                } else if (audioManager.currentState == AudioManager.AudioManagerState.QueryElicitation1)
                {
                    StartCoroutine(StartQueryElicitationTimer());
                    resetFlags();
                }
                else
                {
                    audioManager.OnAudioFinished();
                    PlayNext();
                    resetFlags();
                }
            }  
        }
        void resetFlags()
        {
            voOpeningEventPlayed = false;
            voSighElicitation1Played = false;
            voSighElicitationfail1Played = false;
            QueryElicitation1Played = false;
            QueryElicitationFail1Played = false;
            QueryElicitationPassThankYou1Played = false;
            ThematicContentPlayed = false;
            PosturePlayed = false;
            OrientationPlayed = false;
            SomaticPlayed = false;
            GuidededVocalizationHumPlayed = false;
            GuidededVocalizationAhhPlayed = false;
            GuidededVocalizationOhhPlayed = false;
            GuidededVocalizationAdvancedPlayed = false;
            UnGuidedVocalizationPlayed = false;
            ThematicSavasanaPlayed= false;
            SilentMeditationPlayed = false;
            WakeUpPlayed = false;
            EndingSoonPlayed = false;
            SighElicitation2Played = false;
            SighElicitationFail2Played = false;
            QueryElicitationPass2Played = false;
            ClosingGoodbyePlayed = false;
        }
        
            IEnumerator StartSighElicitationTimer()
            {
                yield return new WaitForSeconds(6.0f); // Wait for the audio event to finish playing
                audioManager.OnAudioFinished();
                pause = false;
            }
            IEnumerator StartQueryElicitationTimer()
            {
                pause = true;
                audioManager.Query1CheckStarted = true;
                yield return new WaitForSeconds(30.0f); // Wait for the audio event to finish playing
                audioManager.OnAudioFinished();
                pause = false;
            }
                    
        
}

