using UnityEngine;

// ���⼺�� ���� ��ƼƼ�� ���� �������̽�
public interface IRotatable
{
    Vector3 FacingDirection { get; }
    void SetDirection(Vector3 direction);
}