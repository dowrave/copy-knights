using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{

    public string enemyName = "enemyDefault";
    public UnitStats baseStats; // Unit Ŭ������ baseStats�� ������ ����
    public AttackType attackType; // ����, ����, Ʈ��
    public AttackRangeType attackRangeType; // �ٰŸ�, ���Ÿ�
    public float movementSpeed = 1f;
    public int blockCount = 1; // �����ϴ� ���� ��
    public GameObject prefab; // ���� ������
    public GameObject projectilePrefab; // ����ü ������ ����. null�̾ ����.


    // �� Ư���� �߰� �Ӽ���
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;

    public float attackRange = 0f; // ����Ʈ 0, ���Ÿ��� �� ����

}