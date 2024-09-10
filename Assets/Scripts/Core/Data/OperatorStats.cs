[System.Serializable]
public struct OperatorStats
{
    // DeployableUnitStats ����
    public float health;
    public float defense;
    public float magicResistance;
    public int deploymentCost;
    public float redeployTime;

    // OperatorStats���� �߰�
    public float attackPower;
    public float attackSpeed;
    public int maxBlockableEnemies;

    public float currentSP;
    public float SpRecoveryRate; // ���۷����͸��� �ٸ�
}