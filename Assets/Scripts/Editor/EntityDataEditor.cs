using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(UnitEntity), true)]
public class UnitEntityEditor : Editor
{
    private List<string> fieldsToExclude = new List<string> { "unitData", "deployableUnitData", "operatorData", "baseStats" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var unitEntity = target as UnitEntity;
        var currentType = unitEntity.GetType();

        // Operator 클래스인 경우 특별 처리
        if (currentType == typeof(Operator))
        {
            DrawOperatorInspector();
        }
        else
        {
            DrawDefaultInspector();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawOperatorInspector()
    {
        // operatorData 필드를 표시
        var operatorDataProperty = serializedObject.FindProperty("operatorData");
        if (operatorDataProperty != null)
        {
            EditorGUILayout.PropertyField(operatorDataProperty, true);
        }

        // 나머지 필드들 표시 (operatorData 제외)
        DrawRemainingFields("operatorData");
    }

    private void DrawDefaultInspector()
    {
        var unitEntity = target as UnitEntity;
        var currentType = unitEntity.GetType();

        // 현재 타입에 해당하는 데이터 필드만 표시
        var dataField = currentType.GetField("unitData", BindingFlags.Instance | BindingFlags.NonPublic) ??
                        currentType.GetField("deployableUnitData", BindingFlags.Instance | BindingFlags.NonPublic);

        if (dataField != null)
        {
            var property = serializedObject.FindProperty(dataField.Name);
            EditorGUILayout.PropertyField(property, new GUIContent("Data"), true);
        }

        // 나머지 필드들 표시
        DrawRemainingFields(dataField?.Name);
    }

    private void DrawRemainingFields(string excludeFieldName)
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (fieldsToExclude.Contains(iterator.name) || iterator.name == excludeFieldName)
                continue;
            EditorGUILayout.PropertyField(iterator, true);
        }
    }
}