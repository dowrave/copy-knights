using UnityEngine;

[System.Serializable]
public class MapDeployableData
{
    public GameObject deployablePrefab; 
    public DeployableUnitData deployableData; // �⺻ ������. ������ �ʱ�ȭ ������ ��ġ�� �ʰ� �ϱ� ����. 
    public int maxDeployCount; // �ִ� ��ġ ���� �� 

    public DeployableManager.DeployableInfo ToDeployableInfo()
    {
        return new DeployableManager.DeployableInfo
        {
            prefab = deployablePrefab,
            maxDeployCount = maxDeployCount,
            remainingDeployCount = maxDeployCount,
            redeployTime = deployableData.stats.RedeployTime,
            deployableUnitData = deployableData
        };  
    }
}
