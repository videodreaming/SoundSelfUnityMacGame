using ConversionUtilities;
using NUnit.Framework;

/// <summary>
/// Test Runner (EditMode) tests for Block 7 / Stage 4d — the pure <see cref="FundamentalSourcePolicy"/>.
/// Pins the active-source authority rules: which modes track, and who may write the master.
/// Full shim-equivalence / real-flow end-states ride the playtest (instantiating MusicSystem1 in
/// EditMode isn't practical — too many Wwise deps). See Docs/BLOCKS_4_5_7_PLAN.md §Stage 4 (4d).
/// </summary>
public class Block7FundamentalPolicyEditModeTests
{
    // ---- StartupSource (Block 7 / 4e zone 1: scene load is owned by the sequencer) ----

    [Test]
    public void StartupSource_IsSequence()
    {
        // Regression pin for the startup ownership contract: MusicSystem1.Start declares this source on scene load
        // (SetFundamentalSource(StartupSource, startup fundamental)) and the activeFundamentalSource field initializer
        // adopts it. If startup ownership ever changes, that must be a deliberate edit here, not an accident.
        Assert.That(FundamentalSourcePolicy.StartupSource, Is.EqualTo(FundamentalSource.Sequence));
    }

    // ---- IsTrackingMode ----

    [TestCase(MusicSystem1.MusicMode.InteractiveTutorial, true)]
    [TestCase(MusicSystem1.MusicMode.Freeplay, true)]
    [TestCase(MusicSystem1.MusicMode.Silent, false)]
    [TestCase(MusicSystem1.MusicMode.FrozenFreeplay, false)]
    [TestCase(MusicSystem1.MusicMode.MusicLoopSilent, false)]
    [TestCase(MusicSystem1.MusicMode.Environment, false)]
    public void IsTrackingMode_TrueOnlyForTutorialAndFreeplay(MusicSystem1.MusicMode mode, bool expected)
    {
        Assert.That(FundamentalSourcePolicy.IsTrackingMode(mode), Is.EqualTo(expected));
    }

    // ---- SourceForInteractionType (Block 7 / 4e zones 2+3: soundscape → owning source) ----

    [Test]
    public void SourceForInteractionType_MusicLoop_IsMusicBed()
    {
        // A MusicLoop's bed owns the key, so Freeplay-entry / soundscape-driven handoff goes to MusicBed.
        Assert.That(FundamentalSourcePolicy.SourceForInteractionType(MusicSystem1.InteractionType.MusicLoop),
            Is.EqualTo(FundamentalSource.MusicBed));
    }

    [Test]
    public void SourceForInteractionType_SoundWorld_IsInputDriven()
    {
        // A SoundWorld is voice-tracked, so it hands the master to the sung-pitch InputDriven source.
        Assert.That(FundamentalSourcePolicy.SourceForInteractionType(MusicSystem1.InteractionType.SoundWorld),
            Is.EqualTo(FundamentalSource.InputDriven));
    }

    // ---- ShouldWriteMaster (only the active source writes) ----

    [Test]
    public void ShouldWriteMaster_ActiveSource_Writes()
    {
        Assert.That(FundamentalSourcePolicy.ShouldWriteMaster(
            FundamentalSource.Sequence, FundamentalSource.Sequence), Is.True);
    }

    [Test]
    public void ShouldWriteMaster_NonActiveSource_DoesNotWrite()
    {
        Assert.That(FundamentalSourcePolicy.ShouldWriteMaster(
            FundamentalSource.MusicBed, FundamentalSource.Sequence), Is.False);
    }

    // ---- CanInputDrivenWriteMaster (the input write-gate, active-source form) ----

    [Test]
    public void CanInputDrivenWriteMaster_InputDrivenActive_True()
    {
        Assert.That(FundamentalSourcePolicy.CanInputDrivenWriteMaster(FundamentalSource.InputDriven), Is.True);
    }

    [TestCase(FundamentalSource.Sequence)]
    [TestCase(FundamentalSource.MusicBed)]
    public void CanInputDrivenWriteMaster_OtherSourceActive_False(FundamentalSource active)
    {
        Assert.That(FundamentalSourcePolicy.CanInputDrivenWriteMaster(active), Is.False);
    }

    // ---- ShouldCleanSlate (Block 7 / 9c Chunk 3: clean-slate vs honor warm-handoff) ----

    [Test]
    public void ShouldCleanSlate_InputDriven_RealSeed_True()
    {
        // Explicit (re)seed of the sung-pitch loop: wipe per-note charge.
        Assert.That(FundamentalSourcePolicy.ShouldCleanSlate(FundamentalSource.InputDriven, NoteName.E), Is.True);
    }

    [Test]
    public void ShouldCleanSlate_InputDriven_Adopt_False()
    {
        // The shadow-tracker fix: adopting InputDriven's existing preferred (None) is the "honor, don't wipe" path —
        // it must preserve any behind-the-curtain charge build so the sung pitch resumes live.
        Assert.That(FundamentalSourcePolicy.ShouldCleanSlate(FundamentalSource.InputDriven, NoteName.None), Is.False);
    }

    [TestCase(FundamentalSource.MusicBed)]
    [TestCase(FundamentalSource.Sequence)]
    public void ShouldCleanSlate_NonInputDriven_RealSeed_False(FundamentalSource source)
    {
        // A non-InputDriven source never owns InputDriven's charge dict, so a takeover never clean-slates it.
        Assert.That(FundamentalSourcePolicy.ShouldCleanSlate(source, NoteName.C), Is.False);
    }

    [TestCase(FundamentalSource.MusicBed)]
    [TestCase(FundamentalSource.Sequence)]
    public void ShouldCleanSlate_NonInputDriven_Adopt_False(FundamentalSource source)
    {
        Assert.That(FundamentalSourcePolicy.ShouldCleanSlate(source, NoteName.None), Is.False);
    }

    // ---- SwitchCommitDisposition (Block 7 / 9c Chunk 4: how a source switch commits its adopted intent) ----

    [Test]
    public void SwitchCommit_NoIntent_None()
    {
        // The source has no preferred yet (e.g. a MusicBed/InputDriven adopt before any note was set) — nothing to apply.
        Assert.That(FundamentalSourcePolicy.SwitchCommitDisposition(NoteName.None, NoteName.C, directorDisabled: false),
            Is.EqualTo(FundamentalSwitchCommit.None));
    }

    [Test]
    public void SwitchCommit_NoIntent_TakesPrecedenceOverDisabled()
    {
        // None intent short-circuits regardless of Director state (no apply path is even considered).
        Assert.That(FundamentalSourcePolicy.SwitchCommitDisposition(NoteName.None, NoteName.C, directorDisabled: true),
            Is.EqualTo(FundamentalSwitchCommit.None));
    }

    [Test]
    public void SwitchCommit_BenignSameNote_Raw_EvenWhenDirectorEnabled()
    {
        // Benign switch: the adopted note already equals the master, so the master does NOT move → raw re-post (Wwise +
        // binaural parity with the legacy direct apply / startup init), but no Director beat and no phantom flourish.
        Assert.That(FundamentalSourcePolicy.SwitchCommitDisposition(NoteName.C, NoteName.C, directorDisabled: false),
            Is.EqualTo(FundamentalSwitchCommit.Raw));
    }

    [Test]
    public void SwitchCommit_MasterMoves_DirectorDisabled_Raw()
    {
        // Master moves but the Director is off (Opening/Savasana/Playground-off) → raw apply, no flourish (Savasana C-pin).
        Assert.That(FundamentalSourcePolicy.SwitchCommitDisposition(NoteName.E, NoteName.C, directorDisabled: true),
            Is.EqualTo(FundamentalSwitchCommit.Raw));
    }

    [Test]
    public void SwitchCommit_MasterMoves_DirectorEnabled_Director()
    {
        // Master moves with the Director enabled → route through the slot + activation so the switch pairs one flourish.
        Assert.That(FundamentalSourcePolicy.SwitchCommitDisposition(NoteName.E, NoteName.C, directorDisabled: false),
            Is.EqualTo(FundamentalSwitchCommit.Director));
    }
}
