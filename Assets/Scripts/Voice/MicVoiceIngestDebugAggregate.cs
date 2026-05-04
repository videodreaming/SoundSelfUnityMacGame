using UnityEngine;

/// <summary>
/// Copies MicPipeline ingest debug + Imitone raw-path debug + tone/imitone gate flags
/// (+ optional DirectVoiceMonitoring transport totals)
/// into one Inspector block after upstream Update() (LateUpdate).
/// </summary>
public class MicVoiceIngestDebugAggregate : MonoBehaviour
{
    [Header("References (auto-filled from this GameObject if empty)")]
    [SerializeField] private MicPipeline micPipeline;
    [SerializeField] private ImitoneVoiceIntepreter interpreter;
    [Tooltip("Optional: cumulative underflow/overflow/starvation from monitoring ring pull.")]
    [SerializeField] private DirectVoiceMonitoring voiceMonitoring;

    [Header("Tone / imitone gate (ImitoneVoiceIntepreter — public runtime flags)")]
    [SerializeField] private bool aggImitoneActive;
    [SerializeField] private bool aggImitoneActiveRaw;
    [SerializeField] private bool aggToneActive;
    [SerializeField] private bool aggToneActiveRaw;
    [SerializeField] private bool aggToneActiveConfident;
    [SerializeField] private bool aggToneActiveVeryConfident;
    [SerializeField] private bool aggToneActiveVeryConfidentRaw;
    [SerializeField] private bool aggToneActiveBiasTrue;
    [SerializeField] private bool aggToneActiveFrame;
    [SerializeField] private bool aggToneActiveConfidentFrame;
    [SerializeField] private bool aggToneActiveBiasTrueFrame;
    [SerializeField] private int aggToneActiveCounter;
    [SerializeField] private int aggToneActiveConfidentCounter;

    [Header("Mic ingest (MicPipeline — same frame as interpreter below)")]
    [SerializeField] private string aggMicExitReason = "";
    [SerializeField] private int aggMicUnreadComputed = -1;
    [SerializeField] private int aggMicLatestRawSampleCount = -1;
    [SerializeField] private int aggMicPosWrite = -1;
    [SerializeField] private int aggMicPosRead = -1;
    [SerializeField] private int aggMicStalledWriteHeadFrames;
    [SerializeField] private int aggMicClipSamples;
    [SerializeField] private int aggMicUnityFrame;
    [SerializeField] private int aggMicGentleUnreadZeroConsecutiveFrames;
    [SerializeField] private int aggMicGentleUnreadZeroRecoveryTotal;
    [SerializeField] private bool aggMicGentleRecoveryEnabled;
    [SerializeField] private long aggMicRawRingWriteTotalSamples;
    [SerializeField] private long aggMicNormRingWriteTotalSamples;

    [Header("Interpreter raw path (ImitoneVoiceIntepreter)")]
    [SerializeField] private bool aggRawConsumedThisFrame;
    [SerializeField] private bool aggInterpMicRefNull;
    [SerializeField] private bool aggInterpMicReady;
    [SerializeField] private bool aggInterpTryCopyTrue;
    [SerializeField] private int aggInterpTryCopySampleCount = -1;
    [SerializeField] private float aggInterpMicDbUnclamped = -999f;
    [SerializeField] private float aggInterpImitoneDbUnclamped = -999f;

    [Header("Monitoring transport (DirectVoiceMonitoring — cumulative)")]
    [SerializeField] private bool aggMonitoringAssigned;
    [SerializeField] private int aggMonUnderflowEvents;
    [SerializeField] private int aggMonUnderflowSamples;
    [SerializeField] private int aggMonOverflowEvents;
    [SerializeField] private int aggMonOverflowSamples;
    [SerializeField] private int aggMonStarvationEvents;

    private void Awake()
    {
        if (micPipeline == null)
        {
            micPipeline = GetComponent<MicPipeline>();
        }

        if (interpreter == null)
        {
            interpreter = GetComponent<ImitoneVoiceIntepreter>();
        }

        if (voiceMonitoring == null)
        {
            voiceMonitoring = GetComponent<DirectVoiceMonitoring>();
            if (voiceMonitoring == null)
            {
                voiceMonitoring = GetComponentInChildren<DirectVoiceMonitoring>(true);
            }
        }
    }

    private void LateUpdate()
    {
        if (micPipeline != null)
        {
            MicPipeline.MicIngestDebugSnapshot m = micPipeline.GetMicIngestDebugSnapshot();
            aggMicExitReason = m.lastExitReason;
            aggMicUnreadComputed = m.lastUnreadComputed;
            aggMicLatestRawSampleCount = m.lastLatestRawSampleCount;
            aggMicPosWrite = m.lastMicPosWrite;
            aggMicPosRead = m.lastMicPosRead;
            aggMicStalledWriteHeadFrames = m.lastStalledWriteHeadFrameCount;
            aggMicClipSamples = m.lastClipSamples;
            aggMicUnityFrame = m.lastUnityFrame;
            aggMicGentleUnreadZeroConsecutiveFrames = m.gentleUnreadZeroConsecutiveFrames;
            aggMicGentleUnreadZeroRecoveryTotal = m.gentleUnreadZeroRecoveryTotal;
            aggMicGentleRecoveryEnabled = m.gentleUnreadZeroRecoveryEnabled;
            aggMicRawRingWriteTotalSamples = m.rawRingWriteTotalSamples;
            aggMicNormRingWriteTotalSamples = m.normalizedRingWriteTotalSamples;
        }

        if (interpreter != null)
        {
            ImitoneVoiceIntepreter.RawVoicePathDebugSnapshot v = interpreter.GetRawVoicePathDebugSnapshot();
            aggRawConsumedThisFrame = v.rawVoiceDataConsumedThisFrame;
            aggInterpMicRefNull = v.interpreterMicPipelineRefNull;
            aggInterpMicReady = v.interpreterMicPipelineReady;
            aggInterpTryCopyTrue = v.interpreterTryCopyReturnedTrue;
            aggInterpTryCopySampleCount = v.interpreterTryCopyOutSampleCount;
            aggInterpMicDbUnclamped = v.telemetryMicDbUnclamped;
            aggInterpImitoneDbUnclamped = v.telemetryImitoneDbUnclamped;

            aggImitoneActive = interpreter.imitoneActive;
            aggImitoneActiveRaw = interpreter.imitoneActiveRaw;
            aggToneActive = interpreter.toneActive;
            aggToneActiveRaw = interpreter.toneActiveRaw;
            aggToneActiveConfident = interpreter.toneActiveConfident;
            aggToneActiveVeryConfident = interpreter.toneActiveVeryConfident;
            aggToneActiveVeryConfidentRaw = interpreter.toneActiveVeryConfidentRaw;
            aggToneActiveBiasTrue = interpreter.toneActiveBiasTrue;
            aggToneActiveFrame = interpreter.toneActiveFrame;
            aggToneActiveConfidentFrame = interpreter.toneActiveConfidentFrame;
            aggToneActiveBiasTrueFrame = interpreter.toneActiveBiasTrueFrame;
            aggToneActiveCounter = interpreter.toneActiveCounter;
            aggToneActiveConfidentCounter = interpreter.toneActiveConfidentCounter;
        }

        aggMonitoringAssigned = voiceMonitoring != null;
        if (voiceMonitoring != null)
        {
            voiceMonitoring.GetBufferedTransportTotals(
                out aggMonUnderflowEvents,
                out aggMonUnderflowSamples,
                out aggMonOverflowEvents,
                out aggMonOverflowSamples,
                out aggMonStarvationEvents);
        }
    }
}
