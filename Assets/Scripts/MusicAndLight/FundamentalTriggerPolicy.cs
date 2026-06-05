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

    /// <summary>
    /// What a trigger band should actually DO this frame, given whether InputDriven currently owns the master
    /// (<paramref name="isActiveWriter"/>) and the slot/master dedupe facts. This is the heart of the 9c model —
    /// the active / behind-the-curtain / dedupe routing (Appendix G §A.2):
    /// <list type="bullet">
    /// <item>Long/Longish + active ⇒ <see cref="TriggerDisposition.ImmediateAudible"/> (set slot + activate now);</item>
    /// <item>Long/Longish + behind-the-curtain (not active) ⇒ <see cref="TriggerDisposition.SilentCommit"/>
    /// (update that source's preferred + reset charge, but do NOT move the audible master);</item>
    /// <item>Short + behind-the-curtain ⇒ <see cref="TriggerDisposition.None"/> (sub-long builds are not committed silently);</item>
    /// <item>Short + active + (target already the master OR already the pending slot) ⇒ <see cref="TriggerDisposition.None"/> (dedupe);</item>
    /// <item>Short + active + neither ⇒ <see cref="TriggerDisposition.DeferredAudible"/> (set slot only — next beat commits it);</item>
    /// <item><see cref="TriggerTest.None"/> ⇒ <see cref="TriggerDisposition.None"/>.</item>
    /// </list>
    /// </summary>
    public static TriggerDisposition RouteTrigger(
        TriggerTest which,
        bool isActiveWriter,
        bool slotEqualsTarget,
        bool targetEqualsMaster)
    {
        if (which == TriggerTest.None)
        {
            return TriggerDisposition.None;
        }

        if (IsImmediate(which)) // Long / Longish
        {
            return isActiveWriter ? TriggerDisposition.ImmediateAudible : TriggerDisposition.SilentCommit;
        }

        // Short (deferred band).
        if (!isActiveWriter)
        {
            return TriggerDisposition.None; // sub-long builds are not committed behind the curtain
        }

        if (targetEqualsMaster || slotEqualsTarget)
        {
            return TriggerDisposition.None; // dedupe: already the master or already the pending slot
        }

        return TriggerDisposition.DeferredAudible;
    }

    /// <summary>
    /// The side-effect <em>contract</em> for each <see cref="TriggerDisposition"/> (Appendix G §A.9), pinned as a pure
    /// tuple so every case's state signature is testable without instantiating <c>MusicSystem1</c> (Wwise deps):
    /// <list type="bullet">
    /// <item><see cref="TriggerDisposition.None"/> ⇒ all false — notably <c>resetsCharge=false</c> (the Case-A guard:
    /// a sub-long build behind the curtain keeps accumulating, it is not reset).</item>
    /// <item><see cref="TriggerDisposition.SilentCommit"/> ⇒ preferred written + charge reset; no master/slot/Director.</item>
    /// <item><see cref="TriggerDisposition.DeferredAudible"/> ⇒ slot set only; preferred untouched
    /// ("don't update preferred on short detection").</item>
    /// <item><see cref="TriggerDisposition.ImmediateAudible"/> ⇒ sets slot + touches the Director (requests activation);
    /// the <em>commit</em> effects (preferred write + charge reset + master move) follow the audible-commit contract
    /// downstream (via the Director consult), so they are false at the trigger site.</item>
    /// </list>
    /// </summary>
    public static (bool writesPreferred, bool resetsCharge, bool writesMaster, bool setsSlot, bool touchesDirector) Effects(
        TriggerDisposition disposition)
    {
        switch (disposition)
        {
            case TriggerDisposition.SilentCommit:
                return (true, true, false, false, false);
            case TriggerDisposition.DeferredAudible:
                return (false, false, false, true, false);
            case TriggerDisposition.ImmediateAudible:
                return (false, false, false, true, true);
            case TriggerDisposition.None:
            default:
                return (false, false, false, false, false);
        }
    }
}

/// <summary>
/// What a trigger band actually does this frame (Block 7 / Stage 9c). Distinct from <see cref="FundamentalTriggerPolicy.TriggerTest"/>,
/// which is only "which threshold band did the charge land in" — this folds in the active-source / behind-the-curtain
/// / dedupe routing. See <see cref="FundamentalTriggerPolicy.RouteTrigger"/> and Appendix G.
/// </summary>
public enum TriggerDisposition
{
    /// <summary>Do nothing (band None, dedupe, or a short build behind the curtain).</summary>
    None,
    /// <summary>Behind-the-curtain long/longish: update the source's preferred + reset charge, but do NOT move the master.</summary>
    SilentCommit,
    /// <summary>Active long/longish: set the slot + activate now → audible master move that pairs a flourish.</summary>
    ImmediateAudible,
    /// <summary>Active short: set the slot only; the next external beat commits it.</summary>
    DeferredAudible,
}
