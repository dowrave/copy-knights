
using UnityEngine;

public interface ICrowdControlTarget
{
    public Vector3 Position { get; }

    public float MovementSpeed { get; }

    public void SetMovementSpeed(float newMovementSpeed);
}
