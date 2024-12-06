#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "New Deployable Unit Data", menuName = "Game/Deployable Unit Data")]
public class DeployableUnitData : ScriptableObject, IBoxDeployableData
{
    // UnitData
    public string entityName;
    public DeployableUnitStats stats;
    public GameObject prefab;

    // DeployableUnitData
    public Sprite? icon;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;

    //public bool isMultiDeployable = false; // 여러 개 배치 여부
    public float cooldownTime = 0f; // 배치 후 쿨다운 시간

    // IDeployableUnitData 인터페이스 구현
    public Sprite? Icon => icon;
}

[System.Serializable]
public struct DeployableUnitStats
{
    [SerializeField] private UnitStats _baseStats;
    [SerializeField] private int _deploymentCost;
    [SerializeField] private float _redeployTime;

    public int DeploymentCost
    {
        get => _deploymentCost;
        set => _deploymentCost = value;
    }

    public float RedeployTime
    {
        get => _redeployTime;
        set => _redeployTime = value;
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