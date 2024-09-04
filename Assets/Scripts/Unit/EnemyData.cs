using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{

    public string enemyName = "enemyDefault";
    public UnitData stats; // Unit Ŭ������ baseStats�� ������ ����
    public float movementSpeed = 1f;
    public int blockCount = 1; // �����ϴ� ���� ��
    public GameObject prefab; // ���� ������
    public GameObject projectilePrefab; // ����ü ������ ����. null�̾ ����.


    // �� Ư���� �߰� �Ӽ���
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;

    public float attackRange = 0f; // ����Ʈ 0, ���Ÿ��� �� ����

}