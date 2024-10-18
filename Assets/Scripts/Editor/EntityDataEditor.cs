using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(UnitEntity), true)]
public class UnitEntityEditor : Editor
{
    // �ν����Ϳ��� ������ �ʵ� ����Ʈ
    private List<string> fieldsToExclude = new List<string> { "unitData", "deployableUnitData", "operatorData", "baseStats" };

    /// <summary>
    /// �ν����� GUI�� �׸��� ���� �޼���
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // ����ȭ ��ü ������Ʈ
        var unitEntity = target as UnitEntity;
        var currentType = unitEntity?.GetType();

        // Operator �� �ڽ� Ŭ������� OperatorData�� ��쵵�� ��
        if (typeof(Operator).IsAssignableFrom(currentType))
        {
            DrawOperatorInspector();
        }
        else
        {
            DrawDefaultInspector();
        }

        serializedObject.ApplyModifiedProperties(); // ����� �Ӽ� ����
    }

    /// <summary>
    /// Operator Ŭ������ �ν����͸� �׸�
    /// </summary>
    private void DrawOperatorInspector()
    {
        // operatorData �ʵ带 ǥ��
        var operatorDataProperty = serializedObject.FindProperty("operatorData");
        if (operatorDataProperty != null)
        {
            EditorGUILayout.PropertyField(operatorDataProperty, true);
        }

        DrawRemainingFields("operatorData");
    }

    /// <summary>
    /// �⺻ �ν����͸� �׸�
    /// </summary>
    private void DrawDefaultInspector()
    {
        var unitEntity = target as UnitEntity;
        var currentType = unitEntity.GetType();

        // ���� Ÿ�Կ� �ش��ϴ� ������ �ʵ常 ǥ��
        var dataField = FindDataField(currentType);

        if (dataField != null)
        {
            var property = serializedObject.FindProperty(dataField.Name);
            EditorGUILayout.PropertyField(property, new GUIContent("Data"), true);
        }

        // ������ �ʵ�� ǥ��
        DrawRemainingFields(dataField?.Name);
    }

    /// <summary>
    /// �־��� Ÿ�԰� �� �θ� Ÿ�Ե鿡�� ������ �ʵ带 ã�� �޼���
    /// </summary>
    private FieldInfo FindDataField(System.Type type)
    {
        while (type != null && type != typeof(object))
        {
            // ���÷����� ����� private �ʵ� �˻�
            var dataField = type.GetField("unitData", BindingFlags.Instance | BindingFlags.NonPublic) ??
                            type.GetField("deployableUnitData", BindingFlags.Instance | BindingFlags.NonPublic) ??
                            type.GetField("opertaorData", BindingFlags.Instance | BindingFlags.NonPublic) ?? 
                            type.GetField("enemyData", BindingFlags.Instance | BindingFlags.NonPublic);

            if (dataField != null)
            {
                return dataField;
            }

            type = type.BaseType; // �θ� Ÿ������ �̵�
        }

        return null;
    } 

    /// <summary>
    /// ������ �ʵ���� �׸�
    /// </summary>
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