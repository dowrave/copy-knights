[System.Serializable]
public struct DeployableUnitStats
{
    public UnitStats baseStats;
    public int deploymentCost;
    public float redeployTime;

    // UnitStats의 프로퍼티들
    public float Health
    {
        get => baseStats.health;
        set => baseStats.health = value;
    }

    public float Defense
    {
        get => baseStats.defense;
        set => baseStats.defense = value;
    }

    public float MagicResistance
    {
        get => baseStats.magicResistance;
        set => baseStats.magicResistance = value;
    }
}