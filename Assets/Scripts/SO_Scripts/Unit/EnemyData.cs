#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject, ICombatData
{
    // UnitData
    [SerializeField] protected string entityID = string.Empty; // 고유 식별자. 다른 에셋과 중복되면 안됨.
    [SerializeField] protected string entityNameLocalizationKey; // 화면에 표시될 이름을 로컬라이제이션 테이블에서 찾기 위한 키. 해당하는 언어를 불러오기 위한 키값이다.
    [SerializeField] protected EnemyStats stats;
    [SerializeField] protected GameObject prefab = default!;
    [SerializeField] protected Color primaryColor;
    [SerializeField] protected Color secondaryColor; // 필요한 경우만 사용

    // EnemyData
    [SerializeField] protected AttackType attackType;
    [SerializeField] protected AttackRangeType attackRangeType;
    // [SerializeField] protected int blockCount = 1;
    [SerializeField] protected float defaultYPosition = 0.5f;
    [SerializeField] protected int playerDamage = 1; // 도착 지점에 도착했을 때 차감할 라이프 포인트 수

    [Header("For Ranged")]
    [SerializeField] protected GameObject? projectilePrefab;

    [Header("VFX Effects")]
    [SerializeField] protected GameObject? meleeAttackEffectPrefab; // 근접 공격 이펙트
    [SerializeField] protected GameObject hitEffectPrefab = default!; // 공격이 적중했을 때의 이펙트

    public virtual void CreateObjectPools()
    {
        if (projectilePrefab != null)
        {
            ObjectPoolManager.Instance?.CreatePool(ProjectileTag, projectilePrefab, 5);
        }
        if (meleeAttackEffectPrefab != null)
        {
            ObjectPoolManager.Instance?.CreatePool(MeleeAttackVFXTag, meleeAttackEffectPrefab, 10);
        }
        if (hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance?.CreatePool(HitVFXTag, hitEffectPrefab, 10);
        }
    }

    // 오브젝트 풀 태그
    protected string _unitTag;
    protected string _projectileTag;
    protected string _meleeAttackVFXTag;
    protected string _hitVFXTag;

    // 프로퍼티들
    // UnitData 관련 프로퍼티
    public string EntityID => entityID;
    public EnemyStats Stats => stats;
    public GameObject Prefab => prefab;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;

    // EnemyData 관련 프로퍼티
    public AttackType AttackType => attackType;
    public AttackRangeType AttackRangeType => attackRangeType;
    // public int BlockCount => blockCount;
    public float DefaultYPosition => defaultYPosition;
    public int PlayerDamage => playerDamage;
    
    // 원거리 공격 관련 프로퍼티
    public GameObject? ProjectilePrefab => projectilePrefab;

    // 이펙트 관련 프로퍼티
    public GameObject? MeleeAttackEffectPrefab => meleeAttackEffectPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;

    // 오브젝트 풀 태그 프로퍼티
    // public string UnitTag => _unitTag ??= $"Opereator_{entityID}";
    // public string ProjectileTag => _projectileTag ??= $"{entityID}_Projectile";
    // public string MeleeAttackVFXTag => _meleeAttackVFXTag ??= $"{entityID}_MeleeAttackVFX";
    // public string HitVFXTag => _hitVFXTag ??= $"{entityID}_HitVFX";
    public string UnitTag
    {
        get
        {
            if (string.IsNullOrEmpty(_unitTag))
            {
                _unitTag = $"Enemy_{entityID}";
            }
            return _unitTag;
        }
    }
    public string ProjectileTag
    {
        get
        {
            if (string.IsNullOrEmpty(_projectileTag))
            {
                _projectileTag = $"{entityID}_Projectile";
            }
            return _projectileTag;
        }
    }
    public string MeleeAttackVFXTag
    {
        get
        {
            if (string.IsNullOrEmpty(_meleeAttackVFXTag))
            {
                _meleeAttackVFXTag = $"{entityID}_MeleeAttackVFX";
            }
            return _meleeAttackVFXTag;
        }
    }
    public string HitVFXTag
    {
        get
        {
            if (string.IsNullOrEmpty(_hitVFXTag))
            {
                _hitVFXTag = $"Operator_{entityID}_HitVFX";
            }
            return _hitVFXTag;
        }
    }
}



[System.Serializable]
public struct EnemyStats
{
    [SerializeField] private UnitStats _baseStats;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _attackSpeed;
    [SerializeField] private float _attackRange;
    [SerializeField] private int _blockSize; // 자신이 차지하는 저지 수

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
    public int BlockSize
    {
        get => _blockSize;
        set => _blockSize = value;
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