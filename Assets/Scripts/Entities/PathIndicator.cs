using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using System.Linq;

public class PathIndicator : MonoBehaviour
{
    private float speed = 10f;
    private float height = 0.3f; // 월드 좌표 기준 +y 위치

    // 경로 관련
    protected PathNavigator navigator;
    protected Barricade? targetBarricade;
    protected List<PathNode> currentPathNodes = new List<PathNode>();
    protected List<Vector3> currentPathPositions = new List<Vector3>();
    protected Vector3 currentDestination; // 현재 향하는 위치
    protected int _currentPathIndex;
    protected bool isWaiting = false; // 단순히 위치에서 기다리는 상태
    protected bool stopAttacking = false; // 인위적으로 넣은 공격 가능 / 불가능 상태

    // 경로 관련 프로퍼티
    public PathNavigator Navigator => navigator;
    public int CurrentPathIndex
    {
        get => _currentPathIndex;
        protected set
        {
            _currentPathIndex = value;
            if (navigator != null)
            {
                navigator.SetCurrentPathIndex(_currentPathIndex);
            }
            else
            {
                Logger.LogWarning($"navigator가 null이라 navigator의 _currentPathIndex가 업데이트되지 않음");
            }
        }
    }

// ---

    public void Initialize(PathData pathData)
    {
        if (pathData == null) Logger.LogError("pathData가 전달되지 않음");

        // Enemy에 작성된 순서를 따라감
        SetupInitialPosition();

        navigator = new PathNavigator(this, pathData.Nodes);
        navigator.OnPathUpdated += HandlePathUpdated;
        navigator.OnDisabled += ReturnToPool;
        navigator.Initialize();

        UpdateNextNode();
    }

    private void OnDiable()
    {
        if (navigator != null)
        {
            navigator.OnPathUpdated -= HandlePathUpdated;
            navigator.OnDisabled -= ReturnToPool;
            navigator.Cleanup();
        }
    }

    protected void HandlePathUpdated(IReadOnlyList<PathNode> newPathNodes, IReadOnlyList<Vector3> newPathPositions)
    {
        // new List<>()는 리스트가 메모리에 계속 할당되어 GC 부하가 발생하므로 자주 실행되는 메서드는 이 방식이 더 좋다
        currentPathNodes.Clear();
        currentPathNodes.AddRange(newPathNodes);

        currentPathPositions.Clear();
        currentPathPositions.AddRange(newPathPositions);

        // 인덱스 할당
        // CurrentPathIndex = 0;
        CurrentPathIndex = currentPathNodes.Count > 1 ? 1 : 0; // [테스트] 뒤로 가는 현상을 방지하기 위해 1로 놔 봄
    }


    // private void InitializeCurrentPath()
    // {
    //     InstanceValidator.ValidateInstance(pathNodes);

    //     foreach (var node in pathNodes)
    //     {
    //         currentPath.Add(MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * height);
    //     }

    //     destinationPosition = currentPath[currentPath.Count - 1]; // 목적지 설정
    // }

    protected void SetupInitialPosition()
    {
        if (currentPathPositions != null && currentPathPositions.Count > 0)
        {
            transform.position = currentPathPositions[0];
        }
    }

    private void Update()
    {
        if (StageManager.Instance!.CurrentGameState == GameState.Battle)
        {
            MoveAlongPath();
        }
    }

    private void MoveAlongPath()
    {
        // 대기 중일 때는 이동하지 않음
        if (isWaiting)
        {
            return;
        }

        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(currentDestination);

        if (Vector3.Distance(transform.position, currentDestination) < 0.05f)
        {
            if (Vector3.Distance(transform.position, navigator.FinalDestination) < 0.05f)
            {
                ReachDestination();
            }
            else if (currentPathNodes[CurrentPathIndex] != null && currentPathNodes[CurrentPathIndex].waitTime > 0)
            {
                StartCoroutine(WaitAtNode(currentPathNodes[CurrentPathIndex].waitTime));
            }
            else
            {
                UpdateNextNode();
            }
        }
    }

    // 마지막 타일의 월드 좌표 기준
    private bool CheckIfReachedDestination()
    {
        if (currentPathNodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = currentPathNodes[currentPathNodes.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance!.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * height;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    }

    private void ReachDestination()
    {
        ReturnToPool();
        // Destroy(gameObject, 0.5f);
    }

    public void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnToPool(ObjectPoolManager.PathIndicatorTag, gameObject);
    }

    public void Move(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
    }

    private void UpdateNextNode()
    {
        CurrentPathIndex++;
        // if (navigator == null) Logger.LogError("navigator가 null임");
        // navigator.UpdateNextNodeIndex(nextNodeIndex);

        if (CurrentPathIndex < currentPathPositions.Count)
        {
            currentDestination = currentPathPositions[CurrentPathIndex];
            // nextNode = pathNodes[nextNodeIndex];
            // nextNodeWorldPosition = MapManager.Instance!.ConvertToWorldPosition(nextNode.gridPosition) +
            //     Vector3.up * height;
        }
    }

    // 대기 중일 때 실행
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }
}
