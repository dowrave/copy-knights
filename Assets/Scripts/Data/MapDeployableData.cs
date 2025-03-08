using UnityEngine;

/// <summary>
/// �������� ���� �����Ǵ� ��ġ ������ ��ҵ��� ������. �ٸ����̵� ���� �ִ�.
/// </summary>
[System.Serializable]
public class MapDeployableData
{
    public GameObject deployablePrefab = default!;
    public DeployableUnitData deployableData = default!; // �⺻ ������. ������ �ʱ�ȭ ������ ��ġ�� �ʰ� �ϱ� ����. 
    public int maxDeployCount = 0; // �ִ� ��ġ ���� �� 

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
