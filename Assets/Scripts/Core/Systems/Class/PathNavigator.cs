using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

// Enemy와 PathIndicator가 공통으로 사용할 "길찾기 두뇌"
// 필요 시 경로를 계산해서 상위 클래스에 넘겨주는 역할
public class PathNavigator 
{
    // 경로가 변경되었을 때 알림을 줄 이벤트
    public event Action<IReadOnlyList<PathNode>, IReadOnlyList<Vector3>> OnPathUpdated;
    
    public event Action OnDisabled; // 비활성화될 필요가 있을 때 실행될 이벤트 - 현재는 pathIndicator에서만 사용

    // public event Action OnPathUpdated;

    private MonoBehaviour owner; // 코루틴 실행을 위한 주체
    // private Vector3 currentDestination; // currentPath[nextNodeIndex]
    private Vector3 _finalDestination; // 최종 목적지
    private List<PathNode> currentPathNodes; // 경로 노드
    private List<Vector3> currentPathPositions; // 경로 노드의 실제 위치
    private int currentPathIndex;
    private Barricade? targetBarricade; 

    public Vector3 FinalDestination => _finalDestination;

    // 생성자에서 초기화
    public PathNavigator(MonoBehaviour owner, List<PathNode> initialPathNodes)
    {
        this.owner = owner;
        currentPathNodes = initialPathNodes;

        // 이벤트 구독
        Barricade.OnBarricadeDeployed += OnBarricadePlaced;
        Barricade.OnBarricadeRemoved += OnBarricadeRemovedWithDelay;
    }

    // OnPathUpdate를 구독하는 부모 클래스의 메서드가 있기 때문에
    // 생성자와 실제 실행 메서드는 별도로 구분하는 게 좋다.
    public void Initialize()
    {
        SetNewPath(currentPathNodes);
        _finalDestination = currentPathPositions[currentPathPositions.Count - 1]; // 최초 경로의 마지막 값

        // 최초 경로가 막혔을 때
        if (IsPathBlocked(0))
        {
            // Logger.Log($"{owner.gameObject.name} 초기화, 기본 경로가 막혀 있어서 경로 변경");
            UpdatePath();
        }
    }

    // 메모리 누수 방지를 위한 정리
    public void Cleanup()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        Barricade.OnBarricadeRemoved -= OnBarricadeRemovedWithDelay;
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
            barricade.CurrentTile.EnemiesOnTile.Contains(owner))
        {
            SetTargetBarricade(barricade);
        }

        // 현재 사용 중인 경로가 막힌 경우
        else if (IsPathBlocked(currentPathIndex))
        {
            FindPathToDestinationOrBarricade();
        }
    }

    private void OnBarricadeRemovedWithDelay(Barricade barricade)
    {
        if(owner != null && owner.gameObject.activeInHierarchy)
        {
            owner.StartCoroutine(OnBarricadeRemoved(barricade));
        }
    }

    private IEnumerator OnBarricadeRemoved(Barricade barricade)
    {
        yield return new WaitForSeconds(0.1f);

        if (targetBarricade == barricade)
        {
            SetTargetBarricade(null);
            FindPathToDestinationOrBarricade(); 
        }

        // targetBarricade로 설정된 이상 해당 객체가 제거되지 않았다면 할당이 해제되지 않음
        // 즉 다른 바리케이드가 제거되어 목적지로 향하는 경로가 열렸더라도, 현재 설정된 타겟 바리케이드로 향한다는 의미
    }

    public bool IsPathBlocked(int currentNodeIndex)
    {
        if (currentPathPositions == null || currentPathPositions.Count == 0) throw new InvalidOperationException("currentPathPositions가 비어 있음");
        if (owner == null) return false;

        for (int i = currentNodeIndex; i < currentPathPositions.Count; i++)
        {
            // 1. 현재 위치에서 다음 노드 체크 (첫 번째 루프에서만)
            if (i == currentNodeIndex)
            {
                if (!PathfindingManager.Instance!.IsPathSegmentValid(owner.transform.position, currentPathPositions[i]))
                {
                    // Logger.Log($"IsPathBlocked : 현재 위치 {owner.transform.position} ~ 현재 목표 노드 {currentPathPositions[i]}까지의 경로가 막혀있다");
                    return true;
                }
            }

            // 2. 노드와 노드 사이 체크 (마지막 노드가 아닐 때만)
            if (i < currentPathPositions.Count - 1)
            {
                if (!PathfindingManager.Instance!.IsPathSegmentValid(currentPathPositions[i], currentPathPositions[i + 1]))
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
        // 최종 목적지까지의 막히지 않은경로를 계산함
        Vector3 currentPosition = owner.transform.position;
        List<PathNode> newPathNodes = CalculatePath(currentPosition, _finalDestination);

        // 막히지 않은 경로가 있다면 해당 경로로 처리
        if (newPathNodes != null)
        {
            SetNewPath(newPathNodes);
            return;
        }

        // 경로가 막혔을 경우 
        // 1. owner가 enemy일 떄의 처리
        if (owner is Enemy enemy)
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

    protected void SetNewPath(List<PathNode> newPathNodes)
    {
        // Logger.Log("SetNewPath 동작");

        if (newPathNodes != null && newPathNodes.Count > 0)
        {
            currentPathNodes = newPathNodes;

            // Enemy의 경우 부유된 y값이 다를 수 있어서 설정
            float floatY = owner is Enemy enemy ? enemy.BaseData.DefaultYPosition : 0.5f; 
            currentPathPositions = currentPathNodes.Select(node => MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * floatY).ToList();

            // foreach (var pathPosition in currentPathPositions)
            // {
            //     Logger.LogFieldStatus(pathPosition);
            // }

            OnPathUpdated?.Invoke(currentPathNodes, currentPathPositions);
        }
    }

    public void SetTargetBarricade(Barricade? barricade)
    {
        if (owner is Enemy enemy)
        {
            targetBarricade = barricade;
            enemy.SetCurrentBarricade(barricade); 
        }
    }

    public void SetCurrentPathIndex(int newIndex)
    {
        currentPathIndex = newIndex;
    }

    // 현재 경로상에서 목적지까지 남은 거리 계산
    public float GetRemainingPathDistance(int currentPathIndex)
    {
        if (currentPathPositions.Count == 0 || currentPathIndex > currentPathPositions.Count - 1)
        {
            return float.MaxValue;
        }

        float distance = 0f;
        for (int i = currentPathIndex; i < currentPathPositions.Count - 1; i++)
        {
            // 첫 타일에 한해서만 현재 위치를 기반으로 계산(여러 Enemy가 같은 타일에 있을 수 있기 때문)
            if (i == currentPathIndex)
            {
                Vector3 nowPosition = new Vector3(owner.transform.position.x, owner.transform.position.y, owner.transform.position.z);
                distance += Vector3.Distance(nowPosition, currentPathPositions[i + 1]);
            }

            distance += Vector3.Distance(currentPathPositions[i], currentPathPositions[i + 1]);
        }

        return distance;
    }    
}