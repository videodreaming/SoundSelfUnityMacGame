using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AVS = light strobing program (DynamicDrop Start / Theta / End). Runs on the same GameObject as <see cref="Sequencer"/>.
/// </summary>
[DisallowMultipleComponent]
public class AVSSequence : MonoBehaviour
{
    /// <summary>Singleton for the AVS program on the Sequencer GameObject (set in <see cref="Awake"/>).</summary>
    public static AVSSequence instance { get; private set; }

    private Director director;

    [Header("Debug logs")]
    [SerializeField] private bool debugAllowLogsAVSProgram = false;
    [Tooltip("When true, AVS warning logs still print even if AVS info logs are off.")]
    [SerializeField] private bool debugAllowLogsWarnings = true;

    /// <param name="isWarning">If true, <c>LogWarning</c> when <see cref="debugAllowLogsWarnings"/>; else <c>Log</c> when <see cref="debugAllowLogsAVSProgram"/>.</param>
    private void DbgLogAvs(string message, bool isWarning = false)
    {
        if (isWarning)
        {
            if (debugAllowLogsWarnings)
                Debug.LogWarning(message);
        }
        else if (debugAllowLogsAVSProgram)
        {
            Debug.Log(message);
        }
    }

    private float _absorptionThreshold;
    private float d = 1f; // debug timer mult, higher makes it go faster for testing

    private bool flagThetaCoroutine = false;
    private readonly List<int> coroutineCleanupList = new List<int>();
    private Coroutine CoroutineDynamicDropStart;
    private Coroutine CoroutineDynamicDropTheta;
    private Coroutine CoroutineDynamicDropEnd;
    private Coroutine CoroutineDropToDelta;

    private const float DropToDeltaTargetHz = 2f;
    /// <summary>When <see cref="StartDropToDelta"/> is called without a transition time, strobe moves toward 2 Hz at this many Hz per minute.</summary>
    private const float DropToDeltaHzPerMinute = 1f;
    private const float MonostereoDeltaRepeatIntervalSec = 120f;
    private const float MonostereoDeltaQueueTimeLimit = 1e6f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("AVSSequence: Multiple AVSSequence instances; destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        instance = this;

        director = GetComponent<Director>();
        var sequencer = GetComponent<Sequencer>();
        if (director == null && sequencer != null)
            director = sequencer.director;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        if (LightControl.instance == null || director == null)
            return;
        if (!(DevelopmentMode.instance != null && DevelopmentMode.instance.developmentMode))
            d = 1f;
        else if (DevelopmentMode.instance == null && debugAllowLogsWarnings)
            Debug.LogWarning("Sequencer: DevelopmentMode instance is null in AVSSequence Start().");
        _absorptionThreshold = UnityEngine.Random.Range(0.08f, 0.35f);
    }

    /// <summary>Opening calibration → first hum: strobe ramp and optional theta branch. Called from <see cref="OpeningStageHandler"/>.</summary>
    public void StartOpeningAVSProgram()
    {
        DbgLogAvs("Sequencer: StartOpeningAVSProgram - AVS_Program_DynamicDrop_Start is starting");
        CoroutineDynamicDropStart = StartCoroutine(AVS_Program_DynamicDrop_Start());
    }

    /// <summary>End-of-session AVS wind-up (e.g. from <see cref="Sequencer"/> standard sequence).</summary>
    public void StartDynamicDropEnd(float transitionTime = 180f)
    {
        CoroutineDynamicDropEnd = StartCoroutine(AVS_Program_DynamicDrop_End(transitionTime));
    }

    /// <summary>Stops every AVS program coroutine (Dynamic Drop + Drop-to-Delta), resets theta handoff state, and clears tracked director queue indices.</summary>
    /// <param name="exceptCoroutine">If set, that handle is left running (e.g. <see cref="AVS_Program_DropToDelta"/> calls this on its first line so it does not abort itself).</param>
    public void StopAllAvsPrograms(Coroutine exceptCoroutine = null)
    {
        DbgLogAvs("AVSSequence: StopAllAvsPrograms");

        if (CoroutineDynamicDropStart != null && CoroutineDynamicDropStart != exceptCoroutine)
        {
            StopCoroutine(CoroutineDynamicDropStart);
            CoroutineDynamicDropStart = null;
        }
        if (CoroutineDynamicDropTheta != null && CoroutineDynamicDropTheta != exceptCoroutine)
        {
            StopCoroutine(CoroutineDynamicDropTheta);
            CoroutineDynamicDropTheta = null;
        }
        if (CoroutineDynamicDropEnd != null && CoroutineDynamicDropEnd != exceptCoroutine)
        {
            StopCoroutine(CoroutineDynamicDropEnd);
            CoroutineDynamicDropEnd = null;
        }
        if (CoroutineDropToDelta != null && CoroutineDropToDelta != exceptCoroutine)
        {
            StopCoroutine(CoroutineDropToDelta);
            CoroutineDropToDelta = null;
        }

        flagThetaCoroutine = false;

        Cleanup(coroutineCleanupList);
    }

    /// <summary>Ramps strobe toward 2 Hz, then mono on next confident-tone release, then recurring director monostereo toggles.</summary>
    /// <param name="transitionTime">If set, seconds over which to reach 2 Hz. If null, ramp at <see cref="DropToDeltaHzPerMinute"/> Hz per minute.</param>
    public void StartDropToDelta(float? transitionTime = null)
    {
        if (LightControl.instance == null)
        {
            DbgLogAvs("AVSSequence: StartDropToDelta — LightControl.instance is null.", true);
            return;
        }
        if (CoroutineDropToDelta != null)
        {
            StopCoroutine(CoroutineDropToDelta);
            CoroutineDropToDelta = null;
        }
        CoroutineDropToDelta = StartCoroutine(AVS_Program_DropToDelta(transitionTime));
    }

    private IEnumerator AVS_Program_DropToDelta(float? transitionTime)
    {
        StopAllAvsPrograms(CoroutineDropToDelta);

        var lc = LightControl.instance;
        if (lc == null)
            yield break;

        var imitone = GetComponent<Sequencer>()?.imitoneVoiceInterpreter;
        if (director == null)
        {
            DbgLogAvs("AVSSequence: DropToDelta — director is null.", true);
            yield break;
        }

        float startRate = lc._strobeRate;
        float deltaHz = Mathf.Abs(startRate - DropToDeltaTargetHz);
        float durationSec;
        if (transitionTime.HasValue)
            durationSec = Mathf.Max(0f, transitionTime.Value);
        else
            durationSec = DropToDeltaHzPerMinute > 0f ? deltaHz * (60f / DropToDeltaHzPerMinute) : 0f;

        DbgLogAvs("AVSSequence: DropToDelta — strobe " + startRate + " Hz → " + DropToDeltaTargetHz + " Hz over " + durationSec + " s");

        lc.SetStrobeRate(DropToDeltaTargetHz, durationSec);
        if (durationSec > 0f)
            yield return new WaitForSeconds(durationSec);

        if (imitone != null)
        {
            bool seenConfident = false;
            while (true)
            {
                if (imitone.toneActiveConfident)
                    seenConfident = true;
                if (seenConfident && !imitone.toneActiveConfident)
                    break;
                yield return null;
            }
            lc.Strobe_MonoStereo(true);
        }
        else
            DbgLogAvs("AVSSequence: DropToDelta — imitoneVoiceInterpreter is null; skipping mono on tone edge.", true);

        coroutineCleanupList.Add(director.AddActionToQueue(
            Action_Strobe_MonoStereo(null),
            "monostereo", false, true, MonostereoDeltaQueueTimeLimit, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));

        while (true)
        {
            yield return new WaitForSeconds(MonostereoDeltaRepeatIntervalSec);
            coroutineCleanupList.Add(director.AddActionToQueue(
                Action_Strobe_MonoStereo(null),
                "monostereo", false, true, MonostereoDeltaQueueTimeLimit, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
        }
    }

    private IEnumerator AVS_Program_DynamicDrop_Start()
    {
        Cleanup(coroutineCleanupList);
        yield return null;

        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Waiting for lights. Currently:" + LightControl.instance.currentColorWorld);

        if (LightControl.instance.currentColorWorld != PreferredColorWorld.Dark && LightControl.instance.currentColorWorld != PreferredColorWorld.BreathOnly)
        {
            DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Lights not Dark at start, resetting to Dark first.");
            LightControl.instance.SetPreferredColor(PreferredColorWorld.Dark, 0.1f);
            LightControl.instance.SetStrobeRate(0f, 0.1f);
            yield return new WaitForSeconds(0.15f);
        }

        while ((LightControl.instance.currentColorWorld == PreferredColorWorld.Dark) || (LightControl.instance.currentColorWorld == PreferredColorWorld.BreathOnly))
            yield return null;

        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Lights detected, set strobe to 45hz.");
        LightControl.instance.SetStrobeRate(45.0f, 0.0f);
        float _timer = 10f / d;
        while (_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Initializeing drop from gamma to high alpha.");
        _timer = 30f / d;
        LightControl.instance.SetStrobeRate(11.0f, _timer);
        while (_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        _timer = 150f / d;
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Begining drop from high alpha to 10hz.");
        LightControl.instance.SetStrobeRate(8.5f, _timer);
        while (_timer > 0)
        {
            if (AVS_Program_ManageThetaTransition())
                yield break;
            _timer -= Time.deltaTime;
            yield return null;
        }
        yield return null;
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Starting Saw Strobe Coroutine.");
        float _wavelength = 360f / d;
        float _halfWavelength = _wavelength / 2;
        LightControl.instance.SetSawStrobe(8.5f, 11.5f, _wavelength);
        DbgLogAvs("Sequencer | AVS Program: DynamicDropStart. Waiting for absorption threshold to be met.");

        _timer = _halfWavelength;
        bool flag1 = false;
        bool flag2 = false;
        while (true)
        {
            if (AVS_Program_ManageThetaTransition())
                yield break;

            if (_timer > 0)
                _timer -= Time.deltaTime;
            else
                _timer = _halfWavelength;
            if (_timer > _halfWavelength * 3 / 4)
            {
                flag2 = false;
                if (!flag1)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60.0f, DirectorActivationBehavior.ExpireWithoutExecuting, DirectorExclusivityBehavior.ReplaceAllOfType));
                    flag1 = true;
                }
            }
            else if (_timer <= _halfWavelength * 3 / 4)
            {
                flag1 = false;
                if (!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
                    flag2 = true;
                }
            }
            yield return null;
        }
    }

    private bool AVS_Program_ManageThetaTransition()
    {
        if (((RespirationTracker.instance._absorption > _absorptionThreshold)) && !flagThetaCoroutine)
        {
            flagThetaCoroutine = true;
            CoroutineDynamicDropTheta = StartCoroutine(AVS_Program_DynamicDrop_Theta());
            return true;
        }
        return false;
    }

    private IEnumerator AVS_Program_DynamicDrop_Theta()
    {
        if (CoroutineDynamicDropStart != null)
        {
            DbgLogAvs("Sequencer | AVS Program: Stopping Coroutine from THETA Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }

        yield return null;
        Cleanup(coroutineCleanupList);
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Starting Theta program.");

        director.ClearQueueOfType("monostereo");
        if (LightControl.instance.bilateral)
        {
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60.0f, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
            DbgLogAvs("Sequencer Director Queue: (AVS Program) DynamicDrop_Theta. Since starting in bilateral, adding " + (director.queueIndex - 1) + " monostereo=mono to director queue, and waiting.");
            director.LogQueue();
        }
        while (LightControl.instance.bilateral)
            yield return null;
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 7hz.");
        float _timer = 120f / d;
        LightControl.instance.SetStrobeRate(7.0f, _timer);
        while (_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + LightControl.instance.bilateral);
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queueing Bilateral Strobe.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, 60f / d, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
        while (!LightControl.instance.bilateral)
            yield return null;
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + LightControl.instance.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f / d, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
        while (LightControl.instance.bilateral)
            yield return null;
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queueing Mono Strobe.");
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. bilateral is: " + LightControl.instance.bilateral);
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, 60f / d, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
        while (LightControl.instance.bilateral)
            yield return null;
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 6hz.");
        _timer = 60f / d;
        LightControl.instance.SetStrobeRate(6.0f, _timer);
        while (_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queuing Gamma Burst.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f / d, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
        _timer = 180f / d;
        while (_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queuing Gamma Burst Stop.");
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 180f / d, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
        _timer = 180f / d;
        while (_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Dropping to 5hz.");
        _timer = 60f / d;
        LightControl.instance.SetStrobeRate(5.0f, _timer);
        while (_timer > 0)
        {
            _timer -= Time.deltaTime;
            yield return null;
        }
        _timer = 300f / d;
        float _halfWave = _timer / 2;
        bool flag1 = false;
        bool flag2 = false;
        while (true)
        {
            if (_timer > 0)
                _timer -= Time.deltaTime;
            else
                _timer = 300f / d;
            if (_timer > _halfWave)
            {
                flag2 = false;
                if (!flag1)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(true), "gamma", false, false, 60f / d, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
                    flag1 = true;
                    DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst.");
                }
            }
            else if (_timer <= _halfWave)
            {
                flag1 = false;
                if (!flag2)
                {
                    coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, 60f / d, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
                    flag2 = true;
                    DbgLogAvs("Sequencer | AVS Program: DynamicDrop_Theta. Queuing Cycled Gamma Burst Stop.");
                }
            }
            yield return null;
        }
    }

    private IEnumerator AVS_Program_DynamicDrop_End(float transitionTime = 180f)
    {
        if (CoroutineDynamicDropStart != null)
        {
            DbgLogAvs("Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropStart);
        }
        if (CoroutineDynamicDropTheta != null)
        {
            DbgLogAvs("Sequencer | AVS Program: Stopping Coroutine from END Coroutine().");
            StopCoroutine(CoroutineDynamicDropTheta);
        }

        yield return null;
        Cleanup(coroutineCleanupList);
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_End. Starting End Program with transitionTime=" + transitionTime + "s.");

        float localTimescale = transitionTime / 180f;
        float phase1Duration = 70f * localTimescale;
        float phase2Duration = 20f * localTimescale;
        float phase3Duration = 90f * localTimescale;

        director.ClearQueueOfType("gamma");
        director.ClearQueueOfType("monostereo");
        if (!LightControl.instance.bilateral)
        {
            float queueTime1 = 30.0f * localTimescale;
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(true), "monostereo", false, true, queueTime1, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
        }
        if (LightControl.instance._gammaBurstMode != 0.0f)
        {
            float queueTime2 = 30.0f * localTimescale;
            coroutineCleanupList.Add(director.AddActionToQueue(Action_Gamma(false), "gamma", false, false, queueTime2, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));
        }

        float elapsedTime = 0f;
        while (elapsedTime < phase1Duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_End. Stabilizing before dramatic rise. Elapsed: " + elapsedTime + "s / " + transitionTime + "s");
        float queueTime3 = 10.0f * localTimescale;
        coroutineCleanupList.Add(director.AddActionToQueue(Action_Strobe_MonoStereo(false), "monostereo", false, true, queueTime3, DirectorActivationBehavior.ActivateThisActionOnNextTone, DirectorExclusivityBehavior.ReplaceAllOfType));

        while (elapsedTime < phase1Duration + phase2Duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_End. Starting dramatic rise to 40hz. Elapsed: " + elapsedTime + "s / " + transitionTime + "s");
        LightControl.instance.SetStrobeRate(40.0f, phase3Duration);

        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DbgLogAvs("Sequencer | AVS Program: DynamicDrop_End. End of AVS Program. Elapsed: " + elapsedTime + "s / " + transitionTime + "s. Goodnight!");
    }

    /// <summary>Removes tracked keys from <see cref="Director.queue"/>, then clears <paramref name="list"/>.</summary>
    private void Cleanup(List<int> list)
    {
        if (list == null || list.Count == 0)
            return;

        if (director == null)
        {
            Debug.LogError("AVSSequence: Cleanup: director is null, skipping cleanup .");
            return;
        }

        DbgLogAvs("Performing cleanup.");
        foreach (int index in list)
        {
            if (director.queue.ContainsKey(index))
            {
                DbgLogAvs("Sequencer  Director Queue (AVS Program): DynamicDrop (Transitioning). Removing " + index + " " + director.queue[index].type);
                director.queue.Remove(index);
            }
            else
            {
                DbgLogAvs("Cleanup: Key " + index + " not found in director.queue, skipping.", true);
            }
        }
        director.LogQueue();
        list.Clear();
    }

    private Action Action_Gamma(bool gammaOn)
    {
        return () => LightControl.instance.Gamma(gammaOn);
    }

    private Action Action_Strobe_MonoStereo(bool? bilateral = null)
    {
        return () => LightControl.instance.Strobe_MonoStereo(bilateral);
    }
}
