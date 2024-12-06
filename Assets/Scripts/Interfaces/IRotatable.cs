using UnityEngine;

// 방향성을 가진 엔티티를 위한 인터페이스
public interface IRotatable
{
    Vector3 FacingDirection { get; }
    void SetDirection(Vector3 direction);
}