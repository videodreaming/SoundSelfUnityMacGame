namespace SoundSelf.Sequence
{
    /// <summary>Playground stage variant rules — Blocks 4/5/7 debug harness (<see cref="StageVariant.Playground_Debug"/>).</summary>
    public static class PlaygroundStagePolicy
    {
        public static bool IsParkedDebugVariant(StageVariant variant) =>
            variant == StageVariant.Playground_Debug;

        /// <summary>Standard / ascending timeline coroutines; false for <see cref="StageVariant.Playground_Debug"/>.</summary>
        public static bool StartsPlaygroundTimelineCoroutine(StageVariant variant) =>
            !IsParkedDebugVariant(variant);
    }
}
