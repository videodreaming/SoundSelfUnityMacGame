using System.Collections;
using UnityEngine;

/// <summary>
/// Owner of the linear / environment ambient bed (<c>Play_AMBIENT_ENVIRONMENT_LOOP</c> /
/// <c>Stop_AMBIENT_ENVIRONMENT_LOOP</c>) and the Wwise State group <c>MusicEnvironmentMode</c>
/// (values <c>Environment</c> / <c>Music</c>).
///
/// <para>
/// Stage D extraction (see <c>Docs/CALIBRATION_UI_SEQUENCING_PLAN.md</c> and
/// <c>Docs/MUSIC_ENVIRONMENT_MODE_WWISE_STATE_REFACTOR_PLAN.md</c>): this class is the
/// **single owner** of the ambient-bed Wwise lifecycle. <see cref="MusicSystem1"/>'s
/// <c>EnterMusicEnvironmentAudio()</c> / <c>ExitMusicEnvironmentAudio()</c> delegate here.
/// Stage handlers also call <see cref="Play"/> on <c>Enter</c> for stages that should hear
/// the bed (e.g. Welcome menu, Calibration), and <see cref="Stop"/> for stages that
/// should not (Opening, Tutorial, Playground, etc.). Both methods are idempotent.
/// </para>
///
/// <para>
/// <b>Scene placement:</b> attach to the **same GameObject** as <see cref="MusicSystem1"/>.
/// That GameObject is already registered with Wwise via <c>AkGameObj</c>, so the Play/Stop
/// pairing stays on one Ak game object (a Wwise requirement — see <c>WwiseVOManager</c>
/// note about same-GO Play/Stop). The <c>Sequencer.calibrationMenu</c> relay uses a separate
/// Ak game object for its own <c>Play_Calibration_Sequence</c> lifecycle.
/// </para>
/// </summary>
public class MusicSystemLinear : MonoBehaviour
{
    public static MusicSystemLinear instance { get; private set; }

    [Header("Wwise contract (MusicEnvironmentMode + AMBIENT_ENVIRONMENT_LOOP)")]
    [Tooltip("Wwise event posted on Play(). The voice must be posted exactly once per Stop/Play cycle — re-entry while playing is a no-op (idempotent).")]
    [SerializeField] private string playEventName = "Play_AMBIENT_ENVIRONMENT_LOOP";
    [Tooltip("Wwise event posted by the delayed Stop coroutine. Spelling matches the Wwise project (designer typo on legacy event is ignored — see refactor plan).")]
    [SerializeField] private string stopEventName = "Stop_AMBIENT_ENVIRONMENT_LOOP";
    [Tooltip("Wwise state group governing the music/environment crossfade. State drives ~40s into-environment / ~10s out-of-environment crossfades inside Wwise.")]
    [SerializeField] private string stateGroupName = "MusicEnvironmentMode";
    [SerializeField] private string stateValueEnvironment = "Environment";
    [SerializeField] private string stateValueMusic = "Music";
    [Tooltip("Delay between State→Music and the Stop event firing — must match the Wwise exit crossfade tail. Cancelled if Play() runs again before it elapses.")]
    [SerializeField] [Range(0f, 60f)] private float delayedStopSeconds = 10f;

    [Header("Debug")]
    [SerializeField] private bool debugAllowLogs = true;

    private bool _isPlaying;
    private Coroutine _delayedStopCoroutine;

    /// <summary>True between a successful <see cref="Play"/> and the moment the delayed Stop coroutine actually posts the Stop event. Re-entries while true are idempotent.</summary>
    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("MusicSystemLinear: Duplicate instance detected on '" + gameObject.name + "'. Existing instance on '" + instance.gameObject.name + "' will be kept; this one will be ignored.");
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    /// <summary>
    /// Enter the environment bed: cancel any pending Stop, set Wwise State to <c>Environment</c>,
    /// post <see cref="playEventName"/> if not already playing. Safe to call repeatedly — re-entry
    /// while already playing only cancels a pending Stop (e.g. if you re-entered during the
    /// ~10s exit tail), avoiding stacked Plays.
    /// </summary>
    public void Play()
    {
        CancelPendingStop();
        AkSoundEngine.SetState(stateGroupName, stateValueEnvironment);
        if (!_isPlaying)
        {
            AkSoundEngine.PostEvent(playEventName, gameObject);
            _isPlaying = true;
            DbgLog("Play — State=" + stateValueEnvironment + ", PostEvent " + playEventName);
        }
        else
        {
            DbgLog("Play — already playing; State re-asserted as " + stateValueEnvironment + " (idempotent).");
        }
    }

    /// <summary>
    /// Exit the environment bed: cancel any pending Stop, set Wwise State to <c>Music</c>,
    /// schedule <see cref="stopEventName"/> to post after <see cref="delayedStopSeconds"/>.
    /// Calling <see cref="Play"/> before the delay elapses cancels the pending Stop and
    /// re-asserts the bed. Idempotent: redundant calls just keep the same pending Stop in
    /// flight (the existing one is cancelled and a fresh one is scheduled, so the delay is
    /// re-armed from the latest call).
    /// </summary>
    public void Stop()
    {
        CancelPendingStop();
        AkSoundEngine.SetState(stateGroupName, stateValueMusic);
        if (_isPlaying)
        {
            _delayedStopCoroutine = StartCoroutine(DelayedStopCoroutine());
            DbgLog("Stop — State=" + stateValueMusic + ", scheduling " + stopEventName + " in " + delayedStopSeconds.ToString("F1") + "s");
        }
        else
        {
            DbgLog("Stop — not playing; State=" + stateValueMusic + " (no Stop event needed).");
        }
    }

    /// <summary>Cancels a pending delayed Stop coroutine if one is queued. Safe to call when nothing is pending.</summary>
    public void CancelPendingStop()
    {
        if (_delayedStopCoroutine != null)
        {
            StopCoroutine(_delayedStopCoroutine);
            _delayedStopCoroutine = null;
        }
    }

    private IEnumerator DelayedStopCoroutine()
    {
        yield return new WaitForSeconds(delayedStopSeconds);
        AkSoundEngine.PostEvent(stopEventName, gameObject);
        _isPlaying = false;
        _delayedStopCoroutine = null;
        DbgLog("delayed — PostEvent " + stopEventName);
    }

    private void DbgLog(string message)
    {
        if (!debugAllowLogs) return;
        Debug.Log("MUSIC_LINEAR: " + message);
    }
}
