using UnityEngine;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject, IBoxDeployableData
{
    // UnitData
    public string entityName;
    public OperatorStats stats;
    public GameObject prefab;

    // DeployableUnitData
    public Sprite? icon;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;

    // OperatorData
    public AttackType attackType;
    public AttackRangeType attackRangeType;
    public Vector2Int[] attackableTiles = { Vector2Int.zero };
    public GameObject projectilePrefab;
    public float maxSP = 30f;
    public float initialSP = 0f;
    public bool autoRecoverSP = true;

    public Sprite? Icon => icon;

}


[System.Serializable]
public struct OperatorStats
{
    [SerializeField] private DeployableUnitStats _deployableUnitStats;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _attackSpeed;
    [SerializeField] private int _maxBlockableEnemies;
    [SerializeField] private float _currentSP;
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

    public float CurrentSP
    {
        get => _currentSP;
        set => _currentSP = value;
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