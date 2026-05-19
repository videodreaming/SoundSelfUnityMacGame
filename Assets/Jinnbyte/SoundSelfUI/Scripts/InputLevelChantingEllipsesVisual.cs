using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Meditation <c>Input_level</c> ellipses: XY scale damped with <see cref="LerpUtilities.DampTool"/> toward 0.6 or 1.0 from <see cref="ImitoneVoiceIntepreter.gameOn"/> only
/// (rates from <see cref="GameValues.ChantLerpSlowDamp1"/> / <see cref="GameValues.ChantLerpSlowDamp2"/> — not the live <c>_chantLerpSlow</c> value), per-slot damp/color/spin presets (indices 0–2).
/// Color per ellipse: optional lag on <see cref="GameValues._chantLerpFast"/> then <see cref="LerpUtilities.DampTool"/> with its own chant-rate multiplier.
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
    private float[] _colorDamped;
    private float[,] _colorLagHist;
    private int[] _colorLagWrite;

    private bool _warnedMissing;

    /// <summary>Cached prefix for <see cref="LerpUtilities.DampTool"/> keys (unique per host GameObject + this component).</summary>
    private string _dampToolKeyPrefix;

    private struct EllipsePreset
    {
        public float scaleDampRateMultiplier;
        public float colorChantDampRateMultiplier;
        public float colorLagSeconds;
        public EllipseSpinChantFormula spinChantFormula;
        public float relativeRotationDegreesPerSecond;
    }

    private static readonly EllipsePreset[] EllipsePresets =
    {
        new EllipsePreset
        {
            scaleDampRateMultiplier = 1f,
            colorChantDampRateMultiplier = 1f,
            colorLagSeconds = 0.2f,
            spinChantFormula = EllipseSpinChantFormula.SlowPlusChargeOver2,
            relativeRotationDegreesPerSecond = 155f,
        },
        new EllipsePreset
        {
            scaleDampRateMultiplier = 0.75f,
            colorChantDampRateMultiplier = 1f,
            colorLagSeconds = 0f,
            spinChantFormula = EllipseSpinChantFormula.FastQuarterChargeThreeQuarters,
            relativeRotationDegreesPerSecond = 173f,
        },
        new EllipsePreset
        {
            scaleDampRateMultiplier = 0.5f,
            colorChantDampRateMultiplier = 1f,
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

    private string ColorDampKey(int index) => $"{GetDampToolKeyPrefix()}_Color_{index}";

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
                float laggedT = PushAndSampleColorLag(i, preset.colorLagSeconds, rawColorTargetT);
                float colorD1 = d1 * preset.colorChantDampRateMultiplier;
                float colorD2 = d2 * preset.colorChantDampRateMultiplier;
                _colorDamped[i] = LerpUtilities.DampTool(
                    ColorDampKey(i),
                    _colorDamped[i],
                    laggedT,
                    colorD1,
                    colorD2,
                    LerpUtilities.ChantLinearCreep,
                    0f);

                float t = Mathf.Clamp01(_colorDamped[i]);
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
        if (_colorDamped != null && _colorDamped.Length == n
            && _colorLagWrite != null && _colorLagWrite.Length == n
            && _colorLagHist != null && _colorLagHist.GetLength(0) == n && _colorLagHist.GetLength(1) == ColorLagRingLength)
            return;

        var prevD = _colorDamped;
        _colorDamped = new float[n];
        for (int i = 0; i < n; i++)
            _colorDamped[i] = prevD != null && i < prevD.Length ? prevD[i] : 0f;

        _colorLagWrite = new int[n];
        _colorLagHist = new float[n, ColorLagRingLength];
    }

    private float PushAndSampleColorLag(int ellipseIndex, float lagSeconds, float rawTargetT)
    {
        lagSeconds = Mathf.Clamp(lagSeconds, 0f, MaxColorLagSeconds);
        int w = _colorLagWrite[ellipseIndex];
        int lagSteps = lagSeconds <= 0f
            ? 0
            : Mathf.Clamp(Mathf.RoundToInt(lagSeconds * 60f), 1, ColorLagRingLength - 1);

        float delayedT;
        if (lagSteps == 0)
            delayedT = rawTargetT;
        else
        {
            int read = (w - lagSteps + ColorLagRingLength * 2) % ColorLagRingLength;
            delayedT = _colorLagHist[ellipseIndex, read];
        }

        _colorLagHist[ellipseIndex, w] = rawTargetT;
        _colorLagWrite[ellipseIndex] = (w + 1) % ColorLagRingLength;
        return delayedT;
    }

    private void OnDisable()
    {
        if (_ellipses != null)
        {
            for (int i = 0; i < _ellipses.Length; i++)
            {
                LerpUtilities.CleanUpDampTool(ScaleDampKey(i));
                LerpUtilities.CleanUpDampTool(SpinDampKey(i));
                LerpUtilities.CleanUpDampTool(ColorDampKey(i));
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
