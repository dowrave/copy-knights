using UnityEngine;

public interface IDeployable
{
    bool IsDeployed { get; }
    int DeploymentCost { get; }

    void Deploy(Vector3 position);
    void Retreat();

}
