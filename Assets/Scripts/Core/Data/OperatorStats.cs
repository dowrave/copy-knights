[System.Serializable]
public struct OperatorStats
{
    public DeployableUnitStats baseStats;
    public float attackPower;
    public float attackSpeed;
    public int maxBlockableEnemies;
    public float currentSP;
    public float spRecoveryRate;

    // UnitStats의 프로퍼티들
    public float Health
    {
        get => baseStats.Health;
        set => baseStats.Health = value;
    }

    public float Defense
    {
        get => baseStats.Defense;
        set => baseStats.Defense = value;
    }

    public float MagicResistance
    {
        get => baseStats.MagicResistance;
        set => baseStats.MagicResistance = value;
    }

    // DeployableUnitStats의 프로퍼티들
    public int DeploymentCost
    {
        get => baseStats.deploymentCost;
        set => baseStats.deploymentCost = value;
    }

    public float RedeployTime
    {
        get => baseStats.redeployTime;
        set => baseStats.redeployTime = value;
    }
}