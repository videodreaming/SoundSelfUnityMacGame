using UnityEngine;

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
        GuidedVocalization,
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

    private AudioManagerState currentState = AudioManagerState.SighElicitation1;
    private bool SighElicitationPass1 = false;
    private bool QueryElicitationPass1 = false;
    private bool SighElicitationPass2 = false;
    private bool QueryElicitationPass2 = false;
    public AudioState[] audioStates;
    public float[] delays; // Array to hold different delays for each state
    private AudioSource audioSource;
    private float sighElicitationTimer = 6f;

    private void Start()
    {
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
        PlayRandomAudio();
    }
    private void Update()
    {
        if (!audioSource.isPlaying)
        {
        OnAudioFinished();
        }
        if (currentState == AudioManagerState.SighElicitation1 || currentState == AudioManagerState.SighElicitationFail1){
        // Start the timer
        // Check if ImitoneVoiceInterpreter.toneActive is true for at least 0.75 seconds during the 6-second timer
        if (ImitoneVoiceInterpreter.Active)
            { 
            // Decrease the timer for toneActive
                sighElicitationTimer -= Time.deltaTime;
            // Check if the tone has been active for at least 0.75 seconds
                if (sighElicitationTimer <= 5.25f)
                {
                SighElicitationPass1 = true;    
                // Reset the timer for the next state
                sighElicitationTimer = 6f;
                }
            }
    // Check if the timer has reached 0
            if (sighElicitationTimer <= 0)
            {
            // Reset the timer for the next state
            sighElicitationTimer = 6f;
            // Set SighElicitationPass1 to false if the condition is not met within the 6 seconds
            SighElicitationPass1 = false;   
            }
        }
         else if (currentState == AudioManagerState.QueryElicitation1 || currentState == AudioManagerState.QueryElicitationFail1)
        {
            //Logic For QueryElicitationPass2
        }
    }
    private void PlayRandomAudio()
    {
        // Select a random audio clip based on the current state
        AudioClip[] audioClips = GetCurrentAudioClipArray().audioClips;
        AudioClip randomClip = audioClips[Random.Range(0, audioClips.Length)];

        // Set the selected clip to the AudioSource
        audioSource.clip = randomClip;
        
        // Subscribe to the OnAudioFinished event
        audioSource.loop = false;
        audioSource.Play();
    }

    private void OnAudioFinished()
    {
        // This method is called when the audio finishes playing
        if (currentState == AudioManagerState.SighElicitation1 || currentState == AudioManagerState.SighElicitationFail1)
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
        }
        else if (currentState == AudioManagerState.QueryElicitation1 || currentState == AudioManagerState.QueryElicitationFail1)
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
        else
        {
            // For other states, simply change to the next state
            ChangeToNextState();
        }
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
        currentState = newState;
        PlayRandomAudio(); // Start playing the audio for the new state
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
