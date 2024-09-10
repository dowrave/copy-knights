using UnityEngine;


public class DeployableUnitData : UnitData
{
    public new DeployableUnitStats stats;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;
    public Sprite icon; // UI 표시 아이콘
    public GameObject prefab; // 오퍼레이터의 프리팹 정보
}
