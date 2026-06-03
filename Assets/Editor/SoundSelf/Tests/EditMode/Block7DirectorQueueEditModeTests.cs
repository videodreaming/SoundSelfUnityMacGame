using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Stage 1 baseline — locks CURRENT <see cref="Director"/> queue behavior so the tuple→struct refactor
/// and self-removal fix can be verified non-regressive. Written BEFORE the change; must stay green after.
/// See Docs/BLOCKS_4_5_7_PLAN.md Stage 1.
/// </summary>
public class Block7DirectorQueueEditModeTests
{
    Director _director;
    GameObject _go;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("DirectorTest");
        _director = _go.AddComponent<Director>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
    }

    static System.Action Noop() => () => { };

    /// <summary>Both audio+visual so ActivateQueue's flourish branches (which touch Ak / lightControl) never fire.</summary>
    System.Action AddAudioVisual(string type, float? time, DirectorActivationBehavior behavior,
        DirectorExclusivityBehavior exclusivity, List<string> executedOrder = null)
    {
        System.Action action = executedOrder == null ? Noop() : () => executedOrder.Add(type);
        _director.AddActionToQueue(action, type, true, true, time, behavior, exclusivity);
        return action;
    }

    // ---- Add / search / id ----

    [Test]
    public void AddActionToQueue_AddsItem_AndReturnsIncrementingId()
    {
        int id0 = _director.AddActionToQueue(Noop(), "typeA", true, true, 10f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        int id1 = _director.AddActionToQueue(Noop(), "typeB", true, true, 10f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);

        Assert.That(id1, Is.GreaterThan(id0));
        Assert.That(_director.SearchQueueForType("typeA"), Is.True);
        Assert.That(_director.SearchQueueForType("typeB"), Is.True);
        Assert.That(_director.SearchQueueForType("nope"), Is.False);
    }

    [Test]
    public void AddActionToQueue_NullAction_ReturnsMinusOne_AndAddsNothing()
    {
        int id = _director.AddActionToQueue(null, "typeA", true, true, 10f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        Assert.That(id, Is.EqualTo(-1));
        Assert.That(_director.SearchQueueForType("typeA"), Is.False);
    }

    [Test]
    public void AddActionToQueue_WhenDisabled_ReturnsMinusOne_AndAddsNothing()
    {
        _director.Disable();
        int id = _director.AddActionToQueue(Noop(), "typeA", true, true, 10f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        Assert.That(id, Is.EqualTo(-1));
        Assert.That(_director.SearchQueueForType("typeA"), Is.False);
    }

    // ---- Exclusivity ----

    [Test]
    public void PreferShorterTimeRemaining_ExistingShorter_SkipsNewLongerAdd()
    {
        _director.AddActionToQueue(Noop(), "typeA", true, true, 50f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.PreferShorterTimeRemaining);
        int second = _director.AddActionToQueue(Noop(), "typeA", true, true, 100f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.PreferShorterTimeRemaining);

        Assert.That(second, Is.EqualTo(-1));
        Assert.That(_director.queue.Count, Is.EqualTo(1));
    }

    [Test]
    public void PreferShorterTimeRemaining_ExistingLonger_ReplacesWithShorter()
    {
        _director.AddActionToQueue(Noop(), "typeA", true, true, 100f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.PreferShorterTimeRemaining);
        int second = _director.AddActionToQueue(Noop(), "typeA", true, true, 50f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.PreferShorterTimeRemaining);

        Assert.That(second, Is.GreaterThanOrEqualTo(0));
        Assert.That(_director.queue.Count, Is.EqualTo(1));
        Assert.That(_director.queue[second].timeLeft, Is.EqualTo(50f).Within(0.001f));
    }

    [Test]
    public void ReplaceAllOfType_ClearsExistingOfTypeThenAdds()
    {
        _director.AddActionToQueue(Noop(), "typeA", true, true, 10f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        _director.AddActionToQueue(Noop(), "typeA", true, true, 20f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        Assert.That(_director.queue.Count, Is.EqualTo(2));

        _director.AddActionToQueue(Noop(), "typeA", true, true, 30f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.ReplaceAllOfType);

        Assert.That(_director.queue.Count, Is.EqualTo(1));
    }

    [Test]
    public void None_AllowsMultipleOfSameType()
    {
        _director.AddActionToQueue(Noop(), "typeA", true, true, 10f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        _director.AddActionToQueue(Noop(), "typeA", true, true, 20f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);

        Assert.That(_director.queue.Count, Is.EqualTo(2));
    }

    // ---- Clear ----

    [Test]
    public void ClearQueueOfType_ReturnsShortestTimeLeft_AndRemovesThatType()
    {
        _director.AddActionToQueue(Noop(), "typeA", true, true, 80f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        _director.AddActionToQueue(Noop(), "typeA", true, true, 30f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        _director.AddActionToQueue(Noop(), "typeB", true, true, 5f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);

        float cleared = _director.ClearQueueOfType("typeA");

        Assert.That(cleared, Is.EqualTo(30f).Within(0.001f));
        Assert.That(_director.SearchQueueForType("typeA"), Is.False);
        Assert.That(_director.SearchQueueForType("typeB"), Is.True);
    }

    [Test]
    public void ClearQueueOfType_NothingCleared_ReturnsMinusOne()
    {
        float cleared = _director.ClearQueueOfType("missing");
        Assert.That(cleared, Is.EqualTo(-1f).Within(0.001f));
    }

    // ---- ReplaceActionInQueue ----

    [Test]
    public void ReplaceActionInQueue_UsesMinOfOldNewAndMax()
    {
        _director.AddActionToQueue(Noop(), "oldType", true, true, 40f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);
        _director.AddActionToQueue(Noop(), "newType", true, true, 70f,
            DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.None);

        int id = _director.ReplaceActionInQueue(Noop(), "newType", "oldType", true, true, 200f,
            DirectorActivationBehavior.ExpireWithoutExecuting);

        Assert.That(id, Is.GreaterThanOrEqualTo(0));
        Assert.That(_director.SearchQueueForType("oldType"), Is.False);
        Assert.That(_director.queue.Count, Is.EqualTo(1));
        // min(old=40, new=70, max=200) = 40
        Assert.That(_director.queue[id].timeLeft, Is.EqualTo(40f).Within(0.001f));
    }

    // ---- ActivateQueue ----

    [Test]
    public void ActivateQueue_ExecutesAllActions_AndClearsQueue()
    {
        var order = new List<string>();
        AddAudioVisual("typeA", 10f, DirectorActivationBehavior.ExpireWithoutExecuting,
            DirectorExclusivityBehavior.None, order);
        AddAudioVisual("typeB", 10f, DirectorActivationBehavior.ExpireWithoutExecuting,
            DirectorExclusivityBehavior.None, order);

        _director.ActivateQueue();

        Assert.That(order.Count, Is.EqualTo(2));
        Assert.That(_director.queue.Count, Is.EqualTo(0));
    }

    [Test]
    public void ActivateQueue_PrioritizesFundamentalThenSoundscapeThenColorWorldShuffle()
    {
        var order = new List<string>();
        // Add out of priority order; all audio+visual so no flourish fires.
        AddAudioVisual("other", 10f, DirectorActivationBehavior.ExpireWithoutExecuting,
            DirectorExclusivityBehavior.None, order);
        AddAudioVisual("ColorWorldShuffle", 10f, DirectorActivationBehavior.ExpireWithoutExecuting,
            DirectorExclusivityBehavior.None, order);
        AddAudioVisual("SoundscapeShuffle", 10f, DirectorActivationBehavior.ExpireWithoutExecuting,
            DirectorExclusivityBehavior.None, order);
        AddAudioVisual("fundamentalChange", 10f, DirectorActivationBehavior.ExpireWithoutExecuting,
            DirectorExclusivityBehavior.None, order);

        _director.ActivateQueue();

        Assert.That(order, Is.EqualTo(new List<string>
        {
            "fundamentalChange", "SoundscapeShuffle", "ColorWorldShuffle", "other"
        }));
    }

    [Test]
    public void ActivateQueue_WhenDisabled_DoesNotExecuteOrClear()
    {
        var order = new List<string>();
        AddAudioVisual("typeA", 10f, DirectorActivationBehavior.ExpireWithoutExecuting,
            DirectorExclusivityBehavior.None, order);
        _director.Disable();

        _director.ActivateQueue();

        Assert.That(order.Count, Is.EqualTo(0));
        Assert.That(_director.queue.Count, Is.EqualTo(1));
    }

    // ---- Phase B: DirectorQueuePolicy (the self-removal fix kernel) ----

    [Test]
    public void Policy_WholeQueueActivation_RetainsOnExpiry()
    {
        Assert.That(DirectorQueuePolicy.GetExpiryDisposition(DirectorActivationBehavior.ActivateEntireQueueOnNextTone),
            Is.EqualTo(DirectorQueueExpiryDisposition.RetainForWholeQueueActivation));
        // The bug fix in one assertion: whole-queue items must NOT self-remove on expiry.
        Assert.That(DirectorQueuePolicy.RemovesFromQueueOnExpiry(DirectorActivationBehavior.ActivateEntireQueueOnNextTone),
            Is.False);
    }

    [Test]
    public void Policy_ActivateThisAction_CapturesThenRemoves()
    {
        Assert.That(DirectorQueuePolicy.GetExpiryDisposition(DirectorActivationBehavior.ActivateThisActionOnNextTone),
            Is.EqualTo(DirectorQueueExpiryDisposition.CaptureActionThenRemove));
        Assert.That(DirectorQueuePolicy.RemovesFromQueueOnExpiry(DirectorActivationBehavior.ActivateThisActionOnNextTone),
            Is.True);
    }

    [Test]
    public void Policy_ExpireWithoutExecuting_Removes()
    {
        Assert.That(DirectorQueuePolicy.GetExpiryDisposition(DirectorActivationBehavior.ExpireWithoutExecuting),
            Is.EqualTo(DirectorQueueExpiryDisposition.RemoveWithoutExecuting));
        Assert.That(DirectorQueuePolicy.RemovesFromQueueOnExpiry(DirectorActivationBehavior.ExpireWithoutExecuting),
            Is.True);
    }

    [Test]
    public void Policy_InvalidBehavior_TreatedAsRemoveWithoutExecuting()
    {
        var invalid = (DirectorActivationBehavior)999;
        Assert.That(DirectorQueuePolicy.GetExpiryDisposition(invalid),
            Is.EqualTo(DirectorQueueExpiryDisposition.RemoveWithoutExecuting));
        Assert.That(DirectorQueuePolicy.RemovesFromQueueOnExpiry(invalid), Is.True);
    }

    // ---- Phase B: CancelPendingWholeQueueActivation is a safe no-op when nothing is pending ----

    [Test]
    public void CancelPendingWholeQueueActivation_NoPending_LeavesQueueIntact()
    {
        AddAudioVisual("Soundscape", 10f, DirectorActivationBehavior.ActivateEntireQueueOnNextTone,
            DirectorExclusivityBehavior.None);

        _director.CancelPendingWholeQueueActivation();

        // Not expired yet → not pending → still queued, untouched.
        Assert.That(_director.queue.Count, Is.EqualTo(1));
    }
}
