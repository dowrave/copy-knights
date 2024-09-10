[System.Serializable]
public struct OperatorStats
{
    // DeployableUnitStats 복붙
    public float health;
    public float defense;
    public float magicResistance;
    public int deploymentCost;
    public float redeployTime;

    // OperatorStats에서 추가
    public float attackPower;
    public float attackSpeed;
    public int maxBlockableEnemies;

    public float currentSP;
    public float SpRecoveryRate; // 오퍼레이터마다 다름
}