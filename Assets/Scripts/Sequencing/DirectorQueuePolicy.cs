using System;

/// <summary>A single entry in <see cref="Director.queue"/>. Replaces the old 6-field value tuple.</summary>
public struct DirectorQueueItem
{
    public Action action;
    public string type;
    public bool isAudioAction;
    public bool isVisualAction;
    public float timeLeft;
    public DirectorActivationBehavior activationBehavior;
    /// <summary>Set true once a <see cref="DirectorActivationBehavior.ActivateEntireQueueOnNextTone"/> item has expired and is awaiting the next tone — keeps it in the queue so it fires instead of self-removing.</summary>
    public bool pendingActivation;

    public DirectorQueueItem(Action action, string type, bool isAudioAction, bool isVisualAction,
        float timeLeft, DirectorActivationBehavior activationBehavior)
    {
        this.action = action;
        this.type = type;
        this.isAudioAction = isAudioAction;
        this.isVisualAction = isVisualAction;
        this.timeLeft = timeLeft;
        this.activationBehavior = activationBehavior;
        this.pendingActivation = false;
    }
}

/// <summary>What <see cref="Director.QueueUpdate"/> does with an item whose timer has reached zero.</summary>
public enum DirectorQueueExpiryDisposition
{
    /// <summary><see cref="DirectorActivationBehavior.ExpireWithoutExecuting"/> (and invalid values): drop without running.</summary>
    RemoveWithoutExecuting,
    /// <summary><see cref="DirectorActivationBehavior.ActivateThisActionOnNextTone"/>: capture the action into a coroutine, then remove the item.</summary>
    CaptureActionThenRemove,
    /// <summary><see cref="DirectorActivationBehavior.ActivateEntireQueueOnNextTone"/>: keep the item in the queue until the whole-queue activation fires (the Stage 1 self-removal fix).</summary>
    RetainForWholeQueueActivation,
}

/// <summary>Pure expiry rules for the Director queue — testable without play mode (see Block7DirectorQueueEditModeTests).</summary>
public static class DirectorQueuePolicy
{
    public static DirectorQueueExpiryDisposition GetExpiryDisposition(DirectorActivationBehavior behavior)
    {
        switch (behavior)
        {
            case DirectorActivationBehavior.ActivateThisActionOnNextTone:
                return DirectorQueueExpiryDisposition.CaptureActionThenRemove;
            case DirectorActivationBehavior.ActivateEntireQueueOnNextTone:
                return DirectorQueueExpiryDisposition.RetainForWholeQueueActivation;
            case DirectorActivationBehavior.ExpireWithoutExecuting:
            default:
                return DirectorQueueExpiryDisposition.RemoveWithoutExecuting;
        }
    }

    /// <summary>
    /// The Stage 1 fix in one rule: whole-queue-activation items must NOT be removed from the queue when their
    /// timer expires (they stay until the activation fires); everything else is removed on expiry.
    /// </summary>
    public static bool RemovesFromQueueOnExpiry(DirectorActivationBehavior behavior) =>
        GetExpiryDisposition(behavior) != DirectorQueueExpiryDisposition.RetainForWholeQueueActivation;
}
