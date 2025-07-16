
using UnityEngine;

public interface ICrowdControlTarget
{
    float MovementSpeed { get; }
    void SetMovementSpeed(float newMovementSpeed);
}
