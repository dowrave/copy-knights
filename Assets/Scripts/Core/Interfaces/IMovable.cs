
using UnityEngine;

// �̵� ������ ��ƼƼ�� ���� �������̽�
public interface IMovable
{
    void Move(Vector3 destination);
    float MovementSpeed { get; }
}
