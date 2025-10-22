using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DeployableInfo
{
    public GameObject prefab = default!;
    public string poolTag;
    public int maxDeployCount = 0;
    public float redeployTime = 0f;

    // 오퍼레이터일 때 할당
    public Operator? deployedOperator;
    public OwnedOperator? ownedOperator;
    public OperatorData? operatorData;
    public int? skillIndex;

    // 일반 배치 가능한 유닛일 때 할당
    public DeployableUnitEntity deployedDeployable;
    // public List<DeployableUnitEntity> deployedDeployables = new List<DeployableUnitEntity>();
    public DeployableUnitData? deployableUnitData;
}