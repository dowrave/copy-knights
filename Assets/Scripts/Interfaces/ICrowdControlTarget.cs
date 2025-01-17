
using UnityEngine;

public interface ICrowdControlTarget
{
    Vector3 Position { get; }

    float MovementSpeed { get; }

    void SetMovementSpeed(float newMovementSpeed);
}
