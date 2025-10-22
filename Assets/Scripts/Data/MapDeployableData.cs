using UnityEngine;

/// <summary>
/// �������� ���� �����Ǵ� ��ġ ������ ��ҵ��� ������. �ٸ����̵� ���� �ִ�.
/// </summary>
[System.Serializable]
public class MapDeployableData
{
    [SerializeField] protected GameObject deployablePrefab = default!;
    [SerializeField] protected DeployableUnitData deployableData = default!; // �⺻ ������. ������ �ʱ�ȭ ������ ��ġ�� �ʰ� �ϱ� ����. 
    [SerializeField] protected int maxDeployCount = 0; // �ִ� ��ġ ���� �� 

    public GameObject DeployablePrefab => deployablePrefab;
    public DeployableUnitData DeployableData => deployableData;
    public int MaxDeployCount => maxDeployCount;

    public DeployableInfo ToDeployableInfo()
    {
        return new DeployableInfo
        {
            prefab = deployablePrefab,
            maxDeployCount = maxDeployCount,
            redeployTime = deployableData.stats.RedeployTime,
            deployableUnitData = deployableData,
            poolTag = deployableData.GetUnitTag()
        };
    }
}
