[System.Serializable]
public class OperatorStats : DeployableUnitStats
{
    public float attackPower;
    public float attackSpeed;
    public int maxBlockableEnemies;

    public float currentSP;
    public float spRecoveryRate;

    // 기본 생성자
    public OperatorStats(float health, float defense, float magicResistance, int deploymentCost, float redeployTime, float attackPower, float attackSpeed, int maxBlockableEnemies, float currentSP, float spRecoveryRate)
        : base(health, defense, magicResistance, deploymentCost, redeployTime)
    {
        this.attackPower = attackPower;
        this.attackSpeed = attackSpeed;
        this.maxBlockableEnemies = maxBlockableEnemies;
        this.currentSP = currentSP;
        this.spRecoveryRate = spRecoveryRate;
    }
}