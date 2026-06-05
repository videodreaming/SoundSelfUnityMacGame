using System;
using System.Collections.Generic;
using UnityEngine;
using ConversionUtilities;

// Part of MusicSystem1 (see MusicSystem1.cs). Split out via `partial` purely to reduce file size (Block 7 / 4c).
// Owns the input-driven (sung-pitch) fundamental tracking: per-note charge accumulation + the change-trigger ladder.
// All state (voiceActivity, fundamentalChargeByNote, thresholds, timers, the apply path) is declared in MusicSystem1.cs;
// these methods are members of the same class. No behavior change — bodies were moved verbatim from MusicSystem1.cs.
public partial class MusicSystem1
{
    private bool TryGetSustainedFundamentalShiftTarget(out NoteName targetFundamental)
    {
        targetFundamental = NoteUtils.AddInterval(fundamentalNoteName, _sustainedFundamentalShiftSemitones);
        if (targetFundamental == NoteName.None)
        {
            if (debugAllowWarnings || debugAllowFundamentalLogicLogs)
            {
                Debug.LogWarning($"MUSIC: Sustained-root shift invalid for fundamentalNoteName={fundamentalNoteName}");
            }
            return false;
        }
        return true;
    }

    private NoteName ResolveFundamentalChangeTarget(NoteName trackedKey)
    {
        if (trackedKey == fundamentalNoteName && TryGetSustainedFundamentalShiftTarget(out NoteName target))
        {
            return target;
        }
        return trackedKey;
    }

    /// <summary>
    /// Shared fundamental-change trigger ladder used by both the non-root and sustained-root paths in
    /// <see cref="FundamentalUpdate"/>. The threshold band is the pure <see cref="FundamentalTriggerPolicy.WhichTest"/>
    /// (parity anchor); the active/dedupe routing is the pure <see cref="FundamentalTriggerPolicy.RouteTrigger"/>.
    /// The resulting disposition drives <see cref="AnnounceInputDrivenFundamental"/> (set the next-commit slot, immediate or deferred).
    /// </summary>
    /// <param name="changeTarget">The note the fundamental should change to (sung pitch for normal path, shifted pitch for sustained root).</param>
    /// <param name="newChangeFundamentalTimer">This note's just-incremented ChangeFundamentalTimer.</param>
    /// <param name="highestFundamentalTimer">Highest ChangeFundamentalTimer across all tracked notes this frame.</param>
    /// <param name="firstFrameActive">Whether this note just activated this frame (gates longish/short tests).</param>
    /// <param name="logContext">Suffix appended to debug logs to distinguish sustained-root from normal-path triggers (e.g. " (sustained root)" or "").</param>
    private void TryApplyFundamentalChangeTriggers(
        NoteName changeTarget,
        float newChangeFundamentalTimer,
        float highestFundamentalTimer,
        bool firstFrameActive,
        string logContext)
    {
        bool retriggerReady = fundamentalTimeSinceLastTrigger >= fundamentalRetriggerThreshold;

        var which = FundamentalTriggerPolicy.WhichTest(
            newChangeFundamentalTimer,
            highestFundamentalTimer,
            retriggerReady,
            firstFrameActive,
            _initiateImminentFundamentalChangeThreshold,
            5.0f,
            _queueFundamentalChangeThreshold);

        // Block 7 / 9c — Chunk 3 runs the FULL ladder behind the curtain (shadow tracker): the write gate is no longer a
        // blanket early-return. RouteTrigger folds in who owns the master (isActiveWriter) + the slot/master dedupe:
        //   active long/longish → ImmediateAudible · active short (not deduped) → DeferredAudible
        //   behind-curtain long/longish → SilentCommit (preferred += charge reset, NO master/Director)
        //   behind-curtain short / deduped / band None → None
        bool isActiveWriter = FundamentalSourcePolicy.CanInputDrivenWriteMaster(activeFundamentalSource);
        bool slotEqualsTarget = targetNextFundamental.HasValue && targetNextFundamental.Value == changeTarget;
        bool targetEqualsMaster = changeTarget == fundamentalNoteName;

        var disposition = FundamentalTriggerPolicy.RouteTrigger(which, isActiveWriter, slotEqualsTarget, targetEqualsMaster);

        switch (disposition)
        {
            case TriggerDisposition.ImmediateAudible:
                if (debugAllowFundamentalLogicLogs)
                {
                    Debug.Log("MUSIC: " + which + " Test" + logContext + " Instantly Triggering Fundamental Change to " + NoteUtils.NoteToWwiseString(changeTarget));
                }
                AnnounceInputDrivenFundamental(changeTarget, immediate: true);
                break;

            case TriggerDisposition.DeferredAudible:
                AnnounceInputDrivenFundamental(changeTarget, immediate: false);
                if (debugAllowFundamentalLogicLogs)
                {
                    Debug.Log("MUSIC: Short Test" + logContext + " New Fundamental Queued: " + NoteUtils.NoteToWwiseString(changeTarget));
                }
                break;

            case TriggerDisposition.SilentCommit:
                SilentCommitInputDriven(changeTarget, logContext);
                break;

            case TriggerDisposition.None:
            default:
                break;
        }
    }

    /// <summary>
    /// Block 7 / 9c Chunk 3 — behind-the-curtain silent commit. The InputDriven ladder is running in a tracking mode
    /// (Tutorial/Freeplay) but InputDriven is NOT the active source, and a long/longish band fired. Per the shadow-tracker
    /// design it must NOT move the audible master / Wwise / binaural / Director — it only records InputDriven's preferred
    /// (so a later adopt re-entry resumes on the sung pitch) and resets charge (mirrors the audible commit's reset; the
    /// short band is deliberately inert behind the curtain so a sub-long build keeps accumulating instead). RouteTrigger
    /// only returns SilentCommit when !isActiveWriter; FundamentalTriggerPolicy.Effects pins writesPreferred+resetsCharge.
    /// </summary>
    private void SilentCommitInputDriven(NoteName target, string logContext)
    {
        preferredFundamentalBySource[FundamentalSource.InputDriven] = target;
        ResetInputDrivenFundamentalTimers();

        if (debugAllowFundamentalChangeLogs || debugAllowFundamentalLogicLogs)
        {
            Debug.Log("[B457 FUND-SHADOW] InputDriven silent-commit preferred=" + NoteUtils.NoteToWwiseString(target)
                + logContext + " (master unchanged, behind curtain; active=" + activeFundamentalSource + ")");
        }
    }

    //Take the fundamental behaviors in the InterpretImitonUpdate method and move them here for clarity
    private void FundamentalUpdate()
    {
        var updates = new Dictionary<NoteName, float>();
        float highestFundamentalTimer = 0;

        // Cache keys to avoid modifying the dictionary while iterating
        List<NoteName> noteKeys = new List<NoteName>(voiceActivity.Keys);

        if (imitoneVoiceInterpreter.imitoneActive)
        {
            // First get the highest fundamental timer at the start
            foreach (NoteName key in noteKeys)
            {
                if (fundamentalChargeByNote[key] > highestFundamentalTimer)
                {
                    highestFundamentalTimer = fundamentalChargeByNote[key];
                }
            }

            // Perform the updates
            foreach (NoteName key in noteKeys)
            {
                var scaleNote = voiceActivity[key];
                float newChangeFundamentalTimer = fundamentalChargeByNote[key];

                if (scaleNote.IsActive)
                {
                    // Shared inputs for both the non-root and sustained-root paths.
                    float _slowWhenHighAbsorption = Mathf.Pow(2, Mathf.Clamp(RespirationTracker.instance._absorption, 0, 1) * -1);
                    bool isSustainedRoot = (key == fundamentalNoteName);

                    float _newChangeMultiplier;
                    NoteName changeTarget;
                    bool canTrigger;
                    string logContext;

                    if (!isSustainedRoot)
                    {
                        // Non-root active note: rate scales with wrapped distance from fundamental; change target is the sung pitch.
                        // Conversion point: NoteName -> int for distance calculation via GetWrappedDistance.
                        int d = NoteUtils.GetWrappedDistance(key, fundamentalNoteName);
                        if (d < 0)
                        {
                            if (debugAllowWarnings || debugAllowFundamentalLogicLogs)
                            {
                                Debug.LogWarning($"MUSIC: GetWrappedDistance() returned -1 (indicating None was passed) for key={key}, fundamentalNoteName={fundamentalNoteName} - distance calculation may be incorrect");
                            }
                        }
                        float _fastWhenVeryDifferent = (d > 4 ? 2.0f : 1.0f);
                        _newChangeMultiplier = _slowWhenHighAbsorption * _fastWhenVeryDifferent;
                        changeTarget = key;
                        canTrigger = true;
                        logContext = "";
                    }
                    else
                    {
                        // Sustained root: fill at quarter speed; change target is fundamental shifted down 5 semitones.
                        // canTrigger gates the change ladder so an invalid shift never re-queues the current fundamental.
                        _newChangeMultiplier = _slowWhenHighAbsorption * _sustainedFundamentalFillRateMultiplier;
                        canTrigger = TryGetSustainedFundamentalShiftTarget(out changeTarget);
                        logContext = " (sustained root)";
                    }

                    newChangeFundamentalTimer += Time.deltaTime * _newChangeMultiplier;

                    if (canTrigger)
                    {
                        TryApplyFundamentalChangeTriggers(
                            changeTarget,
                            newChangeFundamentalTimer,
                            highestFundamentalTimer,
                            scaleNote.JustActivated,
                            logContext);
                    }

                    if (isSustainedRoot)
                    {
                        // Decay competing notes' timers while the player holds the root.
                        foreach (NoteName otherKey in noteKeys)
                        {
                            if (otherKey != key)
                            {
                                float newChangeFundamentalTimerOther = Mathf.Max(0, fundamentalChargeByNote[otherKey] - Time.deltaTime * 0.075f);
                                updates[otherKey] = newChangeFundamentalTimerOther;
                            }
                        }
                    }
                }

                // Save updated charge
                updates[key] = newChangeFundamentalTimer;
            }

            // Apply all updates at once
            foreach (var update in updates)
            {
                fundamentalChargeByNote[update.Key] = update.Value;
            }
        }

        fundamentalTimeSinceLastTrigger += Time.deltaTime;
    }
}
