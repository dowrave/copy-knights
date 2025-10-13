#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject, ICombatData
{
    // UnitData
    [SerializeField] protected string entityName = string.Empty;
    [SerializeField] protected EnemyStats stats;
    [SerializeField] protected GameObject prefab = default!;
    [SerializeField] protected Color primaryColor;
    [SerializeField] protected Color secondaryColor; // 필요한 경우만 사용

    // EnemyData
    [SerializeField] protected AttackType attackType;
    [SerializeField] protected AttackRangeType attackRangeType;
    [SerializeField] protected int blockCount = 1;
    [SerializeField] protected float defaultYPosition = 0.5f;
    [SerializeField] protected int playerDamage = 1; // 도착 지점에 도착했을 때 차감할 라이프 포인트 수

    [Header("For Ranged")]
    [SerializeField] protected GameObject? projectilePrefab;

    [Header("VFX Effects")]
    [SerializeField] protected GameObject? meleeAttackEffectPrefab; // 근접 공격 이펙트
    [SerializeField] protected GameObject hitEffectPrefab = default!; // 공격이 적중했을 때의 이펙트

    public void CreateObjectPools()
    {
        if (projectilePrefab != null)
        {
            ObjectPoolManager.Instance?.CreatePool(GetProjectileTag(), projectilePrefab, 5);
        }
        if (meleeAttackEffectPrefab != null)
        {
            ObjectPoolManager.Instance?.CreatePool(GetMeleeAttackVFXTag(), meleeAttackEffectPrefab, 10);
        }
        if (hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance?.CreatePool(GetHitVFXTag(), hitEffectPrefab, 10);
        }
    }

    public string GetUnitTag() => $"Enemy_{entityName}";
    public string GetProjectileTag() => $"{entityName}_Projectile";
    public string GetMeleeAttackVFXTag() => $"{entityName}_MeleeAttackVFX";
    public string GetHitVFXTag() => $"{entityName}_HitVFX";

    // UnitData 관련 프로퍼티
    public string EntityName => entityName;
    public EnemyStats Stats => stats;
    public GameObject Prefab => prefab;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;

    // EnemyData 관련 프로퍼티
    public AttackType AttackType => attackType;
    public AttackRangeType AttackRangeType => attackRangeType;
    public int BlockCount => blockCount;
    public float DefaultYPosition => defaultYPosition;
    public int PlayerDamage => playerDamage;
    
    // 원거리 공격 관련 프로퍼티
    public GameObject? ProjectilePrefab => projectilePrefab;

    // 이펙트 관련 프로퍼티
    public GameObject? MeleeAttackEffectPrefab => meleeAttackEffectPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;
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