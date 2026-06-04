using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// SOUNDWORLD_SWITCH_NOT_AUDIBLE regression net: SetSoundWorld must always post SoundWorldMode_Switch (the bug was that
/// the audible path posted only InteractiveMusicMode_Switch and skipped the world). See Docs/SOUNDWORLD_SWITCH_NOT_AUDIBLE.md.
/// </summary>
public class InteractiveMusicSetSoundWorldPolicyEditModeTests
{
    private static List<InteractiveMusicSwitchPost> Posts(string world, bool isEnvironment) =>
        new List<InteractiveMusicSwitchPost>(InteractiveMusicSwitchPolicy.SetSoundWorldPosts(world, isEnvironment));

    [Test]
    public void Audible_PostsWorldSwitch_AlwaysPresent()
    {
        var posts = Posts("Shadow", isEnvironment: false);
        Assert.That(posts.Exists(p => p.Group == InteractiveMusicSwitchPolicy.SoundWorldModeSwitchGroup && p.Value == "Shadow"),
            Is.True, "Audible SetSoundWorld must post SoundWorldMode_Switch = <world> (this was the missing post).");
    }

    [Test]
    public void Audible_WorldSwitchFirst_ThenSilence_ThenInteractiveSystem()
    {
        var posts = Posts("Shadow", isEnvironment: false);

        int worldIdx = posts.FindIndex(p => p.Group == InteractiveMusicSwitchPolicy.SoundWorldModeSwitchGroup);
        int silenceIdx = posts.FindIndex(p => p.Group == InteractiveMusicSwitchPolicy.MusicLoopsSwitchGroup
                                              && p.Value == InteractiveMusicSwitchPolicy.SilenceValue);
        int interactiveIdx = posts.FindIndex(p => p.Group == InteractiveMusicSwitchPolicy.InteractiveMusicModeSwitchGroup
                                                  && p.Value == InteractiveMusicSwitchPolicy.InteractiveMusicSystemValue);

        Assert.That(worldIdx, Is.GreaterThanOrEqualTo(0), "World switch must be posted.");
        Assert.That(silenceIdx, Is.GreaterThanOrEqualTo(0), "MusicLoops must be silenced (Block 4 hygiene).");
        Assert.That(interactiveIdx, Is.GreaterThanOrEqualTo(0), "Must route to InteractiveMusicSystem.");

        Assert.That(worldIdx, Is.LessThan(silenceIdx), "SoundWorldMode_Switch must be posted before MusicLoops→Silence.");
        Assert.That(silenceIdx, Is.LessThan(interactiveIdx), "MusicLoops→Silence must precede InteractiveMusicSystem.");
        Assert.That(posts.Count, Is.EqualTo(3));
    }

    [Test]
    public void Environment_PostsWorldOnly_NoModeRoute()
    {
        var posts = Posts("Shadow", isEnvironment: true);

        Assert.That(posts.Count, Is.EqualTo(1));
        Assert.That(posts[0].Group, Is.EqualTo(InteractiveMusicSwitchPolicy.SoundWorldModeSwitchGroup));
        Assert.That(posts[0].Value, Is.EqualTo("Shadow"));
        Assert.That(posts.Exists(p => p.Group == InteractiveMusicSwitchPolicy.InteractiveMusicModeSwitchGroup), Is.False,
            "Environment mode must not route InteractiveMusicMode_Switch (would be inaudible/incorrect).");
    }

    [TestCase("SonoFlore")]
    [TestCase("Shadow")]
    [TestCase("Gentle")]
    [TestCase("Shruti")]
    public void WorldValue_IsPassedThroughExactly(string world)
    {
        var audible = Posts(world, isEnvironment: false);
        var environment = Posts(world, isEnvironment: true);

        Assert.That(audible.Find(p => p.Group == InteractiveMusicSwitchPolicy.SoundWorldModeSwitchGroup).Value, Is.EqualTo(world));
        Assert.That(environment[0].Value, Is.EqualTo(world));
    }
}
