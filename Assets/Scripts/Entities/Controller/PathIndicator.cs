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
        _path = new PathController(this);
    }

    public void Initialize(PathData pathData)
    {
        if (pathData == null) Logger.LogError("pathData가 전달되지 않음");

        Logger.LogFieldStatus(pathData.Nodes.Count);

        // pathData를 받아서 초기화해야 하므로 Awake가 아닌 여기에 구현

        // _path.OnPathUpdated += HandlePathUpdated;
        _path.OnDisabled += ReturnToPool;
        _path.Initialize(pathData.Nodes);

        SetupInitialPosition();
    }

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

    public void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnToPool(ObjectPoolManager.PathIndicatorTag, gameObject);
    }

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
