using ConversionUtilities;
using NUnit.Framework;

/// <summary>Stage 0 — Cue_Key_* map (full Lorna table in Stage 5); see Docs/BLOCKS_4_5_7_PLAN.md.</summary>
public class MusicKeyCuePolicyEditModeTests
{
    [TestCase("Cue_Key_C", NoteName.C)]
    [TestCase("Cue_Key_Gsharp", NoteName.Gs)]
    [TestCase("Cue_Key_Bflat", NoteName.As)]
    [TestCase("Cue_Key_Aflat", NoteName.Gs)]
    [TestCase("Cue_Key_Eflat", NoteName.Ds)]
    public void TryGetNoteForCue_KnownLornaSpellings_ReturnExpected(string cue, NoteName expected)
    {
        Assert.That(MusicKeyCuePolicy.TryGetNoteForCue(cue, out NoteName note), Is.True);
        Assert.That(note, Is.EqualTo(expected));
    }

    [Test]
    public void TryGetNoteForCue_UnknownCueKey_ReturnsFalse()
    {
        Assert.That(MusicKeyCuePolicy.TryGetNoteForCue("Cue_Key_X", out NoteName note), Is.False);
        Assert.That(note, Is.EqualTo(NoteName.None));
    }

    [Test]
    public void LornaExampleTimeline_IsNonEmpty()
    {
        Assert.That(MusicKeyCuePolicy.LornaExampleTimeline.Length, Is.GreaterThan(5));
    }
}
