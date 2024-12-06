using UnityEngine;

public interface IDeployable
{
    bool IsDeployed { get; }

    void Deploy(Vector3 position);
    void Retreat();

}
