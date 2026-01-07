using UnityEngine;
using System;

public interface IDeployable
{
    bool IsDeployed { get; }

    void Deploy(Vector3 position);
    // void Retreat();

    static event Action<DeployableUnitEntity> OnDeployed;
    static event Action<DeployableUnitEntity> OnUndeployed;
    static event Action<DeployableUnitEntity> OnDeployableSelected;
    
    event Action<DeployableUnitEntity> OnRetreat;
}
