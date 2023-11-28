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
        EndingThematicSavasana,
        SighElicitation2,
        SighElicitationFail2,
        QueryElicitation2,
        QueryElicitationFail2,
        QueryElicitationPass2,
        ClosingGoodbye
    }

    private AudioManagerState currentState = AudioManagerState.Opening;
    private bool SighElicitationPass1 = false;
    private bool QueryElicitationPass1 = false;
    private bool SighElicitationPass2 = false;
    private bool QueryElicitationPass2 = false;
    public AudioState[] audioStates;
    public float[] delays; // Array to hold different delays for each state
    private AudioSource audioSource;

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
    void Update()
    {
        if (!audioSource.isPlaying)
        {
        OnAudioFinished();
        }
        if (currentState == AudioManagerState.SighElicitation1 || currentState == AudioManagerState.SighElicitationFail1){
            if(Input.GetKeyDown(KeyCode.G)){
                SighElicitationPass1 = true;
                Debug.Log("SighElicitationPass1="+SighElicitationPass1);
            }
        } else if (currentState == AudioManagerState.QueryElicitation1 || currentState == AudioManagerState.QueryElicitationFail1){
            if(Input.GetKeyDown(KeyCode.H)){
                QueryElicitationPass1 = true;
                Debug.Log("QueryElicitationPass1="+QueryElicitationPass1);
            }
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
                Debug.Log("PASSED1");
            }
            else
            {
                // If the condition is not met, go to VoiceElicitationFail1
                ChangeState(AudioManagerState.SighElicitationFail1);
                Debug.Log("FAILED1");
            }
        }
        else if (currentState == AudioManagerState.QueryElicitation1 || currentState == AudioManagerState.QueryElicitationFail1)
        {
            if (QueryElicitationPass1)
            {
                ChangeState(AudioManagerState.QueryElicitationPassThankYou1);
                Debug.Log("PASSED2");
            } 
            else
            {
                ChangeState(AudioManagerState.QueryElicitationFail1);
                Debug.Log("FAILED2");
            }
        }
        //else if (currentState == AudioManagerState.Somatic)
        //{
    
        //}
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
