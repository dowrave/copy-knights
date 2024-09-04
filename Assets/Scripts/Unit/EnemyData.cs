using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{

    public string enemyName = "enemyDefault";
    public UnitData stats; // Unit 클래스의 baseStats와 동일한 구조
    public float movementSpeed = 1f;
    public int blockCount = 1; // 차지하는 저지 수
    public GameObject prefab; // 적의 프리팹
    public GameObject projectilePrefab; // 투사체 프리팹 정보. null이어도 무관.


    // 적 특유의 추가 속성들
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;

    public float attackRange = 0f; // 디폴트 0, 원거리만 값 설정

}