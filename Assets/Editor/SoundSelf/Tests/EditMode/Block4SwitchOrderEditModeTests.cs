using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Block 4 — Wwise switch hygiene; see Docs/BLOCKS_4_5_7_PLAN.md Stage 3.</summary>
public class Block4SwitchOrderEditModeTests
{
    private static List<InteractiveMusicSwitchOp> Steps(MusicSystem1.InteractionType interactionType) =>
        new List<InteractiveMusicSwitchOp>(InteractiveMusicSwitchPolicy.RecoverInteractiveModeSteps(interactionType));

    [Test]
    public void SoundWorld_SilencesMusicLoopsBeforeInteractiveSystem()
    {
        var steps = Steps(MusicSystem1.InteractionType.SoundWorld);

        int silenceIdx = steps.IndexOf(InteractiveMusicSwitchOp.MusicLoopsSwitchToSilence);
        int interactiveIdx = steps.IndexOf(InteractiveMusicSwitchOp.InteractiveMusicModeToInteractiveMusicSystem);

        Assert.That(silenceIdx, Is.GreaterThanOrEqualTo(0), "SoundWorld entry must silence MusicLoops.");
        Assert.That(interactiveIdx, Is.GreaterThanOrEqualTo(0), "SoundWorld entry must route to InteractiveMusicSystem.");
        Assert.That(silenceIdx, Is.LessThan(interactiveIdx),
            "MusicLoops_Switch → Silence must be posted BEFORE switching to InteractiveMusicSystem.");
    }

    [Test]
    public void SoundWorld_DoesNotRouteToMusicLoops()
    {
        var steps = Steps(MusicSystem1.InteractionType.SoundWorld);
        Assert.IsFalse(steps.Contains(InteractiveMusicSwitchOp.InteractiveMusicModeToMusicLoops));
    }

    [Test]
    public void MusicLoop_RoutesToMusicLoops_AndDoesNotForceSilence()
    {
        var steps = Steps(MusicSystem1.InteractionType.MusicLoop);

        // Unchanged from prior behavior: route to MusicLoops, and DO NOT silence (the loop is chosen by SetMusicLoop).
        Assert.IsTrue(steps.Contains(InteractiveMusicSwitchOp.InteractiveMusicModeToMusicLoops));
        Assert.IsFalse(steps.Contains(InteractiveMusicSwitchOp.MusicLoopsSwitchToSilence));
        Assert.IsFalse(steps.Contains(InteractiveMusicSwitchOp.InteractiveMusicModeToInteractiveMusicSystem));
    }
}
