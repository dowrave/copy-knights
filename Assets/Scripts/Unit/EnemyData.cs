using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : UnitData
{
    public new EnemyStats stats; // Unit Ŭ������ baseStats�� ������ ����

    public AttackType attackType;
    public AttackRangeType attackRangeType;

    public int blockCount = 1; // �����ϴ� ���� ��

    public GameObject projectilePrefab; // ����ü ������ ����. null�̾ ����.
}