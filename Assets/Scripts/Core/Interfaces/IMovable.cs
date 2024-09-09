
using UnityEngine;

// 이동 가능한 엔티티를 위한 인터페이스
public interface IMovable
{
    void Move(Vector3 destination);
    float MovementSpeed { get; }
}
