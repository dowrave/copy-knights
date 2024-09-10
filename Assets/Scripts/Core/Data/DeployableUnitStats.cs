[System.Serializable]
public struct DeployableUnitStats
{
    // UnitStats 복붙
    public float health;
    public float defense;
    public float magicResistance;

    // DeployableUnitStats에서 추가
    public int deploymentCost;
    public float redeployTime;
}