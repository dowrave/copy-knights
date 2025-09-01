#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject, ICombatData
{
    // UnitData
    public string entityName = string.Empty;
    public EnemyStats stats;
    public GameObject prefab = default!;

    // EnemyData
    public AttackType attackType;
    public AttackRangeType attackRangeType;
    public int blockCount = 1;
    public GameObject? projectilePrefab;

    public float defaultYPosition = 0.5f;

    [Header("VFX Effects")]
    public GameObject? meleeAttackEffectPrefab; // 근접 공격 이펙트
    public GameObject hitEffectPrefab = default!; // 공격이 적중했을 때의 이펙트

    public GameObject HitEffectPrefab => hitEffectPrefab;
    public AttackType AttackType => attackType;
    public AttackRangeType AttackRangeType => attackRangeType;
}



[System.Serializable]
public struct EnemyStats
{
    [SerializeField] private UnitStats _baseStats;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _attackSpeed;
    [SerializeField] private float _attackRange;

    public UnitStats BaseStats
    {
        get => _baseStats;
        set => _baseStats = value;
    }

    public float MovementSpeed
    {
        get => _movementSpeed;
        set => _movementSpeed = value;
    }

    public float AttackPower
    {
        get => _attackPower;
        set => _attackPower = value;
    }

    public float AttackSpeed
    {
        get => _attackSpeed;
        set => _attackSpeed = value;
    }

    public float AttackRange
    {
        get => _attackRange;
        set => _attackRange = value;
    }

    // Convenience properties for nested access
    public float Health
    {
        get => _baseStats.Health;
        set => _baseStats.Health = value;
    }

    public float Defense
    {
        get => _baseStats.Defense;
        set => _baseStats.Defense = value;
    }

    public float MagicResistance
    {
        get => _baseStats.MagicResistance;
        set => _baseStats.MagicResistance = value;
    }
}
#nullable restore