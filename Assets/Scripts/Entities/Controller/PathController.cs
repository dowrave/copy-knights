using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

// 경로 계산
public class PathController 
{
    // 경로가 변경되었을 때 알림을 줄 이벤트
    // public event Action<IReadOnlyList<PathNode>, IReadOnlyList<Vector3>> OnPathUpdated;
    
    public event Action OnDisabled; // 비활성화될 필요가 있을 때 실행될 이벤트 - 현재는 pathIndicator에서만 사용

    // public event Action OnPathUpdated;

    private MonoBehaviour _owner; // 코루틴 실행을 위한 주체
    private Vector3 _currentDestination; // currentPath[nextNodeIndex]
    private Vector3 _finalDestination; // 최종 목적지
    private List<PathNode> _currentPathNodes = new List<PathNode>(); // 경로 노드
    private List<Vector3> _currentPathPositions = new List<Vector3>(); // 경로 노드의 실제 위치
    private int _currentPathIndex;
    private Barricade? _targetBarricade; 

    public Vector3 CurrentDestination => _currentDestination;
    public Vector3 FinalDestination => _finalDestination;
    public IReadOnlyList<PathNode> CurrentPathNodes => _currentPathNodes;
    public IReadOnlyList<Vector3> CurrentPathPositions => _currentPathPositions;
    public int CurrentPathIndex => _currentPathIndex;
    public Barricade? TargetBarricade => _targetBarricade;

    public bool IsInitialized { get; private set;} = false;

    // 생성자에서 초기화
    public PathController(MonoBehaviour owner)
    {
        _owner = owner;

        // 이벤트 구독
        Barricade.OnBarricadeDeployed += OnBarricadePlaced;
        Barricade.OnBarricadeRemoved += OnBarricadeRemovedWithDelay;
    }

    // OnPathUpdate를 구독하는 부모 클래스의 메서드가 있기 때문에
    // 생성자와 실제 실행 메서드는 별도로 구분하는 게 좋다.
    public void Initialize(IReadOnlyList<PathNode> initialPathNodes)
    {
        SetNewPath(initialPathNodes);

        _finalDestination = _currentPathPositions[_currentPathPositions.Count - 1]; // 최초 경로의 마지막 값

        // 최초 경로가 막혔을 때
        if (IsPathBlocked(0))
        {
            UpdatePath();
        }

        IsInitialized = true;
    }

    // 외부(Enemy/Indicator)에서 현재 위치를 갱신하며 경로 요청
    public void UpdatePath()
    {
        FindPathToDestinationOrBarricade();
    }

    // --- 기존 Enemy 로직 이식 ---
    protected void OnBarricadePlaced(Barricade barricade)
    {
        // 내 타일과 같은 타일에 바리케이드가 배치된 경우
        if (barricade.CurrentTile != null &&
            barricade.CurrentTile.EnemiesOnTile.Contains(_owner))
        {
            SetTargetBarricade(barricade);
        }

        // 현재 사용 중인 경로가 막힌 경우
        else if (IsPathBlocked(_currentPathIndex))
        {
            FindPathToDestinationOrBarricade();
        }
    }

    private void OnBarricadeRemovedWithDelay(Barricade barricade)
    {
        if(_owner != null && _owner.gameObject.activeInHierarchy)
        {
            _owner.StartCoroutine(OnBarricadeRemoved(barricade));
        }
    }

    private IEnumerator OnBarricadeRemoved(Barricade barricade)
    {
        yield return new WaitForSeconds(0.1f);

        if (_targetBarricade == barricade)
        {
            SetTargetBarricade(null);
            FindPathToDestinationOrBarricade(); 
        }

        // targetBarricade로 설정된 이상 해당 객체가 제거되지 않았다면 할당이 해제되지 않음
        // 즉 다른 바리케이드가 제거되어 목적지로 향하는 경로가 열렸더라도, 현재 설정된 타겟 바리케이드로 향한다는 의미
    }

    public bool IsPathBlocked(int currentNodeIndex)
    {
        if (_currentPathPositions == null || _currentPathPositions.Count == 0) throw new InvalidOperationException("currentPathPositions가 비어 있음");
        if (_owner == null) return false;

        for (int i = currentNodeIndex; i < _currentPathPositions.Count; i++)
        {
            // 1. 현재 위치에서 다음 노드 체크 (첫 번째 루프에서만)
            if (i == currentNodeIndex)
            {
                if (!PathfindingManager.Instance!.IsPathSegmentValid(_owner.transform.position, _currentPathPositions[i]))
                {
                    // Logger.Log($"IsPathBlocked : 현재 위치 {owner.transform.position} ~ 현재 목표 노드 {currentPathPositions[i]}까지의 경로가 막혀있다");
                    return true;
                }
            }

            // 2. 노드와 노드 사이 체크 (마지막 노드가 아닐 때만)
            if (i < _currentPathPositions.Count - 1)
            {
                if (!PathfindingManager.Instance!.IsPathSegmentValid(_currentPathPositions[i], _currentPathPositions[i + 1]))
                {
                    // Logger.Log($"IsPathBlocked : {currentPathPositions[i]} ~ {currentPathPositions[i+1]} 사이의 경로가 막혀있다");
                    return true;
                }
            }
        }
        
        return false;
    }

    public void FindPathToDestinationOrBarricade()
    {
        Logger.Log("경로 수정됨");

        // 최종 목적지까지의 막히지 않은경로를 계산함
        Vector3 currentPosition = _owner.transform.position;
        List<PathNode> newPathNodes = CalculatePath(currentPosition, _finalDestination);

        // 막히지 않은 경로가 있다면 해당 경로로 처리
        if (newPathNodes != null)
        {
            SetNewPath(newPathNodes);
            return;
        }

        // 경로가 막혔을 경우 
        // 1. owner가 enemy일 떄의 처리
        if (_owner is Enemy enemy)
        {
            Logger.Log("경로 재계산 - 모든 길이 막혀 있어 가장 가까운 바리케이드를 목표로 설정");

            // 목적지까지의 경로가 막혔다면 가장 가까운 바리케이드까지의 경로를 계산한 후 그 바리케이드에서 목적지까지의 경로를 계산한다
            Barricade nearestBarricade = PathfindingManager.Instance.GetNearestBarricade(currentPosition);
            if (nearestBarricade != null)
            {
                List<PathNode> toBarricadePath = CalculatePath(currentPosition, nearestBarricade.transform.position);
                SetTargetBarricade(nearestBarricade); // targetBarricade 설정됨
                SetNewPath(toBarricadePath);
            }
        }
        // 2. 이외의 owner는 즉시 비활성화
        else
        {
            OnDisabled?.Invoke();
        }
    }

    private List<PathNode> CalculatePath(Vector3 start, Vector3 end)
    {
        List<PathNode> nodes = PathfindingManager.Instance.FindPathAsNodes(start, end);

        if (nodes == null || nodes.Count == 0)
        {
            Logger.Log($"경로를 계산했으나 시작점{start} 에서 목적지{end}까지 도달할 경로가 없음");
            return null;
        } 

        return nodes;
    }

    protected void SetNewPath(IReadOnlyList<PathNode> newPathNodes)
    {
        // Logger.Log("SetNewPath 동작");
        // foreach (var pathNode in newPathNodes)
        // {
        //     Logger.LogFieldStatus(pathNode);
        // }

        if (newPathNodes != null && newPathNodes.Count > 0)
        {
            // ========= _currentPathNodes 수정 =========
            _currentPathNodes.Clear();
            _currentPathNodes.AddRange(newPathNodes);

            //=========  _currentPathPositions 수정 ============

            // 현재 경로(실제 Vector3 위치) 수정
            float floatY = _owner is Enemy enemy ? enemy.BaseData.DefaultYPosition : 0.3f; // Enemy마다 y 위치가 다름

            _currentPathPositions.Clear();

            // Capacity 미리 확보 : 요소가 Capacity를 초과할 때 List는 배열을 2배로 재할당함
            // 미리 맞춰놓으면 재할당 없이 한꺼번에 추가할 수 있음
            if (_currentPathPositions.Capacity < _currentPathNodes.Count)
            {
                _currentPathPositions.Capacity = _currentPathNodes.Count; 
            }

            // 반복문으로 요소를 _currentPathPositions으로 옮김
            for (int i = 0; i < _currentPathNodes.Count; i++)
            {
                Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(_currentPathNodes[i].gridPosition);
                _currentPathPositions.Add(worldPos + Vector3.up * floatY);
            }

            // ============= 인덱스 및 목적지 수정 ==============
            _currentPathIndex = _currentPathNodes.Count > 1 ? 1 : 0; // [테스트] 뒤로 가는 현상을 방지하기 위해 1로 놔 봄
            _currentDestination = _currentPathPositions[_currentPathIndex];

            return;
        }
    }

    public void SetTargetBarricade(Barricade? barricade)
    {
        if (_owner is Enemy enemy)
        {
            _targetBarricade = barricade;
            enemy.SetCurrentBarricade(barricade); 
        }
    }

    // 현재 경로상에서 목적지까지 남은 거리 계산
    public float GetRemainingPathDistance()
    {
        if (_currentPathPositions.Count == 0 || _currentPathIndex > _currentPathPositions.Count - 1)
        {
            return float.MaxValue;
        }

        float distance = 0f;
        for (int i = _currentPathIndex; i < _currentPathPositions.Count - 1; i++)
        {
            // 첫 타일에 한해서만 현재 위치를 기반으로 계산(여러 Enemy가 같은 타일에 있을 수 있기 때문)
            if (i == _currentPathIndex)
            {
                Vector3 nowPosition = new Vector3(_owner.transform.position.x, _owner.transform.position.y, _owner.transform.position.z);
                distance += Vector3.Distance(nowPosition, _currentPathPositions[i + 1]);
            }

            distance += Vector3.Distance(_currentPathPositions[i], _currentPathPositions[i + 1]);
        }

        return distance;
    }    

    protected void HandlePathUpdated(IReadOnlyList<PathNode> newPathNodes, IReadOnlyList<Vector3> newPathPositions)
    {
        // new List<>()는 리스트가 메모리에 계속 할당되어 GC 부하가 발생하므로 자주 실행되는 메서드는 이 방식이 더 좋다
        _currentPathNodes.Clear();
        _currentPathNodes.AddRange(newPathNodes);

        _currentPathPositions.Clear();
        _currentPathPositions.AddRange(newPathPositions);

        // 인덱스 할당
        // CurrentPathIndex = 0; 
        _currentPathIndex = _currentPathNodes.Count > 1 ? 1 : 0; // [테스트] 뒤로 가는 현상을 방지하기 위해 1로 놔 봄
        _currentDestination = _currentPathPositions[CurrentPathIndex];
    }

    // 노드 인덱스 & 현재 목적지 다음으로 업데이트
    public void UpdateNextNode()
    {
        // pathData 관련 데이터 항목이 없거나, 도달할 노드가 마지막 노드인 경우는 실행되지 않음
        if (_currentPathPositions == null || CurrentPathIndex >= _currentPathPositions.Count - 1)
        {
            Logger.LogError("오류 발생");
            return;
        }

        _currentPathIndex++;

        if (CurrentPathIndex < _currentPathPositions.Count)
        {
            _currentDestination = _currentPathPositions[CurrentPathIndex];
        }
    }

    // 메모리 누수 방지를 위한 정리
    public void Cleanup()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        Barricade.OnBarricadeRemoved -= OnBarricadeRemovedWithDelay;
    }
}