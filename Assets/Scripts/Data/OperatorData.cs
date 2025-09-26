using System.Collections.Generic;
using UnityEngine;
using Skills.Base;

// 필수 필드는 ?이 없고, 선택 필드는 ?이 있다 
[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject, ICombatData
{
    // UnitData
    public string entityName = string.Empty;
    public OperatorClass operatorClass;
    public OperatorStats stats; // 초기값 할당이 없어도 nullable과 상관 없나봄
    public GameObject prefab = default!;
    [SerializeField] protected Color primaryColor;
    [SerializeField] protected Color secondaryColor; // 필요한 경우만 사용

    // DeployableUnitData
    public Sprite? icon; // 오퍼레이터에 사용될 그림. null일 수 있다고 하겠음
    public Sprite? Icon => icon;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;

    // OperatorData
    public AttackType attackType;
    public AttackRangeType attackRangeType;
    public List<Vector2Int> attackableTiles = new List<Vector2Int>{ Vector2Int.zero };
    public OperatorSkill elite0Skill = default!; // 최초 스킬
    public float initialSP = 0f;

    [Header("VisualEffects")] 
    public GameObject deployEffectPrefab = default!; // 배치 이펙트
    public GameObject? meleeAttackEffectPrefab; // 근접 공격 이펙트(공격 시도 시 발생)
    public GameObject hitEffectPrefab = default!; // 공격이 적중했을 때의 이펙트

    [Header("For Ranged")]
    public GameObject? projectilePrefab;
    public GameObject muzzleVFXPrefab = default!; // 원거리 공격일 때 발사하는 객체에서 나오는 총구 효과

    [Header("Level Up Stats")]
    public OperatorLevelStats levelStats = default!; // 레벨 1이 올라갈 때마다 상승하는 스탯량

    [Header("Elite Phase Settings")]
    public ElitePhaseUnlocks elite1Unlocks = default!;

    [Header("Promotion Required Items")]
    public List<PromotionItems> promotionItems = default!;

    public Color PrimaryColor => primaryColor; 
    public Color SecondaryColor => secondaryColor; 
    public GameObject HitEffectPrefab => hitEffectPrefab;
    public AttackType AttackType => attackType;
    public AttackRangeType AttackRangeType => attackRangeType;

    public enum OperatorClass
    {
        Vanguard,
        Guard,
        Defender,
        Caster,
        Sniper,
        Medic,
        Artillery, // 포격사수
        DualBlade,
    }

    [System.Serializable]
    public class OperatorLevelStats
    {
        public float healthPerLevel;
        public float attackPowerPerLevel;
        public float defensePerLevel;
        public float magicResistancePerLevel;
    }

    [System.Serializable] 
    public class ElitePhaseUnlocks
    {
        [Header("Attack Range Changes")]
        public List<Vector2Int>? additionalAttackTiles;

        [Header("New Skills")]
        public OperatorSkill? unlockedSkill;
    }

    [System.Serializable]
    public class PromotionItems
    {
        public ItemData itemData;
        public int count;
    }
}

[System.Serializable]
public struct OperatorStats
{
    [SerializeField] private DeployableUnitStats _deployableUnitStats;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _attackSpeed;
    [SerializeField] private int _maxBlockableEnemies;
    [SerializeField] private float _spRecoveryRate;

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

    public int MaxBlockableEnemies
    {
        get => _maxBlockableEnemies;
        set => _maxBlockableEnemies = value;
    }
    
    public float SPRecoveryRate
    {
        get => _spRecoveryRate;
        set => _spRecoveryRate = value;
    }

    // Convenience properties for nested access
    public float Health
    {
        get => _deployableUnitStats.Health;
        set => _deployableUnitStats.Health = value;
    }

    public float Defense
    {
        get => _deployableUnitStats.Defense;
        set => _deployableUnitStats.Defense = value;
    }

    public float MagicResistance
    {
        get => _deployableUnitStats.MagicResistance;
        set => _deployableUnitStats.MagicResistance = value;
    }

    public int DeploymentCost
    {
        get => _deployableUnitStats.DeploymentCost;
        set => _deployableUnitStats.DeploymentCost = value;
    }

    public float RedeployTime
    {
        get => _deployableUnitStats.RedeployTime;
        set => _deployableUnitStats.RedeployTime = value;
    }
}