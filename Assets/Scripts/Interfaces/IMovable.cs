
using UnityEngine;
using System.Collections.Generic; 
using System.Collections;

// 이동 가능한 엔티티를 위한 인터페이스
public interface IMovable
{
    PathController Path { get; }

    float MovementSpeed { get; }
    Transform transform { get; }
    Vector3 CurrentDestination { get; }
    Vector3 FinalDestination { get; }
    IReadOnlyList<PathNode> CurrentPathNodes { get; }
    IReadOnlyList<Vector3> CurrentPathPositions { get; }
    int CurrentPathIndex { get; }
    Barricade? TargetBarricade { get; }

    // void Move(Vector3 destination);
    void UpdateNextNode();
    IEnumerator WaitAtNode(float waitTime);
    void SetIsWaiting(bool isWaiting);
    void RotateModel();
    Coroutine StartCoroutine(IEnumerator routine);
    void OnReachDestination();
}
