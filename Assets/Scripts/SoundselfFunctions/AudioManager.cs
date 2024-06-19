using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AudioState
{
    public AudioManager.AudioManagerState state;
    public AudioClip[] audioClips;
}

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public enum AudioManagerState
    {
        Opening,
        SighElicitation1,
        SighElicitationFail1,
        QueryElicitation1,
        QueryElicitationFail1,
        QueryElicitationPassThankYou1,
        ThematicContent,
        Posture,
        Orientation,
        Somatic,
        GuidedVocalizationHum,
        GuidedVocalizationAhh,
        GuidedVocalizationOhh,
        GuidedVocalizationAdvanced,
        UnGuidedVocalization,
        ThematicSavasana,
        SilentMeditation,
        WakeUp,
        EndingSoon,
        SighElicitation2,
        SighElicitationFail2,
        QueryElicitation2,
        QueryElicitationFail2,
        QueryElicitationPass2,
        ClosingGoodbye
    }

    public ImitoneVoiceIntepreter ImitoneVoiceInterpreter; //reference to ImitoneVoiceInterpreter
    
    public AudioManagerState currentState = AudioManagerState.Opening;
    public bool SighElicitationPass1 = true;
    private bool QueryElicitationPass1 = false;
    private bool SighElicitationPass2 = false;
    private bool QueryElicitationPass2 = false;
    public AudioState[] audioStates;
    public float[] delays; // Array to hold different delays for each state
    private AudioSource audioSource;
    public float sighElicitationTimer = 6f;
    private float sighTimer = 0.0f;
    private float talkingTimer1 = 0.0f;
    private float notTalkingTimer1 = 0.0f;
    private float QueryTimer1 = 30.0f;
    public bool Query1CheckStarted = false;

    public WwiseGlobalManager wwiseGlobalManager;
    public WwiseLinearMusicManager LinearMusicManager;
    public WwiseInteractiveMusicManager interactiveMusicManager;
    public WwiseVOManager VOManager;
    private float audioClipStartTime = 0.0f;
    public AudioClip recordedAudioClip;

    public Dictionary<AudioManagerState, int> stateEntryCount = new Dictionary<AudioManagerState, int>();

    private void Start()
    {
        currentState = AudioManagerState.Opening;
        // Get the AudioSource component attached to the GameObject
        audioSource = GetComponent<AudioSource>();

        // Subscribe to the OnAudioFinished event
        audioSource.clip = null; // Set the clip to null initially
        audioSource.loop = false; // Ensure looping is disabled

        // Subscribe OnAudioFinished method to the AudioSource's completion event
        audioSource.SetScheduledEndTime(0);

        audioSource.loop = true; // Enable looping again for regular playback
        audioSource.Stop(); // Stop again to clear the OnAudioFinished event trigger
        audioSource.Play(); // Resume normal playback
        PlayAudio();
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.I)){
            ChangeToNextState();
        }
        if (currentState == AudioManagerState.SighElicitationFail1|| currentState == AudioManagerState.SighElicitation1)
        {
            // Check if ImitoneVoiceInterpreter.toneActive is true for at least 0.75 seconds during the 6-second timer
            if (ImitoneVoiceInterpreter.toneActiveConfident)
            {
                // Decrease the timer for toneActive
                sighTimer += Time.deltaTime;
                // Check if the tone has been active for at least 0.75 seconds
                if (sighTimer >= 0.75f /*&& ImitoneVoiceInterpreter.breathStage >= 2*/)
                {
                    SighElicitationPass1 = true;    
                }
            
            }
        } else if (currentState == AudioManagerState.QueryElicitation1 || currentState == AudioManagerState.QueryElicitationFail1)
        {
            if(Query1CheckStarted){
                if(ImitoneVoiceInterpreter.imitoneActive) {
                    talkingTimer1 += Time.deltaTime; // Increment talking timer
                    notTalkingTimer1 = 0.0f; // Reset not talking timer
                } else {
                notTalkingTimer1 += Time.deltaTime; // Increment not talking timer
                }
                if(notTalkingTimer1 >= 8.0f && talkingTimer1 < 5.0f) {
                    Query1CheckStarted = false;
                    QueryElicitationPass1 = false;
                } else if(talkingTimer1 >= 10.0f) {
                    Query1CheckStarted = false;
                    QueryElicitationPass1 = true;
                }
            }
        } 
    }


    private void InitializeStateEntryCount(AudioManagerState state)
    {
        if (!stateEntryCount.ContainsKey(state))
        {
            stateEntryCount[state] = 0;
        }
    }

    public void OnAudioFinished()
    {
        ChangeToNextState();
        
        // This method is called when the audio finishes playing
        /*if (currentState == AudioManagerState.SighElicitation1)
        {
            if (SighElicitationPass1)
            {
                // If the condition is met, go to VoiceElicitationPass1
                ChangeState(AudioManagerState.QueryElicitation1);
            }
            else
            {
                // If the condition is not met, go to VoiceElicitationFail1
                ChangeState(AudioManagerState.SighElicitationFail1);
            }
            // Check if ImitoneVoiceInterpreter.toneActive is true for at least 0.75 seconds during the 6-second timer
            if (ImitoneVoiceInterpreter.toneActiveConfident)
            { 
            // Decrease the timer for toneActive
            sighTimer += Time.deltaTime;
            // Check if the tone has been active for at least 0.75 seconds
            if (sighTimer >= 0.75f)
            {
                SighElicitationPass1 = true;    
            }
            }
            if (SighElicitationPass1)
            {
                Debug.Log("Going into Qurery Elicitation 1");
                // If the condition is met, go to VoiceElicitationPass1
                ChangeState(AudioManagerState.QueryElicitation1);
            }            
        }
        if (currentState == AudioManagerState.QueryElicitation1|| currentState == AudioManagerState.QueryElicitationFail1)
        {
            if (QueryElicitationPass1)
            {
                ChangeState(AudioManagerState.QueryElicitationPassThankYou1);
            } 
            else
            {
                ChangeState(AudioManagerState.QueryElicitationFail1);
            }
        }

        else if (currentState == AudioManagerState.SighElicitation2 || currentState == AudioManagerState.SighElicitationFail2)
        {
            if (SighElicitationPass2)
            {
                // If the condition is met, go to VoiceElicitationPass1
                ChangeState(AudioManagerState.QueryElicitation2);
            }
            else
            {
                // If the condition is not met, go to VoiceElicitationFail1
                ChangeState(AudioManagerState.SighElicitationFail2);
            }
        }
        else if (currentState == AudioManagerState.QueryElicitation2 || currentState == AudioManagerState.QueryElicitationFail2)
        {
            if (QueryElicitationPass2)
            {
                ChangeState(AudioManagerState.QueryElicitationPass2);
            } 
            else
            {
                ChangeState(AudioManagerState.QueryElicitationFail2);
            }
        }
        // else if (currentState == AudioManagerState.QueryElicitationPassThankYou1){
        //     if(recordedAudioClip != null){
        //         ChangeToNextState();
        //     }
        // }
        else if (currentState == AudioManagerState.SighElicitationFail1)
        {  
            if (SighElicitationPass1)
            {
                // If the condition is met, go to VoiceElicitationPass1
                ChangeState(AudioManagerState.QueryElicitation1);
            } 
            else 
            {
                audioClipStartTime = float.MaxValue; // Reset the start time to avoid multiple triggers
                sighElicitationTimer = 6.0f;
                ChangeState(AudioManagerState.SighElicitationFail1);
            }            

        }
        else
        {
            // For other states, simply change to the next state
            ChangeToNextState();
        }*/
    }

    private void ChangeToNextState()
    {
        // Determine the next state based on the current state's order
        AudioManagerState[] allStates = (AudioManagerState[])System.Enum.GetValues(typeof(AudioManagerState));
        int currentIndex = (int)currentState;

        // Increment to the next state, looping back to the first state if at the end
        int nextIndex = (currentIndex + 1) % allStates.Length;

        // Change to the next state
        ChangeState((AudioManagerState)nextIndex);
    }

    // Example method to change the state (you can call this method based on your game logic)
    private void ChangeState(AudioManagerState newState)
    {
        InitializeStateEntryCount(newState); // Initialize entry count for the new state
        currentState = newState;
        PlayAudio(); 
    }

    private void PlayAudio()
    {
        InitializeStateEntryCount(currentState); // Ensure current state is initialized in the dictionary
        
        /*
        AudioState currentAudioState = GetCurrentAudioClipArray();
        AudioClip[] audioClips = currentAudioState.audioClips;
        if (audioClips.Length == 0)
        {
            return; // No audio clips to play
        }

        
        // Adjust the clip index calculation
        int entryCount = stateEntryCount[currentState];
        int clipIndex = entryCount % audioClips.Length; // Use modulo to loop back to the start

        // Set the selected clip to the AudioSource
        audioSource.clip = audioClips[clipIndex];
        
        // Play the selected clip
        audioSource.loop = false;
        audioSource.Play();

        audioClipStartTime = Time.time;

        // Increment the entry count after playing the audio
        stateEntryCount[currentState]++;
        */
    }

    // Helper method to get the current audio clip array based on the current state
    private AudioState GetCurrentAudioClipArray()
    {
        foreach (var state in audioStates)
        {
            if (state.state == currentState)
            {
                return state;
            }
        }

        // Return an empty state if the current state is not recognized
        return new AudioState { state = currentState, audioClips = new AudioClip[0] };
    }
}
