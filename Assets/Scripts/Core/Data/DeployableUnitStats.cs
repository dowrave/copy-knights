[System.Serializable]
public class DeployableUnitStats: UnitStats
{
    public int deploymentCost;
    public float redeployTime; 

    public DeployableUnitStats(float health, float defense, float magicResistance, int deploymentCost, float redeployTime): base(health, defense, magicResistance)
    {
        this.deploymentCost = deploymentCost;
        this.redeployTime = redeployTime;
    }
}