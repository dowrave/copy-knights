using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : UnitData
{
    public new EnemyStats stats; // Unit 클래스의 baseStats와 동일한 구조

    public AttackType attackType;
    public AttackRangeType attackRangeType;

    public int blockCount = 1; // 차지하는 저지 수

    public GameObject projectilePrefab; // 투사체 프리팹 정보. null이어도 무관.
}