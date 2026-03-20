namespace SoundSelf.Sequence
{
    public interface IStageHandler
    {
        StageType StageType { get; }
        void Enter(string variant);
        void Exit();
        bool IsComplete { get; }

        /// <summary>Returns true if this handler is waiting for the given cue to complete.</summary>
        bool WatchesCue(CueType cue) => false;

        /// <summary>Called when a cue fires and this handler is watching it. Default: no-op.</summary>
        void NotifyCue(CueType cue) { }
    }
}
