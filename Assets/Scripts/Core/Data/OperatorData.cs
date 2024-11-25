using System.Collections.Generic;
using UnityEngine;
using Skills.Base;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject, IBoxDeployableData
{
    // UnitData
    public string entityName;
    public OperatorClass operatorClass;
    public OperatorStats stats;
    public GameObject prefab;

    // DeployableUnitData
    public Sprite? icon;
    public Sprite? Icon => icon;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;

    // OperatorData
    public AttackType attackType;
    public AttackRangeType attackRangeType;
    public List<Vector2Int> attackableTiles = new List<Vector2Int>{ Vector2Int.zero };
    public GameObject projectilePrefab;

    public List<Skill> skills;
    public int defaultSkillIndex;
    public float initialSP = 0f;

    [Header("Elite Phase Settings")]
    public ElitePhaseUnlocks elite1Unlocks;


    public enum OperatorClass
    {
        Vanguard,
        Guard,
        Defender,
        Caster,
        Sniper,
        Medic
    }
}

[System.Serializable] 
public class ElitePhaseUnlocks
{
    [Header("Attack Range Changes")]
    public Vector2Int[] additionalAttackTiles;

    [Header("New Skills")]
    public Skill unlockedSkill;
}


[System.Serializable]
public struct OperatorStats
{
    [SerializeField] private DeployableUnitStats _deployableUnitStats;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _attackSpeed;
    [SerializeField] private int _maxBlockableEnemies;
    [SerializeField] private float _startSP;
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

    public float StartSP
    {
        get => _startSP;
        set => _startSP = value;
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

#nullable restore