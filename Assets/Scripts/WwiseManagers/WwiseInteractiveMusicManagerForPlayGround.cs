using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WwiseInteractiveMusicManagerForPlayGround : MonoBehaviour
{
    public MusicSystem1ForPlayGround musicSystem1ForPlayGround;
    public WwiseAVSMusicManagerForPlayGround wwiseAVSMusicManager;
    public RespirationTrackerForPlayground respirationTracker;
    public GameValuesForPlayGround gameValues;
    public string currentSwitchState = "B";
    public string currentToningState = "None";
    public float InteractiveMusicSilentLoopsRTPC = 0.0f;
    public float HarmonySilentVolumeRTPC = 0.0f;
    public float FundamentalSilentVolumeRTPC = 0.0f;
    private float UserNotToningThreshold = 60.0f;
    public ImitoneVoiceIntepreterForPlayground imitoneVoiceIntepreter;
    public WwiseVOManagerForPlayGround wwiseVOManagerForPlayGround;
    public uint playingId;
    private bool toneActiveTriggered = false; // Flag to control the event triggering

    [SerializeField]
    private float elapsedTime = 0f; // Tracks the elapsed time since the start
    private int currentStage = 0; // Tracks the current stage of the sound world

    private float interactiveMusicExperienceTotalTime;
    private float finalStagePreLogicTime;

    private float WakeUpCounter;
     private bool wakeUpEndSoonTriggered = false; // Flag to control the event triggering


    private float soundWorldChangeTime;
    private bool finalStagePreLogicExecuted = false; 
    public bool CFundamentalGHarmonyLock = false;
    public CSVWriter csvWriter;

    private bool thisTonesImpactPlayed = false;

    // Start is called before the first frame update
    void Start()
    {
        WakeUpCounter = 2280.0f;
        interactiveMusicExperienceTotalTime = 1245.0f;
        soundWorldChangeTime = interactiveMusicExperienceTotalTime / 4;
        finalStagePreLogicTime = 15f; 
        
        StartCoroutine(AVS_Program_DynamicDrop());
        //Uncomment when CSV Writer is implemented
        /*if(csvWriter.GameMode == "Preperation")
        {
            WakeupCounter = 2280.0f;
            if(csvWriter.SubGameMode == "Peace")
            {   
                interactiveMusicExpereicneTotalTime = 1245.0f;
                finalStagePreLogicTime = 15f; 
            } else if (csvWriter.SubGameMode == "Narrative")
            {
                interactiveMusicExpereicneTotalTime = 1378.0f;
            } else if (csvWriter.SubGameMode == "Surrender")
            {   
                interactiveMusicExpereicneTotalTime = 1254.0f;
            }
            soundWorldChangeTime = interactiveMusicExperienceTotalTime / 4;
        }*/
        

        AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
        AkSoundEngine.SetState("SoundWorldMode","SonoFlore");
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly","A",gameObject);
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pithces_HarmonyOnly","E",gameObject);
        
        musicSystem1ForPlayGround.fundamentalNote = 9;
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", 30.0f, gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Fundamentalonly", gameObject);
        //AkSoundEngine.PostEvent("Play_SilentLoops3_Harmonyonly", gameObject);
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Fundamentalonly", "AVS System");
        //PlaySoundOnSpecificBus("Play_SilentLoops3_Harmonyonly", "Master Audio Bus");
    }



    public void userToningToChangeFundamental(string fundamentalNote)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup_12Pitches_FundamentalOnly", fundamentalNote,gameObject);
        Debug.Log("Fundamental Note: " + fundamentalNote);
    }
    public void changeHarmony(string harmonyNote)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", harmonyNote, gameObject);
        Debug.Log("Harmony Note: " + harmonyNote);
    }

   
    // Update is called once per frame
    void Update()
    {
        if(imitoneVoiceIntepreter.toneActive == false)
        {
            thisTonesImpactPlayed = false;
        }
        if(WakeUpCounter > -1.0f)
        {
            WakeUpCounter -= Time.deltaTime;
        } 
        
        if( WakeUpCounter <= 0.0f && !wakeUpEndSoonTriggered)
        {
            AkSoundEngine.PostEvent("Play_WakeUpEndSoon_SEQUENCE", gameObject);
            WakeUpCounter = -1.0f;
            wakeUpEndSoonTriggered = true;
        }
        if (imitoneVoiceIntepreter.toneActiveConfident)
        {
            if (!toneActiveTriggered)
            {
                toneActiveTriggered = true; // Set the flag to true after the event is triggered
            }
        }
        else
        {
            if (toneActiveTriggered)
            {
                //AkSoundEngine.StopPlayingID(playingId);
                toneActiveTriggered = false; // Reset the flag when toneActiveConfident becomes false
            }
        }
        if(wwiseVOManagerForPlayGround.interactive == true)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= soundWorldChangeTime)
            {
                if (currentStage == 0)
                {
                    Debug.Log("Stage 0 SonoFlore");
                }
                elapsedTime = 0f; // Reset elapsed time
                currentStage++; // Move to the next stage
                
                switch (currentStage)
                {
                    case 1:
                        Debug.Log("Stage 1 Gentle");
                        AkSoundEngine.SetState("SoundWorldMode", "Gentle");
                        wwiseAVSMusicManager.preferredColor = "Red";
                        break;
                    case 2:
                        Debug.Log("Stage 2 Shadow");
                        AkSoundEngine.SetState("SoundWorldMode", "Shadow");
                        wwiseAVSMusicManager.preferredColor = "Blue";
                        break;
                    case 3:
                        Debug.Log("Stage 3 Shruti");
                        AkSoundEngine.SetState("SoundWorldMode", "Shruti");
                        wwiseAVSMusicManager.preferredColor = "White";
                        break;
                    case 4:
                        Debug.Log("Stage 0 SonoFlore");
                        AkSoundEngine.SetState("SoundWorldMode", "SonoFlore");
                        wwiseAVSMusicManager.preferredColor = "Red";
                        currentStage = 0;
                        break;
                }   
            }

            if(imitoneVoiceIntepreter.imitoneConfidentInactiveTimer > UserNotToningThreshold)
            {
                AkSoundEngine.SetState("InteractiveMusicMode", "Environment");
            }
            else
            {
                AkSoundEngine.SetState("InteractiveMusicMode", "InteractiveMusicSystem");
            }

            if(imitoneVoiceIntepreter._tThisTone > imitoneVoiceIntepreter._activeThreshold4)
            {
                if(!thisTonesImpactPlayed)
                {
                    Debug.Log("this tone is now longer than 8s, play impact");

                    Debug.Log("impact");
                    AkSoundEngine.PostEvent("Play_sfx_Impact",gameObject);
                    thisTonesImpactPlayed = true;   
                }

            }
        }
    }

    //public void AVS_ProgramControl()
    //{
        //Let's do this:

        //When the lights first come on, fade relatively quickly from gamma to high alpha.
        //Then, fade relatively slowly from high alpha to 10hz.
        //Then, once absorption falls above a certain level, begin drop to theta.
        //Once in theta, do either gamma-bursts or bilateral, depending on absorption.
        //In last three minutes, bilateral strobing
        //In last 60 seconds, quickly rise back to Beta while in bilateral.

        //If we get a "change" at one of our holding places, switch to bilateral until next change.

    //}

    IEnumerator AVS_Program_DynamicDrop()
    {
        yield return null;

        float _absorptionThreshold = UnityEngine.Random.Range(0.8f, 3.5f);

        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Waiting for lights. Currently:" + wwiseAVSMusicManager.cycleRecent);

        if(gameValues.developmentPlayground)
        {
            wwiseAVSMusicManager.SetColorWorldByType("Red", 0.0f);
        }

        while((wwiseAVSMusicManager.cycleRecent == "dark") || (wwiseAVSMusicManager.cycleRecent == "Dark"))
        {
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Lights detected, set strobe to 45hz.");
        wwiseAVSMusicManager.SetStrobeRate(5.0f, 0.0f);
        float _timer = 10f;
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Initializeing drop from gamma to high alpha.");
        _timer = 30f;
        wwiseAVSMusicManager.SetStrobeRate(2.0f, _timer);
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        _timer = 120f;
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Begining drop from high alpha to 10hz.");
        wwiseAVSMusicManager.SetStrobeRate(1.0f, _timer);

        //now we will skip ahead if the absorption threshold is met.
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            if(respirationTracker._absorption > _absorptionThreshold)
            {
                Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. Absorption threshold met, starting Theta coroutine");
                //StartCoroutine(AVS_Program_DynamicDrop_Theta());
                break;
            }
            yield return null;
        }
        Debug.Log(WakeUpCounter + "| AVS Program: DynamicDrop. AT END OF PROGRAM SO FAR.");

    }

    //IEnumerator AVS_Program_DynamicDrop_Theta()
    //{
    //}

    public void ChangeToningState()
    {
        AkSoundEngine.SetState("SoundWorldMode", currentToningState);
    }

    public void ChangeSwitchState()
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup", currentSwitchState, gameObject);
    }

    public void setallRTPCValue(float newRTPCValue)
    {
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", newRTPCValue, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", newRTPCValue, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", newRTPCValue, gameObject);
    }

    public void setHarmonySilentVolumeRTPCValue(float newHarmonyVolumeRTPC)
    {
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", newHarmonyVolumeRTPC, gameObject);
    }

    public void setFundamentalSilentVolumeRTPCValue(float newFundamentalVolumeRTPC)
    {
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", newFundamentalVolumeRTPC, gameObject);
    }

    public void setInteractiveMusicSilentLoopsRTPCValue(float newInteractiveMusicSilentLoopRTPC)
    {
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", newInteractiveMusicSilentLoopRTPC, gameObject);
    }



        public enum NoteName
    {
        C,
        CsharpDflat,
        D,
        DsharpEflat,
        E,
        F,
        FsharpGflat,
        G,
        GsharpAflat,
        A,
        AsharpBflat,
        B
    }

    public string ConvertIntToNote(int noteNumber)
    {
        if (noteNumber >= 0 && noteNumber <= 11)
        {
            return Enum.GetName(typeof(NoteName), noteNumber);
        }
        else
        {
            throw new ArgumentException("Invalid noteNumber value");
        }
    }
}
