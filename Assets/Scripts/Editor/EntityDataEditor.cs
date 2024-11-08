using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(UnitEntity), true)]
public class UnitEntityEditor : Editor
{
    // 인스펙터에서 제외할 필드 리스트
    private List<string> fieldsToExclude = new List<string> { "unitData", "deployableUnitData", "operatorData", "baseStats" };

    /// <summary>
    /// 인스펙터 GUI를 그리는 메인 메서드
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // 직렬화 객체 업데이트
        var unitEntity = target as UnitEntity;
        var currentType = unitEntity?.GetType();

        // Operator 및 자식 클래스라면 OperatorData를 띄우도록 함
        if (typeof(Operator).IsAssignableFrom(currentType))
        {
            DrawOperatorInspector();
        }
        else
        {
            DrawDefaultInspector();
        }

        serializedObject.ApplyModifiedProperties(); // 변경된 속성 적용
    }

    /// <summary>
    /// Operator 클래스의 인스펙터를 그림
    /// </summary>
    private void DrawOperatorInspector()
    {
        // operatorData 필드를 표시
        var operatorDataProperty = serializedObject.FindProperty("operatorData");
        if (operatorDataProperty != null)
        {
            EditorGUILayout.PropertyField(operatorDataProperty, true);
        }

        DrawRemainingFields("operatorData");
    }

    /// <summary>
    /// 기본 인스펙터를 그림
    /// </summary>
    private void DrawDefaultInspector()
    {
        var unitEntity = target as UnitEntity;
        var currentType = unitEntity.GetType();

        // 현재 타입에 해당하는 데이터 필드만 표시
        var dataField = FindDataField(currentType);

        if (dataField != null)
        {
            var property = serializedObject.FindProperty(dataField.Name);
            EditorGUILayout.PropertyField(property, new GUIContent("Data"), true);
        }

        // 나머지 필드들 표시
        DrawRemainingFields(dataField?.Name);
    }

    /// <summary>
    /// 주어진 타입과 그 부모 타입들에서 데이터 필드를 찾는 메서드
    /// </summary>
    private FieldInfo FindDataField(System.Type type)
    {
        while (type != null && type != typeof(object))
        {
            // 리플렉션을 사용해 private 필드 검색
            var dataField = type.GetField("unitData", BindingFlags.Instance | BindingFlags.NonPublic) ??
                            type.GetField("deployableUnitData", BindingFlags.Instance | BindingFlags.NonPublic) ??
                            type.GetField("opertaorData", BindingFlags.Instance | BindingFlags.NonPublic) ?? 
                            type.GetField("enemyData", BindingFlags.Instance | BindingFlags.NonPublic);

            if (dataField != null)
            {
                return dataField;
            }

            type = type.BaseType; // 부모 타입으로 이동
        }

        return null;
    } 

    /// <summary>
    /// 나머지 필드들을 그림
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