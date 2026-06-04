using NUnit.Framework;

/// <summary>
/// Single-authority binaural model: stage owns base volume, mode owns attenuation.
/// See Docs/BLOCKS_4_5_7_PLAN.md (binaural consolidation).
/// </summary>
public class BinauralAttenuationPolicyEditModeTests
{
    [Test]
    public void GetFactor_Attenuated_ReducesBy30Percent()
    {
        Assert.That(BinauralAttenuationPolicy.GetFactor(true), Is.EqualTo(0.70f).Within(0.0001f));
    }

    [Test]
    public void GetFactor_NotAttenuated_IsUnity()
    {
        Assert.That(BinauralAttenuationPolicy.GetFactor(false), Is.EqualTo(1.0f).Within(0.0001f));
    }

    [TestCase(100f, true, 70f)]   // AudibleVolume × 0.7 when MusicLoopSilent attenuates
    [TestCase(100f, false, 100f)]
    [TestCase(0f, true, 0f)]     // muted stages stay muted regardless of attenuation
    [TestCase(0f, false, 0f)]
    public void Apply_BaseTimesFactor(float baseVolume, bool attenuated, float expected)
    {
        Assert.That(BinauralAttenuationPolicy.Apply(baseVolume, attenuated), Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void Apply_ClampsToRtpcRange()
    {
        Assert.That(BinauralAttenuationPolicy.Apply(200f, false), Is.EqualTo(100f).Within(0.0001f));
        Assert.That(BinauralAttenuationPolicy.Apply(-10f, false), Is.EqualTo(0f).Within(0.0001f));
    }
}
