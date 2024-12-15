using UnityEngine;

/// <summary>
/// �������� ���� �����Ǵ� ��ġ ������ ��ҵ��� ������. �ٸ����̵� ���� �ִ�.
/// </summary>
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
            redeployTime = deployableData.stats.RedeployTime,
            deployableUnitData = deployableData
        };  
    }
}
