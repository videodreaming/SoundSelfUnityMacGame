namespace SoundSelf.Sequence
{
    public interface IStageHandler
    {
        StageType StageType { get; }
        void Enter(string variant);
        void Exit();
        bool IsComplete { get; }
        
        /// <summary>True while this stage is in tail logic after the next stage has already started.</summary>
        bool IsTransitioningOut => false;

        /// <summary>Begins transition-out (tail) behavior. Default behavior is no-op.</summary>
        void BeginTransitionOut() { }
            
        /// <summary>Returns true if this handler is waiting for the given sequence command to complete.</summary>
        bool WatchesSequenceCommand(SequenceCommand sequenceCommand) => false;

        /// <summary>Called when a sequence command fires and this handler is watching it. Default: no-op.</summary>
        void ExecuteSequenceCommand(SequenceCommand sequenceCommand) { }
    }
}
