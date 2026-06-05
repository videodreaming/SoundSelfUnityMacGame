using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Block 8 / Stage 3b.0: per-soundscape MicMixer monitoring dB lookup + transition constants.
/// </summary>
public class SoundscapeMonitoringPolicyEditModeTests
{
    private static Dictionary<string, float> CustomMap() => new Dictionary<string, float>
    {
        { "SonoFlore", 0f },
        { "ShiftingEarth", 5f },
        { "Shadow", 2f },
    };

    [Test]
    public void BakedMap_WorldsAreZero_LoopsAreThreeDb()
    {
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("SonoFlore"), Is.EqualTo(0f));
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("Shadow"), Is.EqualTo(0f));
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("ShiftingEarth"), Is.EqualTo(3f));
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("SitarAmbience"), Is.EqualTo(3f));
    }

    [Test]
    public void CustomMap_Override_ReturnsMappedDb()
    {
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("ShiftingEarth", CustomMap()), Is.EqualTo(5f));
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("Shadow", CustomMap()), Is.EqualTo(2f));
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("SonoFlore", CustomMap()), Is.EqualTo(0f));
    }

    [Test]
    public void UnknownSoundscape_ReturnsZero()
    {
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("NotARealSoundscape"), Is.EqualTo(0f));
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("NotARealSoundscape", CustomMap()), Is.EqualTo(0f));
    }

    [Test]
    public void NullOrEmptySoundscape_ReturnsZero_ForSilence()
    {
        // MusicLoopSilent (Savasana / Linear) passes null/empty → no boost.
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape(null), Is.EqualTo(0f));
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape(""), Is.EqualTo(0f));
    }

    [Test]
    public void NullMap_UsesBakedDefaults()
    {
        Assert.That(SoundscapeMonitoringPolicy.MonitoringDbForSoundscape("ShiftingEarth", null), Is.EqualTo(3f));
    }

    [Test]
    public void IsKnownSoundscape_MatchesBakedMap()
    {
        Assert.That(SoundscapeMonitoringPolicy.IsKnownSoundscape("Gentle"), Is.True);
        Assert.That(SoundscapeMonitoringPolicy.IsKnownSoundscape("NotARealSoundscape"), Is.False);
        Assert.That(SoundscapeMonitoringPolicy.IsKnownSoundscape(null), Is.False);
    }

    [Test]
    public void LerpDuration_Is10Seconds()
    {
        Assert.That(SoundscapeMonitoringPolicy.DefaultLerpDurationSeconds, Is.EqualTo(10f));
    }

    [Test]
    public void ContributionName_MatchesDirectVoiceMonitoringConstant()
    {
        Assert.That(SoundscapeMonitoringPolicy.ContributionName, Is.EqualTo(DirectVoiceMonitoring.SoundscapeMonitoringContributionName));
    }
}
