using UnityEngine;

/// <summary>Editor playtest key map for <see cref="MusicDebugHarness"/> — tested in EditMode.</summary>
public enum MusicDebugHarnessAction
{
    DumpState,
    CycleSoundWorld,
    CycleMusicLoop,
    StepLornaKeyCue,
    LockFundamentalToC,
    UnlockFundamentalLocks,
    ToggleBinauralPlay,
    ToggleBinauralVolume,
    DirectorQueueRepro,
    JumpCountdownTo15Minutes,
    JumpCountdownToSavasanaLockWindow,
    /// <summary>End current sequence stage (same as UI / Shift+E editor cheat).</summary>
    EndThisSequenceStage,
}

public static class MusicDebugHarnessKeyPolicy
{
    public static bool TryGetActionForKey(KeyCode key, out MusicDebugHarnessAction action)
    {
        switch (key)
        {
            case KeyCode.P:
                action = MusicDebugHarnessAction.DumpState;
                return true;
            case KeyCode.LeftBracket:
                action = MusicDebugHarnessAction.CycleSoundWorld;
                return true;
            case KeyCode.RightBracket:
                action = MusicDebugHarnessAction.CycleMusicLoop;
                return true;
            case KeyCode.Semicolon:
                action = MusicDebugHarnessAction.StepLornaKeyCue;
                return true;
            case KeyCode.L:
                action = MusicDebugHarnessAction.LockFundamentalToC;
                return true;
            case KeyCode.U:
                action = MusicDebugHarnessAction.UnlockFundamentalLocks;
                return true;
            case KeyCode.B:
                action = MusicDebugHarnessAction.ToggleBinauralPlay;
                return true;
            case KeyCode.V:
                action = MusicDebugHarnessAction.ToggleBinauralVolume;
                return true;
            case KeyCode.R:
                action = MusicDebugHarnessAction.DirectorQueueRepro;
                return true;
            case KeyCode.Alpha1:
                action = MusicDebugHarnessAction.JumpCountdownTo15Minutes;
                return true;
            case KeyCode.Alpha2:
                action = MusicDebugHarnessAction.JumpCountdownToSavasanaLockWindow;
                return true;
            case KeyCode.E:
                action = MusicDebugHarnessAction.EndThisSequenceStage;
                return true;
            default:
                action = default;
                return false;
        }
    }
}
