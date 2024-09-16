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

        // Operator Ŭ������ ��� Ư�� ó��
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
        // operatorData �ʵ带 ǥ��
        var operatorDataProperty = serializedObject.FindProperty("operatorData");
        if (operatorDataProperty != null)
        {
            EditorGUILayout.PropertyField(operatorDataProperty, true);
        }

        // ������ �ʵ�� ǥ�� (operatorData ����)
        DrawRemainingFields("operatorData");
    }

    private void DrawDefaultInspector()
    {
        var unitEntity = target as UnitEntity;
        var currentType = unitEntity.GetType();

        // ���� Ÿ�Կ� �ش��ϴ� ������ �ʵ常 ǥ��
        var dataField = currentType.GetField("unitData", BindingFlags.Instance | BindingFlags.NonPublic) ??
                        currentType.GetField("deployableUnitData", BindingFlags.Instance | BindingFlags.NonPublic);

        if (dataField != null)
        {
            var property = serializedObject.FindProperty(dataField.Name);
            EditorGUILayout.PropertyField(property, new GUIContent("Data"), true);
        }

        // ������ �ʵ�� ǥ��
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