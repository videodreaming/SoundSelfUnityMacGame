using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using AK.Wwise;
using Unity.VisualScripting;

//REFACTORING THOUGHTS FROM ROBIN
//WE SHOULD ENSURE THAT THE USERAUDIOSOURCE IS ONLY REFERENCED AND CONTROLLED FROM ONE SCRIPT

public class WwiseVOManager : MonoBehaviour
{
    public CSVLoader csvLoader;
    public Sequencer sequencer;
    public DevelopmentMode  developmentMode;
    public Director director;
    public CSVWriter CSVWriter;
    public LightControl lightControl;
    public MusicSystem1 musicSystem1;
    public ImitoneVoiceIntepreter imitoneVoiceIntepreter;
    public Tutorial tutorial;
    //public MusicSystem1 musicSystem1;
    //public RTPC silentFundamentalrtpcvolume;
    //public RTPC toningFundamentalrtpcvolume;
    //public RTPC silentHarmonyrtpcvolume;
    //public RTPC toningHarmonyrtpcvolume;
    //public float fadeDuration = 54.0f;
    //public float targetValue = 80.0f;
    private bool debugAllowMusicLogs = true;
    private bool pause = true;
    public bool firstTimeUser = true;
    public bool layingDown = true;
    private bool lightsInitialized = false;
    public CSVWriter csvWriter;
    private Coroutine countdownCoroutine; // Reference to the coroutines
    public float totalTimeOfPostUnguidedVocalizationContant;
    public float timeInUnguidedVocalization;
    private int currentStage = 0; //As Sonoflore
    private float totalTimeOfExperience;

    //private bool silentPlaying = false;

    void Start()
    {
        if(CSVLoader.gameMode == "Preperation" || CSVLoader.gameMode == "Skills Training")
        {
            Debug.Log("WWise_VO: Setting up for Preperation or Skills Training");
            totalTimeOfExperience = 2700.0f;
            if (CSVLoader.subGameMode == "Peace" || CSVLoader.subGameMode == "Mindfulness and Joy")
            {
                totalTimeOfPostUnguidedVocalizationContant = 889.0f;
                AkSoundEngine.SetSwitch("VO_ThematicContent", "Peace", gameObject);
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Peace", gameObject);
            } 
            else if (CSVLoader.subGameMode == "Narrative" || CSVLoader.subGameMode == "Psychological Flexibility")
            {

            Debug.Log("WWise_VO: Psychological Flexibility or Narrative");
                totalTimeOfPostUnguidedVocalizationContant = 742.0f;
                AkSoundEngine.SetSwitch("VO_ThematicContent", "Narrative", gameObject);
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Narrative", gameObject);
            } 
            else if (CSVLoader.subGameMode == "Surrender" || CSVLoader.subGameMode == "Psychedelic Prepeation")
            {
                totalTimeOfPostUnguidedVocalizationContant = 775.0f;
                AkSoundEngine.SetSwitch("VO_ThematicContent", "Surrender", gameObject);
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Surrender", gameObject);
            } 
            if(firstTimeUser)
            {
                //AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject,(uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);
                AkSoundEngine.PostEvent("Play_PREPARATION_OPENING_SEQUENCE_LONG", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);  
                AkSoundEngine.SetSwitch("VO_Somatic","Long",gameObject);
            } else {
                AkSoundEngine.PostEvent("Play_OPENING_SEQUENCE_SHORT", gameObject);
                AkSoundEngine.SetSwitch("VO_Somatic","Long",gameObject);
            }
        } else if (CSVLoader.gameMode == "Integration")
        {
            totalTimeOfExperience = 1500.0f;
            if(CSVLoader.subGameMode == "Fireflies")
            {
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Fireflies", gameObject);
            } else if (CSVLoader.subGameMode == "Kindness")
            {
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Kindness", gameObject);
            } else if (CSVLoader.subGameMode == "Metta")
            {
                AkSoundEngine.SetSwitch("VO_ThematicSavasana", "Metta", gameObject);
            }
            AkSoundEngine.PostEvent("Play_INTEGRATION_OPENING_SEQUENCE_SHORT", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, OpeningCallBackFunction, null);
        }
        //SOME IMPORTANT STARTUP BEHAVIORS ARE IN SEQUENCER.CS AND MUSICSYSTEM1.CS
        assignVOs();
        if(developmentMode.startAtStart) //NORMAL START
        {

        }
        else if (developmentMode.startInTutorial)
        {
            tutorial.StartTutorial();
            InitializeLights();
        }
        else if(developmentMode.startInPlayground)
        {
            InitializeLights();
        }
        //NOTE ABOUT WWISE:
        //THE GAMEOBJECT POINTS TO *THIS* GAMEOBJECT. SO WE CAN'T START
        //IT FROM ONE GAMEOBJECT AND THEN STOP IT FROM ANOTHER. IT HAS TO BE THE SAME GAMEOBJECT
    }

    void Update()
    {
        if(developmentMode.developmentMode)
        {
            //toggle gameOn with the "G" button
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (imitoneVoiceIntepreter.gameOn)
                {
                    imitoneVoiceIntepreter.gameOn = false;
                }
                else
                {
                    imitoneVoiceIntepreter.gameOn = true;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            StartCoroutine(MakeWWiseTone());
        }
    }
    

    public void OpeningCallBackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {
            // NOT-YET INTEGRATED ONES
            // BreatheOut_Start
            // Cue_ThematicOpening_End
        if (in_type == AkCallbackType.AK_MusicSyncUserCue)
        {
            Debug.Log("WWise_VO: Callback triggered: " + in_type);
            AkMusicSyncCallbackInfo musicSyncInfo = (AkMusicSyncCallbackInfo)in_info;
            if (musicSyncInfo.userCueName == "Cue_Posture_Start")
            {
                Debug.Log("WWise_VO: Cue_Posture_Start");
            } else if (musicSyncInfo.userCueName == "Cue_ThematicOpening_Start")
            {
                Debug.Log("WWise_VO: Cue_ThematicOpening_Start");
            } else if(musicSyncInfo.userCueName == "Cue_VoiceElicitation1_Start")
            {
                Debug.Log("WWise_VO: Stopping Openign Seq, play sigh Query Seq");
            }
            else if(musicSyncInfo.userCueName == "Cue_Microphone_ON")
            {
                Debug.Log("WWise_VO: Cue Mic On");
                imitoneVoiceIntepreter.gameOn = true;
            }
            else if (musicSyncInfo.userCueName == "Cue_Microphone_OFF")
            {
                Debug.Log("WWise_VO: Cue Mic OFF");
                imitoneVoiceIntepreter.gameOn = false;
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_Start")
            {
                Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_Start");
                imitoneVoiceIntepreter.gameOn = false;
            }
            else if (musicSyncInfo.userCueName == "Cue_VO_GuidedVocalization_End")
            {
                Debug.Log("WWise_VO: Cue_VO_GuidedVocalization_End");
                imitoneVoiceIntepreter.gameOn = true;
            }
            else if(musicSyncInfo.userCueName == "Cue_Somatic_Start")
            {
                Debug.Log("WWise_VO: Somatic Start");
            }
            else if (musicSyncInfo.userCueName == "Cue_BreathIn_Start")
            {
                Debug.Log("WWise_VO: Cue BreathIn Start");
                breathInBehaviour();
            } else if(musicSyncInfo.userCueName == "Cue_Orientation_Start")
            {
                Debug.Log("WWise_VO: Cue Orientation Start");
            } else if (musicSyncInfo.userCueName == "Cue_Sigh_Start")
            {
                Debug.Log("WWise_VO: Cue Sigh Start");
            }  else if (musicSyncInfo.userCueName == "Cue_VoiceElicitation1_End")
            {
                Debug.Log("WWise_VO: PlayingSomaticSeq && Play_SoundSeedBreatheCycle");
            } else if (musicSyncInfo.userCueName == "Cue_LinearHum_Start")
            {
                Debug.Log("WWise_VO: Cue_LinearHum_Start");
                InitializeLights();
                StartCoroutine(MakeWWiseTone());
            } else if (musicSyncInfo.userCueName == "Cue_StartTutorial")
            {
                tutorial.StartTutorial();
            } else if (musicSyncInfo.userCueName == "Cue_InteractiveMusicSystem_Start")
            {
                Debug.Log("WWise_VO: Cue_InteractiveMusicSystem_Start");
                musicSystem1.InteractiveMusicInitializations();
            } else if (musicSyncInfo.userCueName == "Cue_Opening_Start")
            {
                Debug.Log("WWise_VO: Cue_Opening_Start");
            } 
            else
            {
                Debug.LogWarning("WWise_VO: Unexpected Cue: " + in_type + " | " + musicSyncInfo.userCueName);
            }
        }   
    }

    private IEnumerator MakeWWiseTone()
    {
        Debug.Log("WWise_VO: Triggering a False Tone in WWise");
        musicSystem1.PostTheToningEvents();
        float _t = 4f;
        while (_t > 0)
        {
            if(musicSystem1.localToneOn)
            {   //if an actual music system tone comes on, break the loop, so we don't de-activate it here.
                yield break;
            }
            _t -= Time.deltaTime;
            yield return null;
        }
        
        if(musicSystem1.localToneOn)
        { //just in case this would end on the exact frame that the tone starts, check again...
            yield break;
        }

        Debug.Log("WWise_VO: Stopping a False Tone in WWise");
        musicSystem1.StopWwiseToning();
    }

    public void InitializeLights()
    {
        if(!lightsInitialized)
        {
            Debug.Log("WWise_VO: InitializeLights");
            lightControl.SetPreferredColor("Red");
            lightControl.NextPreferredColorWorld(5.0f);
            lightsInitialized = true;
        }
    }
    
    private float GetRTPCValue(RTPC rtpc)
    {
        uint rtpcID = AkSoundEngine.GetIDFromString(rtpc.Name);
        int valueType = 1; // AkRTPCValue_type type, 0 for game object, 1 for global RTPC
        float value;
        AkSoundEngine.GetRTPCValue(rtpcID, gameObject, 0, out value, ref valueType);
        return value;
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
    }
    public void PassBackToVOManager() //REEF - this is currently unused, see Sequencer.cs for where its use it commented out
    {
        Debug.Log("WWise_VO: RanFinalStageLogic");
        AkSoundEngine.PostEvent("Play_THEMATIC_SAVASANA_SEQUENCE", gameObject);
    }

    //REEF - I suggest putting these behaviors in Sequencer, as they don't necessarily pertain to VO, but more to the "sequence" of events. See a comment I left for you on line 143 of that script.
    //Also noting that the design of this doesn't lend itself easily or naturally to different sequences, using different instrument sets, or a different order, or not all of them, which will become more relevant in the not too distant future, but is relevant even now given the possibility that someone will just "stay" in the tutorial.
    //... The ideal system would have a list of sound worlds to play, and would move through the list. That list could be modified based on (a) the launch initializations and (b) the amount of time left when the sequence initiates
    //This will work for now, but I think the system could be cleaner.

    public void BeginMusicSequence(float currentTime) //REEF- I renamed this because I think "CalculateRemainingTime" doesn't adequately describe its function. I changed the reference in Tutorial too.
    {
        timeInUnguidedVocalization = totalTimeOfExperience - totalTimeOfPostUnguidedVocalizationContant - currentTime;   
        float timeInEachSegment = timeInUnguidedVocalization / 4;
        StartCountdownToNextSegment(timeInEachSegment);
        Debug.Log("WWise_VO: Time in each segment: " + timeInEachSegment);
        Debug.Log("WWise_VO: Time in Unguided Vocalization: " + timeInUnguidedVocalization);
    }

    IEnumerator CountdownToNextSegmentCoroutine(float timeInEachSegment)
    {
        //REEF - **Important**, I've changed this to use the director system instead of causing a change right away on the clock. This makes the system more responsive. This way, instead of happening on a precise schedule, the desired change is "queued" and then triggers when an player-driven behavior change happens in the player, so it feels like it is responding to them. Right now, it is set to change  after 60 seconds (that's the 60f) even if there is not behavior change in the player, but I'd recommend this being 120 seconds instead, because it's such a big and important change, and we really want the player to feel it as a response from them. 
        //The system you've designed lends itself to precise, clockwork timing. It should bere-evaluated to work well with the director system, which includes a variable delay. The reconfigured system should calculate the time until the next world-change is added to the queue when the change actually is dynamically triggered. The behavior of setting a new countdown would then have to be triggered by the director system activation event. So you'd put the behavior that starts a new countdown in sequencer.SetSoundWorld. I've put a comment there for you to look at.

        //REEF - **IMPORTANT READ THIS FIRST**, I am just seeing this now, but it looks like Sequencer.cs already has a system that attempts to do what you are doing here. Check it out: sequencer.StartMusicalProgression. I *believe* it has been tested to work.

        if (currentStage == 0)
        {
            //AkSoundEngine.SetState("SoundWorldMode", "SonoFlore");
            sequencer.QueueNewWorld("SonoFlore", "Red", 60f);
            currentStage = 1;
            Debug.Log("WWise_VO: Current Stage: " + currentStage + " | Time in each segment: " + timeInEachSegment + " | SoundWorldMode: SonoFlore");
        }
        else if (currentStage == 1)
        {
            //AkSoundEngine.SetState("SoundWorldMode", "Gentle");
            sequencer.QueueNewWorld("Gentle", "Red", 60f);
            currentStage = 2;
            Debug.Log("WWise_VO: Current Stage: " + currentStage + " | Time in each segment: " + timeInEachSegment + " | SoundWorldMode: Gentle");
        }
        else if (currentStage == 2)
        {
            //AkSoundEngine.SetState("SoundWorldMode", "Shadow");
            sequencer.QueueNewWorld("Shadow", "Blue", 60f);
            currentStage = 3;
            Debug.Log("WWise_VO: Current Stage: " + currentStage + " | Time in each segment: " + timeInEachSegment + " | SoundWorldMode: Shadow");
            
        } else if (currentStage == 3)
        {
            //AkSoundEngine.SetState("SoundWorldMode", "Shruti");
            sequencer.QueueNewWorld("Shruti", "White", 60f);
            currentStage = 4;
            Debug.Log("WWise_VO: Current Stage: " + currentStage + " | Time in each segment: " + timeInEachSegment + " | SoundWorldMode: Shruti");
            //calculateTimeforEarlyTrigger
            float earlyTriggerTime = timeInEachSegment - 15f;
            StartCoroutine(ShrutiEarlyBehavior(earlyTriggerTime));
        } else if (currentStage == 4)
        {
            //PLAY THEMATIC SAVASANA
        }
        yield return new WaitForSeconds(timeInEachSegment);
        StartCountdownToNextSegment(timeInEachSegment);
    }

    IEnumerator ShrutiEarlyBehavior(float earlyTriggerTime)
    {
        yield return new WaitForSeconds(earlyTriggerTime);
        //LOCK FUNDAMENTAL AND HARMONY
        musicSystem1.LockToC(true);
        //to unlock it, call musicSystem1.LockToC(false);
    }

    public void StartCountdownToNextSegment(float timeInEachSegment) //REEF - renamed this for clarity
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownToNextSegmentCoroutine(timeInEachSegment));
    }


    public void breathInBehaviour()
    {
        lightControl.FXWave(0.6f, 5f, 0.25f, true, true);
    }
        
    IEnumerator StartSighElicitationTimer()
        {
            yield return new WaitForSeconds(6.0f); // Wait for the audio event to finish playing
            pause = false;
        }
    IEnumerator StartQueryElicitationTimer()
        {
            pause = true;
            yield return new WaitForSeconds(30.0f); // Wait for the audio event to finish playing
            pause = false;
        }
                    
    
}

