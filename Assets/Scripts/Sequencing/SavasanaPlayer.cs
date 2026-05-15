using System.Collections;
using UnityEngine;

public class SavasanaPlayer : MonoBehaviour
{
    public WwiseVOManager wwiseVOManager;
    public bool playedThematicSavasana { get; private set; } = false;

    private Coroutine _savasanaSectionHeaderCoroutine;

    public void PlayThematicSavasana()
    {
        Debug.Log("Savasana: Playing Thematic Savasana");
        if (playedThematicSavasana)
        {
            Debug.LogWarning("Savasana: Thematic Savasana has already been played.");
            return;
        }
        //AkSoundEngine.PostEvent("Play_VO_ThematicSavasana", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue, wwiseVOManager.ClosingCallBackFunction, null);
        playedThematicSavasana = true;
    }

    /// <summary>
    /// Waits until <paramref name="imitone"/>.<c>gameOn</c> is false, then fades in the Savasana section header via <see cref="UIManager"/>.
    /// Cancel with <see cref="CancelSavasanaSectionHeaderWaitIfRunning"/> on stage cleanup so we never show the header if the stage ends early.
    /// </summary>
    public void BeginShowSavasanaSectionHeaderWhenGameOff(ImitoneVoiceIntepreter imitone, UIManager ui, Sequencer sequencer)
    {
        CancelSavasanaSectionHeaderWaitIfRunning();
        if (imitone == null || ui == null)
            return;
        _savasanaSectionHeaderCoroutine = StartCoroutine(ShowSavasanaSectionHeaderWhenGameOffRoutine(imitone, ui, sequencer));
    }

    public void CancelSavasanaSectionHeaderWaitIfRunning()
    {
        if (_savasanaSectionHeaderCoroutine == null)
            return;
        StopCoroutine(_savasanaSectionHeaderCoroutine);
        _savasanaSectionHeaderCoroutine = null;
    }

    private IEnumerator ShowSavasanaSectionHeaderWhenGameOffRoutine(ImitoneVoiceIntepreter imitone, UIManager ui, Sequencer sequencer)
    {
        while (imitone.gameOn)
            yield return null;

        if (ui != null)
        {
            ui.SetSessionSectionHeader(SessionSectionHeaderKind.Savasana);
            if (sequencer != null)
                ui.RefreshSessionDualStageBannerFromSequencer(sequencer);
        }

        _savasanaSectionHeaderCoroutine = null;
    }
}
