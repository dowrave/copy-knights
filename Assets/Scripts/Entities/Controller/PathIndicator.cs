using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using System.Linq;

public class PathIndicator : MonoBehaviour, IMovable
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float height = 0.3f; // 월드 좌표 기준 +y 위치

    // 경로 관련
    protected PathController _path;
    protected MovementController _movement;

    public PathController Path => _path;
    public MovementController Movement => _movement;

    public float MovementSpeed => speed;
    public Vector3 CurrentDestination => _path.CurrentDestination;
    public Vector3 FinalDestination => _path.FinalDestination;
    public IReadOnlyList<PathNode> CurrentPathNodes => _path.CurrentPathNodes;
    public IReadOnlyList<Vector3> CurrentPathPositions => _path.CurrentPathPositions;
    public int CurrentPathIndex => _path.CurrentPathIndex;
    public Barricade? TargetBarricade => _path.TargetBarricade;

    public bool IsWaiting => _movement.IsWaiting;

// ---

    private void Awake()
    {
        _movement = new MovementController(this);
    }

    public void Initialize(PathData pathData)
    {
        if (pathData == null) Logger.LogError("pathData가 전달되지 않음");

        Logger.LogFieldStatus(pathData.Nodes.Count);

        // pathData를 받아서 초기화해야 하므로 Awake가 아닌 여기에 구현
        _path = new PathController(this, pathData.Nodes);

        // _path.OnPathUpdated += HandlePathUpdated;
        _path.OnDisabled += ReturnToPool;
        _path.Initialize();
        SetupInitialPosition();
    }


    // protected void HandlePathUpdated(IReadOnlyList<PathNode> newPathNodes, IReadOnlyList<Vector3> newPathPositions)
    // {
    //     // new List<>()는 리스트가 메모리에 계속 할당되어 GC 부하가 발생하므로 자주 실행되는 메서드는 이 방식이 더 좋다
    //     currentPathNodes.Clear();
    //     currentPathNodes.AddRange(newPathNodes);

    //     currentPathPositions.Clear();
    //     currentPathPositions.AddRange(newPathPositions);

    //     // 인덱스 할당
    //     // CurrentPathIndex = 0;
    //     CurrentPathIndex = currentPathNodes.Count > 1 ? 1 : 0; // [테스트] 뒤로 가는 현상을 방지하기 위해 1로 놔 봄
    //     currentDestination = currentPathPositions[CurrentPathIndex];
    // }

    protected void SetupInitialPosition()
    {
        if (CurrentPathPositions != null && CurrentPathPositions.Count > 0)
        {
            transform.position = CurrentPathPositions[0];
        }
    }

    private void Update()
    {
        if (StageManager.Instance!.CurrentGameState == GameState.Battle)
        {
            // MoveAlongPath();
            _movement.OnUpdate();
        }
    }

    public void RotateModel() {}

    // private void MoveAlongPath()
    // {
    //     // 대기 중일 때는 이동하지 않음
    //     if (isWaiting)
    //     {
    //         return;
    //     }

    //     if (CheckIfReachedDestination())
    //     {
    //         ReachDestination();
    //         return;
    //     }

    //     Move(CurrentDestination);

    //     if (Vector3.Distance(transform.position, CurrentDestination) < 0.05f)
    //     {
    //         if (Vector3.Distance(transform.position, _path.FinalDestination) < 0.05f)
    //         {
    //             ReachDestination();
    //         }
    //         else if (CurrentPathNodes[CurrentPathIndex] != null && CurrentPathNodes[CurrentPathIndex].waitTime > 0)
    //         {
    //             StartCoroutine(WaitAtNode(CurrentPathNodes[CurrentPathIndex].waitTime));
    //         }
    //         else
    //         {
    //             UpdateNextNode();
    //         }
    //     }
    // }

    // 마지막 타일의 월드 좌표 기준
    // private bool CheckIfReachedDestination()
    // {
    //     if (currentPathNodes.Count == 0) return false;

    //     Vector2Int lastNodeGridPos = currentPathNodes[currentPathNodes.Count - 1].gridPosition;
    //     Vector3 lastNodePosition = MapManager.Instance!.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * height;

    //     return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    // }

    // private void ReachDestination()
    // {
    //     ReturnToPool();
    // }

    public void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnToPool(ObjectPoolManager.PathIndicatorTag, gameObject);
    }

    // public void Move(Vector3 destination)
    // {
    //     transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
    // }

    // private void UpdateNextNode()
    // {
    //     CurrentPathIndex++;

    //     if (CurrentPathIndex < currentPathPositions.Count)
    //     {
    //         currentDestination = currentPathPositions[CurrentPathIndex];
    //     }
    // }


    public void UpdateNextNode() => _path.UpdateNextNode();
    public void SetIsWaiting(bool isWaiting) => _movement.SetIsWaiting(isWaiting);
    public IEnumerator WaitAtNode(float waitTime)
    {
        SetIsWaiting(true);
        yield return new WaitForSeconds(waitTime);
        SetIsWaiting(false);
        UpdateNextNode();
    }
    public void OnReachDestination() => ReturnToPool();

    private void OnDiable()
    {
        if (_path != null)
        {
            // _path.OnPathUpdated -= HandlePathUpdated;
            _path.OnDisabled -= ReturnToPool;
            _path.Cleanup();
        }
    }
}
