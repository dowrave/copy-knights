using UnityEngine;

/// <summary>
/// 스테이지 별로 제공되는 배치 가능한 요소들의 데이터. 바리케이드 등이 있다.
/// </summary>
[System.Serializable]
public class MapDeployableData
{
    [SerializeField] protected GameObject deployablePrefab = default!;
    [SerializeField] protected DeployableUnitData deployableData = default!; // 기본 데이터. 별도의 초기화 로직을 거치지 않게 하기 위함. 
    [SerializeField] protected int maxDeployCount = 0; // 최대 배치 가능 수 

    public GameObject DeployablePrefab => deployablePrefab;
    public DeployableUnitData DeployableData => deployableData;
    public int MaxDeployCount => maxDeployCount;

    public DeployableInfo ToDeployableInfo()
    {
        return new DeployableInfo
        {
            prefab = deployablePrefab,
            maxDeployCount = maxDeployCount,
            redeployTime = deployableData.Stats.RedeployTime,
            deployableUnitData = deployableData,
            poolTag = deployableData.UnitTag
        };
    }
}
