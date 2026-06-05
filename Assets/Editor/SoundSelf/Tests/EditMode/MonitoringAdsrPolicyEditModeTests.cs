using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Block 8 / Stage 3b.1: stacked microphone-monitoring ADSR envelope math.
/// </summary>
public class MonitoringAdsrPolicyEditModeTests
{
    private const float Tol = 0.0001f;

    [Test]
    public void RiseReference_Playful_IsFastOnly()
    {
        // modeMeditativeLerp = 0 → snappy = chantLerpFast only.
        Assert.That(MonitoringAdsrPolicy.RiseReference(0.8f, 0.2f, 0f), Is.EqualTo(0.8f).Within(Tol));
    }

    [Test]
    public void RiseReference_Meditative_IsMeanOfFastAndSlow()
    {
        // modeMeditativeLerp = 1 → 0.5 * (fast + slow).
        Assert.That(MonitoringAdsrPolicy.RiseReference(0.8f, 0.2f, 1f), Is.EqualTo(0.5f).Within(Tol));
    }

    [Test]
    public void RiseReference_MidBlend_IsBetween()
    {
        float fast = 0.8f;
        float mean = 0.5f;
        Assert.That(MonitoringAdsrPolicy.RiseReference(0.8f, 0.2f, 0.5f),
            Is.EqualTo(Mathf.Lerp(fast, mean, 0.5f)).Within(Tol));
    }

    [Test]
    public void RiseReference_ClampsMeditativeLerp()
    {
        // Out-of-range blend weights clamp to [0,1] (no overshoot).
        Assert.That(MonitoringAdsrPolicy.RiseReference(0.8f, 0.2f, 2f), Is.EqualTo(0.5f).Within(Tol));
        Assert.That(MonitoringAdsrPolicy.RiseReference(0.8f, 0.2f, -1f), Is.EqualTo(0.8f).Within(Tol));
    }

    [Test]
    public void DecayDuration_FullCharge_IsRunwaySeconds()
    {
        Assert.That(MonitoringAdsrPolicy.DecayDurationSeconds(1f),
            Is.EqualTo(MonitoringAdsrPolicy.ChargeRunwayAtFullChargeSeconds).Within(Tol));
    }

    [Test]
    public void DecayDuration_LowCharge_FlooredAtMinimum()
    {
        // 0.1 * 10 = 1s → floored to 3s.
        Assert.That(MonitoringAdsrPolicy.DecayDurationSeconds(0.1f),
            Is.EqualTo(MonitoringAdsrPolicy.MinDecaySeconds).Within(Tol));
        Assert.That(MonitoringAdsrPolicy.DecayDurationSeconds(0f),
            Is.EqualTo(MonitoringAdsrPolicy.MinDecaySeconds).Within(Tol));
    }

    [Test]
    public void DecayDuration_MidCharge_ScalesAboveMinimum()
    {
        // 0.5 * 10 = 5s (above the 3s floor).
        Assert.That(MonitoringAdsrPolicy.DecayDurationSeconds(0.5f), Is.EqualTo(5f).Within(Tol));
    }

    // Note: the release gate, attack edge, sum-clamp and BoardFader presence mapping are single-expression rules
    // inlined in DirectVoiceMonitoring (lean-policy rule of thumb) and are exercised by playtest, not EditMode.

    [Test]
    public void Constants_MatchAgreedDefaults()
    {
        Assert.That(MonitoringAdsrPolicy.AttackTarget, Is.EqualTo(0.9f).Within(Tol));
        Assert.That(MonitoringAdsrPolicy.DefaultSustainLevel, Is.EqualTo(0.4f).Within(Tol));
        Assert.That(MonitoringAdsrPolicy.MinDecaySeconds, Is.EqualTo(3f).Within(Tol));
        Assert.That(MonitoringAdsrPolicy.BoardFaderHighDb, Is.EqualTo(0f).Within(Tol));
        Assert.That(MonitoringAdsrPolicy.DefaultBoardFaderLowDb, Is.EqualTo(-18f).Within(Tol));
    }
}
