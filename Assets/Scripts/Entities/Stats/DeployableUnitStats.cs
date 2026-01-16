using UnityEngine;

[System.Serializable]
public struct DeployableUnitStats
{
    [SerializeField] private UnitStats _baseStats;
    [SerializeField] private int _deploymentCost;
    [SerializeField] private float _redeployTime;

    public DeployableUnitStats(UnitStats baseStats, int deploymentCost, float redeployTime)
    {
        _baseStats = baseStats;
        _deploymentCost = deploymentCost;
        _redeployTime = redeployTime;
    }

    public DeployableUnitStats(float health, float defense, float magicResistance, int deploymentCost, float redeployTime)
    {
        _baseStats = new UnitStats(health, defense, magicResistance);
        _deploymentCost = deploymentCost;
        _redeployTime = redeployTime;
    }    

    public int DeploymentCost => _deploymentCost;
    public float RedeployTime => _redeployTime;
    public float Health => _baseStats.Health;
    public float Defense => _baseStats.Defense;
    public float MagicResistance => _baseStats.MagicResistance;
}