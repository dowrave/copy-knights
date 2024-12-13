using UnityEngine;

[System.Serializable]
public class MapDeployableData
{
    public GameObject deployablePrefab; 
    public DeployableUnitData deployableData; // 기본 데이터. 별도의 초기화 로직을 거치지 않게 하기 위함. 
    public int maxDeployCount; // 최대 배치 가능 수 

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
