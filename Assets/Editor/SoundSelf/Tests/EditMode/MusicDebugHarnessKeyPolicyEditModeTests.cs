using NUnit.Framework;
using UnityEngine;

/// <summary>Stage 0 — harness key map; see Docs/BLOCKS_4_5_7_PLAN.md.</summary>
public class MusicDebugHarnessKeyPolicyEditModeTests
{
    [TestCase(KeyCode.P, MusicDebugHarnessAction.DumpState)]
    [TestCase(KeyCode.LeftBracket, MusicDebugHarnessAction.CycleSoundWorld)]
    [TestCase(KeyCode.RightBracket, MusicDebugHarnessAction.CycleMusicLoop)]
    [TestCase(KeyCode.Semicolon, MusicDebugHarnessAction.StepLornaKeyCue)]
    [TestCase(KeyCode.R, MusicDebugHarnessAction.DirectorQueueRepro)]
    [TestCase(KeyCode.Alpha1, MusicDebugHarnessAction.JumpCountdownTo15Minutes)]
    [TestCase(KeyCode.E, MusicDebugHarnessAction.EndThisSequenceStage)]
    [TestCase(KeyCode.G, MusicDebugHarnessAction.GuidedStage1And2Playtest)]
    [TestCase(KeyCode.F, MusicDebugHarnessAction.GuidedGoblinPlaytest)]
    public void TryGetActionForKey_MapsHarnessKeys(KeyCode key, MusicDebugHarnessAction expected)
    {
        Assert.That(MusicDebugHarnessKeyPolicy.TryGetActionForKey(key, out MusicDebugHarnessAction action), Is.True);
        Assert.That(action, Is.EqualTo(expected));
    }

    [Test]
    public void TryGetActionForKey_UnmappedKey_ReturnsFalse()
    {
        Assert.That(MusicDebugHarnessKeyPolicy.TryGetActionForKey(KeyCode.Escape, out _), Is.False);
    }
}
