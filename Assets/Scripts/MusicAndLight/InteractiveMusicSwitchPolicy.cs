using System;
using System.Collections.Generic;

/// <summary>A single Wwise switch post that <see cref="MusicSystem1.RecoverInteractiveMusicModeFromInteractionType"/> may issue.</summary>
public enum InteractiveMusicSwitchOp
{
    /// <summary><c>MusicLoops_Switch → Silence</c>. Silences the music-loop bed before routing to the interactive system.</summary>
    MusicLoopsSwitchToSilence,
    /// <summary><c>InteractiveMusicMode_Switch → InteractiveMusicSystem</c> (SoundWorld interaction).</summary>
    InteractiveMusicModeToInteractiveMusicSystem,
    /// <summary><c>InteractiveMusicMode_Switch → MusicLoops</c> (MusicLoop interaction).</summary>
    InteractiveMusicModeToMusicLoops,
}

/// <summary>A concrete ordered Wwise switch post (group + value) — used where the value is dynamic (e.g. the sound-world name).</summary>
public readonly struct InteractiveMusicSwitchPost
{
    public readonly string Group;
    public readonly string Value;

    public InteractiveMusicSwitchPost(string group, string value)
    {
        Group = group;
        Value = value;
    }

    public override string ToString() => Group + " = " + Value;
}

/// <summary>
/// Block 4 — switch-order policy for recovering interactive music mode when entering Tutorial / Freeplay / FrozenFreeplay.
/// Lorna: <i>"when switching to InteractiveMusicSystem (not MusicLoops), set Music Loop switch to Silence first."</i>
/// For a <see cref="MusicSystem1.InteractionType.SoundWorld"/> entry we silence the loop bed (which <c>Play_MusicLoops</c>
/// may have started) <b>before</b> routing to the interactive system — this kills the abrupt/too-loud "all sound worlds at
/// once" fade-in. The <see cref="MusicSystem1.InteractionType.MusicLoop"/> path is unchanged: its loop is still chosen by
/// <see cref="MusicSystem1.SetMusicLoop"/>, so we must not force Silence there.
/// </summary>
public static class InteractiveMusicSwitchPolicy
{
    public const string SoundWorldModeSwitchGroup = "SoundWorldMode_Switch";
    public const string InteractiveMusicModeSwitchGroup = "InteractiveMusicMode_Switch";
    public const string MusicLoopsSwitchGroup = "MusicLoops_Switch";
    public const string InteractiveMusicSystemValue = "InteractiveMusicSystem";
    public const string SilenceValue = "Silence";

    /// <summary>
    /// Ordered Wwise switch posts for <see cref="MusicSystem1.SetSoundWorld"/>.
    /// Fixes SOUNDWORLD_SWITCH_NOT_AUDIBLE: the audible path previously posted only <c>InteractiveMusicMode_Switch</c>
    /// and never <c>SoundWorldMode_Switch</c>, so the world never changed in Wwise.
    /// <para>Non-Environment (audible): post the <b>world switch first</b>, then silence the music-loop bed (Block 4
    /// hygiene, so <c>Play_MusicLoops</c> cannot bleed), then route to the interactive system.</para>
    /// <para>Environment: set the world value only — the mode is not routed (the change is inaudible until the mode
    /// becomes interactive), preserving prior behavior.</para>
    /// </summary>
    public static IReadOnlyList<InteractiveMusicSwitchPost> SetSoundWorldPosts(string soundWorld, bool isEnvironmentMode)
    {
        if (isEnvironmentMode)
        {
            return new[]
            {
                new InteractiveMusicSwitchPost(SoundWorldModeSwitchGroup, soundWorld),
            };
        }

        return new[]
        {
            new InteractiveMusicSwitchPost(SoundWorldModeSwitchGroup, soundWorld),
            new InteractiveMusicSwitchPost(MusicLoopsSwitchGroup, SilenceValue),
            new InteractiveMusicSwitchPost(InteractiveMusicModeSwitchGroup, InteractiveMusicSystemValue),
        };
    }

    public static IReadOnlyList<InteractiveMusicSwitchOp> RecoverInteractiveModeSteps(MusicSystem1.InteractionType interactionType)
    {
        switch (interactionType)
        {
            case MusicSystem1.InteractionType.SoundWorld:
                return new[]
                {
                    InteractiveMusicSwitchOp.MusicLoopsSwitchToSilence,
                    InteractiveMusicSwitchOp.InteractiveMusicModeToInteractiveMusicSystem,
                };
            case MusicSystem1.InteractionType.MusicLoop:
                return new[] { InteractiveMusicSwitchOp.InteractiveMusicModeToMusicLoops };
            default:
                return Array.Empty<InteractiveMusicSwitchOp>();
        }
    }
}
