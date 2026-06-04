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
