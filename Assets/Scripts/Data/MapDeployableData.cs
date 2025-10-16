using UnityEngine;

/// <summary>
/// �������� ���� �����Ǵ� ��ġ ������ ��ҵ��� ������. �ٸ����̵� ���� �ִ�.
/// </summary>
[System.Serializable]
public class MapDeployableData
{
    protected GameObject deployablePrefab = default!;
    protected DeployableUnitData deployableData = default!; // �⺻ ������. ������ �ʱ�ȭ ������ ��ġ�� �ʰ� �ϱ� ����. 
    protected int maxDeployCount = 0; // �ִ� ��ġ ���� �� 

    public GameObject DeployablePrefab => deployablePrefab;
    public DeployableUnitData DeployableData => deployableData;
    public int MaxDeployCount => maxDeployCount;

    public DeployableManager.DeployableInfo ToDeployableInfo()
    {
        return new DeployableManager.DeployableInfo
        {
            prefab = deployablePrefab,
            maxDeployCount = maxDeployCount,
            redeployTime = deployableData.stats.RedeployTime,
            deployableUnitData = deployableData,
            poolTag = deployableData.GetUnitTag()
        };
    }
}
