using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Sequencer))]
public class SequencerEditor : Editor
{
    static readonly string[] DualStageReadOnlyPropertyNames =
    {
        "dualstageStage",
        "dualstageSecondStageIsMusic",
        "dualstageSecondStageIsSoundSelf",
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        InspectorFieldDrawUtility.DrawPropertiesInOrder(
            serializedObject,
            DualStageReadOnlyPropertyNames);

        serializedObject.ApplyModifiedProperties();
    }
}
