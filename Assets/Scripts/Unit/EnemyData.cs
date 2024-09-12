using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public EnemyStats stats;

    public AttackType attackType;
    public AttackRangeType attackRangeType;

    public int blockCount = 1;

    public GameObject projectilePrefab;
    public GameObject prefab;
}