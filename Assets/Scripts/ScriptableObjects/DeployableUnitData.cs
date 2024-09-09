using UnityEngine;


public class DeployableUnitData : UnitData
{
    public int deploymentCost;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;
    public float redeployTime = 70f; // 재배치 대기 시간
    public Sprite icon; // UI 표시 아이콘
    public GameObject prefab; // 오퍼레이터의 프리팹 정보
}
