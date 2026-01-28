using UnityEngine;
using System;

// 이동 담당
public class MovementController
{
    private IMovable _owner;

    private bool _isWaiting;

    public bool IsWaiting => _isWaiting;
    
    public MovementController(IMovable owner)
    {
        _owner = owner;
    }

    public void OnUpdate()
    {
        if (_owner.CurrentDestination == null) throw new InvalidOperationException("다음 노드가 설정되어있지 않음");
        if (_owner.FinalDestination == null) throw new InvalidOperationException("navigator나 최종 목적지가 설정되지 않음");

        // pathController가 초기화된 후에 동작해야 함
        if (!_owner.Path.IsInitialized) return;

        // 이동 경로 중 도달해야 할 곳이 없다면 false 반환 (갈 곳이 없으니 이동이 불가능)
        if (_owner.CurrentPathIndex >= _owner.CurrentPathPositions.Count) return;

        if (CheckIfReachedDestination())
        {
            _owner.OnReachDestination();
            return;
        }

        Move(_owner.CurrentDestination);
        _owner.RotateModel();

        // 노드 도달 확인
        if (Vector3.Distance(_owner.transform.position, _owner.CurrentDestination) < 0.05f)
        {
            // 목적지 도달
            if (Vector3.Distance(_owner.transform.position, _owner.FinalDestination) < 0.05f)
            {
                _owner.OnReachDestination();
            }
            // 기다려야 하는 경우
            else if (_owner.CurrentPathNodes[_owner.CurrentPathIndex].waitTime > 0)
            {
                _owner.StartCoroutine(_owner.WaitAtNode(_owner.CurrentPathNodes[_owner.CurrentPathIndex].waitTime));
            }
            // 노드 업데이트
            else
            {
                _owner.UpdateNextNode();
            }
        }
    }

    

    public void Move(Vector3 destination)
    {
        _owner.transform.position = Vector3.MoveTowards(_owner.transform.position, destination, _owner.MovementSpeed * Time.deltaTime);
    }

    protected bool CheckIfReachedDestination()
    {
        if (_owner.CurrentPathPositions == null) throw new InvalidOperationException("currentPathPositions가 할당되지 않음");

        if (_owner.CurrentPathPositions.Count == 0) return false;

        Vector3 lastPathPosition = _owner.CurrentPathPositions[_owner.CurrentPathPositions.Count - 1];

        return Vector3.Distance(_owner.transform.position, lastPathPosition) < 0.05f;
    }

    public void SetIsWaiting(bool isWaiting) => _isWaiting = isWaiting;
}