using System.Collections.Generic;
using UnityEngine;
using Skills.Base;

// �ʼ� �ʵ�� ?�� ����, ���� �ʵ�� ?�� �ִ� 
[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject, ICombatData
{
    // UnitData
    public string entityName = string.Empty;
    public OperatorClass operatorClass;
    public OperatorStats stats; // �ʱⰪ �Ҵ��� ��� nullable�� ��� ������
    public GameObject prefab = default!;
    [SerializeField] protected Color primaryColor;
    [SerializeField] protected Color secondaryColor; // �ʿ��� ��츸 ���

    // DeployableUnitData
    public Sprite? icon; // ���۷����Ϳ� ���� �׸�. null�� �� �ִٰ� �ϰ���
    public Sprite? Icon => icon;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;

    // OperatorData
    public AttackType attackType;
    public AttackRangeType attackRangeType;
    public List<Vector2Int> attackableTiles = new List<Vector2Int>{ Vector2Int.zero };
    public OperatorSkill elite0Skill = default!; // ���� ��ų
    public float initialSP = 0f;

    [Header("VisualEffects")] 
    public GameObject deployEffectPrefab = default!; // ��ġ ����Ʈ
    public GameObject? meleeAttackEffectPrefab; // ���� ���� ����Ʈ(���� �õ� �� �߻�)
    public GameObject hitEffectPrefab = default!; // ������ �������� ���� ����Ʈ

    [Header("For Ranged")]
    public GameObject? projectilePrefab;
    public GameObject muzzleVFXPrefab = default!; // ���Ÿ� ������ �� �߻��ϴ� ��ü���� ������ �ѱ� ȿ��

    [Header("Level Up Stats")]
    public OperatorLevelStats levelStats = default!; // ���� 1�� �ö� ������ ����ϴ� ���ȷ�

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
        Artillery, // ���ݻ��
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