using UnityEngine;
using System;

public interface IDeployable
{
    bool IsDeployed { get; }

    void Deploy(Vector3 position, Vector3? facingDirection);
    // void Retreat();

    static event Action<DeployableUnitEntity> OnDeployed;
    static event Action<DeployableUnitEntity> OnUndeployed;
    static event Action<DeployableUnitEntity> OnDeployableSelected;
    static event Action<DeployableUnitEntity> OnRetreat;
}
