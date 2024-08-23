using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WwiseInteractiveMusicManager : MonoBehaviour
{
    public MusicSystem1 musicSystem1;
    public string currentSwitchState = "B"; // Default switch state
    public string currentToningState = "None";
    public float InteractiveMusicSilentLoopsRTPC = 0.0f;
    public float HarmonySilentVolumeRTPC = 0.0f;
    public float FundamentalSilentVolumeRTPC = 0.0f;
    private float UserNotToningThreshold = 10.0f;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public WwiseVOManager wwiseVOManager;
    public uint playingId;
    private bool toneActiveTriggered = false; // Flag to control the event triggering

    [SerializeField]
    private float elapsedTime = 0f; // Tracks the elapsed time since the start
    private int currentStage = 0; // Tracks the current stage of the sound world

    private float interactiveMusicExperienceTotalTime;
    private float finalStagePreLogicTime;

    private float WakeUpCounter; // Timer for the wakeup event
     private bool wakeUpEndSoonTriggered = false; // Flag to control the event "WakeupSoon" triggering


    private float soundWorldChangeTime; //Timer that triggers the sound world change
    private bool finalStagePreLogicExecuted = false; // Flag to control the final stage pre-logic execution
    public bool CFundamentalGHarmonyLock = false; // Flag to lock the fundamental and harmony to C and G respectively
    public CSVWriter csvWriter; //Reference to CSV Writer script

    private bool thisTonesImpactPlayed = false; // Tracks whether this Tone's Impact has been played


    //Fields for Guided Interactive Music Progression
    private int guidedVocalizationPlayCount = 0; // Tracks the number of times the sound has been played
    public bool IsTutorialTestingMicInput = false; // Tracks if we're in the humming session // i.e. if tutorial sequence is looking for sound
    private bool canPlayGuidedVocalization = true; // Flag to allow or block playing the sound
    private float guidedVocalizationThreshold = 3.0f;
    public bool disableMicrophoneResponsiveness = true; // Flag to disable microphone responsiveness
    private bool disableMicrophoneResponsivenessDebugFlag = false; // please keep this "false" so we get alerts if it turns off on first frame


    // Start is called before the first frame update
    void Start()
    {
        WakeUpCounter = 2280.0f;
        interactiveMusicExperienceTotalTime = 1245.0f;
        soundWorldChangeTime = interactiveMusicExperienceTotalTime / 4;
        finalStagePreLogicTime = 15f; 
        
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
        
        musicSystem1.fundamentalNote = 9;
        AkSoundEngine.SetRTPCValue("InteractiveMusicSilentLoops", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("HarmonySilentVolume", 30.0f, gameObject);
        AkSoundEngine.SetRTPCValue("FundamentalSilentVolume", 30.0f, gameObject);
    }



    public void userToningToChangeFundamental(string fundamentalNote)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup_12Pitches_FundamentalOnly", fundamentalNote,gameObject);
        Debug.Log("Fundamental Note: " + ConvertIntToNote(musicSystem1.fundamentalNote));
    }
    public void changeHarmony(string harmonyNote)
    {
        AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", harmonyNote, gameObject);
        Debug.Log("Harmony Note: " + ConvertIntToNote(musicSystem1.harmonyNote));
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
        if(wwiseVOManager.interactive == true)
        {
            HandleGuidedVocalization();
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= soundWorldChangeTime)
            {
                if (currentStage == 0)
                {
                    Debug.Log("Stage 0 SonoFlore");
                }
                Debug.Log("Stage 1 Gentle");
                elapsedTime = 0f; // Reset elapsed time
                currentStage++; // Move to the next stage
                
                switch (currentStage)
                {
                    case 1:
                        Debug.Log("Stage 1 Gentle");
                        AkSoundEngine.SetState("SoundWorldMode", "Gentle");
                        break;
                    case 2:
                        Debug.Log("Stage 2 Shadow");
                        AkSoundEngine.SetState("SoundWorldMode", "Shadow");
                        break;
                    case 3:
                        Debug.Log("Stage 3 Shruti");
                        AkSoundEngine.SetState("SoundWorldMode", "Shruti");
                        break;
                    default:
                    wwiseVOManager.interactive = false;
                    Debug.Log("Stage 4 Final");
                    AkSoundEngine.PostEvent("Stop_InteractiveMusicSystem", gameObject);
                    wwiseVOManager.PassBackToVOManager();
                    break;
                }   
            }
            if (currentStage == 3 && !finalStagePreLogicExecuted)
            {
                // Calculate the remaining time before the final logic execution
                float timeRemainingForFinalLogic = soundWorldChangeTime - elapsedTime;

                if (timeRemainingForFinalLogic <= finalStagePreLogicTime)
                {
                    CFundamentalGHarmonyLock = true;
                    finalStagePreLogicExecuted = true;
                    AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_FundamentalOnly", "C", gameObject);
                    AkSoundEngine.SetSwitch("InteractiveMusicSwitchGroup3_12Pitches_HarmonyOnly", "G", gameObject);
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
                Debug.Log("this tone is now longer than 8s");
                if(!thisTonesImpactPlayed)
                {
                    Debug.Log("impact");
                    AkSoundEngine.PostEvent("Play_sfx_Impact",gameObject);
                    thisTonesImpactPlayed = true;   
                }

            }
        }

        if (disableMicrophoneResponsiveness != disableMicrophoneResponsivenessDebugFlag)
        {
            // Pring a debug message on bool change
            Debug.Log("Microphone Responsiveness is now: " + disableMicrophoneResponsiveness);
            disableMicrophoneResponsivenessDebugFlag = disableMicrophoneResponsiveness;
        }
    }

    private void HandleGuidedVocalization() // Robin naming convention feedback: let's call this "Tutorial"
    {
        float t = 0.0f;
        
        if ((guidedVocalizationPlayCount < 5) && canPlayGuidedVocalization)
        {
            // variable IsTutorialTestingMicInput should be renamed to IsTutorialTestingMicInput
            // One more thing to be slightly mindful of - I'm going to want to use this variable, elsewhere in the scripts,
            // to disable some of the AVS and music behaviors connected to i.e. toneActive, chantCharge, etc.
            // So this should be accessible to me in some way that I can use it in other scripts. Ideally in this format:
            // public bool disableMicrophoneResponsiveness = true when we are in the tutorial && not testing.

            if (!IsTutorialTestingMicInput) //PSUEDOCODE: Make sure this won't work if we are in a tutorial correction
            {
                // BEGIN TESTING CODE BLOCK
                // Play the guided vocalization sound
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationHum", gameObject);
                guidedVocalizationPlayCount++;
                IsTutorialTestingMicInput = true;
                canPlayGuidedVocalization = false; // Block further plays until the next condition is met
            }
        } else if (guidedVocalizationPlayCount >= 5 && guidedVocalizationPlayCount <= 11 && canPlayGuidedVocalization)
        {
            if(!IsTutorialTestingMicInput)
            {
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAhh", gameObject);
                guidedVocalizationPlayCount++;
                IsTutorialTestingMicInput = true;
                canPlayGuidedVocalization = false; // Block further plays until the next condition is met
            }
        } else if (guidedVocalizationPlayCount >= 11 && guidedVocalizationPlayCount < 15 && canPlayGuidedVocalization)
        {
            if(!IsTutorialTestingMicInput)
            {
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationOhh", gameObject);
                guidedVocalizationPlayCount++;
                IsTutorialTestingMicInput = true;
                canPlayGuidedVocalization = false; // Block further plays until the next condition is met
            }
        } else if (guidedVocalizationPlayCount > 15 && guidedVocalizationPlayCount < 19 && canPlayGuidedVocalization )
        {
            if(!IsTutorialTestingMicInput)
            {
                AkSoundEngine.PostEvent("Play_VO_GuidedVocalizationAdvanced", gameObject);
                guidedVocalizationPlayCount++;
                //Instead of IsTutorialTestingMicInput = true, Make it trigger with Cue Logic from Wwise.
                IsTutorialTestingMicInput = true;

                canPlayGuidedVocalization = false; // Block further plays until the next condition is met
            }
        }
        
        if (IsTutorialTestingMicInput)
        {
            //PSUEDOCODE
            //float timeWaiting; 
            //timeWaiting += Time.deltaTime;

            // TO DO: Replace "imitoneVoiceInterpreter.imitoneConfidentActiveTimer" with a new timer that respects isTutorialTestingMicInput, 
            // Check if the player is humming confidently for more than 3 seconds
            if (imitoneVoiceIntepreter.imitoneConfidentActiveTimer > guidedVocalizationThreshold)
            {
                // FIRST, play a thump on success
                // THEN... ONCE !toneActiveConfident
                //FINISH TESTING BLOCK
                 //Player hummed for more than 3 seconds, so allow the next play
                /*
                MODIFICATION: We shouldn't make the following changes until the next frame toneActiveConfident == false
                 
                sIsTutorialTestingMicInput = false;
                canPlayGuidedVocalization = true; // Allow the next play
                disableMicrophoneResponsiveness = true; // This should be set to true when we are in the beginning of the tutorial
               */
            }
        }
        // REEF: Look and Test this
        // PSEUDOCODE for handling "Tutorial Corrections"
        // if (timeWaiting > 8f)
        // {
        //     // Play the correction sound
        //     AkSoundEngine.PosstEvent("PLAYLIST TO BE FILLED OUT", gameObject);
        //     IsTutorialTestingMicInput = true;
        //     canPlayGuidedVocalization = false; // Allow the next play
        // }

        // ANOTHER THING ROBIN WOULD LIKE TO ADD, THAT WILL REQUIRE LORNA'S HELP,
        // THAT CONTROLS WHEN WE ARE LOOKING FOR A TONE FROM THE PLAYER DYNAMICALLY
        //
        // STEP 1: We need a unity cues from Wwise, that Lorna will program into the audio files
        // Cue: "ReactivateTutorialMic" - this cue will be played near the end of the audio file, when we are ready to test again
        //
        // PSUEDOCODE
        // else if (cue == "ReactivateTutorialMic")
        // {
        //     IsTutorialTestingMicInput = true;
        // }

    }


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
