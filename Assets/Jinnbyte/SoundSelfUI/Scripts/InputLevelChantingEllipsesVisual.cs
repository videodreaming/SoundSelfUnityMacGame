using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Meditation <c>Input_level</c> ellipses: XY scale damped with <see cref="LerpUtilities.DampTool"/> toward 0.6 or 1.0 from <see cref="ImitoneVoiceIntepreter.gameOn"/> only
/// (rates from <see cref="GameValues.ChantLerpSlowDamp1"/> / <see cref="GameValues.ChantLerpSlowDamp2"/> — not the live <c>_chantLerpSlow</c> value), per-slot damp/color/spin presets (indices 0–2).
/// Color per ellipse: <see cref="GameValues._chantLerpFast"/> is already a smoothed (twice-damped) value coming out of <see cref="GameValues"/>, so this component intentionally does NOT re-damp it for color.
/// Each ellipse's color simply reads a wall-clock-time-lagged sample of <c>_chantLerpFast</c> from the per-ellipse ring (lag set by preset <c>colorLagSeconds</c>) and lerps <see cref="ChantTeal"/> → <see cref="ChantWhite"/>.
/// The visual variation between the three ellipses comes entirely from their different lag offsets, not from differing damp rates.
/// Spin: each binding sets base °/s and a <see cref="EllipseSpinChantFormula"/>; <see cref="LerpUtilities.DampTool"/> smooths the formula output onto <see cref="Rotate.SpeedMultiplier"/> while <c>gameOn</c>, else toward 0.
/// </summary>
public enum EllipseSpinChantFormula
{
    /// <summary>(chantLerpSlow + chantCharge) / 2 — “ellipse 1” blend.</summary>
    SlowPlusChargeOver2,
    /// <summary>chantLerpFast × 0.25 + chantCharge × 0.75 — “ellipse 2” blend.</summary>
    FastQuarterChargeThreeQuarters,
    /// <summary>(chantLerpFast + chantCharge + chantLerpSlow) / 3 — “ellipse 3” blend.</summary>
    MeanSlowFastCharge,
}

[DefaultExecutionOrder(50)]
public class InputLevelChantingEllipsesVisual : MonoBehaviour
{
    private const float ScaleMin = 0.6f;
    private const float ScaleMax = 1f;
    private const int ColorLagRingLength = 96;
    private const float MaxColorLagSeconds = 0.75f;

    private static readonly Color ChantTeal = new Color(0f, 140f / 255f, 171f / 255f, 1f);
    private static readonly Color ChantWhite = Color.white;

    [SerializeField] private ChantingEllipseBinding[] _ellipses = new ChantingEllipseBinding[3];

    private ImitoneVoiceIntepreter _imitone;
    private GameValues _gameValues;

    private Transform[] _roots;
    private Image[] _images;
    private Rotate[] _rotates;
    private float[] _spinSmoothed;
    private float[] _scaleDamped;
    private float[,] _colorLagHist;
    // Wall-clock timestamp (Time.unscaledTime) of each ring entry, parallel to _colorLagHist.
    // Used so color lag is time-based and matches design intent at any framerate (see PushAndSampleColorLag).
    private float[,] _colorLagTime;
    private int[] _colorLagWrite;
    // Number of valid samples currently held per ellipse ring (0..ColorLagRingLength).
    // Distinguishes "cold" (default-zero) ring slots from real history during the first frames of play.
    private int[] _colorLagCount;

    private bool _warnedMissing;

    /// <summary>Cached prefix for <see cref="LerpUtilities.DampTool"/> keys (unique per host GameObject + this component).</summary>
    private string _dampToolKeyPrefix;

    private struct EllipsePreset
    {
        public float scaleDampRateMultiplier;
        public float colorLagSeconds;
        public EllipseSpinChantFormula spinChantFormula;
        public float relativeRotationDegreesPerSecond;
    }

    private static readonly EllipsePreset[] EllipsePresets =
    {
        new EllipsePreset
        {
            scaleDampRateMultiplier = 1f,
            colorLagSeconds = 0.2f,
            spinChantFormula = EllipseSpinChantFormula.SlowPlusChargeOver2,
            relativeRotationDegreesPerSecond = 155f,
        },
        new EllipsePreset
        {
            scaleDampRateMultiplier = 0.75f,
            colorLagSeconds = 0f,
            spinChantFormula = EllipseSpinChantFormula.FastQuarterChargeThreeQuarters,
            relativeRotationDegreesPerSecond = 173f,
        },
        new EllipsePreset
        {
            scaleDampRateMultiplier = 0.5f,
            colorLagSeconds = 0.4f,
            spinChantFormula = EllipseSpinChantFormula.MeanSlowFastCharge,
            relativeRotationDegreesPerSecond = 201f,
        },
    };

    private static EllipsePreset GetEllipsePreset(int index) =>
        EllipsePresets[Mathf.Clamp(index, 0, EllipsePresets.Length - 1)];

    [System.Serializable]
    public class ChantingEllipseBinding
    {
        [Tooltip("Ellipse object (uniform scale on X/Y; Z unchanged). Damp, color lag, spin formula, and rotation use fixed presets by list index (0–2).")]
        public GameObject ellipseRoot;
    }

    private void Reset()
    {
        _ellipses = new ChantingEllipseBinding[3];
    }

    private static float EvaluateSpinFormula(EllipseSpinChantFormula f, float slow, float fast, float charge)
    {
        switch (f)
        {
            case EllipseSpinChantFormula.SlowPlusChargeOver2:
                return (slow + charge) * 0.5f;
            case EllipseSpinChantFormula.FastQuarterChargeThreeQuarters:
                return fast * 0.5f + charge * 0.5f;
            case EllipseSpinChantFormula.MeanSlowFastCharge:
                return (fast + charge + slow) / 3f;
            default:
                return (slow + charge) * 0.5f;
        }
    }

    private string GetDampToolKeyPrefix()
    {
        if (string.IsNullOrEmpty(_dampToolKeyPrefix))
        {
            _dampToolKeyPrefix =
                $"{nameof(InputLevelChantingEllipsesVisual)}_go{gameObject.GetInstanceID()}_mb{GetInstanceID()}";
        }

        return _dampToolKeyPrefix;
    }

    private string ScaleDampKey(int index) => $"{GetDampToolKeyPrefix()}_Scale_{index}";

    private string SpinDampKey(int index) => $"{GetDampToolKeyPrefix()}_Spin_{index}";

    private void ResolveServices()
    {
        if (_imitone == null)
            _imitone = FindObjectOfType<ImitoneVoiceIntepreter>();
        if (_gameValues == null)
            _gameValues = GameValues.instance;
    }

    private void Start()
    {
        ResolveServices();

        int n = _ellipses != null ? _ellipses.Length : 0;
        _roots = new Transform[n];
        _images = new Image[n];
        _rotates = new Rotate[n];
        _spinSmoothed = new float[n];
        _scaleDamped = new float[n];
        for (int i = 0; i < n; i++)
            _scaleDamped[i] = ScaleMin;

        for (int i = 0; i < n; i++)
        {
            var b = _ellipses[i];
            if (b == null || b.ellipseRoot == null)
                continue;

            _roots[i] = b.ellipseRoot.transform;
            _images[i] = b.ellipseRoot.GetComponent<Image>();
            if (_images[i] == null)
                _images[i] = b.ellipseRoot.GetComponentInChildren<Image>(true);
            _rotates[i] = b.ellipseRoot.GetComponent<Rotate>();
            if (_rotates[i] == null)
                _rotates[i] = b.ellipseRoot.GetComponentInChildren<Rotate>(true);
            if (_rotates[i] != null)
                _rotates[i].BaseRotationDegreesPerSecond = Mathf.Max(0f, GetEllipsePreset(i).relativeRotationDegreesPerSecond);
        }
    }

    private void LateUpdate()
    {
        ResolveServices();

        if (_imitone == null || _gameValues == null || _ellipses == null || _ellipses.Length == 0)
        {
            WarnOnce($"{nameof(InputLevelChantingEllipsesVisual)} on '{name}': missing ImitoneVoiceIntepreter, GameValues, or ellipses list; not driven.");
            return;
        }

        bool gameOn = _imitone.gameOn;
        float slow = _gameValues._chantLerpSlow;
        float fast = _gameValues._chantLerpFast;
        float charge = _gameValues._chantCharge;
        float d1 = _gameValues.ChantLerpSlowDamp1;
        float d2 = _gameValues.ChantLerpSlowDamp2;

        int n = _ellipses.Length;
        EnsureScaleDampedSize(n);
        EnsureColorArrays(n);

        float rawColorTargetT = gameOn ? Mathf.Clamp01(fast) : 0f;

        for (int i = 0; i < n; i++)
        {
            var binding = _ellipses[i];
            if (binding == null || binding.ellipseRoot == null || _roots[i] == null)
                continue;

            EllipsePreset preset = GetEllipsePreset(i);
            float scaleD1 = d1 * preset.scaleDampRateMultiplier;
            float scaleD2 = d2 * preset.scaleDampRateMultiplier;
            _scaleDamped[i] = LerpUtilities.DampTool(
                ScaleDampKey(i),
                _scaleDamped[i],
                gameOn ? ScaleMax : ScaleMin,
                scaleD1,
                scaleD2,
                LerpUtilities.ChantLinearCreep,
                ScaleMin);

            Vector3 ls = _roots[i].localScale;
            _roots[i].localScale = new Vector3(_scaleDamped[i], _scaleDamped[i], ls.z);

            if (_images[i] != null)
            {
                // _chantLerpFast is already a smoothed (twice-damped) value from GameValues, so
                // we deliberately do not re-damp it here. The only per-ellipse processing is the
                // wall-clock-time-based lag (preset.colorLagSeconds), which is what makes the
                // three ellipses' color responses cascade visually.
                float laggedT = PushAndSampleColorLag(i, preset.colorLagSeconds, rawColorTargetT);
                float t = Mathf.Clamp01(laggedT);
                _images[i].color = Color.Lerp(ChantTeal, ChantWhite, t);
            }

            if (_rotates[i] != null)
            {
                _rotates[i].BaseRotationDegreesPerSecond = Mathf.Max(0f, preset.relativeRotationDegreesPerSecond);

                float spinTarget = gameOn
                    ? EvaluateSpinFormula(preset.spinChantFormula, slow, fast, charge)
                    : 0f;

                _spinSmoothed[i] = LerpUtilities.DampTool(
                    SpinDampKey(i),
                    _spinSmoothed[i],
                    spinTarget,
                    d1,
                    d2,
                    LerpUtilities.ChantLinearCreep,
                    0f);

                _rotates[i].SpeedMultiplier = _spinSmoothed[i];
            }
        }
    }

    private void EnsureScaleDampedSize(int n)
    {
        if (_scaleDamped != null && _scaleDamped.Length == n)
            return;

        var prev = _scaleDamped;
        _scaleDamped = new float[n];
        for (int i = 0; i < n; i++)
            _scaleDamped[i] = prev != null && i < prev.Length ? prev[i] : ScaleMin;
    }

    private void EnsureColorArrays(int n)
    {
        if (_colorLagWrite != null && _colorLagWrite.Length == n
            && _colorLagCount != null && _colorLagCount.Length == n
            && _colorLagHist != null && _colorLagHist.GetLength(0) == n && _colorLagHist.GetLength(1) == ColorLagRingLength
            && _colorLagTime != null && _colorLagTime.GetLength(0) == n && _colorLagTime.GetLength(1) == ColorLagRingLength)
            return;

        _colorLagWrite = new int[n];
        _colorLagHist = new float[n, ColorLagRingLength];
        _colorLagTime = new float[n, ColorLagRingLength];
        _colorLagCount = new int[n];
    }

    /// <summary>
    /// Push the latest raw color-target into the per-ellipse ring (timestamped with
    /// <see cref="Time.unscaledTime"/>) and return the sample whose timestamp is closest to
    /// <c>now - lagSeconds</c>, walking backward through valid history.
    ///
    /// <para>Time-based on purpose: the previous implementation derived <c>lagSteps</c> from
    /// <c>lagSeconds * 60f</c>, which silently assumed 60 FPS. When <c>Application.targetFrameRate</c>
    /// drops below 60 (e.g. the 30/20 FPS power-aware cap), that math stretched the perceived
    /// lag by the framerate ratio. This walks the ring by wall-clock time instead, so the
    /// design-intent lag in <see cref="EllipsePreset.colorLagSeconds"/> holds regardless of FPS.</para>
    ///
    /// <para>During the brief startup window before the ring has accumulated <paramref name="lagSeconds"/>
    /// of history (or after array resize), falls back to the oldest valid sample. Once the ring is
    /// fully populated the search runs in at-most O(ColorLagRingLength).</para>
    /// </summary>
    private float PushAndSampleColorLag(int ellipseIndex, float lagSeconds, float rawTargetT)
    {
        lagSeconds = Mathf.Clamp(lagSeconds, 0f, MaxColorLagSeconds);
        float now = Time.unscaledTime;
        int w = _colorLagWrite[ellipseIndex];

        // Push current sample at the write head, then advance.
        _colorLagHist[ellipseIndex, w] = rawTargetT;
        _colorLagTime[ellipseIndex, w] = now;
        _colorLagWrite[ellipseIndex] = (w + 1) % ColorLagRingLength;
        if (_colorLagCount[ellipseIndex] < ColorLagRingLength)
            _colorLagCount[ellipseIndex]++;

        if (lagSeconds <= 0f)
            return rawTargetT;

        float targetTime = now - lagSeconds;
        int count = _colorLagCount[ellipseIndex];
        // Newest valid entry is the one we just wrote (at the OLD write head, now `w`).
        int newestIdx = w;

        int fallbackIdx = newestIdx;
        for (int step = 0; step < count; step++)
        {
            int idx = (newestIdx - step + ColorLagRingLength) % ColorLagRingLength;
            if (_colorLagTime[ellipseIndex, idx] <= targetTime)
                return _colorLagHist[ellipseIndex, idx];
            fallbackIdx = idx;
        }
        // Ring does not yet hold lagSeconds of history — return the oldest valid sample.
        return _colorLagHist[ellipseIndex, fallbackIdx];
    }

    private void OnDisable()
    {
        if (_ellipses != null)
        {
            for (int i = 0; i < _ellipses.Length; i++)
            {
                LerpUtilities.CleanUpDampTool(ScaleDampKey(i));
                LerpUtilities.CleanUpDampTool(SpinDampKey(i));
            }
        }

        if (_rotates == null)
            return;
        for (int i = 0; i < _rotates.Length; i++)
        {
            if (_rotates[i] != null)
                _rotates[i].SpeedMultiplier = 1f;
        }
    }

    private void WarnOnce(string msg)
    {
        if (_warnedMissing)
            return;
        _warnedMissing = true;
        Debug.LogWarning(msg, this);
    }
}
