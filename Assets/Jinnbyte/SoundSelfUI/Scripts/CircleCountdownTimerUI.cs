using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the meditation session circle: timer label (updated when the clock’s whole second changes) and radial <see cref="Image.fillAmount"/>
/// from <see cref="TimeTrackerScript"/> session countdown (<see cref="TimeTrackerScript.CountdownFull"/>,
/// <see cref="TimeTrackerScript.ConfiguredFullAtLastConfigure"/>).
/// </summary>
/// <remarks>
/// Inspector: drag the <b>GameObjects</b> <c>TimerText</c> and <c>Filler</c> into the slots (not the component headers).
/// Leave empty to auto-find children <c>TimerText</c> (TMP or legacy <see cref="Text"/>) and <c>Slider/Filler</c> (<see cref="Image"/>).
/// </remarks>
[DisallowMultipleComponent]
public class CircleCountdownTimerUI : MonoBehaviour
{
    [Tooltip("Drag the TimerText GameObject here. Uses TextMeshProUGUI or legacy Text on that object.")]
    [SerializeField] private GameObject _timerTextObject;
    [Tooltip("Drag the Filler GameObject here (the Image with radial fill).")]
    [SerializeField] private GameObject _fillerObject;

    private TextMeshProUGUI _timerTextTmp;
    private Text _timerTextLegacy;
    private Image _fillerImage;

    private bool _warnedMissingRefs;

    private int _clockLabelLastFlooredFullSeconds = int.MinValue;
    private bool _clockLabelShowedNoTracker;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        WarnIfIncomplete();
    }

    private void Update()
    {
        ResolveReferences();

        RefreshClockLabelIfWholeSecondChanged();

        if (_fillerImage != null)
            _fillerImage.fillAmount = GetSessionCountdownProgress();
    }

    private void RefreshClockLabelIfWholeSecondChanged()
    {
        var tt = TimeTrackerScript.instance;
        if (tt == null)
        {
            if (!_clockLabelShowedNoTracker)
            {
                _clockLabelShowedNoTracker = true;
                _clockLabelLastFlooredFullSeconds = int.MinValue;
                ApplyClockLabelText("--:--");
            }

            return;
        }

        _clockLabelShowedNoTracker = false;
        int floored = Mathf.FloorToInt(Mathf.Max(0f, tt.CountdownFull));
        if (floored == _clockLabelLastFlooredFullSeconds)
            return;

        _clockLabelLastFlooredFullSeconds = floored;
        int minutes = floored / 60;
        int seconds = floored % 60;
        ApplyClockLabelText($"{minutes}:{seconds:D2}");
    }

    private void ApplyClockLabelText(string clock)
    {
        if (_timerTextTmp != null)
            _timerTextTmp.text = clock;
        else if (_timerTextLegacy != null)
            _timerTextLegacy.text = clock;
    }

    /// <summary>
    /// Session full countdown completion ratio in [0, 1]: <c>1 - (CountdownFull / ConfiguredFullAtLastConfigure)</c>.
    /// When the configured baseline is zero, returns 0 until the countdown is also zero (then 1).
    /// </summary>
    private static float GetSessionCountdownProgress()
    {
        var tt = TimeTrackerScript.instance;
        if (tt == null)
            return 0f;

        float full = Mathf.Max(0f, tt.CountdownFull);
        float configured = Mathf.Max(0f, tt.ConfiguredFullAtLastConfigure);
        if (configured <= 0f)
            return full <= 0f ? 1f : 0f;

        return Mathf.Clamp01(1f - (full / configured));
    }

    private void ResolveReferences()
    {
        if (_timerTextTmp == null && _timerTextLegacy == null)
        {
            GameObject go = _timerTextObject;
            if (go == null)
            {
                var tr = transform.Find("TimerText");
                if (tr != null)
                    go = tr.gameObject;
            }

            if (go == null)
            {
                foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (tmp != null && tmp.gameObject.name == "TimerText")
                    {
                        go = tmp.gameObject;
                        break;
                    }
                }
            }

            if (go == null)
            {
                foreach (var leg in GetComponentsInChildren<Text>(true))
                {
                    if (leg != null && leg.gameObject.name == "TimerText")
                    {
                        go = leg.gameObject;
                        break;
                    }
                }
            }

            if (go != null)
            {
                _timerTextTmp = go.GetComponent<TextMeshProUGUI>();
                _timerTextLegacy = _timerTextTmp == null ? go.GetComponent<Text>() : null;
            }
        }

        if (_fillerImage == null)
        {
            GameObject go = _fillerObject;
            if (go == null)
            {
                var fillerTr = transform.Find("Slider/Filler");
                if (fillerTr != null)
                    go = fillerTr.gameObject;
            }

            if (go == null)
            {
                foreach (var img in GetComponentsInChildren<Image>(true))
                {
                    if (img == null || img.gameObject.name != "Filler")
                        continue;
                    if (img.transform.parent != null && img.transform.parent.name == "Slider")
                    {
                        go = img.gameObject;
                        break;
                    }
                }
            }

            if (go != null)
                _fillerImage = go.GetComponent<Image>();
        }
    }

    private void WarnIfIncomplete()
    {
        if (_warnedMissingRefs)
            return;

        bool missing = false;
        if (_timerTextTmp == null && _timerTextLegacy == null)
        {
            Debug.LogWarning(
                $"{nameof(CircleCountdownTimerUI)} on '{name}': could not resolve timer text. Assign the Timer Text Object field (drag the TimerText GameObject), or add a child named TimerText with TextMeshProUGUI or UI Text.",
                this);
            missing = true;
        }

        if (_fillerImage == null)
        {
            Debug.LogWarning(
                $"{nameof(CircleCountdownTimerUI)} on '{name}': could not resolve Filler Image. Assign the Filler Object field (drag the Filler GameObject), or use hierarchy Slider/Filler with an Image.",
                this);
            missing = true;
        }

        if (missing)
            _warnedMissingRefs = true;
    }
}
