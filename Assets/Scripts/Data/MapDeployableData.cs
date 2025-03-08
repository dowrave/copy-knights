using UnityEngine;

/// <summary>
/// 스테이지 별로 제공되는 배치 가능한 요소들의 데이터. 바리케이드 등이 있다.
/// </summary>
[System.Serializable]
public class MapDeployableData
{
    public GameObject deployablePrefab = default!;
    public DeployableUnitData deployableData = default!; // 기본 데이터. 별도의 초기화 로직을 거치지 않게 하기 위함. 
    public int maxDeployCount = 0; // 최대 배치 가능 수 

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
