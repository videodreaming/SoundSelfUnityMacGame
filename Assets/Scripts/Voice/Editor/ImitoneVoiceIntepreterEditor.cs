using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ImitoneVoiceIntepreter))]
public class ImitoneVoiceIntepreterEditor : Editor
{
    static readonly string[] ReadOnlyPropertyNames =
    {
        // Pre-header runtime mirrors (frozen).
        "gameOn",
        "pitch_hz",
        "note_st",
        "toneActiveBiasTrueTimer",
        "_dbThreshold",
        // dbController / Noise Floor (all match scene; locked).
        "_volumeChangeMeasurementWindow",
        "_volumeDropTriggerThresholdDB",
        "_volumeJumpTriggerThresholdDB",
        "_afterDropWaitTime",
        "_noiseFloorMeasurementTime",
        "noiseFloorMeasurementMaxAge",
        "_thresholdAboveNoiseFloor",
        "_noiseFloorThreshold",
        // Band Pass Filter cutoffs.
        "_highPassCutoffHz",
        "_lowPassCutoffHz",
        // Normalization.
        "normalizationEnabled",
        "normalizationGainDb",
        "normalizationClampAbs",
        // Normalization Gain Riding (all match scene; locked).
        "gainRidingEnabled",
        "gainRidingTargetDb",
        "gainRidingRaiseThresholdDb",
        "gainRidingLowerThresholdDb",
        "gainRidingRapidLowerThresholdDb",
        "gainRidingRaiseMaxToneActiveConfidentSeconds",
        "gainRidingRaiseRateDbPerSecond",
        "gainRidingLowerRateDbPerSecond",
        "gainRidingRapidLowerRateDbPerSecond",
        "gainRidingGainDbClamp",
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Gray fields are read-only (see Docs/INSPECTOR_CLEANUP_IMITONE_AND_DVR2.md). " +
            "Object references stay editable. Expand checklist as you lock more fields.",
            MessageType.None);

        InspectorFieldDrawUtility.DrawPropertiesInOrder(serializedObject, ReadOnlyPropertyNames);

        serializedObject.ApplyModifiedProperties();
    }
}
