using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

// Enemy와 PathIndicator가 공통으로 사용할 "길찾기 두뇌"
// 길을 찾는 로직만 수행함(길을 따라가는 건 owner에 구현)
public class PathNavigator
{
    // 경로가 변경되었을 때 알림을 줄 이벤트
    // public event Action<List<Vector3>> OnPathUpdated;
    public event Action<List<PathNode>> OnPathUpdated;

    private MonoBehaviour owner; // 코루틴 실행을 위한 주체
    private Vector3 currentStartPosition;
    private Vector3 finalDestination;
    
    private List<PathNode> pathNodes;
    private Barricade targetBarricade;
    private List<Vector3> currentPath = new List<Vector3>();
    private int nextNodeIndex = 0;

    // 생성자에서 초기화
    public PathNavigator(MonoBehaviour owner, List<PathNode> pathNodes, Vector3 destination)
    {
        this.owner = owner;
        this.pathNodes = pathNodes;
        this.finalDestination = destination;

        currentPath = pathNodes.Select(node => MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f).ToList();
        
        // 이벤트 구독
        Barricade.OnBarricadeDeployed += OnBarricadePlaced;
        Barricade.OnBarricadeRemoved += OnBarricadeRemovedWithDelay;

        if (IsPathBlocked())
        {
            UpdatePath(owner.transform.position);
        }
    }

    // 메모리 누수 방지를 위한 정리
    public void Cleanup()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        Barricade.OnBarricadeRemoved -= OnBarricadeRemovedWithDelay;
    }

    // 외부(Enemy/Indicator)에서 현재 위치를 갱신하며 경로 요청
    public void UpdatePath(Vector3 startPos)
    {
        this.currentStartPosition = startPos;
        FindPathToDestinationOrBarricade();
    }

    // --- 기존 Enemy 로직 이식 ---
    protected void OnBarricadePlaced(Barricade barricade)
    {
        // 내 타일과 같은 타일에 바리케이드가 배치된 경우
        if (barricade.CurrentTile != null &&
            barricade.CurrentTile.EnemiesOnTile.Contains(owner))
        {
            targetBarricade = barricade;
        }

        // 현재 사용 중인 경로가 막힌 경우
        else if (IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }
    }

    private void OnBarricadeRemovedWithDelay(Barricade barricade)
    {
        if(owner != null && owner.gameObject.activeInHierarchy)
            owner.StartCoroutine(OnBarricadeRemoved(barricade));
    }

    private IEnumerator OnBarricadeRemoved(Barricade barricade)
    {
        yield return new WaitForSeconds(0.1f);

        if (targetBarricade == null || targetBarricade == barricade)
        {
            targetBarricade = null;
            FindPathToDestinationOrBarricade();
        }
    }

    public bool IsPathBlocked()
    {
        if (currentPath == null || currentPath.Count == 0) throw new InvalidOperationException("currentPath가 비어 있음");

        for (int i = nextNodeIndex; i <= currentPath.Count - 1; i++)
        {
            // 경로가 막힌 상황 : 기존 경로 데이터들을 정리한다
            if ((i == nextNodeIndex && PathfindingManager.Instance!.IsPathSegmentValid(owner.transform.position, currentPath[i]) == false) ||
                PathfindingManager.Instance!.IsPathSegmentValid(currentPath[i], currentPath[i + 1]) == false)
            {
                // pathData = null;
                currentPath.Clear();
                return true;
            }
        }

        return false;
    }

    public void FindPathToDestinationOrBarricade()
    {
        // 1. 목적지로 가본다
        if (!CalculateAndSetPath(currentStartPosition, finalDestination))
        {
            // 2. 안되면 바리케이드 찾는다
            SetBarricadePath();
        }
    }

    private void SetBarricadePath()
    {
        targetBarricade = PathfindingManager.Instance.GetNearestBarricade(currentStartPosition);
        if (targetBarricade != null)
        {
            CalculateAndSetPath(currentStartPosition, targetBarricade.transform.position);
        }
    }

    private bool CalculateAndSetPath(Vector3 start, Vector3 end)
    {
        var nodes = PathfindingManager.Instance.FindPathAsNodes(start, end);
        if (nodes == null || nodes.Count == 0) return false;

        SetNewPath(nodes);
        Logger.Log("새로운 경로가 계산됨");

        foreach (var node in nodes)
        {
            Logger.LogFieldStatus(node);
        }

        // 노드를 월드 좌표로 변환
        // currentPath = nodes.Select(node => 
        //     MapManager.Instance.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f
        // ).ToList();

        // 경로 갱신 알림
        // OnPathUpdated?.Invoke(currentPath);

        return true;
    }

    protected void SetNewPath(List<PathNode> newPathNodes)
    {
        if (newPathNodes != null && newPathNodes.Count > 0)
        {
            pathNodes = newPathNodes;
            OnPathUpdated?.Invoke(newPathNodes);
            // currentPath = newPathNodes.Select(node => MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f).ToList();

            // if (owner is Enemy enemy)
            // {
            //     OnPathUpdated?.Invoke(currentPath);
            //     enemy.SetPathNodes(pathNodes);
            //     enemy.SetCurrentPath(currentPath);
            // }
            // else if (owner is PathIndicator indicator)
            // {
            //     indicator.SetPathNodes(pathNodes);
            //     indicator.SetCurrentPath(currentPath);
            // }

            // nextNodeIndex = 0;
        }
    }

    public void UpdateNextNodeIndex(int newIndex)
    {
        nextNodeIndex = newIndex;
    }
}