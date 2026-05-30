using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Draws serialized properties in default order; object references stay editable.</summary>
internal static class InspectorFieldDrawUtility
{
    public static void DrawPropertiesInOrder(
        SerializedObject serializedObject,
        IReadOnlyCollection<string> readOnlyPropertyNames,
        params string[] excludePropertyNames)
    {
        var readOnly = new HashSet<string>(readOnlyPropertyNames);
        var exclude = new HashSet<string>(excludePropertyNames) { "m_Script" };

        var prop = serializedObject.GetIterator();
        var enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (exclude.Contains(prop.name))
                continue;

            bool isReference = prop.propertyType == SerializedPropertyType.ObjectReference;
            bool readOnlyField = readOnly.Contains(prop.name);
            EditorGUI.BeginDisabledGroup(readOnlyField && !isReference);
            EditorGUILayout.PropertyField(prop, true);
            EditorGUI.EndDisabledGroup();
        }
    }
}
