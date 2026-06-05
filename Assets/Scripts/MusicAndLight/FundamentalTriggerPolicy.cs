/// <summary>
/// Pure, testable rules for the InputDriven fundamental-change trigger ladder (Block 7 / Stage 9).
///
/// 9a introduces this as a *characterization* of the existing ladder in
/// <c>MusicSystem1.TryApplyFundamentalChangeTriggers</c> so 9a and 9c are both provably parity.
/// <see cref="WhichTest"/> answers "which threshold band did this note's charge land in this frame" —
/// the pure threshold/precedence math only. The two gates the policy deliberately does NOT apply (the
/// caller does): the active-source write gate (<c>CanInputDrivenWriteMaster</c>) and the short-test
/// dedupe (don't re-queue a note already queued/current). Keeping those out of <see cref="WhichTest"/>
/// is what lets the same band logic stay stable across 9a and the 9c slot model.
///
/// EditMode tests: <c>Block7FundamentalTriggerPolicyEditModeTests</c>.
/// </summary>
public static class FundamentalTriggerPolicy
{
    public enum TriggerTest
    {
        None,
        Short,
        Longish,
        Long,
    }

    /// <summary>
    /// Which trigger band a note's just-incremented charge lands in this frame, mirroring the live ladder:
    /// <list type="bullet">
    /// <item>not the leader (<paramref name="charge"/> &lt; <paramref name="highestCharge"/>) or retrigger
    /// not ready ⇒ <see cref="TriggerTest.None"/>;</item>
    /// <item><paramref name="charge"/> ≥ <paramref name="longThreshold"/> ⇒ <see cref="TriggerTest.Long"/>
    /// (fires on <em>any</em> frame — the only band not gated by <paramref name="firstFrameActive"/>);</item>
    /// <item>first-frame and ≥ (<paramref name="longThreshold"/> − <paramref name="longishOffset"/>) ⇒
    /// <see cref="TriggerTest.Longish"/>;</item>
    /// <item>first-frame and ≥ <paramref name="shortThreshold"/> ⇒ <see cref="TriggerTest.Short"/>;</item>
    /// <item>otherwise <see cref="TriggerTest.None"/>.</item>
    /// </list>
    /// Long/Longish are the immediate (audible-now) bands; Short is the deferred (queue-for-next-beat) band.
    /// </summary>
    public static TriggerTest WhichTest(
        float charge,
        float highestCharge,
        bool retriggerReady,
        bool firstFrameActive,
        float longThreshold,
        float longishOffset,
        float shortThreshold)
    {
        if (!retriggerReady)
            return TriggerTest.None;

        // Only the leading note (highest charge this frame) may trigger.
        if (charge < highestCharge)
            return TriggerTest.None;

        if (charge >= longThreshold)
            return TriggerTest.Long;

        if (firstFrameActive && charge >= longThreshold - longishOffset)
            return TriggerTest.Longish;

        if (firstFrameActive && charge >= shortThreshold)
            return TriggerTest.Short;

        return TriggerTest.None;
    }

    /// <summary>True for the immediate (audible-now) bands; false for Short (deferred) and None.</summary>
    public static bool IsImmediate(TriggerTest test)
        => test == TriggerTest.Long || test == TriggerTest.Longish;
}
