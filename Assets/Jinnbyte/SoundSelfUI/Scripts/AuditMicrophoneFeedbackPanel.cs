using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuditMicrophoneFeedbackPanel : MonoBehaviour
{
    [SerializeField] private GameObject audiometerFeedbackButton;
    [SerializeField] private GameObject audiometerFeedbackPopup;
    [SerializeField] private GameObject audiometerFeedbackIndicator;
    [SerializeField] private GameObject miceDisable;

    [SerializeField] private Sequencer sequencer;

    bool showAudiometerFeedbackButton = true;
    // Start is called before the first frame update
    void OnEnable()
    {
        ShowAudiometerFeedbackButton(showAudiometerFeedbackButton);

    }
    public void ShowAudiometerFeedbackButton(bool status)
    {
        showAudiometerFeedbackButton = status;
        audiometerFeedbackButton.SetActive(status);
        audiometerFeedbackPopup.SetActive(false);

    }
    public void ShowAudiometerFeedbackPopup()
    {
        audiometerFeedbackButton.SetActive(false);
        audiometerFeedbackPopup.SetActive(true);
    }
    public void Close()
    {
        audiometerFeedbackPopup.SetActive(false);
        audiometerFeedbackButton.SetActive(showAudiometerFeedbackButton);
    }
    void OnDisable()
    {
        Close();
    }
    void Update()
    {
        if (audiometerFeedbackPopup.activeSelf)
        {
            audiometerFeedbackIndicator.SetActive(sequencer.imitoneVoiceInterpreter.toneActive);
            miceDisable.SetActive(sequencer.imitoneVoiceInterpreter.gameOn);
        }

    }

}
