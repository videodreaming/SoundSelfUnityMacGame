using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;



public class WwiseVOManager : MonoBehaviour
{
    public AudioManager audioManager;

    private bool pause = true;
    private float postureSwitchCountDown = 110.0f;
    private bool postureSwitchPlayed = false;
    private float ThematicContentCountDown = 184.0f;
    private bool ThematicContentPlayed = false;
    private bool ThematicandPostureCountdown = false;

    public string ThematicContent = null;
    public string ThematicSavasana = null;
    public string VO_ClosingGoodbye = null;
    public bool firstTimeUser = true;
    public bool layingDown = true;
    public string currentlyPlaying;


    public CSVWriter csvWriter;


    void Awake()
    {

    }


    void Start()
    {
        AkSoundEngine.SetSwitch("VO_ThematicContent","Peace", gameObject);
        assignVOs();
        playOpening();
        currentlyPlaying ="VO_OPENING_SEQUENCE";
        if(firstTimeUser)
        {
            ThematicContentCountDown = 184.0f;
            postureSwitchCountDown = 110.0f;
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_LONG", gameObject);  
        } else {
            ThematicContentCountDown = 110.0f;
            postureSwitchCountDown = 75.0f;
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_SHORT", gameObject);
        }
        ThematicandPostureCountdown = true;
    }
    
    void CueTest()
    {
        Debug.Log("CueTest");
    }

    void Update()
    {
        if(ThematicandPostureCountdown)
        {
            if(postureSwitchCountDown > 0.0f)
            {
                postureSwitchCountDown -= Time.deltaTime;
            }
            if(ThematicContentCountDown > 0.0f)
            {
                ThematicContentCountDown -= Time.deltaTime;
            }

        }
        if(postureSwitchCountDown <= 0.0f )
        {
            if(!postureSwitchPlayed)
            {
                postureSwitchPlayed = true;
                AkSoundEngine.PostEvent("Play_VO_POSTURE_SWITCH",gameObject);
            }

        }
        if(ThematicContentCountDown <= 0.0f)
        {
            if(!ThematicContentPlayed)
            {
                Debug.Log("In Thamtic Switch");
                ThematicContentPlayed = true;
                currentlyPlaying = "VO_OPENING_SEQUENCE";
                AkSoundEngine.PostEvent("Play_VO_OPENING_THEMATIC_SWITCH", gameObject,(uint)AkCallbackType.AK_MusicSyncUserCue, MyCallbackFunction, null);
            }
            
        }
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
           // AkSoundEngine.SetSwitch("VO_Opening", "openingLong", gameObject);
            //AkSoundEngine.SetSwitch("VO_Somatic", "long", gameObject);
        } else if (firstTimeUser == false)
        {
           // AkSoundEngine.SetSwitch("VO_Opening", "openingShort", gameObject);
           // AkSoundEngine.SetSwitch("VO_Somatic", "short", gameObject);
        } else 
        {
            //AkSoundEngine.SetSwitch("VO_Opening", "openingPassive", gameObject);
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
            //AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_ADJUNCT_LONG", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
        } else 
        {
            AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_ADJUNCT_SHORT", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
        }
    }

    void PlayNext()
    {
        Debug.Log("PlaynextRAN");
            if(currentlyPlaying == "VO_OPENING_SEQUENCE")
            {
                Debug.Log("fdsasdfas");
                AkSoundEngine.PostEvent("Play_SIGH_QUERY_SEQUENCE_1", gameObject, (uint)AkCallbackType.AK_EndOfEvent, MyCallbackFunction, null);
                AkSoundEngine.PostEvent("Play_SIGH_QUERY_SEQUENCE_1_SFX", gameObject);
                currentlyPlaying ="Play_SIGH_QUERY";
            } else if (currentlyPlaying == "Play_SIGH_QUERY")
            {
                Debug.Log("FinishedupSIGHQUERY");
                AkSoundEngine.PostEvent("Play_SOMATIC_SEQUENCE",gameObject);
                currentlyPlaying = "SOMATICSEQUENCE";
            }

    }

        void MyCallbackFunction(object in_cookie, AkCallbackType in_type, object in_info)
        {
            if (in_type == AkCallbackType.AK_MusicSyncUserCue)
            {
                Debug.Log("cue hit");
                PlayNext();
            }   
            if (in_type == AkCallbackType.AK_EndOfEvent)
            {
                // Call OnAudioFinished only if the AudioManager state is not SighElicitation1
                if (audioManager.currentState == AudioManager.AudioManagerState.SighElicitation1 || audioManager.currentState == AudioManager.AudioManagerState.SighElicitationFail1)
                {
                    pause = true;
                    StartCoroutine(StartSighElicitationTimer());
                } else if (audioManager.currentState == AudioManager.AudioManagerState.QueryElicitation1)
                {
                    StartCoroutine(StartQueryElicitationTimer());
                }
                else
                {
                    Debug.Log("callback ran");
                    audioManager.OnAudioFinished();
                    PlayNext();
                }
            }  
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

