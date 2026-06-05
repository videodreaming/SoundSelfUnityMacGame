using NUnit.Framework;
using static FundamentalTriggerPolicy;

/// <summary>
/// Test Runner (EditMode) tests for Block 7 / Stage 9a — the pure <see cref="FundamentalTriggerPolicy"/>.
/// These pin <see cref="FundamentalTriggerPolicy.WhichTest"/> as a characterization of the live ladder in
/// <c>MusicSystem1.TryApplyFundamentalChangeTriggers</c> (the parity anchor): production routes through the
/// policy in 9a and again under the 9c slot model, and these cases must stay green across both.
/// Thresholds mirror production (short=12, long=22, longish offset=5) but are passed explicitly — the
/// production constants themselves are pinned by the playtest/characterization, not here.
/// See Docs/BLOCKS_4_5_7_PLAN.md Appendix G.
/// </summary>
public class Block7FundamentalTriggerPolicyEditModeTests
{
    private const float Long = 22f;
    private const float Short = 12f;
    private const float Offset = 5f; // longish band = [Long-Offset, Long) = [17, 22)

    private static TriggerTest Which(float charge, float highest, bool retrigger, bool firstFrame)
        => WhichTest(charge, highest, retrigger, firstFrame, Long, Offset, Short);

    // ---- Long: fires on ANY frame, takes precedence ----

    [Test]
    public void Long_FiresOnFirstFrame()
        => Assert.That(Which(25f, 25f, true, true), Is.EqualTo(TriggerTest.Long));

    [Test]
    public void Long_FiresOffFirstFrame()
        // The only band not gated by firstFrameActive — a sustained note keeps long-testing.
        => Assert.That(Which(25f, 25f, true, false), Is.EqualTo(TriggerTest.Long));

    [Test]
    public void Long_TakesPrecedenceOverLongishAndShort()
        // charge in the long band on a first frame is Long, never Longish/Short.
        => Assert.That(Which(30f, 30f, true, true), Is.EqualTo(TriggerTest.Long));

    // ---- Longish: high band, first-frame only ----

    [Test]
    public void Longish_FirstFrame_InBand()
        => Assert.That(Which(19f, 19f, true, true), Is.EqualTo(TriggerTest.Longish));

    [Test]
    public void Longish_OffFirstFrame_IsNone()
        // Sub-long charge only triggers on the activating frame; a held note in [17,22) off-frame is inert.
        => Assert.That(Which(19f, 19f, true, false), Is.EqualTo(TriggerTest.None));

    // ---- Short: low band, first-frame only, deferred ----

    [Test]
    public void Short_FirstFrame_InBand()
        => Assert.That(Which(14f, 14f, true, true), Is.EqualTo(TriggerTest.Short));

    [Test]
    public void Short_OffFirstFrame_IsNone()
        => Assert.That(Which(14f, 14f, true, false), Is.EqualTo(TriggerTest.None));

    [Test]
    public void Short_AtThreshold_InBand()
        => Assert.That(Which(Short, Short, true, true), Is.EqualTo(TriggerTest.Short));

    // ---- None: below short, retrigger not ready, not the leader ----

    [Test]
    public void BelowShort_IsNone()
        => Assert.That(Which(10f, 10f, true, true), Is.EqualTo(TriggerTest.None));

    [Test]
    public void RetriggerNotReady_IsNone_EvenWhenLong()
        => Assert.That(Which(30f, 30f, false, true), Is.EqualTo(TriggerTest.None));

    [Test]
    public void NotTheLeader_IsNone_EvenWhenLong()
        // Another note has higher charge this frame; only the leader may trigger.
        => Assert.That(Which(25f, 30f, true, true), Is.EqualTo(TriggerTest.None));

    // ---- IsImmediate ----

    [TestCase(TriggerTest.Long, true)]
    [TestCase(TriggerTest.Longish, true)]
    [TestCase(TriggerTest.Short, false)]
    [TestCase(TriggerTest.None, false)]
    public void IsImmediate_TrueOnlyForLongAndLongish(TriggerTest test, bool expected)
        => Assert.That(IsImmediate(test), Is.EqualTo(expected));

    // ============================================================================================
    // RouteTrigger — the active / behind-the-curtain / dedupe routing (Stage 9c, Appendix G §A.2 + §B).
    // The slot/master dedupe flags only matter for the active Short band; all other rows ignore them.
    // ============================================================================================

    // ---- None band always routes to None ----

    [Test]
    public void Route_NoneBand_IsNone()
        => Assert.That(RouteTrigger(TriggerTest.None, true, false, false), Is.EqualTo(TriggerDisposition.None));

    // ---- Active writer: Long/Longish → ImmediateAudible, Short → DeferredAudible (single-source parity) ----

    [Test]
    public void Route_Long_Active_IsImmediateAudible()
        => Assert.That(RouteTrigger(TriggerTest.Long, true, false, false), Is.EqualTo(TriggerDisposition.ImmediateAudible));

    [Test]
    public void Route_Longish_Active_IsImmediateAudible()
        => Assert.That(RouteTrigger(TriggerTest.Longish, true, false, false), Is.EqualTo(TriggerDisposition.ImmediateAudible));

    [Test]
    public void Route_Short_Active_Neither_IsDeferredAudible()
        => Assert.That(RouteTrigger(TriggerTest.Short, true, false, false), Is.EqualTo(TriggerDisposition.DeferredAudible));

    // ---- Active Short dedupe: target already the master, or already the pending slot → None ----

    [Test]
    public void Route_Short_Active_TargetEqualsMaster_IsNone()
        => Assert.That(RouteTrigger(TriggerTest.Short, true, false, true), Is.EqualTo(TriggerDisposition.None));

    [Test]
    public void Route_Short_Active_SlotEqualsTarget_IsNone()
        => Assert.That(RouteTrigger(TriggerTest.Short, true, true, false), Is.EqualTo(TriggerDisposition.None));

    // ---- Behind the curtain (not active writer): Long/Longish → SilentCommit, Short → None ----

    [Test]
    public void Route_Long_Behind_IsSilentCommit() // Case C (Long resolved by WhichTest on any frame)
        => Assert.That(RouteTrigger(TriggerTest.Long, false, false, false), Is.EqualTo(TriggerDisposition.SilentCommit));

    [Test]
    public void Route_Longish_Behind_IsSilentCommit() // Case B
        => Assert.That(RouteTrigger(TriggerTest.Longish, false, false, false), Is.EqualTo(TriggerDisposition.SilentCommit));

    [Test]
    public void Route_Short_Behind_IsNone() // Case A — a sub-long build is NOT committed behind the curtain
        => Assert.That(RouteTrigger(TriggerTest.Short, false, false, false), Is.EqualTo(TriggerDisposition.None));

    // ============================================================================================
    // Effects — the per-disposition side-effect contract (Stage 9c, Appendix G §A.9).
    // ============================================================================================

    [Test]
    public void Effects_None_IsAllFalse() // Case-A guard: charge is NOT reset behind the curtain
        => Assert.That(Effects(TriggerDisposition.None), Is.EqualTo((false, false, false, false, false)));

    [Test]
    public void Effects_SilentCommit_WritesPreferredAndResetsChargeOnly()
        => Assert.That(Effects(TriggerDisposition.SilentCommit), Is.EqualTo((true, true, false, false, false)));

    [Test]
    public void Effects_DeferredAudible_SetsSlotOnly() // preferred deliberately untouched on short detection
        => Assert.That(Effects(TriggerDisposition.DeferredAudible), Is.EqualTo((false, false, false, true, false)));

    [Test]
    public void Effects_ImmediateAudible_SetsSlotAndTouchesDirector() // commit effects follow downstream
        => Assert.That(Effects(TriggerDisposition.ImmediateAudible), Is.EqualTo((false, false, false, true, true)));
}
