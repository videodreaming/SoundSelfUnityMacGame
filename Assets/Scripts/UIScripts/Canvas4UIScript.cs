using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Canvas4UIScript : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timeLeftText;
    public TextMeshProUGUI toneDetectedText;
    public TextMeshProUGUI microphoneVolumeText;

    [Header("Component References")]
    public Sequencer sequencer;
    public ImitoneVoiceIntepreter imitoneVoiceInterpreter;

    private void Start()
    {
        // Find components if not assigned
        if (sequencer == null)
        {
            sequencer = FindObjectOfType<Sequencer>();
        }
        
        if (imitoneVoiceInterpreter == null)
        {
            imitoneVoiceInterpreter = FindObjectOfType<ImitoneVoiceIntepreter>();
        }

        // Initialize text fields
        if (timeLeftText != null)
        {
            timeLeftText.text = "Time Left: --:--";
        }
        
        if (toneDetectedText != null)
        {
            toneDetectedText.text = "Tone Detected: No";
        }
        
        if (microphoneVolumeText != null)
        {
            microphoneVolumeText.text = "Microphone Volume: -- dB";
        }
    }

    private void Update()
    {
        // Update time left until end of sequence
        if (timeLeftText != null && sequencer != null)
        {
            float timeLeft = TimeTrackerScript.instance != null ? TimeTrackerScript.instance.GetTimeLeftSeconds() : 0f;
            if (timeLeft < 999999.0f && timeLeft > 0f)
            {
                int minutes = Mathf.FloorToInt(timeLeft / 60f);
                int seconds = Mathf.FloorToInt(timeLeft % 60f);
                timeLeftText.text = $"Time Left: {minutes:D2}:{seconds:D2}";
            }
            else
            {
                timeLeftText.text = "Time Left: --:--";
            }
        }

        // Update tone detection status
        if (toneDetectedText != null && imitoneVoiceInterpreter != null)
        {
            bool toneDetected = imitoneVoiceInterpreter.toneActiveConfident;
            toneDetectedText.text = $"Tone Detected: {(toneDetected ? "Yes" : "No")}";
            
            // Optionally change color based on detection
            if (toneDetected)
            {
                toneDetectedText.color = Color.green;
            }
            else
            {
                toneDetectedText.color = Color.white;
            }
        }

        // Update microphone volume
        if (microphoneVolumeText != null && imitoneVoiceInterpreter != null)
        {
            float volumeDb = imitoneVoiceInterpreter._dbMicrophone;
            
            // Check if volume is valid (not the initial -999.0f value)
            if (volumeDb > -999.0f)
            {
                microphoneVolumeText.text = $"Microphone Volume: {volumeDb:F1} dB";
            }
            else
            {
                microphoneVolumeText.text = "Microphone Volume: -- dB";
            }
        }
    }
}
